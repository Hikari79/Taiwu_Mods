using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony12;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ModExceptionHelper
{

    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }


    public static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry modEntry;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            TimeTestHelper.Start();
            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;
            Main.modEntry = modEntry;

            Application.logMessageReceivedThreaded += new Application.LogCallback(ExceptionHelper.Instance.Handler);
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {

        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }

    public class ExceptionHelper
    {
        private static ExceptionHelper instance;
        public static ExceptionHelper Instance
        {
            get
            {
                if(instance==null)
                {
                    instance = new ExceptionHelper();
                }
                return instance;
            }
        }

        private ExceptionHelper()
        {
            modsTypesNamesCache = GetAllModsTypesNames();
        }

        private readonly Dictionary<UnityModManager.ModInfo, List<string>> modsTypesNamesCache;

        public Dictionary<UnityModManager.ModInfo, List<string>> GetAllModsTypesNames()
        {
            Dictionary<UnityModManager.ModInfo, List<string>> result = new Dictionary<UnityModManager.ModInfo, List<string>>();
            foreach (var mod in UnityModManager.modEntries)
            {
                Assembly assembly = mod.Assembly;
                if (assembly == null)
                {
                    string text = System.IO.Path.Combine(mod.Path, mod.Info.AssemblyName);
                    assembly = Assembly.LoadFile(text);
                }
                if (assembly == null)
                {
                    Main.Logger.Log($"无法加载MOD\"{mod.Info.DisplayName}\"的程序集");
                    continue;
                }
                List<string> typesNames = new List<string>();
                assembly.GetTypes().Do(x => typesNames.Add(x.FullName));
                result.Add(mod.Info, typesNames);
            }
            return result;
        }
        public void SetErrorMod(Dictionary<UnityModManager.ModInfo,List<string>> obj,UnityModManager.ModInfo modInfo,string message)
        {
            if(obj.TryGetValue(modInfo,out List<string> value))
            {
                if (!value.Contains(message))
                    value.Add(message);                   
            }
            else
            {
                obj.Add(modInfo, new List<string>() { message });
            }
        }

        public bool GetErrorMods(string logString,string stackString,out Dictionary<UnityModManager.ModInfo, List<string>> result)
        {
            Dictionary<UnityModManager.ModInfo, List<string>> errorMods = new Dictionary<UnityModManager.ModInfo, List<string>>();
            try
            {
                foreach (var mod in modsTypesNamesCache)
                {
                    foreach(var name in mod.Value)
                    {
                        if (logString.Contains(name))
                        {
                            ExceptionHelper.Instance.SetErrorMod(errorMods, mod.Key, name);
                        }
                        if (stackString.Contains(name))
                        {
                            ExceptionHelper.Instance.SetErrorMod(errorMods, mod.Key, name);
                        }
                    }
                }
                string pattern = @"(?<= )\S+?_Patch\d+";
                foreach (Match match in Regex.Matches(logString + stackString, pattern))
                {
                    string matchString = match.Groups[0].Value;
                    string fullName = matchString.Substring(0, matchString.LastIndexOf('_'));
                    int num = fullName.LastIndexOf('.');
                    string methodName = fullName.Substring(num + 1, fullName.Length - fullName.LastIndexOf('.') - 1);
                    string typeName = fullName.Substring(0, fullName.LastIndexOf('.'));
                    string index = matchString.Substring(matchString.LastIndexOf("_Patch") + 6, matchString.Length - (matchString.LastIndexOf("_Patch") + 6));
                    Type classtyp = AccessTools.TypeByName(typeName);
                    if (classtyp == null)
                    {
                        Main.Logger.Log($"无法获取到{fullName}的类型");
                        continue;
                    }
                    MethodInfo methodInfo = classtyp.GetMethod(methodName, AccessTools.all);
                    if (methodInfo == null)
                    {
                        Main.Logger.Log($"无法获取到{fullName}的方法");
                        continue;
                    }
                    var info = PatchProcessor.GetPatchInfo(methodInfo);
                    if (info == null)
                    {
                        Main.Logger.Log($"无法获取到对{fullName}的补丁");
                        continue;
                    }
                    int patchIndex = int.Parse(index);
                    foreach (var patch in info.Prefixes)
                    {
                        if (patch.index == patchIndex)
                        {
                            UnityModManager.ModInfo modInfo = UnityModManager.FindMod(patch.owner).Info;
                            ExceptionHelper.Instance.SetErrorMod(errorMods, modInfo, matchString + ".Prefix()");
                        }
                    }
                    foreach (var patch in info.Postfixes)
                    {
                        if (patch.index == patchIndex)
                        {
                            UnityModManager.ModInfo modInfo = UnityModManager.FindMod(patch.owner).Info;
                            ExceptionHelper.Instance.SetErrorMod(errorMods, modInfo, matchString + ".Postfix()");
                        }
                    }
                }
                result = errorMods;
                return true;
            }
            catch(Exception e)
            {
                errorMods.Clear();
                errorMods.Add(Main.modEntry.Info, new List<string>() { e.Message,e.StackTrace});
                result = errorMods;
                return false;
            }
            
        }
        public void Handler(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                Main.Logger.Log(TimeTestHelper.Stop().ToString() + "ms");
                TimeTestHelper.Start();
                if (GetErrorMods(logString, stackTrace, out Dictionary<UnityModManager.ModInfo, List<string>> errorMods))
                {
                    if (errorMods.Count > 0)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine("          ");
                        stringBuilder.AppendLine($"找到可能引发此异常的MOD{errorMods.Count}个:");
                        int num = 1;
                        foreach (var kv in errorMods)
                        {
                            stringBuilder.AppendLine($"----------第{num}个----------");
                            stringBuilder.AppendLine($"MOD显示名称：{kv.Key.DisplayName}");
                            stringBuilder.AppendLine($"ID：{kv.Key.Id}");
                            stringBuilder.AppendLine($"作者：{kv.Key.Author}");
                            stringBuilder.AppendLine($"版本：{kv.Key.Version}");
                            stringBuilder.AppendLine("检测到调用栈中存在此MOD的类/方法：");
                            foreach (var s in kv.Value)
                            {
                                stringBuilder.AppendLine(s);
                            }
                            stringBuilder.AppendLine("建议将完整报错信息提交给MOD作者等待修复或者暂时卸载此MOD");
                            num++;
                        }
                        Main.Logger.Log(stringBuilder.ToString());
                    }
                    else
                        Main.Logger.Log("\n未检测到引发此异常的MOD,可能是游戏本身BUG或者是游戏/存档数据错误(TXT类MOD也可能引发此问题)。\n建议反馈给螺舟支持，反馈方式：http://help.conchship.com.cn/");
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("         ");
                    stringBuilder.AppendLine("MOD异常助手在检测该次异常时出现错误，无法继续检测引发该异常的MOD");
                    stringBuilder.AppendLine("错误信息：");
                    foreach(var kv in errorMods)
                    {
                        foreach (var s in kv.Value)
                            stringBuilder.AppendLine(s);
                    }
                    stringBuilder.AppendLine("请将上述错误信息提交给MOD异常助手的作者以修复本MOD（贴吧/NGA均可）");
                    Main.Logger.Log(stringBuilder.ToString());
                }
                Main.Logger.Log(TimeTestHelper.Stop().ToString() + "ms");
            }
        }
    }
    public static class TimeTestHelper
    {
        private static Stopwatch stopwatch;
        public static void Start()
        {
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }
            else
            {
                stopwatch.Restart();
            }
        }
        public static long Stop()
        {
            stopwatch.Stop();
            return stopwatch.ElapsedTicks;
        }
    }
}
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

namespace GongFaLevelUp
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
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            
            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            if(modEntry.Enabled)
            {
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                catch(Exception e)
                {
                    modEntry.Enabled = false;
                    Main.enabled = false;
                    modEntry.Active = false;
                }
            }


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

    public class ModData
    {
        private static readonly int baseGongFaNeedExp = 100;
        private static ModData _modData;
        public static ModData Instance
        {
            get
            {
                if(_modData==null)
                {
                    _modData = new ModData();
                }
                return _modData;
            }
        }
        private ModData()
        {
            gongFaFixData = new Dictionary<int, Dictionary<int, string>>();
            gongFaExp = new Dictionary<int, int[]>();
        }

        private Dictionary<int, Dictionary<int, string>> gongFaFixData;

        public int GetGongFaFixData(int gongFaID,int index,int defaultValue)
        {
            if(gongFaFixData.TryGetValue(gongFaID,out Dictionary<int, string> data))
            {
                if(data.TryGetValue(index,out string value))
                {
                    return int.Parse(value);
                }
            }
            return defaultValue;
        }

        public T GetGongFaFixData<T>(int gongFaID, int index, T defaultValue)
        {
            if (gongFaFixData.TryGetValue(gongFaID, out Dictionary<int, string> data))
            {
                if (data.TryGetValue(index, out string value))
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            return defaultValue;
        }
        public enum FixMode
        {
            发挥上限上升=711, 使用需求下降
        }
        public void FixGongFaData(int gongFaID,FixMode gongFaFixMode,object value)
        {
            if(gongFaFixData.TryGetValue(gongFaID,out Dictionary<int,string> data))
            {
                string oldValue;
                int index = (int)gongFaFixMode;
                bool flag = data.TryGetValue(index, out oldValue);
                switch(gongFaFixMode)
                {
                    case FixMode.发挥上限上升:
                        {
                            if (flag)
                                data[index] = (int.Parse(oldValue) + Convert.ToInt32(value)).ToString();
                            else
                                data[index] = Convert.ToInt32(value).ToString();
                            break;
                        }
                }
            }
            else
            {
                gongFaFixData.Add(gongFaID, new Dictionary<int, string>());
                FixGongFaData(gongFaID, gongFaFixMode, value);
            }
        }

        /// <summary>
        /// 记录功法的其他信息，key=功法ID，value=信息，int[0]=当前强化等级,int[1]=当前强化所需实战经验,int[3]=此功法当前实战经验 
        /// </summary>
        private Dictionary<int,int[]> gongFaExp;
        public int GetGongFaExpData(int gongFaID,int index)
        {
            if(gongFaExp.TryGetValue(gongFaID,out int[] data))
            {
                return data[index];
            }
            else
            {
                gongFaExp.Add(gongFaID, new int[] { 0, baseGongFaNeedExp,0 });
                return gongFaExp[gongFaID][index];
            }

        }

        [HarmonyPatch(typeof(DateFile), "GetGongFaMaxUsePower")]
        public class GongFaLevelUp_DateFile_GetGongFaMaxUsePower_Patch
        {
            public static void Postfix(int actorId, int gongFaId,ref int __result)
            {
                if (!Main.enabled)
                    return;
                if (actorId != DateFile.instance.MianActorID())
                    return;
                int addValue = ModData.Instance.GetGongFaFixData(gongFaId,(int)FixMode.发挥上限上升, 0);
                __result += addValue;
                return;
            }
        }
    }

    public static class Utils
    {
        static Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
        static Dictionary<string, FieldInfo> fieldInfoCache = new Dictionary<string, FieldInfo>();
        public static BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;
        public static object Invoke(object instance,string name,params object[] objs)
        {
            if(methodCache.TryGetValue(name,out MethodInfo methodInfo))
            {
                return methodInfo.Invoke(instance, all, null, objs, System.Globalization.CultureInfo.CurrentCulture);
            }
            else
            {
                MethodInfo method = instance.GetType().GetMethod(name, all);
                methodCache.Add(name, method);
                return method.Invoke(instance, all, null, objs, System.Globalization.CultureInfo.CurrentCulture);
            }
        }
        public static object GetValue(object instance,string name)
        {
            if (fieldInfoCache.TryGetValue(name, out FieldInfo fieldInfo))
            {
                return fieldInfo.GetValue(instance);
            }
            else
            {
                FieldInfo field = instance.GetType().GetField(name, all);
                fieldInfoCache.Add(name, field);
                return fieldInfo.GetValue(instance);
            }
        }
        public static void SetValue(object instance,string name,object value)
        {
            if (fieldInfoCache.TryGetValue(name, out FieldInfo fieldInfo))
            {
                fieldInfo.SetValue(instance, value);
            }
            else
            {
                FieldInfo field = instance.GetType().GetField(name, all);
                fieldInfoCache.Add(name, field);
                field.SetValue(instance, value);
            }
        }
    }


    public class GongFaLevelUP
    {

        public static void NeedLevelUP(int gongFaID)
        {

        }

        /// <summary>
        /// 替换修习度为100的功法的修习按钮功能
        /// </summary>
        [HarmonyPatch(typeof(BuildingWindow), "StudySkillUp")]
        public class GongFaLevelUP_BuildingWindow_StudySkillUP
        {
            public static bool Prefix(int ___studySkillId, int ___skillUpUseTime)
            {
                if (!Main.enabled)
                    return true;
                int actorID = DateFile.instance.MianActorID();
                int gongFaID = ___studySkillId;
                int gongFaLevel = DateFile.instance.GetGongFaLevel(actorID, gongFaID, 0);
                if (gongFaID <= 0 || gongFaLevel < 100 || BuildingWindow.instance.studySkillTyp != 17)
                    return true;
                int gongFaExLevel = ModData.Instance.GetGongFaExpData(gongFaID, 0);
                if (gongFaExLevel != 0 && gongFaExLevel % 20 == 0)
                {
                    NeedLevelUP(gongFaID);
                    return false;
                }
                int[] studyNeedCost = (int[])Utils.Invoke(BuildingWindow.instance, "GetMaxStudyNeedCost", new object[] { gongFaID, 100, HomeSystem.instance.homeMapPartId, HomeSystem.instance.homeMapPlaceId, HomeSystem.instance.homeMapbuildingIndex, 66, 0 });
                studyNeedCost[0] = studyNeedCost[0] * (100 + 5 * ModData.Instance.GetGongFaExpData(gongFaID, 0)) / 100;
                if (DateFile.instance.gongFaExperienceP < studyNeedCost[0])
                {
                    YesOrNoWindow.instance.SetYesOrNoWindow(-1, DateFile.instance.massageDate[7006][2].Split(new char[]
                    {
                        '|'
                    })[0], DateFile.instance.massageDate[7006][2].Split(new char[]
                    {
                        '|'
                    })[1], false, true);
                    return false;
                }
                if (DateFile.instance.dayTime < ___skillUpUseTime)
                {
                    YesOrNoWindow.instance.SetYesOrNoWindow(-1, DateFile.instance.massageDate[7006][4].Split(new char[]
                    {
                        '|'
                    })[0], DateFile.instance.massageDate[7006][4].Split(new char[]
                    {
                        '|'
                    })[1], false, true);
                    return false;
                }
                UIDate.instance.ChangeTime(false, ___skillUpUseTime);
                DateFile.instance.ChangeGongfaExp(-studyNeedCost[0], false);
                ModData.Instance.FixGongFaData(gongFaID, ModData.FixMode.发挥上限上升, 1);
                BuildingWindow.instance.UpdateStudySkillWindow();
                BuildingWindow.instance.UpdateLevelUPSkillWindow();
                BuildingWindow.instance.UpdateReadBookWindow();
                return false;
            }
        }


        /// <summary>
        /// 替换修习度超过100后修习界面的文本显示
        /// </summary>
        [HarmonyPatch(typeof(BuildingWindow), "UpdateStudySkillWindow")]
        public class GongFaLevelUp_BuildingWindow_UpdateStudySkillWindow
        {
            public static void Postfix(int ___studySkillId, int ___studySkillTyp)
            {
                if (!Main.enabled)
                    return;
                int gongFaID = ___studySkillId;
                if (gongFaID <= 0)
                    return;
                if (___studySkillTyp != 17)
                    return;
                int actorID = DateFile.instance.MianActorID();
                int gongFaLevel = DateFile.instance.GetGongFaLevel(actorID, gongFaID, 0);
                if (gongFaLevel < 100)
                    return;

                BuildingWindow.instance.gongFaLevelParts.Do((x => x.SetActive(true)));
                int gongFaExLevel = ModData.Instance.GetGongFaExpData(gongFaID, 0);
                BuildingWindow.instance.gongFaLevelBar.fillAmount =gongFaExLevel % 100 / 100f;
                BuildingWindow.instance.gongFaLevelBar.GetComponent<Image>().color = (gongFaExLevel > 20 ? new Color(0.294117659f, 0.09803922f, 0.09803922f) : new Color(0.392156869f, 0.784313738f, 0f));
                BuildingWindow.instance.gongFaLevelText.text = gongFaExLevel.ToString() + "%";

            }
        }
    }
}
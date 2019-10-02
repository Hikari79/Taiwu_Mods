using UnityEngine;
using System;
using UnityModManagerNet;
using UnityEngine.Networking;
using System.Reflection;
using System.Collections;
using UnityEngine.Events;
using Newtonsoft.Json;
using System.IO;
using Harmony12;

namespace RobTomb
{
    public class AutoUpdate
    {
        public enum Status
        {
            initial,networkError,checkUpdateing,updating,httpError,needUpdate,newest,error, updateSuccessfully
        }
        public static AutoUpdate instance;
        private static string output = string.Empty;
        private static string checkUpdateUrl = "https://github.com/Charlotte-poi/Taiwu_Mods/raw/master/Download/UpdateInfo.json";
        private static string downloadUrl = "";
        private static UnityWebRequest www;
        public static Status status = Status.initial;

        public static void OnGUI(UnityModManager.ModEntry modEntry,ref bool autoCheckUpdate)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("更新设置:");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            autoCheckUpdate = GUILayout.Toggle(autoCheckUpdate,"启动时自动检测更新");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("检查更新"))
            {
                CheckUpdate(modEntry);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            switch(status)
            {
                case Status.networkError:
                    output = "network error,请检查网络连接";
                    break;
                case Status.checkUpdateing:
                    output = $"正在检测更新,{www.downloadProgress*100}%已完成";
                    break;
                case Status.error:
                    break;
                case Status.needUpdate:
                    output = "有可用更新";
                    break;
                case Status.httpError:
                    output = "httperror.";
                    break;
                case Status.newest:
                    output = "当前已是最新版本";
                    break;
                case Status.updating:
                    output = $"正在下载更新中,{www.downloadProgress*100}%已完成";
                    break;
                case Status.updateSuccessfully:
                    output = "下载更新包成功，请关闭游戏用umm更新至最新版本";
                    break;
            }
            if (output != string.Empty)
                GUILayout.Label(output);
            GUILayout.BeginHorizontal();
            if(status==Status.needUpdate)
            {
                if (GUILayout.Button("更新"))
                {
                    DateFile.instance.StartCoroutine(Update(modEntry,downloadUrl));
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        public static void CheckUpdate(UnityModManager.ModEntry modEntry)
        {
            status = Status.checkUpdateing;
            if (!UnityModManager.HasNetworkConnection())
            {
                status = Status.networkError;
                return;
            }
            SingletonObject.getInstance<YieldHelper>().StartYield(HasNewerVersion(modEntry, checkUpdateUrl));
        }

        private static IEnumerator HasNewerVersion(UnityModManager.ModEntry modEntry, string url)
        {
            www = UnityWebRequest.Get(url);
            www.timeout = 100;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                status = Status.httpError;
            }
            else
            {
                UpdateInfo updateInfo = ParseJson(www.downloadHandler.text,modEntry.Info.Id);
                if(VersionCompare(modEntry.Info.Version , updateInfo.latestVersion))
                {
                    downloadUrl = updateInfo.downLoadUrl;
                    status = Status.needUpdate;
                    modEntry.NewestVersion = new Version(updateInfo.latestVersion);
                }
                else if(status!=Status.error)
                {
                    status = Status.newest;
                }
            }
        }
        private static bool VersionCompare(string str1,string str2)
        {
            try
            {
                string[] s1 = str1.Split('.');
                string[] s2 = str2.Split('.');
                for (int i = 0; i < Mathf.Min(s1.Length,s2.Length); i++)
                {
                    if (int.Parse(s1[i]) < int.Parse(s2[i]))
                        return true;
                }
                if (s1.Length < s2.Length)
                    return true;
                return false;
            }
            catch(Exception e)
            {
                status = Status.error;
                output = e.Message;
                return false;
            }

        }
        private  static UpdateInfo ParseJson(string json,string modName)
        {
            UpdateInfo[] updateInfos = JsonConvert.DeserializeObject<UpdateInfo[]>(json);
            foreach(var updateinfo in updateInfos)
            {
                if (updateinfo.modName == modName)
                    return updateinfo;
            }
            status = Status.error;
            output = "无此mod资料";
            return new UpdateInfo("modname","0.0.0","");
        }

        public static IEnumerator Update(UnityModManager.ModEntry modEntry, string url)
        {
            www = UnityWebRequest.Get(url);
            www.timeout = 100;
            status = Status.updating;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                status = Status.httpError;
            }
            else
            {
                string[] name = downloadUrl.Split('/');
                using (FileStream fileStream = new FileStream(Path.Combine(Environment.CurrentDirectory, "UnityModManager", "The Scroll Of Taiwu",modEntry.Info.Id,name[name.Length-1]), FileMode.Create))
                {
                    fileStream.Write(www.downloadHandler.data,0, www.downloadHandler.data.Length);
                }
                status = Status.updateSuccessfully;
            }
        }
    }

    public class UpdateInfo
    {
        public string modName;
        public string latestVersion;
        public string downLoadUrl;
        public UpdateInfo(string name,string version,string url)
        {
            modName = name;
            latestVersion = version;
            downLoadUrl = url;
        }
    }
}

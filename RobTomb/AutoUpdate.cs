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
using System.Runtime.InteropServices;
using System.Threading;

namespace RobTomb
{
    public class AutoUpdate
    {
        public enum Status
        {
            initial, networkError, checkUpdateing, updating, httpError, needUpdate, newest, error, updateSuccessfully
        }
        private static AutoUpdate _instance;
        public static AutoUpdate Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AutoUpdate();
                return _instance;
            }
        }



        public UpdateInfo updateInfo;
        private string ouput;
        private readonly string checkUpdateUrl = "https://github.com/Charlotte-poi/Taiwu_Mods/raw/master/Download/UpdateInfo.json";
        private string downloadUrl;
        private UnityWebRequest www;
        private Thread _thread;
        private Thread Thread
        {
            get
            {
                if (_thread == null)
                    _thread = new Thread(new ThreadStart(GetWindow));
                if(_thread.IsAlive)
                {
                    _thread.Abort();
                    Main.Logger.Log("结束线程");
                }
                _thread = new Thread(new ThreadStart(GetWindow));
                return _thread;
            }
        }
        public Status status = Status.initial;

        public void OnGUI(UnityModManager.ModEntry modEntry, ref bool autoCheckUpdate)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("更新设置:");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            autoCheckUpdate = GUILayout.Toggle(autoCheckUpdate, "启动时自动检测更新");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button((string.IsNullOrEmpty(Main.settings.ummPath)) ? "设置umm安装地址" : "当前设定的umm路径为:"+Main.settings.ummPath))
            {
                Thread.Start();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("检查更新"))
            {
                CheckUpdate(modEntry);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            switch (Instance.status)
            {
                case Status.networkError:
                    Instance.ouput = "network error,请检查网络连接";
                    break;
                case Status.checkUpdateing:
                    Instance.ouput = $"正在检测更新,{Instance.www.downloadProgress * 100}%已完成";
                    break;
                case Status.error:
                    break;
                case Status.needUpdate:
                    Instance.ouput = "有可用更新\n";
                    Instance.ouput += Instance.updateInfo.updateInfo;
                    break;
                case Status.httpError:
                    Instance.ouput = "httperror.";
                    break;
                case Status.newest:
                    Instance.ouput = "当前已是最新版本";
                    break;
                case Status.updating:
                    Instance.ouput = $"正在下载更新中,已完成{Instance.www.downloadProgress * 100}%";
                    break;
                case Status.updateSuccessfully:
                    Instance.ouput = "下载更新包成功，请关闭游戏用umm更新至最新版本";
                    break;
            }
            if (Instance.ouput != string.Empty)
                GUILayout.Label(Instance.ouput);
            GUILayout.BeginHorizontal();
            if (Instance.status == Status.needUpdate)
            {
                if (GUILayout.Button("更新"))
                {
                    if (isPathCorrect())
                    {
                        DateFile.instance.StartCoroutine(Update(modEntry, Instance.downloadUrl));
                    }
                    else
                    {
                        Thread.Start();
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }


        private bool isPathCorrect()
        {
            if(Directory.Exists(Path.Combine(Environment.CurrentDirectory, "UnityModManager", "The Scroll Of Taiwu")))
            {
                Main.settings.ummPath = Path.Combine(Environment.CurrentDirectory, "UnityModManager");
                return true;
            }
            if (string.IsNullOrEmpty(Main.settings.ummPath))
                return false;
            if (!Directory.Exists(Main.settings.ummPath))
                return false;
            if (!Directory.Exists(Path.Combine(Main.settings.ummPath, "The Scroll Of Taiwu")))
                return false;
            return true;
        }

        private void GetWindow()
        {
            OpenDialogDir ofn2 = new OpenDialogDir();
            ofn2.pszDisplayName = new string(new char[2000]); ;     // 存放目录路径缓冲区  
            ofn2.lpszTitle ="请手动选择umm路径";// 标题  
                                            //ofn2.ulFlags = BIF_NEWDIALOGSTYLE | BIF_EDITBOX; // 新的样式,带编辑框  
            IntPtr pidlPtr = DllOpenFileDialog.SHBrowseForFolder(ofn2);

            char[] charArray = new char[2000];
            for (int i = 0; i < 2000; i++)
                charArray[i] = '\0';

            DllOpenFileDialog.SHGetPathFromIDList(pidlPtr, charArray);
            string fullDirPath = new String(charArray);
            Main.settings.ummPath = fullDirPath.Substring(0, fullDirPath.IndexOf('\0'));
        }

        public void CheckUpdate(UnityModManager.ModEntry modEntry)
        {
            Instance.status = Status.checkUpdateing;
            if (!UnityModManager.HasNetworkConnection())
            {
                Instance.status = Status.networkError;
                return;
            }
            SingletonObject.getInstance<YieldHelper>().StartYield(HasNewerVersion(modEntry, Instance.checkUpdateUrl));
        }

        private IEnumerator HasNewerVersion(UnityModManager.ModEntry modEntry, string url)
        {
            Instance.www = UnityWebRequest.Get(url);
            Instance.www.timeout = 100;
            yield return Instance.www.SendWebRequest();
            if (Instance.www.isNetworkError || Instance.www.isHttpError)
            {
                Instance.status = Status.httpError;
            }
            else
            {
                Instance.updateInfo = ParseJson(Instance.www.downloadHandler.text, modEntry.Info.Id);
                if (VersionCompare(modEntry.Info.Version, Instance.updateInfo.latestVersion))
                {
                    Instance.downloadUrl = Instance.updateInfo.downLoadUrl;
                    Instance.status = Status.needUpdate;
                    modEntry.NewestVersion = new Version(Instance.updateInfo.latestVersion);
                }
                else if (Instance.status != Status.error)
                {
                    Instance.status = Status.newest;
                }
            }
        }
        private bool VersionCompare(string str1, string str2)
        {
            try
            {
                string[] s1 = str1.Split('.');
                string[] s2 = str2.Split('.');
                for (int i = 0; i < Mathf.Min(s1.Length, s2.Length); i++)
                {
                    if (int.Parse(s1[i]) < int.Parse(s2[i]))
                        return true;
                }
                if (s1.Length < s2.Length)
                    return true;
                return false;
            }
            catch (Exception e)
            {
                Instance.status = Status.error;
                Instance.ouput = e.Message;
                return false;
            }

        }
        private UpdateInfo ParseJson(string json, string modName)
        {
            UpdateInfo[] updateInfos = JsonConvert.DeserializeObject<UpdateInfo[]>(json);
            foreach (var updateinfo in updateInfos)
            {
                if (updateinfo.modName == modName)
                    return updateinfo;
            }
            Instance.status = Status.error;
            Instance.ouput = "无此mod资料";
            return new UpdateInfo("modname", "0.0.0", "","");
        }

        public IEnumerator Update(UnityModManager.ModEntry modEntry, string url)
        {
            Instance.www = UnityWebRequest.Get(url);
            Instance.www.timeout = 100;
            Instance.status = Status.updating;
            yield return Instance.www.SendWebRequest();
            if (Instance.www.isNetworkError || Instance.www.isHttpError)
            {
                Instance.status = Status.httpError;
            }
            else
            {
                string[] name = Instance.downloadUrl.Split('/');
                using (FileStream fileStream = new FileStream(Path.Combine(Main.settings.ummPath, "The Scroll Of Taiwu", modEntry.Info.Id, name[name.Length - 1]), FileMode.Create))
                {
                    fileStream.Write(Instance.www.downloadHandler.data, 0, Instance.www.downloadHandler.data.Length);
                }
                Instance.status = Status.updateSuccessfully;
            }
        }
    }

    public class UpdateInfo
    {
        public string modName;
        public string latestVersion;
        public string downLoadUrl;
        public string updateInfo;
        public UpdateInfo(string name, string version, string url,string updateinfo)
        {
            modName = name;
            latestVersion = version;
            downLoadUrl = url;
            this.updateInfo = updateinfo;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]

    public class OpenDialogFile
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public String filter = null;
        public String customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public String file = null;
        public int maxFile = 0;
        public String fileTitle = null;
        public int maxFileTitle = 0;
        public String initialDir = null;
        public String title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public String defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public String templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenDialogDir
    {
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr pidlRoot = IntPtr.Zero;
        public String pszDisplayName = null;
        public String lpszTitle = null;
        public UInt32 ulFlags = 0;
        public IntPtr lpfn = IntPtr.Zero;
        public IntPtr lParam = IntPtr.Zero;
        public int iImage = 0;
    }

    public class DllOpenFileDialog
    {
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenDialogFile ofn);

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenDialogFile ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In, Out] OpenDialogDir ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);

    }
}

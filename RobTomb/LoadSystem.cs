using UnityEngine;
using Harmony12;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;
using GameData;
using System.Diagnostics;
using System.Linq;
using UnityModManagerNet;

namespace LoadSystem
{
    public class DataManager
    {
        private static DataManager instance;
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataManager();
                return instance;
            }
        }
        private DataManager()
        {
            modData = new ModData();
            enabled = false;
        }
        public ModData modData;   //供mod调用的mod数据，DataManager.Instance.modDate
        public bool enabled;
        public UnityModManager.ModEntry mod;
        public UnityModManager.ModEntry.ModLogger logger;

        public void OnToggle(bool value)
        {
            enabled = value;
        }
        public void Register(string path,UnityModManager.ModEntry mod)
        {
            if (Directory.Exists(path))
                BaseData.resdir = path;
            else
                BaseData.resdir = "";
            this.mod = mod;
            logger = this.mod.Logger;
        }

        public int GetGameActorData(int actorID,int index,bool applyBonus = true)
        {
            try
            {
                string s = DateFile.instance.GetActorDate(actorID, index, applyBonus);
                if (int.TryParse(s, out int value))
                {
                    return value;
                }
                throw new Exception($"[{mod.Info.DisplayName}]获取游戏人物数据出错,ID = {actorID},index = {index},applyBonus = {applyBonus}");
            }
            catch(Exception e)
            {
                throw e;
            }
            
        }

    }
    public class ModData
    {
        public enum DataType
        {       
            actorDate,actorsGongFas
        }
        public Dictionary<int, Dictionary<int, string>> actorsDate;                              //人物数据 
        public Dictionary<int, SortedDictionary<int, int[]>> actorsGongFas;                //功法数据
        public string GetActorData(int actorID, int index)
        {
            if (actorsDate.TryGetValue(actorID, out Dictionary<int, string> dic))
            {
                if (dic.TryGetValue(index, out string result))
                {
                    return result;
                }
            }
            throw new Exception($"[{DataManager.Instance.mod.Info.DisplayName}]获取MOD人物数据出错，actorID={actorID},key={index},请将此信息反馈给MOD作者");
        }
        public bool TryGetActorData(int actorID, int index, out string result)
        {
            if (actorsDate.TryGetValue(actorID, out Dictionary<int, string> dic))
            {
                if (dic.TryGetValue(index, out string s))
                {
                    result = s;
                    return true;
                }
            }
            result = string.Empty;
            return false;
        }

        public void SetActorData(int actorID, int key, string value)
        {
            if (actorsDate.ContainsKey(actorID))
            {
                actorsDate[actorID][key] = value;
                return;
            }
            actorsDate.Add(actorID, new Dictionary<int, string>());
            actorsDate[actorID][key] = value;
        }

        public void SetGameActorData(int actorID, int key, string value, bool add)
        {
            if (add)
            {
                string s = Characters.GetCharProperty(actorID, key);
                Characters.SetCharProperty(actorID, key, (int.Parse(value) + int.Parse(s)).ToString());
                return;
            }
            Characters.SetCharProperty(actorID, key, value);
        }

        /// <summary>
        /// 加载mod数据
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "LoadDate")]
        public class LoadModData_DateFile_LoadDate_Patch
        {
            public static void Postfix()
            {
                ModData data = DataManager.Instance.modData;
                if (DateFile.instance.modDate.TryGetValue("RobTomb", out Dictionary<string, string> temp))
                {
                    if (temp.TryGetValue("actorsDate", out string s))
                    {
                        data.actorsDate = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, string>>>(s)??new Dictionary<int, Dictionary<int, string>>();
                    }
                    else
                        data.actorsDate = new Dictionary<int, Dictionary<int, string>>();
                    if (temp.TryGetValue("actorsGongFas", out s))
                    {
                        data.actorsGongFas = JsonConvert.DeserializeObject<Dictionary<int, SortedDictionary<int, int[]>>>(s)??new Dictionary<int, SortedDictionary<int, int[]>>();  
                        foreach (var actorGongFa in data.actorsGongFas)
                        {
                            if(actorGongFa.Value.ContainsKey(20409))                        //a little patch
                            {
                                actorGongFa.Value.Add(BaseData.GetNewID(BaseData.BaseDataType.GongFa_Date, 20409), actorGongFa.Value[20409]);
                                actorGongFa.Value.Remove(20409);
                            }
                            if (DateFile.instance.actorGongFas.ContainsKey(actorGongFa.Key))
                            {
                                foreach (var kv in actorGongFa.Value)
                                {
                                    DateFile.instance.actorGongFas[actorGongFa.Key][kv.Key] = kv.Value;
                                }

                            }
                        }
                    }
                    else
                        data.actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
                }
                else
                {
                    data.actorsDate = new Dictionary<int, Dictionary<int, string>>();
                    data.actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
                }
            }
        }

        /// <summary>
        /// 存储mod数据
        /// </summary>
        [HarmonyPatch(typeof(ArchiveSystem.SaveGame), "StartSavingData")]
        public class SaveModData_ArchiveSystem_SaveGame_StartSavingDate
        {
            public static bool start =false;
            public static void Prefix()
            {
                start = true;
                if (!DataManager.Instance.enabled)
                    return;
                FixOldSave();
                if (!DateFile.instance.modDate.ContainsKey("RobTomb"))
                    DateFile.instance.modDate.Add("RobTomb", new Dictionary<string, string>());
                DateFile.instance.modDate["RobTomb"]["actorsDate"] = JsonConvert.SerializeObject(DataManager.Instance.modData.actorsDate);
                DateFile.instance.modDate["RobTomb"]["actorsGongFas"] = JsonConvert.SerializeObject(DataManager.Instance.modData.actorsGongFas);
            }

            /// <summary>
            /// 将老版本盗墓直接保存到存档里的数据转移到外面
            /// </summary>
            private static void FixOldSave()
            {
                var data = DataManager.Instance.modData;
                DataManager.Instance.logger.Log("开始转移盗墓笔记存档数据（如果存在）");
                int num = 0;
                List<int> actorsIDs = new List<int>(GameData.Characters.GetAllCharIds());
                foreach (var actorID in actorsIDs)
                {
                    foreach (var key in BaseData.actorsDateKeys)
                    {
                        string date;
                        if (Characters.HasCharProperty(actorID, key))
                        {
                            date = Characters.GetCharProperty(actorID, key);
                            //DataManager.Instance.logger.Log("[" + actorID + "]" + "[" + key + "]" + ":" + date);
                            data.SetActorData(actorID, key, date);
                            Characters.RemoveCharProperty(actorID, key);
                            num++;
                        }
                    }
                }

                foreach (var actorgongfa in DateFile.instance.actorGongFas)
                {
                    foreach (var key in BaseData.actorsGongFasKeys)
                    {
                        if (actorgongfa.Value.TryGetValue(key, out int[] date))
                        {
                            if (data.actorsGongFas.ContainsKey(actorgongfa.Key))
                                data.actorsGongFas[actorgongfa.Key][key] = date;
                            else
                            {
                                data.actorsGongFas.Add(actorgongfa.Key, new SortedDictionary<int, int[]>());
                                data.actorsGongFas[actorgongfa.Key][key] = date;
                            }
                            actorgongfa.Value.Remove(key);
                            num++;
                        }
                    }
                }
                DataManager.Instance.logger.Log($"转移完毕，共转移{num}条数据。");
            }
        }

        [HarmonyPatch(typeof(ArchiveSystem.SaveGame), "DoSavingAgent")]
        public class ReLoadModData_ArchiveSystem_SaveGame_DoSavingAgent
        {
            public static void Postfix()
            {
                if (!DataManager.Instance.enabled)
                    return;
                if(SaveModData_ArchiveSystem_SaveGame_StartSavingDate.start)
                {
                    SaveModData_ArchiveSystem_SaveGame_StartSavingDate.start = false;
                    DataManager.Instance.logger.Log("start reload moddata");
                    LoadModData_DateFile_LoadDate_Patch.Postfix();
                    DataManager.Instance.logger.Log("reload over");
                }
            }
        }
    }
    public static class BaseData
    {
        public static string resdir;
        public enum BaseDataType
        {
            Event_Date, Item_Date, PresetActor_Date, EnemyTeam_Date, GongFaOtherFPower_Date,GongFa_Date
        }
        public static Dictionary<string, Dictionary<int, Dictionary<int, string>>> nameToDic = new Dictionary<string, Dictionary<int, Dictionary<int, string>>>();
        public static Dictionary<string, Dictionary<int, int>> idConvert = new Dictionary<string, Dictionary<int, int>>();
        public static Dictionary<string, int[]> fixData = new Dictionary<string, int[]>()     //本身行号更改后，需要一并更改的数据项号
        {
            {"Event_Date", new int [] {5,7} },
            {"Item_Date",new int[]{999} },
            {"PresetActor_Date",new int []{997} }
        };
        public static Dictionary<string, Dictionary<string, int[]>> fixOtherData = new Dictionary<string, Dictionary<string, int[]>>()//更改本身行号以后，需要更改其他数据簿中的行号
        {
            {
                "EnemyTeam_Date",new Dictionary<string, int[]>{{"Event_Date",new int[]{101,102,103 } } }
            },
            {
                "PresetActor_Date",
                new Dictionary<string, int[]>
                {
                    { "Item_Date",new int[] {201,301,302,303,304,305,306,307,308,309,310,311,312 } },
                    { "GongFa_Date",new int[]{996} }
                 }
            },
            {"Event_Date",new Dictionary<string, int[]>{{"EnemyTeam_Date",new int[] {8} }} },
            {"GongFa_Date",new Dictionary<string, int[]>{{"GongFaOtherFPower_Date",new int[] {103,104 } } } }
        };
        public static List<int> actorsDateKeys = new List<int> { 79 };
        public static List<int> actorsGongFasKeys = new List<int> { 20409 };

        public static int GetNewID(BaseDataType type,int oldID)
        {
            string typeStr = Enum.GetName(typeof(BaseDataType), type);
            if(idConvert.ContainsKey(typeStr))
            {
                if (idConvert[typeStr].TryGetValue(oldID, out int value))
                    return value;
            }
            throw new Exception($"盗墓笔记无法找到相应的对象,类型:{typeStr},旧序号:{oldID}\n请将此信息反馈给作者.");
            

        }
        public static List<int> GetNewIDs(BaseDataType type)
        {
            string typeStr = Enum.GetName(typeof(BaseDataType), type);
            if (idConvert.ContainsKey(typeStr))
            {
                return new List<int>(idConvert[typeStr].Values);
            }
            return new List<int>();
        }

        /// <summary>
        /// 游戏开始读取数据
        /// </summary>
        [HarmonyPatch(typeof(GetSprites), "GetDate")]
        public class BaseData_GetSprites_GetDate_Patch
        {
            public static void Postfix(string dateName, Dictionary<int, Dictionary<int, string>> dateList, int passDateIndex = -1)
            {
                if (!DataManager.Instance.enabled)
                    return;
                //加载当前指定目录下对应txt
                string path = resdir + "\\" + dateName + ".txt";
                string text;
                if (File.Exists(path))
                {
                    using(StreamReader s = File.OpenText(path))
                    {
                        text =s.ReadToEnd();
                    }
                }
                else
                {
                    return;
                }
                SortedDictionary<int, Dictionary<int, string>> _newDateList = new SortedDictionary<int, Dictionary<int, string>>();
                //array[x] =txt第x行 
                string[] array = text.Replace("\r", "").Split('\n');

                //array2 = 数据字典中的所有键
                string[] array2 = array[0].Split(new char[]
                {
            ','
                });

                //找到当前数据的最大键num
                List<int> list = new List<int>(dateList.Keys);
                int num = list[0];
                foreach (int i in list)
                {
                    if (i > num) num = i;
                }

                //创建新老id对应字典
                Dictionary<int, int> oldToNew = new Dictionary<int, int>();
                for (int i = 1; i < array.Length; i++)
                {
                    if (int.TryParse(array[i].Split(',')[0], out int id))
                        oldToNew.Add(id, num + i);
                }
                BaseData.idConvert.Add(dateName, oldToNew);


                //处理并写入数据
                bool needReplace = BaseData.fixData.ContainsKey(dateName);
                for (int i = 1; i < array.Length; i++)
                {
                    //array3 = 第i行的的数据集合
                    string[] array3 = array[i].Split(new char[]
                    {
                ','
                    });
                    if (array3[0] != "#" && array3[0] != "")
                    {
                        //行号替换
                        array3[0] = (num + i).ToString();

                        Dictionary<int, string> dictionary = new Dictionary<int, string>();
                        for (int j = 0; j < array2.Length; j++)
                        {
                            if (array2[j] != "#" && array2[j] != "" && int.Parse(array2[j]) != passDateIndex)
                            {
                                int number = int.Parse(array2[j]);
                                dictionary.Add(number, Regex.Unescape(array3[j]));

                                //对某些特殊值进行替换
                                if (needReplace)
                                {
                                    for (int n = 0; n < BaseData.fixData[dateName].Length; n++)
                                    {
                                        if (number == BaseData.fixData[dateName][n])
                                        {
                                            string branch = dictionary[number];
                                            string[] option = branch.Split('|');
                                            for (int m = 0; m < option.Length; m++)
                                            {
                                                string[] parm = option[m].Split('&');
                                                for (int k = 0; k < parm.Length; k++)
                                                {
                                                    foreach (KeyValuePair<int, int> id in BaseData.idConvert[dateName])
                                                    {
                                                        if (parm[k].Equals(id.Key.ToString()))
                                                        {
                                                            parm[k] = id.Value.ToString();
                                                            break;
                                                        }
                                                    }
                                                }
                                                option[m] = parm[0];
                                                for (int k = 1; k < parm.Length; k++)
                                                {
                                                    option[m] = option[m] + '&' + parm[k];
                                                }
                                            }
                                            branch = option[0];
                                            for (int m = 1; m < option.Length; m++)
                                            {
                                                branch = branch + '|' + option[m];
                                            }
                                            dictionary[number] = branch;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        SortedDictionary<int, Dictionary<int, string>> newDateList = _newDateList;
                        lock (newDateList)
                        {
                            _newDateList.Add(int.Parse(array3[0]), dictionary);
                        }
                    }
                }
                foreach (int key in _newDateList.Keys)
                {
                    dateList.Add(key, _newDateList[key]);
                }
                //建立名字与数据的字典
                BaseData.nameToDic.Add(dateName, dateList);
            }
        }



        /// <summary>
        /// 待所有数据加载完成后，对需要替换的id进行处理
        /// </summary>
        [HarmonyPatch(typeof(ArchiveSystem.GameData.ReadonlyData), "Load")]
        public class FixOtherData_ArchiveSystem_GameData_ReadonlyData_Load_Patch
        {
            public static void Postfix()
            {
                try
                {
                    if (!DataManager.Instance.enabled)
                        return;
                    foreach (string name in BaseData.fixOtherData.Keys)
                    {
                        DataManager.Instance.logger.Log($"开始更新{name}序号");
                        foreach (string fixname in BaseData.fixOtherData[name].Keys)
                        {
                            if (!BaseData.idConvert.ContainsKey(fixname))
                            {
                                break;
                            }
                            List<int> dataId = new List<int>(BaseData.idConvert[name].Values);
                            for (int i = 0; i < dataId.Count; i++)
                            {
                                Dictionary<int, string> data = BaseData.nameToDic[name][dataId[i]];
                                foreach (int index in BaseData.fixOtherData[name][fixname])
                                {
                                    string branch = data[index];
                                    string[] option = branch.Split('|');
                                    for (int m = 0; m < option.Length; m++)
                                    {
                                        string[] parm = option[m].Split('&');
                                        for (int k = 0; k < parm.Length; k++)
                                        {
                                            foreach (KeyValuePair<int, int> id in BaseData.idConvert[fixname])
                                            {
                                                if (parm[k].Equals(id.Key.ToString()))
                                                {
                                                    parm[k] = id.Value.ToString();
                                                    break;
                                                }
                                            }
                                        }
                                        option[m] = parm[0];
                                        for (int k = 1; k < parm.Length; k++)
                                        {
                                            option[m] = option[m] + '&' + parm[k];
                                        }
                                    }
                                    branch = option[0];
                                    for (int m = 1; m < option.Length; m++)
                                    {
                                        branch = branch + '|' + option[m];
                                    }
                                    data[index] = branch;
                                }
                            }

                        }
                        DataManager.Instance.logger.Log($"更新{name}序号完成");
                    }
                    DataManager.Instance.logger.Log("更新所有数据序号完成");

                    for(int i=0;i<actorsGongFasKeys.Count;i++)                                                                        //将mod添加的功法做个新老ID映射变化
                    {
                        actorsGongFasKeys[i] = GetNewID(BaseDataType.GongFa_Date, actorsGongFasKeys[i]);
                    }
                }
                catch (Exception e)
                {
                    DataManager.Instance.logger.Log("[error]FixOtherData_ArchiveSystem_GameData_ReadonlyData_Load_Patch.Postfix():" + e.Message);
                    throw e;
                }
            }

        }
    }
}
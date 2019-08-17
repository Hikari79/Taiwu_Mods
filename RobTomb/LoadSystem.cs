using UnityEngine;
using Harmony12;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace RobTomb
{
    public static class RobTomb_LoadData
    {
        public static string resdir;
        public static Dictionary<string, Dictionary<int, Dictionary<int, string>>> nameToData = new Dictionary<string, Dictionary<int, Dictionary<int, string>>>();
        public static Dictionary<string, Dictionary<int, int>> id = new Dictionary<string, Dictionary<int, int>>();
        public static Dictionary<string, int[]> fixData = new Dictionary<string, int[]>()
        {
            {"Event_Date", new int [] {5,7,8} },
            {"Item_Date",new int[]{999} },
            {"PresetActor_Date",new int []{997} }
        };
        public static Dictionary<string,Dictionary<string,int[]>> fixOtherData=new Dictionary<string, Dictionary<string, int[]>>()
        {
            {"EnemyTeam_Date",new Dictionary<string, int[]>{{"Event_Date",new int[]{101,102,103 } } } },
            {"PresetActor_Date",new Dictionary<string, int[]>{{ "Item_Date",new int[] {201,301,302,303,304,305,306,307,308,309,310,311,312 } },
                                                                                                    { "GongFa_Date",new int[]{996} }
                                                                                                 }
            },
            {"Event_Date",new Dictionary<string, int[]>{{"EnemyTeam_Date",new int[] {8} }} }
        };
    }

    /// <summary>
    /// 游戏开始读取数据
    /// </summary>
    [HarmonyPatch(typeof(GetSprites), "GetDate")]
    public class RobTomb_GetDate_Patch
    {
        public static void Postfix(string dateName, Dictionary<int, Dictionary<int, string>> dateList, int passDateIndex = -1)
        {
            if (!Main.enabled)
                return;
            //加载当前指定目录下对应txt
            string path = RobTomb_LoadData.resdir + "\\"+dateName+".txt";
            string text;
            if (File.Exists(path))
            {
                text = File.OpenText(path).ReadToEnd();
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
            foreach(int i in list)
            {
                if (i > num) num = i;
            }

            //创建新老id对应字典
            Dictionary<int,int> oldToNew = new Dictionary<int, int>();
            for (int i = 1; i < array.Length; i++)
            {
                int id;
                if(int.TryParse(array[i].Split(',')[0], out id))
                    oldToNew.Add(id,num+i);
            }
            RobTomb_LoadData.id.Add(dateName, oldToNew);


            //处理并写入数据
            bool needReplace = RobTomb_LoadData.fixData.ContainsKey(dateName);
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
                                for(int n=0;n<RobTomb_LoadData.fixData[dateName].Length;n++)
                                {
                                    if(number== RobTomb_LoadData.fixData[dateName][n])
                                    {
                                        string branch = dictionary[number];
                                        string[] option = branch.Split('|');
                                        for(int m = 0;m<option.Length;m++)
                                        {
                                            string[] parm = option[m].Split('&');
                                            for(int k=0;k<parm.Length;k++)
                                            {
                                                foreach (KeyValuePair<int, int> id in RobTomb_LoadData.id[dateName])
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
                                                option[m] =option[m]+'&' + parm[k];
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
            //建立名字与数据的zi'字典
            RobTomb_LoadData.nameToData.Add(dateName, dateList);

            Main.Logger.Log("加载" + dateName + "成功");             
        }
    }

    [HarmonyPatch(typeof(ArchiveSystem.GameData.ReadonlyData), "Load")]
    public class ArchiveSystem_GameData_ReadonlyData_Load_Patch
    {
        public static void Postfix()
        {
            try
            {
                if (!Main.enabled)
                    return;
                foreach (string name in RobTomb_LoadData.fixOtherData.Keys)
                {
                    Main.Logger.Log($"开始处理{name}");
                    foreach (string fixname in RobTomb_LoadData.fixOtherData[name].Keys)
                    {
                        if (!RobTomb_LoadData.id.ContainsKey(fixname))
                        {
                            break;
                        }
                        List<int> dataId = new List<int>(RobTomb_LoadData.id[name].Values);
                        for (int i = 0; i < dataId.Count; i++)
                        {
                            Dictionary<int, string> data = RobTomb_LoadData.nameToData[name][dataId[i]];
                            foreach (int index in RobTomb_LoadData.fixOtherData[name][fixname])
                            {
                                string branch = data[index];
                                string[] option = branch.Split('|');
                                for (int m = 0; m < option.Length; m++)
                                {
                                    string[] parm = option[m].Split('&');
                                    for (int k = 0; k < parm.Length; k++)
                                    {
                                        foreach (KeyValuePair<int, int> id in RobTomb_LoadData.id[fixname])
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
                    Main.Logger.Log($"处理{name}完成");
                }
                Main.Logger.Log("处理所有数据完成");

                Dictionary<int, string> gongfa = new Dictionary<int, string>(DateFile.instance.gongFaDate[20408]);
                gongfa[0] = "三华聚顶·极";
                gongfa[2] = "9";
                gongfa[63] = "100";
                gongfa[64] = "0.9";
                gongfa[66] = "83";
                gongfa[73] = "680";
                gongfa[99] = "三华者，玉华、金华、九华也。\n精化为气,气化为神,精气神三化合聚于上丹田,如草之开花结子,是为内丹炼成。\n炼精化气,炼气化神,炼神还虚,名为三华聚顶。";
                gongfa[5] = "9";
                gongfa[710] = "603&3.45|64&0.85|65&0.65|66&0.85|513&3.7";
                gongfa[50032] = "1";
                gongfa[50033] = "1";
                gongfa[51101] = "0.15";
                gongfa[51102] = "0.15";
                gongfa[51103] = "-0.05";
                gongfa[51104] = "-0.05";
                gongfa[51106] = "0.2";
                gongfa[103] = "779";
                gongfa[104] = "5779";
                gongfa[50073] = "0";
                DateFile.instance.gongFaDate.Add(20409, gongfa);
                DateFile.instance.gongFaFPowerDate.Add(5779, new Dictionary<int, string> { { 0, "逆·三华聚顶·极" }, { 1, "20007" }, { 2, "0" }, { 3, "4" }, { 4, "100" }, { 5, "0" }, { 6, "0" }, { 7, "0" }, { 8, "0" }, { 97, "精气合一" }, { 99, "受到伤害时用内外伤共同分担所受伤害" }, { 98, "" } });
                DateFile.instance.gongFaFPowerDate.Add(779, new Dictionary<int, string> { { 0, "正·三华聚顶·极" }, { 1, "20007" }, { 2, "0" }, { 3, "4" }, { 4, "100" }, { 5, "0" }, { 6, "0" }, { 7, "0" }, { 8, "0" }, { 97, "神形合一" }, { 99, "战斗时可同时提升提气架势" }, { 98, "" } });


            }
            catch (System.Exception e)
            {
                Main.Logger.Log("[error]RobTomb_LoadBaseDate_Patch:" + e.Message);
            }
            

            /*
            string s = "";
            foreach(int index in DateFile.instance.eventDate[RobTomb_LoadData.id["Event_Date"][19980141]].Keys)
            {
                s += index.ToString() + ':' + DateFile.instance.eventDate[RobTomb_LoadData.id["Event_Date"][19980141]][index] + "      ";
            }
            Main.Logger.Log(s);
            */
        }

    }

    [HarmonyPatch(typeof(DateFile), "LoadDate")]
    public class LoadDate_Patch
    {
        public static void Postfix()
        {            
            Dictionary<string, string> temp;
            if (DateFile.instance.modDate.TryGetValue("RobTomb", out temp))
            {
                string s;
                if (temp.TryGetValue("actorsDate", out s))
                {
                    ModDate.actorsDate = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, string>>>(s);
                    if (ModDate.actorsDate == null)
                        ModDate.actorsDate = new Dictionary<int, Dictionary<int, string>>();
                    foreach(var actorDate in ModDate.actorsDate)
                    {
                        if(DateFile.instance.actorsDate.ContainsKey(actorDate.Key))
                        {
                            foreach (var kv in actorDate.Value)
                                DateFile.instance.actorsDate[actorDate.Key][kv.Key] = kv.Value;
                        }
                    }
                }
                else
                    ModDate.actorsDate = new Dictionary<int, Dictionary<int, string>>();
                if (temp.TryGetValue("actorsGongFas", out s))
                {
                    ModDate.actorsGongFas = JsonConvert.DeserializeObject<Dictionary<int, SortedDictionary<int, int[]>>>(s);
                    if (ModDate.actorsGongFas == null)
                        ModDate.actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
                    foreach(var actorGongFa in ModDate.actorsGongFas)
                    {
                        if(DateFile.instance.actorGongFas.ContainsKey(actorGongFa.Key))
                        {
                            foreach (var kv in actorGongFa.Value)
                                DateFile.instance.actorGongFas[actorGongFa.Key][kv.Key] = kv.Value;
                        }
                    }
                }
                else
                    ModDate.actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
            }
            else
                DateFile.instance.modDate.Add("RobTomb", new Dictionary<string, string>());
        }
    }

    [HarmonyPatch(typeof(ArchiveSystem.GameData.DefaultData), "StartSerializingActorsData")]
    public class ArchiveSystem_GameData_DefaultData_StartSerializingActorsData_Patch
    {
        public static void Prefix(ref Dictionary<int, Dictionary<int, string>> actorsData)
        {
            if (!Main.enabled)
                return;
            Main.Logger.Log("开始转移盗墓笔记存档数据");
            ModDate.actorsDate = new Dictionary<int, Dictionary<int, string>>();
            foreach(var actordate in actorsData)
            {
                foreach(var key in ModDate.actorsDateKeys)
                {
                    string date;
                    if(actordate.Value.TryGetValue(key,out date))
                    {
                        Main.Logger.Log("["+actordate.Key +"]"+"["+key+"]"+":"+date);
                        if(ModDate.actorsDate.ContainsKey(actordate.Key))
                            ModDate.actorsDate[actordate.Key][key] = date;
                        else
                        {
                            ModDate.actorsDate.Add(actordate.Key, new Dictionary<int, string>());
                            ModDate.actorsDate[actordate.Key][key] = date;
                        }
                        actordate.Value.Remove(key);
                        Main.Logger.Log($"{actordate.Value.ContainsKey(key)}");
                    }
                }
            }
            ModDate.actorsGongFas = new Dictionary<int, SortedDictionary<int, int[]>>();
            foreach (var actorgongfa in DateFile.instance.actorGongFas)
            {
                foreach (var key in ModDate.actorsGongFasKeys)
                {
                    int[] date;
                    if (actorgongfa.Value.TryGetValue(key, out date))
                    {
                        if (ModDate.actorsGongFas.ContainsKey(actorgongfa.Key))
                            ModDate.actorsGongFas[actorgongfa.Key][key] = date;
                        else
                        {
                            ModDate.actorsGongFas.Add(actorgongfa.Key, new SortedDictionary<int, int[]>());
                            ModDate.actorsGongFas[actorgongfa.Key][key] = date;
                        }
                        actorgongfa.Value.Remove(key);
                    }
                }
            }
            if (!DateFile.instance.modDate.ContainsKey("RobTomb"))
                DateFile.instance.modDate.Add("RobTomb", new Dictionary<string, string>());
            DateFile.instance.modDate["RobTomb"]["actorsDate"] = JsonConvert.SerializeObject(ModDate.actorsDate);
            DateFile.instance.modDate["RobTomb"]["actorsGongFas"] = JsonConvert.SerializeObject(ModDate.actorsGongFas);
            Main.Logger.Log("转移完毕");
        }

        public static void Postfix()
        {
            LoadDate_Patch.Postfix();
        }
    }
}

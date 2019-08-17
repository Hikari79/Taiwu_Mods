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

namespace Ohlaohlaohla
{

    public class Settings : UnityModManager.ModSettings
    {
        public int spaceTime;
        public int defaultKey;
        public int minGongFaCanUseLevel;
        public bool usingGongFaEffect;
        public List<int> myVIPGongFa;
        public List<int> myConfirmedGongFa;
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }


    public static class Main
    {
        public static Dictionary<int,List<int>> gongfaCosts = new Dictionary<int, List<int>>();                                 //全摧破功法式消耗顺序（id-list<int>式类型）
        public static Dictionary<int, List<int>> battleGongfaCosts = new Dictionary<int, List<int>>();                     //战斗时全摧破功法式消耗顺序（id-list<int>式类型）
        public static List<int> battleUsingGongFa = new List<int>();
        public static GameObject comboAttackWindow;
        public static KeyCode button1 = KeyCode.Alpha1;
        public static KeyCode button2 = KeyCode.Alpha2;
        public static KeyCode button3 = KeyCode.Alpha3;
        public static int comboTime = 0;
        public static List<int> weaponAttackTypes;
        public static bool combo = false;
        public static int comboAttackIndex = -1;
        public static string textOfMinGongFaLevel;
        public static GameObject[] gameObjects = new GameObject[9];
        public static GameObject[] gameObjects2 = new GameObject[9];
        public static bool enabled;
        public static Settings settings;
        public static bool showGongFa = false;
        public static bool showGongFa2 = false;
        public static int showGongFaType = 0;
        public static int showGongFaType2 = 0;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;
            textOfMinGongFaLevel = settings.minGongFaCanUseLevel.ToString();
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
            bool inGame = SaveDateFile.instance.dateId != 0;
            List<int> removeId = new List<int>();
            GUILayout.BeginHorizontal();
            GUILayout.Label("连击超时后默认选项：");
            settings.defaultKey = GUILayout.Toolbar(settings.defaultKey, new string[] { "保持原有式不变", "1号式", "2号式", "3号式", "取消连击" });
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("可用摧破功法的最低修习程度（不可低于25）");
            textOfMinGongFaLevel=GUILayout.TextField(textOfMinGongFaLevel, 3, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            settings.usingGongFaEffect = GUILayout.Toggle(settings.usingGongFaEffect, "开启运功特效");
            int num;
            if(int.TryParse(textOfMinGongFaLevel,out num))
            {
                settings.minGongFaCanUseLevel = Mathf.Clamp(num, 25, 100);
            }
            GUILayout.Label("战斗时优先出现的功法:");
            if(!inGame)
            {
                GUILayout.Label("存档未载入！");
                return;
            }
            GUILayout.BeginHorizontal();
            for (int i=0;i<settings.myVIPGongFa.Count;i++)
            {
                if(GUILayout.Button(DateFile.instance.gongFaDate[settings.myVIPGongFa[i]][0]))
                {
                    removeId.Add(settings.myVIPGongFa[i]);
                }
            }
            if(GUILayout.Button("添加功法"))
            {
                showGongFa = !showGongFa;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (showGongFa)
            {
                int gangid = -1;
                string[] names = new string[11];
                for (int i= 0;i<11;i++)
                {
                    names[i] = DateFile.instance.actorAttrDate[604+i][0];
                }
                showGongFaType = GUILayout.SelectionGrid(showGongFaType, names,6);
                List<int> list = new List<int>(DateFile.instance.actorGongFas[DateFile.instance.mianActorId].Keys);
                for(int i=0;i<list.Count;i++)
                {
                    if(int.Parse(DateFile.instance.gongFaDate[list[i]][3])!=gangid)
                    {
                        if(i!=0)
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.BeginHorizontal();
                        gangid = int.Parse(DateFile.instance.gongFaDate[list[i]][3]);
                    }
                    if(int.Parse(DateFile.instance.gongFaDate[list[i]][6])==1&& DateFile.instance.GetGongFaLevel(DateFile.instance.mianActorId, list[i], 0) >=25 && int.Parse(DateFile.instance.gongFaDate[list[i]][61])-104==showGongFaType)
                    {
                        if(GUILayout.Button(DateFile.instance.gongFaDate[list[i]][0])&&!settings.myVIPGongFa.Contains(list[i]))
                        {
                            settings.myVIPGongFa.Add(list[i]);
                        }
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            foreach(int id in removeId)
            {
                settings.myVIPGongFa.Remove(id);
            }
            removeId.Clear();








            GUILayout.Label("战斗时固定出现的功法:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < settings.myConfirmedGongFa.Count; i++)
            {
                if (GUILayout.Button(DateFile.instance.gongFaDate[settings.myConfirmedGongFa[i]][0]))
                {
                    removeId.Add(settings.myConfirmedGongFa[i]);
                }
            }
            if (GUILayout.Button("添加功法"))
            {
                showGongFa2 = !showGongFa2;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (showGongFa2)
            {
                string[] names = new string[11];
                int gangid = -1;
                for (int i = 0; i < 6; i++)
                {
                    names[i] = DateFile.instance.actorAttrDate[604+i][0];
                }
                for (int i = 6; i < 11; i++)
                {
                    names[i] = DateFile.instance.actorAttrDate[604 + i][0];
                }
                showGongFaType2 = GUILayout.SelectionGrid(showGongFaType2, names,6);
                List<int> list = new List<int>(DateFile.instance.actorGongFas[DateFile.instance.mianActorId].Keys);
                for (int i = 0; i < list.Count; i++)
                {
                    if (int.Parse(DateFile.instance.gongFaDate[list[i]][3]) != gangid)
                    {
                        if (i != 0)
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.BeginHorizontal();
                        gangid = int.Parse(DateFile.instance.gongFaDate[list[i]][3]);
                    }
                    if (int.Parse(DateFile.instance.gongFaDate[list[i]][6]) == 1 && DateFile.instance.GetGongFaLevel(DateFile.instance.mianActorId, list[i], 0) >= 25 && int.Parse(DateFile.instance.gongFaDate[list[i]][61]) - 104 == showGongFaType2)
                    {
                        if (GUILayout.Button(DateFile.instance.gongFaDate[list[i]][0]) && !settings.myConfirmedGongFa.Contains(list[i]))
                        {
                            settings.myConfirmedGongFa.Add(list[i]);
                        }
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            foreach (int id in removeId)
            {
                settings.myConfirmedGongFa.Remove(id);
            }
            removeId.Clear();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public static List<int> GetWeaponAttackTypes(int weaponId)
        {
            string[] vs = DateFile.instance.GetItemDate(weaponId, 7, true).Split('|');
            List<int> costs = new List<int>();
            for (int i=0;i<vs.Length;i++)
            {               
                int attackType;
                if (int.TryParse(vs[i], out attackType))
                {
                    if(!costs.Contains(attackType))
                    {
                        costs.Add(attackType);
                    }
                }
            }
            costs.Sort();
            return costs;
        }

        public static void InitializeActorAttackGongfa()
        {
            int actorId = DateFile.instance.MianActorID();
            foreach(int id in DateFile.instance.actorGongFas[actorId].Keys)
            {
                try
                {
                    if (int.Parse(DateFile.instance.gongFaDate[id][6]) == 1 && DateFile.instance.GetGongFaLevel(actorId, id, 0) >= 25 && int.Parse(DateFile.instance.gongFaDate[id][40]) == 0)
                    {
                        List<int> costs = new List<int>(Main.gongfaCosts[id]);
                        Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();
                        for (int i = 0; i < costs.Count; i++)
                        {
                            if (keyValuePairs.ContainsKey(costs[i]))
                            {
                                keyValuePairs[costs[i]] += 1;
                            }
                            else
                            {
                                keyValuePairs[costs[i]] = 1;
                            }
                        }
                        bool flag302 = BattleSystem.instance.GetGongFaFEffect(302, true, actorId, 0);
                        bool flag241 = BattleSystem.instance.GetGongFaFEffect(241, true, actorId, 0);
                        bool flag308 = BattleSystem.instance.GetGongFaFEffect(308, true, actorId, 0);
                        List<int> removeCosts = new List<int>();
                        foreach (int num5 in costs)
                        {
                            if (flag302 && (num5 == 0 || num5 == 1 || num5 == 2) && keyValuePairs[num5] > 1)
                            {
                                keyValuePairs[num5] -= 1;
                                flag302 = false;
                                removeCosts.Add(num5);
                                break;
                            }
                            if (flag241 && (num5 == 3 || num5 == 4 || num5 == 5) && keyValuePairs[num5] > 1)
                            {
                                keyValuePairs[num5] -= 1;
                                flag241 = false;
                                removeCosts.Add(num5);
                                break;
                            }
                            if (flag308 && (num5 == 6 || num5 == 7 || num5 == 8) && keyValuePairs[num5] > 1)
                            {
                                keyValuePairs[num5] -= 1;
                                flag308 = false;
                                removeCosts.Add(num5);
                                break;
                            }
                        }
                        foreach (int cost in removeCosts)
                        {
                            costs.Remove(cost);
                        }
                        battleGongfaCosts.Add(id, costs);
                    }
                }
                catch(Exception e)
                {
                    Main.Logger.Log(e.Message + id.ToString());
                }                
            }
        }
        
        public static int Sort(int id1 ,int id2)
        {
            int level1 = int.Parse(DateFile.instance.gongFaDate[id1][2]);
            int level2 = int.Parse(DateFile.instance.gongFaDate[id2][2]);
            bool flag1 = settings.myVIPGongFa.Contains(id1);
            bool flag2 = settings.myVIPGongFa.Contains(id2);
            if (!flag1 && flag2)
                return 1;
            else if (flag1 && !flag2)
                return -1;
            else
            {
                if (level1 < level2)
                    return 1;
                else if (level1 > level2)
                    return -1;
                else
                    return 0;
            }
        }

        public static bool CostsEnough(int id,SortedDictionary<int,int> actorActionCost)
        {
            List<int> cost = Main.battleGongfaCosts[id];
            if (actorActionCost.Count == 0)
                return false;
            int index = actorActionCost.Keys.Last();
            for (int i = cost.Count - 1; i >= 0; i--)
            {
                int num;
                if (!actorActionCost.TryGetValue(index, out num))
                {
                    return false;
                }
                if (num != cost[i])
                {
                    return false;
                }
                index--;
            }
            return true;
        }

        public static void InitializeGongFaButton()
        {
            for(int i=0;i<9;i++)
            {
                int id = 30101;
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(BattleSystem.instance.battleGongFa[0], Vector3.zero, Quaternion.identity);
                gameObject.name = "BattleGongFa,"+id;
                gameObject.transform.SetParent(BattleSystem.instance.actorGongFaHolder[0], false);
                gameObject.transform.Find("GongFaName").GetComponent<Text>().text = DateFile.instance.gongFaDate[id][0];
                GameObject gameObject2 = gameObject.transform.Find("GongFaIcon").gameObject;
                SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(gameObject2.GetComponent<Image>(), "gongFaSprites", new int[]
                {
                int.Parse(DateFile.instance.gongFaDate[id][98])
                });
                //gameObject2.GetComponent<Image>().sprite = GetSprites.instance.gongFaSprites[DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][98])];
                gameObject2.name = $"GongFaIcon," + id;
                gameObjects2[i] = gameObject2;
                GameObject gameObject3 = gameObject.transform.Find("GongFaSizeBack").gameObject;
                gameObject3.SetActive(true);
                for (int j = 0; j < 6; j++)
                {
                    Transform transform = gameObject3.transform.Find(string.Format("Cost{0}Icon", j + 1));
                    GameObject gameObject7;
                    if (transform == null)
                    {
                        gameObject7 = UnityEngine.Object.Instantiate(gameObject3.transform.Find("Cost1Icon").gameObject, gameObject3.transform.Find("Cost1Icon").parent);
                        gameObject7.name = $"Cost{j + 1}Icon";
                    }
                    else
                        gameObject7 = transform.gameObject;
                    gameObject7.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[0][98])];
                    gameObject7.transform.Find("CostText").GetComponent<Text>().text = "";
                    gameObject7.SetActive( true );
                }
                gameObject3.GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedRowCount;
                gameObject3.GetComponent<GridLayoutGroup>().constraintCount = 1;
                gameObjects[i] = gameObject;
                gameObject.SetActive(false);
            }

            for(int i=0;i<settings.myConfirmedGongFa.Count;i++)
            {
                gameObjects[i].SetActive(true);
                SetActorGongFaButton(settings.myConfirmedGongFa[i], i);
            }
            BattleSystem.instance.otherGongFaRange.sizeDelta = new Vector2((float)Mathf.Max(125 + settings.myConfirmedGongFa.Count * 95, 220), 160f);

        }

        public static void SetActorGongFaButton(int id,int index)
        {
            GameObject gameObject= gameObjects[index];
            gameObject.name = "BattleGongFa," + id;
            gameObject.transform.Find("GongFaName").GetComponent<Text>().text = DateFile.instance.gongFaDate[id][0];
            GameObject gameObject2 = gameObjects2[index];
            gameObject2.SetActive(true);
            gameObject2.GetComponent<Image>().sprite = GetSprites.instance.gongFaSprites[DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][98])];
            gameObject2.name = "GongFaIcon," + id;
            GameObject gameObject3 = gameObject.transform.Find("GongFaSizeBack").gameObject;
            gameObject3.SetActive(true);
            int num = 0;
            int num2 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][40]);
            bool flag = num2 > 0;
            if (flag)
            {
                GameObject gameObject4 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject4.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[15];
                gameObject4.transform.Find("CostText").GetComponent<Text>().text = num2.ToString();
                gameObject4.SetActive(true);
                num++;

            }
            int num3 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][39]);
            bool flag2 = num3 > 0;
            if (flag2)
            {
                GameObject gameObject5 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject5.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[16];
                gameObject5.transform.Find("CostText").GetComponent<Text>().text = num3.ToString();
                gameObject5.SetActive(true);
                num++;
            }
            int num4 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][38]);
            bool flag3 = num4 > 0;
            if (flag3)
            {
                GameObject gameObject6 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject6.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[17];
                gameObject6.transform.Find("CostText").GetComponent<Text>().text = num4.ToString();
                gameObject6.SetActive(true);
                num++;
            }
            int count = 0;
            if(!flag)
                 count = Main.battleGongfaCosts[id].Count;
            for (int i = num; i < 6; i++)
            {
                Transform transform = gameObject3.transform.Find(string.Format("Cost{0}Icon", i + 1));
                GameObject gameObject7 = transform.gameObject;
                int num5 = 0;
                if (i - num < count)
                    num5 = Main.battleGongfaCosts[id][i - num];
                gameObject7.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[num5][98])];
                gameObject7.transform.Find("CostText").GetComponent<Text>().text = "";
                gameObject7.SetActive(i < num + count ? true : false);
            }
            if (num+ count > 4)
                gameObject3.GetComponent<GridLayoutGroup>().spacing = new Vector2(-13.0f, 0f);
            else
                gameObject3.GetComponent<GridLayoutGroup>().spacing = new Vector2(-10.0f, 0f);
        }
        
        public static void UpdateAttackGongFa(List<int> actorAttackGongFas)
        {
            actorAttackGongFas.Sort(Sort);
            int count = Mathf.Min(9, settings.myConfirmedGongFa.Count + actorAttackGongFas.Count);
            BattleSystem.instance.otherGongFaRange.sizeDelta = new Vector2((float)Mathf.Max(125 + count * 95, 220), 160f);
            for (int i = settings.myConfirmedGongFa.Count; i < 9; i++)
            {
                if (i < count)
                {
                    Main.gameObjects[i].SetActive(true);
                    Main.SetActorGongFaButton(actorAttackGongFas[i- settings.myConfirmedGongFa.Count], i);
                }
                else
                {
                    Main.gameObjects[i].SetActive(false);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Loading), "LoadBaseDate")]
    public class LoadBaseDate_Patch
    {
        public static void Postfix()
        {
            if (!Main.enabled)
                return;
            foreach(KeyValuePair<int, Dictionary<int,string>> gongfa  in DateFile.instance.gongFaDate)
            {
                int num;
                int.TryParse(gongfa.Value[6], out num);
                if (num != 1)
                    continue;
                List<int> costs = new List<int>();
                for (int i=0;i<3;i++)
                {
                    string[] cost = gongfa.Value[11+i].Split('|');
                    int costCount = 0;
                    if (cost.Length > 1)
                        int.TryParse(cost[1], out costCount);
                    if(costCount!=0)
                    {
                        int costType;                        
                        if(int.TryParse(cost[0], out costType))
                        {
                            for (int j = 0; j < costCount; j++)
                            {
                                costs.Add(costType);
                            }
                        }
                    }
                }
                while (costs.Count >= 1)
                {
                    if(Main.gongfaCosts.ContainsKey(gongfa.Key))
                    {
                        int index = gongfa.Key % costs.Count;
                        Main.gongfaCosts[gongfa.Key].Add(costs[index]);
                        costs.RemoveAt(index);
                    }
                    else
                    {
                        Main.gongfaCosts.Add(gongfa.Key, new List<int>());
                    }
                }
            }
            Main.Logger.Log("处理功法式顺序成功！");
        }
    }

    [HarmonyPatch(typeof(WindowManage), "GongFaMassage")]
    public class GongFaMassage_Patch
    {
        public static void Postfix(int gongFaId,ref string __result)
        {
            if (!Main.enabled)
                return;
            if (!Main.gongfaCosts.ContainsKey(gongFaId))
                return;
            string text = __result;
            string[] temp = text.Split(new string[] { "招式消耗：" }, StringSplitOptions.None);
            string[] temp2 = temp[1].Split(new char[] { '\n' },2);
            string s = "招式消耗：";
            for(int i=0;i<Main.gongfaCosts[gongFaId].Count;i++)
            {
                int cost = Main.gongfaCosts[gongFaId][i];
                s += DateFile.instance.SetColoer(DateFile.instance.ParseInt(DateFile.instance.attackTypDate[cost][99]), DateFile.instance.attackTypDate[cost][0], false);
                if (i!= Main.gongfaCosts[gongFaId].Count-1)
                    s += '※';
            }
            __result = temp[0] + s + '\n' + temp2[1];
        }
    }

    /*
    [HarmonyPatch(typeof(BattleSystem), "SetAttackGongFa")]
    public class SetAttackGongFa_Patch
    {
        public static bool Prefix(int id)
        {
            if (!Main.enabled)
                return true;
            if (!Main.gongfaCosts.ContainsKey(id))
                return true;
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(BattleSystem.instance.battleGongFa[0], Vector3.zero, Quaternion.identity);
            gameObject.name = "BattleGongFa," + id;
            gameObject.transform.SetParent(BattleSystem.instance.actorGongFaHolder[0], false);
            gameObject.transform.Find("GongFaName").GetComponent<Text>().text = DateFile.instance.gongFaDate[id][0];
            GameObject gameObject2 = gameObject.transform.Find("GongFaIcon").gameObject;
            gameObject2.GetComponent<Image>().sprite = GetSprites.instance.gongFaSprites[DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][98])];
            gameObject2.name = "GongFaIcon," + id;
            GameObject gameObject3 = gameObject.transform.Find("GongFaSizeBack").gameObject;
            gameObject3.SetActive(true);
            int num = 0;
            int num2 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][40]);
            bool flag = num2 > 0;
            if (flag)
            {
                GameObject gameObject4 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject4.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[15];
                gameObject4.transform.Find("CostText").GetComponent<Text>().text = num2.ToString();
                gameObject4.SetActive(true);
                num++;
            }
            int num3 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][39]);
            bool flag2 = num3 > 0;
            if (flag2)
            {
                GameObject gameObject5 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject5.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[16];
                gameObject5.transform.Find("CostText").GetComponent<Text>().text = num3.ToString();
                gameObject5.SetActive(true);
                num++;
            }
            int num4 = DateFile.instance.ParseInt(DateFile.instance.gongFaDate[id][38]);
            bool flag3 = num4 > 0;
            if (flag3)
            {
                GameObject gameObject6 = gameObject3.transform.Find(string.Format("Cost{0}Icon", num + 1)).gameObject;
                gameObject6.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[17];
                gameObject6.transform.Find("CostText").GetComponent<Text>().text = num4.ToString();
                gameObject6.SetActive(true);
                num++;
            }        
            int count = Mathf.Max(3, num + Main.battleGongfaCosts[id].Count);
            for (int i = num; i < count; i++)
            {
                Transform transform = gameObject3.transform.Find(string.Format("Cost{0}Icon", i + 1));
                GameObject gameObject7;
                if (transform == null)
                {
                    gameObject7 = UnityEngine.Object.Instantiate(gameObject3.transform.Find("Cost1Icon").gameObject, gameObject3.transform.Find("Cost1Icon").parent);
                    gameObject7.name = $"Cost{i + 1}Icon";
                }
                else
                    gameObject7 = transform.gameObject;
                int num5 = 0;
                if(i-num< Main.battleGongfaCosts[id].Count)
                    num5 = Main.battleGongfaCosts[id][i-num];
                gameObject7.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[num5][98])];
                gameObject7.transform.Find("CostText").GetComponent<Text>().text = "";
                gameObject7.SetActive(i<num+ Main.battleGongfaCosts[id].Count?true:false);
                Main.gameObjects.Add(gameObject7);
            }
            gameObject3.GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gameObject3.GetComponent<GridLayoutGroup>().constraintCount = 1;
            if(count>4)
            gameObject3.GetComponent<GridLayoutGroup>().spacing = new Vector2(-13.0f, 0f);

            return false;
        }
        
    }
    */
    [HarmonyPatch(typeof(BattleSystem), "CloseBattleEndWindow")]
    public class CloseBattleEndWindow_Patch
    {
        public static void Postfix()
        {
            for (int i = 0; i < 9; i++)
            {
                if (Main.gameObjects[i] != null)
                    GameObject.Destroy(Main.gameObjects[i]);
            }
            Main.battleGongfaCosts.Clear();
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "ActionEventAttack")]
    public class ActionEventAttack_Patch
    {
        public static void Prefix(bool isActor,ref int ___actorAttackTyp)
        {
            if (!Main.enabled)
                return;
            if (!isActor)
                return;
            if (Main.comboAttackIndex == -1)
                return;
            ___actorAttackTyp = Main.weaponAttackTypes[Main.comboAttackIndex];
            Main.comboAttackIndex = -1;

        }
    }

    [HarmonyPatch(typeof(BattleSystem), "GongFaCanUse")]
    public class GongFaCanUse_Patch
    {
        public static void Postfix(bool isActor, int _gongFaId,SortedDictionary<int,int> ___actorActionCost, ref bool __result)
        {
            if (!Main.enabled)
                return;
            if (!isActor || !Main.battleGongfaCosts.ContainsKey(_gongFaId)||!__result)
                return;
            __result = Main.CostsEnough(_gongFaId,___actorActionCost);
        }
    }

 
    [HarmonyPatch(typeof(BattleSystem), "CommonAttack")]
    public class CommonAttack_Patch
    {
        public static bool Prefix(bool isActor)
        {
            if (!Main.enabled)
                return true;
            if (!isActor)
                return true;
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(false);
            if (!st.GetFrames().Select(f => f.GetMethod().Name).Any(n => n.Contains("ActionEventEndAttack")))
            {
                return true;
            }
            int weaponId = BattleSystem.instance.GetWeaponId(isActor, -1, false);
            Main.weaponAttackTypes = Main.GetWeaponAttackTypes(weaponId);
            if (Main.weaponAttackTypes.Count == 1)
                return true;
            BattleSystem.instance.attackPartChooseMask.SetActive(true);
            Main.comboAttackWindow.SetActive(true);
            for (int i=0;i<3;i++)
            {
                GameObject gameObject=Main.comboAttackWindow.transform.GetChild(i).gameObject;
                gameObject.SetActive(i<Main.weaponAttackTypes.Count?true:false);
                if(i< Main.weaponAttackTypes.Count)
                gameObject.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[Main.weaponAttackTypes[i]][98])];
            }
            TweenSettingsExtensions.SetUpdate<Tweener>(TweenSettingsExtensions.SetEase<Tweener>(ShortcutExtensions.DOScale(Main.comboAttackWindow.GetComponent<RectTransform>(), new Vector3(1.8f, 1.8f, 1f), 0.1f),Ease.OutBack), true);
            TweenSettingsExtensions.SetUpdate<Tweener>(TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(Main.comboAttackWindow.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), 0.2f), 0.1f), Ease.OutBack), true);
            Main.combo = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "ShowBattleWindow")]
    public class ShowBattleWindow_Patch
    {
        public static void Prefix()
        {
            if (!Main.enabled)
                return;
            Main.InitializeActorAttackGongfa();
        }
        public static void Postfix()
        {
            if (!Main.enabled)
                return;
            if(Main.comboAttackWindow==null)
            {
                Main.comboAttackWindow =GameObject.Instantiate(BattleSystem.instance.attackPartChooseWindow);
                for(int i=0;i<Main.comboAttackWindow.transform.childCount;i++)
                {
                    GameObject.Destroy(Main.comboAttackWindow.transform.GetChild(i).gameObject);
                }
                Main.comboAttackWindow.transform.parent = BattleSystem.instance.attackPartChooseWindow.transform.parent;
                Main.comboAttackWindow.transform.localPosition = BattleSystem.instance.attackPartChooseWindow.transform.localPosition;
                Main.comboAttackWindow.AddComponent<GridLayoutGroup>();
                GridLayoutGroup gridLayoutGroup = Main.comboAttackWindow.GetComponent<GridLayoutGroup>();
                gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                for (int i=0;i<3;i++)
                {
                    GameObject gameObject = GameObject.Instantiate(BattleSystem.instance.attackPartChooseButton[i].gameObject, Main.comboAttackWindow.transform);
                    gameObject.name = $"comboAttackType{i+1}";
                    gameObject.tag = "SystemIcon";
                    for(int j=0;j< gameObject.transform.childCount;j++)
                    {
                        GameObject.Destroy(gameObject.transform.GetChild(j).gameObject);
                    }
                    gameObject.SetActive(false);
                }               
                Main.comboAttackWindow.SetActive(false);               
            }
            //添加九个icon
            for (int i = 0; i < BattleSystem.instance.actorGongFaHolder[0].childCount; i++)
            {
                Transform child = BattleSystem.instance.actorGongFaHolder[0].GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
            Main.InitializeGongFaButton();
        }
    }

    [HarmonyPatch(typeof(WindowManage), "WindowSwitch")]
    public class WindowSwitch_Patch
    {
        public static bool Prefix(bool on, GameObject tips, ref Text ___informationMassage, ref Text ___informationName, ref int ___tipsW, ref bool ___anTips)
        {
            if (tips == null)
            {
                return true;
            }
            if (!Main.enabled)
            {
                return true;
            }
            if (tips.name == "comboAttackType1"|| tips.name == "comboAttackType2"|| tips.name == "comboAttackType3")
            {
                ___informationName.text = "连击";
                ___informationMassage.text = "连击时获得选定式";
                ___tipsW = 230;
                ___anTips = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "SetChooseAttackPart")]
    public class SetChooseAttackPart_Pactch
    {
        public static bool Prefix(int typ,float ___baseTimeScale,ref int ___actorMoreTurn,ref bool ___actorReAttack,ref bool ___enemyReAttack,int ___changeAttackRange)
        {
            if (!Main.enabled)
                return true;
            if (!Main.combo)
                return true;
            Main.combo = false;
            Main.comboTime = 0;
            Time.timeScale = ___baseTimeScale;
            BattleSystem.instance.attackPartChooseMask.SetActive(false);
            Main.comboAttackWindow.SetActive(false);
            Traverse.Create(BattleSystem.instance).Method("TimeGo", new object[] { }).GetValue();
            if(typ!=3)
            {
                Traverse.Create(BattleSystem.instance).Method("CommonAttack", new object[] { true }).GetValue();
                Main.comboAttackIndex = typ;
            }                
            else
            {
                bool flag25 = ___actorMoreTurn == 0;
                ___actorMoreTurn = 0;
                ___actorReAttack = false;
                bool flag26 = BattleSystem.instance.otherEnemyIndex != -1;
                if (flag26)
                {
                    BattleSystem.instance.StartCoroutine(Traverse.Create(BattleSystem.instance).Method("OtherBattlerOut",new object[] { false, BattleSystem.instance.otherEnemyIndex, 0.3f, true }).GetValue<IEnumerator>());
                    return false;
                }
                bool flag27 = flag25 && !___actorReAttack && !___enemyReAttack && BattleSystem.instance.GetGongFaFEffect(30001, false, BattleSystem.instance.ActorId(false, false), 0);
                if (flag27)
                {
                    ___enemyReAttack = true;
                    Traverse.Create(BattleSystem.instance).Method("StartAttack",new object[] { false, 1000, true }).GetValue();
                    return false;
                }
                bool flag28 = flag25 && !___actorReAttack && !___enemyReAttack && BattleSystem.instance.GetGongFaFEffect(40001, false, BattleSystem.instance.ActorId(false, false), 0);
                if (flag28)
                {
                    ___enemyReAttack = true;
                    Traverse.Create(BattleSystem.instance).Method("StartAttack", new object[] { false, 500, true }).GetValue();
                    return false;
                }
                bool flag29 = ___enemyReAttack;
                if (flag29)
                {
                    return false;
                }
                float num4 = 0.1f;
                bool flag35 = ___changeAttackRange != 0;
                if (flag35)
                {
                    num4 += 0.35f;
                    Traverse.Create(BattleSystem.instance).Method("ChangeAttackRange",new object[] { BattleSystem.instance.battleRange, true, 0.2f, true, false }).GetValue();
                }
                BattleSystem.instance.StartCoroutine(Traverse.Create(BattleSystem.instance).Method("GoAttackTime",new object[] { num4 }).GetValue<IEnumerator>());
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleSystem),"Update")]
    public class Update_Patch
    {
        private static void Prefix()
        {
            if (!Main.enabled)
                return;
            if (!Main.combo)
                return;
            Main.comboTime += 1;
            if (Main.comboTime >= Main.settings.spaceTime)
            {
                if(Main.weaponAttackTypes.Count >= Main.settings.defaultKey)
                    BattleSystem.instance.SetChooseAttackPart(Main.settings.defaultKey-1);
                else if(Main.settings.defaultKey==4)
                    BattleSystem.instance.SetChooseAttackPart(3);
                else
                    BattleSystem.instance.SetChooseAttackPart(-1);
            }
            if(Input.GetKey(Main.button1))
            {
                BattleSystem.instance.SetChooseAttackPart(0);
            }
            if(Input.GetKey(Main.button2)&&Main.weaponAttackTypes.Count>=2)
            {
                BattleSystem.instance.SetChooseAttackPart(1);
            }
            if (Input.GetKey(Main.button3) && Main.weaponAttackTypes.Count >= 3)
            {
                BattleSystem.instance.SetChooseAttackPart(2);
            }
            if(Input.GetKeyDown((KeyCode)32))
            {
                BattleSystem.instance.SetChooseAttackPart(3);
            }
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "AddActionCostIcon")]
    public class AddActionCostIcon_Patch
    {
        public static void Postfix(bool isActor,SortedDictionary<int,int> ___actorActionCost)
        {
            if (!Main.enabled)
                return;
            if (!isActor)
                return;
            Main.battleUsingGongFa.Clear();
            List<int> actorAttackGongFas = new List<int>();
            foreach (int id in Main.battleGongfaCosts.Keys)
            {
                if (Main.CostsEnough(id,___actorActionCost))
                {
                    Main.battleUsingGongFa.Add(id);
                    if (!Main.settings.myConfirmedGongFa.Contains(id)&&DateFile.instance.GetGongFaLevel(DateFile.instance.mianActorId,id,0)>Main.settings.minGongFaCanUseLevel)
                        actorAttackGongFas.Add(id);
                }
            }
            Main.UpdateAttackGongFa(actorAttackGongFas);
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "RemoveActionCostIcon")]
    public class RemoveActionCostIcon_Patch
    {
        public static void Postfix(bool isActor, SortedDictionary<int, int> ___actorActionCost)
        {
            if (!Main.enabled)
                return;
            if (!isActor)
                return;
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(false);
            if (st.GetFrames().Select(f => f.GetMethod().Name).Any(n => n.Contains("AddActionCostIcon")))
            {
                return;
            }
            bool flag = st.GetFrames().Select(f => f.GetMethod().Name).Any(n => n.Contains("SetUseGongFa"));
            if(!flag)
            Main.battleUsingGongFa.Clear();
            List<int> actorAttackGongFas = new List<int>();
            foreach (int id in Main.battleGongfaCosts.Keys)
            {
                if (Main.CostsEnough(id, ___actorActionCost))
                {
                    if(!flag)
                    Main.battleUsingGongFa.Add(id);
                    if(!Main.settings.myConfirmedGongFa.Contains(id)&& DateFile.instance.GetGongFaLevel(DateFile.instance.mianActorId, id, 0) > Main.settings.minGongFaCanUseLevel)
                    actorAttackGongFas.Add(id);
                }
            }
            Main.UpdateAttackGongFa(actorAttackGongFas);
        }
    }

    [HarmonyPatch(typeof(DateFile), "ActorAddValue")]
    public class ActorAddValue_Patch
    {
        public static void Postfix(int id, int index,ref int __result)
        {
            if (!Main.enabled)
                return;
            if (id != DateFile.instance.mianActorId)
                return;
            if (!Main.settings.usingGongFaEffect)
                return;
            int num = 50000 + index;           
            if (DateFile.instance.gongFaDate[0].ContainsKey(num)&&Main.battleUsingGongFa.Count>0)
            {
                foreach(int gongFaId in Main.battleUsingGongFa)
                {
                    float value = DateFile.instance.ParseFloat(DateFile.instance.gongFaDate[gongFaId][num]);
                    bool flag8 = Math.Abs(value) > 0f;
                    if (flag8)
                    {
                        __result += BattleVaule.instance.SetGongFaValue(true, id, true, gongFaId, num, -1)/5;
                    }
                }             
            }
        }
    }
}
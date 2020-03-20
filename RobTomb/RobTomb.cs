using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony12;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using UnityEngine.Networking;
using AI;
using System.Diagnostics;
using GameData;
using LoadSystem;
using Random = UnityEngine.Random;
using static LoadSystem.BaseData;
using v2;
using System.IO;

namespace RobTomb
{

    public class Settings : UnityModManager.ModSettings
    {
        public bool daomu;
        public int paixu;
        public int search;
        public string amount = "0";
        public bool noPoisonItem;
        public bool autoCheckUpdate;
        public string ummPath = string.Empty;
        public bool debug;
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    


    public static class Main
    {
        public static bool enabled = false;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static int round = 0;                 //盗墓轮数
        public static int dieActorId = 0;          //墓主人id
        public static List<int> normalActors = new List<int>();          //当前格的人的集合
        public static int gongFaId = 0;                     //古冢遗刻功法id
        public static List<int> treasure = new List<int>();             //天材地宝itemid集合
        public static bool isinGang = false;                                    //在门派驻地唯一一格
        public static bool hasWaived = false;                       //放弃修习
        public static bool haveOtherWay = false;                //被发现时选择了除“束手就擒”的选项
        public static int enemyValueId = 0;                         //敌人
        public static int safeitemId = 0;                               //藏匿物品的id
        public static int basejilv = 0;                                   //盗墓未被发现的基础概率
        public static int nextjilv = 0;                                   //下轮被发现的概率（用于显示）
        public static bool baolu = false;                              //由于事情分支而被门派发现且仍未逃走
        public static bool hasKill = false;                            //死斗中选择杀死对面
        public static int button = 2;                                     //设置中盗墓手札的开关
        public static int debtTime = 0;                                //古冢遗刻的剩余时间
        public static List<int> bixieWeapon = new List<int>   //对僵尸宝具
        {40701,
        40702,
        40703,
        40704,
        40705,
        40706,
        40707,
        40708,
        40709,
        40801,
        40802,
        40803,
        40804,
        40805,
        40806,
        40807,
        40808,
        40809,
        60207, //九灵辟邪匣
        62209,//神鬼踏歌
        81605,//天香伏邪手
        81607,//降魔神木臂
        81704,//缚妖五指束
        63104,//降魔杵
        63109,//三界降服
        52609,//轩辕夏禹剑
        52706,//却邪
        82405,//斩魔雌雄剑
        82506,//辟邪神木剑
        52809,//斩龙铡
        82706,//辟邪神木刀
        82708,//太一伏魔刀
        53808,//镇狱碑
        63909,//神骇
        };
        public static Dictionary<int, int> getItem = new Dictionary<int, int>();                       //获得物品
        public static Dictionary<int, int> getItemCache = new Dictionary<int, int>();             //获得物品缓存
        public static int[] getRecourse = { 0, 0, 0, 0, 0, 0 };                                                        //获得资源
        public static int[] getRecourseCache = { 0, 0, 0, 0, 0, 0 };                                              //获得资源缓存
        public static int baseGongId = 0;        //当前地点归属，0：野外，1-15：各大门派，其余：村庄
        private static HarmonyInstance harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);
            Logger = modEntry.Logger;
            string resdir = System.IO.Path.Combine(modEntry.Path, "Data");
            Logger.Log(" resdir :" + resdir);
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            if (settings.autoCheckUpdate)
                AutoUpdate.Instance.CheckUpdate(modEntry);
            
            DataManager.Instance.Register(resdir, modEntry);
            harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            return true;
        }


        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            DataManager.Instance.OnToggle(value);
            enabled = value;
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.daomu = GUILayout.Toggle(settings.daomu, "开启盗墓玩法");
            GUILayout.Label("坟墓排序方式：");
            settings.paixu = GUILayout.Toolbar(settings.paixu, new string[] { "默认（按死亡顺序）", "按地位由高到低", "按地位由低到高" });
            GUILayout.Label("坟墓筛选方式：");
            settings.search = GUILayout.Toolbar(settings.search, new string[] { "无", "只显示被已挖过的", "只显示尚未挖过的", "只显示有粽子的" });
            GUILayout.BeginHorizontal();
            GUILayout.Label("最多同时显示的坟墓数量（0代表全部显示）：");
            settings.amount = GUILayout.TextField(settings.amount, 5, GUILayout.Width(200), GUILayout.ExpandWidth(false));

            if (settings.amount == "DMON") //开启debugmode
                settings.debug = true;
            if (settings.amount == "DMOFF")//关闭debugmode
                settings.debug = false;

            GUILayout.EndHorizontal();
            settings.noPoisonItem = GUILayout.Toggle(settings.noPoisonItem, "过滤带毒物品(非装备）");
            if (GUILayout.Button("盗墓手札", GUILayout.MaxWidth(200)))
                button = (button + 1) % 4;
            if (button == 1)
            {
                GUIStyle myStyle = new GUIStyle();
                myStyle.fontSize = 22;
                GUILayout.Label("<color=#E4504DFF>沙雕版\n一、选择目标篇\n1.孤家寡人，莫得朋友\n2.地处偏远，莫得看守\n3.门派驻地，勿要靠近\n二、成功率篇\n1.深思熟虑，三思而行\n2.聪颖冷静，多多益善\n3.见好就收，方能无恙\n三、收获篇\n1.细腻之人，自无遗漏\n2.天材地宝，福者得之\n四、遇敌篇\n1.三十六计，走为上计\n2.有舍有得，多多变通\n五、鬼怪篇\n1.善事利器，无所不催\n2.以己之长，攻彼之短</color>", myStyle);
            }
            else if (button == 3)
            {
                GUIStyle myStyle = new GUIStyle();
                myStyle.fontSize = 22;
                GUILayout.Label("<color=#E4504DFF>正常版\n设定集：\n1.基础成功率由你谋划所花时间与人物聪颖程度决定\n2.门派驻地内的墓受到保护，墓主人生前地位越高则保护越严密\n3.同格内墓主人的友人越多，越有可能盗墓失败\n4.坚毅能够提升进入疲惫状态前最大的盗墓次数，以及提供中毒和受伤的减免\n5.细腻越高越容易在墓中找到墓主人的物品和资源\n6.水性越高，越容易找到珍稀物品；福源越高，获得的珍稀物品的品级越高\n7.还有些隐藏设定就暂且不表了~</color>", myStyle);
            }
            AutoUpdate.Instance.OnGUI(modEntry, ref settings.autoCheckUpdate);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public static int SortList1(int a, int b) //a b表示列表中的元素

        {

            if (Math.Abs(DataManager.Instance.GetGameActorData(a, 20, false)) < Math.Abs(DataManager.Instance.GetGameActorData(b, 20, false)))

            {
                return -1;

            }

            else if (Math.Abs(DataManager.Instance.GetGameActorData(a, 20, false)) > Math.Abs(DataManager.Instance.GetGameActorData(b, 20, false)))

            {

                return 1;

            }

            return 0;
        }
        public static int SortList2(int a, int b) //a b表示列表中的元素

        {

            if (Math.Abs(DataManager.Instance.GetGameActorData(a, 20, false)) < Math.Abs(DataManager.Instance.GetGameActorData(b, 20, false)))

            {
                return 1;

            }

            else if (Math.Abs(DataManager.Instance.GetGameActorData(a, 20, false)) > Math.Abs(DataManager.Instance.GetGameActorData(b, 20, false)))

            {

                return -1;

            }

            return 0;
        }

        public static void Finish()
        {
            switch (MessageEventManager.Instance.EventValue[0])
            {
                case 0: //默认结局
                    {
                        int actorId = DateFile.instance.mianActorId;
                        int moodchange = int.Parse(DateFile.instance.goodnessDate[DateFile.instance.GetActorGoodness(actorId)][102]);
                        DateFile.instance.SetActorMood(actorId, moodchange, 100, true);
                        break;
                    }
                case 1:   //逃离
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        break;
                    }

                case 2:   //流言蜚语
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        DateFile.instance.SetActorFameList(actorId, 401, 1, 0);
                        break;
                    }

                case 3://被抓到
                    {
                        int actorId = DateFile.instance.mianActorId;
                        int level = enemyValueId % 10;
                        int id = 0;
                        int level2 = Mathf.Clamp(level - 1, 1, 9);
                        bool flag = false;
                        List<int> gangActors = new List<int>(DateFile.instance.GetGangActor(baseGongId, level2));
                    X:
                        if (gangActors.Count > 0)
                        {
                            id = gangActors[UnityEngine.Random.Range(0, gangActors.Count)];
                        }
                        else
                        {
                            if (level2 == 1 || flag)
                            {
                                level2 += 1;
                                flag = true;
                            }
                            else
                            {
                                level2 -= 1;
                            }
                            gangActors.AddRange(DateFile.instance.GetGangActor(baseGongId, level2));
                            if (level2 == 9) { return; }
                            goto X;
                        }
                        List<int> itemIds = new List<int>(getItem.Keys);
                        string text = "";
                        if (itemIds.Count > 0)
                        {
                            for (int i = 0; i < itemIds.Count; i++)
                            {
                                DateFile.instance.ChangeTwoActorItem(actorId, id, itemIds[i], getItem[itemIds[i]]);
                                //要不要加个经历显示呢，比如xxx在xx缴获了太吾盗掘的赃物ORZ  但可能有坏档风险  PeopleLifeAI.instance.AISetMassage()

                                text += DateFile.instance.GetActorName(actorId) + "失去了盗墓所得......";
                            }
                        }
                        Logger.Log("成功失去物品");
                        for (int i = 0; i < 6; i++)
                        {
                            UIDate.instance.ChangeTwoActorResource(actorId, id, i, Main.getRecourse[i], true);
                        }
                        Main.Logger.Log("成功失去资源");
                        DateFile.instance.SetActorMood(actorId, -10, 100, false);
                        DateFile.instance.SetActorFameList(actorId, 104, 1, 0);
                        MessageEventManager.Instance.SetBadSocial(dieActorId, actorId, 401);
                        if (text != "")
                            text += "\n";
                        text += "墓主人亲友与" + DateFile.instance.GetActorName(actorId) + "结下了仇怨......";
                        TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                        if (Main.baseGongId > 0 && Main.baseGongId <= 5)
                        {
                            List<int> list = new List<int>();
                            for (int i = 1; i <= 9; i++)
                            {
                                list.AddRange(DateFile.instance.GetGangActor(Main.baseGongId, i));
                            }
                            Main.Logger.Log("成功添加门派人物");
                            for (int i = 0; i < list.Count; i++)
                            {
                                int favor = DataManager.Instance.GetGameActorData(list[i], 3, false);
                                if (favor != -1)
                                    DateFile.instance.ChangeFavor(list[i], -2000, false, false);
                            }
                            Main.Logger.Log("成功改变好感");
                        }
                        else if (baseGongId <= 10)
                        {
                            DateFile.instance.SetGangValue(int.Parse(DateFile.instance.GetGangDate(baseGongId, 11)), int.Parse(DateFile.instance.GetGangDate(baseGongId, 3)), -10);
                            Main.Logger.Log("成功失去恩义");
                        }
                        else if (baseGongId <= 15)
                        {
                            List<int> list = new List<int>();
                            for (int i = 1; i <= 9; i++)
                            {
                                list.AddRange(DateFile.instance.GetGangActor(Main.baseGongId, i));
                            }
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (DataManager.Instance.GetGameActorData(list[i], 47, false) > 0)
                                {
                                    int favor = DataManager.Instance.GetGameActorData(list[i], 3, false);
                                    if (favor != -1)
                                        DateFile.instance.ChangeFavor(list[i], -12000, false, true);
                                }
                            }
                            Main.Logger.Log("成功下降支持");
                        }
                        else
                        {
                            DateFile.instance.SetGangValue(int.Parse(DateFile.instance.GetGangDate(baseGongId, 11)), int.Parse(DateFile.instance.GetGangDate(baseGongId, 3)), -10);
                            Main.Logger.Log("成功失去恩义");
                        }
                        int goodness = int.Parse(DateFile.instance.GetGangDate(baseGongId, 13));
                        if (goodness >= 875)
                        {
                            DateFile.instance.MakeRandInjury(actorId, (UnityEngine.Random.Range(0, 100) >= 75) ? 10 : 0, UnityEngine.Random.Range(100, 1000));
                        }
                        else if (goodness >= 625)
                        {
                            List<int> list = new List<int>(DateFile.instance.actorItemsDate[actorId].Keys);
                            if (list.Count > 0)
                            {
                                int itemId = list[UnityEngine.Random.Range(0, list.Count)];
                                DateFile.instance.ChangeTwoActorItem(actorId, id, itemId, 1, -1);
                            }
                        }
                        else if (goodness >= 375)
                        {
                            UIDate.instance.ChangeResource(actorId, 5, -(10 - level) * (10 - level) * (10 - level) * 100, true);
                        }
                        else if (goodness <= 125 && goodness >= 0)
                        {
                            UIDate.instance.ChangeTime(true, 10);
                        }
                        break;
                    }
                case 4:  //学功法技艺结果
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, +10, 100, false);
                        break;
                    }
                case 5: //寻获宝物结果
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, +5, 100, false);
                        break;
                    }
                case 6://一无所获
                    {
                        int actorId = DateFile.instance.mianActorId;
                        DateFile.instance.SetActorMood(actorId, -5, 100, false);
                        break;
                    }

            }
        }
        /// <summary>
        /// 走为上计
        /// </summary>
        public static void DoFlee()
        {
            Main.haveOtherWay = true;
            int actorId = DateFile.instance.mianActorId;
            int jilv = 0;
            int actorSpeed = BattleVaule.instance.GetMoveSpeed(true, actorId, false);
            if (actorSpeed <= 150)
            {
                jilv = actorSpeed / 5;
            }
            else if (actorSpeed <= 300)
            {
                jilv = 30 + (actorSpeed - 150) / 10;
            }
            else
            {
                jilv = 45 + (actorSpeed - 300) / 20;
            }
            if (MessageEventManager.Instance.EventValue[1] == 1)
            {
                int battleEnemyId = int.Parse(DateFile.instance.presetGangGroupDateValue[enemyValueId][301].Split(new char[] { '|' })[0]);
                jilv -= BattleVaule.instance.GetMoveSpeed(false, battleEnemyId, false, 0) / 10;
            }
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                EndToEvent(199801301);
            }
            else
            {
                if (MessageEventManager.Instance.EventValue[1] != 0)
                    EndToEvent(199802412);
                else
                    EndToEvent(199801302);
            }
        }

        /// <summary>
        /// 调整输入框数据类型
        /// </summary>
        public static void InputFix()
        {
            ui_MessageWindow.Instance.inputTextField.contentType = InputField.ContentType.IntegerNumber;
        }

        /// <summary>
        /// 贿赂
        /// </summary>
        public static void Bribe(int num)
        {
            ui_MessageWindow.Instance.inputTextField.contentType = InputField.ContentType.Name;
            if (num > DateFile.instance.ActorResource(DateFile.instance.mianActorId)[5])
            {
                Main.EndToEvent(1998014204);
                return;
            }
            else UIDate.instance.ChangeResource(DateFile.instance.mianActorId, 5, -num);
            Main.haveOtherWay = true;
            int id = MessageEventManager.Instance.MainEventData[1];
            int goodness = DateFile.instance.GetActorGoodness(id);
            int level = Math.Abs(DataManager.Instance.GetGameActorData(dieActorId, 20, false));
            int level2 = Math.Abs(DataManager.Instance.GetGameActorData(id, 20, false));
            int x = 50;
            switch ((10 - level2) / 2)
            {
                case 0:
                case 1:
                    x = 50;
                    break;
                case 2:
                case 3:
                    x = 100;
                    break;
                case 4:
                    x = 200;
                    break;
            }
            int jilv = Mathf.Clamp(num / x, 0, 100) - (10 - level) * 10;
            jilv += int.Parse(DateFile.instance.goodnessDate[goodness][24]);
            Main.Logger.Log(jilv.ToString());
            if (goodness == 2) jilv = 0;
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                Main.EndToEvent(199801421);
            }
            else
            {
                Main.EndToEvent(199801422);
            }
        }

        /// <summary>
        /// 忽悠
        /// </summary>
        public static void SweetTalk()
        {
            Main.haveOtherWay = true;
            int actorId = DateFile.instance.mianActorId;
            int fame = DateFile.instance.GetActorFame(actorId);
            int charm = DataManager.Instance.GetGameActorData(actorId, 15, true);
            int level = 10 - Mathf.Abs(DataManager.Instance.GetGameActorData(dieActorId, 20, false));
            int jilv = 0;
            if (DataManager.Instance.GetGameActorData(actorId, 14, false) == 2) jilv += 20;
            jilv += charm / 30;
            jilv += fame / 2;
            jilv -= level * 5;
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                Main.EndToEvent(199801431);
            }
            else
            {
                Main.EndToEvent(199801432);
            }
        }

        /// <summary>
        /// 束手就擒
        /// </summary>
        public static void NoResistance()
        {

            ui_MessageWindow.Instance.chooseItemEvents.Remove(GetNewID(BaseDataType.Event_Date,1998014402));
            if (MessageEventManager.Instance.EventValue[1] == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    getRecourse[i] = 0;
                }
            }
            if (MessageEventManager.Instance.EventValue[1] == 1)
            {
                if (getItem.Keys.Contains(safeitemId))
                {
                    getItem.Remove(safeitemId);
                    safeitemId = 0;
                }
            }
            if (!haveOtherWay && UnityEngine.Random.Range(0, 100) < 30 + DateFile.instance.GetActorResources(DateFile.instance.mianActorId)[5] / 5)
            {
                EndToEvent(1998014403);
            }
            else if (baseGongId > 0)
            {
                EndToEvent(1998014420);
            }
        }

        /// <summary>
        /// 被抓到之后的转跳
        /// </summary>
        public static void Punish()
        {
            if (baseGongId <= 15)
                EndToEvent(1998014403 + baseGongId);
            else
                EndToEvent(1998014424);
        }

        public static IEnumerator BackToMassageWindow(float waitTime, int giveItemId, int changeEvent, int otherValue1)
        {
            yield return new WaitForSeconds(waitTime);
            MessageEventManager.Instance.MainEventData = new int[]
            {
            MessageEventManager.Instance.MainEventData[0],
            MessageEventManager.Instance.MainEventData[1],
            GetNewID(BaseDataType.Event_Date,1998003),
            MessageEventManager.Instance.MainEventData[3],
            giveItemId,
            changeEvent,
            otherValue1
            };
            ui_MessageWindow.Instance.GetEventBooty(DateFile.instance.MianActorID(), ui_MessageWindow.Instance.massageItemTyp);
            ui_MessageWindow.Instance.ChangeMassageWindow(ui_MessageWindow.Instance.massageItemTyp);
            yield break;
        }

        /// <summary>
        /// 按钮转跳
        /// </summary>
        public static void RobTomb2()
        {
            int actorId = DateFile.instance.MianActorID();
            if (!ui_MessageWindow.Instance.chooseActorEvents.Contains(GetNewID(BaseDataType.Event_Date,1998001)))
                ui_MessageWindow.Instance.chooseActorEvents.Add(GetNewID(BaseDataType.Event_Date,1998001));
            DateFile.instance.SetEvent(new int[]
            { 0,
             -1,
            GetNewID(BaseDataType.Event_Date,19981),
             0
             }, true);
        }

        /// <summary>
        /// 古冢遗刻
        /// </summary>
        public static void LearnGongFa()
        {
            if (gongFaId != 0)
            {
                if (DateFile.instance.dayTime < 20)
                {
                    debtTime += 20 - DateFile.instance.dayTime;
                    UIDate.instance.ChangeTime(false, 20);
                }
                else
                {
                    UIDate.instance.ChangeTime(false, 20);
                }
                int actorId = DateFile.instance.MianActorID();
                int gongfazizhi = int.Parse(DateFile.instance.gongFaDate[gongFaId][61]) + 500;
                if (DateFile.instance.actorGongFas[actorId].ContainsKey(gongFaId))
                {
                    if (DateFile.instance.GetGongFaLevel(actorId, gongFaId, 0) >= 100 && DateFile.instance.GetGongFaFLevel(actorId, gongFaId, false) >= 10)
                    {
                        Characters.SetCharProperty(actorId, gongfazizhi, (int.Parse(Characters.GetCharProperty(actorId, gongfazizhi)) + 20).ToString());
                        TipsWindow.instance.SetTips(0, new string[] { "（太吾对应的资质上升了……）" }, 200);
                        DataManager.Instance.modData.SetActorData(dieActorId, 79, "0");
                        Main.EndToEvent(199801613);
                        return;
                    }
                }
                int rand = 0;
                int level = int.Parse(DateFile.instance.gongFaDate[gongFaId][2]);
                for (int i = 0; i < 10; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < DateFile.instance.GetActorValue(actorId, gongfazizhi, false) / 5 + DateFile.instance.GetActorResources(actorId)[1] / 2 + DateFile.instance.GetActorValue(actorId, 65, false) / 10 - 5 * level)
                    {
                        rand += 1;
                    }
                }
                int badlevel = 0;
                for (int i = 0; i < rand; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 100 - DateFile.instance.GetActorValue(actorId, gongfazizhi, false) / 5 - DateFile.instance.GetActorResources(actorId)[5] / 2 - DateFile.instance.GetActorValue(actorId, 65, false) / 10 + 5 * level)
                    {
                        badlevel += 1;
                    }
                }
                DateFile.instance.ChangeActorGongFa(actorId, gongFaId, 25, rand, badlevel, false);
                DateFile.instance.ChangeMianQi(actorId, 100 * int.Parse(DateFile.instance.gongFaDate[gongFaId][2]) * badlevel, 5);
                if (badlevel != 0) TipsWindow.instance.SetTips(0, new string[] { "你渐渐有了些异样的体悟......" }, 200);
                DataManager.Instance.modData.SetActorData(dieActorId, 79, "0");
                EndToEvent(199801611);
            }
        }

        /// <summary>
        /// 古冢遗刻技艺版
        /// </summary>
        public static void LearnJiYi()
        {
            if (gongFaId != 0)
            {
                if (DateFile.instance.dayTime < 20)
                {
                    debtTime += 20 - DateFile.instance.dayTime;
                    UIDate.instance.ChangeTime(false, 20);
                }
                else
                {
                    UIDate.instance.ChangeTime(false, 20);
                }
                int actorId = DateFile.instance.MianActorID();
                int jiyi = int.Parse(DateFile.instance.skillDate[gongFaId][3]);
                if (DateFile.instance.GetSkillLevel(gongFaId) >= 100 && DateFile.instance.GetSkillFLevel(gongFaId) >= 10)
                {
                    Characters.SetCharProperty(actorId, 501 + jiyi, (int.Parse(Characters.GetCharProperty(actorId, 501 + jiyi)) + 20).ToString());
                    TipsWindow.instance.SetTips(0, new string[] { "（太吾对应的资质上升了……）" }, 200);
                    DataManager.Instance.modData.SetActorData(dieActorId, 79, "0");
                    EndToEvent(199801713);
                    return;
                }
                int rand = 0;
                int level = int.Parse(DateFile.instance.skillDate[gongFaId][2]);
                for (int i = 0; i < 10; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < DateFile.instance.GetActorValue(actorId, 501 + jiyi, false) / 5 + DateFile.instance.GetActorResources(actorId)[1] / 2 + DateFile.instance.GetActorValue(actorId, 65, true) / 10 - 5 * level)
                    {
                        rand += 1;
                    }
                }
                DateFile.instance.ChangeMianSkill(gongFaId, 25, rand, false);
                DataManager.Instance.modData.SetActorData(dieActorId, 79, "0");
                EndToEvent(199801711);
            }
        }

        /// <summary>
        /// 事件结束转到xx事件
        /// </summary>
        /// <param name="eventId">txt配置文件中的原始ID</param>
        public static void EndToEvent(int eventId)
        {
            MessageEventManager.Instance.MainEventData[2] = GetNewID(BaseDataType.Event_Date,eventId);
            MessageEventManager.Instance.EventValue = new List<int>();
        }


        /// <summary>
        /// 设置天材地宝
        /// </summary>
        public static void SetTreasure()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 1; j <= 14; j++)
                {
                    if (!treasure.Contains(3000 + i * 100 + j))
                    {
                        treasure.Add(3000 + i * 100 + j);
                    }
                }
            }
            for (int i = 1; i <= 96; i++)
            {
                if (!treasure.Contains(4000 + i))
                {
                    treasure.Add(4000 + i);
                }
            }
            for (int i = 1; i <= 42; i++)
            {
                if (!treasure.Contains(4200 + i))
                {
                    treasure.Add(4200 + i);
                }
            }
            for (int i = 1; i <= 9; i++)
            {
                if (!treasure.Contains(4300 + i))
                {
                    treasure.Add(4300 + i);
                }
            }
        }


        public static void RobTombsuccessfully()
        {
            switch(MessageEventManager.Instance.EventValue[1])
            {
                case 1:             //跳过清点
                    {
                        if (getItemCache.Count > 0)
                        {
                            List<int> itemId = new List<int>(getItemCache.Keys);
                            for (int i = 0; i < itemId.Count; i++)
                            {
                                if (getItem.ContainsKey(itemId[i]))
                                    getItem[itemId[i]] += getItemCache[itemId[i]];
                                else
                                    getItem.Add(itemId[i], getItemCache[itemId[i]]);
                                getItemCache.Remove(itemId[i]);
                            }
                        }
                        EndToEvent(1998022);
                        return;
                    }
                case 2:
                    {
                        int actorID = DateFile.instance.mianActorId;
                        int itemID = MessageEventManager.Instance.MainEventData[3];
                        DateFile.instance.LoseItem(actorID, itemID, DateFile.instance.GetItemNumber(actorID, itemID), true);
                        getItem.Remove(itemID);
                        break;
                    }
                case 3:
                    {
                        int actorID = DateFile.instance.mianActorId;
                        int itemID = MessageEventManager.Instance.MainEventData[3];
                        DateFile.instance.ChangeTwoActorItem(actorID, Main.dieActorId, itemID, DateFile.instance.GetItemNumber(actorID, itemID));
                        getItem.Remove(itemID);
                        break;
                    }
            }
            if (getRecourseCache.Max() != 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    getRecourse[i] += getRecourseCache[i];
                    getRecourseCache[i] = 0;
                }
                EndToEvent(1998019);
                return;
            }

            if (getItemCache.Count > 0)
            {
                KeyValuePair<int, int> item = getItemCache.Last();
                getItemCache.Remove(item.Key);
                if (getItem.ContainsKey(item.Key))
                    getItem[item.Key] += item.Value;
                else
                    getItem.Add(item.Key, item.Value);
                MessageEventManager.Instance.MainEventData[3] = item.Key;
                EndToEvent(1998020);
                return;
            }
            EndToEvent(1998022);
        }

        /*
        public static void ShowGetItem()
        {
            float num = 0f; 
            foreach (KeyValuePair<int,int> item in getItem)
            {
                int[] array =new int[] {item.Key,item.Value};
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(BattleSystem.instance.battleBootyIcon, Vector3.zero, Quaternion.identity);
                gameObject.name = "Item," + array[0];
                gameObject.transform.SetParent(BattleSystem.instance.battleBootyHolder, false);
                GameObject gameObject2 = gameObject.transform.Find("ItemIcon").gameObject;
                gameObject2.name = "Item," + array[0];
                gameObject2.GetComponent<Image>().sprite = GetSprites.instance.itemSprites[int.Parse(DateFile.instance.GetItemDate(array[0], 98, true))];
                Image component = gameObject.transform.Find("ItemBack").GetComponent<Image>();
                component.sprite = GetSprites.instance.itemBackSprites[int.Parse(DateFile.instance.GetItemDate(array[0], 4, true))];
                component.color = ActorMenu.instance.LevelColor(int.Parse(DateFile.instance.GetItemDate(array[0], 8, true)));
                if (int.Parse(DateFile.instance.GetItemDate(array[0], 6, true)) > 0)
                {
                    gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = "×" + array[1];
                }
                else
                {
                    int num2 = int.Parse(DateFile.instance.GetItemDate(array[0], 901, true));
                    int num3 = int.Parse(DateFile.instance.GetItemDate(array[0], 902, true));
                    gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = string.Format("{0}{1}</color>/{2}", ActorMenu.instance.Color3(num2, num3), num2, num3);
                }
                gameObject.transform.localScale = new Vector3(0f, 0f, 1f);
                TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(gameObject.GetComponent<RectTransform>(), new Vector3(1.4f, 1.4f, 1f), 0.1f), num), Ease.OutBack);
                TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(gameObject.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), 0.4f), num + 0.1f), Ease.OutBack);
                num += 0.1f;
            }
        }
        */

        public static void Reset()
        {
            round = 0;
            dieActorId = 0;
            normalActors.Clear();
            gongFaId = 0;
            treasure.Clear();
            isinGang = false;
            hasWaived = false;
            haveOtherWay = false;
            baolu = false;
            enemyValueId = 0;
            safeitemId = 0;
            nextjilv = 0;
            basejilv = 0;
            getItem.Clear();
            getItemCache.Clear();
            hasKill = false;
            for (int i = 0; i < 6; i++)
            {
                getRecourse[i] = 0;
                getRecourseCache[i] = 0;
            }
            baseGongId = 0;
            SetTreasure();
        }

        /// <summary>
        /// 逃脱僵尸追击？？
        /// </summary>
        public static void ZombieFlee()
        {
            int actorId = DateFile.instance.mianActorId;
            int jilv = 33;
            int actorSpeed = BattleVaule.instance.GetMoveSpeed(true, actorId, false);
            int level = 10 - Math.Abs(DataManager.Instance.GetGameActorData(dieActorId, 20, false));
            if (actorSpeed <= 150)
            {
                jilv = actorSpeed / 5;
            }
            else if (actorSpeed <= 300)
            {
                jilv = 30 + (actorSpeed - 150) / 10;
            }
            else
            {
                jilv = 45 + (actorSpeed - 300) / 20;
            }
            int zoobieSpeed = 100 + 15 * level;
            Logger.Log("我方速度：" + actorSpeed.ToString() + "\n敌方速度：" + zoobieSpeed.ToString());
            jilv -= zoobieSpeed / 10;
            Logger.Log("逃脱几率：" + jilv.ToString() + "%");
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                string text = "";
                if (baseGongId != 0)
                {
                    int worldId = int.Parse(DateFile.instance.GetGangDate(baseGongId, 11));
                    int num2 = int.Parse(DateFile.instance.GetGangDate(baseGongId, 3));
                    int num4 = int.Parse(DateFile.instance.partWorldMapDate[num2][98]);
                    int num5 = num4 * num4;
                    int num6 = (14 - DateFile.instance.worldResource * 4) * 3;
                    for (int i = 0; i < num5; i++)
                    {
                        int num7 = i;
                        if (DateFile.instance.placeResource.ContainsKey(num2) && DateFile.instance.placeResource[num2].ContainsKey(num7))
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                UIDate.instance.ChangePlaceResource(false, -20, num2, num7, j, true);
                            }
                        }
                    }

                    text += "当地的资源和文化与安定都下降了......";
                }
                foreach (KeyValuePair<int, int> item in getItem)
                {
                    DateFile.instance.ChangeTwoActorItem(actorId, dieActorId, item.Key, item.Value);
                    if (text != "") text = text + "\n";
                    text = text + "失去了" + DateFile.instance.SetColoer(20001 + int.Parse(DateFile.instance.GetItemDate(item.Key, 8, false)), DateFile.instance.GetItemDate(item.Key, 0, false));
                }
                if (text != "") TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                EndToEvent(199802321);
            }
            else
            {
                EndToEvent(199802322);
            }
        }


        /// <summary>
        /// fight！！！
        /// </summary>
        public static void BattleAgainstZoobie()
        {
            MessageEventManager.Instance.MakeEventBattel(GetNewID(BaseDataType.EnemyTeam_Date,2000), 0);
            //StartBattle.instance.ShowStartBattleWindow();
        }

        /// <summary>
        /// 僵尸战胜利
        /// </summary>
        public static void WinZoobie()
        {
            DestroyTomb(dieActorId);
            TipsWindow.instance.SetTips(0, new string[] { "此地墓穴已被摧毁..." }, 200);
        }

        public static void DestroyTomb(int actorId)
        {
            List<int> list2 = new List<int>(DateFile.instance.actorItemsDate[actorId].Keys);
            for (int j = 0; j < list2.Count; j++)
            {
                int itemId = list2[j];
                DateFile.instance.LoseItem(actorId, itemId, DateFile.instance.GetItemNumber(actorId, itemId), true, true);
            }
            for (int k = 0; k < 7; k++)
            {
                Characters.RemoveCharProperty(actorId, 401 + k);
            }
            DateFile.instance.MoveOutPlace(actorId);
        }

        /// <summary>
        /// 偷袭未实装
        /// </summary>
        public static void SneakRaid()
        {
            int actorId = DateFile.instance.mianActorId;
            int[] actorResources = DateFile.instance.GetActorResources(actorId);
            int jilv = 33;
            int actorPower = DataManager.Instance.GetGameActorData(actorId, 993, false);
            int enemyPower = DataManager.Instance.GetGameActorData(MessageEventManager.Instance.MainEventData[1], 993, false);
            if (actorPower >= enemyPower) jilv -= 20;
            else jilv += 20;
            jilv += actorResources[5] / 2;
            if (haveOtherWay) jilv -= 20;
            Main.Logger.Log("我方战力：" + actorPower.ToString() + "敌方战力：" + enemyPower.ToString() + "\n偷袭成功率：" + jilv.ToString() + "%");
            if (UnityEngine.Random.Range(0, 100) < jilv)
            {
                EndToEvent(1998023123);
            }
            else
            {
                EndToEvent(1998023124);
            }
        }


        public static void Kill()
        {
            int actorId = DateFile.instance.mianActorId;
            int num = MessageEventManager.Instance.MainEventData[1];
            Main.hasKill = true;
            if (DataManager.Instance.GetGameActorData(num, 8, false) == 1)
            {
                Characters.SetCharProperty(num, 12, "0");
                PeopleLifeAI.instance.AISetMassage(95, num, DateFile.instance.mianPartId, DateFile.instance.mianPlaceId, null, -1, true);
                DateFile.instance.RemoveActor(new List<int>
                 {
                    num
                 }, true, false);
            }
            EndToEvent(19980263);
        }

        public static void LetHeLeave()
        {
            baolu = true;
            EndToEvent(19980264);
        }

        /// <summary>
        /// 盗墓事件
        /// </summary>
        public static void RobTomb()
        {
            if (ui_MessageWindow.Instance.chooseActorEvents.Contains(GetNewID(BaseDataType.Event_Date,1998001)))
            {
                ui_MessageWindow.Instance.chooseActorEvents.Remove(GetNewID(BaseDataType.Event_Date,1998001));
            }
            int partId = WorldMapSystem.instance.choosePartId;
            int placeId = WorldMapSystem.instance.choosePlaceId;
            int actorId = DateFile.instance.MianActorID();
            int gangId = int.Parse(Characters.GetCharProperty(dieActorId, 19));
            int level = 10 - Math.Abs(DataManager.Instance.GetGameActorData(dieActorId, 20, false));
            int[] actorResources = DateFile.instance.GetActorResources(actorId);
            int jilv;
            List<int> friends = new List<int>();
            int a = 0;
            for (int i = 0; i < 11; i++)
            {
                friends.AddRange(DateFile.instance.GetActorSocial(dieActorId, 301 + i, false));
            }
            List<int> baseFriends = new List<int>();
            if (friends.Count > 0)
            {
                for (int i = 0; i < friends.Count; i++)
                {
                    if (normalActors.Contains(friends[i]) && !baseFriends.Contains(friends[i]))
                    {
                        baseFriends.Add(friends[i]);
                    }
                }
                a = Mathf.Clamp(baseFriends.Count * 5, 0, 50);
            }
            switch (MessageEventManager.Instance.EventValue[1])
            {
                case 1:
                    {
                        basejilv = 50;
                        break;
                    }
                case 2:
                    {
                        basejilv = 70 + actorResources[1] / 2;
                        break;
                    }
                case 3:
                    {
                        basejilv = 90 + actorResources[1];
                        break;
                    }
                case 4:
                    {
                        basejilv = 110 + actorResources[1] * 2;
                        break;
                    }
                case 5:
                    {
                        basejilv = 150 + actorResources[1] * 3;
                        break;
                    }
                default:
                    break;
            }
            if (isinGang) a += level * 5;
            if (normalActors.Count <= 0)
            {
                jilv = 100;
                nextjilv = 100;
            }
            else
            {
                jilv = basejilv - Mathf.Clamp(round * (15 - actorResources[5] / 2), 0, 200) - a;
                nextjilv = basejilv - Mathf.Clamp((round + 1) * (15 - actorResources[5] / 2), 0, 200) - a;
            }
            if (jilv <= 0)
            {
                EndToEvent(1998030);
                return;
            }
            nextjilv = Mathf.Clamp(100 - nextjilv, 0, 100);
            int maxRound = 4 + actorResources[4] / 10;
            bool isTired = round > maxRound;
            if (UnityEngine.Random.Range(0, 100) < jilv && (round < maxRound || isTired) || hasWaived)
            {
                round += 1;
                if (hasWaived) round -= 1; //使在放弃修习时round不加

                string text = "";
                if (UnityEngine.Random.Range(0, 100) < 30 - actorResources[4] / 2 + (isTired ? 20 : 0))
                {
                    int typ = UnityEngine.Random.Range(0, 5);
                    DateFile.instance.ChangePoison(actorId, typ, (UnityEngine.Random.Range(0, 100) > 10 * level ? UnityEngine.Random.Range(50, level * 50) : UnityEngine.Random.Range(100, level * 100)) * (100 - actorResources[4]) / 100);
                    if (text != "") text += "\n";
                    text += "在墓中吸入不明的气体，NAME中毒了......";
                }

                if (UnityEngine.Random.Range(0, 100) < 30 - actorResources[4] / 2 + (isTired ? 20 : 0))
                {
                    DateFile.instance.MakeRandInjury(actorId, (UnityEngine.Random.Range(0, 100) >= 75) ? 10 : 0, (UnityEngine.Random.Range(0, 100) > 10 * level ? UnityEngine.Random.Range(50, level * 50) : UnityEngine.Random.Range(200, level * 200)) * (100 - actorResources[4]) / 100);
                    if (text != "") text += "\n";
                    text += "在墓中触发了未知的机关，NAME受伤了......";
                }

                text = text.Replace("NAME", DateFile.instance.GetActorName(actorId));
                if (text != "")
                    TipsWindow.instance.SetTips(0, new string[] { text }, 200);
                //判定是否被挖过秘籍
                bool hasgongfa = true;
                if (DataManager.Instance.modData.TryGetActorData(dieActorId, 79, out string num))
                {
                    if (int.Parse(num) != 1) hasgongfa = false;
                }
                else
                {
                    DataManager.Instance.modData.SetActorData(dieActorId, 79, "1");
                }
                //古冢遗刻
                if (UnityEngine.Random.Range(0, 100) < 70 + actorResources[0] && Main.dieActorId % 5 == 0 && hasgongfa && level >= 7 && !hasWaived)
                {
                    if (gangId >= 1 && gangId <= 15)
                    {
                        if (gangId == 4 && level == 9)
                        {
                            if (UnityEngine.Random.Range(0, 100) < 20 + actorResources[6])
                            {
                                gongFaId = GetNewID(BaseDataType.GongFa_Date,20409);
                                EndToEvent(1998016);
                                return;
                            }
                        }
                        List<int> gongFalist = new List<int>(DateFile.instance.gongFaDate.Keys);
                        List<int> dieActorsGongFa = new List<int>();
                        for (int i = 0; i < gongFalist.Count; i++)
                        {
                            if (int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][3]) == gangId && int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][2]) >= 7 && int.Parse(DateFile.instance.gongFaDate[gongFalist[i]][2]) <= level)
                            {
                                dieActorsGongFa.Add(gongFalist[i]);
                            }
                        }
                        if (dieActorsGongFa.Count > 0)
                        {
                            gongFaId = dieActorsGongFa[UnityEngine.Random.Range(0, dieActorsGongFa.Count)];
                            Logger.Log("古冢遗刻功法");
                            EndToEvent(1998016);
                        }
                        return;
                    }
                    else if (gangId != 16)
                    {
                        int zizhi = 0, jiyi = 0;
                        for (int i = 1; i < 17; i++)
                        {
                            int actorjiyi = DataManager.Instance.GetGameActorData(dieActorId, 500 + i, false);
                            if (actorjiyi >= zizhi)
                            {
                                zizhi = actorjiyi;
                                jiyi = i;
                            }
                        }
                        if (zizhi >= 90 && jiyi != 0)
                        {
                            gongFaId = 9 * (jiyi - 1) + Mathf.Clamp(zizhi / 15, 1, 9);
                            Logger.Log("古冢遗刻技艺:" + gongFaId);
                            EndToEvent(1998017);
                            return;
                        }

                    }
                }
                hasWaived = false;
                //挖到一个粽子
                if (UnityEngine.Random.Range(0, 100) < 5 || int.Parse(DataManager.Instance.modData.GetActorData(dieActorId, 79)) == 2)
                {
                    MessageEventManager.Instance.MainEventData[1] = GetNewID(BaseDataType.PresetActor_Date,2000);
                    Main.Logger.Log(MessageEventManager.Instance.MainEventData[1].ToString());
                    DataManager.Instance.modData.SetActorData(dieActorId, 79, "2");
                    EndToEvent(1998023);
                    return;
                }

                //福缘深厚
                if (UnityEngine.Random.Range(0, 100) < 10 + actorResources[2] / 2)
                {
                    int getItemLevel = 1;
                    List<int> getItemIdList = new List<int>();
                    for (int i = 0; i < 9 + actorResources[6] / 10; i++)
                    {
                        if (UnityEngine.Random.Range(0, 100) < 20 + actorResources[6] / 2)
                        {
                            getItemLevel++;
                        }
                    }
                    getItemLevel = Mathf.Clamp(getItemLevel, 1, 8);
                    for (int i = 0; i < treasure.Count; i++)
                    {
                        if (int.Parse(DateFile.instance.GetItemDate(treasure[i], 8, true)) == getItemLevel && !getItemIdList.Contains(treasure[i]))
                        {
                            getItemIdList.Add(treasure[i]);
                        }
                    }
                    int getItemId = getItemIdList[UnityEngine.Random.Range(0, getItemIdList.Count)];
                    DateFile.instance.GetItem(actorId, getItemId, 1, true, -1, 0);
                    if (getItem.ContainsKey(getItemId))
                        getItem[getItemId] += 1;
                    else
                        getItem.Add(getItemId, 1);
                    TipsWindow.instance.SetTips(5007, new string[]
                    {
                            DateFile.instance.GetActorName(actorId, false, false),
                            DateFile.instance.presetitemDate[getItemId][0],
                            ""
                    }, 100, -755f, -380f, 600, 100);
                    Logger.Log("福缘深厚");
                    MessageEventManager.Instance.MainEventData[3] = getItemId;
                    EndToEvent(1998018);
                    return;
                }



                //盗取道具资源
                for (int i = 0; i < 6; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 50 + actorResources[0])
                    {
                        int amount = PeopleLifeAI.instance.ResourceSize(Main.dieActorId, i, 30, 50);
                        UIDate.instance.ChangeTwoActorResource(Main.dieActorId, actorId, i, amount, true);
                        getRecourseCache[i] = amount;
                        PeopleLifeAI.instance.AISetMassage(9, actorId, partId, placeId, new int[]
                        {
                        0,
                        i
                         }, Main.dieActorId, true);
                    }
                }

                List<int> itemIds = new List<int>(DateFile.instance.actorItemsDate[Main.dieActorId].Keys);
                foreach (KeyValuePair<int, int> item in DateFile.instance.actorItemsDate[Main.dieActorId])
                {
                    bool flag = false; //已被上过毒
                    for (int i = 0; i < 6; i++)
                    {
                        if (int.Parse(DateFile.instance.GetItemDate(item.Key, 71 + i, true)) > 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (UnityEngine.Random.Range(0, 100) < 30 && int.Parse(DateFile.instance.GetItemDate(item.Key, 53)) == 1 && flag)
                    {
                        int rand = UnityEngine.Random.Range(1, 7);
                        for (int i = 0; i < rand; i++)
                            DateFile.instance.ChangItemDate(item.Key, 71 + UnityEngine.Random.Range(0, 6), UnityEngine.Random.Range(1, 11) * 100, true);
                    }
                }
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) < 50 + actorResources[0])
                    {
                        if (settings.noPoisonItem)
                        {
                            if (int.Parse(DateFile.instance.GetItemDate(itemIds[i], 4, true)) != 4)
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    if (int.Parse(DateFile.instance.GetItemDate(itemIds[i], 71 + j, true)) > 0)
                                        continue;
                                }
                            }
                        }
                        int itemID = itemIds[i];
                        int ItemNum = DateFile.instance.GetItemNumber(Main.dieActorId, itemID);
                        if (getItemCache.ContainsKey(itemID))
                            getItemCache[itemID] += ItemNum;
                        else
                            getItemCache.Add(itemID, ItemNum);
                        PeopleLifeAI.instance.AISetMassage(13, actorId, partId, placeId, new int[]
                        {
                        0,
                        int.Parse(DateFile.instance.GetItemDate(itemIds[i], 999, true))
                         }, Main.dieActorId, true);
                        DateFile.instance.ChangeTwoActorItem(Main.dieActorId, actorId, itemIds[i], DateFile.instance.actorItemsDate[Main.dieActorId][itemIds[i]], -1);
                    }
                }
                if (getRecourseCache.Max() != 0 || getItemCache.Count != 0)
                {
                    Main.Logger.Log("成功盗取道具资源");
                    MessageEventManager.Instance.EventValue = new List<int> { 0, 0 };
                    Main.RobTombsuccessfully();
                }
                else
                {
                    Main.Logger.Log("一无所获");
                    EndToEvent(1998021);
                }
            }
            else if (round == maxRound)
            {

                round += 1;
                Main.EndToEvent(1998012);
            }
            else
            {
                round++;
                enemyValueId = 0;
                if (baseGongId != 0 && baseGongId != 16)
                {
                    enemyValueId = DateFile.instance.GetGangValueId(baseGongId, Mathf.Clamp(10 - level + UnityEngine.Random.Range(-1, 2), 1, 9));
                }
                else
                {
                    int id = normalActors[UnityEngine.Random.Range(0, normalActors.Count)];
                    bool battle = false;
                    int goodness = DateFile.instance.GetActorGoodness(id);
                    int brave = DateFile.instance.GetActorResources(id)[3];
                    int power = DataManager.Instance.GetGameActorData(id, 993, false);
                    if (goodness == 2 || goodness == 4)
                    {
                        if (power + brave * 100 >= DataManager.Instance.GetGameActorData(DateFile.instance.mianActorId, 993, false) && DataManager.Instance.GetGameActorData(id, 19, false) != 16)
                        {
                            battle = true;
                        }
                    }
                    if (battle)
                        enemyValueId = id;
                }
                Logger.Log(enemyValueId.ToString());
                if (enemyValueId != 0)
                {
                    if (baseGongId != 0 && baseGongId != 16)
                    {
                        int battleEnemyId = int.Parse(DateFile.instance.presetGangGroupDateValue[enemyValueId][301].Split(new char[] { '|' })[0]);
                        if (baolu)
                        {
                            List<int> boss = new List<int>();
                            boss.AddRange(DateFile.instance.GetGangActor(baseGongId, 1));
                            boss.AddRange(DateFile.instance.GetGangActor(baseGongId, 2));
                            if (boss.Count > 0)
                            {
                                MessageEventManager.Instance.MainEventData[1] = boss[UnityEngine.Random.Range(0, boss.Count)];
                                Logger.Log("援军到场");
                                EndToEvent(19980265);
                                return;
                            }
                        }
                        Logger.Log(battleEnemyId.ToString());
                        MessageEventManager.Instance.MainEventData[1] = battleEnemyId;
                        Logger.Log("被人发现1");
                        EndToEvent(1998013);
                    }
                    else
                    {
                        MessageEventManager.Instance.MainEventData[1] = enemyValueId;
                        Logger.Log("被人发现2");
                        EndToEvent(1998024);
                    }
                }
                else
                {
                    Logger.Log("被人发现3");
                    EndToEvent(1998015);
                }
            }
        }
    }

    /// <summary>
    /// 替换按钮功能
    /// </summary>
    [HarmonyPatch(typeof(WorldMapSystem), "OpenToStory")]
    public class RobTomb_OpenToStory_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                return true;
            }
            else if (Main.settings.daomu)
            {

                int partId = DateFile.instance.mianPartId;
                int placeId = DateFile.instance.mianPlaceId;
                Main.Reset();
                Main.normalActors = DateFile.instance.HaveActor(partId, placeId, true, false, true, true);
                List<int> gangId = new List<int>(DateFile.instance.gangDate.Keys);
                for (int i = 0; i < gangId.Count; i++)
                {
                    if (DateFile.instance.GetGangDate(gangId[i], 0) == DateFile.instance.GetNewMapDate(partId, placeId, 98))
                    {
                        Main.baseGongId = gangId[i];
                    }
                    if (int.Parse(DateFile.instance.GetGangDate(gangId[i], 3)) == partId && int.Parse(DateFile.instance.GetGangDate(gangId[i], 4)) == placeId)
                    {
                        Main.isinGang = true;
                        Main.baseGongId = gangId[i];
                        break;
                    }
                }
                Main.Logger.Log(DateFile.instance.GetGangDate(Main.baseGongId, 0));
                Main.RobTomb2();

                return false;
            }
            else return true;
        }
    }

    /// <summary>
    ///设置按钮可用于否
    /// </summary>
    [HarmonyPatch(typeof(ChoosePlaceWindow), "UpdateToStoryButton")]
    public class RobTomb_ChoosePlaceWindow_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                DateFile.instance.massageDate[619][1] = "显示此地正在发生的奇遇事件…";
                DateFile.instance.massageDate[619][0] = "奇遇";
                return true;
            }
            else if (Main.settings.daomu)
            {
                DateFile.instance.massageDate[619][0] = "盗墓";
                DateFile.instance.massageDate[619][1] = "消耗时间挖掘此地坟墓获取道具或资源…";
                bool flag = WorldMapSystem.instance.choosePartId == DateFile.instance.mianPartId && WorldMapSystem.instance.choosePlaceId == DateFile.instance.mianPlaceId;
                List<int> list = DateFile.instance.HaveActor(WorldMapSystem.instance.choosePartId, WorldMapSystem.instance.choosePlaceId, false, true, false, false);
                ChoosePlaceWindow.Instance.openToStoryButton.interactable = list.Count > 0 && flag;
                return false;
            }
            else
            {
                DateFile.instance.massageDate[619][1] = "显示此地正在发生的奇遇事件…";
                DateFile.instance.massageDate[619][0] = "奇遇";
                return true;
            }
        }
    }

    /// <summary>
    /// 创建坟墓页面
    /// </summary>
    [HarmonyPatch(typeof(ui_MessageWindow), "GetActor")]
    public class RobTomb_GetAcotr_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled)
            {
                return true;
            }
            else if (Main.settings.daomu && ui_MessageWindow.Instance.massageItemTyp == GetNewID(BaseDataType.Event_Date,1998001))
            {
                for (int i = 0; i < ui_MessageWindow.Instance.actorHolder.childCount; i++)
                {
                    UnityEngine.Object.Destroy(ui_MessageWindow.Instance.actorHolder.GetChild(i).gameObject);
                }
                int num = DateFile.instance.MianActorID();
                int partId = DateFile.instance.mianPartId;
                int placeId = DateFile.instance.mianPlaceId;
                List<int> list = DateFile.instance.HaveActor(partId, placeId, false, true, false, true);
                List<int> dieActors = new List<int>();
                switch (Main.settings.search)
                {
                    case 0:
                        {
                            dieActors.AddRange(list);
                            break;
                        }
                    case 1:
                        {
                            foreach (int id in list)
                            {
                                if (DataManager.Instance.modData.TryGetActorData(id, 79, out string s))
                                {
                                    dieActors.Add(id);
                                }
                            }
                            break;
                        }
                    case 2:
                        {
                            foreach (int id in list)
                            {
                                if (!DataManager.Instance.modData.TryGetActorData(id, 79, out string s))
                                {
                                    dieActors.Add(id);
                                }
                            }
                            break;
                        }
                    case 3:
                        {
                            foreach (int id in list)
                            {
                                if (DataManager.Instance.modData.TryGetActorData(id, 79, out string s))
                                {
                                    if (int.Parse(s) == 2)
                                        dieActors.Add(id);
                                }
                            }
                            break;
                        }
                }
                switch (Main.settings.paixu)
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            dieActors.Sort(Main.SortList1);
                            break;
                        }
                    case 2:
                        {
                            dieActors.Sort(Main.SortList2);
                            break;
                        }
                }
                int number;
                bool flag = int.TryParse(Main.settings.amount, out number);
                if (flag && number == 0) flag = false;
                for (int num10 = 0; num10 < dieActors.Count && (num10 < number || !flag); num10++)
                {
                    int num11 = dieActors[num10];
                    int level = Math.Abs(DataManager.Instance.GetGameActorData(num11, 20, false));
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ui_MessageWindow.Instance.actorIcon, Vector3.zero, Quaternion.identity);
                    gameObject.name = "Actor," + num11;
                    gameObject.transform.SetParent(ui_MessageWindow.Instance.actorHolder, false);
                    gameObject.GetComponent<Toggle>().group = ui_MessageWindow.Instance.actorHolder.GetComponent<ToggleGroup>();
                    if (DateFile.instance.acotrTeamDate.Contains(num11))
                    {
                        gameObject.transform.Find("IsInTeamIcon").gameObject.SetActive(true);
                    }
                    gameObject.transform.Find("IsInBuildingIcon").gameObject.SetActive(DateFile.instance.ActorIsWorking(num11) != null);
                    int num12 = DateFile.instance.GetActorFavor(false, num, num11, false, false);
                    gameObject.transform.Find("ListActorFavorText").GetComponent<Text>().text = ((num11 != num && num12 != -1) ? DateFile.instance.Color5(num12, true, -1) : DateFile.instance.SetColoer(20002, DateFile.instance.massageDate[303][2], false));
                    gameObject.transform.Find("ListActorNameText").GetComponent<Text>().text = DateFile.instance.SetColoer(20011 - level, DateFile.instance.GetActorName(num11, false, false));
                    Transform transform = gameObject.transform.Find("ListActorFaceHolder").Find("FaceMask").Find("MianActorFace");
                    transform.GetComponent<ActorFace>().SetActorFace(num11, false);
                }
                dieActors.Clear();
                return false;
            }
            else return true;
        }
    }


    /// <summary>
    /// 选择盗墓人物
    /// </summary>
    [HarmonyPatch(typeof(ui_MessageWindow), "SetActor")]
    public class RobTomb_SetActor_Patch
    {
        public static bool Prefix(ref int ___chooseActorId)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                if (MessageEventManager.Instance.MainEventData[2] == GetNewID(BaseDataType.Event_Date,19981))
                {
                    for (int i = 0; i < ui_MessageWindow.Instance.actorHolder.childCount; i++)
                    {
                        UnityEngine.Object.Destroy(ui_MessageWindow.Instance.actorHolder.GetChild(i).gameObject);
                    }
                    Main.dieActorId = ___chooseActorId;
                    MessageEventManager.Instance.MainEventData[1] = ___chooseActorId;
                    ui_MessageWindow.Instance.CloseActorsWindow();
                    ui_MessageWindow.Instance.StartCoroutine(Main.BackToMassageWindow(0.2f, ___chooseActorId, 0, 0));
                    return false;
                }
                else return true;
            }
        }
    }

    /// <summary>
    ///  创建选择物品菜单
    /// </summary>
    [HarmonyPatch(typeof(ui_MessageWindow), "GetItem")]
    public class RobTomb_GetItem_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (ui_MessageWindow.Instance.massageItemTyp == GetNewID(BaseDataType.Event_Date,1998014402))
            {
                for (int i = 0; i < ui_MessageWindow.Instance.itemHolder.childCount; i++)
                {
                    UnityEngine.Object.Destroy(ui_MessageWindow.Instance.itemHolder.GetChild(i).gameObject);
                }
                List<int> itemId = new List<int>(Main.getItem.Keys);
                int actorID = DateFile.instance.mianActorId;
                for (int i = 0; i < itemId.Count; i++)
                {
                    int num8 = itemId[i];
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(UsefulPrefabs.instance.itemIconNoDrag, Vector3.zero, Quaternion.identity);
                    gameObject.name = "Item," + num8;
                    gameObject.transform.SetParent(ui_MessageWindow.Instance.itemHolder, false);
                    gameObject.GetComponent<Toggle>().group = ui_MessageWindow.Instance.itemHolder.GetComponent<ToggleGroup>();
                    Image component = gameObject.transform.Find("ItemBack").GetComponent<Image>();
                    SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(component, "itemBackSprites", new int[]
                    {
                int.Parse(DateFile.instance.GetItemDate(num8, 4, true))
                    });
                    component.color = DateFile.instance.LevelColor(int.Parse(DateFile.instance.GetItemDate(num8, 8, true)));
                    bool flag16 = int.Parse(DateFile.instance.GetItemDate(num8, 6, true)) > 0;
                    if (flag16)
                    {
                        gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = "×" + DateFile.instance.GetItemNumber(actorID, num8);
                    }
                    else
                    {
                        int num11 = int.Parse(DateFile.instance.GetItemDate(num8, 901, true));
                        int num12 = int.Parse(DateFile.instance.GetItemDate(num8, 902, true));
                        gameObject.transform.Find("ItemNumberText").GetComponent<Text>().text = string.Format("{0}{1}</color>/{2}", DateFile.instance.Color3(num11, num12), num11, num12);
                    }
                    GameObject gameObject2 = gameObject.transform.Find("ItemIcon").gameObject;
                    gameObject2.name = "ItemIcon," + num8;
                    SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(gameObject2.GetComponent<Image>(), "itemSprites", new int[]
                    {
                int.Parse(DateFile.instance.GetItemDate(num8, 98, true))
                    });
                }
                return false;
            }
            return true;
        }
    }

    /// <summary>
    ///选择藏匿的物品
    /// </summary>
    [HarmonyPatch(typeof(ui_MessageWindow), "SetItem")]
    public class RobTomb_SetItem_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                if (MessageEventManager.Instance.MainEventData[2] == GetNewID(BaseDataType.Event_Date,199801440))
                {
                    Main.safeitemId = ActorMenu.choseItemId;
                    ui_MessageWindow.Instance.CloseItemsWindow();
                    MessageEventManager.Instance.StartCoroutine(Main.BackToMassageWindow(0.2f, ActorMenu.choseItemId, 0, 0));
                    return false;
                }
                else return true;
            }
        }
    }

    /// <summary>
    /// 事件结束
    /// </summary>
    [HarmonyPatch(typeof(MessageEventManager), "EndEvent")]
    public class RobTomb_EndEvent_Patch
    {
        public static bool Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else
            {
                int actorId = DateFile.instance.mianActorId;
                if (MessageEventManager.Instance.EventValue.Count > 0 && MessageEventManager.Instance.EventValue[0] != 0)
                {
                    switch (MessageEventManager.Instance.EventValue[0])
                    {
                        case 199801:
                            {
                                Main.RobTomb();
                                return false;
                            }
                        case 199802:
                            {
                                Main.Finish();
                                return false;
                            }
                        case 199803:
                            {
                                Main.DoFlee();
                                return false;
                            }

                        case 199804:
                            {
                                Main.InputFix();
                                return false;
                            }

                        case 199806:
                            {
                                Main.Bribe(int.Parse(ui_MessageWindow.inputText));
                                return false;
                            }

                        case 199807:
                            {
                                Main.SweetTalk();
                                return false;
                            }

                        case 199808:
                            {
                                Main.NoResistance();
                                return false;
                            }
                        case 199805:
                            {
                                if (!ui_MessageWindow.Instance.chooseItemEvents.Contains(GetNewID(BaseDataType.Event_Date,1998014402)))
                                    ui_MessageWindow.Instance.chooseItemEvents.Add(GetNewID(BaseDataType.Event_Date,1998014402));
                                if (Main.hasKill)
                                    DateFile.instance.SetActorFameList(actorId, 108, 1);
                                return false;
                            }
                        case 199809:
                            {
                                Main.Punish();
                                return false;
                            }
                        case 199810:
                            {
                                Main.LearnGongFa();
                                return false;
                            }
                        case 199811:
                            {
                                Main.LearnJiYi();
                                return false;
                            }
                        case 199812:
                            {
                                Main.hasWaived = true;
                                Main.RobTomb();
                                return false;
                            }
                        case 199813:
                            {
                                Main.RobTombsuccessfully();
                                return false;
                            }
                        case 199814:
                            {
                                Main.ZombieFlee();
                                return false;
                            }
                        case 199815:
                            {
                                Main.BattleAgainstZoobie();
                                return false;
                            }
                        case 199816:
                            {
                                Main.SneakRaid();
                                return false;
                            }
                        case 199817:
                            {
                                Main.WinZoobie();
                                return false;
                            }
                        case 199818:
                            {
                                Main.Kill();
                                return false;
                            }
                        case 199819:
                            {
                                Main.LetHeLeave();
                                return false;
                            }
                        case 199820:
                            {
                                Main.baolu = true;
                                return false;
                            }
                    }
                    return true;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// 自定义替换文本
    /// </summary>
    [HarmonyPatch(typeof(ui_MessageWindow), "ChangeText")]
    public class RobTomb_ChangeText_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            if (BaseData.GetNewIDs(BaseDataType.Event_Date).Contains(MessageEventManager.Instance.MainEventData[2]))
            {
                try
                {
                    int gangId = DataManager.Instance.GetGameActorData(Main.dieActorId, 19, false);
                    int level = DataManager.Instance.GetGameActorData(Main.dieActorId, 20, false);
                    __result = __result.Replace("LEVEL", DateFile.instance.presetGangGroupDateValue[DateFile.instance.GetGangValueId(gangId, level)][1001]);
                    __result = __result.Replace("DN", DateFile.instance.GetActorName(Main.dieActorId, false, false));
                    __result = __result.Replace("PLACE", DateFile.instance.GetGangDate(Main.baseGongId, 0));
                    __result = __result.Replace("FAME", DateFile.instance.GetActorFameText(DateFile.instance.mianActorId));
                    __result = __result.Replace("XING", DateFile.instance.actorSurnameDate[DataManager.Instance.GetGameActorData(DateFile.instance.mianActorId, 29, false)][0]);
                    if (MessageEventManager.Instance.MainEventData[2] == GetNewID(BaseDataType.Event_Date,1998017) || MessageEventManager.Instance.MainEventData[2] == GetNewID(BaseDataType.Event_Date,199801711) || MessageEventManager.Instance.MainEventData[2] == GetNewID(BaseDataType.Event_Date,199801713))
                        __result = __result.Replace("JIYI", DateFile.instance.skillDate[Main.gongFaId][0]);
                    else
                        __result = __result.Replace("GONGFA", DateFile.instance.gongFaDate[Main.gongFaId][0]);
                    __result = __result.Replace("JILV", Main.nextjilv.ToString());
                }
                catch (Exception e)
                {
                    Main.Logger.Log(e.Message);
                    Main.Logger.Log("RobTomb_ChangeText_Patch");
                }
                return;
            }

        }
    }

    [HarmonyPatch(typeof(MessageEventManager), "GetEventIF")]
    public class RobTomb_GetEventIF_Patch
    {
        public static bool Prefix(ref bool __result, int eventId)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (eventId == GetNewID(BaseDataType.Event_Date,1998014402))
            {
                if (Main.getItem.Count > 0)
                {
                    __result = true;
                }
                else __result = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 生成zoobie
    /// </summary>
    [HarmonyPatch(typeof(DateFile), "MakeNewActor")]
    public class RobTomb_MakeNewActor_Patch
    {
        public static bool Prefix(int baseActorId, bool makeNewFeatures, int temporaryId, int age, int baseCharm, string[] attrValue, string[] skillValue, string[] gongFaValue, string[] resourceValue, int randObbs, ref int __result)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (baseActorId == GetNewID(BaseDataType.PresetActor_Date,2000))
            {
                int num = temporaryId;
                int level = 10 - Math.Abs(DataManager.Instance.GetGameActorData(Main.dieActorId, 20, false));
                int zoobielevel = Mathf.Clamp(level + UnityEngine.Random.Range(-1, 2), 1, 10);
                MethodInfo DoActorMake = typeof(DateFile).GetMethod("DoActorMake", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic);
                if (zoobielevel < 10)
                {
                    DoActorMake.Invoke(DateFile.instance, new object[] { baseActorId, num, makeNewFeatures, 0, 0, age, attrValue, skillValue, gongFaValue, resourceValue, baseCharm, null, null, randObbs, 0, 0 });
                }
                else //僵尸王尚未实装
                {
                    DoActorMake.Invoke(DateFile.instance, new object[] { baseActorId, num, makeNewFeatures, 0, 0, age, attrValue, skillValue, gongFaValue, resourceValue, baseCharm, null, null, randObbs, 0, 0 });
                }

                DateFile.instance.MakeActorName(num, DataManager.Instance.GetGameActorData(num, 29, false), DateFile.instance.GetActorDate(num, 5, false), true);
                Characters.SetCharProperty(num, 20, Mathf.Clamp(10 - zoobielevel, 1, 9).ToString());
                Characters.SetCharProperty(num, 8, "3");
                Characters.SetCharProperty(num, 706, (int.Parse(DateFile.instance.presetActorDate[baseActorId][706]) + 5000 * zoobielevel).ToString());
                Characters.SetCharProperty(num, 901, "0");// (zoobielevel * 3).ToString();
                Characters.SetCharProperty(num, 81, (int.Parse(DateFile.instance.presetActorDate[baseActorId][81]) + 1000 * zoobielevel).ToString());
                Characters.SetCharProperty(num, 82, (int.Parse(DateFile.instance.presetActorDate[baseActorId][82]) + 1000 * zoobielevel).ToString());
                Characters.SetCharProperty(num, 71, (int.Parse(DateFile.instance.presetActorDate[baseActorId][71]) + 50 * zoobielevel * Mathf.Clamp(zoobielevel - 5, 1, 7)).ToString());
                Characters.SetCharProperty(num, 72, (int.Parse(DateFile.instance.presetActorDate[baseActorId][72]) + 50 * zoobielevel * Mathf.Clamp(zoobielevel - 5, 1, 7)).ToString());
                Characters.SetCharProperty(num, 73, (int.Parse(DateFile.instance.presetActorDate[baseActorId][73]) + 50 * zoobielevel * Mathf.Clamp(zoobielevel - 5, 1, 7)).ToString());
                Characters.SetCharProperty(num, 32, (int.Parse(DateFile.instance.presetActorDate[baseActorId][32]) + zoobielevel * 500).ToString());
                Characters.SetCharProperty(num, 33, (int.Parse(DateFile.instance.presetActorDate[baseActorId][33]) + zoobielevel * 500).ToString());

                int num7 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1101]);
                int num8 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1102]);
                int num9 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1106]);
                int num10 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1107]);
                int num11 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1108]);
                int num12 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1109]);
                int num13 = int.Parse(DateFile.instance.presetActorDate[baseActorId][1111]);
                if (num7 > 100)
                {
                    Characters.SetCharProperty(num, 1101, (100 + (num7 - 100) * zoobielevel).ToString());
                }
                if (num8 > 100)
                {
                    Characters.SetCharProperty(num, 1102, (100 + (num8 - 100) * zoobielevel).ToString());
                }
                if (num9 > 100)
                {
                    Characters.SetCharProperty(num, 1106, (100 + (num9 - 100) * zoobielevel).ToString());
                }
                if (num10 > 100)
                {
                    Characters.SetCharProperty(num, 1107, (100 + (num10 - 100) * zoobielevel).ToString());
                }
                if (num11 > 100)
                {
                    Characters.SetCharProperty(num, 1108, (100 + (num11 - 100) * zoobielevel).ToString());
                }
                if (num12 > 100)
                {
                    Characters.SetCharProperty(num, 1109, (100 + (num12 - 100) * zoobielevel).ToString());
                }
                if (num13 > 100)
                {
                    Characters.SetCharProperty(num, 1111, (100 + (num13 - 100) * zoobielevel).ToString());
                }

                int num14 = int.Parse(DateFile.instance.presetActorDate[baseActorId][92]);
                int num15 = int.Parse(DateFile.instance.presetActorDate[baseActorId][93]);
                int num16 = int.Parse(DateFile.instance.presetActorDate[baseActorId][94]);
                int num17 = int.Parse(DateFile.instance.presetActorDate[baseActorId][95]);
                int num18 = int.Parse(DateFile.instance.presetActorDate[baseActorId][96]);
                int num19 = int.Parse(DateFile.instance.presetActorDate[baseActorId][97]);
                int num20 = int.Parse(DateFile.instance.presetActorDate[baseActorId][98]);
                if (num14 > 100)
                {
                    Characters.SetCharProperty(num, 92, (100 + (num14 - 100) * zoobielevel).ToString());
                }
                if (num15 > 100)
                {
                    Characters.SetCharProperty(num, 93, (100 + (num15 - 100) * zoobielevel).ToString());
                }
                if (num16 > 100)
                {
                    Characters.SetCharProperty(num, 94, (100 + (num16 - 100) * zoobielevel).ToString());
                }
                if (num17 > 100)
                {
                    Characters.SetCharProperty(num, 95, (100 + (num17 - 100) * zoobielevel).ToString());
                }
                if (num18 > 100)
                {
                    Characters.SetCharProperty(num, 96, (100 + (num18 - 100) * zoobielevel).ToString());
                }
                if (num19 > 100)
                {
                    Characters.SetCharProperty(num, 97, (100 + (num19 - 100) * zoobielevel).ToString());
                }
                if (num20 > 100)
                {
                    Characters.SetCharProperty(num, 98, (100 + (num20 - 100) * zoobielevel).ToString());
                }
                for (int i = 0; i < 6; i++)
                {
                    Characters.SetCharProperty(num, 61 + i, (int.Parse(DateFile.instance.presetActorDate[baseActorId][61 + i]) * zoobielevel).ToString());
                }
                DateFile.instance.MakeNewActorGongFa(num, true);
                int item = GetNewID(BaseDataType.Item_Date,20000);
                Characters.SetCharProperty(num, 201, (item + Mathf.Clamp(zoobielevel - 1, 1, 7)).ToString() + "&" + ((20 - zoobielevel) / 2).ToString());
                DateFile.instance.MakeNewActorItem(num);

                //fix weapon data
                int weaponId = int.Parse(Characters.GetCharProperty(num, 301));
                int head = int.Parse(Characters.GetCharProperty(num, 304));
                int body = int.Parse(Characters.GetCharProperty(num, 306));
                int foot = int.Parse(Characters.GetCharProperty(num, 307));
                for (int i = 0; i < 6; i++)
                {
                    Items.SetItemProperty(weaponId, 71 + i, (50 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                }
                Items.SetItemProperty(weaponId, 601, (300 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(head, 601, (400 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(body, 601, (600 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(foot, 601, (500 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());

                Items.SetItemProperty(weaponId, 603, (500 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(head, 603, (200 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(body, 603, (200 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(foot, 603, (200 + 150 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());

                Items.SetItemProperty(weaponId, 902, (20 + 10 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(head, 902, (20 + 10 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(body, 902, (20 + 15 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());
                Items.SetItemProperty(foot, 902, (10 + 15 * zoobielevel * UnityEngine.Random.Range(80, 121) / 100).ToString());

                Items.SetItemProperty(weaponId, 901, (int.Parse(Items.GetItemProperty(weaponId, 902)) * UnityEngine.Random.Range(60, 101) / 100).ToString());
                Items.SetItemProperty(head, 901, (int.Parse(Items.GetItemProperty(head, 902)) * UnityEngine.Random.Range(60, 101) / 100).ToString());
                Items.SetItemProperty(body, 901, (int.Parse(Items.GetItemProperty(body, 902)) * UnityEngine.Random.Range(60, 101) / 100).ToString());
                Items.SetItemProperty(foot, 901, (int.Parse(Items.GetItemProperty(foot, 902)) * UnityEngine.Random.Range(60, 101) / 100).ToString());

                Items.SetItemProperty(weaponId, 503, (400 + 10 * zoobielevel).ToString());

                Items.SetItemProperty(weaponId, 8, Mathf.Clamp(zoobielevel, 1, 9).ToString());
                Items.SetItemProperty(head, 8, Mathf.Clamp(zoobielevel, 1, 9).ToString());
                Items.SetItemProperty(body, 8, Mathf.Clamp(zoobielevel, 1, 9).ToString());
                Items.SetItemProperty(foot, 8, Mathf.Clamp(zoobielevel, 1, 9).ToString());
                __result = num;
                return false;
            }
            return true;
        }
    }


    /// <summary>
    /// 移动限制
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "UpdateBattleRange")]
    public class RobTomb_UpdateBattleRange_Patch
    {
        public static bool Prefix(ref int range, bool isActor)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return true;
            }
            else if (DateFile.instance.GetActorDate(BattleSystem.instance.ActorId(false, false), 997, false) == GetNewID(BaseDataType.PresetActor_Date,2000).ToString())
            {
                if (range > 60)
                {
                    range = 60;

                    StackTrace stackTrace = new StackTrace();
                    foreach (var s in stackTrace.GetFrames())
                    {
                        if (s.GetMethod().Name == "Initialize")
                        {
                            return true;
                        }
                    }
                    BattleSystem.instance.ShowBattleState(GetNewID(BaseDataType.GongFaOtherFPower_Date,20000), isActor);
                }
            }
            return true;
        }
    }

    /// <summary>
    /// 辟邪特效增加穿透
    /// </summary>
    [HarmonyPatch(typeof(BattleVaule), "GetAttackDef")]
    public class RobTomb_GetAttackDef_Patch
    {
        public static void Postfix(bool isActor, int defActorId, int weaponId, int gongFaId, ref int __result)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            if (isActor == true && DataManager.Instance.GetGameActorData(defActorId, 997, false) == GetNewID(BaseDataType.PresetActor_Date,2000))
            {
                if (!Main.bixieWeapon.Contains(int.Parse(DateFile.instance.GetItemDate(weaponId, 999, true))))
                    return;
                int level = int.Parse(DateFile.instance.GetItemDate(weaponId, 8, true));
                if (__result < 0)
                {
                    __result = __result - level * 5;
                }
                else if (__result - level * 10 < 0)
                {
                    __result = -(level * 10 - __result) / 2;
                }
                else
                    __result = __result - level * 10;
                BattleSystem.instance.ShowBattleState(GetNewID(BaseDataType.GongFaOtherFPower_Date,19999), isActor);
                return;
            }
        }
    }

    /// <summary>
    /// 修复距离限定到6时不能逃跑
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "UpdateDoOtherButton")]
    public class RobTomb_UpdateDoOtherButton_Patch
    {
        public static void Postfix(ref bool ___battleGo, ref int ___actorNeedUseGongFa, ref int ___actorDoOtherTyp, ref int ___actorDoingOtherTyp)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            else if (DataManager.Instance.GetGameActorData(BattleSystem.instance.ActorId(false, false), 997, false) == GetNewID(BaseDataType.PresetActor_Date,2000))
            {
                bool flag = ___battleGo && ___actorNeedUseGongFa == 0 && BattleSystem.instance.actorUseGongFaId == 0 && ___actorDoOtherTyp == 0 && ___actorDoingOtherTyp == 0;
                BattleSystem.instance.battlerRunButton.interactable = (flag && BattleSystem.instance.battleRange >= 60);
            }
        }
    }


    /// <summary>
    /// 修复敌人AI在战斗距离6时产生的BUG       其实还是有bug 但再想修就得去写整个AI了ORZ
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "SetNeedRange")]
    public class RobTomb_SetNeedRange_Patch
    {
        public static void Postfix(bool isActor, int value, ref int ___AI_MoveToDefRange, ref int ___AI_MoveToHealRange, ref int ___AI_MoveToUnAttackRange)
        {
            if (!Main.enabled || !Main.settings.daomu)
            {
                return;
            }
            else if (!isActor && DataManager.Instance.GetGameActorData(BattleSystem.instance.ActorId(false, false), 997, false) == GetNewID(BaseDataType.PresetActor_Date,2000))
            {
                if (___AI_MoveToDefRange != -1)
                {
                    ___AI_MoveToDefRange = Mathf.Min(___AI_MoveToDefRange, 60);
                    value = ___AI_MoveToDefRange;
                }
                else if (___AI_MoveToHealRange != -1)
                {
                    ___AI_MoveToHealRange = Mathf.Min(___AI_MoveToHealRange, 60);
                    value = ___AI_MoveToHealRange;
                }
                else if (___AI_MoveToUnAttackRange != -1)
                {
                    ___AI_MoveToUnAttackRange = Mathf.Min(___AI_MoveToUnAttackRange, 60);
                    value = ___AI_MoveToUnAttackRange;
                }
                BattleSystem.instance.enemyNeedRange = ((value != -1) ? value : BattleSystem.instance.battleRange);
                BattleSystem.instance.enemyNeedRangeSlider.value = (float)BattleSystem.instance.enemyNeedRange;
                BattleSystem.instance.enemyNeedRangeText.text = ((float)BattleSystem.instance.enemyNeedRange / 10f).ToString("f1");
            }
        }
    }
    /*
  
    [HarmonyPatch(typeof(BattleEndWindow), "SetupBattleEndEvent")]
    public class RobTomb_SetupBattleEndEvent_Patch
    {
        public static void Prefix()
        {

            StackTrace stackTrace = new StackTrace();
            foreach(var s in stackTrace.GetFrames())
            {
                Main.Logger.Log(s.GetMethod().Name);
            }
            if(!Main.enabled||!Main.settings.daomu)
            {
                return ;
            }
            else
            {
                string[] array = DateFile.instance.enemyTeamDate[StartBattle.instance.enemyTeamId][101 + BattleEndWindow.instance.battleEndTyp].Split(new char[]
                {
                 '&'
                });
                if (int.Parse(array[0]) == GetNewID(BaseDataType.Event_Date,1998025)) //恶战胜利逃走被暴露
                    Main.baolu = true;
                DateFile.instance.ResetBattleDate();
                AudioManager.instance.UpdatePlaceBGM(DateFile.instance.mianPartId, DateFile.instance.mianPlaceId);
                UIState.BattleSystem.Back();
                return;
            }         
            return ;
        }
    }
    */

    [HarmonyPatch(typeof(UIDate), "UpdateMaxDayTime")]
    public class RobTomb_UpdateMaxDayTime_Patch
    {
        public static void Prefix()
        {
            if (!Main.enabled || !Main.settings.daomu)
                return;
            if (DateFile.instance.dayTime < Main.debtTime)
            {
                Main.debtTime = Main.debtTime - DateFile.instance.dayTime;
                DateFile.instance.dayTime = 0;
            }
            else
            {
                DateFile.instance.dayTime = DateFile.instance.dayTime - Main.debtTime;
                Main.debtTime = 0;
            }

        }
    }
    
    [HarmonyPatch(typeof(BattleSystem), "AddBattleInjury")]
    public class RobTomb_AddBattleInjury_Patch
    {
        public static bool Prefix(ref List<int> __result,bool isActor, int actorId, int attackerId, int injuryId, int injuryPower, bool realDamage, bool sendEvent)
        {
            if (!Main.enabled)
                return true;          
            bool flag;
            StackTrace s = new StackTrace();
            flag = s.GetFrame(2).GetMethod().Name != "Prefix";
            if (flag)
            {
                List<int> list = new List<int>{injuryId,injuryPower,actorId};
                list = SubSystems.SpecialEffectSystem.ModifyData(DataUid.ActorDataId(186), list, attackerId);
                list = SubSystems.SpecialEffectSystem.ModifyData(DataUid.ActorDataId(168), list, actorId);
                injuryId = list[0];
                injuryPower = list[1];
                int effectID = GetNewID(BaseDataType.GongFaOtherFPower_Date, 20002);
                if (BattleSystem.instance.GetGongFaFEffect(effectID, isActor, actorId, 0))
                {
                    BattleSystem.instance.ShowBattleState(effectID, isActor);
                    int injurytyp = int.Parse(DateFile.instance.injuryDate[injuryId][1]) > 0 ? 0 : 1;
                    int hp = DateFile.instance.MaxHp(actorId);
                    int sp = DateFile.instance.MaxSp(actorId);
                    float ssp = (float)sp / (hp + sp);
                    int hppower = (int)(injuryPower * (1 - ssp));
                    int sppower = (int)(injuryPower * ssp);
                    BattleSystem.instance.AddBattleInjury(isActor, actorId,attackerId, injurytyp == 0 ? injuryId : (injuryId - 3), hppower,realDamage,sendEvent);
                    BattleSystem.instance.AddBattleInjury(isActor, actorId,attackerId, injurytyp == 0 ? (injuryId + 3) : injuryId, sppower,realDamage,sendEvent);
                    __result = list;
                    return false;
                }
                return true;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(BattleSystem), "UpdateBattlerMagicAndStrength")]
    public class RobTomb_UpdateBattlerMagicAndStrength_Patch
    {
        public static bool Prefix(bool isActor, int power, float ___actorMagic, float ___actorStrength, float ___enemyMagic, float ___enemyStrength)
        {
            if (!Main.enabled)
                return true;
            int actorId = BattleSystem.instance.ActorId(isActor, false);
            int effectID = GetNewID(BaseDataType.GongFaOtherFPower_Date, 20001);
            if (BattleSystem.instance.GetGongFaFEffect(effectID, isActor, actorId, 0))
            {
                if (isActor)
                {
                    bool flag = ___actorMagic < 30000f;
                    if (flag)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        BattleSystem.instance.UpdateMagic(isActor, value);
                    }
                    bool flag2 = ___actorStrength < 30000f;
                    if (flag2)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        BattleSystem.instance.UpdateStrength(isActor, value);
                    }
                }
                else
                {
                    bool flag = ___enemyMagic < 30000f;
                    if (flag)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        BattleSystem.instance.UpdateMagic(isActor, value);
                    }
                    bool flag2 = ___enemyStrength < 30000f;
                    if (flag2)
                    {
                        float value = (float)(BattleVaule.instance.GetMagicSpeed(isActor, actorId, true, 1) * power / 100) * Time.timeScale;
                        BattleSystem.instance.UpdateStrength(isActor, value);
                    }
                }
                return false;
            }
            else return true;
        }
    }
    
   
    [HarmonyPatch(typeof(BattleSystem), "ShowBaseGongFaState")]
    public class RobTomb_ShowBaseGongFaState_Patch
    {
        public static void Postfix(bool isActor, ref float __result, bool showState = true)
        {
            int actorId = BattleSystem.instance.ActorId(isActor, false);
            int effectID = GetNewID(BaseDataType.GongFaOtherFPower_Date, 20001);
            if (BattleSystem.instance.GetGongFaFEffect(effectID, isActor, actorId, 0))
            {
                if (showState)
                {
                    BattleSystem.instance.StartCoroutine(Traverse.Create(BattleSystem.instance).Method("WaitShowBattleState", new object[] { effectID, isActor, __result }).GetValue<IEnumerator>());
                }
                __result += 0.5f;
            }
        }
    }
    

    public class UIFix
    {
        [HarmonyPatch(typeof(WindowManage), "WindowSwitch")]
        public class WindowManage_WindowSwitch_Patch
        {
            public static void Prefix(GameObject tips)
            {
                if (!Main.enabled)
                    return;
                if (!Main.settings.debug)
                    return;
                if (tips == null)
                    return;
                Main.Logger.Log($"name:{tips.name}");
                Main.Logger.Log($"tag:{tips.tag}");
            }
        }
    }
}

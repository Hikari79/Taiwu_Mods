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

namespace WeaponCostFix
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
        public static GameObject[] weaponCostToggle=new GameObject[3];
        public static int preAttackType = 15;
        public static KeyCode button1 = KeyCode.Alpha1;
        public static KeyCode button2 = KeyCode.Alpha2;
        public static KeyCode button3 = KeyCode.Alpha3;
        public static int[] defAttack = new int[17] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

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
            AutoUpdate.AutoUpdate.OnGUI(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        
    }



 
    [HarmonyPatch(typeof(BattleSystem), "ShowBattleWindow")]
    public static class BattleSystem_ShowBattleWindow_Patch
    {
        public static void Postfix(int ___actorUseWeaponIndex,ref int[] ___actorWeaponCost)
        {
            if (!Main.enabled)
                return;
            for(int i=0;i<Main.defAttack.Length;i++)
            {
                Main.defAttack[i] = 0;
            }
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<ToggleGroup>();
            int weaponId = BattleSystem.instance.GetWeaponId(true, ___actorUseWeaponIndex, false);
            string[] costs = DateFile.instance.GetItemDate(weaponId, 7, false).Split('|');
            List<int> listOfCosts = new List<int>();
            for (int i = 0; i < costs.Length; i++)
            {
                int cost;
                if (int.TryParse(costs[i], out cost))
                {
                    if (!listOfCosts.Contains(cost))
                    {
                        listOfCosts.Add(cost);
                    }
                }
            }
            listOfCosts.Sort();
            for (int i = 0; i < 3; i++)
            {
                //复制toggle
                Main.weaponCostToggle[i] = UnityEngine.Object.Instantiate(BattleSystem.instance.actorWeapons[0].gameObject);
                Main.weaponCostToggle[i].name = $"weaponCostToggle{i + 1}";
                Main.weaponCostToggle[i].tag = "SystemIcon";
                //复制背景图+移位改名
                Image image = UnityEngine.Object.Instantiate(BattleSystem.instance.actorWeapons[0].transform.parent.Find("BattleButtonBack").gameObject.GetComponent<Image>(), BattleSystem.instance.actorWeapons[0].transform.parent);
                image.transform.localPosition = new Vector3(-200f + 50f * i, 0f, 0f);
                image.name = $"weaponCostToggleBack{i + 1}";
                Main.weaponCostToggle[i].transform.SetParent(image.transform, false);
                Main.weaponCostToggle[i].transform.localPosition = new Vector3(0f, 0f, 0f);
                //找到图标+改名+换图
                Transform transform = Main.weaponCostToggle[i].transform.Find("Weapon1Icon,298");
                transform.gameObject.name = $"weaponCostIcon{i+1}";
                transform.gameObject.tag = "SystemIcon";
                //transform.gameObject.GetComponent<Image>().sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[i < listOfCosts.Count ? listOfCosts[i] : 15][98])];
                SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(transform.gameObject.GetComponent<Image>(), "gongFaCostSprites", new int[]
                {
                    int.Parse(DateFile.instance.attackTypDate[i < listOfCosts.Count ? listOfCosts[i] : 15][98])
                });
                UnityEngine.Object.DestroyImmediate(Main.weaponCostToggle[i].GetComponent<Toggle>());
                Main.weaponCostToggle[i].AddComponent<Toggle>();
                Toggle toggle = Main.weaponCostToggle[i].GetComponent<Toggle>();
                toggle.name += $",{(i < listOfCosts.Count ? listOfCosts[i] : 15)}";
                toggle.interactable = true;
                Transform transform2 = toggle.transform.Find("Weapon1LabelIcon");
                Image image2 = transform2.gameObject.GetComponent<Image>();
                transform2.localPosition = new Vector3(0f, 0f, 0f);
                toggle.graphic = image2;
                toggle.group = gameObject.GetComponent<ToggleGroup>();
                toggle.isOn = false;
                toggle.onValueChanged.AddListener((bool value) => OnToggleClick(toggle, value));
            }
            for (int i = 0; i < 3; i++)
            {
                Main.weaponCostToggle[i].SetActive(i < listOfCosts.Count ? true : false);
                Main.weaponCostToggle[i].transform.parent.gameObject.SetActive(i < listOfCosts.Count ? true : false);
            }
            Main.weaponCostToggle[0].GetComponent<Toggle>().isOn = true;
            gameObject.GetComponent<ToggleGroup>().allowSwitchOff = false;
                       
        }
        public static void OnToggleClick(Toggle toggle, bool value)
        {
            if (!Main.enabled)
                return;
            if (value == false)
                return;
            string[] temp = toggle.name.Split(',');
            int cost = int.Parse(temp[1]);
            Traverse.Create(BattleSystem.instance).Field("actorWeaponCost").SetValue(new int[6] { cost,cost,cost,cost,cost,cost});
            for (int i = 0; i < 6; i++)
            {
                SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(BattleSystem.instance.actorWeaponCostIcon[i], "gongFaCostSprites", new int[]
                {
                    int.Parse(DateFile.instance.attackTypDate[cost][98])
                });
                //BattleSystem.instance.actorWeaponCostIcon[i].sprite = GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[cost][98])];
                TweenSettingsExtensions.SetEase<Tweener>(ShortcutExtensions.DOScale(BattleSystem.instance.actorWeaponCostIcon[i].GetComponent<RectTransform>(), new Vector3(2f, 2f, 1f), 0.1f), Ease.OutBack);
                TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetDelay<Tweener>(ShortcutExtensions.DOScale(BattleSystem.instance.actorWeaponCostIcon[i].GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), 0.3f), 0.1f), Ease.OutBack);
            }
        }



    }

    [HarmonyPatch(typeof(WindowManage), "WindowSwitch")]
    public static class WindowManage_WindowSwitch_Patch
    {
        public static void Postfix(bool on, GameObject tips, ref Text ___informationMassage, ref Text ___informationName, ref int ___tipsW, ref bool ___anTips)
        {
            if (tips == null) return;
            if (!Main.enabled) return;
            List<string> names = new List<string>() { "weaponCostIcon1", "weaponCostIcon2", "weaponCostIcon3" };
            if (names.Contains(tips.name))
            {
                int num;
                int.TryParse(tips.transform.parent.gameObject.name.Split(',')[1], out num);
                ___informationName.text = "切换式";
                ___informationMassage.text = $"选择下次攻击时要使用的式\n（连续使用相同式进行攻击时，对方对应回避会相应提高）\n\n当前敌方对此式的回避提高：{DateFile.instance.SetColoer(20006, (Main.defAttack[num] * 5).ToString() + "%", false)}";
                ___tipsW = 600;
                ___anTips = true;
            }
        }
    }


    [HarmonyPatch(typeof(WindowManage), "ShowItemMassage")]
    public static class WindowManage_ShowItemMassage_Patch
    {
        private static void Postfix(WindowManage __instance, int itemId, ref string ___baseWeaponMassage, ref Text ___informationMassage)
        {
            if (!Main.enabled)
                return;
            string text = ___baseWeaponMassage;
            bool flag2 = int.Parse(DateFile.instance.GetItemDate(itemId, 1, true)) == 1;
            if (flag2)
            {
                text += DateFile.instance.SetColoer(10002, "\n【式效率】\n", false);
                string[] costs = DateFile.instance.GetItemDate(itemId, 7, false).Split('|');
                Dictionary<int, int> kvs = new Dictionary<int, int>();
                foreach(string cost in costs)
                {
                    int result;
                    if(int.TryParse(cost,out result))
                    {
                        if (!kvs.ContainsKey(result))
                        {
                            kvs.Add(result, 1);
                        }
                        else
                        {
                            kvs[result] += 1;
                        }
                    }                  
                }
                text += "·";
                List<int> keys = new List<int>(kvs.Keys);
                for(int i=0;i<keys.Count;i++)
                {
                    text += string.Format("{0}{1}", DateFile.instance.attackTypDate[keys[i]][0]+':',DateFile.instance.SetColoer(20006, (60+kvs[keys[i]]*15).ToString() + "%", false));
                    if (i != keys.Count - 1)
                        text += "※";
                }
                text += "\n";
                ___baseWeaponMassage = text;
                ___informationMassage.text = text;
            }
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "ChangeWeapon")]
    public static class ChangeWeapon_Patch
    {
        public  static void Postfix(bool isActor, int index,ref int[] ___actorWeaponCost)
        {
            if (!Main.enabled || Main.weaponCostToggle == null)
                return;
            if(isActor)
            {
                if(Main.weaponCostToggle[0]==null)
                {
                    return;
                }
                int weaponId = BattleSystem.instance.GetWeaponId(isActor, index, false);
                string[] costs = DateFile.instance.GetItemDate(weaponId, 7, false).Split('|');
                List<int> listOfCosts = new List<int>();
                for (int i = 0; i < costs.Length; i++)
                {
                    int cost;
                    if (int.TryParse(costs[i], out cost))
                    {
                        if (!listOfCosts.Contains(cost))
                        {
                            listOfCosts.Add(cost);
                        }
                    }
                }
                listOfCosts.Sort();
                for(int i=0;i<3;i++)
                {
                    Main.weaponCostToggle[i].SetActive(i < listOfCosts.Count ? true : false);
                    Main.weaponCostToggle[i].transform.parent.gameObject.SetActive(i < listOfCosts.Count ? true : false);
                    if(i<listOfCosts.Count)
                    {
                        GameObject gameObject = Main.weaponCostToggle[i];
                        SingletonObject.getInstance<DynamicSetSprite>().SetImageSprite(gameObject.transform.Find($"weaponCostIcon{i + 1}").gameObject.GetComponent<Image>(), "gongFaCostSprites", new int[] { int.Parse(DateFile.instance.attackTypDate[listOfCosts[i]][98]) });
                        //gameObject.transform.Find($"weaponCostIcon{i+1}").gameObject.GetComponent<Image>().sprite= GetSprites.instance.gongFaCostSprites[DateFile.instance.ParseInt(DateFile.instance.attackTypDate[listOfCosts[i]][98])];
                        gameObject.GetComponent<Toggle>().name = $"weaponCostToggle{i + 1},{listOfCosts[i]}";                        
                    }
                }
                BattleSystem_ShowBattleWindow_Patch.OnToggleClick(Main.weaponCostToggle[0].GetComponent<Toggle>(), true);
            }
        }
    }

    [HarmonyPatch(typeof(BattleVaule), "GetWeaponHit")]
    public class GetWeaponHit_Patch
    {
        public static void Postfix(bool isActor, int actorId, bool inBattle, int weaponId, int typ,ref int __result)
        {
            if (!Main.enabled || !isActor)
                return;
            string[] costs = DateFile.instance.GetItemDate(weaponId, 7, false).Split('|');
            Dictionary<int, int> kvs = new Dictionary<int, int>();
            foreach (string cost in costs)
            {
                int result;
                if (int.TryParse(cost, out result))
                {
                    if (!kvs.ContainsKey(result))
                    {
                        kvs.Add(result, 1);
                    }
                    else
                    {
                        kvs[result] += 1;
                    }
                }
            }
            bool flag = false;
            int x=0;
            foreach(int cost in kvs.Keys)
            {
                if (int.Parse(DateFile.instance.attackTypDate[cost][1]) == typ&&kvs[cost]>x)
                {
                    flag = true;
                    x = kvs[cost];
                }
            }
            if(flag)
            {
                __result = __result * (60 + x * 15) / 100;
            }
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "ActionEventAttack")]
    public class ActionEventAttack_Patch
    {
        public static void Prefix(bool isActor,int ___actorAttackTyp)
        {
            if (!Main.enabled)
                return;
            if(isActor)
            {
                if(___actorAttackTyp==Main.preAttackType)
                {
                    Main.defAttack[___actorAttackTyp] += 3;
                }
                for (int i = 0; i < Main.defAttack.Length; i++)
                {
                    Main.defAttack[i] = Mathf.Clamp(Main.defAttack[i] - 1, 0, 10);
                }
                Main.preAttackType = ___actorAttackTyp;
            }
        }
    }

    [HarmonyPatch(typeof(BattleVaule), "GetDeferDefuse")]
    public class GetDeferDefuse_Patch
    {
        public static void Postfix(bool isActor, int actorId, bool inBattle, int typ, int addValue,ref int __result)
        {
            if (!Main.enabled)
                return;
            if (isActor || !inBattle)
                return;
            bool ___attackPause = Traverse.Create(BattleSystem.instance).Field("attackPause").GetValue<bool>();
            if (!___attackPause)
                return;          
            int ___actorAttackTyp = Traverse.Create(BattleSystem.instance).Field("actorAttackTyp").GetValue<int>();
            if(typ==int.Parse(DateFile.instance.attackTypDate[___actorAttackTyp][1])+2)
            {
                __result = __result * (100 + Main.defAttack[___actorAttackTyp] * 5) / 100;
            }
        }
    }

    [HarmonyPatch(typeof(BattleSystem), "Update")]
    public class Update_Patch
    {
        private static void Prefix()
        {
            if (!Main.enabled)
                return;
            bool flag = !BattleSystem.instance.battleWindow.activeInHierarchy || ActorMenu.instance.actorMenu.activeInHierarchy;
            if (flag)
                return;
            if (Traverse.Create(BattleSystem.instance).Field("battleEnd").GetValue<bool>())
                return;
            if (Input.GetKey(Main.button1))
            {
                Main.weaponCostToggle[0].GetComponent<Toggle>().isOn = true;
            }
            if (Input.GetKey(Main.button2)&&Main.weaponCostToggle[1].activeInHierarchy)
            {
                Main.weaponCostToggle[1].GetComponent<Toggle>().isOn = true;
            }
            if (Input.GetKey(Main.button3) && Main.weaponCostToggle[2].activeInHierarchy)
            {
                Main.weaponCostToggle[2].GetComponent<Toggle>().isOn = true;
            }
        }
    }
}
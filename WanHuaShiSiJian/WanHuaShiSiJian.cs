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

namespace WanHuaShiSiJian
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

            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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

    [HarmonyPatch(typeof(BattleSystem), "ActionEventUseGongFaEnd")]
    public class WanHuaShiSiJian_BattleSystem_ActionEventUseGongFaEnd_Patch
    {
        public static void Postfix(bool isActor)
        {
            if (!Main.enabled)
                return;
            int num = BattleSystem.instance.ActorId(isActor, false);
            bool flag4 = isActor && BattleSystem.instance.battleTyp == 0 && !DateFile.instance.actorGongFas[num].ContainsKey(BattleSystem.instance.actorNowUseingGongFa);
            if (flag4)
            {
                int gongFaLevel = DateFile.instance.GetGongFaLevel(num, BattleSystem.instance.actorNowUseingGongFa, 0);
                bool flag5 = gongFaLevel < 100;
                if (flag5)
                {
                    bool flag6 = UnityEngine.Random.Range(0, 100) < (100 - int.Parse(DateFile.instance.gongFaDate[BattleSystem.instance.actorNowUseingGongFa][2]) * 5) * (150 - gongFaLevel) / 100;
                    if (flag6)
                    {
                        DateFile.instance.ChangeActorGongFa(num, BattleSystem.instance.actorNowUseingGongFa, 1, 0, 0, true);
                        BattleSystem.instance.ShowBattleState(10305, isActor, 0);
                    }
                }
            }
        }
    }

}
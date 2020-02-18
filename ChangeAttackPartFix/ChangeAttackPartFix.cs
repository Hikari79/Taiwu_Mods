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

namespace ChangeAttackPartFix
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

    [HarmonyPatch(typeof(BattleSystem), "SetChooseAttackPart")]
    public class XXX
    {
        public static bool Prefix(int typ, ref bool ___chooseAttack, ref int ___actorChooseAttackPart)
        {
            if (!Main.enabled)
                return true;
            bool flag = ___chooseAttack;
            if (!flag)
            {
                ___chooseAttack = true;
                ___actorChooseAttackPart = typ;
                TweenSettingsExtensions.SetUpdate<TweenerCore<Vector3, Vector3, VectorOptions>>(TweenSettingsExtensions.SetEase<TweenerCore<Vector3, Vector3, VectorOptions>>(ShortcutExtensions.DOScale(BattleSystem.instance.attackPartChooseWindow.GetComponent<RectTransform>(), new Vector3(1.2f, 1.2f, 1f), 0.1f), (DG.Tweening.Ease)27), true);
                TweenSettingsExtensions.SetUpdate<TweenerCore<Vector3, Vector3, VectorOptions>>(TweenSettingsExtensions.SetEase<TweenerCore<Vector3, Vector3, VectorOptions>>(TweenSettingsExtensions.SetDelay<TweenerCore<Vector3, Vector3, VectorOptions>>(ShortcutExtensions.DOScale(BattleSystem.instance.attackPartChooseWindow.GetComponent<RectTransform>(), new Vector3(0f, 0f, 1f), 0.1f), 0.1f), (Ease)1), true);
                BattleSystem.instance.StartCoroutine(AttackPartChooseEnd(10.0f));
            }
            return false;
        }

        private static IEnumerator AttackPartChooseEnd(float waitTime)
        {
            yield return new WaitForSecondsRealtime(waitTime);
            BattleSystem.instance.attackPartChooseWindow.SetActive(false);
            BattleSystem.instance.attackPartChooseMask.SetActive(false);
            BattleSystem.instance.CacheStart();
            Utils.Invoke(BattleSystem.instance, "ActionEventAttack", new object[] { true });
            BattleSystem.instance.CacheStop();
            BattleSystem.instance.TimeGo();
            Utils.SetValue(BattleSystem.instance, "chooseAttack", false);
            yield break;
        }
    }
}
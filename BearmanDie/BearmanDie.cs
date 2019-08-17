using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Harmony12;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace BearmanDie
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
        public static string input;
        public static string output;
        public static Type type = Type.defult;
        public enum Type
        {
            defult,arrary,method
        }
        public static int index = 0;
        public static string log;
        public static Traverse traverse;
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
            GUILayout.Label("字段获取&修改&调用方法");
            input = GUILayout.TextField(input);
            if (GUILayout.Button("获取字段&调用方法"))
            {
                string[] temp = input.Split(new char[] { '.',' ' },StringSplitOptions.RemoveEmptyEntries);
                traverse = Traverse.CreateWithType(temp[0]);
                for(int i=0;i<temp.Length-1;i++)
                {
                    if(!temp[i+1].Contains('[')&& !temp[i + 1].Contains('('))
                    {
                        if (traverse.Field(temp[i + 1]).FieldExists())
                            traverse = traverse.Field(temp[i + 1]);
                        else
                            traverse = traverse.Property(temp[i + 1]);
                        type = Type.defult;
                    }
                    else if(!temp[i+1].Contains('('))
                    {
                        string[] tempp = temp[i + 1].Split(new char[] { '[', ']',' ' },StringSplitOptions.RemoveEmptyEntries);
                        traverse = traverse.Field(tempp[0]);
                        for (int j=0;j<tempp.Length-1;j++)
                        {
                            if (traverse.Property("Item", new object[] { int.Parse(tempp[j + 1]) }).GetValue() != null)
                            {
                                type = Type.defult;
                                traverse = traverse.Property("Item", new object[] { int.Parse(tempp[j + 1]) });
                            }
                            else
                            {
                                type = Type.arrary ;
                                index = int.Parse(tempp[j + 1]);
                                output =((int[])traverse.GetValue())[index].ToString();
                            }
                        }
                    }
                    else
                    {
                        string[] tempp = temp[i + 1].Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        object[] obj;
                        if (tempp.Length >= 2)
                        {
                            obj = new object[tempp.Length - 1];
                            MethodInfo methodInfo1 = traverse.GetValue().GetType().GetMethod(tempp[0]);
                            ParameterInfo[] parameterInfos = methodInfo1.GetParameters();
                            for (int j = 0; j < obj.Length; j++)
                            {
                                obj[j] = Convert.ChangeType(tempp[j + 1], parameterInfos[j].ParameterType);
                            }
                        }
                        else
                        {
                            obj = null;
                        }
                        traverse = traverse.Method(tempp[0], obj);
                        type = Type.method;
                    }
                }
                
                switch(type)
                {
                    case Type.defult:
                        output = traverse.GetValue().ToString();
                        break;
                    case Type.method:
                        object ob= traverse.GetValue();
                        if (ob != null)
                            output = ob.ToString();
                        else
                            output = "null";
                        break;
                    default:
                        break;

                }

            }

            output = GUILayout.TextField(output);
            if(GUILayout.Button("修改"))
            {
                System.Type type = traverse.GetValue().GetType();
                if(Main.type==Type.defult)
                traverse.SetValue(Convert.ChangeType(output,type));
                else
                {
                    int[] temp = (int[])traverse.GetValue();
                    temp[index] = int.Parse(output);
                }
            }

            GUILayout.TextArea(
                "熊人降伏修改器简易使用说明\n\n"+
                "获取 & 修改字段\n"+
                "1.在上方输入栏中输入你想获取的字段，格式类似于\"DateFile.instance.dayTime\"或者\"DateFile.instance.actorsDate[10001][12]\"(不含引号以及所有字符应是英文且区分大小写，目前支持获取的字段格式有整数、布尔、浮点数、整数数组、列表、字典)\n"+
                "2.点击获取字段按钮，得到的结果会显示在下方的输入文本框中。\n"+
                "3.可以自行修改文本框中的值，然后点击修改按钮，获取的指定游戏字段的值即被修改为文本框中的数值。\n" +
                "注意事项：修改字段前必须先获取字段，不然可能会出现蜜汁bug。\n\n" +
                "调用方法\n" +
                "1.在上方输入栏中输入你想调用的方法，格式类似与\"DateFile.instance.GetActorDate(10001,12,true)\"(不含引号以及所有字符应是英文且区分大小写，末尾不用写分号，参数应是常量，且类型为整数，浮点，布尔中的一种，不支持调用具有重载的方法)\n" +
                "2.点击调用方法按钮，若方法具有返回值则会显示在下方的输入文本框中，若无返回值则会显示null。\n\n" +
                "附录：\n"+
                "常用字段常用字段/方法\n" +
                "DateFile.instance.mianActorId     太吾传人人物ID\n" +
                "DateFile.instance.dayTime            当前行动力\n" +
                "DateFile.instance.actorsDate[人物ID][61-66]    某人的六维基础值\n" +
                "DateFile.instance.actorsDate[人物ID][501-516]   某人的技艺资质基础值\n" +
                "DateFile.instance.actorsDate[人物ID][601-614]    某人的功法资质基础值\n" +
                "DateFile.instance.actorsDate[人物ID][401-408]  某人的资源\n" +
                "DateFile.instance.actorsDate[人物ID][551]   技艺资质成长\n" +
                "DateFile.instance.actorsDate[人物ID][651]   功法资质成长\n" +
                "DateFile.instance.actorInjuryDate[人物ID].Clear()      某人伤势痊愈（非战斗时）\n" +
                "DateFile.instance.AddSocial(人物ID1,人物ID2,关系类型)        人物1添加人物2的某种关系 \n"+
                "DateFile.instance.RemoveActorSocial(人物ID1,人物ID2,关系类型)       删除人物1关系中某个对人物2的关系\n" +
                "DateFile.instance.ChangeFavor(人物ID,好感值,true,true)      让某人对太吾的好感改变\n" +
                "DateFile.instance.ChangeActorGongFa(人物ID,功法ID,修习度,心法等级,逆练等级,true)        修改人物某功法的数据\n"+
                "DateFile.instance.AddActorFeature(人物ID,特性ID)        添加人物特性\n" +
                "DateFile.instance.ChangeActorFeature(人物ID,旧特性ID,新特性ID)   改变某人的某个特性\n" +
                "DateFile.instance.ChangeTwoActorItem(拾趣物品人物ID,得到物品人物ID,物品ID,物品数量,-1,0,0)\n" +
                "DateFile.instance.GangActorLevelUp(人物ID,目标门派ID,目标门派地位)            改变某人门派及地位(1-9，1最高)\n" +
                "...懒得写了，看需求再加\n");
        }
        
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }

}
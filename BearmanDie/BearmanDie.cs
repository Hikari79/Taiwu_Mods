using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Harmony12;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;

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
        public static int index = 0;
        public static string log;
        public static Type type;
        public static DataType dataType;
        public enum DataType
        {
            basic, array, method,
        }
        public static XQ xQ;
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
                string[] temp = input.Replace(" ",string.Empty).Replace(";",string.Empty).Split(new char[] { '.'}, StringSplitOptions.RemoveEmptyEntries);
                bool[] flag = new bool[temp.Length];
                string namespaceClassName = string.Empty;
                int index = 0;
                for (int i = 0; i < temp.Length; i++)
                {
                    namespaceClassName += temp[i];
                    type = AccessTools.TypeByName(namespaceClassName);
                    if (type != null)
                    {
                        flag[i] = true;
                        index = i;
                    }
                    else
                    {
                        flag[i] = false;
                    }
                    namespaceClassName += ".";
                }
                string name = string.Join(".", temp, 0, index + 1);
                Main.Logger.Log(name+index);
                xQ =new XQ(name);
                for (int i = index; i < temp.Length - 1; i++)
                {
                    if (!temp[i + 1].Contains('[') && !temp[i + 1].Contains('('))
                    {
                        Main.Logger.Log(temp[i + 1]);GameData.Characters.GetCharProperty(10001, 14);
                        try
                        {
                            if (xQ.FieldExists(temp[i+1]))
                            {
                                xQ = xQ.Field(temp[i + 1]);
                                Main.Logger.Log("字段");
                            }
                            else
                            {
                                xQ = xQ.Property(temp[i + 1]);
                                Main.Logger.Log("属性");
                            }
                            dataType = DataType.basic;
                        }
                        catch (Exception e)
                        {
                            Main.Logger.Log(e.Message);
                        }
                    }
                    else if (!temp[i + 1].Contains('('))
                    {
                        string[] tempp = temp[i + 1].Split(new char[] { '[', ']', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        xQ  =xQ .Field(tempp[0]);
                        for (int j = 0; j < tempp.Length - 1; j++)
                        {
                            if (xQ .Property("Item", new object[] { int.Parse(tempp[j + 1]) }).GetValue() != null)
                            {
                                dataType = DataType.basic;
                                xQ  = xQ .Property("Item", new object[] { int.Parse(tempp[j + 1]) });
                            }
                            else
                            {
                                dataType = DataType.array;
                                index = int.Parse(tempp[j + 1]);
                                output = ((int[])xQ.GetValue())[index].ToString();
                            }
                        }
                    }
                    else
                    {
                        string[] tempp = temp[i + 1].Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        object[] obj;
                        if (tempp.Length>1)
                        {
                            obj = new object[tempp.Length - 1];
                            MethodInfo methodInfo1 =(xQ._type??xQ.Resolve()._type).GetMethod(tempp[0],AccessTools.all);
                            ParameterInfo[] parameterInfos = methodInfo1.GetParameters();
                            for (int j = 0; j < obj.Length; j++)
                            {
                                obj[j] = Convert.ChangeType(tempp[j + 1], parameterInfos[j].ParameterType);
                                Main.Logger.Log(obj[j].ToString());
                            }  
                        }
                        else
                        {
                            obj = null;
                        }
                        xQ  = xQ.Method(tempp[0], obj);
                        dataType = DataType.method;
                    }
                }
                switch (dataType)
                {
                    case DataType.basic:
                        output = (xQ .GetValue() ?? "null").ToString();
                        break;
                    case DataType.method:
                        object ob = xQ.GetValue();
                        Main.Logger.Log(xQ._method.Name);
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
            if (GUILayout.Button("修改"))
            {
                System.Type type = xQ.GetValue().GetType();
                if (dataType == DataType.basic)
                    xQ.SetValue(Convert.ChangeType(output, type));
                else
                {
                    int[] temp = (int[])xQ.GetValue();
                    temp[index] = int.Parse(output);
                }
            }
            GUILayout.TextArea(
                "熊人降伏修改器简易使用说明\n\n" +
                "获取 & 修改字段\n" +
                "1.在上方输入栏中输入你想获取的字段，格式类似于\"DateFile.instance.dayTime\"或者\"DateFile.instance.actorsDate[10001][12]\"(不含引号以及所有字符应是英文且区分大小写，目前支持获取的字段格式有整数、布尔、浮点数、整数数组、列表、字典)\n" +
                "2.点击获取字段按钮，得到的结果会显示在下方的输入文本框中。\n" +
                "3.可以自行修改文本框中的值，然后点击修改按钮，获取的指定游戏字段的值即被修改为文本框中的数值。\n" +
                "注意事项：修改字段前必须先获取字段，不然可能会出现蜜汁bug。\n\n" +
                "调用方法\n" +
                "1.在上方输入栏中输入你想调用的方法，格式类似与\"DateFile.instance.GetActorDate(10001,12,true)\"(不含引号以及所有字符应是英文且区分大小写，末尾不用写分号，参数应是常量，且类型为整数，浮点，布尔中的一种，不支持调用具有重载的方法)\n" +
                "2.点击调用方法按钮，若方法具有返回值则会显示在下方的输入文本框中，若无返回值则会显示null。\n\n" +
                "附录：\n" +
                "常用字段常用字段/方法\n" +
                "DateFile.instance.mianActorId     太吾传人人物ID\n" +
                "DateFile.instance.dayTime            当前行动力\n" +
                "GameData.Characters.SetCharProperty(人物ID,index,value)    修改某人index项数据至value\n" +
                "DateFile.instance.actorInjuryDate[人物ID].Clear()      某人伤势痊愈（非战斗时）\n" +
                "DateFile.instance.battleActorsInjurys[人物ID].Clear()     某人伤势痊愈      (战斗时)   \n" +
                "DateFile.instance.AddSocial(人物ID1,人物ID2,关系类型)        人物1添加人物2的某种关系 \n" +
                "DateFile.instance.RemoveActorSocial(人物ID1,人物ID2,关系类型)       删除人物1关系中某个对人物2的关系\n" +
                "DateFile.instance.ChangeFavor(人物ID,好感值,true,true)      让某人对太吾的好感改变\n" +
                "DateFile.instance.ChangeActorGongFa(人物ID,功法ID,修习度,心法等级,逆练等级,true)        修改人物某功法的数据\n" +
                "DateFile.instance.AddActorFeature(人物ID,特性ID)        添加人物特性\n" +
                "DateFile.instance.ChangeActorFeature(人物ID,旧特性ID,新特性ID)   改变某人的某个特性\n" +
                "DateFile.instance.ChangeTwoActorItem(拾趣物品人物ID,得到物品人物ID,物品ID,物品数量,-1,0,0)\n" +
                "DateFile.instance.GangActorLevelUp(人物ID,目标门派ID,目标门派地位)            改变某人门派及地位(1-9，1最高)\n");
        }



        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }


    public class XQ
    {
        public  Type _type;
        private object _root;
        private MemberInfo _info;
        private object[] _params;
        public MethodBase _method;

        public XQ()
        {

        }
        public XQ(string name)
        {
            _type = AccessTools.TypeByName(name);
        }

        public XQ(object root)
        {
            this._root = root;
            this._type = ((root != null) ? root.GetType() : null);
        }

        public XQ(object root, MemberInfo info, object[] index)
        {
            this._root = root;
            this._type = ((root != null) ? root.GetType() : null);
            this._info = info;
            this._params = index;
        }

        private XQ(object root, MethodInfo method, object[] parameter)
        {
            this._root = root;
            this._type = method.ReturnType;
            this._method = method;
            this._params = parameter;
        }

        public XQ Field(string name)
        {
            bool flag = name == null;
            if (flag)
            {
                throw new ArgumentNullException("name cannot be null");
            }
            XQ xQ = this.Resolve();
            bool flag2 = xQ._type == null;
            XQ result;
            if (flag2)
            {
                result = new XQ();
            }
            else
            {
                FieldInfo fieldInfo = xQ._type.GetField(name, AccessTools.all);
                bool flag3 = fieldInfo == null;
                if (flag3)
                {
                    throw new Exception($"未在{_type.FullName}中找到字段{name}");
                    result = new XQ();
                }
                else
                {
                    bool flag4 = !fieldInfo.IsStatic && xQ._root == null;
                    if (flag4)
                    {
                        throw new Exception($"{name}非静态字段或者未给出实例对象");
                        result = new XQ();
                    }
                    else
                    {
                        result = new XQ(xQ._root, fieldInfo, null);
                    }
                }
            }
            return result;
        }
        public bool FieldExists(string s)
        {
            XQ xq = this.Resolve();
            List<string> name = AccessTools.GetFieldNames(xq._type);
            return name.Contains(s);
        }


        public XQ Property(string name, object[] index = null)
        {
            bool flag = name == null;
            if (flag)
            {
                throw new ArgumentNullException("name cannot be null");
            }
            XQ xQ = this.Resolve();
            bool flag2 = xQ._type == null;
            XQ result;
            if (flag2)
            {
                result = new XQ();
            }
            else
            {
                PropertyInfo propertyInfo = xQ._type.GetProperty(name, AccessTools.all);
                bool flag3 = propertyInfo == null;
                if (flag3)
                {
                    result = new XQ();
                }
                else
                {
                    result = new XQ(xQ._root, propertyInfo, index);
                }
            }
            return result;
        }
        public XQ Method(string name, params object[] arguments)
        {
            bool flag = name == null;
            if (flag)
            {
                throw new ArgumentNullException("name cannot be null");
            }
            XQ xQ = this.Resolve();
            bool flag2 = xQ._type == null;
            XQ result;
            if (flag2)
            {
                result = new XQ();
            }
            else
            {
                Type[] types = AccessTools.GetTypes(arguments);
                MethodBase methodInfo = xQ._type.GetMethod(name, types);
                bool flag3 = methodInfo == null;
                if (flag3)
                {
                    throw new Exception($"未找到{name}方法");
                    result = new XQ();
                }
                else
                {
                    result = new XQ(xQ._root, (MethodInfo)methodInfo, arguments);
                }
            }
            return result;
        }
        public XQ Resolve()
        {
            bool flag = this._root == null && this._type != null;
            XQ result;
            if (flag)
            {
                result = this;
            }
            else
            {
                result = new XQ(this.GetValue());
            }
            return result;
        }



        public object GetValue()
        {
            bool flag = this._info is FieldInfo;
            object result;
            if (flag)
            {
                result = ((FieldInfo)this._info).GetValue(this._root);
            }
            else
            {
                bool flag2 = this._info is PropertyInfo;
                if (flag2)
                {
                    result = ((PropertyInfo)this._info).GetValue(this._root, AccessTools.all, null, this._params, System.Globalization.CultureInfo.CurrentCulture);
                }
                else
                {
                    bool flag3 = this._method != null;
                    if (flag3)
                    {
                        result = this._method.Invoke(this._root, this._params);
                    }
                    else
                    {
                        bool flag4 = this._root == null && this._type != null;
                        if (flag4)
                        {
                            result = this._type;
                        }
                        else
                        {
                            result = this._root;
                        }
                    }
                }
            }
            return result;
        }
        public XQ SetValue(object value)
        {
            bool flag = this._info is FieldInfo;
            if (flag)
            {
                ((FieldInfo)this._info).SetValue(this._root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
            }
            bool flag2 = this._info is PropertyInfo;
            if (flag2)
            {
                ((PropertyInfo)this._info).SetValue(this._root, value, AccessTools.all, null, this._params, CultureInfo.CurrentCulture);
            }
            bool flag3 = this._method != null;
            if (flag3)
            {
                throw new Exception("cannot set value of method " + this._method.FullDescription());
            }
            return this;
        }
    }

}
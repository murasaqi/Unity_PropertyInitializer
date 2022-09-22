using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;


public static class PropertyInitializerUtility
{
    public static bool IsSerializable(this FieldInfo fieldInfo)
    {
        var attributes = fieldInfo.GetCustomAttributes(true);
        if (attributes.Any(attr => attr is NonSerializedAttribute))
            return false;
 
        if (fieldInfo.IsPrivate && !attributes.Any(attr => attr is SerializeField))
            return false;
 
        return fieldInfo.FieldType.IsSerializable;
    }
 
    public static bool IsSerializable(this Type type)
    {
        if (type.IsSubclassOf(typeof(UnityEngine.Object)) ||
            type.IsEnum ||
            type.IsValueType ||
            type == typeof(string)
           )
            return true;
 
        var arrayType = type.GetArrayType();
        if (arrayType != null)
            return arrayType.IsSerializable();
 
        return false;
    }
 
    private static Type GetArrayType(this Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
 
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return type.GetGenericArguments()[0];
        return null;
    }
    public static object DeepCopy(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        var type = obj.GetType();

        // プリミティブ型、列挙型、文字列型はDeepCopyする必要がないため、そのままreturn
        if (type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            return obj;
        }

        // Delegate, Action, FuncはBinaryCopyする
        if (obj is Delegate)
        {
            return BinaryCopy(obj);
        }

        // 配列はそのまま返すと shallow copy になるため、処理
        if (type.IsArray)
        {
            // 何の配列なのかを取得
            var elementType = Type.GetType(type.FullName.Replace("[]", string.Empty));
            if (elementType == null)
            {
                // throw new Exception
                return null;
            }

            // T[] を Array へ変換
            var array = obj as Array;
            if (array == null)
            {
                // throw new Exception
                return null;
            }

            // T[]のインスタンスを作成
            var copiedArray = Array.CreateInstance(elementType, array.Length);

            // 要素をコピー
            foreach (var i in Enumerable.Range(0, array.Length))
            {
                copiedArray.SetValue(DeepCopy(array.GetValue(i)), i);
            }

            return copiedArray;
        }

        if (type.IsClass || type.IsValueType)
        {
            // Tのインスタンスを作成
            var copiedInstance = Activator.CreateInstance(type);

            // フィールドをそれぞれコピー
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(obj);
                if (fieldValue == null)
                {
                    continue;
                }
                field.SetValue(copiedInstance, DeepCopy(fieldValue));
            }
            return copiedInstance;
        }

        // throw new ArgumentException("The object is unknown type");
        return null;
    }
    

    
    
    public static ListView CreateEmptyListView()
    {
        const int itemHeight = 16;
        var listView = new ListView();
        listView.showAddRemoveFooter = true;
        listView.selectionType = SelectionType.Multiple;
        listView.reorderable = true;
        listView.reorderMode = ListViewReorderMode.Animated;
        listView.showBorder = true;
        listView.showFoldoutHeader = true;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        listView.showBoundCollectionSize = true;
        listView.showAddRemoveFooter = true;
        return listView;
    }

    public static Type SystemTypeToUnityObjectType(Type type)
    {
        Type convertedType = null;
        if(type == typeof(System.Int32)) convertedType = typeof(int);
        if(type == typeof(System.Int64)) convertedType = typeof(long);
        if(type == typeof(System.Single)) convertedType = typeof(float);
        if(type == typeof(System.Double)) convertedType = typeof(double);
        if(type == typeof(System.Boolean)) convertedType = typeof(bool);
        if(type == typeof(System.String)) convertedType = typeof(string);
        if(type == typeof(System.Char)) convertedType = typeof(char);
        if(type == typeof(System.Byte)) convertedType = typeof(byte);
        if(type == typeof(System.SByte)) convertedType = typeof(sbyte);
        if(type == typeof(System.UInt16)) convertedType = typeof(ushort);
        if(type == typeof(System.UInt32)) convertedType = typeof(uint);
        if(type == typeof(System.UInt64)) convertedType = typeof(ulong);
        if(type == typeof(System.Decimal)) convertedType = typeof(decimal);
        // if(type == typeof(System.Object)) convertedType = typeof(object);
        // if(type == typeof(System.Void)) convertedType = typeof(void);
        return convertedType;
    }
    public static VisualElement GetBaseField<T>(object value, string key =null, object databe = null)
    {
        var type = typeof(T);
        VisualElement field = null;
        if (type.IsArray)
        {
            var array = value as Array;
            if (array == null)
            {
                return null;
            }
            var listView = CreateEmptyListView();
            var elementType = typeof(T).ToString().Replace("[]", "");
            var deepCopiedArray = DeepCopy(array) as Array;

            
            Func<VisualElement> makeItem = () =>
            {
                MethodInfo method = typeof(PropertyInitializerUtility).GetMethod("GetBaseField");
                MethodInfo generic = method.MakeGenericMethod(Type.GetType(elementType));
                var inputField = generic.Invoke(null, new object[] { null ,key,databe}) as VisualElement;
                return inputField;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                
                var property = e.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty);
                property.SetValue(e, array.GetValue(i));
                e.name = $"{key}:{i}";
                listView.itemsSource[i] = deepCopiedArray.GetValue(i);

            };
           
            listView.itemsSource = deepCopiedArray;
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.name = key;
            listView.headerTitle = key;
            field = listView;
        } else if (type.IsGenericType &&  type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var values = value as IList;

            var listView = CreateEmptyListView();
            Func<VisualElement> makeItem = () =>
            {
                MethodInfo method = typeof(PropertyInitializerUtility).GetMethod("GetBaseField");
                MethodInfo generic = method.MakeGenericMethod(elementType);
                var inputField = generic.Invoke(null, new object[] { null ,key,databe}) as VisualElement;
                return inputField;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                if(e.Q<Label>() != null)
                {
                    e.Q<Label>().text = $"{key}:{i}";
                }
                e.name = $"{key}:{i}";
                var property = e.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty);
                property.SetValue(e, values[i]);
                listView.itemsSource[i] = values[i];
            };
            listView.itemsSource = values;
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.name = key;
            listView.headerTitle = key;
            field = listView;
            
        }
        else
        {
            if (type == typeof(System.Int32) || type == typeof(int))
            {
                var integerField= new IntegerField();
                if(value != null)integerField.value = (int)value;
                field = integerField;
            }
            else if (type == typeof(System.Single) || type == typeof(float))
            {
                var floatField= new FloatField();
                if(value != null)floatField.value = (float)value;
                field = floatField;
            }
            else if (type == typeof(System.Double) || type == typeof(double))
            {
                var doubleField= new DoubleField();
                if(value != null)doubleField.value = (double)value;
                field = doubleField;
            }
            else if (type == typeof(System.Boolean) || type == typeof(bool))
            {
                var toggle = new Toggle();
                if(value != null)toggle.value = (bool)value;
                field = toggle;
            }
            else if (type == typeof(System.String) || type == typeof(string))
            {
                var textField= new TextField();
                if(value != null)textField.value = (string)value;
                field = textField;
            }
            else if (type == typeof(System.Enum) || type.IsEnum)
            {
                var enumField= new EnumField();
                if(value != null)enumField.value = (Enum)value;
                field = enumField;
            }
            else if (type == typeof(Color))
            {
                var colorField= new ColorField();
                colorField.value = (Color)value;
                field = colorField;
            }
            else if (type == typeof(UnityEngine.Object))
            {
                var objectField= new ObjectField();
                objectField.objectType = type;
                objectField.value = (UnityEngine.Object)value;
               
                field = objectField;
            }


            // field.AddToClassList("unity-property-field__input");
            field.name = key;
            
            var f = field as BaseField<T>;

            if (key != null)
            {
                f.label = key;
                f.name = key;
            }
            // var label = field.Q<Label>();
            // label.style.marginRight = -13;
            // label.style.minWidth = 75;
            // label.style.paddingLeft = 1;
            // label.style.paddingRight = 15;
            // label.style.paddingTop = 2;
            // label.style.overflow = Overflow.Hidden;
            // f.style.height = new StyleLength(StyleKeyword.Auto);
            // f.style.bottom = 1;
            
            f.RegisterValueChangedCallback((evt =>
            {
                if (databe != null)
                {
                    var names=f.name.Split(":");
                    var propertyPath = names[0];
                    var jsonSerializerTest = databe as JsonSerializeTest;
                    if (names.Length == 1)
                    {
                        jsonSerializerTest.UpdateSerializedValue(propertyPath,JsonConvert.SerializeObject(evt.newValue));     
                    }
                    else
                    {
                        VisualElement parent = f;
                        while (parent.parent != null)
                        {
                            parent = parent.parent;
                            if (parent.name == propertyPath)
                            {
                                break;
                            }
                        }
                        if (parent.GetType() == typeof(ListView))
                        {
                            
                            var listView = parent as ListView;
                            if (listView != null && listView.itemsSource != null)
                            {

                                var list = listView.itemsSource as IList;
                                
                                list[int.Parse(names[1])] = evt.newValue;
                                var serializedValue = JsonConvert.SerializeObject(list);
                                jsonSerializerTest.UpdateSerializedValue(propertyPath,serializedValue);
                            }
                        }   
                    }
                    jsonSerializerTest.SaveToJsonText();
                }
               
                
            }));
        }
        return field;


    }



    static T BinaryCopy<T>(T source)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException();
        }

        if (source == null)
        {
            return default(T);
        }

        using (var memory = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(memory, source);
            memory.Seek(0, SeekOrigin.Begin);
            return (T) formatter.Deserialize(memory);
        }
    }
}
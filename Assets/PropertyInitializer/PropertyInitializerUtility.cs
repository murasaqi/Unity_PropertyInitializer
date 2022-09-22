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
using UnityEditor;
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
        var listView = new ListView();
        listView.showAddRemoveFooter = true;
        listView.selectionType = SelectionType.Multiple;
        listView.reorderable = true;
        listView.reorderMode = ListViewReorderMode.Animated;
        listView.showBorder = true;
        listView.showFoldoutHeader = true;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        listView.showBoundCollectionSize = true;
        listView.showAddRemoveFooter = false;
     
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
        return convertedType;
    }
    public static VisualElement GetBaseField<T>(object value, string key =null, object databe = null)
    {
        var type = typeof(T);
        VisualElement field = null;
        if (type.IsArray && !type.ToString().Contains("UnityEngine.Vector"))
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
                var inputField = generic.Invoke(null, new object[] { null ,null,databe}) as VisualElement;
                Debug.Log(listView);
                return inputField;
            };
            
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                if(e.Q<Label>() != null)
                {
                    e.Q<Label>().text = $"{key}:{i}";
                }
                var property = e.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty);
                property.SetValue(e, array.GetValue(i));
                e.name = $"type(IList)____{key}:{i}";
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
                var inputField = generic.Invoke(null, new object[] { null ,null,databe}) as VisualElement;
                Debug.Log(inputField);
                return inputField;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                if(e.Q<Label>() != null)
                {
                    e.Q<Label>().text = $"{key}:{i}";
                }
                e.name = $"type(IList)____{key}:{i}";
                var property = e.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty);
                property.SetValue(e, values[i]);
                // listView.itemsSource[i] = values[i];
            
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
                integerField.value = value != null ? (int)value : 0;
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
            else if (type == typeof(Vector2))
            {
                var objectField= new Vector2Field();
                objectField.value = (Vector2)value;
               
                field = objectField;
            }else if (type == typeof(Vector2Int))
            {
                var objectField= new Vector2IntField();
                objectField.value = (Vector2Int)value;
               
                field = objectField;
            }
            else if (type == typeof(Vector3))
            {
                var objectField = new Vector3Field();
                objectField.value = (Vector3)value;

                field = objectField;
            }
            else if (type == typeof(Vector3Int))
            {
                var objectField = new Vector3IntField();
                objectField.value = (Vector3Int)value;
                field = objectField;
            }
            else if (type == typeof(Vector4))
            {
                var objectField = new Vector4Field();
                objectField.value = (Vector4)value;
                field = objectField;
            }
            else if (type == typeof(Bounds))
            {
                var objectField = new BoundsField();
                objectField.value = (Bounds)value;
                field = objectField;
            }
            else if (type == typeof(Rect))
            {
                var objectField = new RectField();
                objectField.value = (Rect)value;
                field = objectField;
            }
            else if (type == typeof(RectInt))
            {
                var objectField = new RectIntField();
                objectField.value = (RectInt)value;
                field = objectField;
            }
            else if (type == typeof(AnimationCurve))
            {
                var objectField = new CurveField();
                objectField.value = (AnimationCurve)value;
                field = objectField;
            }
            else if (type == typeof(LayerMask))
            {
                var objectField = new LayerField();
                objectField.value = (LayerMask)value;
                field = objectField;
            }
            else if (type == typeof(Gradient))
            {
                var objectField = new GradientField();
                objectField.value = (Gradient)value;
                field = objectField;
            }
            else if (type == typeof(AnimationClip))
            {
                var objectField = new ObjectField();
                objectField.objectType = type;
                objectField.value = (AnimationClip)value;
                field = objectField;
            }
          

            var f = field as BaseField<T>;

            if (key != null)
            {
                f.label = key;
                f.name = key;
            }
           
            f.RegisterValueChangedCallback((evt =>
            {
                Debug.Log(evt);
                if (databe != null)
                {
                    var names=f.name.Split("____");
                    // var listName = names[0];
                    var propertyPath = names[0];
                    var propertyInitializer = databe as PropertyInitializer;
                    if (names.Length == 1)
                    {
                        var type = propertyInitializer.target.GetType().GetField(propertyPath).FieldType;
                        if(!type.ToString().Contains("UnityEngine."))
                        {
                            
                            propertyInitializer.UpdateSerializedValue(propertyPath, JsonConvert.SerializeObject(evt.newValue));
                        }else
                        {   
                            propertyInitializer.UpdateSerializedValue(propertyPath,JsonUtility.ToJson(evt.newValue));     
                            
                        }
                       
                    }
                    else
                    {
                        var split = names[1].Split(":");
                        var listPropertyName = split[0];
                        var index = split[1];
                        VisualElement parent = f.parent;
                        while (parent.parent != null)
                        {
                            if (parent.name == listPropertyName)
                            {
                                Debug.Log(parent.name);
                                break;
                            }
                            parent = parent.parent;
                        }
                        if (parent.GetType() == typeof(ListView))
                        {
                            
                            var listView = parent as ListView;
                            if (listView != null && listView.itemsSource != null)
                            {

                                var list = listView.itemsSource as IList;
                                
                                list[int.Parse(index)] = evt.newValue;
                                var serializedValue = JsonConvert.SerializeObject(list);
                                propertyInitializer.UpdateSerializedValue(listPropertyName,serializedValue);
                            }
                        }   
                    }
                    propertyInitializer.SaveToJsonText();
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
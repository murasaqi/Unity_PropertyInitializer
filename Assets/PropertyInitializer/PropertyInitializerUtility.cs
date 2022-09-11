using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


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

    // public static VisualElement GetField(Type type, object value)
    // {
    //     
    // }

    public static VisualElement GetArrayField(Type type, object value)
    {
        VisualElement field = null;
        if (type.IsArray)
        {
           
            var array = value as Array;
            if (array == null)
            {
                // throw new Exception
                return null;
            }

            Debug.Log(array);
            // T[]のインスタンスを作成
            var copiedArray = Array.CreateInstance(type, array.Length);

            // 要素をコピー
            foreach (var i in Enumerable.Range(0, array.Length))
            {
                copiedArray.SetValue(DeepCopy(array.GetValue(i)), i);
            }

            
            Func<VisualElement> makeItem = () => new Label();

            // As the user scrolls through the list, the ListView object
            // recycles elements created by the "makeItem" function,
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list).
            Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = copiedArray.GetValue(i).ToString();

            // Provide the list view with an explict height for every row
            // so it can calculate how many items to actually display
            const int itemHeight = 16;

            var listView = new ListView(copiedArray, itemHeight, makeItem, bindItem);

            listView.selectionType = SelectionType.Multiple;

            listView.onItemsChosen += objects => Debug.Log(objects);
            listView.onSelectionChange += objects => Debug.Log(objects);

            listView.style.flexGrow = 1.0f;
            field = listView;
        } else if (type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var values = value as IList;
            
            var elementType = type.GetGenericArguments()[0];
            
            Func<VisualElement> makeItem = () =>
            {
                MethodInfo method = typeof(PropertyInitializerUtility).GetMethod("GetBaseField");
                MethodInfo generic = method.MakeGenericMethod(elementType);
                return generic.Invoke(null, new object[] { null }) as VisualElement;
            };
            Action<VisualElement, int> bindItem = (e, i) =>
            {
               e.GetType().GetField("value").SetValue(e, values[i]);
            };
            const int itemHeight = 16;
            var listView = new ListView(values, itemHeight, makeItem, bindItem);
            
            listView.selectionType = SelectionType.Multiple;
            
            listView.onItemsChosen += objects => Debug.Log(objects);
            listView.onSelectionChange += objects => Debug.Log(objects);
            
            listView.style.flexGrow = 1.0f;
            field = listView;
            
            
            
        }

        return field;
    }
    public static VisualElement GetBaseField<T>(object value, EventCallback<ChangeEvent<T>> callback  =null)
    {
        var type = typeof(T);
        VisualElement field = null;
        if (type.IsArray)
        {
           Debug.Log(value);
            var array = value as Array;
            if (array == null)
            {
                // throw new Exception
                return null;
            }

            // Debug.Log(array);
            // T[]のインスタンスを作成
            var copiedArray = Array.CreateInstance(type, array.Length);

            // 要素をコピー
            foreach (var i in Enumerable.Range(0, array.Length))
            {
                copiedArray.SetValue(DeepCopy(array.GetValue(i)), i);
            }

            
            Func<VisualElement> makeItem = () => new Label();

            // As the user scrolls through the list, the ListView object
            // recycles elements created by the "makeItem" function,
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list).
            Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = copiedArray.GetValue(i).ToString();

            // Provide the list view with an explict height for every row
            // so it can calculate how many items to actually display
            const int itemHeight = 16;

            var listView = new ListView(copiedArray, itemHeight, makeItem, bindItem);

            listView.selectionType = SelectionType.Multiple;

            listView.onItemsChosen += objects => Debug.Log(objects);
            listView.onSelectionChange += objects => Debug.Log(objects);

            listView.style.flexGrow = 1.0f;
            field = listView;
        } else if (type.IsGenericType &&  type.GetGenericTypeDefinition() == typeof(List<>))
        {
            Debug.Log(value);
            var elementType = type.GetGenericArguments()[0];
            var values = value as IList;
            

            Func<VisualElement> makeItem = () =>
            {
                MethodInfo method = typeof(PropertyInitializerUtility).GetMethod("GetBaseField");
                MethodInfo generic = method.MakeGenericMethod(elementType);
                return generic.Invoke(null, new object[] { null ,null}) as VisualElement;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var property = e.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty);
                property.SetValue(e, values[i]);
            };

            const int itemHeight = 16;

            var listView = new ListView(values, itemHeight, makeItem, bindItem);

            listView.selectionType = SelectionType.Multiple;

            listView.onItemsChosen += objects => Debug.Log(objects);
            listView.onSelectionChange += objects => Debug.Log(objects);

            listView.style.flexGrow = 1.0f;
            field = listView;
            
        }
        else
        {
            if (type == typeof(System.Int32) || type == typeof(int))
            {
                var integerField= new IntegerField();
                integerField.value = (int)value;
                field = integerField;
            }
            else if (type == typeof(System.Single) || type == typeof(float))
            {
                var floatField= new FloatField();
                floatField.value = (float)value;
                field = floatField;
            }
            else if (type == typeof(System.Double) || type == typeof(double))
            {
                var doubleField= new DoubleField();
                doubleField.value = (double)value;
                field = doubleField;
            }
            else if (type == typeof(System.Boolean) || type == typeof(bool))
            {
                var toggle= new Toggle();
                toggle.value = (bool)value;
                field = toggle;
            }
            else if (type == typeof(System.String) || type == typeof(string))
            {
                var textField= new TextField();
                textField.value = (string)value;
                field = textField;
            }
            else if (type == typeof(System.Enum) || type.IsEnum)
            {
                var enumField= new EnumField();
                enumField.value = (Enum)value;
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
            
            field.RegisterCallback<ChangeEvent<T>>((e) =>
            {
                Debug.Log(e.newValue);
            });

            var method = typeof(INotifyValueChangedExtensions).GetMethod("RegisterValueChangedCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .MakeGenericMethod(type);
          
            method.Invoke(field, new object[] { null, callback });
            Debug.Log(method);

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
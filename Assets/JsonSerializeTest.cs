using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


[Serializable]
public struct SerializedPropertyInfo
{
    public string name;
    public string type;
    public string value;
}

[CustomEditor(typeof(JsonSerializeTest))]
public class JsonSerializeTestEditor: Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var jsonSerializeTest =serializedObject.targetObject as JsonSerializeTest;
        // jsonSerializeTest.json = "";
        var root = new VisualElement();
        
        root.Add(new PropertyField(serializedObject.FindProperty("target")));
        var json = new PropertyField(serializedObject.FindProperty("json"));
        root.Add(new PropertyField(serializedObject.FindProperty("serializedPropertyInfos")));
        root.Add(json);

        var fields = jsonSerializeTest.target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        jsonSerializeTest.jObject = JObject.Parse(jsonSerializeTest.json);
        var serializedTargetObject = new SerializedObject(serializedObject.FindProperty("target").objectReferenceValue);
        foreach (var fSerializedPropertyInfo in jsonSerializeTest.serializedPropertyInfos)
        {
            
            var valueStr =fSerializedPropertyInfo.value;
            
            // var f =jsonSerializeTest.target.GetType().GetField(fSerializedPropertyInfo.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // TODO : JsonのKeyからプロパティを算出してそのプロパティの型にキャストしたJsonのValueで初期化する
            var name = fSerializedPropertyInfo.name;
            var typ = Type.GetType(fSerializedPropertyInfo.type);
            var field =typeof(PropertyInitializerUtility)
                .GetMethod("GetBaseField")
                .MakeGenericMethod(Type.GetType(fSerializedPropertyInfo.type))
                .Invoke(null, new object[] {jsonSerializeTest.ConvertJsonStrToObject(typ,valueStr) ,name ,jsonSerializeTest}) as VisualElement;
            //
            if(field !=null)root.Add(field);
            
        }
        
        

        var serializeButton = new Button();
        serializeButton.text = "Serialize";
        serializeButton.clicked += jsonSerializeTest.ToJson;
        root.Add(serializeButton);
        
        var applyButton = new Button();
        applyButton.text = "Apply";
        applyButton.clicked += jsonSerializeTest.Apply;
        root.Add(applyButton);
        // root.Add(button);
        return root;
    }

   
}
[ExecuteAlways]
public class JsonSerializeTest : MonoBehaviour
{

    public MonoBehaviour target;
    public string json;
    public List<SerializedPropertyInfo> serializedPropertyInfos = new List<SerializedPropertyInfo>();
    public JObject jObject;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void OnValidate()
    {
        // throw new NotImplementedException();
    }
    [ContextMenu("Serialize")]
    public void ToJson()
    {
        serializedPropertyInfos.Clear();
        json = JsonUtility.ToJson(target);
        var fields = target.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        jObject = JObject.Parse(json);

        foreach (var f in fields)
        {
            var value = PropertyInitializerUtility.DeepCopy(f.GetValue(target));
            var type = value.GetType();
            var typeName = value.GetType().ToString();
            var valueToStr = jObject[f.Name].ToString();
            Debug.Log(value);
            serializedPropertyInfos.Add(new SerializedPropertyInfo()
            {
                name = f.Name,
                type = typeName,
                value = valueToStr
            });
        }
        
    }

    public object ConvertJsonStrToObject(Type type, string jsonValue)
    {
        
        // var value = jsonValue;

         if (type.IsValueType || type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            var deserializedObject = Activator.CreateInstance(type);
            deserializedObject = Convert.ChangeType(deserializedObject, type);
            // Debug.Log(deserializedObject);
            return deserializedObject;
        }else
        if(type.IsArray)
        {
            var elementType = Type.GetType(type.ToString().Replace("[]", ""));
            var parsed = jsonValue.Replace("[","").Replace("]","").Replace(" \"", "").Replace("\"", "").Split(",");
            // var deserializedObject = Activator.CreateInstance(type) as Array;
            var array = Array.CreateInstance(elementType, parsed.Length) as Array;
            
            for (int i = 0; i < parsed.Length; i++)
            {
                try
                {
                    var result = Convert.ChangeType(parsed[i], elementType);
                    if (result != null)
                    {
                        Debug.Log(result);
                        array.SetValue(result,i);
                    }
                }
                catch (Exception e)
                {
                    // Console.WriteLine(e);
                    // throw;
                }
           
            }
            return array;
        }else if (type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            Debug.Log(elementType);
            // Debug.Log($"{type.GetGenericTypeDefinition()}{elementType},{deserializedObject}");
            var parsed = jsonValue.Replace("[","").Replace("]","").Replace(" \"", "").Replace("\"", "").Split(",").ToList();
           
            var copiedArray =  (IList) Activator.CreateInstance(type);
            copiedArray.Clear();
            parsed.ForEach((x) =>
            {
                try
                {
                    var result = Convert.ChangeType(x, elementType);
                    if (result != null)
                    {
                        Debug.Log(result);
                        copiedArray.Add(result);
                    }
                }
                catch (Exception e)
                {
                    // Console.WriteLine(e);
                    // throw;
                }
           
            });

  
            foreach (var element in copiedArray)
            {
                Debug.Log(element);
            }

            return copiedArray;
        }
        else
        {
            // deserializedObject = JsonConvert.DeserializeObject(jsonValue, type);
            return JsonConvert.DeserializeObject(jsonValue, type);
        }

        // return deserializedObject;
    }

    [ContextMenu("Apply")]
    public void Apply()
    {


        foreach (var serializedPropertyInfo in serializedPropertyInfos)
        {
            var field = target.GetType().GetField(serializedPropertyInfo.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            var deserializedType =  Type.GetType(serializedPropertyInfo.type);
            var deserializedObject = ConvertJsonStrToObject(deserializedType, serializedPropertyInfo.value);
            field.SetValue(target,deserializedObject);
            // var propertyField = target.GetType().GetField(serializedPropertyInfo.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);           
            // if (propertyField != null)
            // {
            //     
            //     var type =propertyField.GetValue(target).GetType();
            //    
            //     Debug.Log($"{serializedPropertyInfo.name} {serializedPropertyInfo.value}");
            //     Debug.Log(type);
            //     var valueStr = "{" + '"' + serializedPropertyInfo.name + '"' + ":" + serializedPropertyInfo.value + "}";
            //     Debug.Log(valueStr);
            //     JsonUtility.FromJsonOverwrite(valueStr,target);
            //     // var value = JsonUtility.FromJson(, type);
            //     // propertyField.SetValue(target, value);
            // } 
        }
    }

    public void SaveToJsonText()
    {
        json = jObject.ToString();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

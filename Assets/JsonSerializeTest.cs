using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.WSA;


[Serializable]
public class SerializedPropertyInfo
{
    public string name;
    public string type;
    public string value;
}

[CustomEditor(typeof(JsonSerializeTest))]
public class JsonSerializeTestEditor: Editor
{
    private VisualElement serializedPropertyContainer;
    public override VisualElement CreateInspectorGUI()
    {
        var jsonSerializeTest =serializedObject.targetObject as JsonSerializeTest;
        var root = new VisualElement();
        
        root.Add(new PropertyField(serializedObject.FindProperty("target")));
        var json = new PropertyField(serializedObject.FindProperty("json"));
        root.Add(json);
        serializedPropertyContainer = new VisualElement();
        root.Add(serializedPropertyContainer);
        InitializeSerializedValueUI(jsonSerializeTest);
        var serializeButton = new Button();
        serializeButton.text = "Serialize";
        serializeButton.clicked += () =>
        {
            jsonSerializeTest.ToJson();
            InitializeSerializedValueUI(jsonSerializeTest);
        };
        root.Add(serializeButton);
        
        var applyButton = new Button();
        applyButton.text = "Apply";
        applyButton.clicked += jsonSerializeTest.Apply;
        root.Add(applyButton);
        // root.Add(button);
        return root;
    }

    private void InitializeSerializedValueUI(JsonSerializeTest jsonSerializeTest)
    {
        serializedPropertyContainer.Clear();
        foreach (var fSerializedPropertyInfo in jsonSerializeTest.serializedPropertyInfos)
        {
            
            var valueStr =fSerializedPropertyInfo.value;
            var name = fSerializedPropertyInfo.name;
            var typ = Type.GetType(fSerializedPropertyInfo.type);
            var field =typeof(PropertyInitializerUtility)
                .GetMethod("GetBaseField")
                .MakeGenericMethod(Type.GetType(fSerializedPropertyInfo.type))
                .Invoke(null, new object[] {jsonSerializeTest.ConvertJsonStrToObject(typ,valueStr) ,name ,jsonSerializeTest}) as VisualElement;
            if(field !=null)serializedPropertyContainer.Add(field);
            
        }
    }

   
}
[ExecuteAlways]
public class JsonSerializeTest : MonoBehaviour
{

    public MonoBehaviour target;
    public string json;
    public List<SerializedPropertyInfo> serializedPropertyInfos = new List<SerializedPropertyInfo>();
    public JObject jObject;
    public Dictionary<string, SerializedPropertyInfo> serializedPropertyInfoDic = new Dictionary<string, SerializedPropertyInfo>();
    // Start is called before the first frame update
    void Start()
    {
        
    }


    [ContextMenu("Serialize")]
    public void ToJson()
    {
        serializedPropertyInfos.Clear();
        serializedPropertyInfoDic.Clear();
        json = JsonUtility.ToJson(target);
        var fields = target.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        jObject = JObject.Parse(json);

        foreach (var f in fields)
        {
           
            var value = PropertyInitializerUtility.DeepCopy(f.GetValue(target));
            var type = value.GetType();
            var typeName = value.GetType().ToString();
            var valueToStr = JsonConvert.SerializeObject(f.GetValue(target));
          
            serializedPropertyInfos.Add(new SerializedPropertyInfo()
            {
                name = f.Name,
                type = typeName,
                value = valueToStr
            });
            
            serializedPropertyInfoDic.Add(f.Name,serializedPropertyInfos.Last());
        }
        
    }

    public void UpdateSerializedValue(string key, string value)
    {
        serializedPropertyInfos.Find(match:m=>m.name == key).value = value;
    }
    
    public static string[] ParseJsonArrayValue(string value)
    {
        return value.Replace("\n","").Replace("[","").Replace("]","").Replace("\"", "").Split(",");
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
            var parsed = ParseJsonArrayValue(jsonValue);
            var array = Array.CreateInstance(elementType, parsed.Length) as Array;
            for (int i = 0; i < parsed.Length; i++)
            {
                try
                {
                    var result = Convert.ChangeType(parsed[i], elementType);
                    if (result != null)
                    {
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
            var parsed = ParseJsonArrayValue(jsonValue).ToList();//jsonValue.Replace("\n","").Replace("[","").Replace("]","").Replace(" \"", "").Replace("\"", "").Split(",").ToList();
           
            var copiedArray =  (IList) Activator.CreateInstance(type);
            copiedArray.Clear();
            parsed.ForEach((x) =>
            {
                try
                {
                    var result = Convert.ChangeType(x, elementType);
                    if (result != null)
                    {
                        copiedArray.Add(result);
                    }
                }
                catch (Exception e)
                {
                    // Console.WriteLine(e);
                    // throw;
                }
           
            });

            
            return copiedArray;
        }
        else
        {
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
          
        }
    }

    public void SaveToJsonText()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("{");
        foreach (var serializedPropertyInfo in serializedPropertyInfos)
        {
            var deserializedType = Type.GetType(serializedPropertyInfo.type);
            var deserializedObject = ConvertJsonStrToObject(deserializedType, serializedPropertyInfo.value);
            stringBuilder.AppendLine('"' + serializedPropertyInfo.name + '"' + ":" + JsonConvert.SerializeObject(deserializedObject) + ",");
        }
        stringBuilder.AppendLine("}");
        
        json = stringBuilder.ToString();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

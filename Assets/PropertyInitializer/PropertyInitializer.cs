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
    public string value;
}

[CustomEditor(typeof(PropertyInitializer))]
public class PropertyInitializerEditor: Editor
{
    private VisualElement serializedPropertyContainer;
    private PropertyInitializer propertyInitializer;
    public override VisualElement CreateInspectorGUI()
    {
        propertyInitializer =serializedObject.targetObject as PropertyInitializer;
        var root = new VisualElement();

        var objectField = new ObjectField();
        objectField.bindingPath = "target";
        objectField.objectType = typeof(MonoBehaviour);
        objectField.Bind(serializedObject);
        objectField.RegisterValueChangedCallback((e) =>
        {
            
            if (e.newValue != null)
            {
                Debug.Log(objectField.value.GetType());
            }
        });
       
        var jsonField = new TextField();
        jsonField.bindingPath = "json";
        jsonField.Bind(serializedObject);
        jsonField.value = propertyInitializer.json;
        jsonField.RegisterValueChangedCallback((e) =>
        {
            if (e.newValue == null) return;
            var lines = e.newValue.Replace("{","").Replace("}","").Split("\n");
        
            foreach (var line in lines)
            {
                var property = line.Split(":");
                if (property.Length < 2) continue;
                var name = property[0].Replace("\"", "");
                var value = property[1];
                Debug.Log($"{name},{value}");
                // propertyInitializer.UpdateSerializedValue(name,value);
            }
            
            InitializeSerializedValueUI();
        });
  
        // root.Add(new PropertyField(serializedObject.FindProperty("serializedPropertyInfos")));
        serializedPropertyContainer = new VisualElement();
        var serializeButton = new Button();
        serializeButton.text = "Serialize";
        serializeButton.clicked += () =>
        {
            propertyInitializer.ToJson();
            InitializeSerializedValueUI();
        };
        
        var applyButton = new Button();
        applyButton.text = "Apply";
        applyButton.clicked += propertyInitializer.Apply;
        
        root.Add(objectField);
        root.Add(serializeButton);
        root.Add(jsonField);
        root.Add(new PropertyField(serializedObject.FindProperty("serializedPropertyInfos")));
        root.Add(serializedPropertyContainer);
        root.Add(applyButton);
        
        InitializeSerializedValueUI();
        return root;
    }

    private void InitializeSerializedValueUI()
    {
        serializedPropertyContainer.Clear();
        foreach (var fSerializedPropertyInfo in propertyInitializer.serializedPropertyInfos)
        {
            
            var valueStr =fSerializedPropertyInfo.value;
            var name = fSerializedPropertyInfo.name;
            // var typ = Type.GetType($"{fSerializedPropertyInfo.type}, {fSerializedPropertyInfo.assembly}");
            var type    = propertyInitializer.target.GetType().GetField(name).FieldType;
            Debug.Log($"{name} {type} {valueStr}");
            var field =typeof(PropertyInitializerUtility)
                .GetMethod("GetBaseField")
                .MakeGenericMethod(type)
                .Invoke(null, new object[] {propertyInitializer.ConvertJsonStrToObject(type,valueStr) ,name ,propertyInitializer}) as VisualElement;
            if(field !=null)serializedPropertyContainer.Add(field);
            
        }
    }

   
}
[ExecuteAlways]
public class PropertyInitializer : MonoBehaviour
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
        Debug.Log(json);
        var fields = target.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        jObject = JObject.Parse(json);
        
        foreach (var f in fields)
        {
            object value;
            string valueToStr;
            Debug.Log(f);
            if (!PropertyInitializerUtility.IsSerializable(f))
            {
                value = f.GetValue(target);
                valueToStr = JsonUtility.ToJson(value);
                Debug.Log(valueToStr);
            }
            else
            {
                value = f.GetValue(target);
                valueToStr = JsonConvert.SerializeObject(value);
                Debug.Log(valueToStr);     
            }
           
          
            serializedPropertyInfos.Add(new SerializedPropertyInfo()
            {
                name = f.Name,
                value = valueToStr,
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
        if (type == typeof(Vector2))
        {
            var valueObject = new Vector2();
            var jObject = JObject.Parse(jsonValue);
            
            valueObject.x = jObject["x"].ToObject<float>();
            valueObject.y = jObject["y"].ToObject<float>();
            return valueObject;
        }
        else if (type == typeof(Vector2Int))
        {
            var valueObject = new Vector2Int();
            var jObject = JObject.Parse(jsonValue);
            
            valueObject.x = jObject["x"].ToObject<int>();
            valueObject.y = jObject["y"].ToObject<int>();
            return valueObject;
        }
        else if (type == typeof(Vector3))
        {
            var valueObject = new Vector3();
            var jObject = JObject.Parse(jsonValue);
            Debug.Log(jObject);
            valueObject.x = jObject["x"].ToObject<float>();
            valueObject.y = jObject["y"].ToObject<float>();
            valueObject.z = jObject["z"].ToObject<float>();
            return valueObject;
        }
        else if (type == typeof(Vector3Int))
        {
            var valueObject = new Vector3Int();
            var jObject = JObject.Parse(jsonValue);

            valueObject.x = jObject["x"].ToObject<int>();
            valueObject.y = jObject["y"].ToObject<int>();
            valueObject.z = jObject["z"].ToObject<int>();
            return valueObject;
        }
        else if (type == typeof(Vector4))
        {
            var valueObject = new Vector4();
            var jObject = JObject.Parse(jsonValue);

            valueObject.x = jObject["x"].ToObject<float>();
            valueObject.y = jObject["y"].ToObject<float>();
            valueObject.z = jObject["z"].ToObject<float>();
            valueObject.w = jObject["w"].ToObject<float>();
            return valueObject;
        }
        else if (type == typeof(Quaternion))
        {
            var valueObject = new Quaternion();
            var jObject = JObject.Parse(jsonValue);

            valueObject.x = jObject["x"].ToObject<float>();
            valueObject.y = jObject["y"].ToObject<float>();
            valueObject.z = jObject["z"].ToObject<float>();
            valueObject.w = jObject["w"].ToObject<float>();
            return valueObject;
        }
        else if (type == typeof(Color))
        {
            var valueObject = new Color();
            var jObject = JObject.Parse(jsonValue);

            valueObject.r = jObject["r"].ToObject<float>();
            valueObject.g = jObject["g"].ToObject<float>();
            valueObject.b = jObject["b"].ToObject<float>();
            valueObject.a = jObject["a"].ToObject<float>();
            return valueObject;
        }
        else if (type == typeof(Color32))
        {
            var valueObject = new Color32();
            var jObject = JObject.Parse(jsonValue);

            valueObject.r = jObject["r"].ToObject<byte>();
            valueObject.g = jObject["g"].ToObject<byte>();
            valueObject.b = jObject["b"].ToObject<byte>();
            valueObject.a = jObject["a"].ToObject<byte>();
            return valueObject;
        }
        else if (type == typeof(Rect))
        {
            var valueObject = new Rect();
            var jObject = JObject.Parse(jsonValue);

            valueObject.x = jObject["x"].ToObject<float>();
            valueObject.y = jObject["y"].ToObject<float>();
            valueObject.width = jObject["width"].ToObject<float>();
            valueObject.height = jObject["height"].ToObject<float>();
            return valueObject;
        }
        else if(type.IsArray)
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
        }else if (type.IsGenericType &&  type.GetGenericTypeDefinition() == typeof(List<>))
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
        else if (type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            if (type == typeof(string) || type == typeof(System.String))
            {
                return jsonValue.Replace("\"", "");
            }
            else
            {
                var deserializedObject = Activator.CreateInstance(type);
                deserializedObject = Convert.ChangeType(deserializedObject, type);
                return deserializedObject;     
            }
        }
        else if (type.IsClass || type.IsValueType)
        {
            return JsonConvert.DeserializeObject(jsonValue, type);
        }
        else
        {
            return JsonConvert.DeserializeObject(jsonValue, type);
        }
        
    }

    [ContextMenu("Apply")]
    public void Apply()
    {


        foreach (var serializedPropertyInfo in serializedPropertyInfos)
        {
            var field = target.GetType().GetField(serializedPropertyInfo.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            var deserializedType =  target.GetType().GetField(serializedPropertyInfo.name).FieldType;
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
            stringBuilder.AppendLine('"' + serializedPropertyInfo.name + '"' + ":" + serializedPropertyInfo.value + ",");
        }
        stringBuilder.AppendLine("}");
        
        json = stringBuilder.ToString();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

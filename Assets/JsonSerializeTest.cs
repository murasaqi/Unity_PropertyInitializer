using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

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
        root.Add(json);

        var fields = jsonSerializeTest.target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        jsonSerializeTest.jObject = JObject.Parse(jsonSerializeTest.json);
        var serializedTargetObject = new SerializedObject(serializedObject.FindProperty("target").objectReferenceValue);
        foreach (var f in fields)
        {
           
            // Debug.Log(f.Name);
            if (jsonSerializeTest.jObject.ContainsKey(f.Name))
            {
                var property = serializedTargetObject.FindProperty(f.Name).Copy();
                var propertyField = new PropertyField(property);
                VisualElement field = null;
                
                // TODO : JsonのKeyからプロパティを算出してそのプロパティの型にキャストしたJsonのValueで初期化する
                // https://tech-and-investment.com/json5-parse/

                field =typeof(PropertyInitializerUtility)
                    .GetMethod("GetBaseField")
                    .MakeGenericMethod(f.FieldType)
                    .Invoke(null, new object[] { f.GetValue(serializedTargetObject.targetObject),f.Name ,jsonSerializeTest}) as VisualElement;
                
                if(field !=null)root.Add(field);
            }
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
    public JObject jObject;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void OnValidate()
    {
        throw new NotImplementedException();
    }
    [ContextMenu("Serialize")]
    public void ToJson()
    {
        json = JsonUtility.ToJson(target);
        var deserialized = JsonConvert.DeserializeObject(json);
        Debug.Log(deserialized);
        var fields = target.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var f in fields)
        {
            Debug.Log(f.GetValue(target));
        }
        jObject = JObject.Parse(json);
        

    } 

    [ContextMenu("Apply")]
    public void Apply()
    {
        // JsonPropertyCollection properties = new JsonPropertyCollection(target.GetType());
        foreach (var f in jObject)
        {
            
            
            var propertyField = target.GetType().GetField(f.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);           
            if (propertyField != null)
            {
                
                var type =propertyField.GetValue(target).GetType();
               
                Debug.Log($"{f.Key} {f.Value}");
                Debug.Log(type);
                var valueStr = "{" + '"' + f.Key + '"' + ":" + f.Value + "}";
                Debug.Log(valueStr);
                JsonUtility.FromJsonOverwrite(valueStr,target);
                // var value = JsonUtility.FromJson(, type);
                // propertyField.SetValue(target, value);
            } 

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

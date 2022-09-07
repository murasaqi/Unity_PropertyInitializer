using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

using UnityEngine.UIElements;

#if UNITY_EDITOR
 
public static class FieldInfoExtension
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
}

[CustomEditor(typeof(PropertyInitializer))]
public class ValiableInitializerEditor: Editor
{

    // public Dictionary<int,string> serializedPropertyIndex = new Dictionary<int,string>();
    // public List<VisualElement> fields = new List<VisualElement>();
    public Dictionary<string,VisualElement> uiPropertyPairs = new Dictionary<string,VisualElement>();
   
    public SerializedObject copyFrom;
    public SerializedObject cloneTarget;

    public PopupField<string> propertyPopupField;
    public VisualElement addPropertyContainer;
    public Button addButton;
    public Button applyButton;
    public VisualElement propertyContainer;
    public PropertyInitializer propertyInitializer;
    
    
    
    public override VisualElement   CreateInspectorGUI()
    {
        var root = new VisualElement();
        root.Add(new PropertyField(serializedObject.FindProperty("propertyNameList")));
        root.Add(new PropertyField(serializedObject.FindProperty("visiblePropertyNameList")));
        root.Add(new PropertyField(serializedObject.FindProperty("copyFieldList")));
        
        propertyPopupField = new PopupField<string>();
        addPropertyContainer = new VisualElement();
        propertyContainer = new VisualElement();
        propertyInitializer = serializedObject.targetObject as PropertyInitializer;
        addButton = new Button();
        addButton.text = "Add";
        addButton.clicked += () =>
        {
            
            propertyInitializer.MoveCopyList(propertyPopupField.value);
            propertyContainer.Add(uiPropertyPairs[propertyPopupField.value]);
                
        };
        root.Add(addPropertyContainer);
        addPropertyContainer.Add(propertyPopupField);
        addPropertyContainer.Add(addButton);
        var clearButton = new Button(() =>
        {
            propertyInitializer.Clear();
            propertyContainer.Clear();
            propertyInitializer.cloneTarget = Instantiate(propertyInitializer.targetObject);
            serializedObject.ApplyModifiedProperties();
            
            InitPopUp();
        });
        clearButton.text = "Clear";
        addPropertyContainer.Add(clearButton);
        addPropertyContainer.style.flexDirection = FlexDirection.Row;
        applyButton = new Button();
        applyButton.text = "Apply";

        var targetObjectField = new ObjectField("Target Object");
        targetObjectField.objectType = typeof(MonoBehaviour);
        targetObjectField.Bind(serializedObject);
        targetObjectField.bindingPath = "targetObject";
        targetObjectField.RegisterValueChangedCallback((e) =>
        {
            if (propertyInitializer.targetObject  != null &&  e.newValue != e.previousValue)
            {
                propertyInitializer.Clear();
                InitPopUp();
            }
        });

        if (propertyInitializer.targetObject != null)
        {
            InitPopUp();
        }
        root.Add(targetObjectField);
        
        root.Add(propertyContainer);
        applyButton = new Button();
        applyButton.text = "Apply";
        root.Add(applyButton);
        
            
        return root;
    }
    

    private void InitPopUp()
    {
        propertyContainer.Clear();
        uiPropertyPairs.Clear();
        propertyInitializer.GetFields();
        cloneTarget = new SerializedObject(propertyInitializer.cloneTarget);
        var iterator = cloneTarget.GetIterator ();
        
        while (iterator.NextVisible(true)){
                
          
            if (propertyInitializer.propertyNameList.Contains(iterator.propertyPath))
            {
          
                
                var field = new PropertyField(iterator);
                field.Bind(cloneTarget);
                field.bindingPath = iterator.propertyPath;   
                
                Debug.Log(iterator.propertyPath);
                uiPropertyPairs.Add(iterator.propertyPath,field);
        
                if (propertyInitializer.visiblePropertyNameList.Contains(iterator.propertyPath))
                {
                    propertyContainer.Add(field);
                }
            }

        }
        
        Debug.Log(uiPropertyPairs.Count);

        propertyPopupField.choices = propertyInitializer.propertyNameList;
        propertyPopupField.index = 0;
        applyButton.clicked += () =>
        {
            propertyInitializer.ApplyPropertyValue();

        };    
    }
   
}
#endif

[Serializable] 
public class CopyFieldInfo
{
    public object copyFromObject;
    public object copyToObject;
    public FieldInfo copyFromFieldInfo;
    public FieldInfo copyToFieldInfo;
    
    public CopyFieldInfo (object copyFromObject, object copyToObject,string propertyPath)
    {
        var copyFieldInfo = copyToObject.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var originalFieldInfo = copyFromObject.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if(copyFieldInfo ==null || originalFieldInfo == null)
        {
            Debug.LogError("FieldInfo is null");
            return;
        }
        this.copyFromObject = copyFromObject;
        this.copyToObject = copyToObject;
        this.copyFromFieldInfo = copyFieldInfo;
        this.copyToFieldInfo = originalFieldInfo;
    }
    
    public void CopyValueFromTo()
    {
        var value = copyFromFieldInfo.GetValue(copyFromObject);
        var valueType = value.GetType();
        if (valueType.IsArray)
        {
            var copyArray = value as Array;
            var newArray = Array.CreateInstance(copyArray.GetType().GetElementType(),copyArray.Length);
            for (int i = 0; i < copyArray.Length; i++)
            {
                newArray.SetValue(copyArray.GetValue(i),i);
            }
            copyToFieldInfo.SetValue(copyToObject,newArray);
        }else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var copyList = value as IList;
            var newList = (IList)Activator.CreateInstance(valueType);
            foreach (var item in copyList)
            {
                newList.Add(item);
            }
            copyToFieldInfo.SetValue(copyToObject,newList);
        }else
        {
            copyToFieldInfo.SetValue(copyToObject,value);
        }
       
    }
    
    public void CopyValueToFrom()
    {
        copyFromFieldInfo.SetValue(copyFromObject,copyToFieldInfo.GetValue(copyToObject));
    }

    
 
}
[ExecuteAlways]
public class PropertyInitializer : MonoBehaviour
{
    
    public MonoBehaviour targetObject;
    [SerializeField]public MonoBehaviour cloneTarget;
    public List<string> propertyNameList = new List<string>();
    public List<string> visiblePropertyNameList = new List<string>();
    public Dictionary<string,CopyFieldInfo> allEditableFieldInfoDictionary = new Dictionary<string, CopyFieldInfo>();
    public List<CopyFieldInfo> copyFieldList = new List<CopyFieldInfo>();
    void Start()
    {
        Init();
    }

    public void Init()
    {
        GetFields();

        foreach (var propertyName in visiblePropertyNameList)
        {
            MoveCopyList(propertyName);
        }
    }

    public void GetFields()
    {
        if(targetObject == null) return;
        CloneTarget();
        propertyNameList.Clear();
        allEditableFieldInfoDictionary.Clear();
        var fields = targetObject.GetType().GetFields(BindingFlags.Public| BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.IsPublic || FieldInfoExtension.IsSerializable(field))
            {
                propertyNameList.Add(field.Name);
                allEditableFieldInfoDictionary.Add(field.Name,new CopyFieldInfo(cloneTarget,targetObject,field.Name));
            }
        }
    }
    
    public void CloneTarget()
    {
        if(targetObject != null)
        {
            cloneTarget = Instantiate(targetObject);
        }

    }

    public void Clear()
    {
        propertyNameList.Clear();
        allEditableFieldInfoDictionary.Clear();
        copyFieldList.Clear();
        visiblePropertyNameList.Clear();
    }
    
    public void ClearPropertyInfos()
    {
        allEditableFieldInfoDictionary.Clear();
    }
    
    public void MoveCopyList(string key)
    {
        if(!allEditableFieldInfoDictionary.ContainsKey(key)) return;
        if(!visiblePropertyNameList.Contains(key))visiblePropertyNameList.Add(key);
        var copyFieldInfo = allEditableFieldInfoDictionary[key];
        if (!copyFieldList.Contains(copyFieldInfo))
        {
            copyFieldList.Add(copyFieldInfo);
        }
    }

    public void ApplyPropertyValue()
    {
        foreach (CopyFieldInfo copyFieldInfo in copyFieldList)
        {
            copyFieldInfo.CopyValueFromTo();
            
        }
    }

 
    // Update is called once per frame
    void Update()
    {
        
    }
}

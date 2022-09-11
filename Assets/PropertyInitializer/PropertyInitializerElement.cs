using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;




[AddComponentMenu("")]
// [CanEditMultipleObjects]
public class PropertyInitializerElement: MonoBehaviour
{
    [SerializeField]public Transform parent;
    [SerializeField]public MonoBehaviour targetObject;
    [SerializeField]public MonoBehaviour cloneObject;
    [SerializeField]public List<string> serializedPropertyNameList = new List<string>();
    [SerializeField]public List<string> initializePropertyNameList = new List<string>();
    [SerializeField]public Dictionary<string,CopyFieldInfo> serializedFieldInfoPair = new Dictionary<string, CopyFieldInfo>();
    [SerializeField]public List<CopyFieldInfo> initializeFieldList = new List<CopyFieldInfo>();

    public string json;

    public void Aplly(Transform parent, MonoBehaviour targetObject, MonoBehaviour cloneObject)
    {
        this.parent = parent;
        this.targetObject = targetObject;
        this.cloneObject = cloneObject;
        
        Init();
    }

     public MonoBehaviour GetCloneObject()
    {
        // if(cloneObject == null && targetObject!=null)
        // {
        //     CloneTarget();
        // }
        return cloneObject;
    }
    public void Init()
    {
        TryGetSerializedFields();

        json = JsonUtility.ToJson(targetObject);

        foreach (var propertyName in initializePropertyNameList)
        {
            MoveCopyList(propertyName);
        }
        
        Debug.Log(targetObject);
        Debug.Log(cloneObject);
    }

    public void TryGetSerializedFields()
    {
        if(targetObject == null) return;
        serializedPropertyNameList.Clear();
        serializedFieldInfoPair.Clear();
        initializeFieldList.Clear();
        var fields = targetObject.GetType().GetFields(BindingFlags.Public| BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.IsPublic || PropertyInitializerUtility.IsSerializable(field))
            {
                var copyFieldInfo = new CopyFieldInfo(cloneObject, targetObject, field.Name);
                serializedPropertyNameList.Add(field.Name);
                serializedFieldInfoPair.Add(field.Name,copyFieldInfo);
                initializeFieldList.Add(copyFieldInfo);
            }
        }
    }
    
    public void OnDestroy()
    {
        Clear();
        if(cloneObject != null)DestroyImmediate(cloneObject.gameObject);
    }
    // public void CloneTarget()
    // {
    //     if(targetObject != null)
    //     {
    //         if(cloneObject != null)DestroyImmediate(cloneObject.gameObject);
    //         cloneObject = Instantiate(targetObject);
    //         cloneObject.transform.SetParent(transform);
    //         initializePropertyNameList.Clear();
    //     }else
    //     {
    //         if(cloneObject != null) DestroyImmediate(cloneObject);
    //         cloneObject = null;
    //     }
    //
    // }
    
    

    public void Clear()
    {
        if(serializedPropertyNameList != null)serializedPropertyNameList.Clear();
        serializedFieldInfoPair.Clear();
        initializeFieldList.Clear();
        initializePropertyNameList.Clear();
    }
    public void DestroyClone()
    {
        if(cloneObject != null) DestroyImmediate(cloneObject.gameObject);
        cloneObject = null;
    }
    public void ClearPropertyInfos()
    {
        serializedFieldInfoPair.Clear();
    }

    // public void MoveCopyList()
    // {
    //     foreach (var key in initializePropertyNameList)
    //     {
    //         MoveCopyList(key);
    //     }
    // }
    public void MoveCopyList(string key)
    {
        if(!initializePropertyNameList.Contains(key))initializePropertyNameList.Add(key);

        // if (serializedFieldInfoPair.ContainsKey(key))
        // {
        //     var copyFieldInfo = serializedFieldInfoPair[key];
        //     initializeFieldList.Add(copyFieldInfo);
        //     
        // }
        // initializeFieldList.DistinctBy(x => x.serializedValues.name);
    }

    public void ApplyPropertyValue()
    {

        // JsonUtility.FromJsonOverwrite(json, targetObject);
        // AssetDatabase.SaveAssets();
        foreach (var initialize in initializeFieldList)
        {
            initialize.CopyValueFromTo();
            
        }
    }

 
    
}
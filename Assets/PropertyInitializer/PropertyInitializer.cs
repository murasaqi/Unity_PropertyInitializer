using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;


[Serializable] 
public class CopyFieldInfo
{
    public object referenceObject;
    public object applyTargetObject;
    public FieldInfo referenceFieldInfo;
    public FieldInfo applyTargetFieldInfo;
    
    public PropertyInitializerSerializedValue serializedValues = new PropertyInitializerSerializedValue();

    public CopyFieldInfo (object referenceObject, object applyTargetObject,string propertyPath)
    {
        var referenceFieldInfo = referenceObject.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var applyTargetFieldInfo = applyTargetObject.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
       
      
        if(referenceFieldInfo ==null || applyTargetFieldInfo == null)
        {
            Debug.LogError("FieldInfo is null");
            return;
        }
        
        this.referenceObject = referenceObject;
        this.applyTargetObject = applyTargetObject;
        this.referenceFieldInfo = referenceFieldInfo;
        this.applyTargetFieldInfo = applyTargetFieldInfo;
        serializedValues = new PropertyInitializerSerializedValue()
        {
            name = referenceFieldInfo.Name,
            type = referenceFieldInfo.FieldType.ToString(),
            value = JsonUtility.ToJson(this.referenceFieldInfo.GetValue(this.referenceObject))
        };

        
    }


    public object StrListToValue(Type type, List<string> values)
    {
        if(type.IsArray)
        {
            var elementType = type.GetElementType();
            var array = Array.CreateInstance(elementType, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                array.SetValue(Convert.ChangeType(values[i], elementType), i);
            }
            return array;
        }
        else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(type);
            foreach (var value in values)
            {
                list.Add(Convert.ChangeType(value, elementType));
            }
            return list;
        }
        else
        {
            return Convert.ChangeType(values[0], type);
        }
    }

    public string ValueToJson(object value)
    {
        return JsonUtility.ToJson(value);
        var valueType = value.GetType();
        var result = new List<string>();
        if (valueType.IsArray)
        {
            var copyArray = value as Array;
            for (int i = 0; i < copyArray.Length; i++)
            {
                result.Add(copyArray.GetValue(i).ToString());
            }
          
        }else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var copyList = value as IList;
            foreach (var item in copyList)
            {
                result.Add(item.ToString());
            }
        }else
        {
            result.Add(value.ToString());
        }

        // return result;
    }
   
    public object GetCopyValue(FieldInfo fieldInfo, object target)
    {
        var value =  PropertyInitializerUtility.DeepCopy(fieldInfo.GetValue(target));
        var valueType = value.GetType();
        //
        // if (valueType.IsArray)
        // {
        //     var copyArray = value as Array;
        //     var newArray = Array.CreateInstance(copyArray.GetType().GetElementType(),copyArray.Length);
        //     for (int i = 0; i < copyArray.Length; i++)
        //     {
        //         newArray.SetValue(copyArray.GetValue(i),i);
        //     }
        //
        //     return newArray;
        //
        // }else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
        // {
        //     var copyList = value as IList;
        //     var newList = (IList)Activator.CreateInstance(valueType);
        //     foreach (var item in copyList)
        //     {
        //         newList.Add(item);
        //     }
        //     return newList;
        // }else
        // {
        //     return value;
        // }

        return value;
    }

    public void CopyValueFromTo()
    {
        var type = Type.GetType(serializedValues.type);
        if (type == null)
        {
            type =Assembly.Load("UnityEngine.dll").GetType(serializedValues.type);
        }

        var value = JsonUtility.FromJson(serializedValues.value, type);
        Debug.Log(value);
        JsonUtility.FromJsonOverwrite(serializedValues.value,applyTargetFieldInfo);
        // if(type != null)applyTargetFieldInfo.SetValue(applyTargetObject, JsonUtility.FromJson(serializedValues.value,type));
        // var value = GetCopyValue(referenceFieldInfo,referenceObject);
        // var applyTargetValue = GetCopyValue(applyTargetFieldInfo,applyTargetObject);
        // Debug.Log($"ref: {value},apply: {applyTargetValue}");
        // Debug.Log($"Befor: {applyTargetFieldInfo.GetValue(applyTargetObject)}");
        // applyTargetFieldInfo.SetValue(applyTargetObject,value);
        //
        // Debug.Log($"After: {applyTargetFieldInfo.GetValue(applyTargetObject)}");

    }
    
    // public void CopyValueToFrom()
    // {
    //     referenceFieldInfo.SetValue(referenceObject,applyTargetFieldInfo.GetValue(applyTargetObject));
    // }

}


[ExecuteAlways]
public class PropertyInitializer : MonoBehaviour
{
    [SerializeField]public List<PropertyInitializerElement> propertyInitializerElements = new List<PropertyInitializerElement>();

    public List<MonoBehaviour> targetObjects = new List<MonoBehaviour>();
    
    // public List<PropertyInitializerSerializedValue> serializedValues = new List<PropertyInitializerSerializedValue>();
    void Start()
    {
        Init();
    }

    public void OnValidate()
    {
        // Init();
    }


    public void AddTargetObject(MonoBehaviour targetObject)
    {
        if (targetObjects.Contains(targetObject))
        {
            return;
        }
        targetObjects.Add(targetObject);
        Init();
    }

    
    public void RemoveTargetObject(MonoBehaviour targetObject)
    {
        if (!targetObjects.Contains(targetObject))
        {
            return;
        }
        targetObjects.Remove(targetObject);
        Init();
    }
  
    
    
    [ContextMenu("Init")]
    public void Init()
    {
        if(targetObjects.Count == 0) return;
        propertyInitializerElements = GetComponents<PropertyInitializerElement>().ToList();


        propertyInitializerElements.DistinctBy(x => x.targetObject);

        foreach (var propertyInitializer in propertyInitializerElements)
        {
            propertyInitializer.Init();
            
        }

        
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            var find = propertyInitializerElements.FindAll(x => x.cloneObject.transform == child);
            if (find.Count == 0)
            {
                DestroyImmediate(child.gameObject);
            }

        }
    
        foreach (var propertyInitializerElement in propertyInitializerElements)
        {
            if (!targetObjects.Contains(propertyInitializerElement.targetObject))
            {
                propertyInitializerElement.DestroyClone();
                DestroyImmediate(propertyInitializerElement);
            }

            if (propertyInitializerElement.cloneObject != null)
            {
                // propertyInitializerElement.cloneObject.hideFlags 
            }
        }
        
        
        foreach (var targetObject in targetObjects)
        {
           
            // if(targetObject == null) continue;
            var isContain = propertyInitializerElements.FindAll(x => x.targetObject == targetObject);
            
            if(isContain.Count == 0 && targetObject != null)
            {
                AddPropertyInitializerElement(targetObject);
            }

            if (isContain.Count == 1 && isContain.First().cloneObject == null)
            {
                isContain.First().cloneObject = GetClone(targetObject);
            }

            if (isContain.Count > 1)
            {
                for (int i = 1; i < isContain.Count; i++)
                {
                    DestroyImmediate(isContain[i]);
                }
            }

        }
        
        
        
        // serializedValues.Clear();
        //
        // foreach(var propertyInitializerElement in propertyInitializerElements)
        // {
        //     foreach (var v in propertyInitializerElement.serializedFieldInfoPair.Values)
        //     {
        //         var value = v.GetCopyValue(v.referenceFieldInfo, v.referenceObject);
        //
        //         var item = new PropertyInitializerSerializedValue()
        //         {
        //             name = v.referenceFieldInfo.Name,
        //             type = value.GetType().ToString(),
        //             value = v.ValueToStringList(value)
        //         };
        //         serializedValues.Add(item);
        //     }
        // }
    }

    public MonoBehaviour GetClone(MonoBehaviour target)
    {
        var clone = Instantiate(target);
        clone.transform.SetParent(transform);

        return clone;
    }
    public void AddPropertyInitializerElement(MonoBehaviour target)
    {
      
        var clone = GetClone(target);

        foreach (var mono in clone.GetComponents<MonoBehaviour>())
        {
            mono.enabled = false;
        }
        var element = gameObject.AddComponent<PropertyInitializerElement>();
        element.name = $"{target.name} PropertyInitializerElement";
        clone.hideFlags = HideFlags.HideInHierarchy;
        clone.hideFlags = HideFlags.HideInInspector;
        element.Aplly(transform,target,clone);
        propertyInitializerElements.Add(element);
    }
    [ContextMenu("Debug")]
    public void DoDebug()
    {
        foreach (var element in propertyInitializerElements)
        {
            Debug.Log($"{element.targetObject},{element.cloneObject}");
            // Debug.Log($"{element.targetObject},{element.cloneObject}");
        }
    }
    
    [ContextMenu("Destroy All")]
    public void DestroyAll()
    {
        foreach (var element in propertyInitializerElements)
        {
            DestroyImmediate(element.gameObject);
        }
        propertyInitializerElements.Clear();
        
        var addedPropertyInitializerElements = GetComponents<PropertyInitializerElement>();
        
        foreach (var addedPropertyInitializerElement in addedPropertyInitializerElements)
        {
            DestroyImmediate(addedPropertyInitializerElement);
        }
    }   

   

    // Update is called once per frame
    void Update()
    {
        
    }
}

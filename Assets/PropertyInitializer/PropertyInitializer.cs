using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;


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
    [SerializeField]public List<PropertyInitializerElement> propertyInitializerElements = new List<PropertyInitializerElement>();

    public List<MonoBehaviour> targetObjects = new List<MonoBehaviour>();
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

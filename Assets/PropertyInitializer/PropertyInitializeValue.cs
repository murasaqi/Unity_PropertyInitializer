using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[Serializable]
public struct PropertyInitializerSerializedValue
{
    public string name;
    public string type;
    public string value;
}


[CreateAssetMenu(fileName = "PropertyInitializeValueAsset", menuName = "ScriptableObjects/PropertyInitializeValueAsset", order = 1)]

public class PropertyInitializeValue : ScriptableObject
{
    // public List<SerializedValue> serializedValues = new List<SerializedValue>();

    public PropertyInitializeValue()
    {
        
        // objects.Add();
    }
}

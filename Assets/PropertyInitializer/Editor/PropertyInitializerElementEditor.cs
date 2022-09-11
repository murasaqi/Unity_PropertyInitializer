using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


public class PropertyInitializerUI
{
    private Dictionary<string,VisualElement> uiPropertyPairs = new Dictionary<string,VisualElement>();
    private Dictionary<string,string> propertyPathDisplayNamePairs = new Dictionary<string,string>();
    private SerializedObject serializedElement;
    private SerializedObject cloneTarget;

    private PopupField<string> propertyPopupField;
    private VisualElement addPropertyContainer;
    private Button addButton;
    private Button applyButton;
    private VisualElement propertyContainer;
    private PropertyInitializerElement propertyInitializerElement;
    private ObjectField targetObjectField;
    private Button clearButton;
    private int currentSelectIndex;
    
    private void InitUIElements(VisualElement root, SerializedObject serializedObject)
    {
       
        addPropertyContainer = new VisualElement();
        propertyContainer = new VisualElement();
        propertyPopupField = new PopupField<string>();
        addButton = new Button();
        targetObjectField = new ObjectField("Target Object");
        applyButton = new Button();  
        clearButton = new Button();
        
        addPropertyContainer.Add(propertyPopupField);
        addPropertyContainer.Add(addButton);
        addPropertyContainer.Add(clearButton);
        root.Add(new Label(serializedObject.targetObject.name));
        root.Add(new PropertyField(serializedObject.FindProperty("propertyNameList")));
        root.Add(new PropertyField(serializedObject.FindProperty("visiblePropertyNameList")));
        root.Add(new PropertyField(serializedObject.FindProperty("copyFieldList")));
        root.Add(targetObjectField);
        
        
        var objectField = new ObjectField("Clone Object");
        objectField.objectType = typeof(MonoBehaviour);
        objectField.Bind(serializedObject);
        objectField.bindingPath = "cloneObject";
        root.Add(objectField);
        root.Add(addPropertyContainer);
        root.Add(propertyContainer);
        root.Add(applyButton);
        applyButton.text = "Apply";
        addButton.text = "Add";
        targetObjectField.objectType = typeof(MonoBehaviour);
        targetObjectField.Bind(serializedObject);
        targetObjectField.bindingPath = "targetObject";
        
        clearButton.text = "Clear"; 
        addPropertyContainer.style.flexDirection = FlexDirection.Row;
        
    }
    
    public  VisualElement  CreateInspectorGUI(SerializedObject serializedObject)
    {
        propertyInitializerElement = serializedObject.targetObject as PropertyInitializerElement;
        var root = new VisualElement();
        InitUIElements(root, serializedObject);
       
        
        addButton.clicked += () =>
        {
            var ui = GetPropertyUI(propertyPopupField.value);
            Debug.Log(ui);
            if(ui == null) return;
            
            propertyInitializerElement.MoveCopyList(propertyPopupField.value);
            propertyContainer.Add(ui);
            Debug.Log(propertyContainer.childCount);
                
        };
     
        clearButton.clicked+= () =>
        {
            propertyInitializerElement.Clear();
            InitPopUp();
        };

        
        targetObjectField.RegisterValueChangedCallback((e) =>
        {
            if (propertyInitializerElement.targetObject  != null &&  e.newValue != e.previousValue)
            {
                propertyInitializerElement.Clear();
                InitPopUp();
            }
        });

        if (propertyInitializerElement.targetObject != null)
        {
            InitPopUp();
        }
       
        applyButton.clicked += () =>
        {
            propertyInitializerElement.ApplyPropertyValue();
        };   
            
        return root;
    }
    
    public VisualElement GetPropertyUI(string propertyPath)
    {
        
        if (propertyPathDisplayNamePairs.ContainsKey(propertyPath))
        {
            var key = propertyPathDisplayNamePairs[propertyPath];
            if (uiPropertyPairs.ContainsKey(key))
            {
                return uiPropertyPairs[key];
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }
    
    private void InitPopUp()
    {
        propertyContainer.Clear();
        uiPropertyPairs.Clear();
        propertyInitializerElement.TryGetSerializedFields();
        propertyPathDisplayNamePairs.Clear();
        var cloneObject = propertyInitializerElement.GetCloneObject();
        if(cloneObject == null) return;
        cloneTarget = new SerializedObject(cloneObject);
        
        
        var iterator = cloneTarget.GetIterator ();
        while (iterator.NextVisible(true)){ 
            if (propertyInitializerElement.serializedPropertyNameList.Contains(iterator.propertyPath))
            {
                
                propertyPathDisplayNamePairs.Add(iterator.propertyPath,iterator.displayName);
                var field = new PropertyField(iterator);
                field.Bind(cloneTarget);
                field.bindingPath = iterator.propertyPath;   
                
                uiPropertyPairs.Add(iterator.displayName,field);
        
                if (propertyInitializerElement.initializePropertyNameList.Contains(iterator.propertyPath))
                {
                    propertyContainer.Add(field);
                }
            }

        }
        

        propertyPopupField.choices = propertyInitializerElement.serializedPropertyNameList;
        propertyPopupField.index = currentSelectIndex;
        propertyPopupField.RegisterValueChangedCallback((e) =>
        {
            currentSelectIndex = propertyPopupField.index;
        });
    }
}


[CustomEditor(typeof(PropertyInitializerElement))]
public class PropertyInitializerElementEditor: Editor
{

    private PropertyInitializerElement propertyInitializer;
    public override VisualElement   CreateInspectorGUI()
    {
        propertyInitializer = serializedObject.targetObject as PropertyInitializerElement;
        
        var root = new VisualElement();
        // var targetObjectField = new PropertyField(serializedObject.FindProperty("targetObjects"));
        // targetObjectField.Bind(serializedObject);
        // targetObjectField.bindingPath = "targetObjects";
        // root.Add(targetObjectField);
        
        var ui = new PropertyInitializerUI();
        var i = 0;
        propertyInitializer.Init();
        
        root.Add(ui.CreateInspectorGUI(serializedObject));    
        

        return root;
    }
    
   
}


[CustomEditor(typeof(PropertyInitializer))]
[CanEditMultipleObjects]
public class PropertyInitializerEditor: Editor
{

    private PropertyInitializer propertyInitializer;
    
    private VisualElement targetObjectContainer;
    private VisualElement buttonContainer;

    private List<VisualElement> targetObjectFields = new List<VisualElement>();
    public override VisualElement   CreateInspectorGUI()
    {
         propertyInitializer = serializedObject.targetObject as PropertyInitializer;
         targetObjectContainer = new VisualElement();
         buttonContainer = new VisualElement();
         InitTargetObjectField();
         buttonContainer.style.flexDirection = FlexDirection.Row;
         buttonContainer.style.justifyContent = Justify.FlexEnd;
         buttonContainer.style.marginTop = 4;
         var addButton = new Button();
         addButton.text = "+";
         addButton.clicked += () =>
         {
             propertyInitializer.targetObjects.Add(null);
             InitTargetObjectField();
             
             propertyInitializer.Init();
         };
         
         
         var removeButton = new Button();
         removeButton.text = "-";
         removeButton.clickable.clicked += () =>
         {
             foreach (var targetObjectField in targetObjectFields)
             {

                 if (targetObjectField.Q<Toggle>().value)
                 {
                     propertyInitializer.targetObjects.Remove(targetObjectField.Q<ObjectField>().value as MonoBehaviour);
                 }
                 
             }
             InitTargetObjectField();
             propertyInitializer.Init();
         };
         buttonContainer.Add(addButton);
         buttonContainer.Add(removeButton);

           
         var root = new VisualElement();
         root.Add(targetObjectContainer);
         root.Add(buttonContainer);
         
         
         root.Add(new PropertyField(serializedObject.FindProperty("targetObjects")));
         root.Add(new PropertyField(serializedObject.FindProperty("propertyInitializerElements")));
         

         
         return root;
     }

    public void InitTargetObjectField()
    {
        targetObjectContainer.Clear();
        foreach (var targetObject in propertyInitializer.targetObjects)
        {
            targetObjectContainer.Add(CreateTargetObjectField(targetObject));
        }
    }

    public VisualElement CreateTargetObjectField(MonoBehaviour targetObject)
    {
        var container = new VisualElement();
        targetObjectFields.Clear();
        
        container.style.flexDirection = FlexDirection.Row;
        // container.style.flexWrap = Wrap.Wrap;
        container.style.justifyContent = Justify.SpaceBetween;
        container.style.alignItems = Align.Auto;
        container.Add(new Toggle());
        var objectField = new ObjectField()
        {
            objectType = typeof(MonoBehaviour),
        };
        objectField.style.width = new StyleLength(Length.Percent(90));
        objectField.value = targetObject;
        
        objectField.RegisterValueChangedCallback((e) =>
        {
            if (e.newValue != e.previousValue)
            {
                propertyInitializer.RemoveTargetObject(e.previousValue as MonoBehaviour);
                propertyInitializer.AddTargetObject(e.newValue as MonoBehaviour);
            }
        });
        // objectField.style.marginLeft = 8;
        container.Add(objectField);
        targetObjectFields.Add(container);
        return container;
    }

     public void AddTarget()
     {
//         
    }
    
   
}
using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using Pickle.ObjectProviders;
using System.Collections;

namespace Pickle.Editor
{
    public struct PickleFieldConfiguration
    {
        public IObjectProvider ObjectProvider;
        public PickerType PickerType;
        public AutoPickMode AutoPickMode;
        public Predicate<ObjectTypePair> Filter;
    }


#if DEFAULT_TO_PICKLE
    [CustomPropertyDrawer(typeof(UnityEngine.Object), true)]
#endif
    [CustomPropertyDrawer(typeof(PickleAttribute))]
    public class PickleAttributeDrawer : PropertyDrawer
    {
        private const float AUTO_PICK_BUTTON_WIDTH = 20f;

        private bool _isInitialized;
        private bool _isValidField;
        private Type _fieldType;
        private ObjectFieldDrawer _objectFieldDrawer;
        private IObjectPicker _objectPicker;
        private SerializedProperty _property;
        private PickleFieldConfiguration _configuration;


        private void Initialize(SerializedProperty property)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _isValidField = property.propertyType == SerializedPropertyType.ObjectReference;
        
            if (_isValidField)
            {
                var targetObject = property.serializedObject.targetObject;
                var targetObjectType = targetObject.GetType();

                // find the field type
                _fieldType = ExtractFieldType(fieldInfo);

                if (_fieldType == null)
                {
                    _isValidField = false;
                    return;
                }

                _configuration = new PickleFieldConfiguration();
                _configuration.Filter = null;

                PickerType pickerType = PickleSettings.GetDefaultPickerType(_fieldType);

                if (base.attribute != null)
                {
                    var attribute = (PickleAttribute)this.attribute;

                    _configuration = ExtractConfigurationFromAttribute(attribute, targetObject, targetObjectType, _fieldType);

                    if (attribute.CustomTypeName != null)
                    {
                        _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, attribute.CustomTypeName);
                    }
                    else if (attribute.AdditionalTypeFilter != null)
                    {
                        _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, $"{_fieldType.Name}: {attribute.AdditionalTypeFilter.Name}");
                    }
                    else
                    {
                        _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, _fieldType);
                    }
                }
                else
                {
                    _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, _fieldType);

                    _configuration.ObjectProvider = ObjectProviderUtilities.ResolveProviderTypeToProvider(PickleSettings.GetDefaultProviderType(), _fieldType, targetObject, true);
                    _configuration.AutoPickMode = PickleSettings.DefaultAutoPickMode;
                    _configuration.PickerType = PickleSettings.GetDefaultPickerType(_fieldType);
                }

                InitializePickerPopup(_configuration.ObjectProvider, pickerType);

                _objectFieldDrawer.OnObjectPickerButtonClicked += OpenObjectPicker;
                _objectPicker.OnOptionPicked += ChangeObject;
            }
        }

        private void InitializePickerPopup(IObjectProvider objectProvider, PickerType pickerType)
        {
            if (pickerType == PickerType.Default) pickerType = PickerType.Dropdown;

            if (pickerType == PickerType.Dropdown)
            {
                _objectPicker = new ObjectPickerDropdown(
                    objectProvider,
                    new AdvancedDropdownState(),
                    _configuration.Filter
                );
            }
            else if (pickerType == PickerType.Window)
            {
                _objectPicker = new ObjectPickerWindowBuilder(_property.displayName, objectProvider, _configuration.Filter);
            }
            else
            {
                throw new System.NotImplementedException($"Picker type {pickerType} not implemented!");
            }
        }

        private static PickleFieldConfiguration ExtractConfigurationFromAttribute(
            PickleAttribute attribute, 
            UnityEngine.Object targetObject, 
            Type targetObjectType,
            Type fieldType)
        {
            var result = new PickleFieldConfiguration();

            var lookupType = attribute.LookupType == ObjectProviderType.Default ? PickleSettings.GetDefaultProviderType() : attribute.LookupType;
            result.ObjectProvider = lookupType.ResolveProviderTypeToProvider(fieldType, targetObject);

            if (!string.IsNullOrEmpty(attribute.FilterMethodName))
            {
                var filterMethodInfo = targetObjectType.GetMethod(
                    attribute.FilterMethodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );

                if (filterMethodInfo != null)
                {
                    var parameters = filterMethodInfo.GetParameters();

                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(ObjectTypePair))
                    {
                        result.Filter = (Predicate<ObjectTypePair>)Delegate.CreateDelegate(typeof(Predicate<ObjectTypePair>), targetObject, filterMethodInfo);
                    }
                    else
                    {
                        Debug.LogError($"CustomPicker filter method with name {attribute.FilterMethodName} on object {targetObject} has wrong arguments!", targetObject);
                    }
                }
                else
                {
                    Debug.LogError($"CustomPicker filter method with name {attribute.FilterMethodName} on object {targetObject}:{targetObjectType} not found!", targetObject);
                }
            }

            if (attribute.AdditionalTypeFilter != null)
            {
                if (result.Filter == null)
                {
                    result.Filter = (objectTypePair) => attribute.AdditionalTypeFilter.IsAssignableFrom(objectTypePair.Object.GetType());
                }
                else
                {
                    var customFilter = result.Filter;
                    result.Filter = (objectTypePair) => 
                        attribute.AdditionalTypeFilter.IsAssignableFrom(objectTypePair.Object.GetType())
                        && customFilter(objectTypePair);
                }
            }

            result.PickerType = attribute.PickerType == PickerType.Default ? PickleSettings.GetDefaultPickerType(fieldType) : attribute.PickerType;

            result.AutoPickMode = attribute.AutoPickMode == AutoPickMode.Default ? PickleSettings.DefaultAutoPickMode : attribute.AutoPickMode;

            return result;
        }

        private Type ExtractFieldType(System.Reflection.FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                return null;

            var fieldType = fieldInfo.FieldType;

            if (fieldType.IsArray)
                return fieldType.GetElementType();
            
            if (typeof(IList).IsAssignableFrom(fieldType))
                return fieldType.GetGenericArguments()[0];

            return fieldInfo.FieldType;
        }

        private void ChangeObject(UnityEngine.Object obj)
        {
            if (obj == _property.objectReferenceValue)
                return;

            if (typeof(Component).IsAssignableFrom(_fieldType) && obj is GameObject go)
            {
                obj = go.GetComponent(_fieldType);
            }

            if (_property.objectReferenceValue != obj)
            {
                _property.objectReferenceValue = obj;
                _property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void OpenObjectPicker()
        {
            _objectPicker.Show(_objectFieldDrawer.FieldRect, _property.objectReferenceValue);
        }

        private bool CheckObjectType(UnityEngine.Object obj)
        {
            if (typeof(Component).IsAssignableFrom(_fieldType) && obj is GameObject go)
            {
                if (!go.TryGetComponent(_fieldType, out var component))
                    return false;

                obj = component;
            }

            return _fieldType.IsAssignableFrom(obj.GetType()) && 
                (_configuration.Filter == null || _configuration.Filter.Invoke(ObjectTypePair.EDITOR_ConstructPairFromObject(obj)));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _property = property;

            Initialize(property);

            if (!_isValidField)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (_configuration.AutoPickMode != AutoPickMode.None)
            {
                var buttonRect = Rect.MinMaxRect(position.xMax - AUTO_PICK_BUTTON_WIDTH, position.yMin, position.xMax, position.yMax);
                position.width -= AUTO_PICK_BUTTON_WIDTH;

                if (GUI.Button(buttonRect, "A"))
                {
                    ChangeObject(_configuration.AutoPickMode.DoAutoPick(_property.serializedObject.targetObject, _fieldType, _configuration.Filter));
                }
            }

            var newReference = _objectFieldDrawer.Draw(position, label, property.objectReferenceValue);
            ChangeObject(newReference);
        }
    }
}

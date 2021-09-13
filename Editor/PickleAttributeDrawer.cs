using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using Pickle.ObjectProviders;
using System.Collections;

namespace Pickle.Editor
{
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
        private Predicate<ObjectTypePair> _filter;
        private AutoPickMode _autoPickMode;

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

                _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, _fieldType);
                _objectFieldDrawer.OnObjectPickerButtonClicked += OpenObjectPicker;

                _filter = null;

                var attribute = (PickleAttribute)this.attribute;

                IObjectProvider objectProvider;
                PickerType pickerType = PickleSettings.GetDefaultPickerType(_fieldType);

                if (base.attribute != null)
                {
                    if (attribute.FilterType != null)
                    {
                        _filter = pair =>
                        {
                            Type componentType = pair.Object.GetType();
                            return attribute.FilterType.IsAssignableFrom(componentType);
                        };
                    }
                    
                    objectProvider = attribute.LookupType.ResolveProviderTypeToProvider(_fieldType, targetObject);

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
                                _filter = (Predicate<ObjectTypePair>)Delegate.CreateDelegate(typeof(Predicate<ObjectTypePair>), targetObject, filterMethodInfo);
                            }
                            else
                            {
                                Debug.LogError($"CustomPicker filter method with name {attribute.FilterMethodName} on object {targetObject} has wrong arguments!", targetObject);
                            }
                        }
                        else
                        {
                            Debug.LogError($"CustomPicker filter method with name {attribute.FilterMethodName} on object {targetObject} not found!", targetObject);
                        }
                    }

                    pickerType = attribute.PickerType;

                    // TODO :: CHECK IF VALID FOR BASE OBJECT
                    _autoPickMode = attribute.AutoPickMode;
                }
                else
                {
                    objectProvider = ObjectProviderUtilities.GetDefaultObjectProviderForType(_fieldType, targetObject, true);
                }
                
                if (pickerType == PickerType.Dropdown)
                {
                    _objectPicker = new ObjectPickerDropdown(
                        objectProvider,
                        new AdvancedDropdownState(),
                        _filter
                    );
                }
                else if (pickerType == PickerType.Window)
                {
                    _objectPicker = new ObjectPickerWindowBuilder(_property.displayName, objectProvider, _filter);
                }
                else
                {
                    throw new System.NotImplementedException($"Picker type {pickerType} not implemented!");
                }

                _objectPicker.OnOptionPicked += ChangeObject;
            }
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

            return _fieldType.IsAssignableFrom(obj.GetType()) && (_filter == null || _filter.Invoke(ObjectTypePair.EDITOR_ConstructPairFromObject(obj)));
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

            if (_autoPickMode != AutoPickMode.None)
            {
                var buttonRect = Rect.MinMaxRect(position.xMax - AUTO_PICK_BUTTON_WIDTH, position.yMin, position.xMax, position.yMax);
                position.width -= AUTO_PICK_BUTTON_WIDTH;

                if (GUI.Button(buttonRect, "A"))
                {
                    ChangeObject(_autoPickMode.DoAutoPick(_property.serializedObject.targetObject, _fieldType));        
                }
            }

            var newReference = _objectFieldDrawer.Draw(position, label, property.objectReferenceValue);
            ChangeObject(newReference);
        }
    }
}

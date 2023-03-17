using System;
using UnityEditor;
using UnityEngine;
using Pickle.ObjectProviders;
using UnityEditor.IMGUI.Controls;

namespace Pickle.Editor
{
    public class PickleField
    {
        private const float AUTO_PICK_BUTTON_WIDTH = 20f;

        private bool _isValidField;
        private Type _fieldType;
        private ObjectFieldDrawer _objectFieldDrawer;
        private IObjectPicker _objectPicker;
        private SerializedProperty _property;
        private PickleFieldConfiguration _configuration;

        public PickleField(
            SerializedProperty property,
            Type fieldType,
            PickleFieldConfiguration configuration)
        {
            _configuration = configuration;
            _objectFieldDrawer = 
                configuration.EmptyFieldLabel != null ?
                    new ObjectFieldDrawer(CheckObjectType, configuration.EmptyFieldLabel) :
                    new ObjectFieldDrawer(CheckObjectType, fieldType);
            _fieldType = fieldType;

            _isValidField = property.propertyType == SerializedPropertyType.ObjectReference;

            if (_isValidField)
            {
                var targetObject = property.serializedObject.targetObject;
                var targetObjectType = targetObject.GetType();

                InitializePickerPopup(_configuration.ObjectProvider, _configuration.PickerType);

                _objectFieldDrawer.OnObjectPickerButtonClicked += OpenObjectPicker;
                _objectPicker.OnOptionPicked += ChangeObject;
            }
        }

        public float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _property = property;

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

        private void OpenObjectPicker()
        {
            _objectPicker.Show(_objectFieldDrawer.FieldRect, _property.objectReferenceValue);
        }

        public bool CheckObjectType(UnityEngine.Object obj)
        {
            if (typeof(Component).IsAssignableFrom(_fieldType) && obj is GameObject go)
            {
                if (!go.TryGetComponent(_fieldType, out var component))
                    return false;

                obj = component;
            }

            return _fieldType.IsAssignableFrom(obj.GetType()) && (_configuration.Filter == null || _configuration.Filter.Invoke(ObjectTypePair.EDITOR_ConstructPairFromObject(obj)));
        }
    }
}

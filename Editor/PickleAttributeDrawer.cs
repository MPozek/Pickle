using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using Pickle.ObjectProviders;

namespace Pickle.Editor
{
#if DEFAULT_TO_PICKLE
    [CustomPropertyDrawer(typeof(UnityEngine.Object), true)]
#endif
    [CustomPropertyDrawer(typeof(PickleAttribute))]
    public class PickleAttributeDrawer : PropertyDrawer
    {
        private bool _isInitialized;
        private bool _isValidField;
        private Type _fieldType;
        private ObjectFieldDrawer _objectFieldDrawer;
        private IObjectPicker _objectPicker;
        private SerializedProperty _property;
        private Predicate<ObjectTypePair> _filter;

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
                _fieldType = ExtractTypeFromPropertyPath(targetObjectType, property.propertyPath);

                _objectFieldDrawer = new ObjectFieldDrawer(CheckObjectType, _fieldType);
                _objectFieldDrawer.OnObjectPickerButtonClicked += OpenObjectPicker;

                _filter = null;

                var attribute = (PickleAttribute)this.attribute;
                IObjectProvider objectProvider;
                PickerType pickerType = PickerType.Dropdown;

                if (attribute != null)
                {
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
                    _objectPicker = new ObjectPickerWindowDefinition(_property.displayName, objectProvider, _filter);
                }
                else
                {
                    throw new System.NotImplementedException($"Picker type {pickerType} not implemented!");
                }

                _objectPicker.OnOptionPicked += ChangeObject;
            }
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
                obj = go.GetComponent(_fieldType);
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

            var newReference = _objectFieldDrawer.Draw(position, label, property.objectReferenceValue);
            ChangeObject(newReference);
        }


        private static System.Type ExtractTypeFromPropertyPath(System.Type baseType, string relativePath, int pathStartIndex = 0)
        {
            var dotIndex = relativePath.IndexOf('.', pathStartIndex);

            if (dotIndex < 0)
            {
                // we're almost there
                var field = ReflectionUtilities.ResolveFieldFromName(baseType, relativePath.Substring(pathStartIndex));
                if (field == null)
                {
                    Debug.LogError($"Couldn't find end field with name {relativePath.Substring(pathStartIndex)} full path is {relativePath}");

                    return null;
                }
                return field.FieldType;
            }
            else
            {
                var nextPathPart = relativePath.Substring(pathStartIndex, dotIndex - pathStartIndex);

                if (nextPathPart == "Array")
                {
                    // now we need to handle this stupid special case, the current baseType is a collection
                    // might be an array, might be a list or something, we can try to extract it's generic attribute,
                    // but we need to move the start index to after the format
                    // the format in front is:
                    // data[someIntHere].restOfThePath
                    // so we can find the next dot and skip it basically

                    dotIndex = relativePath.IndexOf('.', dotIndex + 1);

                    // arrays will return for GetElementType, Lists will not, so we grab the first generic argument
                    var elementType = baseType.IsArray ? baseType.GetElementType() : baseType.GetGenericArguments()[0];

                    if (dotIndex < 0)
                    {
                        return elementType;
                    }
                    else
                    {
                        return ExtractTypeFromPropertyPath(elementType, relativePath, dotIndex + 1);
                    }
                }
                else
                {
                    var field = ReflectionUtilities.ResolveFieldFromName(baseType, relativePath.Substring(pathStartIndex));

                    if (field == null)
                    {
                        Debug.LogError("Couldn't find field with name " + nextPathPart + " on type " + baseType);
                        return null;
                    }

                    return ExtractTypeFromPropertyPath(field.FieldType, relativePath, dotIndex + 1);
                }
            }
        }
    }
}

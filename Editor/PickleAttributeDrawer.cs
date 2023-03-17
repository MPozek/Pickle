using System;
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
        public string EmptyFieldLabel;
        public Predicate<ObjectTypePair> Filter;
    }


#if DEFAULT_TO_PICKLE
    [CustomPropertyDrawer(typeof(UnityEngine.Object), true)]
#endif
    [CustomPropertyDrawer(typeof(PickleAttribute))]
    public class PickleAttributeDrawer : PropertyDrawer
    {
        private PickleField _fieldDrawer;
        private bool _isValidField;

        private void Initialize(SerializedProperty property)
        {
            if (_fieldDrawer != null)
                return;

            var fieldType = ExtractFieldType(fieldInfo);

            _isValidField = fieldType != null;

            var targetObject = property.serializedObject.targetObject;
            var targetObjectType = targetObject.GetType();

            var configuration = new PickleFieldConfiguration();
            configuration.Filter = null;

            configuration.PickerType = PickleSettings.GetDefaultPickerType(fieldType);

            if (base.attribute != null)
            {
                var attribute = (PickleAttribute)this.attribute;

                configuration = ExtractConfigurationFromAttribute(attribute, targetObject, targetObjectType, fieldType);

                if (attribute.CustomTypeName != null)
                {
                    configuration.EmptyFieldLabel = attribute.CustomTypeName;
                }
                else if (attribute.AdditionalTypeFilter != null)
                {
                    configuration.EmptyFieldLabel = $"{fieldType.Name}: {attribute.AdditionalTypeFilter.Name}";
                }
                else
                {
                    configuration.EmptyFieldLabel = null;
                }
            }
            else
            {
                configuration.EmptyFieldLabel = null;

                configuration.ObjectProvider = ObjectProviderUtilities.ResolveProviderTypeToProvider(PickleSettings.GetDefaultProviderType(), fieldType, targetObject, true);
                configuration.AutoPickMode = PickleSettings.DefaultAutoPickMode;
                configuration.PickerType = PickleSettings.GetDefaultPickerType(fieldType);
            }

            _fieldDrawer = new PickleField(
                property,
                fieldType,
                configuration
            );
        }

        private static Type ExtractFieldType(System.Reflection.FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                return null;

            var fieldType = fieldInfo.FieldType;

            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (typeof(IList).IsAssignableFrom(fieldType))
                return fieldType.GetGenericArguments()[0];

            return fieldType;
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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            return _fieldDrawer.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            if (!_isValidField)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            _fieldDrawer.OnGUI(position, property, label);
        }
    }
}

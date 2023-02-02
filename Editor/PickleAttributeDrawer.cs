using System;
using UnityEditor;
using UnityEngine;
using Pickle.ObjectProviders;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;

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
                var ownerObject = property.GetOwnerObject();

                configuration = ExtractConfigurationFromAttribute(attribute, ownerObject, targetObject, targetObjectType, fieldType);

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
            object ownerObject,
            UnityEngine.Object targetObject,
            Type targetObjectType,
            Type fieldType)
        {
            var result = new PickleFieldConfiguration();

            var lookupType = attribute.LookupType == ObjectProviderType.Default ? PickleSettings.GetDefaultProviderType() : attribute.LookupType;
            result.ObjectProvider = lookupType.ResolveProviderTypeToProvider(fieldType, targetObject);

            if (!string.IsNullOrEmpty(attribute.FilterMethodName))
            {
                var filterMethodInfo = ownerObject.GetType().GetMethod(
                    attribute.FilterMethodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );

                if (filterMethodInfo != null)
                {
                    var parameters = filterMethodInfo.GetParameters();

                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(ObjectTypePair))
                    {
                        result.Filter = (Predicate<ObjectTypePair>)Delegate.CreateDelegate(typeof(Predicate<ObjectTypePair>), ownerObject, filterMethodInfo);
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

    public static class SerializedPropertyExtension
    {
        const BindingFlags FLAGS_ALL = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public static object GetOwnerObject(this SerializedProperty property)
        {
            object owner = property.serializedObject.targetObject;

            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; ++i)
            {
                var element = elements[i];
                if (IsArrayElement(element))
                {
                    var (arrayName, index) = GetArrayInfo(element);

                    var arrayFieldInfo = FindField(owner.GetType(), arrayName);
                    if (arrayFieldInfo == null)
                        return null;

                    var array = arrayFieldInfo.GetValue(owner) as System.Array;
                    if (array == null)
                        return null;

                    owner = array.GetValue(index);
                }
                else
                {
                    var fieldInfo = FindField(owner.GetType(), element);
                    if (fieldInfo == null)
                        return null;

                    owner = fieldInfo.GetValue(owner);
                }

                if (owner == null)
                    return null;

            }

            return owner;
        }

        public static bool IsArrayElement(string element)
        {
            return element.EndsWith("]");
        }

        static Regex arrayNameIndexRegex = new Regex(@"^(.+?)\[(.+?)\]$");
        static (string name, int index) GetArrayInfo(string element)
        {
            var m = arrayNameIndexRegex.Match(element);
            return (m.Groups[1].Value, int.Parse(m.Groups[2].Value));
        }

        public static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                var result = type.GetField(name, FLAGS_ALL);

                if (result != null)
                    return result;

                type = type.BaseType;

            }

            return null;
        }
    }
}

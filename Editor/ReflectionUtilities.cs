using System;
using System.Linq;
using System.Reflection;

namespace Pickle.Editor
{
    internal static class ReflectionUtilities
    {
        public static Type ExtractTypeFromPropertyPath(object targetObject, Type baseType, in string relativePath, int pathStartIndex = 0)
        {
            var dotIndex = relativePath.IndexOf('.', pathStartIndex);

            if (dotIndex < 0)
            {
                // we're almost there
                var field = ResolveFieldFromName(baseType, relativePath.Substring(pathStartIndex));
                if (field == null)
                {
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
                    // fieldName.Array.data[someIntHere].restOfThePathIfOurPropertyIsNotTheElementOfTheArrayOtherwiseEmpty
                    // so we can find the next dot and skip it basically

                    var newDotIndex = relativePath.IndexOf('.', dotIndex + 1);

                    // check if our property is the element of the array
                    if (newDotIndex < 0)
                    {
                        // arrays will return for GetElementType, Lists will not, so we grab the first generic argument
                        return baseType.IsArray ? baseType.GetElementType() : baseType.GetGenericArguments()[0]; ;
                    }
                    else
                    {
                        // find the index specific index we're drawing
                        int elementInCollectionIndex = ParseElementIndexFromSubpath(relativePath, dotIndex);

                        // resolve the indexed object
                        var newTargetObject = GetValueAtIndex(baseType, targetObject, elementInCollectionIndex);
                        var elementType = newTargetObject.GetType();

                        return ExtractTypeFromPropertyPath(newTargetObject, elementType, relativePath, newDotIndex + 1);
                    }
                }
                else
                {
                    var field = ResolveFieldFromName(baseType, nextPathPart);

                    if (field == null)
                    {
                        throw new System.ArgumentException($"Couldn't find field at relative path {relativePath.Substring(pathStartIndex)} in type {baseType}");
                    }

                    var fieldValue = GetFieldValue(targetObject, field);

                    return ExtractTypeFromPropertyPath(fieldValue, fieldValue != null ? fieldValue.GetType() : field.FieldType, relativePath, dotIndex + 1);
                }
            }
        }

        private static int ParseElementIndexFromSubpath(in string relativePath, int startIndex)
        {
            var bracketStartIndex = relativePath.IndexOf('[', startIndex);
            var bracketEndIndex = relativePath.IndexOf(']', bracketStartIndex);

            if (bracketStartIndex < 0 || bracketEndIndex < 0)
                return -1;

            bracketStartIndex++;
            var index = int.Parse(relativePath.Substring(bracketStartIndex, bracketEndIndex - bracketStartIndex));
            return index;
        }

        private static object GetValueAtIndex(System.Type type, object instance, int index)
        {
            var indexingProperty = FindArrayIndexingProperty(type);
            if (indexingProperty == null) return null;
            return GetIndexingPropertyValue(instance, indexingProperty, index);
        }

        private static readonly object[] INDEXING_PARAMETER = new object[1];
        private static object GetIndexingPropertyValue(object targetObject, PropertyInfo indexingProperty, int index)
        {
            INDEXING_PARAMETER[0] = index;
            return indexingProperty.GetValue(targetObject, INDEXING_PARAMETER);
        }

        private static PropertyInfo FindArrayIndexingProperty(Type type)
        {
            return type.GetProperties().Where((pi) =>
            {
                var indexParams = pi.GetIndexParameters();
                return indexParams.Length == 1 && indexParams[0].ParameterType == typeof(int);
            }).FirstOrDefault();
        }

        public static FieldInfo ResolveFieldFromName(Type type, string name)
        {
            FieldInfo field = null;

            while (type != null && field == null)
            {
                field = Array.Find(
                    type.GetFields(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    ),
                    (f) => f.Name == name
                );

                type = type.BaseType;
            }

            return field;
        }

        private static object GetFieldValue(object targetObject, FieldInfo field)
        {
            if (targetObject == null) return null;
            return field.GetValue(targetObject);
        }
    }
}

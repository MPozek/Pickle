using System;

namespace Pickle.Editor
{
    internal static class ReflectionUtilities
    {
        public static System.Reflection.FieldInfo ResolveFieldFromName(System.Type type, string name)
        {
            var field = Array.Find(type.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance),
                (f) => f.Name == name);

            if (field == null)
            {
                var baseType = type.BaseType;
                if (baseType != null)
                {
                    field = ResolveFieldFromName(baseType, name);
                }
            }

            return field;
        }
    }
}

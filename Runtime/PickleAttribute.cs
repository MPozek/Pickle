using System;
using UnityEngine;

namespace Pickle
{

    [AttributeUsage(AttributeTargets.Field)]
    public class PickleAttribute : PropertyAttribute
    {
        public ObjectProviderType LookupType = ObjectProviderType.Assets | ObjectProviderType.Scene;
        public PickerType PickerType = PickerType.Dropdown;
        public string FilterMethodName = null;
        public AutoPickMode AutoPickMode = AutoPickMode.None;
        public Type FilterType = null;
    }

    public enum PickerType
    {
        Window, Dropdown
    }

    public enum AutoPickMode
    {
        None, GetComponent, GetComponentInChildren, FindObject, GetComponentInParent
    }

    public static class EnumExtensions
    {
        public static UnityEngine.Object DoAutoPick(this AutoPickMode mode, UnityEngine.Object fromObject, System.Type targetType)
        {
            switch (mode)
            {
                case AutoPickMode.GetComponent:
                    return ((Component)fromObject).GetComponent(targetType);
                case AutoPickMode.GetComponentInChildren:
                    return ((Component)fromObject).GetComponentInChildren(targetType);
                case AutoPickMode.GetComponentInParent:
                    return ((Component)fromObject).GetComponentInParent(targetType);
                case AutoPickMode.FindObject:
                    return GameObject.FindObjectOfType(targetType);
            }

            throw new System.NotImplementedException($"Auto picking for mode {mode} is not implemented!");
        }
    }
}
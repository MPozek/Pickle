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

        public PickleAttribute(ObjectProviderType providerType, string filterMethod = null)
        {
            LookupType = providerType;
            FilterMethodName = filterMethod;
        }

        public PickleAttribute(ObjectProviderType providerType, AutoPickMode autoPick, string filterMethod = null)
        {
            LookupType = providerType;
            AutoPickMode = autoPick;
            FilterMethodName = filterMethod;
        }

        public PickleAttribute(AutoPickMode autoPick, string filterMethod = null)
        {
            AutoPickMode = autoPick;
            FilterMethodName = filterMethod;
        }
    }

    public enum PickerType
    {
        Window, Dropdown
    }
}
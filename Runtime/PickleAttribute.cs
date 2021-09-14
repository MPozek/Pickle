using System;
using UnityEngine;

namespace Pickle
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PickleAttribute : PropertyAttribute
    {
        public ObjectProviderType LookupType = ObjectProviderType.Default;
        public PickerType PickerType = PickerType.Default;
        public string FilterMethodName = null;
        public AutoPickMode AutoPickMode = AutoPickMode.Default;

        public PickleAttribute() { }

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
}
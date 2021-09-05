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
    }

    public enum PickerType
    {
        Window, Dropdown
    }
}
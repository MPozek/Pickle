﻿using System;
using UnityEngine;

#if !PICKLE_IN_ROOT_NAMESPACE
namespace Pickle
{
#else 
using Pickle;
#endif

    [AttributeUsage(AttributeTargets.Field)]
    public class PickleAttribute : PropertyAttribute
    {
        public ObjectProviderType LookupType = ObjectProviderType.Default;
        public PickerType PickerType = PickerType.Default;
        public Type InterfaceFilter;
        public string FilterMethodName = null;
        public AutoPickMode AutoPickMode = AutoPickMode.Default;

        public PickleAttribute() { }

        public PickleAttribute(string filterMethod)
        {
            FilterMethodName = filterMethod;
        }

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

        public PickleAttribute(
            Type interfaceFilter,
            ObjectProviderType providerType = ObjectProviderType.Default,
            AutoPickMode autoPick = AutoPickMode.Default,
            string filterMethod = null)
        {
            InterfaceFilter = interfaceFilter;
            LookupType = providerType;
            AutoPickMode = autoPick;
            FilterMethodName = filterMethod;
        }

        public PickleAttribute(Type interfaceFilter, AutoPickMode autoPick, string filterMethod = null)
        {
            InterfaceFilter = interfaceFilter;
            AutoPickMode = autoPick;
            FilterMethodName = filterMethod;
        }

        public PickleAttribute(Type interfaceFilter, string filterMethod)
        {
            InterfaceFilter = interfaceFilter;
            FilterMethodName = filterMethod;
        }

        public PickleAttribute(Type interfaceFilter, ObjectProviderType providerType, string filterMethod = null)
        {
            InterfaceFilter = interfaceFilter;
            LookupType = providerType;
            FilterMethodName = filterMethod;
        }
    }

#if !PICKLE_IN_ROOT_NAMESPACE
}
#endif
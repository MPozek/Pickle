using UnityEngine;
using System.Linq;

namespace Pickle
{
    public enum AutoPickMode
    {
        None = 0, 
        GetComponent = 1, 
        GetComponentInChildren = 2, 
        FindObject = 3, 
        GetComponentInParent = 4,

        Default = -1,
    }

    public static class AutoPickExtensions
    {
        public static Object DoAutoPick(this AutoPickMode mode, UnityEngine.Object fromObject, System.Type targetType)
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

        public static Object DoAutoPick(this AutoPickMode mode, Object fromObject, System.Type targetType, System.Predicate<ObjectTypePair> filter)
        {
            if (filter == null)
                return mode.DoAutoPick(fromObject, targetType);

            System.Func<Object, bool> predicate = (obj) => filter.Invoke(new ObjectTypePair { Object = obj, Type = ObjectSourceType.Scene });

            switch (mode)
            {
                case AutoPickMode.GetComponent:
                    return ((Component)fromObject).GetComponents(targetType).FirstOrDefault(predicate);
                case AutoPickMode.GetComponentInChildren:
                    return ((Component)fromObject).GetComponentsInChildren(targetType).FirstOrDefault(predicate);
                case AutoPickMode.GetComponentInParent:
                    return ((Component)fromObject).GetComponentsInParent(targetType).FirstOrDefault(predicate);
                case AutoPickMode.FindObject:
                    return GameObject.FindObjectsOfType(targetType).FirstOrDefault(predicate);
            }

            throw new System.NotImplementedException($"Auto picking for mode {mode} is not implemented!");
        }
    }
}
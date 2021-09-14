namespace Pickle
{
    public enum AutoPickMode
    {
        None, GetComponent, GetComponentInChildren, FindObject, GetComponentInParent
    }

    public static class AutoPickExtensions
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
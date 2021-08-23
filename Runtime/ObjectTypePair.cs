using UnityEngine;

namespace Pickle
{
    public struct ObjectTypePair
    {
        public ObjectSourceType Type;
        public UnityEngine.Object Object;

#if UNITY_EDITOR
        public static ObjectTypePair EDITOR_ConstructPairFromObject(Object obj)
        {
            return new ObjectTypePair
            {
                Object = obj,
                Type = UnityEditor.EditorUtility.IsPersistent(obj) ? ObjectSourceType.Asset : ObjectSourceType.Scene
            };
        }
#endif
    }
}

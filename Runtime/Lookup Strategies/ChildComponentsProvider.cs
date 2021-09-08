using System.Collections.Generic;
using UnityEngine;

namespace Pickle.ObjectProviders
{

    public class ChildComponentsProvider : IObjectProvider
    {
        private Transform _parent;
        private System.Type _componentType;

        public ChildComponentsProvider(Transform parent, System.Type componentType)
        {
            _parent = parent;
            _componentType = componentType;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
            foreach (var component in _parent.GetComponentsInChildren(_componentType, true))
            {
                yield return new ObjectTypePair
                {
                    Object = component,
                    Type = ObjectSourceType.Scene
                };
            }
        }
    }
}

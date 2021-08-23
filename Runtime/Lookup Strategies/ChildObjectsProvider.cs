using System.Collections.Generic;
using UnityEngine;

namespace Pickle.ObjectProviders
{
    public class ChildObjectsProvider : IObjectProvider
    {
        private Transform _parent;

        public ChildObjectsProvider(Transform parent)
        {
            _parent = parent;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
            Queue<Transform> openSet = new Queue<Transform>();
            openSet.Enqueue(_parent);

            while (openSet.Count > 0)
            {
                var cur = openSet.Dequeue();

                yield return new ObjectTypePair { Object = cur.gameObject, Type = ObjectSourceType.Scene };
            
                for (int i = 0; i < cur.childCount; i++)
                {
                    openSet.Enqueue(cur.GetChild(i));
                }
            }
        }
    }
}

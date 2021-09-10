using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickle.ObjectProviders
{
    public class SceneObjectsProvider : IObjectProvider
    {
        private static readonly List<GameObject> ROOT_OBJECT_CACHE = new List<GameObject>();
        private Scene _scene;

        public SceneObjectsProvider(Scene scene)
        {
            _scene = scene;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
            _scene.GetRootGameObjects(ROOT_OBJECT_CACHE);

            foreach (var rootGameObject in ROOT_OBJECT_CACHE)
            {
                var hierarchyEnumerator = TraverseHierarchy(rootGameObject);
                while (hierarchyEnumerator.MoveNext())
                    yield return new ObjectTypePair { Object = hierarchyEnumerator.Current, Type = ObjectSourceType.Scene };
            }

            ROOT_OBJECT_CACHE.Clear();
        }

        private IEnumerator<GameObject> TraverseHierarchy(GameObject root)
        {
            yield return root;

            var childCount = root.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var hierarchyEnumerator = TraverseHierarchy(root.transform.GetChild(i).gameObject);
                while (hierarchyEnumerator.MoveNext())
                    yield return hierarchyEnumerator.Current;
            }
        }
    }
}

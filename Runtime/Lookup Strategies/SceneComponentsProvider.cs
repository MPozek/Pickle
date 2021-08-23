using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickle.ObjectProviders
{

    public class SceneComponentsProvider : IObjectProvider
    {
        private static readonly List<GameObject> ROOT_OBJECT_CACHE = new List<GameObject>();
        private static readonly List<UnityEngine.Object> COMPONENT_RESULT_CACHE = new List<UnityEngine.Object>();

        private Scene _scene;
        private readonly System.Type _type;

        public SceneComponentsProvider(Scene scene, System.Type componentType)
        {
            _scene = scene;
            _type = componentType;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
            _scene.GetRootGameObjects(ROOT_OBJECT_CACHE);
            foreach (var rootGameObject in ROOT_OBJECT_CACHE)
            {
                COMPONENT_RESULT_CACHE.AddRange(rootGameObject.GetComponentsInChildren(_type, true));
                
                foreach (var component in COMPONENT_RESULT_CACHE)
                {
                    yield return new ObjectTypePair { Object = component, Type = ObjectSourceType.Scene };
                }

                COMPONENT_RESULT_CACHE.Clear();
            }
            ROOT_OBJECT_CACHE.Clear();
        }
    }
}

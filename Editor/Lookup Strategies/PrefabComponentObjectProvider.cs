using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pickle.ObjectProviders
{
    public class PrefabComponentObjectProvider : IObjectProvider
    {
        private readonly bool _allowPackageAssets;
        private readonly System.Type _type;

        public PrefabComponentObjectProvider(System.Type componentType, bool allowPackageAssets = false)
        {
            _allowPackageAssets = allowPackageAssets;
            _type = componentType;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (!_allowPackageAssets && !path.StartsWith("Assets"))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (asset.TryGetComponent(_type, out var prefabComponent))
                    yield return new ObjectTypePair { Object = prefabComponent, Type = ObjectSourceType.Asset };
            }
#else
            return null;
#endif
        }
    }
}

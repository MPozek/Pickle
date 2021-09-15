using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pickle.ObjectProviders
{
    public class AssetObjectProvider : IObjectProvider
    {
        private readonly bool _allowPackageAssets;
        private readonly System.Type _type;
        private readonly Predicate<UnityEngine.Object> _additionalFilter;

        public AssetObjectProvider(System.Type assetType, Predicate<UnityEngine.Object> additionalFilter = null, bool allowPackageAssets = false)
        {
            _allowPackageAssets = allowPackageAssets;
            _type = assetType;
            _additionalFilter = additionalFilter;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{_type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (!_allowPackageAssets && !path.StartsWith("Assets"))
                    continue;

                if (path.EndsWith(".unity"))
                {
                    // it's invalid to use load all assets on scene objects
                    var asset = AssetDatabase.LoadAssetAtPath(path, _type);
                
                    if (_additionalFilter == null || _additionalFilter.Invoke(asset))
                    {
                        yield return new ObjectTypePair { Object = asset, Type = ObjectSourceType.Asset };
                    }
                }
                else
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var loadedGuid, out long _);

                        if (guid == loadedGuid && _additionalFilter == null || _additionalFilter.Invoke(asset))
                        {
                            yield return new ObjectTypePair { Object = asset, Type = ObjectSourceType.Asset };
                        }
                    }
                }
            }
#else
            return null;
#endif
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickle.ObjectProviders
{
    public static class ObjectProviderUtilities
    {
        public static IObjectProvider ResolveProviderTypeToProvider(
            this ObjectProviderType providerType, 
            System.Type fieldType, 
            UnityEngine.Object owner = null,
            bool allowPackages = false)
        {
            List<IObjectProvider> strategiesForUnion = new List<IObjectProvider>();

            // find the owner gameobject
            // if the owner is not a gameobject or a component, we later skip looking at children / scene objects
            GameObject ownerGameObject = null;
            if (owner)
            {
                if (owner is Component comp)
                    ownerGameObject = comp.gameObject;
                else if (owner is GameObject go)
                    ownerGameObject = go;
            }

            var isFieldTypeObject = typeof(UnityEngine.Object) == fieldType;
            var isFieldTypeAComponent = typeof(Component).IsAssignableFrom(fieldType);
            var isFieldTypeASceneObject = isFieldTypeObject || typeof(GameObject) == fieldType || isFieldTypeAComponent;

            if ((providerType & ObjectProviderType.Assets) != 0)
            {
                if (isFieldTypeAComponent)
                {
                    strategiesForUnion.Add(new PrefabComponentObjectProvider(fieldType, allowPackages));
                }
                else
                {
                    strategiesForUnion.Add(new AssetObjectProvider(fieldType, null, allowPackages));
                }
            }

            if (ownerGameObject && isFieldTypeASceneObject)
            {
                if ((providerType & ObjectProviderType.Scene) != 0)
                {
                    if (isFieldTypeAComponent)
                    {
                        strategiesForUnion.Add(new SceneComponentsProvider(ownerGameObject.scene, fieldType));
                    }
                    else if (isFieldTypeObject)
                    {
                        strategiesForUnion.Add(new SceneComponentsProvider(ownerGameObject.scene, typeof(Component)));
                        strategiesForUnion.Add(new SceneObjectsProvider(ownerGameObject.scene));
                    }
                    else
                    {
                        strategiesForUnion.Add(new SceneObjectsProvider(ownerGameObject.scene));
                    }
                }
                else
                {
                    if ((providerType & ObjectProviderType.RootChildren) != 0)
                    {
                        if (isFieldTypeAComponent)
                        {
                            strategiesForUnion.Add(new RootChildrenComponentsProvider(ownerGameObject.transform, fieldType));
                        }
                        else if (isFieldTypeObject)
                        {
                            strategiesForUnion.Add(new RootChildrenComponentsProvider(ownerGameObject.transform, fieldType));
                            strategiesForUnion.Add(new RootChildrenObjectsProvider(ownerGameObject.transform));
                        }
                        else
                        {
                            strategiesForUnion.Add(new RootChildrenObjectsProvider(ownerGameObject.transform));
                        }
                    }
                    else if ((providerType & ObjectProviderType.Children) != 0)
                    {
                        if (isFieldTypeAComponent)
                        {
                            strategiesForUnion.Add(new ChildComponentsProvider(ownerGameObject.transform, fieldType));
                        }
                        else if (isFieldTypeObject)
                        {
                            strategiesForUnion.Add(new ChildComponentsProvider(ownerGameObject.transform, fieldType));
                            strategiesForUnion.Add(new ChildObjectsProvider(ownerGameObject.transform));
                        }
                        else
                        {
                            strategiesForUnion.Add(new ChildObjectsProvider(ownerGameObject.transform));
                        }
                    }
                }
            }

            return new ObjectProviderUnion(strategiesForUnion.ToArray());
        }
    }
}

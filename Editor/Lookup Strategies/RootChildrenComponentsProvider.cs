using System;
using UnityEngine;

namespace Pickle.ObjectProviders
{

    public class RootChildrenComponentsProvider : ChildComponentsProvider
    {
        public RootChildrenComponentsProvider(Transform parent, Type componentType) : base(parent.root, componentType)
        {
        }
    }
}

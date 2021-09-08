using UnityEngine;

namespace Pickle.ObjectProviders
{
    public class RootChildrenObjectsProvider : ChildObjectsProvider
    {
        public RootChildrenObjectsProvider(Transform parent) : base(parent.root)
        {
        }
    }
}

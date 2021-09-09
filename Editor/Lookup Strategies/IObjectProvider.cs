using System.Collections.Generic;

namespace Pickle.ObjectProviders
{
    public interface IObjectProvider
    {
        IEnumerator<ObjectTypePair> Lookup();
    }
}

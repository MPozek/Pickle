using System.Collections.Generic;

namespace Pickle.ObjectProviders
{
    public class ObjectProviderUnion : IObjectProvider
    {
        private IObjectProvider[] _strategies;

        public ObjectProviderUnion(params IObjectProvider[] strategies)
        {
            _strategies = strategies;
        }

        public IEnumerator<ObjectTypePair> Lookup()
        {
            foreach (var strategy in _strategies)
            {
                var strategyResults = strategy.Lookup();
                while (strategyResults.MoveNext())
                    yield return strategyResults.Current;
            }
        }
    }
}

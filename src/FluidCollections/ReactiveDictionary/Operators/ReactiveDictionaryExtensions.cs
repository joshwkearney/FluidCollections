using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {       
        public static IObservable<TValue> LatestValue<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, TKey key) {
            return dict
                .AsObservable()
                .SelectMany(changes => changes
                    .Where(x => x.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate)
                    .Where(x => x.Key.Equals(key)))
                .Select(x => x.Value);
        }

        public static bool ContainsKey<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, TKey key) {
            return dict.TryGetValue(key, out _);
        }

        public static IObservable<KeyValuePair<TKey, TValue>> ElementsAddedOrUpdated<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict
                .AsObservable()
                .Select(x => x.Where(y => y.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate))
                .SelectMany(x => x)
                .Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value));
        }

        public static IObservable<TKey> ElementsRemoved<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict
                .AsObservable()
                .Select(x => x.Where(y => y.ChangeReason == ReactiveDictionaryChangeReason.Remove))
                .SelectMany(x => x)
                .Select(x => x.Key);
        }

#if net462
        public static IReactiveDictionary<TKey, TValue> ObserveOnDispatcher<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            if (dict == null) throw new ArgumentNullException(nameof(dict));

            return dict.AsObservable().ObserveOnDispatcher().ToDictionary(dict.TryGetValue);
        }
#endif

    }
}
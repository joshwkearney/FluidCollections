using System;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static IReactiveDictionary<TKey, TValue> Where<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TKey, TValue, bool> selector) {
            return dict
                .AsObservable()
                .Select(changes => changes
                    .Select(change => {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate && !selector(change.Key, change.Value)) {
                            if (dict.TryGetValue(change.Key, out var value)) {
                                return new ReactiveDictionaryChange<TKey, TValue>(change.Key, value, ReactiveDictionaryChangeReason.Remove);
                            }
                            else {
                                return null;
                            }
                        }
                        else {
                            return change;
                        }
                    })
                    .Where(x => x != null)
                    .ToArray()
                )
                .Where(x => x.Any())
                .ToDictionary((TKey key, out TValue value) => {
                    if (!dict.TryGetValue(key, out value)) {
                        return false;
                    }

                    return selector(key, value);
                });
        }

        public static IReactiveDictionary<TKey, TValue> Where<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TValue, bool> selector) {
            return dict.Where((_, value) => selector(value));
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static ICollectedReactiveSet<TValue> ToValueSet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.ToSet().Select(x => x.Value);
        }

        public static ICollectedReactiveSet<TKey> ToKeySet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.ToSet().Select(x => x.Key);
        }

        public static IReactiveSet<KeyValuePair<TKey, TValue>> ToSet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.AsObservable().SelectMany(changes => changes.SelectMany(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    if (dict.TryGetValue(change.Key, out var oldValue)) {
                        // Updating, so remove the old pair and add the new one
                        return new[] {
                            new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                                ReactiveSetChangeReason.Remove,
                                new[] { new KeyValuePair<TKey, TValue>(change.Key, oldValue) }
                            ),
                            new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                                ReactiveSetChangeReason.Add,
                                new[] { new KeyValuePair<TKey, TValue>(change.Key, change.Value) }
                            )
                        };
                    }
                }

                // Just adding or removing
                return new[] {
                    new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                        change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate ? ReactiveSetChangeReason.Add : ReactiveSetChangeReason.Remove,
                        new[] { new KeyValuePair<TKey, TValue>(change.Key, change.Value) }
                    )
                };
            }))
            .ToReactiveSet(pair => dict.TryGetValue(pair.Key, out var value) && value.Equals(pair.Value));
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static ICollectedReactiveSet<TValue> ToValueSet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, IDictionary<TValue, int> innerDictionary) {
            return new SetSelectImplementation<KeyValuePair<TKey, TValue>, TValue>(
                dict.ToSet().AsObservable(),
                x => x.Value,
                innerDictionary
            );
        }

        public static ICollectedReactiveSet<TValue> ToValueSet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.ToValueSet(new Dictionary<TValue, int>());
        }

        public static IReactiveSet<KeyValuePair<TKey, TValue>> ToSet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.AsObservable().Select(changes => changes.SelectMany(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    if (dict.TryGetValue(change.Key, out var oldValue)) {
                        // Updating, so remove the old pair and add the new one
                        return new[] {
                            new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                                new KeyValuePair<TKey, TValue>(change.Key, oldValue),
                                ReactiveSetChangeReason.Remove
                            ),
                            new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                                new KeyValuePair<TKey, TValue>(change.Key, change.Value),
                                ReactiveSetChangeReason.Add
                            )
                        };
                    }
                }

                // Just adding or removing
                return new[] {
                    new ReactiveSetChange<KeyValuePair<TKey, TValue>>(
                        new KeyValuePair<TKey, TValue>(change.Key, change.Value),
                        change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate ? ReactiveSetChangeReason.Add : ReactiveSetChangeReason.Remove
                    )
                };
            }))
            .ToReactiveSet(pair => dict.TryGetValue(pair.Key, out var value) && value.Equals(pair.Value));
        }

        public static IReactiveSet<TKey> ToKeySet<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict
                .AsObservable()
                .Select(changes => changes
                    .Select(change => {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                            return new ReactiveSetChange<TKey>(change.Key, ReactiveSetChangeReason.Add);
                        }
                        else {
                            return new ReactiveSetChange<TKey>(change.Key, ReactiveSetChangeReason.Remove);
                        }

                    })
                )
                .ToReactiveSet(dict.ContainsKey);
        }
    }
}
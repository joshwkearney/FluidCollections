using System;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveDictionary<TKey, TValue> ToReactiveDictionaryUnstable<TKey, TValue>(
            this IReactiveSet<TKey> set, 
            Func<TKey, TValue> valueFactory) where TValue : IEquatable<TValue> {

            return set
                .AsObservable()
                .Select(changes => changes.Select(change => {
                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                        return new ReactiveDictionaryChange<TKey, TValue>(
                            change.Value,
                            valueFactory(change.Value),
                            ReactiveDictionaryChangeReason.AddOrUpdate
                        );
                    }
                    else {
                        return new ReactiveDictionaryChange<TKey, TValue>(
                            change.Value,
                            valueFactory(change.Value),
                            ReactiveDictionaryChangeReason.Remove
                        );
                    }
                }))
                .ToDictionary((TKey key, out TValue value) => {
                    if (set.Contains(key)) {
                        value = valueFactory(key);
                        return true;
                    }

                    value = default;
                    return false;
                });
        }

        public static ICollectedReactiveDictionary<TKey, TValue> ToReactiveDictionary<TKey, TValue>(
            this IReactiveSet<TKey> set,
            Func<TKey, TValue> valueFactory) {

            return set.ToReactiveDictionaryUnstable(x => true).Select((key, _) => valueFactory(key));
        }
    }
}
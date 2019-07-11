using System;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static IObservable<TResult> Aggregate<TKey, TValue, TResult>(
                this IReactiveDictionary<TKey, TValue> dict,
                TResult seed,
                Func<TResult, TKey, TValue, TResult> addFunc,
                Func<TResult, TKey, TValue, TResult> removeFunc) {

            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (addFunc == null) throw new ArgumentNullException(nameof(addFunc));
            if (removeFunc == null) throw new ArgumentNullException(nameof(removeFunc));

            return Observable.Create<TResult>(observer => {
                var current = seed;
                observer.OnNext(current);

                IDisposable sub = null;
                sub = dict
                    .AsObservable()
                    .Subscribe(
                        x => {
                            foreach (var change in x) {
                                try {
                                    if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                                        current = addFunc(current, change.Key, change.Value);
                                    }
                                    else {
                                        current = removeFunc(current, change.Key, change.Value);
                                    }
                                }
                                catch (Exception ex) {
                                    observer.OnError(ex);
                                    sub?.Dispose();
                                    return;
                                }
                            }

                            observer.OnNext(current);
                        },
                        observer.OnError,
                        observer.OnCompleted
                    );

                return sub;
            })
            .DistinctUntilChanged();
        }

        public static IObservable<TResult> Aggregate<TKey, TValue, TResult>(
                this IReactiveDictionary<TKey, TValue> dict,
                TResult seed,
                Func<TResult, TValue, TResult> addFunc,
                Func<TResult, TValue, TResult> removeFunc) {

            return dict.Aggregate(seed, (result, _, value) => addFunc(result, value), (result, _, value) => removeFunc(result, value));
        }

        public static IObservable<ImmutableDictionary<TKey, TValue>> ToImmutableDictionaries<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.Aggregate(
                ImmutableDictionary<TKey, TValue>.Empty,
                (result, key, value) => result.SetItem(key, value),
                (result, key, value) => result.Remove(key)
            );
        }

        public static IObservable<ImmutableSortedDictionary<TKey, TValue>> ToSortedImmutableDictionaries<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.Aggregate(
                ImmutableSortedDictionary<TKey, TValue>.Empty,
                (result, key, value) => result.SetItem(key, value),
                (result, key, value) => result.Remove(key)
            );
        }

        public static IObservable<int> Count<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict) {
            return dict.Aggregate(0, (total, _) => total + 1, (total, _) => total - 1);
        }
    }
}
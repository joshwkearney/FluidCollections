using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static IReactiveDictionary<TKey, TValue> SubscribeMany<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TKey, TValue, IDisposable> subscriptionFactory) {
            var seq = Observable.Create<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>>(observer => {
                Dictionary<KeyValuePair<TKey, TValue>, IDisposable> subs = new Dictionary<KeyValuePair<TKey, TValue>, IDisposable>();

                var disposable1 = dict.AsObservable().Subscribe(
                        changes => {
                        foreach (var change in changes) {
                            if (dict.ContainsKey(change.Key)) {
                                if (subs.TryGetValue(change.Pair, out var sub)) {
                                    sub.Dispose();
                                }
                            }

                            if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                                subs[change.Pair] = subscriptionFactory(change.Key, change.Value);
                            }
                        }

                        observer.OnNext(changes);
                    },
                    observer.OnError,
                    observer.OnCompleted
                );

                return () => {
                    disposable1.Dispose();

                    foreach (var sub in subs) {
                        sub.Value.Dispose();
                    }
                };
            });

            return seq.ToDictionary(dict.TryGetValue);
        }

        public static IReactiveDictionary<TKey, TValue> SubscribeMany<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TValue, IDisposable> subscriptionFactory) {
            return dict.SubscribeMany((_, value) => subscriptionFactory(value));
        }

        public static IReactiveDictionary<TKey, TValue> SubscribeMany<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TKey, TValue, Action> subscriptionFactory) {
            return dict.SubscribeMany((key, value) => new SubscriptionHandle(subscriptionFactory(key, value)));
        }

        public static IReactiveDictionary<TKey, TValue> SubscribeMany<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, Func<TValue, Action> subscriptionFactory) {
            return dict.SubscribeMany((_, value) => new SubscriptionHandle(subscriptionFactory(value)));
        }
    }
}
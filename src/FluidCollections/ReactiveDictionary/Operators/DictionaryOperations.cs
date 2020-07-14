using System;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static IReactiveDictionary<TKey, TValue> Join<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, IReactiveDictionary<TKey, TValue> dict2) {
            var obs1 = dict.AsObservable().Select(changes => changes.Select(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.Remove) {
                    if (dict2.TryGetValue(change.Key, out var value)) {
                        return new ReactiveDictionaryChange<TKey, TValue>(change.Key, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                    }
                }

                return change;
            }));

            var obs2 = dict2.AsObservable().Select(changes => changes.Select(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.Remove) {
                    if (dict2.TryGetValue(change.Key, out var value)) {
                        return new ReactiveDictionaryChange<TKey, TValue>(change.Key, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                    }
                }

                return change;
            }));

            return obs1.Merge(obs2)
                .Select(x => x.ToArray())
                .Where(x => x.Length > 0)
                .ToDictionary((TKey key, out TValue value) => {
                    if (dict.TryGetValue(key, out value)) {
                        return true;
                    }

                    return dict2.TryGetValue(key, out value);
                });
        }

        public static IReactiveDictionary<TKey, (TFirstValue first, TSecondValue second)> SymmetricJoin<TKey, TFirstValue, TSecondValue>(
            this IReactiveDictionary<TKey, TFirstValue> dict,
            IReactiveDictionary<TKey, TSecondValue> dict2) {

            return dict.SymmetricJoinEquatable(dict2, (x, y) => (first: x, second: y));
        }

        public static IReactiveDictionary<TKey, TResultValue> SymmetricJoinEquatable<TKey, TFirstValue, TSecondValue, TResultValue>(
            this IReactiveDictionary<TKey, TFirstValue> dict,
            IReactiveDictionary<TKey, TSecondValue> dict2,
            Func<TFirstValue, TSecondValue, TResultValue> selector) where TResultValue : IEquatable<TResultValue> {

            var obs1 = dict.AsObservable().Select(changes => changes.Select(change => {
                if (dict2.TryGetValue(change.Key, out var value)) {
                    if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                        return new ReactiveDictionaryChange<TKey, TResultValue>(
                            change.Key,
                            selector(change.Value, value),
                            ReactiveDictionaryChangeReason.AddOrUpdate
                        );
                    }
                    else if (change.ChangeReason == ReactiveDictionaryChangeReason.Remove) {
                        return new ReactiveDictionaryChange<TKey, TResultValue>(
                            change.Key,
                            selector(change.Value, value),
                            ReactiveDictionaryChangeReason.Remove
                        );
                    }
                }

                return null;
            }));

            var obs2 = dict2.AsObservable().Select(changes => changes.Select(change => {
                if (dict.TryGetValue(change.Key, out var value)) {
                    if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                        return new ReactiveDictionaryChange<TKey, TResultValue>(
                            change.Key,
                            selector(value, change.Value),
                            ReactiveDictionaryChangeReason.AddOrUpdate
                        );
                    }
                    else if (change.ChangeReason == ReactiveDictionaryChangeReason.Remove) {
                        return new ReactiveDictionaryChange<TKey, TResultValue>(
                            change.Key,
                            selector(value, change.Value),
                            ReactiveDictionaryChangeReason.Remove
                        );
                    }
                }

                return null;
            }));

            return obs1.Merge(obs2)
                .Select(changes => changes.Where(x => x != null).ToArray())
                .Where(x => x.Length > 0)
                .ToDictionary((TKey key, out TResultValue value) => {
                    if (dict.TryGetValue(key, out var value1) && dict2.TryGetValue(key, out var value2)) {
                        value = selector(value1, value2);
                        return true;
                    }

                    value = default;
                    return false;
                });
        }

        public static IReactiveDictionary<TKey, TValue> Except<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, IReactiveSet<TKey> keys) {
            var obs1 = dict.AsObservable()
                .Select(changes => {
                    return changes.Select(change => {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                            if (keys.Contains(change.Key)) {
                                return null;
                            }
                        }

                        return change;
                    });
                });

            var obs2 = keys.AsObservable().Select(change => { 
                if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                    return change.Items.Select(x => new ReactiveDictionaryChange<TKey, TValue>(x, default, ReactiveDictionaryChangeReason.Remove));
                }
                else {
                    return change.Items.Select(x => { 
                        if (dict.TryGetValue(x, out var value)) {
                            return new ReactiveDictionaryChange<TKey, TValue>(x, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                        }
                        else {
                            return null;
                        }
                    });
                }
            });

            return obs1.Merge(obs2)
                .Where(x => x != null)
                .ToDictionary((TKey key, out TValue value) => {
                    if (keys.Contains(key)) {
                        value = default;
                        return false;
                    }

                    return dict.TryGetValue(key, out value);
                });
        }

        public static IReactiveDictionary<TKey, TValue> SymmetricExcept<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, IReactiveDictionary<TKey, TValue> dict2) {
            var obs1 = dict.AsObservable().Select(changes => changes.Select(change => {
                if (!dict2.TryGetValue(change.Key, out var value)) {
                    return change;
                }

                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    return new ReactiveDictionaryChange<TKey, TValue>(change.Key, default, ReactiveDictionaryChangeReason.Remove);
                }
                else {
                    return new ReactiveDictionaryChange<TKey, TValue>(change.Key, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                }
            }));

            var obs2 = dict2.AsObservable().Select(changes => changes.Select(change => {
                if (!dict.TryGetValue(change.Key, out var value)) {
                    return change;
                }

                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    return new ReactiveDictionaryChange<TKey, TValue>(change.Key, default, ReactiveDictionaryChangeReason.Remove);
                }
                else {
                    return new ReactiveDictionaryChange<TKey, TValue>(change.Key, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                }
            }));

            return obs1.Merge(obs2)
                .Select(changes => changes.Where(x => x != null).ToArray())
                .Where(x => x.Length > 0)
                .ToDictionary((TKey key, out TValue value) => {
                    bool firstContains = dict.TryGetValue(key, out var value1);
                    bool secondContains = dict2.TryGetValue(key, out var value2);

                    if (firstContains && secondContains) {
                        value = default;
                        return false;
                    }
                    else if (firstContains) {
                        value = value1;
                        return true;
                    }
                    else {
                        value = value2;
                        return true;
                    }
                });
        }

        public static IReactiveDictionary<TKey, TValue> Intersection<TKey, TValue>(this IReactiveDictionary<TKey, TValue> dict, IReactiveSet<TKey> keys) {
            var obs1 = dict.AsObservable().Select(changes => changes.Select(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    if (keys.Contains(change.Key)) {
                        return change;
                    }
                    else {
                        return null;
                    }
                }

                return change;
            }));

            var obs2 = keys.AsObservable().Select(change => change.Items.Select(item => {
                if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                    return new ReactiveDictionaryChange<TKey, TValue>(item, default, ReactiveDictionaryChangeReason.Remove);
                }
                else if (dict.TryGetValue(item, out var value)) {
                    return new ReactiveDictionaryChange<TKey, TValue>(item, value, ReactiveDictionaryChangeReason.AddOrUpdate);
                }

                return null;
            }));

            return obs1.Merge(obs2)
                .Select(changes => changes.Where(x => x != null).ToArray())
                .Where(x => x.Length > 0)
                .ToDictionary((TKey key, out TValue value) => {
                    if (keys.Contains(key)) {
                        value = default;
                        return false;
                    }

                    return dict.TryGetValue(key, out value);
                });
        }

    }
}
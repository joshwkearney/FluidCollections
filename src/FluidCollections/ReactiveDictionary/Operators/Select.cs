using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        public static IReactiveDictionary<TKey, TNewValue> SelectEquatable<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict,
            Func<TKey, TValue, TNewValue> selector) where TNewValue : IEquatable<TNewValue> {

            return dict.AsObservable().Select(changes => changes.Select(change => {
                if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                    return new ReactiveDictionaryChange<TKey, TNewValue>(change.Key, selector(change.Key, change.Value), ReactiveDictionaryChangeReason.AddOrUpdate);
                }
                else if (dict.TryGetValue(change.Key, out var value)) {
                    return new ReactiveDictionaryChange<TKey, TNewValue>(change.Key, selector(change.Key, change.Value), ReactiveDictionaryChangeReason.Remove);
                }
                else {
                    return null;
                }
            }))
            .ToDictionary((TKey key, out TNewValue value) => {
                if (dict.TryGetValue(key, out var oldValue)) {
                    value = selector(key, oldValue);
                    return true;
                }

                value = default;
                return false;
            });
        }

        public static IReactiveDictionary<TKey, TNewValue> SelectEquatable<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict,
            Func<TValue, TNewValue> selector) where TNewValue : IEquatable<TNewValue> {

            return dict.SelectEquatable((_, value) => selector(value));
        }

        public static ICollectedReactiveDictionary<TKey, TNewValue> Select<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict, 
            Func<TKey, TValue, TNewValue> selector) {

            return new SelectingDictionary<TKey, TValue, TNewValue>(dict, selector, new Dictionary<TKey, TNewValue>());
        }

        public static ICollectedReactiveDictionary<TKey, TNewValue> Select<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict, 
            Func<TKey, TValue, TNewValue> selector,
            IDictionary<TKey, TNewValue> innerDictionary) {

            return new SelectingDictionary<TKey, TValue, TNewValue>(dict, selector, innerDictionary);
        }

        public static ICollectedReactiveDictionary<TKey, TNewValue> Select<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict, 
            Func<TValue, TNewValue> selector) {

            return dict.Select((_, value) => selector(value));
        }

        public static ICollectedReactiveDictionary<TKey, TNewValue> Select<TKey, TValue, TNewValue>(
            this IReactiveDictionary<TKey, TValue> dict, 
            Func<TValue, TNewValue> selector,
            IDictionary<TKey, TNewValue> innerDictionary) {

            return dict.Select((_, value) => selector(value), innerDictionary);
        }


        private class SelectingDictionary<TKey, TValue, TNewValue> : ICollectedReactiveDictionary<TKey, TNewValue> {
            private readonly IDictionary<TKey, TNewValue> dict;
            private readonly Subject<IEnumerable<ReactiveDictionaryChange<TKey, TNewValue>>> subject = 
                new Subject<IEnumerable<ReactiveDictionaryChange<TKey, TNewValue>>>();
            private readonly Func<TKey, TValue, TNewValue> selector;
            private readonly IDisposable subscriptions;

            public event PropertyChangedEventHandler PropertyChanged;

            public object SyncRoot { get; } = new object();

            public TNewValue this[TKey key] => this.dict[key];

            public int Count => this.dict.Count;

            public SelectingDictionary(IReactiveDictionary<TKey, TValue> dict, Func<TKey, TValue, TNewValue> selector, IDictionary<TKey, TNewValue> inner) {
                this.selector = selector;

                this.dict = inner;
                this.dict.Clear();

                this.subscriptions = dict.AsObservable().Subscribe(
                    this.ProcessChanges,
                    this.subject.OnError,
                    this.subject.OnCompleted
                );
            }

            public IEnumerable<KeyValuePair<TKey, TNewValue>> AsEnumerable() => this.dict;

            public IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TNewValue>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveDictionaryChange<TKey, TNewValue>>>(observer => {
                    lock (this.SyncRoot) {
                        var changes = this.dict.Select(x => new ReactiveDictionaryChange<TKey, TNewValue>(x.Key, x.Value, ReactiveDictionaryChangeReason.AddOrUpdate));

                        observer.OnNext(changes);
                        return this.subject.Subscribe(observer);
                    }
                });
            }

            public void Dispose() => this.subscriptions.Dispose();

            public bool TryGetValue(TKey key, out TNewValue value) => this.dict.TryGetValue(key, out value);

            private void ProcessChanges(IEnumerable<ReactiveDictionaryChange<TKey, TValue>> changes) {
                lock (this.SyncRoot) {
                    var newChanges = changes.Select(change => {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                            return new ReactiveDictionaryChange<TKey, TNewValue>(change.Key, this.selector(change.Key, change.Value), change.ChangeReason);
                        }
                        else if (this.dict.TryGetValue(change.Key, out var value)) {
                            return new ReactiveDictionaryChange<TKey, TNewValue>(change.Key, value, change.ChangeReason);
                        }
                        else {
                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .ToArray();

                    this.subject.OnNext(newChanges);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    foreach (var change in newChanges) {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                            this.dict[change.Key] = change.Value;
                        }
                        else {
                            this.dict.Remove(change.Key);
                        }
                    }
                }
            }
        }
    }
}
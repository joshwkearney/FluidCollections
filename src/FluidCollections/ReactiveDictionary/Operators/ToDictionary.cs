using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveDictionaryExtensions {
        internal delegate bool TryGetValueDelegate<TKey, TValue>(TKey key, out TValue value);

        internal static IReactiveDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> sequence, 
            TryGetValueDelegate<TKey, TValue> trygetvalue) {

            return new SimpleReactiveDictionary<TKey, TValue>(sequence, trygetvalue);
        }

        public static ICollectedReactiveDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> sequence) {

            return new DependentReactiveDictionary<TKey, TValue>(sequence, new Dictionary<TKey, TValue>());
        }

        public static ICollectedReactiveDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IReactiveDictionary<TKey, TValue> dict, 
            IDictionary<TKey, TValue> inner) {

            return new DependentReactiveDictionary<TKey, TValue>(dict.AsObservable(), inner);
        }

        public static ICollectedReactiveDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IReactiveDictionary<TKey, TValue> dict) {

            return new DependentReactiveDictionary<TKey, TValue>(dict.AsObservable(), new Dictionary<TKey, TValue>());
        }

        public static ICollectedReactiveDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(
            this IReactiveDictionary<TKey, TValue> dict) {

            return new DependentReactiveDictionary<TKey, TValue>(dict.AsObservable(), new ConcurrentDictionary<TKey, TValue>());
        }

        private class SimpleReactiveDictionary<TKey, TValue> : IReactiveDictionary<TKey, TValue> {
            private readonly IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> changes;
            private readonly TryGetValueDelegate<TKey, TValue> trygetvalue;

            public SimpleReactiveDictionary(IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> changes, TryGetValueDelegate<TKey, TValue> trygetvalue) {
                this.changes = changes;
                this.trygetvalue = trygetvalue;
            }

            public TValue this[TKey key] {
                get {
                    if (this.trygetvalue(key, out var value)) {
                        return value;
                    }

                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }
            }

            public IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> AsObservable() => this.changes;

            public void Dispose() { }

            public bool TryGetValue(TKey key, out TValue value) => this.trygetvalue(key, out value);
        }

        private class DependentReactiveDictionary<TKey, TValue> : ICollectedReactiveDictionary<TKey, TValue> {
            private readonly Subject<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> subject = new Subject<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>>();
            private readonly IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            private readonly IDisposable subscriptions;

            public event PropertyChangedEventHandler PropertyChanged;

            public int Count => this.dict.Count;

            public object SyncRoot { get; } = new object();

            public TValue this[TKey key] => this.dict[key];

            public DependentReactiveDictionary(IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> inner, IDictionary<TKey, TValue> dict) {
                this.dict = dict;
                this.dict.Clear();

                this.subscriptions = inner.Subscribe(
                    this.ProcessChanges,
                    this.subject.OnError,
                    this.subject.OnCompleted
                );
            }

            public IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> AsObservable() {
                return Observable.Create<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>>(observer => {
                    lock (this.SyncRoot) {
                        var initialState = this.dict
                            .Select(x => new ReactiveDictionaryChange<TKey, TValue>(x.Key, x.Value, ReactiveDictionaryChangeReason.AddOrUpdate))
                            .ToArray();

                        if (initialState.Any()) {
                            observer.OnNext(initialState);
                        }

                        var subscription = this.subject.Subscribe(observer);
                        return () => subscription.Dispose();
                    }
                });
            }

            public bool TryGetValue(TKey key, out TValue value) => this.dict.TryGetValue(key, out value);

            public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable() => this.dict;

            public bool ContainsKey(TKey key) => this.dict.ContainsKey(key);

            public void Dispose() => this.subscriptions.Dispose();

            private void ProcessChanges(IEnumerable<ReactiveDictionaryChange<TKey, TValue>> changes) {
                lock (this.SyncRoot) {
                    this.subject.OnNext(changes);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    foreach (var change in changes) {
                        if (change.ChangeReason == ReactiveDictionaryChangeReason.AddOrUpdate) {
                            dict[change.Key] = change.Value;
                        }
                        else {
                            dict.Remove(change.Key);
                        }
                    }
                }
            }
        }
    }
}
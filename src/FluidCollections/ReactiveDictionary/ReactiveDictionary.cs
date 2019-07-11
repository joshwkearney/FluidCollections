using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public class ReactiveDictionary<TKey, TValue> : ICollectedReactiveDictionary<TKey, TValue> {
        private readonly Subject<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> changes = new Subject<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>>();
        private readonly IDictionary<TKey, TValue> dict;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => this.dict.Count;

        public object SyncRoot { get; } = new object();

        public TValue this[TKey key] {
            get {
                return this.dict[key];
            }
            set {
                if (!this.Add(key, value)) {
                    lock (this.SyncRoot) {
                        this.changes.OnNext(new[] {
                            new ReactiveDictionaryChange<TKey, TValue>(key, value, ReactiveDictionaryChangeReason.AddOrUpdate)
                        });

                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                        this.dict[key] = value;
                    }
                }
            }
        }

        public ReactiveDictionary(IDictionary<TKey, TValue> innerDictionary) {
            this.dict = innerDictionary;
        }

        public ReactiveDictionary() : this(new Dictionary<TKey, TValue>()) { }

        public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable() => this.dict;

        public IObservable<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>> AsObservable() {
            return Observable.Create<IEnumerable<ReactiveDictionaryChange<TKey, TValue>>>(observer => {
                lock (this.SyncRoot) {
                    var initialState = this.dict
                        .Select(x => new ReactiveDictionaryChange<TKey, TValue>(x.Key, x.Value, ReactiveDictionaryChangeReason.AddOrUpdate))
                        .ToArray();

                    if (initialState.Any()) {
                        observer.OnNext(initialState);
                    }

                    var subscription = this.changes.Subscribe(observer);
                    return () => subscription.Dispose();
                }
            });
        }

        public bool Add(TKey key, TValue value) {
            if (this.dict.ContainsKey(key)) {
                return false;
            }

            lock (this.SyncRoot) {
                this.changes.OnNext(new[] { new ReactiveDictionaryChange<TKey, TValue>(key, value, ReactiveDictionaryChangeReason.AddOrUpdate) });
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                this.dict.Add(key, value);
                return true;
            }
        }

        public int AddRange(IEnumerable<KeyValuePair<TKey, TValue>> elements) {
            int added = 0;

            lock (this.SyncRoot) {
                var addedItems = new Dictionary<TKey, TValue>();
                var changes = new List<ReactiveDictionaryChange<TKey, TValue>>();

                foreach (var item in elements) {
                    if (!this.dict.ContainsKey(item.Key) && !addedItems.ContainsKey(item.Key)) {
                        added++;
                        changes.Add(
                            new ReactiveDictionaryChange<TKey, TValue>(item, ReactiveDictionaryChangeReason.AddOrUpdate)
                        );

                        addedItems.Add(item.Key, item.Value);
                    }
                }

                this.changes.OnNext(changes);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                foreach (var pair in addedItems) {
                    this.dict.Add(pair.Key, pair.Value);
                }
            }

            return added;
        }

        public void UpdateRange(IEnumerable<KeyValuePair<TKey, TValue>> elements) {
            lock (this.SyncRoot) {
                var addedItems = new Dictionary<TKey, TValue>();
                var changes = new List<ReactiveDictionaryChange<TKey, TValue>>();

                foreach (var item in elements) {
                    changes.Add(
                        new ReactiveDictionaryChange<TKey, TValue>(item, ReactiveDictionaryChangeReason.AddOrUpdate)
                    );

                    addedItems[item.Key] = item.Value;
                }

                this.changes.OnNext(changes);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                foreach (var pair in addedItems) {
                    this.dict[pair.Key] = pair.Value;
                }
            }
        }

        public bool Remove(TKey item) {
            if (this.dict.TryGetValue(item, out var value)) {
                lock (this.SyncRoot) {
                    this.changes.OnNext(new[] { new ReactiveDictionaryChange<TKey, TValue>(item, value, ReactiveDictionaryChangeReason.Remove) });
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                    this.dict.Remove(item);
                    return true;
                }
            }

            return false;
        }

        public int RemoveRange(IEnumerable<TKey> elements) {
            int removed = 0;

            lock (this.SyncRoot) {
                var removedItems = new HashSet<TKey>();
                var changes = new List<ReactiveDictionaryChange<TKey, TValue>>();

                foreach (var item in elements) {
                    if (this.dict.TryGetValue(item, out var value) && !removedItems.Contains(item)) {
                        removed++;
                        changes.Add(
                            new ReactiveDictionaryChange<TKey, TValue>(item, value, ReactiveDictionaryChangeReason.Remove)
                        );

                        removedItems.Add(item);
                    }
                }

                this.changes.OnNext(changes);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));

                foreach (var item in removedItems) {
                    this.dict.Remove(item);
                }
            }

            return removed;
        }

        public void Edit(IEnumerable<KeyValuePair<TKey, TValue>> elements) {
            IDictionary<TKey, TValue> dict;

            if (elements is IDictionary<TKey, TValue> elemDict) {
                dict = elemDict;
            }
            else {
                dict = elements.ToDictionary(x => x.Key, x => x.Value);
            }

            lock (this.SyncRoot) {
                var toAddOrUpdate = new Dictionary<TKey, TValue>();
                var toRemove = new HashSet<TKey>();

                foreach (var pair in dict) {
                    if (!this.dict.TryGetValue(pair.Key, out var value) || !value.Equals(pair.Value)) {
                        toAddOrUpdate.Add(pair.Key, pair.Value);
                    }
                }

                foreach (var pair in this.dict) {
                    if (!dict.ContainsKey(pair.Key)) {
                        toRemove.Add(pair.Key);
                    }
                }

                this.RemoveRange(toRemove);
                this.UpdateRange(toAddOrUpdate);
            }
        }

        public bool TryGetValue(TKey key, out TValue value) => this.dict.TryGetValue(key, out value);

        public void Dispose() {
            this.changes.OnCompleted();
        }
    }   
}
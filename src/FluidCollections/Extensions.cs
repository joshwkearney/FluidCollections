using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluidCollections {
    public static class Extensions {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IList<T> list) {
            return new ListToReadOnlyAdapter<T>(list);
        }

        public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this ICollection<T> collection) {
            return new CollectionToReadOnlyAdapter<T>(collection);
        }

        public static ArraySegment<T> ArraySubSegment<T>(this T[] list, int index, int count) {
            return new ArraySegment<T>(list, index, count);
        }

        public static IReadOnlyDictionary<TKey, TNewValue> ToReadOnlyDictionary<TKey, TValue, TNewValue>(this IDictionary<TKey, TValue> dictionary) where TValue : TNewValue {
            return new ReadOnlyDictionaryWrapper<TKey, TValue, TNewValue>(dictionary);
        } 

        public static TValue AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
            return dictionary.AddOrUpdate(key, value, (k, v) => value);
        }

        private class ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue> : IReadOnlyDictionary<TKey, TReadOnlyValue> where TValue : TReadOnlyValue {
            private readonly IDictionary<TKey, TValue> dictionary;

            public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary) {
                this.dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
            }

            public bool ContainsKey(TKey key) => this.dictionary.ContainsKey(key);

            public IEnumerable<TKey> Keys { get => this.dictionary.Keys; }

            public bool TryGetValue(TKey key, out TReadOnlyValue result) {
                bool success = this.dictionary.TryGetValue(key, out TValue temp);
                result = temp;
                return success;
            }

            public IEnumerable<TReadOnlyValue> Values { get => this.dictionary.Values.Cast<TReadOnlyValue>(); }

            public TReadOnlyValue this[TKey key] { get => this.dictionary[key]; }

            public int Count { get => this.dictionary.Count; }

            public IEnumerator<KeyValuePair<TKey, TReadOnlyValue>> GetEnumerator() {
                return this.dictionary
                    .Select(x => new KeyValuePair<TKey, TReadOnlyValue>(x.Key, x.Value))
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        private class CollectionToReadOnlyAdapter<T> : IReadOnlyCollection<T> {
            private readonly ICollection<T> realCollection;

            public int Count => this.realCollection.Count;

            public IEnumerator<T> GetEnumerator() => this.realCollection.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public CollectionToReadOnlyAdapter(ICollection<T> list) {
                this.realCollection = list;
            }
        }

        private class ListToReadOnlyAdapter<T> : IReadOnlyList<T> {
            private readonly IList<T> realList;

            public T this[int index] => this.realList[index];

            public int Count => this.realList.Count;

            public IEnumerator<T> GetEnumerator() => this.realList.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public ListToReadOnlyAdapter(IList<T> list) {
                this.realList = list;
            }
        }
    }
}
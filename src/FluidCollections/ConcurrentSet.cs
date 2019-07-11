using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluidCollections {
    internal class ConcurrentSet<T> : IEnumerable<T>, IEnumerable, ICollection<T>, ICollection, IReadOnlyCollection<T>, ISet<T> {
        private readonly ConcurrentDictionary<T, bool> dict;
        private readonly IEqualityComparer<T> comparer;

        public ConcurrentSet() : this(EqualityComparer<T>.Default) { }

        public ConcurrentSet(IEqualityComparer<T> comparer) {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            this.dict = new ConcurrentDictionary<T, bool>(comparer);
            this.comparer = comparer;
        }

        public ConcurrentSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            this.dict = new ConcurrentDictionary<T, bool>(
                collection.ToDictionary(x => x, x => true),
                comparer
            );

            this.comparer = comparer;
        }

        public ConcurrentSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default) { }

        public int Count => this.dict.Count;

        bool ICollection.IsSynchronized => ((ICollection)this.dict).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)this.dict).SyncRoot;

        bool ICollection<T>.IsReadOnly => false;

        public bool IsEmpty => this.dict.IsEmpty;

        public void Clear() => this.dict.Clear();

        public bool Contains(T item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return this.dict.ContainsKey(item);
        }

        public void CopyTo(T[] array, int index) {
            if (array == null) throw new ArgumentNullException(nameof(array));

            foreach (T item in this) {
                array[index] = item;
                index++;
            }
        }

        public IEnumerator<T> GetEnumerator() => this.dict.Keys.GetEnumerator();

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) throw new ArgumentNullException(nameof(array));

            foreach (T item in this) {
                array.SetValue(item, index);
                index++;
            }
        }

        void ICollection<T>.Add(T item) {
            this.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool Add(T item) {
            return this.dict.TryAdd(item, true);
        }

        public bool Remove(T item) {
            return this.dict.TryRemove(item, out _);
        }

        public void ExceptWith(IEnumerable<T> other) => throw new NotImplementedException();

        public void IntersectWith(IEnumerable<T> other) => throw new NotImplementedException();

        private void IntersectWithSetWithSameEC(ISet<T> set) => throw new NotImplementedException();

        private void IntersectWithEnumerable(IEnumerable<T> other) => throw new NotImplementedException();

        public bool IsProperSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();

        private bool IsSubsetOfSetWithSameEC(ISet<T> set) => throw new NotImplementedException();

        private bool IsProperSubsetOfEnumerable(IEnumerable<T> other) => throw new NotImplementedException();

        public bool IsSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();

        public bool IsProperSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();

        public bool IsSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();

        public bool Overlaps(IEnumerable<T> other) => throw new NotImplementedException();

        public bool SetEquals(IEnumerable<T> other) => throw new NotImplementedException();

        public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotImplementedException();

        public void UnionWith(IEnumerable<T> other) => throw new NotImplementedException();
    }
}
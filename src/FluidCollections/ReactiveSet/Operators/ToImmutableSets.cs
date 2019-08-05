using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IObservable<IImmutableSet<T>> ToImmutableSets<T>(this IReactiveSet<T> set) => set.ToImmutableSets(() => ImmutableHashSet<T>.Empty);

        public static IObservable<ImmutableSortedSet<T>> ToImmutableSortedSets<T>(this IReactiveSet<T> set) {
            return set.Aggregate(ImmutableSortedSet<T>.Empty, (x, y) => x.Add(y), (x, y) => x.Remove(y));
        }

        private static IObservable<IImmutableSet<T>> ToImmutableSets<T>(this IReactiveSet<T> set, Func<IImmutableSet<T>> setFactory) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (setFactory == null) throw new ArgumentNullException(nameof(setFactory));

            return set.Aggregate(ImmutableHashSet<T>.Empty, (x, y) => x.Add(y), (x, y) => x.Remove(y));
        }
    }
}

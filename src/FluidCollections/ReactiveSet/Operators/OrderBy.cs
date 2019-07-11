using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {       
        public static IOrderedReactiveSet<T> OrderBy<T, TCompare>(this IReactiveSet<T> set, Func<T, TCompare> orderBy) where TCompare : IComparable<TCompare> {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));

            var comparer = Comparer<TCompare>.Default;

            return new SetOrderByImplementation<T>(set, Comparer<T>.Create(
                 (x, y) => {
                     return comparer.Compare(orderBy(x), orderBy(y));
                 }
            ));
        }

        public static IOrderedReactiveSet<T> OrderByDescending<T, TCompare>(this IReactiveSet<T> set, Func<T, TCompare> orderBy) where TCompare : IComparable<TCompare> {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));

            var comparer = Comparer<TCompare>.Default;

            return new SetOrderByImplementation<T>(set, Comparer<T>.Create(
                 (x, y) => {
                     return -comparer.Compare(orderBy(x), orderBy(y));
                 }
            ));
        }
    }
}
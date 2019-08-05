using System;
using System.Collections.Generic;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IOrderedReactiveSet<T> OrderBy<T, TCompare>(this IReactiveSet<T> set, Func<T, TCompare> compareFunc) {
            int comparison(T first, T second) {
                int comp = Comparer<TCompare>.Default.Compare(compareFunc(first), compareFunc(second));
                if (comp != 0) {
                    return comp;
                }

                comp = Comparer<T>.Default.Compare(first, second);
                if (comp != 0) {
                    return comp;
                }

                return 1;
            }

            return new OrderedReactiveSet<T>(set.AsObservable(), Comparer<T>.Create(comparison));
        }
    }
}

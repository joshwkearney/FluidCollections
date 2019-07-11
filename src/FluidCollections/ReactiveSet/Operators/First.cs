using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static T FirstOrDefault<T>(this IReactiveSet<T> set) {
            return set.ToEnumerable().FirstOrDefault();
        }

        public static T FirstOrDefault<T>(this IReactiveSet<T> set, Func<T, bool> predicate) {
            return set.ToEnumerable().FirstOrDefault(predicate);
        }
    }
}
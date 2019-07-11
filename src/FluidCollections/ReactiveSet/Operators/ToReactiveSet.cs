using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<T> ToReactiveSet<T>(this IObservable<IEnumerable<ReactiveSetChange<T>>> observable) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));

            return new ToReactiveSetImplementation2<T>(new HashSet<T>(), observable);
        }

        internal static IReactiveSet<T> ToReactiveSet<T>(this IObservable<IEnumerable<ReactiveSetChange<T>>> observable, Func<T, bool> containsFunc) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));
            if (containsFunc == null) throw new ArgumentNullException(nameof(containsFunc));

            return new ToReactiveSetImplementation1<T>(observable, containsFunc);
        }

        public static ICollectedReactiveSet<T> Collect<T, TSet>(this IReactiveSet<T> set) where TSet : ISet<T>, new() {
            if (set == null) throw new ArgumentNullException(nameof(set));

            var innerSet = new TSet();
            return new ToReactiveSetImplementation2<T>(innerSet, set.AsObservable());
        }

        public static ICollectedReactiveSet<T> Collect<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.Collect<T, HashSet<T>>();
        }
    }
}
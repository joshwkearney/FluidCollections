using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<T> ToReactiveSet<T>(this IObservable<ReactiveSetChange<T>> observable) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));

            return new ReactiveSet<T>(observable);
        }

        internal static IReactiveSet<T> ToReactiveSet<T>(this IObservable<ReactiveSetChange<T>> observable, Func<T, bool> containsFunc) {
            if (observable == null) throw new ArgumentNullException(nameof(observable));
            if (containsFunc == null) throw new ArgumentNullException(nameof(containsFunc));

            return new ToReactiveSetImplementation<T>(observable, containsFunc);
        }

        public static ICollectedReactiveSet<T> Collect<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return new ReactiveSet<T>(set.AsObservable());
        }
    }
}
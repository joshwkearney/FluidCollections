using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<TResult> SelectUnstable<T, TResult>(this IReactiveSet<T> set, Func<T, TResult> filter) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return set.AsObservable()
                .Select(x => x.Select(y => new ReactiveSetChange<TResult>(filter(y.Value), y.ChangeReason)))
                .ToReactiveSet();
        }

        internal static ICollectedReactiveSet<TResult> Select<T, TResult>(this IReactiveSet<T> set, Func<T, TResult> filter, IDictionary<TResult, int> innerDictionary) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return new SetSelectImplementation<T, TResult>(set.AsObservable(), filter, innerDictionary);
        }

        public static ICollectedReactiveSet<TResult> Select<T, TResult>(this IReactiveSet<T> set, Func<T, TResult> filter) {
            return set.Select(filter, new Dictionary<TResult, int>());
        }
    }
}
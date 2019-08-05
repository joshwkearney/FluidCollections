using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<TResult> Select<T, TResult>(this IReactiveSet<T> set, Func<T, TResult> filter) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return new SetSelectImplementation<T, TResult>(set.AsObservable(), filter);
        }
    }
}
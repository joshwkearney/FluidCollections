using System;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<T> Where<T>(this IReactiveSet<T> set, Func<T, bool> filter) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return set
                .AsObservable()
                .Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(filter)))
                .ToReactiveSet(x => filter(x) && set.Contains(x));
        }
    }
}
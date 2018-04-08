using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<T> Where<T>(this IReactiveSet<T> set, Func<T, bool> filter) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return set
                .AsObservable()
                .Select(changes => changes.Where(change => filter(change.Value)))
                .ToReactiveSet(x => filter(x) && set.Contains(x));
        }
    }
}
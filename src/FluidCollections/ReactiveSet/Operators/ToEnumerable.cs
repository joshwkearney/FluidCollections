using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IEnumerable<T> ToEnumerable<T>(this IReactiveSet<T> set) {
            return set.AsObservable()
                .FirstAsync()
                .Wait()
                .Where(x => x.ChangeReason == ReactiveSetChangeReason.Add)
                .Select(x => x.Value);
        }
    }
}
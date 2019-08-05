using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static bool Any<T>(this IReactiveSet<T> set) {
            return set.AsObservable().Select(x => x.Items.Any()).FirstAsync().Wait();
        }
    }
}
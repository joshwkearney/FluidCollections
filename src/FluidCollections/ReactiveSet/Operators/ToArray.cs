using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static T[] ToArray<T>(this IReactiveSet<T> set) {
            return set.AsObservable().FirstAsync().Select(x => x.Items.ToArray()).Wait();
        }
    }
}
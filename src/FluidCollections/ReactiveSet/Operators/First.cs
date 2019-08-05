using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions { 
        public static T First<T>(this IReactiveSet<T> set) {
            return set.AsObservable().Select(x => x.Items.First()).FirstAsync().Wait();
        }

        public static T FirstOrDefault<T>(this IReactiveSet<T> set) {
            return set.AsObservable().Select(x => x.Items.FirstOrDefault()).FirstAsync().Wait();
        }

        public static T FirstOrDefault<T>(this IReactiveSet<T> set, Func<T, bool> predicate) {
            return set.AsObservable().Select(x => x.Items.FirstOrDefault(predicate)).FirstAsync().Wait();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IObservable<int> Count<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.Aggregate(0, (x, _) => x + 1, (x, _) => x - 1);
        }

        public static IObservable<int> Sum(this IReactiveSet<int> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.Aggregate(0, (x, y) => x + y, (x, y) => x - y);
        }

        public static IObservable<int> Product(this IReactiveSet<int> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.Aggregate(1, (x, y) => x * y, (x, y) => x / y);
        }

        public static IObservable<T> Min<T>(this IOrderedReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().Select(_ => set.Min).DistinctUntilChanged();
        }

        public static IObservable<T> Max<T>(this IOrderedReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().Select(_ => set.Max).DistinctUntilChanged();
        }
    }
}

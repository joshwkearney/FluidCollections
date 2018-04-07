using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IObservable<T> ElementsAdded<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set
                .AsObservable()
                .Select(x => x.Where(y => y.ChangeReason == ReactiveSetChangeReason.Add))
                .SelectMany(x => x)
                .Select(x => x.Value);
        }

        public static IObservable<T> ElementsRemoved<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set
                .AsObservable()
                .Select(x => x.Where(y => y.ChangeReason == ReactiveSetChangeReason.Remove))
                .SelectMany(x => x)
                .Select(x => x.Value);
        }        

        public static IReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, TimeSpan bufferTime) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().Buffer(bufferTime).Select(x => x.SelectMany(y => y)).ToReactiveSet(set.Contains);
        }

        public static IReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, int bufferCount) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().Buffer(bufferCount).Select(x => x.SelectMany(y => y)).ToReactiveSet(set.Contains);
        }

#if net462
        public static IReactiveSet<T> ObserveOnDispatcher<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().ObserveOnDispatcher().ToReactiveSet(set.Contains);
        }
#endif

    }
}
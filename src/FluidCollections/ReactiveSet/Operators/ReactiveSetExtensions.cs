using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IObservable<T> ElementsAdded<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set
                .AsObservable()
                .Where(x => x.ChangeReason == ReactiveSetChangeReason.Add)
                .SelectMany(x => x.Items);
        }

        public static IObservable<T> ElementsRemoved<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set
                .AsObservable()
                .Where(x => x.ChangeReason == ReactiveSetChangeReason.Remove)
                .SelectMany(x => x.Items);
        }        

#if net462
        public static IReactiveSet<T> ObserveOnDispatcher<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().ObserveOnDispatcher().ToSet(set.Contains);
        }
#endif
    }
}
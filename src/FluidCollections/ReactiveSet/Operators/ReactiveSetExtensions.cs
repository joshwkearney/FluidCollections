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

        //public static ICollectedReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, TimeSpan bufferTime) {
        //    if (set == null) throw new ArgumentNullException(nameof(set));

        //    var obs = set.AsObservable();
        //    return obs.FirstAsync()
        //        .Concat(
        //            obs
        //                .Skip(1)
        //                .Buffer(bufferTime)
        //                .Select(x => x.SelectMany(y => y)))
        //        .ToReactiveSet();
        //}

        //public static ICollectedReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, int bufferCount) {
        //    if (set == null) throw new ArgumentNullException(nameof(set));

        //    var obs = set.AsObservable();
        //    return obs.FirstAsync().Concat(obs.Skip(1).Buffer(bufferCount).Select(x => x.SelectMany(y => y))).ToReactiveSet();
        //}

#if net462
        public static IReactiveSet<T> ObserveOnDispatcher<T>(this IReactiveSet<T> set) {
            if (set == null) throw new ArgumentNullException(nameof(set));

            return set.AsObservable().ObserveOnDispatcher().ToSet(set.Contains);
        }
#endif
    }
}
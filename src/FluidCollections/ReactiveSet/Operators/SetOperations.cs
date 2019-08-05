using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<T> Union<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => !second.Contains(x))));
            var obs2 = second.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => !first.Contains(x))));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) || second.Contains(x));
        }

        public static IReactiveSet<T> Intersection<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => second.Contains(x))));
            var obs2 = second.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => first.Contains(x))));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) && second.Contains(x));
        }

        public static IReactiveSet<T> Except<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => !second.Contains(x))));
            var obs2 = second.AsObservable()
                .Select(change => {
                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                        return new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, change.Items);
                    }
                    else {
                        return new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, change.Items.Where(first.Contains));
                    }
                });

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) && !second.Contains(x));
        }

        public static IReactiveSet<T> SymmetricExcept<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first.Union(second).Except(first.Intersection(second));
        }
    }
}
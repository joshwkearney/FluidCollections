using System;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<T> Union<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(change => new ReactiveSetChange<T>(change.ChangeReason, change.Items.Where(x => !second.Contains(x))));
            var obs2 = second.AsObservable().Select(change => changes.Where(x => !first.Contains(x.Value)));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) || second.Contains(x));
        }

        public static IReactiveSet<T> Intersection<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(changes => changes.Where(x => second.Contains(x.Value)));
            var obs2 = second.AsObservable().Select(changes => changes.Where(x => first.Contains(x.Value)));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) && second.Contains(x));
        }

        public static IReactiveSet<T> Except<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(changes => changes.Where(x => !second.Contains(x.Value)));
            var obs2 = second.AsObservable().Select(
                changes => changes.Where(x =>
                    x.ChangeReason == ReactiveSetChangeReason.Add ||
                    x.ChangeReason == ReactiveSetChangeReason.Remove && first.Contains(x.Value)
                )
                .Select(x => {
                    if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                        return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                    }
                    else {
                        return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                    }
                }));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) && !second.Contains(x));
        }

        public static IReactiveSet<T> SymmetricExcept<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var obs1 = first.AsObservable().Select(changes => changes.Select(x => {
                if (!second.Contains(x.Value)) {
                    return x;
                }

                if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                    return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                }
                else {
                    return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                }
            }));

            var obs2 = second.AsObservable().Select(changes => changes.Select(x => {
                if (!first.Contains(x.Value)) {
                    return x;
                }

                if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                    return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                }
                else {
                    return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                }
            }));

            return obs1.Merge(obs2).ToReactiveSet(x => first.Contains(x) ^ second.Contains(x));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IObservable<TResult> Aggregate<T, TResult>(this IReactiveSet<T> set, TResult seed, Func<TResult, T, TResult> addFunc, Func<TResult, T, TResult> removeFunc) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (addFunc == null) throw new ArgumentNullException(nameof(addFunc));
            if (removeFunc == null) throw new ArgumentNullException(nameof(removeFunc));

            return Observable.Create<TResult>(observer => {
                var current = seed;
                observer.OnNext(current);

                IDisposable sub = null;
                sub = set
                    .AsObservable()
                    .Subscribe(
                        x => {
                            foreach (var change in x) {
                                try {
                                    if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                                        current = addFunc(current, change.Value);
                                    }
                                    else {
                                        current = removeFunc(current, change.Value);
                                    }
                                }
                                catch (Exception ex) {
                                    observer.OnError(ex);
                                    sub?.Dispose();
                                    return;
                                }
                            }

                            observer.OnNext(current);
                        },
                        observer.OnError,
                        observer.OnCompleted
                    );

                return sub;
            })
            .DistinctUntilChanged();
        }

        public static IObservable<IImmutableSet<T>> ToImmutableSets<T>(this IReactiveSet<T> set) => set.ToImmutableSets(() => ImmutableHashSet<T>.Empty);

        public static IObservable<ImmutableSortedSet<T>> ToImmutableSortedSets<T>(this IReactiveSet<T> set) {
            return set.Aggregate(ImmutableSortedSet<T>.Empty, (x, y) => x.Add(y), (x, y) => x.Remove(y));
        }

        private static IObservable<IImmutableSet<T>> ToImmutableSets<T>(this IReactiveSet<T> set, Func<IImmutableSet<T>> setFactory) {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (setFactory == null) throw new ArgumentNullException(nameof(setFactory));

            return set.Aggregate(ImmutableHashSet<T>.Empty, (x, y) => x.Add(y), (x, y) => x.Remove(y));
        }
    }
}
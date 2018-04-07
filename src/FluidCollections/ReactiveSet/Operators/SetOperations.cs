using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static IReactiveSet<T> Union<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first
                .SelectManyChanges(
                    second,
                    changes => changes.Where(x => !second.Contains(x.Value)),
                    changes => changes.Where(x => !first.Contains(x.Value))
                )
                .ToReactiveSet(x => first.Contains(x) || second.Contains(x));
        }

        public static IReactiveSet<T> Intersection<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first
                .SelectManyChanges(
                    second,
                    changes => changes.Where(x => second.Contains(x.Value)),
                    changes => changes.Where(x => first.Contains(x.Value))
                )
                .ToReactiveSet(x => first.Contains(x) && second.Contains(x));
        }

        public static IReactiveSet<T> Except<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first
                .SelectManyChanges(
                    second,
                    changes => changes.Where(x => !second.Contains(x.Value)),
                    changes => changes.Where(x => 
                        x.ChangeReason == ReactiveSetChangeReason.Add && first.Contains(x.Value)
                        || x.ChangeReason == ReactiveSetChangeReason.Remove && second.Contains(x.Value)
                    )
                    .Select(x => {
                        if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                        }
                        else {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                        }
                    })
                )
                .ToReactiveSet(x => first.Contains(x) && !second.Contains(x));
        }

        public static IReactiveSet<T> SymmetricExcept<T>(this IReactiveSet<T> first, IReactiveSet<T> second) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first
                .SelectManyChanges(
                    second,
                    changes => changes.Select(x => {
                        if (!second.Contains(x.Value)) {
                            return x;
                        }

                        if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                        }
                        else {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                        }
                    }),
                    changes => changes.Select(x => {
                        if (!first.Contains(x.Value)) {
                            return x;
                        }

                        if (x.ChangeReason == ReactiveSetChangeReason.Add) {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Remove);
                        }
                        else {
                            return new ReactiveSetChange<T>(x.Value, ReactiveSetChangeReason.Add);
                        }
                    })
                )
                .ToReactiveSet(x => first.Contains(x) ^ second.Contains(x));
        }

        private static IObservable<IEnumerable<ReactiveSetChange<T>>> SelectManyChanges<T>(
            this IReactiveSet<T> first, 
            IReactiveSet<T> second, 
            Func<IEnumerable<ReactiveSetChange<T>>, IEnumerable<ReactiveSetChange<T>>> selector1, 
            Func<IEnumerable<ReactiveSetChange<T>>, IEnumerable<ReactiveSetChange<T>>> selector2) {
            
            return Observable.Create<IEnumerable<ReactiveSetChange<T>>>(observer => {
                var lockObj = new object();

                bool isFirstCompleted = false;
                bool isSecondCompleted = false;

                var sub1 = first.AsObservable().Subscribe(
                    changes => {
                        lock (lockObj) {
                            var newChanges = selector1(changes).ToArray();

                            if (newChanges.Any()) {
                                observer.OnNext(newChanges);
                            }
                        }
                    },
                    observer.OnError,
                    () => {
                        lock (lockObj) {
                            isFirstCompleted = true;

                            if (isSecondCompleted) {
                                observer.OnCompleted();
                            }
                        }
                    }
                );

                var sub2 = second.AsObservable().Subscribe(
                    changes => {
                        lock (lockObj) {
                            var newChanges = selector2(changes).ToArray();

                            if (newChanges.Any()) {
                                observer.OnNext(newChanges);
                            }
                        }
                    },
                    observer.OnError,
                    () => {
                        lock (lockObj) {
                            isSecondCompleted = true;

                            if (isFirstCompleted) {
                                observer.OnCompleted();
                            }
                        }
                    }
                );

                return () => {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            });
        }
    }
}
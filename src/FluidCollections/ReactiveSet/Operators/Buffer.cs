using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace FluidCollections {
    public static partial class ReactiveSetExtensions {
        public static ICollectedReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, int count) {
            if (set == null) {
                throw new ArgumentNullException(nameof(set));
            }

            var obs = Observable.Create<ReactiveSetChange<T>>(observer => {
                var isFirst = true;

                return set.AsObservable()
                    .Where(x => {
                        if (isFirst) {
                            isFirst = false;
                            observer.OnNext(x);
                            return false;
                        }
                        else {
                            return true;
                        }
                    })
                    .Buffer(count)
                    .Subscribe(
                        x => {
                            var added = new HashSet<T>();
                            var removed = new HashSet<T>();

                            foreach (var change in x) {
                                if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                                    foreach (var item in change.Items) {
                                        if (removed.Contains(item)) {
                                            removed.Remove(item);
                                        }
                                        else {
                                            added.Add(item);
                                        }
                                    }
                                }
                                else {
                                    foreach (var item in change.Items) {
                                        if (added.Contains(item)) {
                                            added.Remove(item);
                                        }
                                        else {
                                            removed.Add(item);
                                        }
                                    }
                                }
                            }

                            if (added.Any()) {
                                observer.OnNext(new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, added));
                            }

                            if (removed.Any()) {
                                observer.OnNext(new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, removed));
                            }
                        },
                        observer.OnError,
                        observer.OnCompleted
                    );
            });

            return obs.ToReactiveSet();
        }

        public static ICollectedReactiveSet<T> Buffer<T>(this IReactiveSet<T> set, TimeSpan time) {
            if (set == null) {
                throw new ArgumentNullException(nameof(set));
            }

            var obs = Observable.Create<ReactiveSetChange<T>>(observer => {
                var isFirst = true;

                return set.AsObservable()
                    .Where(x => {
                        if (isFirst) {
                            isFirst = false;
                            observer.OnNext(x);
                            return false;
                        }
                        else {
                            return true;
                        }
                    })
                    .Buffer(time)
                    .Subscribe(
                        x => {
                            var added = new HashSet<T>();
                            var removed = new HashSet<T>();

                            foreach (var change in x) {
                                if (change.ChangeReason == ReactiveSetChangeReason.Add) {
                                    foreach (var item in change.Items) {
                                        if (removed.Contains(item)) {
                                            removed.Remove(item);
                                        }
                                        else {
                                            added.Add(item);
                                        }
                                    }
                                }
                                else {
                                    foreach (var item in change.Items) {
                                        if (added.Contains(item)) {
                                            added.Remove(item);
                                        }
                                        else {
                                            removed.Add(item);
                                        }
                                    }
                                }
                            }

                            if (added.Any()) {
                                observer.OnNext(new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, added));
                            }

                            if (removed.Any()) {
                                observer.OnNext(new ReactiveSetChange<T>(ReactiveSetChangeReason.Remove, removed));
                            }
                        },
                        observer.OnError,
                        observer.OnCompleted
                    );
            });

            return obs.ToReactiveSet();
        }
    }
}

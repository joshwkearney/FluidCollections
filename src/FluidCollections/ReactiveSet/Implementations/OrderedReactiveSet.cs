using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace FluidCollections {
    public class OrderedReactiveSet<T> : IOrderedReactiveSet<T> {
        private readonly OrderedSet<T> list;
        private readonly Subject<ReactiveSetChange<T>> subject = new Subject<ReactiveSetChange<T>>();
        private readonly IDisposable subscriptions;
        private readonly object syncRoot = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => this.list.Count;

        public T this[int index] => this.list[index];

        public T Min => this.list.Min;

        public T Max => this.list.Max;

        public OrderedReactiveSet(IObservable<ReactiveSetChange<T>> changes) : this(changes, Comparer<T>.Default) { }

        internal OrderedReactiveSet(IObservable<ReactiveSetChange<T>> changes, IComparer<T> comparer) {
            this.list = new OrderedSet<T>(comparer);

            this.subscriptions = changes.Subscribe(
                ProcessIncomingChange,
                this.subject.OnError,
                this.subject.OnCompleted
            );
        }

        public IObservable<ReactiveSetChange<T>> AsObservable() {
            return Observable.Create<ReactiveSetChange<T>>(observer => {
                lock (this.syncRoot) {
                    var initial = new ReactiveSetChange<T>(ReactiveSetChangeReason.Add, this.list);
                    observer.OnNext(initial);

                    return this.subject.Subscribe(observer);
                }
            });
        }

        public bool Contains(T item) => this.list.Contains(item);

        public void Dispose() {
            this.subscriptions.Dispose();
        }

        private void ProcessIncomingChange(ReactiveSetChange<T> changes) {
            // Update the local set first
            lock (this.syncRoot) {
                // Signal observers of the change
                this.subject.OnNext(changes);

                if (changes.ChangeReason == ReactiveSetChangeReason.Add) {
                    foreach (var item in changes.Items) {
                        this.list.Add(item);
                    }
                }
                else {
                    foreach (var item in changes.Items) {
                        this.list.Remove(item);
                    }
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Min)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Max)));
            }
        }

        public int IndexOf(T item) => this.list.IndexOf(item);
    }
}
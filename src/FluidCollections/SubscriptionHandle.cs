using System;

namespace FluidCollections {
    internal class SubscriptionHandle : IDisposable {
        private Action action;
        private bool isDisposed = false;

        public SubscriptionHandle(Action action) {
            this.action = action;
        }

        public void Dispose() {
            if (!this.isDisposed) {
                this.action();
                this.isDisposed = true;
            }
        }
    }
}
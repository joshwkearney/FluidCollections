namespace FluidCollections {

    public interface IOrderedReactiveSet<T> : ICollectedReactiveSet<T> {
        T this[int index] { get; }

        T Min { get; }

        T Max { get; }

        int IndexOf(T item);
    }
}
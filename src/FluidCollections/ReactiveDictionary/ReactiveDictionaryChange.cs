using System.Collections.Generic;

namespace FluidCollections {
    public enum ReactiveDictionaryChangeReason {
        AddOrUpdate = 0,
        Remove = 2,
    }

    public class ReactiveDictionaryChange<TKey, TValue> {
        public ReactiveDictionaryChangeReason ChangeReason { get; }

        public TKey Key { get; }

        public TValue Value { get; }

        public KeyValuePair<TKey, TValue> Pair => new KeyValuePair<TKey, TValue>(this.Key, this.Value);

        public ReactiveDictionaryChange(KeyValuePair<TKey, TValue> pair, ReactiveDictionaryChangeReason reason) {
            this.ChangeReason = reason;
            this.Key = pair.Key;
            this.Value = pair.Value;
        }

        public ReactiveDictionaryChange(TKey key, TValue value, ReactiveDictionaryChangeReason reason) {
            this.ChangeReason = reason;
            this.Key = key;
            this.Value = value;
        }
    }
}
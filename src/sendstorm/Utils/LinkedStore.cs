using System.Collections;
using System.Collections.Generic;

namespace Sendstorm.Utils
{
    internal class LinkedStore<TKey, TValue> : IEnumerable<TValue>
    {
        public static readonly LinkedStore<TKey, TValue> Empty = new LinkedStore<TKey, TValue>();

        public LinkedStore<TKey, TValue> Next;

        public TValue Value;

        public TKey Key;

        public bool IsEmpty;

        private LinkedStore() { this.IsEmpty = true; }

        public LinkedStore(TKey key, TValue value)
        : this(key, value, Empty)
        { }

        private LinkedStore(TKey key, TValue value, LinkedStore<TKey, TValue> next)
        {
            this.Value = value;
            this.Key = key;
            this.Next = next;
            this.IsEmpty = false;
        }

        public LinkedStore<TKey, TValue> Add(TKey key, TValue paramValue)
        {
            return this.Next == null ? new LinkedStore<TKey, TValue>(key, paramValue, Empty) :
                new LinkedStore<TKey, TValue>(key, paramValue, this);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public IEnumerator<TValue> GetEnumerator() => new OrderedLinkedStoreEnumerator(this);

        private class OrderedLinkedStoreEnumerator : IEnumerator<TValue>
        {
            private readonly LinkedStore<TKey, TValue> init;
            private LinkedStore<TKey, TValue> current;

            public OrderedLinkedStoreEnumerator(LinkedStore<TKey, TValue> init)
            {
                this.init = init;
            }

            public bool MoveNext()
            {
                if (this.current == null && this.init.Next != null)
                    this.current = this.init;
                else if (this.current?.Next != null && this.current.Next != Empty)
                    this.current = this.current.Next;
                else
                    return false;

                return true;
            }

            public void Reset() => this.current = this.init;

            public TValue Current => this.current.Value;

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
                // do nothing
            }
        }
    }
}

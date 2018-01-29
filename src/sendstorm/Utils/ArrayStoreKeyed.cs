using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sendstorm.Utils
{
    internal class ArrayStoreKeyed<TKey, TValue> : IEnumerable<TValue>
    {
        public static readonly ArrayStoreKeyed<TKey, TValue> Empty = new ArrayStoreKeyed<TKey, TValue>();

        public KeyValue<TKey, TValue>[] Repository { get; }

        public TValue Last => this.Repository[this.Length - 1].Value;

        public int Length { get; }

        private ArrayStoreKeyed(KeyValue<TKey, TValue> item, KeyValue<TKey, TValue>[] old)
        {
            if (old.Length == 0)
                this.Repository = new[] { item };
            else
            {
                this.Repository = new KeyValue<TKey, TValue>[old.Length + 1];
                Array.Copy(old, this.Repository, old.Length);
                this.Repository[old.Length] = item;
            }

            this.Length = old.Length + 1;
        }

        internal ArrayStoreKeyed(KeyValue<TKey, TValue>[] initial)
        {
            this.Repository = initial;
            this.Length = initial.Length;
        }

        internal ArrayStoreKeyed(TKey key, TValue value)
        {
            this.Repository = new[] { new KeyValue<TKey, TValue>(key, value) };
            this.Length = 1;
        }

        public ArrayStoreKeyed()
        {
            this.Repository = new KeyValue<TKey, TValue>[0];
        }

        public TValue this[int i] => this.Repository[i].Value;

        public ArrayStoreKeyed<TKey, TValue> Add(TKey key, TValue value) =>
           new ArrayStoreKeyed<TKey, TValue>(new KeyValue<TKey, TValue>(key, value), this.Repository);

        public ArrayStoreKeyed<TKey, TValue> AddOrUpdate(TKey key, TValue value, bool allowUpdate = true)
        {
            var length = this.Repository.Length;
            var count = length - 1;
            while (count >= 0 && !Equals(this.Repository[count].Key, key)) count--;

            if (count == -1)
                return this.Add(key, value);

            if (!allowUpdate)
                return this;

            var newRepository = new KeyValue<TKey, TValue>[length];
            Array.Copy(this.Repository, newRepository, length);
            newRepository[count] = new KeyValue<TKey, TValue>(key, value);
            return new ArrayStoreKeyed<TKey, TValue>(newRepository);
        }

        internal ArrayStoreKeyed<TKey, TValue> WhereOrDefault(Func<KeyValue<TKey, TValue>, bool> predicate)
        {
            var initial = this.Repository.Where(predicate).ToArray();
            return initial.Length == 0 ? null : new ArrayStoreKeyed<TKey, TValue>(initial);
        }

        public TValue GetOrDefault(TKey key)
        {
            var length = this.Repository.Length;
            for (var i = 0; i < length; i++)
            {
                var item = this.Repository[i];
                if (item.Key.Equals(key))
                    return item.Value;
            }

            return default(TValue);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.Repository.GetEnumerator();

        public IEnumerator<TValue> GetEnumerator()
        {
            for (var i = 0; i < this.Length; i++)
                yield return this.Repository[i].Value;
        }
    }

    internal class KeyValue<TKey, TValue>
    {
        public TKey Key { get; set; }

        public TValue Value { get; set; }

        public KeyValue(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}

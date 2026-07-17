using System;
using System.Collections.Generic;
using System.Collections;

namespace MobileCore
{
    public class BoundedBuffer<T> : IReadOnlyBuffer, IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private readonly int capacity;
        private readonly List<T> items = new List<T>();

        public int Count => items.Count;
        public int Capacity => capacity;
        public bool HasFreeSlot => Count < capacity;
        public T this[int index] => items[index];
        public event Action OnChanged;

        public BoundedBuffer(int capacity)
        {
            this.capacity = capacity;
        }

        public bool TryAdd(T item)
        {
            if (!HasFreeSlot)
            {
                return false;
            }
            items.Add(item);
            OnChanged?.Invoke();
            return true;
        }

        public bool TryRemove(T item)
        {
            if (!items.Contains(item))
            {
                return false;
            }
            items.Remove(item);
            OnChanged?.Invoke();
            return true;

        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }
    }
}
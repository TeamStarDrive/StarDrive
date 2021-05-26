using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ship_Game.Utils
{
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class SafeArray<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        T[] Items = Empty<T>.Array;
        public int Count { get; private set; }
        public int Capacity => Items.Length;
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public object SyncRoot => this;
        public bool IsSynchronized => true;

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        // Separated throw from this[] to enable MS IL inlining
        void ThrowIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public T this[int index] 
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                return Items[index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                Items[index] = value;
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public int Add(object value)
        {
            int count = Count;
            Add((T)value);
            return count;
        }

        public void Clear()
        {
            
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
    }
}

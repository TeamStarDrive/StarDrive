using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ship_Game.Utils
{
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class SafeArray<T> : SafeArrayBase<T>, IArray<T>, IList<T>, 
        IReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        object Sync = new object();
        public int Capacity { get { lock (Sync) { return Items.Length; } } }
        public bool IsReadOnly  => false;
        public bool IsFixedSize => false;
        public bool IsEmpty     => Count == 0;
        public bool NotEmpty    => Count != 0;
        public object SyncRoot => Sync;
        public bool IsSynchronized => true;

        public SafeArray()
        {
        }

        public SafeArray(ICollection<T> collection) : base(collection)
        {
        }

        public T this[int index]
        {
            get
            {
                lock (Sync)
                {
                    return Get(index);
                }
            }
            set
            {
                lock (Sync)
                {
                    Set(index, value);
                }
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public void Add(T item)
        {
            lock (Sync)
            {
                AddUnlocked(item);
            }
        }

        public int Add(object value)
        {
            lock (Sync)
            {
                int count = Count;
                AddUnlocked((T)value);
                return count;
            }
        }

        public void Clear()
        {
            if (Count == 0)
                return;
            lock (Sync)
            {
                ClearUnlocked();
            }
        }

        public bool Contains(T item)
        {
            if (Count == 0)
                return false;
            lock (Sync)
            {
                return ContainsUnlocked(item);
            }
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count == 0)
                return;
            lock (Sync)
            {
                CopyToUnlocked(array, arrayIndex);
            }
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public void Insert(int index, T item)
        {
            lock (Sync)
            {
                InsertUnlocked(index, item);
            }
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public int IndexOf(T item)
        {
            lock (Sync)
            {
                return IndexOfUnlocked(item);
            }
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }
        
        public void RemoveAt(int index)
        {
            lock (Sync)
            {
                RemoveAtUnlocked(index);
            }
        }

        public bool Remove(T item)
        {
            lock (Sync)
            {
                return RemoveUnlocked(item);
            }
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public bool RemoveSwapLast(T item)
        {
            lock (Sync)
            {
                return RemoveSwapLastUnlocked(item);
            }
        }

        public void RemoveAtSwapLast(int index)
        {
            lock (Sync)
            {
                RemoveAtSwapLastUnlocked(index);
            }
        }

        public T PopFirst()
        {
            lock (Sync)
            {
                return PopFirstUnlocked();
            }
        }

        public T PopLast()
        {
            lock (Sync)
            {
                return PopLastUnlocked();
            }
        }

        public bool TryPopLast(out T item)
        {
            lock (Sync)
            {
                return TryPopLastUnlocked(out item);
            }
        }
        
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // Provides really crude safe enumeration of this SafeArray
        // Simply acquires a lock during MoveNext(), does not guarantee states
        public struct Enumerator : IEnumerator<T>
        {
            int Index;
            readonly SafeArray<T> Arr;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SafeArray<T> arr)
            {
                Index = 0;
                Arr = arr;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                lock (Arr.Sync)
                {
                    if (Index >= Arr.Count)
                        return false;
                    Current = Arr.Items[Index++];
                    return true;
                }
            }
            public void Reset()
            {
                Index = 0;
            }
        }
    }
}

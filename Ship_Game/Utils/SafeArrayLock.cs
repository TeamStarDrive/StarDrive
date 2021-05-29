using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ship_Game.Utils
{
    // TODO: This is 50% slower than a regular lock() statement
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class SafeArrayLock<T> : SafeArrayBase<T>, IArray<T>, IList<T>, 
        IReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        struct ScopedReadLock : IDisposable
        {
            readonly ReaderWriterLockSlim Lock;
            public ScopedReadLock(ReaderWriterLockSlim theLock)
            {
                (Lock = theLock).EnterReadLock();
            }
            public void Dispose()
            {
                Lock.ExitReadLock();
            }
        }

        struct ScopedWriteLock : IDisposable
        {
            readonly ReaderWriterLockSlim Lock;
            public ScopedWriteLock(ReaderWriterLockSlim theLock)
            {
                (Lock = theLock).EnterWriteLock();
            }
            public void Dispose()
            {
                Lock.ExitWriteLock();
            }
        }

        public int Capacity { get { using(new ScopedReadLock(Lock)) { return Items.Length; } } }
        public bool IsReadOnly  => false;
        public bool IsFixedSize => false;
        public bool IsEmpty     => Count == 0;
        public bool NotEmpty    => Count != 0;
        public object SyncRoot => Lock;
        public bool IsSynchronized => true;

        public SafeArrayLock()
        {
        }

        public SafeArrayLock(ICollection<T> collection) : base(collection)
        {
        }

        public T this[int index]
        {
            get
            {
                using(new ScopedReadLock(Lock))
                    return Get(index);
            }
            set
            {
                using(new ScopedWriteLock(Lock))
                    Set(index, value);
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public void Add(T item)
        {
            using(new ScopedWriteLock(Lock))
                AddUnlocked(item);
        }

        public int Add(object value)
        {
            using(new ScopedWriteLock(Lock))
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
            using(new ScopedWriteLock(Lock))
                ClearUnlocked();
        }

        public bool Contains(T item)
        {
            if (Count == 0)
                return false;
            using(new ScopedReadLock(Lock))
                return ContainsUnlocked(item);
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count == 0)
                return;
            using(new ScopedReadLock(Lock))
                CopyToUnlocked(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public void Insert(int index, T item)
        {
            using(new ScopedWriteLock(Lock))
                InsertUnlocked(index, item);
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public int IndexOf(T item)
        {
            using(new ScopedReadLock(Lock))
                return IndexOfUnlocked(item);
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }
        
        public void RemoveAt(int index)
        {
            using(new ScopedWriteLock(Lock))
                RemoveAtUnlocked(index);
        }

        public bool Remove(T item)
        {
            using(new ScopedWriteLock(Lock))
                return RemoveUnlocked(item);
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public bool RemoveSwapLast(T item)
        {
            using(new ScopedWriteLock(Lock))
                return RemoveSwapLastUnlocked(item);
        }

        public void RemoveAtSwapLast(int index)
        {
            using(new ScopedWriteLock(Lock))
                RemoveAtSwapLastUnlocked(index);
        }

        public T PopFirst()
        {
            using(new ScopedWriteLock(Lock))
                return PopFirstUnlocked();
        }

        public T PopLast()
        {
            using(new ScopedWriteLock(Lock))
                return PopLastUnlocked();
        }

        public bool TryPopLast(out T item)
        {
            using(new ScopedWriteLock(Lock))
                return TryPopLastUnlocked(out item);
        }
        
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // Provides really crude safe enumeration of this SafeArray
        // Simply acquires a lock during MoveNext(), does not guarantee states
        public struct Enumerator : IEnumerator<T>
        {
            int Index;
            readonly SafeArrayLock<T> Arr;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SafeArrayLock<T> arr)
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
                using(new ScopedReadLock(Arr.Lock))
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

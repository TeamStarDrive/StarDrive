using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ship_Game
{
    public sealed class BatchRemovalCollection<T> : Array<T>, IDisposable
    {
        ConcurrentStack<T> PendingRemovals;
        ReaderWriterLockSlim ThisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public BatchRemovalCollection()
        {
            PendingRemovals = new ConcurrentStack<T>();
        }
        public BatchRemovalCollection(IReadOnlyList<T> listToCopy) : this()
        {
            base.AddRange(listToCopy);
        }

        // Acquires a deterministic Read Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireReadLock())
        //     item = list.First();
        public ScopedReadLock AcquireReadLock()
        {
            return new ScopedReadLock(ThisLock);
        }

        // Acquires a deterministic Write Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireWriteLock())
        //     list.Add(item);
        public ScopedWriteLock AcquireWriteLock()
        {
            return new ScopedWriteLock(ThisLock);
        }

        public void ApplyPendingRemovals()
        {
            if (PendingRemovals.IsEmpty) return;
            using (AcquireWriteLock())
            {
                while (!PendingRemovals.IsEmpty)
                {
                    PendingRemovals.TryPop(out var result);
                    base.Remove(result);
                }
            }
        }

        public void QueuePendingRemoval(T item)
        {
            PendingRemovals.Push(item);
        }

        public void ClearPendingRemovals()
        {
            PendingRemovals.Clear();
        }

        public new void Add(T item)
        {
            ThisLock.EnterWriteLock();
            base.Add(item);
            ThisLock.ExitWriteLock();
        }

        public new void Clear()
        {
            ThisLock.EnterWriteLock();
            base.Clear();
            ThisLock.ExitWriteLock();
            PendingRemovals?.Clear();
        }

        public void ClearAndRecycle()
        {
            ThisLock.EnterWriteLock();
            PendingRemovals = new ConcurrentStack<T>(this); 
            base.Clear();
            ThisLock.ExitWriteLock();
        }

        public new bool Remove(T item)
        {
            ThisLock.EnterWriteLock();
            bool found = base.Remove(item);
            ThisLock.ExitWriteLock();
            return found;
        }

        public new bool Contains(T item)
        {
            ThisLock.EnterReadLock();
            bool result = base.Contains(item);
            ThisLock.ExitReadLock();
            return result;
        }

        public new void AddRange(IReadOnlyList<T> list)
        {
            ThisLock.EnterWriteLock();
            base.AddRange(list);
            ThisLock.ExitWriteLock();
        }

        // This is used to reduce the time locks are held. Downside is higher memory usage
        // ReadLock is acquired and base.ToArray() called
        public T[] AtomicCopy()
        {
            using (new ScopedReadLock(ThisLock))
                return base.ToArray();
        }

        public new T[] ToArray()
        {
            using (new ScopedReadLock(ThisLock))
                return base.ToArray();
        }

        public T RecycleObject(Action<T> action)
        {
            if (!PendingRemovals.TryPop(out T item))
                return item;
            action.Invoke(item);
            return item;
        }

        public void UnsafeAdd(T item) => base.Add(item);

        public void UnsafeRemove(T item) => base.Remove(item);

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~BatchRemovalCollection() { Destroy(); }

        void Destroy()
        {
            PendingRemovals?.Clear();
            PendingRemovals = null;
            base.Clear();
            ThisLock?.Dispose(ref ThisLock);            
        }
    }
}
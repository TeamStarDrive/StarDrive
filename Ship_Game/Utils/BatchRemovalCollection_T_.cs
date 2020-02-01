using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ship_Game
{
    public sealed class BatchRemovalCollection<T> : Array<T>, IDisposable
    {
        private ConcurrentStack<T> PendingRemovals;
        private ReaderWriterLockSlim ThisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public BatchRemovalCollection()
        {
            PendingRemovals = new ConcurrentStack<T>();
        }
        public BatchRemovalCollection(ICollection<T> listToCopy, bool noRemoveQueue = false)
        {
            base.AddRange(listToCopy);
            if (noRemoveQueue) return;
            PendingRemovals = new ConcurrentStack<T>();
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

        public void ClearAdd(IEnumerable<T> item)
        {
            ThisLock.EnterWriteLock();
            base.Clear();
            base.AddRange(item);
            ThisLock.ExitWriteLock();
        }

        public new bool Contains(T item)
        {
            ThisLock.EnterReadLock();
            bool result = base.Contains(item);
            ThisLock.ExitReadLock();
            return result;
        }

        public new void AddRange(ICollection<T> collection)
        {
            ThisLock.EnterWriteLock();
            base.AddRange(collection);
            ThisLock.ExitWriteLock();
        }

        // This is used to reduce the time locks are held. Downside is higher memory usage
        // ReadLock is acquired and base.ToArray() called
        public T[] AtomicCopy()
        {
            ThisLock.EnterReadLock();
            var arr = ToArray();
            ThisLock.ExitReadLock();
            return arr;
        }

        public T RecycleObject()
        {
            if (!PendingRemovals.TryPop(out T item))
                return item;
            (item as Empire.InfluenceNode)?.Wipe();
            return item;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~BatchRemovalCollection() { Destroy(); }

        private void Destroy()
        {
            PendingRemovals?.Clear();
            PendingRemovals = null;
            base.Clear();
            ThisLock?.Dispose(ref ThisLock);            
        }
    }
}
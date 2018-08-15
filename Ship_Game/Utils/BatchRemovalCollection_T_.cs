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
        public BatchRemovalCollection(bool noQueueForRemoval)
        {
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
        public void ApplyPendingRemovals(bool saveForPooling)
        {
            using (AcquireWriteLock())
            {
                if (saveForPooling)
                {
                    foreach (T item in PendingRemovals.ToArray())
                    {
                        Remove(item);
                    }
                    return;
                }
                while (!PendingRemovals.IsEmpty)
                {
                    PendingRemovals.TryPop(out var result);
                    Remove(result);
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

        public bool IsPendingRemoval(T item)
        {
            return PendingRemovals.Contains(item);
        }

        public int PendingRemovalCount => PendingRemovals.Count;
        
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
        public void ClearAll()
        {
            ThisLock.EnterWriteLock();
            base.Clear();
            ThisLock.ExitWriteLock();
            PendingRemovals?.Clear();
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
        //Contains<TSource>(this IEnumerable<TSource> source, TSource value);
        //Contains<TSource>(this IEnumerable<TSource> source, TSource value);
        //public bool Contains<T>(T item)
        //{
        //    thisLock.EnterReadLock();
        //    var result = Contains<T>(item ); //.Contains<T>(item);
        //    thisLock.ExitReadLock();
        //    return result;
        //}

        public new void AddRange(ICollection<T> collection)
        {
            ThisLock.EnterWriteLock();
            base.AddRange(collection);
            ThisLock.ExitWriteLock();
        }

        public new void AddRange(IEnumerable<T> enumerable)
        {
            ThisLock.EnterWriteLock();
            base.AddRange(enumerable);
            ThisLock.ExitWriteLock();
        }
        // to use this:
        // somelist.foreach(action => { do some action with action},false,false,false);
        // to enable parallel mode set the last false to "true'
        public void ForEach(Action<T> action, bool performActionOnClones = true, bool asParallel = true, bool inParallel = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            var wrapper = new Action<T>(obj =>
            {
                try
                {
                    action(obj);
                }
                catch (ArgumentNullException)
                {
                    //if a null gets into the list then swallow an ArgumentNullException so we can continue adding
                }
            });
            if (performActionOnClones)
            {
                ThisLock.EnterReadLock();
                var clones = Clone(asParallel: asParallel);
                if (asParallel)
                {
                    clones.AsParallel().ForAll(wrapper);
                }
                else if (inParallel)
                {
                    System.Threading.Tasks.Parallel.ForEach(clones, wrapper);
                }
                else
                {
                    clones.ForEach(wrapper);
                }
                ThisLock.ExitReadLock();
            }
            else
            {
                ThisLock.EnterReadLock();
                {
                    if (asParallel)
                    {
                        this.AsParallel().ForAll(wrapper);
                    }
                    else if (inParallel)
                    {
                        System.Threading.Tasks.Parallel.ForEach(this, wrapper);
                    }
                    else
                    {
                        base.ForEach(wrapper);
                    }
                }
                ThisLock.ExitReadLock();
            }
        }

        public void ForAll(Action<T> action, bool performActionOnClones = true, bool asParallel = true, bool inParallel = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            var wrapper = new Action<T>(obj =>
            {
                try
                {
                    action(obj);
                }
                catch (ArgumentNullException)
                {
                    //if a null gets into the list then swallow an ArgumentNullException so we can continue adding
                }
            });
            if (performActionOnClones)
            {
                var clones = Clone(asParallel: asParallel);
                if (asParallel)
                {
                    clones.AsParallel().ForAll(wrapper);
                }
                else if (inParallel)
                {
                    System.Threading.Tasks.Parallel.ForEach(clones, wrapper);
                }
                else
                {
                    clones.ForEach(wrapper);
                }
            }
            else
            {
                ThisLock.EnterReadLock();
                {
                    if (asParallel)
                    {
                        this.AsParallel().ForAll(wrapper);
                    }
                    else if (inParallel)
                    {
                        System.Threading.Tasks.Parallel.ForEach(this, wrapper);
                    }
                    else
                    {
                        ForEach(wrapper);
                    }
                }
                ThisLock.ExitReadLock();
            }
        }
        public Array<T> Clone(bool asParallel = true)
        {
            ThisLock.EnterReadLock();
            var copy = asParallel ? new Array<T>(this.AsParallel()) : new Array<T>(this);
            ThisLock.ExitReadLock();
            return copy;
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
        
        public bool TryReuseItem(out T item)
        {
            if (!PendingRemovals.TryPop(out item))
                return false;
            (item as Empire.InfluenceNode)?.Wipe();
            return true;
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
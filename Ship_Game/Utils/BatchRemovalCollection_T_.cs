using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        public BatchRemovalCollection(ICollection<T> listToCopy)
        {
            base.AddRange(listToCopy);
        }

        // Acquires a deterministic Read Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireReadLock())
        //     item = list.First();
        public ScopedReadLock AcquireReadLock()
        {
            return new ScopedReadLock(this.ThisLock);
        }

        // Acquires a deterministic Write Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireWriteLock())
        //     list.Add(item);
        public ScopedWriteLock AcquireWriteLock()
        {
            return new ScopedWriteLock(this.ThisLock);
        }

        public void ApplyPendingRemovals()
        {
            if (this.PendingRemovals.IsEmpty) return;
            using (AcquireWriteLock())
            {
                while (!this.PendingRemovals.IsEmpty)
                {
                    this.PendingRemovals.TryPop(out var result);
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
                    foreach (T item in this.PendingRemovals.ToArray())
                    {
                        Remove(item);
                    }
                    return;
                }
                while (!this.PendingRemovals.IsEmpty)
                {
                    this.PendingRemovals.TryPop(out var result);
                    Remove(result);
                }
            }
        }
        public void QueuePendingRemoval(T item)
        {
            this.PendingRemovals.Push(item);
        }
        public void ClearPendingRemovals()
        {
            this.PendingRemovals.Clear();
        }

        public bool IsPendingRemoval(T item)
        {
            return this.PendingRemovals.Contains(item);
        }

        public new void Add(T item)
        {
            this.ThisLock.EnterWriteLock();
            base.Add(item);
            this.ThisLock.ExitWriteLock();
        }
        //public Array<T> Get()
        //{
        //    var list = new Array<T>();
        //    thisLock.EnterReadLock();
        //    list.AddRange(this);// = base.ToList();
        //    thisLock.ExitReadLock();
        //    return list;// this as Array<T>;
        //}
        
        //public new Enumerator GetEnumerator()
        //{
        //    //thisLock.EnterReadLock(); // Removed by RedFox, this doesn't
        //    var result = base.GetEnumerator();
        //    //thisLock.ExitReadLock();
        //    return result;
        //}
        public new void Clear()
        {
            this.ThisLock.EnterWriteLock();
            base.Clear();
            this.PendingRemovals.Clear();
            this.ThisLock.ExitWriteLock();
        }
        public void ClearAndRecycle()
        {
            this.ThisLock.EnterWriteLock();
            this.PendingRemovals = new ConcurrentStack<T>(this); 
            base.Clear();
            this.ThisLock.ExitWriteLock();
        }
        public void ClearAll()
        {
            this.ThisLock.EnterWriteLock();
            base.Clear();
            this.ThisLock.ExitWriteLock();
            this.PendingRemovals?.Clear();
        }
        public new void Remove(T item)
        {
            this.ThisLock.EnterWriteLock();
            base.Remove(item);
            this.ThisLock.ExitWriteLock();
        }
        public void ClearAdd(IEnumerable<T> item)
        {
            this.ThisLock.EnterWriteLock();
            base.Clear();
            base.AddRange(item);
            this.ThisLock.ExitWriteLock();
            
        }
        public new bool Contains(T item)
        {
            this.ThisLock.EnterReadLock();
            bool result = base.Contains(item);
            this.ThisLock.ExitReadLock();
            return result;
        }
        //Contains<TSource>(this IEnumerable<TSource> source, TSource value);
        //Contains<TSource>(this IEnumerable<TSource> source, TSource value);
        //public bool Contains<T>(T item)
        //{
        //    thisLock.EnterReadLock();
        //    var result = this.Contains<T>(item ); //.Contains<T>(item);
        //    thisLock.ExitReadLock();
        //    return result;
        //}

        public new void AddRange(IEnumerable<T> collection)
        {
            this.ThisLock.EnterWriteLock();
            base.AddRange(collection);
            this.ThisLock.ExitWriteLock();
        }
        // to use this:
        // somelist.foreach(action => { do some action with action},false,false,false);
        // to enable parallel mode set the last false to "true'
        public void ForEach(Action<T> action, Boolean performActionOnClones = true, Boolean asParallel = true, Boolean inParallel = false)
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
                this.ThisLock.EnterReadLock();
                var clones = this.Clone(asParallel: asParallel);
                if (asParallel)
                {
                    clones.AsParallel().ForAll(wrapper);
                }
                else if (inParallel)
                {
                    Parallel.ForEach(clones, wrapper);
                }
                else
                {
                    clones.ForEach(wrapper);
                }
                this.ThisLock.ExitReadLock();
            }
            else
            {
                this.ThisLock.EnterReadLock();
                {
                    if (asParallel)
                    {
                        this.AsParallel().ForAll(wrapper);
                    }
                    else if (inParallel)
                    {
                        Parallel.ForEach(this, wrapper);
                    }
                    else
                    {
                        base.ForEach(wrapper);
                    }
                }
                this.ThisLock.ExitReadLock();
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
                    Parallel.ForEach(clones, wrapper);
                }
                else
                {
                    clones.ForEach(wrapper);
                }
            }
            else
            {
                this.ThisLock.EnterReadLock();
                {
                    if (asParallel)
                    {
                        this.AsParallel().ForAll(wrapper);
                    }
                    else if (inParallel)
                    {
                        Parallel.ForEach(this, wrapper);
                    }
                    else
                    {
                        ForEach(wrapper);
                    }
                }
                this.ThisLock.ExitReadLock();
            }
        }
        public Array<T> Clone(bool asParallel = true)
        {
            this.ThisLock.EnterReadLock();
            var copy = asParallel ? new Array<T>(this.AsParallel()) : new Array<T>(this);
            this.ThisLock.ExitReadLock();
            return copy;
        }

        // This is used to reduce the time locks are held. Downside is higher memory usage
        // ReadLock is acquired and base.ToArray() called
        public T[] AtomicCopy()
        {
            this.ThisLock.EnterReadLock();
            var arr = ToArray();
            this.ThisLock.ExitReadLock();
            return arr;
        }
        
        public bool TryReuseItem(out T item)
        {
            if (!this.PendingRemovals.TryPop(out item))
                return false;
            (item as Empire.InfluenceNode)?.Wipe();
            return true;
        }

        public T RecycleObject()
        {
            if (!this.PendingRemovals.TryPop(out T item))
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
            ThisLock?.Dispose();
            ThisLock = null;
        }
    }
}
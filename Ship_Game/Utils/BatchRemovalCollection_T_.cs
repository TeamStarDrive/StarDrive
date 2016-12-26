using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Ship_Game
{
    public sealed class BatchRemovalCollection<T> : Array<T>,IDisposable //where T : new()
    {
        private ConcurrentStack<T> pendingRemovals;
        private ReaderWriterLockSlim thisLock;

        public BatchRemovalCollection()
        {
            //this.pendingRemovals = new Array<T>();
            pendingRemovals = new ConcurrentStack<T>();
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }
        public BatchRemovalCollection(bool noQueueForRemoval)
        {
            //this.pendingRemovals = new Array<T>();
            //this.pendingRemovals = new ConcurrentStack<T>();
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }
        public BatchRemovalCollection(Array<T> ListToCopy)
        {
            //Array<T> list = this as Array<T>;
            //list = ListToCopy.ToArray<T>();
            base.AddRange(ListToCopy);
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        // Acquires a deterministic Read Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireReadLock())
        //     item = list.First();
        public ScopedReadLock AcquireReadLock()
        {
            return new ScopedReadLock(thisLock);
        }

        // Acquires a deterministic Write Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (list.AcquireWriteLock())
        //     list.Add(item);
        public ScopedWriteLock AcquireWriteLock()
        {
            return new ScopedWriteLock(thisLock);
        }

        public void ApplyPendingRemovals()
        {
            using (AcquireWriteLock())
            {
                while (!pendingRemovals.IsEmpty)
                {
                    pendingRemovals.TryPop(out var result);
                    Remove(result);
                }
            }
        }
        public void ApplyPendingRemovals(bool saveForPooling)
        {
            using (AcquireWriteLock())
            {
                if (saveForPooling)
                {
                    foreach (T item in pendingRemovals.ToArray())
                    {
                        Remove(item);
                    }
                    return;
                }
                while (!pendingRemovals.IsEmpty)
                {
                    pendingRemovals.TryPop(out var result);
                    Remove(result);
                }
            }
        }
        public void QueuePendingRemoval(T item)
        {
            pendingRemovals.Push(item);
        }
        public void ClearPendingRemovals()
        {
            pendingRemovals.Clear();
        }

        public bool IsPendingRemoval(T item)
        {
            return pendingRemovals.Contains(item);
        }

        public new void Add(T item)
        {
            thisLock.EnterWriteLock();
            base.Add(item);
            thisLock.ExitWriteLock();
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
            thisLock.EnterWriteLock();
            base.Clear();
            pendingRemovals.Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAndRecycle()
        {
            thisLock.EnterWriteLock();
            pendingRemovals = new ConcurrentStack<T>(this); 
            base.Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAll()
        {
            thisLock.EnterWriteLock();
            base.Clear();
            thisLock.ExitWriteLock();
            pendingRemovals?.Clear();
        }
        public new void Remove(T item)
        {
            thisLock.EnterWriteLock();
            base.Remove(item);
            thisLock.ExitWriteLock();
        }
        public void ClearAdd(IEnumerable<T> item)
        {
            thisLock.EnterWriteLock();
            base.Clear();
            base.AddRange(item);
            thisLock.ExitWriteLock();
            
        }
        public new bool Contains(T item)
        {
            thisLock.EnterReadLock();
            bool result = base.Contains(item);
            thisLock.ExitReadLock();
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
            thisLock.EnterWriteLock();
            base.AddRange(collection);
            thisLock.ExitWriteLock();
        }
        // to use this:
        // somelist.foreach(action => { do some action with action},false,false,false);
        // to enable parallel mode set the last false to "true'
        public void ForEach(Action<T> action, Boolean performActionOnClones = true, Boolean asParallel = true, Boolean inParallel = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
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
                this.thisLock.EnterReadLock();
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
                this.thisLock.ExitReadLock();
            }
            else
            {
                this.thisLock.EnterReadLock();
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
                this.thisLock.ExitReadLock();
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
                thisLock.EnterReadLock();
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
                thisLock.ExitReadLock();
            }
        }
        public Array<T> Clone(bool asParallel = true)
        {
            thisLock.EnterReadLock();
            var copy = asParallel ? new Array<T>(this.AsParallel()) : new Array<T>(this);
            thisLock.ExitReadLock();
            return copy;
        }

        // This is used to reduce the time locks are held. Downside is higher memory usage
        // ReadLock is acquired and base.ToArray() called
        public T[] AtomicCopy()
        {
            thisLock.EnterReadLock();
            var arr = ToArray();
            thisLock.ExitReadLock();
            return arr;
        }
        
        public bool TryReuseItem(out T item)
        {
            if (!pendingRemovals.TryPop(out item))
                return false;
            (item as Empire.InfluenceNode)?.Wipe();
            return true;
        }

        public T RecycleObject()
        {            
            T item;
            if (!pendingRemovals.TryPop(out item))
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
        // simpler, faster destruction logic
        private void Destroy()
        {
            thisLock?.Dispose();
            thisLock = null;
        }


    }
}
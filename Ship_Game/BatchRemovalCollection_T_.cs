using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Ship_Game
{
    public sealed class BatchRemovalCollection<T> : List<T>,IDisposable //where T : new()
    {
        //public List<T> pendingRemovals;
        public ConcurrentStack<T> pendingRemovals;
        public ReaderWriterLockSlim thisLock;

        public BatchRemovalCollection()
        {
            //this.pendingRemovals = new List<T>();
            pendingRemovals = new ConcurrentStack<T>();
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(bool noQueueForRemoval)
        {
            //this.pendingRemovals = new List<T>();
            //this.pendingRemovals = new ConcurrentStack<T>();
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(List<T> ListToCopy)
        {
            //List<T> list = this as List<T>;
            //list = ListToCopy.ToList<T>();
            base.AddRange(ListToCopy);
            thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public void ApplyPendingRemovals()
        {
            while (!pendingRemovals.IsEmpty)
            {
                pendingRemovals.TryPop(out var result); //out T result);
                Remove(result);
            }
        }
        public void ApplyPendingRemovals(bool saveForPooling)
        {
            if (saveForPooling)
            {
                foreach (T item in pendingRemovals.ToArray())
                {
                    Remove(item);
                }
                return;
            }
            var removes = new List<T>();
            while (!pendingRemovals.IsEmpty)
            {
                pendingRemovals.TryPop(out var result); //out T result);
                //removes.Add(result);
                Remove(result);
                
            }
            //this.thisLock.EnterWriteLock();
            //removes = (this as List<T>).Except(removes).ToList();

            //(this as List<T>).Clear();
            //(this as List<T>).AddRange(removes);
            //this.thisLock.ExitWriteLock();
        }
        public void QueuePendingRemoval(T item)
        {
            pendingRemovals.Push(item);
        }
        public void ClearPendingRemovals()
        {
            pendingRemovals.Clear();
        }
        public new void Add(T item)
        {
            thisLock.EnterWriteLock();
            base.Add(item);
            thisLock.ExitWriteLock();
        }
        public List<T> Get()
        {
            var list = new List<T>();
            thisLock.EnterReadLock();
            list.AddRange(this);// = base.ToList();
            thisLock.ExitReadLock();
            return list;// this as List<T>;
        }
        
        public new Enumerator GetEnumerator()
        {
            thisLock.EnterReadLock();
            var result = base.GetEnumerator();
            thisLock.ExitReadLock();
            return result;
        }
        public new void Clear()
        {
            thisLock.EnterWriteLock();
            base.Clear();
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

        public void AddRange(List<T> item)
        {
            thisLock.EnterWriteLock();
            base.AddRange(item);
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
        public List<T> Clone(bool asParallel = true)
        {
            thisLock.EnterReadLock();
            var copy = asParallel ? new List<T>(this.AsParallel()) : new List<T>(this);
            thisLock.ExitReadLock();
            return copy;
        }
 

        public T RecycleObject()
        {            
            T test;

            if (!pendingRemovals.TryPop(out test)) return test;
            if (test is Empire.InfluenceNode)
                (test as Empire.InfluenceNode).Wipe();
            return test;
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
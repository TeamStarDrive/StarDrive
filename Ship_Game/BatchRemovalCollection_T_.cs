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

        //adding for thread safe Dispose because class uses unmanaged resources
        private bool disposed;

        public BatchRemovalCollection()
        {
            //this.pendingRemovals = new List<T>();
            this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(bool noQueueForRemoval)
        {
            //this.pendingRemovals = new List<T>();
            //this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(List<T> ListToCopy)
        {
            //List<T> list = this as List<T>;
            //list = ListToCopy.ToList<T>();
            base.AddRange(ListToCopy);
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public void ApplyPendingRemovals()
        {
            T result;            
            while (!this.pendingRemovals.IsEmpty)
            {               
                this.pendingRemovals.TryPop(out result); //out T result);
                this.Remove(result);
            }
        }
        public void ApplyPendingRemovals(bool SaveForPooling)
        {
            if (SaveForPooling)
            {
                foreach(T item in this.pendingRemovals.ToArray())
                {
                    this.Remove(item);
                }
                return;
            }
            T result;
            List<T> removes = new List<T>();
            while (!this.pendingRemovals.IsEmpty)
            {
                
                this.pendingRemovals.TryPop(out result); //out T result);
                //removes.Add(result);
                this.Remove(result);
                
            }
            //this.thisLock.EnterWriteLock();
            //removes = (this as List<T>).Except(removes).ToList();

            //(this as List<T>).Clear();
            //(this as List<T>).AddRange(removes);
            //this.thisLock.ExitWriteLock();
        }
        public void QueuePendingRemoval(T item)
        {
            this.pendingRemovals.Push(item);
        }
        public void ClearPendingRemovals()
        {
            this.pendingRemovals.Clear();
        }
        new public void Add(T item)
        {
            thisLock.EnterWriteLock();
            base.Add(item);
            thisLock.ExitWriteLock();
        }
        public List<T> Get()
        {
            List<T> list = new List<T>();
            thisLock.EnterReadLock();
            list.AddRange(this as List<T>);// = base.ToList();
            thisLock.ExitReadLock();
            return list;// this as List<T>;
        }
        
        new public Enumerator GetEnumerator()
        {
            thisLock.EnterReadLock();
            var result = base.GetEnumerator();
            thisLock.ExitReadLock();
            return result;
        }
        new public void Clear()
        {
            thisLock.EnterWriteLock();
            base.Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAndRecycle()
        {
            thisLock.EnterWriteLock();
            List<T> test = (this as List<T>);
            this.pendingRemovals =  new ConcurrentStack<T>(test); 
            base.Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAll()
        {
            thisLock.EnterWriteLock();
            base.Clear();
            thisLock.ExitWriteLock();
            if(this.pendingRemovals !=null)
            {
                this.pendingRemovals.Clear();
            }
        }
        new public void Remove(T item)
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
        new public bool Contains(T item)
        {
            thisLock.EnterReadLock();
            var result = base.Contains(item);
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

        public void ForAll(Action<T> action, Boolean performActionOnClones = true, Boolean asParallel = true, Boolean inParallel = false)
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
                        this.ForEach(wrapper);
                    }
                }
                this.thisLock.ExitReadLock();
            }
        }
        public List<T> Clone(Boolean asParallel = true)
        {
             
            this.thisLock.EnterReadLock();
            
            var test =asParallel ? new List<T>(this.AsParallel()) : new List<T>(this);
            this.thisLock.ExitReadLock();
            return test;
        }
 

        public T RecycleObject()
        {            
            T test;
            
            if (this.pendingRemovals.TryPop(out test))
            {
                if (test is Empire.InfluenceNode)
                    (test as Empire.InfluenceNode).Wipe();
                
            }            
            return test;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BatchRemovalCollection() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.thisLock != null)
                        this.thisLock.Dispose();

                }
                this.thisLock = null;
                this.disposed = true;
            }
        }


    }
}
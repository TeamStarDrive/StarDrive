using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ship_Game
{
    public class BatchRemovalCollection<T> : List<T>
    {
        //public List<T> pendingRemovals;
        public ConcurrentStack<T> pendingRemovals;
        public ReaderWriterLockSlim thisLock;

        public BatchRemovalCollection()
        {
            //this.pendingRemovals = new List<T>();
            this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }

        public void ApplyPendingRemovals()
        {
            //for (int i = 0; i < this.pendingRemovals.Count; i++)
            //foreach(T i in this.pendingRemovals)

            while (!this.pendingRemovals.IsEmpty)
            {
                T result;
                this.pendingRemovals.TryPop(out result); //out T result);
                //base.Remove(this.pendingRemovals(i));
                this.Remove(result);


            }
            //this.pendingRemovals=new ConcurrentBag<T>();
        }

        public void QueuePendingRemoval(T item)
        {
            this.pendingRemovals.Push(item);
        }
        //new public IEnumerator<T> GetEnumerator()
        //{
        //    using (IEnumerator<T> ie = internalList.GetEnumerator())
        //        while (ie.MoveNext())
        //        {
        //            yield return internalList.Combine(BatchRemovalCollection ie.Current);
        //        }
        //}
        new public void Add(T item)
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Add(item);
            thisLock.ExitWriteLock();
        }
        public List<T> Get()
        {
            thisLock.EnterReadLock();
            var list = this;
            return this as List<T>;
        }
        new public Enumerator GetEnumerator()
        {
            thisLock.EnterReadLock();
            var result = (this as List<T>).GetEnumerator();
            thisLock.ExitReadLock();
            return result;
        }
        new public void Clear()
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Clear();
            thisLock.ExitWriteLock();
        }
        new public void Remove(T item)
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Remove(item);
            thisLock.ExitWriteLock();

        }
        new public bool Contains(T item)
        {
            thisLock.EnterReadLock();
            var result = (this as List<T>).Contains(item);
            thisLock.ExitReadLock();
            return result;
        }


    }
}
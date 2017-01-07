using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Ship_Game
{
    public sealed class SafeQueue<T> : LinkedList<T>, IDisposable //where T : new()
    {
        //public LinkedArray<T> pendingRemovals;
        public ConcurrentStack<T> pendingRemovals;
        public ReaderWriterLockSlim thisLock;

        public SafeQueue()
        {
            //this.pendingRemovals = new LinkedArray<T>();
            this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public SafeQueue(bool noQueueForRemoval)
        {
            //this.pendingRemovals = new LinkedArray<T>();
            //this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        //public OrderQueue(Array<T> ListToCopy)
        //{
        //    LinkedArray<T> list = this as LinkedArray<T>;
        //    list = ListToCopy.ToArray<T>();
        //    this.AddRange(list);
        //    this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        //}
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
                foreach (T item in this.pendingRemovals.ToArray())
                {
                    this.Remove(item);
                }
                return;
            }
            T result;
            LinkedList<T> removes = new LinkedList<T>();
            while (!this.pendingRemovals.IsEmpty)
            {

                this.pendingRemovals.TryPop(out result); //out T result);
                //removes.Add(result);
                this.Remove(result);

            }
            //this.thisLock.EnterWriteLock();
            //removes = (this as LinkedArray<T>).Except(removes).ToList();

            //(this as LinkedArray<T>).Clear();
            //(this as LinkedArray<T>).AddRange(removes);
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
      new public void AddLast(T item)
        {
            thisLock.EnterWriteLock();
            base.AddLast(item);
            thisLock.ExitWriteLock();
        }
       new public void AddFirst(T item)
        {
            thisLock.EnterWriteLock();
           base.AddFirst(item);
            thisLock.ExitWriteLock();
        }
        public LinkedList<T> Get()
        {
            thisLock.EnterReadLock();
            var list = this;
            thisLock.ExitReadLock();
            return this as LinkedList<T>;
        }

        new public LinkedListNode<T> Last
        {
            get
            {
                thisLock.EnterReadLock();
                var result = base.Last;
                thisLock.ExitReadLock();
                return result;
            }
        }

        new public int Count
        {
            get
            {
                thisLock.EnterReadLock();
                var result = base.Count;
                thisLock.ExitReadLock();
                return result;
            }
        }

        new public LinkedListNode<T> First
        {
            get
            {
                thisLock.EnterReadLock();
                var result = base.First;
                thisLock.ExitReadLock();
                return result;
            }
        }

	
        public T FirstOrDefault()
        {
            //get
            {
                thisLock.EnterReadLock();
                var result = (this as LinkedList<T>).FirstOrDefault();
                thisLock.ExitReadLock();
                return result;
            }
        }
        public T LastOrDefault()
        {
            //get    TSource LastOrDefault<TSource>(this IEnumerable<TSource> source);
            {
                thisLock.EnterReadLock();
                var result = (this as LinkedList<T>).LastOrDefault();
                thisLock.ExitReadLock();
                return result;
            }
        }
        new public Enumerator GetEnumerator()
        {
            thisLock.EnterReadLock();
            var result = (this as LinkedList<T>).GetEnumerator();
            thisLock.ExitReadLock();
            return result;
        }
        new public void Clear()
        {
            thisLock.EnterWriteLock();
            (this as LinkedList<T>).Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAndRecycle()
        {
            thisLock.EnterWriteLock();
            LinkedList<T> test = (this as LinkedList<T>);
            this.pendingRemovals = new ConcurrentStack<T>(test);
            (this as LinkedList<T>).Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAll()
        {
            thisLock.EnterWriteLock();
            (this as LinkedList<T>).Clear();
            thisLock.ExitWriteLock();
            if (this.pendingRemovals != null)
            {
                this.pendingRemovals.Clear();
            }
        }
        new public void Remove(T item)
        {

            thisLock.EnterWriteLock();
            (this as LinkedList<T>).Remove(item);
            thisLock.ExitWriteLock();

        }
        new public void RemoveLast()
        {

            thisLock.EnterWriteLock();
            (this as LinkedList<T>).RemoveLast();
            thisLock.ExitWriteLock();

        }
        new public void RemoveFirst()
        {

            thisLock.EnterWriteLock();
            (this as LinkedList<T>).RemoveFirst();
            thisLock.ExitWriteLock();

        }
        new public bool Contains(T item)
        {
            thisLock.EnterReadLock();
            var result = (this as LinkedList<T>).Contains(item);
            thisLock.ExitReadLock();
            return result;
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

        ~SafeQueue() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            thisLock?.Dispose(ref thisLock);
        }


    }
}
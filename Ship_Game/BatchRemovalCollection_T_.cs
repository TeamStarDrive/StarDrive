using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ship_Game
{
    public class BatchRemovalCollection<T> : List<T>,IDisposable
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
            while (!this.pendingRemovals.IsEmpty)
            {
                this.pendingRemovals.TryPop(out result); //out T result);
                this.Remove(result);
            }
        }
        public void QueuePendingRemoval(T item)
        {
            this.pendingRemovals.Push(item);
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
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
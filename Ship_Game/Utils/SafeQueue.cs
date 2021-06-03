using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ship_Game.Utils
{
    /// <summary>
    /// A thread-safe Queue, optimized for Enqueue and Dequeue operations
    /// Some of the code has been manually inlined for better optimization,
    /// since this SafeQueue is used heavily in ShipAI
    /// 
    /// For Unit Tests, check SDUnitTests/TestSafeQueueT.cs
    /// </summary>
    [DebuggerTypeProxy(typeof(SafeQueueDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class SafeQueue<T> : IDisposable, IReadOnlyCollection<T>
    {
        T[] Items;
        int Head; // index of the First element
        public int Count { get; private set; }
        ReaderWriterLockSlim ThisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        AutoResetEvent ItemAdded = new AutoResetEvent(false);

        // This is relatively atomic, no reason to lock on this
        // as it wouldn't provide any benefits or thread safety
        public bool IsEmpty  => Count == 0;
        public bool NotEmpty => Count != 0;

        public SafeQueue()
        {
            Items = Empty<T>.Array;
        }

        public SafeQueue(int capacity)
        {
            Items = new T[capacity];
        }

        public SafeQueue(ICollection<T> collection)
        {
            int i = 0;
            Items = new T[collection.Count];
            foreach (T item in collection)
                Items[i++] = item;
        }


        // Acquires a deterministic Read Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (queue.AcquireReadLock())
        //     item = queue.First();
        public ScopedReadLock AcquireReadLock() => new ScopedReadLock(ThisLock);


        // Acquires a deterministic Write Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (queue.AcquireWriteLock())
        //     queue.Add(item);
        public ScopedWriteLock AcquireWriteLock() => new ScopedWriteLock(ThisLock);


        // This is intentionally left as non-threadsafe
        // Use AcquireReadLock() if you need thread safety
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
                return Items[(Head + index) % Items.Length];
            }
        }

        // This is a thread-safe accessor
        public T ElementAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
            try
            {
                ThisLock.EnterWriteLock();
                return Items[(Head + index) % Items.Length];
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        // Adds an item to the end of the queue
        // Could by also named PushToEnd. This Enqueued item will be
        // the last item you get when calling Dequeue()
        public void Enqueue(T item)
        {
            ThisLock.EnterWriteLock();
            int length = Items.Length;
            if (Count == length)
            {
                int cap = length < 4 ? 4 : length * 3 / 2;
                int rem = cap % 4;
                if (rem != 0) cap += 4 - rem;

                var newArray = new T[cap];
                if (length > 0)
                { 
                    // @note This is different from PushToFront
                    // reorder the ring buffer to a clean layout
                    for (int j = 0, i = Head; j < length;)
                    {
                        newArray[j++] = Items[i++];
                        if (i == length) i = 0;
                    }
                    Head = 0;
                }
                Items = newArray;
            }

            Items[(Head + Count) % Items.Length] = item;
            ++Count;
            ItemAdded.Set();
            ThisLock.ExitWriteLock();
        }


        // This will push the item to the front of the queue
        // which will cut in line of everyone else
        // If Dequeue is called, this will be the item returned
        public void PushToFront(T item)
        {
            ThisLock.EnterWriteLock();
            int length = Items.Length;
            if (Count == length) // grow
            {
                int cap = length < 4 ? 4 : length * 3 / 2;
                int rem = cap % 4;
                if (rem != 0) cap += 4 - rem;

                var newArray = new T[cap];
                if (length > 0)
                {
                    // @note This is different from ENQUEUE grow
                    // reorder the ring buffer to a clean layout
                    for (int j = 1, i = Head, n = length+1; j < n;)
                    {
                        newArray[j++] = Items[i++];
                        if (i == length) i = 0;
                    }
                    Head = 0;
                }
                Items = newArray;
            }
            else
            {
                if (--Head < 0) Head = length - 1;
            }
            Items[Head] = item;
            ++Count;
            ItemAdded.Set();
            ThisLock.ExitWriteLock();
        }


        // Will dequeue the FIRST item from the queue, which is the first item that was Enqueued
        public T Dequeue()
        {
            if (Count == 0)
                return default;

            ThisLock.EnterWriteLock();
            T item = Items[Head];
            if (++Head == Items.Length) Head = 0;
            --Count;
            ThisLock.ExitWriteLock();
            return item;
        }

        // Will TRY to dequeue the FIRST item from the queue, which is the first item that was Enqueued
        public bool TryDequeue(out T item)
        {
            ThisLock.EnterWriteLock();
            if (Count == 0)
            {
                item = default;
                ThisLock.ExitWriteLock();
                return false;
            }
            item = Items[Head];
            if (++Head == Items.Length) Head = 0;
            --Count;
            ThisLock.ExitWriteLock();
            return true;
        }

        // This will Set the ItemAdded event,
        // so that blocking WaitDequeue event can resume
        public void Notify()
        {
            ItemAdded.Set();
        }

        // block until an item is available or timeout was reached
        public bool WaitDequeue(out T item, int millisecondTimeout = -1)
        {
            if (ItemAdded.WaitOne(millisecondTimeout) && Count > 0)
            {
                ThisLock.EnterWriteLock();
                item = Items[Head];
                if (++Head == Items.Length) Head = 0;
                --Count;
                ThisLock.ExitWriteLock();
                return true;
            }
            item = default;
            return false;
        }

        // Same as Dequeue, but doesn't return any value
        public void RemoveFirst()
        {
            if (Count == 0)
                return;
            ThisLock.EnterWriteLock();
            Items[Head] = default; // clear the item
            if (++Head == Items.Length) Head = 0;
            --Count;
            ThisLock.ExitWriteLock();
        }

        // Will remove the last item in the queue, which would be the last item Dequeued
        public void RemoveLast()
        {
            if (Count == 0)
                return;
            ThisLock.EnterWriteLock();
            Items[(Head + Count - 1) % Items.Length] = default; // clear the item
            --Count;
            ThisLock.ExitWriteLock();
        }

        // Peeks the first element that would be dequeued
        // If there is nothing to peek, default(T) is returned
        public T PeekFirst
        {
            get
            {
                ThisLock.EnterReadLock();
                T result = Count > 0 ? Items[Head] : default;
                ThisLock.ExitReadLock();
                return result;
            }
        }

        public bool TryPeekFirst(out T result)
        {
            ThisLock.EnterReadLock();
            bool success = Count > 0;
            result = success ? Items[Head] : default;
            ThisLock.ExitReadLock();
            return success;
        }

        // Peek the last element in the queue
        // The last element is the absolute last element that would be processed from the queue
        public T PeekLast
        {
            get
            {
                ThisLock.EnterReadLock();
                T result = Count > 0 ? Items[(Head + Count - 1) % Items.Length] : default;
                ThisLock.ExitReadLock();
                return result;
            }
        }

        public bool TryPeekLast(out T result)
        {
            ThisLock.EnterReadLock();
            bool success = Count > 0;
            result = success ? Items[(Head + Count - 1) % Items.Length] : default;
            ThisLock.ExitReadLock();
            return success;
        }

        public void Clear()
        {
            ThisLock.EnterWriteLock();
            Array.Clear(Items, 0, Items.Length);
            Head  = 0;
            Count = 0;
            ThisLock.ExitWriteLock();
        }

        public bool Contains(T item)
        {
            ThisLock.EnterReadLock();
            int length = Items.Length;
            int count  = Count;
            if (item == null)
            {
                for (int j = 0, i = Head; j < count; ++j)
                {
                    if (Items[i] == null)
                    {
                        ThisLock.ExitReadLock();
                        return true;
                    }
                    if (++i == length) i = 0;
                }
            }
            else
            {
                var c = EqualityComparer<T>.Default;
                for (int j = 0, i = Head; j < count; ++j)
                {
                    if (c.Equals(Items[i], item))
                    {
                        ThisLock.ExitReadLock();
                        return true;
                    }
                    if (++i == length) i = 0;
                }
            }
            ThisLock.ExitReadLock();
            return false;
        }

        public T[] ToArray()
        {
            ThisLock.EnterReadLock();
            int count = Count;
            var arr = new T[count];
            for (int j = 0, i = Head; j < count;)
            {
                arr[j++] = Items[i++];
                if (i == Items.Length) i = 0;
            }
            ThisLock.ExitReadLock();
            return arr;
        }

        
        public T[] TakeAll()
        {
            ThisLock.EnterReadLock();
            int count = Count;
            var arr = new T[count];
            for (int j = 0, i = Head; j < count;)
            {
                arr[j++] = Items[i++];
                if (i == Items.Length) i = 0;
            }
            Array.Clear(Items, 0, Items.Length);
            Head  = 0;
            Count = 0;
            ThisLock.ExitReadLock();
            return arr;
        }

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        public void Dispose()
        {
            Destroy(true);
            GC.SuppressFinalize(this);
        }

        ~SafeQueue() { Destroy(false); }

        void Destroy(bool force)
        {
            Count = 0;
            if (force)
            {
                if (ItemAdded != null)
                {
                    ItemAdded.Set();
                    ItemAdded = null;
                }
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator    IEnumerable.GetEnumerator()    => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {
            int Head;
            int Index;
            readonly int Count;
            readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SafeQueue<T> queue)
            {
                Head  = queue.Head;
                Index = 0;
                Count = queue.Count;
                Items = queue.Items;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (++Index > Count)
                    return false;
                Current = Items[Head];
                if (++Head >= Items.Length)
                    Head = 0;
                return true;
            }
            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }

    internal sealed class SafeQueueDebugView<T>
    {
        readonly SafeQueue<T> Queue;

        public SafeQueueDebugView(SafeQueue<T> queue)
        {
            Queue = queue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => Queue.ToArray();
        public T FirstToDequeue => Queue.PeekFirst;
    }
}
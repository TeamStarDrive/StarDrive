using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SDUtils;
#pragma warning disable CA1065

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
    public sealed class SafeQueue<T> : IDisposable, IReadOnlyCollection<T>
    {
        T[] Items;
        int Head; // index of the First element
        public int Count { get; private set; }
        ReaderWriterLockSlim ThisLock = new(LockRecursionPolicy.SupportsRecursion);
        AutoResetEvent ItemAdded = new(false);

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
        public ScopedReadLock AcquireReadLock() => new(ThisLock);


        // Acquires a deterministic Write Lock on this Collection
        // You must use the C# using block to ensure deterministic release of the lock
        // using (queue.AcquireWriteLock())
        //     queue.Add(item);
        public ScopedWriteLock AcquireWriteLock() => new(ThisLock);


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
            try
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
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        // This will push the item to the front of the queue
        // which will cut in line of everyone else
        // If Dequeue is called, this will be the item returned
        public void PushToFront(T item)
        {
            try
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
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }


        // Will dequeue the FIRST item from the queue, which is the first item that was Enqueued
        public T Dequeue()
        {
            try
            {
                ThisLock.EnterWriteLock();
                if (Count > 0)
                {
                    T item = Items[Head];
                    if (++Head == Items.Length) Head = 0;
                    --Count;
                    return item;
                }
                else
                {
                    return default;
                }
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        // Will TRY to dequeue the FIRST item from the queue, which is the first item that was Enqueued
        public bool TryDequeue(out T item)
        {
            try
            {
                ThisLock.EnterWriteLock();
                if (Count > 0)
                {
                    item = Items[Head];
                    if (++Head == Items.Length) Head = 0;
                    --Count;
                    return true;
                }
                else
                {
                    item = default;
                    return false;
                }
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
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
                try
                {
                    ThisLock.EnterWriteLock();
                    if (Count > 0)
                    {
                        item = Items[Head];
                        if (++Head == Items.Length) Head = 0;
                        --Count;
                        return true;
                    }
                }
                finally
                {
                    ThisLock.ExitWriteLock();
                }
            }
            item = default;
            return false;
        }

        // Same as Dequeue, but doesn't return any value
        public void RemoveFirst()
        {
            try
            {
                ThisLock.EnterWriteLock();
                if (Count > 0)
                {
                    Items[Head] = default; // clear the item
                    if (++Head == Items.Length) Head = 0;
                    --Count;
                }
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        // Will remove the last item in the queue, which would be the last item Dequeued
        public void RemoveLast()
        {
            try
            {
                ThisLock.EnterWriteLock();
                if (Count > 0)
                {
                    Items[(Head + Count - 1) % Items.Length] = default; // clear the item
                    --Count;
                }
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        // Peeks the first element that would be dequeued
        // If there is nothing to peek, default(T) is returned
        public T PeekFirst
        {
            get
            {
                try
                {
                    ThisLock.EnterReadLock();
                    T result = Count > 0 ? Items[Head] : default;
                    return result;
                }
                finally
                {
                    ThisLock.ExitReadLock();
                }
            }
        }

        public bool TryPeekFirst(out T result)
        {
            try
            {
                ThisLock.EnterReadLock();
                bool success = Count > 0;
                result = success ? Items[Head] : default;
                return success;
            }
            finally
            {
                ThisLock.ExitReadLock();
            }
        }

        // Peek the last element in the queue
        // The last element is the absolute last element that would be processed from the queue
        public T PeekLast
        {
            get
            {
                try
                {
                    ThisLock.EnterReadLock();
                    T result = Count > 0 ? Items[(Head + Count - 1) % Items.Length] : default;
                    return result;
                }
                finally
                {
                    ThisLock.ExitReadLock();
                }
            }
        }

        public bool TryPeekLast(out T result)
        {
            try
            {
                ThisLock.EnterReadLock();
                bool success = Count > 0;
                result = success ? Items[(Head + Count - 1) % Items.Length] : default;
                return success;
            }
            finally
            {
                ThisLock.ExitReadLock();
            }
        }

        public void Clear()
        {
            try
            {
                ThisLock.EnterWriteLock();
                Array.Clear(Items, 0, Items.Length);
                Head  = 0;
                Count = 0;
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                ThisLock.EnterReadLock();
                int length = Items.Length;
                int count  = Count;
                if (item == null)
                {
                    for (int j = 0, i = Head; j < count; ++j)
                    {
                        if (Items[i] == null) return true;
                        if (++i == length) i = 0;
                    }
                }
                else
                {
                    var c = EqualityComparer<T>.Default;
                    for (int j = 0, i = Head; j < count; ++j)
                    {
                        if (c.Equals(Items[i], item)) return true;
                        if (++i == length) i = 0;
                    }
                }
                return false;
            }
            finally
            {
                ThisLock.ExitReadLock();
            }
        }

        // @returns TRUE if any item matches the Predicate
        public bool Any(Predicate<T> predicate)
        {
            try
            {
                ThisLock.EnterReadLock();
                int length = Items.Length;
                int count = Count;
                for (int j = 0, i = Head; j < count; ++j)
                {
                    if (predicate(Items[i])) return true;
                    if (++i == length) i = 0;
                }
                return false;
            }
            finally
            {
                ThisLock.ExitReadLock();
            }
        }

        public T[] ToArray()
        {
            try
            {
                ThisLock.EnterReadLock();
                int count = Count;
                var arr = new T[count];
                for (int j = 0, i = Head; j < count;)
                {
                    arr[j++] = Items[i++];
                    if (i == Items.Length) i = 0;
                }
                return arr;
            }
            finally
            {
                ThisLock.ExitReadLock();
            }
        }

        public T[] TakeAll()
        {
            try
            {
                ThisLock.EnterWriteLock();
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
                return arr;
            }
            finally
            {
                ThisLock.ExitWriteLock();
            }
        }

        public void SetRange(IReadOnlyList<T> items)
        {
            Clear();
            foreach (var item in items)
                Enqueue(item);
        }

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SafeQueue() { Dispose(false); }

        void Dispose(bool force)
        {
            Count = 0;
            if (force)
            {
                var itemAdded = ItemAdded;
                if (itemAdded != null)
                {
                    if (!itemAdded.SafeWaitHandle.IsClosed)
                    {
                        ItemAdded = null;
                        itemAdded.Set();
                        itemAdded.Dispose();
                    }
                }
            }
            ThisLock.Dispose();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator    IEnumerable.GetEnumerator()    => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {
            int Head;
            int Index;
            readonly SafeQueue<T> Queue;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SafeQueue<T> queue)
            {
                Head = queue.Head;
                Index = 0;
                Queue = queue;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                // lock to ensure SafeQueue's thread-safety, during foreach or LINQ loops
                try
                {
                    Queue.ThisLock.EnterReadLock();
                    if (++Index > Queue.Count)
                        return false;

                    if (Head >= Queue.Items.Length)
                        Head = 0;

                    Current = Queue.Items[Head];
                    ++Head;
                    return true;
                }
                finally
                {
                    Queue.ThisLock.ExitReadLock();
                }
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
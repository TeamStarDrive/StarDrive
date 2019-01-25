using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ship_Game
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

        static readonly T[] Empty = Empty<T>.Array;

        // This is relatively atomic, no reason to lock on this
        // as it wouldn't provide any benefits or thread safety
        public bool IsEmpty  => Count == 0;
        public bool NotEmpty => Count != 0;

        public SafeQueue()
        {
            Items = Empty;
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
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
                    return Items[(Head + index) % Items.Length];
                }
            }
        }

        // Adds an item to the end of the queue
        // Could by also named PushToEnd. This Enqueued item will be
        // the last item you get when calling Dequeue()
        public void Enqueue(T item)
        {
            ThisLock.EnterWriteLock();
            unchecked {
                int length = Items.Length;
                if (Count == length) { // heavily optimized:
                    int cap = length < 4 ? 4 : length * 3 / 2;
                    int rem = cap % 4;
                    if (rem != 0) cap += 4 - rem;

                    var newArray = new T[cap];
                    if (length > 0) { // reorder the ringbuffer to a clean layout
                        for (int j = 0, i = Head; j < length;) {
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
            ThisLock.ExitWriteLock();
        }


        // This will push the item to the front of the queue
        // which will cut in line of everyone else
        // If Dequeue is called, this will be the item returned
        public void PushToFront(T item)
        {
            ThisLock.EnterWriteLock();
            unchecked {
                int length = Items.Length;
                if (Count == length) { // heavily optimized:
                    int cap = length < 4 ? 4 : length * 3 / 2;
                    int rem = cap % 4;
                    if (rem != 0) cap += 4 - rem;

                    var newArray = new T[cap];
                    if (length > 0) { // reorder the ringbuffer to a clean layout
                        for (int j = 1, i = Head, n = length+1; j < n;) {
                            newArray[j++] = Items[i++];
                            if (i == length) i = 0;
                        }
                        Head = 0;
                    }
                    Items = newArray;
                }
                else {
                    if (--Head < 0) Head = length - 1;
                }
                Items[Head] = item;
                ++Count;
                ItemAdded.Set();
            }
            ThisLock.ExitWriteLock();
        }


        // Will dequeue the FIRST item from the queue, which is the first item that was Enqueued
        public T Dequeue()
        {
            unchecked {
                if (Count == 0)
                    return default(T);
                ThisLock.EnterWriteLock();
                T item = Items[Head];
                if (++Head == Items.Length) Head = 0;
                --Count;
                ThisLock.ExitWriteLock();
                return item;
            }
        }

        // block until an item is available or timeout was reached
        public bool WaitDequeue(out T item, int millisecondTimeout = -1)
        {
            unchecked {
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
        }

        // Same as Dequeue, but doesn't return any value
        public void RemoveFirst()
        {
            unchecked {
                if (Count == 0)
                    return;
                ThisLock.EnterWriteLock();
                Items[Head] = default(T); // clear the item
                if (++Head == Items.Length) Head = 0;
                --Count;
                ThisLock.ExitWriteLock();
            }
        }

        // Will remove the last item in the queue, which would be the last item Dequeued
        public void RemoveLast()
        {
            unchecked {
                if (Count == 0)
                    return;
                ThisLock.EnterWriteLock();
                Items[(Head + Count - 1) % Items.Length] = default(T); // clear the item
                --Count;
                ThisLock.ExitWriteLock();
            }
        }

        // Peeks the first element that would be dequeued
        // If there is nothing to peek, default(T) is returned
        public T PeekFirst
        {
            get
            {
                ThisLock.EnterReadLock();
                T result = Count > 0 ? Items[Head] : default(T);
                ThisLock.ExitReadLock();
                return result;
            }
        }

        // Peek the last element in the queue
        // The last element is the absolute last element that would be processed from the queue
        public T PeekLast
        {
            get
            {
                ThisLock.EnterReadLock();
                T result = Count > 0 ? Items[(Head + Count - 1) % Items.Length] : default(T);
                ThisLock.ExitReadLock();
                return result;
            }
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
            unchecked
            {
                ThisLock.EnterReadLock();
                int length = Items.Length;
                int count  = Count;
                if (item == null) {
                    for (int j = 0, i = Head; j < count; ++j) {
                        if (Items[i] == null) {
                            ThisLock.ExitReadLock();
                            return true;
                        }
                        if (++i == length) i = 0;
                    }
                }
                else {
                    var c = EqualityComparer<T>.Default;
                    for (int j = 0, i = Head; j < count; ++j) {
                        if (c.Equals(Items[i], item)) {
                            ThisLock.ExitReadLock();
                            return true;
                        }
                        if (++i == length) i = 0;
                    }
                }
                ThisLock.ExitReadLock();
                return false;
            }
        }

        public T[] ToArray()
        {
            unchecked {
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
        }

        
        public T[] TakeAll()
        {
            unchecked {
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
        }

        public override string ToString()
        {
            return GetType().GenericName();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~SafeQueue() { Destroy(); }

        private void Destroy()
        {
            Count = 0;
            ItemAdded?.Set();
            ItemAdded?.Dispose(ref ItemAdded);
            ThisLock?.Dispose(ref ThisLock);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator    IEnumerable.GetEnumerator()    => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {
            private int Head;
            private int Index;
            private readonly int Count;
            private readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SafeQueue<T> queue)
            {
                Head  = queue.Head;
                Index = 0;
                Count = queue.Count;
                Items = queue.Items;
                Current = default(T);
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                unchecked {
                    if (++Index > Count)
                        return false;
                    Current = Items[Head];
                    if (++Head >= Items.Length)
                        Head = 0;
                    return true;
                }
            }
            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }

    internal sealed class ReadOnlyCollectionDebugView<T>
    {
        private readonly IReadOnlyCollection<T> Collection;

        public ReadOnlyCollectionDebugView(IReadOnlyCollection<T> collection)
        {
            Collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => Collection.ToArray();
    }

    internal sealed class SafeQueueDebugView<T>
    {
        private readonly SafeQueue<T> Queue;

        public SafeQueueDebugView(SafeQueue<T> queue)
        {
            Queue = queue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => Queue.ToArray();
        public T FirstToDequeue => Queue.PeekFirst;
    }
}
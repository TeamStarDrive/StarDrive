using System;
using System.Threading;

namespace Ship_Game
{
    public static class ThreadExt
    {
        public static ScopedReadLock AcquireReadLock(this ReaderWriterLockSlim readlock)
        {
            return new ScopedReadLock(readlock);
        }

        public static ScopedWriteLock AcquireWriteLock(this ReaderWriterLockSlim writelock)
        {
            return new ScopedWriteLock(writelock);
        }

        // Partitioned parallel For
        public static void ParallelForEach<T>(this T[] array, Action<T> action)
        {
            Parallel.For(0, array.Length, (start, end) => {
                for (int i = start; i < end; ++i)
                    action(array[i]);
            });
        }

        public static void ParallelForEach<T>(this Array<T> array, Action<T> action)
        {
            Parallel.For(0, array.Count, (start, end) => {
                for (int i = start; i < end; ++i)
                    action(array[i]);
            });
        }
    }
}

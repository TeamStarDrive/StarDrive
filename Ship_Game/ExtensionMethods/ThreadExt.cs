using System;
using System.Threading;
using SDUtils;

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

        // Partitioned parallel range iteration
        // Use this rather than Parallel.ForEach, to improve iteration performance and avoid callback overhead
        // @note This is about 3x slower than Ship_Game.Parallel.For, but it's tens of times faster
        //       than System.Threading.Tasks.Parallel.ForEach
        public static void ParallelRange<T>(this T[] array, Action<ArrayView<T>> action)
        {
            Parallel.For(0, array.Length, (start, end) =>
            {
                action(new ArrayView<T>(start, end, array));
            });
        }

        public static void ParallelRange<T>(this Array<T> array, Action<ArrayView<T>> action)
        {
            Parallel.For(0, array.Count, (start, end) =>
            {
                action(array.SubRange(start, end));
            });
        }
    }
}

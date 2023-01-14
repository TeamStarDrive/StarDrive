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
    }
}

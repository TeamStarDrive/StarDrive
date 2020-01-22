using System;
using System.Threading;

namespace Ship_Game
{
    public struct ScopedReadLock : IDisposable
    {
        readonly ReaderWriterLockSlim Lock;

        public ScopedReadLock(ReaderWriterLockSlim slimLock)
        {
            (Lock = slimLock).EnterReadLock();
        }
        public void Dispose()
        {
            Lock.ExitReadLock();
        }
    }

    public struct ScopedWriteLock : IDisposable
    {
        readonly ReaderWriterLockSlim Lock;

        public ScopedWriteLock(ReaderWriterLockSlim slimLock)
        {
            (Lock = slimLock).EnterWriteLock();
        }
        public void Dispose()
        {
            Lock.ExitWriteLock();
        }
    }
}

using System;
using System.Threading;

namespace Ship_Game
{
    public class ScopedReadLock : IDisposable
    {
        private ReaderWriterLockSlim Lock;

        public ScopedReadLock(ReaderWriterLockSlim slimLock)
        {
            (Lock = slimLock).EnterReadLock();
        }
        ~ScopedReadLock()
        {
            Lock?.ExitReadLock();
            Lock = null;
        }
        public void Dispose()
        {
            Lock?.ExitReadLock();
            Lock = null;
            GC.SuppressFinalize(this);
        }
    }

    public class ScopedWriteLock : IDisposable
    {
        private ReaderWriterLockSlim Lock;

        public ScopedWriteLock(ReaderWriterLockSlim slimLock)
        {
            (Lock = slimLock).EnterWriteLock();
        }
        ~ScopedWriteLock()
        {
            Lock?.ExitWriteLock();
            Lock = null;
        }
        public void Dispose()
        {
            Lock?.ExitWriteLock();
            Lock = null;
            GC.SuppressFinalize(this);
        }
    }
}

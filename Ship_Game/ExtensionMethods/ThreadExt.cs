using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

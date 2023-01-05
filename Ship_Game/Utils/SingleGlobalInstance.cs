using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SDUtils;

namespace Ship_Game
{
    public sealed class SingleGlobalInstance : IDisposable
    {
        private Mutex Mutex;
        public readonly bool UniqueInstance; // true if this is an unique game instance

        public SingleGlobalInstance()
        {
            try
            {
                string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly()
                                    .GetCustomAttributes(typeof(GuidAttribute), false)[0]).Value;
                Mutex = new Mutex(true, $"Global\\{{{appGuid}}}", out UniqueInstance);
            }
            catch (AbandonedMutexException)
            {
            }
        }

        public void Dispose()
        {
            Mem.Dispose(ref Mutex);
            GC.SuppressFinalize(this);
        }

        ~SingleGlobalInstance()
        {
            Mem.Dispose(ref Mutex);
        }
    }
}


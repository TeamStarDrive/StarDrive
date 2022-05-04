using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDUtils
{
    public static class Memory
    {
        public static void Dispose<T>(ref T instance) where T : class, IDisposable
        {
            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }
        }
    }
}

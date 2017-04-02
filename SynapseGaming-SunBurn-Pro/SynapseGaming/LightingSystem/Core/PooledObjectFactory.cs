// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.PooledObjectFactory`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Object pool that maintains a list of unused objects
    /// which are recycled to avoid allocating new objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PooledObjectFactory<T> where T : new()
    {
        protected static T[] Empty = new T[0];
        protected T[] Reuse = Empty;
        protected int ReuseSize;

        /// <summary>
        /// Returns an existing unused object if one exists,
        /// otherwise a new object is created.
        /// </summary>
        /// <returns></returns>
        public virtual T New()
        {
            if (ReuseSize > 0)
            {
                T item = Reuse[--ReuseSize];
                Reuse[ReuseSize] = default(T);
                return item;
            }
            return new T();
        }

        /// <summary>
        /// Places an unused object back in the object pool
        /// for reuse during a later call to the New method.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Free(T obj)
        {
            if (ReuseSize == Reuse.Length)
            {
                Array.Resize(ref Reuse, ReuseSize == 0 ? 4 : ReuseSize * 2);
            }
            Reuse[ReuseSize++] = obj;
        }

        /// <summary>Removes all objects from the object pool.</summary>
        public virtual void Clear()
        {
            if (ReuseSize != 0)
            {
                Array.Clear(Reuse, 0, ReuseSize);
                ReuseSize = 0;
            }
        }

    }

    internal class TrackingPool<T> : PooledObjectFactory<T> where T : new()
    {
        protected T[] Tracked = Empty;
        protected int TrackedSize;

        public override T New()
        {
            T obj = base.New();
            if (Tracked.Length == TrackedSize)
            {
                Array.Resize(ref Tracked, TrackedSize == 0 ? 4 : TrackedSize * 2);
            }
            Tracked[TrackedSize++] = obj;
            return obj;
        }

        public override void Free(T obj)
        {
            var c = EqualityComparer<T>.Default;
            for (int i = 0; i < TrackedSize; ++i)
            {
                if (Tracked[i].Equals(obj))
                {
                    // replace this with the last element for a quick removal
                    Tracked[i] = Tracked[--TrackedSize];
                    Tracked[TrackedSize] = default(T);
                    break;
                }
            }
            base.Free(obj);
        }

        // this will move all currently tracked objects into the reuse pool
        public void RecycleAllTracked()
        {
            // move all tracked items to PoolStack
            int trackedSize = TrackedSize;
            if (trackedSize == 0)
                return;

            int poolSize    = ReuseSize;
            int newSize     = poolSize + trackedSize;
            ReuseSize  = newSize;
            T[] items = Reuse;

            if (newSize > Reuse.Length) // optimized array join
            {
                items = new T[newSize];
                for (int i = 0; i < poolSize; ++i)
                    items[i] = Reuse[i];
                Reuse = items;
            }

            for (int i = 0; i < trackedSize; ++i)
            {
                items[i + poolSize] = Tracked[i];
                Tracked[i] = default(T);
            }
            TrackedSize = 0;
        }

        public override void Clear()
        {
            if (ReuseSize != 0)
            {
                Array.Clear(Reuse, 0, ReuseSize);
                ReuseSize = 0;
            }
            if (TrackedSize != 0)
            {
                Array.Clear(Tracked, 0, TrackedSize);
                TrackedSize = 0;
            }
        }
    }


    internal class DisposablePool<T> : TrackingPool<T> where T : IDisposable, new()
    {
        public override void Clear()
        {
            if (ReuseSize != 0)
            {
                Array.Clear(Reuse, 0, ReuseSize);
                ReuseSize = 0;
            }

            // dispose currently live objects
            for (int i = 0; i < TrackedSize; ++i)
            {
                Tracked[i].Dispose();
                Tracked[i] = default(T);
            }
            TrackedSize = 0;
        }
    }

}

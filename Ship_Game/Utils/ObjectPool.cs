using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.Utils
{
    public interface IClearable
    {
        void Clear();
    }

    public class ObjectPool<T> where T : class, IClearable, new()
    {
        readonly Array<T> PooledItems = new Array<T>();

        public ObjectPool()
        {
        }

        /// Clears all pooled items
        public void Destroy()
        {
            PooledItems.Clear();
        }

        /// Creates or reuses a new instance
        public T Create()
        {
            if (PooledItems.IsEmpty)
                return new T();
            return PooledItems.PopLast();
        }

        /// Releases an instance to be reused
        public void Release(T obj)
        {
            obj.Clear();
            PooledItems.Add(obj);
        }

        public void ReleaseAll(T[] objects)
        {
            for (int i = 0; i < objects.Length; ++i)
                objects[i].Clear();
            PooledItems.AddRange(objects);
        }

        /// Pre-allocates objects if needed
        public void Reserve(int count)
        {
            while (PooledItems.Count < count)
                PooledItems.Add(new T());
        }
    }
}

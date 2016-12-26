using System;
using System.Collections.Generic;
using System.Text;

namespace Ship_Game
{
    /// <summary>
    /// This is a custom wrapper of List, to make debugging easier
    /// </summary>
    public class Array<T> : List<T>
    {
        public Array()
        {
        }

        public Array(int capacity) : base(capacity)
        {
        }

        public Array(IEnumerable<T> collection) : base(collection)
        {
        }

        public new T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException($"Index [{index}] out of range (len={Count}) in {ToString()}");
                return base[index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException($"Index [{index}] out of range (len={Count}) in {ToString()}");
                base[index] = value;
            }
        }

        public override string ToString()
        {
            return GetType().GenericName();
        }
    }
}

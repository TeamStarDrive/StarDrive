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
                    throw new IndexOutOfRangeException($"Index [{index}] out of range (len={Count}) {ToString()}");
                return base[index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException($"Index [{index}] out of range (len={Count}) {ToString()}");
                base[index] = value;
            }
        }

        private static void GenericName(StringBuilder sb, Type type)
        {
            if (!type.IsGenericType)
            {
                sb.Append(type.Name);
                return;
            }

            sb.Append(type.Name.Split('`')[0]).Append('<');
            var args = type.GenericTypeArguments;
            for (int i = 0; i < args.Length; ++i)
            {
                GenericName(sb, args[i]);
                if (i != args.Length - 1) sb.Append(',');
            }
            sb.Append('>');
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            GenericName(sb, GetType());
            return sb.ToString();
        }
    }
}

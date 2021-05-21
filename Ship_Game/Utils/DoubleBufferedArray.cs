using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Implements a double-buffering algorithm in order to provide Thread safety
    /// </summary>
    public class DoubleBufferedArray<T> where T : class
    {
        /// <summary>
        /// Thread safe access to the Array items
        /// </summary>
        public Array<T> Items = new Array<T>();

        Array<T> Back = new Array<T>();

        public void Add(T item)
        {
            Back.Add(item);
        }

        public void Remove(T item)
        {
            Back.RemoveRef(item);
        }

        public void Update()
        {
            Array<T> old = Items;
            Items = Back;
            Back = old;
            
            // now sync the new BackBuffer with Items
            Back.Clear();
            Back.AddRange(Items);
        }
    }
}

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
        /// Thread safe access to the Array items.
        /// Only call this ONCE to get the safe reference, do NOT call it multiple times!
        /// </summary>
        /// <example>
        /// Ship[] ships = Ships.GetItems(); // ONLY GET IT ONCE
        /// if (ships.Length > 0)
        /// {
        ///     Ship ship = ships[0];
        ///     ...
        /// }
        /// </example>
        public T[] GetItems() => Front;
        
        T[] Front = Empty<T>.Array;
        bool Changed;
        readonly Array<T> Back;

        public DoubleBufferedArray()
        {
             Back = new Array<T>();
        }

        public DoubleBufferedArray(int capacity)
        {
             Back = new Array<T>(capacity);
        }
        
        /// <summary>
        /// Applies any changes made by Add/Remove/etc
        /// </summary>
        public void ApplyChanges()
        {
            if (Changed)
            {
                Changed = false;
                Front = Back.ToArray();
            }
        }

        /// <summary>
        /// Clears this array and immediately applies the changes
        /// </summary>
        public void ClearAndApply()
        {
            Changed = false;
            Front = Empty<T>.Array;
            Back.Clear();
        }

        /// <summary>
        /// Sets a single value at index, but only if it actually changed!
        /// </summary>
        public void Set(int index, T item)
        {
            if (!object.ReferenceEquals(Back[index], item))
            {
                Back[index] = item;
                Changed = true;
            }
        }

        public void Add(T item)
        {
            Back.Add(item);
            Changed = true;
        }

        public void AddUnique(T item)
        {
            Changed = Back.AddUniqueRef(item);
            Changed = true;
        }

        public void Remove(T item)
        {
            Changed = Back.RemoveRef(item);
        }

        public void Resize(int newSize)
        {
            if (Back.Count != newSize)
            {
                Back.Resize(newSize);
                Changed = true;
            }
        }
    }
}

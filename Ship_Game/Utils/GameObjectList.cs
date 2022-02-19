using System;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Implements a double-buffering list for managing a list of GameObjects
    ///
    /// The GameObjects are removed when they become inactive
    ///
    /// Items can be Added 
    ///
    /// The front buffer is an array copy of the back-buffer and is updated by ApplyChanges()
    /// </summary>
    public class GameObjectList<T> where T : GameplayObject
    {
        T[] Front = Empty<T>.Array;
        readonly Array<T> Back;
        bool Changed;

        public GameObjectList()
        {
            Back = new Array<T>();
        }

        /// <summary>
        /// Gives the total number of objects in BackBuffer,
        /// but not all of these may be accessible.
        ///
        /// For true number of currently available objects, use `GetItems().Length`
        /// </summary>
        public int NumBackingItems => Back.Count;

        /// <summary>Find GameObject by Id</summary>
        /// <returns>GameObject or null</returns>
        public T Find(int id)
        {
            if (id <= 0)
                return null;

            var items = GetItems();
            for (int i = 0; i < items.Length; ++i)
                if (items[i].Id == id)
                    return items[i];

            return null;
        }

        /// <summary>Find GameObject by Id</summary>
        /// <returns>true if GameObject was found</returns>
        public bool Find(int id, out T found)
        {
            return (found = Find(id)) != null;
        }

        /// <returns>TRUE if GameObject with `id` exists in this list</returns>
        public bool Contains(int id)
        {
            return Find(id) != null;
        }
        
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
        public T[] GetItems()
        {
            if (!Changed)
                return Front;

            ApplyChanges();
            return Front;
        }

        /// <summary>
        /// Adds an item to the back buffer
        /// </summary>
        public void Add(T item)
        {
            lock (Back)
            {
                Changed = true;
                Back.Add(item);
            }
        }

        /// <summary>
        /// Applies any changes made to the back-buffer to the front-buffer
        /// </summary>
        public void ApplyChanges()
        {
            if (Changed)
            {
                lock (Back)
                {
                    if (Changed)
                    {
                        Changed = false;
                        Front = Back.ToArray(); // SLOW
                    }
                }
            }
        }

        /// <summary>
        /// Removes GameplayObjects with Active == false
        /// </summary>
        public void RemoveInActiveAndApplyChanges()
        {
            lock (Back)
            {
                Changed = false;
                Back.RemoveInActiveObjects();
                Front = Back.ToArray(); // SLOW
            }
        }

        /// <summary>
        /// Clears this array and immediately applies the changes
        /// </summary>
        public void ClearAndApplyChanges()
        {
            lock (Back)
            {
                Changed = false;
                Back.Clear();
                Front = Empty<T>.Array;
            }
        }
    }
}

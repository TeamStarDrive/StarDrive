using System;

namespace Ship_Game.Utils
{
    public class ObjectRecycler<T> : Array<T>
    {
        Array<T> InActiveObjects = new Array<T>();

        public bool Remove(T item, Action<T> cleanItem)
        {
            bool found = base.Remove(item);
            if (found)
            {
                cleanItem(item);
                InActiveObjects.Add(item);
            }
            return found;
        }

        public T GetRecycledItem(Func<T> createItem)
        {
            if (InActiveObjects.TryPopLast(out T item))
            {
                return item;
            }
            return createItem();
        }

        public T GetRecycledItem()
        {
            if (InActiveObjects.TryPopLast(out T item))
                return item;
            else
                return default(T);
        }

        public void CleanRecycled(Action<T> cleanObject) => InActiveObjects.ForEach(cleanObject);

        public void PurgeRecycled(Action<T> cleanObject)
        {
            CleanRecycled(cleanObject);
            InActiveObjects.Clear();
        }
    }
}
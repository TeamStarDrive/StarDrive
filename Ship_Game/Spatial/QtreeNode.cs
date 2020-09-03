using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class QtreeNode
    {
        public readonly float X, Y, LastX, LastY;
        public QtreeNode NW, NE, SE, SW;
        public int Count;
        public SpatialObj[] Items;
        public QtreeNode(float x, float y, float lastX, float lastY)
        {
            X = x; Y = y;
            LastX = lastX; LastY = lastY;
            Items = Quadtree.NoObjects;
        }
        public void Add(ref SpatialObj obj)
        {
            int count = Count;
            SpatialObj[] oldItems = Items;
            if (oldItems.Length == count)
            {
                if (count == 0)
                {
                    var newItems = new SpatialObj[Quadtree.CellThreshold];
                    newItems[count] = obj;
                    Items = newItems;
                    ++Count;
                }
                else // oldItems.Length == Count
                {
                    //Array.Resize(ref Items, Count * 2);
                    var newItems = new SpatialObj[oldItems.Length * 2];
                    int i = 0;
                    for (; i < oldItems.Length; ++i)
                        newItems[i] = oldItems[i];
                    newItems[count] = obj;
                    Items = newItems;
                    ++Count;
                }
            }
            else
            {
                oldItems[count] = obj;
                ++Count;
            }
        }

        // Because SwapLast reorders elements, we decrement the ref index to allow loops to continue
        // naturally. Index is not decremented if it is the last element
        public void RemoveAtSwapLast(ref int index)
        {
            int newCount = Count-1;
            if (newCount < 0) // FIX: this is a threading issue, the item was already removed
                return; 

            Count = newCount;
            ref SpatialObj last = ref Items[newCount];
            if (index != newCount) // only swap and change ref index if it wasn't the last element
            {
                Items[index] = last;
                --index;
            }
            last.Obj = null; // prevent zombie objects
            if (newCount == 0) Items = Quadtree.NoObjects;
        }

        public bool Overlaps(ref Vector2 topLeft, ref Vector2 topRight)
        {
            return X <= topRight.X && LastX > topLeft.X
                                   && Y <= topRight.Y && LastY > topLeft.Y;
        }
    }
}
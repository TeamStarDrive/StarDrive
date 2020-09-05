using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class QtreeNode
    {
        public float X, Y, LastX, LastY;
        public QtreeNode NW, NE, SE, SW;
        public int Count;
        public SpatialObj[] Items;

        public QtreeNode(float x, float y, float lastX, float lastY)
        {
            X = x; Y = y;
            LastX = lastX; LastY = lastY;
            Items = Quadtree.NoObjects;
        }

        public void InitializeForReuse(float x, float y, float lastX, float lastY)
        {
            X = x; Y = y;
            LastX = lastX; LastY = lastY;

            if (Count != 0)
            {
                Array.Clear(Items, 0, Count);
                Count = 0;
            }
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

        public bool Overlaps(ref Vector2 topLeft, ref Vector2 topRight)
        {
            return X <= topRight.X && LastX > topLeft.X
                                   && Y <= topRight.Y && LastY > topLeft.Y;
        }
    }
}
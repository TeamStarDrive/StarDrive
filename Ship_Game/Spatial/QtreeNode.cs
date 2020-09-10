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
        public int Id;
        public int Level;

        public int TotalTreeDepthCount;

        public QtreeNode(int level, float x, float y, float lastX, float lastY)
        {
            X = x; Y = y;
            LastX = lastX; LastY = lastY;
            Items = Quadtree.NoObjects;
            Level = level;
        }

        public override string ToString()
        {
            return $"ID={Id} L{Level} N={Count} TN={TotalTreeDepthCount} X={X} LX={LastX} Y={Y} LY={LastY}";
        }

        public void InitializeForReuse(int level, float x, float y, float lastX, float lastY)
        {
            X = x; Y = y;
            LastX = lastX; LastY = lastY;
            NW = NE = SE = SW = null;
            Level = level;

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
            ++TotalTreeDepthCount;
        }

        public bool Overlaps(in Vector2 topLeft, in Vector2 botRight)
        {
            return X <= botRight.X && LastX > topLeft.X
                && Y <= botRight.Y && LastY > topLeft.Y;
        }

        public bool Overlaps(in SpatialObj o)
        {
            return X <= o.LastX && LastX > o.X
                && Y <= o.LastY && LastY > o.Y;
        }
    }
}
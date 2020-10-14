using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class QtreeNode
    {
        public AABoundingBox2D AABB;
        public QtreeNode NW, NE, SE, SW;
        public int Count;
        public SpatialObj[] Items;
        public int Id;
        public int Level;
        public int TotalTreeDepthCount;

        public QtreeNode(int level, in AABoundingBox2D bounds)
        {
            AABB = bounds;
            Items = Quadtree.NoObjects;
            Level = level;
        }

        public override string ToString()
        {
            return $"ID={Id} L{Level} N={Count} TN={TotalTreeDepthCount} {AABB}";
        }

        public void InitializeForReuse(int level, in AABoundingBox2D bounds)
        {
            AABB = bounds;
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
    }
}
using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class QtreeNode
    {
        public static readonly int[] NoObjects = new int[0];

        public AABoundingBox2D AABB;
        public QtreeNode NW, NE, SE, SW;
        public int Count;
        public int[] Items;
        public int Id;
        public int Level;

        public QtreeNode(int level, in AABoundingBox2D bounds)
        {
            AABB = bounds;
            Items = NoObjects;
            Level = level;
        }

        public override string ToString()
        {
            return $"ID={Id} L{Level} N={Count} {AABB}";
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

        public void Add(int objectId)
        {
            int count = Count;
            int[] oldItems = Items;
            if (oldItems.Length == count)
            {
                if (count == 0)
                {
                    var newItems = new int[Quadtree.CellThreshold];
                    newItems[count] = objectId;
                    Items = newItems;
                    Count = 1;
                }
                else // oldItems.Length == Count
                {
                    //Array.Resize(ref Items, Count * 2);
                    var newItems = new int[oldItems.Length * 2];
                    for (int i = 0; i < oldItems.Length; ++i)
                        newItems[i] = oldItems[i];
                    newItems[count] = objectId;
                    Items = newItems;
                    Count = count+1;
                }
            }
            else
            {
                oldItems[count] = objectId;
                Count = count+1;
            }
        }
    }
}
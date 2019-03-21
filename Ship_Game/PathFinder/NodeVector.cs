using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.PathFinder
{
     /**
      * @brief Custom tailored PathFinder vector optimized specifically for A*
      * Items are sorted in descending order by FScore.
      * Item repositioning, which is very frequent in A*, is optimized to
      */
    public class NodeVector
    {
        Node[] Data;
        public int Size { get; private set; }
        int Capacity;

        // some statistics... to get better idea of what to optimize
        public static float AvgInsertShift; // average number of elements shifted during insert
        public static float AvgReposRev;    // average number of elements shifted reverse during repos
        public static float AvgReposFwd;    // average number of elements shifted forward during repos

        public NodeVector() {}
        public NodeVector(int cap) { reserve(cap); }
        public void      Clear()        { Size = 0; }
        public int       size()         { return Size; }
        public bool      empty()        { return Size == 0;   }
        public Node pop()          { return Data[--Size];}
        public Node get(int index) { return Data[index]; }

        public void reserve(int capacity)
        {
            var data = new Node[Capacity = capacity];
            if (Data != null) Array.Copy(Data, 0, data, 0, Size);
            Data = data;
        }

        /**
         * @brief Performs a binary insertion
         * @param item New item to insert
         */
        public void insert(Node item)
        {
            int imax = Size;
            int imin = 0;
            Log.Assert(imax < Capacity, "imax must be less than capacity");
            float itemScore = item.FScore;

            // binary search for appropriate index
            while (imin < imax)
            {
                int imid = (imin + imax) / 2;
                if (Data[imid].FScore > itemScore)
                    imin = ++imid;
                else
                    imax = imid;
            }

            AvgInsertShift = (AvgInsertShift + (Size - imin)) * 0.5f;

            // shift everything else forward once:
            Array.Copy(Data, imin, Data, imin+1, Size);
            Data[imin] = item;
            ++Size;
        }

        /**
         * @brief instead of erase/insert, we reposition the item because
         *        we know Astar most often repositions items at the end of openlist
         * @param item Existing item to reposition/update
         */
        public void repos(Node item)
        {
            for (int i = Size-1; i >= 0; --i) // reverse iter
            {
                if (Data[i] != item) // ref match is enough
                    continue;

                int istart = i;
                float itemScore = item.FScore;
                if (i != 0) // backwards movement
                {
                    float prevScore = Data[i-1].FScore;
                    if (prevScore == itemScore) return; // it's sorted
                    if (prevScore < itemScore) // we have to move backwards
                    {
                        do // shift backward until sorted
                        {
                            Node prev = Data[--i];
                            if (prev.FScore >= itemScore) break;
                            Data[i]   = item;
                            Data[i+1] = prev;
                        } while (i > 0);
                        AvgReposRev = (AvgReposRev + (istart-i)) * 0.5f;
                        return;
                    }
                }
                int last = Size-1;
                if (i < last) // forward shift
                {
                    do // shift forward until sorted
                    {
                        Node next = Data[++i];
                        if (itemScore >= next.FScore) break; // it's sorted
                        Data[i]   = item;
                        Data[i-1] = next;
                    } while (i < last);
                }
                AvgReposFwd = (AvgReposFwd + (i-istart)) * 0.5f;
                return; // done. openlist is sorted
            }
        }
    }

}

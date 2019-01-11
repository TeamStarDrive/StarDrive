using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public static class ListExt
    {
        // Shuffles all items using a temporary random generator
        public static void Shuffle<T>(this IList<T> list, int seed = 0)
        {
            Random random = seed == 0 ? new Random() : new Random(seed);
            int n = list.Count;
            while (n > 1) {  
                --n;  
                int k = random.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }
    }
}

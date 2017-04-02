using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public static class StringExt
    {
        public static bool Empty(this string str)
        {
            // ReSharper disable once ReplaceWithStringIsNullOrEmpty
            return str == null || str.Length == 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public static class StringExtensions
    {
        public static StringBuilder Append(this StringBuilder sb, string a, string b)
        {
            return sb.Append(a).Append(b);
        }
        public static StringBuilder Append(this StringBuilder sb, string a, string b, string c)
        {
            return sb.Append(a).Append(b).Append(c);
        }
        public static StringBuilder Append(this StringBuilder sb, string a, string b, string c, string d)
        {
            return sb.Append(a).Append(b).Append(c).Append(d);
        }
    }
}

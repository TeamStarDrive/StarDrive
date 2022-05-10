using System.Text;

namespace SDUtils
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

        public static string NormalizedFilePath(this string text)
        {
            return text.Replace('\\', '/');
        }
    }
}

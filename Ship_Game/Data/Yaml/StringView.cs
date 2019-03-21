using System;

namespace Ship_Game.Data.Yaml
{
    public struct StringView : IEquatable<StringView>
    {
        public int Start;
        public int Length;
        public char[] Chars;

        public char Char0 => Chars[Start];
        public string Text => new string(Chars, Start, Length);
        public char this[int index] => Chars[Start + index];

        public override string ToString() => $"Length:{Length} Text:\"{Text}\"";

        public StringView(char[] chars)
        {
            Start = 0;
            Length = chars.Length;
            Chars = chars;
        }

        public StringView(char[] chars, int start, int length)
        {
            Start = start;
            Length = length;
            Chars = chars;
        }

        public void Clear()
        {
            Start = Length = 0;
            Chars = Empty<char>.Array;
        }

        public void SkipWhiteSpace(out int depth)
        {
            depth = 0;
            for (; Length > 0; ++Start, --Length)
            {
                char c = Chars[Start];
                if      (c == ' ')  depth += 1;
                else if (c == '\t') depth += 2;
                else break;
            }
        }

        public void TrimStart()
        {
            for (; Length > 0; ++Start, --Length)
            {
                char c = Chars[Start];
                if (c != ' ' && c != '\t') break;
            }
        }

        public void TrimEnd()
        {
            int i = (Start + Length) - 1;
            for (; i >= Start; --Length, --i)
            {
                char c = Chars[i];
                if (c != ' ' && c != '\t') break;
            }
        }

        public bool StartsWith(string startsWith)
        {
            if (startsWith.Length > Length)
                return false;
            int s = Start;
            for (int i = 0; i < startsWith.Length; ++i)
                if (Chars[s + i] != startsWith[i])
                    return false;
            return true;
        }

        public static bool operator==(in StringView a, string b)
        {
            if (b == null || a.Length != b.Length)
                return false;
            int end = a.Start + a.Length;
            for (int i = a.Start, j = 0; i < end; ++i, ++j)
                if (a.Chars[i] != b[j])
                    return false;
            return true;
        }

        public static bool operator!=(in StringView a, string b)
        {
            if (b == null || a.Length != b.Length)
                return true;
            int end = a.Start + a.Length;
            for (int i = a.Start, j = 0; i < end; ++i, ++j)
                if (a.Chars[i] != b[j])
                    return true;
            return false;
        }

        
        public bool Equals(StringView other)
        {
            if (Length != other.Length)
                return false;
            int s1 = Start;
            int s2 = other.Start;
            for (int i = 0; i < Length; ++i)
                if (Chars[s1 + i] != other.Chars[s2 + i])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is StringView other && Equals(other);
        }

        static uint Fnv1AHash(char[] chars)
        {
            uint hash = 0x811c9dc5;
            for (int i = 0; i < chars.Length; ++i)
            {
                hash = hash ^ chars[i];
                hash = hash * 16777619;
            }
            return hash;
        }

        public override int GetHashCode()
        {
            int hashCode = Start;
            hashCode = (hashCode * 397) ^ Length;
            hashCode = (hashCode * 397) ^ (Chars != null ? Chars.GetHashCode() : 0);
            return hashCode;
        }

        public void Skip(int count)
        {
            int n = Math.Min(count, Length);
            Start  += n;
            Length -= n;
        }

        public void ChompFirst()
        {
            if (Length > 0)
            {
                ++Start;
                --Length;
            }
        }

        public char PopFirst()
        {
            char c = Chars[Start];
            ++Start;
            --Length;
            return c;
        }

        public int IndexOf(char c)
        {
            int end = Start + Length;
            for (int i = Start; i < end; ++i)
                if (Chars[i] == c)
                    return i - Start;
            return -1;
        }

        public unsafe int ToInt()
        {
            fixed (char* str = Chars) return ToInt(str + Start, Length);
        }

        public unsafe double ToDouble()
        {
            fixed (char* str = Chars) return ToDouble(str + Start, Length);
        }

        public unsafe float ToFloat()
        {
            fixed (char* str = Chars) return (float)ToDouble(str + Start, Length);
        }

        public static unsafe int ToInt(string s)
        {
            fixed (char* str = s) return ToInt(str, s.Length);
        }

        public static unsafe double ToDouble(string s)
        {
            fixed (char* str = s) return ToDouble(str, s.Length);
        }

        public static unsafe float ToFloat(string s)
        {
            fixed (char* str = s) return (float)ToDouble(str, s.Length);
        }

        static unsafe int ToInt(char* start, int count)
        {
            char* s = start;
            char* e = s + count;
            int  intPart  = 0;
            bool negative = false;
            char ch       = *s;

            if (ch == '-')
            { negative = true; ++s; } // change sign and skip '-'
            else if (ch == '+')  ++s; // ignore '+'

            for (; s < e && '0' <= (ch = *s) && ch <= '9'; ++s) {
                intPart = (intPart << 3) + (intPart << 1) + (ch - '0'); // intPart = intPart*10 + digit
            }
            if (negative) intPart = -intPart; // twiddle sign

            return intPart;
        }

        static unsafe double ToDouble(char* start, int count)
        {
            char* s = start;
            char* e = s + count;
            long power    = 1;
            long intPart  = 0;
            bool negative = false;
            char ch       = *s;

            if (ch == '-')
            { negative = true; ++s; } // change sign and skip '-'
            else if (ch == '+')  ++s; // ignore '+'

            for (; s < e && '0' <= (ch = *s) && ch <= '9'; ++s) {
                intPart = (intPart << 3) + (intPart << 1) + (ch - '0'); // intPart = intPart*10 + digit
            }

            int exponent = 0;

            // @note The '.' is actually the sole reason for this function in the first place. Locale independence.
            if (ch == '.') { /* fraction part follows*/
                while (++s < e) {
                    ch = *s;
                    if (ch == 'e') { // parse e-016 e+3 etc.
                        ++s;
                        int esign = (*s++ == '+') ? +1 : -1;
                        exponent = (*s++ - '0');
                        exponent = (exponent << 3) + (exponent << 1) + (*s++ - '0');
                        exponent = (exponent << 3) + (exponent << 1) + (*s++ - '0');
                        exponent *= esign;
                        break;
                    }
                    if (ch < '0' || '9' < ch)
                        break;
                    intPart = (intPart << 3) + (intPart << 1) + (ch - '0'); // intPart = intPart*10 + digit
                    power   = (power   << 3) + (power   << 1);              // power *= 10
                }
            }
            double result = power == 1 ? intPart : intPart / (double)power;
            if (exponent > 0)
                result *= Math.Pow(10.0, exponent);
            return result * (negative ? -1.0 : +1.0);
        }
    }
}

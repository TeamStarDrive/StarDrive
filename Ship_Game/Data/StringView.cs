using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data
{
    public struct StringView : IEquatable<StringView>
    {
        public int Start;
        public int Length;
        public string Str;

        public char Char0 => Str[Start];
        public string Text => Str.Substring(Start, Length);

        public override string ToString() => $"Length:{Length} Text:\"{Text}\"";

        public StringView(string str)
        {
            Start = 0;
            Length = str.Length;
            Str = str;
        }

        public StringView(int start, int length, string str)
        {
            Start = start;
            Length = length;
            Str = str;
        }

        public void Clear()
        {
            Start = Length = 0;
            Str = "";
        }

        public void SkipWhiteSpace(out int depth)
        {
            depth = 0;
            for (; Length > 0; ++Start, --Length)
            {
                char c = Str[Start];
                if      (c == ' ')  depth += 1;
                else if (c == '\t') depth += 2;
                else break;
            }
        }

        public void TrimStart()
        {
            for (; Length > 0; ++Start, --Length)
            {
                char c = Str[Start];
                if (c != ' ' && c != '\t') break;
            }
        }

        public void TrimEnd()
        {
            int i = (Start + Length) - 1;
            for (; i >= Start; --Length, --i)
            {
                char c = Str[i];
                if (c != ' ' && c != '\t') break;
            }
        }

        public bool StartsWith(string startsWith)
        {
            if (startsWith.Length > Length)
                return false;
            int s = Start;
            for (int i = 0; i < startsWith.Length; ++i)
                if (Str[s + i] != startsWith[i])
                    return false;
            return true;
        }

        public static bool operator==(in StringView a, string b)
        {
            if (b == null || a.Length != b.Length)
                return false;
            int end = a.Start + a.Length;
            for (int i = a.Start, j = 0; i < end; ++i, ++j)
                if (a.Str[i] != b[j])
                    return false;
            return true;
        }

        public static bool operator!=(in StringView a, string b)
        {
            if (b == null || a.Length != b.Length)
                return true;
            int end = a.Start + a.Length;
            for (int i = a.Start, j = 0; i < end; ++i, ++j)
                if (a.Str[i] != b[j])
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
                if (Str[s1 + i] != other.Str[s2 + i])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StringView other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashCode = Start;
            hashCode = (hashCode * 397) ^ Length;
            hashCode = (hashCode * 397) ^ (Str != null ? Str.GetHashCode() : 0);
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
            char c = Str[Start];
            ++Start;
            --Length;
            return c;
        }

        public int IndexOf(char c)
        {
            int end = Start + Length;
            for (int i = Start; i < end; ++i)
                if (Str[i] == c)
                    return Start - i;
            return -1;
        }

        public unsafe int ToInt()
        {
            fixed (char* str = Str)
            {
                char* s = str + Start;
                char* e = s + Length;
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
        }

        public unsafe double ToDouble()
        {
            fixed (char* str = Str)
            {
                char* s = str + Start;
                char* e = s + Length;
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

        public float ToFloat()
        {
            return (float)ToDouble();
        }
    }
}

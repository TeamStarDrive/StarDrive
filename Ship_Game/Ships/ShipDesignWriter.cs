using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public sealed class ShipDesignWriter
    {
        StringBuilder Sb;

        public ShipDesignWriter()
        {
            Sb = new StringBuilder();
        }

        public void Write<T>(string key, T value)
        {
            Sb.Append(key).Append('=').Append(value).Append('\n');
        }

        public void Write(string key, bool value)
        {
            Sb.Append(key).Append('=').Append(value?"true":"false").Append('\n');
        }

        public void Write(string key, string value)
        {
            if (value.NotEmpty())
            {
                Sb.Append(key).Append('=').Append(value).Append('\n');
            }
        }
        
        // key=values0;values1;values2
        public void Write(string key, string[] values)
        {
            if (values.Length != 0)
            {
                Sb.Append(key).Append('=');
                for (int i = 0; i < values.Length; ++i)
                {
                    Sb.Append(values[i]);
                    if (i != values.Length - 1)
                        Sb.Append(';');
                }
                Sb.Append('\n');
            }
        }

        public void WriteLine(string value)
        {
            Sb.Append(value).Append('\n');
        }

        public void WriteLine()
        {
            Sb.Append('\n');
        }

        public void Write(string value)
        {
            Sb.Append(value);
        }

        public void Write(char ch)
        {
            Sb.Append(ch);
        }

        public void FlushToFile(FileInfo file)
        {
            File.WriteAllText(file.FullName, Sb.ToString(), Encoding.UTF8);
        }

        public override string ToString()
        {
            return Sb.ToString();
        }

        public byte[] GetASCIIBytes()
        {
            char[] chars = new char[Sb.Length];
            Sb.CopyTo(0, chars, 0, chars.Length);

            // ship design text is all ASCII, so convert is easy
            byte[] bytes = new byte[chars.Length];
            for (int i = 0; i < chars.Length; ++i)
                bytes[i] = (byte)chars[i];

            return bytes;
        }
    }
}

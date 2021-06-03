using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public sealed class ShipDataWriter
    {
        StringBuilder Sb;

        public ShipDataWriter()
        {
            Sb = new StringBuilder();
        }

        public void Write<T>(string key, T value)
        {
            Sb.Append(key).Append('=').Append(value).Append('\n');
        }

        public void Write(string key, string value)
        {
            if (value.NotEmpty())
            {
                Sb.Append(key).Append('=').Append(value).Append('\n');
            }
        }

        public void WriteLine(string value)
        {
            Sb.Append(value).Append('\n');
        }

        public void FlushToFile(FileInfo file)
        {
            File.WriteAllText(file.FullName, Sb.ToString(), Encoding.UTF8);
        }
    }
}

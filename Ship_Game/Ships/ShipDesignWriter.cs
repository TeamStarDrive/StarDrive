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
        // NOTE: while this implementation is definitely not faster,
        //       it uses up to 2-3x less memory than StringBuilder
        //       in the desired cycle of: 1) create, 2) append, 3) ToBytes
        Chunk Head;
        Chunk Tail;
        int Count;

        class Chunk
        {
            public Chunk Next;
            public readonly byte[] Bytes = new byte[512];
            public int Count;
            public int Remaining => 512 - Count;
        }

        public ShipDesignWriter()
        {
            Head = Tail = new Chunk();
        }

        public void Clear()
        {
            Head = Tail = new Chunk();
            Count = 0;
        }

        Chunk AddTail()
        {
            var newTail = new Chunk();
            Tail.Next = newTail;
            Tail = newTail;
            return newTail;
        }

        public unsafe void Write(string value)
        {
            int length = value.Length;
            Chunk tail = Tail;

            fixed (char* chars = value)
            {
                if (tail.Remaining >= length)
                {
                    fixed (byte* bytes = tail.Bytes)
                    {
                        for (int i = 0; i < length; ++i)
                            bytes[tail.Count++] = (byte)chars[i];
                    }
                }
                else
                {
                    int srcIdx = 0;
                    while (srcIdx != length)
                    {
                        int free = tail.Remaining;
                        fixed (byte* bytes = tail.Bytes)
                        {
                            for (int i = 0; i < free && srcIdx != length; ++i)
                                bytes[tail.Count++] = (byte)chars[srcIdx++];
                        }
                        tail = AddTail();
                    }
                }
            }

            Count += length;
        }

        public void Write(char ch)
        {
            Chunk tail = Tail;
            if (tail.Remaining == 0)
                tail = AddTail();

            tail.Bytes[tail.Count++] = (byte)ch;
            ++Count;
        }

        public void FlushToFile(FileInfo file)
        {
            byte[] bytes = GetASCIIBytes();
            using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            fs.Write(bytes, 0, bytes.Length);
        }

        public override string ToString()
        {
            byte[] bytes = GetASCIIBytes();
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }

        public byte[] GetASCIIBytes()
        {
            byte[] bytes = new byte[Count];
            int count = 0;
            Chunk chunk = Head;
            do
            {
                Array.Copy(chunk.Bytes, 0, bytes, count, chunk.Count);
                count += chunk.Count;
                chunk = chunk.Next;
            }
            while (chunk != null);
            return bytes;
        }

        public void Write<T>(string key, T value)
        {
            Write(key);
            Write('=');
            Write(value.ToString());
            Write('\n');
        }

        public void Write(string key, bool value)
        {
            Write(key);
            Write(value ? "=true\n" : "=false\n");
        }

        public void Write(string key, string value)
        {
            if (value.NotEmpty())
            {
                Write(key);
                Write('=');
                Write(value);
                Write('\n');
            }
        }

        // key=values0;values1;values2\n
        public void Write(string key, string[] values)
        {
            Write(key);
            Write('=');
            Write(values);
            Write('\n');
        }

        // values0;values1;values2
        public void Write(string[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                Write(values[i]);
                if (i != values.Length - 1)
                    Write(';');
            }
        }

        public void WriteLine()
        {
            Write('\n');
        }

        public void WriteLine(string value)
        {
            Write(value);
            Write('\n');
        }

        public void WriteLine(string[] values)
        {
            Write(values);
            Write('\n');
        }
    }
}

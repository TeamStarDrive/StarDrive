using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This writer is designed to be reused multiple times
    /// between serialization calls.
    ///
    /// Clear() can be called to reset the state, however memory
    /// will not be freed - it is cached
    /// </summary>
    public sealed unsafe class ShipDesignWriter : IDisposable
    {
        // NOTE: This implementation is both faster and more memory efficient
        //       than using a StringBuilder for this specific use case

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct ByteBuffer
        {
            public byte* Data;
            public int Capacity;
            public int Size;
        }

        [DllImport("SDNative.dll")]
        static extern ByteBuffer* ByteBufferNew(int defaultCapacity);
        
        [DllImport("SDNative.dll")]
        static extern void ByteBufferDelete(ByteBuffer* b);

        [DllImport("SDNative.dll")]
        static extern void ByteBufferCopy(ByteBuffer* b, byte[] dst);

        [DllImport("SDNative.dll")]
        static extern void ByteBufferWriteC(ByteBuffer* b, char ch);

        [DllImport("SDNative.dll")]
        static extern void ByteBufferWriteS(ByteBuffer* b,
            [MarshalAs(UnmanagedType.LPWStr)] string str, int len
        );

        [DllImport("SDNative.dll")]
        static extern void ByteBufferWriteKV(ByteBuffer* b,
            [MarshalAs(UnmanagedType.LPWStr)] string key, int keylen,
            [MarshalAs(UnmanagedType.LPWStr)] string val, int vallen
        );

        ByteBuffer* Buffer;
        public int Capacity => Buffer->Capacity;

        public ShipDesignWriter(int initialCapacity = 4096)
        {
            Buffer = ByteBufferNew(initialCapacity);
        }

        ~ShipDesignWriter()
        {
            Destroy();
        }

        void Destroy()
        {
            ByteBufferDelete(Buffer);
            Buffer = null;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            Buffer->Size = 0;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Buffer->Data, Buffer->Size);
        }

        // NOTE: This must ALWAYS COPY the bytes
        public byte[] GetASCIIBytes()
        {
            byte[] bytes = new byte[Buffer->Size];
            ByteBufferCopy(Buffer, bytes);
            return bytes;
        }

        public void FlushToFile(FileInfo file)
        {
            using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            byte[] bytes = GetASCIIBytes();
            fs.Write(bytes, 0, bytes.Length);
        }

        // value
        public void Write(string value)
        {
            ByteBufferWriteS(Buffer, value, value.Length);
        }

        public void Write(char ch)
        {
            // fastpath: already enough capacity
            if (Buffer->Size < Buffer->Capacity)
            {
                Buffer->Data[Buffer->Size++] = (byte)ch;
            }
            else
            {
                ByteBufferWriteC(Buffer, ch);
            }
        }

        // key=value\n
        public void Write<T>(string key, T value)
        {
            string val = value.ToString();
            ByteBufferWriteKV(Buffer, key, key.Length, val, val.Length);
        }

        // key=true|false\n
        public void Write(string key, bool value)
        {
            string val = value ? "=true\n" : "=false\n";
            ByteBufferWriteKV(Buffer, key, key.Length, val, val.Length);
        }

        // if value then: key=value\n
        public void Write(string key, string value)
        {
            if (value.NotEmpty())
            {
                ByteBufferWriteKV(Buffer, key, key.Length, value, value.Length);
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

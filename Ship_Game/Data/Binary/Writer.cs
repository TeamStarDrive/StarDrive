using System;
using System.IO;
using System.Text;

namespace Ship_Game.Data.Binary
{
    public class Writer : IDisposable
    {
        Stream OutStream;
        readonly Encoding Encoding;
        int BufferLen;
        byte[] Buffer;
        char[] StringBuffer;

        public Writer(Stream outStream)
        {
            OutStream = outStream;
            Encoding = Encoding.UTF8;
            Buffer = new byte[256 * 1024];
            StringBuffer = new char[256 * 1024];
        }

        ~Writer() { Destroy(); }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            Flush();
            OutStream.Close();
            Buffer = null;
            OutStream = null;
            StringBuffer = null;
        }

        public void Flush()
        {
            if (BufferLen > 0)
                FlushToUnderlying();
            OutStream.Flush();
        }

        void FlushToUnderlying()
        {
            OutStream.Write(Buffer, 0, BufferLen);
            BufferLen = 0;
        }

        // Increases BufferLen and returns offset where to write data
        // Flushes underlying buffer automatically
        int EnsureCapacity(int count)
        {
            int len = BufferLen;
            int newLength = len + count;
            if (newLength > Buffer.Length)
            {
                OutStream.Write(Buffer, 0, BufferLen);
                BufferLen = count;
                return 0;
            }
            BufferLen = newLength;
            return len;
        }

        public void Write(byte[] bytes, int offset, int count)
        {
            if (count >= Buffer.Length) // it will never fit?
            {
                FlushToUnderlying();
                OutStream.Write(bytes, offset, count);
                return;
            }

            int bufferOffset = EnsureCapacity(count);
            Array.Copy(bytes, offset, Buffer, bufferOffset, count);
        }

        public void Write(byte value)
        {
            if (BufferLen == Buffer.Length)
                FlushToUnderlying();
            Buffer[BufferLen++] = value;
        }
        
        public void Write(byte[] bytes) => Write(bytes, 0, bytes.Length);
        public void Write(bool value)   => Write(value ? (byte)1 : (byte)0);
        public void Write(sbyte value)  => Write((byte)value);
        
        public void Write(char ch)
        {
            throw new NotImplementedException();
        }
        public void Write(char[] chars)
        {
            throw new NotImplementedException();
        }

        public void Write(short value)
        {
            int offset = EnsureCapacity(2);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) ((uint) value >> 8);
        }

        public void Write(ushort value)
        {
            int offset = EnsureCapacity(2);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) ((uint) value >> 8);
        }

        public virtual void Write(int value)
        {
            int offset = EnsureCapacity(4);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
        }

        public virtual void Write(uint value)
        {
            int offset = EnsureCapacity(4);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
        }

        public void Write(long value)
        {
            int offset = EnsureCapacity(8);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
            Buffer[offset+4] = (byte) (value >> 32);
            Buffer[offset+5] = (byte) (value >> 40);
            Buffer[offset+6] = (byte) (value >> 48);
            Buffer[offset+7] = (byte) (value >> 56);
        }

        public void Write(ulong value)
        {
            int offset = EnsureCapacity(8);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
            Buffer[offset+4] = (byte) (value >> 32);
            Buffer[offset+5] = (byte) (value >> 40);
            Buffer[offset+6] = (byte) (value >> 48);
            Buffer[offset+7] = (byte) (value >> 56);
        }

        public unsafe void Write(float floating)
        {
            uint value = *(uint*)&floating;
            int offset = EnsureCapacity(4);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
        }

        public unsafe void Write(double floating)
        {
            ulong value = *(ulong*)&floating;
            int offset = EnsureCapacity(8);
            Buffer[offset]   = (byte) value;
            Buffer[offset+1] = (byte) (value >> 8);
            Buffer[offset+2] = (byte) (value >> 16);
            Buffer[offset+3] = (byte) (value >> 24);
            Buffer[offset+4] = (byte) (value >> 32);
            Buffer[offset+5] = (byte) (value >> 40);
            Buffer[offset+6] = (byte) (value >> 48);
            Buffer[offset+7] = (byte) (value >> 56);
        }

        (char[],int) GetCharsFast(string s)
        {
            int numChars = s.Length;
            int capacity = StringBuffer.Length;
            if (capacity < numChars)
            {
                while (capacity < numChars)
                    capacity *= 2;
                StringBuffer = new char[capacity];
            }
            s.CopyTo(0, StringBuffer, 0, numChars);
            return (StringBuffer,numChars);
        }

        public void Write(string value)
        {
            (char[] chars, int numChars) = GetCharsFast(value);

            int count = Encoding.GetByteCount(chars, 0, numChars);
            WriteVLu32((uint)count);

            if (count >= Buffer.Length) // it will never fit?
            {
                FlushToUnderlying();

                // how many chars we can write in one go
                int maxWrite = Buffer.Length / 3;
                int remainingChars = numChars;
                int charPos = 0;

                // write it in chunks
                while (remainingChars > 0)
                {
                    int charsToWrite = remainingChars < maxWrite ? remainingChars : maxWrite;
                    int bytesWritten = Encoding.GetBytes(chars, charPos, charsToWrite, Buffer, 0);
                    OutStream.Write(Buffer, 0, bytesWritten);

                    remainingChars -= charsToWrite;
                    charPos += charsToWrite;
                }
            }
            else
            {
                int offset = EnsureCapacity(count);
                Encoding.GetBytes(chars, 0, numChars, Buffer, offset);
            }
        }

        // Writes an UNSIGNED Variable-Length quantity integer
        // https://en.wikipedia.org/wiki/Variable-length_quantity
        public void WriteVLu32(uint value)
        {
            uint num;
            for (num = value; num >= 0x80U; num >>= 7)
                Write((byte)(num | 0x80U));
            Write((byte)num);
        }

        // Writes an UNSIGNED Variable-Length quantity integer
        public void WriteVLu64(ulong value)
        {
            ulong num;
            for (num = value; num >= 0x80UL; num >>= 7)
                Write((byte)(num | 0x80UL));
            Write((byte)num);
        }

        // Writes a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public void WriteVLi32(int value)
        {
            bool negative = value < 0;
            uint num = (uint)(negative ? -value : value);

            // encode 6 bits
            uint continueBit = num >= 0x40U ? 0x80U : 0;
            uint signBit = negative ? 0x40U : 0;
            Write((byte)((num & 0x3FU) | continueBit | signBit));
            num >>= 6;

            // encode 7-bit chunks
            while (num != 0)
            {
                continueBit = num >= 0x80U ? 0x80U : 0;
                Write((byte)(num | continueBit));
                num >>= 7;
            }
        }

        // Writes a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public void WriteVLi64(long value)
        {
            bool negative = value < 0;
            ulong num = (ulong)(negative ? -value : value);

            // encode 6 bits
            ulong continueBit = num >= 0x40UL ? 0x80UL : 0;
            ulong signBit = negative ? 0x40UL : 0;
            Write((byte)((num & 0x3FUL) | continueBit | signBit));
            num >>= 6;

            // encode 7-bit chunks
            while (num != 0)
            {
                continueBit = num >= 0x80UL ? 0x80UL : 0;
                Write((byte)(num | continueBit));
                num >>= 7;
            }
        }
    }
}

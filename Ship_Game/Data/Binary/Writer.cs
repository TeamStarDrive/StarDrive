using System;
using System.IO;
using System.Text;

namespace Ship_Game.Data.Binary;

public class Writer : IDisposable
{
    Stream OutStream;
    readonly Encoding Encoding;
    int BufferLen;
    byte[] Buffer;
    char[] StringBuffer;

    public int Length => BufferLen;

    public Writer(Stream outStream)
    {
        OutStream = outStream;
        Encoding = Encoding.UTF8;
        // the smallest savegame is around ~1MB, so reserve more than enough
        Buffer = new byte[2 * 1024 * 1024];
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
    // @return offset where to write data
    int EnsureCapacity(int count)
    {
        int len = BufferLen;
        int newLength = len + count;
        if (newLength > Buffer.Length)
        {
            OutStream.Write(Buffer, 0, len);
            BufferLen = count;
            return 0;
        }
        BufferLen = newLength;
        return len;
    }

    // @return Current Buffer Len
    int EnsureCapacityUpTo(int count)
    {
        int len = BufferLen;
        if ((len + count) > Buffer.Length)
        {
            OutStream.Write(Buffer, 0, len);
            len = 0;
        }
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
        int len = EnsureCapacityUpTo(5); // 7 bits payload per byte
        uint num;
        for (num = value; num >= 0x80U; num >>= 7)
            Buffer[len++] = (byte)(num | 0x80U);
        Buffer[len++] = (byte)num;
        BufferLen = len;
    }

    // Writes an UNSIGNED Variable-Length quantity integer
    public void WriteVLu64(ulong value)
    {
        int len = EnsureCapacityUpTo(10); // 7 bits payload per byte
        ulong num;
        for (num = value; num >= 0x80UL; num >>= 7)
            Buffer[len++] = (byte)(num | 0x80UL);
        Buffer[len++] = (byte)num;
        BufferLen = len;
    }

    // Writes a SIGNED Variable-Length quantity integer
    // 2 most significant bits are metadata: ABxxxxxx
    // A: continuation bit
    // B: sign bit
    // Rest of the bytes are 7-bit with 1-bit for continuation
    public void WriteVLi32(int value)
    {
        int len = EnsureCapacityUpTo(5); // 7 bits payload per byte
        bool negative = value < 0;
        uint num = (uint)(negative ? -value : value);

        // encode 6 bits
        uint continueBit = num >= 0x40U ? 0x80U : 0;
        uint signBit = negative ? 0x40U : 0;
        Buffer[len++] = (byte)((num & 0x3FU) | continueBit | signBit);
        num >>= 6;

        // encode 7-bit chunks
        while (num != 0)
        {
            continueBit = num >= 0x80U ? 0x80U : 0;
            Buffer[len++] = (byte)(num | continueBit);
            num >>= 7;
        }
        BufferLen = len;
    }

    // Writes a SIGNED Variable-Length quantity integer
    // 2 most significant bits are metadata: ABxxxxxx
    // A: continuation bit
    // B: sign bit
    // Rest of the bytes are 7-bit with 1-bit for continuation
    public void WriteVLi64(long value)
    {
        int len = EnsureCapacityUpTo(10); // 7 bits payload per byte
        bool negative = value < 0;
        ulong num = (ulong)(negative ? -value : value);

        // encode 6 bits
        ulong continueBit = num >= 0x40UL ? 0x80UL : 0;
        ulong signBit = negative ? 0x40UL : 0;
        Buffer[len++] = (byte)((num & 0x3FUL) | continueBit | signBit);
        num >>= 6;

        // encode 7-bit chunks
        while (num != 0)
        {
            continueBit = num >= 0x80UL ? 0x80UL : 0;
            Buffer[len++] = (byte)(num | continueBit);
            num >>= 7;
        }
        BufferLen = len;
    }

    // Predicts the Variable-Length integer size (in bytes) of this `value`
    public static int PredictVLuSize(uint value)
    {
        // VL uint fits 7 bits in every byte
        if (value < 128u) return 1; // 2^7
        if (value < 16384u) return 2; // 2^14
        if (value < 2097152u) return 3; // 2^21
        if (value < 268435456u) return 4; // 2^28
        return 5;
    }

    public static int PredictVLSize(int value)
    {
        // VL int first byte fits 6 bits of data
        // rest of the bytes fit 7 bits of data
        uint absValue = (uint)(value > 0 ? value : -value);
        if (absValue < 64u) return 1; // 2^6
        if (absValue < 8192u) return 2; // 2^13
        if (absValue < 1048576u) return 3; // 2^20
        if (absValue < 134217728u) return 4; // 2^27
        return 5;
    }
}

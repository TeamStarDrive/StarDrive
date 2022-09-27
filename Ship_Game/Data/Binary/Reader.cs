using System;
using System.IO;

namespace Ship_Game.Data.Binary;

public class Reader : BinaryReader
{
    public Reader(Stream inStream) : base(inStream)
    {
    }

    // Reads an unsigned Variable-Length quantity integer
    public uint ReadVLu32()
    {
        byte bits = ReadByte();
        uint count = (bits & 0x7FU);
        int shift = 7;

        while ((bits & 0x80) != 0) // continue?
        {
            if (shift == 35) // is the stream corrupted?
                throw new InvalidDataException("ReadVLUInt32 failed due to corrupted stream");
            bits = ReadByte();
            count |= (bits & 0x7FU) << shift;
            shift += 7;
        }

        return count;
    }

    // Reads an unsigned Variable-Length quantity integer
    public ulong ReadVLu64()
    {
        byte bits = ReadByte();
        ulong count = (bits & 0x7FUL);
        int shift = 7;

        while ((bits & 0x80) != 0) // continue?
        {
            if (shift == 70) // is the stream corrupted?
                throw new InvalidDataException("ReadVLULong64 failed due to corrupted stream");
            bits = ReadByte();
            count |= (bits & 0x7FUL) << shift;
            shift += 7;
        }

        return count;
    }

    // Reads a SIGNED Variable-Length quantity integer
    // 2 most significant bits are metadata: ABxxxxxx
    // A: continuation bit
    // B: sign bit
    // Rest of the bytes are 7-bit with 1-bit for continuation
    public int ReadVLi32()
    {
        byte bits = ReadByte();
        bool negative = (bits & 0x40) != 0;
        uint count = (bits & 0x3FU);
        int shift = 6;

        while ((bits & 0x80) != 0) // continue?
        {
            if (shift == 34) // is the stream corrupted?
                throw new InvalidDataException("ReadVLInt32 failed due to corrupted stream");
            bits = ReadByte();
            count |= (bits & 0x7FU) << shift;
            shift += 7;
        }

        return negative ? -(int)count : (int)count;
    }

    // Reads a SIGNED Variable-Length quantity integer
    // 2 most significant bits are metadata: ABxxxxxx
    // A: continuation bit
    // B: sign bit
    // Rest of the bytes are 7-bit with 1-bit for continuation
    public long ReadVLi64()
    {
        byte bits = ReadByte();
        bool negative = (bits & 0x40) != 0;
        ulong count = (bits & 0x3FUL);
        int shift = 6;

        while ((bits & 0x80) != 0) // continue?
        {
            if (shift == 69) // is the stream corrupted?
                throw new InvalidDataException("ReadVLLong64 failed due to corrupted stream");
            bits = ReadByte();
            count |= (bits & 0x7FUL) << shift;
            shift += 7;
        }

        return negative ? -(long)count : (long)count;
    }
}

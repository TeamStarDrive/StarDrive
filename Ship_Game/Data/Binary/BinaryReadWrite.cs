using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Binary
{
    public static class BinaryReadWrite
    {
        // Writes an UNSIGNED Variable-Length quantity integer
        // https://en.wikipedia.org/wiki/Variable-length_quantity
        public static void WriteVLu32(this BinaryWriter bw, uint value)
        {
            uint num;
            for (num = value; num >= 0x80U; num >>= 7)
                bw.Write((byte)(num | 0x80U));
            bw.Write((byte)num);
        }

        // Writes an UNSIGNED Variable-Length quantity integer
        public static void WriteVLu64(this BinaryWriter bw, ulong value)
        {
            ulong num;
            for (num = value; num >= 0x80UL; num >>= 7)
                bw.Write((byte)(num | 0x80UL));
            bw.Write((byte)num);
        }

        // Writes a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public static void WriteVLi32(this BinaryWriter bw, int value)
        {
            bool negative = value < 0;
            uint num = (uint)(negative ? -value : value);

            // encode 6 bits
            uint continueBit = num >= 0x40U ? 0x80U : 0;
            uint signBit = negative ? 0x40U : 0;
            bw.Write((byte)((num & 0x3FU) | continueBit | signBit));
            num >>= 6;

            // encode 7-bit chunks
            while (num != 0)
            {
                continueBit = num >= 0x80U ? 0x80U : 0;
                bw.Write((byte)(num | continueBit));
                num >>= 7;
            }
        }

        // Writes a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public static void WriteVLi64(this BinaryWriter bw, long value)
        {
            bool negative = value < 0;
            ulong num = (ulong)(negative ? -value : value);

            // encode 6 bits
            ulong continueBit = num >= 0x40UL ? 0x80UL : 0;
            ulong signBit = negative ? 0x40UL : 0;
            bw.Write((byte)((num & 0x3FUL) | continueBit | signBit));
            num >>= 6;

            // encode 7-bit chunks
            while (num != 0)
            {
                continueBit = num >= 0x80UL ? 0x80UL : 0;
                bw.Write((byte)(num | continueBit));
                num >>= 7;
            }
        }

        // Reads an unsigned Variable-Length quantity integer
        public static uint ReadVLu32(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            uint count = (bits & 0x7FU);
            int shift = 7;

            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 35) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLUInt32 failed due to corrupted stream");
                bits = br.ReadByte();
                count |= (bits & 0x7FU) << shift;
                shift += 7;
            }

            return count;
        }

        // Reads an unsigned Variable-Length quantity integer
        public static ulong ReadVLu64(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            ulong count = (bits & 0x7FUL);
            int shift = 7;

            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 70) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLULong64 failed due to corrupted stream");
                bits = br.ReadByte();
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
        public static int ReadVLi32(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            bool negative = (bits & 0x40) != 0;
            uint count = (bits & 0x3FU);
            int shift = 6;

            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 34) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLInt32 failed due to corrupted stream");
                bits = br.ReadByte();
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
        public static long ReadVLi64(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            bool negative = (bits & 0x40) != 0;
            ulong count = (bits & 0x3FUL);
            int shift = 6;

            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 69) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLLong64 failed due to corrupted stream");
                bits = br.ReadByte();
                count |= (bits & 0x7FUL) << shift;
                shift += 7;
            }

            return negative ? -(long)count : (long)count;
        }
    }
}

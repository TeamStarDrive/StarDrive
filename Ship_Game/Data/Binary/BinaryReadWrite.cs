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
        public static void WriteVL(this BinaryWriter bw, uint value)
        {
            uint num;
            for (num = (uint)value; num >= 0x80U; num >>= 7)
                bw.Write((byte)(num | 0x80U));
            bw.Write((byte)num);
        }

        // Writes a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public static void WriteVL(this BinaryWriter bw, int value)
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

        // Reads an unsigned Variable-Length quantity integer
        public static uint ReadVLUInt(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            uint count = (uint)(bits & 0x7F);
            int shift = 7;

            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 35) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLUInt failed due to corrupted stream");
                bits = br.ReadByte();
                count |= (uint)(bits & 0x7F) << shift;
                shift += 7;
            }

            return count;
        }

        // Reads a SIGNED Variable-Length quantity integer
        // 2 most significant bits are metadata: ABxxxxxx
        // A: continuation bit
        // B: sign bit
        // Rest of the bytes are 7-bit with 1-bit for continuation
        public static int ReadVLInt(this BinaryReader br)
        {
            byte bits = br.ReadByte();
            bool negative = (bits & 0x40) != 0;
            uint count = (uint)(bits & 0x3F);
            int shift = 6;
            while ((bits & 0x80) != 0) // continue?
            {
                if (shift == 34) // is the stream corrupted?
                    throw new InvalidDataException("ReadVLUInt failed due to corrupted stream");
                bits = br.ReadByte();
                count |= (uint)(bits & 0x7F) << shift;
                shift += 7;
            }

            return negative ? -(int)count : (int)count;
        }
    }
}

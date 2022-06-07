using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using Ship_Game.Data.Binary;

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinaryReadWriteTests : StarDriveTest
    {
        readonly MemoryStream Buffer = new();
        readonly Writer BW;
        readonly Reader BR;

        public BinaryReadWriteTests()
        {
            BW = new Writer(Buffer);
            BR = new Reader(Buffer);
        }

        void SeekTo(int pos) => Buffer.Seek(pos, SeekOrigin.Begin);
        void FlushAndSeekTo(int pos) { BW.Flush(); SeekTo(pos); }

        void WriteVL(uint value)  => BW.WriteVLu32(value);
        void WriteVL(int value)   => BW.WriteVLi32(value);
        void WriteVL(ulong value) => BW.WriteVLu64(value);
        void WriteVL(long value)  => BW.WriteVLi64(value);
        int   ReadVLi32() => BR.ReadVLi32();
        uint  ReadVLu32() => BR.ReadVLu32();
        long  ReadVLi64() => BR.ReadVLi64();
        ulong ReadVLu64() => BR.ReadVLu64();
        uint  EncDec(uint value)  { SeekTo(0); BW.WriteVLu32(value); FlushAndSeekTo(0); return ReadVLu32(); }
        int   EncDec(int value)   { SeekTo(0); BW.WriteVLi32(value); FlushAndSeekTo(0); return ReadVLi32(); }
        ulong EncDec(ulong value) { SeekTo(0); BW.WriteVLu64(value); FlushAndSeekTo(0); return ReadVLu64(); }
        long  EncDec(long value)  { SeekTo(0); BW.WriteVLi64(value); FlushAndSeekTo(0); return ReadVLi64(); }

        [TestMethod]
        public void EncodeVLu32()
        {
            Assert.AreEqual(0u, EncDec(0u));
            Assert.AreEqual(0x40u, EncDec(0x40u));
            Assert.AreEqual(0x7Fu, EncDec(0x7Fu));
            Assert.AreEqual(0x80u, EncDec(0x80u));
            Assert.AreEqual(0xFFu, EncDec(0xFFu));
            Assert.AreEqual(1024u, EncDec(1024u));
            Assert.AreEqual(655935u, EncDec(655935u));
            Assert.AreEqual(24655935u, EncDec(24655935u));
            Assert.AreEqual(uint.MaxValue, EncDec(uint.MaxValue));
        }

        [TestMethod]
        public void EncodeVLi32()
        {
            Assert.AreEqual(0, EncDec(0));
            Assert.AreEqual(0x40, EncDec(0x40));
            Assert.AreEqual(0x7F, EncDec(0x7F));
            Assert.AreEqual(0x80, EncDec(0x80));
            Assert.AreEqual(0xFF, EncDec(0xFF));
            Assert.AreEqual(1024, EncDec(1024));
            Assert.AreEqual(655935, EncDec(655935));
            Assert.AreEqual(24655935, EncDec(24655935));
            Assert.AreEqual(int.MaxValue, EncDec(int.MaxValue));
            Assert.AreEqual(-1024, EncDec(-1024));
            Assert.AreEqual(-655935, EncDec(-655935));
            Assert.AreEqual(-24655935, EncDec(-24655935));
            Assert.AreEqual(int.MinValue, EncDec(int.MinValue));
        }

        [TestMethod]
        public void EncodeVLu64()
        {
            Assert.AreEqual(0ul, EncDec(0ul));
            Assert.AreEqual(0x40ul, EncDec(0x40ul));
            Assert.AreEqual(0x7Ful, EncDec(0x7Ful));
            Assert.AreEqual(0x80ul, EncDec(0x80ul));
            Assert.AreEqual(0xFFul, EncDec(0xFFul));
            Assert.AreEqual(1024ul, EncDec(1024ul));
            Assert.AreEqual(655935ul, EncDec(655935ul));
            Assert.AreEqual(24655935ul, EncDec(24655935ul));
            Assert.AreEqual(24655232232332935ul, EncDec(24655232232332935ul));
            Assert.AreEqual(ulong.MaxValue, EncDec(ulong.MaxValue));
        }

        [TestMethod]
        public void EncodeVLi64()
        {
            Assert.AreEqual(0L, EncDec(0L));
            Assert.AreEqual(0x40L, EncDec(0x40L));
            Assert.AreEqual(0x7FL, EncDec(0x7FL));
            Assert.AreEqual(0x80L, EncDec(0x80L));
            Assert.AreEqual(0xFFL, EncDec(0xFFL));
            Assert.AreEqual(1024L, EncDec(1024L));
            Assert.AreEqual(655935L, EncDec(655935L));
            Assert.AreEqual(24655935L, EncDec(24655935L));
            Assert.AreEqual(24655232232332935L, EncDec(24655232232332935L));
            Assert.AreEqual(-24655232232332935L, EncDec(-24655232232332935L));
            Assert.AreEqual(long.MaxValue, EncDec(long.MaxValue));
            Assert.AreEqual(-1024L, EncDec(-1024L));
            Assert.AreEqual(-655935, EncDec(-655935L));
            Assert.AreEqual(-24655935L, EncDec(-24655935L));
            Assert.AreEqual(long.MinValue, EncDec(long.MinValue));
        }

        [TestMethod]
        public void EncodeMultiVLu32()
        {
            WriteVL(0u);
            WriteVL(1024u);
            WriteVL(24655935u);
            WriteVL(0u);
            WriteVL(uint.MaxValue);
            WriteVL(65536u);

            FlushAndSeekTo(0);
            Assert.AreEqual(0u, ReadVLu32());
            Assert.AreEqual(1024u, ReadVLu32());
            Assert.AreEqual(24655935u, ReadVLu32());
            Assert.AreEqual(0u, ReadVLu32());
            Assert.AreEqual(uint.MaxValue, ReadVLu32());
            Assert.AreEqual(65536u, ReadVLu32());
        }

        [TestMethod]
        public void EncodeMultiVLi32()
        {
            WriteVL(0);
            WriteVL(-1024);
            WriteVL(24655935);
            WriteVL(0);
            WriteVL(int.MaxValue);
            WriteVL(-65536);
            WriteVL(int.MinValue);

            FlushAndSeekTo(0);
            Assert.AreEqual(0, ReadVLi32());
            Assert.AreEqual(-1024, ReadVLi32());
            Assert.AreEqual(24655935, ReadVLi32());
            Assert.AreEqual(0, ReadVLi32());
            Assert.AreEqual(int.MaxValue, ReadVLi32());
            Assert.AreEqual(-65536, ReadVLi32());
            Assert.AreEqual(int.MinValue, ReadVLi32());
        }

        [TestMethod]
        public void EncodeMultiVLu64()
        {
            WriteVL(0ul);
            WriteVL(1024ul);
            WriteVL(24655935ul);
            WriteVL(246559213423432435ul);
            WriteVL(0ul);
            WriteVL(ulong.MaxValue);
            WriteVL(65536ul);

            FlushAndSeekTo(0);
            Assert.AreEqual(0ul, ReadVLu64());
            Assert.AreEqual(1024ul, ReadVLu64());
            Assert.AreEqual(24655935ul, ReadVLu64());
            Assert.AreEqual(246559213423432435ul, ReadVLu64());
            Assert.AreEqual(0ul, ReadVLu64());
            Assert.AreEqual(ulong.MaxValue, ReadVLu64());
            Assert.AreEqual(65536ul, ReadVLu64());
        }

        [TestMethod]
        public void EncodeMultiVLi64()
        {
            WriteVL(0L);
            WriteVL(-1024L);
            WriteVL(24655935L);
            WriteVL(-246559213423432435L);
            WriteVL(0L);
            WriteVL(long.MaxValue);
            WriteVL(-65536L);
            WriteVL(long.MinValue);

            FlushAndSeekTo(0);
            Assert.AreEqual(0L, ReadVLi64());
            Assert.AreEqual(-1024L, ReadVLi64());
            Assert.AreEqual(24655935L, ReadVLi64());
            Assert.AreEqual(-246559213423432435L, ReadVLi64());
            Assert.AreEqual(0L, ReadVLi64());
            Assert.AreEqual(long.MaxValue, ReadVLi64());
            Assert.AreEqual(-65536L, ReadVLi64());
            Assert.AreEqual(long.MinValue, ReadVLi64());
        }

        [TestMethod]
        public void EncodeMixedVL()
        {
            WriteVL(0u);
            WriteVL(-1024);
            WriteVL(24655935u);
            WriteVL(0);
            WriteVL(uint.MaxValue);
            WriteVL(-65536);
            WriteVL(0u);
            WriteVL(23225234);

            FlushAndSeekTo(0);
            Assert.AreEqual(0u, ReadVLu32());
            Assert.AreEqual(-1024, ReadVLi32());
            Assert.AreEqual(24655935u, ReadVLu32());
            Assert.AreEqual(0, ReadVLi32());
            Assert.AreEqual(uint.MaxValue, ReadVLu32());
            Assert.AreEqual(-65536, ReadVLi32());
            Assert.AreEqual(0u, ReadVLu32());
            Assert.AreEqual(23225234, ReadVLi32());
        }

        [TestMethod]
        public void WriteByteValues()
        {
            BW.Write((byte)254);
            BW.Write((sbyte)-123);
            BW.Write((byte)123);
            BW.Write((sbyte)-53);

            FlushAndSeekTo(0);
            Assert.AreEqual(254, BR.ReadByte());
            Assert.AreEqual(-123, BR.ReadSByte());
            Assert.AreEqual(123, BR.ReadByte());
            Assert.AreEqual(-53, BR.ReadSByte());
        }

        [TestMethod]
        public void WriteBoolValues()
        {
            BW.Write(true);
            BW.Write(false);
            BW.Write(false);
            BW.Write(true);
            BW.Write(true);
            BW.Write(false);
            BW.Write(true);
            FlushAndSeekTo(0);
            Assert.AreEqual(true, BR.ReadBoolean());
            Assert.AreEqual(false, BR.ReadBoolean());
            Assert.AreEqual(false, BR.ReadBoolean());
            Assert.AreEqual(true, BR.ReadBoolean());
            Assert.AreEqual(true, BR.ReadBoolean());
            Assert.AreEqual(false, BR.ReadBoolean());
            Assert.AreEqual(true, BR.ReadBoolean());
        }

        // write raw values, un-encoded
        [TestMethod]
        public void WriteIntegers()
        {
            BW.Write((short)-655);
            BW.Write((ushort)32143);
            BW.Write(-65536);
            BW.Write(24655935u);
            BW.Write(-246559213423432435L);
            BW.Write(246559213423ul);

            FlushAndSeekTo(0);
            Assert.AreEqual(-655, BR.ReadInt16());
            Assert.AreEqual(32143, BR.ReadUInt16());
            Assert.AreEqual(-65536, BR.ReadInt32());
            Assert.AreEqual(24655935u, BR.ReadUInt32());
            Assert.AreEqual(-246559213423432435L, BR.ReadInt64());
            Assert.AreEqual(246559213423ul, BR.ReadUInt64());
        }

        [TestMethod]
        public void WriteFloats()
        { 
            BW.Write(-0.123123f);
            BW.Write(+12332.123f);
            BW.Write(-10.123123213123);
            BW.Write(+11232122332.121323);

            FlushAndSeekTo(0);
            Assert.AreEqual(-0.123123f, BR.ReadSingle());
            Assert.AreEqual(+12332.123f, BR.ReadSingle());
            Assert.AreEqual(-10.123123213123, BR.ReadDouble());
            Assert.AreEqual(+11232122332.121323, BR.ReadDouble());
        }

        [TestMethod]
        public void WriteStrings()
        { 
            BW.Write("Hello World");
            BW.Write("alerjaliejadiosadja98ewejuawikdjadasd;kljmas;d");
            BW.Write("õäöüÿÕÄÖÜ€ΔΘΛδψως℃℉☔☝♿⚡⛔❌⭙😁😉😈🙋😨");

            var sb1 = new StringBuilder();
            for (int i = 0; i < 1000; ++i)
                sb1.Append("õäöüÿÕÄÖÜ€ΔΘΛδψωςАА́А̀БВГҐДЂЃЕЀЁЄЖЗЗ́ЅИІЇИ́ЍЙЈКЛЉМНЊОŌПРСС́ТЋЌУӮЎФХЦЧЏШЩЪЫЬЭЮЯ");
            string largeString = sb1.ToString();
            BW.Write(largeString);

            var sb2 = new StringBuilder();
            for (int i = 0; i < 10000; ++i)
                sb2.Append("õäöüÿÕÄÖÜ€ΔΘΛδψωςАА́А̀БВГҐДЂЃЕЀЁЄЖЗЗ́ЅИІЇИ́ЍЙЈКЛЉМНЊОŌПРСС́ТЋЌУӮЎФХЦЧЏШЩЪЫЬЭЮЯ");
            string hugeString = sb2.ToString();
            BW.Write(hugeString);

            FlushAndSeekTo(0);

            Assert.AreEqual("Hello World", BR.ReadString());
            Assert.AreEqual("alerjaliejadiosadja98ewejuawikdjadasd;kljmas;d", BR.ReadString());
            Assert.AreEqual("õäöüÿÕÄÖÜ€ΔΘΛδψως℃℉☔☝♿⚡⛔❌⭙😁😉😈🙋😨", BR.ReadString());

            string readLarge = BR.ReadString();
            Assert.AreEqual(largeString, readLarge,
                $"Expected.Len={largeString.Length} Actual.Len={readLarge.Length}");

            string readHuge = BR.ReadString();
            Assert.AreEqual(hugeString, readHuge,
                $"Expected.Len={hugeString.Length} Actual.Len={readHuge.Length}");
        }
    }
}

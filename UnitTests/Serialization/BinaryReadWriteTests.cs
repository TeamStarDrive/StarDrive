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

        protected override void Dispose(bool disposing)
        {
            BW.Dispose();
            BR.Dispose();
            base.Dispose(disposing);
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
            AssertEqual(0u, EncDec(0u));
            AssertEqual(0x40u, EncDec(0x40u));
            AssertEqual(0x7Fu, EncDec(0x7Fu));
            AssertEqual(0x80u, EncDec(0x80u));
            AssertEqual(0xFFu, EncDec(0xFFu));
            AssertEqual(1024u, EncDec(1024u));
            AssertEqual(655935u, EncDec(655935u));
            AssertEqual(24655935u, EncDec(24655935u));
            AssertEqual(uint.MaxValue, EncDec(uint.MaxValue));
        }

        [TestMethod]
        public void EncodeVLi32()
        {
            AssertEqual(0, EncDec(0));
            AssertEqual(0x40, EncDec(0x40));
            AssertEqual(0x7F, EncDec(0x7F));
            AssertEqual(0x80, EncDec(0x80));
            AssertEqual(0xFF, EncDec(0xFF));
            AssertEqual(1024, EncDec(1024));
            AssertEqual(655935, EncDec(655935));
            AssertEqual(24655935, EncDec(24655935));
            AssertEqual(int.MaxValue, EncDec(int.MaxValue));
            AssertEqual(-1024, EncDec(-1024));
            AssertEqual(-655935, EncDec(-655935));
            AssertEqual(-24655935, EncDec(-24655935));
            AssertEqual(int.MinValue, EncDec(int.MinValue));
        }

        [TestMethod]
        public void EncodeVLu64()
        {
            AssertEqual(0ul, EncDec(0ul));
            AssertEqual(0x40ul, EncDec(0x40ul));
            AssertEqual(0x7Ful, EncDec(0x7Ful));
            AssertEqual(0x80ul, EncDec(0x80ul));
            AssertEqual(0xFFul, EncDec(0xFFul));
            AssertEqual(1024ul, EncDec(1024ul));
            AssertEqual(655935ul, EncDec(655935ul));
            AssertEqual(24655935ul, EncDec(24655935ul));
            AssertEqual(24655232232332935ul, EncDec(24655232232332935ul));
            AssertEqual(ulong.MaxValue, EncDec(ulong.MaxValue));
        }

        [TestMethod]
        public void EncodeVLi64()
        {
            AssertEqual(0L, EncDec(0L));
            AssertEqual(0x40L, EncDec(0x40L));
            AssertEqual(0x7FL, EncDec(0x7FL));
            AssertEqual(0x80L, EncDec(0x80L));
            AssertEqual(0xFFL, EncDec(0xFFL));
            AssertEqual(1024L, EncDec(1024L));
            AssertEqual(655935L, EncDec(655935L));
            AssertEqual(24655935L, EncDec(24655935L));
            AssertEqual(24655232232332935L, EncDec(24655232232332935L));
            AssertEqual(-24655232232332935L, EncDec(-24655232232332935L));
            AssertEqual(long.MaxValue, EncDec(long.MaxValue));
            AssertEqual(-1024L, EncDec(-1024L));
            AssertEqual(-655935, EncDec(-655935L));
            AssertEqual(-24655935L, EncDec(-24655935L));
            AssertEqual(long.MinValue, EncDec(long.MinValue));
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
            AssertEqual(0u, ReadVLu32());
            AssertEqual(1024u, ReadVLu32());
            AssertEqual(24655935u, ReadVLu32());
            AssertEqual(0u, ReadVLu32());
            AssertEqual(uint.MaxValue, ReadVLu32());
            AssertEqual(65536u, ReadVLu32());
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
            AssertEqual(0, ReadVLi32());
            AssertEqual(-1024, ReadVLi32());
            AssertEqual(24655935, ReadVLi32());
            AssertEqual(0, ReadVLi32());
            AssertEqual(int.MaxValue, ReadVLi32());
            AssertEqual(-65536, ReadVLi32());
            AssertEqual(int.MinValue, ReadVLi32());
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
            AssertEqual(0ul, ReadVLu64());
            AssertEqual(1024ul, ReadVLu64());
            AssertEqual(24655935ul, ReadVLu64());
            AssertEqual(246559213423432435ul, ReadVLu64());
            AssertEqual(0ul, ReadVLu64());
            AssertEqual(ulong.MaxValue, ReadVLu64());
            AssertEqual(65536ul, ReadVLu64());
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
            AssertEqual(0L, ReadVLi64());
            AssertEqual(-1024L, ReadVLi64());
            AssertEqual(24655935L, ReadVLi64());
            AssertEqual(-246559213423432435L, ReadVLi64());
            AssertEqual(0L, ReadVLi64());
            AssertEqual(long.MaxValue, ReadVLi64());
            AssertEqual(-65536L, ReadVLi64());
            AssertEqual(long.MinValue, ReadVLi64());
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
            AssertEqual(0u, ReadVLu32());
            AssertEqual(-1024, ReadVLi32());
            AssertEqual(24655935u, ReadVLu32());
            AssertEqual(0, ReadVLi32());
            AssertEqual(uint.MaxValue, ReadVLu32());
            AssertEqual(-65536, ReadVLi32());
            AssertEqual(0u, ReadVLu32());
            AssertEqual(23225234, ReadVLi32());
        }

        int WriteVLSize<T>(T value) // @return # of bytes written
        {
            int sizeBefore = BW.Length;
            if (value is int i32) WriteVL(i32);
            else if (value is uint u32) WriteVL(u32);
            else if (value is long i64) WriteVL(i64);
            else if (value is ulong u64) WriteVL(u64);
            return BW.Length - sizeBefore;
        }

        [TestMethod]
        public void EncodePredictionVLu32()
        {
            AssertEqual(WriteVLSize(32u), Writer.PredictVLuSize(32u), "PredictVL(32)");
            // 2^7
            AssertEqual(WriteVLSize(127u), Writer.PredictVLuSize(127u), $"PredictVL({127u})");
            AssertEqual(WriteVLSize(128u), Writer.PredictVLuSize(128u), $"PredictVL({128u})");
            // 2^14
            AssertEqual(WriteVLSize(16383u), Writer.PredictVLuSize(16383u), $"PredictVL({16383u})");
            AssertEqual(WriteVLSize(16384u), Writer.PredictVLuSize(16384u), $"PredictVL({16384u})");
            // 2^21
            AssertEqual(WriteVLSize(2097151u), Writer.PredictVLuSize(2097151u), $"PredictVL({2097151u})");
            AssertEqual(WriteVLSize(2097152u), Writer.PredictVLuSize(2097152u), $"PredictVL({2097152u})");
            // 2^28
            AssertEqual(WriteVLSize(268435455u), Writer.PredictVLuSize(268435455u), $"PredictVL({268435455u})");
            AssertEqual(WriteVLSize(268435456u), Writer.PredictVLuSize(268435456u), $"PredictVL({268435456u})");
            // 2^32
            AssertEqual(WriteVLSize(4294967295u), Writer.PredictVLuSize(4294967295u), $"PredictVL({4294967295u})");
        }

        [TestMethod]
        public void EncodePredictionVLi32()
        {
            AssertEqual(WriteVLSize(32), Writer.PredictVLSize(32), "PredictVL(32)");
            // 2^6
            AssertEqual(WriteVLSize(63), Writer.PredictVLSize(63), $"PredictVL({63})");
            AssertEqual(WriteVLSize(64), Writer.PredictVLSize(64), $"PredictVL({64})");
            // 2^13
            AssertEqual(WriteVLSize(8191), Writer.PredictVLSize(8191), $"PredictVL({8191})");
            AssertEqual(WriteVLSize(8192), Writer.PredictVLSize(8192), $"PredictVL({8192})");
            // 2^20
            AssertEqual(WriteVLSize(1048575), Writer.PredictVLSize(1048575), $"PredictVL({1048575})");
            AssertEqual(WriteVLSize(1048576), Writer.PredictVLSize(1048576), $"PredictVL({1048576})");
            // 2^27
            AssertEqual(WriteVLSize(134217727), Writer.PredictVLSize(134217727), $"PredictVL({134217727})");
            AssertEqual(WriteVLSize(134217728), Writer.PredictVLSize(134217728), $"PredictVL({134217728})");
            // 2^31
            AssertEqual(WriteVLSize(2147483647), Writer.PredictVLSize(2147483647), $"PredictVL({2147483647})");

            AssertEqual(WriteVLSize(-32), Writer.PredictVLSize(-32), "PredictVL(-32)");
            // 2^6
            AssertEqual(WriteVLSize(-63), Writer.PredictVLSize(-63), $"PredictVL({-63})");
            AssertEqual(WriteVLSize(-64), Writer.PredictVLSize(-64), $"PredictVL({-64})");
            // 2^13
            AssertEqual(WriteVLSize(-8191), Writer.PredictVLSize(-8191), $"PredictVL({-8191})");
            AssertEqual(WriteVLSize(-8192), Writer.PredictVLSize(-8192), $"PredictVL({-8192})");
            // 2^20
            AssertEqual(WriteVLSize(-1048575), Writer.PredictVLSize(-1048575), $"PredictVL({-1048575})");
            AssertEqual(WriteVLSize(-1048576), Writer.PredictVLSize(-1048576), $"PredictVL({-1048576})");
            // 2^27
            AssertEqual(WriteVLSize(-134217727), Writer.PredictVLSize(-134217727), $"PredictVL({-134217727})");
            AssertEqual(WriteVLSize(-134217728), Writer.PredictVLSize(-134217728), $"PredictVL({-134217728})");
            // 2^31
            AssertEqual(WriteVLSize(-2147483647), Writer.PredictVLSize(-2147483647), $"PredictVL({-2147483647})");
        }

        [TestMethod]
        public void WriteByteValues()
        {
            BW.Write((byte)254);
            BW.Write((sbyte)-123);
            BW.Write((byte)123);
            BW.Write((sbyte)-53);

            FlushAndSeekTo(0);
            AssertEqual(254, BR.ReadByte());
            AssertEqual(-123, BR.ReadSByte());
            AssertEqual(123, BR.ReadByte());
            AssertEqual(-53, BR.ReadSByte());
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
            AssertEqual(true, BR.ReadBoolean());
            AssertEqual(false, BR.ReadBoolean());
            AssertEqual(false, BR.ReadBoolean());
            AssertEqual(true, BR.ReadBoolean());
            AssertEqual(true, BR.ReadBoolean());
            AssertEqual(false, BR.ReadBoolean());
            AssertEqual(true, BR.ReadBoolean());
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
            AssertEqual(-655, BR.ReadInt16());
            AssertEqual(32143, BR.ReadUInt16());
            AssertEqual(-65536, BR.ReadInt32());
            AssertEqual(24655935u, BR.ReadUInt32());
            AssertEqual(-246559213423432435L, BR.ReadInt64());
            AssertEqual(246559213423ul, BR.ReadUInt64());
        }

        [TestMethod]
        public void WriteFloats()
        { 
            BW.Write(-0.123123f);
            BW.Write(+12332.123f);
            BW.Write(-10.123123213123);
            BW.Write(+11232122332.121323);

            FlushAndSeekTo(0);
            AssertEqual(-0.123123f, BR.ReadSingle());
            AssertEqual(+12332.123f, BR.ReadSingle());
            AssertEqual(-10.123123213123, BR.ReadDouble());
            AssertEqual(+11232122332.121323, BR.ReadDouble());
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

            AssertEqual("Hello World", BR.ReadString());
            AssertEqual("alerjaliejadiosadja98ewejuawikdjadasd;kljmas;d", BR.ReadString());
            AssertEqual("õäöüÿÕÄÖÜ€ΔΘΛδψως℃℉☔☝♿⚡⛔❌⭙😁😉😈🙋😨", BR.ReadString());

            string readLarge = BR.ReadString();
            AssertEqual(largeString, readLarge,
                $"Expected.Len={largeString.Length} Actual.Len={readLarge.Length}");

            string readHuge = BR.ReadString();
            AssertEqual(hugeString, readHuge,
                $"Expected.Len={hugeString.Length} Actual.Len={readHuge.Length}");
        }
    }
}

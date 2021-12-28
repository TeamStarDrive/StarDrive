using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Ship_Game.Data.Binary;

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinaryReadWriteTests : StarDriveTest
    {
        readonly MemoryStream Buffer = new MemoryStream();

        static void SeekTo(MemoryStream ms, int pos) => ms.Seek(pos, SeekOrigin.Begin);
        static void WriteVL(MemoryStream ms, uint value) => new BinaryWriter(ms).WriteVL(value);
        static void WriteVL(MemoryStream ms, int value) => new BinaryWriter(ms).WriteVL(value);
        static void WriteVL(MemoryStream ms, ulong value) => new BinaryWriter(ms).WriteVL(value);
        static void WriteVL(MemoryStream ms, long value) => new BinaryWriter(ms).WriteVL(value);
        static int ReadInt(MemoryStream ms) => new BinaryReader(ms).ReadVLInt();
        static uint ReadUInt(MemoryStream ms) => new BinaryReader(ms).ReadVLUInt();
        static long ReadLong(MemoryStream ms) => new BinaryReader(ms).ReadVLLong();
        static ulong ReadULong(MemoryStream ms) => new BinaryReader(ms).ReadVLULong();

        static uint EncDec(uint value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadUInt(ms);
        }

        static int EncDec(int value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadInt(ms);
        }

        static ulong EncDec(ulong value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadULong(ms);
        }

        static long EncDec(long value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadLong(ms);
        }

        [TestMethod]
        public void EncodeSingleVLUInt()
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
        public void EncodeSingleVLInt()
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
        public void EncodeSingleVLULong()
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
        public void EncodeSingleVLLong()
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
        public void EncodeMultipleUInt()
        {
            WriteVL(Buffer, 0u);
            WriteVL(Buffer, 1024u);
            WriteVL(Buffer, 24655935u);
            WriteVL(Buffer, 0u);
            WriteVL(Buffer, uint.MaxValue);
            WriteVL(Buffer, 65536u);

            SeekTo(Buffer, 0);

            Assert.AreEqual(0u, ReadUInt(Buffer));
            Assert.AreEqual(1024u, ReadUInt(Buffer));
            Assert.AreEqual(24655935u, ReadUInt(Buffer));
            Assert.AreEqual(0u, ReadUInt(Buffer));
            Assert.AreEqual(uint.MaxValue, ReadUInt(Buffer));
            Assert.AreEqual(65536u, ReadUInt(Buffer));
        }

        [TestMethod]
        public void EncodeMultipleInt()
        {
            WriteVL(Buffer, 0);
            WriteVL(Buffer, -1024);
            WriteVL(Buffer, 24655935);
            WriteVL(Buffer, 0);
            WriteVL(Buffer, int.MaxValue);
            WriteVL(Buffer, -65536);
            WriteVL(Buffer, int.MinValue);

            SeekTo(Buffer, 0);

            Assert.AreEqual(0, ReadInt(Buffer));
            Assert.AreEqual(-1024, ReadInt(Buffer));
            Assert.AreEqual(24655935, ReadInt(Buffer));
            Assert.AreEqual(0, ReadInt(Buffer));
            Assert.AreEqual(int.MaxValue, ReadInt(Buffer));
            Assert.AreEqual(-65536, ReadInt(Buffer));
            Assert.AreEqual(int.MinValue, ReadInt(Buffer));
        }

        [TestMethod]
        public void EncodeMultipleULong()
        {
            WriteVL(Buffer, 0ul);
            WriteVL(Buffer, 1024ul);
            WriteVL(Buffer, 24655935ul);
            WriteVL(Buffer, 246559213423432435ul);
            WriteVL(Buffer, 0ul);
            WriteVL(Buffer, ulong.MaxValue);
            WriteVL(Buffer, 65536ul);

            SeekTo(Buffer, 0);

            Assert.AreEqual(0ul, ReadULong(Buffer));
            Assert.AreEqual(1024ul, ReadULong(Buffer));
            Assert.AreEqual(24655935ul, ReadULong(Buffer));
            Assert.AreEqual(246559213423432435ul, ReadULong(Buffer));
            Assert.AreEqual(0ul, ReadULong(Buffer));
            Assert.AreEqual(ulong.MaxValue, ReadULong(Buffer));
            Assert.AreEqual(65536ul, ReadULong(Buffer));
        }

        [TestMethod]
        public void EncodeMultipleLong()
        {
            WriteVL(Buffer, 0L);
            WriteVL(Buffer, -1024L);
            WriteVL(Buffer, 24655935L);
            WriteVL(Buffer, -246559213423432435L);
            WriteVL(Buffer, 0L);
            WriteVL(Buffer, long.MaxValue);
            WriteVL(Buffer, -65536L);
            WriteVL(Buffer, long.MinValue);

            SeekTo(Buffer, 0);

            Assert.AreEqual(0L, ReadLong(Buffer));
            Assert.AreEqual(-1024L, ReadLong(Buffer));
            Assert.AreEqual(24655935L, ReadLong(Buffer));
            Assert.AreEqual(-246559213423432435L, ReadLong(Buffer));
            Assert.AreEqual(0L, ReadLong(Buffer));
            Assert.AreEqual(long.MaxValue, ReadLong(Buffer));
            Assert.AreEqual(-65536L, ReadLong(Buffer));
            Assert.AreEqual(long.MinValue, ReadLong(Buffer));
        }

        [TestMethod]
        public void EncodeMixedIntAndUInt()
        {
            WriteVL(Buffer, 0u);
            WriteVL(Buffer, -1024);
            WriteVL(Buffer, 24655935u);
            WriteVL(Buffer, 0);
            WriteVL(Buffer, uint.MaxValue);
            WriteVL(Buffer, -65536);
            WriteVL(Buffer, 0u);
            WriteVL(Buffer, 23225234);

            SeekTo(Buffer, 0);

            Assert.AreEqual(0u, ReadUInt(Buffer));
            Assert.AreEqual(-1024, ReadInt(Buffer));
            Assert.AreEqual(24655935u, ReadUInt(Buffer));
            Assert.AreEqual(0, ReadInt(Buffer));
            Assert.AreEqual(uint.MaxValue, ReadUInt(Buffer));
            Assert.AreEqual(-65536, ReadInt(Buffer));
            Assert.AreEqual(0u, ReadUInt(Buffer));
            Assert.AreEqual(23225234, ReadInt(Buffer));
        }
    }
}

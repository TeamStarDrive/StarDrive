using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Binary;

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinaryReadWriteTests : StarDriveTest
    {
        readonly MemoryStream Buffer = new MemoryStream();

        static void SeekTo(MemoryStream ms, int pos)
        {
            ms.Seek(pos, SeekOrigin.Begin);
        }

        static void WriteVL(MemoryStream ms, uint value)
        {
            new BinaryWriter(ms).WriteVL(value);
        }

        static void WriteVL(MemoryStream ms, int value)
        {
            new BinaryWriter(ms).WriteVL(value);
        }

        static int ReadVLInt(MemoryStream ms)
        {
            return new BinaryReader(ms).ReadVLInt();
        }

        static uint ReadVLUInt(MemoryStream ms)
        {
            return new BinaryReader(ms).ReadVLUInt();
        }

        static uint WriteReadUInt(uint value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadVLUInt(ms);
        }

        static int WriteReadInt(int value)
        {
            var ms = new MemoryStream();
            WriteVL(ms, value);
            SeekTo(ms, 0);
            return ReadVLInt(ms);
        }

        [TestMethod]
        public void EncodeSingleVLUInt()
        {
            Assert.AreEqual(0u, WriteReadUInt(0u));
            Assert.AreEqual(0x40u, WriteReadUInt(0x40u));
            Assert.AreEqual(0x7Fu, WriteReadUInt(0x7Fu));
            Assert.AreEqual(0x80u, WriteReadUInt(0x80u));
            Assert.AreEqual(0xFFu, WriteReadUInt(0xFFu));
            Assert.AreEqual(1024u, WriteReadUInt(1024u));
            Assert.AreEqual(655935u, WriteReadUInt(655935u));
            Assert.AreEqual(24655935u, WriteReadUInt(24655935u));
            Assert.AreEqual(uint.MaxValue, WriteReadUInt(uint.MaxValue));
        }

        [TestMethod]
        public void EncodeSingleVLInt()
        {
            Assert.AreEqual(0, WriteReadInt(0));
            Assert.AreEqual(0x40, WriteReadInt(0x40));
            Assert.AreEqual(0x7F, WriteReadInt(0x7F));
            Assert.AreEqual(0x80, WriteReadInt(0x80));
            Assert.AreEqual(0xFF, WriteReadInt(0xFF));
            Assert.AreEqual(1024, WriteReadInt(1024));
            Assert.AreEqual(655935, WriteReadInt(655935));
            Assert.AreEqual(24655935, WriteReadInt(24655935));
            Assert.AreEqual(int.MaxValue, WriteReadInt(int.MaxValue));
            Assert.AreEqual(-1024, WriteReadInt(-1024));
            Assert.AreEqual(-655935, WriteReadInt(-655935));
            Assert.AreEqual(-24655935, WriteReadInt(-24655935));
            Assert.AreEqual(int.MinValue, WriteReadInt(int.MinValue));
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

            Assert.AreEqual(0u, ReadVLUInt(Buffer));
            Assert.AreEqual(1024u, ReadVLUInt(Buffer));
            Assert.AreEqual(24655935u, ReadVLUInt(Buffer));
            Assert.AreEqual(0u, ReadVLUInt(Buffer));
            Assert.AreEqual(uint.MaxValue, ReadVLUInt(Buffer));
            Assert.AreEqual(65536u, ReadVLUInt(Buffer));
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

            Assert.AreEqual(0, ReadVLInt(Buffer));
            Assert.AreEqual(-1024, ReadVLInt(Buffer));
            Assert.AreEqual(24655935, ReadVLInt(Buffer));
            Assert.AreEqual(0, ReadVLInt(Buffer));
            Assert.AreEqual(int.MaxValue, ReadVLInt(Buffer));
            Assert.AreEqual(-65536, ReadVLInt(Buffer));
            Assert.AreEqual(int.MinValue, ReadVLInt(Buffer));
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

            Assert.AreEqual(0u, ReadVLUInt(Buffer));
            Assert.AreEqual(-1024, ReadVLInt(Buffer));
            Assert.AreEqual(24655935u, ReadVLUInt(Buffer));
            Assert.AreEqual(0, ReadVLInt(Buffer));
            Assert.AreEqual(uint.MaxValue, ReadVLUInt(Buffer));
            Assert.AreEqual(-65536, ReadVLInt(Buffer));
            Assert.AreEqual(0u, ReadVLUInt(Buffer));
            Assert.AreEqual(23225234, ReadVLInt(Buffer));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Data;

namespace UnitTests.Serialization
{
    [TestClass]
    public class StringViewTests : StarDriveTest
    {
        // An inefficient way to create a StringView
        // Only meant to be used for TESTING
        static StringView FromString(string text)
        {
            return new StringView(text.ToCharArray());
        }

        [TestMethod]
        public void ReadLine()
        {
            var sr = new StringReader("Test\nSomeLines\r\nAnd carriage\rAndEmpties\n\n\n");
            
            var buffer = new char[1024];
            Assert.AreEqual("Test", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.AreEqual("SomeLines", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.AreEqual("And carriage", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.AreEqual("AndEmpties", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.AreEqual("", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.AreEqual("", StringView.ReadLine(sr, buffer).Value.Text);
            Assert.IsFalse(StringView.ReadLine(sr, buffer, out _));
        }

        [TestMethod]
        public void NextTokenWorksDynamically()
        {
            StringView view = FromString("key1=value1;key2=value2");
            Assert.AreEqual("key1", view.Next('=').Text);
            Assert.AreEqual("value1", view.Next(';').Text);
            Assert.AreEqual("key2", view.Next('=').Text);
            Assert.AreEqual("value2", view.Next(';').Text);
            Assert.AreEqual("", view.Text);
        }

        [TestMethod]
        public void Trim()
        {
            StringView view = FromString("  \t  \t\tHello, StringView\t \t \t");
            view.TrimStart();
            Assert.AreEqual("Hello, StringView\t \t \t", view.Text);
            view.TrimEnd();
            Assert.AreEqual("Hello, StringView", view.Text);
        }

        [TestMethod]
        public void ToDouble()
        {
            Assert.AreEqual(-31.9510, FromString("-31.9510").ToDouble());
            Assert.AreEqual(31.9510, FromString("31.9510").ToDouble());
            Assert.AreEqual(31.9510, FromString("+31.9510").ToDouble());
            Assert.AreEqual(0.0, FromString("not-a-number").ToDouble());
        }

        [TestMethod]
        public void ToInt()
        {
            Assert.AreEqual(-3123123, FromString("-3123123").ToInt());
            Assert.AreEqual(3123123, FromString("3123123").ToInt());
            Assert.AreEqual(3123123, FromString("+3123123").ToInt());
            Assert.AreEqual(0, FromString("not-a-number").ToInt());
        }
    }
}

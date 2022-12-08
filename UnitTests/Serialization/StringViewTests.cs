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
            var r = new GenericStringViewParser("Reader", "Test\nSomeLines\r\nAnd carriage\rAndEmpties\n\n\n");
            AssertEqual("Test", r.ReadLine().Text);
            AssertEqual("SomeLines", r.ReadLine().Text);
            AssertEqual("And carriage", r.ReadLine().Text);
            AssertEqual("AndEmpties", r.ReadLine().Text);
            AssertEqual("", r.ReadLine().Text);
            AssertEqual("", r.ReadLine().Text);
            Assert.IsFalse(r.ReadLine(out _));
        }

        [TestMethod]
        public void NextTokenWorksDynamically()
        {
            StringView view = FromString("key1=value1;key2=value2");
            AssertEqual("key1", view.Next('=').Text);
            AssertEqual("value1", view.Next(';').Text);
            AssertEqual("key2", view.Next('=').Text);
            AssertEqual("value2", view.Next(';').Text);
            AssertEqual("", view.Text);
        }

        [TestMethod]
        public void Trim()
        {
            StringView view = FromString("  \t  \t\tHello, StringView\t \t \t");
            view.TrimStart();
            AssertEqual("Hello, StringView\t \t \t", view.Text);
            view.TrimEnd();
            AssertEqual("Hello, StringView", view.Text);
        }

        [TestMethod]
        public void ToDouble()
        {
            AssertEqual(-31.9510, FromString("-31.9510").ToDouble());
            AssertEqual(31.9510, FromString("31.9510").ToDouble());
            AssertEqual(31.9510, FromString("+31.9510").ToDouble());
            AssertEqual(0.0, FromString("not-a-number").ToDouble());
        }

        [TestMethod]
        public void ToInt()
        {
            AssertEqual(-3123123, FromString("-3123123").ToInt());
            AssertEqual(3123123, FromString("3123123").ToInt());
            AssertEqual(3123123, FromString("+3123123").ToInt());
            AssertEqual(0, FromString("not-a-number").ToInt());
        }
    }
}

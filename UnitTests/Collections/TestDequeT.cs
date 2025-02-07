﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestDequeT : StarDriveTest
    {
        [TestMethod]
        public void PushToFront()
        {
            var deque = new Deque<int>();
            for (int i = 0; i < 32; ++i)
            {
                deque.PushToFront(i+1);
                AssertEqual(i+1, deque.Count);
            }
            for (int i = 0; i < 32; ++i)
            {
                AssertEqual(32-i, deque.PopFirst());
            }
        }
        
        [TestMethod]
        public void Add()
        {
            var deque = new Deque<int>();
            for (int i = 0; i < 32; ++i)
            {
                deque.Add(i+1);
                AssertEqual(i+1, deque.Count);
            }
            for (int i = 0; i < 32; ++i)
            {
                AssertEqual(32-i, deque.PopLast());
            }
        }

        [TestMethod]
        public void PushToFrontAndAdd()
        {
            var deque = new Deque<int>();
            for (int i = 0; i < 32; i += 2)
            {
                deque.Add(i+1);
                deque.PushToFront(i+2);
                AssertEqual(i+2, deque.Count);
            }
            for (int i = 0; i < 32; i += 2)
            {
                AssertEqual((32-i)-1, deque.PopLast());
                AssertEqual(32-i, deque.PopFirst());
            }
        }

        [TestMethod]
        public void Contains()
        {
            var deque = new Deque<string>();
            deque.Add("4");
            deque.Add("3");
            deque.Add("6");
            deque.PushToFront("2");
            deque.PushToFront("1");
            deque.PushToFront("5");
            AssertEqual(6, deque.Count);
            
            Assert.IsTrue(deque.Contains("6"));
            Assert.IsTrue(deque.Contains("5"));
            Assert.IsTrue(deque.Contains("4"));
            Assert.IsTrue(deque.Contains("3"));
            Assert.IsTrue(deque.Contains("2"));
            Assert.IsTrue(deque.Contains("1"));
            Assert.IsFalse(deque.Contains("batteries"));
        }

        [TestMethod]
        public void IndexOf()
        {
            var deque = new Deque<string>();
            deque.Add("2");
            deque.Add("3");
            deque.PushToFront("1");
            deque.PushToFront("0");
            AssertEqual(4, deque.Count);

            AssertEqual(0, deque.IndexOf("0"));
            AssertEqual(1, deque.IndexOf("1"));
            AssertEqual(2, deque.IndexOf("2"));
            AssertEqual(3, deque.IndexOf("3"));
            AssertEqual(-1, deque.IndexOf("batteries"));
        }

        
        [TestMethod]
        public void Insert()
        {
            var deque = new Deque<string>();
            deque.Add("2");
            deque.Add("3");
            deque.PushToFront("1");
            deque.PushToFront("0");
            AssertEqual(4, deque.Count);

            deque.Insert(0, "x");
            AssertEqual(5, deque.Count);
            AssertEqual(0, deque.IndexOf("x"));

            deque.Insert(5, "y");
            AssertEqual(6, deque.Count);
            AssertEqual(5, deque.IndexOf("y"));

            deque.Insert(2, "z");
            AssertEqual(7, deque.Count);
            AssertEqual(2, deque.IndexOf("z"));
            
            deque.Insert(1, "r");
            AssertEqual(8, deque.Count);
            AssertEqual(1, deque.IndexOf("r"));

            deque.Insert(6, "s");
            AssertEqual(9, deque.Count);
            AssertEqual(6, deque.IndexOf("s"));
        }

        [TestMethod]
        public void RemoveAt()
        {
            var deque = new Deque<string>();
            deque.Add("3");
            deque.Add("4");
            deque.Add("5");
            deque.PushToFront("2");
            deque.PushToFront("1");
            deque.PushToFront("0");
            AssertEqual(6, deque.Count);

            AssertEqual(0, deque.IndexOf("0"));
            deque.RemoveAt(0);
            AssertEqual(5, deque.Count);
            AssertEqual(-1, deque.IndexOf("0"));
            deque.PushToFront("0");
            AssertEqual(0, deque.IndexOf("0"));

            AssertEqual(2, deque.IndexOf("2"));
            deque.RemoveAt(2);
            AssertEqual(5, deque.Count);
            AssertEqual(-1, deque.IndexOf("2"));
            deque.Insert(2, "2");
            AssertEqual(2, deque.IndexOf("2"));

            AssertEqual(3, deque.IndexOf("3"));
            deque.RemoveAt(3);
            AssertEqual(5, deque.Count);
            AssertEqual(-1, deque.IndexOf("3"));
            deque.Insert(3, "3");
            AssertEqual(3, deque.IndexOf("3"));

            
            AssertEqual(5, deque.IndexOf("5"));
            deque.RemoveAt(5);
            AssertEqual(5, deque.Count);
            AssertEqual(-1, deque.IndexOf("5"));
            deque.Insert(5, "5");
            AssertEqual(5, deque.IndexOf("5"));
        }
    }
}

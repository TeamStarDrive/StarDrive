using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Utils;
using System;

namespace UnitTests.Collections;

[TestClass]
public class StableCollectionTests : StarDriveTest
{
    public StableCollection<T> NewCollection<T>(T[] args)
    {
        var c = new StableCollection<T>();
        foreach (T arg in args) c.Insert(arg);
        return c;
    }

    public string[] ArrABCDE() => new[]{"a","b","c","d","e"};
    public StableCollection<string> MakeABCDE() => NewCollection(ArrABCDE());

    [TestMethod]
    public void Insert()
    {
        var c = new StableCollection<string>();

        Assert.AreEqual(0, c.Insert("dog"));
        Assert.AreEqual(1, c.Count);

        Assert.AreEqual(1, c.Insert("cat"));
        Assert.AreEqual(2, c.Count);

        Assert.ThrowsException<NullReferenceException>(() => c.Insert(null));

        Assert.That.EqualCollections(new[]{"dog","cat"}, c.ToArr());
    }

    [TestMethod]
    public void IndexOf()
    {
        var c = MakeABCDE();
        Assert.AreEqual(5, c.Count);
        Assert.AreEqual(0, c.IndexOf("a"));
        Assert.AreEqual(1, c.IndexOf("b"));
        Assert.AreEqual(2, c.IndexOf("c"));
        Assert.AreEqual(3, c.IndexOf("d"));
        Assert.AreEqual(4, c.IndexOf("e"));
        Assert.ThrowsException<NullReferenceException>(() => c.IndexOf(null));

        Assert.That.EqualCollections(ArrABCDE(), c.ToArr());
    }

    [TestMethod]
    public void Contains()
    {
        var c = MakeABCDE();
        Assert.AreEqual(5, c.Count);
        Assert.IsTrue(c.Contains("a"));
        Assert.IsTrue(c.Contains("b"));
        Assert.IsTrue(c.Contains("c"));
        Assert.IsTrue(c.Contains("d"));
        Assert.IsTrue(c.Contains("e"));
        
        Assert.IsFalse(c.Contains(""));
        Assert.IsFalse(c.Contains("x"));
        Assert.ThrowsException<NullReferenceException>(() => c.Contains(null));
    }

    [TestMethod]
    public void RemoveAtDoesNotChangeObjectIndexOf()
    {
        var c = MakeABCDE();
        // this is a bit funky. Even if we remove elements, their INDEX will remain the same!
        c.RemoveAt(0);
        Assert.IsFalse(c.Contains("a"));
        Assert.AreEqual(4, c.Count); // but the count does change!

        c.RemoveAt(2);
        Assert.IsFalse(c.Contains("c"));
        Assert.AreEqual(3, c.Count);

        Assert.That.EqualCollections(new[]{"b","d","e"}, c.ToArr());
    }

    [TestMethod]
    public void Remove()
    {
        var c = MakeABCDE();
        Assert.IsTrue(c.Remove("a"));
        Assert.IsTrue(c.Remove("c"));

        Assert.IsFalse(c.Remove("a"));
        Assert.IsFalse(c.Remove("x"));
    }

    [TestMethod]
    public void Enumerator()
    {
        var c = MakeABCDE();
        var items = new Array<string>();
        foreach (string s in c)
            items.Add(s);
        Assert.That.EqualCollections(ArrABCDE(), items);
    }

    (StableCollection<string>, Array<string>) CreateMultipleSlabs()
    {
        var c = new StableCollection<string>();
        var arr = new Array<string>();
        for (int i = 0; i < 1000; ++i)
        {
            string s = i.ToString();
            arr.Add(s);
            c.Insert(s);
        }
        return (c, arr);
    }

    [TestMethod]
    public void MultipleSlabsInsert()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        Assert.That.EqualCollections(arr, c.ToArr());
    }

    void DeleteRandomItems(StableCollection<string> c, Array<string> arr, Action<int> delete)
    {
        var rand = new SeededRandom(1337);
        for (int i = 0; i < 100; ++i)
        {
            int idx = rand.InRange(c.Count);
            arr.Remove(idx.ToString()); // must use the value because Array<T> indices aren't stable
            delete(idx);
        }
    }

    [TestMethod]
    public void MultipleSlabsRemoveAt()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        DeleteRandomItems(c, arr, (idx) => c.RemoveAt(idx));

        Assert.That.EqualCollections(arr, c.ToArr());
    }

    [TestMethod]
    public void MultipleSlabsRemove()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        DeleteRandomItems(c, arr, (idx) => c.Remove(idx.ToString()));

        Assert.That.EqualCollections(arr, c.ToArr());
    }

    [TestMethod]
    public void MultipleSlabsIndexOf()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        for (int i = 0; i < 1000; ++i)
        {
            string s = i.ToString();
            Assert.AreEqual(i, c.IndexOf(s)); // yep, this found some issues
        }

        for (int i = 11; i < 1000; i += 33)
        {
            string s = i.ToString();
            Assert.AreEqual(i, c.IndexOf(s));
            c.RemoveAt(i);
            arr.Remove(s);
            Assert.AreEqual(-1, c.IndexOf(s));

            Assert.That.EqualCollections(arr, c.ToArr());
        }
    }
}

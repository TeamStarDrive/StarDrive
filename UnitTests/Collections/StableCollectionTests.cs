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

        AssertEqual(0, c.Insert("dog"));
        AssertEqual(1, c.Count);
        Assert.IsFalse(c.IsFreeSlot(0));

        AssertEqual(1, c.Insert("cat"));
        AssertEqual(2, c.Count);
        Assert.IsFalse(c.IsFreeSlot(1));

        Assert.ThrowsException<NullReferenceException>(() => c.Insert(null));

        AssertEqualCollections(new[]{"dog","cat"}, c.ToArr());
    }

    [TestMethod]
    public void Reset()
    {
        var c = new StableCollection<string>();
        c.Reset(ArrABCDE());
        AssertEqualCollections(ArrABCDE(), c.ToArr());
        for (int i = 0; i < 10; ++i)
            c.Insert(i.ToString());

        c.Reset(ArrABCDE());
        AssertEqualCollections(ArrABCDE(), c.ToArr());
    }

    [TestMethod]
    public void IndexOf()
    {
        var c = MakeABCDE();
        AssertEqual(5, c.Count);
        AssertEqual(0, c.IndexOf("a"));
        AssertEqual(1, c.IndexOf("b"));
        AssertEqual(2, c.IndexOf("c"));
        AssertEqual(3, c.IndexOf("d"));
        AssertEqual(4, c.IndexOf("e"));
        Assert.ThrowsException<NullReferenceException>(() => c.IndexOf(null));

        Assert.IsFalse(c.IsFreeSlot(0));
        Assert.IsFalse(c.IsFreeSlot(1));
        Assert.IsFalse(c.IsFreeSlot(2));
        Assert.IsFalse(c.IsFreeSlot(3));
        Assert.IsFalse(c.IsFreeSlot(4));

        AssertEqualCollections(ArrABCDE(), c.ToArr());
    }

    [TestMethod]
    public void Contains()
    {
        var c = MakeABCDE();
        AssertEqual(5, c.Count);
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
        AssertEqual(4, c.Count); // but the count does change!
        Assert.IsTrue(c.IsFreeSlot(0));

        c.RemoveAt(2);
        Assert.IsFalse(c.Contains("c"));
        AssertEqual(3, c.Count);
        Assert.IsTrue(c.IsFreeSlot(2));

        AssertEqualCollections(new[]{"b","d","e"}, c.ToArr());
    }

    [TestMethod]
    public void Remove()
    {
        var c = MakeABCDE();
        Assert.IsFalse(c.IsFreeSlot(0));
        Assert.IsFalse(c.IsFreeSlot(2));
        Assert.IsTrue(c.Remove("a"));
        Assert.IsTrue(c.Remove("c"));
        Assert.IsTrue(c.IsFreeSlot(0));
        Assert.IsTrue(c.IsFreeSlot(2));

        Assert.IsFalse(c.Remove("a"));
        Assert.IsFalse(c.Remove("x"));
    }

    [TestMethod]
    public void ReInsert()
    {
        var c = MakeABCDE();
        Assert.IsTrue(c.Remove("a"));
        Assert.IsTrue(c.Remove("c"));
        Assert.IsTrue(c.IsFreeSlot(0));
        Assert.IsTrue(c.IsFreeSlot(2));

        AssertEqual(0, c.Insert("0"));
        Assert.IsFalse(c.IsFreeSlot(0));
        AssertEqual(2, c.Insert("2"));
        Assert.IsFalse(c.IsFreeSlot(2));

        Assert.IsTrue(c.Remove("0"));
        Assert.IsTrue(c.IsFreeSlot(0));
        AssertEqual(0, c.Insert("new"));
        Assert.IsFalse(c.IsFreeSlot(0));
    }

    [TestMethod]
    public void Enumerator()
    {
        var c = MakeABCDE();
        var items = new Array<string>();
        foreach (string s in c)
            items.Add(s);
        AssertEqualCollections(ArrABCDE(), items);
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
        AssertEqualCollections(arr, c.ToArr());
    }

    [TestMethod]
    public void MultipleSlabsReset()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        c.Reset(arr);
        AssertEqualCollections(arr, c.ToArr());

        c.Reset(ArrABCDE());
        AssertEqualCollections(ArrABCDE(), c.ToArr());

        c.Reset(arr);
        AssertEqualCollections(arr, c.ToArr());
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

        AssertEqualCollections(arr, c.ToArr());
    }

    [TestMethod]
    public void MultipleSlabsRemove()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        DeleteRandomItems(c, arr, (idx) => c.Remove(idx.ToString()));

        AssertEqualCollections(arr, c.ToArr());
    }

    [TestMethod]
    public void MultipleSlabsIndexOf()
    {
        (StableCollection<string> c, Array<string> arr) = CreateMultipleSlabs();
        for (int i = 0; i < 1000; ++i)
        {
            string s = i.ToString();
            AssertEqual(i, c.IndexOf(s)); // yep, this found some issues
        }

        for (int i = 11; i < 1000; i += 33)
        {
            string s = i.ToString();
            AssertEqual(i, c.IndexOf(s));
            c.RemoveAt(i);
            arr.Remove(s);
            AssertEqual(-1, c.IndexOf(s));

            AssertEqualCollections(arr, c.ToArr());
        }
    }
}

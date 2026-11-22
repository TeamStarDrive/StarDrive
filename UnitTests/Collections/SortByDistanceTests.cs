using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Collections;

[TestClass]
public class SortByDistanceTests
{
    static SpatialObjectBase At(float x) => new(GameObjectType.Proj) { Position = new Vector2(x, 0f) };

    static void AssertNearestFirst(SpatialObjectBase[] items, Vector2 from)
    {
        for (int i = 1; i < items.Length; ++i)
            Assert.IsTrue(items[i - 1].Position.Distance(from) <= items[i].Position.Distance(from), $"nearest first, at {i}");
    }

    [TestMethod]
    public void ASortAfterALongerOneStillSortsNearestFirst()
    {
        var many = new SpatialObjectBase[300];
        for (int i = 0; i < many.Length; ++i)
            many[i] = At(300 - i);
        many.SortByDistance(Vector2.Zero);
        AssertNearestFirst(many, Vector2.Zero);

        var few = new[] { At(5), At(-1), At(3) };
        few.SortByDistance(Vector2.Zero);
        Assert.AreEqual(-1f, few[0].Position.X, "the nearest, behind");
        Assert.AreEqual(3f, few[1].Position.X);
        Assert.AreEqual(5f, few[2].Position.X);
    }

    [TestMethod]
    public void OnlyTheCountedItemsAreSorted()
    {
        var items = new[] { At(9), At(2), At(1) };
        items.SortByDistance(2, Vector2.Zero);
        Assert.AreEqual(2f, items[0].Position.X);
        Assert.AreEqual(9f, items[1].Position.X);
        Assert.AreEqual(1f, items[2].Position.X, "the item past the count stays put");
    }

    [TestMethod]
    public void SortingAgainTakesNothingOffTheHeap()
    {
        var items = new SpatialObjectBase[20];
        for (int i = 0; i < items.Length; ++i)
            items[i] = At(i * 7 % 20);
        items.SortByDistance(Vector2.Zero);

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        items.SortByDistance(new Vector2(10f, 0f));
        Assert.AreEqual(0L, System.GC.GetAllocatedBytesForCurrentThread() - before, "the keys are kept between sorts");
        AssertNearestFirst(items, new Vector2(10f, 0f));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Point = SDGraphics.Point;

namespace UnitTests.Ships;

[TestClass]
public class HullEditorTests : StarDriveTest
{
    static ShipHull RowHull(int width, int centerX)
    {
        var hull = new ShipHull { GridCenter = new Point(centerX, 0) };
        var slots = new Array<HullSlot>();
        for (int x = 0; x < width; ++x)
            slots.Add(new HullSlot(x, 0, Restrictions.I));
        hull.SetHullSlots(slots);
        return hull;
    }

    static void AssertSymmetric(ShipHull hull)
    {
        foreach (HullSlot slot in hull.HullSlots)
        {
            HullSlot twin = hull.FindSlot(hull.MirroredSlotPos(slot.Pos));
            Assert.IsNotNull(twin, $"{slot} has no mirror slot");
            AssertEqual(slot.R, twin.R);
        }
    }

    static void AssertFullRow(ShipHull hull, int width)
    {
        AssertEqual(new Point(width, 1), hull.Size);
        for (int x = 0; x < width; ++x)
            Assert.IsNotNull(hull.FindSlot(new Point(x, 0)), $"no slot at column {x}");
    }

    [TestMethod]
    public void MirrorAxisIsTheLeftEdgeOfTheCenterColumn()
    {
        ShipHull hull = RowHull(width: 4, centerX: 2);
        AssertEqual(new Point(1, 0), hull.MirroredSlotPos(new Point(2, 0)));
        AssertEqual(new Point(3, 0), hull.MirroredSlotPos(new Point(0, 0)));
        AssertSymmetric(hull);
    }

    [TestMethod]
    public void MirroredSlotAddedPastTheLeftEdgeKeepsTheHullSymmetric()
    {
        ShipHull hull = RowHull(width: 4, centerX: 2);
        Point added = new(4, 0);
        Point mirror = hull.MirroredSlotPos(added);
        AssertEqual(new Point(-1, 0), mirror);

        var slots = new Array<HullSlot>(hull.HullSlots)
        {
            new HullSlot(added.X, added.Y, Restrictions.O),
            new HullSlot(mirror.X, mirror.Y, Restrictions.O),
        };
        hull.SetHullSlots(slots);

        AssertEqual(new Point(6, 1), hull.Size);
        AssertEqual(new Point(3, 0), hull.GridCenter);
        AssertSymmetric(hull);
    }

    [TestMethod]
    public void AFastDragPaintsEveryCellItCrosses()
    {
        ShipHull hull = RowHull(width: 4, centerX: 2);
        Point lastCell = new Point(3, 0).Sub(hull.GridCenter);

        hull = HullEditorControls.PaintSlots(hull, ref lastCell, new Point(7, 0), add: true, mirror: true);

        Assert.IsNotNull(hull);
        AssertFullRow(hull, width: 12);
        AssertSymmetric(hull);
    }

    [TestMethod]
    public void DraggingPastTheLeftEdgeKeepsPaintingAfterTheGridShifts()
    {
        ShipHull hull = RowHull(width: 4, centerX: 2);
        Point lastCell = new Point(0, 0).Sub(hull.GridCenter);

        hull = HullEditorControls.PaintSlots(hull, ref lastCell, new Point(-1, 0), add: true, mirror: false);
        AssertFullRow(hull, width: 5);

        // the grid grew left, so the next cell left of the edge is again column -1
        hull = HullEditorControls.PaintSlots(hull, ref lastCell, new Point(-1, 0), add: true, mirror: false);
        Assert.IsNotNull(hull, "the second cell was skipped");
        AssertFullRow(hull, width: 6);
    }

    [TestMethod]
    public void DragDeleteRemovesTheCellsAndTheirMirrors()
    {
        ShipHull hull = RowHull(width: 6, centerX: 3);
        Point lastCell = new Point(5, 0).Sub(hull.GridCenter);

        hull = HullEditorControls.PaintSlots(hull, ref lastCell, new Point(3, 0), add: false, mirror: true);

        Assert.IsNotNull(hull);
        AssertEqual(2, hull.HullSlots.Length);
        Assert.IsNotNull(hull.FindSlot(new Point(0, 0)));
        Assert.IsNotNull(hull.FindSlot(new Point(5, 0)));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.FleetDesign;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.UI;

[TestClass]
public class FleetButtonsListTests : StarDriveTest
{
    public FleetButtonsListTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    void AddShipTo(Fleet fleet)
    {
        fleet.AddShip(SpawnShip("Vulcan Scout", Player, new Vector2(200_000)));
    }

    FleetButtonsList CreateListAndFinishItsSlideIn()
    {
        var list = new FleetButtonsList(new RectF(0, 60, 60, 1000), Universe, Universe, null, null, _ => false);
        list.Update(1f);
        list.Update(1f);
        return list;
    }

    static Array<FleetButton> VisibleButtons(FleetButtonsList list)
    {
        var visible = new Array<FleetButton>();
        for (int i = 0; i < list.Count; ++i)
            if (list[i] is FleetButton { Visible: true } b)
                visible.Add(b);
        return visible;
    }

    static void AssertButtonsSitInTheirListSlots(FleetButtonsList list, string when)
    {
        Array<FleetButton> visible = VisibleButtons(list);
        var shown = new Array<Vector2>();
        foreach (FleetButton b in visible)
            shown.Add(b.Pos);

        list.PerformLayout();
        for (int i = 0; i < visible.Count; ++i)
            AssertEqual(0.5f, visible[i].Pos, shown[i], $"{when}: fleet {visible[i].FleetKey} sits in its list slot");
    }

    [TestMethod]
    public void AFleetThatGetsShipsAgainTakesItsOwnSlotInsteadOfSharingOne()
    {
        Fleet second = Player.CreateFleet(2, null);
        Fleet third = Player.CreateFleet(3, null);
        AddShipTo(third);

        FleetButtonsList list = CreateListAndFinishItsSlideIn();
        AssertEqual(1, VisibleButtons(list).Count, "setup: only the fleet with ships shows");
        AssertButtonsSitInTheirListSlots(list, "after the opening slide-in");

        AddShipTo(second);
        list.Update(1f);
        list.Update(1f);

        Array<FleetButton> visible = VisibleButtons(list);
        AssertEqual(2, visible.Count, "the refitted fleet shows again");
        Assert.AreNotEqual(visible[0].Pos.Y, visible[1].Pos.Y, "two fleets never share a slot");
        AssertButtonsSitInTheirListSlots(list, "after the fleet came back");
    }
}

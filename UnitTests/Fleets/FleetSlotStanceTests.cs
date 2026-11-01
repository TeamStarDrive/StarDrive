using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class FleetSlotStanceTests : StarDriveTest
{
    const string Design = "Terran-Prototype"; // its design stance is Short Range
    static readonly Vector2 FleetPos = new(200_000f, 200_000f);

    public FleetSlotStanceTests()
    {
        LoadStarterShips(Design);
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
    }

    Fleet EmptyFleet()
    {
        Fleet fleet = Player.CreateFleet(Player.CreateFleetKey(), null);
        fleet.FinalPosition = FleetPos;
        return fleet;
    }

    static FleetDataNode AddSlot(Fleet fleet, Vector2 offset)
    {
        var slot = new FleetDataNode { ShipName = Design, RelativeFleetOffset = offset };
        fleet.DataNodes.Add(slot);
        return slot;
    }

    Ship NewShip() => SpawnShip(Design, Player, FleetPos + new Vector2(20_000f, 0f));

    Ship BuildFor(Fleet fleet, FleetDataNode slot)
    {
        var build = new FleetRequisition(Design, Player, fleet, false);
        slot.Goal = build;
        Ship built = NewShip();
        build.ReportShipComplete(built);
        build.AdvanceToNextStep();
        build.Evaluate();
        Assert.AreSame(built, slot.Ship, "setup: the new ship takes the slot it was built for");
        return built;
    }

    [TestMethod]
    public void AShipBuiltForASlotTakesTheStanceSetForIt()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.Evade);

        Ship built = BuildFor(fleet, slot);

        AssertEqual(CombatState.Evade, built.AI.CombatState, "the stance picked in the fleet design screen reaches the ship built for the slot");
    }

    [TestMethod]
    public void ClearingAFleetsOrdersKeepsTheStanceSetForItsSlots()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode chosen = AddSlot(fleet, new Vector2(0f, -1500f));
        chosen.SetCombatStance(CombatState.HoldPosition);
        Ship inChosen = BuildFor(fleet, chosen);
        FleetDataNode untouched = AddSlot(fleet, new Vector2(0f, 1500f));
        Ship inUntouched = BuildFor(fleet, untouched);
        inUntouched.AI.CombatState = CombatState.Evade;

        fleet.ClearOrders();

        AssertEqual(CombatState.HoldPosition, inChosen.AI.CombatState, "a ship keeps the stance set for its slot");
        AssertEqual(CombatState.ShortRange, inUntouched.AI.CombatState, "a ship in an untouched slot goes back to its design's stance");
    }

    [TestMethod]
    public void AShipBuiltForAnUntouchedSlotKeepsItsDesignsStance()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        AssertEqual(CombatState.Artillery, slot.CombatState, "setup: an untouched slot holds the first stance of the list");

        Ship built = BuildFor(fleet, slot);

        AssertEqual(CombatState.ShortRange, built.AI.CombatState, "a slot never set by hand leaves the ship on its design's stance");
    }

    [TestMethod]
    public void AnIdleShipAssignedToASlotTakesItsStance()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.BroadsideLeft);
        Ship idle = NewShip();

        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { idle }), "setup: the idle ship fills the slot");

        AssertEqual(CombatState.BroadsideLeft, idle.AI.CombatState);
    }

    [TestMethod]
    public void AShipAddedToAFleetWithAnEmptySlotOfItsDesignTakesTheSlotsStance()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.HoldPosition);
        Ship ship = NewShip();

        fleet.AddShip(ship);

        Assert.AreSame(ship, slot.Ship, "setup: the ship fills the empty slot of its design");
        AssertEqual(CombatState.HoldPosition, ship.AI.CombatState);
    }

    [TestMethod]
    public void AShipAddedToAFleetWithAnUntouchedSlotKeepsItsDesignsStance()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        Ship ship = NewShip();

        fleet.AddShip(ship);

        Assert.AreSame(ship, slot.Ship, "setup: the ship fills the empty slot of its design");
        AssertEqual(CombatState.ShortRange, ship.AI.CombatState, "an untouched slot passes on nothing");
    }

    [TestMethod]
    public void AutoArrangeLeavesAShipInItsOwnSlotWithItsOwnStance()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode empty = AddSlot(fleet, new Vector2(0f, -1500f));
        empty.SetCombatStance(CombatState.Evade);
        FleetDataNode own = AddSlot(fleet, new Vector2(0f, 1500f));
        Ship ship = NewShip();
        fleet.AddExistingShip(ship, own);
        ship.SetCombatStance(CombatState.BroadsideRight);

        fleet.AutoArrange();

        Assert.AreSame(ship, own.Ship, "the ship stays in its slot");
        Assert.IsNull(empty.Ship, "the empty slot of its design earlier in the list stays empty");
        AssertEqual(CombatState.BroadsideRight, ship.AI.CombatState, "and the ship keeps the stance it was given");
    }

    [TestMethod]
    public void AutoArrangeKeepsTheStanceSetForASlotWhenTheGameChangedItsShips()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.Evade);
        Ship ship = NewShip();
        fleet.AddExistingShip(ship, slot);
        ship.AI.CombatState = CombatState.HoldPosition; // as a stand-ground order does

        fleet.AutoArrange();

        AssertEqual(CombatState.Evade, slot.CombatState, "the slot keeps the stance the player set for it");
        AssertEqual(CombatState.HoldPosition, ship.AI.CombatState, "and the ship already in it keeps its own");
    }

    [TestMethod]
    public void ASavedFleetDesignKeepsTheStanceSetForASlot()
    {
        var slot = new FleetDataNode { ShipName = Design };
        slot.SetCombatStance(CombatState.Evade);

        var loaded = new FleetDataNode(slot.GetDesignOnly());

        AssertEqual(CombatState.Evade, loaded.ShownStance, "a fleet design saved and loaded again keeps the slot's stance");
    }

    [TestMethod]
    public void AStanceSetOnTheSelectionPanelStaysWithTheShipsSlot()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        Ship first = NewShip();
        fleet.AddExistingShip(first, slot);

        first.SetCombatStance(CombatState.OrbitRight);
        fleet.RemoveShip(first, clearOrders: false);
        Assert.IsNull(slot.Ship, "setup: the slot is free again");

        Ship replacement = BuildFor(fleet, slot);
        AssertEqual(CombatState.OrbitRight, replacement.AI.CombatState, "the ship that next takes the slot gets the stance its last ship was given");
    }

    [TestMethod]
    public void AShipThatLeftItsFleetNoLongerChangesItsOldSlot()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.Evade);
        Ship ship = NewShip();
        fleet.AddExistingShip(ship, slot);
        fleet.RemoveShip(ship, clearOrders: false);

        ship.SetCombatStance(CombatState.AttackRuns);

        AssertEqual(CombatState.Evade, slot.CombatState, "a ship out of the fleet sets only its own stance");
    }

    [TestMethod]
    public void AnEmptySlotShowsItsDesignsStanceUntilOneIsSet()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));

        AssertEqual(CombatState.ShortRange, slot.ShownStance, "the stance buttons show the stance the ship will be built with");

        slot.SetCombatStance(CombatState.Evade);
        AssertEqual(CombatState.Evade, slot.ShownStance);
    }

    [TestMethod]
    public void AStanceSetForASlotSurvivesASave()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode slot = AddSlot(fleet, new Vector2(0f, -1500f));
        slot.SetCombatStance(CombatState.Evade);
        int key = fleet.Key;
        UState.StarDate = 1001f; // a game past its first turn loads without a capital

        SavedGame save = Universe.Save("UnitTest.FleetSlotStance", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);

        Fleet back = loaded.UState.Player.GetFleetOrNull(key);
        Assert.IsNotNull(back, "setup: the fleet loads");
        AssertEqual(1, back.DataNodes.Count, "setup: with its slot");
        AssertEqual(CombatState.Evade, back.DataNodes[0].ShownStance, "the slot still holds the stance set for it");
    }
}

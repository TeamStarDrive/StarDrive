using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Auto explore counts and sends out the player's ships of the design chosen as the scout in the automation window, even
/// one that is not of the scout role, and never a scout of either kind that a carrier or a planet launched. When nothing
/// is left to explore, only idle ships of the scout role on scout or fighter hulls are scrapped, so an armed design picked
/// as the scout and a big Recon ship stay.
/// </summary>
[TestClass]
public class AutoScoutDesignTests : StarDriveTest
{
    const string Fighter = "Fang Strafer";
    const string Scout = "Unarmed Scout";
    const string Freighter = "Owlwok Freighter S";

    public AutoScoutDesignTests()
    {
        LoadStarterShips(Fighter, Scout, Freighter);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    TestShip Spawn(string name, Empire owner)
    {
        TestShip ship = SpawnShip(name, owner, new Vector2(200_000, 200_000));
        RunObjectsSim(TestSimStep);
        return ship;
    }

    [TestMethod]
    public void TheDesignPickedAsTheScoutIsSentExploringThoughItIsAFighter()
    {
        Player.data.CurrentAutoScout = Fighter;
        TestShip fighter = Spawn(Fighter, Player);
        AssertEqual(RoleName.fighter, fighter.DesignRole, "setup: the picked design is a fighter");

        Assert.IsTrue(fighter.IsGoodScout(), "auto explore counts it as a scout");
        Assert.IsTrue(fighter.IsIdleScout(), "and sends it exploring while it is idle");
    }

    [TestMethod]
    public void AFighterOfAnotherDesignIsNoScout()
    {
        Player.data.CurrentAutoScout = Scout;
        TestShip fighter = Spawn(Fighter, Player);

        Assert.IsFalse(fighter.IsGoodScout() || fighter.IsIdleScout(), "a fighter whose design is not the picked scout is no scout");
    }

    [TestMethod]
    public void AnotherEmpiresShipOfThePickedDesignIsNoScout()
    {
        Player.data.CurrentAutoScout = Fighter;
        Enemy.data.CurrentAutoScout = Fighter;
        TestShip fighter = Spawn(Fighter, Enemy);

        Assert.IsFalse(fighter.IsGoodScout() || fighter.IsIdleScout(), "the pick is the player's own; an AI scouts with ships of the scout role");
    }

    [TestMethod]
    public void AFighterACarrierLaunchedIsNoScoutThoughItIsThePickedDesign()
    {
        Player.data.CurrentAutoScout = Fighter;
        TestShip carrier = Spawn(Scout, Player);
        TestShip fighter = Spawn(Fighter, Player);
        fighter.Mothership = carrier;

        Assert.IsFalse(fighter.IsGoodScout() || fighter.IsIdleScout(), "a carrier's fighter stays with its carrier");
    }

    [TestMethod]
    public void AScoutACarrierLaunchedIsNoScoutForExploring()
    {
        TestShip carrier = Spawn(Fighter, Player);
        TestShip scout = Spawn(Scout, Player);
        scout.Mothership = carrier;
        AssertEqual(RoleName.scout, scout.DesignRole, "setup: a ship of the scout role");

        Assert.IsFalse(scout.IsGoodScout() || scout.IsIdleScout(), "a carrier's scout stays with its carrier");
    }

    [TestMethod]
    public void APlanetsDefenseFighterIsNoScoutThoughItIsThePickedDesign()
    {
        Player.data.CurrentAutoScout = Fighter;
        Planet home = AddHomeWorldToEmpire(new Vector2(300_000), Player, new Vector2(305_000), explored: true);
        Ship defender = Ship.CreateDefenseShip(UState, Fighter, Player, home.Position + new Vector2(2000, 0), home);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(defender.IsHomeDefense, "setup: the planet launched it");

        Assert.IsFalse(defender.IsGoodScout() || defender.IsIdleScout(), "a planet's defense fighter stays to defend it");
    }

    [TestMethod]
    public void WithNothingLeftToExploreOnlyIdleScoutsOnScoutOrFighterHullsAreScrapped()
    {
        Planet home = AddHomeWorldToEmpire(new Vector2(300_000), Player, new Vector2(305_000), explored: true); // a colony to scrap at
        foreach (Planet p in home.System.PlanetList)
            p.SetExploredBy(Player);
        home.System.UpdateFullyExploredBy(Player);
        Player.data.CurrentAutoScout = Fighter;
        Player.AutoExplore = true;
        TestShip fighter = Spawn(Fighter, Player);
        TestShip scout = Spawn(Scout, Player);
        TestShip bigScout = SpawnReconFreighter();
        Assert.IsTrue(UState.Systems.All(s => s.IsFullyExploredBy(Player)), "setup: there is nothing left to explore");
        Assert.IsTrue(scout.IsIdleScout() && fighter.IsIdleScout() && bigScout.IsIdleScout(), "setup: all three are idle scouts");

        typeof(Empire).GetMethod("AssignExplorationTasks", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Player, null);

        Assert.IsFalse(Player.AutoExplore, "setup: auto explore turns itself off");
        Assert.IsTrue(Player.AI.HasGoal(GoalType.ScrapShip, scout), "an idle scout on a scout hull is scrapped");
        Assert.IsFalse(Player.AI.HasGoal(GoalType.ScrapShip, fighter), "the fighter picked as the scout is kept");
        Assert.IsFalse(Player.AI.HasGoal(GoalType.ScrapShip, bigScout), "a Recon scout on a big hull is kept");
    }

    // an unarmed freighter design set to Recon, which the shipyard makes a scout
    TestShip SpawnReconFreighter()
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(Freighter, out IShipDesign from), $"setup: {Freighter} is loaded");
        ShipDesign design = ((ShipDesign)from).GetClone($"{Freighter} Recon");
        design.ShipCategory = ShipCategory.Recon;
        design.SetDesignSlots(from.GetOrLoadDesignSlots(), updateRole: true);
        TestShip ship = SpawnShip(Ship.CreateNewShipTemplate(Empire.Void, design), Player, new Vector2(200_000, 200_000));
        RunObjectsSim(TestSimStep);
        AssertEqual(RoleName.scout, ship.DesignRole, "setup: a scout");
        AssertEqual(RoleName.freighter, ship.ShipData.HullRole, "setup: on a freighter hull");
        return ship;
    }
}

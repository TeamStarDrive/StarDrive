using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Gameplay;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.ExoticSystems;

/// <summary>
/// A research or mining station goes to a system holding a colony of the empire's, no farther from its weighted center
/// than its systems lie on average, or inside its own borders or those of an empire it has an alliance or open borders
/// with; a trade treaty alone does not count. Lying next to one of its colonies is not enough.
/// The player's automatic stations follow the same rule. A fleet goes to clear a system near home by the same measure.
/// </summary>
[TestClass]
public class StationDistanceTests : StarDriveTest
{
    static readonly MethodInfo MineablePlanets = typeof(MiningOpsPlanner).GetMethod("GetPotentialMineablePlanets", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo SetInfluenceActive = typeof(Ship_Game.Empire).GetMethod("SetInfluenceActive", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly Vector2 Center = new(-600_000, 0);
    readonly Planet RemoteColony;

    public StationDistanceTests()
    {
        CreateUniverseAndPlayerEmpire();
        AddHomeWorldToEmpire(new Vector2(0, 900_000), Player);
        AddHomeWorldToEmpire(Center, Enemy);
        AddDummyPlanetToEmpire(Center + new Vector2(0, 200_000), Enemy);
        AddDummyPlanetToEmpire(Center + new Vector2(0, -200_000), Enemy);
        RemoteColony = AddDummyPlanetToEmpire(Center + new Vector2(1_200_000, 0), Enemy);
        Enemy.WeightedCenter = Center;
        AssertEqual(1f, 400_000f, Enemy.AverageSystemDistanceFromCenter, "setup: the empire's systems lie 400,000 from its center on average");
    }

    // a planet to mine, explored by the empire, in a system of its own this far from the empire's center
    Planet MineableAt(Vector2 offset)
    {
        Planet p = AddDummyPlanet(Center + offset);
        Mineable(p);
        foreach (SolarSystem s in UState.Systems)
            s.FiveClosestSystems = UState.GetFiveClosestSystems(s);
        return p;
    }

    void Mineable(Planet p)
    {
        p.Mining = new Mineable(p);
        p.System.SetExploredBy(Enemy);
        p.SetExploredBy(Enemy);
    }

    bool WantsToMine(Planet p) => ((Planet[])MineablePlanets.Invoke(Enemy.AI.MiningOpsAI, new object[] { false })).Contains(p);

    [TestMethod]
    public void AStationGoesNoFartherFromTheEmpiresCenterThanItsSystemsLieOnAverage()
    {
        Planet within = MineableAt(new Vector2(300_000, 0));
        Planet beyond = MineableAt(new Vector2(600_000, 50_000));

        Assert.IsTrue(WantsToMine(within), "300,000 from the center, within the 400,000 its systems lie from it on average");
        Assert.IsFalse(WantsToMine(beyond), "600,000 from the center, outside every border and beyond the average");
    }

    [TestMethod]
    public void LyingNextToARemoteColonyIsNotEnoughForAStation()
    {
        Planet nextToIt = MineableAt(new Vector2(1_400_000, 0));
        Assert.IsTrue(nextToIt.System.FiveClosestSystems.Contains(RemoteColony.System), "setup: the remote colony's system is among its five closest");
        Assert.AreEqual(InfluenceStatus.Neutral, UState.Influence.GetInfluenceStatus(Enemy, nextToIt.System.Position), "setup: outside every border");

        Assert.IsFalse(WantsToMine(nextToIt), "a system beside a colony far from the center, outside every border, gets no station");
    }

    [TestMethod]
    public void AStationGoesToASystemHoldingAColonyOfTheEmpiresHoweverFar()
    {
        Mineable(RemoteColony);
        Assert.IsTrue(WantsToMine(RemoteColony), "the remote colony's own system, 1,200,000 from the center");

        SetInfluenceActive.Invoke(Enemy, new object[] { false });
        Assert.IsFalse(Enemy.InfluenceActive, "setup: espionage has the empire's borders down");
        Assert.IsTrue(HelperFunctions.InGoodDistanceForStations(Enemy, RemoteColony.System, Enemy.AverageSystemDistanceFromCenter),
                      "a system holding a colony of the empire's, even while its borders are down");
    }

    [TestMethod]
    public void AFleetGoesToClearASystemNoFartherFromTheEmpiresCenterThanItsSystemsLieOnAverage()
    {
        SolarSystem within = MineableAt(new Vector2(300_000, 0)).System;
        SolarSystem beyond = MineableAt(new Vector2(600_000, 50_000)).System;

        Assert.IsTrue(HelperFunctions.WorthAFleetToClear(Enemy, within), "300,000 from the center, within the average");
        Assert.IsFalse(HelperFunctions.WorthAFleetToClear(Enemy, beyond), "600,000 from the center, with no faction there");
        Assert.IsTrue(HelperFunctions.WorthAFleetToClear(Enemy, RemoteColony.System), "a system holding a colony of the empire's, however far");
    }

    [TestMethod]
    public void AStationGoesAnywhereInsideTheEmpiresOwnBorders()
    {
        SolarSystem besideTheColony = MineableAt(new Vector2(1_240_000, 0)).System;
        Assert.IsTrue(UState.Influence.IsInInfluenceOf(Enemy, besideTheColony.Position), "setup: inside the remote colony's borders");

        Assert.IsTrue(HelperFunctions.InGoodDistanceForStations(Enemy, besideTheColony, Enemy.AverageSystemDistanceFromCenter),
                      "inside its borders, however far from the center");
    }

    [TestMethod]
    [DataRow(null, false, DisplayName = "no treaty: no station")]
    [DataRow(TreatyType.Trade, false, DisplayName = "a trade treaty alone: no station")]
    [DataRow(TreatyType.OpenBorders, true, DisplayName = "open borders: a station")]
    [DataRow(TreatyType.Alliance, true, DisplayName = "an alliance: a station")]
    public void InsideAnotherEmpiresBordersAStationGoesOnlyWithAnAllianceOrOpenBorders(TreatyType? treaty, bool goes)
    {
        Vector2 nearTheirHome = new(60_000, 900_000);
        SolarSystem theirs = AddDummyPlanet(nearTheirHome).System;
        Assert.IsTrue(UState.Influence.IsInInfluenceOf(Player, nearTheirHome) && !UState.Influence.IsInInfluenceOf(Enemy, nearTheirHome),
                      "setup: inside the other empire's borders only, far from the empire's center");
        Enemy.AI.EndWarFromEvent(Player);
        if (treaty != null)
            Enemy.SignTreatyWith(Player, treaty.Value);

        Assert.AreEqual(goes, HelperFunctions.InGoodDistanceForStations(Enemy, theirs, Enemy.AverageSystemDistanceFromCenter));
    }
}

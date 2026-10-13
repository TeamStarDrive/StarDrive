using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class ColonyShipAttackTests : StarDriveTest
{
    const string Warship = "Vulcan Scout";
    const string ColonyShip = "Colony Ship";
    readonly SolarSystem System;
    readonly TestShip Settler;
    readonly TestShip Guard;

    public ColonyShipAttackTests()
    {
        LoadStarterShips(Warship, ColonyShip);
        CreateUniverseAndPlayerEmpire();
        CreateThirdMajorEmpire();
        System = AddDummyPlanetToEmpire(new Vector2(200_000f), Player).System;
        Settler = SpawnShip(ColonyShip, ThirdMajor, System.Position + new Vector2(10_000f, 0f));
        Guard = SpawnShip(Warship, Player, System.Position + new Vector2(12_000f, 0f));
        UState.Objects.Update(new(time: 1f)); // joins the spatial index
        UState.Objects.Update(new(time: 1f)); // placed in the system
        Assert.AreSame(System, Settler.System, "setup: the colony ship is in the player's system");
        Assert.IsFalse(Player.IsAtWarWith(ThirdMajor), "setup: no war");
    }

    bool PlayerMayFireOnTheSettler => Settler.IsAttackable(Player, Player.GetRelations(ThirdMajor));

    [TestMethod]
    public void TheColonyShipOfAnEmpireWithNoTreatyMayBeAttackedOnOrderInTheSystemWeHold()
    {
        Assert.IsFalse(PlayerMayFireOnTheSettler, "setup: our ships leave it alone on their own");

        Guard.AI.OrderAttackSpecificTarget(Settler);

        Assert.AreSame(Settler, Guard.AI.Target, "the order is taken");
        Assert.IsTrue(PlayerMayFireOnTheSettler, "and our weapons may hit it");
    }

    [TestMethod]
    public void AnAttackOrderOnAShipThatBecameOursIsIgnored()
    {
        TestShip captured = SpawnShip(Warship, Player, System.Position + new Vector2(14_000f, 0f));

        Guard.AI.OrderAttackSpecificTarget(captured);

        Assert.AreNotSame(captured, Guard.AI.Target, "our own ship is not taken as a target");
    }

    [TestMethod]
    public void AnOrderedAttackOnAColonyShipHitsIt()
    {
        Settler.AI.OrderHoldPosition();
        float health = Settler.Health;

        Guard.AI.OrderAttackSpecificTarget(Settler);
        RunSimWhile((simTimeout: 20, fatal: false), () => Settler.Health >= health && Settler.Active);

        Assert.IsTrue(Settler.Health < health || !Settler.Active, "our ship fires on the colony ship and its shots land");
    }

    [TestMethod]
    public void AnOrderedAttackEndsWhenTheColonyShipLeavesTheSystem()
    {
        Guard.AI.OrderAttackSpecificTarget(Settler);
        Settler.SetSystem(null);

        Assert.IsFalse(PlayerMayFireOnTheSettler, "outside the system it settles, the colony ship is not ours to attack");
    }

    [TestMethod]
    public void AColonyShipThatLeftAndCameBackNeedsANewOrder()
    {
        Guard.AI.OrderAttackSpecificTarget(Settler);
        Vector2 inside = Settler.Position;

        Settler.Position = System.Position + new Vector2(System.Radius * 3f, 0f);
        UState.Objects.Update(new(time: 1f));
        UState.Objects.Update(new(time: 1f));
        Assert.IsNull(Settler.System, "setup: the colony ship left the system");
        Settler.Position = inside;
        UState.Objects.Update(new(time: 1f));
        UState.Objects.Update(new(time: 1f));
        Assert.AreSame(System, Settler.System, "setup: and came back");

        Assert.IsFalse(PlayerMayFireOnTheSettler, "the order held while it stayed; a new visit needs a new order");
    }

    [TestMethod]
    public void AQueuedAttackOnAColonyShipCountsAsAnOrder()
    {
        TestShip hostile = SpawnShip(Warship, Enemy, Guard.Position + new Vector2(1_000f, 0f));
        Guard.AI.OrderAttackSpecificTarget(hostile);
        Assert.AreSame(hostile, Guard.AI.Target, "setup: the guard is on another target");

        Guard.AI.OrderQueueSpecificTarget(Settler);

        Assert.IsTrue(PlayerMayFireOnTheSettler, "a colony ship queued with shift and a right-click is ordered attacked too");
    }

    [TestMethod]
    public void AColonyShipOfAnEmpireWithANonAggressionPactMayNotBeAttacked()
    {
        Player.SignTreatyWith(ThirdMajor, TreatyType.NonAggression);

        Guard.AI.OrderAttackSpecificTarget(Settler);

        Assert.IsNull(Guard.AI.Target, "a treaty holds");
        Assert.IsFalse(PlayerMayFireOnTheSettler);
    }

    [TestMethod]
    public void AColonyShipJoiningItsOwnEmpiresColonyMayNotBeAttacked()
    {
        Planet theirs = AddPlanetTo(System, ThirdMajor);
        Assert.IsTrue(System.HasPlanetsOwnedBy(ThirdMajor), "setup: they hold a planet in the system too");

        Guard.AI.OrderAttackSpecificTarget(Settler);

        Assert.IsNull(Guard.AI.Target, $"a system both empires hold is not ours alone ({theirs.Name})");
    }

    [TestMethod]
    public void AnAIStopsFiringOnColonyShipsInASystemItClaimedOnceWeHaveANonAggressionPact()
    {
        TestShip ours = SpawnShip(ColonyShip, Player, System.Position + new Vector2(-10_000f, 0f));
        SolarSystem claimed = AddDummyPlanet(new Vector2(-200_000f)).System;
        ours.Position = claimed.Position + new Vector2(10_000f, 0f);
        UState.Objects.Update(new(time: 1f));
        UState.Objects.Update(new(time: 1f));
        Assert.AreSame(claimed, ours.System, "setup: our colony ship is in a system the AI claimed");
        Relationship aiToUs = ThirdMajor.GetRelations(Player);
        aiToUs.WarnedSystemsList.Add(claimed);
        Assert.IsTrue(ours.IsAttackable(ThirdMajor, aiToUs), "setup: after its warning the AI fires on our colony ships there");

        ThirdMajor.SignTreatyWith(Player, TreatyType.NonAggression);

        Assert.IsFalse(ours.IsAttackable(ThirdMajor, aiToUs), "a non-aggression pact stops it; the claim and its anger remain");
    }

    [TestMethod]
    public void AnAIUnderAPeaceTreatyDoesNotFireOnColonyShipsInASystemItClaimsAfterwards()
    {
        TestShip ours = SpawnShip(ColonyShip, Player, new Vector2(-200_000f) + new Vector2(10_000f, 0f));
        SolarSystem claimed = AddDummyPlanet(new Vector2(-200_000f)).System;
        UState.Objects.Update(new(time: 1f));
        UState.Objects.Update(new(time: 1f));
        Assert.AreSame(claimed, ours.System, "setup: our colony ship is in the system");
        ThirdMajor.SignTreatyWith(Player, TreatyType.Peace);
        Relationship aiToUs = ThirdMajor.GetRelations(Player);
        aiToUs.WarnedSystemsList.Add(claimed);

        Assert.IsFalse(ours.IsAttackable(ThirdMajor, aiToUs), "a claim made during a peace treaty is not fired upon either");
    }

    Planet AddPlanetTo(SolarSystem system, Ship_Game.Empire owner)
    {
        var p = new Planet(UState.CreateId(), system, system.Position + new Vector2(-15_000f, 0f), fertility: 1f, minerals: 1f, maxPop: 4f);
        p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
        p.TestSetOrbitalRadius(p.Position.Distance(system.Position) + p.Radius);
        system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
        system.PlanetList.Add(p);
        p.SetOwner(owner);
        return p;
    }
}

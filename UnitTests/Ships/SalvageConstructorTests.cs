using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage unit is a hangar bay, as a refining module is a mining bay: it carries 60 ordnance and launches the
/// empire's constructor, or with auto pick the cheapest whatever the platform costs, never a fighter, for an order on
/// a derelict, once its salvage ship is parked at its mission's site. The launch costs ordnance, so a ship short of it
/// waits, and the unit then waits out its 30 s timer; a constructor lost on its way is launched again. After its
/// launch the constructor flies to a site 100 out from the derelict's radius, however far from its ship, and claims
/// the derelict as it starts to build there, with the trust cost of salvaging in a system where another empire has a
/// colony, once for each platform. Its construction is the platform's cost to the empire, never below 100, both times
/// the production pace. The platform it builds takes over the derelict, the constructor is gone with the ordnance it
/// took, and its unit is free to launch again; once the platform has taken itself apart, the unit gets that ordnance
/// back. A rival's constructor turns back when the derelict is claimed and docks, giving its ordnance back, and so does
/// one whose order the second click calls off; one whose ship is destroyed or boarded is scuttled at once, and one
/// flying home to a lost ship is removed. One boarded on its way is its boarder's ship, and its unit launches another
/// for the order; one boarded on its way home leaves its unit to the next order. Launched orders hold the units before
/// those waiting to launch. A ship whose last order ends waits for its constructor flying home to dock before it
/// goes. A platform's loss ends its order and its claim, and calling the order off gives up the
/// claim at once while the platform stays to come apart. An order mid-flight survives a save and goes on to build.
/// </summary>
[TestClass]
public class SalvageConstructorTests : SalvageOrderTest
{
    public SalvageConstructorTests()
    {
        LoadStarterShips("Owlwok Constructor", "Kulrathi Constructor");
    }

    // a salvageable derelict of the player's, so the player knows of it, and its killer too; the ship itself is gone
    Derelict FormsKilledBy(Vector2 at, Empire killer)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, killer), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    static SalvageDerelict OrderOf(Empire empire, Derelict d) => SalvageDerelict.OrderOf(empire, d);

    static Ship ShipOf(SalvageDerelict order) => MissionOf(order).SalvageShip;

    static ShipModule BayOf(Ship ship, Ship launched) => ship.Modules.Find(m => m.TryGetHangarShip(out Ship s) && s == launched);

    // runs the universe's objects while `running` holds, evaluating the order once a simulated second as its empire's turn does
    void RunWithOrder(SalvageDerelict order, double seconds, Func<bool> running)
    {
        int frame = 0;
        RunSimWhile((seconds, fatal: true), running, () =>
        {
            if (++frame % 60 == 0 && order.Owner.AI.HasGoal(order))
                order.Evaluate();
        });
    }

    void RunPastLaunch(Ship constructor) => RunSimWhile((simTimeout: 5, fatal: true), () => constructor.IsLaunching);

    // out of reach of the ship's death blast
    void RunClearOf(Ship ship, Ship constructor)
        => RunSimWhile((simTimeout: 30, fatal: true), () => constructor.Position.InRadius(ship.Position, 1500f));

    // the empire's order, its salvage ship built at its site, its constructor launched
    SalvageDerelict OrderedAndLaunched(Derelict d, Empire empire = null)
    {
        SalvageDerelict order = Ordered(d, empire: empire);
        if (ShipOf(order) == null)
            ShipBuilt(order);
        Launched(order);
        return order;
    }

    // the player's order, its constructor launched toward a platform site on the derelict's far side from the ship, so it flies clear of the ship's death blast
    SalvageDerelict LaunchedAway(Derelict d)
    {
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        order.StaticBuildPos = d.RestPosition + ship.Position.DirectionToTarget(d.RestPosition) * (d.Radius + SalvageDerelict.PlatformClearance);
        Launched(order);
        return order;
    }

    // the player's order with its platform built and at work
    SalvageDerelict WithItsPlatform(Derelict d)
    {
        SalvageDerelict order = OrderedAndLaunched(d);
        RunWithOrder(order, 120, () => order.Platform == null);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    [TestMethod]
    public void ASalvageUnitIsAHangarBayThatLaunchesTheEmpiresConstructorAndNoFighter()
    {
        Ship ship = SpawnShip(TwinShip, Player, Homeworld.Position + new Vector2(5_000f, 0));
        ShipModule[] units = ship.Modules.Filter(m => m.Salvage);
        AssertEqual(2, units.Length, "setup: the ship has two salvage units");
        foreach (ShipModule unit in units)
            Assert.IsTrue(unit.IsSalvageBay && unit.Is(ShipModuleType.Hangar) && !unit.IsFighterHangar, "a salvage unit is a hangar bay but no fighter hangar");
        AssertEqual(2, ship.Carrier.AllHangars.Length, "the ship's hangars are its units");
        AssertEqual(0, ship.Carrier.AllFighterHangars.Length, "and it has no fighter hangar");
        AssertEqual(0.01f, 120f, ship.OrdinanceMax, "each unit carries 60 ordnance");

        AssertEqual(Constructor, CarrierBays.HangarShipName(Player, units[0]), "a unit launches the empire's constructor");
        foreach (IShipDesign design in Enemy.ShipsWeCanBuildSnapshot)
            if (design.IsConstructor)
                Enemy.RemoveBuildableShip(design);
        AssertEqual("", CarrierBays.HangarShipName(Enemy, units[0]), "and nothing else when its empire has no constructor to launch");
    }

    [TestMethod]
    public void WithAutoPickAUnitLaunchesTheCheapestConstructorWhateverThePlatformCosts()
    {
        ShipModule unit = SpawnShip("Terran Salvage Ship", Player, Homeworld.Position + new Vector2(5_000f, 0)).Modules.Find(m => m.Salvage);
        IShipDesign cheap = ResourceManager.Ships.GetDesign("Owlwok Constructor");
        IShipDesign dearer = ResourceManager.Ships.GetDesign("Kulrathi Constructor");
        Player.AddBuildableShip(cheap);
        Player.AddBuildableShip(dearer);
        Player.AutoPickConstructors = true;
        AssertLessThan(cheap.GetCost(Player), dearer.GetCost(Player), "setup: one constructor is cheaper");

        float construction = SalvageDerelict.PlatformConstruction(Player, ResourceManager.Ships.GetDesign(ShipDesign.SalvagePlatformName));
        AssertGreaterThan(construction, dearer.GetCost(Player) * 2f, "setup: the platform takes more than twice either's cost");
        Assert.AreSame(dearer, ShipBuilder.PickConstructor(Player, construction, GetCheapest: true),
                       "setup: so a job of that size gets the faster one");
        AssertEqual(cheap.Name, CarrierBays.HangarShipName(Player, unit), "a unit launches the cheapest");

        Player.AutoPickConstructors = false;
        AssertEqual(Constructor, CarrierBays.HangarShipName(Player, unit), "and without auto pick the empire's own");
    }

    [TestMethod]
    public void AnOrderLaunchesTheConstructorFromAFreeUnitOfItsParkedShipForItsOrdnance()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        float ordnance = ship.Ordinance;
        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "a ship not yet parked at its site launches nothing");
        AssertEqual("LaunchConstructor", order.StepName, "and the order waits");

        Ship constructor = Launched(order);
        Assert.IsTrue(constructor is { Name: Constructor, IsConstructor: true, IsLaunching: true }, "parked, it launches the empire's constructor as a hangar launches its ship");
        Assert.AreSame(ship, constructor.Mothership, "from the salvage ship");
        ShipModule bay = BayOf(ship, constructor);
        Assert.IsTrue(bay is { IsSalvageBay: true }, "out of its salvage unit");
        AssertEqual(0.01f, ordnance - 26f, ship.Ordinance, "which the ship pays, the constructor's 26");
        AssertEqual(0.01f, 30f, bay.HangarTimer, "and the unit waits out its 30 s timer");

        AssertEqual("BuildPlatform", order.StepName, "the order waits for its platform");
        Assert.IsTrue(constructor.AI.OrderQueue.PeekFirst is { Plan: ShipAI.Plan.DeployStructure } g && g.Goal == order, "the constructor is off to build it");
        AssertEqual(1f, d.Radius + SalvageDerelict.PlatformClearance, order.BuildPosition.Distance(d.RestPosition), "on a site 100 out from the derelict's radius");
        Assert.IsNull(d.Claimant, "and the derelict is not claimed before the constructor starts to build");

        Universe.UpdateClickableItems();
        Assert.IsFalse(Universe.ClickableBuildGoals.Any(b => b.AssociatedGoal == order), "the order puts no build marker on the map");
    }

    [TestMethod]
    public void AUnitWhoseConstructorStillFliesLaunchesNoOther()
    {
        TwinShips();
        Derelict a = Forms(Wreck);
        Derelict b = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict first = OrderedAndLaunched(a);
        Ship ship = ShipOf(first);
        ShipModule flying = BayOf(ship, first.FinishedShip);
        foreach (ShipModule unit in ship.Carrier.AllHangars)
            unit.HangarTimer = 0f; // its timer runs out while the constructor still flies

        Ship second = OrderedAndLaunched(b).FinishedShip;
        Assert.AreNotSame(flying, BayOf(ship, second), "the second order's constructor comes from the other unit");
    }

    [TestMethod]
    public void ADeadUnitLaunchesNothing()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        ShipModule[] units = ship.Carrier.AllHangars;
        units[0].Die(null, cleanupOnly: true);

        Ship constructor = Launched(order);
        Assert.AreSame(units[1], BayOf(ship, constructor), "the live unit launches the constructor");
    }

    [TestMethod]
    public void ThePlatformsConstructionIsItsCostNeverBelowAHundredBothTimesThePace()
    {
        IShipDesign platform = ResourceManager.Ships.GetDesign(ShipDesign.SalvagePlatformName);
        AssertGreaterThan(platform.GetCost(Player), 100f, "setup: the platform costs more than 100");
        AssertEqual(0.01f, platform.GetCost(Player), SalvageDerelict.PlatformConstruction(Player, platform), "so its construction is its cost");
        SalvageDerelict order = OrderedAndLaunched(Forms(Wreck));
        AssertEqual(0.01f, platform.GetCost(Player), order.FinishedShip.Construction.ConstructionNeeded, "the constructor builds that much");

        Player.data.Traits.ShipCostMod = -0.5f;
        AssertLessThan(platform.GetCost(Player), 100f, "setup: a cost bonus takes the platform's cost below 100");
        AssertEqual(0.01f, 100f, SalvageDerelict.PlatformConstruction(Player, platform), "and its construction to 100");

        UState.P.Pace = 3f;
        AssertLessThan(platform.GetCost(Player), 100f * UState.ProductionPace, "setup: at a slower pace it still costs less than 100 times it");
        AssertEqual(0.01f, 100f * UState.ProductionPace, SalvageDerelict.PlatformConstruction(Player, platform), "times the production pace");
        SalvageDerelict floored = OrderedAndLaunched(Forms(Wreck + new Vector2(0, 5_000f)));
        AssertEqual(0.01f, 100f * UState.ProductionPace, floored.FinishedShip.Construction.ConstructionNeeded, "which the next constructor builds");
    }

    [TestMethod]
    public void AShipShortOfOrdnanceLaunchesOnceItHasEnough()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        ship.ChangeOrdnance(-ship.Ordinance);
        Parked(order);

        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "a ship with no ordnance launches nothing");
        AssertEqual("LaunchConstructor", order.StepName, "and the order waits");

        ship.ChangeOrdnance(ship.OrdinanceMax);
        order.Evaluate();
        Assert.IsNotNull(order.FinishedShip, "it launches once the ship has the ordnance");
    }

    [TestMethod]
    public void AConstructorLostOnItsWayIsLaunchedAgainOnceAUnitIsReady()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship lost = order.FinishedShip;
        ShipModule[] units = ship.Modules.Filter(m => m.IsSalvageBay);
        foreach (ShipModule unit in units)
            unit.HangarTimer = unit.HangarTimerConstant; // the other one launched lately too

        lost.Die(null, cleanupOnly: true);
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order), "the order stands when its constructor is lost");
        Assert.IsNull(order.FinishedShip, "no unit launches while its timer runs");

        RunSimWhile((simTimeout: 40, fatal: true), () => units.All(u => u.HangarTimer > 0f));
        order.Evaluate();
        Assert.IsTrue(order.FinishedShip is { Active: true } again && again != lost, "a unit whose timer ran out launches another");
    }

    [TestMethod]
    public void TheConstructorFliesToItsSiteClaimsTheDerelictAsItStartsToBuildAndLeavesThePlatform()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        ShipModule bay = BayOf(ship, constructor);
        float ordnance = ship.Ordinance;

        RunWithOrder(order, 120, () => d.Claimant == null);
        AssertEqual(Player, d.Claimant, "the derelict is claimed");
        AssertLessThan(constructor.Position.Distance(order.BuildPosition), ConstructionShip.ConstructingDistance, "once the constructor is at its site");
        Assert.IsNull(order.Platform, "as it starts to build");
        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns, d.TurnsLeft, "and its timer stands");

        RunWithOrder(order, 120, () => order.Platform == null);
        Ship platform = order.Platform;
        Assert.IsTrue(platform is { Active: true, IsSalvagePlatform: true } && platform.Loyalty == Player, "the constructor builds the empire's salvage platform");
        AssertLessThan(platform.Position.Distance(order.BuildPosition), 1f, "on its site");
        Assert.IsFalse(constructor.Active, "and goes, as a constructor that deploys does");
        Assert.IsNull(bay.HangarShip, "its unit is free to launch again");
        AssertEqual(0.01f, 0f, ship.Carrier.OrdnanceInSpace, "and the ordnance the constructor took is spent");
        AssertEqual(0.01f, ordnance, ship.Ordinance, "not given back while the platform stands");

        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "the order goes on with the platform");
        AssertEqual(Player, d.Claimant, "holding its claim");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "with the button lit");
    }

    [TestMethod]
    public void EachPlatformCostsTheTrustOfAnAIWithAColonyInTheSystemAsItsConstructorStartsToBuild()
    {
        TwinShips();
        Derelict first = Forms(Wreck);
        Derelict second = Forms(Wreck + new Vector2(0, 5_000f));
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = false;
        Relationship aiToPlayer = Enemy.GetRelations(Player);
        float trust = aiToPlayer.Trust;

        SalvageDerelict a = OrderedAndLaunched(first);
        AssertEqual(0.01f, trust, aiToPlayer.Trust, "no trust is lost as the ship parks or launches");
        RunWithOrder(a, 120, () => first.Claimant == null);
        AssertEqual(0.01f, trust - 5f, aiToPlayer.Trust, "the AI with a colony in the system trusts the player 5 less as the constructor starts to build");
        RunWithOrder(a, 30, () => a.Platform == null);
        AssertEqual(0.01f, trust - 5f, aiToPlayer.Trust, "once for that platform");

        SalvageDerelict b = OrderedAndLaunched(second);
        RunWithOrder(b, 120, () => second.Claimant == null);
        AssertEqual(0.01f, trust - 10f, aiToPlayer.Trust, "and 5 more for the next");
    }

    [TestMethod]
    public void APlatformThatTookItselfApartGivesItsUnitBackTheOrdnanceItsLaunchTook()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        float afterLaunch = ship.Ordinance;
        PlatformBuilt(order);
        AssertEqual(0.01f, afterLaunch, ship.Ordinance, "setup: the launch's ordnance is spent while the platform stands");

        SalvageDerelict.Cancel(Player, d);
        for (int turn = 0; turn < 1 + SalvageDerelict.DismantleTurns && Player.AI.HasGoal(order); ++turn)
            order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order) || order.Platform.Active, "setup: the platform has taken itself apart");
        AssertEqual(0.01f, afterLaunch + 26f, ship.Ordinance, "and its unit gets the launch's 26 ordnance back");
    }

    [TestMethod]
    public void ARivalsConstructorTurnsBackWhenTheDerelictIsClaimedAndDocks()
    {
        Derelict d = FormsKilledBy(Wreck, killer: Enemy);
        SalvageDerelict order = Ordered(d, empire: Enemy);
        Ship ship = ShipBuilt(order);
        float full = ship.Ordinance;
        Ship constructor = Launched(order);
        ShipModule bay = BayOf(ship, constructor);
        RunPastLaunch(constructor);

        Assert.IsTrue(d.TryClaim(Player), "setup: the player's constructor starts to build there first");
        order.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(order), "the rival's order ends");
        RunObjectsSim(TestSimStep);
        AssertEqual(AIState.ReturnToHangar, constructor.AI.State, "and its constructor turns back");

        RunSimWhile((simTimeout: 60, fatal: true), () => constructor.Active);
        AssertEqual(0.01f, full, ship.Ordinance, "it docks at its salvage ship, which gets the launch ordnance back");
        Assert.IsNull(bay.HangarShip, "and its unit is free");
        AssertEqual(Player, d.Claimant, "and the claim stays the player's");
    }

    [TestMethod]
    public void TheSecondClickSendsAFlyingConstructorHome()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship constructor = order.FinishedShip;
        RunPastLaunch(constructor);

        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsNull(OrderOf(Player, d), "the click calls the order off");
        RunObjectsSim(TestSimStep);
        AssertEqual(AIState.ReturnToHangar, constructor.AI.State, "and its constructor flies home");
    }

    [TestMethod]
    public void AShipWhoseLastOrderEndsWaitsForItsConstructorToDockBeforeItGoes()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunPastLaunch(constructor);
        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: its last order ends, its constructor flying home");

        mission.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(mission) && ship.WorksForSalvage, "the ship waits for its constructor");
        RunSimWhile((simTimeout: 60, fatal: true), () => constructor.Active);
        mission.Evaluate();
        Assert.IsTrue(!Player.AI.HasGoal(mission) && !ship.WorksForSalvage, "and once it has docked, the ship goes, a ship like any other");
    }

    [TestMethod]
    public void AConstructorInFlightWhoseShipIsDestroyedIsScuttledAtOnce()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = LaunchedAway(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunClearOf(ship, constructor);

        ship.Die(null, cleanupOnly: true);
        Assert.IsTrue(constructor.Active, "setup: the constructor outlives the ship's blast");
        MissionOf(order).Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the order ends with its ship");
        Assert.IsFalse(constructor.Active, "and its constructor is scuttled at once");
    }

    [TestMethod]
    public void AConstructorInFlightWhoseShipIsBoardedIsScuttledAtOnce()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunPastLaunch(constructor);

        ship.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(ship);
        ship.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, ship.Loyalty, "setup: the ship is boarded");
        AssertGreaterThan(ship.Carrier.OrdnanceInSpace, 0f, "setup: the constructor's ordnance is out with it");
        MissionOf(order).Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the order ends with its ship");
        Assert.IsFalse(constructor.Active, "and its constructor is scuttled at once");
        AssertEqual(0.01f, 0f, ship.Carrier.OrdnanceInSpace, "the ship's new owner does not count its ordnance as out");
    }

    [TestMethod]
    public void AConstructorBoardedOnItsWayIsTheBoardersAndItsUnitLaunchesAnother()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship boarded = order.FinishedShip;
        ShipModule bay = BayOf(ship, boarded);
        RunPastLaunch(boarded);
        float inSpace = ship.Carrier.OrdnanceInSpace;

        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(boarded);
        boarded.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, boarded.Loyalty, "setup: the constructor is boarded");
        order.Evaluate();
        Assert.IsNull(boarded.Mothership, "the boarded constructor is no longer the ship's");
        Assert.IsNull(bay.HangarShip, "and its unit is free");
        AssertEqual(0.01f, inSpace - 26f, ship.Carrier.OrdnanceInSpace, "with the 26 ordnance it took written off");

        bay.HangarTimer = 0f; // its timer ran out meanwhile
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(order.FinishedShip is { } another && another != boarded && another.Loyalty == Player, "the order launches another constructor");
        AssertEqual(Enemy, boarded.Loyalty, "and leaves the boarded one to its boarder");
    }

    [TestMethod]
    public void AConstructorBoardedOnItsWayHomeLeavesItsUnitToTheNextOrder()
    {
        TwinShips();
        Derelict first = Forms(Wreck);
        Derelict second = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict next = Forms(Wreck + new Vector2(0, -5_000f));
        SalvageDerelict order = OrderedAndLaunched(first);
        Ship ship = ShipOf(order);
        Ship boarded = order.FinishedShip;
        ShipModule bay = BayOf(ship, boarded);
        OrderedAndLaunched(second);
        RunPastLaunch(boarded);
        SalvageDerelict.Cancel(Player, first);
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the order ends before its platform, its constructor flying home");

        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(boarded);
        boarded.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, boarded.Loyalty, "setup: the constructor is boarded on its way home");
        bay.HangarTimer = 0f; // its timer ran out meanwhile

        SalvageDerelict nextOrder = Ordered(next);
        Assert.AreSame(MissionOf(order), MissionOf(nextOrder), "a new order counts the unit free");
        Ship launched = Launched(nextOrder);
        Assert.AreSame(bay, BayOf(ship, launched), "and launches its constructor from the boarded one's bay");
        Assert.IsNull(boarded.Mothership, "which lets the boarded constructor go");
        AssertEqual(Enemy, boarded.Loyalty, "to its boarder");
    }

    [TestMethod]
    public void AConstructorFlyingHomeToAShipLostOnTheWayIsRemoved()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = LaunchedAway(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunClearOf(ship, constructor);
        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        RunObjectsSim(TestSimStep);
        AssertEqual(AIState.ReturnToHangar, constructor.AI.State, "setup: the constructor flies home");

        ship.Die(null, cleanupOnly: true);
        Assert.IsTrue(constructor.Active, "setup: the constructor outlives the ship's blast");
        RunSimWhile((simTimeout: 5, fatal: true), () => constructor.Active);
    }

    [TestMethod]
    public void AConstructorFliesOnAfterItsLaunchHoweverFarFromItsShip()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunPastLaunch(constructor);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(constructor.AI.OrderQueue.PeekFirst is { Plan: ShipAI.Plan.DeployStructure }, "after its launch it flies on to build, where a fighter turns back to its carrier");

        constructor.Position = ship.Position + new Vector2(ship.SensorRange + 10_000f, 0);
        RunSimWhile((simTimeout: 2, fatal: false));
        Assert.IsTrue(constructor.AI.OrderQueue.PeekFirst is { Plan: ShipAI.Plan.DeployStructure }, "and beyond its ship's sensors it is not called back");
    }

    [TestMethod]
    public void LaunchedOrdersHoldTheUnitsBeforeOrdersWaitingToLaunch()
    {
        TwinShips();
        Derelict a = Forms(Wreck);
        Derelict b = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict waiting = Ordered(a);
        Ship ship = ShipBuilt(waiting);
        float full = ship.Ordinance;
        ship.ChangeOrdnance(-full);
        Parked(waiting);
        waiting.Evaluate();
        AssertEqual("LaunchConstructor", waiting.StepName, "setup: an order that cannot launch yet");
        ship.ChangeOrdnance(full);
        SalvageDerelict launched = Ordered(b);
        Launched(launched);
        AssertEqual("LaunchConstructor", waiting.StepName, "setup: the first order still waits to launch");

        BayOf(ship, launched.FinishedShip).Die(null, cleanupOnly: true);
        AssertEqual(1, SalvageMission.LiveUnits(ship), "setup: the unit that launched the second order's constructor dies");
        launched.Evaluate(); // the goals are evaluated last to first
        waiting.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(launched), "the launched order keeps the unit left counted busy");
        Assert.IsFalse(Player.AI.HasGoal(waiting), "so the one placed before it but waiting to launch ends");
    }

    [TestMethod]
    public void ThePlatformsLossEndsTheOrderAndGivesUpItsClaim()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = WithItsPlatform(d);

        order.Platform.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "a lost platform ends its order");
        Assert.IsNull(d.Claimant, "and its claim");
    }

    [TestMethod]
    public void CallingOffAnOrderWithItsPlatformBuiltGivesUpTheClaimAndLeavesThePlatformToComeApart()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = WithItsPlatform(d);
        Ship platform = order.Platform;

        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsNull(d.Claimant, "the claim goes at once");
        Assert.IsTrue(order.CalledOff && Player.AI.HasGoal(order), "while the order winds down");
        Assert.IsTrue(platform.Active, "with its platform");
    }

    [TestMethod]
    public void AnOrderMidFlightSurvivesSaveAndLoadAndBuildsItsPlatform()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = OrderedAndLaunched(d);
        Ship ship = ShipOf(order);
        Ship constructor = order.FinishedShip;
        RunPastLaunch(constructor);
        RunSimWhile((simTimeout: 1, fatal: false));

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageConstructor", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        var loadedOrder = (SalvageDerelict)player.AI.FindGoal(g => g is SalvageDerelict);
        Assert.IsNotNull(loadedOrder, "the order loads");
        AssertEqual("BuildPlatform", loadedOrder.StepName, "waiting for its platform");
        Ship flying = loadedOrder.FinishedShip;
        Assert.IsTrue(flying is { Active: true } && flying.Id == constructor.Id, "with its constructor in flight");
        Assert.IsTrue(flying.Mothership?.Id == ship.Id, "still its salvage ship's");
        Assert.IsNotNull(BayOf(flying.Mothership, flying), "holding its unit");
        AssertEqual(order.BuildPosition, loadedOrder.BuildPosition, "bound for the same site");
        Assert.IsTrue(loadedOrder.Mission?.SalvageShip?.Id == ship.Id, "its mission and ship with it");

        int frame = 0;
        for (double time = 0; loadedOrder.Platform == null; time += TestSimStepD)
        {
            AssertLessThan(time, 120.0, "the loaded constructor must build its platform");
            loaded.UState.Objects.Update(TestSimStep);
            if (++frame % 60 == 0)
                loadedOrder.Evaluate();
        }
        AssertEqual(player, loadedOrder.Derelict.Claimant, "claiming the derelict");
    }
}

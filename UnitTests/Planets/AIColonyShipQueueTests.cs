using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

[TestClass]
public class AIColonyShipQueueTests : StarDriveTest
{
    [TestMethod]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, false, "empty",       true)]
    [DataRow(GameDifficulty.Brutal, false, QueueItemType.ColonyShip,      false, false, "empty",       true)]
    [DataRow(GameDifficulty.Insane, false, QueueItemType.ColonyShip,      false, false, "empty",       true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShipClaim, false, false, "empty",       true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, true,  "empty",       true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, false, "goalAfter",   true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, false, "held",        false)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShipClaim, false, false, "held",        false)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, false, "goalBefore",  false)]
    [DataRow(GameDifficulty.Normal, false, QueueItemType.ColonyShip,      false, false, "empty",       false)]
    [DataRow(GameDifficulty.Hard,   true,  QueueItemType.ColonyShip,      false, false, "empty",       false)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShipClaim, true,  false, "empty",       false)]
    public void OnHardAndAboveTheAIsEarliestColonyShipForASystemItHoldsNoPlanetInGoesFirstInItsQueue(GameDifficulty difficulty,
        bool atWar, QueueItemType colonyShipType, bool standbyShip, bool outpostQueued, string targetSystem, bool goesFirst)
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.Difficulty = difficulty;
        Enemy.TestInitModifiers();
        if (!atWar)
            Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.AreEqual(atWar, Enemy.IsAtWarWithMajorEmpire, "setup: Enemy's war state");

        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        home.ProdHere = 0;
        Assert.AreEqual(0, home.Construction.Count, "setup: the queue starts empty");
        IShipDesign warship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        IShipDesign colonyShip = ResourceManager.GetShipTemplate("Colony Ship").ShipData;

        // a warship well under way, which the AI's own scoring keeps ahead of the colony ship
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        QueueItem underWay = home.Construction.GetConstructionQueue()[0];
        underWay.Cost = 1_000_000;
        underWay.ProductionSpent = 500_000;

        if (outpostQueued)
            Assert.IsTrue(home.Construction.Enqueue(ResourceManager.CreateBuilding(home, Building.OutpostId)), "setup: an Outpost is queued");

        // the target's system is empty, or held by the AI, or empty with another goal for it made after or before this one
        Planet target = targetSystem == "held" ? AddUnownedPlanetTo(home.System) : AddTargetInAnEmptySystem();
        Assert.AreEqual(targetSystem == "held", target.System.HasPlanetsOwnedBy(Enemy), "setup: whether the AI holds a planet in the target's system");
        var otherGoal = targetSystem is "goalBefore" or "goalAfter"
                      ? new MarkForColonization(Enemy) { TargetPlanet = AddUnownedPlanetTo(target.System) } : null;
        if (targetSystem == "goalBefore")
            Enemy.AI.AddGoal(otherGoal);

        Goal goal = standbyShip ? new StandbyColonyShip(Enemy) : new MarkForColonization(Enemy) { TargetPlanet = target };
        goal.PlanetBuildingAt = home;
        Enemy.AI.AddGoal(goal);
        if (targetSystem == "goalAfter")
            Enemy.AI.AddGoal(otherGoal);
        home.Construction.Enqueue(colonyShip, colonyShipType, goal);
        // many turns from done, so a refit never slots in behind it
        FindItem(home, goal).Cost = 1_000_000;
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);

        var refit = new QueueItem(home)
        {
            isShip   = true,
            ShipData = warship,
            Cost     = 50f,
            QType    = QueueItemType.CombatShip,
            Goal     = new RefitShip(Enemy),
        };
        home.Construction.EnqueueRefitShip(refit);

        var queue = home.Construction.GetConstructionQueue();
        int ahead = outpostQueued ? 1 : 0;
        Assert.AreEqual(4 + ahead, queue.Count);
        if (goesFirst)
        {
            if (outpostQueued)
                Assert.IsTrue(queue[0].isBuilding && queue[0].Building.IsOutpost, "an Outpost goes ahead of the colony ship");
            Assert.AreSame(goal, queue[ahead].Goal, "the colony ship goes ahead of a warship queued before it and one queued after it");
            Assert.AreSame(refit, queue[ahead + 1], "a refit goes right behind the colony ship");
        }
        else
        {
            Assert.AreSame(refit, queue[0], "a refit goes first");
            Assert.AreSame(underWay, queue[1], "the colony ship stays behind the warship the AI's scoring puts ahead of it");
        }
    }

    // an unowned planet in the system, away from its first one
    Planet AddUnownedPlanetTo(SolarSystem system)
    {
        var p = new Planet(UState.CreateId(), system, system.Position + new Vector2(8000), 1f, 1f, 4f);
        system.PlanetList.Add(p);
        return p;
    }

    // a planet in an empty system for a colony ship to aim at
    Planet AddTargetInAnEmptySystem() => AddDummyPlanet(new Vector2(400_000));

    static QueueItem FindItem(Planet planet, Goal goal)
    {
        foreach (QueueItem q in planet.Construction.GetConstructionQueue())
            if (q.Goal == goal)
                return q;
        throw new AssertFailedException("setup: the goal's item is not in the queue");
    }

    [TestMethod]
    public void APortIsTimedForALeadingColonyShipByTheItemsKeptFirstOnly()
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.Difficulty = GameDifficulty.Hard;
        Enemy.TestInitModifiers();
        Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.IsTrue(Enemy.ColonyShipsGoFirst, "setup: Enemy's colony ships go first");

        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        IShipDesign warship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        IShipDesign colonyShip = ResourceManager.GetShipTemplate("Colony Ship").ShipData;
        float cost = colonyShip.GetCost(Enemy);
        int onAnEmptyPort = home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true);
        Assert.IsTrue(onAnEmptyPort < 9999, "setup: the port can build the colony ship");

        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        home.Construction.GetConstructionQueue()[0].Cost = 1_000_000;
        Assert.AreEqual(onAnEmptyPort, home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true),
                        "a leading colony ship does not wait for the warship");
        Assert.IsTrue(home.TurnsUntilQueueComplete(cost, 1f, colonyShip) > onAnEmptyPort, "any other ship waits for the warship");

        var goal = new MarkForColonization(Enemy) { TargetPlanet = AddTargetInAnEmptySystem(), PlanetBuildingAt = home };
        Enemy.AI.AddGoal(goal);
        home.Construction.Enqueue(colonyShip, QueueItemType.ColonyShip, goal);
        QueueItem leading = home.Construction.GetConstructionQueue()[0];
        Assert.AreSame(goal, leading.Goal, "setup: the queued colony ship leads");
        leading.Cost = 1_000_000;
        Assert.IsTrue(home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true) > onAnEmptyPort,
                      "a leading colony ship waits for the colony ship ahead of it");
    }

    [TestMethod]
    [DataRow(null,      false)]
    [DataRow("Outpost", false)]
    [DataRow("Capital", false)]
    [DataRow(null,      true)]
    public void AStarvingPlanetsFoodBuildingGoesAheadOfALeadingColonyShipButNeverAheadOfAnOutpostOrCapital(
        string leadBuilding, bool starvesAfterQueueing)
    {
        Planet home = AddHardAIHomeworldAtPeace(taxRate: 0.5f);
        Assert.IsFalse(home.ProductionBuildingsFirst, "setup: no production building goes first");
        if (!starvesAfterQueueing)
            Starve(home);

        Goal goal = QueueLeadingColonyShipAndAWarship(home, leadBuilding);
        QueueABuildingFarFromDone(home, "Greenhouse");
        var queue = home.Construction.GetConstructionQueue();
        int ahead = leadBuilding != null ? 1 : 0;
        if (starvesAfterQueueing)
        {
            Assert.AreSame(goal, queue[0].Goal, "setup: the Greenhouse waits behind the colony ship while the planet is fed");
            Starve(home);
        }

        GiveStoreAndMoneyForARush(home);
        Invoke(home, "PrioritizeCriticalFoodBuildings");
        if (leadBuilding != null)
        {
            Assert.IsTrue(IsOutpostOrCapital(queue[0]), "the governor halts behind an Outpost or Capital");
            AssertEqual(0.01f, 0f, queue[0].ProductionSpent, "and rushes nothing");
        }
        else
        {
            Assert.IsTrue(queue[0].isBuilding && queue[0].Building.ProducesFood, "the governor puts the Greenhouse first");
            if (starvesAfterQueueing)
                AssertEqual(0.01f, 10f, queue[0].ProductionSpent, "and rushes it");
        }

        QueueAWarshipAndARefit(home, out QueueItem refit);
        Assert.IsTrue(queue[ahead].isBuilding && queue[ahead].Building.ProducesFood, "the Greenhouse stays ahead of the colony ship");
        Assert.AreSame(goal, queue[ahead + 1].Goal, "the colony ship comes next");
        Assert.AreSame(refit, queue[ahead + 2], "a refit goes behind both");
    }

    [TestMethod]
    [DataRow(0.2f, 0f,  false, false, true)]
    [DataRow(0.2f, 0f,  true,  false, true)]
    [DataRow(0.2f, 10f, false, false, false)]
    [DataRow(0.5f, 0f,  false, false, false)]
    [DataRow(0.2f, 10f, false, true,  true)]
    public void AProductionBuildingTheGovernorPutsFirstGoesAheadOfALeadingColonyShip(float taxRate, float netProduction,
        bool outpostQueued, bool incomeDropsAfterQueueing, bool goesAhead)
    {
        Planet home = AddHardAIHomeworldAtPeace(taxRate);
        SetNetProduction(home, netProduction);
        typeof(Planet).GetProperty(nameof(Planet.Level))!.SetValue(home, 5); // more than this test ever queues
        Assert.AreEqual(goesAhead && !incomeDropsAfterQueueing, home.ProductionBuildingsFirst, "setup: whether a production building goes first");
        Assert.IsFalse(home.FoodBuildingsFirst, "setup: the planet is fed");

        Goal goal = QueueLeadingColonyShipAndAWarship(home, outpostQueued ? "Outpost" : null);
        QueueABuildingFarFromDone(home, "Nano Mine");
        var queue = home.Construction.GetConstructionQueue();
        int ahead = outpostQueued ? 1 : 0;
        if (incomeDropsAfterQueueing)
        {
            Assert.AreSame(goal, queue[0].Goal, "setup: the Nano Mine waits behind the colony ship while production is good");
            SetNetProduction(home, 0f);
        }

        GiveStoreAndMoneyForARush(home);
        Invoke(home, "PrioritizeCriticalProductionBuildings");
        if (outpostQueued)
        {
            Assert.IsTrue(IsOutpostOrCapital(queue[0]), "the governor halts behind an Outpost");
            AssertEqual(0.01f, 0f, queue[0].ProductionSpent, "and rushes nothing");
        }
        else if (incomeDropsAfterQueueing)
        {
            Assert.IsTrue(queue[0].isBuilding && queue[0].Building.ProducesProduction, "the governor puts the Nano Mine first");
            AssertEqual(0.01f, 10f, queue[0].ProductionSpent, "and rushes it");
        }

        QueueAWarshipAndARefit(home, out QueueItem refit);
        if (goesAhead)
        {
            Assert.IsTrue(queue[ahead].isBuilding && queue[ahead].Building.ProducesProduction, "the Nano Mine stays ahead of the colony ship");
            Assert.AreSame(goal, queue[ahead + 1].Goal, "the colony ship comes next");
            Assert.AreSame(refit, queue[ahead + 2], "a refit goes behind both");
        }
        else
        {
            Assert.AreSame(goal, queue[ahead].Goal, "the colony ship goes ahead of the Nano Mine");
            Assert.AreSame(refit, queue[ahead + 1], "a refit goes right behind the colony ship");
        }
    }

    Planet AddHardAIHomeworldAtPeace(float taxRate)
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.Difficulty = GameDifficulty.Hard;
        Enemy.TestInitModifiers();
        Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.IsTrue(Enemy.ColonyShipsGoFirst, "setup: Enemy's colony ships go first");
        Enemy.data.TaxRate = taxRate;
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        home.ProdHere = 0;
        return home;
    }

    static void SetNetProduction(Planet home, float netProduction)
    {
        typeof(ColonyResource).GetProperty(nameof(ColonyResource.NetIncome))!.SetValue(home.Prod, netProduction);
    }

    // never about to finish, so a refit never slots in behind it
    static void QueueABuildingFarFromDone(Planet home, string name)
    {
        Assert.IsTrue(home.Construction.Enqueue(ResourceManager.CreateBuilding(home, name)), $"setup: a {name} is queued");
        foreach (QueueItem q in home.Construction.GetConstructionQueue())
            if (q.isBuilding && q.Building.Name == name)
                q.Cost = 1_000_000;
    }

    // the governor's moves rush the item they put first, which needs stored production and the rush fee
    void GiveStoreAndMoneyForARush(Planet home)
    {
        home.ProdHere = 100;
        Assert.IsTrue(home.ProdHere >= 10, "setup: the store holds a rush");
        Enemy.AddMoney(10_000);
    }

    static void Starve(Planet home)
    {
        Assert.IsFalse(home.IsCybernetic, "setup: the planet eats food");
        typeof(Planet).GetField("Unfed", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(home, -1f);
        Assert.IsTrue(home.FoodBuildingsFirst, "setup: the planet starves");
    }

    static bool IsOutpostOrCapital(QueueItem q) => q.isBuilding && q.Building.IsCapitalOrOutpost;

    // queues an Outpost, or a Capital in place of the homeworld's own
    static QueueItem QueueOutpostOrCapital(Planet home, string kind)
    {
        int id = Building.OutpostId;
        if (kind == "Capital")
        {
            Building capital = null;
            foreach (Building b in home.Buildings)
                if (b.IsCapital)
                    capital = b;
            if (capital != null)
                home.DestroyBuilding(capital);
            id = Building.CapitalId;
        }
        Assert.IsTrue(home.Construction.Enqueue(ResourceManager.CreateBuilding(home, id)), $"setup: a {kind} is queued");
        foreach (QueueItem q in home.Construction.GetConstructionQueue())
        {
            if (q.isBuilding && q.Building.BID == id)
            {
                q.Cost = 1_000_000; // so a rush shows on it and never finishes it
                return q;
            }
        }
        throw new AssertFailedException($"setup: the {kind} is not in the queue");
    }

    // a colony ship for a chosen planet, and a warship far from done, behind an Outpost or Capital if asked
    Goal QueueLeadingColonyShipAndAWarship(Planet home, string leadBuilding)
    {
        if (leadBuilding != null)
            QueueOutpostOrCapital(home, leadBuilding);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = AddTargetInAnEmptySystem(), PlanetBuildingAt = home };
        Enemy.AI.AddGoal(goal);
        home.Construction.Enqueue(ResourceManager.GetShipTemplate("Colony Ship").ShipData, QueueItemType.ColonyShip, goal);
        FindItem(home, goal).Cost = 1_000_000;
        QueueAWarship(home);
        return goal;
    }

    static void QueueAWarship(Planet home)
    {
        home.Construction.Enqueue(ResourceManager.GetShipTemplate("Rocket Scout").ShipData, QueueItemType.CombatShip);
        foreach (QueueItem q in home.Construction.GetConstructionQueue())
            if (q.QType == QueueItemType.CombatShip && q.ProductionSpent == 0)
                q.Cost = 1_000_000; // never about to finish, so a refit never slots in behind it
    }

    void QueueAWarshipAndARefit(Planet home, out QueueItem refit)
    {
        QueueAWarship(home);
        refit = new QueueItem(home)
        {
            isShip   = true,
            ShipData = ResourceManager.GetShipTemplate("Rocket Scout").ShipData,
            Cost     = 50f,
            QType    = QueueItemType.CombatShip,
            Goal     = new RefitShip(Enemy),
        };
        home.Construction.EnqueueRefitShip(refit);
    }

    static void Invoke(Planet planet, string method)
    {
        typeof(Planet).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(planet, null);
    }

    [TestMethod]
    [DataRow("Outpost")]
    [DataRow("Capital")]
    public void NothingTheyDoMovesAPlayersItemPastAnOutpostOrCapitalInTheFirstSlot(string kind)
    {
        CreateUniverseAndPlayerEmpire();
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        home.ProdHere = 0;
        QueueItem lead = QueueOutpostOrCapital(home, kind);
        IShipDesign warship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        var queue = home.Construction.GetConstructionQueue();
        Assert.AreSame(lead, queue[0], "setup: the first slot is locked");
        Assert.IsTrue(home.Construction.IsFirstSlotLocked);
        QueueItem second = queue[1], third = queue[2];
        second.Cost = third.Cost = 1_000_000; // never about to finish, so a refit never slots in behind them

        Assert.IsTrue(home.Construction.MoveTo(0, 2), "moved to the top");
        Assert.AreSame(lead, queue[0], "an item goes second");
        Assert.AreSame(third, queue[1]);
        Assert.IsFalse(home.Construction.MoveTo(0, 1), "an item already second goes no higher");
        Assert.IsFalse(home.Construction.MoveTo(2, 0), "the first item cannot be moved down");
        Assert.IsFalse(home.Construction.Swap(0, 1), "nor swapped");
        home.Construction.Reorder(lead, 2);
        Assert.AreSame(lead, queue[0], "nor dragged down");
        home.Construction.Reorder(second, 0);
        Assert.AreSame(lead, queue[0], "nor anything dragged past it");
        Assert.AreSame(second, queue[1], "dragged to the top, an item goes second");

        var refit = new QueueItem(home) { isShip = true, ShipData = warship, Cost = 50f, QType = QueueItemType.CombatShip,
                                          Goal = new RefitShip(Player) };
        home.Construction.EnqueueRefitShip(refit);
        Assert.AreSame(lead, queue[0], "a refit does not jump it");
        Assert.AreSame(refit, queue[1], "and goes second");
    }

    [TestMethod]
    public void ANormalAIsQueueKeepsAnOutpostInTheFirstSlot()
    {
        CreateUniverseAndPlayerEmpire();
        Assert.IsFalse(Enemy.ColonyShipsGoFirst, "setup: Normal does not rank colony ships");
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        QueueItem outpost = QueueOutpostOrCapital(home, "Outpost");
        // a well-advanced warship and a cheap scout, which the AI's own scoring would put first
        home.Construction.Enqueue(ResourceManager.GetShipTemplate("Rocket Scout").ShipData, QueueItemType.CombatShip);
        foreach (QueueItem q in home.Construction.GetConstructionQueue())
            if (q.isShip) { q.Cost = 1_000_000; q.ProductionSpent = 900_000; }
        home.Construction.Enqueue(ResourceManager.GetShipTemplate("Rocket Scout").ShipData, QueueItemType.Scout);
        home.Construction.Enqueue(ResourceManager.GetShipTemplate("Rocket Scout").ShipData, QueueItemType.CombatShip);
        Assert.AreSame(outpost, home.Construction.GetConstructionQueue()[0], "the Outpost stays first through the AI's sorts");
    }

    [TestMethod]
    public void PrioritizedProjectorsLineUpBehindAnOutpostInTheFirstSlot()
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.PrioitizeProjectors = true;
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        QueueItem outpost = QueueOutpostOrCapital(home, "Outpost");
        IShipDesign ship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        home.Construction.Enqueue(ship, QueueItemType.CombatShip);
        home.Construction.Enqueue(ship, QueueItemType.RoadNode, displayName: "first road node");
        home.Construction.Enqueue(ship, QueueItemType.RoadNode, displayName: "second road node");

        var queue = home.Construction.GetConstructionQueue();
        Assert.AreSame(outpost, queue[0], "the Outpost stays first");
        Assert.AreEqual("first road node", queue[1].DisplayName, "the first projector goes right behind it");
        Assert.AreEqual("second road node", queue[2].DisplayName, "and the next one behind the first, so a road completes segment by segment");
    }

    [TestMethod]
    public void ARushedRequisitionRushesItsOwnShipBehindAnOutpostInTheFirstSlot()
    {
        CreateUniverseAndPlayerEmpire();
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        QueueItem outpost = QueueOutpostOrCapital(home, "Outpost");
        IShipDesign ship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        home.Construction.Enqueue(ship, QueueItemType.CombatShip);
        home.Construction.Enqueue(ship, QueueItemType.CombatShip, displayName: "requisitioned");
        home.Construction.MoveToAndContinuousRushFirstItem();

        var queue = home.Construction.GetConstructionQueue();
        Assert.AreSame(outpost, queue[0], "the Outpost stays first");
        Assert.IsFalse(outpost.Rush, "and is not the one rushed");
        Assert.AreEqual("requisitioned", queue[1].DisplayName, "the requisitioned ship goes right behind it");
        Assert.IsTrue(queue[1].Rush, "with continuous rush");
    }
}

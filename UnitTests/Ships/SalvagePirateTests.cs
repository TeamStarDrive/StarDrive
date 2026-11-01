using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Pirates who board a salvage platform end its owner's order at once, the player told only of the boarding, and what
/// the platform held is lost; their own order recovers the derelict's ship from the start, the platform staying at its
/// derelict, and once the ship rises for them the platform takes itself apart. One boarded as it packs up recovers too,
/// whole again. One at a derelict they cannot recover, already recover from another platform, or whose owner's ship
/// rises, is taken apart over two turns, any claim left as it was; one with no order, or boarded by pirates already
/// defeated, is taken away. A boarding applied off the sim thread is taken over on it, and an order that finds its
/// platform boarded first hands it over, at work or packing up. A save keeps their recovery. Pirates who take a platform
/// back under a protection contract take it apart rather than hand it back, leaving the derelict free, and pirates
/// defeated take their platforms with them. A salvage ship their base takes in counts as a freighter, armed or not, never
/// one they spawn, and their base under attack drafts none. A pirate troop ship sent to board a platform keeps the order
/// where another empire's drops it, and a pirate transporter beams onto one; a platform pirates hold no one else boards,
/// by a right-click, a troop ship's order or a planet's boarders.
/// </summary>
[TestClass]
public class SalvagePirateTests : SalvageOrderTest
{
    const string Station = "Cordrazine Small Station Mk1";
    readonly Empire Corsairs;

    public SalvagePirateTests()
    {
        LoadStarterShips(Station, Player.data.DefaultTroopShip);
        CreateAMinorFaction("Corsairs");
        Corsairs = Faction;
        Assert.IsTrue(Corsairs.WeArePirates, "setup: the Corsairs are pirates");
        LoadStarterShips(Corsairs.data.PirateBaseBasic, Corsairs.data.PirateFrigateBasic);
    }

    // the pirates board the platform, which changes hands as the objects update
    void Boarded(Ship platform)
    {
        platform.LoyaltyChangeFromBoarding(Corsairs);
        UState.Objects.UpdateLists();
        AssertEqual(Corsairs, platform.Loyalty, "setup: the platform changes hands");
    }

    // the pirates board the platform, the change of hands applied off the sim thread, as a save or the UI applies it
    void BoardedOffTheSimThread(Ship platform)
    {
        platform.LoyaltyChangeFromBoarding(Corsairs);
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, new Thread(() => { }));
        try
        {
            UState.Objects.UpdateLists();
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        AssertEqual(Corsairs, platform.Loyalty, "setup: the platform changes hands off the sim thread");
    }

    // the order called off, its platform packs up
    static void PacksUp(SalvageDerelict order)
    {
        for (int i = 0; i < 3 && !order.Platform.Dismantling; ++i)
            order.Evaluate();
        Assert.IsTrue(order.Platform.Dismantling, "setup: called off, the platform packs up");
    }

    // a salvage ship of the pirates' with a cannon in place of its smaller cargo hold
    Ship ArmedSalvageShip(Vector2 at)
    {
        IShipDesign from = ResourceManager.Ships.GetDesign("Terran Salvage Ship");
        ShipDesign armed = ((ShipDesign)from).GetClone("Armed Salvage Ship");
        armed.SetDesignSlots(from.GetOrLoadDesignSlots()
                                 .Select(s => s.ModuleUID == "BasicCargoHold" ? new DesignSlot(s.Pos, "HVVulcanCannon", s.Size, 0, ModuleOrientation.Normal, null) : s)
                                 .ToArray(), updateRole: false);
        Assert.IsTrue(armed.IsSalvageShip, "setup: an armed salvage ship");
        Ship ship = Ship.CreateShipAtPoint(UState, Ship.CreateNewShipTemplate(Empire.Void, armed), Corsairs, at);
        AssertGreaterThan(ship.BaseStrength, 0f, "setup: armed");
        return ship;
    }

    // a salvageable derelict of the player's station, which can only be salvaged
    Derelict StationForms(Vector2 at)
    {
        TestShip ship = SpawnShip(Station, Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = UState.Derelicts.Current.Last();
        Assert.IsFalse(DerelictRecovery.CanRecover(d), "setup: a station's, which cannot be recovered");
        return d;
    }

    // a troop ship of the empire's with its troop aboard
    Ship TroopShip(Empire owner, Vector2 at)
    {
        Ship ship = SpawnShip(Player.data.DefaultTroopShip, owner, at);
        ResourceManager.CreateTroop("Wyvern", owner).LandOnShip(ship);
        return ship;
    }

    static bool Boards(Ship ship, Ship target) => ship.AI.FindGoal(ShipAI.Plan.BoardShip, out _) && ship.AI.EscortTarget == target;

    // the empire's luck seed set to the first whose recovery draw for the derelict succeeds
    static void Succeeds(Derelict d, Empire empire)
    {
        PropertyInfo seed = typeof(Empire).GetProperty(nameof(Empire.LuckSeed))!;
        for (int s = 1; s < 1_000_000; ++s)
        {
            seed.SetValue(empire, s);
            if (DerelictRecovery.Draws(empire, d).Recovery < 0.01f)
                return;
        }
        Assert.Fail("setup: no seed draws a success");
    }

    [TestMethod]
    public void PiratesWhoBoardAPlatformEndItsOwnersOrderAndRecoverItsDerelictForThemselves()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        order.Evaluate();
        AssertGreaterThan(platform.GetProduction(), 0f, "setup: the platform holds what it stripped");
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        Boarded(platform);
        Assert.IsFalse(Player.AI.HasGoal(order), "its owner's order ends at once");
        AssertEqual(1, notes.NumberOfNotifications, "the player told only of the boarding");
        AssertEqual(0f, platform.GetProduction(), "what it held is lost");
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        Assert.IsTrue(theirs is { Recovering: true, Finished: false, BreakInDone: 0, RecoveryWork: 0f } && theirs.Owner == Corsairs,
                      "the pirates' own order recovers the derelict from the start");
        AssertEqual(Corsairs, d.Claimant, "which is theirs");
        Assert.IsTrue(platform.AI.State != AIState.Flee && !platform.AI.FindGoal(ShipAI.Plan.Orbit, out _), "the platform stays at its derelict");

        Succeeds(d, Corsairs);
        for (int i = 0; i < 30 && !theirs.RecoverySucceeded; ++i)
            theirs.Evaluate();
        Assert.IsTrue(theirs.RecoverySucceeded, "their recovery succeeds");
        UState.Derelicts.Update(new FixedSimTime(Derelict.RiseSeconds));
        AssertEqual(Corsairs, d.RecoveredAs?.Loyalty, "and the ship rises for them");

        theirs.Evaluate();
        Assert.IsTrue(platform.Dismantling, "then the platform takes itself apart");
        theirs.Evaluate();
        theirs.Evaluate();
        Assert.IsFalse(Corsairs.AI.HasGoal(theirs) || platform.Active, "and is gone, their order with it");
    }

    [TestMethod]
    public void APlatformBoardedAtADerelictTheyCannotRecoverIsTakenApartOverTwoTurns()
    {
        Derelict d = StationForms(Wreck);
        Ship platform = AtWork(d).Platform;
        Boarded(platform);
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        Assert.IsNotNull(theirs, "setup: the pirates' order");
        Assert.IsNull(d.Claimant, "they claim no derelict they cannot recover");

        theirs.Evaluate();
        Assert.IsTrue(platform.Dismantling && platform.Active, "the platform takes itself apart");
        theirs.Evaluate();
        Assert.IsTrue(platform.Active, "over two turns");
        theirs.Evaluate();
        Assert.IsFalse(platform.Active, "then it is gone");
    }

    [TestMethod]
    public void APlatformWithNoOrderThatPiratesBoardIsTakenAway()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, Wreck);
        Boarded(platform);
        Assert.IsFalse(platform.Active, "a salvage platform with no order to take over is taken away");
        Assert.IsNull(SalvageDerelict.OfPlatform(platform), "and no order of theirs works it");
    }

    [TestMethod]
    public void ABoardingAppliedOffTheSimThreadIsTakenOverOnIt()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        BoardedOffTheSimThread(platform);
        Assert.IsTrue(Player.AI.HasGoal(order) && SalvageDerelict.OfPlatform(platform) == null, "its owner's order waits for the sim thread");

        Universe.InvokePendingSimThreadActions();
        Assert.IsFalse(Player.AI.HasGoal(order), "where it ends");
        Assert.IsTrue(SalvageDerelict.OfPlatform(platform) is { Recovering: true, CalledOff: false } && d.Claimant == Corsairs, "and the pirates' recovery begins");
    }

    [TestMethod]
    public void ASaveKeepsThePiratesRecovery()
    {
        Derelict d = Forms(Wreck);
        Ship platform = AtWork(d).Platform;
        Boarded(platform);
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        theirs.Evaluate();
        AssertEqual(1, theirs.BreakInDone, "setup: a turn of their break-in done");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvagePirates", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Empire pirates = loaded.UState.Empires.First(e => e.WeArePirates);
        SalvageDerelict kept = pirates.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(kept is { Recovering: true, BreakInDone: 1, Mission: null } && kept.Platform?.Loyalty == pirates,
                      "a save keeps the pirates' recovery, with its platform and no salvage ship");
        AssertEqual(pirates, kept.Derelict?.Claimant, "and their claim");
        kept.Evaluate();
        AssertEqual(2, kept.BreakInDone, "which goes on after the load");
    }

    [TestMethod]
    public void PiratesWhoTakeAPlatformBackUnderAProtectionContractTakeItApartRatherThanHandItBack()
    {
        Derelict d = Forms(Wreck);
        Ship platform = AtWork(d).Platform;
        Boarded(platform);
        var protection = new PirateProtection(Corsairs, Player, platform);
        Corsairs.AI.AddGoal(protection);
        protection.AdvanceToNextStep();
        protection.AdvanceToNextStep();
        AssertEqual("ReturnTargetToOriginalOwner", protection.StepName, "setup: they hold the platform, to hand it back");

        protection.Evaluate();
        UState.Objects.UpdateLists();
        AssertEqual(Corsairs, platform.Loyalty, "the platform is not handed back");
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        Assert.IsTrue(theirs is { CalledOff: true }, "its recovery given up");
        Assert.IsNull(d.Claimant, "and the derelict left free");
        theirs.Evaluate();
        Assert.IsTrue(platform.Dismantling, "so it takes itself apart");
    }

    [TestMethod]
    public void PiratesDefeatedTakeTheirPlatformsWithThem()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Derelict d = Forms(Wreck);
        Ship platform = AtWork(d).Platform;
        Boarded(platform);
        AssertEqual(Corsairs, d.Claimant, "setup: the pirates recover the derelict");
        Corsairs.Pirates.Init();
        Corsairs.Pirates.SetLevel(1); // as a new game sets them up

        Corsairs.Pirates.LevelDown();
        Assert.IsTrue(Corsairs.IsDefeated, "setup: losing it defeats them");
        Assert.IsFalse(platform.Active, "their platform goes with them");
        Assert.IsNull(d.Claimant, "and their claim");
    }

    [TestMethod]
    public void ASalvageShipTheirBaseTakesInCountsAsAFreighterNeverOneTheySpawn()
    {
        Ship ship = ArmedSalvageShip(Wreck);
        Corsairs.Pirates.SetLevel(Ship_Game.Pirates.MaxLevel); // no level to roll for
        Assert.IsFalse(Corsairs.Pirates.ShipsWeCanSpawn.Contains(ship.Name), "setup: not a design they spawn");

        Corsairs.Pirates.TakeInLandedShip(ship);
        Assert.IsFalse(Corsairs.Pirates.ShipsWeCanSpawn.Contains(ship.Name), "their base takes it in as a freighter, never to spawn its like");
        Assert.IsFalse(ship.Active, "and it is gone");
    }

    [TestMethod]
    public void APirateTroopShipSentToBoardAPlatformKeepsTheOrderWhereAnothersDropsIt()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, Wreck);
        Ship pirate = TroopShip(Corsairs, Wreck + new Vector2(0, 8_000f));
        Ship rival = TroopShip(Enemy, Wreck + new Vector2(0, -8_000f));
        pirate.AI.OrderTroopToBoardShip(platform);
        rival.AI.OrderTroopToBoardShip(platform);

        RunObjectsSim(1f);
        Assert.IsTrue(Boards(pirate, platform), "a pirate troop ship keeps its order to board a salvage platform");
        Assert.IsFalse(rival.AI.FindGoal(ShipAI.Plan.BoardShip, out _), "where another empire's drops it");
    }

    [TestMethod]
    public void APirateTransporterBeamsItsTroopOntoAPlatform()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        Ship scout = SpawnShip("Vulcan Scout", Player, platform.Position + new Vector2(0, 300f));
        Ship boarder = SpawnShip("Vulcan Scout", Corsairs, platform.Position + new Vector2(0, -100f));
        ResourceManager.CreateTroop("Wyvern", Corsairs).LandOnShip(boarder);
        boarder.AI.NearByShips = new() { new(platform), new(scout) };

        MethodInfo beam = typeof(ShipAI).GetMethod("DoAssaultTransporterLogic", BindingFlags.Instance | BindingFlags.NonPublic);
        beam.Invoke(boarder.AI, new object[] { boarder.Modules[0] });
        Assert.IsTrue(platform.TroopsAreBoardingShip, "a pirate transporter beams its troop onto the platform, the nearest ship");
        Assert.IsFalse(scout.TroopsAreBoardingShip, "not onto the next one");
    }

    [TestMethod]
    public void ThePlayerCannotBoardAPlatformPiratesHold()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Corsairs, Wreck);
        Ship troopShip = TroopShip(Player, Wreck + new Vector2(0, 8_000f));
        var commands = new Ship_Game.Universe.ShipMoveCommands(Universe);
        Assert.IsFalse(commands.AttackSpecificShip(troopShip, platform), "a right-click with a troop ship on the pirates' platform is refused");
        Assert.IsFalse(commands.RightClickOnShip(troopShip, platform), "and for a troop ship of a group too");

        troopShip.AI.OrderTroopToBoardShip(platform);
        RunObjectsSim(1f);
        Assert.IsFalse(troopShip.AI.FindGoal(ShipAI.Plan.BoardShip, out _), "a troop ship sent to board it drops the order");
    }

    [TestMethod]
    public void APlanetSendsNoBoardersAtAPlatformPiratesHold()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        var troops = new Troop[3];
        for (int i = 0; i < troops.Length; ++i)
        {
            troops[i] = ResourceManager.CreateTroop("Wyvern", Player);
            Assert.IsTrue(troops[i].TryLandTroop(Homeworld), "setup: the planet must hold troops");
            troops[i].UpdateMoveActions(troops[i].MaxStoredActions);
        }
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Corsairs, Homeworld.Position + new Vector2(Homeworld.Radius + 500f, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Corsairs, Homeworld.Position + new Vector2(0, Homeworld.Radius + 5000f));
        inPlay.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(Homeworld.System.ShipList.Contains(platform) && Homeworld.System.ShipList.Contains(inPlay), "setup: both in the planet's system");

        new AssaultBombers(Homeworld, Player, Corsairs).Evaluate();
        Ship[] boarders = troops.Select(t => t.HostShip).Where(s => s != null).ToArray();
        Assert.IsTrue(boarders.Length > 0, "setup: the planet must send boarders");
        foreach (Ship boarder in boarders)
            Assert.AreNotSame(platform, boarder.AI.EscortTarget, "no boarders go after the pirates' closer platform");
        Assert.IsTrue(boarders.Any(b => b.AI.EscortTarget == inPlay), "the boarders go after their ship beside it");
    }

    [TestMethod]
    public void AnOrderThatFindsItsPlatformBoardedHandsItOverToThePirates()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        BoardedOffTheSimThread(platform);

        order.Evaluate(); // its turn comes before the takeover queued for the sim thread
        Assert.IsFalse(Player.AI.HasGoal(order), "an order that finds its platform boarded ends");
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        Assert.IsTrue(theirs is { Recovering: true, CalledOff: false } && d.Claimant == Corsairs, "handing the platform over to the pirates' recovery");
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(platform.Active && SalvageDerelict.OfPlatform(platform) == theirs, "which the queued takeover leaves as it is");
    }

    [TestMethod]
    public void AnOrderPackingUpThatFindsItsPlatformBoardedHandsItOverToThePirates()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        SalvageDerelict.Cancel(Player, d);
        PacksUp(order);
        BoardedOffTheSimThread(platform);

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "an order packing up that finds its platform boarded ends");
        Assert.IsTrue(SalvageDerelict.OfPlatform(platform) is { Recovering: true, CalledOff: false } && !platform.Dismantling,
                      "handing the platform over whole to the pirates' recovery");
    }

    [TestMethod]
    public void APlatformBoardedAsItPacksUpRecoversItsDerelictForThePirates()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        SalvageDerelict.Cancel(Player, d);
        PacksUp(order);

        Boarded(platform);
        Assert.IsTrue(SalvageDerelict.OfPlatform(platform) is { Recovering: true, CalledOff: false } && d.Claimant == Corsairs,
                      "a platform boarded as it packs up recovers its derelict for the pirates");
        Assert.IsFalse(platform.Dismantling, "whole again");
    }

    [TestMethod]
    public void APlatformBoardedAsItsOwnersShipRisesIsTakenApartTheShipRisingForItsOwner()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, recover: true);
        Ship platform = order.Platform;
        Succeeds(d, Player);
        for (int i = 0; i < 30 && !order.RecoverySucceeded; ++i)
            order.Evaluate();
        Assert.IsTrue(order.RecoverySucceeded && d.Rising, "setup: the owner's ship rises");

        Boarded(platform);
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        Assert.IsTrue(theirs is { CalledOff: true } && d.Claimant != Corsairs, "the pirates have nothing to recover");
        UState.Derelicts.Update(new FixedSimTime(Derelict.RiseSeconds));
        AssertEqual(Player, d.RecoveredAs?.Loyalty, "the ship still rises for its owner");
        theirs.Evaluate();
        Assert.IsTrue(platform.Dismantling, "while the platform takes itself apart");
    }

    [TestMethod]
    public void APlatformBoardedAtADerelictThePiratesRecoverFromAnotherIsTakenApartTheirClaimKept()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        SalvageDerelict first = AtWork(d);
        Ship packingUp = first.Platform;
        SalvageDerelict.Cancel(Player, d);
        PacksUp(first);
        SalvageDerelict second = Ordered(d);
        Launched(second);
        Ship platform = PlatformBuilt(second);
        Assert.IsTrue(packingUp.Active && platform.Active, "setup: a second platform stands at the derelict while the first packs up");

        Boarded(platform);
        SalvageDerelict theirs = SalvageDerelict.OfPlatform(platform);
        AssertEqual(Corsairs, d.Claimant, "setup: the pirates recover the derelict from the second");
        Boarded(packingUp);
        SalvageDerelict other = SalvageDerelict.OfPlatform(packingUp);
        Assert.IsTrue(other is { CalledOff: true }, "the first, boarded too, gets no second recovery");
        AssertEqual(theirs, SalvageDerelict.OrderOf(Corsairs, d), "their one recovery stands");
        AssertEqual(Corsairs, d.Claimant, "with their claim");
        other.Evaluate();
        Assert.IsTrue(packingUp.Dismantling, "so the first takes itself apart");
    }

    [TestMethod]
    public void APlatformBoardedByPiratesAlreadyDefeatedIsTakenAway()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        Corsairs.Pirates.Init();
        Corsairs.Pirates.SetLevel(1);
        Corsairs.Pirates.LevelDown();
        Assert.IsTrue(Corsairs.IsDefeated, "setup: the pirates are defeated, troops of theirs still aboard");

        Boarded(platform);
        Assert.IsFalse(Player.AI.HasGoal(order), "its owner's order ends");
        Assert.IsFalse(platform.Active, "and the platform is taken away, as defeated pirates run no orders");
        Assert.IsNull(d.Claimant, "leaving the derelict free");
    }

    [TestMethod]
    public void APirateBaseUnderAttackDraftsNoSalvageShip()
    {
        Ship pirateBase = SpawnShip(Corsairs.data.PirateBaseBasic, Corsairs, Wreck + new Vector2(-80_000f, 0));
        Ship salvageShip = ArmedSalvageShip(pirateBase.Position + new Vector2(0, 200_000f));
        Corsairs.GetRelations(Enemy).AtWar = Enemy.GetRelations(Corsairs).AtWar = true;
        SpawnShipNoCombatHoldPos("Dreadnought mk1-a", Enemy, pirateBase.Position + new Vector2(3_000f, 0));
        UState.Objects.Update(new(time: 2f));
        AssertGreaterThan(pirateBase.AI.PotentialTargets.Sum(s => s.BaseStrength), pirateBase.AI.FriendliesNearby.Sum(s => s.BaseStrength),
                          "setup: the base is outgunned");
        pirateBase.InCombat = true;

        new PirateDefendBase(Corsairs, pirateBase).Evaluate();
        Assert.AreNotEqual(pirateBase, salvageShip.AI.EscortTarget, "an armed salvage ship they took is not drafted to escort the base");

        Ship frigate = SpawnShip(Corsairs.data.PirateFrigateBasic, Corsairs, pirateBase.Position + new Vector2(0, -200_000f));
        UState.Objects.UpdateLists();
        new PirateDefendBase(Corsairs, pirateBase).Evaluate();
        AssertEqual(pirateBase, frigate.AI.EscortTarget, "setup: a warship of theirs is");
    }
}

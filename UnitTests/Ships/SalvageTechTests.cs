using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Stage = Ship_Game.Commands.Goals.SalvageDerelict.Stage;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvageable derelict holds a few techs, picked at random at its death from those that unlock its design's modules
/// and its hull, root techs left out: 2 for a frigate, 3 for a destroyer, 4 for a cruiser, 5 for a battleship, 6 for a
/// capital and 3 for an untethered station; a Remnant ship holds none. Each load its platform sends to the salvage ship
/// advances each of those techs the empire has not researched and sees on its tree by a fifth of what the tech has left
/// to research, times the load's share of the derelict's production, so the second of two loads pays a fifth of what
/// is left by then, and salvage never completes a tech. A load the ship takes only part of pays for that part, and the
/// empire's module health bonus leaves the payout alone. The order keeps the points each load gave each tech. An
/// empire sees a wreck's techs once its salvage platform stands at it, and from then on even after the platform is
/// lost; until then the count is all the wreck holds, and after it only those the empire can learn. A salvage shows as
/// Waiting for a salvage ship, Salvage ship ordered, Salvage ship on its way, Platform ordered, Building platform, then
/// Salvaging, and another empire's as Salvaging.
/// </summary>
[TestClass]
public class SalvageTechTests : SalvageOrderTest
{
    Derelict Forms(Empire owner, Vector2 at, Empire killer = null)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, killer), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    // a tech of the derelict's, unresearched and on the player's tree, with this much of its cost researched
    TechEntry Learnable(string uid, float researched)
    {
        TechEntry tech = Player.GetTechEntry(uid);
        tech.Unlocked = false;
        tech.Discovered = true;
        tech.Progress = researched * tech.TechCost;
        Assert.IsFalse(tech.IsHidden(Player), $"setup: {uid} is not hidden from the player");
        return tech;
    }

    // two turns fill a hold of 24 and the third sends it to the salvage ship
    static void SendsALoadOf24(SalvageDerelict order)
    {
        Ship ship = MissionOf(order).SalvageShip;
        float before = ship.GetProduction();
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, before + 24f, ship.GetProduction(), "setup: a load of 24 reaches the salvage ship");
    }

    [TestMethod]
    public void AWreckHoldsAsManyTechsAsItsHullRoleAtRandomFromThoseOfItsModulesAndHull()
    {
        AssertEqual(2, Derelict.TechsHeld(RoleName.frigate), "a frigate's wreck holds 2 techs");
        AssertEqual(3, Derelict.TechsHeld(RoleName.destroyer), "a destroyer's 3");
        AssertEqual(4, Derelict.TechsHeld(RoleName.cruiser), "a cruiser's 4");
        AssertEqual(5, Derelict.TechsHeld(RoleName.battleship), "a battleship's 5");
        AssertEqual(6, Derelict.TechsHeld(RoleName.capital), "a capital's 6");
        AssertEqual(3, Derelict.TechsHeld(RoleName.station), "an untethered station's 3");

        Array<string> techs = Derelict.TechsOf(ResourceManager.Ships.GetDesign("Dreadnought mk1-a"));
        AssertEqual(17, techs.Count, "the Dreadnought's modules and hull come from 17 techs past the roots");
        Assert.IsTrue(techs.Contains("Titans"), "its hull's tech among them");
        Assert.IsTrue(techs.Contains("CrystalArmor"), "and its armour's");
        Assert.IsFalse(techs.Contains("StarshipConstruction") || techs.Contains("SpaceWeapons"), "but no root tech, though two unlock its modules");

        var picks = new Array<string>();
        for (int i = 0; i < 5; ++i)
        {
            Derelict d = Forms(Player, Wreck + new Vector2(0, i * 5_000f));
            AssertEqual(6, d.Techs.Length, "a capital's wreck holds 6 of them");
            AssertEqual(6, d.Techs.Distinct().ToArray().Length, "each once");
            Assert.IsTrue(d.Techs.All(t => techs.Contains(t)), "all among its design's");
            picks.Add(string.Join(",", d.Techs.OrderBy(t => t)));
        }
        AssertGreaterThan(picks.Distinct().ToArray().Length, 1, "picked at random, so five wrecks do not all hold the same");
    }

    [TestMethod]
    public void ARemnantWreckHoldsNoTechs()
    {
        CreateAMinorFaction("Remnant");
        Assert.IsTrue(Faction.WeAreRemnants, "setup: the faction is the Remnants");
        AssertEqual(0, Forms(Faction, Wreck).Techs.Length, "a Remnant ship's wreck holds no tech");
    }

    [TestMethod]
    public void EachLoadAdvancesEachTechTheEmpireCanLearnByAFifthOfWhatIsLeftTimesTheLoadsShareOfTheWreck()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry fresh = Learnable(d.Techs[0], 0f);
        TechEntry begun = Learnable(d.Techs[1], 0.4f);
        TechEntry researched = Learnable(d.Techs[2], 0f);
        researched.Unlocked = true;
        TechEntry undiscovered = Learnable(d.Techs[3], 0f);
        undiscovered.Discovered = false;
        float share = 24f / d.Production;

        order.Evaluate();
        order.Evaluate();
        AssertEqual(0f, fresh.Progress, "setup: stripping pays nothing until a load reaches the salvage ship");
        order.Evaluate();
        AssertEqual(0.01f, 0.2f * share * fresh.TechCost, fresh.Progress, "a fifth of a fresh tech's cost, times the load's share of the wreck");
        AssertEqual(0.01f, (0.4f + 0.2f * share * 0.6f) * begun.TechCost, begun.Progress, "a fifth of what a begun one has left");
        AssertEqual(0f, researched.Progress, "nothing to a tech already researched");
        AssertEqual(0f, undiscovered.Progress, "nor to one not yet on the empire's tree");

        fresh.Progress = 0.6f * fresh.TechCost; // its research went on meanwhile
        SendsALoadOf24(order);
        AssertEqual(0.01f, (0.6f + 0.2f * share * 0.4f) * fresh.TechCost, fresh.Progress, "the next load pays a fifth of what is left by then");
    }

    [TestMethod]
    public void SalvageNeverCompletesATech()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0.9f);

        SendsALoadOf24(order);
        AssertEqual(0.01f, (0.9f + 0.2f * 24f / d.Production * 0.1f) * tech.TechCost, tech.Progress, "a tech nearly done gains a fifth of the little it has left");
        AssertLessThan(tech.Progress, tech.TechCost, "so it is never complete");
        Assert.IsTrue(tech.Locked, "and stays unresearched");
    }

    [TestMethod]
    public void APartialLoadPaysOnlyForWhatTheShipTook()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = MissionOf(order).SalvageShip;
        ship.LoadProduction(0f); // the hold takes its limit from the cargo room of the moment
        ship.CargoSpaceMax = 10f;
        TechEntry tech = Learnable(d.Techs[0], 0f);

        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 10f, ship.GetProduction(), "setup: the ship takes 10 of the load of 24");
        AssertEqual(0.01f, 14f, order.Platform.GetProduction(), "setup: the rest stays aboard");
        AssertEqual(0.01f, 0.2f * 10f / d.Production * tech.TechCost, tech.Progress, "the tech is paid for the 10 the ship took");
    }

    [TestMethod]
    public void AnEmpiresModuleHealthBonusLeavesThePayoutAlone()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0f);
        Player.data.Traits.ModHpModifier = 2.1f; // Skilled Engineers with the module health techs researched

        SendsALoadOf24(order);
        AssertEqual(0.01f, 0.2f * 24f / d.Production * tech.TechCost, tech.Progress, "every empire is paid a flat fifth");
    }

    [TestMethod]
    public void ATechHiddenFromTheEmpiresRaceIsNeverAdvanced()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0f);
        Array<Technology.RaceRequirements> hiddenFrom = tech.Tech.HiddenFrom;
        hiddenFrom.Add(new() { ShipType = Player.data.Traits.ShipType });
        try
        {
            Assert.IsTrue(tech.IsHidden(Player), "setup: the tech is hidden from the player's race");
            SendsALoadOf24(order);
            AssertEqual(0f, tech.Progress, "a tech hidden from the empire's race gains nothing");
        }
        finally
        {
            hiddenFrom.RemoveAt(hiddenFrom.Count - 1);
        }
    }

    [TestMethod]
    public void AnOrderKeepsThePointsEachLoadGaveEachTech()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry fresh = Learnable(d.Techs[0], 0f);
        TechEntry begun = Learnable(d.Techs[1], 0.4f);
        TechEntry researched = Learnable(d.Techs[2], 0f);
        researched.Unlocked = true;
        AssertEqual(6, order.Gained.Length, "a count for each of the wreck's techs");

        SendsALoadOf24(order);
        AssertGreaterThan(fresh.Progress, 0f, "setup: the load paid the fresh tech");
        AssertEqual(0.01f, fresh.Progress, order.Gained[0], "the order keeps what the load gave the fresh tech");
        AssertEqual(0.01f, begun.Progress - 0.4f * begun.TechCost, order.Gained[1], "and the begun one");
        AssertEqual(0f, order.Gained[2], "and nothing for one already researched");

        SendsALoadOf24(order);
        AssertEqual(0.01f, fresh.Progress, order.Gained[0], "the points add up load after load");
        AssertEqual(0.01f, fresh.Progress, DerelictTechIcons.GainedOf(order, fresh), "as the panel reads them");
    }

    [TestMethod]
    public void AnEmpireSeesAWrecksTechsOnceItsPlatformStandsAtItAndAfterThePlatformIsLost()
    {
        Derelict d = Forms(Player, Wreck, killer: Enemy);
        Assert.IsTrue(d.IsKnownBy(Enemy), "setup: the empire that destroyed the ship knows of the wreck too");
        Assert.IsFalse(SalvageDerelict.SeesTechs(Player, d), "with no platform at the wreck, its techs are hidden");
        SalvageDerelict order = Ordered(d);
        ShipBuilt(order);
        Launched(order);
        Assert.IsFalse(SalvageDerelict.SeesTechs(Player, d), "as they are while the salvage ship is ordered or on its way, and the constructor builds");

        PlatformBuilt(order);
        Assert.IsTrue(SalvageDerelict.SeesTechs(Player, d), "a standing platform shows them");
        Assert.IsFalse(d.ShowsTechsTo(Enemy), "to its empire only");

        order.Platform.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the platform is lost, and its order with it");
        Assert.IsTrue(SalvageDerelict.SeesTechs(Player, d), "the techs stay shown");
        Derelict later = Forms(Player, Wreck + new Vector2(0, 5_000f));
        Assert.IsFalse(SalvageDerelict.SeesTechs(Player, later), "but a wreck no platform stood at hides its own");
    }

    [TestMethod]
    public void TheCountIsAllTheWreckHoldsUntilTheEmpireSeesItsTechsThenOnlyThoseItCanLearn()
    {
        Derelict d = Forms(Player, Wreck);
        for (int i = 0; i < d.Techs.Length; ++i)
            Learnable(d.Techs[i], 0f);
        Player.GetTechEntry(d.Techs[0]).Unlocked = true;
        Player.GetTechEntry(d.Techs[1]).Discovered = false;

        var learnable = new Array<TechEntry>();
        DerelictTechIcons.Learnable(Player, d, learnable);
        CollectionAssert.AreEqual(new[] { d.Techs[2], d.Techs[3], d.Techs[4], d.Techs[5] }, learnable.Select(t => t.UID).ToArray(),
                                  "the techs the empire can learn, in the wreck's order");
        AssertEqual(6, DerelictTechIcons.Count(seen: false, d, learnable), "all 6 the wreck holds while its techs are hidden");
        AssertEqual(4, DerelictTechIcons.Count(seen: true, d, learnable), "then only the 4 the empire can learn");
    }

    [TestMethod]
    public void ASalvageGoesFromItsShipToItsPlatformToSalvagingAndAnotherEmpiresShowsAsSalvaging()
    {
        Derelict d = Forms(Player, Wreck);
        AssertEqual(Stage.None, SalvageDerelict.StageOf(Player, d, out _, out _), "no salvage yet");

        var waiting = new SalvageDerelict(Player, d);
        Player.AI.AddGoal(waiting); // as one that has found no salvage ship yet
        AssertEqual(Stage.WaitingForShip, SalvageDerelict.StageOf(Player, d, out _, out _), "ordered, before it finds a salvage ship");
        Player.AI.RemoveGoal(waiting);

        SalvageDerelict order = Ordered(d);
        AssertEqual(Stage.ShipOrdered, SalvageDerelict.StageOf(Player, d, out _, out _), "while its salvage ship is queued");
        Ship ship = ShipBuilt(order, at: Homeworld.Position + new Vector2(2_000f, 0));
        MissionOf(order).Evaluate();
        AssertEqual(Stage.ShipOnItsWay, SalvageDerelict.StageOf(Player, d, out _, out _), "while the ship flies out");
        ship.Position = MissionOf(order).Site;
        ship.AI.ClearOrders(); // its move there over
        Ship constructor = Launched(order);
        Assert.IsNotNull(constructor, "setup: parked at its site, the ship launches the constructor");
        AssertEqual(Stage.BuildingPlatform, SalvageDerelict.StageOf(Player, d, out _, out _), "while the constructor flies out and builds");
        PlatformBuilt(order);
        AssertEqual(Stage.Salvaging, SalvageDerelict.StageOf(Player, d, out _, out _), "once the platform stands");

        Derelict theirs = Forms(Player, Wreck + new Vector2(0, 5_000f));
        Assert.IsTrue(theirs.TryClaim(Enemy), "setup: another empire salvages a second wreck");
        AssertEqual(Stage.Salvaging, SalvageDerelict.StageOf(Player, theirs, out Empire claimant, out _), "which shows as Salvaging");
        AssertEqual(Enemy, claimant, "in the colour of the empire salvaging it");
    }

    [TestMethod]
    public void AnOrderWhoseParkedShipHasNoOrdnanceToLaunchReadsPlatformOrdered()
    {
        Derelict d = Forms(Player, Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        ship.ChangeOrdnance(-ship.Ordinance);
        Parked(order);
        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "setup: with no ordnance its unit launches nothing");
        AssertEqual(Stage.PlatformOrdered, SalvageDerelict.StageOf(Player, d, out _, out _), "so the platform reads ordered");
    }

    [TestMethod]
    public void PointsShowWholeBelowTenThousandThenInThousandsWithOneDecimalThenWholeThousands()
    {
        AssertEqual("7", DerelictTechIcons.Points(7.9f), "whole points, cut short");
        AssertEqual("990", DerelictTechIcons.Points(990f), "under a thousand");
        AssertEqual("9999", DerelictTechIcons.Points(9999f), "up to 9999");
        AssertEqual("12.5k", DerelictTechIcons.Points(12_500f), "then thousands with one decimal");
        AssertEqual("99.9k", DerelictTechIcons.Points(99_940f), "up to 99.9k");
        AssertEqual("100k", DerelictTechIcons.Points(99_960f), "then whole thousands, so six fit side by side, from where one decimal would round to 100");
        AssertEqual("250k", DerelictTechIcons.Points(250_400f), "however many");
    }
}

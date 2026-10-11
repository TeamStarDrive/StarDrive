using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
#pragma warning disable CA2213

namespace UnitTests.ExoticSystems
{
    [TestClass]
    public class StationDesignPicks : StarDriveTest
    {
        public StationDesignPicks()
        {
            CreateUniverseAndPlayerEmpire();
            ResearchDebugUnlocks.UnlockAllResearch(Player, unlockBonuses: false);
            Player.AutoPickBestResearchStation = false;
            Player.AutoPickBestMiningStation = false;
            Assert.IsTrue(Player.CanBuildResearchStations && Player.CanBuildMiningStations, "test needs station tech");
        }

        [TestMethod]
        public void AnUnsetDropdownResolvesToTheRacialDefault()
        {
            Player.data.CurrentMiningStation = "";
            Player.data.CurrentResearchStation = "";

            AssertEqual(Player.data.DefaultMiningStation, Player.data.MiningStation);
            AssertEqual(Player.data.DefaultResearchStation, Player.data.ResearchStation);
            Assert.IsNotNull(ShipBuilder.StationDesignOrNull(Player.data.MiningStation), "the default mining station must be a real design");
            Assert.IsNotNull(ShipBuilder.StationDesignOrNull(Player.data.ResearchStation), "the default research station must be a real design");
        }

        [TestMethod]
        public void ANameThatNoLongerResolvesIsNullNotAThrow()
        {
            Assert.IsNull(ShipBuilder.StationDesignOrNull("No Such Station Design"));
            Assert.IsNull(ShipBuilder.StationDesignOrNull(""));
            Assert.IsNull(ShipBuilder.StationDesignOrNull(null));

            Player.data.CurrentResearchStation = "No Such Station Design";
            AssertEqual("No Such Station Design", Player.data.ResearchStation, "the property passes a set name through unchanged");
            Assert.IsNull(ShipBuilder.StationDesignOrNull(Player.data.ResearchStation), "so the goal sees nothing to build, and no exception");
        }

        [TestMethod]
        public void AResearchStationGoalWithAStaleNameClearsItAndRetries()
        {
            AddHomeWorldToEmpire(new SDGraphics.Vector2(400000), Player);
            Player.UpdateRallyPoints();
            Planet planet = AddDummyPlanet(new SDGraphics.Vector2(10), 0, 0, 0);
            Player.data.CurrentResearchStation = "No Such Station Design";

            var goal = new ProcessResearchStation(Player, planet);
            Player.AI.AddGoalAndEvaluate(goal);

            AssertEqual("", Player.data.CurrentResearchStation, "the stale choice is cleared so the default takes over");
            Assert.IsTrue(Player.AI.HasGoal(g => g is ProcessResearchStation), "the goal waits for the next evaluation");
            Assert.IsFalse(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(planet)), "and has queued nothing yet");

            goal.Evaluate();

            Assert.IsTrue(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(planet)), "the next evaluation builds the default station");
        }

        [TestMethod]
        public void AMiningGoalWithAStaleNameClearsItAndRetries()
        {
            AddHomeWorldToEmpire(new SDGraphics.Vector2(400000), Player);
            Player.UpdateRallyPoints();
            Planet planet = AddDummyPlanet(new SDGraphics.Vector2(10), 0, 0, 0);
            planet.Mining = new Mineable(planet);
            Player.data.CurrentMiningStation = "No Such Station Design";

            var goal = new MiningOps(Player, planet);
            Player.AI.AddGoalAndEvaluate(goal);

            AssertEqual("", Player.data.CurrentMiningStation, "the stale choice is cleared so the default takes over");
            Assert.IsTrue(Player.AI.HasGoal(g => g is MiningOps), "the goal waits for the next evaluation");
            Assert.IsFalse(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(planet)), "and has queued nothing yet");

            goal.Evaluate();

            Assert.IsTrue(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(planet)), "the next evaluation builds the default station");
        }

        [TestMethod]
        public void AGoalWithNoDefaultToFallBackOnFails()
        {
            Planet planet = AddDummyPlanet(new SDGraphics.Vector2(10), 0, 0, 0);
            Player.data.CurrentResearchStation = "";
            Player.data.DefaultResearchStation = "No Such Station Design";

            Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, planet));

            Assert.IsFalse(Player.AI.HasGoal(g => g is ProcessResearchStation), "nothing to build, the goal removes itself");
        }

        [TestMethod]
        public void TheAutoPickerFallsBackToNullWhenTheNamedDesignIsGone()
        {
            Assert.IsFalse(Enemy.CanBuildResearchStations || Enemy.CanBuildMiningStations, "the AI empire must have nothing to pick from");
            Enemy.data.CurrentResearchStation = "No Such Station Design";
            Enemy.data.CurrentMiningStation = "No Such Station Design";

            Assert.IsNull(ShipBuilder.PickResearchStation(Enemy));
            Assert.IsNull(ShipBuilder.PickMiningStation(Enemy));
        }

        const string SturdyStation = "Sturdy Station Test";
        const string BareStation = "Bare Station Test";
        const string StarvedStation = "Starved Station Test";
        const string PlainStation = "Plain Station Test";

        static ShipModule Module(DesignSlot slot) => ResourceManager.GetModuleTemplate(slot.ModuleUID);

        ShipDesign AddStation(IShipDesign from, string name, DesignSlot[] slots)
        {
            ShipDesign design = ((ShipDesign)from).GetClone(name);
            design.SetDesignSlots(slots, updateRole: false);
            Player.AddBuildableShip(design);
            return design;
        }

        // the design plus `count` copies of one module in a strip below its grid, so no slots overlap
        ShipDesign AddStationWithExtra(IShipDesign from, string name, DesignSlot extra, int count)
        {
            ShipDesign design = ((ShipDesign)from).GetClone(name);
            ShipGridInfo info = design.GridInfo;
            var slots = new Array<DesignSlot>(from.GetOrLoadDesignSlots());
            for (int i = 0; i < count; ++i)
                slots.Add(new DesignSlot(new Point(i * extra.Size.X, info.Size.Y), extra.ModuleUID, extra.Size, 0, ModuleOrientation.Normal, null));
            info.Size = new Point(Math.Max(info.Size.X, count * extra.Size.X), info.Size.Y + extra.Size.Y);
            design.GridInfo = info;
            design.SetDesignSlots(slots.ToArray(), updateRole: false);
            Player.AddBuildableShip(design);
            return design;
        }

        [TestMethod]
        public void AutoPickTakesTheSturdierOfTwoMiningStationsThatRefineAlike()
        {
            Player.AutoPickBestMiningStation = true;
            IShipDesign best = ShipBuilder.PickMiningStation(Player);
            Assert.IsNotNull(best, "setup: the empire must have a mining station to pick");

            DesignSlot[] slots = best.GetOrLoadDesignSlots();
            DesignSlot smallestHold = slots.Filter(s => Module(s).CargoCapacity > 0 && Module(s).Refining <= 0)
                                           .FindMin(s => Module(s).CargoCapacity);
            Assert.IsNotNull(smallestHold, $"setup: '{best.Name}' needs a cargo hold");

            foreach (IShipDesign design in Player.ShipsWeCanBuildSnapshot)
                if (design.IsMiningStation)
                    Player.RemoveBuildableShip(design);
            ShipDesign sturdy = AddStation(best, SturdyStation, slots.Filter(s => s != smallestHold));
            ShipDesign bare = AddStation(best, BareStation, slots.Filter(s => Module(s).Refining > 0 || Module(s).CargoCapacity > 0));

            AssertEqual(bare.BaseRefiningPerTurn, sturdy.BaseRefiningPerTurn, "setup: both refine alike");
            Assert.IsTrue(sturdy.BaseCargoSpace * 0.5f >= ShipResupply.NumTurnsForGoodRefiningSupply,
                "setup: both hold enough to score their full refining");
            Assert.IsTrue(bare.BaseCargoSpace > sturdy.BaseCargoSpace, "setup: the bare one holds more");
            Assert.IsTrue(sturdy.GetStrength(Player) > bare.GetStrength(Player), "setup: the other one is sturdier");

            AssertEqual(SturdyStation, ShipBuilder.PickMiningStation(Player)?.Name,
                "a tie on refining goes to the sturdier station, not the one with more cargo per refining");
        }

        [TestMethod]
        public void AutoPickNeverTakesAMiningStationThatStoresTooLittleToRefine()
        {
            Player.AutoPickBestMiningStation = true;
            IShipDesign best = ShipBuilder.PickMiningStation(Player);
            Assert.IsNotNull(best, "setup: the empire must have a mining station to pick");

            DesignSlot unit = best.GetOrLoadDesignSlots().FindMax(s => Module(s).Refining);
            float suppliable = best.BaseCargoSpace / (GlobalStats.Defaults.MiningStationFoodPerOneRefining * ShipResupply.NumTurnsForGoodRefiningSupply);
            int extraUnits = (int)((suppliable - best.BaseRefiningPerTurn) / Module(unit).Refining) + 1;
            ShipDesign starved = AddStationWithExtra(best, StarvedStation, unit, extraUnits);

            Assert.IsTrue(starved.BaseCargoSpace * 0.5f >= ShipResupply.NumTurnsForGoodRefiningSupply,
                "setup: it holds enough to score its full refining");
            Assert.IsFalse(ShipResupply.HasGoodTotalSupplyForRefining(starved), "setup: but too little to supply 20 turns of it");

            AssertEqual(best.Name, ShipBuilder.PickMiningStation(Player)?.Name,
                "a station that cannot store supplies for 20 turns of refining is never picked");
        }

        [TestMethod]
        public void AutoPickNeverTakesAResearchStationThatStoresTooLittleToResearch()
        {
            Player.AutoPickBestResearchStation = true;
            IShipDesign best = ShipBuilder.PickResearchStation(Player);
            Assert.IsNotNull(best, "setup: the empire must have a research station to pick");

            DesignSlot[] slots = best.GetOrLoadDesignSlots();
            DesignSlot lab = slots.FindMax(s => Module(s).ResearchPerTurn);
            float suppliable = best.BaseCargoSpace / (GlobalStats.Defaults.ResearchStationProductionPerResearch * ShipResupply.NumTurnsForGoodResearchSupply);
            float outOfTheRunning = best.BaseResearchPerTurn / 0.8f;
            int extraLabs = (int)((Math.Max(suppliable, outOfTheRunning) - best.BaseResearchPerTurn) / Module(lab).ResearchPerTurn) + 1;

            foreach (IShipDesign design in Player.ShipsWeCanBuildSnapshot)
                if (design.IsResearchStation)
                    Player.RemoveBuildableShip(design);
            ShipDesign plain = AddStation(best, PlainStation, slots);
            ShipDesign starved = AddStationWithExtra(best, StarvedStation, lab, extraLabs);

            Assert.IsTrue(ShipResupply.HasGoodTotalSupplyForResearch(plain), "setup: one stores enough");
            Assert.IsFalse(ShipResupply.HasGoodTotalSupplyForResearch(starved), "setup: the other stores too little");
            Assert.IsTrue(plain.BaseResearchPerTurn < starved.BaseResearchPerTurn * 0.8f, "setup: and researches far more");

            AssertEqual(PlainStation, ShipBuilder.PickResearchStation(Player)?.Name,
                "a station that cannot store supplies for 30 turns of research is never picked");
        }

        [TestMethod]
        public void ARealNameResolvesToItsDesign()
        {
            string name = Player.data.MiningStation;
            IShipDesign design = ShipBuilder.StationDesignOrNull(name);
            Assert.IsNotNull(design, $"'{name}' should be a shipped design");
            AssertEqual(name, design.Name);
        }
    }
}

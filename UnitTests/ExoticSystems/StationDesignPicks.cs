using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Ships;
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

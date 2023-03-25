using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.ExoticSystems
{
    [TestClass]
    public class ResearchStations : StarDriveTest
    {
        readonly Planet Homeworld;
        readonly Planet NewPlanet;
        readonly Planet EnemyHome;

        public ResearchStations()
        {
            CreateUniverseAndPlayerEmpire();
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            NewPlanet = AddDummyPlanet(new Vector2(10), 0, 0, 0);
            Homeworld = AddHomeWorldToEmpire(new Vector2(400000), Player);
            EnemyHome = AddHomeWorldToEmpire(new Vector2(-400000), Enemy);

            PlanetType type = ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant);
            var random = new SeededRandom();
            NewPlanet.GenerateNewFromPlanetType(random, type, scale: 1.5f, preDefinedPop: 16);

            if (!NewPlanet.IsResearchable)
                NewPlanet.SetResearchable(true, Universe.UState);
        }

        [TestMethod]
        public void ResearchStationDeployment()
        {
            Player.UpdateRallyPoints();
            ResearchDebugUnlocks.UnlockAllResearch(Player, unlockBonuses: false);
            AssertEqual(true, Player.CanBuildPlatforms);
            AssertEqual(true, Player.CanBuildResearchStations);

            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            // Not explored yet
            AssertEqual(false, Player.AI.HasGoal(g => g.IsResearchStationGoal(NewPlanet)));

            NewPlanet.SetExploredBy(Player);
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            // No automation activated
            AssertEqual(false, Player.AI.HasGoal(g => g.IsResearchStationGoal(NewPlanet)));

            Player.AutoBuildResearchStations = true;
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            AssertEqual(true, Player.AI.HasGoal(g => g.IsResearchStationGoal(NewPlanet)));
            AssertEqual(true, Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(NewPlanet)));

            SolarSystem system = NewPlanet.System;
            system.SetResearchable(true, Universe.UState);
            AssertEqual(false, system.IsExploredBy(Player));

            // system not explored yet
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            AssertEqual(false, Player.AI.HasGoal(g => g.IsResearchStationGoal(system)));

            system.SetExploredBy(Player);
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            Player.AI.ResearchStationsAI.RunResearchStationPlanner();
            AssertEqual(true, Player.AI.HasGoal(g => g.IsResearchStationGoal(system)));
            AssertEqual(true, Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(system)));
        }
    }
}
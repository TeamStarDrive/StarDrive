using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class RemnantRaidTargetTests : StarDriveTest
    {
        readonly SolarSystem PortalSystem;
        readonly Planet EmptyPlanet; // closer to the portal than the colony
        readonly Planet Colony;
        readonly Planet EnemyHome;
        readonly Ship Portal;

        public RemnantRaidTargetTests()
        {
            LoadStarterShips("Neutron Bomber Mk1");
            CreateUniverseAndPlayerEmpire();
            EnemyHome = AddHomeWorldToEmpire(new Vector2(400_000), Enemy);
            CreateAMinorFaction("Remnant");

            PortalSystem = new SolarSystem(UState, new Vector2(-200_000, 0)) { Sun = SunType.RandomHabitableSun(UState.Random) };
            EmptyPlanet = AddPlanet(PortalSystem, PortalSystem.Position + new Vector2(6_000, 0));
            Colony = AddPlanet(PortalSystem, PortalSystem.Position + new Vector2(30_000, 0));
            UState.AddSolarSystem(PortalSystem);
            Colony.SetOwner(Player);

            Portal = OpenPortal(PortalSystem);
        }

        // the way the Remnants open one: the goal is made with the new ship, which is placed in its system on the next update
        Ship OpenPortal(SolarSystem system, bool firstStep = true)
        {
            Ship portal = SpawnShip("Vulcan Scout", Faction, system.Position + new Vector2(5_000, 0));
            var goal = new RemnantPortal(Faction, portal, system.Name);
            Faction.AI.AddGoal(goal);
            Assert.IsNull(portal.System, "setup: a new ship has no system yet");
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in its system
            if (!firstStep)
                goal.AdvanceToNextStep();
            goal.Evaluate();
            return portal;
        }

        SolarSystem EmptySystem(Vector2 pos)
        {
            var system = new SolarSystem(UState, pos) { Sun = SunType.RandomHabitableSun(UState.Random) };
            UState.AddSolarSystem(system);
            return system;
        }

        static Planet AddPlanet(SolarSystem system, Vector2 pos)
        {
            var p = new Planet(system.Universe.CreateId(), system, pos, fertility: 1f, minerals: 1f, maxPop: 4f);
            p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
            p.TestSetOrbitalRadius(p.Position.Distance(system.Position) + p.Radius);
            system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
            system.PlanetList.Add(p);
            return p;
        }

        // a raid on the colony, waiting for its fleet (WaitForCompletion)
        RemnantEngageEmpire RaidUnderWay(int fleetShips = 1, int taskStep = 5, string shipName = "Vulcan Scout")
        {
            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();
            raid.Task = MilitaryTask.CreateRemnantEngagement(raid.TargetPlanet, Faction);
            raid.Task.TargetEmpire = Enemy;
            raid.Fleet = new Fleet(UState, Faction) { FleetTask = raid.Task };
            for (int i = 0; i < fleetShips; ++i)
                raid.Fleet.AddShip(SpawnShip(shipName, Faction, Portal.Position));
            raid.Fleet.TaskStep = taskStep;
            raid.AdvanceToNextStep();
            raid.AdvanceToNextStep();
            return raid;
        }

        static void RunFleetTask(Fleet fleet)
        {
            typeof(Fleet).GetMethod("DoRemnantEngagement", BindingFlags.Instance | BindingFlags.NonPublic)!
                         .Invoke(fleet, new object[] { fleet.FleetTask });
        }

        void FocusRemnantsOnEnemy()
        {
            typeof(Remnants).GetProperty(nameof(Remnants.Story))!.SetValue(Faction.Remnants, Remnants.RemnantStory.AncientExterminators);
            Faction.Remnants.SetFocusOnEmpire(Enemy);
        }

        [TestMethod]
        public void APortalRegistersItsSystemOnceItsShipIsPlaced()
        {
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            OpenPortal(system);
            Assert.IsTrue(UState.HasRemnantPortal(system), "known portal systems feed the pathfinder and the raid rules");
        }

        [TestMethod]
        public void APortalFromAnOlderSaveRegistersItsSystem()
        {
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            OpenPortal(system, firstStep: false);
            Assert.IsTrue(UState.HasRemnantPortal(system), "a portal goal past its first step registers its system too");
        }

        [TestMethod]
        public void ARaidTargetsTheColonyInItsPortalSystemWhoeverOwnsIt()
        {
            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();
            Assert.AreSame(Colony, raid.TargetPlanet, "the player's colony, not the empty planet nearer the portal, though the raid is after the enemy");
        }

        [TestMethod]
        public void ARaidStartedWhileThePortalChasesAnEnemyTargetsTheColonyInItsSystem()
        {
            Portal.Position = PortalSystem.Position + new Vector2(150_000, 0);
            Portal.SetSystem(null); // as the ship's once a second update does outside a system

            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();

            Assert.AreSame(Colony, raid.TargetPlanet, "the colony in the portal's system comes first while the portal is away");
        }

        [TestMethod]
        public void ARaidSkipsAColonyItMayNotAttack()
        {
            try
            {
                GlobalStats.RestrictAIPlayerInteraction = true;
                Faction.GetRelations(Player).UpdateRelationship(Faction, Player);
                var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
                raid.Evaluate();
                Assert.AreSame(EnemyHome, raid.TargetPlanet, "the player is locked out, so the raid goes after its target empire");
            }
            finally
            {
                GlobalStats.RestrictAIPlayerInteraction = false;
            }
        }

        [TestMethod]
        public void TheRaidStaysOnThePortalSystemColonyWhileItStands()
        {
            RemnantEngageEmpire raid = RaidUnderWay();

            GoalStep result = raid.Evaluate();

            Assert.AreEqual(GoalStep.TryAgain, result, "the raid goes on instead of ending");
            Assert.AreSame(Colony, raid.TargetPlanet, "the raid keeps its target while the colony stands");
            AssertEqual(5, raid.Fleet.TaskStep, "the fleet is not sent elsewhere or home");
        }

        [TestMethod]
        public void TheRaidStaysOnTheColonyWhileThePortalChasesAnEnemyOutOfItsSystem()
        {
            FocusRemnantsOnEnemy();
            RemnantEngageEmpire raid = RaidUnderWay();
            Portal.Position = PortalSystem.Position + new Vector2(150_000, 0);
            Portal.SetSystem(null); // as the ship's once a second update does outside a system

            Assert.AreEqual(GoalStep.TryAgain, raid.Evaluate(), "the raid goes on instead of ending");
            Assert.AreSame(Colony, raid.TargetPlanet, "the raid keeps the colony in the portal's system");
            AssertEqual(5, raid.Fleet.TaskStep, "the fleet is not sent elsewhere or home");
        }

        [TestMethod]
        public void ARaidHoldsOnlyTheColonyInItsOwnPortalsSystem()
        {
            FocusRemnantsOnEnemy();
            var otherPortalSystem = new SolarSystem(UState, new Vector2(0, -300_000)) { Sun = SunType.RandomHabitableSun(UState.Random) };
            Planet otherColony = AddPlanet(otherPortalSystem, otherPortalSystem.Position + new Vector2(20_000, 0));
            UState.AddSolarSystem(otherPortalSystem);
            otherColony.SetOwner(Player);
            OpenPortal(otherPortalSystem);
            RemnantEngageEmpire raid = RaidUnderWay();
            raid.TargetPlanet = otherColony; // a colony of the raid's target empire that another empire took meanwhile

            raid.Evaluate();

            Assert.AreSame(EnemyHome, raid.TargetPlanet, "another portal's system does not hold the raid");
        }

        [TestMethod]
        public void TheRaidMovesOnWhenItCanNoLongerBombTheColony()
        {
            FocusRemnantsOnEnemy();
            RemnantEngageEmpire raid = RaidUnderWay(taskStep: 7);

            raid.Evaluate();

            Assert.AreSame(EnemyHome, raid.TargetPlanet, "with no bombing left the raid goes after its target empire");
        }

        [TestMethod]
        public void ClearingAPortalSystemColonyDoesNotChangeTheStrengthEstimate()
        {
            Faction.InitFleetEmpireStrMultiplier();
            RemnantEngageEmpire raid = RaidUnderWay(taskStep: 6);
            Colony.WipeOutColony(Player);
            float before = Faction.GetFleetStrEmpireMultiplier(Enemy);

            RunFleetTask(raid.Fleet);
            AssertEqual(0.0001f, before, Faction.GetFleetStrEmpireMultiplier(Enemy), "a portal system colony does not count");

            UState.DeregisterRemnantPortal(PortalSystem);
            raid.Fleet.TaskStep = 6;
            RunFleetTask(raid.Fleet);
            Assert.IsTrue(Faction.GetFleetStrEmpireMultiplier(Enemy) < before, "setup: a cleared colony elsewhere lowers the estimate");
        }

        [TestMethod]
        public void LosingTheFleetAtAPortalSystemColonyDoesNotChangeTheStrengthEstimate()
        {
            Faction.InitFleetEmpireStrMultiplier();
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 0);
            float before = Faction.GetFleetStrEmpireMultiplier(Enemy);

            Assert.AreEqual(GoalStep.GoalFailed, raid.Evaluate());
            AssertEqual(0.0001f, before, Faction.GetFleetStrEmpireMultiplier(Enemy), "a portal system colony does not count");

            UState.DeregisterRemnantPortal(PortalSystem);
            RaidUnderWay(fleetShips: 0).Evaluate();
            Assert.IsTrue(Faction.GetFleetStrEmpireMultiplier(Enemy) > before, "setup: a fleet lost elsewhere raises the estimate");
        }

        [TestMethod]
        public void ABomberOnlyFleetCountsItsFailedRaidOnce()
        {
            Faction.InitFleetEmpireStrMultiplier();
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 3, shipName: "Neutron Bomber Mk1");
            raid.TargetPlanet = EnemyHome;
            AssertEqual(3, Faction.Remnants.NumBombersInFleet(raid.Fleet), "setup: only bombers are left");
            float before = Faction.GetFleetStrEmpireMultiplier(Enemy);

            raid.Evaluate();
            float afterBailing = Faction.GetFleetStrEmpireMultiplier(Enemy);
            AssertEqual(8, raid.Fleet.TaskStep, "setup: the fleet heads home");
            raid.Evaluate();

            Assert.IsTrue(afterBailing > before, "setup: a failed raid raises the estimate");
            AssertEqual(0.0001f, afterBailing, Faction.GetFleetStrEmpireMultiplier(Enemy), "not again on the way home");
        }

        [TestMethod]
        public void AFleetCalledBackToDefendThePortalIsReleasedWhenTheFightIsOver()
        {
            for (int i = 0; i < 3; ++i)
                SpawnShipNoCombatHoldPos("Fang Strafer", Player, Portal.Position + new Vector2(2_000 + i * 200, 0));
            UState.Objects.Update(new(time: 2.0f));
            Faction.Threats.Update(new(time: 2.0f));
            Assert.IsTrue(Faction.Remnants.GetHostileStrInPortalSystem(Portal) > Portal.BaseStrength * 0.75f, "setup: the portal is under a real attack");

            RemnantEngageEmpire raid = RaidUnderWay();
            Portal.InCombat = true;
            Assert.AreEqual(GoalStep.GoToNextStep, raid.Evaluate(), "the raid is called off and waits for its fleet");
            AssertEqual(8, raid.Fleet.TaskStep, "the fleet is ordered back to the portal");

            raid.Fleet.TaskStep = 10; // back at the portal
            Assert.AreEqual(GoalStep.TryAgain, raid.Evaluate(), "the fleet stays while the portal is under attack");

            Portal.InCombat = false;
            Assert.AreEqual(GoalStep.GoalComplete, raid.Evaluate(), "the fleet is released once the fight is over");
        }

        [TestMethod]
        public void ARaidStartingInThePortalSystemWarnsTheColonyOwner()
        {
            var notifications = new NotificationManager(Universe.ScreenManager, Universe);
            Universe.NotificationManager = notifications;
            Colony.GenerateNewFromPlanetType(new SeededRandom(1), ResourceManager.Planets.PlanetOrRandom(1), 1f);
            Building detector = ResourceManager.CreateBuilding(Colony, Building.TerraformerId);
            detector.DetectsRemnantFleet = true;
            Colony.TilesList[0].PlaceBuilding(detector, Colony);
            RemnantEngageEmpire raid = RaidUnderWay(taskStep: 1);

            RunFleetTask(raid.Fleet);

            AssertEqual(1, notifications.NumberOfNotifications, "the Remnant fleet detector warns of the raid");
        }
    }
}

using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
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
        RemnantEngageEmpire RaidUnderWay(int fleetShips = 1, int taskStep = 5, string shipName = "Vulcan Scout", Planet target = null)
        {
            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();
            if (target != null)
                raid.TargetPlanet = target;
            raid.Task = MilitaryTask.CreateRemnantEngagement(raid.TargetPlanet, Faction);
            raid.Task.TargetEmpire = Enemy;
            if (fleetShips > 0) // as the raid's CreateFleet does, so the task knows its fleet
            {
                raid.Task.CreateRemnantFleet(Faction, SpawnShip(shipName, Faction, Portal.Position), "Ancient Fleet", out Fleet fleet);
                raid.Fleet = fleet;
            }
            else
            {
                raid.Fleet = new Fleet(UState, Faction) { FleetTask = raid.Task };
            }
            for (int i = 1; i < fleetShips; ++i)
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

        void LevelUpWithBombersAffordable(int level)
        {
            Remnants remnants = Faction.Remnants;
            typeof(Remnants).GetProperty(nameof(Remnants.Level))!.SetValue(remnants, level);
            typeof(Remnants).GetProperty(nameof(Remnants.Production))!.SetValue(remnants, 1_000_000f);
            Faction.data.RemnantBomberLight = "Neutron Bomber Mk1";
            remnants.ShipCosts[RemnantShipType.BomberLight] = 1f;
            AssertGreaterThan(remnants.GetNumBombersNeeded(Colony), 0, "setup: the new level needs bombers");
        }

        [TestMethod]
        public void TheWarmongersGoAfterTheWeakestEmpireWhenEveryEmpireIsAtWar()
        {
            typeof(Remnants).GetProperty(nameof(Remnants.Story))!.SetValue(Faction.Remnants, Remnants.RemnantStory.AncientWarMongers);
            Player.TotalScore = 1000;
            Enemy.TotalScore = 400;
            Player.UpdateRelationships(takeTurn: false);
            Enemy.UpdateRelationships(takeTurn: false);
            Assert.IsTrue(Player.IsAtWarWithMajorEmpire && Enemy.IsAtWarWithMajorEmpire, "setup: every empire is at war");

            Assert.IsTrue(Faction.Remnants.FindValidTarget(out Ship_Game.Empire target), "setup: the Remnants find a target");
            Assert.AreSame(Enemy, target, "with every empire at war the Warmongers go after the weakest, as their story says");
        }

        [TestMethod]
        public void WithNoPortalLeftTheRemnantsGoAfterTheEmpireTheyFocusOn()
        {
            FocusRemnantsOnEnemy();

            Assert.AreSame(EnemyHome, Faction.Remnants.GetTargetPlanetForFleetTaskWhenNoPortals(Portal.Position),
                           "the empire that attacked their portal, while it stands, not the player");
        }

        [TestMethod]
        public void WithNoPortalLeftTheRemnantsTurnOnThePlayerWhenTheirFocusHasJustLostItsLastPlanet()
        {
            FocusRemnantsOnEnemy();
            EnemyHome.WipeOutColony(Enemy);
            Assert.IsFalse(Enemy.IsDefeated, "setup: the defeat is noticed only at the empire's next turn");

            Assert.AreSame(Colony, Faction.Remnants.GetTargetPlanetForFleetTaskWhenNoPortals(Portal.Position),
                           "an empire with no planet left offers nothing to aim at");
        }

        [TestMethod]
        public void ARaidFleetLostOnItsWayHomeIsNotCountedAsALostRaid()
        {
            Faction.InitFleetEmpireStrMultiplier();
            UState.DeregisterRemnantPortal(PortalSystem);
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 0, taskStep: 8);
            float before = Faction.GetFleetStrEmpireMultiplier(Enemy);

            Assert.AreEqual(GoalStep.GoalFailed, raid.Evaluate(), "setup: the fleet is gone");

            AssertEqual(0.0001f, before, Faction.GetFleetStrEmpireMultiplier(Enemy),
                        "a raid already heading home was counted when it turned back, or did its job, so its loss is not counted again");
        }

        [TestMethod]
        public void ARaidThatTurnsOnAnotherEmpireTellsItsTask()
        {
            typeof(Remnants).GetProperty(nameof(Remnants.Story))!.SetValue(Faction.Remnants, Remnants.RemnantStory.AncientExterminators);
            CreateThirdMajorEmpire();
            Player.TotalScore = 1000;
            Enemy.TotalScore = 800;
            ThirdMajor.TotalScore = 100;
            RemnantEngageEmpire raid = RaidUnderWay(taskStep: 7);

            raid.Evaluate();

            Assert.AreSame(ThirdMajor, raid.TargetEmpire, "setup: the raid turns on the weakest empire");
            Assert.AreSame(ThirdMajor, raid.Task.TargetEmpire, "planets the fleet clears from now on count against the new target");
        }

        [TestMethod]
        public void AGatheringRaidTakesOnlyTheStrengthItStillMisses()
        {
            FocusRemnantsOnEnemy();
            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();
            raid.Evaluate();
            raid.Task = MilitaryTask.CreateRemnantEngagement(raid.TargetPlanet, Faction);
            raid.Task.TargetEmpire = Enemy;
            raid.Task.CreateRemnantFleet(Faction, SpawnShip("Neutron Bomber Mk1", Faction, Portal.Position), "Ancient Fleet", out Fleet fleet);
            raid.Fleet = fleet;
            for (int i = 0; i < 2; ++i)
                fleet.AddShip(SpawnShip("Neutron Bomber Mk1", Faction, Portal.Position));

            float required = Faction.Remnants.RequiredAttackFleetStr(Enemy);
            Ship probe = SpawnShip("Vulcan Scout", Faction, Portal.Position);
            float escortStr = probe.BaseStrength;
            probe.QueueTotalRemoval();
            int escorts = (int)(3 * required / escortStr) + 10;
            for (int i = 0; i < escorts; ++i)
            {
                Ship escort = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(-800 - i * 10, 0));
                escort.IsGuardian = true;
            }
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system
            AssertEqual(0, Faction.Remnants.GetNumBombersNeeded(Colony), "setup: this level needs no more bombers");

            for (int i = 0; i < 5 && raid.Evaluate() == GoalStep.TryAgain; ++i) { }

            float escortsGathered = raid.Fleet.GetStrength() - raid.Fleet.GetBomberStrength();
            Assert.IsTrue(escortsGathered >= required, $"setup: the raid gathers what it needs ({escortsGathered} of {required})");
            Assert.IsTrue(escortsGathered < required + 2 * escortStr,
                          $"the raid takes about what it needs, not a second batch ({escortsGathered} for {required})");
        }

        Ship[] AddGuardians(int count, int first)
        {
            var guardians = new Ship[count];
            for (int i = 0; i < count; ++i)
            {
                guardians[i] = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(-800 - (first + i) * 10, 0));
                guardians[i].IsGuardian = true;
            }
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system
            return guardians;
        }

        [TestMethod]
        public void AGatheringHelpFleetTakesOnlyTheStrengthItStillMisses()
        {
            FocusRemnantsOnEnemy();
            var help = new RemnantHelpEmpire(Faction, Portal, Enemy);
            AssertEqual(GoalStep.GoToNextStep, help.Evaluate(), "setup: a planet to help is chosen");
            float required = Faction.Remnants.RequiredAttackFleetStr(Enemy);
            Ship[] first = AddGuardians(1, 0);
            float escortStr = first[0].BaseStrength;
            Assert.IsTrue(escortStr < required / 2, "setup: one ship is well short of what the fleet needs");

            AssertEqual(GoalStep.TryAgain, help.Evaluate(), "setup: the first ship is not enough");
            AddGuardians((int)(3 * required / escortStr) + 10, 1);
            for (int i = 0; i < 5 && help.Evaluate() == GoalStep.TryAgain; ++i) { }

            float gathered = help.Fleet.GetStrength();
            Assert.IsTrue(gathered >= required, $"setup: the fleet gathers what it needs ({gathered} of {required})");
            Assert.IsTrue(gathered <= required + escortStr + 1f, $"the fleet takes what it misses, not the whole strength again ({gathered} for {required}, {escortStr} a ship)");
        }

        [TestMethod]
        public void APortalDefenseFleetWithoutItsPortalShipWaitsInsteadOfCrashing()
        {
            MilitaryTask task = MilitaryTask.CreateRemnantDefendPortal(Faction, Portal);
            task.CreateRemnantFleet(Faction, SpawnShip("Vulcan Scout", Faction, Portal.Position), "Ancient Guardians", out Fleet fleet);
            typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.TargetShip))!.SetValue(task, null);
            fleet.TaskStep = 2;

            typeof(Fleet).GetMethod("DoRemnantPortalDefense", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

            AssertEqual(2, fleet.TaskStep, "the fleet without its portal ship stays at its step");
        }

        [TestMethod]
        public void AModWithoutARemnantDesignMultiplierSizesRaidsAsVanilla()
        {
            typeof(Remnants).GetProperty(nameof(Remnants.Level))!.SetValue(Faction.Remnants, Remnants.MaxLevel);
            Enemy.OffensiveStrength = 10_000_000f;
            float set = GlobalStats.Defaults.RemnantDesignStrMultiplier;
            try
            {
                GlobalStats.Defaults.RemnantDesignStrMultiplier = 2f;
                float vanilla = Faction.Remnants.RequiredAttackFleetStr(Enemy);
                Assert.IsTrue(vanilla > Remnants.MaxLevel * Remnants.MaxLevel * 1000, "setup: the target's strength sets the raid size");

                GlobalStats.Defaults.RemnantDesignStrMultiplier = 0f;

                AssertEqual(1f, vanilla, Faction.Remnants.RequiredAttackFleetStr(Enemy), "a mod that leaves the multiplier at 0 gets vanilla's 2, as the difficulty settings do");
            }
            finally
            {
                GlobalStats.Defaults.RemnantDesignStrMultiplier = set;
            }
        }

        [TestMethod]
        public void TheRemnantsCanPickAShipAtTheTopLevelOnInsane()
        {
            UState.P.Difficulty = GameDifficulty.Insane;
            typeof(Remnants).GetProperty(nameof(Remnants.Level))!.SetValue(Faction.Remnants, Remnants.MaxLevel);

            for (int shipsInFleet = 0; shipsInFleet < 4; ++shipsInFleet)
                Assert.IsTrue(System.Enum.IsDefined(Faction.Remnants.SelectShipForCreation(Remnants.MaxLevel, shipsInFleet)),
                              $"a ship type is picked for a fleet of {shipsInFleet} at the top level");
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
        public void ARaidGatheringWhenTheRemnantsLevelUpGathersTheBombersTheNewLevelNeeds()
        {
            FocusRemnantsOnEnemy();
            var raid = new RemnantEngageEmpire(Faction, Portal, Enemy);
            raid.Evaluate();
            raid.Evaluate();
            AssertEqual(0, Faction.Remnants.GetNumBombersNeeded(Colony), "setup: the raid begins at a level that needs no bombers");

            LevelUpWithBombersAffordable(6);
            raid.Evaluate();

            AssertGreaterThan(Faction.Remnants.NumBombersInFleet(raid.Fleet), 0, "the raid gathers bombers for the new level");
        }

        [TestMethod]
        public void ARaidUnderWayWhenTheRemnantsLevelUpSendsForBombers()
        {
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 3);
            foreach (Ship ship in raid.Fleet.Ships)
                ship.Position = EnemyHome.Position;
            AssertEqual(0, Faction.Remnants.NumBombersInFleet(raid.Fleet), "setup: the raid set off with no bombers");

            LevelUpWithBombersAffordable(6);
            AssertGreaterThan(Faction.Remnants.GetNumBombersNeeded(Colony), 3, "setup: the new level needs more bombers than the raid has escorts");
            raid.Evaluate();
            raid.Evaluate();

            Ship[] bombers = raid.Fleet.Ships.Filter(s => s.DesignRole == RoleName.bomber);
            AssertGreaterThan(bombers.Length, 0, "the raid sends for the bombers the new level needs");
            AssertEqual(3, bombers.Length, "but no more than the ships escorting them");
            AssertEqual(5, raid.Fleet.TaskStep, "so the raid does not turn home as a bomber-only fleet");
            foreach (Ship bomber in bombers)
                Assert.IsTrue(bomber.CatchingUpToFleet, "a bomber sent from the portal catches up without holding the raid back");
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
        public void TheRemnantsKeepOnlyTheTasksOfRaidsUnderWay()
        {
            RemnantEngageEmpire released = RaidUnderWay(taskStep: 10); // back at the portal
            RemnantEngageEmpire underWay = RaidUnderWay();
            Faction.AI.AddPendingTask(released.Task); // as each raid's CreateFleet does
            Faction.AI.AddPendingTask(underWay.Task);
            MilitaryTask releasedTask = released.Task;

            Assert.AreEqual(GoalStep.GoalComplete, released.Evaluate(), "setup: the fleet back at the portal is released");
            Faction.AI.Update();

            CollectionAssert.AreEqual(new[] { underWay.Task }, Faction.AI.GetTasks().ToArray(),
                $"the Remnants' task list must hold the raid under way and not the released one ({releasedTask})");
        }

        [TestMethod]
        public void AReleasedRaidFleetGoesBackToEscortThePortal()
        {
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 2, taskStep: 10);
            Ship[] ships = raid.Fleet.Ships.ToArray();

            Assert.AreEqual(GoalStep.GoalComplete, raid.Evaluate(), "setup: the fleet back at the portal is released");

            foreach (Ship ship in ships)
            {
                Assert.IsNull(ship.Fleet, "setup: the released ship left its fleet");
                Assert.IsTrue(ship.AI.FindGoal(ShipAI.Plan.Escort, out ShipAI.ShipGoal escort) && escort.TargetShip == Portal,
                              "a released ship must escort its portal, where the next raid gathers its fleet, instead of fleeing elsewhere");
            }
        }

        [TestMethod]
        public void WhenAPortalDiesItsEscortsGuardTheNearestPlanetInItsSystem()
        {
            Ship escort = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(-800, 0));
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system
            escort.AI.AddEscortGoal(Portal);
            Goal portalGoal = Faction.AI.FindGoal(g => g is RemnantPortal && g.TargetShip == Portal);

            Portal.Die(null, cleanupOnly: true);
            Assert.AreEqual(GoalStep.GoalFailed, portalGoal.Evaluate(), "setup: the portal's goal ends with the portal");

            Assert.IsTrue(escort.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == EmptyPlanet,
                          "an escort left without its portal guards the nearest planet in its system");
        }

        [TestMethod]
        public void AnIdleShipInALivePortalsSystemIsLeftToThePortal()
        {
            Ship idle = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(-800, 0));
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system
            idle.AI.ClearOrders();
            Portal.SetSystem(null);

            Faction.Remnants.SendIdleShipsToGuardPlanets();

            Assert.IsFalse(idle.AI.FindGoal(ShipAI.Plan.Orbit, out _),
                           "a portal gives the idle ships in its system an escort order instead, also while it is out chasing an enemy");
        }

        [TestMethod]
        public void AShipSpawnedToExterminateIsNotSentToGuardAPlanet()
        {
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            Planet planet = AddPlanet(system, system.Position + new Vector2(8_000, 0));
            Ship exterminator = SpawnShip("Vulcan Scout", Faction, planet.Position + new Vector2(3_000, 0));
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in its system
            exterminator.AI.DefaultAIState = AIState.Exterminate;
            exterminator.AI.ClearOrders();

            Faction.Remnants.SendIdleShipsToGuardPlanets();

            Assert.IsFalse(exterminator.AI.FindGoal(ShipAI.Plan.Orbit, out _), "a ship an event spawned to exterminate keeps hunting");
        }

        [TestMethod]
        public void AGuardianThatLostItsOrbitInASaveGuardsTheNearestPlanetAgainAfterALoad()
        {
            UState.StarDate = 1042.5f;
            UState.Remnants = Faction;
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            Planet guarded = AddPlanet(system, system.Position + new Vector2(8_000, 0));
            AddPlanet(system, system.Position + new Vector2(-20_000, 0));
            Ship guardian = SpawnShip("Vulcan Scout", Faction, guarded.Position + new Vector2(3_000, 0));
            guardian.OrderToOrbit(guarded, clearOrders: true, MoveOrder.Aggressive);
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in its system
            guardian.AI.ClearOrders();
            Assert.AreEqual(AIState.AwaitingOrders, guardian.AI.State, "setup: the guardian sits idle, as the chase left it");

            SavedGame save = Universe.Save("UnitTest.IdleGuardian", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
            Ship loadedGuardian = loaded.UState.Objects.FindShip(guardian.Id);
            Planet nearest = loadedGuardian.System.PlanetList.FindMin(p => p.Position.SqDist(loadedGuardian.Position));

            Assert.IsTrue(loadedGuardian.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == nearest,
                          "after the load the guardian guards the nearest planet in its system again");
        }

        [TestMethod]
        public void ThePortalLeavesItsHangarShipsFlyingHome()
        {
            Ship hangarShip = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(800, 0));
            Ship guardian = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(-800, 0));
            hangarShip.IsGuardian = guardian.IsGuardian = true;
            hangarShip.Mothership = Portal;
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system
            hangarShip.AI.OrderReturnToHangar();

            Faction.Remnants.OrderEscortPortal(Portal);

            Assert.IsTrue(guardian.AI.FindGoal(ShipAI.Plan.Escort, out _), "setup: an idle guardian in the portal's system is sent to escort it");
            Assert.AreEqual(AIState.ReturnToHangar, hangarShip.AI.State, "a hangar ship flying home must not be turned around to escort the portal");
        }

        [TestMethod]
        public void TheRemnantsNeitherDraftNorSendAwayAHangarShip()
        {
            Ship hangarShip = SpawnShip("Vulcan Scout", Faction, Portal.Position + new Vector2(800, 0));
            hangarShip.IsGuardian = true; // as every Remnant ship is after a load
            hangarShip.Mothership = Portal;
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the portal's system

            Faction.Remnants.AssignShipInPortalSystem(Portal, bombersNeeded: 0, neededStr: 1_000_000f, out var drafted);
            Assert.IsFalse(drafted.ContainsRef(hangarShip), "a raid must not draft a hangar ship");

            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            AddPlanet(system, system.Position + new Vector2(8_000, 0));
            hangarShip.Position = system.Position + new Vector2(3_000, 0);
            UState.Objects.Update(new(time: 1f)); // moved in the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in the other system
            hangarShip.AI.ClearOrders();
            Assert.AreSame(system, hangarShip.System, "setup: the hangar ship is in a system with no portal");
            Assert.AreEqual(AIState.AwaitingOrders, hangarShip.AI.State, "setup: the hangar ship sits idle");
            Assert.AreEqual(AIState.AwaitingOrders, hangarShip.AI.DefaultAIState, "setup: the hangar ship has no standing task");

            Faction.Remnants.CallGuardians(Portal);
            Assert.IsFalse(hangarShip.AI.FindGoal(ShipAI.Plan.Escort, out _), "a new portal must not call a hangar ship away from its carrier");

            Faction.Remnants.SendIdleShipsToGuardPlanets();
            Assert.IsFalse(hangarShip.AI.FindGoal(ShipAI.Plan.Orbit, out _), "an idle hangar ship is not sent to guard a planet");
        }

        [TestMethod]
        public void ARaidOnItsWayHomeSendsForNoBombers()
        {
            RemnantEngageEmpire raid = RaidUnderWay(fleetShips: 3, taskStep: 8);
            foreach (Ship ship in raid.Fleet.Ships)
                ship.Position = EnemyHome.Position;

            LevelUpWithBombersAffordable(6);
            raid.Evaluate();
            raid.Evaluate();

            AssertEqual(0, Faction.Remnants.NumBombersInFleet(raid.Fleet), "a raid turning home must not spend production on bombers");
        }

        void StartHibernation()
        {
            Remnants remnants = Faction.Remnants;
            UState.StarDate = (remnants.NextLevelUpDate - remnants.NeededHibernationTurns / 10f).RoundToFractionOf10();
            remnants.CheckHibernation();
            Assert.IsTrue(remnants.Hibernating, "setup: the Remnants go into hibernation");
        }

        Ship IdleShipInDeepSpace(Vector2 pos)
        {
            Ship ship = SpawnShip("Vulcan Scout", Faction, pos);
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f));
            ship.AI.ClearOrders();
            Assert.IsNull(ship.System, "setup: the ship is outside every system");
            return ship;
        }

        [TestMethod]
        public void WhenTheRemnantsHibernateIdleShipsInDeepSpaceEscortTheClosestPortal()
        {
            Ship idle = IdleShipInDeepSpace(new Vector2(0, -300_000));

            Faction.Remnants.SendIdleShipsToGuardPlanets();
            Assert.IsFalse(idle.AI.FindGoal(ShipAI.Plan.Escort, out _), "setup: on a load or a portal's death ships in deep space are left alone");

            StartHibernation();
            Assert.IsTrue(idle.AI.FindGoal(ShipAI.Plan.Escort, out ShipAI.ShipGoal escort) && escort.TargetShip == Portal,
                          "when the Remnants hibernate, an idle ship in deep space goes to the portal");
        }

        [TestMethod]
        public void WhenTheRemnantsHibernateWithNoPortalIdleShipsInDeepSpaceGuardTheNearestPlanet()
        {
            Portal.Die(null, cleanupOnly: true);
            Ship idle = IdleShipInDeepSpace(EnemyHome.Position + new Vector2(-150_000, 0));

            StartHibernation();
            Assert.IsTrue(idle.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == EnemyHome,
                          "with no portal left, an idle ship in deep space guards the nearest planet");
        }

        [TestMethod]
        public void ASeenRaidFleetSettingOffFlashesItsTargetSystem()
        {
            Planet farColony = AddPlanet(PortalSystem, PortalSystem.Position + new Vector2(60_000, 0));
            farColony.SetOwner(Player);
            RemnantEngageEmpire raid = RaidUnderWay(taskStep: 1, target: farColony);
            foreach (Ship ship in raid.Fleet.Ships)
            {
                ship.Position = PortalSystem.Position + new Vector2(800_000, 0);
                ship.KnownByEmpires.SetSeen(Player);
            }
            raid.Fleet.AveragePosition(force: true);

            RunFleetTask(raid.Fleet);
            Player.AssessSystemsInDanger(TestSimStep);

            AssertEqual(2, raid.Fleet.TaskStep, "setup: the fleet set off for its gathering point");
            Assert.IsTrue(Player.SystemsWithThreat.Any(t => t.TargetSystem == PortalSystem),
                          "the target system flashes while a seen raid fleet heads for it, wherever the planet sits in the system");
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

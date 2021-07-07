using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Compnonents;
using Ship_Game.Empires;
using Ship_Game.Empires.Components;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

namespace UnitTests.AITests.Empire
{
    using static EmpireAI;

    [TestClass]
    public class TestEmpireAI : StarDriveTest
    {
        // NOTE: This constructor is called every time a [TestMethod] is executed
        public TestEmpireAI()
        {
            CreateGameInstance();
            ResourceManager.TestOptions testOptions = ResourceManager.TestOptions.LoadPlanets;
            testOptions |= ResourceManager.TestOptions.TechContent;
            LoadStarterShips(testOptions,
                             "Excalibur-Class Supercarrier", "Corsair", "Supply Shuttle",
                             "Flak Fang", "Akagi-Class Mk Ia Escort Carrier", "Rocket Inquisitor",
                             "Cordrazine Prototype", "Cordrazine Troop", "PLT-Defender");

            CreateUniverseAndPlayerEmpire();
            Enemy.isFaction = false;

            AddPlanetToUniverse(2, 2, 40000, true, Vector2.One);
            AddPlanetToUniverse(1.9f, 1.9f, 40000, true, new Vector2(5000));
            AddPlanetToUniverse(1.7f, 1.7f, 40000, true, new Vector2(-5000));
            for (int x = 0; x < 50; x++)
                AddPlanetToUniverse(0.1f, 0.1f, 1000, true, Vector2.One);
            AddHomeWorldToEmpire(Player, out Planet hw1);
            AddPlanetToUniverse(hw1, true, Vector2.Zero);
            AddHomeWorldToEmpire(Enemy, out Planet hw2);
            AddPlanetToUniverse(hw2, true, new Vector2(2000));
            foreach (string uid in ResourceManager.GetShipTemplateIds())
                Player.ShipsWeCanBuild.Add(uid);

            Universe.Objects.UpdateLists(true);
        }

        public void AddPlanetToUniverse(Planet p, bool explored, Vector2 pos)
        {
            var s1         = new SolarSystem {Position = pos};
            p.Center       = pos + Vector2.One;
            p.ParentSystem = s1;
            s1.PlanetList.Add(p);
            if (explored)
                s1.SetExploredBy(Player);
            Ship_Game.Empire.Universe.PlanetsDict.Add(Guid.NewGuid(), p);
            UniverseScreen.SolarSystemList.Add(s1);
        }
        public void AddPlanetToUniverse(float fertility, float minerals, float pop, bool explored, Vector2 pos)
        {
            AddDummyPlanet(fertility, minerals, pop, out Planet p);
            p.Center = pos;
            AddPlanetToUniverse(p, explored, pos);
        }

        /* going to add tests here
        [TestMethod]
        public void TestExpansionPlannerColonize()
        {
            var expansionAI = TestEmpire.GetEmpireAI().ExpansionAI;
            TestEmpire.AutoColonize = true;
            expansionAI.RunExpansionPlanner();

            Assert.AreEqual(13, expansionAI.RankedPlanets.Length,
                "Colonization target list should be 13");

            var markedPlanet = expansionAI.GetColonizationGoalPlanets();
            Assert.AreEqual(3, markedPlanet.Length, "Expected 3 colony goals ");

            //mock colonization success
            expansionAI.DesiredPlanets[0].Owner = TestEmpire;
            TestEmpire.GetEmpireAI().EndAllTasks();
            expansionAI.RunExpansionPlanner();
            Assert.AreEqual(13, expansionAI.RankedPlanets.Length);
            markedPlanet = expansionAI.GetColonizationGoalPlanets();
            Assert.AreEqual(3, markedPlanet.Length, "Expected 3 colony goals ");
            expansionAI.RunExpansionPlanner();

        }*/

        [TestMethod]
        public void ShipBuiltAndUpdateBuildLists()
        {
            string testName = "";
            var build = new RoleBuildInfo(10, Player.GetEmpireAI(), true);
            string shipName = Player.GetEmpireAI().GetAShip(build);
            Assert.IsTrue(shipName == "Rocket Inquisitor", "Build did not create expected ship");

            // prepare shipswecanbuildTest
            var ship = SpawnShip("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            var prototype = SpawnShip("Cordrazine Prototype", Player, Vector2.Zero);
            shipName = ship.Name;
            Player.ShipsWeCanBuild.Remove(ship.Name);
            Player.ShipsWeCanBuild.Remove(prototype.Name);

            // verify that we can not currently add wanted ship
            Player.UpdateShipsWeCanBuild(new Array<String>{ ship.BaseHull.Name });
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(shipName), $"{shipName} Without tech this should not have been added. ");

            // after techs are added we should be able to add wanted ship
            ShipDesignUtils.MarkDesignsUnlockable(new ProgressCounter());
            foreach (var tech in ship.shipData.TechsNeeded)
            {
                Player.UnlockTech(tech, TechUnlockType.Normal);
            }
            foreach (var tech in prototype.shipData.TechsNeeded)
            {
                Player.UnlockTech(tech, TechUnlockType.Normal);
            }
            Player.UnlockedHullsDict[ship.shipData.Hull] = true;
            Player.UnlockedHullsDict[prototype.shipData.Hull] = true;
            Player.UpdateShipsWeCanBuild(new Array<String> { ship.shipData.Hull });
            Assert.IsTrue(Player.ShipsWeCanBuild.Contains(shipName), $"{shipName} Not found in ShipWeCanBuild");
            Assert.IsTrue(Player.canBuildCarriers, $"{shipName} did not mark {ship.DesignRole} as buildable");

            Player.UpdateShipsWeCanBuild(new Array<String> { prototype.shipData.Hull });
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(prototype.Name), "Prototype ship added to shipswecanbuild");

            // Check that adding again does not does not trigger updates.
            Player.canBuildCapitals = false;
            Player.UpdateShipsWeCanBuild(new Array<String> { ship.BaseHull.Name });
            Assert.IsFalse(Player.canBuildCapitals, $"UpdateShipsWeCanBuild triggered unneeded updates");

            // add new player ship design
            Assert.IsTrue(TestShipAddedToShipsWeCanBuild("Rocket Inquisitor", Player, true), "Bug: Could not add Player ship to shipswecanbuild");
            Player.ShipsWeCanBuild.Remove("Rocket Inquisitor");
            Assert.IsFalse(TestShipAddedToShipsWeCanBuild("Excalibur-Class Supercarrier", Player, true, unlockHull: false), "Added ship with locked hull");

            // add new enemy design
            GlobalStats.UsePlayerDesigns = true;
            Assert.IsTrue(TestShipAddedToShipsWeCanBuild("Excalibur-Class Supercarrier", Enemy, true), "Bug: Could not add valid design to shipswecanbuild");
            GlobalStats.UsePlayerDesigns = false;
            Assert.IsFalse(TestShipAddedToShipsWeCanBuild("Flak Fang", Enemy, true), "Use Player design restriction added to shipswecanbuild");

            // fail to add incompatible design
            Assert.IsFalse(TestShipAddedToShipsWeCanBuild("Supply Shuttle", Player, true), "Bug: Supply shuttle added to shipsWeCanBuild");

            testName = "Update Structures: ";
            Assert.IsTrue(TestShipAddedToShipsWeCanBuild("PLT-Defender", Player, false), testName + "ShipsWeCanBuild was not updated.");
            Assert.IsTrue(Player.structuresWeCanBuild.Contains("PLT-Defender"), testName + "StructuresWeCanBuild Was Not Updated");
        }

        bool TestShipAddedToShipsWeCanBuild(string baseDesign, Ship_Game.Empire empire, bool playerDesign, bool unlockHull = true)
        {
            string key1 = RandomMath.IntBetween(1, 999999).ToString();
            string key2 = RandomMath.IntBetween(1, 999999).ToString();
            string newName = baseDesign + $"-test-{key1}-test-{key2}";
            var ship = SpawnShip(baseDesign, empire, Vector2.Zero);
            empire.UnlockedHullsDict[ship.shipData.Hull] = unlockHull;
            ship.shipData.Name = newName;
            ResourceManager.AddShipTemplate(ship.shipData, false, playerDesign);
            empire.UpdateShipsWeCanBuild();
            return empire.ShipsWeCanBuild.Contains(newName);
        }

        [TestMethod]
        public void TestBuildCounts()
        {
            // setup Build
            var build = new RoleBuildInfo(2, Player.GetEmpireAI(), true);

            // init base variables
            var combatRole      = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ShipData.RoleName.fighter);
            float roleBudget    = build.RoleBudget(combatRole);
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            int count;

            // try to build a lot of ships. this loop should break
            for(count = 1; count < 50; count++)
            {
                string shipName   = Player.GetEmpireAI().GetAShip(build);

                if (shipName.IsEmpty())
                {
                    bool canBuildMore = build.CanBuildMore(combatRole);
                    Assert.IsFalse(canBuildMore, "Role says it cant build more but build process says no");
                    break;
                }

                // create the ship
                var ship            = SpawnShip(shipName, Player, Vector2.Zero);
                float shipMaint     = ship.GetMaintCost();

                Assert.IsFalse(shipMaint > roleUnitMaint
                    , $"Ship maintenance: {shipMaint} should never be more than per unit maintenance: {roleUnitMaint}");

                float currentMaint = build.RoleCurrentMaintenance(combatRole);
                Assert.IsTrue((roleUnitMaint * count).AlmostEqual(currentMaint)
                    , $"Current Maintenance: {currentMaint} should equal projected: {roleUnitMaint * count}");
            }

            Assert.IsTrue(count < 49, $"Test failure! Loop completed! Investigate");

            Assert.AreEqual(build.RoleCount(combatRole), (int)(roleBudget / roleUnitMaint));
        }
        
        [TestMethod]
        public void TestBuildScrap()
        {
            var combatRole      = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ShipData.RoleName.fighter);
            float buildCapacity = 0.75f;
            var build           = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), true);

            // build 50 ships
            for (int x = 0; x < 50; x++)
            {
                // This will peak the "Rocket Inquisitor" ships since it is stronger
                string shipName = Player.GetEmpireAI().GetAShip(build);
                if (shipName.NotEmpty())
                    SpawnShip(shipName, Player, Vector2.Zero);
            }

            // add them to the universe
            Universe.Objects.UpdateLists();

            // The expected maintenance for the Flak Fang is 0.12, since Cordrazine
            // Have -25% maintenance reduction
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            Assert.AreEqual(0.12f, roleUnitMaint, "Unexpected maintenance value");

            // simulate building a bunch of ships by lowering the role build budget by the 
            // role maintenance. Keep building until it starts to scrap.
            // 
            for (int x = 0; x < 20; ++x)
            {
                buildCapacity = build.RoleBudget(combatRole) - roleUnitMaint;

                if (buildCapacity < 0)
                {
                    foreach (Ship ship in Player.OwnedShips)
                        if (ship.AI.State == AIState.Scrap)
                            ship.Die(ship, true);
                }

                // once we bottom out add three more ships to build cap
                if (buildCapacity + roleUnitMaint <= 0)
                    buildCapacity = roleUnitMaint * 3;

                Universe.Objects.UpdateLists();

                build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), false);
                // this is the actual number of ships to build with available budget.
                int roleCountWanted    = build.RoleCountDesired(combatRole);
                // this is the formula used to determine the number of ships that can be built with available budget.
                int expectedBuildCount = (int)Math.Ceiling(build.RoleBudget(combatRole) / roleUnitMaint);

                // test that formula for building ships matches the actual building ships process.
                Assert.AreEqual(expectedBuildCount, roleCountWanted, $"{combatRole}: expected number of ships to build did not match actual");

                float currentMain      = build.RoleCurrentMaintenance(combatRole);
                int shipsBeingScrapped = Player.OwnedShips.Filter(s => s.AI.State == AIState.Scrap).Length;

                // now make sure that the maintenance and budgets dont create a scrap loop.
                if (currentMain + roleUnitMaint > buildCapacity)
                {
                    string shipName = Player.GetEmpireAI().GetAShip(build);
                    Assert.IsTrue(shipName.IsEmpty(), $"Current maintenance {currentMain}" +
                                                      $" + new ship maintenance {roleUnitMaint} was greater than build cap {buildCapacity}" +
                                                      $" but we still built a ship.");
                }
                else
                {
                    Assert.IsFalse(build.RoleIsScrapping(combatRole), "We have build budget and we are set to scrap");
                    Assert.IsTrue(shipsBeingScrapped <= 0, $"We have build budget and we have ships scrapping: {shipsBeingScrapped}");
                    string shipName = Player.GetEmpireAI().GetAShip(build);
                    Assert.IsTrue(shipName.NotEmpty(), "We have build budget but we aren't building");
                }
            }
        }

        [TestMethod]
        public void TestOverBudgetSpendingHigh()
        {
            // normalized money is not reset to zero
            Player.Money = 500;
            Player.UpdateNormalizedMoney(Player.Money);

            for (int x = -1; x < 11; x++)
            {
                float percent = x * 0.1f;
                float overSpend = Player.GetEmpireAI().OverSpendRatio(1000, percent, 10f);
                percent = 2 - percent;
                Assert.IsTrue(overSpend.AlmostEqual(percent), $"Expected {percent} got {overSpend}");
            }
        }

        [TestMethod]
        public void TestOverBudgetSpendingLow()
        {
            // normalized money is not reset to zero
            Player.Money = 50;
            Player.UpdateNormalizedMoney(Player.Money);
            for (int x = -1; x < 1; x++)
            {
                float percent = x * 0.05f;
                float overSpend = Player.GetEmpireAI().OverSpendRatio(1000, percent, 10f);
                percent = 0.2f - percent;
                Assert.IsTrue(overSpend.AlmostEqual(percent), $"Expected {percent} got {overSpend}");
            }
        }

        [TestMethod]
        public void TestShipListTracking()
        {
            Assert.IsTrue(Player.OwnedShips.Count == 0);
            IEmpireShipLists playerShips = Player;

            string shipName = "Rocket Inquisitor";

            // test that ship is added to empire on creation
            var ship = SpawnShip(shipName, Player, Vector2.Zero);
            Universe.Objects.UpdateLists(true);
            Assert.IsTrue(Player.OwnedShips.Count == 1);

            // test that removed ship removes the ship from ship list
            playerShips.RemoveShipAtEndOfTurn(ship);
            Universe.Objects.UpdateLists(true);
            Assert.IsTrue(Player.OwnedShips.Count == 0);

            // test that a ship added to empire directly is added.
            playerShips.AddNewShipAtEndOfTurn(ship);
            Universe.Objects.UpdateLists(true);
            Assert.IsTrue(Player.OwnedShips.Count == 1);

            // test that a ship cant be added twice
            // debugwin will enable error checking
            Universe.DebugWin = new Ship_Game.Debug.DebugInfoScreen(null);
            playerShips.AddNewShipAtEndOfTurn(ship);
            Universe.Objects.UpdateLists(true);
            Assert.IsTrue(Player.OwnedShips.Count == 1);
            Universe.DebugWin = null;

            // test that removing the same ship twice doesn't fail.
            playerShips.RemoveShipAtEndOfTurn(ship);
            Assert.IsTrue(Player.OwnedShips.Count == 1);
            Universe.Objects.UpdateLists(true);
            playerShips.RemoveShipAtEndOfTurn(ship);
            Universe.Objects.UpdateLists(true);
            Assert.IsTrue(Player.OwnedShips.Count == 0);
        }

        [TestMethod]
        public void ShipListConcurrencyStressTest()
        {
            Assert.AreEqual(0, Enemy.OwnedShips.Count);

            // we need to rework basic empires. Proper empire updates cannot be done the way they currently are.
            Enemy.data.IsRebelFaction = false;

            // create areas of operation among empires
            foreach(var empire in EmpireManager.Empires)
            {
                empire.data.Defeated = false;
                foreach(var planet in Universe.PlanetsDict.Values)
                {
                    if (RandomMath.RollDice(50))
                    {
                        planet.Owner = empire;
                        empire.GetEmpireAI().AreasOfOperations.Add(new AO(Enemy.Capital, 10));
                    }
                }
            }

            string shipName = "Rocket Inquisitor";

            // create a base number of ships.
            for (int x=0;x< 100; ++x)
            {
                SpawnShip(shipName, Enemy, Vector2.Zero);
            }
            Universe.ScreenManager.InvokePendingEmpireThreadActions();
            Universe.Objects.UpdateLists(true);

            int numberOfShips = Enemy.OwnedShips.Count;
            int shipsRemoved = 0;
            // create a background thread to stress ship pool functions.
            bool stopStress = false;
            var stressTask = Parallel.Run(() =>
            {
                BackGroundPoolStress(ref stopStress);
            });

            int loyaltyChanges = 0;

            // add random number of ships to random empires.
            int first = 0, last = 10;
            {
                for (int i = first; i < last; ++i)
                {
                    int addedShips = 0;

                    foreach(var empire in EmpireManager.Empires)
                    {
                        foreach (var s in empire.OwnedShips)
                        {
                            if (s.Active)
                            {
                                Assert.AreEqual(s.loyalty, empire);
                                float random = RandomMath.AvgRandomBetween(1, 100);
                                if (random > 80)
                                {
                                    s.RemoveFromUniverseUnsafe();
                                    shipsRemoved++;
                                }
                                else if (random > 60)
                                {
                                    var changeTo = EmpireManager.Empires.Find(e => e != empire);
                                    s.LoyaltyChangeFromBoarding(changeTo,false);
                                    loyaltyChanges++;
                                }
                            }

                        }
                    }

                    addedShips = RandomMath.IntBetween(1, 30);

                    Parallel.For(0, EmpireManager.NumEmpires, (firstEmpire, lastEmpire) =>
                        {
                            for (int e = firstEmpire; e < lastEmpire; e++)
                            {
                                var empire = EmpireManager.Empires[e];
                                for (int y = 0; y < addedShips; ++y)
                                {
                                    SpawnShip(shipName, empire, Vector2.Zero);
                                }

                            }
                        }
                    );
                    Universe.Objects.UpdateLists(true);
                    numberOfShips += addedShips * 2;
                }
            }
            stopStress = true;
            stressTask.CancelAndWait();

            int actualShipCount = 0;
            foreach(var empire in EmpireManager.Empires)
            {
                Universe.Objects.UpdateLists(true);
                actualShipCount += empire.OwnedShips.Count;
            }

            numberOfShips -= shipsRemoved;
            Assert.AreEqual(numberOfShips, actualShipCount);
            Log.Info($"loyalty Changes: {loyaltyChanges} Removed Ships: {shipsRemoved} Active Ships: {actualShipCount}");

            Enemy.data.IsRebelFaction = true;
        }

        private int BackGroundPoolStress(ref bool stopStress)
        {
            int removedShips = 0;
            while (!stopStress)
            {
                foreach (var empire in EmpireManager.Empires)
                {
                    var ships = empire.OwnedShips;
                    foreach (var s in ships)
                    {
                        if (s.Active)
                        {
                            s.AI.ClearOrders();
                        }
                    }
                }
            }

            return removedShips;
        }

        [TestMethod]
        public void TestDefeatedEmpireShipRemoval()
        {
            Assert.IsTrue(Player.OwnedShips.Count == 0);
            string shipName = "Rocket Inquisitor";

            // test that ships are removed from empire on defeat
            SpawnShip(shipName, Player, Vector2.Zero);
            Universe.Objects.UpdateLists();
            Assert.IsTrue(Player.OwnedShips.Count == 1);
            Player.SetAsDefeated();
            Universe.Objects.UpdateLists();
            Assert.IsTrue(Player.OwnedShips.Count == 0);
        }

        [TestMethod]
        public void TestMergedEmpireShipRemoval()
        {
            Assert.IsTrue(Player.OwnedShips.Count == 0);
            string shipName = "Rocket Inquisitor";

            SpawnShip(shipName, Enemy, Vector2.Zero);
            Universe.Objects.UpdateLists();
            Assert.IsTrue(Enemy.OwnedShips.Count == 1);
            Player.AbsorbEmpire(Enemy, false);
            Universe.Objects.UpdateLists();
            // test that ship is added to empire on merge
            Assert.IsTrue(Player.OwnedShips.Count == 1);
            // test that ship is removed from target empire
            Assert.AreEqual(0, Enemy.OwnedShips.Count);
        }
        [TestMethod]
        public void AIManagedPools()
        {
            Player.GetEmpireAI().AreasOfOperations.Add(new AO(Player.Capital, 10));
            Enemy.ShipsWeCanBuild = Player.ShipsWeCanBuild;
            Player.isPlayer = false;

            // add ships one by one for easier debugging. 
            foreach (var shipName in Enemy.ShipsWeCanBuild)
            {
                var ship = SpawnShip(shipName, Enemy, Vector2.Zero);
                ship.LoyaltyChangeByGift(Player);
                Universe.Objects.UpdateLists();
                Universe.EndOfTurnUpdate(TestSimStep);
            }

            var forcePools = Player.AIManagedShips.GetShipsFromOffensePools();
            var shipsOnDefense = new Array<Ship>();
            var shipsThatCantBeAdded = new Array<Ship>();

            // filter out ships that should not be in force pool
            foreach (var ship in Player.OwnedShips)
            {
                if (ship.AI.State == AIState.SystemDefender) shipsOnDefense.Add(ship);
                if (ship.DesignRole == ShipData.RoleName.supply) shipsThatCantBeAdded.Add(ship);
                if (ship.IsPlatformOrStation) shipsThatCantBeAdded.Add(ship);
            }

            // verify counts
            int unAdded = shipsOnDefense.Count + shipsThatCantBeAdded.Count;
            Assert.AreEqual(forcePools.Count , Player.OwnedShips.Count - unAdded);
            Assert.AreEqual(shipsOnDefense.Count, 1, "Did Something change in ship system defender states?");
            Assert.AreEqual(shipsThatCantBeAdded.Count, 2,"Did something change in supply shuttles or stations");
        }

        [TestMethod]
        public void TestBudgetLoad()
        {
            var budget = new BudgetPriorities(Enemy);
            int budgetAreas = Enum.GetNames(typeof(BudgetPriorities.BudgetAreas)).Length;
            Assert.IsTrue(budget.Count() == budgetAreas);
        }
    }
}


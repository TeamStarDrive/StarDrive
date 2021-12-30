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
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Corsair",
                             "Fang Strafer",
                             "Cordrazine-Prototype",
                             "Cordrazine Troop",
                             "Platform Base mk1-a");

            CreateUniverseAndPlayerEmpire("Cordrazine");

            AddPlanetToUniverse(2, 2, 40000, true, Vector2.One);
            AddPlanetToUniverse(1.9f, 1.9f, 40000, true, new Vector2(5000));
            AddPlanetToUniverse(1.7f, 1.7f, 40000, true, new Vector2(-5000));
            for (int x = 0; x < 50; x++)
                AddPlanetToUniverse(0.1f, 0.1f, 1000, true, Vector2.One);
            AddHomeWorldToEmpire(Player, out Planet hw1);
            AddPlanetToUniverse(hw1, true, Vector2.Zero);
            AddHomeWorldToEmpire(Enemy, out Planet hw2);
            AddPlanetToUniverse(hw2, true, new Vector2(2000));

            UnlockAllShipsFor(Player);

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
        public void MilitaryPlannerShouldCreateBestKnownFighter()
        {
            var build = new RoleBuildInfo(10, Player.GetEmpireAI(), true);
            string shipName = Player.GetEmpireAI().GetAShip(build);

            // it should be random:
            if (!(shipName == "Vulcan Scout" || shipName == "Fang Strafer" || shipName == "Rocket Scout"))
                throw new AssertFailedException($"Build should have created Vulcan Scout/Fang Strafer/Rocket Scout but created: {shipName}");
        }

        [TestMethod]
        public void TestBuildCounts()
        {
            // setup Build
            var build = new RoleBuildInfo(2, Player.GetEmpireAI(), true);

            // init base variables
            var combatRole      = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(RoleName.fighter);
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
            var combatRole = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(RoleName.fighter);
            float buildCapacity = 0.75f;
            var build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), true);

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

            // The expected maintenance for the Fang Strafer is 0.144, based on the cost of the ship
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            Assert.AreEqual(0.144f, roleUnitMaint, 0.001f, "Unexpected maintenance value");

            // simulate building a bunch of ships by lowering the role build budget by the role maintenance.
            // Keep building until it starts to scrap.
            for (int x = 0; x < 20; ++x)
            {
                // reduce the budget by the role maintenance.
                buildCapacity = build.RoleBudget(combatRole) - roleUnitMaint;

                build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), ignoreDebt: false);
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

                // once we bottom out then kill all scrapping ships, reset build capacity, and add more ships
                if (buildCapacity < 0)
                {
                    foreach (Ship ship in Player.OwnedShips)
                        if (ship.AI.State == AIState.Scrap)
                            ship.Die(ship, true);

                    Universe.Objects.UpdateLists();
                    buildCapacity = roleUnitMaint * (x / 2f);
                    string shipName;
                    build = new RoleBuildInfo(buildCapacity, Player.GetEmpireAI(), false);
                    do
                    {
                        shipName = Player.GetEmpireAI().GetAShip(build);
                        if (shipName.NotEmpty())
                            SpawnShip(shipName, Player, Vector2.Zero);
                    }
                    while (shipName.NotEmpty());
                    Universe.Objects.UpdateLists();
                }
            }
        }

        [TestMethod]
        public void TestOverBudgetSpendingHigh()
        {
            // normalized money is not reset to zero
            Player.Money = 1000;
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
            Player.Money = 100;
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

            string shipName = "Fang Strafer";

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

            string shipName = "Fang Strafer";

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
                                Assert.AreEqual(s.Loyalty, empire);
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
            string shipName = "Fang Strafer";

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
            string shipName = "Fang Strafer";

            SpawnShip(shipName, Enemy, Vector2.Zero);
            Universe.Objects.UpdateLists();
            Assert.IsTrue(Enemy.OwnedShips.Count == 1);
            Player.AbsorbEmpire(Enemy);
            Universe.Objects.UpdateLists();
            // test that ship is added to empire on merge
            Assert.IsTrue(Player.OwnedShips.Count == 1);
            // test that ship is removed from target empire
            Assert.AreEqual(0, Enemy.OwnedShips.Count);
        }

        
    }
}


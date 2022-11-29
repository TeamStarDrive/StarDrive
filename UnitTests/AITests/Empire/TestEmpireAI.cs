using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires.Components;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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

            AddDummyPlanet(new(1000), 2, 2, 40000, Vector2.One, explored:true);
            AddDummyPlanet(new(1000), 1.9f, 1.9f, 40000, new(5000), explored:true);
            AddDummyPlanet(new(1000), 1.7f, 1.7f, 40000, new(-5000), explored:true);
            for (int x = 0; x < 50; x++)
                AddDummyPlanet(new(1000), 0.1f, 0.1f, 1000, Vector2.One, explored:true);
            AddHomeWorldToEmpire(new(1000), Player, Vector2.Zero, explored:true);
            AddHomeWorldToEmpire(new(1000), Enemy, new(2000), explored:true);

            UnlockAllShipsFor(Player);
            UState.Objects.UpdateLists(true);
        }

        [TestMethod]
        public void MilitaryPlannerShouldCreateBestKnownFighter()
        {
            var build = new RoleBuildInfo(10, Player.AI, true);
            string shipName = Player.AI.GetAShip(build);

            // it should be random:
            if (!(shipName == "Vulcan Scout" || shipName == "Fang Strafer" || shipName == "Rocket Scout"))
                throw new AssertFailedException($"Build should have created Vulcan Scout/Fang Strafer/Rocket Scout but created: {shipName}");
        }

        [TestMethod]
        public void TestBuildCounts()
        {
            // setup Build
            var build = new RoleBuildInfo(2, Player.AI, true);

            // init base variables
            var combatRole = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(RoleName.fighter);
            float roleBudget = build.RoleBudget(combatRole);
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            int count;

            // try to build a lot of ships. this loop should break
            for (count = 1; count < 50; count++)
            {
                string shipName = Player.AI.GetAShip(build);

                if (shipName.IsEmpty())
                {
                    bool canBuildMore = build.CanBuildMore(combatRole);
                    AssertFalse(canBuildMore, "Role says it cant build more but build process says no");
                    break;
                }

                // create the ship
                var ship = SpawnShip(shipName, Player, Vector2.Zero);
                float shipMaint = ship.GetMaintCost();
                AssertLessThan(shipMaint, roleUnitMaint, "Ship maintenance must be less than role unit maintenance");

                float currentMaint = build.RoleCurrentMaintenance(combatRole);
                AssertEqual(roleUnitMaint * count, currentMaint, "Current maintenance should equal projected");
            }

            AssertTrue(count < 49, "GetAShip failed to terminate correctly");

            AssertEqual(build.RoleCount(combatRole), (int)(roleBudget / roleUnitMaint));
        }
        
        [TestMethod]
        public void TestBuildScrap()
        {
            var combatRole = RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(RoleName.fighter);
            float buildCapacity = 0.75f;
            var build = new RoleBuildInfo(buildCapacity, Player.AI, true);

            // build 50 ships
            for (int x = 0; x < 50; x++)
            {
                // This will peak the "Rocket Inquisitor" ships since it is stronger
                string shipName = Player.AI.GetAShip(build);
                if (shipName.NotEmpty())
                    SpawnShip(shipName, Player, Vector2.Zero);
            }

            // add them to the universe
            UState.Objects.UpdateLists();

            // The expected maintenance for the Fang Strafer is 0.144, based on the cost of the ship
            float roleUnitMaint = build.RoleUnitMaintenance(combatRole);
            AssertEqual(0.001f, 0.147f, roleUnitMaint, "Unexpected maintenance value");

            // simulate building a bunch of ships by lowering the role build budget by the role maintenance.
            // Keep building until it starts to scrap.
            for (int x = 0; x < 20; ++x)
            {
                // reduce the budget by the role maintenance.
                buildCapacity = build.RoleBudget(combatRole) - roleUnitMaint;

                build = new RoleBuildInfo(buildCapacity, Player.AI, ignoreDebt: false);
                // this is the actual number of ships to build with available budget.
                int roleCountWanted    = build.RoleCountDesired(combatRole);
                // this is the formula used to determine the number of ships that can be built with available budget.
                int expectedBuildCount = (int)Math.Ceiling(build.RoleBudget(combatRole) / roleUnitMaint);

                // test that formula for building ships matches the actual building ships process.
                AssertEqual(expectedBuildCount, roleCountWanted, $"{combatRole}: expected number of ships to build did not match actual");

                float currentMain      = build.RoleCurrentMaintenance(combatRole);
                int shipsBeingScrapped = Player.OwnedShips.Filter(s => s.AI.State == AIState.Scrap).Length;

                // now make sure that the maintenance and budgets dont create a scrap loop.
                if (currentMain + roleUnitMaint > buildCapacity)
                {
                    string shipName = Player.AI.GetAShip(build);
                    AssertTrue(shipName.IsEmpty(), $"Current maintenance {currentMain}" +
                                                      $" + new ship maintenance {roleUnitMaint} was greater than build cap {buildCapacity}" +
                                                      $" but we still built a ship.");
                }
                else
                {
                    AssertFalse(build.RoleIsScrapping(combatRole), "We have build budget and we are set to scrap");
                    AssertTrue(shipsBeingScrapped <= 0, $"We have build budget and we have ships scrapping: {shipsBeingScrapped}");
                    string shipName = Player.AI.GetAShip(build);
                    AssertTrue(shipName.NotEmpty(), "We have build budget but we aren't building");
                }

                // once we bottom out then kill all scrapping ships, reset build capacity, and add more ships
                if (buildCapacity < 0)
                {
                    foreach (Ship ship in Player.OwnedShips)
                        if (ship.AI.State == AIState.Scrap)
                            ship.Die(ship, true);

                    UState.Objects.UpdateLists();
                    buildCapacity = roleUnitMaint * (x / 2f);
                    string shipName;
                    build = new RoleBuildInfo(buildCapacity, Player.AI, false);
                    do
                    {
                        shipName = Player.AI.GetAShip(build);
                        if (shipName.NotEmpty())
                            SpawnShip(shipName, Player, Vector2.Zero);
                    }
                    while (shipName.NotEmpty());
                    UState.Objects.UpdateLists();
                }
            }
        }

        [TestMethod]
        public void TestOverBudgetSpendingHigh()
        {
            // normalized money is not reset to zero
            Player.Money = 1000;

            for (int x = -1; x < 11; x++)
            {
                float percent = x * 0.1f;
                float overSpend = Player.AI.OverSpendRatio(1000, percent, 10f);
                percent = 2 - percent;
                AssertEqual(overSpend, percent);
            }
        }

        [TestMethod]
        public void TestOverBudgetSpendingLow()
        {
            // normalized money is not reset to zero
            Player.Money = 100;
            for (int x = -1; x < 1; x++)
            {
                float percent = x * 0.05f;
                float overSpend = Player.AI.OverSpendRatio(1000, percent, 10f);
                percent = 0.2f - percent;
                AssertEqual(overSpend, percent);
            }
        }

        [TestMethod]
        public void TestShipListTracking()
        {
            AssertEqual(0, Player.OwnedShips.Count);
            IEmpireShipLists playerShips = Player;

            string shipName = "Fang Strafer";

            // test that ship is added to empire on creation
            var ship = SpawnShip(shipName, Player, Vector2.Zero);
            UState.Objects.UpdateLists(true);
            AssertEqual(1, Player.OwnedShips.Count);

            // test that removed ship removes the ship from ship list
            playerShips.RemoveShipAtEndOfTurn(ship);
            UState.Objects.UpdateLists(true);
            AssertEqual(0, Player.OwnedShips.Count);

            // test that a ship added to empire directly is added.
            playerShips.AddNewShipAtEndOfTurn(ship);
            UState.Objects.UpdateLists(true);
            AssertEqual(1, Player.OwnedShips.Count);

            // test that a ship cant be added twice
            // debugwin will enable error checking
            Universe.ToggleDebugWindow();
            playerShips.AddNewShipAtEndOfTurn(ship);
            UState.Objects.UpdateLists(true);
            AssertEqual(1, Player.OwnedShips.Count);
            Universe.ToggleDebugWindow();

            // test that removing the same ship twice doesn't fail.
            playerShips.RemoveShipAtEndOfTurn(ship);
            AssertEqual(1, Player.OwnedShips.Count);
            UState.Objects.UpdateLists(true);
            playerShips.RemoveShipAtEndOfTurn(ship);
            UState.Objects.UpdateLists(true);
            AssertEqual(0, Player.OwnedShips.Count);
        }

        [TestMethod]
        public void ShipListConcurrencyStressTest()
        {
            AssertEqual(0, Enemy.OwnedShips.Count);

            // create areas of operation among empires
            foreach(var empire in UState.Empires)
            {
                empire.data.Defeated = false;
                foreach(var planet in UState.Planets)
                {
                    if (RandomMath.RollDice(50))
                    {
                        planet.SetOwner(empire);
                        empire.AI.AreasOfOperations.Add(new AO(UState, Enemy.Capital, Enemy, 10));
                    }
                }
            }

            string shipName = "Fang Strafer";

            // create a base number of ships.
            for (int x=0;x< 100; ++x)
            {
                SpawnShip(shipName, Enemy, Vector2.Zero);
            }
            Universe.InvokePendingSimThreadActions();
            UState.Objects.UpdateLists();

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

                    foreach(var empire in UState.Empires)
                    {
                        foreach (var s in empire.OwnedShips)
                        {
                            if (s.Active)
                            {
                                AssertEqual(s.Loyalty, empire);
                                float random = RandomMath.AvgInt(1, 100);
                                if (random > 80)
                                {
                                    s.RemoveFromUniverseUnsafe();
                                    shipsRemoved++;
                                }
                                else if (random > 60)
                                {
                                    var changeTo = UState.Empires.Find(e => e != empire);
                                    s.LoyaltyChangeFromBoarding(changeTo,false);
                                    loyaltyChanges++;
                                }
                            }

                        }
                    }

                    addedShips = RandomMath.Int(1, 30);

                    Parallel.For(0, UState.NumEmpires, (firstEmpire, lastEmpire) =>
                        {
                            for (int e = firstEmpire; e < lastEmpire; e++)
                            {
                                var empire = UState.Empires[e];
                                for (int y = 0; y < addedShips; ++y)
                                {
                                    SpawnShip(shipName, empire, Vector2.Zero);
                                }
                            }
                        }
                    );
                    UState.Objects.UpdateLists(true);
                    numberOfShips += addedShips * 2;
                }
            }
            stopStress = true;
            stressTask.CancelAndWait();

            int actualShipCount = 0;
            foreach(var empire in UState.Empires)
            {
                UState.Objects.UpdateLists(true);
                actualShipCount += empire.OwnedShips.Count;
            }

            numberOfShips -= shipsRemoved;
            AssertEqual(numberOfShips, actualShipCount);
            Log.Info($"loyalty Changes: {loyaltyChanges} Removed Ships: {shipsRemoved} Active Ships: {actualShipCount}");

            Enemy.data.IsRebelFaction = true;
        }

        private int BackGroundPoolStress(ref bool stopStress)
        {
            int removedShips = 0;
            while (!stopStress)
            {
                foreach (var empire in UState.Empires)
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
            AssertEqual(0, Player.OwnedShips.Count);
            string shipName = "Fang Strafer";

            // test that ships are removed from empire on defeat
            SpawnShip(shipName, Player, Vector2.Zero);
            UState.Objects.UpdateLists();
            AssertEqual(1, Player.OwnedShips.Count);
            Player.SetAsDefeated();
            UState.Objects.UpdateLists();
            AssertEqual(0, Player.OwnedShips.Count);
        }

        [TestMethod]
        public void TestMergedEmpireShipRemoval()
        {
            AssertEqual(0, Player.OwnedShips.Count);
            string shipName = "Fang Strafer";

            SpawnShip(shipName, Enemy, Vector2.Zero);
            UState.Objects.UpdateLists();
            AssertEqual(1, Enemy.OwnedShips.Count);
            Player.AbsorbEmpire(Enemy);
            UState.Objects.UpdateLists();
            // test that ship is added to empire on merge
            AssertEqual(1, Player.OwnedShips.Count);
            // test that ship is removed from target empire
            AssertEqual(0, Enemy.OwnedShips.Count);
        }
    }
}


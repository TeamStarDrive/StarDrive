using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Fleets
{
    [TestClass]
    public class FleetTests : StarDriveTest
    {
        Array<Ship> PlayerShips = new Array<Ship>();
        Array<Ship> EnemyShips  = new Array<Ship>();
        Array<Fleet> PlayerFleets = new Array<Fleet>();
        public FleetTests()
        {
            CreateGameInstance();

            // Excalibur class has all the bells and whistles
            LoadStarterShips(new[] { "Excalibur-Class Supercarrier", "Corsair", "Supply Shuttle" });
            CreateUniverseAndPlayerEmpire(out Empire empire);
            CreateTestEnv();
        }

        void CreateTestEnv()
        {
            PlayerShips.ClearAndDispose();
            EnemyShips.ClearAndDispose();
            PlayerFleets.ClearAndDispose();
            PlayerShips  = new Array<Ship>();
            EnemyShips   = new Array<Ship>();
            PlayerFleets = new Array<Fleet>();
        }

        Ship CreatePlayerShip(string shipName, Vector2 pos)
        {
            var ship = Ship.CreateShipAtPoint(shipName, Player, pos);
            ship.SetSystem(null);
            return ship;
        }

        void CreateWantedShipsAndAddThemToList(int numberWanted, string shipName, Array<Ship> shipList)
        {
            for (int i =0; i < numberWanted; i++)
            {
                shipList.Add(CreatePlayerShip(shipName, Vector2.Zero));
            }
        }

        void CreateTestFleet(Array<Ship> ships, Array<Fleet> fleets)
        {
            var fleet = new Fleet();
            foreach(var ship in ships)
            {
                fleet.AddShip(ship);
            }
            fleets.Add(fleet);
        }

        /// <summary>
        /// BVTs the fleet.
        /// </summary>
        [TestMethod]
        public void TestFleetAssembly()
        {
            CreateTestEnv();
            CreateWantedShipsAndAddThemToList(10, "Excalibur-Class Supercarrier", PlayerShips);
            CreateTestFleet(PlayerShips, PlayerFleets);
            var fleet = PlayerFleets[0];
            
            // verify fleet created and has the expected ships
            Assert.IsNotNull(fleet, "Fleet failed to create");
            Assert.AreEqual(10, fleet.CountShips,$"Expected 10 ships in fleet got {fleet.CountShips}");

            // TestFleet assembly
            fleet.AutoArrange();

            int flankCount     = fleet.AllFlanks.Count;
            Assert.AreEqual(5, flankCount, $" expected 5 flanks got{flankCount}");
            var flanks         = fleet.AllFlanks;
            int squadCount = flanks.Sum(sq => sq.Count);
            Assert.AreEqual(3, squadCount, $"Expected 3 squads got {squadCount}");
            int squadShipCount = flanks.Sum(sq => sq.Sum(s=> s.Ships.Count));
            Assert.AreEqual(10, squadShipCount, $"Expected 10 ships in fleet got {squadShipCount}");

        }

        [TestMethod]
        public void TestFleetCreationNodes()
        {
            CreateTestEnv();
            CreateWantedShipsAndAddThemToList(10, "Excalibur-Class Supercarrier", PlayerShips);
            foreach (var ship in PlayerShips)
            {
                ship.AI.CombatState = CombatState.Artillery;
            }

            CreateTestFleet(PlayerShips, PlayerFleets);
            var fleet = PlayerFleets[0];
            fleet.SetCommandShip(null);
            fleet.Update(FixedSimTime.Zero/*paused during init*/);
            fleet.AssembleFleet(Vector2.Zero,)
            fleet.AutoArrange();
            foreach (var ship in PlayerShips)
            {
                Assert.IsFalse(ship.RelativeFleetOffset == Vector2.Zero);
                Assert.IsFalse(ship.FleetOffset == Vector2.Zero);
                Assert.IsFalse(ship.AI.FleetNode.FleetOffset == Vector2.Zero);
            }
            
            

        }
    }
}

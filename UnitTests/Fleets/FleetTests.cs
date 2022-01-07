using System;
using System.Collections.Generic;
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
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Heavy Carrier mk5-b", "Corsair", "Terran-Prototype");
            CreateUniverseAndPlayerEmpire();
        }

        Ship CreatePlayerShip(string shipName, Vector2 pos)
        {
            return SpawnShip(shipName, Player, pos);
        }

        void CreateWantedShipsAndAddThemToList(int numberWanted, string shipName, Array<Ship> shipList)
        {
            for (int i =0; i < numberWanted; i++)
            {
                shipList.Add(CreatePlayerShip(shipName, Vector2.Zero));
            }
        }

        Fleet CreateTestFleet(Array<Ship> ships, Array<Fleet> fleets)
        {
            var fleet = new Fleet {Owner = ships[0].Loyalty};
            foreach(var ship in ships)
            {
                fleet.AddShip(ship);
            }
            fleets.Add(fleet);
            return fleet;
        }

        [TestMethod]
        public void FleetAssemblesIntoFlanksAndSquads()
        {
            CreateWantedShipsAndAddThemToList(10, "Heavy Carrier mk5-b", PlayerShips);
            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);

            // verify fleet created and has the expected ships
            Assert.AreEqual(10, fleet.CountShips, $"Expected 10 ships in fleet but got {fleet.CountShips}");

            fleet.AutoArrange();

            int flankCount = fleet.AllFlanks.Count;
            Assert.AreEqual(5, flankCount, $"Expected 5 flanks got {flankCount}");

            Array<Array<Fleet.Squad>> flanks = fleet.AllFlanks;
            int squadCount = flanks.Sum(sq => sq.Count);
            Assert.AreEqual(3, squadCount, $"Expected 3 squads got {squadCount}");

            int squadShipCount = flanks.Sum(sq => sq.Sum(s=> s.Ships.Count));
            Assert.AreEqual(10, squadShipCount, $"Expected 10 ships in fleet got {squadShipCount}");
        }

        [TestMethod]
        public void FleetArrangesIntoNonZeroOffsets()
        {
            CreateWantedShipsAndAddThemToList(10, "Heavy Carrier mk5-b", PlayerShips);
            foreach (var ship in PlayerShips)
            {
                ship.AI.CombatState = CombatState.Artillery;
            }

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.SetCommandShip(null);
            fleet.Update(FixedSimTime.Zero/*paused during init*/);
            fleet.AutoArrange();
            foreach (var ship in PlayerShips)
            {
                Assert.IsTrue(ship.RelativeFleetOffset != Vector2.Zero, $"Ship RelativeFleetOffset must not be Zero: {ship}");
                Assert.IsTrue(ship.FleetOffset != Vector2.Zero, $"Ship FleetOffset must not be Zero: {ship}");
            }
        }

        [TestMethod]
        public void FleetIsAbleToWarpMove()
        {
            CreateWantedShipsAndAddThemToList(3, "Terran-Prototype", PlayerShips);
            CreateWantedShipsAndAddThemToList(200, "Vulcan Scout", PlayerShips);
            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.AutoArrange();

            // assign a warp move command forward by 40k
            // 
            // if Fleet Warp works correctly, all ships should enter Warp within 5 seconds
            var forward = PlayerShips[0].Direction;
            var target = PlayerShips[0].Position + forward*40_000f;
            fleet.MoveToNow(target, forward);

            var shipsThatWereInWarp = new HashSet<Ship>();
            Universe.Objects.EnableParallelUpdate = false;

            LoopWhile((timeout:5.0, fatal:false), () => fleet.Ships.Any(s => !s.IsInWarp), () =>
            {
                foreach (Ship s in fleet.Ships)
                    if (s.IsInWarp) shipsThatWereInWarp.Add(s);
                RunObjectsSim(TestSimStep);
            });

            LoopWhile((timeout:1.0, fatal:false), () => true, () =>
            {
                RunObjectsSim(TestSimStep);
            });

            if (shipsThatWereInWarp.Count != fleet.Ships.Count)
            {
                var notInWarp = new Array<Ship>(fleet.Ships.Except(shipsThatWereInWarp));
                for (int i = 0; i < notInWarp.Count && i < 10; ++i)
                {
                    Ship s = notInWarp[i];
                    Log.Write($"dist={target.Distance(s.Position)} maxSpeed={s.SpeedLimit} vel={s.Velocity.Length()} state={s.ShipEngines}\n\t\t\t\t\t{s}");
                }
                Assert.Fail($"{notInWarp.Count} fleet ships did not enter warp!");
            }

            foreach (Ship s in shipsThatWereInWarp)
            {
                float distance = target.Distance(s.Position);
                if (distance > 7500+500)
                {
                    Log.Write($"dist={distance} maxSpeed={s.SpeedLimit} vel={s.Velocity.Length()} state={s.ShipEngines}\n\t\t\t\t\t{s}");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
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
            for (int i = 0; i < numberWanted; i++)
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

        Fleet CreateMassivePlayerFleet(Vector2 initialDir)
        {
            CreateWantedShipsAndAddThemToList(5, "Terran-Prototype", PlayerShips);
            CreateWantedShipsAndAddThemToList(195, "Vulcan Scout", PlayerShips);
            foreach (Ship s in PlayerShips)
                s.Direction = initialDir;

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.AutoArrange();
            return fleet;
        }

        Vector2 FleetMoveTo(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Log.Write($"Fleet.MoveToNow({target.X},{target.Y})");
            fleet.MoveToNow(target, finalDirection: offset.Normalized());
            return target;
        }

        Vector2 FleetQueueMoveOrder(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Log.Write($"Fleet.QueueMoveOrder({target.X},{target.Y})");
            fleet.FormationWarpTo(target, finalDirection: offset.Normalized(), queueOrder: true);
            return target;
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveUp()
        {
            // move up
            var offset = new Vector2(0, -40_000f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveLeft()
        {
            // move left
            var offset = new Vector2(-40_000f, 0f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveDown()
        {
            // move down
            var offset = new Vector2(0, 40_000f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveRight()
        {
            // move right
            var offset = new Vector2(40_000f, 0);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        Fleet CreateRandomizedPlayerFleet(int randomSeed)
        {
            Log.Write($"RandomizedFleet seed={randomSeed}");
            CreateWantedShipsAndAddThemToList(5, "Terran-Prototype", PlayerShips);
            CreateWantedShipsAndAddThemToList(195, "Vulcan Scout", PlayerShips);

            // scatter the ships
            var random = new ThreadSafeRandom();
            foreach (Ship s in PlayerShips)
            {
                s.Direction = random.Direction2D();
                s.Position = random.Vector2D(2000);
            }

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.AutoArrange();
            return fleet;
        }

        [TestMethod]
        public void FleetCanAssembleAndFormationWarp()
        {
            Fleet fleet = CreateRandomizedPlayerFleet(12345);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down); // assemble the fleet in distance

            // order it to warp forward at an angle
            var finalTarget = FleetMoveTo(fleet, new Vector2(50_000, 50_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 30.0);
        }

        [TestMethod]
        public void FleetCanAssembleAndFormationWarpToMultipleWayPoints()
        {
            Fleet fleet = CreateRandomizedPlayerFleet(12345);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down); // assemble the fleet in distance

            // order it to warp forward at an angle
            FleetMoveTo(fleet, new Vector2(50_000, 50_000));
            // and then queue up another WayPoint to the fleet
            var finalTarget = FleetQueueMoveOrder(fleet, new Vector2(-20_000, 40_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 30.0);
        }

        void AssertAllShipsWarpedToTarget(Fleet fleet, Vector2 target, double simTimeout)
        {
            var shipsThatWereInWarp = new HashSet<Ship>();
            RunSimWhile((simTimeout, fatal:false), body:() =>
            {
                foreach (Ship s in fleet.Ships)
                    if (s.IsInWarp) shipsThatWereInWarp.Add(s);
            });

            float Dist(Ship s) => s.Position.Distance(target);
            void Print(string wat, Ship s)
            {
                Log.Write($"{wat} dist:{Dist(s)} V:{s.Velocity.Length()} Vmax:{s.SpeedLimit}");
                Log.Write($"\t\t\tstate:{s.engineState} {s.ShipEngines}");
                Log.Write($"\t\t\t{s}");
            }

            var didWarp = shipsThatWereInWarp.ToArray().Sorted(s => s.Id);
            if (didWarp.Length != fleet.Ships.Count)
            {
                var notInWarp = fleet.Ships.Except(didWarp);
                string error = $"{notInWarp.Length} fleet ships did not enter warp!";
                Log.Write(error);
                for (int i = 0; i < notInWarp.Length && i < 10; ++i)
                    Print("DID_NOT_WARP", notInWarp[i]);
                Assert.Fail(error);
            }

            var didNotArrive = didWarp.Filter(s => Dist(s) > 7500+500);
            if (didNotArrive.Length != 0)
            {
                string error = $"{didNotArrive.Length} fleet ships did not arrive at destination!";
                Log.Write(error);
                for (int i = 0; i < didNotArrive.Length && i < 10; ++i)
                    Print("DID_NOT_ARRIVE", didNotArrive[i]);
                Assert.Fail(error);
            }

            Log.Write("All ships arrived at destination");
        }
    }
}

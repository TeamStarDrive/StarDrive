using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets
{
    [TestClass]
    public class FleetTests : StarDriveTest
    {
        Array<Ship> PlayerShips = new();
        Array<Ship> EnemyShips  = new();
        Array<Fleet> PlayerFleets = new();

        public FleetTests()
        {
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Heavy Carrier mk5-b", "Corsair", "Terran-Prototype");
            CreateUniverseAndPlayerEmpire();
            //Universe.UState.Objects.EnableParallelUpdate = false;
            UState.Events.Disabled = true;
        }

        void CreateWantedShipsAndAddThemToList(int numberWanted, string shipName, Array<Ship> shipList)
        {
            for (int i = 0; i < numberWanted; i++)
            {
                var ship = SpawnShip(shipName, Player, Vector2.Zero);
                shipList.Add(ship);
                if (ship.MaxFTLSpeed < Ship.LightSpeedConstant)
                    throw new InvalidOperationException($"Invalid FleetTest ship: '{ship.Name}' CANNOT WARP! MaxFTLSpeed: {ship.MaxFTLSpeed:0}");
            }
        }

        Fleet CreateTestFleet(Array<Ship> ships, Array<Fleet> fleets)
        {
            Fleet fleet = ships[0].Loyalty.CreateFleet(1, null);
            fleet.AddShips(ships);
            fleets.Add(fleet);
            return fleet;
        }

        [TestMethod]
        public void FleetAssemblesIntoFlanksAndSquads()
        {
            CreateWantedShipsAndAddThemToList(10, "Heavy Carrier mk5-b", PlayerShips);
            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);

            // verify fleet created and has the expected ships
            AssertEqual(10, fleet.CountShips, $"Expected 10 ships in fleet but got {fleet.CountShips}");

            fleet.AutoArrange();

            int flankCount = fleet.AllFlanks.Count;
            AssertEqual(5, flankCount, $"Expected 5 flanks got {flankCount}");

            Array<Array<Fleet.Squad>> flanks = fleet.AllFlanks;
            int squadCount = flanks.Sum(sq => sq.Count);
            AssertEqual(3, squadCount, $"Expected 3 squads got {squadCount}");

            int squadShipCount = flanks.Sum(sq => sq.Sum(s=> s.Ships.Count));
            AssertEqual(10, squadShipCount, $"Expected 10 ships in fleet got {squadShipCount}");
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

        Fleet ArrangedFleet(int key, params (string Name, int Count)[] parts)
        {
            var ships = new Array<Ship>();
            foreach ((string name, int count) in parts)
            {
                for (int i = 0; i < count; ++i)
                    ships.Add(SpawnShip(name, Player, new Vector2(200_000f * key, 200_000f)));
            }

            Fleet fleet = Player.CreateFleet(key, null);
            fleet.AddShips(ships);
            fleet.AutoArrange();
            return fleet;
        }

        // prototypes go to the screen, carriers to the rear, corsairs to the center and scouts to the wings
        Fleet ScreenedFleet(int key) => ArrangedFleet(key, ("Terran-Prototype", 40), ("Vulcan Scout", 60));
        Fleet FleetWithACenterAndRear(int key) => ArrangedFleet(key, ("Heavy Carrier mk5-b", 21), ("Corsair", 30), ("Vulcan Scout", 41));

        [TestMethod]
        public void AFleetArrangedAgainKeepsTheSameFormation()
        {
            Fleet screened = ScreenedFleet(2);
            Fleet centerAndRear = FleetWithACenterAndRear(3);
            Assert.IsTrue(screened.ScreenFlank.Count > 9 && centerAndRear.CenterFlank.Count > 5 && centerAndRear.RearFlank.Count > 0,
                          $"setup: a screen row of 9 squads and a center row of 5 are full, with rear squads"
                          + $" (screen {screened.ScreenFlank.Count}, center {centerAndRear.CenterFlank.Count}, rear {centerAndRear.RearFlank.Count})");

            foreach (Fleet fleet in new[] { screened, centerAndRear })
            {
                Ship[] ships = fleet.Ships.ToArray();
                var first = new Vector2[ships.Length];
                for (int i = 0; i < ships.Length; ++i)
                    first[i] = ships[i].FleetOffset;

                fleet.AutoArrange();

                for (int i = 0; i < ships.Length; ++i)
                    Assert.IsTrue(ships[i].FleetOffset.InRadius(first[i], 1f),
                                  $"{ships[i]} moved in the formation from {first[i]} to {ships[i].FleetOffset}");
            }
        }

        // a few corsairs in the center, so the long wings reach back beside the wide rear of carriers
        Fleet FleetWithAShallowCenter(int key) => ArrangedFleet(key, ("Heavy Carrier mk5-b", 20), ("Corsair", 5), ("Vulcan Scout", 121));

        // the hulls of two ships, each around its place in the formation, overlap
        static bool ShipsOverlap(Ship a, Ship b)
        {
            Vector2 apart = a.RelativeFleetOffset - b.RelativeFleetOffset;
            return Math.Abs(apart.X) < (a.GridWidth + b.GridWidth) * 8f - 1f
                && Math.Abs(apart.Y) < (a.GridHeight + b.GridHeight) * 8f - 1f;
        }

        static Map<Ship, Fleet.Squad> SquadOfEachShip(Fleet fleet)
        {
            var squads = new Map<Ship, Fleet.Squad>();
            foreach (Array<Fleet.Squad> flank in fleet.AllFlanks)
                foreach (Fleet.Squad squad in flank)
                    foreach (Ship ship in squad.Ships)
                        squads[ship] = squad;
            return squads;
        }

        static void AssertNoOverlap(Fleet fleet, Array<Fleet.Squad> flank, string what)
        {
            Map<Ship, Fleet.Squad> squads = SquadOfEachShip(fleet);
            foreach (Fleet.Squad squad in flank)
                foreach (Ship ship in squad.Ships)
                    foreach (Ship other in fleet.Ships)
                        Assert.IsTrue(squads[other] == squad || !ShipsOverlap(ship, other),
                                      $"{what} {ship.Name} at {ship.RelativeFleetOffset} overlaps {other.Name} at {other.RelativeFleetOffset}");
        }

        [TestMethod]
        public void EveryWingShipHasAPlaceOfItsOwn()
        {
            Fleet shallow = FleetWithAShallowCenter(4);
            bool reachesTheRear = false;
            foreach (Fleet.Squad wing in shallow.LeftFlank)
                foreach (Fleet.Squad rear in shallow.RearFlank)
                    reachesTheRear |= Math.Abs(wing.Offset.Y - rear.Offset.Y) < (wing.GetSquadSize().Y + rear.GetSquadSize().Y) * 0.5f;
            Assert.IsTrue(reachesTheRear, "setup: a row of the left wing is level with rear squads");

            foreach (Fleet fleet in new[] { ScreenedFleet(2), FleetWithACenterAndRear(3), shallow })
            {
                Assert.IsTrue(fleet.LeftFlank.Count > 4 && fleet.RightFlank.Count > 4,
                              $"setup: wings of more than one row (left {fleet.LeftFlank.Count}, right {fleet.RightFlank.Count})");
                foreach (Fleet.Squad squad in fleet.LeftFlank)
                    Assert.IsTrue(squad.Offset.X < 0f, $"a left wing squad at {squad.Offset} is on the right");
                foreach (Fleet.Squad squad in fleet.RightFlank)
                    Assert.IsTrue(squad.Offset.X > 0f, $"a right wing squad at {squad.Offset} is on the left");

                AssertNoOverlap(fleet, fleet.LeftFlank, "the left wing ship");
                AssertNoOverlap(fleet, fleet.RightFlank, "the right wing ship");
            }
        }

        [TestMethod]
        public void TheRearStandsBehindTheCenter()
        {
            Fleet fleet = FleetWithACenterAndRear(5);
            Assert.IsTrue(fleet.CenterFlank.Count > 5 && fleet.RearFlank.Count > 1,
                          $"setup: center rows and rear squads (center {fleet.CenterFlank.Count}, rear {fleet.RearFlank.Count})");

            foreach (Fleet.Squad rear in fleet.RearFlank)
                foreach (Fleet.Squad center in fleet.CenterFlank)
                    Assert.IsTrue(rear.Offset.Y - rear.GetSquadSize().Y * 0.5f >= center.Offset.Y + center.GetSquadSize().Y * 0.5f - 1f,
                                  $"the rear squad at {rear.Offset} does not start behind the center squad at {center.Offset}");

            AssertNoOverlap(fleet, fleet.RearFlank, "the rear ship");
        }

        Vector2 FleetMoveTo(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Vector2 finalDir = offset.Normalized();
            Log.Write($"Fleet.MoveToNow({target.X},{target.Y})");
            fleet.MoveTo(target, finalDir, MoveOrder.Regular);
            return target;
        }

        Vector2 FleetQueueMoveOrder(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Vector2 finalDir = offset.Normalized();
            Log.Write($"Fleet.QueueMoveOrder({target.X},{target.Y})");
            fleet.MoveTo(target, finalDir, MoveOrder.AddWayPoint);
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

        Fleet CreateRandomizedPlayerFleet(int randomSeed, bool bigShips)
        {
            Log.Write($"RandomizedFleet seed={randomSeed}");
            if (bigShips)
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
            Fleet fleet = CreateRandomizedPlayerFleet(12345, bigShips:true);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down, forceAssembly:true); // assemble the fleet in distance

            // order it to warp forward at an angle
            var finalTarget = FleetMoveTo(fleet, new Vector2(50_000, 50_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 30.0);
        }

        [TestMethod]
        public void FleetCanAssembleAndFormationWarpToMultipleWayPoints()
        {
            Fleet fleet = CreateRandomizedPlayerFleet(12345, bigShips:false);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down, forceAssembly:true); // assemble the fleet in distance

            // order it to warp forward at an angle
            FleetMoveTo(fleet, new Vector2(20_000, 20_000));
            // and then queue up another WayPoint to the fleet
            var finalTarget = FleetQueueMoveOrder(fleet, new Vector2(-20_000, 20_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 120.0);
        }

        void AssertAllShipsWarpedToTarget(Fleet fleet, Vector2 target, double simTimeout)
        {
            var shipsThatWereInWarp = new HashSet<Ship>();
            const float arrivalDist = 8000;

            // run while some ships are still not within arrivalDist
            RunSimWhile((simTimeout, fatal:false), () => fleet.Ships.Any(s => Dist(s) > arrivalDist), body:() =>
            {
                fleet.Update(TestSimStep);
                foreach (Ship s in fleet.Ships)
                    if (s.IsInWarp) shipsThatWereInWarp.Add(s);
            });

            float Dist(Ship s)
            {
                return s.Position.Distance(target + s.FleetOffset);
            }

            void Print(string wat, Ship s)
            {
                Log.Write($"{wat} dist:{Dist(s):0} V:{s.Velocity.Length():0} STLCap:{s.STLSpeedLimit:0} FTLCap:{s.FTLSpeedLimit:0} Vmax:{s.VelocityMax:0}");
                Log.Write($"\t\t\tstate:{s.engineState} Goal:{s.AI.OrderQueue.PeekFirst?.Plan} {s.ShipEngines}");
                Log.Write($"\t\t\t{s}");
            }

            var didWarp = shipsThatWereInWarp.ToArr().Sorted(s => s.Id).Sorted(s => (int)s.ShipEngines.ReadyForFormationWarp);
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

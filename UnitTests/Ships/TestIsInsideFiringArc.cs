using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestIsInsideFiringArcs : StarDriveTest
    {
        public static bool EnableVisualization = false;

        public TestIsInsideFiringArcs()
        {
            LoadStarterShips("Flak Fang");
            CreateGameInstance();
            CreateUniverseAndPlayerEmpire(out _);
        }

        class ArcVisualization : TestGameComponent
        {
            readonly string What;
            readonly Ship Ship;
            readonly Weapon Wep;
            readonly Vector2 Point;
            readonly bool Expected;
            public ArcVisualization(string what, Ship ship, Weapon wep, Vector2 point, bool expected)
            {
                What = what;
                Ship = ship;
                Wep = wep;
                Point = point;
                Expected = expected;
            }

            public override void Draw(SpriteBatch batch)
            {
                Vector2 c = Game.Manager.ScreenCenter;
                ShipModule m = Wep.Module;
                Vector2 mc = c + m.Center;
                float facing = (Ship.Rotation + m.FacingRadians);
                Vector2 md = facing.RadiansToDirection();
                batch.DrawCircle(mc, m.Radius, Color.Yellow, 2);
                batch.DrawLine(mc, mc + md*m.Radius, Color.Yellow, 2);

                bool inArc = Ship.IsInsideFiringArc(Wep, Point);
                Color color = inArc == Expected ? Color.Green : Color.Red;
                batch.DrawCircle(c + Point, 10, color, 2);

                Vector2 left = (facing - m.FieldOfFire * 0.5f).RadiansToDirection();
                Vector2 right = (facing + m.FieldOfFire * 0.5f).RadiansToDirection();
                batch.DrawLine(mc, mc + left * 500, Color.Orange);
                batch.DrawLine(mc, mc + right * 500, Color.Orange);
                
                Game.DrawText(5, 10, What);
                Game.DrawText(5, 30, $" InsideArc: {inArc}", color);
                Game.DrawText(5, 50, $" Expected:  {Expected}", color);
                Game.DrawText(5, 70, GetDescription(Ship, Wep, Point));
            }
        }

        static string GetDescription(Ship ship, Weapon w, Vector2 point)
        {
            ShipModule m = w.Module;
            float sr = ship.Rotation;
            float mf = m.FacingRadians;
            float gf = sr + mf;
            float ff = m.FieldOfFire;
            return $"ship position: {ship.Center}\n"+
                   $"ship rotation: {sr.String(3)} rads {sr.ToDegrees().String(3)} degrees\n"+
                   $"module pos:    {m.Center}\n"+
                   $"module local  facing: {mf.String(3)} rads {mf.ToDegrees().String(3)} degrees\n"+
                   $"module global facing: {gf.String(3)} rads {gf.ToDegrees().String(3)} degrees\n"+
                   $"module arc:    {ff.String(3)} rads {ff.ToDegrees().String(3)} degrees\n"+
                   $"target point:  {point}\n";
        }

        void CheckArc(string what, Ship ship, Weapon w, float facing, float fireArc, Vector2 point, bool expectedResult)
        {
            w.Module.Center = w.Module.Position = Vector2.Zero;
            w.Module.FacingDegrees = facing;
            w.Module.FieldOfFire = fireArc.ToRadians();
            bool result = ship.IsInsideFiringArc(w, point);
            if (EnableVisualization && result != expectedResult)
            {
                var vis = new ArcVisualization(what, ship, w, point, expectedResult);
                Game.ShowAndRun(vis);
            }

            string description = $"{what} \n" + GetDescription(ship, w, point);
            if (expectedResult) Assert.IsTrue(result, description);
            else                Assert.IsFalse(result, description);
        }

        void InsideFiringArc(Ship ship, Weapon w, float facing, float fireArc, Vector2 point)
        {
            CheckArc("InsideFiringArc", ship, w, facing, fireArc, point, expectedResult:true);
        }
        
        void OutsideFiringArc(Ship ship, Weapon w, float facing, float fireArc, Vector2 point)
        {
            CheckArc("OutsideFiringArc", ship, w, facing, fireArc, point, expectedResult:false);
        }

        void SetShipPosAndFacing(Ship ship, Vector2 center, float rotation)
        {
            ship.Center = ship.Position = center;
            ship.RotationDegrees = rotation;
        }

        [TestMethod]
        public void ShipFiringArc()
        {
            Ship ship = SpawnShip("Flak Fang", Player, Vector2.Zero);
            Weapon w = ship.Weapons.First;
            
            const int fireArc = 90;
            void Inside(float f, float x, float y)  =>  InsideFiringArc(ship, w, f, fireArc, new Vector2(x,y));
            void Outside(float f, float x, float y) => OutsideFiringArc(ship, w, f, fireArc, new Vector2(x,y));

            // Up
            Inside(f:0, x:0, y:-100);
            Inside(f:0, x:-95, y:-100);
            Inside(f:0, x:+95, y:-100);
            Outside(f:0, x:-105, y:-100);
            Outside(f:0, x:+105, y:-100);

            // Up (360)
            Inside(f:360, x:0, y:-100);
            Inside(f:360, x:-95, y:-100);
            Inside(f:360, x:+95, y:-100);
            Outside(f:360, x:-105, y:-100);
            Outside(f:360, x:+105, y:-100);

            // Right
            Inside(f:90, x:+100, y:0);
            Inside(f:90, x:+100, y:-95);
            Inside(f:90, x:+100, y:+95);
            Outside(f:90, x:+100, y:-105);
            Outside(f:90, x:+100, y:+105);

            // Right (negative facing)
            Inside(f:-270, x:+100, y:0);
            Inside(f:-270, x:+100, y:-95);
            Inside(f:-270, x:+100, y:+95);
            Outside(f:-270, x:+100, y:-105);
            Outside(f:-270, x:+100, y:+105);

            // Left
            Inside(f:270, x:-100, y:0);
            Inside(f:270, x:-100, y:-95);
            Inside(f:270, x:-100, y:+95);
            Outside(f:270, x:-100, y:-105);
            Outside(f:270, x:-100, y:+105);
            
            // Left (negative facing)
            Inside(f:-90, x:-100, y:0);
            Inside(f:-90, x:-100, y:-95);
            Inside(f:-90, x:-100, y:+95);
            Outside(f:-90, x:-100, y:-105);
            Outside(f:-90, x:-100, y:+105);
        }

        // This covers several real world cases which were identified as bugs
        // They are here to prevent any regressions
        [TestMethod]
        public void ShipRealUseCases()
        {
            Ship ship = SpawnShip("Flak Fang", Player, Vector2.Zero);
            Weapon w = ship.Weapons.First;

            // ship is at 24,-1008 and looking DOWN at us at 0,0, fire arc is 23 degrees
            SetShipPosAndFacing(ship, new Vector2(24, -1008), rotation:180);
            InsideFiringArc(ship, w, facing:0, fireArc:23, point:new Vector2(0, 0));
        }

        static Vector2 PointOnCircle(float rotation, float radius) => MathExt.PointOnCircle(rotation, 100);

        void Run360Loop(int turretFacing, int targetRotationOffset)
        {
            Ship ship = SpawnShip("Flak Fang", Player, Vector2.Zero);
            Weapon w = ship.Weapons.First;
            for (int shipRotation = 0; shipRotation < 360; shipRotation += 1)
            {
                int fireArc = 10;
                int targetRotation = shipRotation + targetRotationOffset; // IN FRONT OF US
                SetShipPosAndFacing(ship, Vector2.Zero, shipRotation);
                void Inside(float f)  =>  InsideFiringArc(ship, w, f, fireArc, PointOnCircle(targetRotation, 100));
                void Outside(float f) => OutsideFiringArc(ship, w, f, fireArc, PointOnCircle(targetRotation, 100));

                int facing = turretFacing;
                Inside(facing);
                Inside(facing + (fireArc/2 - 1)); // still within the fire arc, but close to arc edge
                Inside(facing - (fireArc/2 - 1)); // still within the fire arc, but close to arc edge

                // Negative tests make sure that out of arc points are reported correctly as outside
                Outside(facing+90);  // negative test: turret is facing 90 degrees to the RIGHT
                Outside(facing+180); // negative test: turret is facing 180 degrees away
                Outside(facing+270); // negative test: turret is facing 90 degrees to the LEFT

                Outside(facing-90);  // negative test: turret is facing 90 degrees to the LEFT
                Outside(facing-180); // negative test: turret is facing 180 degrees away
                Outside(facing-270); // negative test: turret is facing 90 degrees to the RIGHT
            }
        }

        [TestMethod]
        public void TurretFacingForward_TargetInFront_360Loop()
        {
            Run360Loop(turretFacing: 0 /*turret facing forward*/,  targetRotationOffset: 0 /*target in front of the ship*/);
        }
        [TestMethod]
        public void TurretFacingBehind_TargetToBehind_360Loop()
        {
            Run360Loop(turretFacing: 180 /*turret facing backward*/, targetRotationOffset: 180 /*target behind the ship*/);
            Run360Loop(turretFacing: -180 /*turret facing backward*/, targetRotationOffset: -180 /*target behind the ship*/);
        }
        [TestMethod]
        public void TurretFacingRight_TargetToRight_360Loop()
        {
            Run360Loop(turretFacing: 90 /*turret facing right*/,  targetRotationOffset: 90 /*target to the right*/);
            Run360Loop(turretFacing: -270 /*turret facing right*/, targetRotationOffset: -270 /*target to the right*/);
        }
        [TestMethod]
        public void TurretFacingLeft_TargetToLeft_360Loop()
        {
            Run360Loop(turretFacing: 270 /*turret facing left*/, targetRotationOffset: 270 /*target to the left*/);
            Run360Loop(turretFacing: -90 /*turret facing left*/, targetRotationOffset: -90 /*target to the left*/);
        }
        [TestMethod]
        public void TurretFacingTopRight_TargetToTopRight_360Loop()
        {
            Run360Loop(turretFacing: 45 /*turret facing top right*/, targetRotationOffset: 45 /*target to top right*/);
            Run360Loop(turretFacing: 45-360 /*turret facing top right*/, targetRotationOffset: 45-360 /*target to top right*/);
        }
        [TestMethod]
        public void TurretFacingTopLeft_TargetToTopLeft_360Loop()
        {
            Run360Loop(turretFacing: -45 /*turret facing top left*/, targetRotationOffset: -45 /*target to top left*/);
            Run360Loop(turretFacing: 360-45 /*turret facing top left*/, targetRotationOffset: 360-45 /*target to top left*/);
        }
    }
}

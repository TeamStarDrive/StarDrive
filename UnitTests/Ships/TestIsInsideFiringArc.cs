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
        public static bool EnableVisualization = true;

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
                float facing = (m.FacingRadians + Ship.Rotation);
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
                   $"module global facing: {gf.String(3)} rads {gf.ToDegrees().String(3)}\n"+
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
            
            // Up
            InsideFiringArc(ship, w, facing:0, fireArc:90, point:new Vector2(0, -100));
            InsideFiringArc(ship, w, facing:0, fireArc:90, point:new Vector2(-95, -100));
            InsideFiringArc(ship, w, facing:0, fireArc:90, point:new Vector2(+95, -100));
            OutsideFiringArc(ship, w, facing:0, fireArc:90, point:new Vector2(-105, -100));
            OutsideFiringArc(ship, w, facing:0, fireArc:90, point:new Vector2(+105, -100));

            // Up (360)
            InsideFiringArc(ship, w, facing:360, fireArc:90, point:new Vector2(0, -100));
            InsideFiringArc(ship, w, facing:360, fireArc:90, point:new Vector2(-95, -100));
            InsideFiringArc(ship, w, facing:360, fireArc:90, point:new Vector2(+95, -100));
            OutsideFiringArc(ship, w, facing:360, fireArc:90, point:new Vector2(-105, -100));
            OutsideFiringArc(ship, w, facing:360, fireArc:90, point:new Vector2(+105, -100));

            // Right
            InsideFiringArc(ship, w, facing:90, fireArc:90, point:new Vector2(+100, 0));
            InsideFiringArc(ship, w, facing:90, fireArc:90, point:new Vector2(+100, -95));
            InsideFiringArc(ship, w, facing:90, fireArc:90, point:new Vector2(+100, +95));
            OutsideFiringArc(ship, w, facing:90, fireArc:90, point:new Vector2(+100, -105));
            OutsideFiringArc(ship, w, facing:90, fireArc:90, point:new Vector2(+100, +105));

            // Right (negative facing)
            InsideFiringArc(ship, w, facing:-270, fireArc:90, point:new Vector2(+100, 0));
            InsideFiringArc(ship, w, facing:-270, fireArc:90, point:new Vector2(+100, -95));
            InsideFiringArc(ship, w, facing:-270, fireArc:90, point:new Vector2(+100, +95));
            OutsideFiringArc(ship, w, facing:-270, fireArc:90, point:new Vector2(+100, -105));
            OutsideFiringArc(ship, w, facing:-270, fireArc:90, point:new Vector2(+100, +105));

            // Left
            InsideFiringArc(ship, w, facing:270, fireArc:90, point:new Vector2(-100, 0));
            InsideFiringArc(ship, w, facing:270, fireArc:90, point:new Vector2(-100, -95));
            InsideFiringArc(ship, w, facing:270, fireArc:90, point:new Vector2(-100, +95));
            OutsideFiringArc(ship, w, facing:270, fireArc:90, point:new Vector2(-100, -105));
            OutsideFiringArc(ship, w, facing:270, fireArc:90, point:new Vector2(-100, +105));
            
            // Left (negative facing)
            InsideFiringArc(ship, w, facing:-90, fireArc:90, point:new Vector2(-100, 0));
            InsideFiringArc(ship, w, facing:-90, fireArc:90, point:new Vector2(-100, -95));
            InsideFiringArc(ship, w, facing:-90, fireArc:90, point:new Vector2(-100, +95));
            OutsideFiringArc(ship, w, facing:-90, fireArc:90, point:new Vector2(-100, -105));
            OutsideFiringArc(ship, w, facing:-90, fireArc:90, point:new Vector2(-100, +105));
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

        [TestMethod]
        public void InFrontOfUs720Loop()
        {
            Ship ship = SpawnShip("Flak Fang", Player, Vector2.Zero);
            Weapon w = ship.Weapons.First;

            for (int rotation = -720; rotation < +720; ++rotation)
            {
                SetShipPosAndFacing(ship, Vector2.Zero, rotation:rotation);
                InsideFiringArc(ship, w, facing:0, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                InsideFiringArc(ship, w, facing:+4, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                InsideFiringArc(ship, w, facing:-4, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:+90, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:+180, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:+270, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:-90, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:-180, fireArc:10, MathExt.PointOnCircle(rotation, 100));
                OutsideFiringArc(ship, w, facing:-270, fireArc:10, MathExt.PointOnCircle(rotation, 100));
            }
        }
    }
}

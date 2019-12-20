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
            readonly Ship Ship;
            readonly Weapon Wep;
            readonly Vector2 Point;
            public ArcVisualization(Ship ship, Weapon wep, Vector2 point)
            {
                Ship = ship;
                Wep = wep;
                Point = point;
            }

            public override void Draw(SpriteBatch batch)
            {
                Vector2 c = Game.Manager.ScreenCenter;
                ShipModule m = Wep.Module;
                Vector2 mc = c + m.Center;
                Vector2 md = m.FacingRadians.RadiansToDirection();
                batch.DrawCircle(mc, m.Radius, Color.Yellow, 2);
                batch.DrawLine(mc, mc + md*m.Radius, Color.Yellow, 2);

                bool inArc = Ship.IsInsideFiringArc(Wep, Point);
                Color color = inArc ? Color.Green : Color.Red;
                batch.DrawCircle(c + Point, 10, color, 2);

                Vector2 left = (m.FacingRadians - m.FieldOfFire * 0.5f).RadiansToDirection();
                Vector2 right = (m.FacingRadians + m.FieldOfFire * 0.5f).RadiansToDirection();
                batch.DrawLine(mc, mc + left * 500, Color.Orange);
                batch.DrawLine(mc, mc + right * 500, Color.Orange);

                Game.DrawText(5, 10, $"WepFacing: {m.FacingDegrees}");
                Game.DrawText(5, 30, $"Point:     {Point}");
                Game.DrawText(5, 50, $"InsideArc: {inArc}", color);
            }
        }

        bool CheckArc(Ship ship, Weapon w, float facing, float fireArc, Vector2 point)
        {
            w.Module.Center = w.Module.Position = Vector2.Zero;
            w.Module.FacingDegrees = facing;
            w.Module.FieldOfFire = fireArc.ToRadians();
            if (EnableVisualization)
            {
                var vis = new ArcVisualization(ship, w, point);
                Game.ShowAndRun(vis);
            }
            return ship.IsInsideFiringArc(w, point);
        }

        void InsideFiringArc(Ship ship, Weapon w, float facing, float fireArc, Vector2 point)
        {
            Assert.IsTrue(CheckArc(ship, w, facing, fireArc, point),
                $"InsideFiringArc facing:{w.Module.FacingDegrees}  "+
                $"arc:{w.Module.FieldOfFire.ToDegrees()}  point:{point}");
        }
        
        void OutsideFiringArc(Ship ship, Weapon w, float facing, float fireArc, Vector2 point)
        {
            Assert.IsFalse(CheckArc(ship, w, facing, fireArc, point),
                $"OutsideFiringArc facing:{w.Module.FacingDegrees}  "+
                $"arc:{w.Module.FieldOfFire.ToDegrees()}  point:{point}");
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

    }
}

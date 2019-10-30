using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipInfoOverlayComponent : UIElementV2
    {
        Ship SelectedShip;

        public ShipInfoOverlayComponent()
        {
            Visible = false;
        }

        public void ShowShip(Ship ship)
        {
            if (SelectedShip != ship)
            {
                SelectedShip = ship;
                if (ship != null)
                {
                    // TODO: USE NEW FAST POWER RECALC FROM SHIP DESIGN SCREEN
                    ship.RecalculatePower();
                    ship.ShipStatusChange();
                }
            }
            Visible = (SelectedShip != null);
        }

        public override bool HandleInput(InputState input)
        {
            return Rect.HitTest(input.CursorPosition);
        }

        public override void Draw(SpriteBatch batch)
        {
            Ship ship = SelectedShip;
            if (!Visible || ship == null)
                return;

            SpriteFont font8 = Fonts.Arial8Bold;
            SpriteFont font12 = Fonts.Arial12Bold;
            int x = (int)X;
            int y = (int)Y;

            var shipBackground = new Rectangle(x, y, 360, 240);
            var shipOverlay = new Rectangle(x + 140, y + 20, 200, 200);
            batch.Draw(ResourceManager.Texture("NewUI/colonyShipBuildBG"), shipBackground);
            ship.RenderOverlay(batch, shipOverlay, true, moduleHealthColor: false);

            float mass = ship.Mass * EmpireManager.Player.data.MassModifier;
            float subLightSpeed = ship.Thrust / mass;
            float warpSpeed = ship.WarpThrust / mass * EmpireManager.Player.data.FTLModifier;
            float turnRate = ship.TurnThrust.ToDegrees() / mass / 700;

            var cursor = new Vector2(x + 25, y + 1);
            DrawShipValueLine(batch, font12, ref cursor, ship.Name, "", Color.White);
            DrawShipValueLine(batch, font8, ref cursor, ship.shipData.ShipCategory + ", " + ship.shipData.CombatState, "", Color.Gray);
            WriteLine(ref cursor, font8);
            DrawShipValueLine(batch, font8, ref cursor, "Weapons:", ship.Weapons.Count, Color.LightBlue);
            DrawShipValueLine(batch, font8, ref cursor, "Max W.Range:", ship.WeaponsMaxRange, Color.LightBlue);
            DrawShipValueLine(batch, font8, ref cursor, "Avr W.Range:", ship.WeaponsAvgRange, Color.LightBlue);
            DrawShipValueLine(batch, font8, ref cursor, "Warp:", warpSpeed, Color.LightGreen);
            DrawShipValueLine(batch, font8, ref cursor, "Speed:", subLightSpeed, Color.LightGreen);
            DrawShipValueLine(batch, font8, ref cursor, "Turn Rate:", turnRate, Color.LightGreen);
            DrawShipValueLine(batch, font8, ref cursor, "Repair:", ship.RepairRate, Color.Goldenrod);
            DrawShipValueLine(batch, font8, ref cursor, "Shields:", ship.shield_max, Color.Goldenrod);
            DrawShipValueLine(batch, font8, ref cursor, "EMP Def:", ship.EmpTolerance, Color.Goldenrod);
            DrawShipValueLine(batch, font8, ref cursor, "Hangars:", ship.Carrier.AllFighterHangars.Length, Color.IndianRed);
            DrawShipValueLine(batch, font8, ref cursor, "Troop Bays:", ship.Carrier.AllTroopBays.Length, Color.IndianRed);
            DrawShipValueLine(batch, font8, ref cursor, "Troops:", ship.TroopCapacity, Color.IndianRed);
            DrawShipValueLine(batch, font8, ref cursor, "Bomb Bays:", ship.BombBays.Count, Color.IndianRed);
            DrawShipValueLine(batch, font8, ref cursor, "Cargo Space:", ship.CargoSpaceMax, Color.Khaki);
        }

        void DrawShipValueLine(SpriteBatch batch, SpriteFont font, ref Vector2 cursor, string description, string data, Color color)
        {
            WriteLine(ref cursor, font);
            var ident = new Vector2(cursor.X + 80, cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data, ident, color);
        }

        void DrawShipValueLine(SpriteBatch batch, SpriteFont font, ref Vector2 cursor, string description, float data, Color color)
        {
            if (data.LessOrEqual(0))
                return;

            WriteLine(ref cursor, font);
            var ident = new Vector2(cursor.X + 80, cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data.GetNumberString(), ident, color);
        }

        void WriteLine(ref Vector2 cursor, SpriteFont font)
        {
            cursor.Y += font.LineSpacing + 2;
        }
    }
}

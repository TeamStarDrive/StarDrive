﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipInfoOverlayComponent : UIElementV2
    {
        GameScreen Screen;
        Ship SelectedShip;
        bool LowRes;
        SpriteFont TitleFont;
        SpriteFont Font;
        int TextWidth;

        public ShipInfoOverlayComponent(GameScreen screen)
        {
            Visible = false;
            Screen = screen;
            LowRes = screen.LowRes;
        }

        public void ShowToLeftOf(Vector2 leftOf, Ship ship)
        {
            if (ship == null)
            {
                Visible = false;
                return;
            }

            float minimumSize = LowRes ? 256 : 320;
            float size        = Math.Max(minimumSize, (Screen.Width * 0.16f).RoundTo10());
            Vector2 pos       = new Vector2(leftOf.X - size*1.6f, leftOf.Y - size/4).RoundTo10();
            pos.Y             = Math.Max(100f, pos.Y);
            ShowShip(ship, pos, size);
        }

        public void ShowShip(Ship ship, Vector2 screenPos, float shipRectSize)
        {
            if (SelectedShip != ship)
            {
                SelectedShip = ship;
                // TODO: USE NEW FAST POWER RECALC FROM SHIP DESIGN SCREEN
                ship.RecalculatePower(); // SLOOOOOOW
                ship.ShipStatusChange();
            }

            Visible = true;

            TextWidth = (shipRectSize/2).RoundTo10();
            Size = new Vector2(shipRectSize + TextWidth, shipRectSize);
            Pos = screenPos;

            TitleFont = LowRes ? Fonts.Arial12Bold : Fonts.Arial14Bold;
            Font      = LowRes ? Fonts.Arial8Bold : Fonts.Arial11Bold;
        }

        public override bool HandleInput(InputState input)
        {
            return Rect.HitTest(input.CursorPosition);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Ship ship = SelectedShip;
            if (!Visible || ship == null)
                return;

            int size = (int)(Height - 56);
            var shipOverlay = new Rectangle((int)X + TextWidth, (int)Y + 40, size, size);
            new Menu2(Rect).Draw(batch, elapsed);

            ship.RenderOverlay(batch, shipOverlay, true, moduleHealthColor: false);
            float mass          = ShipStats.GetMass(ship.Mass, EmpireManager.Player);
            float warpSpeed     = ShipStats.GetFTLSpeed(ship.WarpThrust, mass, EmpireManager.Player);
            float subLightSpeed = ShipStats.GetSTLSpeed(ship.Thrust, mass, EmpireManager.Player);
            float turnRateDeg   = ShipStats.GetTurnRadsPerSec(ship.TurnThrust, mass, ship.Level).ToDegrees();
            var cursor = new Vector2(X + (Width*0.06f).RoundTo10(), Y + (int)(Height * 0.025f));
            DrawShipValueLine(batch, TitleFont, ref cursor, ship.Name, "", Color.White);
            DrawShipValueLine(batch, Font, ref cursor, ship.shipData.ShipCategory + ", " + ship.shipData.CombatState, "", Color.Gray);
            WriteLine(ref cursor, Font);
            DrawShipValueLine(batch, Font, ref cursor, "Weapons:", ship.Weapons.Count, Color.LightBlue);
            DrawShipValueLine(batch, Font, ref cursor, "Max W.Range:", ship.WeaponsMaxRange, Color.LightBlue);
            DrawShipValueLine(batch, Font, ref cursor, "Avr W.Range:", ship.WeaponsAvgRange, Color.LightBlue);
            DrawShipValueLine(batch, Font, ref cursor, "Warp:", warpSpeed, Color.LightGreen);
            DrawShipValueLine(batch, Font, ref cursor, "Speed:", subLightSpeed, Color.LightGreen);
            DrawShipValueLine(batch, Font, ref cursor, "Turn Rate:", turnRateDeg, Color.LightGreen);
            DrawShipValueLine(batch, Font, ref cursor, "Repair:", ship.RepairRate, Color.Goldenrod);
            DrawShipValueLine(batch, Font, ref cursor, "Shields:", ship.shield_max, Color.Goldenrod);
            DrawShipValueLine(batch, Font, ref cursor, "EMP Def:", ship.EmpTolerance, Color.Goldenrod);
            DrawShipValueLine(batch, Font, ref cursor, "Hangars:", ship.Carrier.AllFighterHangars.Length, Color.IndianRed);
            DrawShipValueLine(batch, Font, ref cursor, "Troop Bays:", ship.Carrier.AllTroopBays.Length, Color.IndianRed);
            DrawShipValueLine(batch, Font, ref cursor, "Troops:", ship.TroopCapacity, Color.IndianRed);
            DrawShipValueLine(batch, Font, ref cursor, "Bomb Bays:", ship.BombBays.Count, Color.IndianRed);
            DrawShipValueLine(batch, Font, ref cursor, "Cargo Space:", ship.CargoSpaceMax, Color.Khaki);
        }

        void DrawShipValueLine(SpriteBatch batch, SpriteFont font, ref Vector2 cursor, string description, string data, Color color)
        {
            WriteLine(ref cursor, font);
            var ident = new Vector2(cursor.X + (TextWidth*0.5f).RoundTo10(), cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data, ident, color);
        }

        void DrawShipValueLine(SpriteBatch batch, SpriteFont font, ref Vector2 cursor, string description, float data, Color color)
        {
            if (data.LessOrEqual(0))
                return;

            WriteLine(ref cursor, font);
            var ident = new Vector2(cursor.X + (TextWidth*0.6f).RoundTo10(), cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data.GetNumberString(), ident, color);
        }

        static void WriteLine(ref Vector2 cursor, SpriteFont font)
        {
            cursor.Y += font.LineSpacing + 2;
        }
    }
}

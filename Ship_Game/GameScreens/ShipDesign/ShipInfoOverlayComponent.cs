using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using SgMotion;
using System.Windows.Forms;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class ShipInfoOverlayComponent : UIElementV2
    {
        GameScreen Screen;
        UniverseState Universe;
        Empire Player => Universe.Player;
        Ship SelectedShip;
        ShipDesignStats Ds;
        bool LowRes;
        Graphics.Font TitleFont;
        Graphics.Font Font;
        int TextWidth;

        public ShipInfoOverlayComponent(GameScreen screen, UniverseState us)
        {
            Visible = false;
            Screen = screen;
            Universe = us;
            LowRes = screen.LowRes;
        }

        public void ShowToLeftOf(Vector2 leftOf, IShipDesign ship)
        {
            if (ship != null && ResourceManager.GetShipTemplate(ship.Name, out Ship template))
            {
                ShowToLeftOf(leftOf, template);
            }
        }

        public void ShowToLeftOf(Vector2 leftOf, Ship ship)
        {
            if (ship == null)
            {
                Visible = false;
                return;
            }

            float minimumSize = LowRes ? 272 : 340;
            float size = Math.Max(minimumSize, (Screen.Width * 0.16f).RoundTo10());
            Vector2 pos = new Vector2(leftOf.X - size*1.6f, leftOf.Y - size/4).RoundTo10();
            pos.Y = Math.Max(100f, pos.Y);
            ShowShip(ship, pos, size);
        }

        public void ShowToTopOf(Vector2 topOf, Ship ship)
        {
            if (ship == null)
            {
                Visible = false;
                return;
            }

            float minimumSize = LowRes ? 272 : 340;
            float size = Math.Max(minimumSize, (Screen.Width * 0.16f).RoundTo10());
            Vector2 pos = new Vector2(topOf.X, topOf.Y - size - 20).RoundTo10();
            pos.X = Math.Max(100f, pos.X);
            ShowShip(ship, pos, size);
        }

        public void ShowShip(Ship ship, Vector2 screenPos, float shipRectSize)
        {
            if (SelectedShip != ship)
            {
                SelectedShip = ship;
                ship.RecalculatePower();
                ship.ShipStatusChange();
                Ds = new(ship, Player);
            }

            Visible = true;

            TextWidth = (shipRectSize/2).RoundTo10();
            Size = new Vector2(shipRectSize + TextWidth, shipRectSize);
            Pos = screenPos;
            if (Pos.X < 0) Pos.X = 0;
            if (Pos.Y < 0) Pos.Y = 0;
            if (Bottom > Screen.Height) Pos.Y -= (Bottom - Screen.Height);

            TitleFont = LowRes ? Fonts.Arial12Bold : Fonts.Arial14Bold;
            Font      = LowRes ? Fonts.Arial8Bold : Fonts.Arial11Bold;
        }

        public override bool HandleInput(InputState input)
        {
            return Rect.HitTest(input.CursorPosition);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Ship s = SelectedShip;
            if (!Visible || s == null)
                return;

            int size = (int)(Height - 56);
            var shipOverlay = new Rectangle((int)Right - size - 24, (int)Y + 28, size, size);
            new Menu2(Rect).Draw(batch, elapsed); // background with menu2 borders

            s.RenderOverlay(batch, shipOverlay, true, moduleHealthColor: false);
            float mass          = s.Stats.GetMass(Player);
            float warpSpeed     = s.Stats.GetFTLSpeed(mass, Player);
            float subLightSpeed = s.Stats.GetSTLSpeed(mass, Player);
            float turnRateDeg   = s.Stats.GetTurnRadsPerSec(s.Level).ToDegrees();

            var p = new Vector2(X + 25, Y + 22);
            DrawText(TitleFont, s.Name, "", Color.White);
            DrawText(Font, $"{s.ShipData.ShipCategory}, {s.ShipData.DefaultCombatState}", "", Color.Gray);

            Vector2 start = p;
            // --- Core values with icons --- //
            float charWidth = LowRes ? 8 : 10;

            // left side
            CoreValue(charWidth*3.5f, "UI/icon_offense", "DPS", Str(s.TotalDps), Color.OrangeRed);
            if (Ds.HasEnergyWeapons)
            {
                float duration = Ds.HasBeams() ? Ds.BurstEnergyDuration : Ds.EnergyDuration;
                bool isInf = Ds.HasBeams() ? Ds.HasBeamDurationPositive() : Ds.HasEnergyWepsPositive();
                string energyTime = isInf ? "INF" : $"{duration}s";
                CoreValue(charWidth*3.5f, "UI/lightningBolt", "ETM", energyTime, Color.LightGoldenrodYellow);
            }
            if (Ds.HasOrdnance())
            {
                string ammoTime = Ds.HasOrdInfinite() ? "INF" : $"{(int)Ds.AmmoTime}s";
                CoreValue(charWidth*3.5f, "Modules/Ordnance", "OTM", ammoTime, Color.Khaki);
            }

            // right side
            p = new(start.X + charWidth * 10, start.Y);
            CoreValue(charWidth*2, "UI/icon_shield", "HP", Str(s.HealthMax), Color.CadetBlue);
            if (s.ShieldMax > 0)
            {
                CoreValue(charWidth*2, "Modules/Shield_1KW", "SP", Str(s.ShieldMax), Color.AliceBlue);
            }

            ////////////////////////////////////

            // verbose stats
            p = new(start.X, start.Y + 60);

            DrawValue("Weapons:", s.Weapons.Count, Color.LightBlue);
            if (s.WeaponsMaxRange > 0)
            {
                DrawText(Font, "W.Range:", $"{Str(s.WeaponsAvgRange)}..{Str(s.WeaponsMaxRange)}", Color.LightBlue);
            }
            DrawValue("Warp:", warpSpeed, Color.LightGreen);
            DrawValue("Speed:", subLightSpeed, Color.LightGreen);
            DrawValue("TurnRate:", turnRateDeg, Color.LightGreen);
            DrawValue("Repair:", s.RepairRate, Color.Goldenrod);
            DrawValue("EMP Def:", s.EmpTolerance, Color.Goldenrod);
            DrawValue("Hangars:", s.Carrier.AllFighterHangars.Length, Color.IndianRed);
            DrawValue("Troops:", s.TroopCapacity, Color.IndianRed);
            DrawValue("BombBays:", s.BombBays.Count, Color.IndianRed);
            DrawValue("Cargo:", s.CargoSpaceMax, Color.Khaki);

            void CoreValue(float ident, string icon, string title, string value, Color color)
            {
                batch.Draw(ResourceManager.Texture(icon), new RectF(p.X, p.Y, 20, 20), Color.White);
                batch.DrawString(Font, title, new Vector2(p.X+22, p.Y+1).Rounded(), color);
                batch.DrawString(Font, value, new Vector2(p.X+22+ident, p.Y+1).Rounded(), color);
                p.Y += 20;
            }

            void DrawText(Graphics.Font font, string title, string text, Color color)
            {
                var ident = new Vector2(p.X + (TextWidth*0.36f).RoundTo10(), p.Y);
                batch.DrawString(font, title, p, color);
                batch.DrawString(font, text, ident, color);
                p.Y += font.LineSpacing + 2;
            }

            void DrawValue(string title, float value, Color color)
            {
                if (value <= 0f)
                    return;
                var ident = new Vector2(p.X + (TextWidth*0.36f).RoundTo10(), p.Y);
                batch.DrawString(Font, title, p, color);
                batch.DrawString(Font, Str(value), ident, color);
                p.Y += Font.LineSpacing + 2;
            }
        }

        static string Str(float value) => value.GetNumberString();
    }
}

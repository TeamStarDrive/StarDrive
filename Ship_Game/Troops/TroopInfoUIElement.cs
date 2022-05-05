using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public sealed class TroopInfoUIElement : UIElementContainer
    {
        readonly UniverseScreen Universe;
        readonly Rectangle LeftRect;
        readonly Rectangle DefenseRect;
        readonly Rectangle SoftAttackRect;
        readonly Rectangle HardAttackRect;
        readonly Rectangle RangeRect;
        Rectangle ItemDisplayRect;
        DanButton LaunchTroop;
        readonly Selector Sel;
        readonly UITextBox DescriptionBox;
        readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        public PlanetGridSquare Tile { get; private set; }

        public TroopInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Universe          = screen;
            Sel               = new Selector(r, Color.Black);
            LeftRect          = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            DefenseRect       = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
            SoftAttackRect    = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
            HardAttackRect    = new Rectangle(LeftRect.X + 12, SoftAttackRect.Y + 16 + 5, 16, 16);
            RangeRect         = new Rectangle(LeftRect.X + 12, HardAttackRect.Y + 16 + 5, 16, 16);
            DefenseRect.X    -= 3;
            ItemDisplayRect   = new Rectangle(LeftRect.X + 85, LeftRect.Y + 5, 128, 128);
            Rectangle desRect = new Rectangle(RangeRect.X, RangeRect.Y + 5, LeftRect.Width + 8, 240);
            Submenu sub       = new Submenu(desRect);
            DescriptionBox    = Add(new UITextBox(sub));

            ToolTipItems.Add(new TippedItem(DefenseRect, GameText.IndicatesThisUnitsGroundCombat));
            ToolTipItems.Add(new TippedItem(SoftAttackRect, GameText.IndicatesThisUnitsCombatEffectiveness));
            ToolTipItems.Add(new TippedItem(HardAttackRect, GameText.IndicatesThisUnitsCombatEffectiveness2));
            ToolTipItems.Add(new TippedItem(RangeRect, GameText.IndicatesTheTileRangeThis));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed) // refactored by  Fat Bastard Aug 6, 2018
        {
            if (Tile == null || Tile.NothingOnTile)
                return;

            //MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            batch.FillRectangle(Sel.Rect, Color.Black);

            var slant = new Header(new Rectangle(Sel.Rect.X, Sel.Rect.Y, Sel.Rect.Width, 41), "");
            var body = new Body(new Rectangle(slant.leftRect.X, Sel.Rect.Y + 44, Sel.Rect.Width, Sel.Rect.Height - 44));
            Color color = Color.White;
            body.Draw(batch, elapsed);
            batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), SoftAttackRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/attack_hard"), HardAttackRect, color);
            if (!Tile.BuildingOnTile)
                batch.Draw(ResourceManager.Texture("UI/icon_offense"), RangeRect, color);

            if (Tile.TroopsAreOnTile) // draw troop_stats
            {
                Troop troopToDraw = null;
                using (Tile.TroopsHere.AcquireReadLock())
                {
                    for (int i = 0; i < Tile.TroopsHere.Count; ++i)
                    {
                        Troop troop = Tile.TroopsHere[i];
                        if (Tile.TroopsHere.Count == 1)
                            troopToDraw = troop;
                        else if (troop.Loyalty != EmpireManager.Player && troop.Hovered)
                            troopToDraw = troop;
                        else if (troop.Loyalty == EmpireManager.Player)
                            troopToDraw = troop;
                    }

                    DrawTroopStats(batch, troopToDraw, slant, Universe.Input.CursorPosition, color);
                }
            }
            else // draw building stats
            {
                if (Tile.Building.Strength < Tile.Building.StrengthMax)
                    DrawInfoData(batch, DefenseRect, Tile.Building.Strength + "/" + Tile.Building.StrengthMax.String(1), color, 2, 11);
                else
                    DrawInfoData(batch, DefenseRect, Tile.Building.StrengthMax.String(1), color, 2, 11);

                slant.text = Tile.Building.TranslatedName.Text;
                DrawInfoData(batch, SoftAttackRect, Tile.Building.SoftAttack.ToString(), color, 5, 8);
                DrawInfoData(batch, HardAttackRect, Tile.Building.HardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                batch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", Tile.Building.Icon, "_64x64")), ItemDisplayRect, color);
            }

            slant.Draw(batch, elapsed);
            base.Draw(batch, elapsed);
        }

        void DrawTroopStats(SpriteBatch batch, Troop troop, Header slant, Vector2 mousePos, Color color)
        {
            if (troop == null)
                return;

            if (troop.Strength < troop.ActualStrengthMax)
                DrawInfoData(batch, DefenseRect, troop.Strength.String(1) + "/" + troop.ActualStrengthMax.String(1), color, 2, 11);
            else
                DrawInfoData(batch, DefenseRect, troop.ActualStrengthMax.String(1), color, 2, 11);

            DrawInfoData(batch, SoftAttackRect, troop.ActualSoftAttack.ToString(), color, 5, 8);
            DrawInfoData(batch, HardAttackRect, troop.ActualHardAttack.ToString(), color, 5, 8);
            DrawInfoData(batch, RangeRect, troop.ActualRange.ToString(), color, 5, 8);
            ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
            DrawLaunchButton(batch, troop, slant);
            DrawLevelStars(batch, troop.Level, mousePos);
            slant.text = troop.Name;
        }

        void DrawInfoData(SpriteBatch batch, Rectangle rect, string data, Color color, int xOffSet, int yOffSet)
        {
            Graphics.Font font = Fonts.Arial12;
            Vector2 pos = new Vector2((rect.X + rect.Width + xOffSet), (rect.Y + yOffSet - font.LineSpacing / 2));
            batch.DrawString(font, data, pos, color);
        }

        void DrawLaunchButton(SpriteBatch batch, Troop troop, Header slant)
        {
            troop.Draw(batch, ItemDisplayRect);
            if (troop.Loyalty != EmpireManager.Player)
                LaunchTroop = null;
            else
            {
                string buttonText =  troop.AvailableAttackActions >= 1 ? "" : string.Concat(" (", troop.MoveTimer.ToString("0"), ")");
                LaunchTroop = new DanButton(new Vector2(slant.leftRect.X + 5, Sel.Bottom + 15), 
                                            Localizer.Token(GameText.Launch)+buttonText);
                LaunchTroop.DrawBlue(batch);
            }
        }

        void DrawLevelStars(SpriteBatch batch, int level, Vector2 mousePos)
        {
            if (level <= 0)
                return;

            Color color;
            switch (level)
            {
                default: color = Color.White; break;
                case 3:
                case 4:
                case 5:  color = Color.SandyBrown; break;
                case 6:
                case 7:
                case 9:  color = Color.DodgerBlue; break;
                case 10: color = Color.Gold;  break;
            }

            var starIcon = ResourceManager.Texture("UI/icon_star");
            for (int i = 0; i < level; i++)
            {
                var star = new Rectangle(LeftRect.X + LeftRect.Width - 20 - 12 * i, LeftRect.Y + 12, 12, 11);
                if (star.HitTest(mousePos))
                    ToolTip.CreateTooltip(GameText.IndicatesThisTroopsExperienceLevel);

                batch.Draw(starIcon, star, color);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (base.HandleInput(input))
                return true;

            foreach (TippedItem ti in ToolTipItems)
            {
                if (ti.Rect.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(ti.Tooltip);
            }

            // currently selected troop Launch
            if (LaunchTroop != null && LaunchTroop.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(GameText.LaunchThisTroopIntoOrbit);
                if (LaunchTroop.HandleInput(input))
                {
                    var combatScreen = (CombatScreen)Universe.workersPanel;
                    if (combatScreen.TryLaunchTroopFromActiveTile())
                        GameAudio.TroopTakeOff();
                    else
                        GameAudio.NegativeClick();
                    return true;
                }
            }

            using (Tile.TroopsHere.AcquireReadLock())
            {
                for (int i = 0; i < Tile.TroopsHere.Count; ++i)
                {
                    Troop troop = Tile.TroopsHere[i];
                    troop.Hovered = troop.ClickRect.HitTest(input.CursorPosition);
                }
            }
            return false;
        }

        public void SetTile(PlanetGridSquare pgs, Troop troop = null)
        {
            if (Tile == pgs)
                return;

            Tile = pgs;
            Visible = Tile != null;
            if (Tile == null)
                return;

            DescriptionBox.Clear();
            // Try get the first troop on the tile if troop not known
            if (troop == null)
                troop = Tile.TryGetFirstTroop();

            if (troop != null)
                DescriptionBox.AddLines(troop.Description, Fonts.Arial12, Color.White);
            else if (pgs.BuildingOnTile)
                DescriptionBox.AddLines(pgs.Building.DescriptionText.Text, Fonts.Arial12, Color.White);
        }
    }
}

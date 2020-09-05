using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class TroopInfoUIElement : UIElement
    {
        private readonly UniverseScreen screen;
        private readonly Rectangle LeftRect;
        private readonly Rectangle DefenseRect;
        private readonly Rectangle SoftAttackRect;
        private readonly Rectangle HardAttackRect;
        private readonly Rectangle RangeRect;
        private Rectangle ItemDisplayRect;
        private DanButton LaunchTroop;
        private readonly Selector Sel;
        private ScrollList2<TextListItem> DescriptionSL;
        public PlanetGridSquare Tile;
        private readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        public TroopInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.screen       = screen;
            ScreenManager     = sm;
            ElementRect       = r;
            Sel               = new Selector(r, Color.Black);
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            LeftRect          = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            DefenseRect       = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
            SoftAttackRect    = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
            HardAttackRect    = new Rectangle(LeftRect.X + 12, SoftAttackRect.Y + 16 + 5, 16, 16);
            RangeRect         = new Rectangle(LeftRect.X + 12, HardAttackRect.Y + 16 + 5, 16, 16);
            DefenseRect.X     = DefenseRect.X - 3;
            ItemDisplayRect   = new Rectangle(LeftRect.X + 85, LeftRect.Y + 5, 128, 128);
            Rectangle desRect = new Rectangle(RangeRect.X, RangeRect.Y - 10, LeftRect.Width + 8, 95);
            Submenu sub       = new Submenu(desRect);
            DescriptionSL     = new ScrollList2<TextListItem>(sub, Fonts.Arial12.LineSpacing + 1);

            ToolTipItems.Add(new TippedItem
            {
                R = DefenseRect,
                TIP_ID = 33
            });
            ToolTipItems.Add(new TippedItem
            {
                R = SoftAttackRect,
                TIP_ID = 34
            });
            ToolTipItems.Add(new TippedItem
            {
                R = HardAttackRect,
                TIP_ID = 35
            });
            ToolTipItems.Add(new TippedItem
            {
                R = RangeRect,
                TIP_ID = 251
            });
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed) // refactored by  Fat Bastard Aug 6, 2018
        {
            if (Tile == null || Tile.NothingOnTile)
                return;

            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ScreenManager.SpriteBatch.FillRectangle(Sel.Rect, Color.Black);

            float x          = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 mousePos = new Vector2(x, state.Y);
            Header slant     = new Header(new Rectangle(Sel.Rect.X, Sel.Rect.Y, Sel.Rect.Width, 41), "");
            Body body        = new Body(new Rectangle(slant.leftRect.X, Sel.Rect.Y + 44, Sel.Rect.Width, Sel.Rect.Height - 44));
            Color color      = Color.White;
            body.Draw(batch, elapsed);
            batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), SoftAttackRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/attack_hard"), HardAttackRect, color);
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

                    DrawTroopStats(batch, troopToDraw, slant, mousePos, color);
                }
            }
            else // draw building stats
            {
                if (Tile.building.Strength < Tile.building.StrengthMax)
                    DrawInfoData(batch, DefenseRect, Tile.building.Strength + "/" + Tile.building.StrengthMax.String(1), color, 2, 11);
                else
                    DrawInfoData(batch, DefenseRect, Tile.building.StrengthMax.String(1), color, 2, 11);

                slant.text = Localizer.Token(Tile.building.NameTranslationIndex);
                DrawInfoData(batch, SoftAttackRect, Tile.building.SoftAttack.ToString(), color, 5, 8);
                DrawInfoData(batch, HardAttackRect, Tile.building.HardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                batch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", Tile.building.Icon, "_64x64")), ItemDisplayRect, color);
            }

            slant.Draw(batch, elapsed);
            DescriptionSL.Draw(batch, elapsed);
        }

        private void DrawTroopStats(SpriteBatch batch, Troop troop, Header slant, Vector2 mousePos, Color color)
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
            DrawLaunchButton(troop, slant);
            DrawLevelStars(troop.Level, mousePos);
            slant.text = troop.Name;
        }

        private void DrawInfoData(SpriteBatch batch, Rectangle rect, string data, Color color, int xOffSet, int yOffSet)
        {
            SpriteFont font = Fonts.Arial12;
            Vector2 pos = new Vector2((rect.X + rect.Width + xOffSet), (rect.Y + yOffSet - font.LineSpacing / 2));
            batch.DrawString(font, data, pos, color);
        }

        private void DrawLaunchButton(Troop troop, Header slant)
        {
            troop.Draw(ScreenManager.SpriteBatch, ItemDisplayRect);
            if (troop.Loyalty != EmpireManager.Player)
                LaunchTroop = null;
            else
            {
                string buttonText =  troop.AvailableAttackActions >= 1 ? "" : string.Concat(" (", troop.MoveTimer.ToString("0"), ")");
                LaunchTroop = new DanButton(new Vector2(slant.leftRect.X + 5, ElementRect.Y + ElementRect.Height + 15), 
                                            Localizer.Token(1435)+buttonText);
                LaunchTroop.DrawBlue(ScreenManager.SpriteBatch);
            }
        }

        private void DrawLevelStars(int level, Vector2 mousePos)
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

            for (int i = 0; i < level; i++)
            {
                var star = new Rectangle(LeftRect.X + LeftRect.Width - 20 - 12 * i, LeftRect.Y + 12, 12, 11);
                if (star.HitTest(mousePos))
                    ToolTip.CreateTooltip(127);

                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_star"), star, color);
            }
        }

        public override bool HandleInput(InputState input)
        {
            try
            {
                DescriptionSL.HandleInput(input);
            }
            catch
            {
                return false;
            }

            foreach (TippedItem ti in ToolTipItems)
            {
                if (ti.R.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(ti.TIP_ID);
            }

            // currently selected troop Launch
            if (LaunchTroop != null && LaunchTroop.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(67);
                if (LaunchTroop.HandleInput(input))
                {
                    var combatScreen = (CombatScreen)screen.workersPanel;
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
            Tile = pgs;
            if (Tile == null)
                return;

            if (troop != null)
            {
                DescriptionSL.ResetWithParseText(Fonts.Arial12, troop.Description, LeftRect.Width - 15);
            }
            else if (pgs.BuildingOnTile)
            {
                DescriptionSL.ResetWithParseText(Fonts.Arial12, Localizer.Token(pgs.building.DescriptionIndex), LeftRect.Width - 15);
            }
        }

        private struct TippedItem
        {
            public Rectangle R;
            public int TIP_ID;
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class TroopInfoUIElement : UIElement
    {
        private Rectangle SliderRect;
        private Rectangle ClickRect;
        private readonly UniverseScreen screen;
        private readonly Rectangle LeftRect;
        private Rectangle RightRect;
        private Rectangle FlagRect;
        private readonly Rectangle DefenseRect;
        private readonly Rectangle SoftAttackRect;
        private readonly Rectangle HardAttackRect;
        private readonly Rectangle RangeRect;
        private Rectangle ItemDisplayRect;
        private DanButton LaunchTroop;
        private readonly Selector Sel;
        private ScrollList DescriptionSL;
        public PlanetGridSquare pgs;
        private readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        public TroopInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.screen       = screen;
            ScreenManager     = sm;
            ElementRect       = r;
            Sel               = new Selector(r, Color.Black);
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SliderRect        = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
            ClickRect         = new Rectangle(ElementRect.X + ElementRect.Width - 16, ElementRect.Y + ElementRect.Height / 2 - 11, 11, 22);
            LeftRect          = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            RightRect         = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            FlagRect          = new Rectangle(r.X + r.Width - 31, r.Y + 22 - 13, 26, 26);
            DefenseRect       = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
            SoftAttackRect    = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
            HardAttackRect    = new Rectangle(LeftRect.X + 12, SoftAttackRect.Y + 16 + 5, 16, 16);
            RangeRect         = new Rectangle(LeftRect.X + 12, HardAttackRect.Y + 16 + 5, 16, 16);
            DefenseRect.X     = DefenseRect.X - 3;
            ItemDisplayRect   = new Rectangle(LeftRect.X + 85, LeftRect.Y + 5, 128, 128);
            Rectangle desRect = new Rectangle(RangeRect.X, RangeRect.Y - 10, LeftRect.Width + 8, 95);
            Submenu sub       = new Submenu(desRect);
            DescriptionSL     = new ScrollList(sub, Fonts.Arial12.LineSpacing + 1);

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

        public override void Draw(GameTime gameTime) // refactored by  Fat Bastard Aug 6, 2018
        {
            if (pgs == null)
                return;

            if (pgs.TroopsHere.Count == 0 && pgs.building == null)
                return;

            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ScreenManager.SpriteBatch.FillRectangle(Sel.Rect, Color.Black);

            float x          = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 mousePos = new Vector2(x, state.Y);
            string slantText = pgs.TroopsHere.Count > 0 ? pgs.SingleTroop.Name : Localizer.Token(pgs.building.NameTranslationIndex);
            Header slant     = new Header(new Rectangle(Sel.Rect.X, Sel.Rect.Y, Sel.Rect.Width, 41), slantText);
            Body body        = new Body(new Rectangle(slant.leftRect.X, Sel.Rect.Y + 44, Sel.Rect.Width, Sel.Rect.Height - 44));
            Color color      = Color.White;

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            slant.Draw(ScreenManager);
            body.Draw(ScreenManager);
            spriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, color);
            spriteBatch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), SoftAttackRect, color);
            spriteBatch.Draw(ResourceManager.Texture("Ground_UI/attack_hard"), HardAttackRect, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/icon_offense"), RangeRect, color);

            if (pgs.TroopsHere.Count > 0) // draw troop_stats
            {
                Troop troop = pgs.SingleTroop;
                if (troop.Strength < troop.ActualStrengthMax)
                    DrawInfoData(spriteBatch, DefenseRect, troop.Strength.String(1) + "/" + troop.ActualStrengthMax.String(1), color, 2, 11);
                else
                    DrawInfoData(spriteBatch, DefenseRect, troop.ActualStrengthMax.String(1), color, 2, 11);

                DrawInfoData(spriteBatch, SoftAttackRect, troop.ActualSoftAttack.ToString(), color, 5, 8);
                DrawInfoData(spriteBatch, HardAttackRect, troop.ActualHardAttack.ToString(), color, 5, 8);
                DrawInfoData(spriteBatch, RangeRect, troop.ActualRange.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                DrawLaunchButton(troop, slant);
                DrawLevelStars(troop.Level, mousePos);
            }
            else // draw building stats
            {
                if (pgs.building.Strength < pgs.building.StrengthMax)
                    DrawInfoData(spriteBatch, DefenseRect, pgs.building.Strength + "/" + pgs.building.StrengthMax.String(1), color, 2, 11);
                else
                    DrawInfoData(spriteBatch, DefenseRect, pgs.building.StrengthMax.String(1), color, 2, 11);

                DrawInfoData(spriteBatch, SoftAttackRect, pgs.building.SoftAttack.ToString(), color, 5, 8);
                DrawInfoData(spriteBatch, HardAttackRect, pgs.building.HardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_"
                                               , pgs.building.Icon, "_64x64")), ItemDisplayRect, color);
            }

            DrawDescription(color);
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
                                            string.Concat(Localizer.Token(1435), buttonText));
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

        private void DrawDescription(Color color)
        {
            foreach (ScrollList.Entry e in DescriptionSL.VisibleEntries)
            {
                string t1 = e.item as string;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, t1, new Vector2(DefenseRect.X, e.Y), color);
            }
            DescriptionSL.Draw(ScreenManager.SpriteBatch);
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
                    {
                        GameAudio.TroopTakeOff();
                    }
                    else
                    {
                        GameAudio.NegativeClick();
                    }
                    return true;
                }
            }            
            return false;
        }

        public void SetPGS(PlanetGridSquare pgs)
        {
            this.pgs = pgs;
            if (this.pgs == null)
                return;

            if (pgs.TroopsHere.Count != 0)
            {
                DescriptionSL.SetItems(Fonts.Arial12.ParseTextToLines(pgs.SingleTroop.Description, LeftRect.Width-15));
                return;
            }

            if (pgs.building == null)
                return;

            DescriptionSL.SetItems(Fonts.Arial12.ParseTextToLines(Localizer.Token(pgs.building.DescriptionIndex), LeftRect.Width-15));
        }

        private struct TippedItem
        {
            public Rectangle R;
            public int TIP_ID;
        }
    }
}
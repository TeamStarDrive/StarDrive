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
        private ScrollList<TextListItem> DescriptionSL;
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
            LeftRect          = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            DefenseRect       = new Rectangle(LeftRect.X + 12, LeftRect.Y + 18, 22, 22);
            SoftAttackRect    = new Rectangle(LeftRect.X + 12, DefenseRect.Y + 22 + 5, 16, 16);
            HardAttackRect    = new Rectangle(LeftRect.X + 12, SoftAttackRect.Y + 16 + 5, 16, 16);
            RangeRect         = new Rectangle(LeftRect.X + 12, HardAttackRect.Y + 16 + 5, 16, 16);
            DefenseRect.X     = DefenseRect.X - 3;
            ItemDisplayRect   = new Rectangle(LeftRect.X + 85, LeftRect.Y + 5, 128, 128);
            Rectangle desRect = new Rectangle(RangeRect.X, RangeRect.Y - 10, LeftRect.Width + 8, 95);
            Submenu sub       = new Submenu(desRect);
            DescriptionSL     = new ScrollList<TextListItem>(sub, Fonts.Arial12.LineSpacing + 1);

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
            Color color = Color.White;

            SpriteBatch batch = ScreenManager.SpriteBatch;
            slant.Draw(ScreenManager);
            body.Draw(ScreenManager);
            batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), SoftAttackRect, color);
            batch.Draw(ResourceManager.Texture("Ground_UI/attack_hard"), HardAttackRect, color);
            batch.Draw(ResourceManager.Texture("UI/icon_offense"), RangeRect, color);

            if (pgs.TroopsHere.Count > 0) // draw troop_stats
            {
                Troop troop = pgs.SingleTroop;
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
            }
            else // draw building stats
            {
                if (pgs.building.Strength < pgs.building.StrengthMax)
                    DrawInfoData(batch, DefenseRect, pgs.building.Strength + "/" + pgs.building.StrengthMax.String(1), color, 2, 11);
                else
                    DrawInfoData(batch, DefenseRect, pgs.building.StrengthMax.String(1), color, 2, 11);

                DrawInfoData(batch, SoftAttackRect, pgs.building.SoftAttack.ToString(), color, 5, 8);
                DrawInfoData(batch, HardAttackRect, pgs.building.HardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                batch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", pgs.building.Icon, "_64x64")), ItemDisplayRect, color);
            }
            DescriptionSL.Draw(batch);
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
                if (!ti.R.HitTest(input.CursorPosition))
                {
                    continue;
                }
                ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (LaunchTroop != null && LaunchTroop.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(67);
                if (LaunchTroop.HandleInput(input))
                {
                    if (((CombatScreen) screen.workersPanel).ActiveTile.SingleTroop.AvailableMoveActions < 1)
                    {
                        GameAudio.NegativeClick();                        
                        return true;
                    }
                    GameAudio.TroopTakeOff();
                    
                    using (pgs.TroopsHere.AcquireWriteLock())
                        if (pgs.TroopsHere.Count > 0) pgs.SingleTroop.Launch();

                    ((CombatScreen) screen.workersPanel).ActiveTile = null;
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
                DescriptionSL.ResetWithParseText(Fonts.Arial12, pgs.SingleTroop.Description, LeftRect.Width - 15);
            }
            else if (pgs.building != null)
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
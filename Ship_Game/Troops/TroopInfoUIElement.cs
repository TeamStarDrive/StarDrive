using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class TroopInfoUIElement : UIElement
    {
        private Rectangle SliderRect;
        private Rectangle clickRect;
        private UniverseScreen screen;
        private Rectangle LeftRect;
        private Rectangle RightRect;
        private Rectangle flagRect;
        private Rectangle DefenseRect;
        private Rectangle SoftAttackRect;
        private Rectangle HardAttackRect;
        private Rectangle ItemDisplayRect;
        private DanButton LaunchTroop;
        private Selector sel;
        private ScrollList DescriptionSL;
        public PlanetGridSquare pgs;
        private Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        private new Color tColor = new Color(255, 239, 208);
        private string fmt = "0.#";

        public TroopInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
        {
            this.screen = screen;
            this.ScreenManager = sm;
            this.ElementRect = r;
            this.sel = new Selector(r, Color.Black);
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
            this.SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
            this.clickRect = new Rectangle(this.ElementRect.X + this.ElementRect.Width - 16, this.ElementRect.Y + this.ElementRect.Height / 2 - 11, 11, 22);
            this.LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            this.RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            this.flagRect = new Rectangle(r.X + r.Width - 31, r.Y + 22 - 13, 26, 26);
            this.DefenseRect = new Rectangle(this.LeftRect.X + 12, this.LeftRect.Y + 18, 22, 22);
            this.SoftAttackRect = new Rectangle(this.LeftRect.X + 12, this.DefenseRect.Y + 22 + 5, 16, 16);
            this.HardAttackRect = new Rectangle(this.LeftRect.X + 12, this.SoftAttackRect.Y + 16 + 5, 16, 16);
            this.DefenseRect.X = this.DefenseRect.X - 3;
            this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85, this.LeftRect.Y + 5, 128, 128);
            Rectangle DesRect = new Rectangle(this.HardAttackRect.X, this.HardAttackRect.Y - 10, this.LeftRect.Width + 8, 95);
            Submenu sub = new Submenu(DesRect);
            this.DescriptionSL = new ScrollList(sub, Fonts.Arial12.LineSpacing + 1);
            TroopInfoUIElement.TippedItem def = new TroopInfoUIElement.TippedItem()
            {
                r = this.DefenseRect,
                TIP_ID = 33
            };
            this.ToolTipItems.Add(def);
            def = new TroopInfoUIElement.TippedItem()
            {
                r = this.SoftAttackRect,
                TIP_ID = 34
            };
            this.ToolTipItems.Add(def);
            def = new TroopInfoUIElement.TippedItem()
            {
                r = this.HardAttackRect,
                TIP_ID = 35
            };
            this.ToolTipItems.Add(def);
        }

        public override void Draw(GameTime gameTime) // refactored by  Fat Bastard Aug 6, 2018
        {
            if (pgs == null)
                return;

            if (pgs.TroopsHere.Count == 0 && pgs.building == null)
                return;

            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ScreenManager.SpriteBatch.FillRectangle(sel.Rect, Color.Black);

            float x          = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 mousePos = new Vector2(x, state.Y);
            string slantText = pgs.TroopsHere.Count > 0 ? pgs.TroopsHere[0].Name : Localizer.Token(pgs.building.NameTranslationIndex);
            Header slant     = new Header(new Rectangle(sel.Rect.X, sel.Rect.Y, sel.Rect.Width, 41), slantText);
            Body body        = new Body(new Rectangle(slant.leftRect.X, sel.Rect.Y + 44, sel.Rect.Width, sel.Rect.Height - 44));
            Color color      = Color.White;

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            slant.Draw(ScreenManager);
            body.Draw(ScreenManager);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], DefenseRect, Color.White);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], SoftAttackRect, Color.White);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/attack_hard"], HardAttackRect, Color.White);
            if (pgs.TroopsHere.Count > 0) // draw troop_stats
            {
                Troop troop = pgs.TroopsHere[0];
                if (troop.Strength < troop.ActualStrengthMax)
                    DrawinfoData(spriteBatch, DefenseRect, troop.Strength.String(1) + "/" + troop.ActualStrengthMax.String(1), color, 2, 11);
                else
                    DrawinfoData(spriteBatch, DefenseRect, troop.ActualStrengthMax.String(1), color, 2, 11);

                DrawinfoData(spriteBatch, SoftAttackRect, troop.NetSoftAttack.ToString(), color, 5, 8);
                DrawinfoData(spriteBatch, HardAttackRect, troop.NetHardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                DrawLaunchButton(troop, slant);
                DrawLevelStars(troop.Level, mousePos);
            }
            else // draw building stats
            {
                if (pgs.building.Strength < pgs.building.StrengthMax)
                    DrawinfoData(spriteBatch, DefenseRect, pgs.building.Strength + "/" + pgs.building.StrengthMax.String(1), color, 2, 11);
                else
                    DrawinfoData(spriteBatch, DefenseRect, pgs.building.StrengthMax.String(1), color, 2, 11);

                DrawinfoData(spriteBatch, SoftAttackRect, pgs.building.SoftAttack.ToString(), color, 5, 8);
                DrawinfoData(spriteBatch, HardAttackRect, pgs.building.HardAttack.ToString(), color, 5, 8);
                ItemDisplayRect = new Rectangle(LeftRect.X + 85 + 16, LeftRect.Y + 5 + 16, 64, 64);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_"
                                               , pgs.building.Icon, "_64x64")], ItemDisplayRect, color);
            }

            DrawDescription(color);
        }

        private void DrawinfoData(SpriteBatch batch, Rectangle rect, string data, Color color, int xOffSet, int yOffSet)
        {
            SpriteFont font = Fonts.Arial12;
            Vector2 pos = new Vector2((rect.X + rect.Width + xOffSet), (rect.Y + yOffSet - font.LineSpacing / 2));
            batch.DrawString(font, data, pos, color);
        }

        private void DrawLaunchButton(Troop troop, Header slant)
        {
            troop.Draw(ScreenManager.SpriteBatch, ItemDisplayRect);
            if (troop.GetOwner() != EmpireManager.Player)
                LaunchTroop = null;
            else
            {
                string buttonText =  troop.AvailableAttackActions >= 1 ? "" : string.Concat(" (", troop.MoveTimer.ToString("0"), ")");
                LaunchTroop = new DanButton(new Vector2(slant.leftRect.X + 5, ElementRect.Y + ElementRect.Height + 15), 
                                            string.Concat(Localizer.Token(1435), buttonText));
                LaunchTroop.DrawBlue(ScreenManager);
            }
        }

        private void DrawLevelStars(int level, Vector2 mousePos)
        {
            if (level <= 0)
                return;
            Color color = level < 3 ? Color.White : Color.Gold;
            for (int i = 0; i < level; i++)
            {
                var star = new Rectangle(LeftRect.X + LeftRect.Width - 20 - 12 * i, LeftRect.Y + 12, 12, 11);
                if (star.HitTest(mousePos))
                    ToolTip.CreateTooltip(127);

                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_star"], star, color);
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
                this.DescriptionSL.HandleInput(input);
            }
            catch
            {
                return false;
            }
            foreach (TippedItem ti in ToolTipItems)
            {
                if (!ti.r.HitTest(input.CursorPosition))
                {
                    continue;
                }
                ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (this.LaunchTroop != null && this.LaunchTroop.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(67);
                if (this.LaunchTroop.HandleInput(input))
                {
                    if ((this.screen.workersPanel as CombatScreen).ActiveTroop.TroopsHere[0].AvailableMoveActions < 1)
                    {
                        GameAudio.PlaySfxAsync("UI_Misc20");                        
                        return true;
                    }
                    GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    
                    using (pgs.TroopsHere.AcquireWriteLock())
                        if (pgs.TroopsHere.Count > 0) pgs.TroopsHere[0].Launch();

                    (this.screen.workersPanel as CombatScreen).ActiveTroop = null;
                }
            }            
            return false;
        }

        public void SetPGS(PlanetGridSquare pgs)
        {
            this.pgs = pgs;
            if (this.pgs == null)
            {
                return;
            }
            if (pgs.TroopsHere.Count != 0)
            {
                DescriptionSL.Reset();
                HelperFunctions.parseTextToSL(pgs.TroopsHere[0].Description, (LeftRect.Width - 15), Fonts.Arial12, ref DescriptionSL);
                return;
            }
            if (pgs.building != null)
            {
                DescriptionSL.Reset();
                HelperFunctions.parseTextToSL(Localizer.Token(pgs.building.DescriptionIndex), (LeftRect.Width - 15), Fonts.Arial12, ref DescriptionSL);
            }
        }

        private struct TippedItem
        {
            public Rectangle r;
            public int TIP_ID;
        }
    }
}
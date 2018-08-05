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

        public override void Draw(GameTime gameTime)
        {
            string str;
            string str1;
            if (this.pgs == null)
                return;

            if (this.pgs.TroopsHere.Count == 0 && this.pgs.building == null)
                return;

            MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
            this.ScreenManager.SpriteBatch.FillRectangle(this.sel.Rect, Color.Black);
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            Header slant = new Header(new Rectangle(this.sel.Rect.X, this.sel.Rect.Y, this.sel.Rect.Width, 41), (this.pgs.TroopsHere.Count > 0 ? this.pgs.TroopsHere[0].Name : Localizer.Token(this.pgs.building.NameTranslationIndex)));
            Body body = new Body(new Rectangle(slant.leftRect.X, this.sel.Rect.Y + 44, this.sel.Rect.Width, this.sel.Rect.Height - 44));
            slant.Draw(this.ScreenManager);
            body.Draw(this.ScreenManager);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], this.DefenseRect, Color.White);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], this.SoftAttackRect, Color.White);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/attack_hard"], this.HardAttackRect, Color.White);
            SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
            Troop troop = pgs.TroopsHere[0];
            Vector2 strPos = new Vector2((DefenseRect.X + DefenseRect.Width + 2), (DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
            if (troop != null)
            {
                if (troop.Strength < troop.ActualStrengthMax)
                    DrawinfoData(spriteBatch, DefenseRect, troop.Strength.String(1) + "/" + troop.ActualStrengthMax.String(1), Color.White);
                else
                    DrawinfoData(spriteBatch, DefenseRect, troop.ActualStrengthMax.String(1), Color.White);
            }

            else
            {
                DrawinfoData(spriteBatch, DefenseRect, pgs.building.Strength + "/" + pgs.building.StrengthMax.String(1), Color.White);
            }



                ;


            bool Troop = false;
            if (this.pgs.TroopsHere.Count > 0)
             {
                Troop = true;
            }



            
            Vector2 defPos = new Vector2((DefenseRect.X + DefenseRect.Width + 2), (DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (Troop
                ? pgs.TroopsHere[0].Strength.String(1) + "/" + pgs.TroopsHere[0].ActualStrengthMax.String(1)
                : pgs.building.Strength + "/" + pgs.building.StrengthMax.String(1))
                , defPos, Color.White);

            defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.SoftAttackRect.Y + 8 - Fonts.Arial12Bold.LineSpacing / 2));
            //SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            if (this.pgs.TroopsHere.Count > 0)
            {
                int softAttack = this.pgs.TroopsHere[0].GetSoftAttack();
                str = softAttack.ToString(this.fmt);
            }
            else
            {
                str = this.pgs.building.SoftAttack.ToString();
            }
            spriteBatch.DrawString(arial12Bold, str, defPos, Color.White);
            defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.HardAttackRect.Y + 8 - Fonts.Arial12Bold.LineSpacing / 2));
            SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
            SpriteFont spriteFont = Fonts.Arial12Bold;
            if (this.pgs.TroopsHere.Count > 0)
            {
                int hardAttack = this.pgs.TroopsHere[0].GetHardAttack();
                str1 = hardAttack.ToString(this.fmt);
            }
            else
            {
                str1 = this.pgs.building.HardAttack.ToString();
            }
            spriteBatch1.DrawString(spriteFont, str1, defPos, Color.White);
            if (this.pgs.TroopsHere.Count == 0)
            {
                this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85 + 16, this.LeftRect.Y + 5 + 16, 64, 64);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", this.pgs.building.Icon, "_64x64")], this.ItemDisplayRect, Color.White);
            }
            else
            {
                this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85 + 16, this.LeftRect.Y + 5 + 16, 64, 64);
                this.pgs.TroopsHere[0].Draw(this.ScreenManager.SpriteBatch, this.ItemDisplayRect);
                if (this.pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                {
                    this.LaunchTroop = null;
                }
                else
                {
                    this.LaunchTroop = new DanButton(new Vector2((float)(slant.leftRect.X + 5), (float)(this.ElementRect.Y + this.ElementRect.Height + 15)), string.Concat(Localizer.Token(1435), (this.pgs.TroopsHere[0].AvailableMoveActions >= 1 ? "" : string.Concat(" (", this.pgs.TroopsHere[0].MoveTimer.ToString("0"), ")"))));
                    this.LaunchTroop.DrawBlue(this.ScreenManager);
                }
                if (pgs.TroopsHere[0].Level > 0)
                {
                    for (int i = 0; i < pgs.TroopsHere[0].Level; i++)
                    {
                        var star = new Rectangle(LeftRect.X + LeftRect.Width - 20 - 12 * i, LeftRect.Y + 12, 12, 11);
                        if (star.HitTest(MousePos))
                        {
                            ToolTip.CreateTooltip(127);
                        }
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_star"], star, Color.White);
                    }
                }
            }

            foreach (ScrollList.Entry e in DescriptionSL.VisibleEntries)
            {
                string t1 = e.item as string;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, t1, new Vector2(DefenseRect.X, e.Y), Color.White);
            }
            DescriptionSL.Draw(ScreenManager.SpriteBatch);
        }


        private void DrawinfoData(SpriteBatch batch, Rectangle rect, string data, Color color)
        {
            SpriteFont font = Fonts.Arial12;
            Vector2 pos = new Vector2((rect.X + rect.Width + 2), (rect.Y + 11 - font.LineSpacing / 2));
            batch.DrawString(font, data, pos, color);
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
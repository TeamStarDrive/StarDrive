using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class PlanetInfoUIElement : UIElement
    {
        private Rectangle SliderRect;

        private Rectangle clickRect;

        private UniverseScreen screen;

        private Rectangle LeftRect;

        private Rectangle RightRect;

        private Rectangle PlanetIconRect;

        private Rectangle flagRect;

        private Rectangle moneyRect;
        private Rectangle SendTroops;

        private Rectangle popRect;

        private string PlanetTypeRichness;

        private Vector2 PlanetTypeCursor;

        public Planet p;

        private Selector sel;

        private SkinnableButton Inspect;

        private SkinnableButton Invade;

        private ColonyScreen.ColonySlider ColonySliderFood;

        private ColonyScreen.ColonySlider ColonySliderProd;

        private ColonyScreen.ColonySlider ColonySliderRes;

        private ColonyScreen.Lock FoodLock;

        private ColonyScreen.Lock ProdLock;

        private ColonyScreen.Lock ResLock;

        private Rectangle Housing;

        private Rectangle DefenseRect;

        private Rectangle ShieldRect;

        private bool draggingSlider1;

        private bool draggingSlider2;

        private bool draggingSlider3;

        private Array<PlanetInfoUIElement.TippedItem> ToolTipItems = new Array<PlanetInfoUIElement.TippedItem>();

        private float slider1Last;

        private float slider2Last;

        private float slider3Last;

        private float rPercentLast;

        private float fPercentLast;

        private float pPercentLast;

        //private Rectangle BackButton;

        private string fmt = "0.#";

        private Rectangle Mark;

        public PlanetInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
        {
            this.screen = screen;
            this.ScreenManager = sm;
            this.ElementRect = r;
            this.sel = new Selector(r, Color.Black);
            this.Housing = r;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
            this.SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
            this.clickRect = new Rectangle(this.ElementRect.X + this.ElementRect.Width - 16, this.ElementRect.Y + this.ElementRect.Height / 2 - 11, 11, 22);
            this.LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            this.RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            this.PlanetIconRect = new Rectangle(this.LeftRect.X + 55, this.Housing.Y + 120, 80, 80);
            this.Inspect = new SkinnableButton(new Rectangle(this.PlanetIconRect.X + this.PlanetIconRect.Width / 2 - 16, this.PlanetIconRect.Y, 32, 32), "UI/viewPlanetIcon")
            {
                HoverColor = this.tColor,
                IsToggle = false
            };
            this.Invade = new SkinnableButton(new Rectangle(this.PlanetIconRect.X + this.PlanetIconRect.Width / 2 - 16, this.PlanetIconRect.Y + 48, 32, 32), "UI/ColonizeIcon")
            {
                HoverColor = this.tColor,
                IsToggle = false
            };
            this.ColonySliderFood = new ColonyScreen.ColonySlider()
            {
                sRect = new Rectangle(this.RightRect.X, this.Housing.Y + 120, 145, 6)
            };
            this.ColonySliderProd = new ColonyScreen.ColonySlider()
            {
                sRect = new Rectangle(this.RightRect.X, this.Housing.Y + 160, 145, 6)
            };
            this.ColonySliderRes = new ColonyScreen.ColonySlider()
            {
                sRect = new Rectangle(this.RightRect.X, this.Housing.Y + 200, 145, 6)
            };
            this.FoodLock = new ColonyScreen.Lock();
            this.ResLock = new ColonyScreen.Lock();
            this.ProdLock = new ColonyScreen.Lock();
            this.flagRect = new Rectangle(r.X + r.Width - 60, this.Housing.Y + 63, 26, 26);
            this.DefenseRect = new Rectangle(this.LeftRect.X + 13, this.Housing.Y + 112, 22, 22);
            this.ShieldRect = new Rectangle(this.LeftRect.X + 13, this.Housing.Y + 112 + 75, 22, 22);
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.p == null) return;  //fbedard

            string str;
            string str1;
            MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
            this.ToolTipItems.Clear();
            PlanetInfoUIElement.TippedItem def = new PlanetInfoUIElement.TippedItem()
            {
                r = this.DefenseRect,
                TIP_ID = 31
            };
            this.ToolTipItems.Add(def);
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/unitselmenu_main"], this.Housing, Color.White);
            Vector2 NamePos = new Vector2((float)(this.Housing.X + 41), (float)(this.Housing.Y + 65));
            if (this.p.Owner == null || !this.p.IsExploredBy(EmpireManager.Player))
            {
                if (!this.p.IsExploredBy(EmpireManager.Player))
                {
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(Localizer.Token(1429), this.p.GetTypeTranslation()), NamePos, this.tColor);
                    Vector2 TextCursor2 = new Vector2((float)(this.sel.Rect.X + this.sel.Rect.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
                    float population = this.p.Population / 1000f;
                    //renamed textcursor,popstring
                    string popString2 = population.ToString(this.fmt);
                    float maxPopulation = this.p.MaxPopulation / 1000f + this.p.MaxPopBonus / 1000f;
                    popString2 = string.Concat(popString2, " / ", maxPopulation.ToString(this.fmt));
                    TextCursor2.X = TextCursor2.X - (Fonts.Arial12Bold.MeasureString(popString2).X + 5f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString2, TextCursor2, this.tColor);

                    this.popRect = new Rectangle((int)TextCursor2.X - 23, (int)TextCursor2.Y - 3, 22, 22);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_pop_22"], this.popRect, Color.White);

                    string text = Localizer.Token(1430);
                    Vector2 Cursor = new Vector2((float)(this.Housing.X + 20), (float)(this.Housing.Y + 115));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, this.tColor);
                    return;
                }
                if (!this.p.Habitable)
                {
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
                    string text = Localizer.Token(1427);
                    Vector2 Cursor = new Vector2((float)(this.Housing.X + 20), (float)(this.Housing.Y + 115));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, this.tColor);
                    return;
                }
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
                Vector2 TextCursor = new Vector2((float)(this.sel.Rect.X + this.sel.Rect.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
                float single = this.p.Population / 1000f;
                string popString = single.ToString(this.fmt);
                float maxPopulation1 = this.p.MaxPopulation / 1000f + this.p.MaxPopBonus / 1000f;
                popString = string.Concat(popString, " / ", maxPopulation1.ToString(this.fmt));
                TextCursor.X = TextCursor.X - (Fonts.Arial12Bold.MeasureString(popString).X + 5f);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString, TextCursor, this.tColor);

                this.popRect = new Rectangle((int)TextCursor.X - 23, (int)TextCursor.Y - 3, 22, 22);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_pop_22"], this.popRect, Color.White);

                this.PlanetTypeRichness = string.Concat(this.p.GetTypeTranslation(), " ", this.p.GetRichness());
                this.PlanetTypeCursor = new Vector2((float)(this.PlanetIconRect.X + this.PlanetIconRect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.PlanetTypeRichness).X / 2f, (float)(this.PlanetIconRect.Y + this.PlanetIconRect.Height + 5));
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.PlanetType)], this.PlanetIconRect, Color.White);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.PlanetTypeRichness, this.PlanetTypeCursor, this.tColor);
                Rectangle fIcon = new Rectangle(240, this.Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.TextureDict["NewUI/icon_food"].Height, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
                PlanetInfoUIElement.TippedItem ti = new PlanetInfoUIElement.TippedItem()
                {
                    r = fIcon,
                    TIP_ID = 20
                };
                this.ToolTipItems.Add(ti);
                Vector2 tcurs = new Vector2((float)(fIcon.X + 25), (float)(this.Housing.Y + 205));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.Fertility.ToString(this.fmt), tcurs, this.tColor);
                Rectangle pIcon = new Rectangle(300, this.Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.TextureDict["NewUI/icon_production"].Height, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], pIcon, Color.White);
                ti = new PlanetInfoUIElement.TippedItem()
                {
                    r = pIcon,
                    TIP_ID = 21
                };
                this.ToolTipItems.Add(ti);
                tcurs = new Vector2(325f, (float)(this.Housing.Y + 205));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.MineralRichness.ToString(this.fmt), tcurs, this.tColor);
                this.Mark = new Rectangle(this.RightRect.X - 10, this.Housing.Y + 150, 182, 25);
                Vector2 Text = new Vector2((float)(this.RightRect.X + 25), (float)(this.Mark.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2));
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button_blue"], this.Mark, Color.White);
                if (GlobalStats.IsGermanOrPolish)
                {
                    Text.X = Text.X - 9f;
                }
                bool marked = false;
                foreach (Goal g in EmpireManager.Player.GetGSAI().Goals)
                {
                    if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != this.p)
                    {
                        continue;
                    }
                    marked = true;
                }
                if (marked)
                {
                    if (!this.Mark.HitTest(MousePos))
                    {
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text, new Color(88, 108, 146));
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text, new Color(174, 202, 255));
                    }
                    ti = new PlanetInfoUIElement.TippedItem()
                    {
                        r = this.Mark,
                        TIP_ID = 25
                    };
                    this.ToolTipItems.Add(ti);
                }
                else
                {
                    if (!this.Mark.HitTest(MousePos))
                    {
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text, new Color(88, 108, 146));
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text, new Color(174, 202, 255));
                    }
                    ti = new PlanetInfoUIElement.TippedItem()
                    {
                        r = this.Mark,
                        TIP_ID = 24
                    };
                    this.ToolTipItems.Add(ti);
                }

                //Ship troopShip
                ti = new PlanetInfoUIElement.TippedItem()
                {
                    r = pIcon,
                    TIP_ID = 21
                };
                int troops = 0;
                this.ToolTipItems.Add(ti);

                this.SendTroops = new Rectangle(this.Mark.X, this.Mark.Y - this.Mark.Height -5, 182, 25);
                Text = new Vector2((float)(SendTroops.X + 25), (float)(SendTroops.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2));
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button_blue"], SendTroops, Color.White);         
                troops= this.screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0 )
                     .Where(troopAI => troopAI.AI.OrderQueue
                         .Where(goal => goal.TargetPlanet != null && goal.TargetPlanet == p).Count() >0).Count();
                if (!this.SendTroops.HitTest(MousePos))
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,String.Concat("Invading : ",troops) , Text, new Color(88, 108, 146)); // Localizer.Token(1425)
                else
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text, new Color(174, 202, 255)); // Localizer.Token(1425)

                this.Inspect.Draw(this.ScreenManager);
                this.Invade.Draw(this.ScreenManager);
                return;
            }
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
            SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
            KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[this.p.Owner.data.Traits.FlagIndex];
            spriteBatch.Draw(item.Value, this.flagRect, this.p.Owner.EmpireColor);
            Vector2 TextCursor3 = new Vector2((float)(this.sel.Rect.X + this.sel.Rect.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
            float population1 = this.p.Population / 1000f;
            string popString3 = population1.ToString(this.fmt);
            float single1 = this.p.MaxPopulation / 1000f + this.p.MaxPopBonus / 1000f;
            popString3 = string.Concat(popString3, " / ", single1.ToString(this.fmt));
            TextCursor3.X = TextCursor3.X - (Fonts.Arial12Bold.MeasureString(popString3).X + 5f);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString3, TextCursor3, this.tColor);

            this.popRect = new Rectangle((int)TextCursor3.X - 23, (int)TextCursor3.Y - 3, 22, 22);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_pop_22"], this.popRect, Color.White);

            this.moneyRect = new Rectangle((int)this.popRect.X - 70, (int)this.popRect.Y, 22, 22);
            Vector2 TextCursorMoney = new Vector2((float)this.moneyRect.X + 24, (float)TextCursor3.Y);

            float taxRate = this.p.Owner.data.TaxRate;
            float grossIncome = this.p.GrossIncome;
            float grossUpkeepPI = (float)((double)this.p.TotalMaintenanceCostsPerTurn + (double)this.p.TotalMaintenanceCostsPerTurn * (double)this.p.Owner.data.Traits.MaintMod);
            float netIncomePI = (float)(grossIncome - grossUpkeepPI);

            if (p.Owner == EmpireManager.Player)
            {
                string sNetIncome = netIncomePI.ToString("F2");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sNetIncome, TextCursorMoney, netIncomePI > 0.0 ? Color.LightGreen : Color.Salmon);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_money_22"], moneyRect, Color.White);
            }

            this.PlanetTypeRichness = string.Concat(this.p.GetTypeTranslation(), " ", this.p.GetRichness());
            this.PlanetTypeCursor = new Vector2((float)(this.PlanetIconRect.X + this.PlanetIconRect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.PlanetTypeRichness).X / 2f, (float)(this.PlanetIconRect.Y + this.PlanetIconRect.Height + 5));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.PlanetType)], this.PlanetIconRect, Color.White);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.PlanetTypeRichness, this.PlanetTypeCursor, this.tColor);
            this.p.UpdateIncomes(false);
            this.ColonySliderFood.amount = this.p.FarmerPercentage;
            this.ColonySliderFood.cursor = new Rectangle(this.ColonySliderFood.sRect.X + (int)((float)this.ColonySliderFood.sRect.Width * this.ColonySliderFood.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
            this.ColonySliderProd.amount = this.p.WorkerPercentage;
            this.ColonySliderProd.cursor = new Rectangle(this.ColonySliderProd.sRect.X + (int)((float)this.ColonySliderProd.sRect.Width * this.ColonySliderProd.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
            this.ColonySliderRes.amount = this.p.ResearcherPercentage;
            this.ColonySliderRes.cursor = new Rectangle(this.ColonySliderRes.sRect.X + (int)((float)this.ColonySliderRes.sRect.Width * this.ColonySliderRes.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.ColonySliderFood.sRect.X, this.ColonySliderFood.sRect.Y, (int)(this.ColonySliderFood.amount * (float)this.ColonySliderFood.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderFood.sRect.X, this.ColonySliderFood.sRect.Y, (int)(this.ColonySliderFood.amount * (float)this.ColonySliderFood.sRect.Width), 6)), (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderFood.sRect, this.ColonySliderFood.Color);
            Rectangle fIcon2 = new Rectangle(this.ColonySliderFood.sRect.X - 35, this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon2, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : new Color(110, 110, 110, 255)));
            PlanetInfoUIElement.TippedItem ti1 = new PlanetInfoUIElement.TippedItem()
            {
                r = fIcon2
            };
            if (this.p.Owner.data.Traits.Cybernetic != 0)
            {
                ti1.TIP_ID = 77;
            }
            else
            {
                ti1.TIP_ID = 70;
            }
            this.ToolTipItems.Add(ti1);
            if (this.ColonySliderFood.cState != "normal")
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            }
            else
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            }
            Vector2 tickCursor = new Vector2();
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width / 10 * i), (float)(this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height + 2));
                if (this.ColonySliderFood.state != "normal")
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
                }
                else
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
                }
            }
            Vector2 textPos = new Vector2((float)(this.ColonySliderFood.sRect.X + 180), (float)(this.ColonySliderFood.sRect.Y - 2));
            if (this.p.Owner.data.Traits.Cybernetic == 0)
            {
                float netFoodPerTurn = this.p.NetFoodPerTurn - this.p.Consumption;
                str = netFoodPerTurn.ToString(this.fmt);
            }
            else
            {
                str = "0";
            }
            string food = str;
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(food).X;
            if (this.p.NetFoodPerTurn - this.p.Consumption >= 0f || this.p.Owner.data.Traits.Cybernetic == 1)
            {
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, new Color(255, 239, 208));
            }
            else
            {
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, Color.LightPink);
            }
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_brown"], new Rectangle(this.ColonySliderProd.sRect.X, this.ColonySliderProd.sRect.Y, (int)(this.ColonySliderProd.amount * (float)this.ColonySliderProd.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderProd.sRect.X, this.ColonySliderProd.sRect.Y, (int)(this.ColonySliderProd.amount * (float)this.ColonySliderProd.sRect.Width), 6)), Color.White);
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderProd.sRect, this.ColonySliderProd.Color);
            Rectangle pIcon1 = new Rectangle(this.ColonySliderProd.sRect.X - 35, this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], pIcon1, Color.White);
            ti1 = new PlanetInfoUIElement.TippedItem()
            {
                r = pIcon1,
                TIP_ID = 71
            };
            this.ToolTipItems.Add(ti1);
            if (this.ColonySliderProd.cState != "normal")
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderProd.cursor, Color.White);
            }
            else
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderProd.cursor, Color.White);
            }
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderProd.sRect.Width / 10 * i), (float)(this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height + 2));
                if (this.ColonySliderProd.state != "normal")
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
                }
                else
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
                }
            }
            textPos = new Vector2((float)(this.ColonySliderProd.sRect.X + 180), (float)(this.ColonySliderProd.sRect.Y - 2));
            if (this.p.Owner.data.Traits.Cybernetic != 0)
            {
                float netProductionPerTurn = this.p.NetProductionPerTurn - this.p.Consumption;
                str1 = netProductionPerTurn.ToString(this.fmt);
            }
            else
            {
                str1 = this.p.NetProductionPerTurn.ToString(this.fmt);
            }
            string prod = str1;
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(prod).X;
            if (this.p.Owner.data.Traits.Cybernetic == 0 || this.p.NetProductionPerTurn - this.p.Consumption >= 0f)
            {
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, new Color(255, 239, 208));
            }
            else
            {
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, Color.LightPink);
            }
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_blue"], new Rectangle(this.ColonySliderRes.sRect.X, this.ColonySliderRes.sRect.Y, (int)(this.ColonySliderRes.amount * (float)this.ColonySliderRes.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderRes.sRect.X, this.ColonySliderRes.sRect.Y, (int)(this.ColonySliderRes.amount * (float)this.ColonySliderRes.sRect.Width), 6)), Color.White);
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderRes.sRect, this.ColonySliderRes.Color);
            Rectangle rIcon = new Rectangle(this.ColonySliderRes.sRect.X - 35, this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_science"].Height / 2, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rIcon, Color.White);
            ti1 = new PlanetInfoUIElement.TippedItem()
            {
                r = rIcon,
                TIP_ID = 72
            };
            this.ToolTipItems.Add(ti1);
            if (this.ColonySliderRes.cState != "normal")
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderRes.cursor, Color.White);
            }
            else
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderRes.cursor, Color.White);
            }
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderRes.sRect.Width / 10 * i), (float)(this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height + 2));
                if (this.ColonySliderRes.state != "normal")
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
                }
                else
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
                }
            }
            textPos = new Vector2((float)(this.ColonySliderRes.sRect.X + 180), (float)(this.ColonySliderRes.sRect.Y - 2));
            string res = this.p.NetResearchPerTurn.ToString(this.fmt);
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(res).X;
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, res, textPos, new Color(255, 239, 208));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], this.DefenseRect, Color.White);
            Vector2 defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.TotalDefensiveStrength.ToString(this.fmt), defPos, Color.White);
            if (this.p.ShieldStrengthMax > 0f)
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_planetshield"], this.ShieldRect, Color.Green);
                Vector2 shieldPos = new Vector2((float)(this.ShieldRect.X + this.ShieldRect.Width + 2), (float)(this.ShieldRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.ShieldStrengthCurrent.ToString(this.fmt), shieldPos, Color.White);
            }
            this.Inspect.Draw(this.ScreenManager);
            this.Invade.Draw(this.ScreenManager);
        }

        public override bool HandleInput(InputState input)
        {
            if (this.p == null)
            {
                return false;
            }
            if (this.ShieldRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2240));
            }
            foreach (PlanetInfoUIElement.TippedItem ti in this.ToolTipItems)
            {
                if (!ti.r.HitTest(input.CursorPosition))
                {
                    continue;
                }
                ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (this.Mark.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                bool marked = false;
                Goal markedGoal = null;
                foreach (Goal g in EmpireManager.Player.GetGSAI().Goals)
                {
                    if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
                    {
                        continue;
                    }
                    marked = true;
                    markedGoal = g;
                }
                if (marked)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    if (markedGoal.GetColonyShip() != null)
                    {
                        lock (markedGoal.GetColonyShip())
                        {
                            markedGoal.GetColonyShip().AI.OrderQueue.Clear();
                            markedGoal.GetColonyShip().AI.State = AIState.AwaitingOrders;
                        }
                    }
                    EmpireManager.Player.GetGSAI().Goals.QueuePendingRemoval(markedGoal);
                    EmpireManager.Player.GetGSAI().Goals.ApplyPendingRemovals();
                }
                else
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    EmpireManager.Player.GetGSAI().Goals.Add(new MarkForColonization(p, EmpireManager.Player));
                }
            }
            if (this.SendTroops.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                Array<Ship> troopShips = new Array<Ship>(this.screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0
                         && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                         && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                Array<Planet> planetTroops = new Array<Planet>(this.screen.player.GetPlanets().Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                if (troopShips.Count > 0)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    troopShips.First().AI.OrderAssaultPlanet(this.p);

                }
                else
                    if (planetTroops.Count > 0)
                    {
                        {
                            Ship troop = planetTroops.First().TroopsHere.First().Launch();
                            if (troop != null)
                            {
                                GameAudio.PlaySfxAsync("echo_affirm");                              
                                troop.AI.OrderAssaultPlanet(this.p);
                            }
                        }
                    }
                    else
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                    }
                

            }

            if (this.Inspect.Hover)
            {
                if (this.p.Owner == null || this.p.Owner != EmpireManager.Player)
                {
                    ToolTip.CreateTooltip(61);
                }
                else
                {
                    ToolTip.CreateTooltip(76);
                }
            }
            if (this.Invade.Hover)
            {
                ToolTip.CreateTooltip(62);
            }
            if (this.p.Habitable)
            {
                if (this.Inspect.HandleInput(input))
                {
                    this.screen.ViewPlanet(null);
                }
                if (this.Invade.HandleInput(input))
                {
                    this.screen.OpenCombatMenu(null);
                }
            }
            if (!this.ElementRect.HitTest(input.CursorPosition))
            {
                return false;
            }
            if (this.p.Owner != null && this.p.Owner == EmpireManager.Player)
            {
                this.HandleSlider(input);
            }
            return true;
        }

        private void HandleSlider(InputState input)
        {
            if (this.p == null)
            {
                return;
            }
            this.p.UpdateIncomes(false);
            Vector2 mousePos = input.CursorPosition;
            if (this.p.Owner.data.Traits.Cybernetic == 0)
            {
                if (this.ColonySliderFood.sRect.HitTest(mousePos) || this.draggingSlider1)
                {
                    this.ColonySliderFood.state = "hover";
                    this.ColonySliderFood.Color = new Color(164, 154, 133);
                }
                else
                {
                    this.ColonySliderFood.state = "normal";
                    this.ColonySliderFood.Color = new Color(72, 61, 38);
                }
                if (this.ColonySliderFood.cursor.HitTest(mousePos) || this.draggingSlider1)
                {
                    this.ColonySliderFood.cState = "hover";
                }
                else
                {
                    this.ColonySliderFood.cState = "normal";
                }
            }
            if (this.ColonySliderProd.sRect.HitTest(mousePos) || this.draggingSlider2)
            {
                this.ColonySliderProd.state = "hover";
                this.ColonySliderProd.Color = new Color(164, 154, 133);
            }
            else
            {
                this.ColonySliderProd.state = "normal";
                this.ColonySliderProd.Color = new Color(72, 61, 38);
            }
            if (this.ColonySliderProd.cursor.HitTest(mousePos) || this.draggingSlider2)
            {
                this.ColonySliderProd.cState = "hover";
            }
            else
            {
                this.ColonySliderProd.cState = "normal";
            }
            if (this.ColonySliderRes.sRect.HitTest(mousePos) || this.draggingSlider3)
            {
                this.ColonySliderRes.state = "hover";
                this.ColonySliderRes.Color = new Color(164, 154, 133);
            }
            else
            {
                this.ColonySliderRes.state = "normal";
                this.ColonySliderRes.Color = new Color(72, 61, 38);
            }
            if (this.ColonySliderRes.cursor.HitTest(mousePos) || this.draggingSlider3)
            {
                this.ColonySliderRes.cState = "hover";
            }
            else
            {
                this.ColonySliderRes.cState = "normal";
            }
            if (this.p.Owner.data.Traits.Cybernetic == 0 && this.ColonySliderFood.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                this.draggingSlider1 = true;
            }
            if (this.ColonySliderProd.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                this.draggingSlider2 = true;
            }
            if (this.ColonySliderRes.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                this.draggingSlider3 = true;
            }
            if (this.draggingSlider1 && !this.FoodLock.Locked && (!this.ProdLock.Locked || !this.ResLock.Locked) && this.p.Owner.data.Traits.Cybernetic == 0)
            {
                this.ColonySliderFood.cursor.X = input.MouseCurr.X;
                if (this.ColonySliderFood.cursor.X > this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width)
                {
                    this.ColonySliderFood.cursor.X = this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width;
                }
                else if (this.ColonySliderFood.cursor.X < this.ColonySliderFood.sRect.X)
                {
                    this.ColonySliderFood.cursor.X = this.ColonySliderFood.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    this.draggingSlider1 = false;
                }
                this.fPercentLast = this.p.FarmerPercentage;
                this.p.FarmerPercentage = ((float)this.ColonySliderFood.cursor.X - (float)this.ColonySliderFood.sRect.X) / (float)this.ColonySliderFood.sRect.Width;
                float difference = this.fPercentLast - this.p.FarmerPercentage;
                if (!this.ProdLock.Locked && !this.ResLock.Locked)
                {
                    Planet workerPercentage = this.p;
                    workerPercentage.WorkerPercentage = workerPercentage.WorkerPercentage + difference / 2f;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet farmerPercentage = this.p;
                        farmerPercentage.FarmerPercentage = farmerPercentage.FarmerPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                    Planet researcherPercentage = this.p;
                    researcherPercentage.ResearcherPercentage = researcherPercentage.ResearcherPercentage + difference / 2f;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet planet = this.p;
                        planet.FarmerPercentage = planet.FarmerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (this.ProdLock.Locked && !this.ResLock.Locked)
                {
                    Planet researcherPercentage1 = this.p;
                    researcherPercentage1.ResearcherPercentage = researcherPercentage1.ResearcherPercentage + difference;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet farmerPercentage1 = this.p;
                        farmerPercentage1.FarmerPercentage = farmerPercentage1.FarmerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (!this.ProdLock.Locked && this.ResLock.Locked)
                {
                    Planet workerPercentage1 = this.p;
                    workerPercentage1.WorkerPercentage = workerPercentage1.WorkerPercentage + difference;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet planet1 = this.p;
                        planet1.FarmerPercentage = planet1.FarmerPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                }
            }
            if (this.draggingSlider2 && !this.ProdLock.Locked && (!this.FoodLock.Locked || !this.ResLock.Locked))
            {
                this.ColonySliderProd.cursor.X = input.MouseCurr.X;
                if (this.ColonySliderProd.cursor.X > this.ColonySliderProd.sRect.X + this.ColonySliderProd.sRect.Width)
                {
                    this.ColonySliderProd.cursor.X = this.ColonySliderProd.sRect.X + this.ColonySliderProd.sRect.Width;
                }
                else if (this.ColonySliderProd.cursor.X < this.ColonySliderProd.sRect.X)
                {
                    this.ColonySliderProd.cursor.X = this.ColonySliderProd.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    this.draggingSlider2 = false;
                }
                this.pPercentLast = this.p.WorkerPercentage;
                this.p.WorkerPercentage = ((float)this.ColonySliderProd.cursor.X - (float)this.ColonySliderProd.sRect.X) / (float)this.ColonySliderProd.sRect.Width;
                float difference = this.pPercentLast - this.p.WorkerPercentage;
                if (!this.FoodLock.Locked && !this.ResLock.Locked)
                {
                    Planet farmerPercentage2 = this.p;
                    farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + difference / 2f;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet workerPercentage2 = this.p;
                        workerPercentage2.WorkerPercentage = workerPercentage2.WorkerPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                    Planet researcherPercentage2 = this.p;
                    researcherPercentage2.ResearcherPercentage = researcherPercentage2.ResearcherPercentage + difference / 2f;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet planet2 = this.p;
                        planet2.WorkerPercentage = planet2.WorkerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (this.FoodLock.Locked && !this.ResLock.Locked)
                {
                    Planet researcherPercentage3 = this.p;
                    researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet workerPercentage3 = this.p;
                        workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (!this.FoodLock.Locked && this.ResLock.Locked)
                {
                    Planet farmerPercentage3 = this.p;
                    farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + difference;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet planet3 = this.p;
                        planet3.WorkerPercentage = planet3.WorkerPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
            }
            if (this.draggingSlider3 && !this.ResLock.Locked && (!this.FoodLock.Locked || !this.ProdLock.Locked))
            {
                this.ColonySliderRes.cursor.X = input.MouseCurr.X;
                if (this.ColonySliderRes.cursor.X > this.ColonySliderRes.sRect.X + this.ColonySliderRes.sRect.Width)
                {
                    this.ColonySliderRes.cursor.X = this.ColonySliderRes.sRect.X + this.ColonySliderRes.sRect.Width;
                }
                else if (this.ColonySliderRes.cursor.X < this.ColonySliderRes.sRect.X)
                {
                    this.ColonySliderRes.cursor.X = this.ColonySliderRes.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    this.draggingSlider3 = false;
                }
                this.rPercentLast = this.p.ResearcherPercentage;
                this.p.ResearcherPercentage = ((float)this.ColonySliderRes.cursor.X - (float)this.ColonySliderRes.sRect.X) / (float)this.ColonySliderRes.sRect.Width;
                float difference = this.rPercentLast - this.p.ResearcherPercentage;
                if (!this.ProdLock.Locked && !this.FoodLock.Locked)
                {
                    Planet workerPercentage4 = this.p;
                    workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + difference / 2f;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet researcherPercentage4 = this.p;
                        researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                    Planet farmerPercentage4 = this.p;
                    farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference / 2f;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet planet4 = this.p;
                        planet4.ResearcherPercentage = planet4.ResearcherPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
                else if (this.ProdLock.Locked && !this.FoodLock.Locked)
                {
                    Planet farmerPercentage5 = this.p;
                    farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet researcherPercentage5 = this.p;
                        researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
                else if (!this.ProdLock.Locked && this.FoodLock.Locked)
                {
                    Planet workerPercentage5 = this.p;
                    workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet planet5 = this.p;
                        planet5.ResearcherPercentage = planet5.ResearcherPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                }
            }
            MathHelper.Clamp(this.p.FarmerPercentage, 0f, 1f);
            MathHelper.Clamp(this.p.WorkerPercentage, 0f, 1f);
            MathHelper.Clamp(this.p.ResearcherPercentage, 0f, 1f);
            this.slider1Last = (float)this.ColonySliderFood.cursor.X;
            this.slider2Last = (float)this.ColonySliderProd.cursor.X;
            this.slider3Last = (float)this.ColonySliderRes.cursor.X;
        }

        public void SetPlanet(Planet p)
        {
            if (p.Owner != null && p.Owner.data.Traits.Cybernetic != 0)
            {
                p.FoodLocked = true;
            }
            this.p = p;
            this.FoodLock.Locked = p.FoodLocked;
            this.ResLock.Locked = p.ResLocked;
            this.ProdLock.Locked = p.ProdLocked;
        }

        private struct TippedItem
        {
            public Rectangle r;

            public int TIP_ID;
        }
    }
}
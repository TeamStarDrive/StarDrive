using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
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
        private Rectangle InjuryRect;
        private Rectangle OffenseRect;

        private Rectangle ShieldRect;

        private bool draggingSlider1;

        private bool draggingSlider2;

        private bool draggingSlider3;

        private Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        private float slider1Last;

        private float slider2Last;

        private float slider3Last;

        private float rPercentLast;

        private float fPercentLast;

        private float pPercentLast;

        //private Rectangle BackButton;

        private string fmt = "0.#";

        private Rectangle Mark;

        public PlanetInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            sel = new Selector(r, Color.Black);
            Housing = r;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
            clickRect = new Rectangle(ElementRect.X + ElementRect.Width - 16, ElementRect.Y + ElementRect.Height / 2 - 11, 11, 22);
            LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            PlanetIconRect = new Rectangle(LeftRect.X + 75, Housing.Y + 120, 80, 80);
            Inspect = new SkinnableButton(new Rectangle(PlanetIconRect.X + PlanetIconRect.Width / 2 - 16, PlanetIconRect.Y, 32, 32), "UI/viewPlanetIcon")
            {
                HoverColor = tColor,
                IsToggle = false
            };
            Invade = new SkinnableButton(new Rectangle(PlanetIconRect.X + PlanetIconRect.Width / 2 - 16, PlanetIconRect.Y + 48, 32, 32), "UI/ColonizeIcon")
            {
                HoverColor = tColor,
                IsToggle = false
            };
            ColonySliderFood = new ColonyScreen.ColonySlider(ColonyScreen.ColonySlider.Food)
            {
                sRect = new Rectangle(RightRect.X, Housing.Y + 120, 145, 6)
            };
            ColonySliderProd = new ColonyScreen.ColonySlider(ColonyScreen.ColonySlider.Production)
            {
                sRect = new Rectangle(RightRect.X, Housing.Y + 160, 145, 6)
            };
            ColonySliderRes = new ColonyScreen.ColonySlider(ColonyScreen.ColonySlider.Research)
            {
                sRect = new Rectangle(RightRect.X, Housing.Y + 200, 145, 6)
            };
            FoodLock    = new ColonyScreen.Lock();
            ResLock     = new ColonyScreen.Lock();
            ProdLock    = new ColonyScreen.Lock();
            flagRect    = new Rectangle(r.X + r.Width - 60, Housing.Y + 63, 26, 26);
            DefenseRect = new Rectangle(LeftRect.X + 13, Housing.Y + 114, 22, 22);
            OffenseRect = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 22, 22, 22);
            InjuryRect  = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 44, 22, 22);
            ShieldRect  = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 66, 22, 22);
        }

        public override void Draw(GameTime gameTime)
        {
            if (p == null) return;  //fbedard

            string str;
            string str1;
            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ToolTipItems.Clear();
            TippedItem def = new TippedItem
            {
                r = DefenseRect,
                TIP_ID = 31
            };
            ToolTipItems.Add(def);
            TippedItem injury = new TippedItem
            {
                r = InjuryRect,
                TIP_ID = 249
            };
            ToolTipItems.Add(injury);
            TippedItem offense = new TippedItem
            {
                r = OffenseRect,
                TIP_ID = 250
            };
            ToolTipItems.Add(offense);
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, state.Y);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            Vector2 NamePos = new Vector2(Housing.X + 41, Housing.Y + 65);
            if (p.Owner == null || !p.IsExploredBy(EmpireManager.Player))
            {
                if (!p.IsExploredBy(EmpireManager.Player))
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(Localizer.Token(1429), p.GetTypeTranslation()), NamePos, tColor);
                    Vector2 TextCursor2 = new Vector2(sel.Rect.X + sel.Rect.Width - 65, NamePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);
                    float population = p.Population / 1000f;
                    //renamed textcursor,popstring
                    string popString2 = population.ToString(fmt);
                    float maxPopulation = p.MaxPopulation / 1000f + p.MaxPopBonus / 1000f;
                    popString2 = string.Concat(popString2, " / ", maxPopulation.ToString(fmt));
                    TextCursor2.X = TextCursor2.X - (Fonts.Arial12Bold.MeasureString(popString2).X + 5f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString2, TextCursor2, tColor);

                    popRect = new Rectangle((int)TextCursor2.X - 23, (int)TextCursor2.Y - 3, 22, 22);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

                    string text = Localizer.Token(1430);
                    Vector2 Cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, tColor);
                    return;
                }
                if (!p.Habitable)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, p.Name, NamePos, tColor);
                    string text = Localizer.Token(1427);
                    Vector2 Cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, tColor);
                    return;
                }
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, p.Name, NamePos, tColor);
                Vector2 TextCursor = new Vector2(sel.Rect.X + sel.Rect.Width - 65, NamePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);
                float single = p.Population / 1000f;
                string popString = single.ToString(fmt);
                float maxPopulation1 = p.MaxPopulation / 1000f + p.MaxPopBonus / 1000f;
                popString = string.Concat(popString, " / ", maxPopulation1.ToString(fmt));
                TextCursor.X = TextCursor.X - (Fonts.Arial12Bold.MeasureString(popString).X + 5f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString, TextCursor, tColor);

                popRect = new Rectangle((int)TextCursor.X - 23, (int)TextCursor.Y - 3, 22, 22);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

                PlanetTypeRichness = string.Concat(p.GetTypeTranslation(), " ", p.GetRichness());
                PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f, PlanetIconRect.Y + PlanetIconRect.Height + 5);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Planets/", p.PlanetType)), PlanetIconRect, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
                Rectangle fIcon = new Rectangle(240, Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_food").Height, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                TippedItem ti = new TippedItem
                {
                    r = fIcon,
                    TIP_ID = 20
                };
                ToolTipItems.Add(ti);
                Vector2 tcurs = new Vector2(fIcon.X + 25, Housing.Y + 205);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.Fertility.ToString(fmt), tcurs, tColor);
                Rectangle pIcon = new Rectangle(300, Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_production").Height, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);
                ti = new TippedItem
                {
                    r = pIcon,
                    TIP_ID = 21
                };
                ToolTipItems.Add(ti);
                tcurs = new Vector2(325f, Housing.Y + 205);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.MineralRichness.ToString(fmt), tcurs, tColor);
                Mark = new Rectangle(RightRect.X - 10, Housing.Y + 150, 182, 25);
                Vector2 Text = new Vector2(RightRect.X + 25, Mark.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/dan_button_blue"), Mark, Color.White);
                if (GlobalStats.IsGermanOrPolish)
                {
                    Text.X = Text.X - 9f;
                }
                bool marked = false;
                foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
                {
                    if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
                    {
                        continue;
                    }
                    marked = true;
                }
                if (marked)
                {
                    if (!Mark.HitTest(MousePos))
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text, new Color(88, 108, 146));
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text, new Color(174, 202, 255));
                    }
                    ti = new TippedItem
                    {
                        r = Mark,
                        TIP_ID = 25
                    };
                    ToolTipItems.Add(ti);
                }
                else
                {
                    if (!Mark.HitTest(MousePos))
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text, new Color(88, 108, 146));
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text, new Color(174, 202, 255));
                    }
                    ti = new TippedItem
                    {
                        r = Mark,
                        TIP_ID = 24
                    };
                    ToolTipItems.Add(ti);
                }

                //Ship troopShip
                ti = new TippedItem
                {
                    r = pIcon,
                    TIP_ID = 21
                };
                int troops = 0;
                ToolTipItems.Add(ti);

                SendTroops = new Rectangle(Mark.X, Mark.Y - Mark.Height -5, 182, 25);
                Text = new Vector2(SendTroops.X + 25, SendTroops.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/dan_button_blue"), SendTroops, Color.White);         
                troops= screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0 )
                     .Where(troopAI => troopAI.AI.OrderQueue
                         .Where(goal => goal.TargetPlanet != null && goal.TargetPlanet == p).Count() >0).Count();
                if (!SendTroops.HitTest(MousePos))
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,String.Concat("Invading : ",troops) , Text, new Color(88, 108, 146)); // Localizer.Token(1425)
                else
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text, new Color(174, 202, 255)); // Localizer.Token(1425)

                Inspect.Draw(ScreenManager);
                Invade.Draw(ScreenManager);
                return;
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, p.Name, NamePos, tColor);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[p.Owner.data.Traits.FlagIndex];
            spriteBatch.Draw(item.Value, flagRect, p.Owner.EmpireColor);
            Vector2 TextCursor3 = new Vector2(sel.Rect.X + sel.Rect.Width - 65, NamePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);
            float population1 = p.Population / 1000f;
            string popString3 = population1.ToString(fmt);
            float single1 = p.MaxPopulation / 1000f + p.MaxPopBonus / 1000f;
            popString3 = string.Concat(popString3, " / ", single1.ToString(fmt));
            TextCursor3.X = TextCursor3.X - (Fonts.Arial12Bold.MeasureString(popString3).X + 5f);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, popString3, TextCursor3, tColor);

            popRect = new Rectangle((int)TextCursor3.X - 23, (int)TextCursor3.Y - 3, 22, 22);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

            moneyRect = new Rectangle(popRect.X - 70, popRect.Y, 22, 22);
            Vector2 TextCursorMoney = new Vector2((float)moneyRect.X + 24, TextCursor3.Y);

            float taxRate = p.Owner.data.TaxRate;
            float grossIncome = p.GrossIncome;
            float grossUpkeepPI = (float)(p.TotalMaintenanceCostsPerTurn + p.TotalMaintenanceCostsPerTurn * (double)p.Owner.data.Traits.MaintMod);
            float netIncomePI = grossIncome - grossUpkeepPI;

            if (p.Owner == EmpireManager.Player)
            {
                string sNetIncome = netIncomePI.ToString("F2");
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sNetIncome, TextCursorMoney, netIncomePI > 0.0 ? Color.LightGreen : Color.Salmon);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_money_22"), moneyRect, Color.White);
            }

            PlanetTypeRichness = string.Concat(p.GetTypeTranslation(), " ", p.GetRichness());
            PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f, PlanetIconRect.Y + PlanetIconRect.Height + 5);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Planets/", p.PlanetType)), PlanetIconRect, Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            p.UpdateIncomes(false);
            ColonySliderFood.Value = p.FarmerPercentage;
            ColonySliderFood.cursor = new Rectangle(ColonySliderFood.sRect.X + (int)(ColonySliderFood.sRect.Width * ColonySliderFood.Value) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
            ColonySliderProd.Value = p.WorkerPercentage;
            ColonySliderProd.cursor = new Rectangle(ColonySliderProd.sRect.X + (int)(ColonySliderProd.sRect.Width * ColonySliderProd.Value) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
            ColonySliderRes.Value = p.ResearcherPercentage;
            ColonySliderRes.cursor = new Rectangle(ColonySliderRes.sRect.X + (int)(ColonySliderRes.sRect.Width * ColonySliderRes.Value) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
            
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                new Rectangle(ColonySliderFood.sRect.X, ColonySliderFood.sRect.Y, (int)(ColonySliderFood.Value * ColonySliderFood.sRect.Width), 6), 
                (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            ScreenManager.SpriteBatch.DrawRectangle(ColonySliderFood.sRect, ColonySliderFood.Color);
            Rectangle fIcon2 = new Rectangle(ColonySliderFood.sRect.X - 35, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon2, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : new Color(110, 110, 110, 255)));
            TippedItem ti1 = new TippedItem
            {
                r = fIcon2
            };
            if (p.Owner.data.Traits.Cybernetic != 0)
            {
                ti1.TIP_ID = 77;
            }
            else
            {
                ti1.TIP_ID = 70;
            }
            ToolTipItems.Add(ti1);
            if (ColonySliderFood.cState != "normal")
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderFood.cursor, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), ColonySliderFood.cursor, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
            }
            Vector2 tickCursor = new Vector2();
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width / 10 * i, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height + 2);
                if (ColonySliderFood.state != "normal")
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
                }
            }
            Vector2 textPos = new Vector2(ColonySliderFood.sRect.X + 180, ColonySliderFood.sRect.Y - 2);
            if (p.Owner.data.Traits.Cybernetic == 0)
            {
                float netFoodPerTurn = p.NetFoodPerTurn - p.Consumption;
                str = netFoodPerTurn.ToString(fmt);
            }
            else
            {
                str = "0";
            }
            string food = str;
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(food).X;
            if (p.NetFoodPerTurn - p.Consumption >= 0f || p.Owner.data.Traits.Cybernetic == 1)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, new Color(255, 239, 208));
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, Color.LightPink);
            }
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_brown"), 
                new Rectangle(ColonySliderProd.sRect.X, ColonySliderProd.sRect.Y, (int)(ColonySliderProd.Value * ColonySliderProd.sRect.Width), 6), Color.White);
            ScreenManager.SpriteBatch.DrawRectangle(ColonySliderProd.sRect, ColonySliderProd.Color);
            Rectangle pIcon1 = new Rectangle(ColonySliderProd.sRect.X - 35, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon1, Color.White);
            ti1 = new TippedItem
            {
                r = pIcon1,
                TIP_ID = 71
            };
            ToolTipItems.Add(ti1);
            if (ColonySliderProd.cState != "normal")
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderProd.cursor, Color.White);
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), ColonySliderProd.cursor, Color.White);
            }
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2(ColonySliderFood.sRect.X + ColonySliderProd.sRect.Width / 10 * i, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height + 2);
                if (ColonySliderProd.state != "normal")
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
                }
            }
            textPos = new Vector2(ColonySliderProd.sRect.X + 180, ColonySliderProd.sRect.Y - 2);
            if (p.Owner.data.Traits.Cybernetic != 0)
            {
                float netProductionPerTurn = p.NetProductionPerTurn - p.Consumption;
                str1 = netProductionPerTurn.ToString(fmt);
            }
            else
            {
                str1 = p.NetProductionPerTurn.ToString(fmt);
            }
            string prod = str1;
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(prod).X;
            if (p.Owner.data.Traits.Cybernetic == 0 || p.NetProductionPerTurn - p.Consumption >= 0f)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, new Color(255, 239, 208));
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, Color.LightPink);
            }
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_blue"), 
                new Rectangle(ColonySliderRes.sRect.X, ColonySliderRes.sRect.Y, (int)(ColonySliderRes.Value * ColonySliderRes.sRect.Width), 6), Color.White);
            ScreenManager.SpriteBatch.DrawRectangle(ColonySliderRes.sRect, ColonySliderRes.Color);
            Rectangle rIcon = new Rectangle(ColonySliderRes.sRect.X - 35, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_science").Height / 2, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIcon, Color.White);
            ti1 = new TippedItem
            {
                r = rIcon,
                TIP_ID = 72
            };
            ToolTipItems.Add(ti1);
            if (ColonySliderRes.cState != "normal")
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderRes.cursor, Color.White);
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), ColonySliderRes.cursor, Color.White);
            }
            for (int i = 0; i < 11; i++)
            {
                tickCursor = new Vector2(ColonySliderFood.sRect.X + ColonySliderRes.sRect.Width / 10 * i, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height + 2);
                if (ColonySliderRes.state != "normal")
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
                }
            }
            textPos = new Vector2((ColonySliderRes.sRect.X + 180), (ColonySliderRes.sRect.Y - 2));
            string res = p.NetResearchPerTurn.ToString(fmt);
            textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(res).X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, res, textPos, new Color(255, 239, 208));

            DrawPlanetStats(DefenseRect, ((float)p.TotalDefensiveStrength).String(1), "UI/icon_shield", Color.White, Color.White);

            // Added by Fat Bastard - display total injury level inflicted automatically to invading troops
            if (p.TotalInvadeInjure > 0)
                DrawPlanetStats(InjuryRect, ((float)p.TotalInvadeInjure).String(1), "UI/icon_injury", Color.White, Color.White);

            // Added by Fat Bastard - display total space offense of the planet
            if (p.TotalSpaceOffense > 0)
            {
                string offenseNumberString = ((float) Math.Round(p.TotalSpaceOffense,0)).GetNumberString();
                DrawPlanetStats(OffenseRect, offenseNumberString, "UI/icon_offense", Color.White, Color.White);
            }

            if (p.ShieldStrengthMax > 0f)
                DrawPlanetStats(ShieldRect, p.ShieldStrengthCurrent.String(1), "NewUI/icon_planetshield", Color.White, Color.Green);

            Inspect.Draw(ScreenManager);
            Invade.Draw(ScreenManager);
        }

        private void DrawPlanetStats(Rectangle rect, string data, string texturePath, Color color, Color texcolor)
        {
            SpriteFont font = Fonts.Arial12Bold;
            Vector2 pos     = new Vector2((rect.X + rect.Width + 2), (rect.Y + 11 - font.LineSpacing / 2));
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(texturePath), rect, texcolor);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data, pos, color);
        }

        public override bool HandleInput(InputState input)
        {
            if (p == null)
            {
                return false;
            }
            if (ShieldRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2240));
            }
            foreach (TippedItem ti in ToolTipItems)
            {
                if (!ti.r.HitTest(input.CursorPosition))
                {
                    continue;
                }
                ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (Mark.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                bool marked = false;
                Goal markedGoal = null;
                foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
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
                    EmpireManager.Player.GetEmpireAI().Goals.QueuePendingRemoval(markedGoal);
                    EmpireManager.Player.GetEmpireAI().Goals.ApplyPendingRemovals();
                }
                else
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    EmpireManager.Player.GetEmpireAI().Goals.Add(new MarkForColonization(p, EmpireManager.Player));
                }
            }
            if (SendTroops.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                Array<Ship> troopShips = new Array<Ship>(screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0
                         && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                         && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                Array<Planet> planetTroops = new Array<Planet>(screen.player.GetPlanets().Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                if (troopShips.Count > 0)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    troopShips.First().AI.OrderAssaultPlanet(p);

                }
                else
                    if (planetTroops.Count > 0)
                    {
                        {
                            Ship troop = planetTroops.First().TroopsHere.First().Launch();
                            if (troop != null)
                            {
                                GameAudio.PlaySfxAsync("echo_affirm");                              
                                troop.AI.OrderAssaultPlanet(p);
                            }
                        }
                    }
                    else
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                    }
                

            }

            if (Inspect.Hover)
            {
                if (p.Owner == null || p.Owner != EmpireManager.Player)
                {
                    ToolTip.CreateTooltip(61);
                }
                else
                {
                    ToolTip.CreateTooltip(76);
                }
            }
            if (Invade.Hover)
            {
                ToolTip.CreateTooltip(62);
            }
            if (p.Habitable)
            {
                if (Inspect.HandleInput(input))
                {
                    screen.ViewPlanet(null);
                }
                if (Invade.HandleInput(input))
                {
                    screen.OpenCombatMenu(null);
                }
            }
            if (!ElementRect.HitTest(input.CursorPosition))
            {
                return false;
            }
            if (p.Owner != null && p.Owner == EmpireManager.Player)
            {
                HandleSlider(input);
            }
            return true;
        }

        private void HandleSlider(InputState input)
        {
            if (p == null)
            {
                return;
            }
            p.UpdateIncomes(false);
            Vector2 mousePos = input.CursorPosition;
            if (p.Owner.data.Traits.Cybernetic == 0)
            {
                if (ColonySliderFood.sRect.HitTest(mousePos) || draggingSlider1)
                {
                    ColonySliderFood.state = "hover";
                    ColonySliderFood.Color = new Color(164, 154, 133);
                }
                else
                {
                    ColonySliderFood.state = "normal";
                    ColonySliderFood.Color = new Color(72, 61, 38);
                }
                if (ColonySliderFood.cursor.HitTest(mousePos) || draggingSlider1)
                {
                    ColonySliderFood.cState = "hover";
                }
                else
                {
                    ColonySliderFood.cState = "normal";
                }
            }
            if (ColonySliderProd.sRect.HitTest(mousePos) || draggingSlider2)
            {
                ColonySliderProd.state = "hover";
                ColonySliderProd.Color = new Color(164, 154, 133);
            }
            else
            {
                ColonySliderProd.state = "normal";
                ColonySliderProd.Color = new Color(72, 61, 38);
            }
            if (ColonySliderProd.cursor.HitTest(mousePos) || draggingSlider2)
            {
                ColonySliderProd.cState = "hover";
            }
            else
            {
                ColonySliderProd.cState = "normal";
            }
            if (ColonySliderRes.sRect.HitTest(mousePos) || draggingSlider3)
            {
                ColonySliderRes.state = "hover";
                ColonySliderRes.Color = new Color(164, 154, 133);
            }
            else
            {
                ColonySliderRes.state = "normal";
                ColonySliderRes.Color = new Color(72, 61, 38);
            }
            if (ColonySliderRes.cursor.HitTest(mousePos) || draggingSlider3)
            {
                ColonySliderRes.cState = "hover";
            }
            else
            {
                ColonySliderRes.cState = "normal";
            }
            if (p.Owner.data.Traits.Cybernetic == 0 && ColonySliderFood.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                draggingSlider1 = true;
            }
            if (ColonySliderProd.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                draggingSlider2 = true;
            }
            if (ColonySliderRes.cursor.HitTest(mousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                draggingSlider3 = true;
            }
            if (draggingSlider1 && !FoodLock.Locked && (!ProdLock.Locked || !ResLock.Locked) && p.Owner.data.Traits.Cybernetic == 0)
            {
                ColonySliderFood.cursor.X = input.MouseCurr.X;
                if (ColonySliderFood.cursor.X > ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width)
                {
                    ColonySliderFood.cursor.X = ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width;
                }
                else if (ColonySliderFood.cursor.X < ColonySliderFood.sRect.X)
                {
                    ColonySliderFood.cursor.X = ColonySliderFood.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider1 = false;
                }
                fPercentLast = p.FarmerPercentage;
                p.FarmerPercentage = (ColonySliderFood.cursor.X - (float)ColonySliderFood.sRect.X) / ColonySliderFood.sRect.Width;
                float difference = fPercentLast - p.FarmerPercentage;
                if (!ProdLock.Locked && !ResLock.Locked)
                {
                    Planet workerPercentage = p;
                    workerPercentage.WorkerPercentage = workerPercentage.WorkerPercentage + difference / 2f;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet farmerPercentage = p;
                        farmerPercentage.FarmerPercentage = farmerPercentage.FarmerPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                    Planet researcherPercentage = p;
                    researcherPercentage.ResearcherPercentage = researcherPercentage.ResearcherPercentage + difference / 2f;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet planet = p;
                        planet.FarmerPercentage = planet.FarmerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (ProdLock.Locked && !ResLock.Locked)
                {
                    Planet researcherPercentage1 = p;
                    researcherPercentage1.ResearcherPercentage = researcherPercentage1.ResearcherPercentage + difference;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet farmerPercentage1 = p;
                        farmerPercentage1.FarmerPercentage = farmerPercentage1.FarmerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (!ProdLock.Locked && ResLock.Locked)
                {
                    Planet workerPercentage1 = p;
                    workerPercentage1.WorkerPercentage = workerPercentage1.WorkerPercentage + difference;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet planet1 = p;
                        planet1.FarmerPercentage = planet1.FarmerPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                }
            }
            if (draggingSlider2 && !ProdLock.Locked && (!FoodLock.Locked || !ResLock.Locked))
            {
                ColonySliderProd.cursor.X = input.MouseCurr.X;
                if (ColonySliderProd.cursor.X > ColonySliderProd.sRect.X + ColonySliderProd.sRect.Width)
                {
                    ColonySliderProd.cursor.X = ColonySliderProd.sRect.X + ColonySliderProd.sRect.Width;
                }
                else if (ColonySliderProd.cursor.X < ColonySliderProd.sRect.X)
                {
                    ColonySliderProd.cursor.X = ColonySliderProd.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider2 = false;
                }
                pPercentLast = p.WorkerPercentage;
                p.WorkerPercentage = (ColonySliderProd.cursor.X - (float)ColonySliderProd.sRect.X) / ColonySliderProd.sRect.Width;
                float difference = pPercentLast - p.WorkerPercentage;
                if (!FoodLock.Locked && !ResLock.Locked)
                {
                    Planet farmerPercentage2 = p;
                    farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + difference / 2f;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet workerPercentage2 = p;
                        workerPercentage2.WorkerPercentage = workerPercentage2.WorkerPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                    Planet researcherPercentage2 = p;
                    researcherPercentage2.ResearcherPercentage = researcherPercentage2.ResearcherPercentage + difference / 2f;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet planet2 = p;
                        planet2.WorkerPercentage = planet2.WorkerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (FoodLock.Locked && !ResLock.Locked)
                {
                    Planet researcherPercentage3 = p;
                    researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet workerPercentage3 = p;
                        workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (!FoodLock.Locked && ResLock.Locked)
                {
                    Planet farmerPercentage3 = p;
                    farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + difference;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet planet3 = p;
                        planet3.WorkerPercentage = planet3.WorkerPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
            }
            if (draggingSlider3 && !ResLock.Locked && (!FoodLock.Locked || !ProdLock.Locked))
            {
                ColonySliderRes.cursor.X = input.MouseCurr.X;
                if (ColonySliderRes.cursor.X > ColonySliderRes.sRect.X + ColonySliderRes.sRect.Width)
                {
                    ColonySliderRes.cursor.X = ColonySliderRes.sRect.X + ColonySliderRes.sRect.Width;
                }
                else if (ColonySliderRes.cursor.X < ColonySliderRes.sRect.X)
                {
                    ColonySliderRes.cursor.X = ColonySliderRes.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider3 = false;
                }
                rPercentLast = p.ResearcherPercentage;
                p.ResearcherPercentage = (ColonySliderRes.cursor.X - (float)ColonySliderRes.sRect.X) / ColonySliderRes.sRect.Width;
                float difference = rPercentLast - p.ResearcherPercentage;
                if (!ProdLock.Locked && !FoodLock.Locked)
                {
                    Planet workerPercentage4 = p;
                    workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + difference / 2f;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet researcherPercentage4 = p;
                        researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                    Planet farmerPercentage4 = p;
                    farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference / 2f;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet planet4 = p;
                        planet4.ResearcherPercentage = planet4.ResearcherPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
                else if (ProdLock.Locked && !FoodLock.Locked)
                {
                    Planet farmerPercentage5 = p;
                    farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet researcherPercentage5 = p;
                        researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
                else if (!ProdLock.Locked && FoodLock.Locked)
                {
                    Planet workerPercentage5 = p;
                    workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet planet5 = p;
                        planet5.ResearcherPercentage = planet5.ResearcherPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                }
            }
            MathHelper.Clamp(p.FarmerPercentage, 0f, 1f);
            MathHelper.Clamp(p.WorkerPercentage, 0f, 1f);
            MathHelper.Clamp(p.ResearcherPercentage, 0f, 1f);
            slider1Last = ColonySliderFood.cursor.X;
            slider2Last = ColonySliderProd.cursor.X;
            slider3Last = ColonySliderRes.cursor.X;
        }

        public void SetPlanet(Planet p)
        {
            if (p.Owner != null && p.Owner.data.Traits.Cybernetic != 0)
            {
                p.FoodLocked = true;
            }
            this.p = p;
            FoodLock.Locked = p.FoodLocked;
            ResLock.Locked = p.ResLocked;
            ProdLock.Locked = p.ProdLocked;
        }

        private struct TippedItem
        {
            public Rectangle r;

            public int TIP_ID;
        }
    }
}
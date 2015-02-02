using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;

namespace Ship_Game
{
	public class PlanetInfoUIElement : UIElement
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

		private ColonyScreen.Slider SliderFood;

		private ColonyScreen.Slider SliderProd;

		private ColonyScreen.Slider SliderRes;

		private ColonyScreen.Lock FoodLock;

		private ColonyScreen.Lock ProdLock;

		private ColonyScreen.Lock ResLock;

		private Rectangle Housing;

		private Rectangle DefenseRect;

		private Rectangle ShieldRect;

		private bool draggingSlider1;

		private bool draggingSlider2;

		private bool draggingSlider3;

		private List<PlanetInfoUIElement.TippedItem> ToolTipItems = new List<PlanetInfoUIElement.TippedItem>();

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
			this.sel = new Selector(this.ScreenManager, r, Color.Black);
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
			this.SliderFood = new ColonyScreen.Slider()
			{
				sRect = new Rectangle(this.RightRect.X, this.Housing.Y + 120, 145, 6)
			};
			this.SliderProd = new ColonyScreen.Slider()
			{
				sRect = new Rectangle(this.RightRect.X, this.Housing.Y + 160, 145, 6)
			};
			this.SliderRes = new ColonyScreen.Slider()
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
			if (this.p.Owner == null || !this.p.ExploredDict[EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)])
			{
				if (!this.p.ExploredDict[EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty)])
				{
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(Localizer.Token(1429), this.p.GetTypeTranslation()), NamePos, this.tColor);
					Vector2 TextCursor2 = new Vector2((float)(this.sel.Menu.X + this.sel.Menu.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
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
				if (!this.p.habitable)
				{
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
					string text = Localizer.Token(1427);
					Vector2 Cursor = new Vector2((float)(this.Housing.X + 20), (float)(this.Housing.Y + 115));
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, this.tColor);
					return;
				}
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
				Vector2 TextCursor = new Vector2((float)(this.sel.Menu.X + this.sel.Menu.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
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
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.planetType)], this.PlanetIconRect, Color.White);
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
				if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
				{
					Text.X = Text.X - 9f;
				}
				bool marked = false;
				foreach (Goal g in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals)
				{
					if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != this.p)
					{
						continue;
					}
					marked = true;
				}
				if (marked)
				{
					if (!HelperFunctions.CheckIntersection(this.Mark, MousePos))
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
					if (!HelperFunctions.CheckIntersection(this.Mark, MousePos))
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
                ti = new PlanetInfoUIElement.TippedItem()
                {
                    r = pIcon,
                    TIP_ID = 21
                };
                int troops = 0;
                this.ToolTipItems.Add(ti);

                this.SendTroops = new Rectangle(this.Mark.X, this.Mark.Y - this.Mark.Height -5, 182, 25);
                Text = new Vector2((float)(SendTroops.X + 25), (float)(SendTroops.Y));
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button_blue"], SendTroops, Color.White);
                 //Ship troopShip; 

                
                troops= this.screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0 )
                     .Where(troopAI => troopAI.GetAI().OrderQueue
                         .Where(goal => goal.TargetPlanet != null && goal.TargetPlanet == p).Count() >0).Count();
                    
                     
          


                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,String.Concat("Invading : ",troops) , Text, new Color(88, 108, 146)); // Localizer.Token(1425)

				this.Inspect.Draw(this.ScreenManager);
				this.Invade.Draw(this.ScreenManager);
				return;
			}
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, NamePos, this.tColor);
			SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
			KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[this.p.Owner.data.Traits.FlagIndex];
			spriteBatch.Draw(item.Value, this.flagRect, this.p.Owner.EmpireColor);
			Vector2 TextCursor3 = new Vector2((float)(this.sel.Menu.X + this.sel.Menu.Width - 65), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - (float)(Fonts.Arial12Bold.LineSpacing / 2) + 2f);
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
            float grossIncome = (this.p.GrossMoneyPT + this.p.GrossMoneyPT * this.p.Owner.data.Traits.TaxMod) * this.p.Owner.data.TaxRate + this.p.PlusFlatMoneyPerTurn + (this.p.Population / 1000f * this.p.PlusCreditsPerColonist);
            float grossIncomePI = (float)((double)this.p.GrossMoneyPT + (double)this.p.Owner.data.Traits.TaxMod * (double)this.p.GrossMoneyPT);
            float grossUpkeepPI = (float)((double)this.p.TotalMaintenanceCostsPerTurn + (double)this.p.TotalMaintenanceCostsPerTurn * (double)this.p.Owner.data.Traits.MaintMod);
            float netIncomePI = (float)(grossIncome - grossUpkeepPI);

            if (p.Owner == EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
            {
                string sNetIncome = netIncomePI.ToString("F2");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sNetIncome, TextCursorMoney, netIncomePI > 0.0 ? Color.LightGreen : Color.Salmon);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_money_22"], moneyRect, Color.White);
            }

			this.PlanetTypeRichness = string.Concat(this.p.GetTypeTranslation(), " ", this.p.GetRichness());
			this.PlanetTypeCursor = new Vector2((float)(this.PlanetIconRect.X + this.PlanetIconRect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.PlanetTypeRichness).X / 2f, (float)(this.PlanetIconRect.Y + this.PlanetIconRect.Height + 5));
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.planetType)], this.PlanetIconRect, Color.White);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.PlanetTypeRichness, this.PlanetTypeCursor, this.tColor);
			this.p.UpdateIncomes();
			this.SliderFood.amount = this.p.FarmerPercentage;
			this.SliderFood.cursor = new Rectangle(this.SliderFood.sRect.X + (int)((float)this.SliderFood.sRect.Width * this.SliderFood.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderFood.sRect.Y + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.SliderProd.amount = this.p.WorkerPercentage;
			this.SliderProd.cursor = new Rectangle(this.SliderProd.sRect.X + (int)((float)this.SliderProd.sRect.Width * this.SliderProd.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderProd.sRect.Y + this.SliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.SliderRes.amount = this.p.ResearcherPercentage;
			this.SliderRes.cursor = new Rectangle(this.SliderRes.sRect.X + (int)((float)this.SliderRes.sRect.Width * this.SliderRes.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderRes.sRect.Y + this.SliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.SliderFood.sRect.X, this.SliderFood.sRect.Y, (int)(this.SliderFood.amount * (float)this.SliderFood.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderFood.sRect.X, this.SliderFood.sRect.Y, (int)(this.SliderFood.amount * (float)this.SliderFood.sRect.Width), 6)), (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, this.SliderFood.sRect, this.SliderFood.Color);
			Rectangle fIcon2 = new Rectangle(this.SliderFood.sRect.X - 35, this.SliderFood.sRect.Y + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
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
			if (this.SliderFood.cState != "normal")
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			}
			else
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			}
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderFood.sRect.Width / 10 * i), (float)(this.SliderFood.sRect.Y + this.SliderFood.sRect.Height + 2));
				if (this.SliderFood.state != "normal")
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
				}
			}
			Vector2 textPos = new Vector2((float)(this.SliderFood.sRect.X + 180), (float)(this.SliderFood.sRect.Y - 2));
			if (this.p.Owner.data.Traits.Cybernetic == 0)
			{
				float netFoodPerTurn = this.p.NetFoodPerTurn - this.p.consumption;
				str = netFoodPerTurn.ToString(this.fmt);
			}
			else
			{
				str = "0";
			}
			string food = str;
			textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(food).X;
			if (this.p.NetFoodPerTurn - this.p.consumption >= 0f || this.p.Owner.data.Traits.Cybernetic == 1)
			{
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, new Color(255, 239, 208));
			}
			else
			{
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, Color.LightPink);
			}
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_brown"], new Rectangle(this.SliderProd.sRect.X, this.SliderProd.sRect.Y, (int)(this.SliderProd.amount * (float)this.SliderProd.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderProd.sRect.X, this.SliderProd.sRect.Y, (int)(this.SliderProd.amount * (float)this.SliderProd.sRect.Width), 6)), Color.White);
			Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, this.SliderProd.sRect, this.SliderProd.Color);
			Rectangle pIcon1 = new Rectangle(this.SliderProd.sRect.X - 35, this.SliderProd.sRect.Y + this.SliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], pIcon1, Color.White);
			ti1 = new PlanetInfoUIElement.TippedItem()
			{
				r = pIcon1,
				TIP_ID = 71
			};
			this.ToolTipItems.Add(ti1);
			if (this.SliderProd.cState != "normal")
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderProd.cursor, Color.White);
			}
			else
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderProd.cursor, Color.White);
			}
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderProd.sRect.Width / 10 * i), (float)(this.SliderProd.sRect.Y + this.SliderProd.sRect.Height + 2));
				if (this.SliderProd.state != "normal")
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
				}
			}
			textPos = new Vector2((float)(this.SliderProd.sRect.X + 180), (float)(this.SliderProd.sRect.Y - 2));
			if (this.p.Owner.data.Traits.Cybernetic != 0)
			{
				float netProductionPerTurn = this.p.NetProductionPerTurn - this.p.consumption;
				str1 = netProductionPerTurn.ToString(this.fmt);
			}
			else
			{
				str1 = this.p.NetProductionPerTurn.ToString(this.fmt);
			}
			string prod = str1;
			textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(prod).X;
			if (this.p.Owner.data.Traits.Cybernetic == 0 || this.p.NetProductionPerTurn - this.p.consumption >= 0f)
			{
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, new Color(255, 239, 208));
			}
			else
			{
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, Color.LightPink);
			}
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_blue"], new Rectangle(this.SliderRes.sRect.X, this.SliderRes.sRect.Y, (int)(this.SliderRes.amount * (float)this.SliderRes.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderRes.sRect.X, this.SliderRes.sRect.Y, (int)(this.SliderRes.amount * (float)this.SliderRes.sRect.Width), 6)), Color.White);
			Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, this.SliderRes.sRect, this.SliderRes.Color);
			Rectangle rIcon = new Rectangle(this.SliderRes.sRect.X - 35, this.SliderRes.sRect.Y + this.SliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_science"].Height / 2, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rIcon, Color.White);
			ti1 = new PlanetInfoUIElement.TippedItem()
			{
				r = rIcon,
				TIP_ID = 72
			};
			this.ToolTipItems.Add(ti1);
			if (this.SliderRes.cState != "normal")
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderRes.cursor, Color.White);
			}
			else
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderRes.cursor, Color.White);
			}
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderRes.sRect.Width / 10 * i), (float)(this.SliderRes.sRect.Y + this.SliderRes.sRect.Height + 2));
				if (this.SliderRes.state != "normal")
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
				}
				else
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
				}
			}
			textPos = new Vector2((float)(this.SliderRes.sRect.X + 180), (float)(this.SliderRes.sRect.Y - 2));
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
			if (HelperFunctions.CheckIntersection(this.ShieldRect, input.CursorPosition))
			{
				ToolTip.CreateTooltip(Localizer.Token(2240), this.ScreenManager);
			}
			foreach (PlanetInfoUIElement.TippedItem ti in this.ToolTipItems)
			{
				if (!HelperFunctions.CheckIntersection(ti.r, input.CursorPosition))
				{
					continue;
				}
				ToolTip.CreateTooltip(ti.TIP_ID, this.ScreenManager);
			}
			if (HelperFunctions.CheckIntersection(this.Mark, input.CursorPosition) && input.InGameSelect)
			{
				bool marked = false;
				Goal markedGoal = new Goal();
				foreach (Goal g in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals)
				{
					if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != this.p)
					{
						continue;
					}
					marked = true;
					markedGoal = g;
				}
				if (marked)
				{
					AudioManager.PlayCue("echo_affirm");
					if (markedGoal.GetColonyShip() != null)
					{
						lock (markedGoal.GetColonyShip())
						{
							markedGoal.GetColonyShip().GetAI().OrderQueue.Clear();
							markedGoal.GetColonyShip().GetAI().State = AIState.AwaitingOrders;
						}
					}
					EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals.QueuePendingRemoval(markedGoal);
					EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals.ApplyPendingRemovals();
				}
				else
				{
					AudioManager.PlayCue("echo_affirm");
					Goal g = new Goal(this.p, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty));
					EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetGSAI().Goals.Add(g);
				}
			}
            if (HelperFunctions.CheckIntersection(this.SendTroops, input.CursorPosition) && input.InGameSelect)
            {
                List<Ship> troopShips = new List<Ship>(this.screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0
                         && troop.GetAI().State == AIState.AwaitingOrders
                         && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, p.Position)));
                List<Planet> planetTroops = new List<Planet>(this.screen.player.GetPlanets().Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Position, p.Position)));
                if (troopShips.Count > 0)
                {
                    AudioManager.PlayCue("echo_affirm");
                    troopShips.First().GetAI().OrderAssaultPlanet(this.p);

                }
                else
                    if (planetTroops.Count > 0)
                    {


                        {
                            Ship troop = planetTroops.First().TroopsHere.First().Launch();
                            if (troop != null)
                            {


                                AudioManager.PlayCue("echo_affirm");
                                
                                troop.GetAI().OrderAssaultPlanet(this.p);
                            }
                        }
                    }
                    else
                    {
                        AudioManager.PlayCue("blip_click");
                    }
                

            }

			if (this.Inspect.Hover)
			{
				if (this.p.Owner == null || this.p.Owner != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
				{
					ToolTip.CreateTooltip(61, this.ScreenManager);
				}
				else
				{
					ToolTip.CreateTooltip(76, this.ScreenManager);
				}
			}
			if (this.Invade.Hover)
			{
				ToolTip.CreateTooltip(62, this.ScreenManager);
			}
			if (this.p.habitable)
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
			if (!HelperFunctions.CheckIntersection(this.ElementRect, input.CursorPosition))
			{
				return false;
			}
			if (this.p.Owner != null && this.p.Owner == EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
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
			this.p.UpdateIncomes();
			Vector2 mousePos = input.CursorPosition;
			if (this.p.Owner.data.Traits.Cybernetic == 0)
			{
				if (HelperFunctions.CheckIntersection(this.SliderFood.sRect, mousePos) || this.draggingSlider1)
				{
					this.SliderFood.state = "hover";
					this.SliderFood.Color = new Color(164, 154, 133);
				}
				else
				{
					this.SliderFood.state = "normal";
					this.SliderFood.Color = new Color(72, 61, 38);
				}
				if (HelperFunctions.CheckIntersection(this.SliderFood.cursor, mousePos) || this.draggingSlider1)
				{
					this.SliderFood.cState = "hover";
				}
				else
				{
					this.SliderFood.cState = "normal";
				}
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.sRect, mousePos) || this.draggingSlider2)
			{
				this.SliderProd.state = "hover";
				this.SliderProd.Color = new Color(164, 154, 133);
			}
			else
			{
				this.SliderProd.state = "normal";
				this.SliderProd.Color = new Color(72, 61, 38);
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.cursor, mousePos) || this.draggingSlider2)
			{
				this.SliderProd.cState = "hover";
			}
			else
			{
				this.SliderProd.cState = "normal";
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.sRect, mousePos) || this.draggingSlider3)
			{
				this.SliderRes.state = "hover";
				this.SliderRes.Color = new Color(164, 154, 133);
			}
			else
			{
				this.SliderRes.state = "normal";
				this.SliderRes.Color = new Color(72, 61, 38);
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.cursor, mousePos) || this.draggingSlider3)
			{
				this.SliderRes.cState = "hover";
			}
			else
			{
				this.SliderRes.cState = "normal";
			}
			if (this.p.Owner.data.Traits.Cybernetic == 0 && HelperFunctions.CheckIntersection(this.SliderFood.cursor, mousePos) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider1 = true;
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.cursor, mousePos) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider2 = true;
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.cursor, mousePos) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider3 = true;
			}
			if (this.draggingSlider1 && !this.FoodLock.Locked && (!this.ProdLock.Locked || !this.ResLock.Locked) && this.p.Owner.data.Traits.Cybernetic == 0)
			{
				this.SliderFood.cursor.X = input.CurrentMouseState.X;
				if (this.SliderFood.cursor.X > this.SliderFood.sRect.X + this.SliderFood.sRect.Width)
				{
					this.SliderFood.cursor.X = this.SliderFood.sRect.X + this.SliderFood.sRect.Width;
				}
				else if (this.SliderFood.cursor.X < this.SliderFood.sRect.X)
				{
					this.SliderFood.cursor.X = this.SliderFood.sRect.X;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Released)
				{
					this.draggingSlider1 = false;
				}
				this.fPercentLast = this.p.FarmerPercentage;
				this.p.FarmerPercentage = ((float)this.SliderFood.cursor.X - (float)this.SliderFood.sRect.X) / (float)this.SliderFood.sRect.Width;
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
				this.SliderProd.cursor.X = input.CurrentMouseState.X;
				if (this.SliderProd.cursor.X > this.SliderProd.sRect.X + this.SliderProd.sRect.Width)
				{
					this.SliderProd.cursor.X = this.SliderProd.sRect.X + this.SliderProd.sRect.Width;
				}
				else if (this.SliderProd.cursor.X < this.SliderProd.sRect.X)
				{
					this.SliderProd.cursor.X = this.SliderProd.sRect.X;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Released)
				{
					this.draggingSlider2 = false;
				}
				this.pPercentLast = this.p.WorkerPercentage;
				this.p.WorkerPercentage = ((float)this.SliderProd.cursor.X - (float)this.SliderProd.sRect.X) / (float)this.SliderProd.sRect.Width;
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
				this.SliderRes.cursor.X = input.CurrentMouseState.X;
				if (this.SliderRes.cursor.X > this.SliderRes.sRect.X + this.SliderRes.sRect.Width)
				{
					this.SliderRes.cursor.X = this.SliderRes.sRect.X + this.SliderRes.sRect.Width;
				}
				else if (this.SliderRes.cursor.X < this.SliderRes.sRect.X)
				{
					this.SliderRes.cursor.X = this.SliderRes.sRect.X;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Released)
				{
					this.draggingSlider3 = false;
				}
				this.rPercentLast = this.p.ResearcherPercentage;
				this.p.ResearcherPercentage = ((float)this.SliderRes.cursor.X - (float)this.SliderRes.sRect.X) / (float)this.SliderRes.sRect.Width;
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
			this.slider1Last = (float)this.SliderFood.cursor.X;
			this.slider2Last = (float)this.SliderProd.cursor.X;
			this.slider3Last = (float)this.SliderRes.cursor.X;
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
			Empire owner = p.Owner;
			EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty);
		}

		private struct TippedItem
		{
			public Rectangle r;

			public int TIP_ID;
		}
	}
}
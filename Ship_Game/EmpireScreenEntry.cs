using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EmpireScreenEntry
	{
		public Planet p;

		public Rectangle TotalEntrySize;

		public Rectangle SysNameRect;

		public Rectangle PlanetNameRect;

		public Rectangle SliderRect;

		public Rectangle StorageRect;

		public Rectangle QueueRect;

		public Rectangle PopRect;

		public Rectangle FoodRect;

		public Rectangle ProdRect;

		public Rectangle ResRect;

		public Rectangle MoneyRect;

		private ColonyScreen.Slider SliderFood;

		private ColonyScreen.Slider SliderProd;

		private ColonyScreen.Slider SliderRes;

		private ColonyScreen.Lock FoodLock;

		private ColonyScreen.Lock ProdLock;

		private ColonyScreen.Lock ResLock;

		private ProgressBar FoodStorage;

		private ProgressBar ProdStorage;

		private Rectangle ApplyProductionRect;

		private DropDownMenu foodDropDown;

		private DropDownMenu prodDropDown;

		private Rectangle foodStorageIcon;

		private Rectangle prodStorageIcon;

		private EmpireScreen eScreen;

		private bool LowRes;

		private bool ApplyHover;

		private float fPercentLast;

		private float pPercentLast;

		private float rPercentLast;

		private bool draggingSlider1;

		private bool draggingSlider2;

		private bool draggingSlider3;

		private MouseState currentMouse;

		private MouseState previousMouse;

		private float slider1Last;

		private float slider2Last;

		private float slider3Last;

		private string fmt = "0.#";

		public EmpireScreenEntry(Planet planet, int x, int y, int width1, int height, EmpireScreen eScreen)
		{
			if (Ship.universeScreen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366)
			{
				this.LowRes = true;
			}
			int SliderWidth = 375;
			if (this.LowRes)
			{
				SliderWidth = 250;
			}
			this.eScreen = eScreen;
			this.p = planet;
			this.TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f) - 30, height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f), height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 30, height);
			this.FoodRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 30, y, 30, height);
			this.ProdRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 60, y, 30, height);
			this.ResRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 90, y, 30, height);
			this.MoneyRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 120, y, 30, height);
			this.SliderRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 150, y, SliderWidth, height);
			this.StorageRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.SliderRect.Width + 150, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 120)) * 0.33f), height);
			this.QueueRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.SliderRect.Width + this.StorageRect.Width + 150, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 150)) * 0.33f), height);
			float width = (float)((int)((float)this.SliderRect.Width * 0.8f));
			if (SliderWidth == 250)
			{
				width = 180f;
			}
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			Rectangle foodRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.25 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderFood = new ColonyScreen.Slider();
			
				this.SliderFood.sRect = foodRect;
                this.SliderFood.amount = this.p.FarmerPercentage;
			
			this.FoodLock = new ColonyScreen.Lock();

            this.FoodLock.LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderFood.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height);
			
			if (this.p.Owner.data.Traits.Cybernetic != 0)
			{
				this.p.FoodLocked = true;
			}
			this.FoodLock.Locked = this.p.FoodLocked;
			Rectangle prodRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.5 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderProd = new ColonyScreen.Slider()
			{
				sRect = prodRect,
				amount = this.p.WorkerPercentage
			};
			this.ProdLock = new ColonyScreen.Lock()
			{
				LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderProd.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height),
				Locked = this.p.ProdLocked
			};
			Rectangle resRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.75 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderRes = new ColonyScreen.Slider()
			{
				sRect = resRect,
				amount = this.p.ResearcherPercentage
			};
			this.ResLock = new ColonyScreen.Lock()
			{
				LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderRes.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height),
				Locked = this.p.ResLocked
			};
			this.FoodStorage = new ProgressBar(new Rectangle(this.StorageRect.X + 50, this.SliderRect.Y + (int)(0.25 * (double)this.SliderRect.Height), (int)(0.4f * (float)this.StorageRect.Width), 18))
			{
				Max = this.p.MAX_STORAGE,
				Progress = this.p.FoodHere,
				color = "green"
			};
			int ddwidth = (int)(0.2f * (float)this.StorageRect.Width);
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				ddwidth = (int)Fonts.Arial12.MeasureString(Localizer.Token(330)).X + 22;
			}
			this.foodDropDown = new DropDownMenu(new Rectangle(this.StorageRect.X + 50 + (int)(0.4f * (float)this.StorageRect.Width) + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			this.foodDropDown.AddOption(Localizer.Token(329));
			this.foodDropDown.AddOption(Localizer.Token(330));
			this.foodDropDown.AddOption(Localizer.Token(331));
			this.foodDropDown.ActiveIndex = (int)this.p.fs;
			this.foodStorageIcon = new Rectangle(this.StorageRect.X + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
			this.ProdStorage = new ProgressBar(new Rectangle(this.StorageRect.X + 50, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height + 10, (int)(0.4f * (float)this.StorageRect.Width), 18))
			{
				Max = this.p.MAX_STORAGE,
				Progress = this.p.ProductionHere
			};
			this.prodStorageIcon = new Rectangle(this.StorageRect.X + 20, this.ProdStorage.pBar.Y + this.ProdStorage.pBar.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
			this.prodDropDown = new DropDownMenu(new Rectangle(this.StorageRect.X + 50 + (int)(0.4f * (float)this.StorageRect.Width) + 20, this.ProdStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			this.prodDropDown.AddOption(Localizer.Token(329));
			this.prodDropDown.AddOption(Localizer.Token(330));
			this.prodDropDown.AddOption(Localizer.Token(331));
			this.prodDropDown.ActiveIndex = (int)this.p.ps;
			this.ApplyProductionRect = new Rectangle(this.QueueRect.X + this.QueueRect.Width - 50, this.QueueRect.Y + this.QueueRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			string str;
			string str1;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			Color TextColor = new Color(255, 239, 208);
			if (Fonts.Pirulen16.MeasureString(this.p.system.Name).X <= (float)this.SysNameRect.Width)
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Pirulen16.MeasureString(this.p.system.Name).X / 2f, (float)(this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, this.p.system.Name, SysNameCursor, TextColor);
			}
			else
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Pirulen12.MeasureString(this.p.system.Name).X / 2f, (float)(this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.p.system.Name, SysNameCursor, TextColor);
			}
			Rectangle planetIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 25, this.PlanetNameRect.Height - 50, this.PlanetNameRect.Height - 50);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.planetType)], planetIconRect, Color.White);
			Vector2 cursor = new Vector2((float)(this.PopRect.X + this.PopRect.Width - 5), (float)(this.PlanetNameRect.Y + this.PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			float population = this.p.Population / 1000f;
			string popstring = population.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(popstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, popstring, cursor, Color.White);
			cursor = new Vector2((float)(this.FoodRect.X + this.FoodRect.Width - 5), (float)(this.PlanetNameRect.Y + this.PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			if (this.p.Owner.data.Traits.Cybernetic == 0)
			{
				float netFoodPerTurn = this.p.NetFoodPerTurn - this.p.consumption;
				str = netFoodPerTurn.ToString("#.0");
			}
			else
			{
				str = this.p.NetFoodPerTurn.ToString("#.0");
			}
			string fstring = str;
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(fstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, fstring, cursor, (this.p.NetFoodPerTurn - this.p.consumption >= 0f ? Color.White : Color.LightPink));
			cursor = new Vector2((float)(this.ProdRect.X + this.FoodRect.Width - 5), (float)(this.PlanetNameRect.Y + this.PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			if (this.p.Owner.data.Traits.Cybernetic != 0)
			{
				float netProductionPerTurn = this.p.NetProductionPerTurn - this.p.consumption;
				str1 = netProductionPerTurn.ToString("#.0");
			}
			else
			{
				str1 = this.p.NetProductionPerTurn.ToString("#.0");
			}
			string pstring = str1;
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(pstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			bool pink = false;
			if (this.p.Owner.data.Traits.Cybernetic != 0 && this.p.NetProductionPerTurn - this.p.consumption < 0f)
			{
				pink = true;
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, pstring, cursor, (pink ? Color.LightPink : Color.White));
			cursor = new Vector2((float)(this.ResRect.X + this.FoodRect.Width - 5), (float)(this.PlanetNameRect.Y + this.PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			string rstring = this.p.NetResearchPerTurn.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(rstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, rstring, cursor, Color.White);
			cursor = new Vector2((float)(this.MoneyRect.X + this.FoodRect.Width - 5), (float)(this.PlanetNameRect.Y + this.PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			float Money = this.p.GrossMoneyPT + this.p.Owner.data.Traits.TaxMod * this.p.GrossMoneyPT - (this.p.TotalMaintenanceCostsPerTurn + this.p.TotalMaintenanceCostsPerTurn * this.p.Owner.data.Traits.MaintMod);
			string mstring = Money.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(mstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, mstring, cursor, (Money >= 0f ? Color.White : Color.LightPink));
			if (Fonts.Pirulen16.MeasureString(this.p.Name).X + (float)planetIconRect.Width + 10f <= (float)this.PlanetNameRect.Width)
			{
				Vector2 PlanetNameCursor = new Vector2((float)(planetIconRect.X + planetIconRect.Width + 10), (float)(this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, this.p.Name, PlanetNameCursor, TextColor);
			}
			else if (Fonts.Pirulen12.MeasureString(this.p.Name).X + (float)planetIconRect.Width + 10f <= (float)this.PlanetNameRect.Width)
			{
				Vector2 PlanetNameCursor = new Vector2((float)(planetIconRect.X + planetIconRect.Width + 10), (float)(this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.p.Name, PlanetNameCursor, TextColor);
			}
			else
			{
				Vector2 PlanetNameCursor = new Vector2((float)(planetIconRect.X + planetIconRect.Width + 10), (float)(this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial8Bold.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, this.p.Name, PlanetNameCursor, TextColor);
			}
			this.DrawSliders(ScreenManager);
			if (this.p.Owner.data.Traits.Cybernetic != 0)
			{
				this.FoodStorage.DrawGrayed(ScreenManager.SpriteBatch);
				this.foodDropDown.DrawGrayed(ScreenManager.SpriteBatch);
			}
			else
			{
				this.FoodStorage.Draw(ScreenManager.SpriteBatch);
				this.foodDropDown.Draw(ScreenManager.SpriteBatch);
			}
			this.ProdStorage.Draw(ScreenManager.SpriteBatch);
			this.prodDropDown.Draw(ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], this.foodStorageIcon, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : new Color(110, 110, 110, 255)));
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], this.prodStorageIcon, Color.White);
			if (HelperFunctions.CheckIntersection(this.foodStorageIcon, MousePos))
			{
				if (this.p.Owner.data.Traits.Cybernetic != 0)
				{
					ToolTip.CreateTooltip(77, ScreenManager);
				}
				else
				{
					ToolTip.CreateTooltip(73, ScreenManager);
				}
			}
			if (HelperFunctions.CheckIntersection(this.prodStorageIcon, MousePos))
			{
				ToolTip.CreateTooltip(74, ScreenManager);
			}
			if (this.p.ConstructionQueue.Count > 0)
			{
				QueueItem qi = this.p.ConstructionQueue[0];
				Vector2 bCursor = new Vector2((float)(this.QueueRect.X + 10), (float)(this.QueueRect.Y + this.QueueRect.Height / 2 - 15));
				if (qi.isBuilding)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", qi.Building.Icon, "_48x48")], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.Building.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(ScreenManager.SpriteBatch);
				}
				if (qi.isShip)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[qi.sData.Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.sData.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(ScreenManager.SpriteBatch);
				}
				if (qi.isTroop)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", qi.troop.TexturePath)], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.troop.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(ScreenManager.SpriteBatch);
				}
				ScreenManager.SpriteBatch.Draw((this.ApplyHover ? ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover1"] : ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"]), this.ApplyProductionRect, Color.White);
			}
		}

		private void DrawSliders(Ship_Game.ScreenManager ScreenManager)
		{
			string str;
			string str1;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 vector2 = new Vector2(x, (float)state.Y);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.SliderFood.sRect.X, this.SliderFood.sRect.Y, (int)(this.SliderFood.amount * (float)this.SliderFood.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderFood.sRect.X, this.SliderFood.sRect.Y, (int)(this.SliderFood.amount * (float)this.SliderFood.sRect.Width), 6)), (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, this.SliderFood.sRect, this.SliderFood.Color);
			if (this.SliderFood.cState != "normal")
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderFood.cursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			}
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderFood.sRect.Width / 10 * i), (float)(this.SliderFood.sRect.Y + this.SliderFood.sRect.Height + 2));
				if (this.SliderFood.state != "normal")
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
				}
				else
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, (this.p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
				}
			}
			Vector2 textPos = new Vector2((float)(this.SliderRect.X + this.SliderRect.Width - 5), (float)(this.SliderFood.sRect.Y + this.SliderFood.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
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
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, new Color(255, 239, 208));
			}
			else
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, Color.LightPink);
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_brown"], new Rectangle(this.SliderProd.sRect.X, this.SliderProd.sRect.Y, (int)(this.SliderProd.amount * (float)this.SliderProd.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderProd.sRect.X, this.SliderProd.sRect.Y, (int)(this.SliderProd.amount * (float)this.SliderProd.sRect.Width), 6)), Color.White);
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, this.SliderProd.sRect, this.SliderProd.Color);
			if (this.SliderProd.cState != "normal")
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderProd.cursor, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderProd.cursor, Color.White);
			}
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderProd.sRect.Width / 10 * i), (float)(this.SliderProd.sRect.Y + this.SliderProd.sRect.Height + 2));
				if (this.SliderProd.state != "normal")
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
				}
				else
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
				}
			}
			textPos = new Vector2((float)(this.SliderRect.X + this.SliderRect.Width - 5), (float)(this.SliderProd.sRect.Y + this.SliderProd.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
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
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, new Color(255, 239, 208));
			}
			else
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, prod, textPos, Color.LightPink);
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_blue"], new Rectangle(this.SliderRes.sRect.X, this.SliderRes.sRect.Y, (int)(this.SliderRes.amount * (float)this.SliderRes.sRect.Width), 6), new Rectangle?(new Rectangle(this.SliderRes.sRect.X, this.SliderRes.sRect.Y, (int)(this.SliderRes.amount * (float)this.SliderRes.sRect.Width), 6)), Color.White);
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, this.SliderRes.sRect, this.SliderRes.Color);
			if (this.SliderRes.cState != "normal")
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.SliderRes.cursor, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.SliderRes.cursor, Color.White);
			}
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.SliderFood.sRect.X + this.SliderRes.sRect.Width / 10 * i), (float)(this.SliderRes.sRect.Y + this.SliderRes.sRect.Height + 2));
				if (this.SliderRes.state != "normal")
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
				}
				else
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
				}
			}
			textPos = new Vector2((float)(this.SliderRect.X + this.SliderRect.Width - 5), (float)(this.SliderRes.sRect.Y + this.SliderRes.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			string res = this.p.NetResearchPerTurn.ToString(this.fmt);
			textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(res).X;
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, res, textPos, new Color(255, 239, 208));
			this.FoodLock.Locked = this.p.FoodLocked;
			this.ProdLock.Locked = this.p.ProdLocked;
			this.ResLock.Locked = this.p.ResLocked;
			if (!this.FoodLock.Hover && !this.FoodLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, new Color(255, 255, 255, 50));
			}
			else if (!this.FoodLock.Hover || this.FoodLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, new Color(255, 255, 255, 150));
			}
			if (!this.ProdLock.Hover && !this.ProdLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, new Color(255, 255, 255, 50));
			}
			else if (!this.ProdLock.Hover || this.ProdLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, new Color(255, 255, 255, 150));
			}
			if (!this.ResLock.Hover && !this.ResLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, new Color(255, 255, 255, 50));
				return;
			}
			if (!this.ResLock.Hover || this.ResLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, Color.White);
				return;
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, new Color(255, 255, 255, 150));
		}

		public void HandleInput(InputState input, Ship_Game.ScreenManager ScreenManager)
		{
			this.p.UpdateIncomes();
			this.currentMouse = Mouse.GetState();
			if (!HelperFunctions.CheckIntersection(this.ApplyProductionRect, input.CursorPosition))
			{
				this.ApplyHover = false;
			}
			else
			{
				ToolTip.CreateTooltip(50, ScreenManager);
				this.ApplyHover = true;
			}
			if (HelperFunctions.ClickedRect(this.ApplyProductionRect, input) && this.p.ConstructionQueue.Count > 0)
			{
				this.eScreen.ClickTimer = 0.25f;
				if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
				{
                    bool flag=true;
                    while (this.p.ApplyStoredProduction())
                    {
                        AudioManager.PlayCue("sd_ui_accept_alt3");
                        if(flag)
                        flag = false;

                    }
                    
                    if(flag)
						AudioManager.PlayCue("UI_Misc20");


				}
				else if (this.p.ApplyStoredProduction())
				{

					AudioManager.PlayCue("sd_ui_accept_alt3");
				}
				else 
				{
					AudioManager.PlayCue("UI_Misc20");
				}

			}
			if (this.p.Owner.data.Traits.Cybernetic == 0)
			{
				if (!HelperFunctions.CheckIntersection(this.FoodLock.LockRect, input.CursorPosition))
				{
					this.FoodLock.Hover = false;
				}
				else
				{
					if (this.FoodLock.Locked)
					{
						this.FoodLock.Hover = false;
						if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							this.p.FoodLocked = false;
							this.FoodLock.Locked = false;
							AudioManager.PlayCue("sd_ui_accept_alt3");
						}
					}
					else
					{
						this.FoodLock.Hover = true;
						if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							this.p.FoodLocked = true;
							this.FoodLock.Locked = true;
							AudioManager.PlayCue("sd_ui_accept_alt3");
						}
					}
					ToolTip.CreateTooltip(69, ScreenManager);
				}
			}
			if (!HelperFunctions.CheckIntersection(this.ProdLock.LockRect, input.CursorPosition))
			{
				this.ProdLock.Hover = false;
			}
			else
			{
				if (this.ProdLock.Locked)
				{
					this.ProdLock.Hover = false;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						this.p.ProdLocked = false;
						this.ProdLock.Locked = false;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
				else
				{
					this.ProdLock.Hover = true;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						this.p.ProdLocked = true;
						this.ProdLock.Locked = true;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
				ToolTip.CreateTooltip(69, ScreenManager);
			}
			if (!HelperFunctions.CheckIntersection(this.ResLock.LockRect, input.CursorPosition))
			{
				this.ResLock.Hover = false;
			}
			else
			{
				if (this.ResLock.Locked)
				{
					this.ResLock.Hover = false;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						this.p.ResLocked = false;
						this.ResLock.Locked = false;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
				else
				{
					this.ResLock.Hover = true;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						this.p.ResLocked = true;
						this.ResLock.Locked = true;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
				ToolTip.CreateTooltip(69, ScreenManager);
			}
			if (this.p.Owner.data.Traits.Cybernetic == 0 && HelperFunctions.CheckIntersection(this.foodDropDown.r, input.CursorPosition) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
			{
				this.foodDropDown.Toggle();
				Planet planet1 = this.p;
				planet1.fs = (Planet.GoodState)((int)planet1.fs + (int)Planet.GoodState.IMPORT);
				if (this.p.fs > Planet.GoodState.EXPORT)
				{
					this.p.fs = Planet.GoodState.STORE;
				}
				AudioManager.PlayCue("sd_ui_accept_alt3");
			}
			if (HelperFunctions.CheckIntersection(this.prodDropDown.r, input.CursorPosition) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
			{
				this.prodDropDown.Toggle();
				AudioManager.PlayCue("sd_ui_accept_alt3");
				Planet planet2 = this.p;
				planet2.ps = (Planet.GoodState)((int)planet2.ps + (int)Planet.GoodState.IMPORT);
				if (this.p.ps > Planet.GoodState.EXPORT)
				{
					this.p.ps = Planet.GoodState.STORE;
				}
			}
			if (this.p.Owner.data.Traits.Cybernetic == 0)
			{
				if (HelperFunctions.CheckIntersection(this.SliderFood.sRect, input.CursorPosition) || this.draggingSlider1)
				{
					this.SliderFood.state = "hover";
					this.SliderFood.Color = new Color(164, 154, 133);
				}
				else
				{
					this.SliderFood.state = "normal";
					this.SliderFood.Color = new Color(72, 61, 38);
				}
				if (HelperFunctions.CheckIntersection(this.SliderFood.cursor, input.CursorPosition) || this.draggingSlider1)
				{
					this.SliderFood.cState = "hover";
				}
				else
				{
					this.SliderFood.cState = "normal";
				}
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.sRect, input.CursorPosition) || this.draggingSlider2)
			{
				this.SliderProd.state = "hover";
				this.SliderProd.Color = new Color(164, 154, 133);
			}
			else
			{
				this.SliderProd.state = "normal";
				this.SliderProd.Color = new Color(72, 61, 38);
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.cursor, input.CursorPosition) || this.draggingSlider2)
			{
				this.SliderProd.cState = "hover";
			}
			else
			{
				this.SliderProd.cState = "normal";
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.sRect, input.CursorPosition) || this.draggingSlider3)
			{
				this.SliderRes.state = "hover";
				this.SliderRes.Color = new Color(164, 154, 133);
			}
			else
			{
				this.SliderRes.state = "normal";
				this.SliderRes.Color = new Color(72, 61, 38);
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.cursor, input.CursorPosition) || this.draggingSlider3)
			{
				this.SliderRes.cState = "hover";
			}
			else
			{
				this.SliderRes.cState = "normal";
			}
			if (HelperFunctions.CheckIntersection(this.SliderFood.cursor, input.CursorPosition) && (!this.ProdLock.Locked || !this.ResLock.Locked) && !this.FoodLock.Locked && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider1 = true;
			}
			if (HelperFunctions.CheckIntersection(this.SliderProd.cursor, input.CursorPosition) && (!this.FoodLock.Locked || !this.ResLock.Locked) && !this.ProdLock.Locked && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider2 = true;
			}
			if (HelperFunctions.CheckIntersection(this.SliderRes.cursor, input.CursorPosition) && (!this.ProdLock.Locked || !this.FoodLock.Locked) && !this.ResLock.Locked && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
			{
				this.draggingSlider3 = true;
			}
			if (this.draggingSlider1 && !this.FoodLock.Locked && (!this.ProdLock.Locked || !this.ResLock.Locked))
			{
				this.SliderFood.cursor.X = this.currentMouse.X;
				if (this.SliderFood.cursor.X > this.SliderFood.sRect.X + this.SliderFood.sRect.Width)
				{
					this.SliderFood.cursor.X = this.SliderFood.sRect.X + this.SliderFood.sRect.Width;
				}
				else if (this.SliderFood.cursor.X < this.SliderFood.sRect.X)
				{
					this.SliderFood.cursor.X = this.SliderFood.sRect.X;
				}
				if (this.currentMouse.LeftButton == ButtonState.Released)
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
						Planet farmerPercentage1 = this.p;
						farmerPercentage1.FarmerPercentage = farmerPercentage1.FarmerPercentage + this.p.ResearcherPercentage;
						this.p.ResearcherPercentage = 0f;
					}
				}
				else if (this.ProdLock.Locked && !this.ResLock.Locked)
				{
					Planet researcherPercentage1 = this.p;
					researcherPercentage1.ResearcherPercentage = researcherPercentage1.ResearcherPercentage + difference;
					if (this.p.ResearcherPercentage < 0f)
					{
						Planet farmerPercentage2 = this.p;
						farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + this.p.ResearcherPercentage;
						this.p.ResearcherPercentage = 0f;
					}
				}
				else if (!this.ProdLock.Locked && this.ResLock.Locked)
				{
					Planet workerPercentage1 = this.p;
					workerPercentage1.WorkerPercentage = workerPercentage1.WorkerPercentage + difference;
					if (this.p.WorkerPercentage < 0f)
					{
						Planet farmerPercentage3 = this.p;
						farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + this.p.WorkerPercentage;
						this.p.WorkerPercentage = 0f;
					}
				}
			}
			if (this.draggingSlider2 && !this.ProdLock.Locked && (!this.FoodLock.Locked || !this.ResLock.Locked))
			{
				this.SliderProd.cursor.X = this.currentMouse.X;
				if (this.SliderProd.cursor.X > this.SliderProd.sRect.X + this.SliderProd.sRect.Width)
				{
					this.SliderProd.cursor.X = this.SliderProd.sRect.X + this.SliderProd.sRect.Width;
				}
				else if (this.SliderProd.cursor.X < this.SliderProd.sRect.X)
				{
					this.SliderProd.cursor.X = this.SliderProd.sRect.X;
				}
				if (this.currentMouse.LeftButton == ButtonState.Released)
				{
					this.draggingSlider2 = false;
				}
				this.pPercentLast = this.p.WorkerPercentage;
				this.p.WorkerPercentage = ((float)this.SliderProd.cursor.X - (float)this.SliderProd.sRect.X) / (float)this.SliderProd.sRect.Width;
				float difference = this.pPercentLast - this.p.WorkerPercentage;
				if (!this.FoodLock.Locked && !this.ResLock.Locked)
				{
					Planet planet3 = this.p;
					planet3.FarmerPercentage = planet3.FarmerPercentage + difference / 2f;
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
						Planet workerPercentage3 = this.p;
						workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + this.p.ResearcherPercentage;
						this.p.ResearcherPercentage = 0f;
					}
				}
				else if (this.FoodLock.Locked && !this.ResLock.Locked)
				{
					Planet researcherPercentage3 = this.p;
					researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
					if (this.p.ResearcherPercentage < 0f)
					{
						Planet workerPercentage4 = this.p;
						workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + this.p.ResearcherPercentage;
						this.p.ResearcherPercentage = 0f;
					}
				}
				else if (!this.FoodLock.Locked && this.ResLock.Locked)
				{
					Planet farmerPercentage4 = this.p;
					farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference;
					if (this.p.FarmerPercentage < 0f)
					{
						Planet planet4 = this.p;
						planet4.WorkerPercentage = planet4.WorkerPercentage + this.p.FarmerPercentage;
						this.p.FarmerPercentage = 0f;
					}
				}
			}
			if (this.draggingSlider3 && !this.ResLock.Locked && (!this.FoodLock.Locked || !this.ProdLock.Locked))
			{
				this.SliderRes.cursor.X = this.currentMouse.X;
				if (this.SliderRes.cursor.X > this.SliderRes.sRect.X + this.SliderRes.sRect.Width)
				{
					this.SliderRes.cursor.X = this.SliderRes.sRect.X + this.SliderRes.sRect.Width;
				}
				else if (this.SliderRes.cursor.X < this.SliderRes.sRect.X)
				{
					this.SliderRes.cursor.X = this.SliderRes.sRect.X;
				}
				if (this.currentMouse.LeftButton == ButtonState.Released)
				{
					this.draggingSlider3 = false;
				}
				this.rPercentLast = this.p.ResearcherPercentage;
				this.p.ResearcherPercentage = ((float)this.SliderRes.cursor.X - (float)this.SliderRes.sRect.X) / (float)this.SliderRes.sRect.Width;
				float difference = this.rPercentLast - this.p.ResearcherPercentage;
				if (!this.ProdLock.Locked && !this.FoodLock.Locked)
				{
					Planet workerPercentage5 = this.p;
					workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference / 2f;
					if (this.p.WorkerPercentage < 0f)
					{
						Planet researcherPercentage4 = this.p;
						researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + this.p.WorkerPercentage;
						this.p.WorkerPercentage = 0f;
					}
					Planet farmerPercentage5 = this.p;
					farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference / 2f;
					if (this.p.FarmerPercentage < 0f)
					{
						Planet researcherPercentage5 = this.p;
						researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + this.p.FarmerPercentage;
						this.p.FarmerPercentage = 0f;
					}
				}
				else if (this.ProdLock.Locked && !this.FoodLock.Locked)
				{
					Planet planet5 = this.p;
					planet5.FarmerPercentage = planet5.FarmerPercentage + difference;
					if (this.p.FarmerPercentage < 0f)
					{
						Planet researcherPercentage6 = this.p;
						researcherPercentage6.ResearcherPercentage = researcherPercentage6.ResearcherPercentage + this.p.FarmerPercentage;
						this.p.FarmerPercentage = 0f;
					}
				}
				else if (!this.ProdLock.Locked && this.FoodLock.Locked)
				{
					Planet workerPercentage6 = this.p;
					workerPercentage6.WorkerPercentage = workerPercentage6.WorkerPercentage + difference;
					if (this.p.WorkerPercentage < 0f)
					{
						Planet planet6 = this.p;
						planet6.ResearcherPercentage = planet6.ResearcherPercentage + this.p.WorkerPercentage;
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
			this.SliderFood.amount = this.p.FarmerPercentage;
			this.SliderFood.cursor = new Rectangle(this.SliderFood.sRect.X + (int)((float)this.SliderFood.sRect.Width * this.SliderFood.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderFood.sRect.Y + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.SliderProd.amount = this.p.WorkerPercentage;
			this.SliderProd.cursor = new Rectangle(this.SliderProd.sRect.X + (int)((float)this.SliderProd.sRect.Width * this.SliderProd.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderProd.sRect.Y + this.SliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.SliderRes.amount = this.p.ResearcherPercentage;
			this.SliderRes.cursor = new Rectangle(this.SliderRes.sRect.X + (int)((float)this.SliderRes.sRect.Width * this.SliderRes.amount) - ResourceManager.TextureDict["NewUI/slider_crosshair"].Width / 2, this.SliderRes.sRect.Y + this.SliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.previousMouse = this.currentMouse;
			this.p.UpdateIncomes();
		}

		public void SetNewPos(int x, int y)
		{
			if (Ship.universeScreen.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366)
			{
				this.LowRes = true;
			}
			int SliderWidth = 375;
			if (this.LowRes)
			{
				SliderWidth = 250;
			}
			this.p.UpdateIncomes();
			this.TotalEntrySize = new Rectangle(x, y, this.TotalEntrySize.Width, this.TotalEntrySize.Height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f) - 30, this.TotalEntrySize.Height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)(this.TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f), this.TotalEntrySize.Height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 30, this.TotalEntrySize.Height);
			this.FoodRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 30, y, 30, this.TotalEntrySize.Height);
			this.ProdRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 60, y, 30, this.TotalEntrySize.Height);
			this.ResRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 90, y, 30, this.TotalEntrySize.Height);
			this.MoneyRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 120, y, 30, this.TotalEntrySize.Height);
			this.SliderRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + 150, y, this.SliderRect.Width, this.TotalEntrySize.Height);
			this.StorageRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.SliderRect.Width + 150, y, this.StorageRect.Width, this.TotalEntrySize.Height);
			this.QueueRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.SliderRect.Width + this.StorageRect.Width + 150, y, this.QueueRect.Width, this.TotalEntrySize.Height);
			float width = (float)((int)((float)this.SliderRect.Width * 0.8f));
			if (SliderWidth == 250)
			{
				width = 180f;
			}
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			Rectangle foodRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.25 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderFood.sRect = foodRect;
			this.SliderFood.amount = this.p.FarmerPercentage;
			this.FoodLock.LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderFood.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height);
			Rectangle prodRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.5 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderProd.sRect = prodRect;
			this.SliderProd.amount = this.p.WorkerPercentage;
			this.ProdLock.LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderProd.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height);
			Rectangle resRect = new Rectangle(this.SliderRect.X + 10, this.SliderRect.Y + (int)(0.75 * (double)this.SliderRect.Height), (int)width, 6);
			this.SliderRes.sRect = resRect;
			this.SliderRes.amount = this.p.ResearcherPercentage;
			this.ResLock.LockRect = new Rectangle(this.SliderFood.sRect.X + this.SliderFood.sRect.Width + 10, this.SliderRes.sRect.Y + 2 + this.SliderFood.sRect.Height / 2 - ResourceManager.TextureDict[this.FoodLock.Path].Height / 2, ResourceManager.TextureDict[this.FoodLock.Path].Width, ResourceManager.TextureDict[this.FoodLock.Path].Height);
			this.FoodStorage = new ProgressBar(new Rectangle(this.StorageRect.X + 50, this.SliderRect.Y + (int)(0.25 * (double)this.SliderRect.Height), (int)(0.4f * (float)this.StorageRect.Width), 18))
			{
				Max = this.p.MAX_STORAGE,
				Progress = this.p.FoodHere,
				color = "green"
			};
			int ddwidth = (int)(0.2f * (float)this.StorageRect.Width);
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				ddwidth = (int)Fonts.Arial12.MeasureString(Localizer.Token(330)).X + 22;
			}
			this.foodDropDown = new DropDownMenu(new Rectangle(this.StorageRect.X + 50 + (int)(0.4f * (float)this.StorageRect.Width) + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			this.foodDropDown.AddOption(Localizer.Token(329));
			this.foodDropDown.AddOption(Localizer.Token(330));
			this.foodDropDown.AddOption(Localizer.Token(331));
			this.foodDropDown.ActiveIndex = (int)this.p.fs;
			this.foodStorageIcon = new Rectangle(this.StorageRect.X + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
			this.ProdStorage = new ProgressBar(new Rectangle(this.StorageRect.X + 50, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height + 10, (int)(0.4f * (float)this.StorageRect.Width), 18))
			{
				Max = this.p.MAX_STORAGE,
				Progress = this.p.ProductionHere
			};
			this.prodStorageIcon = new Rectangle(this.StorageRect.X + 20, this.ProdStorage.pBar.Y + this.ProdStorage.pBar.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
			this.prodDropDown = new DropDownMenu(new Rectangle(this.StorageRect.X + 50 + (int)(0.4f * (float)this.StorageRect.Width) + 20, this.ProdStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			this.prodDropDown.AddOption(Localizer.Token(329));
			this.prodDropDown.AddOption(Localizer.Token(330));
			this.prodDropDown.AddOption(Localizer.Token(331));
			this.prodDropDown.ActiveIndex = (int)this.p.ps;
			this.ApplyProductionRect = new Rectangle(this.QueueRect.X + this.QueueRect.Width - 50, this.QueueRect.Y + this.QueueRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height);
		}
	}
}
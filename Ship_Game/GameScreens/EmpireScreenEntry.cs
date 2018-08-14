using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class EmpireScreenEntry
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

		private ColonyScreen.ColonySlider ColonySliderFood;

		private ColonyScreen.ColonySlider ColonySliderProd;

		private ColonyScreen.ColonySlider ColonySliderRes;

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
			if (Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366)
			{
				LowRes = true;
			}
			int SliderWidth = 375;
			if (LowRes)
			{
				SliderWidth = 250;
			}
			this.eScreen = eScreen;
			p = planet;
			TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			SysNameRect = new Rectangle(x, y, (int)((TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f) - 30, height);
			PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)((TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f), height);
			PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 30, height);
			FoodRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 30, y, 30, height);
			ProdRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 60, y, 30, height);
			ResRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 90, y, 30, height);
			MoneyRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 120, y, 30, height);
			SliderRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 150, y, SliderWidth, height);
			StorageRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + 150, y, (int)((TotalEntrySize.Width - (SliderWidth + 120)) * 0.33f), height);
			QueueRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + StorageRect.Width + 150, y, (int)((TotalEntrySize.Width - (SliderWidth + 150)) * 0.33f), height);
			float width = (int)(SliderRect.Width * 0.8f);
			if (SliderWidth == 250)
			{
				width = 180f;
			}
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			Rectangle foodRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.25 * SliderRect.Height), (int)width, 6);
			ColonySliderFood = new ColonyScreen.ColonySlider();
			
				ColonySliderFood.sRect = foodRect;
                ColonySliderFood.amount = p.FarmerPercentage;
			
			FoodLock = new ColonyScreen.Lock();

            FoodLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderFood.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height);
			
			if (p.Owner.data.Traits.Cybernetic != 0)
			{
				p.FoodLocked = true;
			}
			FoodLock.Locked = p.FoodLocked;
			Rectangle prodRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.5 * SliderRect.Height), (int)width, 6);
			ColonySliderProd = new ColonyScreen.ColonySlider
			{
				sRect = prodRect,
				amount = p.WorkerPercentage
			};
			ProdLock = new ColonyScreen.Lock
			{
				LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderProd.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height),
				Locked = p.ProdLocked
			};
			Rectangle resRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.75 * SliderRect.Height), (int)width, 6);
			ColonySliderRes = new ColonyScreen.ColonySlider
			{
				sRect = resRect,
				amount = p.ResearcherPercentage
			};
			ResLock = new ColonyScreen.Lock
			{
				LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderRes.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height),
				Locked = p.ResLocked
			};
			FoodStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, SliderRect.Y + (int)(0.25 * SliderRect.Height), (int)(0.4f * StorageRect.Width), 18))
			{
				Max = p.MaxStorage,
				Progress = p.FoodHere,
				color = "green"
			};
			int ddwidth = (int)(0.2f * StorageRect.Width);
			if (GlobalStats.IsGermanOrPolish)
			{
				ddwidth = (int)Fonts.Arial12.MeasureString(Localizer.Token(330)).X + 22;
			}
			foodDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			foodDropDown.AddOption(Localizer.Token(329));
			foodDropDown.AddOption(Localizer.Token(330));
			foodDropDown.AddOption(Localizer.Token(331));
			foodDropDown.ActiveIndex = (int)p.FS;
			foodStorageIcon = new Rectangle(StorageRect.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
			ProdStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, FoodStorage.pBar.Y + FoodStorage.pBar.Height + 10, (int)(0.4f * StorageRect.Width), 18))
			{
				Max = p.MaxStorage,
				Progress = p.ProductionHere
			};
			prodStorageIcon = new Rectangle(StorageRect.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
			prodDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			prodDropDown.AddOption(Localizer.Token(329));
			prodDropDown.AddOption(Localizer.Token(330));
			prodDropDown.AddOption(Localizer.Token(331));
			prodDropDown.ActiveIndex = (int)p.PS;
			ApplyProductionRect = new Rectangle(QueueRect.X + QueueRect.Width - 50, QueueRect.Y + QueueRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			string str;
			string str1;
			float x = Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, state.Y);
			Color TextColor = new Color(255, 239, 208);
			if (Fonts.Pirulen16.MeasureString(p.ParentSystem.Name).X <= SysNameRect.Width)
			{
				Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen16.MeasureString(p.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, p.ParentSystem.Name, SysNameCursor, TextColor);
			}
			else
			{
				Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen12.MeasureString(p.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, p.ParentSystem.Name, SysNameCursor, TextColor);
			}
			Rectangle planetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 25, PlanetNameRect.Height - 50, PlanetNameRect.Height - 50);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Planets/", p.PlanetType)), planetIconRect, Color.White);
			Vector2 cursor = new Vector2(PopRect.X + PopRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			float population = p.Population / 1000f;
			string popstring = population.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(popstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, popstring, cursor, Color.White);
			cursor = new Vector2(FoodRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			if (p.Owner.data.Traits.Cybernetic == 0)
			{
				float netFoodPerTurn = p.NetFoodPerTurn - p.Consumption;
				str = netFoodPerTurn.ToString("#.0");
			}
			else
			{
				str = p.NetFoodPerTurn.ToString("#.0");
			}
			string fstring = str;
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(fstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, fstring, cursor, (p.NetFoodPerTurn - p.Consumption >= 0f ? Color.White : Color.LightPink));
			cursor = new Vector2(ProdRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			if (p.Owner.data.Traits.Cybernetic != 0)
			{
				float netProductionPerTurn = p.NetProductionPerTurn - p.Consumption;
				str1 = netProductionPerTurn.ToString("#.0");
			}
			else
			{
				str1 = p.NetProductionPerTurn.ToString("#.0");
			}
			string pstring = str1;
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(pstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			bool pink = false;
			if (p.Owner.data.Traits.Cybernetic != 0 && p.NetProductionPerTurn - p.Consumption < 0f)
			{
				pink = true;
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, pstring, cursor, (pink ? Color.LightPink : Color.White));
			cursor = new Vector2(ResRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			string rstring = p.NetResearchPerTurn.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(rstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, rstring, cursor, Color.White);
			cursor = new Vector2(MoneyRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			float Money = p.NetIncome;
			string mstring = Money.ToString("#.0");
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(mstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, mstring, cursor, (Money >= 0f ? Color.White : Color.LightPink));
			if (Fonts.Pirulen16.MeasureString(p.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
			{
				Vector2 PlanetNameCursor = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, p.Name, PlanetNameCursor, TextColor);
			}
			else if (Fonts.Pirulen12.MeasureString(p.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
			{
				Vector2 PlanetNameCursor = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
				ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, p.Name, PlanetNameCursor, TextColor);
			}
			else
			{
				Vector2 PlanetNameCursor = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial8Bold.LineSpacing / 2);
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, p.Name, PlanetNameCursor, TextColor);
			}
			DrawSliders(ScreenManager);
			if (p.Owner.data.Traits.Cybernetic != 0)
			{
				FoodStorage.DrawGrayed(ScreenManager.SpriteBatch);
				foodDropDown.DrawGrayed(ScreenManager.SpriteBatch);
			}
			else
			{
				FoodStorage.Draw(ScreenManager.SpriteBatch);
				foodDropDown.Draw(ScreenManager.SpriteBatch);
			}
			ProdStorage.Draw(ScreenManager.SpriteBatch);
			prodDropDown.Draw(ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), foodStorageIcon, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : new Color(110, 110, 110, 255)));
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), prodStorageIcon, Color.White);
			if (foodStorageIcon.HitTest(MousePos))
			{
				if (p.Owner.data.Traits.Cybernetic != 0)
				{
					ToolTip.CreateTooltip(77);
				}
				else
				{
					ToolTip.CreateTooltip(73);
				}
			}
			if (prodStorageIcon.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(74);
			}
			if (p.ConstructionQueue.Count > 0)
			{
				QueueItem qi = p.ConstructionQueue[0];
				Vector2 bCursor = new Vector2(QueueRect.X + 10, QueueRect.Y + QueueRect.Height / 2 - 15);
				if (qi.isBuilding)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", qi.Building.Icon, "_48x48")), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.Building.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
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
					ScreenManager.SpriteBatch.Draw(ResourceManager.HullsDict[qi.sData.Hull].Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    if (qi.DisplayName != null)
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.DisplayName, tCursor, Color.White);
                    else
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.sData.Name, tCursor, Color.White);  //display construction ship
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(ScreenManager.SpriteBatch);
				}
				else if (qi.isTroop)
				{
                    Troop template = ResourceManager.GetTroopTemplate(qi.troopType);
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Troops/" + template.TexturePath), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.troopType, tCursor, Color.White);
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(ScreenManager.SpriteBatch);
				}
				ScreenManager.SpriteBatch.Draw((ApplyHover ? ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1") : ResourceManager.Texture("NewUI/icon_queue_rushconstruction")), ApplyProductionRect, Color.White);
			}
		}

		private void DrawSliders(ScreenManager ScreenManager)
		{
			string str1;
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), new Rectangle(ColonySliderFood.sRect.X, ColonySliderFood.sRect.Y, (int)(ColonySliderFood.amount * ColonySliderFood.sRect.Width), 6), new Rectangle(ColonySliderFood.sRect.X, ColonySliderFood.sRect.Y, (int)(ColonySliderFood.amount * ColonySliderFood.sRect.Width), 6), (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : Color.DarkGray));
			ScreenManager.SpriteBatch.DrawRectangle(ColonySliderFood.sRect, ColonySliderFood.Color);
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
			Vector2 textPos = new Vector2(SliderRect.X + SliderRect.Width - 5, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
		    string food;
            if (p.Owner.data.Traits.Cybernetic == 0)
			{
				float netFoodPerTurn = p.NetFoodPerTurn - p.Consumption;
			    food = netFoodPerTurn.String();
			}
			else
			{
			    food = "0";
			}
			textPos.X -= Fonts.Arial12Bold.MeasureString(food).X;
			if (p.NetFoodPerTurn - p.Consumption >= 0f || p.Owner.data.Traits.Cybernetic == 1)
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, new Color(255, 239, 208));
			}
			else
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, food, textPos, Color.LightPink);
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_brown"), new Rectangle(ColonySliderProd.sRect.X, ColonySliderProd.sRect.Y, (int)(ColonySliderProd.amount * ColonySliderProd.sRect.Width), 6), new Rectangle(ColonySliderProd.sRect.X, ColonySliderProd.sRect.Y, (int)(ColonySliderProd.amount * ColonySliderProd.sRect.Width), 6), Color.White);
			ScreenManager.SpriteBatch.DrawRectangle(ColonySliderProd.sRect, ColonySliderProd.Color);
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
			textPos = new Vector2(SliderRect.X + SliderRect.Width - 5, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
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
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_blue"), new Rectangle(ColonySliderRes.sRect.X, ColonySliderRes.sRect.Y, (int)(ColonySliderRes.amount * ColonySliderRes.sRect.Width), 6), new Rectangle(ColonySliderRes.sRect.X, ColonySliderRes.sRect.Y, (int)(ColonySliderRes.amount * ColonySliderRes.sRect.Width), 6), Color.White);
			ScreenManager.SpriteBatch.DrawRectangle(ColonySliderRes.sRect, ColonySliderRes.Color);
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
			textPos = new Vector2(SliderRect.X + SliderRect.Width - 5, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			string res = p.NetResearchPerTurn.ToString(fmt);
			textPos.X = textPos.X - Fonts.Arial12Bold.MeasureString(res).X;
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, res, textPos, new Color(255, 239, 208));
			FoodLock.Locked = p.FoodLocked;
			ProdLock.Locked = p.ProdLocked;
			ResLock.Locked = p.ResLocked;
			if (!FoodLock.Hover && !FoodLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, new Color(255, 255, 255, 50));
			}
			else if (!FoodLock.Hover || FoodLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, new Color(255, 255, 255, 150));
			}
			if (!ProdLock.Hover && !ProdLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, new Color(255, 255, 255, 50));
			}
			else if (!ProdLock.Hover || ProdLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, new Color(255, 255, 255, 150));
			}
			if (!ResLock.Hover && !ResLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, new Color(255, 255, 255, 50));
				return;
			}
			if (!ResLock.Hover || ResLock.Locked)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, Color.White);
				return;
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, new Color(255, 255, 255, 150));
		}

		public void HandleInput(InputState input, ScreenManager ScreenManager)
		{
            p.UpdateIncomes(false);
			currentMouse = Mouse.GetState();
			if (!ApplyProductionRect.HitTest(input.CursorPosition))
			{
				ApplyHover = false;
			}
			else
			{
				ToolTip.CreateTooltip(50);
				ApplyHover = true;
			}
			if (HelperFunctions.ClickedRect(ApplyProductionRect, input) && p.ConstructionQueue.Count > 0)
			{
				eScreen.ClickTimer = 0.25f;
				if (input.KeysCurr.IsKeyDown(Keys.LeftControl))
				{
                    bool flag=true;
                    while (p.ApplyStoredProduction(0))
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        if(flag)
                        flag = false;

                    }
                    
                    if(flag)
						GameAudio.PlaySfxAsync("UI_Misc20");


				}
				else if (p.ApplyStoredProduction(0))
				{

					GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
				}
				else 
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}

			}
			if (p.Owner.data.Traits.Cybernetic == 0)
			{
				if (!FoodLock.LockRect.HitTest(input.CursorPosition))
				{
					FoodLock.Hover = false;
				}
				else
				{
					if (FoodLock.Locked)
					{
						FoodLock.Hover = false;
						if (input.LeftMouseClick)
						{
							p.FoodLocked = false;
							FoodLock.Locked = false;
							GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
						}
					}
					else
					{
						FoodLock.Hover = true;
						if (input.LeftMouseClick)
						{
							p.FoodLocked = true;
							FoodLock.Locked = true;
							GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
						}
					}
					ToolTip.CreateTooltip(69);
				}
			}
			if (!ProdLock.LockRect.HitTest(input.CursorPosition))
			{
				ProdLock.Hover = false;
			}
			else
			{
				if (ProdLock.Locked)
				{
					ProdLock.Hover = false;
					if (input.LeftMouseClick)
					{
						p.ProdLocked = false;
						ProdLock.Locked = false;
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
					}
				}
				else
				{
					ProdLock.Hover = true;
					if (input.LeftMouseClick)
					{
						p.ProdLocked = true;
						ProdLock.Locked = true;
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
					}
				}
				ToolTip.CreateTooltip(69);
			}
			if (!ResLock.LockRect.HitTest(input.CursorPosition))
			{
				ResLock.Hover = false;
			}
			else
			{
				if (ResLock.Locked)
				{
					ResLock.Hover = false;
					if (input.LeftMouseClick)
					{
						p.ResLocked = false;
						ResLock.Locked = false;
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
					}
				}
				else
				{
					ResLock.Hover = true;
					if (input.LeftMouseClick)
					{
						p.ResLocked = true;
						ResLock.Locked = true;
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
					}
				}
				ToolTip.CreateTooltip(69);
			}
		    if (p.Owner.data.Traits.Cybernetic == 0 && foodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
			{
				foodDropDown.Toggle();
				Planet planet1 = p;
				planet1.FS = (Planet.GoodState)((int)planet1.FS + (int)Planet.GoodState.IMPORT);
				if (p.FS > Planet.GoodState.EXPORT)
				{
					p.FS = Planet.GoodState.STORE;
				}
				GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
			}
		    if (prodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
			{
				prodDropDown.Toggle();
				GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
				Planet planet2 = p;
				planet2.PS = (Planet.GoodState)((int)planet2.PS + (int)Planet.GoodState.IMPORT);
				if (p.PS > Planet.GoodState.EXPORT)
				{
					p.PS = Planet.GoodState.STORE;
				}
			}
			if (p.Owner.data.Traits.Cybernetic == 0)
			{
				if (ColonySliderFood.sRect.HitTest(input.CursorPosition) || draggingSlider1)
				{
					ColonySliderFood.state = "hover";
					ColonySliderFood.Color = new Color(164, 154, 133);
				}
				else
				{
					ColonySliderFood.state = "normal";
					ColonySliderFood.Color = new Color(72, 61, 38);
				}
				if (ColonySliderFood.cursor.HitTest(input.CursorPosition) || draggingSlider1)
				{
					ColonySliderFood.cState = "hover";
				}
				else
				{
					ColonySliderFood.cState = "normal";
				}
			}
			if (ColonySliderProd.sRect.HitTest(input.CursorPosition) || draggingSlider2)
			{
				ColonySliderProd.state = "hover";
				ColonySliderProd.Color = new Color(164, 154, 133);
			}
			else
			{
				ColonySliderProd.state = "normal";
				ColonySliderProd.Color = new Color(72, 61, 38);
			}
			if (ColonySliderProd.cursor.HitTest(input.CursorPosition) || draggingSlider2)
			{
				ColonySliderProd.cState = "hover";
			}
			else
			{
				ColonySliderProd.cState = "normal";
			}
			if (ColonySliderRes.sRect.HitTest(input.CursorPosition) || draggingSlider3)
			{
				ColonySliderRes.state = "hover";
				ColonySliderRes.Color = new Color(164, 154, 133);
			}
			else
			{
				ColonySliderRes.state = "normal";
				ColonySliderRes.Color = new Color(72, 61, 38);
			}
			if (ColonySliderRes.cursor.HitTest(input.CursorPosition) || draggingSlider3)
			{
				ColonySliderRes.cState = "hover";
			}
			else
			{
				ColonySliderRes.cState = "normal";
			}
			if (ColonySliderFood.cursor.HitTest(input.CursorPosition) && (!ProdLock.Locked || !ResLock.Locked) && !FoodLock.Locked && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
			{
				draggingSlider1 = true;
			}
			if (ColonySliderProd.cursor.HitTest(input.CursorPosition) && (!FoodLock.Locked || !ResLock.Locked) && !ProdLock.Locked && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
			{
				draggingSlider2 = true;
			}
			if (ColonySliderRes.cursor.HitTest(input.CursorPosition) && (!ProdLock.Locked || !FoodLock.Locked) && !ResLock.Locked && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
			{
				draggingSlider3 = true;
			}
			if (draggingSlider1 && !FoodLock.Locked && (!ProdLock.Locked || !ResLock.Locked))
			{
				ColonySliderFood.cursor.X = currentMouse.X;
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
						Planet farmerPercentage1 = p;
						farmerPercentage1.FarmerPercentage = farmerPercentage1.FarmerPercentage + p.ResearcherPercentage;
						p.ResearcherPercentage = 0f;
					}
				}
				else if (ProdLock.Locked && !ResLock.Locked)
				{
					Planet researcherPercentage1 = p;
					researcherPercentage1.ResearcherPercentage = researcherPercentage1.ResearcherPercentage + difference;
					if (p.ResearcherPercentage < 0f)
					{
						Planet farmerPercentage2 = p;
						farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + p.ResearcherPercentage;
						p.ResearcherPercentage = 0f;
					}
				}
				else if (!ProdLock.Locked && ResLock.Locked)
				{
					Planet workerPercentage1 = p;
					workerPercentage1.WorkerPercentage = workerPercentage1.WorkerPercentage + difference;
					if (p.WorkerPercentage < 0f)
					{
						Planet farmerPercentage3 = p;
						farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + p.WorkerPercentage;
						p.WorkerPercentage = 0f;
					}
				}
			}
			if (draggingSlider2 && !ProdLock.Locked && (!FoodLock.Locked || !ResLock.Locked))
			{
				ColonySliderProd.cursor.X = currentMouse.X;
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
					Planet planet3 = p;
					planet3.FarmerPercentage = planet3.FarmerPercentage + difference / 2f;
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
						Planet workerPercentage3 = p;
						workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + p.ResearcherPercentage;
						p.ResearcherPercentage = 0f;
					}
				}
				else if (FoodLock.Locked && !ResLock.Locked)
				{
					Planet researcherPercentage3 = p;
					researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
					if (p.ResearcherPercentage < 0f)
					{
						Planet workerPercentage4 = p;
						workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + p.ResearcherPercentage;
						p.ResearcherPercentage = 0f;
					}
				}
				else if (!FoodLock.Locked && ResLock.Locked)
				{
					Planet farmerPercentage4 = p;
					farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference;
					if (p.FarmerPercentage < 0f)
					{
						Planet planet4 = p;
						planet4.WorkerPercentage = planet4.WorkerPercentage + p.FarmerPercentage;
						p.FarmerPercentage = 0f;
					}
				}
			}
			if (draggingSlider3 && !ResLock.Locked && (!FoodLock.Locked || !ProdLock.Locked))
			{
				ColonySliderRes.cursor.X = currentMouse.X;
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
					Planet workerPercentage5 = p;
					workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference / 2f;
					if (p.WorkerPercentage < 0f)
					{
						Planet researcherPercentage4 = p;
						researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + p.WorkerPercentage;
						p.WorkerPercentage = 0f;
					}
					Planet farmerPercentage5 = p;
					farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference / 2f;
					if (p.FarmerPercentage < 0f)
					{
						Planet researcherPercentage5 = p;
						researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + p.FarmerPercentage;
						p.FarmerPercentage = 0f;
					}
				}
				else if (ProdLock.Locked && !FoodLock.Locked)
				{
					Planet planet5 = p;
					planet5.FarmerPercentage = planet5.FarmerPercentage + difference;
					if (p.FarmerPercentage < 0f)
					{
						Planet researcherPercentage6 = p;
						researcherPercentage6.ResearcherPercentage = researcherPercentage6.ResearcherPercentage + p.FarmerPercentage;
						p.FarmerPercentage = 0f;
					}
				}
				else if (!ProdLock.Locked && FoodLock.Locked)
				{
					Planet workerPercentage6 = p;
					workerPercentage6.WorkerPercentage = workerPercentage6.WorkerPercentage + difference;
					if (p.WorkerPercentage < 0f)
					{
						Planet planet6 = p;
						planet6.ResearcherPercentage = planet6.ResearcherPercentage + p.WorkerPercentage;
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
			ColonySliderFood.amount = p.FarmerPercentage;
			ColonySliderFood.cursor = new Rectangle(ColonySliderFood.sRect.X + (int)(ColonySliderFood.sRect.Width * ColonySliderFood.amount) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
			ColonySliderProd.amount = p.WorkerPercentage;
			ColonySliderProd.cursor = new Rectangle(ColonySliderProd.sRect.X + (int)(ColonySliderProd.sRect.Width * ColonySliderProd.amount) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
			ColonySliderRes.amount = p.ResearcherPercentage;
			ColonySliderRes.cursor = new Rectangle(ColonySliderRes.sRect.X + (int)(ColonySliderRes.sRect.Width * ColonySliderRes.amount) - ResourceManager.Texture("NewUI/slider_crosshair").Width / 2, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
			previousMouse = currentMouse;
            p.UpdateIncomes(false);
		}

		public void SetNewPos(int x, int y)
		{
			if (Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366)
			{
				LowRes = true;
			}
			int SliderWidth = 375;
			if (LowRes)
			{
				SliderWidth = 250;
			}
            p.UpdateIncomes(false);
			TotalEntrySize = new Rectangle(x, y, TotalEntrySize.Width, TotalEntrySize.Height);
			SysNameRect = new Rectangle(x, y, (int)((TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f) - 30, TotalEntrySize.Height);
			PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)((TotalEntrySize.Width - (SliderWidth + 150)) * 0.17f), TotalEntrySize.Height);
			PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 30, TotalEntrySize.Height);
			FoodRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 30, y, 30, TotalEntrySize.Height);
			ProdRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 60, y, 30, TotalEntrySize.Height);
			ResRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 90, y, 30, TotalEntrySize.Height);
			MoneyRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 120, y, 30, TotalEntrySize.Height);
			SliderRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 150, y, SliderRect.Width, TotalEntrySize.Height);
			StorageRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + 150, y, StorageRect.Width, TotalEntrySize.Height);
			QueueRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + StorageRect.Width + 150, y, QueueRect.Width, TotalEntrySize.Height);
			float width = (int)(SliderRect.Width * 0.8f);
			if (SliderWidth == 250)
			{
				width = 180f;
			}
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			Rectangle foodRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.25 * SliderRect.Height), (int)width, 6);
			ColonySliderFood.sRect = foodRect;
			ColonySliderFood.amount = p.FarmerPercentage;
			FoodLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderFood.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height);
			Rectangle prodRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.5 * SliderRect.Height), (int)width, 6);
			ColonySliderProd.sRect = prodRect;
			ColonySliderProd.amount = p.WorkerPercentage;
			ProdLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderProd.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height);
			Rectangle resRect = new Rectangle(SliderRect.X + 10, SliderRect.Y + (int)(0.75 * SliderRect.Height), (int)width, 6);
			ColonySliderRes.sRect = resRect;
			ColonySliderRes.amount = p.ResearcherPercentage;
			ResLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 10, ColonySliderRes.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture(FoodLock.Path).Height / 2, ResourceManager.Texture(FoodLock.Path).Width, ResourceManager.Texture(FoodLock.Path).Height);
			FoodStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, SliderRect.Y + (int)(0.25 * SliderRect.Height), (int)(0.4f * StorageRect.Width), 18))
			{
				Max = p.MaxStorage,
				Progress = p.FoodHere,
				color = "green"
			};
			int ddwidth = (int)(0.2f * StorageRect.Width);
			if (GlobalStats.IsGermanOrPolish)
			{
				ddwidth = (int)Fonts.Arial12.MeasureString(Localizer.Token(330)).X + 22;
			}
			foodDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			foodDropDown.AddOption(Localizer.Token(329));
			foodDropDown.AddOption(Localizer.Token(330));
			foodDropDown.AddOption(Localizer.Token(331));
			foodDropDown.ActiveIndex = (int)p.FS;
			foodStorageIcon = new Rectangle(StorageRect.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
			ProdStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, FoodStorage.pBar.Y + FoodStorage.pBar.Height + 10, (int)(0.4f * StorageRect.Width), 18))
			{
				Max = p.MaxStorage,
				Progress = p.ProductionHere
			};
			prodStorageIcon = new Rectangle(StorageRect.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
			prodDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
			prodDropDown.AddOption(Localizer.Token(329));
			prodDropDown.AddOption(Localizer.Token(330));
			prodDropDown.AddOption(Localizer.Token(331));
			prodDropDown.ActiveIndex = (int)p.PS;
			ApplyProductionRect = new Rectangle(QueueRect.X + QueueRect.Width - 50, QueueRect.Y + QueueRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height);
		}
	}
}
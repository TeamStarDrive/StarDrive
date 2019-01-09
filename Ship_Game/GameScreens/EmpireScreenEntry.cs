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

        ColonySliderGroup Sliders;

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




		public EmpireScreenEntry(Planet planet, int x, int y, int width1, int height, EmpireScreen eScreen)
		{
			if (Empire.Universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366)
			{
				LowRes = true;
			}
			int sliderWidth = 375;
			this.eScreen = eScreen;
			p = planet;
			TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			SysNameRect = new Rectangle(x, y, (int)((TotalEntrySize.Width - (sliderWidth + 150)) * 0.17f) - 30, height);
			PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)((TotalEntrySize.Width - (sliderWidth + 150)) * 0.17f), height);
			PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 30, height);
			FoodRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 30, y, 30, height);
			ProdRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 60, y, 30, height);
			ResRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 90, y, 30, height);
			MoneyRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 120, y, 30, height);
			SliderRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + 150, y, sliderWidth, height);
			StorageRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + 150, y, (int)((TotalEntrySize.Width - (sliderWidth + 120)) * 0.33f), height);
			QueueRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + SliderRect.Width + StorageRect.Width + 150, y, (int)((TotalEntrySize.Width - (sliderWidth + 150)) * 0.33f), height);
			int width = (int)(SliderRect.Width * 0.8f);
            width = width.RoundUpToMultipleOf(10);

            Sliders = new ColonySliderGroup(null, SliderRect);
            Sliders.Create(SliderRect.X + 10, SliderRect.Y, width, (int)(0.25 * SliderRect.Height), drawIcons:false);
            Sliders.SetPlanet(planet);

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

		public void Draw(SpriteBatch batch)
		{
			float x = Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, state.Y);
			Color TextColor = new Color(255, 239, 208);
			if (Fonts.Pirulen16.MeasureString(p.ParentSystem.Name).X <= SysNameRect.Width)
			{
				Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen16.MeasureString(p.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
				batch.DrawString(Fonts.Pirulen16, p.ParentSystem.Name, SysNameCursor, TextColor);
			}
			else
			{
				Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen12.MeasureString(p.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
				batch.DrawString(Fonts.Pirulen12, p.ParentSystem.Name, SysNameCursor, TextColor);
			}
			Rectangle planetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 25, PlanetNameRect.Height - 50, PlanetNameRect.Height - 50);
			batch.Draw(ResourceManager.Texture(string.Concat("Planets/", p.PlanetType)), planetIconRect, Color.White);
			var cursor = new Vector2(PopRect.X + PopRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			float population = p.PopulationBillion;
			string popstring = population.String();
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(popstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			batch.DrawString(Fonts.Arial12, popstring, cursor, Color.White);
			cursor = new Vector2(FoodRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);

			string fstring = p.Food.NetIncome.String();
			cursor.X -= Fonts.Arial12.MeasureString(fstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			batch.DrawString(Fonts.Arial12, fstring, cursor, (p.Food.NetIncome >= 0f ? Color.White : Color.LightPink));
			
            cursor = new Vector2(ProdRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			string pstring = p.Prod.NetIncome.String();
			cursor.X -= Fonts.Arial12.MeasureString(pstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			bool pink = p.Prod.NetIncome < 0f;
            batch.DrawString(Fonts.Arial12, pstring, cursor, (pink ? Color.LightPink : Color.White));
			
            cursor = new Vector2(ResRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			string rstring = p.Res.NetIncome.String();
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(rstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			batch.DrawString(Fonts.Arial12, rstring, cursor, Color.White);
			
            cursor = new Vector2(MoneyRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
			float money = p.NetIncome;
			string mstring = money.String();
			cursor.X = cursor.X - Fonts.Arial12.MeasureString(mstring).X;
			HelperFunctions.ClampVectorToInt(ref cursor);
			batch.DrawString(Fonts.Arial12, mstring, cursor, (money >= 0f ? Color.White : Color.LightPink));
			
            if (Fonts.Pirulen16.MeasureString(p.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
			{
				var a = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
				batch.DrawString(Fonts.Pirulen16, p.Name, a, TextColor);
			}
			else if (Fonts.Pirulen12.MeasureString(p.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
			{
				var b = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
				batch.DrawString(Fonts.Pirulen12, p.Name, b, TextColor);
			}
			else
			{
				var c = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial8Bold.LineSpacing / 2);
				batch.DrawString(Fonts.Arial8Bold, p.Name, c, TextColor);
			}

			DrawSliders(batch);

			if (p.Owner.data.Traits.Cybernetic != 0)
			{
				FoodStorage.DrawGrayed(batch);
				foodDropDown.DrawGrayed(batch);
			}
			else
			{
				FoodStorage.Draw(batch);
				foodDropDown.Draw(batch);
			}
			ProdStorage.Draw(batch);
			prodDropDown.Draw(batch);
			batch.Draw(ResourceManager.Texture("NewUI/icon_food"), foodStorageIcon, (p.Owner.data.Traits.Cybernetic == 0 ? Color.White : new Color(110, 110, 110, 255)));
			batch.Draw(ResourceManager.Texture("NewUI/icon_production"), prodStorageIcon, Color.White);
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
					batch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", qi.Building.Icon, "_48x48")), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					batch.DrawString(Fonts.Arial12Bold, qi.Building.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(batch);
				}
				if (qi.isShip)
				{
					batch.Draw(ResourceManager.HullsDict[qi.sData.Hull].Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    if (qi.DisplayName != null)
                        batch.DrawString(Fonts.Arial12Bold, qi.DisplayName, tCursor, Color.White);
                    else
                        batch.DrawString(Fonts.Arial12Bold, qi.sData.Name, tCursor, Color.White);  //display construction ship
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(batch);
				}
				else if (qi.isTroop)
				{
                    Troop template = ResourceManager.GetTroopTemplate(qi.troopType);
					batch.Draw(ResourceManager.Texture("Troops/" + template.TexturePath), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					batch.DrawString(Fonts.Arial12Bold, qi.troopType, tCursor, Color.White);
					tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = qi.Cost,
						Progress = qi.productionTowards
					};
					pb.Draw(batch);
				}
				batch.Draw((ApplyHover ? ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1") : ResourceManager.Texture("NewUI/icon_queue_rushconstruction")), ApplyProductionRect, Color.White);
			}
		}

		private void DrawSliders(SpriteBatch batch)
		{
            Sliders.Draw(batch);
		}

		public void HandleInput(InputState input, ScreenManager ScreenManager)
		{
            p.UpdateIncomes(false);
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

            Sliders.HandleInput(input);
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
            Sliders.UpdatePos(SliderRect.X + 10, SliderRect.Y);

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
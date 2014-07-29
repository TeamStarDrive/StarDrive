using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    public class EspionageScreen : GameScreen, IDisposable
    {
        private UniverseScreen screen;

		public DanButton Contact;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu2 DMenu;

		public bool LowRes;

		public Rectangle SelectedInfoRect;

		public Rectangle IntelligenceRect;

		public Rectangle OperationsRect;

		public Empire SelectedEmpire;

		private List<RaceEntry> Races = new List<RaceEntry>();

		private GenericSlider SpyBudgetSlider;

		private GenericSlider CounterSpySlider;

		private Rectangle OpSLRect;

		private ScrollList OperationsSL;

		private DanButton ExecuteOperation;

		private Ship_Game.AgentComponent AgentComponent;

		private CloseButton close;

		private float TransitionElapsedTime;

		public EspionageScreen(UniverseScreen screen)
		{
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 766)
			{
				this.TitleBar.Draw();
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(6089), this.TitlePos, new Color(255, 239, 208));
			}
			this.DMenu.Draw();
			Color color = new Color(118, 102, 67, 50);
			foreach (RaceEntry race in this.Races)
			{
				if (race.e.isFaction)
				{
					continue;
				}
				Vector2 NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
				if (race.e.data.Defeated)
				{
					if (race.e.data.AbsorbedBy == null)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/x_red"], race.container, Color.White);
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
						Rectangle r = new Rectangle(race.container.X, race.container.Y, 124, 124);
						SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
						KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).data.Traits.FlagIndex];
						spriteBatch.Draw(item.Value, r, EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).EmpireColor);
					}
				}
				else if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e && EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].Known)
				{
					if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].AtWar && !race.e.data.Defeated)
					{
						Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, war, Color.Red);
					}
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
				}
				else if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e)
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/unknown"], race.container, Color.White);
				}
				else
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
					NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
				}
				if (race.e != this.SelectedEmpire)
				{
					continue;
				}
				Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, race.container, Color.Orange);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.SelectedInfoRect, new Color(23, 20, 14));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.IntelligenceRect, new Color(23, 20, 14));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.OperationsRect, new Color(23, 20, 14));
			Vector2 TextCursor = new Vector2((float)(this.SelectedInfoRect.X + 20), (float)(this.SelectedInfoRect.Y + 10));
			TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 4);
			HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(6090), TextCursor, Fonts.Arial20Bold);
            TextCursor.X = this.IntelligenceRect.X + 20;
            HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(6092), TextCursor, Fonts.Arial20Bold);
			this.AgentComponent.Draw();
			if (this.AgentComponent.SelectedAgent != null)
			{
				TextCursor = new Vector2((float)(this.OperationsRect.X + 20), (float)(this.OperationsRect.Y + 10));
				HelperFunctions.DrawDropShadowText(base.ScreenManager, this.AgentComponent.SelectedAgent.Name, TextCursor, Fonts.Arial20Bold);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Level ", this.AgentComponent.SelectedAgent.Level.ToString(), " Agent"), TextCursor, Color.Gray);
			}
			this.close.Draw(base.ScreenManager);
			if (base.IsActive)
			{
				ToolTip.Draw(base.ScreenManager);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		private void DrawBadStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightPink);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightPink);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		private void DrawGoodStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightGreen);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightGreen);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		private void DrawStat(string text, float value, ref Vector2 Position)
		{
			Color lightGreen;
			Color color;
			if (value <= 10f)
			{
				value = value * 100f;
			}
			HelperFunctions.ClampVectorToInt(ref Position);
			SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
			SpriteFont arial12 = Fonts.Arial12;
			string str = text;
			Vector2 position = Position;
			if (value > 0f)
			{
				lightGreen = Color.LightGreen;
			}
			else
			{
				lightGreen = (value == 0f ? Color.White : Color.LightPink);
			}
			spriteBatch.DrawString(arial12, str, position, lightGreen);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(string.Concat(value.ToString(), "%")).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			string str1 = string.Concat(value.ToString(), "%");
			Vector2 vector2 = nPos;
			if (value > 0f)
			{
				color = Color.LightGreen;
			}
			else
			{
				color = (value == 0f ? Color.White : Color.LightPink);
			}
			spriteBatch1.DrawString(arial12Bold, str1, vector2, color);
			Position.Y = Position.Y + (float)Fonts.Arial12Bold.LineSpacing;
		}

		private void DrawStat(string text, float value, ref Vector2 Position, bool OppositeBonuses)
		{
			Color lightGreen;
			Color color;
			if (value < 10f)
			{
				value = value * 100f;
			}
			HelperFunctions.ClampVectorToInt(ref Position);
			SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
			SpriteFont arial12 = Fonts.Arial12;
			string str = text;
			Vector2 position = Position;
			if (value < 0f)
			{
				lightGreen = Color.LightGreen;
			}
			else
			{
				lightGreen = (value == 0f ? Color.White : Color.LightPink);
			}
			spriteBatch.DrawString(arial12, str, position, lightGreen);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
                nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(string.Concat(value.ToString(), "%")).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			string str1 = string.Concat(value.ToString(), "%");
			Vector2 vector2 = nPos;
			if (value < 0f)
			{
				color = Color.LightGreen;
			}
			else
			{
				color = (value == 0f ? Color.White : Color.LightPink);
			}
			spriteBatch1.DrawString(arial12Bold, str1, vector2, color);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		private void DrawStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.White);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
				nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.White);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}


        ~EspionageScreen()
        {
            //should implicitly do the same thing as the original bad finalize
        }

		private float GetMilitaryStr(Empire e)
		{
			float single;
			float str = 0f;
			try
			{
				foreach (Ship ship in e.GetShips())
				{
					str = str + ship.GetStrength();
				}
				return str;
			}
			catch
			{
				single = str;
			}
			return single;
		}

		private float GetPop(Empire e)
		{
			float pop = 0f;
			foreach (Planet p in e.GetPlanets())
			{
				pop = pop + p.Population;
			}
			return pop;
		}

		private float GetScientificStr(Empire e)
		{
			float scientificStr = 0f;
			foreach (KeyValuePair<string, TechEntry> Technology in e.GetTDict())
			{
				if (!Technology.Value.Unlocked)
				{
					continue;
				}
				scientificStr = scientificStr + ResourceManager.TechTree[Technology.Key].Cost;
			}
			return scientificStr;
		}

		private Operation GetSelectedOp()
		{
			for (int i = this.OperationsSL.indexAtTop; i < this.OperationsSL.Entries.Count && i < this.OperationsSL.indexAtTop + this.OperationsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.OperationsSL.Entries[i];
				if ((e.item as Operation).Selected)
				{
					return e.item as Operation;
				}
			}
			return null;
		}

		public override void HandleInput(InputState input)
		{
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			this.AgentComponent.HandleInput(input);
			//this.showExecuteButton = false;
			if (this.SelectedEmpire != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) && !this.SelectedEmpire.data.Defeated && this.Contact.HandleInput(input))
			{
				base.ScreenManager.AddScreen(new DiplomacyScreen(this.SelectedEmpire, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), "Greeting"));
			}
			bool GotRace = false;
			foreach (RaceEntry race in this.Races)
			{
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) == race.e || !EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].Known)
				{
					if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e || !HelperFunctions.ClickedRect(race.container, input))
					{
						continue;
					}
					this.SelectedEmpire = race.e;
					GotRace = true;
					AudioManager.PlayCue("echo_affirm");
					for (int j = this.OperationsSL.indexAtTop; j < this.OperationsSL.Entries.Count && j < this.OperationsSL.indexAtTop + this.OperationsSL.entriesToDisplay; j++)
					{
						ScrollList.Entry f = this.OperationsSL.Entries[j];
						(f.item as Operation).Selected = false;
					}
				}
				else
				{
					if (!HelperFunctions.ClickedRect(race.container, input))
					{
						continue;
					}
					this.SelectedEmpire = race.e;
					GotRace = true;
				}
			}
			
			if (!HelperFunctions.CheckIntersection(this.AgentComponent.ComponentRect, input.CursorPosition))
			{
				if (input.InGameSelect && !GotRace)
				{
					this.AgentComponent.SelectedAgent = null;
				}
				else if (input.InGameSelect && GotRace)
				{
					this.AgentComponent.Reinitialize();
				}
			}
			if (input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			float screenWidth = (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			float screenHeight = (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			Rectangle titleRect = new Rectangle((int)screenWidth / 2 - 200, 44, 400, 80);
			this.TitleBar = new Menu2(base.ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(6089)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle leftRect = new Rectangle((int)screenWidth / 2 - 640, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1280, 660);
			this.DMenu = new Menu2(base.ScreenManager, leftRect);
			this.close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
			this.SelectedInfoRect = new Rectangle(leftRect.X + 60, leftRect.Y + 250, 368, 376);
			this.IntelligenceRect = new Rectangle(this.SelectedInfoRect.X + this.SelectedInfoRect.Width + 30, this.SelectedInfoRect.Y, 368, 376);
			this.OperationsRect = new Rectangle(this.IntelligenceRect.X + this.IntelligenceRect.Width + 30, this.SelectedInfoRect.Y, 368, 376);
			this.Contact = new DanButton(new Vector2((float)(this.SelectedInfoRect.X + this.SelectedInfoRect.Width / 2 - 91), (float)(this.SelectedInfoRect.Y + this.SelectedInfoRect.Height - 45)), Localizer.Token(1644))
			{
				Toggled = true
			};
			this.OpSLRect = new Rectangle(this.OperationsRect.X + 20, this.OperationsRect.Y + 20, this.OperationsRect.Width - 40, this.OperationsRect.Height - 45);
			Submenu OpSub = new Submenu(base.ScreenManager, this.OpSLRect);
			this.OperationsSL = new ScrollList(OpSub, Fonts.Arial12Bold.LineSpacing + 5);
			Empire.PlantMole.Selected = false;
			Empire.DiscoverPlot.Selected = false;
			Empire.DamageRelations.Selected = false;
			Empire.PlantBomb.Selected = false;
			Empire.TriggerBombs.Selected = false;
			Empire.StealTech.Selected = false;
			this.OperationsSL.AddItem(Empire.PlantMole);
			this.OperationsSL.AddItem(Empire.DiscoverPlot);
			this.OperationsSL.AddItem(Empire.DamageRelations);
			this.OperationsSL.AddItem(Empire.PlantBomb);
			this.OperationsSL.AddItem(Empire.TriggerBombs);
			this.OperationsSL.AddItem(Empire.StealTech);
			Vector2 ExecutePos = new Vector2((float)(this.OperationsRect.X + this.OperationsRect.Width / 2 - 91), (float)(this.OperationsRect.Y + this.OperationsRect.Height - 60));
			this.ExecuteOperation = new DanButton(ExecutePos, "Execute Op")
			{
				Toggled = true
			};
            Rectangle ComponentRect = new Rectangle(this.SelectedInfoRect.X + 20, this.SelectedInfoRect.Y + 35, this.SelectedInfoRect.Width - 40, this.SelectedInfoRect.Height - 95);
			this.AgentComponent = new Ship_Game.AgentComponent(ComponentRect, this);
			foreach (Empire e in EmpireManager.EmpireList)
			{
				if (e != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
				{
					if (e.isFaction && (e.isFaction || !e.data.Defeated))
					{
						continue;
					}
					RaceEntry re = new RaceEntry()
					{
						e = e
					};
					this.Races.Add(re);
				}
				else
				{
					RaceEntry re = new RaceEntry()
					{
						e = e
					};
					this.SelectedEmpire = e;
					this.Races.Add(re);
				}
			}
			Vector2 Cursor = new Vector2(screenWidth / 2f - (float)(148 * this.Races.Count / 2), (float)(leftRect.Y + 10));
			int j = 0;
			foreach (RaceEntry re in this.Races)
			{
				re.container = new Rectangle((int)Cursor.X + 10 + j * 148, leftRect.Y + 40, 124, 148);
				j++;
			}
			Rectangle rectangle = new Rectangle();
			this.SpyBudgetSlider = new GenericSlider(rectangle, Localizer.Token(1637), 0f, 100f)
			{
				amount = 0f
			};
			Rectangle rectangle1 = new Rectangle();
			this.CounterSpySlider = new GenericSlider(rectangle1, Localizer.Token(1637), 0f, 100f)
			{
				amount = 0f
			};
			base.ScreenManager.racialMusic.SetVolume(0f);
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			EspionageScreen transitionElapsedTime = this;
			transitionElapsedTime.TransitionElapsedTime = transitionElapsedTime.TransitionElapsedTime + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
    }
}

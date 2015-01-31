using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EventScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		public ExplorationEvent ExpEvent;

		private Outcome outcome;

		private UniverseScreen screen;

		private Rectangle MainRect;

		private Rectangle TopRect;

		private Rectangle BlackRect;

		//private float transitionElapsedTime;

		public EventScreen(UniverseScreen screen, Empire playerEmpire, ExplorationEvent e, Outcome outcome)
		{
			this.outcome = outcome;
			this.ExpEvent = e;
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
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.MainRect, new Color(37, 37, 37));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.TopRect, new Color(54, 54, 54));
			Vector2 cursor = new Vector2((float)(this.TopRect.X + 10), (float)(this.TopRect.Y + 4));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, this.ExpEvent.Name, cursor, Fonts.Arial12Bold);
			Vector2 DescriptionPos = new Vector2((float)(this.MainRect.X + 30), (float)(this.MainRect.Y + 40));
			string Description = HelperFunctions.parseText(Fonts.Arial12Bold, this.outcome.DescriptionText, (float)(this.MainRect.Width - 55));
			if (Fonts.Arial12Bold.MeasureString(Description).Y < (float)this.MainRect.Height)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Description, DescriptionPos, Color.White);
			}
			this.BlackRect = new Rectangle(this.MainRect.X + 20, (int)DescriptionPos.Y + (int)Fonts.Arial12Bold.MeasureString(Description).Y + 10, this.MainRect.Width - 40, this.MainRect.Height - ((int)DescriptionPos.Y + (int)Fonts.Arial12Bold.MeasureString(Description).Y + 10 - this.MainRect.Y) - 20);
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.BlackRect, Color.Black);
			Vector2 TheirTextPos = new Vector2((float)(this.BlackRect.X + 10), (float)(this.BlackRect.Y + 10));
			if (this.outcome.GetArtifact() != null)
			{
				string theirText = string.Concat("Artifact Granted: ", this.outcome.GetArtifact().Name);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.LightGreen);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				Rectangle Icon = new Rectangle((int)TheirTextPos.X, (int)TheirTextPos.Y, 32, 32);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Artifact Icons/", this.outcome.GetArtifact().Name)], Icon, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + 36f;
				theirText = HelperFunctions.parseText(Fonts.Arial12, this.outcome.GetArtifact().Description, (float)(this.MainRect.Width - 40));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + Fonts.Arial12.MeasureString(theirText).Y;
				if (this.outcome.GetArtifact().DiplomacyMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int diplomacyMod = (int)(this.outcome.GetArtifact().DiplomacyMod * 100f);
					theirText = string.Concat("Diplomacy Bonus: ", diplomacyMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().FertilityMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					theirText = string.Concat("Fertility Bonus to all Owned Colonies: ", this.outcome.GetArtifact().FertilityMod.ToString("#.0"));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().GroundCombatMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int groundCombatMod = (int)(this.outcome.GetArtifact().GroundCombatMod * 100f);
					theirText = string.Concat("Empire-wide Ground Combat Bonus: ", groundCombatMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().ModuleHPMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int moduleHPMod = (int)(this.outcome.GetArtifact().ModuleHPMod * 100f);
					theirText = string.Concat("Empire-wide Ship Module Hitpoint Bonus: ", moduleHPMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().PlusFlatMoney > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					theirText = string.Concat("Credits per Turn Bonus: ", this.outcome.GetArtifact().PlusFlatMoney.ToString());
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().ProductionMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int productionMod = (int)(this.outcome.GetArtifact().ProductionMod * 100f);
					theirText = string.Concat("Empire-wide Production Bonus: ", productionMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().ReproductionMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int reproductionMod = (int)(this.outcome.GetArtifact().ReproductionMod * 100f);
					theirText = string.Concat("Empire-wide Popoulation Growth Bonus: ", reproductionMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().ResearchMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int researchMod = (int)(this.outcome.GetArtifact().ResearchMod * 100f);
					theirText = string.Concat("Empire-wide Research Bonus: ", researchMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().SensorMod > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int sensorMod = (int)(this.outcome.GetArtifact().SensorMod * 100f);
					theirText = string.Concat("Empire-wide Sensor Range Bonus: ", sensorMod.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
				if (this.outcome.GetArtifact().ShieldPenBonus > 0f)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					int shieldPenBonus = (int)(this.outcome.GetArtifact().ShieldPenBonus * 100f);
					theirText = string.Concat("Empire-wide Bonus Shield Penetration Chance: ", shieldPenBonus.ToString(), "%");
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				}
			}
			if (this.outcome.UnlockTech != null)
			{
				if (!this.outcome.WeHadIt)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					string theirText = string.Concat("Technology Acquired: ", Localizer.Token(ResourceManager.TechTree[this.outcome.UnlockTech].NameIndex));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
					TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
					if (ResourceManager.TechTree[this.outcome.UnlockTech].ModulesUnlocked.Count > 0)
					{
						ShipModule unlockedMod = ResourceManager.ShipModulesDict[ResourceManager.TechTree[this.outcome.UnlockTech].ModulesUnlocked[0].ModuleUID];
						Rectangle IconRect = new Rectangle((int)TheirTextPos.X, (int)TheirTextPos.Y, 16 * unlockedMod.XSIZE, 16 * unlockedMod.YSIZE);
						//{
							IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
                            IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;
						//};
						while (IconRect.Height > 96)
						{
							IconRect.Height = IconRect.Height - unlockedMod.YSIZE;
							IconRect.Width = IconRect.Width - unlockedMod.XSIZE;
							IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
							IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;
						}
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[unlockedMod.UID].IconTexturePath], IconRect, Color.White);
						string moduleName = Localizer.Token(unlockedMod.NameIndex);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, moduleName, new Vector2(TheirTextPos.X + 100f, TheirTextPos.Y), Color.Orange);
						string desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(unlockedMod.DescriptionIndex), (float)(this.BlackRect.Width - 120));
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, new Vector2(TheirTextPos.X + 100f, TheirTextPos.Y + 22f), Color.White);
					}
				}
				else
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					string theirText = "We found some alien technology, but we already possessed this knowledge.";
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
					TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
			}
			if (this.outcome.MoneyGranted > 0)
			{
				TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
				string theirText = string.Concat("Money Granted: ", this.outcome.MoneyGranted.ToString());
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			if (this.outcome.ScienceBonus > 0f)
			{
				TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
				int scienceBonus = (int)(this.outcome.ScienceBonus * 100f);
				string theirText = string.Concat("Research Bonus Granted: ", scienceBonus.ToString(), "%");
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, this.MainRect, new Color(24, 81, 91));
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}


		public override void HandleInput(InputState input)
		{
			if (input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.MainRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 400);
			this.TopRect = new Rectangle(this.MainRect.X, this.MainRect.Y, this.MainRect.Width, 28);
			this.BlackRect = new Rectangle(this.MainRect.X + 20, this.MainRect.Y + 80, this.MainRect.Width - 40, 240);
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}
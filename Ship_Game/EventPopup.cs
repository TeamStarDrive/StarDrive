using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EventPopup : PopupWindow
	{
		public bool fade = true;

		public bool FromGame;

		private UniverseScreen screen;

		public ExplorationEvent ExpEvent;

		private Outcome outcome;

		private Rectangle BlackRect;

		public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, Outcome outcome)
		{
			this.screen = s;
			this.outcome = outcome;
			this.ExpEvent = e;
			this.fade = true;
			base.IsPopup = true;
			this.FromGame = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
			this.r = new Rectangle(0, 0, 600, 600);
		}

		public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, Outcome outcome, bool TriggerNow)
		{
			this.screen = s;
			if (TriggerNow)
			{
				e.TriggerOutcome(playerEmpire, outcome);
			}
			this.outcome = outcome;
			this.ExpEvent = e;
			this.fade = true;
			base.IsPopup = true;
			this.FromGame = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
			this.r = new Rectangle(0, 0, 600, 600);
		}

		public override void Draw(GameTime gameTime)
		{
			if (this.fade)
			{
				base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			}
			base.DrawBase(gameTime);
			base.ScreenManager.SpriteBatch.Begin();
			Vector2 TheirTextPos = new Vector2((float)(this.BlackRect.X + 10), (float)(this.BlackRect.Y + 10));
			string Description = HelperFunctions.parseText(Fonts.Verdana12Bold, this.outcome.DescriptionText, (float)(this.BlackRect.Width - 40));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12Bold, Description, TheirTextPos, Color.White);
			TheirTextPos.Y = TheirTextPos.Y + (float)((int)Fonts.Verdana12Bold.MeasureString(Description).Y + 10);
			if (this.outcome.SelectRandomPlanet && this.outcome.GetPlanet() != null)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Relevant Planet: ", this.outcome.GetPlanet().Name), TheirTextPos, Color.LightGreen);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			if (this.outcome.GetArtifact() != null)
			{
				string theirText = string.Concat("Artifact Granted: ", this.outcome.GetArtifact().Name);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.LightGreen);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				Rectangle Icon = new Rectangle((int)TheirTextPos.X, (int)TheirTextPos.Y, 32, 32);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Artifact Icons/", this.outcome.GetArtifact().Name)], Icon, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + 36f;
				theirText = HelperFunctions.parseText(Fonts.Arial12, this.outcome.GetArtifact().Description, (float)(this.BlackRect.Width - 40));
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
					string theirText = string.Concat("Technology Acquired: ", ResourceManager.TechTree[this.outcome.UnlockTech].Name);
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
			base.ScreenManager.SpriteBatch.End();
		}

		public override void HandleInput(InputState input)
		{
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.TitleText = this.ExpEvent.Name;
			this.MiddleText = this.outcome.TitleText;
			base.LoadContent();
			Rectangle FitRect = new Rectangle(this.TitleRect.X - 4, this.TitleRect.Y + this.TitleRect.Height + this.MidContainer.Height + 10, this.TitleRect.Width, 600 - (this.TitleRect.Height + this.MidContainer.Height));
			this.BlackRect = new Rectangle(FitRect.X, FitRect.Y, FitRect.Width, 450);
		}
	}
}
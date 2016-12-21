using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class EventPopup : PopupWindow
	{
		public bool Fade;

		public bool FromGame;

		private UniverseScreen _screen;

		public ExplorationEvent ExpEvent;

		private readonly Outcome _outcome;

		private Rectangle _blackRect;

        public Dictionary<Packagetypes, List<DrawPackage>> DrawPackages = new Dictionary<Packagetypes, List<DrawPackage>>();
        public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, Outcome outcome)
		{
			_screen = s;
			_outcome = outcome;
			ExpEvent = e;
			Fade = true;
			IsPopup = true;
			FromGame = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0);
			R = new Rectangle(0, 0, 600, 600);
            foreach (Packagetypes packagetype in Enum.GetValues(typeof(Packagetypes)))
            {
                DrawPackages.Add(packagetype, new List<DrawPackage>());
            }
        }

		public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, Outcome outcome, bool TriggerNow)
		{
			_screen = s;
			if (TriggerNow)
			{
				e.TriggerOutcome(playerEmpire, outcome);
			}
			_outcome = outcome;
			ExpEvent = e;
			Fade = true;
			IsPopup = true;
			FromGame = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0);
			R = new Rectangle(0, 0, 600, 600);
		    foreach (Packagetypes packagetype in Enum.GetValues(typeof(Packagetypes)))
		    {
		        DrawPackages.Add(packagetype,new List<DrawPackage>());
		    }
		}

		public override void Draw(GameTime gameTime)
		{
			if (Fade)
			{
				ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			}
			DrawBase(gameTime);
			ScreenManager.SpriteBatch.Begin();
			Vector2 TheirTextPos = new Vector2((float)(_blackRect.X + 10), (float)(_blackRect.Y + 10));
			string Description = HelperFunctions.ParseText(Fonts.Verdana10, _outcome.DescriptionText, (float)(_blackRect.Width - 40));
			ScreenManager.SpriteBatch.DrawString(Fonts.Verdana10, Description, TheirTextPos, Color.White);
			TheirTextPos.Y = TheirTextPos.Y + (float)((int)Fonts.Verdana10.MeasureString(Description).Y + 10);
			if (_outcome.SelectRandomPlanet && _outcome.GetPlanet() != null)
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Relevant Planet: ", _outcome.GetPlanet().Name), TheirTextPos, Color.LightGreen);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			if (_outcome.GetArtifact() != null)
			{
				string theirText = string.Concat("Artifact Granted: ", _outcome.GetArtifact().Name);
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.LightGreen);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				Rectangle Icon = new Rectangle((int)TheirTextPos.X, (int)TheirTextPos.Y, 32, 32);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Artifact Icons/", _outcome.GetArtifact().Name)], Icon, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + 36f;
				theirText = HelperFunctions.ParseText(Fonts.Arial12, _outcome.GetArtifact().Description, (float)(_blackRect.Width - 40));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + Fonts.Arial12.MeasureString(theirText).Y;
			    foreach (DrawPackage artifactDrawPackage in DrawPackages[Packagetypes.Artifact])
			    {
                    TheirTextPos.Y +=artifactDrawPackage.Font.LineSpacing;			        			        
                    ScreenManager.SpriteBatch.DrawString(artifactDrawPackage.Font, artifactDrawPackage.Text, TheirTextPos, artifactDrawPackage.Color);
                }               
            }
			if (_outcome.UnlockTech != null)
			{
				if (!_outcome.WeHadIt)
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					string theirText = string.Concat("Technology Acquired: ", Localizer.Token(ResourceManager.TechTree[_outcome.UnlockTech].NameIndex));
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
					TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
					if (ResourceManager.TechTree[_outcome.UnlockTech].ModulesUnlocked.Count > 0)
					{
						ShipModule unlockedMod = ResourceManager.ShipModulesDict[ResourceManager.TechTree[_outcome.UnlockTech].ModulesUnlocked[0].ModuleUID];
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
						ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[unlockedMod.UID].IconTexturePath], IconRect, Color.White);
						string moduleName = Localizer.Token(unlockedMod.NameIndex);
						ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, moduleName, new Vector2(TheirTextPos.X + 100f, TheirTextPos.Y), Color.Orange);
						string desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(unlockedMod.DescriptionIndex), (float)(_blackRect.Width - 120));
						ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, new Vector2(TheirTextPos.X + 100f, TheirTextPos.Y + 22f), Color.White);
					}
				}
				else
				{
					TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					string theirText = "We found some alien technology, but we already possessed this knowledge.";
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
					TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
			}
			if (_outcome.MoneyGranted > 0)
			{
				TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
				string theirText = string.Concat("Money Granted: ", _outcome.MoneyGranted.ToString());
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			if (_outcome.ScienceBonus > 0f)
			{
				TheirTextPos.Y = TheirTextPos.Y + (float)Fonts.Arial12Bold.LineSpacing;
				int scienceBonus = (int)(_outcome.ScienceBonus * 100f);
				string theirText = string.Concat("Research Bonus Granted: ", scienceBonus.ToString(), "%");
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, theirText, TheirTextPos, Color.White);
				TheirTextPos.Y = TheirTextPos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			ScreenManager.SpriteBatch.End();
		}

		public override void HandleInput(InputState input)
		{
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			TitleText = ExpEvent.Name;
			MiddleText = _outcome.TitleText;
			base.LoadContent();
			Rectangle FitRect = new Rectangle(TitleRect.X - 4, TitleRect.Y + TitleRect.Height + MidContainer.Height + 10, TitleRect.Width, 600 - (TitleRect.Height + MidContainer.Height));
			_blackRect = new Rectangle(FitRect.X, FitRect.Y, FitRect.Width, 450);
		}

	    public enum Packagetypes
	    {
	        Artifact,
            Technology,
            Planet

	    }

	    public class DrawPackage
	    {
	        public string Text;
	        public SpriteFont Font;
	        public int Value = 0;
	        public Texture2D Icon;
	        public Color Color;

	        public DrawPackage()
	        {
	        }

	        public DrawPackage(string text, SpriteFont font, int value,
	            Color color)
	        {
	            Text = text;
	            Font = font;
	            Value = value;
	            Color = color;
	        }

	        public DrawPackage(string text, SpriteFont font, float value,
	            Color color, string postFix)
	        {
	            Text = text + Value + postFix;
	            Font = font;
	            Value = (int) (value * 100f);
	            Color = color;
	        }
	    }
	}
}
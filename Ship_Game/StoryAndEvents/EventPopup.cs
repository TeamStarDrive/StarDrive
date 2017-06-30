using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
	public sealed class EventPopup : PopupWindow
	{
		public bool Fade;

		public bool FromGame;

		public ExplorationEvent ExpEvent;

		private readonly Outcome _outcome;

		private Rectangle _blackRect;

        public Map<Packagetypes, Array<DrawPackage>> DrawPackages = new Map<Packagetypes, Array<DrawPackage>>();

		public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, Outcome outcome, bool triggerNow) : base(s, 600, 600)
		{
			if (triggerNow)
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
		    foreach (Packagetypes packagetype in Enum.GetValues(typeof(Packagetypes)))
		    {
		        DrawPackages.Add(packagetype,new Array<DrawPackage>());
		    }
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			if (Fade)
			{
				ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			}
			base.Draw(spriteBatch);

			ScreenManager.SpriteBatch.Begin();
			Vector2 theirTextPos = new Vector2((float)(_blackRect.X + 10), (float)(_blackRect.Y + 10));
			string description = HelperFunctions.ParseText(Fonts.Verdana10, _outcome.DescriptionText, (float)(_blackRect.Width - 40));			
		    theirTextPos = DrawString(Fonts.Verdana10, description, theirTextPos, Color.White);

			if (_outcome.SelectRandomPlanet && _outcome.GetPlanet() != null)
			{
                theirTextPos = DrawString(Fonts.Arial12Bold, string.Concat("Relevant Planet: ", _outcome.GetPlanet().Name), theirTextPos, Color.LightGreen);				
			}
			if (_outcome.GetArtifact() != null)
			{
				string theirText = string.Concat("Artifact Granted: ", _outcome.GetArtifact().Name);
                theirTextPos = DrawString(Fonts.Arial12Bold, theirText, theirTextPos, Color.LightGreen);				
				Rectangle icon = new Rectangle((int)theirTextPos.X, (int)theirTextPos.Y, 32, 32);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Artifact Icons/", _outcome.GetArtifact().Name)], icon, Color.White);
				theirTextPos.Y = theirTextPos.Y + 36f;
				theirText = HelperFunctions.ParseText(Fonts.Arial12, _outcome.GetArtifact().Description, (float)(_blackRect.Width - 40));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, theirText, theirTextPos, Color.White);
				theirTextPos.Y = theirTextPos.Y + Fonts.Arial12.MeasureString(theirText).Y;
			    foreach (DrawPackage artifactDrawPackage in DrawPackages[Packagetypes.Artifact])
			    {
                    theirTextPos.Y +=artifactDrawPackage.Font.LineSpacing;			        			        
                    ScreenManager.SpriteBatch.DrawString(artifactDrawPackage.Font, artifactDrawPackage.Text, theirTextPos, artifactDrawPackage.Color);
                }               
            }
			if (_outcome.UnlockTech != null)
			{
			    if (!_outcome.WeHadIt)
			    {                                        
			        string theirText = string.Concat("Technology Acquired: ",
			            Localizer.Token(ResourceManager.TechTree[_outcome.UnlockTech].NameIndex));
                    theirTextPos = DrawString(Fonts.Arial12Bold, theirText, theirTextPos, Color.White);
			        if (ResourceManager.TechTree[_outcome.UnlockTech].ModulesUnlocked.Count > 0)
			        {
			            ShipModule unlockedMod = ResourceManager.GetModuleTemplate(ResourceManager.TechTree[_outcome.UnlockTech].ModulesUnlocked[0].ModuleUID);
			            Rectangle IconRect = new Rectangle((int) theirTextPos.X, (int) theirTextPos.Y, 16 * unlockedMod.XSIZE,
			                16 * unlockedMod.YSIZE);

			            IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
			            IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;

			            while (IconRect.Height > 96)
			            {
			                IconRect.Height = IconRect.Height - unlockedMod.YSIZE;
			                IconRect.Width = IconRect.Width - unlockedMod.XSIZE;
			                IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
			                IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;
			            }
			            ScreenManager.SpriteBatch.Draw(
			                ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(unlockedMod.UID).IconTexturePath],
			                IconRect, Color.White);
			            string moduleName = Localizer.Token(unlockedMod.NameIndex);
			            DrawString(Fonts.Arial20Bold, moduleName,
			                new Vector2(theirTextPos.X + 100f, theirTextPos.Y), Color.Orange);
			            string desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(unlockedMod.DescriptionIndex),
			                (float) (_blackRect.Width - 120));
			            DrawString(Fonts.Arial12Bold, desc, new Vector2(theirTextPos.X + 100f, theirTextPos.Y + 22f), Color.White);
			        }
			    }
			    else
			    {			        
			        string theirText = "We found some alien technology, but we already possessed this knowledge.";
                    theirTextPos = DrawString(Fonts.Arial12Bold, theirText, theirTextPos, Color.White);			        
			    }
			}
			if (_outcome.MoneyGranted > 0)
			{				
				string theirText = string.Concat("Money Granted: ", _outcome.MoneyGranted.ToString());
                theirTextPos = DrawString(Fonts.Arial12Bold, theirText, theirTextPos, Color.White);
			}
			if (_outcome.ScienceBonus > 0f)
			{				
				int scienceBonus = (int)(_outcome.ScienceBonus * 100f);
				string theirText = string.Concat("Research Bonus Granted: ", scienceBonus.ToString(), "%");
                theirTextPos = DrawString(Fonts.Arial12Bold, theirText, theirTextPos, Color.White);			
			}
			ScreenManager.SpriteBatch.End();
		}



		public override void LoadContent()
		{
			TitleText = ExpEvent.Name;
			MiddleText = _outcome.TitleText;
			base.LoadContent();
			Rectangle fitRect = new Rectangle(TitleRect.X - 4, TitleRect.Y + TitleRect.Height + MidContainer.Height + 10, TitleRect.Width, 600 - (TitleRect.Height + MidContainer.Height));
			_blackRect = new Rectangle(fitRect.X, fitRect.Y, fitRect.Width, 450);
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
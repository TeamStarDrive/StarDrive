using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class VariableUIElement : UIElement
	{
		private List<VariableUIElement.TippedItem> ToolTipItems = new List<VariableUIElement.TippedItem>();

		//private Rectangle SliderRect;

		private UniverseScreen screen;

		public Rectangle LeftRect;

		public Rectangle RightRect;

		public Rectangle Housing;

		public Rectangle Power;

		public Rectangle Shields;

		public Rectangle Ordnance;

		//private ProgressBar pBar;

		//private ProgressBar sBar;

		//private ProgressBar oBar;

		//private SlidingElement sliding_element;

		//private string fmt = "0";

		public VariableUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = sm;
			this.ElementRect = r;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.Housing = r;
			this.LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
			this.RightRect = new Rectangle(this.LeftRect.X + this.LeftRect.Width, this.LeftRect.Y, 220, this.LeftRect.Height);
		}

		public override void Draw(GameTime gameTime)
		{
		}

		public void Draw(GameTime gameTime, string TitleText, string BodyText)
		{
			MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/unitselmenu_main"], this.Housing, Color.White);
			Vector2 NamePos = new Vector2((float)(this.Housing.X + 41), (float)(this.Housing.Y + 65));
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, TitleText, NamePos, this.tColor);
			Vector2 BodyPos = new Vector2(NamePos.X, (float)(this.Housing.Y + 115));
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, BodyText, BodyPos, this.tColor);
		}

		private struct TippedItem
		{   //there seems to be no reason for this struct to exist at all
			//public Rectangle r;

			//public int TIP_ID;
		}
	}
}
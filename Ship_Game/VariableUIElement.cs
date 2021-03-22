using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class VariableUIElement : UIElement
	{
		private UniverseScreen screen;

		public Rectangle LeftRect;

		public Rectangle RightRect;

		public Rectangle Housing;

		public Rectangle Power;

		public Rectangle Shields;

		public Rectangle Ordnance;

		public VariableUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
		{
			this.screen = screen;
			ScreenManager = sm;
			ElementRect = r;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
			Housing = r;
			LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
			RightRect = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
		}

		public override void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
		}

		public void Draw(string TitleText, string BodyText)
		{
			MathHelper.SmoothStep(0f, 1f, TransitionPosition);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
			Vector2 NamePos = new Vector2(Housing.X + 41, Housing.Y + 65);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, TitleText, NamePos, tColor);
			Vector2 BodyPos = new Vector2(NamePos.X, Housing.Y + 115);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, BodyText, BodyPos, tColor);
		}
	}
}
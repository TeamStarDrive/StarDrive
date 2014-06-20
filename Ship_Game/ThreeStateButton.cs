using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ThreeStateButton
	{
		public string Good;

		private ThreeStateButton.State state;

		private Rectangle rect;

		private Vector2 statePos;

		private Vector2 TextPos;

		public ThreeStateButton(Planet.GoodState gstate, string good, Vector2 position)
		{
			this.Good = good;
			if (gstate == Planet.GoodState.IMPORT)
			{
				this.state = ThreeStateButton.State.In;
			}
			if (gstate == Planet.GoodState.EXPORT)
			{
				this.state = ThreeStateButton.State.Out;
			}
			if (gstate == Planet.GoodState.STORE)
			{
				this.state = ThreeStateButton.State.Store;
			}
			this.rect = new Rectangle((int)position.X, (int)position.Y, 32, 32);
			this.TextPos = new Vector2((float)(this.rect.X + 36), (float)(this.rect.Y + 16 - Fonts.Arial12Bold.LineSpacing / 2));
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, int amount)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Goods/", this.Good)], this.rect, Color.White);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, amount.ToString(), this.TextPos, Color.White);
			string statetext = "";
			if (this.state == ThreeStateButton.State.In)
			{
				statetext = "IN";
			}
			if (this.state == ThreeStateButton.State.Out)
			{
				statetext = "OUT";
			}
			if (this.state == ThreeStateButton.State.Store)
			{
				statetext = "-";
			}
			this.statePos = new Vector2((float)(this.rect.X + 16) - Fonts.Arial12Bold.MeasureString(statetext).X / 2f, (float)(this.rect.Y + 30));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, statetext, this.statePos, Color.White);
		}

		public void HandleInput(InputState input, ScreenManager screenManager)
		{
			if (HelperFunctions.CheckIntersection(this.rect, input.CursorPosition))
			{
				ToolTip.CreateTooltip(string.Concat(ResourceManager.GoodsDict[this.Good].Name, " storage. \n\n Click to change Import/Export settings"), screenManager);
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					ThreeStateButton threeStateButton = this;
					threeStateButton.state = (ThreeStateButton.State)((int)threeStateButton.state + (int)ThreeStateButton.State.Out);
					if (this.state > ThreeStateButton.State.Store)
					{
						this.state = ThreeStateButton.State.In;
					}
				}
			}
		}

		private enum State
		{
			In,
			Out,
			Store
		}
	}
}
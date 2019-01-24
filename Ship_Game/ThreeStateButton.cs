using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class ThreeStateButton
	{
		public string Good;

		private State state;

		private Rectangle rect;

		private Vector2 statePos;

		private Vector2 TextPos;

		public ThreeStateButton(Planet.GoodState gstate, string good, Vector2 position)
		{
			Good = good;
			if (gstate == Planet.GoodState.IMPORT)
			{
				state = State.In;
			}
			if (gstate == Planet.GoodState.EXPORT)
			{
				state = State.Out;
			}
			if (gstate == Planet.GoodState.STORE)
			{
				state = State.Store;
			}
			rect = new Rectangle((int)position.X, (int)position.Y, 32, 32);
			TextPos = new Vector2(rect.X + 36, rect.Y + 16 - Fonts.Arial12Bold.LineSpacing / 2);
		}

		public void Draw(ScreenManager ScreenManager, int amount)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Goods/", Good)), rect, Color.White);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, amount.ToString(), TextPos, Color.White);
			string statetext = "";
			if (state == State.In)
			{
				statetext = "IN";
			}
			if (state == State.Out)
			{
				statetext = "OUT";
			}
			if (state == State.Store)
			{
				statetext = "-";
			}
			statePos = new Vector2(rect.X + 16 - Fonts.Arial12Bold.MeasureString(statetext).X / 2f, rect.Y + 30);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, statetext, statePos, Color.White);
		}

		public void HandleInput(InputState input, ScreenManager screenManager)
		{
			if (rect.HitTest(input.CursorPosition))
			{
				ToolTip.CreateTooltip($"{ResourceManager.GoodsDict[Good].Name} storage. \n\n Click to change Import/Export settings");
				if (input.InGameSelect)
				{
					GameAudio.AcceptClick();
					ThreeStateButton threeStateButton = this;
					threeStateButton.state = (State)((int)threeStateButton.state + (int)State.Out);
					if (state > State.Store)
					{
						state = State.In;
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
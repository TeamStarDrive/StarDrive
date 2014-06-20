using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;
using System;

namespace Ship_Game.Gameplay
{
	public class ItemToOffer
	{
		public object Target;

		public int number;

		public bool Selected;

		public string words;

		private Rectangle ClickRect;

		public string SpecialInquiry = "";

		public string Response;

		public bool Hover;

		public ItemToOffer()
		{
		}

		public ItemToOffer(string w, Vector2 Cursor, SpriteFont Font)
		{
			this.words = w;
			int width = (int)Font.MeasureString(w).X;
			this.ClickRect = new Rectangle((int)Cursor.X, (int)Cursor.Y, width, Font.LineSpacing);
		}

		public void Draw(SpriteBatch spriteBatch, SpriteFont Font)
		{
			Color orange;
			SpriteBatch spriteBatch1 = spriteBatch;
			SpriteFont font = Font;
			string str = this.words;
			Vector2 vector2 = new Vector2((float)this.ClickRect.X, (float)this.ClickRect.Y);
			if (this.Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (this.Hover ? Color.White : new Color(255, 255, 255, 220));
			}
			spriteBatch1.DrawString(font, str, vector2, orange);
		}

		public string HandleInput(InputState input, ScrollList.Entry e)
		{
			if (!HelperFunctions.CheckIntersection(this.ClickRect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				string response = (e.item as ItemToOffer).Response;
				string str = response;
				if (response != null)
				{
					if (str == "NAPact")
					{
						ToolTip.CreateTooltip(129, Ship.universeScreen.ScreenManager);
					}
					else if (str == "OpenBorders")
					{
						ToolTip.CreateTooltip(130, Ship.universeScreen.ScreenManager);
					}
					else if (str == "Peace Treaty")
					{
						ToolTip.CreateTooltip(131, Ship.universeScreen.ScreenManager);
					}
					else if (str == "TradeTreaty")
					{
						ToolTip.CreateTooltip(132, Ship.universeScreen.ScreenManager);
					}
					else if (str == "OfferAlliance")
					{
						ToolTip.CreateTooltip(133, Ship.universeScreen.ScreenManager);
					}
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					this.Selected = !this.Selected;
					e.ShowingSub = !e.ShowingSub;
					return this.Response;
				}
			}
			return null;
		}

		public void Update(Vector2 Cursor)
		{
			this.ClickRect.Y = (int)Cursor.Y;
		}
	}
}
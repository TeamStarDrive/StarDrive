using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;
using System;

namespace Ship_Game.Gameplay
{
	public sealed class ItemToOffer
	{
	    public string Words;
		public string SpecialInquiry = "";
		public string Response;

	    public bool Selected;
        private bool Hover;
	    private Rectangle ClickRect;

        public override string ToString()
        {
            return $"Response:\"{Response}\"  Words:\"{Words}\"  Inquiry:\"{SpecialInquiry}\"";
        }

        public ItemToOffer(string words, Vector2 cursor)
		{
		    SpriteFont font = Fonts.Arial12Bold;
            Words = words;
			int width = (int)font.MeasureString(words).X;
			ClickRect = new Rectangle((int)cursor.X, (int)cursor.Y, width, font.LineSpacing);
		}

	    public ItemToOffer(string words, string response, Vector2 cursor)
            : this(words, cursor)
	    {
            Response = response;
	    }

	    public ItemToOffer(int localization, string response, Vector2 cursor)
            : this(Localizer.Token(localization), response, cursor)
	    {
	    }

        public void ChangeSpecialInquiry(Array<string> items)
        {
            if (Selected)
                items.Add(SpecialInquiry);
            else
                items.Remove(SpecialInquiry);
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
		{
			Color orange;
			SpriteBatch spriteBatch1 = spriteBatch;
			string str = Words;
			Vector2 vector2 = new Vector2(ClickRect.X, ClickRect.Y);
			if (Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (Hover ? Color.White : new Color(255, 255, 255, 220));
			}
			spriteBatch1.DrawString(font, str, vector2, orange);
		}

		public string HandleInput(InputState input, ScrollList.Entry e)
		{
			if (!ClickRect.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				Hover = true;
				string response = ((ItemToOffer) e.item).Response;
				string str = response;
				if (response != null)
				{
					if (str == "NAPact")
					{
						ToolTip.CreateTooltip(129);
					}
					else if (str == "OpenBorders")
					{
						ToolTip.CreateTooltip(130);
					}
					else if (str == "Peace Treaty")
					{
						ToolTip.CreateTooltip(131);
					}
					else if (str == "TradeTreaty")
					{
						ToolTip.CreateTooltip(132);
					}
					else if (str == "OfferAlliance")
					{
						ToolTip.CreateTooltip(133);
					}
				}
				if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
				{
					Selected = !Selected;
                    e.Expand(!e.Expanded);
					return Response;
				}
			}
			return null;
		}

		public void Update(Vector2 cursor)
		{
			ClickRect.Y = (int)cursor.Y;
		}
	}
}
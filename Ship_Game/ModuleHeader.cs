using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ModuleHeader
	{
		public string Text;
	    public bool Hover { get; private set; }
	    private bool Open;
		private readonly int Width = 305;
		private Rectangle ClickRect;
		private Rectangle R;

		public ModuleHeader(string text)
		{
			Text = text;
		}
		public ModuleHeader(string text, int width)
		{
			Width = width;
			Text = text;
		}

		public void Draw(ScreenManager screenManager, Vector2 position)
		{
		    DrawWidth(screenManager, position, Width);
		}

		public void DrawWidth(ScreenManager screenManager, Vector2 position, int width)
		{
		    SpriteBatch spriteBatch = screenManager.SpriteBatch;
			R = new Rectangle((int)position.X, (int)position.Y, width, 30);

			new Selector(R, (Hover ? new Color(95, 82, 47) : new Color(32, 30, 18))).Draw(spriteBatch);

		    var textPos = new Vector2(R.X + 10, R.Y + R.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
		    spriteBatch.DrawString(Fonts.Pirulen12, Text, textPos, Color.White);
			ClickRect = new Rectangle(R.X + R.Width - 15, R.Y + 10, 10, 10);

		    string open = Open ? "-" : "+";
			textPos = new Vector2(ClickRect.X - Fonts.Arial20Bold.MeasureString(open).X / 2f,
			                      ClickRect.Y + 6 - Fonts.Arial20Bold.LineSpacing / 2);
		    spriteBatch.DrawString(Fonts.Arial20Bold, open, textPos, Color.White);
		}

        public void Expand(bool expanded, ScrollList.Entry e)
        {
            Open = expanded;
            e.Expand(expanded);
        }

		public bool HandleInput(InputState input, ScrollList.Entry e)
		{
			if (e.CheckHover(input))
			{
			    Hover = true;
			    if (!input.LeftMouseClick)
			        return false;
			    
			    GameAudio.AcceptClick();
			    Expand(!Open, e);
			    return true;
			}
			Hover = false;
			return false;
		}
	}
}
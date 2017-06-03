using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    using BoolExpression = Expression<Func<bool>>;

	public sealed class Checkbox
	{
		private readonly SpriteFont Font;
        private readonly string Text;
        private readonly string TipText;
        private readonly Ref<bool> Binding;

        private readonly Rectangle CheckRect;
		private readonly Vector2 TextPos;
		private readonly Vector2 CheckPos;

		public Checkbox(float x, float y, Ref<bool> binding, SpriteFont font, string title, string tooltip)
		{
		    Binding = binding;
			Font    = font;
            Text    = title;
            TipText = tooltip;
		    var bounds = new Rectangle((int)x, (int)y, (int)font.MeasureString(title).X + 32, font.LineSpacing + 6);
			CheckRect  = new Rectangle(bounds.X + 15, bounds.Y + bounds.Height / 2 - 5, 10, 10);
			TextPos    = new Vector2(CheckRect.X + 15, CheckRect.Y + CheckRect.Height / 2 - font.LineSpacing / 2);
			CheckPos   = new Vector2(CheckRect.X + 5 - font.MeasureString("x").X / 2f, 
                                     CheckRect.Y + 4 - font.LineSpacing / 2);
		}
        public Checkbox(float x, float y, BoolExpression binding, SpriteFont font, string title, string tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, tooltip)
        {
        }
        public Checkbox(float x, float y, BoolExpression binding, SpriteFont font, string title, int tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, Localizer.Token(tooltip))
        {
        }
        public Checkbox(float x, float y, BoolExpression binding, SpriteFont font, int title, int tooltip)
            : this(x, y, new Ref<bool>(binding), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }
        public Checkbox(float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, string title, int tooltip)
            : this(x, y, new Ref<bool>(getter, setter), font, title, Localizer.Token(tooltip))
        {
        }
        public Checkbox(float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, int title, int tooltip)
            : this(x, y, new Ref<bool>(getter, setter), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }

        public void Draw(ScreenManager screenManager)
		{
			if (CheckRect.HitTest(Mouse.GetState().Pos()) && !TipText.Empty())
			{
				ToolTip.CreateTooltip(TipText, screenManager);
			}
			screenManager.SpriteBatch.DrawRectangle(CheckRect, new Color(96, 81, 49));
			screenManager.SpriteBatch.DrawString(Font, Text, TextPos, Color.White);
			if (Binding.Value)
			{
				screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "x", CheckPos, Color.White);
			}
		}

		public bool HandleInput(InputState input)
		{
			if (CheckRect.HitTest(input.CursorPosition) && 
                input.MouseCurr.LeftButton == ButtonState.Pressed && 
                input.MousePrev.LeftButton == ButtonState.Released)
			{
				Binding.Value = !Binding.Value;
			}
			return false;
		}
	}
}
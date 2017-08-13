using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game.Gameplay
{
    public sealed class DialogOption
    {
        public object Target;

        // ReSharper disable once InconsistentNaming -- XML Compatibility
        public int number;

        // ReSharper disable once InconsistentNaming -- XML Compatibility
        public string words;

        private Rectangle _clickRect;

        public string SpecialInquiry = string.Empty;

        public string Response;

        public bool Hover;

        public DialogOption()
        {
        }

        public DialogOption(int n, string w, Vector2 cursor, SpriteFont font)
        {
            number = n;
            words = w;
            int width = (int)font.MeasureString(w).X;
            _clickRect = new Rectangle((int)cursor.X, (int)cursor.Y, width, font.LineSpacing);
        }

        public void Draw(ScreenManager screenManager, SpriteFont font)
        {
            HelperFunctions.DrawDropShadowText(screenManager, string.Concat(number.ToString(), ". ", words),
                new Vector2(_clickRect.X, _clickRect.Y), font, (Hover ? Color.White : new Color(255, 255, 255, 220)));
        }

        public string HandleInput(InputState input)
        {
            if (!_clickRect.HitTest(input.CursorPosition))
            {
                Hover = false;
                return null;
            }

            Hover = true;
            if (input.MouseCurr.LeftButton == ButtonState.Pressed &&
                input.MousePrev.LeftButton == ButtonState.Released)	        
                return Response;

            return null;
        }

        public void Update(Vector2 cursor)
        {
            _clickRect.Y = (int)cursor.Y;
        }
    }
}
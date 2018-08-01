using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game.Gameplay
{
    public sealed class DialogOption
    {
        public object Target;
        [XmlElement(ElementName = "number")]
        public int Number;
        [XmlElement(ElementName = "words")]
        public string Words;
        private Rectangle ClickRect;
        public string SpecialInquiry = string.Empty;
        public string Response;
        public bool Hover;

        public override string ToString() => $"Words: {Words} Response: {Response}";

        public DialogOption()
        {
        }

        public DialogOption(int n, string w, Vector2 cursor, SpriteFont font)
        {
            Number = n;
            Words = w;
            int width = (int)font.MeasureString(w).X;
            ClickRect = new Rectangle((int)cursor.X, (int)cursor.Y, width, font.LineSpacing);
        }

        public void Draw(ScreenManager screenManager, SpriteFont font)
        {
            HelperFunctions.DrawDropShadowText(screenManager, string.Concat(Number.ToString(), ". ", Words),
                new Vector2(ClickRect.X, ClickRect.Y), font, (Hover ? Color.White : new Color(255, 255, 255, 220)));
        }

        public string HandleInput(InputState input)
        {
            if (!ClickRect.HitTest(input.CursorPosition))
            {
                Hover = false;
                return null;
            }

            Hover = true;
            if (input.LeftMouseClick)	        
                return Response;

            return null;
        }

        public void Update(Vector2 cursor)
        {
            ClickRect.Y = (int)cursor.Y;
        }
    }
}
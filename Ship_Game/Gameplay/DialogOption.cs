using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    // @note These are automatically serialized from /DiplomacyDialogs/{Language}/
    public sealed class DialogOption
    {
        public object Target;
        [XmlElement(ElementName = "number")]
        public int Number;
        [XmlElement(ElementName = "words")]
        public string Words;
        public string SpecialInquiry = string.Empty;
        public string Response;
        public bool Hover;

        public override string ToString() => $"Words: {Words} Response: {Response}";

        public DialogOption()
        {
        }

        public DialogOption(int number, string words)
        {
            Number = number;
            Words = words;
        }

        public void Draw(SpriteBatch batch, SpriteFont font)
        {
            batch.DrawDropShadowText(string.Concat(Number.ToString(), ". ", Words),
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
    }
}
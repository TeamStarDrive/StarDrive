using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class ModuleHeader : UIElementV2
    {
        public string Text;
        public bool Hover;
        public bool Open;

        public ModuleHeader(string text, int width = 305)
        {
            Rect = new Rectangle(0, 0, width, 30);
            Text = text;
        }
        
        public override bool HandleInput(InputState input)
        {
            return false;
        }

        public override void Draw(SpriteBatch batch)
        {
            Rectangle r = Rect;
            new Selector(r, (Hover ? new Color(95, 82, 47) : new Color(32, 30, 18))).Draw(batch);

            var textPos = new Vector2(r.X + 10, r.Y + r.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
            var clickRect = new Rectangle(r.X + r.Width - 15, r.Y + 10, 10, 10);
            batch.DrawString(Fonts.Pirulen12, Text, textPos, Color.White);

            string open = Open ? "-" : "+";
            textPos = new Vector2(clickRect.X - Fonts.Arial20Bold.MeasureString(open).X / 2f,
                                  clickRect.Y + 6 - Fonts.Arial20Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial20Bold, open, textPos, Color.White);
        }
    }
}
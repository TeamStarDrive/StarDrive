using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public class DialogOptionListItem : ScrollListItem<DialogOptionListItem>
    {
        readonly Graphics.Font Font = Fonts.Consolas18;
        int TextHeightWithPadding;
        public override int ItemHeight => TextHeightWithPadding;

        public DialogOption Option { get; }
        readonly UILabel Text;

        public DialogOptionListItem(DialogOption option, float maxWidth)
        {
            Option = option;
            Text = Add(new UILabel(Font)
            {
                DropShadow = true,
                Color = Color.White,
                Highlight = Color.LightYellow,
            });
        }

        void UpdateMultilineText(float maxWidth)
        {
            string text = $"{Option.Number}. {Option.Words}";
            string[] lines = Font.ParseTextToLines(text, maxWidth);
            TextHeightWithPadding = lines.Length * 18;
            Text.MultilineText = new(lines);
        }

        public override void PerformLayout()
        {
            Text.Rect = Rect;
            UpdateMultilineText(Rect.Width);
            base.PerformLayout();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Hovered)
                batch.FillRectangle(Rect, Color.Black.AddRgb(0.05f).Alpha(0.33f));

            base.Draw(batch, elapsed); // this will draw our Label
        }
        
    }
}

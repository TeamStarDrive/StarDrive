using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public class DialogOptionListItem : ScrollListItem<DialogOptionListItem>
    {
        readonly SpriteFont Font = Fonts.Consolas18;
        public override int ItemHeight => 24;
        public DialogOption Option { get; }
        readonly UILabel Text;

        public DialogOptionListItem(DialogOption option)
        {
            Option = option;
            Text = Add(new UILabel($"{Option.Number}. {Option.Words}", Font));
            Text.DropShadow = true;
            Text.Color = Color.White;
            Text.Highlight = Color.LightYellow;
            Text.Align = TextAlign.VerticalCenter;
        }

        public override void PerformLayout()
        {
            Text.Rect = Rect;
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

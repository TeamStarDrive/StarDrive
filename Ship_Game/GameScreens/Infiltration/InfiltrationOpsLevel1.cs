using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel1 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly UIPanel LevelPanel;
        readonly ProgressBar LevelProgress;
        readonly Font Font;

        public InfiltrationOpsLevel1(InfiltrationScreen screen, Empire player, in Rectangle rect)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font = screen.LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            /*
            Title.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(Title.Text.Text, TitleFont, Rect.Width, Rect.X), Rect.Y + 5);
            var levelRect = new Rectangle(Rect.X + 5, Rect.Y - 20, Rect.Width - 10, 30);
            LevelProgress.SetRect(levelRect);*/
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}

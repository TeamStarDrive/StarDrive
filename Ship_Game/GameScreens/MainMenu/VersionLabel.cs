using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.MainMenu
{
    public class VersionLabel : UILabel
    {
        readonly SubTexture VersionBar;

        public VersionLabel(UIElementV2 parent, int x, int y, string text)
                            : base(new Vector2(x, y), text, Fonts.Pirulen12)
        {
            VersionBar = parent.ContentManager.LoadSubTexture("Textures/MainMenu/version_bar");
            TextAlign = TextAlign.Right;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            batch.Draw(VersionBar, new Rectangle((int)X, (int)Y, 318, 12), Color);
        }
    }
}

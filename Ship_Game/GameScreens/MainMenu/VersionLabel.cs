using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.MainMenu
{
    public class VersionLabel : UILabel
    {
        SubTexture VersionBar;

        public VersionLabel(UIElementV2 parent, int x, int y, string text)
                            : base(parent, new Vector2(x, y), text, Fonts.Pirulen12)
        {
            VersionBar = parent.ContentManager.Load<SubTexture>("Textures/MainMenu/version_bar");
            AlignRight = true;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
            batch.Draw(VersionBar, new Rectangle((int)X, (int)Y, 318, 12), Color);
        }
    }
}

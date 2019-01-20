using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// A colored UI Panel that also behaves as a container for UI elements
    /// </summary>
    public class UIPanel : UIElementContainer
    {
        public SubTexture Texture;
        public Color Color;

        public UIPanel(UIElementV2 parent, Rectangle rect, Color color)
            : base(parent, rect)
        {
            Color = color;
        }

        public UIPanel(UIElementV2 parent, SubTexture texture, Rectangle rect)
            : base(parent, rect)
        {
            Texture = texture;
            Color = Color.White;
        }
        
        public UIPanel(UIElementV2 parent, SubTexture texture, Rectangle rect, Color color)
            : base(parent, rect)
        {
            Texture = texture;
            Color = color;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (Texture != null)
            {
                batch.Draw(Texture, Rect, Color);
            }
            else
            {
                batch.FillRectangle(Rect, Color);
            }
            base.Draw(batch);
        }
    }
}

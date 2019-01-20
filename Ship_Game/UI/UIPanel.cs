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
    public class UIPanel : UIElementContainer, IColorElement
    {
        public SubTexture Texture;
        public Color Color { get; set; }

        // Hint: use Color.TransparentBlack to create Panels with no fill
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

        public UIPanel(UIElementV2 parent, string tex, int x, int y)
            : base(parent, new Vector2(x,y))
        {
            Texture = parent.ContentManager.Load<SubTexture>("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (Texture != null)
            {
                batch.Draw(Texture, Rect, Color);
            }
            else if (Color.A > 0)
            {
                batch.FillRectangle(Rect, Color);
            }
            base.Draw(batch);
        }
    }
}

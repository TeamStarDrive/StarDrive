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
        public Color Color { get; set; }

        public UIPanel(UIElementV2 parent, Rectangle rect, Color color)
            : base(parent, rect)
        {
            Color = color;
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.FillRectangle(Rect, Color);
            base.Draw(batch);
        }
    }
}

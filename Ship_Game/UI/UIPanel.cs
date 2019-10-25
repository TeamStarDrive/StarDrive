using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    /// <summary>
    /// A colored UI Panel that also behaves as a container for UI elements
    /// </summary>
    public class UIPanel : UIElementContainer, IColorElement, ISpriteElement
    {
        public DrawableSprite Sprite { get; set; }
        public Color Color { get; set; } = Color.White;

        public override string ToString()
        {
            return Sprite == null
                ? $"Panel {ElementDescr} Color={Color}"
                : $"Panel {ElementDescr} Name={Sprite.Name}";
        }

        public UIPanel()
        {
        }

        // Hint: use Color.TransparentBlack to create Panels with no fill
        public UIPanel(UIElementV2 parent, in Rectangle rect, Color color, DrawableSprite sprite = null)
            : base(parent, rect)
        {
            Color = color;
            Sprite = sprite;
        }

        public UIPanel(UIElementV2 parent, Vector2 pos, Vector2 size, Color color) : base(parent, pos, size)
        {
            Color = color;
        }

        public override void Update(float deltaTime)
        {
            Sprite?.Update(deltaTime);
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (Sprite != null)
            {
                Sprite.Draw(batch, Rect, Color);
            }
            else if (Color.A > 0)
            {
                batch.FillRectangle(Rect, Color);
            }

            base.Draw(batch);
        }
    }
}

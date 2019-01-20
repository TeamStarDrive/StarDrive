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
        public bool DebugBorder;

        // Hint: use Color.TransparentBlack to create Panels with no fill
        public UIPanel(UIElementV2 parent, in Rectangle rect, Color color) : base(parent, rect)
        {
            Color = color;
        }

        public UIPanel(UIElementV2 parent, SubTexture tex, in Rectangle rect) : base(parent, rect)
        {
            Texture = tex;
            Color = Color.White;
        }
        
        public UIPanel(UIElementV2 parent, SubTexture tex, in Rectangle r, Color c) : base(parent, r)
        {
            Texture = tex;
            Color = c;
        }

        public UIPanel(UIElementV2 parent, string tex, int x, int y) : base(parent, new Vector2(x,y))
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public UIPanel(UIElementV2 parent, string tex, in Rectangle r) : base(parent, r)
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Color = Color.White;
        }

        public UIPanel(UIElementV2 parent, string tex, Vector2 pos) : base(parent, pos)
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public UIPanel(UIElementV2 parent, string tex) : base(parent, Vector2.Zero)
        {
            Texture = parent.ContentManager.LoadTextureOrDefault("Textures/"+tex);
            Size = Texture.SizeF;
            Color = Color.White;
        }

        public override string ToString()
        {
            return Texture == null
                ? $"Panel Color:{Color} Pos:{X},{Y} {Width}x{Height}"
                : $"Panel Name:{Texture.Name} Pos:{X},{Y} {Width}x{Height}";
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
            if (DebugBorder)
            {
                batch.DrawRectangle(Rect, Color.Red);
            }
            base.Draw(batch);
        }
    }
}

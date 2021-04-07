using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
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

        // If set to a color, will draw a colored border around the panel
        public Color Border = Color.TransparentBlack;

        // If set to true, any bleeding input hovering over this panel will be captured
        public bool CaptureInput;

        // If set to a valid tooltip, will display a tooltip on hover
        public LocalizedText Tooltip;

        public override string ToString()
        {
            return Sprite == null
                ? $"{TypeName} {ElementDescr} Color={Color}"
                : $"{TypeName} {ElementDescr} Name={Sprite.Name}";
        }

        public UIPanel()
        {
        }

        public UIPanel(DrawableSprite sprite)
        {
            Sprite = sprite;
        }

        public UIPanel(in Rectangle rect, DrawableSprite sprite)  : this(rect, Color.White, sprite)
        {
        }

        // Hint: use Color.TransparentBlack to create Panels with no fill
        public UIPanel(in Rectangle rect, Color color, DrawableSprite sprite = null)  : base(rect)
        {
            Color = color;
            Sprite = sprite;
        }

        public UIPanel(float x, float y, float w, float h, DrawableSprite sprite = null) : base(x, y, w, h)
        {
            Sprite = sprite;
        }

        public UIPanel(Vector2 pos, Vector2 size, Color color) : base(pos, size)
        {
            Color = color;
        }

        public UIPanel(in Rectangle rect, SubTexture texture) : this(rect, Color.White, new DrawableSprite(texture))
        {
        }

        public UIPanel(in Rectangle rect, SubTexture texture, Color color) : this(rect, color, new DrawableSprite(texture))
        {
        }

        public UIPanel(Vector2 pos, SubTexture texture) : this(pos, texture, Color.White)
        {
        }

        public UIPanel(Vector2 pos, SubTexture texture, Color color)
        {
            Sprite = new DrawableSprite(texture);
            Color = color;
            Pos = pos;
            Size = Sprite.Size;
        }


        public override bool HandleInput(InputState input)
        {
            // if child elements capture input, then we don't show tooltip
            if (base.HandleInput(input))
                return true;

            bool hovering = HitTest(input.CursorPosition);
            if (hovering && Tooltip.IsValid)
            {
                ToolTip.CreateTooltip(Tooltip, "", input.CursorPosition + new Vector2(10));
            }

            // only return true if CaptureInput was set
            return CaptureInput && hovering;
        }

        public override void Update(float fixedDeltaTime)
        {
            Sprite?.Update(fixedDeltaTime);
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Sprite != null)
            {
                Sprite.Draw(batch, Rect, Color);
            }
            else if (Color.A > 0)
            {
                batch.FillRectangle(Rect, Color);
            }

            if (Border.A > 0)
            {
                batch.DrawRectangle(Rect, Border);
            }

            base.Draw(batch, elapsed);
        }
    }
}

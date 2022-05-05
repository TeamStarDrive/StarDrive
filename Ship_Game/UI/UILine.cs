using System;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.UI
{
    public enum LineStyle
    {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// UILine which is designed to fill a panel Rect, an UIList or a ScrollViewItem
    /// </summary>
    public class UILine : UIElementV2
    {
        readonly float FillPercent;
        readonly float Thickness;
        readonly Color LineColor;
        readonly LineStyle Style;

        /// <summary>
        /// Creates a generic UILine to be used in UIList or ScrollViewItem
        /// </summary>
        /// <param name="fillPercent">Percentage of RECT to fill with the line</param>
        /// <param name="thickness">Thickness of the line</param>
        /// <param name="color">Color of the line</param>
        /// <param name="style">Horizontal or Vertical</param>
        public UILine(float fillPercent, float thickness, Color color,
                      LineStyle style = LineStyle.Horizontal) : base(Vector2.Zero, new Vector2(16f))
        {
            FillPercent = fillPercent;
            Thickness = thickness;
            LineColor = color;
            Style = style;
        }

        /// <summary>
        /// Creates a generic UILine to be used in UIList or ScrollViewItem
        /// </summary>
        /// <param name="size">Size of the UILine</param>
        /// <param name="fillPercent">Percentage of RECT to fill with the line</param>
        /// <param name="thickness">Thickness of the line</param>
        /// <param name="color">Color of the line</param>
        /// <param name="style">Horizontal or Vertical</param>
        public UILine(Vector2 size, float fillPercent, float thickness, Color color,
                      LineStyle style = LineStyle.Horizontal) : base(Vector2.Zero, size)
        {
            FillPercent = fillPercent;
            Thickness = thickness;
            LineColor = color;
            Style = style;
        }


        /// <summary>
        /// Creates a fixed-rect UILine that can be used in top-level UI context
        /// </summary>
        /// <param name="rect">RECT of this UILine to fill</param>
        /// <param name="fillPercent">Percentage of RECT to fill with the line</param>
        /// <param name="thickness">Thickness of the line</param>
        /// <param name="color">Color of the line</param>
        /// <param name="style">Horizontal or Vertical</param>
        public UILine(in Rectangle rect, float fillPercent, float thickness, Color color,
                      LineStyle style = LineStyle.Horizontal) : base(rect)
        {
            FillPercent = fillPercent;
            Thickness = thickness;
            LineColor = color;
            Style = style;
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            float offX, offY;
            if (Style == LineStyle.Horizontal)
            {
                float width = (int)Math.Floor(FillPercent * Width);
                offX = (Width - width) / 2;
                offY = (Height - Thickness) / 2;
            }
            else
            {
                float height = (int)Math.Floor(FillPercent * Height);
                offX = (Width - Thickness) / 2;
                offY = (Height - height) / 2;
            }
            batch.DrawLine(new Vector2(Pos.X + offX, Pos.Y + offY),
                           new Vector2(Right - offX, Pos.Y + offY), LineColor, Thickness);
        }
    }
}
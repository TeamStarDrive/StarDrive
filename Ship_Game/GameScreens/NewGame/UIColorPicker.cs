using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class UIColorPicker : UIElementV2
    {
        public Color CurrentColor = Color.White;
        readonly Menu1 ColorSelectMenu;

        public UIColorPicker(in Rectangle rect) : base(rect)
        {
            ColorSelectMenu = new Menu1(Rect);
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            if (input.RightMouseClick)
            {
                Visible = false;
                return true;
            }

            if (!HitTest(input.CursorPosition))
            {
                if (input.LeftMouseClick)
                {
                    Visible = false;
                    return true;
                }
                return false;
            }
            
            if (input.LeftMouseDown)
            {
                int yPosition = (int)Y + 10;
                int xPositionStart = (int)X + 10;
                for (int i = 0; i <= 255; i++)
                {
                    for (int j = 0; j <= 255; j++)
                    {
                        var thisColor = new Color((byte)i, (byte)j, CurrentColor.B);
                        var colorRect = new Rectangle(2 * j + xPositionStart - 4, yPosition - 4, 8, 8);
                        if (colorRect.HitTest(input.CursorPosition))
                        {
                            CurrentColor = thisColor;
                        }
                    }
                    yPosition += 2;
                }

                yPosition = (int)Y + 10;
                for (int i = 0; i <= 255; i++)
                {
                    var thisColor = new Color(CurrentColor.R, CurrentColor.G, Convert.ToByte(i));
                    var colorRect = new Rectangle((int)X + 10 + 575, yPosition, 20, 2);
                    if (colorRect.HitTest(input.CursorPosition))
                    {
                        CurrentColor = thisColor;
                    }
                    yPosition += 2;
                }
            }

            // always capture hovered input to avoid propagating input to behind us
            return true;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            ColorSelectMenu.Draw(batch, elapsed);

            SubTexture spark = ResourceManager.Texture("Particles/spark");

            int yPosition = (int)Y + 20;
            int xPositionStart = (int)X + 20;
            for (int i = 0; i <= 255; i++)
            {
                for (int j = 0; j <= 255; j++)
                {
                    var r = new Rectangle(2 * j + xPositionStart, yPosition, 2, 2);
                    var thisColor = new Color(Convert.ToByte(i), Convert.ToByte(j), CurrentColor.B);
                    batch.Draw(spark, r, thisColor);
                    if (thisColor.R == CurrentColor.R && thisColor.G == CurrentColor.G)
                    {
                        batch.Draw(spark, r, Color.Red);
                    }
                }
                yPosition += 2;
            }

            yPosition = (int)Y + 10;
            for (int i = 0; i <= 255; i++)
            {
                var r = new Rectangle((int)X + 10 + 575, yPosition, 20, 2);
                var thisColor = new Color(CurrentColor.R, CurrentColor.G, Convert.ToByte(i));
                batch.Draw(spark, r, thisColor);
                if (thisColor.B == CurrentColor.B)
                {
                    batch.Draw(spark, r, Color.Red);
                }
                yPosition += 2;
            }
        }
    }
}
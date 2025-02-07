﻿using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.UI
{
    public class SplitElement : UIElementV2
    {
        public UIElementV2 First;
        public UIElementV2 Second;
        // 0:    Second hugs Right|
        // else: Pos.X + Split
        public float Split = 0f;

        // Displays a tooltip 
        public LocalizedText Tooltip;

        // If TRUE, draws rectangles around First and Second element
        public bool DebugDraw;

        public override string ToString() => $"{TypeName} {ElementDescr} Split={Split} \nFirst={First} \nSecond={Second}";
        
        public SplitElement()
        {
        }
        public SplitElement(UIElementV2 first, UIElementV2 second)
        {
            First = first;
            Second = second;
            Size.X = First.Size.X + Second.Size.X + 2f;
            Size.Y = Math.Max(First.Size.Y, Second.Size.Y);
        }

        public override void PerformLayout()
        {
            base.PerformLayout();

            First.Pos = Pos;
            First.PerformLayout();
            
            if (Split > 0f) // Second is at Pos.X + Split (auto-width)
            {
                Second.Pos.X = Pos.X + Split;
                float secondRight = (Second.Pos.X + Second.Size.X);
                Size.X = (secondRight - Pos.X) + 2f;
            }
            else // Second hugs Right| (size fill)
            {
                float thisRight = Pos.X + Size.X;
                Second.Pos.X = (thisRight - Second.Size.X) + Split;
            }
            Second.Pos.Y = Pos.Y;
            Second.PerformLayout();
            
            Size.Y = Math.Max(First.Size.Y, Second.Size.Y);
        }

        public override bool HandleInput(InputState input)
        {
            if (First.HandleInput(input) || Second.HandleInput(input))
                return true;
            if (Tooltip.NotEmpty && Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(Tooltip);
            return false;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;
            First.Update(fixedDeltaTime);
            Second.Update(fixedDeltaTime);
            RequiresLayout |= First.RequiresLayout | Second.RequiresLayout;
            base.Update(fixedDeltaTime);
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            First.Draw(batch, elapsed);
            Second.Draw(batch, elapsed);

            if (DebugDraw)
            {
                batch.DrawRectangle(First.Rect, Color.IndianRed);
                batch.DrawRectangle(Second.Rect, Color.IndianRed);
            }
        }
    }
}

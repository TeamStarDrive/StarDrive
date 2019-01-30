using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.UI
{
    public class SplitElement : UIElementV2
    {
        public UIElementV2 First;
        public UIElementV2 Second;
        // 0:    Second hugs Right|
        // else: Pos.X + Split
        public float Split = 0f;

        public override string ToString() => $"Split {ElementDescr} Split={Split} \nFirst={First} \nSecond={Second}";
        
        public SplitElement()
        {
        }
        public SplitElement(UIElementV2 first, UIElementV2 second)
        {
            First = first;
            Second = second;
            Height = Math.Max(First.Height, Second.Height);
            Width  = First.Width + Second.Width + 2f;
        }

        public override void PerformLayout()
        {
            First.Pos = Pos;
            First.PerformLayout();

            if (Split > 0f) Second.Pos.X = Pos.X + Split;
            else            Second.Pos.X = (Right - Second.Width) + Split;
            Second.Pos.Y = Pos.Y;
            Second.PerformLayout();

            Height = Math.Max(First.Height, Second.Height);
        }

        public override bool HandleInput(InputState input)
        {
            return First.HandleInput(input) || Second.HandleInput(input);
        }

        public override void Update(float deltaTime)
        {
            First.Update(deltaTime);
            Second.Update(deltaTime);
            base.Update(deltaTime);
        }
        public override void Draw(SpriteBatch batch)
        {
            First.Draw(batch);
            Second.Draw(batch);
        }
    }
}

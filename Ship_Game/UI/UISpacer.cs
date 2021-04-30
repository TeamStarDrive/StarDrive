using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.UI
{
    /// <summary>
    /// An empty do-nothing element with a Pos and Size.
    /// Used to create an artificial spacer in UILists or UITextBox's
    /// </summary>
    public class UISpacer : UIElementV2
    {
        public UISpacer(in Rectangle rect) : base(rect)
        {
        }
        public UISpacer(Vector2 size) : base(Vector2.Zero, size)
        {
        }
        public UISpacer(Vector2 pos, Vector2 size) : base(pos, size)
        {
        }
        public UISpacer(float sizeX, float sizeY) : base(Vector2.Zero, new Vector2(sizeX, sizeY))
        {
        }
        public override bool HandleInput(InputState input)
        {
            return false;
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
        }

    }
}

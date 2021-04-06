using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class OrbitalAssetsUIElement : UIElementV2
    {
        private readonly Selector Sel;
        private readonly Planet P;

        public OrbitalAssetsUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen, Planet p)
        {
            P                  = p;
            //ScreenManager      = sm;
            //ElementRect        = r;
            Sel                = new Selector(r, Color.Black);
            //TransitionOnTime   = TimeSpan.FromSeconds(0.25);
            //TransitionOffTime  = TimeSpan.FromSeconds(0.25);
            Rectangle leftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            Rectangle defenseRect = new Rectangle(leftRect.X + 12, leftRect.Y + 18, 22, 22);
            defenseRect.X = defenseRect.X - 3;
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            //MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            batch.FillRectangle(Sel.Rect, Color.Black);
            var slant = new Header(new Rectangle(Sel.Rect.X, Sel.Rect.Y, Sel.Rect.Width, 41), "Orbital Assets");
            var body = new Body(new Rectangle(slant.leftRect.X, Sel.Rect.Y + 44, Sel.Rect.Width, Sel.Rect.Height - 44));
            slant.Draw(batch, elapsed);
            body.Draw(batch, elapsed);
        }
    }
}
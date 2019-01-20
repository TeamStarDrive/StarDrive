using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.MainMenu
{
    // A simple whizzing Comet used in MainMenuScreen
    public class Comet : UIElementV2
    {
        readonly GameScreen Screen;
        readonly SubTexture Texture;
        Vector2 Direction;
        float Rotation;

        public Comet(GameScreen screen, SubTexture comet)
            : base(screen, new Vector2(RandomMath.RandomBetween(-100f, screen.ScreenWidth+100f), 0f), comet.SizeF)
        {
            Screen = screen;
            Texture = comet;
            Direction = new Vector2(0f, 1f);
            DrawDepth = DrawDepth.ForeAdditive;
        }

        public void SetDirection(Vector2 direction)
        {
            Direction = (direction + RandomMath.Vector2D(0.1f)).Normalized();
            Rotation = Pos.RadiansToTarget(Pos + Direction);
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }

        public override void Update(float deltaTime)
        {
            Pos += Direction * 2400f * deltaTime;

            if (Pos.X < -200f || Pos.X > Screen.ScreenWidth+200f ||
                Pos.Y < -200  || Pos.Y > Screen.ScreenHeight+200f)
                RemoveFromParent(deferred: true);

            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            float alpha = 255f;
            if (Pos.Y > 100f)
                alpha = (25500f / Pos.Y).Clamped(0f, 255f);
                
            var c = new Color(Color.White, (byte)alpha);
            batch.Draw(Texture, Pos, c, Rotation, Texture.CenterF, 0.45f, SpriteEffects.None, 1f);

        }
    }
}

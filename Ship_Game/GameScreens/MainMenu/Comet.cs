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

        public Comet(GameScreen screen)
            : base(new Vector2(RandomMath.RandomBetween(-100f, screen.ScreenWidth+100f), 0f))
        {
            Screen = screen;
            Texture = screen.TransientContent.Load<SubTexture>("Textures/GameScreens/comet2");
            Size = Texture.SizeF;
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

        public override void Update(float fixedDeltaTime)
        {
            Pos += Direction * 2400f * fixedDeltaTime;

            if (Pos.X < -200f || Pos.X > Screen.ScreenWidth+200f ||
                Pos.Y < -200  || Pos.Y > Screen.ScreenHeight+200f)
                RemoveFromParent(deferred: true);

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            float alpha = 255f;
            if (Pos.Y > 100f)
                alpha = (25500f / Pos.Y).Clamped(0f, 255f);
                
            var c = new Color(Color.White, (byte)alpha);
            batch.Draw(Texture, Pos, c, Rotation, Texture.CenterF, 0.45f, SpriteEffects.None, 1f);

        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public struct FadeInOutAnim
    {
        private readonly SubTexture Texture;
        private readonly Rectangle Rect;
        private readonly int FadeIn;
        private readonly int Stay;
        private readonly int FadeOut;
        private readonly int End;

        public FadeInOutAnim(SubTexture texture, Rectangle rect, int fadeIn, int stay, int fadeOut, int end)
        {
            Texture = texture;
            Rect    = rect;
            FadeIn  = fadeIn;
            Stay    = stay;
            FadeOut = fadeOut;
            End     = end;
        }

        public bool InKeyRange(int animationFrame)
        {
            return FadeIn <= animationFrame && animationFrame <= End;
        }

        private static float LerpAlpha(int value, int start, int end)
        {
            return (value - start) * (255f / (end - start));
        }

        public void Draw(SpriteBatch batch, int frame)
        {
            float alpha = 220f;

            if (FadeIn <= frame && frame <= Stay)
            {
                alpha = LerpAlpha(frame, FadeIn, Stay);
            }
            else if (FadeOut <= frame && frame <= End)
            {
                alpha = 255f - LerpAlpha(frame, FadeOut, End);
            }
            batch.Draw(Texture, Rect, new Color(Color.White, (byte)alpha.Clamped(0f, 220f)));
        }
    }
}

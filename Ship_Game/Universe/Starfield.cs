using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Sprites;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class StarField : IDisposable
    {
        static readonly Color[] LayerColors =
        {
            new Color(255, 255, 255, 160), 
            new Color(255, 255, 255, 160), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 110), 
            new Color(255, 255, 255, 220), 
            new Color(255, 255, 255, 90)
        };

        static readonly float[] MoveFactors =
        {
            0.1f, 0.07f, 0.00007f, 0.0006f, 0.001f, 0.014f, 0.002f, 0.0001f
        };

        struct Star
        {
            public Vector3 Position;
            public SubTexture Tex;
            public int WhichLayer;
        }

        Vector2 LastCamPos;
        Vector2 CameraPos;

        readonly Star[] Stars;
        readonly SubTexture[] StarTex;

        SubTexture CloudTex;
        Effect CloudEffect;
        EffectParameter CloudEffectPos;
        Vector2 CloudPos;

        public StarField(GameScreen screen, UniverseState uState)
        {
            CloudTex = ResourceManager.Texture("clouds");
            CloudEffect = screen.TransientContent.Load<Effect>("Effects/Clouds");
            CloudEffectPos = CloudEffect.Parameters["Position"];
            StarTex = SunType.GetLoResTextures();
            Stars = new Star[100];

            Create(uState.Size/2, uState.BackgroundSeed);
        }

        ~StarField() { Destroy(); }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            CloudTex = null;
            CloudEffect = null;
            CloudEffectPos = null;
        }

        public void Draw(SpriteRenderer sr, SpriteBatch batch, Vector2 cameraPos, UniverseScreen screen)
        {
            LastCamPos = CameraPos;
            CameraPos = cameraPos;
            Vector2 movement = -1f * (cameraPos - LastCamPos);

            if (CloudEffect != null)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                CloudEffect.Begin();
                CloudPos -= ((movement * 0.3f) * 1f);
                CloudEffectPos.SetValue(CloudPos);
                CloudEffect.CurrentTechnique.Passes[0].Begin();

                var screenRect = new Rectangle(0, 0, 1, 1);
                batch.Draw(CloudTex, screenRect, new Color(255, 0, 0, 255));
                CloudEffect.CurrentTechnique.Passes[0].End();
                CloudEffect.End();
                batch.End();
            }

            //batch.Begin();
            //for (int i = 0; i < Stars.Length; i++)
            //{
            //    ref Star star = ref Stars[i];

            //    Color c = LayerColors[star.WhichLayer];
            //    switch (star.WhichLayer)
            //    {
            //        case 2: sr.Draw(star.Tex, star.Position, c); break;
            //        case 3: sr.Draw(star.Tex, star.Position, new Color(c.R, c.G, c.B, 255)); break;
            //        case 4: sr.Draw(star.Tex, star.Position, new Color(c.R, c.G, c.B, 255)); break;
            //        default: sr.Draw(star.Tex, new RectF((star.Position.X, star.Position.Y, 1, 1), c); break;
            //    }
            //}
            //batch.End();
        }

        public void Create(float uRadius, int seed)
        {
            var random = new SeededRandom(seed);

            int numSmallStars = 0;
            int numMedStars = 0;
            int numLargeStars = 0;
            int desiredSmallStars = random.Int(10, 30);
            int desiredMedStars = random.Int(2, 10);
            int desiredLargeStars = random.Int(1, 4);

            for (int i = 0; i < Stars.Length; i++)
            {
                ref Star star = ref Stars[i];
                star.Position = random.Vector3D(uRadius);
                int depth = i % MoveFactors.Length;

                if (2 <= depth && depth <= 4)
                {
                    if (depth == 2 && numSmallStars < desiredSmallStars)
                    {
                        ++numSmallStars;
                        star.Tex = ResourceManager.SmallStars.RandomTexture(random);
                        star.WhichLayer = 2;
                    }
                    else if (depth == 3 && numMedStars < desiredMedStars)
                    {
                        ++numMedStars;
                        star.Tex = ResourceManager.MediumStars.RandomTexture(random);
                        star.WhichLayer = 3;
                    }
                    else if (depth == 4 && numLargeStars < desiredLargeStars)
                    {
                        ++numLargeStars;
                        star.Tex = ResourceManager.LargeStars.RandomTexture(random);
                        star.WhichLayer = 4;
                    }
                    else // layers 2,3,4 are full, spill over to layer 7
                    {
                        star.Tex = random.RandItem(StarTex);
                        star.WhichLayer = 7;
                    }
                }
                else // fill layers 0,1,5,6
                {
                    star.Tex = random.RandItem(StarTex);
                    star.WhichLayer = depth;
                }
            }
        }
    }
}
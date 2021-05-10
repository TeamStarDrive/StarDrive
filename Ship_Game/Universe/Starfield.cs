using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Universe.SolarBodies;

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

        static readonly float[] MoveFactors = { 0.1f, 0.07f, 0.00007f, 0.0006f, 0.001f, 0.014f, 0.002f, 0.0001f };

        readonly int DesiredSmallStars = RandomMath.IntBetween(10,30);
        readonly int DesiredMedStars   = RandomMath.IntBetween(2, 10);
        readonly int DesiredLargeStars = RandomMath.IntBetween(1, 4);

        struct Star
        {
            public Vector2 Position;
            public SubTexture Tex;
            public int whichLayer;
        }


        Vector2 LastCamPos;
        Vector2 CameraPos;

        readonly Rectangle StarFieldR;
        readonly Star[] Stars;
        readonly SubTexture[] StarTex;

        SubTexture CloudTex;
        Effect CloudEffect;
        EffectParameter CloudEffectPos;
        Vector2 CloudPos;

        public StarField(GameScreen screen)
        {
            StarFieldR = new Rectangle(0, 0, screen.ScreenWidth, screen.ScreenHeight);
            CloudTex = ResourceManager.Texture("clouds");
            CloudEffect = screen.TransientContent.Load<Effect>("Effects/Clouds");
            CloudEffectPos = CloudEffect.Parameters["Position"];
            StarTex = SunType.GetLoResTextures();
            Stars = new Star[100];
            Reset(Vector2.Zero);
        }
        
        ~StarField() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }
        void Destroy()
        {
            CloudTex = null;
            CloudEffect = null;
            CloudEffectPos = null;
        }

        public void Draw(Vector2 cameraPos, SpriteBatch batch)
        {
            LastCamPos = CameraPos;
            CameraPos = cameraPos;
            Vector2 movement = -1f * (cameraPos - LastCamPos);
            if (movement.Length() > 20000f)
            {
                Reset(cameraPos);
                return;
            }

            if (CloudEffect != null)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                CloudEffect.Begin();
                CloudPos -= ((movement * 0.3f) * 1f);
                CloudEffectPos.SetValue(CloudPos);
                CloudEffect.CurrentTechnique.Passes[0].Begin();
                batch.Draw(CloudTex, StarFieldR, new Color(255, 0, 0, 255));
                CloudEffect.CurrentTechnique.Passes[0].End();
                CloudEffect.End();
                batch.End();
            }

            batch.Begin();
            for (int i = 0; i < Stars.Length; i++)
            {
                ref Star star = ref Stars[i];
                star.Position += (movement * MoveFactors[star.whichLayer]);
                if (star.Position.X < StarFieldR.X)
                {
                    star.Position.X = StarFieldR.Right;
                    star.Position.Y = StarFieldR.Y + RandomMath.InRange(0, StarFieldR.Height);
                }
                else if (star.Position.X > StarFieldR.Right)
                {
                    star.Position.X = StarFieldR.X;
                    star.Position.Y = StarFieldR.Y + RandomMath.InRange(0, StarFieldR.Height);
                }
                if (star.Position.Y < StarFieldR.Y)
                {
                    star.Position.X = StarFieldR.X + RandomMath.InRange(0, StarFieldR.Width);
                    star.Position.Y = StarFieldR.Bottom;
                }
                else if (star.Position.Y > StarFieldR.Bottom)
                {
                    star.Position.X = StarFieldR.X + RandomMath.InRange(0, StarFieldR.Width);
                    star.Position.Y = StarFieldR.Y;
                }
                Color c = LayerColors[star.whichLayer];
                switch (star.whichLayer)
                {
                    case 2: batch.Draw(star.Tex, star.Position, c); break;
                    case 3: batch.Draw(star.Tex, star.Position, new Color(c.R, c.G, c.B, 255)); break;
                    case 4: batch.Draw(star.Tex, star.Position, new Color(c.R, c.G, c.B, 255)); break;
                    default: batch.Draw(star.Tex, new Rectangle((int)star.Position.X, (int)star.Position.Y, 1, 1), c); break;
                }
            }
            batch.End();
        }

        public void Reset(Vector2 position)
        {
            int numSmallStars = 0;
            int numMedStars = 0;
            int numLargeStars = 0;
            int viewportWidth  = Empire.Universe.Viewport.Width;
            int viewportHeight = Empire.Universe.Viewport.Height;
            for (int i = 0; i < Stars.Length; i++)
            {
                ref Star star = ref Stars[i];
                star.Position = new Vector2(RandomMath.InRange(0, viewportWidth), 
                                            RandomMath.InRange(0, viewportHeight));
                int depth = i % MoveFactors.Length;
                if (2 <= depth && depth <= 4)
                {
                    if (depth == 2 && numSmallStars < DesiredSmallStars)
                    {
                        ++numSmallStars;
                        star.Tex = ResourceManager.SmallStars.RandomTexture();
                        star.whichLayer = 2;
                    }
                    else if (depth == 3 && numMedStars < DesiredMedStars)
                    {
                        ++numMedStars;
                        star.Tex = ResourceManager.MediumStars.RandomTexture();
                        star.whichLayer = 3;
                    }
                    else if (depth == 4 && numLargeStars < DesiredLargeStars)
                    {
                        ++numLargeStars;
                        star.Tex = ResourceManager.LargeStars.RandomTexture();
                        star.whichLayer = 4;
                    }
                    else // layers 2,3,4 are full, spill over to layer 7
                    {
                        star.Tex = RandomMath.RandItem(StarTex);
                        star.whichLayer = 7;
                    }
                }
                else // fill layers 0,1,5,6
                {
                    star.Tex = RandomMath.RandItem(StarTex);
                    star.whichLayer = depth;
                }
            }
        }
    }
}
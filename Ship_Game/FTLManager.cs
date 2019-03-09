using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
	internal static class FTLManager
	{
        static FTLConfig FTL;

        class FTLConfig
        {
            #pragma warning disable 649
            [StarData] public readonly string Texture;
            [StarData] public readonly string Animation;
            [StarData] public readonly float Duration = 0.6f;
            [StarData] public readonly float Rotation = 6.28f;
            [StarData] public readonly bool FrontToBack = true;
            [StarData] public readonly Vector2[] Curve;
            #pragma warning restore 649

            public float RotationPerFrame;
            public AnimationCurve Curves;
            public SubTexture Tex;
            public TextureAtlas Anim;
        }

        sealed class FTLInstance
        {
            // the FTL flash moves from Ship front to ship end
            public Vector3 Start;
            public Vector3 End;
            public float Time;
            public float Scale = 0.1f;
            public float Rotation;
            public Vector3 CurrentPos => Start.LerpTo(End, RelativeLife);
            public float RelativeLife => Time / FTL.Duration;
            public SpriteAnimation Animation;
        }

		static readonly Array<FTLInstance> Effects = new Array<FTLInstance>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        public static void LoadContent(GameScreen screen)
        {
            GameContentManager content = screen.TransientContent;
            
            FileInfo file = ResourceManager.GetModOrVanillaFile("FTL.yaml");
            ScreenManager.Instance.AddHotLoadTarget(screen, "FTL", file.FullName, fileInfo =>
            {
                LoadContent(screen);
            });

            using (var parser = new StarDataParser(file))
            {
                Array<FTLConfig> elements = parser.DeserializeArray<FTLConfig>();
                FTL = elements[0];

                if (FTL.Texture.NotEmpty())
                    FTL.Tex = content.LoadSubTexture("Textures/" + FTL.Texture);
                else if (FTL.Animation.NotEmpty())
                    FTL.Anim = content.LoadTextureAtlas("Textures/" + FTL.Animation);
                else
                    FTL.Tex = content.LoadSubTexture("Textures/Ships/FTL.xnb");

                FTL.RotationPerFrame = FTL.Rotation / 60f;

                FTL.Curves = new AnimationCurve();
                foreach (Vector2 p in FTL.Curve)
                    FTL.Curves.Add(p.X * FTL.Duration, p.Y);
            }
        }

        public static void AddFTL(Vector3 position, Vector3 forward, float radius)
        {
            if (!FTL.FrontToBack)
                radius = -radius; // invert the direction

            Vector3 start = position + forward*(radius);
            Vector3 end   = position - forward*(radius*3f);

            var f = new FTLInstance
            {
                Start = start,
                End   = end
            };

            if (FTL.Anim != null)
                f.Animation = new SpriteAnimation(FTL.Anim, FTL.Duration);

            using (Lock.AcquireWriteLock())
            {
                Effects.Add(f);
            }
        }

        public static void DrawFTLModels(GameScreen screen, SpriteBatch batch)
        {
            batch.Begin(SpriteBlendMode.Additive, SpriteSortMode.Immediate, SaveStateMode.None);
            using (Lock.AcquireReadLock())
            {
                foreach (FTLInstance f in Effects)
                {
                    Vector3 worldPos = f.CurrentPos;
                    Vector2 pos  = screen.ProjectTo2D(worldPos);
                    Vector2 edge = screen.ProjectTo2D(worldPos+new Vector3(125,0,0));

                    float relSizeOnScreen = (edge.X - pos.X) / screen.Width;
                    float sizeScaleOnScreen = f.Scale * relSizeOnScreen;
                    
                    if (f.Animation != null)
                    {
                        f.Animation.Draw(batch, pos, Color.White, f.Rotation, sizeScaleOnScreen);
                    }
                    else
                    {
                        batch.Draw(FTL.Tex, pos, Color.White, f.Rotation,
                            FTL.Tex.CenterF, sizeScaleOnScreen, SpriteEffects.FlipVertically, 0.9f);
                    }

                }
            }
            batch.End();
        }
        
        static UIGraphView GetGraph(GameScreen screen)
        {
            if (screen.Find("ftl_debug_graph", out UIGraphView graph))
                return graph;

            graph = screen.Add(new UIGraphView
            {
                Name = "ftl_debug_graph",
                Color = Color.TransparentBlack,
                LineColor = Color.Red,
                Pos = new Vector2(0, screen.Bottom - 500),
                Size = new Vector2(500, 250),
            });
            graph.SetRange(5f, 0, 60);
            return graph;
        }

		public static void Update(GameScreen screen, float deltaTime)
		{
            if (deltaTime <= 0f)
                return;

            UIGraphView graph = GetGraph(screen);

            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < Effects.Count; ++i)
			    {
                    FTLInstance f = Effects[i];
                    f.Time += deltaTime;
                    if (f.Time > FTL.Duration)
                    {
                        Effects.RemoveAtSwapLast(i--);
                        continue;
                    }

                    f.Animation?.Update(deltaTime);

                    f.Scale = FTL.Curves.GetY(f.Time);
                    graph.AddTimedSample(f.Scale);

			        f.Rotation += FTL.RotationPerFrame;
			    }
            }
		}
	}
}
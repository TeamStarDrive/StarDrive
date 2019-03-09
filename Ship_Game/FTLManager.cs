using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
	internal static class FTLManager
	{
        const float FTLTime = 0.6f;

        sealed class FTL
        {
            // the FTL flash moves from Ship front to ship end
            public Vector3 Front;
            public Vector3 Rear;
            public float Time;
            public float Scale = 0.1f;
            public float Rotation;
            public Vector3 CurrentPos => Front.LerpTo(Rear, RelativeLife);
            public float RelativeLife => Time / FTLTime;
            public SpriteAnimation Animation;
        }
		static readonly Array<FTL> Effects = new Array<FTL>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        static SubTexture FTLTexture;
        static AnimationCurve FTLScaleCurve;

        public static void LoadContent(GameContentManager content)
        {
            FTLTexture = content.Load<SubTexture>("Textures/Ships/FTL");
            FTLScaleCurve = new AnimationCurve(new []
            {
                (0.0f,         0.1f),
                (FTLTime*0.25f, 60f),
                (FTLTime*0.50f,  2f),
                (FTLTime*1.00f,  0f),
            });
        }

        public static void AddFTL(Vector3 position, Vector3 forward, float radius)
        {
            Vector3 front = position + forward*(radius);
            Vector3 rear  = position - forward*(radius*3f);
            var f = new FTL
            {
                Front = front,
                Rear = rear
            };

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
                foreach (FTL f in Effects)
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
                        batch.Draw(FTLTexture, pos, Color.White, f.Rotation,
                            FTLTexture.CenterF, sizeScaleOnScreen, SpriteEffects.FlipVertically, 0.9f);
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
                    FTL f = Effects[i];
                    f.Time += deltaTime;
                    if (f.Time > FTLTime)
                    {
                        Effects.RemoveAtSwapLast(i--);
                        continue;
                    }

                    f.Animation?.Update(deltaTime);

                    f.Scale = FTLScaleCurve.GetY(f.Time);
                    graph.AddTimedSample(f.Scale);

			        f.Rotation += 0.09817477f;
			    }
            }
		}
	}
}
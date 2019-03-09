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
        static FTLLayerData[] FTLLayers;
        static UIGraphView DebugGraph;

        class FTLLayerData
        {
            #pragma warning disable 649
            [StarData] public readonly string Texture;
            [StarData] public readonly float Duration = 0.6f;
            [StarData] public readonly float Rotation = 6.28f;
            [StarData] public readonly bool FrontToBack = true;
            [StarData] public readonly Vector2[] ScaleCurve;
            #pragma warning restore 649

            public AnimationCurve ScaleCurveAnim;
            public SubTexture Tex;

            public void LoadContent(GameContentManager content)
            {
                string texture = Texture.NotEmpty()
                    ? "Textures/" + Texture
                    : "Textures/Ships/FTL.xnb";
                Tex = content.LoadSubTexture(texture);

                ScaleCurveAnim = new AnimationCurve();
                foreach (Vector2 p in ScaleCurve)
                    ScaleCurveAnim.Add(p.X * Duration, p.Y);
                ScaleCurveAnim.Finish();
            }
        }

        sealed class FTLLayer
        {
            readonly FTLLayerData FTL;
            readonly Vector3 Start;
            readonly Vector3 End;
            float Time;
            float Scale;
            float Rotation;

            public FTLLayer(in Vector3 start, in Vector3 end, FTLLayerData data)
            {
                FTL = data;
                Start = start;
                End   = end;
                Scale = data.ScaleCurveAnim.GetY(Time);

                if (!data.FrontToBack) // invert the direction
                    Vectors.Swap(ref Start, ref End);
            }

            // @return TRUE if dead
            public bool Update(float deltaTime)
            {
                Time += deltaTime;
                if (Time >= FTL.Duration)
                    return true; // are we done?

                Scale = FTL.ScaleCurveAnim.GetY(Time);

                DebugGraph?.AddTimedSample(Scale);

                Rotation += FTL.Rotation * deltaTime;
                return false;
            }

            public void Draw(SpriteBatch batch, GameScreen screen)
            {
                float relativeTime = Time / FTL.Duration;
                Vector3 worldPos = Start.LerpTo(End, relativeTime);

                Vector2 pos  = screen.ProjectTo2D(worldPos);
                Vector2 edge = screen.ProjectTo2D(worldPos+new Vector3(125,0,0));

                float relSizeOnScreen = (edge.X - pos.X) / screen.Width;
                float sizeScaleOnScreen = Scale * relSizeOnScreen;
                if (sizeScaleOnScreen > 2f)
                {
                    batch.Draw(FTL.Tex, pos, Color.White, Rotation,
                        FTL.Tex.CenterF, sizeScaleOnScreen, SpriteEffects.None, 0.9f);
                }
            }
        }

        sealed class FTLInstance
        {
            readonly FTLLayer[] Layers;

            public FTLInstance(in Vector3 start, in Vector3 end)
            {
                Layers = new FTLLayer[FTLLayers.Length];
                for (int i = 0; i < Layers.Length; ++i)
                {
                    Layers[i] = new FTLLayer(start, end, FTLLayers[i]);
                }
            }

            // @return TRUE if all layers have finished
            public bool Update(float deltaTime)
            {
                bool allFinished = true;
                for (int i = 0; i < Layers.Length; ++i)
                {
                    if (Layers[i] != null)
                    {
                        if (Layers[i].Update(deltaTime))
                            Layers[i] = null; // layer finished
                        else
                            allFinished = false; // this layer was still ok
                    }
                }
                return allFinished;
            }

            public void Draw(SpriteBatch batch, GameScreen screen)
            {
                for (int i = 0; i < Layers.Length; ++i)
                {
                    Layers[i]?.Draw(batch, screen);
                }
            }
        }

		static readonly Array<FTLInstance> Effects = new Array<FTLInstance>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        public static void LoadContent(GameScreen screen)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile("FTL.yaml");
            ScreenManager.Instance.AddHotLoadTarget(screen, "FTL", file.FullName, fileInfo =>
            {
                LoadContent(screen);
            });

            using (var parser = new StarDataParser(file))
            {
                FTLLayers = parser.DeserializeArray<FTLLayerData>().ToArray();
                foreach (FTLLayerData layerData in FTLLayers)
                {
                    layerData.LoadContent(screen.TransientContent);
                }
            }
        }

        public static void AddFTL(in Vector3 position, in Vector3 forward, float radius)
        {
            Vector3 start = position + forward*(radius);
            Vector3 end   = position - forward*(radius*3f);
            var f = new FTLInstance(start, end);

            using (Lock.AcquireWriteLock())
            {
                Effects.Add(f);
            }
        }

        public static void DrawFTLModels(SpriteBatch batch, GameScreen screen)
        {
            batch.Begin(SpriteBlendMode.Additive, SpriteSortMode.Immediate, SaveStateMode.None);
            using (Lock.AcquireReadLock())
            {
                for (int i = 0; i < Effects.Count; ++i)
                {
                    Effects[i].Draw(batch, screen);
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

            DebugGraph = GetGraph(screen);

            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < Effects.Count; ++i)
			    {
                    if (Effects[i].Update(deltaTime))
                    {
                        Effects.RemoveAtSwapLast(i--);
                    }
			    }
            }
		}
	}
}
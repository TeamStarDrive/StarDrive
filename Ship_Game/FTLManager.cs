using System;
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
            [StarData] public readonly Color Color = Color.White;
            [StarData] public readonly Color DebugGraphColor = Color.Red;
            [StarData] readonly Vector2[] ScaleCurve;
            [StarData] readonly Vector2[] ColorCurve;
            #pragma warning restore 649

            public AnimationCurve ScaleCurves;
            public AnimationCurve ColorCurves;
            public SubTexture Tex;

            public void LoadContent(GameContentManager content)
            {
                string texture = Texture.NotEmpty()
                    ? "Textures/" + Texture
                    : "Textures/Ships/FTL.xnb";
                Tex = content.LoadTextureOrDefault(texture);

                ScaleCurves = new AnimationCurve(ScaleCurve, Duration);
                if (ColorCurve != null)
                    ColorCurves = new AnimationCurve(ColorCurve, Duration);
            }
        }

        sealed class FTLLayer
        {
            readonly FTLLayerData FTL;
            readonly Vector3 Offset;
            float Time;
            Vector3 Position;
            float Rotation;
            float Scale;

            public FTLLayer(in Vector3 offset, FTLLayerData data)
            {
                FTL = data;
                Offset = data.FrontToBack ? offset : -offset; // invert the offset
                Scale = data.ScaleCurves.GetY(Time);
            }

            // @return TRUE if layer has finished
            public bool Update(float deltaTime, in Vector3 position)
            {
                Time += deltaTime;
                if (Time >= FTL.Duration)
                    return true; // are we done?

                Position = position;

                Rotation += FTL.Rotation * deltaTime;

                Scale = FTL.ScaleCurves.GetY(Time);
                DebugGraph?.AddTimedSample(Scale, FTL.DebugGraphColor);

                return false;
            }

            public void Draw(SpriteBatch batch, GameScreen screen)
            {
                float relativeTime = Time / FTL.Duration;

                Vector3 start = Position + Offset;
                Vector3 end   = Position - Offset;

                Vector3 worldPos = start.LerpTo(end, relativeTime);

                Vector2 pos  = screen.ProjectTo2D(worldPos);
                Vector2 edge = screen.ProjectTo2D(worldPos+new Vector3(125,0,0));

                float relSizeOnScreen = (edge.X - pos.X) / screen.Width;
                float sizeScaleOnScreen = Scale * relSizeOnScreen;

                Color color = FTL.Color;
                if (FTL.ColorCurves != null)
                {
                    float colorMul = FTL.ColorCurves.GetY(Time);
                    DebugGraph?.AddTimedSample(colorMul*30f, FTL.DebugGraphColor.MultiplyRgb(0.5f));
                    color = color.MultiplyRgb(colorMul);
                }

                batch.Draw(FTL.Tex, pos, color, Rotation,
                    FTL.Tex.CenterF, sizeScaleOnScreen, SpriteEffects.None, 0.9f);
            }
        }

        sealed class FTLInstance
        {
            readonly FTLLayer[] Layers;
            readonly Func<Vector3> GetPosition;

            public FTLInstance(Func<Vector3> getPosition, in Vector3 offset)
            {
                GetPosition = getPosition;
                Layers = new FTLLayer[FTLLayers.Length];
                for (int i = 0; i < Layers.Length; ++i)
                {
                    Layers[i] = new FTLLayer(offset, FTLLayers[i]);
                }
            }

            // @return TRUE if all layers have finished
            public bool Update(float deltaTime)
            {
                Vector3 position = GetPosition();
                bool allFinished = true;
                for (int i = 0; i < Layers.Length; ++i)
                {
                    if (Layers[i] != null)
                    {
                        if (Layers[i].Update(deltaTime, position))
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
            using (Lock.AcquireWriteLock())
            {
                if (FTLLayers != null)
                {
                    Log.Info(ConsoleColor.DarkRed, "FTLManager.Unload");
                    Effects.Clear();
                    FTLLayers = null;
                }

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
        }

        public static void EnterFTL(Vector3 position, in Vector3 forward, float radius)
        {
            var f = new FTLInstance(() => position, forward*(radius*2f));
            using (Lock.AcquireWriteLock())
                Effects.Add(f);
        }

        public static void ExitFTL(Func<Vector3> getPosition, in Vector3 forward, float radius)
        {
            var f = new FTLInstance(getPosition, forward*(radius*-2f));
            using (Lock.AcquireWriteLock())
                Effects.Add(f);
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
        
        static UIGraphView GetGraph(GameScreen screen, string name, float x, float max)
        {
            if (screen.Find(name, out UIGraphView graph))
                return graph;

            graph = screen.Add(new UIGraphView
            {
                Name = name,
                Color = Color.TransparentBlack,
                LineColor = Color.Red,
                Pos = new Vector2(x, screen.Bottom - 500),
                Size = new Vector2(500, 250),
            });
            graph.SetRange(5f, 0, max);
            return graph;
        }

		public static void Update(GameScreen screen, float deltaTime)
		{
            if (deltaTime <= 0f)
                return;

            if (FTLLayers == null)
            {
                LoadContent(screen);
            }

            DebugGraph = GetGraph(screen, "FTL_Debug_Graph", 0, 60);

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
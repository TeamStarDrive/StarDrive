using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.UI.Effects;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    internal static class FTLManager
    {
        static FTLLayerData[] FTLLayers;
        static bool EnableDebugGraph = false;
        static UIGraphView DebugGraph;

        [StarDataType]
        class FTLLayerData
        {
            #pragma warning disable 649
            [StarData] public readonly string Texture;
            [StarData] public readonly float Duration = 0.6f;
            [StarData] public readonly float Rotation = 6.28f;
            [StarData] public readonly bool FrontToBack = true;
            [StarData] public readonly Color Color = Color.White;
            [StarData] public readonly Color DebugGraphColor = Color.Red;
            [StarData] public readonly int PositionFromLayer = -1;
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
                    : "Textures/Ships/FTL.png";
                Tex = content.LoadTextureOrDefault(texture);

                ScaleCurves = new AnimationCurve(ScaleCurve, Duration);
                if (ColorCurve != null)
                    ColorCurves = new AnimationCurve(ColorCurve, Duration);
            }
        }

        sealed class FTLLayer
        {
            readonly FTLLayerData FTL;
            readonly FTLLayer GetPosition;
            readonly Vector3 Offset;
            float Time;
            float Rotation;
            float Scale;
            Vector3 WorldPos;

            public FTLLayer(in Vector3 offset, FTLLayerData data, FTLLayer getPosition)
            {
                FTL = data;
                GetPosition = getPosition;
                Offset = data.FrontToBack ? offset : -offset; // invert the offset
                Scale = data.ScaleCurves.GetY(0f);
            }

            // @return TRUE if layer has finished
            public bool Update(FixedSimTime timeStep, in Vector3 position)
            {
                Time += timeStep.FixedTime;
                if (Time >= FTL.Duration)
                    return true; // are we done?

                Rotation = FTL.Rotation * Time;

                Scale = FTL.ScaleCurves.GetY(Time);
                DebugGraph?.AddTimedSample(Scale, FTL.DebugGraphColor);

                if (GetPosition != null)
                {
                    WorldPos = GetPosition.WorldPos;
                }
                else
                {
                    Vector3 start = position + Offset;
                    Vector3 end   = position - Offset;
                    WorldPos = start.Lerp(end, Time / FTL.Duration);
                }
                return false;
            }

            public void Draw(SpriteBatch batch, GameScreen screen, float radius)
            {
                float flashSize = Math.Min(radius * 0.75f, 200f);
                Vector2d pos  = screen.ProjectToScreenPosition(WorldPos);
                Vector2d edge = screen.ProjectToScreenPosition(WorldPos + new Vector3(flashSize, 0, 0));

                double relSizeOnScreen = (edge.X - pos.X) / screen.Width;
                float sizeScaleOnScreen = (float)(Scale * relSizeOnScreen);

                Color color = FTL.Color;
                if (FTL.ColorCurves != null)
                {
                    float colorMul = FTL.ColorCurves.GetY(Time);
                    //DebugGraph?.AddTimedSample(colorMul*30f, FTL.DebugGraphColor.MultiplyRgb(0.5f));
                    color = color.MultiplyRgb(colorMul);
                }

                Vector2 screenPos = pos.ToVec2f();
                batch.Draw(FTL.Tex, screenPos, color, Rotation,
                           FTL.Tex.CenterF, sizeScaleOnScreen, SpriteEffects.None, 0.9f);
            }
        }

        sealed class FTLInstance
        {
            readonly FTLLayer[] Layers;
            readonly Func<Vector3> GetPosition;
            Vector3 Position;
            readonly float Radius;

            public FTLInstance(Func<Vector3> getPosition, in Vector3 offset, float radius)
            {
                GetPosition = getPosition;
                Radius = radius;
                Layers = new FTLLayer[FTLLayers.Length];
                for (int i = 0; i < Layers.Length; ++i)
                {
                    FTLLayerData data = FTLLayers[i];
                    FTLLayer positionRef = null;
                    if (0 <= data.PositionFromLayer && data.PositionFromLayer < i)
                    {
                        positionRef = Layers[data.PositionFromLayer];
                    }
                    Layers[i] = new FTLLayer(offset, data, positionRef);
                }
            }

            // @return TRUE if all layers have finished
            public bool Update(FixedSimTime timeStep)
            {
                Position = GetPosition();
                bool allFinished = true;
                for (int i = 0; i < Layers.Length; ++i)
                {
                    if (Layers[i] != null)
                    {
                        if (Layers[i].Update(timeStep, Position))
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
                    Layers[i]?.Draw(batch, screen, Radius);
                }
            }
        }

        static Array<FTLInstance> Pending = new Array<FTLInstance>();
        static FTLInstance[] ActiveEffects = Empty<FTLInstance>.Array;
        static readonly object LoadLock = new object();

        public static void LoadContent(GameScreen screen, bool reload = false)
        {
            lock (LoadLock)
            {
                if (FTLLayers != null)
                {
                    lock (Pending) Pending = new Array<FTLInstance>();
                    ActiveEffects = Empty<FTLInstance>.Array;
                    if (!reload)
                        return; // already loaded
                    FTLLayers = null;
                }

                FileInfo file = screen.ScreenManager.AddHotLoadTarget(screen, "FTL.yaml", fileInfo =>
                {
                    LoadContent(screen, reload:true);
                });

                FTLLayers = YamlParser.DeserializeArray<FTLLayerData>(file).ToArray();
                foreach (FTLLayerData layerData in FTLLayers)
                {
                    layerData.LoadContent(ResourceManager.RootContent);
                }
            }
        }

        public static void EnterFTL(Vector3 position, in Vector3 forward, float radius)
        {
            if (FTLLayers == null)
                return;
            var f = new FTLInstance(() => position, forward*(radius*1.5f), radius);
            lock (Pending) Pending.Add(f);
        }

        public static void ExitFTL(Func<Vector3> getPosition, in Vector3 forward, float radius)
        {
            if (FTLLayers == null)
                return;
            var f = new FTLInstance(getPosition, forward*(radius*-2f), radius);
            lock (Pending) Pending.Add(f);
        }

        public static void Update(GameScreen screen, FixedSimTime timeStep)
        {
            if (timeStep.FixedTime <= 0f)
                return;

            if (EnableDebugGraph)
            {
                DebugGraph = GetGraph(screen, "FTL_Debug_Graph", 0, 60);
            }

            var activeEffects = new Array<FTLInstance>();

            lock (Pending)
            {
                if (Pending.NotEmpty)
                {
                    activeEffects.AddRange(Pending);
                    Pending = new Array<FTLInstance>();
                }
            }

            // always update newly added effects first
            for (int i = 0; i < activeEffects.Count; ++i)
            {
                activeEffects[i].Update(timeStep);
            }

            // then go through Currently active effects
            var effects = ActiveEffects;
            for (int i = 0; i < effects.Length; ++i)
            {
                if (!effects[i].Update(timeStep))
                {
                    activeEffects.Add(effects[i]);
                }
            }

            ActiveEffects = activeEffects.ToArray();
        }

        public static void DrawFTLModels(SpriteBatch batch, GameScreen screen)
        {
            lock (LoadLock)
            {
                var effects = ActiveEffects;
                if (effects.Length == 0)
                    return;

                batch.SafeBegin(SpriteBlendMode.Additive, sortImmediate:true);
                for (int i = 0; i < effects.Length; ++i)
                {
                    effects[i].Draw(batch, screen);
                }
                batch.SafeEnd();
            }
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
    }
}
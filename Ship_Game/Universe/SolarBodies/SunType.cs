using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.SpriteSystem;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Utils;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class SunLayerInfo
    {
        [StarData] public readonly string TexturePath; // REQUIRED
        [StarData] public readonly string AnimationPath; // OPTIONAL
        [StarData] public readonly float AnimationSpeed = 1f;
        [StarData] public readonly Color TextureColor = Color.White;
        [StarData] public readonly float LayerScale = 1.0f; // extra visual scale factor for this layer
        [StarData] public readonly SpriteBlendMode BlendMode = SpriteBlendMode.AlphaBlend;
        [StarData] public readonly float RotationSpeed = 0.03f;
        [StarData] public readonly Range RotationStart = new Range(-1f, 1f);
        [StarData] public readonly float PulsePeriod = 0f; // period of animated pulse
        [StarData] public readonly Range PulseScale = new Range(1.0f);
        [StarData] public readonly Range PulseColor = new Range(1.0f);
    }
    
    [StarDataType]
    public class SunType
    {
        [StarData] public string Id;
        [StarData] public readonly string IconPath;
        [StarData] public readonly int IconLayer = 0; // which layer for icon?
        [StarData] public readonly float IconScale = 1.0f; // icon scale in low-res draw
        [StarData] public readonly float LightIntensity = 1.5f;
        [StarData] public readonly float Radius = 150000f;
        [StarData] public readonly Color LightColor = Color.White;
        [StarData] public readonly bool  Habitable = true; // is this star habitable, or is this a dangerous type?
        [StarData] public readonly float RadiationDamage = 0f; // is this star dangerous and damages nearby ships??
        [StarData] public readonly float RadiationRadius = 0f;
        [StarData] public readonly Array<SunLayerInfo> Layers;
        [StarData] public readonly float ResearchableChance; // Can this star contribute to research efforts (needs a research station)

        public bool Disposed; // if true, this SunType was Disposed because of Hotloading
        public SubTexture Icon { get; private set; } // lo-res icon used in background star fields

        public override string ToString() => $"SunType {Id}  {IconPath}  Light:{LightColor}  Habit:{Habitable}";

        static readonly Map<string, SunType> Map = new();
        public static SunType FindSun(string id) => Map[id];
        static SunType[] HabitableSuns;
        static SunType[] BarrenSuns;

        public static SunType RandomHabitableSun(RandomBase random, Predicate<SunType> filter = null)
        {
            if (filter != null)
                return random.Item(HabitableSuns.Filter(filter));
            return random.Item(HabitableSuns);
        }
        public static SunType RandomBarrenSun(RandomBase random)
        {
            return random.Item(BarrenSuns.Length != 0 ? BarrenSuns : HabitableSuns);
        }
        public static SubTexture[] GetLoResTextures()
        {
            return Map.FilterValues(s => s.Icon != null).Select(s => s.Icon);
        }

        public static void LoadSunTypes(bool loadIcons = true)
        {
            GameLoadingScreen.SetStatus("LoadSunTypes");

            FileInfo file = GameBase.ScreenManager.AddHotLoadTarget(null, "Suns.yaml", (f) => LoadSuns(f, loadIcons));
            LoadSuns(file, loadIcons);
        }

        public static void Unload()
        {
            GameBase.ScreenManager.RemoveHotLoadTarget("Suns");
            Map.Clear();
            HabitableSuns = Empty<SunType>.Array;
            BarrenSuns    = Empty<SunType>.Array;
        }

        static void LoadSuns(FileInfo file, bool loadIcons = true)
        {
            // mark any previous suns as Disposed
            if (Map.Count != 0)
            {
                Log.Write(ConsoleColor.Magenta, "Reinitializing Solar Systems...");
                foreach (SunType sun in Map.Values)
                    sun.Disposed = true;
            }

            Array<SunType> all = YamlParser.DeserializeArray<SunType>(file);

            if (loadIcons) // load all sun icons if needed (not necessary for unit tests)
            {
                Parallel.ForEach(all, sun =>
                {
                    sun.Icon = ResourceManager.RootContent.LoadTextureOrDefault("Textures/"+sun.IconPath);
                });
            }

            Map.Clear();
            foreach (SunType sun in all)
                Map[sun.Id] = sun;

            HabitableSuns = all.Filter(s => s.Habitable);
            BarrenSuns    = all.Filter(s => !s.Habitable);
        }

        public float DamageMultiplier(float distFromSun)
        {
            // https://www.desmos.com/calculator/lc2u7qxmhj
            // this is the inverse square law with a min multiplier
            // to create a dead zone where damage is 1.0

            float intensity = ((200f * RadiationRadius) / (distFromSun * distFromSun)) - 0.002f;

            // it's very similar to inverse square law, but the curve is less forgiving
            // there's a % radius where intensity is 1.0
            //float intensity = (0.03f / (distFromSun / RadiationRadius)) - 0.031f;
            return intensity.Clamped(0f, 1f);
        }

        public void DrawIcon(SpriteBatch batch, Rectangle rect)
        {
            batch.Draw(Icon, rect, Color.White);
        }

        static Vector2 ScreenPosition(Vector2 pos, in Matrix view, in Matrix projection)
        {
            return new Vector3(GameBase.Viewport.Project(pos.ToVec3(), 
                            projection, view, Matrix.Identity)).ToVec2();
        }

        public void DrawLowResSun(SpriteBatch batch, SolarSystem sys, in Matrix view, in Matrix projection)
        {
            if (sys.SunLayers.Length == 0)
                return;

            // which layer should we pick?
            int whichLayer = sys.Sun.IconLayer;
            if (whichLayer < 0 || whichLayer >= sys.SunLayers.Length)
            {
                Log.Warning($"{sys.Sun} Invalid IconLayer: {whichLayer}. Using layer 0. Please fix this!");
                whichLayer = 0;
            }
            
            float scale = 0.07f * sys.Sun.IconScale;
            Vector2 pos = ScreenPosition(sys.Position, view, projection);

            sys.SunLayers[whichLayer].DrawLoRes(batch, sys.Sun.Icon, pos, scale);
        }

        public SunLayerState[] CreateLayers(GameContentManager universeContent, RandomBase random)
        {
            var states = new SunLayerState[Layers.Count];
            for (int i = 0; i < Layers.Count; ++i)
                states[i] = new SunLayerState(universeContent, Layers[i], random);
            return states;
        }
        
        public void DrawSunMesh(SolarSystem sys, in Matrix view, in Matrix projection)
        {
            Vector2 pos  = ScreenPosition(sys.Position, view, projection);
            Vector2 edge = ScreenPosition(sys.Position + new Vector2(sys.Sun.Radius, 0f), view, projection);

            float relSizeOnScreen = (edge.X - pos.X) / GameBase.ScreenWidth;
            float sizeScaleOnScreen = 1.25f * relSizeOnScreen; // this yields the base star size

            SpriteBatch batch = GameBase.ScreenManager.SpriteBatch;
            batch.SafeEnd();
            {
                //sys.DysonSwarm?.DrawDysonRings(batch, pos, sizeScaleOnScreen);
                foreach (SunLayerState layer in sys.SunLayers)
                    layer.Draw(batch, pos, sizeScaleOnScreen);
                sys.DysonSwarm?.DrawDysonRings(batch, pos, sizeScaleOnScreen);
            }
            batch.SafeBegin();
        }
    }

    
    public class SunLayerState
    {
        public readonly SunLayerInfo Info;
        public readonly DrawableSprite Sprite;
        float PulseTimer;
        public float Intensity { get; private set; } = 1f; // current sun intensity
        float ScaleIntensity = 1f;
        float ColorIntensity = 1f;

        public SunLayerState(GameContentManager content, SunLayerInfo info, RandomBase random)
        {
            Info = info;
            Sprite = CreateSprite(content, info);
            InitializeSprite(info.RotationStart.Generate(random));
        }

        public SunLayerState(GameContentManager content, SunLayerInfo info, float rotation)
        {
            Info = info;
            Sprite = CreateSprite(content, info);
            InitializeSprite(rotation);
        }

        // Helper method to create the sprite based on the info
        DrawableSprite CreateSprite(GameContentManager content, SunLayerInfo info)
        {
            return info.AnimationPath.NotEmpty() ? DrawableSprite.Animation(content, info.AnimationPath, looping: true)
                                                 : DrawableSprite.SubTex(content, info.TexturePath);
        }

        void InitializeSprite(float rotation)
        {
            Sprite.Effects = SpriteEffects.FlipVertically;
            Sprite.Rotation = rotation;
        }

        public void Update(FixedSimTime timeStep)
        {
            Sprite.Rotation += Info.RotationSpeed * timeStep.FixedTime;
            Sprite.Update(Info.AnimationSpeed * timeStep.FixedTime);

            if (Info.PulsePeriod > 0f)
            {
                // this is a nice sine-wave pulse effect that varies our intensity
                PulseTimer += timeStep.FixedTime;

                // sine-wave pattern
                float progress = (PulseTimer/Info.PulsePeriod)*RadMath.TwoPI; // tick every second
                Intensity = (1.0f + RadMath.Sin(progress))*0.5f; // convert to positive [0.0-1.0] scale

                ScaleIntensity = Info.PulseScale.Min.LerpTo(Info.PulseScale.Max, Intensity);
                ColorIntensity = Info.PulseColor.Min.LerpTo(Info.PulseColor.Max, Intensity);
            }
        }

        public void Draw(SpriteBatch batch, Vector2 screenPos, float sizeScaleOnScreen)
        {
            batch.SafeBegin(Info.BlendMode);

            float scale = ScaleIntensity * sizeScaleOnScreen * Info.LayerScale;
            Color color = Info.TextureColor;

            // draw this layer multiple times to increase the intensity
            for (float intensity = ColorIntensity; intensity.Greater(0f); intensity -= 1f)
            {
                Color c = intensity > 1f ? color : new Color(color, intensity);
                Sprite.Draw(batch, screenPos, scale, c);
            }

            batch.SafeEnd();
        }

        public void DrawLoRes(SpriteBatch batch, SubTexture icon, Vector2 screenPos, float sizeScaleOnScreen)
        {
            float scale = ScaleIntensity * sizeScaleOnScreen;
            batch.Draw(icon, screenPos, Info.TextureColor, Sprite.Rotation, 
                       icon.CenterF, scale, SpriteEffects.FlipVertically, 0.9f);
        }

    }

    [StarDataType]
    public class DysonRings
    {
        [StarData] public static Array<SunLayerInfo> Rings { get; private set; }

        public static void LoadDysonRings()
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile("DysonRings.yaml");
            Rings = YamlParser.DeserializeArray<SunLayerInfo>(file);
        }
    }
}

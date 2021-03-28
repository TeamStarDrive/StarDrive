using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.SpriteSystem;

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
        [StarDataKey] public string Id;
        [StarData] public readonly string IconPath;
        [StarData] public readonly int IconLayer = 0; // which layer for icon?
        [StarData] public readonly float IconScale = 1.0f; // icon scale in low-res draw
        [StarData] public readonly float LightIntensity = 1.5f;
        [StarData] public readonly float Radius = 150000f;
        [StarData] public readonly Color LightColor = Color.White;
        [StarData] public readonly bool  Habitable = true; // is this star habitable, or is this a dangerous type?
        [StarData] public readonly float RadiationDamage = 0f; // is this star dangerous and damages nearby ships??
        [StarData] public readonly float RadiationRadius = 0f;
        [StarData] readonly Array<SunLayerInfo> Layers;
        public SubTexture Icon { get; private set; } // lo-res icon used in background star fields

        public override string ToString() => $"SunType {Id}  {IconPath}  Light:{LightColor}  Habit:{Habitable}";

        static readonly Map<string, SunType> Map = new Map<string, SunType>();
        public static SunType FindSun(string id) => Map[id];
        static SunType[] HabitableSuns;
        static SunType[] BarrenSuns;

        public static SunType RandomHabitableSun(Predicate<SunType> filter)
        {
            return RandomMath.RandItem(HabitableSuns.Filter(filter));
        }
        public static SunType RandomBarrenSun()
        {
            return RandomMath.RandItem(BarrenSuns.Length != 0 ? BarrenSuns : HabitableSuns);
        }
        public static SubTexture[] GetLoResTextures()
        {
            return Map.FilterValues(s => s.Icon != null).Select(s => s.Icon);
        }

        public static void LoadAll()
        {
            GameLoadingScreen.SetStatus("LoadSunTypes", "");
            FileInfo file = ResourceManager.GetModOrVanillaFile("Suns.yaml");
            LoadSuns(file);
            GameBase.ScreenManager.AddHotLoadTarget(null, "Suns", file.FullName, OnSunsFileModified);
        }

        public static void Unload()
        {
            Map.Clear();
            HabitableSuns = Empty<SunType>.Array;
            BarrenSuns    = Empty<SunType>.Array;
        }

        static void LoadSuns(FileInfo file)
        {
            Array<SunType> all;
            using (var parser = new YamlParser(file))
                all = parser.DeserializeArray<SunType>();
            
            Map.Clear();
            foreach (SunType sun in all)
            {
                try
                {
                    var loRes = ResourceManager.RootContent.Load<Texture2D>("Textures/"+sun.IconPath);
                    sun.Icon = new SubTexture(sun.IconPath, loRes);
                }
                catch
                {
                    sun.Icon = ResourceManager.RootContent.DefaultTexture();
                }
                Map[sun.Id] = sun;
            }

            HabitableSuns = all.Filter(s => s.Habitable);
            BarrenSuns    = all.Filter(s => !s.Habitable);
        }

        static void OnSunsFileModified(FileInfo file)
        {
            LoadSuns(file);
            if (Empire.Universe != null)
            {
                // re-initialize all solar systems suns
                Log.Write(ConsoleColor.Magenta, "Reinitializing Solar Systems...");
                foreach (SolarSystem system in Empire.Universe.SolarSystemDict.Values)
                    system.Sun = FindSun(system.Sun.Id);
            }
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
            return GameBase.Viewport.Project(pos.ToVec3(), 
                            projection, view, Matrix.Identity).ToVec2();
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

        public SunLayerState[] CreateLayers()
        {
            var states = new SunLayerState[Layers.Count];
            for (int i = 0; i < Layers.Count; ++i)
                states[i] = new SunLayerState(ResourceManager.RootContent, Layers[i]);
            return states;
        }
        
        public void DrawSunMesh(SolarSystem sys, in Matrix view, in Matrix projection)
        {
            Vector2 pos  = ScreenPosition(sys.Position, view, projection);
            Vector2 edge = ScreenPosition(sys.Position + new Vector2(Radius, 0f), view, projection);

            float relSizeOnScreen = (edge.X - pos.X) / GameBase.ScreenWidth;
            float sizeScaleOnScreen = 1.25f * relSizeOnScreen; // this yields the base star size

            SpriteBatch batch = GameBase.ScreenManager.SpriteBatch;
            batch.End();
            {
                foreach (SunLayerState layer in sys.SunLayers)
                    layer.Draw(batch, pos, sizeScaleOnScreen);
            }
            batch.Begin();
        }
    }

    
    public class SunLayerState
    {
        public readonly SunLayerInfo Info;
        readonly DrawableSprite Sprite;
        float PulseTimer;
        public float Intensity { get; private set; } = 1f; // current sun intensity
        float ScaleIntensity = 1f;
        float ColorIntensity = 1f;


        public SunLayerState(GameContentManager content, SunLayerInfo info)
        {
            Info = info;

            if (info.AnimationPath.NotEmpty())
                Sprite = DrawableSprite.Animation(content, info.AnimationPath, looping: true);
            else
                Sprite = DrawableSprite.SubTex(content, info.TexturePath);
            
            Sprite.Effects = SpriteEffects.FlipVertically;
            Sprite.Rotation = info.RotationStart.Generate();
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
            batch.Begin(Info.BlendMode, SpriteSortMode.Deferred, SaveStateMode.None);

            float scale = ScaleIntensity * sizeScaleOnScreen * Info.LayerScale;
            Color color = Info.TextureColor;

            // draw this layer multiple times to increase the intensity
            for (float intensity = ColorIntensity; intensity.Greater(0f); intensity -= 1f)
            {
                Color c = intensity > 1f ? color : new Color(color, intensity);
                Sprite.Draw(batch, screenPos, scale, c);
            }

            batch.End();
        }

        public void DrawLoRes(SpriteBatch batch, SubTexture icon, Vector2 screenPos, float sizeScaleOnScreen)
        {
            float scale = ScaleIntensity * sizeScaleOnScreen;
            batch.Draw(icon, screenPos, Info.TextureColor, Sprite.Rotation, 
                       icon.CenterF, scale, SpriteEffects.FlipVertically, 0.9f);
        }

    }

}

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

namespace Ship_Game.Universe.SolarBodies
{
    public class SunLayerInfo
    {
        [StarData] public readonly string TexturePath;
        [StarData] public readonly Color TextureColor = Color.White;
        [StarData] public readonly SpriteBlendMode BlendMode = SpriteBlendMode.AlphaBlend;
        [StarData] public readonly float RotationSpeed = 0.03f;
        [StarData] public readonly float PulsePeriod = 5f; // period of animated pulse
        [StarData] public readonly Range PulseScale = new Range(0.95f, 1.05f); 
        [StarData] public readonly Range PulseColor = new Range(0.95f, 1.05f); 
    }

    public class SunType
    {
        [StarDataKey] public string Id;
        [StarData] public readonly string IconPath;
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
            return RandomMath.RandItem(BarrenSuns);
        }
        public static SubTexture[] GetLoResTextures()
        {
            return Map.FilterValues(s => s.Icon != null).Select(s => s.Icon);
        }

        public static void LoadAll()
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile("Suns.yaml");
            LoadSuns(file);
            StarDriveGame.Instance.ScreenManager.AddHotLoadTarget(file.FullName, OnSunsFileModified);
        }

        static void LoadSuns(FileInfo file)
        {
            Array<SunType> all;
            using (var parser = new StarDataParser(file))
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
            // this is a custom non-linear falloff
            // https://www.desmos.com/calculator/lc2u7qxmhj
            // it's very similar to inverse square law
            // but there's a 20% radius where intensity is 1.0
            float linear = distFromSun / RadiationRadius;
            float intensity = (0.2f / linear) - 0.2f;
            return intensity.Clamped(0f, 1f);
        }

        public void DrawIcon(SpriteBatch batch, Rectangle rect)
        {
            batch.Draw(Icon, rect, Color.White);
        }

        static Vector2 ScreenPosition(Vector2 pos, in Matrix view, in Matrix projection)
        {
            return StarDriveGame.Instance.Viewport.Project(pos.ToVec3(), 
                            projection, view, Matrix.Identity).ToVec2();
        }

        public void DrawLowResSun(SpriteBatch batch, SolarSystem sys, in Matrix view, in Matrix projection)
        {
            const float constantScaleOnScreen = 0.05f;
            Vector2 pos = ScreenPosition(sys.Position, view, projection);

            SubTexture sunTex = sys.Sun.Icon;
            foreach (SunLayerState layer in sys.SunLayers)
            {
                batch.Draw(sunTex, pos, layer.Info.TextureColor, 
                    layer.Rotation, sunTex.CenterF, constantScaleOnScreen, SpriteEffects.None, 0.9f);
            }
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

            float relSizeOnScreen = (edge.X - pos.X) / StarDriveGame.Instance.ScreenWidth;
            float sizeScaleOnScreen = 1.25f * relSizeOnScreen; // this yields the base star size

            SpriteBatch batch = StarDriveGame.Instance.ScreenManager.SpriteBatch;
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
        public float Rotation { get; private set; }
        float PulseTimer;
        public float Intensity { get; private set; } = 1f; // current sun intensity
        float ScaleIntensity = 1f;
        float ColorIntensity = 1f;
        readonly SubTexture Texture;

        public SunLayerState(GameContentManager content, SunLayerInfo info)
        {
            Info = info;
            Rotation = info.RotationSpeed * RandomMath.RandomBetween(-20f, 20f); // start at a random rotation
            var tex = content.Load<Texture2D>("Textures/"+info.TexturePath);
            Texture = new SubTexture(info.TexturePath, tex);
        }

        public void Update(float deltaTime)
        {
            Rotation += Info.RotationSpeed * deltaTime;

            if (Info.PulsePeriod > 0f)
            {
                // this is a nice sine-wave pulse effect that varies our intensity
                PulseTimer += deltaTime;
                float progress = (PulseTimer/Info.PulsePeriod)*(float)Math.PI*2; // tick every second
                Intensity = (1.0f + (float)Math.Sin(progress))*0.5f; // convert to positive [0.0-1.0] scale

                ScaleIntensity = Info.PulseScale.Min.LerpTo(Info.PulseScale.Max, Intensity);
                ColorIntensity = Info.PulseColor.Min.LerpTo(Info.PulseColor.Max, Intensity);
            }
        }

        public void Draw(SpriteBatch batch, Vector2 screenPos, float sizeScaleOnScreen)
        {
            batch.Begin(Info.BlendMode, SpriteSortMode.Deferred, SaveStateMode.None);

            float scale = ScaleIntensity * sizeScaleOnScreen;
            Color color = Info.TextureColor;

            // draw this layer multiple times to increase the intensity
            for (float intensity = ColorIntensity; intensity > 0f; intensity -= 1f)
            {
                Color c = intensity > 1f ? color : new Color(color, intensity);
                batch.Draw(Texture, screenPos, c, Rotation, 
                    Texture.CenterF, scale, SpriteEffects.FlipVertically, 0.9f);
            }

            batch.End();
        }
    }

}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
    [StarDataType]
    public class ParticleSettings
    {
        // Name of the ParticleSystem
        [StarData] public string Name;

        // Which particle texture to use
        [StarData] public string TextureName;

        // Path to a ParticleEffect.fx HLSL shader
        [StarData] public string Effect;
        
        // if true, particle never disappears and cannot move (but may rotate)
        [StarData] public bool Static;

        // if true, particle is only rendered when game camera is deemed as "nearView"
        [StarData] public bool OnlyNearView;

        // Maximum number of allowed particles on screen
        [StarData] public int MaxParticles = 100;

        // Duration / Lifetime of a single particle
        [StarData] public TimeSpan Duration = TimeSpan.FromSeconds(1.0);

        // Random amount of extra time added to Duration
        // ActualDuration = Duration + Duration*DurationRandomness*Random(0.0, 1.0)
        [StarData] public float DurationRandomness;
 
        // How much velocity to inherit from the Emitter (float)
        // 0: no velocity inherited
        // +1: all velocity
        // -1: reverse direction
        [StarData] public float EmitterVelocitySensitivity = 1f;

        // How much the particle rotation should follow velocity direction
        // 0: particle rotates how it wants (default)
        // +1: particle rotation is always fixed to its velocity direction
        // -1: particle rotation is reverse of its velocity direction
        [StarData] public float AlignRotationToVelocity;

        // Additional random velocity added for each particle in global X and Y axis
        // If you want to align XY random to velocity vector, see `AlignRandomVelocityXY`
        [StarData] public Range[] RandomVelocityXY = new Range[2]; // default: [ [0,0], [0,0] ]

        // if true, `RandomVelocityXY` is aligned to current velocity vector,
        // meaning X is perpendicular to particle velocity vector and Y is parallel to particle velocity
        [StarData] public bool AlignRandomVelocityXY;

        // Multiplier for setting the end velocity of the particle
        // 0.5 means the particle has half the velocity when it dies
        [StarData] public float EndVelocity = 1f;

        [StarData] public Color MinColor = Color.White;
        [StarData] public Color MaxColor = Color.White;

        // random rotation speed range
        [StarData] public float MinRotateSpeed;
        [StarData] public float MaxRotateSpeed;

        [StarData] public float MinStartSize = 100f;
        [StarData] public float MaxStartSize = 100f;
        [StarData] public float MinEndSize = 100f;
        [StarData] public float MaxEndSize = 100f;

        // DirectX HLSL blend modes
        [StarData] public Blend SourceBlend = Blend.SourceAlpha;
        [StarData] public Blend DestinationBlend = Blend.InverseSourceAlpha;


        // Is this a rotating particle? important for effect technique selection
        public bool IsRotating => MinRotateSpeed != 0f || MaxRotateSpeed != 0f;
        public bool IsAlignRotationToVel => AlignRotationToVelocity != 0f;

        static Map<string, ParticleSettings> Settings = new Map<string, ParticleSettings>();

        public ParticleSettings Clone()
        {
            return (ParticleSettings)MemberwiseClone();
        }

        public static void LoadAll()
        {
            GameLoadingScreen.SetStatus("LoadParticles");

            FileInfo file = GameBase.ScreenManager.AddHotLoadTarget(null, "3DParticles/Particles.yaml", LoadParticles);
            LoadParticles(file);
        }

        public static void Unload()
        {
            GameBase.ScreenManager.RemoveHotLoadTarget("Particles");
            Settings.Clear();
        }

        static void LoadParticles(FileInfo file)
        {
            Settings.Clear();
            using (var parser = new YamlParser(file))
            {
                Array<ParticleSettings> list = parser.DeserializeArray<ParticleSettings>();
                foreach (ParticleSettings ps in list)
                {
                    if (Settings.ContainsKey(ps.Name))
                    {
                        Log.Error($"ParticleSetting duplicate definition='{ps.Name}' in Particles.yaml. Ignoring.");
                    }
                    else
                    {
                        Settings.Add(ps.Name, ps);
                        ps.GetEffect(ResourceManager.RootContent); // compile
                    }
                }
            }
            Empire.Universe?.Particles.Reload();
        }

        public static ParticleSettings Get(string name)
        {
            if (Settings.Count == 0)
                throw new InvalidOperationException("ParticleSettings have not been loaded!");
            if (!Settings.TryGetValue(name, out ParticleSettings ps))
                throw new InvalidDataException($"Unknown ParticleSettings Name: {name}");
            return ps;
        }

        public Effect GetEffect(GameContentManager content)
        {
            return content.LoadEffect("3DParticles/" + Effect);
        }

        public Texture2D GetTexture(GameContentManager content)
        {
            return content.Load<Texture2D>("3DParticles/" + TextureName);
        }
    }
}
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
    [StarDataType]
    public class ParticleSettings
    {
        [StarData] public string Name;
        [StarData] public string TextureName;
        [StarData] public int MaxParticles = 100;
        [StarData] public TimeSpan Duration = TimeSpan.FromSeconds(1.0);
        [StarData] public float DurationRandomness;
        [StarData] public float EmitterVelocitySensitivity = 1f;
        [StarData] public float MinHorizontalVelocity;
        [StarData] public float MaxHorizontalVelocity;
        [StarData] public float MinVerticalVelocity;
        [StarData] public float MaxVerticalVelocity;
        [StarData] public float EndVelocity = 1f;
        [StarData] public Color MinColor = Color.White;
        [StarData] public Color MaxColor = Color.White;
        [StarData] public float MinRotateSpeed;
        [StarData] public float MaxRotateSpeed;
        [StarData] public float MinStartSize = 100f;
        [StarData] public float MaxStartSize = 100f;
        [StarData] public float MinEndSize = 100f;
        [StarData] public float MaxEndSize = 100f;
        [StarData] public Blend SourceBlend = Blend.SourceAlpha;
        [StarData] public Blend DestinationBlend = Blend.InverseSourceAlpha;

        static Map<string, ParticleSettings> Settings = new Map<string, ParticleSettings>();

        public ParticleSettings Clone()
        {
            return (ParticleSettings)MemberwiseClone();
        }

        public static void LoadAll()
        {
            GameLoadingScreen.SetStatus("LoadParticles");
            FileInfo file = ResourceManager.GetModOrVanillaFile("3DParticles/Particles.yaml");
            LoadParticles(file);
            GameBase.ScreenManager.AddHotLoadTarget(null, "Particles", file.FullName, LoadParticles);
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
                foreach (ParticleSettings set in list)
                {
                    if (Settings.ContainsKey(set.Name))
                    {
                        Log.Error($"ParticleSetting duplicate definition='{set.Name}' in Particles.yaml. Ignoring.");
                    }
                    else
                    {
                        Settings.Add(set.Name, set);
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
    }
}
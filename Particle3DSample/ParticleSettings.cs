using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public class ParticleSettings
    {
        [StarData] public string TextureName;
        [StarData] public int MaxParticles = 100;
        [StarData] public TimeSpan Duration = TimeSpan.FromSeconds(1.0);
        [StarData] public float DurationRandomness;
        [StarData] public float EmitterVelocitySensitivity = 1f;
        [StarData] public float MinHorizontalVelocity;
        [StarData] public float MaxHorizontalVelocity;
        [StarData] public float MinVerticalVelocity;
        [StarData] public float MaxVerticalVelocity;
        [StarData] public Vector3 Gravity = Vector3.Zero;
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

        public ParticleSettings()
        {
        }

        public ParticleSettings(Particle3DSample.ParticleSettings s)
        {
            TextureName = s.TextureName;
            MaxParticles = s.MaxParticles;
            Duration = s.Duration;
            DurationRandomness = s.DurationRandomness;
            EmitterVelocitySensitivity = s.EmitterVelocitySensitivity;
            MinHorizontalVelocity = s.MinHorizontalVelocity;
            MaxHorizontalVelocity = s.MaxHorizontalVelocity;
            MinVerticalVelocity = s.MinVerticalVelocity;
            MaxVerticalVelocity = s.MaxVerticalVelocity;
            Gravity = s.Gravity;
            EndVelocity = s.EndVelocity;
            MinColor = s.MinColor;
            MaxColor = s.MaxColor;
            MinRotateSpeed = s.MinRotateSpeed;
            MaxRotateSpeed = s.MaxRotateSpeed;
            MinStartSize = s.MinStartSize;
            MaxStartSize = s.MaxStartSize;
            MinEndSize = s.MinEndSize;
            MaxEndSize = s.MaxEndSize;
            SourceBlend = s.SourceBlend;
            DestinationBlend = s.DestinationBlend;
        }
    }
}
using System;
using Ship_Game.Data.Serialization;
using Vector4 = SDGraphics.Vector4;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class AtmosphereType
    {
        [StarData] public string Id;
        [StarData] public string Clouds; // enable clouds effect with a specified clouds map
        [StarData] public Vector4 Glow;  // enable glow with [R,G,B,A] color, alpha controls intensity
        [StarData] public float Fresnel = 1f; // control fresnel intensity for glow, [0.0; 1.0], default: 1.0

        [StarData] public bool NoHalo;   // disable atmosphere halo (very subtle)
        [StarData] public bool NoAtmosphere; // disable atmosphere sphere (very subtle)
    }
}

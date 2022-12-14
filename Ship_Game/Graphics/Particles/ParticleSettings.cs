using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
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

        // Which particle texture to use from `Content/3DParticles/`
        // If a file with the same name exists in `Mods/MyMod/Content/3DParticles`, then it is used instead
        [StarData] public string TextureName;

        // Path to a ParticleEffect.fx HLSL shader in `Content/3DParticles`
        // Can be used to create your own (better) particle effects if needed
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
        [StarData] public float InheritOwnerVelocity = 1f;

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

        // Linear Starting Color Range assigned at the start of the particle
        //
        // Alpha fades based on the age of the particle. This curve is hard coded
        // to make the particle fade in fairly quickly, then fade out more slowly.
        // The 6.75 constant scales the curve so the alpha will reach 1.0 at relativeAge=0.33
        // enter x*(1-x)*(1-x)*6.75 for x=0:1 into a plotting program to see the curve
        // https://www.desmos.com/calculator/bhcidfwd0e
        // https://www.wolframalpha.com/input/?i=x*%281-x%29*%281-x%29*6.75+for+x%3D0%3A1
        [StarData] public Color[] StartColorRange = { Color.White, Color.White };

        // Linearly interpolate towards a random EndColor
        // Default value is equal to StartColorRange
        // Particle reaches EndColor at relativeAge=EndColorTime
        [StarData] public Color[] EndColorRange = null;

        // relativeAge [0.0; 1.0] when particle reaches it EndColor value
        // default is 1.0, which while not ideal, is predictable
        [StarData] public float EndColorTime = 1.0f;

        // random rotation speed range in radians/s
        [StarData] public Range RotateSpeed;

        // random start and end size ranges
        // Examples:
        //  StartEndSize: [ [16,32], [4,8] ]  # randomly shrinking particle
        //  StartEndSize: [ [4,8], [16,32] ]  # randomly growing particle
        //  StartEndSize: [ 32, 4 ]  # linearly shrinking particle (no random)
        //  StartEndSize: [ 4, 32 ]  # linearly growing particle (no random)
        //  StartEndSize: [ 32 ]  # constant size particle
        [StarData] public Range[] StartEndSize = { new Range(32), new Range(32) };

        // DirectX HLSL blend modes
        // This requires some advanced knowledge of how DirectX Pixel Shaders work
        // Extra reading here: 
        // https://takinginitiative.wordpress.com/2010/04/09/directx-10-tutorial-6-transparency-and-alpha-blending/
        // http://www.directxtutorial.com/Lesson.aspx?lessonid=9-4-10
        [StarData] public Blend[] SrcDstBlend = { Blend.SourceAlpha, Blend.InverseSourceAlpha };


        // Is this a rotating particle? important for effect technique selection
        public bool IsRotating => RotateSpeed.HasValues;
        public bool IsAlignRotationToVel => AlignRotationToVelocity != 0f;

        public ParticleSettings Clone()
        {
            return (ParticleSettings)MemberwiseClone();
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
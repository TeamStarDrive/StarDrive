using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
#pragma warning disable 649

namespace Ship_Game.Graphics.Particles
{
    /// <summary>
    /// A pre-defined composition of multiple Particle emitters
    /// in order to create one big particle effect
    /// </summary>
    public class ParticleEffect
    {
        [StarDataType]
        public class ParticleEffectData
        {
            // Unique identifier of this ParticleEffect in ParticleEffects.yaml
            [StarData] public readonly string Name;

            // if > 0, generates random spray velocities in a loop
            // see `RandomizedSpray` to control the random spray
            // if you set this to 20, effect.Update() will be called 20 times, so be careful!!
            [StarData] public readonly int RandomizedSprayParticles;

            // if `RandomizedSprayParticles` > 0, then this controls the maximum
            // magnitudes of spray particles. Default value = [ [-50,-50,-50], [50,50,50] ]
            [StarData] public readonly Vector3[] RandomizedSpray = { new Vector3(-50, -50, -50), new Vector3(50, 50, 50) };

            [StarData] public readonly ParticleEmitterData[] Emitters;

            public ParticleManager Manager;
            public bool IsDisposed; // for hot-loading
        }

        [StarDataType]
        public class ParticleEmitterData
        {
            // name of the Particle effect defined in Particles.yaml
            [StarData] public readonly string Particle; // eg "ThrustEffect"

            // Rate of particles emitted per second
            [StarData] public readonly float Rate = 10.0f;

            // a global scale for this emitter, default = 1.0
            [StarData] public readonly float Scale = 1.0f;

            // overriding color source for this particle effect, default is White color (inherit Particle color)
            // valid values: Default, EmpireColor, ThrustColor0, ThrustColor1; @see Graphics/Particles/ParticleEffect.cs
            [StarData] public ColorSource ColorSource = ColorSource.Default;

            // overriding Color for this particle effect, default is null
            [StarData] public Color? Color;

            // scales emitter spray velocity, default = 1.0
            // how much velocity is being inherited from emitter spray, 1=100%, 0=0%
            // eg, a missile has thrust exhaust at specific spray velocity
            // 1.0 = particle moves with emitter spray (is spraying)
            // 0.0 = particle does not spray
            [StarData] public float InheritSprayVelocity = 1.0f;

            // scales movement velocity, default = 1.0
            // how much object movement is being inherited from emitter, 1=100%, 0=0%
            // eg. a missile is flying through space,
            // 1.0 = particle moves with the missile,
            // 0.0 = stay behind like a trail
            [StarData] public float InheritMoveVelocity = 1.0f;

            // local offset in relation to velocity direction
            [StarData] public Vector3 LocalOffset;

            // changes the emitter position in integer increments
            // so you can make this same emitter create particles from multiple locations
            // values are always rounded to integers!
            // Ex: [-1, +1] --> [LocalOffset*-1,LocalOffset*0,LocalOffset*1,  LocalOffset*-1,LocalOffset*0,LocalOffset*1,  ...]            [StarData] public Range PositionIncrements;
            [StarData] public Range PositionIncrements;

            // always recalculated
            public float TimeBetweenParticles;

            public int PositionIncrementMin;
            public int PositionIncrementMax;

            public bool IsDisposed; // for hot-loading
        }

        public enum ColorSource
        {
            Default, // Color.White
            EmpireColor, // Empire.EmpireColor
            ThrustColor0, // Empire.ThrustColor0
            ThrustColor1 // Empire.ThrustColor1
        }

        public class EmitterState
        {
            public ParticleEmitterData Data;
            public IParticle Particle;
            float TimeLeftOver;
            readonly Color Color;
            int PositionIncrement;

            public override string ToString() => $"Emitter {Data.Particle} Rate:{Data.Rate} Scale:{Data.Scale}";

            public EmitterState(ParticleEmitterData ed, IParticle particle)
            {
                // set defaults
                ed.TimeBetweenParticles = 1f / ed.Rate;
                ed.PositionIncrementMin = (int)Math.Round(ed.PositionIncrements.Min);
                ed.PositionIncrementMax = (int)Math.Round(ed.PositionIncrements.Max);
                Data = ed;
                Particle = particle;
                PositionIncrement = Data.PositionIncrementMin;
            }

            public EmitterState(EmitterState es, GameplayObject context)
            {
                Data = es.Data;
                Particle = es.Particle;
                Color = GetColor(context);
                PositionIncrement = es.PositionIncrement;
            }

            Color GetColor(GameplayObject context)
            {
                if (Data.ColorSource != ColorSource.Default && context != null)
                {
                    var loyalty = context.GetLoyalty();
                    if (loyalty != null)
                    {
                        switch (Data.ColorSource)
                        {
                            case ColorSource.EmpireColor: return loyalty.EmpireColor;
                            case ColorSource.ThrustColor0: return loyalty.ThrustColor0;
                            case ColorSource.ThrustColor1: return loyalty.ThrustColor1;
                        }
                    }
                }
                else if (Data.Color.HasValue)
                {
                    return Data.Color.Value;
                }
                return Color.White;
            }

            public void Update(
                float timeStep,
                in Vector3 prevPos,
                in Vector3 newPos,
                in Vector3 moveVel,
                in Vector3 sprayVel,
                float scale)
            {
                float timeToSpend = TimeLeftOver + timeStep;
                float currentTime = -TimeLeftOver;
                float timeBetweenParticles = Data.TimeBetweenParticles;
                float totalScale = scale * Data.Scale;

                // sum up the velocities
                Vector3 v = default;
                if (Data.InheritMoveVelocity != 0f)
                    v += moveVel * Data.InheritMoveVelocity;
                if (Data.InheritSprayVelocity != 0f)
                    v += sprayVel * Data.InheritSprayVelocity;

                while (timeToSpend >= timeBetweenParticles)
                {
                    currentTime += timeBetweenParticles;
                    timeToSpend -= timeBetweenParticles;
                    float relTime = currentTime / timeStep;

                    Vector3 offset = Data.LocalOffset;
                    if (Data.PositionIncrementMin != Data.PositionIncrementMax)
                    {
                        offset *= PositionIncrement;
                        ++PositionIncrement;
                        if (PositionIncrement > Data.PositionIncrementMax)
                            PositionIncrement = Data.PositionIncrementMin;
                    }

                    Vector3 pos = prevPos.LerpTo(newPos, relTime);

                    if (offset != Vector3.Zero)
                    {
                        Vector3 dir = v != Vector3.Zero ? v.Normalized() : new Vector3(0, -1, 0); // -Y is UP in universe
                        pos += dir.RightVector(new Vector3(0, 0, -1)) * offset.X;
                        pos += dir * offset.Y;
                    }

                    Particle.AddParticle(pos, v, totalScale, Color);
                }
                TimeLeftOver = timeToSpend;
            }
        }

        public string Name => Data.Name;
        readonly GameplayObject Context;
        public ParticleEffectData Data { get; private set; }
        public EmitterState[] Emitters { get; private set; }
        Vector3 PrevPos;

        public override string ToString() => $"ParticleEffect {Name} Emitters:{Emitters.Length}";

        // initialize a new template
        public ParticleEffect(ParticleEffectData data, ParticleManager manager)
        {
            Data = data;
            Data.Manager = manager;
            var emitters = new Array<EmitterState>();

            for (int i = 0; i < data.Emitters.Length; ++i)
            {
                ParticleEmitterData ed = data.Emitters[i];
                IParticle p = manager.GetParticleOrNull(ed.Particle);
                if (p != null)
                {
                    emitters.Add(new EmitterState(ed, p));
                }
                else
                {
                    Log.Error($"ParticleEffect {data.Name} emitter[{i}].Particle='{ed.Particle}' not found!");
                }
            }

            // since we want to show Emitters[0] as topmost, we must reverse the array
            emitters.Reverse();
            Emitters = emitters.ToArray();
        }

        // clone an instance based on an existing template
        public ParticleEffect(ParticleEffect template, in Vector3 initialPos, GameplayObject context)
        {
            Context = context;
            Data = template.Data;
            PrevPos = initialPos;
            Emitters = CreateEmitters(template);
        }

        EmitterState[] CreateEmitters(ParticleEffect template)
        {
            var emitters = new EmitterState[template.Emitters.Length];
            for (int i = 0; i < emitters.Length; ++i)
            {
                emitters[i] = new EmitterState(template.Emitters[i], Context);
            }
            return emitters;
        }

        public void Dispose()
        {
            Data.IsDisposed = true;
            foreach (ParticleEmitterData ed in Data.Emitters)
                ed.IsDisposed = true;
        }

        bool ReloadAfterDisposed()
        {
            ParticleEffect template = Data.Manager.GetEffectTemplate(Data.Name);
            if (template == null) // effect was removed
                return false;

            // set latest metadata and reset all emitters
            Data = template.Data;
            Emitters = CreateEmitters(template);
            return true;
        }

        /// <param name="timeStep">Fixed Simulation TimeStep</param>
        /// <param name="newPos">Latest position for the emitter</param>
        /// <param name="scale">Size scale of the effect</param>
        public void Update(FixedSimTime timeStep, in Vector3 newPos, float scale = 1f)
        {
            // no randomized spray, use sprayVelocity = Zero
            // randomized spray, use sprayVelocity = One so we inherit from effect settings
            Vector3 sprayVel = Data.RandomizedSprayParticles <= 0 ? Vector3.Zero : Vector3.One;
            Update(timeStep, newPos, sprayVel, scale);
        }

        /// <param name="timeStep">Fixed Simulation TimeStep</param>
        /// <param name="newPos">Latest position for the emitter</param>
        /// <param name="sprayVel">Emitter Spray velocity, to make particles spray out in particular direction</param>
        /// <param name="scale">Size scale of the effect</param>
        public void Update(FixedSimTime timeStep, in Vector3 newPos, in Vector3 sprayVel, float scale = 1f)
        {
            float fixedStep = timeStep.FixedTime;
            if (fixedStep > 0f)
            {
                if (Data.IsDisposed && !ReloadAfterDisposed())
                    return; // effect is not available right now, wait until hot-load user corrects the issue

                Vector3 moveVel = (newPos - PrevPos) / fixedStep;

                int sprayCount = Data.RandomizedSprayParticles;
                if (sprayCount <= 0)
                {
                    for (int i = 0; i < Emitters.Length; ++i)
                    {
                        Emitters[i].Update(fixedStep, PrevPos, newPos, moveVel, sprayVel, scale);
                    }
                }
                else
                {
                    Vector3 sprayMin = Data.RandomizedSpray[0];
                    Vector3 sprayMax = Data.RandomizedSpray[Data.RandomizedSpray.Length-1];

                    for (int j = 0; j < sprayCount; ++j)
                    {
                        var randomSpray = sprayVel * RandomMath.Vector3D(sprayMin, sprayMax);
                        for (int i = 0; i < Emitters.Length; ++i)
                        {
                            Emitters[i].Update(fixedStep, PrevPos, newPos, moveVel, randomSpray, scale);
                        }
                    }
                }
            }
            PrevPos = newPos;
        }
    }
}

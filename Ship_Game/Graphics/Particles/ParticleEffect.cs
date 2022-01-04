using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
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
            [StarData] public readonly string Name;
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
            [StarData] public ColorSource ColorSource = ColorSource.Default;

            // scales emitter velocity, default = 1.0
            [StarData] public float VelocityScale = 1.0f;

            // local offset in relation to velocity direction
            [StarData] public Vector3 LocalOffset;

            // multiplies LocalOffset in increments and then repeats from 0
            // ex: multiplier=3 --> [LocalOffset*0, LocalOffset*1, LocalOffset*2, LocalOffset*0, ...]
            [StarData] public int IncrementalOffsetMultiplier;

            // always recalculated
            public float TimeBetweenParticles;

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
            int IncrementCounter;

            public override string ToString() => $"Emitter {Data.Particle} Rate:{Data.Rate} Scale:{Data.Scale}";

            public EmitterState(ParticleEmitterData ed, IParticle particle)
            {
                Data = ed;
                Particle = particle;
            }

            public EmitterState(EmitterState es, GameplayObject context)
            {
                Data = es.Data;
                Particle = es.Particle;
                Color = GetColorFromContext(Data.ColorSource, context);
            }

            static Color GetColorFromContext(ColorSource source, GameplayObject context)
            {
                if (source != ColorSource.Default && context != null)
                {
                    var loyalty = context.GetLoyalty();
                    if (loyalty != null)
                    {
                        switch (source)
                        {
                            case ColorSource.EmpireColor: return loyalty.EmpireColor;
                            case ColorSource.ThrustColor0: return loyalty.ThrustColor0;
                            case ColorSource.ThrustColor1: return loyalty.ThrustColor1;
                        }
                    }
                }
                return Color.White;
            }

            public void Update(float timeStep, in Vector3 prevPos, in Vector3 newPos, in Vector3 vel, float scale)
            {
                float timeToSpend = TimeLeftOver + timeStep;
                float currentTime = -TimeLeftOver;
                float timeBetweenParticles = Data.TimeBetweenParticles;
                float totalScale = scale * Data.Scale;

                Vector3 v;
                if (Data.VelocityScale == 1f)
                    v = vel;
                else if (Data.VelocityScale == 0f)
                    v = Vector3.Zero;
                else
                    v = vel * Data.VelocityScale;

                while (timeToSpend >= timeBetweenParticles)
                {
                    currentTime += timeBetweenParticles;
                    timeToSpend -= timeBetweenParticles;
                    float relTime = currentTime / timeStep;

                    Vector3 offset = Data.LocalOffset;
                    if (Data.IncrementalOffsetMultiplier != 0)
                    {
                        offset *= IncrementCounter;
                        ++IncrementCounter;
                        if (IncrementCounter >= Data.IncrementalOffsetMultiplier)
                            IncrementCounter = 0;
                    }

                    Vector3 pos = prevPos.LerpTo(newPos, relTime);

                    if (offset != Vector3.Zero)
                    {
                        pos += vel.Normalized() * offset;
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
                    ed.TimeBetweenParticles = 1f / ed.Rate;
                    emitters.Add(new EmitterState(ed, p));
                }
                else
                {
                    Log.Error($"ParticleEffect {data.Name} emitter[{i}].Particle='{ed.Particle}' not found!");
                }
            }

            Emitters = emitters.ToArray();
        }

        // clone an instance based on an existing template
        public ParticleEffect(ParticleEffect template, in Vector3 initialPos, GameplayObject context)
        {
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

        public void Update(FixedSimTime timeStep, in Vector3 newPos, float scale = 1f)
        {
            float fixedStep = timeStep.FixedTime;
            if (fixedStep > 0f)
            {
                if (Data.IsDisposed && !ReloadAfterDisposed())
                    return; // effect is not available right now, wait until hot-load user corrects the issue

                Vector3 vel = (newPos - PrevPos) / fixedStep;
                for (int i = 0; i < Emitters.Length; ++i)
                {
                    Emitters[i].Update(fixedStep, PrevPos, newPos, vel, scale);
                }
            }
            PrevPos = newPos;
        }

        public void Update(FixedSimTime timeStep, in Vector3 newPos, in Vector3 velocity, float scale = 1f)
        {
            float fixedStep = timeStep.FixedTime;
            if (fixedStep > 0f)
            {
                if (Data.IsDisposed && !ReloadAfterDisposed())
                    return; // effect is not available right now, wait until hot-load user corrects the issue

                for (int i = 0; i < Emitters.Length; ++i)
                {
                    Emitters[i].Update(fixedStep, PrevPos, newPos, velocity, scale);
                }
            }
            PrevPos = newPos;
        }
    }
}

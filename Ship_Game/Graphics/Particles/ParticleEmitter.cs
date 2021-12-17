using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Graphics.Particles;

namespace Ship_Game
{
    public class ParticleEmitter
    {
        readonly IParticleSystem ParticleSystem;
        readonly float TimeBetweenParticles;
        Vector3 PreviousPosition;
        float TimeLeftOver;

        // Use ParticleSystem NewEmitter() instead
        internal ParticleEmitter(IParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition)
        {
            ParticleSystem       = particleSystem;
            TimeBetweenParticles = 1f / particlesPerSecond;
            PreviousPosition     = initialPosition;
        }

        public void Update(float elapsedTime, Vector3 newPosition)
        {
            Update(elapsedTime, newPosition, 0, 0, 0);
        }

        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity)
        {
            Update(elapsedTime, newPosition, zVelocity, 0, 0);
        }

        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity, float jitter)
        {
            Update(elapsedTime, newPosition, zVelocity, 0, jitter);
        }
        
        public void Update(float elapsedTime)
        {
            Update(elapsedTime, PreviousPosition);
        }

        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity, float zAxisPos, float jitter)
        {
            if (elapsedTime > 0f)
            {
                Vector3 velocity = newPosition - PreviousPosition;
                velocity.Z += zVelocity;
                velocity /= elapsedTime;
                float timeToSpend = TimeLeftOver + elapsedTime;
                float currentTime = -TimeLeftOver;
                while (timeToSpend > TimeBetweenParticles)
                {
                    currentTime += TimeBetweenParticles;
                    timeToSpend -= TimeBetweenParticles;
                    float mu = currentTime / elapsedTime;
                    Vector3 pos = Vector3.Lerp(PreviousPosition, newPosition, mu);
                    pos.Z += zAxisPos;

                    if (jitter > 0)
                    {
                        pos.X += RandomMath2.RandomBetween(-jitter, jitter);
                        pos.Y += RandomMath2.RandomBetween(-jitter, jitter);
                        pos.Z += RandomMath2.RandomBetween(-jitter, jitter);
                        jitter *= 0.75f;
                    }
                    ParticleSystem.AddParticle(pos, velocity);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }

        public void Update(float elapsedTime, Vector3 newPosition, float scale, Color color)
        {
            if (elapsedTime > 0f)
            {
                Vector3 velocity = (newPosition - PreviousPosition) / elapsedTime;
                float timeToSpend = TimeLeftOver + elapsedTime;
                float currentTime = -TimeLeftOver;                
                while (timeToSpend > TimeBetweenParticles)
                {
                    currentTime += TimeBetweenParticles;
                    timeToSpend -= TimeBetweenParticles;
                    float relTime = currentTime / elapsedTime;
                    Vector3 pos = Vector3.Lerp(PreviousPosition, newPosition, relTime);
                    ParticleSystem.AddParticle(pos, velocity, scale, color);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }

        public void UpdateProjectileTrail(float elapsedTime, Vector3 newPosition, Vector2 pVel)
        {
            if (elapsedTime > 0f)
            {
                Vector3 velocity = pVel.ToVec3();
                float timeToSpend = TimeLeftOver + elapsedTime;
                float currentTime = - TimeLeftOver;

                while (timeToSpend > TimeBetweenParticles)
                {
                    currentTime = currentTime + TimeBetweenParticles;
                    timeToSpend = timeToSpend - TimeBetweenParticles;
                    float mu = currentTime / elapsedTime;
                    Vector3 position = Vector3.Lerp(PreviousPosition, newPosition, mu);
                    ParticleSystem.AddParticle(position, velocity);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }
    }
}
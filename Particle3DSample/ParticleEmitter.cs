using Microsoft.Xna.Framework;
using Ship_Game;

namespace Particle3DSample
{
    public class ParticleEmitter
    {
        private readonly ParticleSystem ParticleSystem;
        private readonly float TimeBetweenParticles;
        private Vector3 PreviousPosition;
        private float TimeLeftOver;
        
        

        // Use ParticleSystem NewEmitter() instead
        internal ParticleEmitter(ParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition)
        {
            ParticleSystem       = particleSystem;
            TimeBetweenParticles = 1f / particlesPerSecond;
            PreviousPosition     = initialPosition;        
        }
        public void Update(float elapsedTime, Vector3 newPosition) => Update(elapsedTime, newPosition, 0, 0, 0);
        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity) => Update(elapsedTime, newPosition, zVelocity, 0, 0);
        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity, float jitter) 
            => Update(elapsedTime, newPosition, zVelocity, 0, jitter);
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
                    float mu     = currentTime / elapsedTime;
                    Vector3 position = Vector3.Lerp(PreviousPosition, newPosition, mu);
                    position.Z      += zAxisPos;
                    if ( jitter > 0)
                    {                       
                        position.X += RandomMath2.RandomBetween(-jitter, jitter);
                        position.Y += RandomMath2.RandomBetween(-jitter, jitter);
                        position.Z += RandomMath2.RandomBetween(-jitter, jitter);
                        jitter *=.75f;
                    }
                    ParticleSystem.AddParticleThreadA(position, velocity);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }

        public void Update(float elapsedTime)
        {
            Update(elapsedTime, PreviousPosition);
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
                    ParticleSystem.AddParticleThreadA(position, velocity);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }
    }
}
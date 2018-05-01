using Microsoft.Xna.Framework;
using System;
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

        public void Update(float elapsedTime, Vector3 newPosition, float zVelocity = 0, float zAxisPos = 0)
        {
            if (elapsedTime > 0f)
            {
                newPosition.Z += zAxisPos;
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
                    Vector3 position = Vector3.Lerp(PreviousPosition, newPosition, mu);
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
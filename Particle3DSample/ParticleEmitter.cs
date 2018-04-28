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
        private float Scale = 1;

        // Use ParticleSystem NewEmitter() instead
        internal ParticleEmitter(ParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition, float scale =1)
        {
            ParticleSystem       = particleSystem;
            TimeBetweenParticles = 1f / particlesPerSecond;
            PreviousPosition     = initialPosition;
            Scale                = scale;
        }

        public void Update(float elapsedTime, Vector3 newPosition)
        {
            if (elapsedTime > 0f)
            {
                Vector3 velocity  = (newPosition - PreviousPosition) / elapsedTime;
                float timeToSpend = TimeLeftOver + elapsedTime;
                float currentTime = -TimeLeftOver;
                while (timeToSpend > TimeBetweenParticles)
                {
                    currentTime += TimeBetweenParticles;
                    timeToSpend -= TimeBetweenParticles;
                    float mu = currentTime / elapsedTime;
                    Vector3 position = Vector3.Lerp(PreviousPosition, newPosition, mu);
                    ParticleSystem.AddParticleThreadA(position, velocity, Scale);
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
                    ParticleSystem.AddParticleThreadA(position, velocity, Scale);
                }
                TimeLeftOver = timeToSpend;
            }
            PreviousPosition = newPosition;
        }
    }
}
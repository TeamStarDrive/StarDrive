using Microsoft.Xna.Framework;
using System;

namespace Particle3DSample
{
	public class ParticleEmitter
	{
		private readonly ParticleSystem ParticleSystem;
		private readonly float TimeBetweenParticles;
		private Vector3 PreviousPosition;
		private float TimeLeftOver;

		public ParticleEmitter(ParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition)
		{
			ParticleSystem       = particleSystem;
			TimeBetweenParticles = 1f / particlesPerSecond;
			PreviousPosition     = initialPosition;
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
					ParticleSystem.AddParticleThreadA(position, velocity);
				}
				TimeLeftOver = timeToSpend;
			}
			PreviousPosition = newPosition;
		}

		public void UpdateProjectileTrail(float elapsedTime, Vector3 newPosition, Vector3 pVel)
		{
			if (elapsedTime > 0f)
			{
				float timeToSpend = TimeLeftOver + elapsedTime;
				float currentTime = - TimeLeftOver;
				while (timeToSpend > TimeBetweenParticles)
				{
					currentTime = currentTime + TimeBetweenParticles;
					timeToSpend = timeToSpend - TimeBetweenParticles;
					float mu = currentTime / elapsedTime;
					Vector3.Lerp(PreviousPosition, newPosition, mu);
					ParticleSystem.AddParticleThreadA(newPosition, pVel);
				}
				TimeLeftOver = timeToSpend;
			}
			PreviousPosition = newPosition;
		}
	}
}
using Microsoft.Xna.Framework;
using System;

namespace Particle3DSample
{
	public class ParticleEmitter
	{
		private ParticleSystem particleSystem;
		private float timeBetweenParticles;
		private Vector3 previousPosition;
		private float timeLeftOver;

		public ParticleEmitter(ParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition)
		{
			this.particleSystem = particleSystem;
			timeBetweenParticles = 1f / particlesPerSecond;
			previousPosition = initialPosition;
		}

		public void Update(float elapsedTime, Vector3 newPosition)
		{
			if (elapsedTime > 0f)
			{
				Vector3 velocity = (newPosition - previousPosition) / elapsedTime;
				float timeToSpend = timeLeftOver + elapsedTime;
				float currentTime = -timeLeftOver;
				while (timeToSpend > timeBetweenParticles)
				{
					currentTime = currentTime + timeBetweenParticles;
					timeToSpend = timeToSpend - timeBetweenParticles;
					float mu = currentTime / elapsedTime;
					Vector3 position = Vector3.Lerp(previousPosition, newPosition, mu);
					particleSystem.AddParticleThreadA(position, velocity);
				}
				timeLeftOver = timeToSpend;
			}
			previousPosition = newPosition;
		}

		public void UpdateProjectileTrail(float elapsedTime, Vector3 newPosition, Vector3 pVel)
		{
			if (elapsedTime > 0f)
			{
				float timeToSpend = timeLeftOver + elapsedTime;
				float currentTime = - timeLeftOver;
				while (timeToSpend > timeBetweenParticles)
				{
					currentTime = currentTime + timeBetweenParticles;
					timeToSpend = timeToSpend - timeBetweenParticles;
					float mu = currentTime / elapsedTime;
					Vector3.Lerp(previousPosition, newPosition, mu);
					particleSystem.AddParticleThreadA(newPosition, pVel);
				}
				timeLeftOver = timeToSpend;
			}
			previousPosition = newPosition;
		}
	}
}
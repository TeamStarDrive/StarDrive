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
			this.timeBetweenParticles = 1f / particlesPerSecond;
			this.previousPosition = initialPosition;
		}

		public void Update(float elapsedTime, Vector3 newPosition)
		{
			if (elapsedTime > 0f)
			{
				Vector3 velocity = (newPosition - this.previousPosition) / elapsedTime;
				float timeToSpend = this.timeLeftOver + elapsedTime;
				float currentTime = -this.timeLeftOver;
				while (timeToSpend > this.timeBetweenParticles)
				{
					currentTime = currentTime + this.timeBetweenParticles;
					timeToSpend = timeToSpend - this.timeBetweenParticles;
					float mu = currentTime / elapsedTime;
					Vector3 position = Vector3.Lerp(this.previousPosition, newPosition, mu);
					this.particleSystem.AddParticleThreadA(position, velocity);
				}
				this.timeLeftOver = timeToSpend;
			}
			this.previousPosition = newPosition;
		}

		public void UpdateProjectileTrail(float elapsedTime, Vector3 newPosition, Vector3 pVel)
		{
			if (elapsedTime > 0f)
			{
				Vector3 vector3 = (newPosition - this.previousPosition) / elapsedTime;
				float timeToSpend = this.timeLeftOver + elapsedTime;
				float currentTime = -this.timeLeftOver;
				while (timeToSpend > this.timeBetweenParticles)
				{
					currentTime = currentTime + this.timeBetweenParticles;
					timeToSpend = timeToSpend - this.timeBetweenParticles;
					float mu = currentTime / elapsedTime;
					Vector3.Lerp(this.previousPosition, newPosition, mu);
					this.particleSystem.AddParticleThreadA(newPosition, pVel);
				}
				this.timeLeftOver = timeToSpend;
			}
			this.previousPosition = newPosition;
		}
	}
}
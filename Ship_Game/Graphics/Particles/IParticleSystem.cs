using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles
{
    public interface IParticleSystem : IDisposable
    {
        string Name { get; }

        // Is this particle system enabled to update and draw ?
        bool IsEnabled { get; set; }

        bool EnableDebug { get; set; }

        // Max number of active particles
        int MaxParticles { get; }

        // Current number of active particles
        int ActiveParticles { get; }

        // ParticleSystem is maxed out?
        bool IsOutOfParticles { get; }

        // Spawn a new particle
        void AddParticle(in Vector3 position);
        void AddParticle(in Vector3 position, float scale);
        void AddParticle(in Vector3 position, in Vector3 velocity);
        void AddParticle(in Vector3 position, in Vector3 velocity, float scale, Color color);

        // Spawn multiple particles in a loop
        void AddMultipleParticles(int numParticles, in Vector3 position);
        void AddMultipleParticles(int numParticles, in Vector3 position, float scale);
        void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity);
        void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity, float scale, Color color);

        // Create a new emitter with 3D position
        ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition);
        ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition, float scale);

        // Create a new emitter with 2D position
        ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition);
        ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition, float scale);

        // Update the particles
        void Update(DrawTimes elapsed);

        // Draw the entire particle system
        void Draw(in Matrix view, in Matrix projection, bool nearView);
    }
}

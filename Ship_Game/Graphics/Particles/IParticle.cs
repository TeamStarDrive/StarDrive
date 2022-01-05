using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles
{
    public interface IParticle : IDisposable
    {
        string Name { get; }

        // Unique ID for fast indexing via ParticleManager
        int ParticleId { get; }

        // Is this particle enabled to update and draw ?
        bool IsEnabled { get; set; }

        bool EnableDebug { get; set; }

        // Max number of active particles
        int MaxParticles { get; }

        // Number of Active particles on the GPU and also particles pending upload to GPU
        int ActiveParticles { get; }

        // Number of particles pending upload to GPU
        int NewParticles { get; }

        // Number of unallocated particles
        int FreeParticles { get; }

        // Number of retired particles which are still on the GPU, but will be removed in 1-2 frames
        int RetiredParticles { get; }

        // Current number of allocated particles, typically slightly larger than ActiveParticles
        int AllocatedParticles { get; }

        // TRUE if no more particles can be allocated due to MaxParticles
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
        // @param totalSimulationTime Total seconds elapsed since simulation started
        void Update(float totalSimulationTime);

        // Draw the entire particle system
        void Draw(in Matrix view, in Matrix projection, bool nearView);
    }
}

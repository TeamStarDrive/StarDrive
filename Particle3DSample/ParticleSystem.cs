using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Ship_Game;

namespace Particle3DSample
{
    public sealed class ParticleSystem : IDisposable
    {
        private readonly string SettingsName;
        private ParticleSettings Settings;
        private readonly GameContentManager Content;

        private Effect particleEffect;
        private EffectParameter effectViewParameter;
        private EffectParameter effectProjectionParameter;
        private EffectParameter effectViewportHeightParameter;
        private EffectParameter effectTimeParameter;

        private ParticleVertex[] particles;
        private DynamicVertexBuffer vertexBuffer;
        private VertexDeclaration vertexDeclaration;

        private int firstActiveParticle;
        private int firstNewParticle;
        private int firstFreeParticle;
        private int firstRetiredParticle;
        private float currentTime;
        private int drawCounter;

        private static Random randomA = new Random();
        private static Random randomB = new Random();
        private readonly GraphicsDevice GraphicsDevice;

        public ParticleSystem(Game1 game, GameContentManager content, string settingsName, GraphicsDevice device)
        {
            GraphicsDevice = device;
            Content        = content;
            SettingsName   = settingsName;
            LoadContent();
        }

        private void AddNewParticlesToVertexBuffer()
        {
            const int stride = 32;
            if (firstNewParticle >= firstFreeParticle)
            {
                vertexBuffer.SetData(firstNewParticle * stride, particles, firstNewParticle, (int)particles.Length - firstNewParticle, stride, SetDataOptions.NoOverwrite);
                if (firstFreeParticle > 0)
                {
                    vertexBuffer.SetData(0, particles, 0, firstFreeParticle, stride, SetDataOptions.NoOverwrite);
                }
            }
            else
            {
                vertexBuffer.SetData(firstNewParticle * stride, particles, firstNewParticle, firstFreeParticle - firstNewParticle, stride, SetDataOptions.NoOverwrite);
            }
            firstNewParticle = firstFreeParticle;
        }

        public void AddParticleThreadA(Vector3 position, Vector3 velocity)
        {
            int nextFreeParticle = firstFreeParticle + 1;
            if (nextFreeParticle >= (int)particles.Length)
            {
                nextFreeParticle = 0;
            }
            if (nextFreeParticle == firstRetiredParticle)
            {
                return;
            }
            velocity = velocity * Settings.EmitterVelocitySensitivity;
            float horizontalVelocity = MathHelper.Lerp(Settings.MinHorizontalVelocity, Settings.MaxHorizontalVelocity, (float)ParticleSystem.randomA.NextDouble());
            double horizontalAngle = ParticleSystem.randomA.NextDouble() * 6.28318548202515;
            velocity.X = velocity.X + horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Z = velocity.Z + horizontalVelocity * (float)Math.Sin(horizontalAngle);
            velocity.Y = velocity.Y + MathHelper.Lerp(Settings.MinVerticalVelocity, Settings.MaxVerticalVelocity, (float)ParticleSystem.randomA.NextDouble());
            Color randomValues = new Color((byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255));
            particles[firstFreeParticle].Position = position;
            particles[firstFreeParticle].Velocity = velocity;
            particles[firstFreeParticle].Random = randomValues;
            particles[firstFreeParticle].Time = currentTime;
            firstFreeParticle = nextFreeParticle;
        }

        public void AddParticleThreadB(Vector3 position, Vector3 velocity)
        {
            int nextFreeParticle = firstFreeParticle + 1;
            if (nextFreeParticle >= (int)particles.Length)
            {
                nextFreeParticle = 0;
            }
            if (nextFreeParticle == firstRetiredParticle)
            {
                return;
            }
            velocity = velocity * Settings.EmitterVelocitySensitivity;
            float horizontalVelocity = MathHelper.Lerp(Settings.MinHorizontalVelocity, Settings.MaxHorizontalVelocity, (float)randomB.NextDouble());
            double horizontalAngle = randomB.NextDouble() * 6.28318548202515;
            velocity.X = velocity.X + horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Z = velocity.Z + horizontalVelocity * (float)Math.Sin(horizontalAngle);
            velocity.Y = velocity.Y + MathHelper.Lerp(Settings.MinVerticalVelocity, Settings.MaxVerticalVelocity, (float)randomB.NextDouble());
            Color randomValues = new Color((byte)randomB.Next(255), (byte)randomB.Next(255), (byte)randomB.Next(255), (byte)randomB.Next(255));
            particles[firstFreeParticle].Position = position;
            particles[firstFreeParticle].Velocity = velocity;
            particles[firstFreeParticle].Random = randomValues;
            particles[firstFreeParticle].Time = currentTime;
            firstFreeParticle = nextFreeParticle;
        }

        public void Draw(GameTime gameTime)
        {
            GraphicsDevice device = GraphicsDevice;
            if (vertexBuffer.IsContentLost)
            {
                vertexBuffer.SetData(particles);
            }
            if (firstNewParticle != firstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }
            if (firstActiveParticle != firstFreeParticle)
            {
                SetParticleRenderStates(device.RenderState);
                effectViewportHeightParameter.SetValue(device.Viewport.Height);
                effectTimeParameter.SetValue(currentTime);
                device.Vertices[0].SetSource(vertexBuffer, 0, 32);
                device.VertexDeclaration = vertexDeclaration;
                particleEffect.Begin();
                foreach (EffectPass pass in particleEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    if (firstActiveParticle >= firstFreeParticle)
                    {
                        device.DrawPrimitives(PrimitiveType.PointList, firstActiveParticle, (int)particles.Length - firstActiveParticle);
                        if (firstFreeParticle > 0)
                        {
                            device.DrawPrimitives(PrimitiveType.PointList, 0, firstFreeParticle);
                        }
                    }
                    else
                    {
                        device.DrawPrimitives(PrimitiveType.PointList, firstActiveParticle, firstFreeParticle - firstActiveParticle);
                    }
                    pass.End();
                }
                particleEffect.End();
                device.RenderState.PointSpriteEnable = false;
                device.RenderState.DepthBufferWriteEnable = true;
            }
            ParticleSystem particleSystem = this;
            particleSystem.drawCounter = particleSystem.drawCounter + 1;
        }

        private void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                if (drawCounter - (int)particles[firstRetiredParticle].Time < 3)
                    return;

                ++firstRetiredParticle;
                if (firstRetiredParticle < particles.Length)
                    continue;
                firstRetiredParticle = 0;
            }
        }

        public void LoadContent()
        {
            Settings = Content.Load<ParticleSettings>(SettingsName);
            particles = new ParticleVertex[Settings.MaxParticles];
            LoadParticleEffect();
            vertexDeclaration = new VertexDeclaration(GraphicsDevice, ParticleVertex.VertexElements);
            int size = 32 * particles.Length;
            vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, size, BufferUsage.WriteOnly | BufferUsage.Points);
        }

        private void LoadParticleEffect()
        {
            var effect = Content.Load<Effect>("3DParticles/ParticleEffect");
            particleEffect = effect.Clone(GraphicsDevice);
            EffectParameterCollection parameters = particleEffect.Parameters;
            effectViewParameter                  = parameters["View"];
            effectProjectionParameter            = parameters["Projection"];
            effectViewportHeightParameter        = parameters["ViewportHeight"];
            effectTimeParameter                  = parameters["CurrentTime"];
            parameters["Duration"].SetValue((float)Settings.Duration.TotalSeconds);
            parameters["DurationRandomness"].SetValue(Settings.DurationRandomness);
            parameters["Gravity"].SetValue(Settings.Gravity);
            parameters["EndVelocity"].SetValue(Settings.EndVelocity);
            parameters["MinColor"].SetValue(Settings.MinColor.ToVector4());
            parameters["MaxColor"].SetValue(Settings.MaxColor.ToVector4());
            parameters["RotateSpeed"].SetValue(new Vector2(Settings.MinRotateSpeed, Settings.MaxRotateSpeed));
            parameters["StartSize"].SetValue(new Vector2(Settings.MinStartSize, Settings.MaxStartSize));
            parameters["EndSize"].SetValue(new Vector2(Settings.MinEndSize, Settings.MaxEndSize));
            Texture2D texture = Content.Load<Texture2D>("3DParticles/" + Settings.TextureName);
            parameters["Texture"].SetValue(texture);

            string techniqueName;
            if (Settings.Duration.TotalSeconds != 6.66)
            {
                techniqueName = (Settings.MinRotateSpeed != 0f || Settings.MaxRotateSpeed != 0f ? "RotatingParticles" : "NonRotatingParticles");
            }
            else
            {
                techniqueName = "StaticParticles";
            }
            particleEffect.CurrentTechnique = particleEffect.Techniques[techniqueName];
        }

        private void RetireActiveParticles()
        {
            float particleDuration = (float)Settings.Duration.TotalSeconds;
            if (particleDuration == 6.66f)
                return;
            while (firstActiveParticle != firstNewParticle)
            {
                float particleAge = currentTime - particles[firstActiveParticle].Time;
                if (particleAge < particleDuration && particleAge > 0f)
                    return;
                particles[firstActiveParticle++].Time = drawCounter;
                if (firstActiveParticle < particles.Length)
                    continue;
                firstActiveParticle = 0;
            }
        }

        public void SetCamera(Matrix view, Matrix projection)
        {
            effectViewParameter.SetValue(view);
            effectProjectionParameter.SetValue(projection);
        }

        private void SetParticleRenderStates(RenderState renderState)
        {
            renderState.PointSpriteEnable      = true;
            renderState.PointSizeMax           = 256f;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Settings.SourceBlend;
            renderState.DestinationBlend       = Settings.DestinationBlend;
            renderState.AlphaTestEnable        = true;
            renderState.AlphaFunction          = CompareFunction.Greater;
            renderState.ReferenceAlpha         = 0;
            renderState.DepthBufferEnable      = true;
            renderState.DepthBufferWriteEnable = false;
        }

        public void UnloadContent()
        {
            particles = null;
            vertexDeclaration.Dispose();
            vertexBuffer.Dispose();
        }

        public void Update(GameTime gameTime)
        {
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            RetireActiveParticles();
            FreeRetiredParticles();

            if (firstActiveParticle == firstFreeParticle)    currentTime = 0f;
            if (firstRetiredParticle == firstActiveParticle) drawCounter = 0;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose(ref vertexBuffer);
            vertexDeclaration?.Dispose(ref vertexDeclaration);
            GC.SuppressFinalize(this);
        }

        ~ParticleSystem()
        {
            vertexBuffer?.Dispose(ref vertexBuffer);
            vertexDeclaration?.Dispose(ref vertexDeclaration);
        }
    }
}
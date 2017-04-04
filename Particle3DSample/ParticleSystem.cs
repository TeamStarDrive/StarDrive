using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Ship_Game;

namespace Particle3DSample
{
    public sealed class ParticleSystem : IDisposable
    {
        private readonly string SettingsName;
        private ParticleSettings Settings;
        private readonly GameContentManager Content;

        private Effect ParticleEffect;
        private EffectParameter EffectViewParameter;
        private EffectParameter EffectProjectionParameter;
        private EffectParameter EffectViewportHeightParameter;
        private EffectParameter EffectTimeParameter;

        private ParticleVertex[] Particles;
        private DynamicVertexBuffer VertexBuffer;
        private VertexDeclaration VertexDeclaration;

        private int FirstActiveParticle;
        private int FirstNewParticle;
        private int FirstFreeParticle;
        private int FirstRetiredParticle;
        private float CurrentTime;
        private int DrawCounter;

        private static readonly Random RandomA = new Random();
        private static readonly Random RandomB = new Random();
        private readonly GraphicsDevice GraphicsDevice;

        private struct ParticleVertex
        {
            public const int SizeInBytes = 32;

            public Vector3 Position;
            public Vector3 Velocity;
            public Color Random;
            public float Time;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                new VertexElement(0, 24, VertexElementFormat.Color,   VertexElementMethod.Default, VertexElementUsage.Color, 0),
                new VertexElement(0, 28, VertexElementFormat.Single,  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0)
            };
        }

        public ParticleSystem(GameContentManager content, string settingsName, GraphicsDevice device)
        {
            GraphicsDevice = device;
            Content        = content;
            SettingsName   = settingsName;
            LoadContent();
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, Vector3 initialPosition)
        {
            return new ParticleEmitter(this, particlesPerSecond, initialPosition);
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, Vector2 initialCenter, float initialZ = 0f)
        {
            return new ParticleEmitter(this, particlesPerSecond, new Vector3(initialCenter, initialZ));
        }

        private void AddNewParticlesToVertexBuffer()
        {
            const int stride = 32;
            if (FirstNewParticle >= FirstFreeParticle)
            {
                VertexBuffer.SetData(FirstNewParticle * stride, Particles, FirstNewParticle, 
                    Particles.Length - FirstNewParticle, stride, SetDataOptions.NoOverwrite);
                if (FirstFreeParticle > 0)
                {
                    VertexBuffer.SetData(0, Particles, 0, FirstFreeParticle, stride, SetDataOptions.NoOverwrite);
                }
            }
            else
            {
                VertexBuffer.SetData(FirstNewParticle * stride, Particles, FirstNewParticle, 
                    FirstFreeParticle - FirstNewParticle, stride, SetDataOptions.NoOverwrite);
            }
            FirstNewParticle = FirstFreeParticle;
        }

        private void AddParticleThread(Random random, Vector3 position, Vector3 velocity)
        {
            int nextFreeParticle = FirstFreeParticle + 1;
            if (nextFreeParticle >= Particles.Length)
                nextFreeParticle = 0;

            if (nextFreeParticle == FirstRetiredParticle)
                return;

            velocity *= Settings.EmitterVelocitySensitivity;
            float horizontalVelocity = Settings.MinHorizontalVelocity.LerpTo(Settings.MaxHorizontalVelocity, (float)random.NextDouble());
            double horizontalAngle = random.NextDouble() * 6.28318548202515;
            velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Z += horizontalVelocity * (float)Math.Sin(horizontalAngle);
            velocity.Y += Settings.MinVerticalVelocity.LerpTo(Settings.MaxVerticalVelocity, (float)random.NextDouble());
            Particles[FirstFreeParticle].Position = position;
            Particles[FirstFreeParticle].Velocity = velocity;
            Particles[FirstFreeParticle].Random   = new Color((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
            Particles[FirstFreeParticle].Time     = CurrentTime;
            FirstFreeParticle = nextFreeParticle;
        }

        public void AddParticleThreadA(Vector3 position, Vector3 velocity) => AddParticleThread(RandomA, position, velocity);
        public void AddParticleThreadB(Vector3 position, Vector3 velocity) => AddParticleThread(RandomB, position, velocity);

        public void Draw(GameTime gameTime)
        {
            GraphicsDevice device = GraphicsDevice;
            if (VertexBuffer.IsContentLost)
            {
                VertexBuffer.SetData(Particles);
            }
            if (FirstNewParticle != FirstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }
            if (FirstActiveParticle != FirstFreeParticle)
            {
                SetParticleRenderStates(device.RenderState);
                EffectViewportHeightParameter.SetValue(device.Viewport.Height);
                EffectTimeParameter.SetValue(CurrentTime);
                device.Vertices[0].SetSource(VertexBuffer, 0, 32);
                device.VertexDeclaration = VertexDeclaration;
                ParticleEffect.Begin();
                foreach (EffectPass pass in ParticleEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    if (FirstActiveParticle >= FirstFreeParticle)
                    {
                        device.DrawPrimitives(PrimitiveType.PointList, FirstActiveParticle, Particles.Length - FirstActiveParticle);
                        if (FirstFreeParticle > 0)
                        {
                            device.DrawPrimitives(PrimitiveType.PointList, 0, FirstFreeParticle);
                        }
                    }
                    else
                    {
                        device.DrawPrimitives(PrimitiveType.PointList, FirstActiveParticle, FirstFreeParticle - FirstActiveParticle);
                    }
                    pass.End();
                }
                ParticleEffect.End();
                device.RenderState.PointSpriteEnable = false;
                device.RenderState.DepthBufferWriteEnable = true;
            }
            ++DrawCounter;
        }

        private void FreeRetiredParticles()
        {
            while (FirstRetiredParticle != FirstActiveParticle)
            {
                if (DrawCounter - (int)Particles[FirstRetiredParticle].Time < 3)
                    return;

                ++FirstRetiredParticle;
                if (FirstRetiredParticle >= Particles.Length)
                    FirstRetiredParticle = 0;
            }
        }

        public void LoadContent()
        {
            Settings = Content.Load<ParticleSettings>(SettingsName);
            Particles = new ParticleVertex[Settings.MaxParticles];
            LoadParticleEffect();
            VertexDeclaration = new VertexDeclaration(GraphicsDevice, ParticleVertex.VertexElements);
            int size = 32 * Particles.Length;
            VertexBuffer = new DynamicVertexBuffer(GraphicsDevice, size, BufferUsage.WriteOnly | BufferUsage.Points);
        }

        private void LoadParticleEffect()
        {
            var effect = Content.Load<Effect>("3DParticles/ParticleEffect");
            ParticleEffect = effect.Clone(GraphicsDevice);
            EffectParameterCollection parameters = ParticleEffect.Parameters;
            EffectViewParameter                  = parameters["View"];
            EffectProjectionParameter            = parameters["Projection"];
            EffectViewportHeightParameter        = parameters["ViewportHeight"];
            EffectTimeParameter                  = parameters["CurrentTime"];
            parameters["Duration"].SetValue((float)Settings.Duration.TotalSeconds);
            parameters["DurationRandomness"].SetValue(Settings.DurationRandomness);
            parameters["Gravity"].SetValue(Settings.Gravity);
            parameters["EndVelocity"].SetValue(Settings.EndVelocity);
            parameters["MinColor"].SetValue(Settings.MinColor.ToVector4());
            parameters["MaxColor"].SetValue(Settings.MaxColor.ToVector4());
            parameters["RotateSpeed"].SetValue(new Vector2(Settings.MinRotateSpeed, Settings.MaxRotateSpeed));
            parameters["StartSize"].SetValue(new Vector2(Settings.MinStartSize, Settings.MaxStartSize));
            parameters["EndSize"].SetValue(new Vector2(Settings.MinEndSize, Settings.MaxEndSize));

            Texture2D texture = ResourceManager.LoadTexture("3DParticles/" + Settings.TextureName);
            parameters["Texture"].SetValue(texture);

            string techniqueName;
            if (Settings.Duration.TotalSeconds < 6.66)
            {
                techniqueName = (Settings.MinRotateSpeed > 0f || Settings.MaxRotateSpeed > 0f) ? "RotatingParticles" : "NonRotatingParticles";
            }
            else
            {
                techniqueName = "StaticParticles";
            }
            ParticleEffect.CurrentTechnique = ParticleEffect.Techniques[techniqueName];
        }

        private void RetireActiveParticles()
        {
            float particleDuration = (float)Settings.Duration.TotalSeconds;
            if (particleDuration == 6.66f) // wtf?? "StaticParticles" ?
                return;
            while (FirstActiveParticle != FirstNewParticle)
            {
                float particleAge = CurrentTime - Particles[FirstActiveParticle].Time;
                if (particleAge < particleDuration && particleAge > 0f)
                    return;
                Particles[FirstActiveParticle++].Time = DrawCounter;
                if (FirstActiveParticle >= Particles.Length)
                    FirstActiveParticle = 0;
            }
        }

        public void SetCamera(Matrix view, Matrix projection)
        {
            EffectViewParameter.SetValue(view);
            EffectProjectionParameter.SetValue(projection);
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
            Particles = null;
            VertexDeclaration.Dispose();
            VertexBuffer.Dispose();
        }

        public void Update(GameTime gameTime)
        {
            CurrentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            RetireActiveParticles();
            FreeRetiredParticles();

            if (FirstActiveParticle == FirstFreeParticle)    CurrentTime = 0f;
            if (FirstRetiredParticle == FirstActiveParticle) DrawCounter = 0;
        }

        public void Dispose()
        {
            VertexBuffer?.Dispose(ref VertexBuffer);
            VertexDeclaration?.Dispose(ref VertexDeclaration);
            GC.SuppressFinalize(this);
        }

        ~ParticleSystem()
        {
            VertexBuffer?.Dispose(ref VertexBuffer);
            VertexDeclaration?.Dispose(ref VertexDeclaration);
        }
    }
}
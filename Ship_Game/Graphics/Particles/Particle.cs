using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using SDGraphics;
using Ship_Game.Data;
using Ship_Game.Graphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Utils;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    public sealed class Particle : IParticle
    {
        // Settings class controls the appearance and animation of this particle system.
        ParticleSettings Settings;

        // For loading the effect file and particle texture.
        readonly GameContentManager Content;

        // Custom effect for drawing particles. This computes the particle
        // animation entirely in the vertex shader: no per-particle CPU work required!
        Effect ParticleEffect;
        
        // Shortcuts for accessing frequently changed effect parameters.
        EffectParameter EffectViewParameter;
        EffectParameter EffectProjectionParameter;
        EffectParameter EffectViewportScaleParameter;
        EffectParameter EffectTimeParameter;
        
        // An array of particles, treated as a circular queue.
        ParticleVertex[] Particles;
        
        // A vertex buffer holding our particles. This contains the same data as
        // the particles array, but copied across to where the GPU can access it.
        DynamicVertexBuffer VertexBuffer;

        // Index buffer turns sets of four vertices into particle quads (pairs of triangles).
        IndexBuffer IndexBuffer;

        VertexDeclaration VertexDeclaration;

        // The particles array and vertex buffer are treated as a circular queue.
        // Initially, the entire contents of the array are free, because no particles
        // are in use. When a new particle is created, this is allocated from the
        // beginning of the array. If more than one particle is created, these will
        // always be stored in a consecutive block of array elements. Because all
        // particles last for a similar amount of time, old particles will always be
        // removed in order from the start of this active particle region, so the
        // active and free regions will never be intermingled. Because the queue is
        // circular, there can be times when the active particle region wraps from the
        // end of the array back to the start. The queue uses modulo arithmetic to
        // handle these cases. For instance with a four entry queue we could have:
        //
        //      0
        //      1 - first active particle
        //      2 
        //      3 - first free particle
        //
        // In this case, particles 1 and 2 are active, while 3 and 4 are free.
        // Using modulo arithmetic we could also have:
        //
        //      0
        //      1 - first free particle
        //      2 
        //      3 - first active particle
        //
        // Here, 3 and 0 are active, while 1 and 2 are free.
        //
        // But wait! The full story is even more complex.
        //
        // When we create a new particle, we add them to our managed particles array.
        // We also need to copy this new data into the GPU vertex buffer, but we don't
        // want to do that straight away, because setting new data into a vertex buffer
        // can be an expensive operation. If we are going to be adding several particles
        // in a single frame, it is faster to initially just store them in our managed
        // array, and then later upload them all to the GPU in one single call. So our
        // queue also needs a region for storing new particles that have been added to
        // the managed array but not yet uploaded to the vertex buffer.
        //
        // Another issue occurs when old particles are retired. The CPU and GPU run
        // asynchronously, so the GPU will often still be busy drawing the previous
        // frame while the CPU is working on the next frame. This can cause a
        // synchronization problem if an old particle is retired, and then immediately
        // overwritten by a new one, because the CPU might try to change the contents
        // of the vertex buffer while the GPU is still busy drawing the old data from
        // it. Normally the graphics driver will take care of this by waiting until
        // the GPU has finished drawing inside the VertexBuffer.SetData call, but we
        // don't want to waste time waiting around every time we try to add a new
        // particle! To avoid this delay, we can specify the SetDataOptions.NoOverwrite
        // flag when we write to the vertex buffer. This basically means "I promise I
        // will never try to overwrite any data that the GPU might still be using, so
        // you can just go ahead and update the buffer straight away". To keep this
        // promise, we must avoid reusing vertices immediately after they are drawn.
        //
        // So in total, our queue contains four different regions:
        //
        // Vertices between firstActiveParticle and firstNewParticle are actively
        // being drawn, and exist in both the managed particles array and the GPU
        // vertex buffer.
        //
        // Vertices between firstNewParticle and firstFreeParticle are newly created,
        // and exist only in the managed particles array. These need to be uploaded
        // to the GPU at the start of the next draw call.
        //
        // Vertices between firstFreeParticle and firstRetiredParticle are free and
        // waiting to be allocated.
        //
        // Vertices between firstRetiredParticle and firstActiveParticle are no longer
        // being drawn, but were drawn recently enough that the GPU could still be
        // using them. These need to be kept around for a few more frames before they
        // can be reallocated.
        // [ _ _ _ |fRetired x x x x x |fActive a a a a |fNew n n n |fFree _ _ _ _ _ ]
        int FirstRetiredParticle; // this is the first particle which was completely retired
        int FirstActiveParticle; // active range is [FirstActiveParticle, FirstFreeParticle)
        int FirstNewParticle; // exists only in CPU memory
        int FirstFreeParticle; // can allocate new particle here

        // keep track of exact number of allocated particles
        // this makes it easier to check for MaxParticles overflow
        // Retired+Active+New
        public int AllocatedParticles { get; private set; }

        // This is the actual particle count, which is Particles.Length / 4
        public int MaxParticles { get; }

        // Number of Particles that have been added and are being drawn
        // This includes retired particles and particles that are in queue to be added to gpu
        // [ _ _ _ |fRetired x x x x x |fActive a a a a |fNew n n n |fFree _ _ _ _ _ ]
        public int RetiredParticles => FirstActiveParticle - FirstRetiredParticle;
        public int ActiveParticles  => FirstFreeParticle - FirstActiveParticle;
        public int NewParticles     => FirstFreeParticle - FirstNewParticle;
        public int FreeParticles    => MaxParticles - AllocatedParticles;
        public bool IsOutOfParticles => AllocatedParticles == MaxParticles;

        // Store the current time, in seconds.
        float CurrentTime;

        // Count how many times Draw has been called. This is used to know
        // when it is safe to retire old particles back into the free list.
        int DrawCounter;

        public string Name { get; set; }

        public int ParticleId { get; }

        /// <summary>
        /// Can be used to disable this particle system (for debugging purposes or otherwise)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// If true, particle shader will generate debug markers around particles
        /// </summary>
        public bool EnableDebug { get; set; }

        readonly ThreadSafeRandom Random = new ThreadSafeRandom();
        readonly GraphicsDevice GraphicsDevice;

        readonly object Sync = new object();
        Array<ParticleVertex> PendingParticles = new Array<ParticleVertex>();
        Array<ParticleVertex> BackBuffer = new Array<ParticleVertex>();

        struct ParticleVertex
        {
            // Stores which corner of the particle quad this vertex represents.
            public Short2 Corner;
            // Stores the starting position of the particle.
            public Vector3 Position;
            // Stores the starting velocity of the particle.
            public Vector3 Velocity;
            // Overriding multiplicative color value for this particle
            public Color Color;
            // Four random values, used to make each particle look slightly different.
            public Color Random;
            // Extra scaling multiplier added to the particle
            public float Scale;
            // The time (in seconds) at which this particle was created.
            public float Time;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0,  VertexElementFormat.Short2,  VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 4,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 1),
                new VertexElement(0, 16, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                new VertexElement(0, 28, VertexElementFormat.Color,   VertexElementMethod.Default, VertexElementUsage.Color, 0),
                new VertexElement(0, 32, VertexElementFormat.Color,   VertexElementMethod.Default, VertexElementUsage.Color, 1),
                new VertexElement(0, 36, VertexElementFormat.Single,  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, 40, VertexElementFormat.Single,  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1)
            };

            public const int SizeInBytes = 44;
        }

        public Particle(GameContentManager content, ParticleSettings settings, int id)
        {
            Name = settings.Name;
            GraphicsDevice = content.Device;
            Content = content;
            ParticleId = id;

            MaxParticles = settings.MaxParticles;
            Settings = settings.Clone();
            Settings.MaxParticles = MaxParticles; 

            LoadParticleEffect();

            VertexDeclaration = new VertexDeclaration(GraphicsDevice, ParticleVertex.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(GraphicsDevice, ParticleVertex.SizeInBytes*MaxParticles*4,
                                                   BufferUsage.WriteOnly);

            // Allocate the particle array, and fill in the corner fields (which never change).
            Particles = new ParticleVertex[MaxParticles * 4];

            for (int i = 0; i < MaxParticles; i++)
            {
                Particles[i * 4 + 0].Corner = new Short2(-1, -1); // TopLeft
                Particles[i * 4 + 1].Corner = new Short2(+1, -1); // TopRight
                Particles[i * 4 + 2].Corner = new Short2(+1, +1); // BotRight
                Particles[i * 4 + 3].Corner = new Short2(-1, +1); // BotLeft
            }

            // Create and populate the index buffer.
            uint[] indices = new uint[MaxParticles * 6];

            for (int i = 0; i < MaxParticles; i++)
            {
                indices[i * 6 + 0] = (uint)(i * 4 + 0);
                indices[i * 6 + 1] = (uint)(i * 4 + 1);
                indices[i * 6 + 2] = (uint)(i * 4 + 2);

                indices[i * 6 + 3] = (uint)(i * 4 + 0);
                indices[i * 6 + 4] = (uint)(i * 4 + 2);
                indices[i * 6 + 5] = (uint)(i * 4 + 3);
            }

            IndexBuffer = new IndexBuffer(GraphicsDevice, typeof(uint), indices.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indices);
        }

        void LoadParticleEffect()
        {
            Effect effect = Settings.GetEffect(Content);

            // If we have several particle systems, the content manager will return
            // a single shared effect instance to them all. But we want to preconfigure
            // the effect with parameters that are specific to this particular
            // particle system. By cloning the effect, we prevent one particle system
            // from stomping over the parameter settings of another.
            ParticleEffect = effect.Clone(GraphicsDevice);

            EffectParameterCollection parameters = ParticleEffect.Parameters;

            // Look up shortcuts for parameters that change every frame.
            EffectViewParameter           = parameters["View"];
            EffectProjectionParameter     = parameters["Projection"];
            EffectViewportScaleParameter  = parameters["ViewportScale"];
            EffectTimeParameter           = parameters["CurrentTime"];

            // Set the values of parameters that do not change.
            parameters["Duration"].SetValue((float)Settings.Duration.TotalSeconds);
            parameters["DurationRandomness"].SetValue(Settings.DurationRandomness);
            parameters["AlignRotationToVelocity"].SetValue(Settings.AlignRotationToVelocity);
            parameters["EndVelocity"].SetValue(Settings.EndVelocity);

            Color[] startColor = Settings.StartColorRange;
            Color[] endColor = Settings.EndColorRange ?? startColor;
            parameters["StartMinColor"].SetValue(startColor[0].ToVector4());
            parameters["StartMaxColor"].SetValue(startColor[startColor.Length-1].ToVector4());
            parameters["EndMinColor"].SetValue(endColor[0].ToVector4());
            parameters["EndMaxColor"].SetValue(endColor[endColor.Length-1].ToVector4());

            // To reach endColor at relativeAge=EndColorTime
            // Set EndColorTimeMul multiplier to 1/EndColorTime so it reaches EndColor at that relativeAge
            // Ex: EndColorTime=0.75, EndColorTimeMul=1/0.75=1.33, EndColor is reached at relativeAge*1.33, so faster
            parameters["EndColorTimeMul"].SetValue(1f / Settings.EndColorTime);

            parameters["RotateSpeed"].SetValue(new Vector2(Settings.RotateSpeed.Min, Settings.RotateSpeed.Max));

            Range startSize = Settings.StartEndSize[0];
            Range endSize = Settings.StartEndSize[Settings.StartEndSize.Length - 1];
            parameters["StartSize"].SetValue(new Vector2(startSize.Min, startSize.Max));
            parameters["EndSize"].SetValue(new Vector2(endSize.Min, endSize.Max));
            
            Texture2D texture = Settings.GetTexture(Content);
            ParticleEffect.Parameters["Texture"].SetValue(texture);

            string technique = "FullDynamicParticles";
            switch (Settings.Static)
            {
                case false when Settings.IsAlignRotationToVel:
                    technique = "DynamicAlignRotationToVelocityParticles";
                    break;
                case false when !Settings.IsRotating:
                    technique = "DynamicNonRotatingParticles";
                    break;
                case true when Settings.IsRotating:
                    technique = "StaticRotatingParticles";
                    break;
                case true when !Settings.IsRotating:
                    technique = "StaticNonRotatingParticles";
                    break;
            }

            ParticleEffect.CurrentTechnique = ParticleEffect.Techniques[technique];
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition)
        {
            return new ParticleEmitter(this, particlesPerSecond, scale: 1f, initialPosition);
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition, float scale)
        {
            return new ParticleEmitter(this, particlesPerSecond, scale: scale, initialPosition);
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition)
        {
            return new ParticleEmitter(this, particlesPerSecond, scale: 1f, new Vector3(initialPosition, 0f));
        }

        public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition, float scale)
        {
            return new ParticleEmitter(this, particlesPerSecond, scale: scale, new Vector3(initialPosition, 0f));
        }

        /// <summary>
        /// Updates the Particle queue by retiring aged particles and submitting pending particles
        /// </summary>
        public void Update(float totalSimulationTime)
        {
            var particles = Particles;
            if (particles == null || !IsEnabled)
                return;

            CurrentTime = totalSimulationTime;

            int numAllocated = AllocatedParticles;
            if (numAllocated > 0 && !Settings.Static)
            {
                RetireActiveParticles(particles);
                AllocatedParticles = numAllocated = FreeRetiredParticles(particles, numAllocated);
            }

            lock (Sync)
            {
                // swap PendingParticles into BackBuffer
                (BackBuffer, PendingParticles) = (PendingParticles, BackBuffer);
            }

            int count = BackBuffer.Count;
            if (count > 0)
            {
                ParticleVertex[] backBuffer = BackBuffer.GetInternalArrayItems();

                for (int i = 0; i < count && numAllocated < MaxParticles; ++i)
                {
                    int nextFreeIdx = FirstFreeParticle++ % MaxParticles;
                    ++numAllocated;

                    ref ParticleVertex srcVertex = ref backBuffer[i];
                    ref ParticleVertex dstVertex0 = ref particles[nextFreeIdx * 4];
                    ref ParticleVertex dstVertex1 = ref particles[nextFreeIdx * 4 + 1];
                    ref ParticleVertex dstVertex2 = ref particles[nextFreeIdx * 4 + 2];
                    ref ParticleVertex dstVertex3 = ref particles[nextFreeIdx * 4 + 3];
                    dstVertex0 = srcVertex;
                    dstVertex1 = srcVertex;
                    dstVertex2 = srcVertex;
                    dstVertex3 = srcVertex;
                    dstVertex0.Corner = new Short2(-1, -1); // TopLeft
                    dstVertex1.Corner = new Short2(+1, -1); // TopRight
                    dstVertex2.Corner = new Short2(+1, +1); // BotRight
                    dstVertex3.Corner = new Short2(-1, +1); // BotLeft
                }

                AllocatedParticles = numAllocated;
                BackBuffer.Clear();
            }
            // if we have no more particles allocated, reset all counters to mitigate potential overflow
            else if (numAllocated == 0 && FirstActiveParticle != 0)
            {
                // If we let our timer go on increasing for ever, it would eventually
                // run out of floating point precision, at which point the particles
                // would render incorrectly. An easy way to prevent this is to notice
                // that the time value doesn't matter when NO particles are being drawn,
                // so we can reset it back to zero any time the active queue is empty.
                CurrentTime = 0f;
                FirstRetiredParticle = 0;
                FirstActiveParticle = 0;
                FirstNewParticle = 0;
                FirstFreeParticle = 0;
                DrawCounter = 0;
            }
        }

        /// <summary>
        /// Checking when active particles have reached the end of their life and
        /// move old particles from the active area of the queue to the retired section.
        /// </summary>
        void RetireActiveParticles(ParticleVertex[] particles)
        {
            float particleDuration = (float)Settings.Duration.TotalSeconds;

            while (FirstActiveParticle < FirstNewParticle)
            {
                // Is this particle old enough to retire?
                // We multiply the active particle index by four, because each
                // particle consists of a quad that is made up of four vertices.
                int idx = FirstActiveParticle % MaxParticles;
                ref ParticleVertex particle = ref particles[idx * 4];
                float particleAge = CurrentTime - particle.Time;
                if (particleAge < particleDuration)
                    break;

                // Remember the time at which we retired this particle.
                particle.Time = DrawCounter;

                // Move the particle from the active to the retired queue.
                ++FirstActiveParticle;
            }
        }

        /// <summary>
        /// Check if retired particles have been kept around long
        /// enough that we can be sure the GPU is no longer using them and move
        /// old particles from the retired area of the queue to the free section.
        /// </summary>
        /// <returns>New # of Allocated Particles</returns>
        int FreeRetiredParticles(ParticleVertex[] particles, int numAllocated)
        {
            while (numAllocated > 0 && FirstRetiredParticle < FirstActiveParticle)
            {
                // Has this particle been unused long enough that the GPU is sure to be finished with it?
                // We multiply the retired particle index by four, because each
                // particle consists of a quad that is made up of four vertices.
                int idx = FirstRetiredParticle % MaxParticles;
                int age = DrawCounter - (int)particles[idx * 4].Time;

                // The GPU is never supposed to get more than 2 frames behind the CPU.
                // We add 1 to that, just to be safe in case of buggy drivers that
                // might bend the rules and let the GPU get further behind.
                if (age < 3)
                    break;

                // Move the particle from the retired to the free queue.
                ++FirstRetiredParticle;
                --numAllocated;
            }
            return numAllocated;
        }

        public void Draw(in Matrix view, in Matrix projection, bool nearView)
        {
            var particles = Particles;
            if (particles == null || !IsEnabled || (!nearView && Settings.OnlyNearView))
                return;
            
            int firstActive = FirstActiveParticle;
            int firstNew = FirstNewParticle;
            int firstFree = FirstFreeParticle;

            // Restore the vertex buffer contents if the graphics device was lost.
            if (VertexBuffer.IsContentLost)
            {
                VertexBuffer.SetData(particles);
            }

            int activeIdx = firstActive % MaxParticles;
            int freeIdx = firstFree % MaxParticles;

            // If there are any particles waiting in the newly added queue,
            // we'd better upload them to the GPU ready for drawing.
            if (firstNew < firstFree)
            {
                int newIdx = firstNew % MaxParticles;
                AddNewParticlesToVertexBuffer(particles, newIdx, freeIdx);
                // Reset NewParticles pointer to the end of the queue
                FirstNewParticle = firstFree;
            }

            // If there are any active particles, draw them now!
            if (firstActive < firstFree)
            {
                GraphicsDevice device = GraphicsDevice;
                RenderStates.EnableAlphaBlend(device, Settings.SrcDstBlend[0], Settings.SrcDstBlend[1]);
                RenderStates.EnableAlphaTest(device, CompareFunction.Greater, referenceAlpha:0);
                RenderStates.DisableDepthWrite(device);

                EffectViewParameter.SetValue(view);
                EffectProjectionParameter.SetValue(projection);

                if (EnableDebug)
                {
                    RenderStates.DisableAlphaBlend(device);
                    RenderStates.DisableAlphaTest(device);
                }

                // Set an effect parameter describing the viewport size. This is
                // needed to convert particle sizes into screen space point sizes.
                EffectViewportScaleParameter.SetValue(new Vector2(0.5f / device.Viewport.AspectRatio, -0.5f));

                // Set an effect parameter describing the current time. All the vertex
                // shader particle animation is keyed off this value.
                EffectTimeParameter.SetValue(CurrentTime);
                
                // Set the particle vertex and index buffer.
                device.Vertices[0].SetSource(VertexBuffer, 0, ParticleVertex.SizeInBytes);
                device.Indices = IndexBuffer;
                device.VertexDeclaration = VertexDeclaration;

                ParticleEffect.Begin();
                foreach (EffectPass pass in ParticleEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    if (activeIdx < freeIdx)
                    {
                        // If the active particles are all in one consecutive range,
                        // we can draw them all in a single call.
                        int numParticles = (freeIdx - activeIdx);
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                                     activeIdx * 4, numParticles * 4, // 4 points
                                                     activeIdx * 6, numParticles * 2); // 2 triangles
                    }
                    else
                    {
                        int numParticles = (MaxParticles - activeIdx);
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                                     activeIdx * 4, numParticles * 4, // 4 points
                                                     activeIdx * 6, numParticles * 2); // 2 triangles
                        if (freeIdx > 0)
                        {
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                                         0, freeIdx * 4,
                                                         0, freeIdx * 2);
                        }
                    }
                    pass.End();
                }
                ParticleEffect.End();

                RenderStates.DisableAlphaTest(device);
                RenderStates.EnableDepthWrite(device);
            }
            ++DrawCounter;
        }

        /// <summary>
        /// Upload new particles from our managed array to the GPU vertex buffer.
        /// </summary>
        void AddNewParticlesToVertexBuffer(ParticleVertex[] particles, int newIdx, int freeIdx)
        {
            const int stride = ParticleVertex.SizeInBytes;

            if (newIdx < freeIdx)
            {
                // If the new particles are all in one consecutive range,
                // we can upload them all in a single call.
                int numParticles = freeIdx - newIdx;
                VertexBuffer.SetData(newIdx * stride * 4, particles,
                                     startIndex: newIdx * 4, elementCount: numParticles * 4,
                                     stride, SetDataOptions.NoOverwrite);
            }
            else
            {
                // If the new particle range wraps past the end of the queue
                // back to the start, we must split them over two upload calls.
                int numParticles = (MaxParticles - newIdx);
                VertexBuffer.SetData(newIdx * stride * 4, particles,
                                     startIndex: newIdx * 4, elementCount: numParticles * 4,
                                     stride, SetDataOptions.NoOverwrite);

                if (freeIdx > 0)
                {
                    VertexBuffer.SetData(0, particles,
                                         startIndex: 0, elementCount: freeIdx * 4,
                                         stride, SetDataOptions.NoOverwrite);
                }
            }
        }

        public void AddParticle(in Vector3 position, in Vector3 velocity, float scale, Color color)
        {
            // when Graphics device is reset, this particle system will be disposed
            // and Particles will be set to null
            if (Particles == null || !IsEnabled)
                return;

            // thread unsafe check: can we even add any particles?
            // consider half of the retired particles to be potentially free
            int maybeFreeParticles = (MaxParticles - AllocatedParticles) + (RetiredParticles / 2);
            if (maybeFreeParticles == 0)
                return;

            // Adjust the input velocity based on how much
            // this particle system wants to be affected by it.
            Vector3 v = velocity * Settings.InheritOwnerVelocity;

            ThreadSafeRandom random = Random;

            // Add in some random amount of horizontal velocity.
            Range randVelX = Settings.RandomVelocityXY[0];
            Range randVelY = Settings.RandomVelocityXY[1];
            float velX = randVelX.Min.LerpTo(randVelX.Max, random.Float());
            float velY = randVelY.Min.LerpTo(randVelY.Max, random.Float());

            if (Settings.AlignRandomVelocityXY)
            {
                // aligned to global camera in Universe
                Vector3 forward = (Settings.InheritOwnerVelocity == 0 ? velocity : v).Normalized();
                Vector3 right;
                if (forward == Vector3.Zero) // emitter is stationary, follow universe coordinates
                {
                    forward = new Vector3(0, -1, 0); // -Y is UP in universe
                    right = Vector3.UnitX; // since forward is pointing up, we must point +X = right
                }
                else
                {
                    right = forward.RightVector(new Vector3(0, 0, -1));
                }
                v += right * velX;
                v += forward * velY;
            }
            else
            {
                float horizontalAngle = random.Float() * RadMath.TwoPI;
                v.X += velX * RadMath.Cos(horizontalAngle);
                v.Z += velX * RadMath.Sin(horizontalAngle);
                v.Y += velY;
            }

            // Choose four random control values. These will be used by the vertex
            // shader to give each particle a different size, rotation, and color.
            var randomValues = new Color(random.Byte(), random.Byte(), random.Byte(), random.Byte());

            var vertex = new ParticleVertex()
            {
                Position = position,
                Velocity = v,
                Color = color,
                Random = randomValues,
                Scale = scale,
                Time = CurrentTime,
            };

            lock (Sync)
            {
                PendingParticles.Add(vertex);
            }
        }

        public void AddParticle(in Vector3 position)
        {
            AddParticle(position, Vector3.Zero, 1f, Color.White);
        }
        public void AddParticle(in Vector3 position, float scale)
        {
            AddParticle(position, Vector3.Zero, scale, Color.White);
        }
        public void AddParticle(in Vector3 position, in Vector3 velocity)
        {
            AddParticle(position, velocity, 1f, Color.White);
        }

        public void AddMultipleParticles(int numParticles, in Vector3 position)
        {
            AddMultipleParticles(numParticles, position, Vector3.Zero, 1f, Color.White);
        }
        public void AddMultipleParticles(int numParticles, in Vector3 position, float scale)
        {
            AddMultipleParticles(numParticles, position, Vector3.Zero, scale, Color.White);
        }
        public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity)
        {
            AddMultipleParticles(numParticles, position, velocity, 1f, Color.White);
        }
        public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity, float scale, Color color)
        {
            for (int i = 0; i < numParticles; ++i)
                AddParticle(position, velocity, scale, color);
        }

        public void Dispose()
        {
            Particles = null;
            VertexBuffer?.Dispose(ref VertexBuffer);
            IndexBuffer?.Dispose(ref IndexBuffer);
            VertexDeclaration?.Dispose(ref VertexDeclaration);
            GC.SuppressFinalize(this);
        }

        ~Particle()
        {
            VertexBuffer?.Dispose(ref VertexBuffer);
            IndexBuffer?.Dispose(ref IndexBuffer);
            VertexDeclaration?.Dispose(ref VertexDeclaration);
        }
    }
}
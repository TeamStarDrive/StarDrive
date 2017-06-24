// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SpriteManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework.Graphics;
using Mesh;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Acts as a resource manager for arrays and buffers used during sprite creation.
    /// </summary>
    public class SpriteManager : IManagerService
    {
        private readonly TrackingPool<RenderableMesh> MeshPool = new TrackingPool<RenderableMesh>();
        private DisposablePool<SpriteVertexBuffer> DisposablePool0 = new DisposablePool<SpriteVertexBuffer>();
        private IGraphicsDeviceService igraphicsDeviceService_0;
        private static readonly Type ThisType = typeof(SpriteManager);

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType => ThisType;

        /// <summary>
        /// Sets the order this manager is processed relative to other managers
        /// in the IManagerServiceProvider. Managers with lower processing order
        /// values are processed first.
        /// 
        /// In the case of BeginFrameRendering and EndFrameRendering, BeginFrameRendering
        /// is processed in the normal order (lowest order value to highest), however
        /// EndFrameRendering is processed in reverse order (highest to lowest) to ensure
        /// the first manager begun is the last one ended (FILO).
        /// </summary>
        public int ManagerProcessOrder { get; set; } = 100;

        /// <summary>Creates a new SpriteManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public SpriteManager(IGraphicsDeviceService graphicsdevicemanager)
        {
            this.igraphicsDeviceService_0 = graphicsdevicemanager;
        }

        /// <summary>
        /// Creates a new SpriteContainer instance for storing and rendering 2D sprites.
        /// </summary>
        /// <returns></returns>
        public SpriteContainer CreateSpriteContainer()
        {
            SpriteContainer spriteContainer = new SpriteContainer(this.igraphicsDeviceService_0.GraphicsDevice, this.MeshPool, this.DisposablePool0);
            spriteContainer.ObjectType = ObjectType.Dynamic;
            return spriteContainer;
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public void ApplyPreferences(ILightingSystemPreferences preferences)
        {
        }

        /// <summary>
        /// Removes all objects from the container. Commonly used while clearing the scene.
        /// </summary>
        public void Clear()
        {
            this.MeshPool.RecycleAllTracked();
            this.DisposablePool0.RecycleAllTracked();
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public void Unload()
        {
            this.MeshPool.Clear();
            this.DisposablePool0.Clear();
        }
    }
}

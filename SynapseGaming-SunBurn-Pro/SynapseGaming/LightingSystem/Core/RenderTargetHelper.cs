// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.RenderTargetHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Helper class for rendering to a texture. Provides automatic support for rendering
    /// reflection and refraction textures, as well as standard render-to-texture.
    /// </summary>
    public class RenderTargetHelper : IUnloadable, IManager, IRenderableManager
    {
        private SceneState sceneState_0 = new SceneState();
        private TargetType targetType_0 = TargetType.Standard;
        private Viewport viewport_0;
        private Plane plane_0 = new Plane();
        private int int_0;
        private int int_1;
        private int int_2;
        private SurfaceFormat surfaceFormat_0;
        private MultiSampleType multiSampleType_0;
        private int int_3;
        private RenderTargetUsage renderTargetUsage_0;
        private RenderTarget2D renderTarget2D_0;
        private DepthStencilBuffer depthStencilBuffer_0;
        private RenderTarget2D renderTarget2D_1;
        private RenderTarget2D renderTarget2D_2;
        private RenderTarget2D renderTarget2D_3;
        private RenderTarget2D renderTarget2D_4;
        private DepthStencilBuffer depthStencilBuffer_1;
        private Viewport viewport_1;

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>
        /// Scene rendering state used to render objects to this RenderTargetHelper. The state values
        /// may be different from those passed into BeginFrameRendering to accommodate reflection and refraction.
        /// </summary>
        public ISceneState SceneState => this.sceneState_0;

        /// <summary>
        /// Rendering preferences used to render objects to this RenderTargetHelper.
        /// </summary>
        public ILightingSystemPreferences Preferences { get; private set; } = (ILightingSystemPreferences) new LightingSystemPreferences();

        /// <summary>Creates a new RenderTargetHelper instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="type">Type of rendering to perform on the render target.</param>
        /// <param name="width">Render target width.</param>
        /// <param name="height">Render target height.</param>
        /// <param name="format">Render target format.</param>
        public RenderTargetHelper(IGraphicsDeviceService graphicsdevicemanager, TargetType type, int width, int height, SurfaceFormat format)
        {
            this.GraphicsDeviceManager = graphicsdevicemanager;
            this.targetType_0 = type;
            this.int_0 = width;
            this.int_1 = height;
            this.surfaceFormat_0 = format;
            this.int_2 = 1;
            this.multiSampleType_0 = MultiSampleType.None;
            this.int_3 = 0;
            this.renderTargetUsage_0 = LightingSystemManager.Instance.GetBestRenderTargetUsage();
        }

        /// <summary>Creates a new RenderTargetHelper instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="type">Type of rendering to perform on the render target.</param>
        /// <param name="width">Render target width.</param>
        /// <param name="height">Render target height.</param>
        /// <param name="numberlevels">Number of render target mipmap levels.</param>
        /// <param name="format">Render target format.</param>
        /// <param name="multisampletype">Render target multisample type.</param>
        /// <param name="multisamplequality">Render target multisample quality.</param>
        /// <param name="usage">Render target usage.</param>
        public RenderTargetHelper(IGraphicsDeviceService graphicsdevicemanager, TargetType type, int width, int height, int numberlevels, SurfaceFormat format, MultiSampleType multisampletype, int multisamplequality, RenderTargetUsage usage)
        {
            this.GraphicsDeviceManager = graphicsdevicemanager;
            this.targetType_0 = type;
            this.int_0 = width;
            this.int_1 = height;
            this.int_2 = numberlevels;
            this.surfaceFormat_0 = format;
            this.multiSampleType_0 = multisampletype;
            this.int_3 = multisamplequality;
            this.renderTargetUsage_0 = usage;
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            this.Preferences = preferences;
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public void Clear()
        {
            this.Preferences = new LightingSystemPreferences();
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public void Unload()
        {
            this.Clear();
            Disposable.Free(ref this.renderTarget2D_0);
            Disposable.Free(ref this.depthStencilBuffer_0);
        }

        /// <summary>
        /// Gets the texture containing the resulting rendered image.
        /// </summary>
        /// <returns></returns>
        public Texture2D GetTexture()
        {
            if (this.renderTarget2D_0 != null)
                return this.renderTarget2D_0.GetTexture();
            return null;
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        public void BeginFrameRendering(ISceneState state)
        {
            if (this.targetType_0 != TargetType.Standard)
                throw new Exception("Non standard targets require a world reflection plane, please use another overload for this method.");
            this.BeginFrameRendering(state, this.plane_0);
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        /// <param name="worldreflectionplane">World space plane used as the reflection surface.</param>
        public void BeginFrameRendering(ISceneState state, Plane worldreflectionplane)
        {
            this.BeginFrameRendering(state, worldreflectionplane, worldreflectionplane);
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        /// <param name="worldreflectionplane">World space plane used as the reflection surface.</param>
        /// <param name="worldclippingplane">World space plane used for object clipping.  This is normally
        /// the reflection plane, however providing a separate adjusted clipping plane can help remove artifacts
        /// where objects and the reflection surface intersect.</param>
        public void BeginFrameRendering(ISceneState state, Plane worldreflectionplane, Plane worldclippingplane)
        {
            GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
            if (this.renderTarget2D_0 == null)
            {
                this.renderTarget2D_0 = new RenderTarget2D(graphicsDevice, this.int_0, this.int_1, this.int_2, this.surfaceFormat_0, this.multiSampleType_0, this.int_3, this.renderTargetUsage_0);
                DepthFormat format = DepthFormat.Depth24;
                if (graphicsDevice.DepthStencilBuffer != null)
                    format = graphicsDevice.DepthStencilBuffer.Format;
                this.depthStencilBuffer_0 = new DepthStencilBuffer(graphicsDevice, this.int_0, this.int_1, format, this.multiSampleType_0, this.int_3);
                this.viewport_0.X = 0;
                this.viewport_0.Y = 0;
                this.viewport_0.Width = this.int_0;
                this.viewport_0.Height = this.int_1;
                this.viewport_0.MinDepth = 0.0f;
                this.viewport_0.MaxDepth = 1f;
            }
            this.renderTarget2D_1 = (RenderTarget2D) graphicsDevice.GetRenderTarget(0);
            this.renderTarget2D_2 = (RenderTarget2D) graphicsDevice.GetRenderTarget(1);
            this.renderTarget2D_3 = (RenderTarget2D) graphicsDevice.GetRenderTarget(2);
            this.renderTarget2D_4 = (RenderTarget2D) graphicsDevice.GetRenderTarget(3);
            this.depthStencilBuffer_1 = graphicsDevice.DepthStencilBuffer;
            this.viewport_1 = graphicsDevice.Viewport;
            graphicsDevice.SetRenderTarget(0, this.renderTarget2D_0);
            graphicsDevice.SetRenderTarget(1, null);
            graphicsDevice.SetRenderTarget(2, null);
            graphicsDevice.SetRenderTarget(3, null);
            graphicsDevice.DepthStencilBuffer = this.depthStencilBuffer_0;
            graphicsDevice.Viewport = this.viewport_0;
            if (this.targetType_0 != TargetType.Standard)
            {
                if (worldclippingplane.DotCoordinate(state.ViewToWorld.Translation) > 0.0)
                {
                    worldclippingplane.Normal *= -1f;
                    worldclippingplane.D *= -1f;
                }
                Plane plane = Plane.Transform(worldclippingplane, state.ViewProjection);
                graphicsDevice.ClipPlanes[0].Plane = plane;
                graphicsDevice.ClipPlanes[0].IsEnabled = true;
            }
            if (this.targetType_0 != TargetType.Reflection)
            {
                Matrix view = state.View;
                Matrix projection = SceneState.Projection;
                this.sceneState_0.BeginFrameRendering(ref view, ref projection, state.ElapsedTime, state.Environment, state.RenderingToScreen);
            }
            else
            {
                if (worldreflectionplane.DotCoordinate(state.ViewToWorld.Translation) > 0.0)
                {
                    worldreflectionplane.Normal *= -1f;
                    worldreflectionplane.D *= -1f;
                }
                Matrix view = Matrix.CreateReflection(worldreflectionplane) * state.View;
                Matrix projection = SceneState.Projection;
                this.sceneState_0.BeginFrameRendering(ref view, ref projection, state.ElapsedTime, state.Environment, state.RenderingToScreen);
            }
        }

        /// <summary>Finalizes rendering.</summary>
        public void EndFrameRendering()
        {
            GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
            graphicsDevice.SetRenderTarget(0, this.renderTarget2D_1);
            graphicsDevice.SetRenderTarget(1, this.renderTarget2D_2);
            graphicsDevice.SetRenderTarget(2, this.renderTarget2D_3);
            graphicsDevice.SetRenderTarget(3, this.renderTarget2D_4);
            graphicsDevice.DepthStencilBuffer = this.depthStencilBuffer_1;
            graphicsDevice.Viewport = this.viewport_1;
            if (this.targetType_0 == TargetType.Standard)
                return;
            graphicsDevice.ClipPlanes[0].IsEnabled = false;
        }

        /// <summary>Type of rendering to perform on the render target.</summary>
        public enum TargetType
        {
            Reflection,
            Refraction,
            Standard
        }
    }
}

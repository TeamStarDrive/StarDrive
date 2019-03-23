// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.PostProcessManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Manages all post processors and applies post processing effects to the scene.
    /// </summary>
    public class PostProcessManager : IUnloadable, IManager, IRenderableManager, IManagerService, IPostProcessManager
    {
        private static readonly SurfaceFormat[] Formats = { SurfaceFormat.Color, SurfaceFormat.Bgra1010102, SurfaceFormat.Rgba1010102, SurfaceFormat.Rgba32, SurfaceFormat.Rgba64, SurfaceFormat.HalfVector4, SurfaceFormat.Vector4 };
        private static List<SurfaceFormat> TextureSurfaceFormats = new List<SurfaceFormat>();
        private bool EnablePostProcess = true;
        private bool NeedsUpdate = true;
        private readonly List<IPostProcessor> ActivePostProcessors = new List<IPostProcessor>();
        private readonly GraphicsDeviceMonitor GraphicsDeviceMonitor0;
        private int ViewportWidth;
        private int ViewportHeight;
        private static SurfaceFormat[] SupportedFormats;

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType { get; } = SceneInterface.PostProcessManagerType;

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
        public int ManagerProcessOrder { get; set; } = 20;

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>
        /// Indicates all post processors initialized correctly and are compatible.
        /// </summary>
        public bool PostProcessorsAreCompatible { get; private set; } = true;

        internal static SurfaceFormat[] AllSupportedFormats { get; } = SupportedFormats;

        /// <summary>Creates a PostProcessManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public PostProcessManager(IGraphicsDeviceService graphicsdevicemanager)
        {
            GraphicsDeviceManager = graphicsdevicemanager;
            GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(GraphicsDeviceManager);
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            EnablePostProcess = preferences.PostProcessingDetail != DetailPreference.Off;
            for (int i = 0; i < ActivePostProcessors.Count; ++i)
                ActivePostProcessors[i].ApplyPreferences(preferences);
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        public virtual void BeginFrameRendering(ISceneState state)
        {
            if (!EnablePostProcess)
                return;
            BeginFrame();
            if (!PostProcessorsAreCompatible)
                return;
            for (int i = 0; i < ActivePostProcessors.Count; ++i)
                ActivePostProcessors[i].BeginFrameRendering(state);
        }

        /// <summary>Finalizes rendering and applies all post processing.</summary>
        public virtual void EndFrameRendering()
        {
            if (!PostProcessorsAreCompatible || !EnablePostProcess)
                return;
            Texture2D mastersource = null;
            Texture2D lastprocessorsource = null;
            for (int i = ActivePostProcessors.Count - 1; i >= 0; --i)
            {
                lastprocessorsource = this.ActivePostProcessors[i].EndFrameRendering(mastersource, lastprocessorsource);
                if (mastersource == null)
                    mastersource = lastprocessorsource;
            }
        }

        private void BeginFrame()
        {
            GraphicsDevice device = GraphicsDeviceManager.GraphicsDevice;
            Viewport viewport = device.Viewport;

            NeedsUpdate = NeedsUpdate || GraphicsDeviceMonitor0.Changed || ViewportWidth != viewport.Width || ViewportHeight != viewport.Height;
            if (!NeedsUpdate && SupportedFormats != null)
                return;
            NeedsUpdate = false;
            for (int i = 0; i < ActivePostProcessors.Count; ++i)
                ActivePostProcessors[i].Unload();
            ViewportWidth  = viewport.Width;
            ViewportHeight = viewport.Height;
            GraphicsDeviceSupport support = LightingSystemManager.Instance.GetGraphicsDeviceSupport(device);
            TextureSurfaceFormats.Clear();
            foreach (SurfaceFormat format in Formats)
            {
                GraphicsDeviceSupport.FormatSupport formatSupport = support.SurfaceFormat[format];
                if (formatSupport.RenderTarget && formatSupport.Texture && formatSupport.Blending)
                    TextureSurfaceFormats.Add(format);
            }
            SupportedFormats = TextureSurfaceFormats.ToArray();
            for (int i = 0; i < ActivePostProcessors.Count; ++i)
            {
                if (ActivePostProcessors[i].Initialize(TextureSurfaceFormats))
                {
                    if (i + 1 < ActivePostProcessors.Count)
                    {
                        RemoveUnsupportedFormats(TextureSurfaceFormats, ActivePostProcessors[i].SupportedSourceFormats);
                        if (TextureSurfaceFormats.Count < 1)
                        {
                            PostProcessorsAreCompatible = false;
                            return;
                        }
                    }
                }
                else
                {
                    PostProcessorsAreCompatible = false;
                    return;
                }
            }
            PostProcessorsAreCompatible = true;
        }

        private void RemoveUnsupportedFormats(List<SurfaceFormat> textureFormats, SurfaceFormat[] supportedFormats)
        {
            for (int i = 0; i < textureFormats.Count; ++i)
            {
                bool flag = false;
                for (int j = 0; j < supportedFormats.Length; ++j)
                {
                    if (textureFormats[i] == supportedFormats[j])
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    textureFormats.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>
        /// Adds a new post processor to the processing chain. The last processor added
        /// to the chain is the first to apply its visual effects.
        /// </summary>
        /// <param name="postprocessor"></param>
        public void AddPostProcessor(IPostProcessor postprocessor)
        {
            this.ActivePostProcessors.Add(postprocessor);
            this.NeedsUpdate = true;
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public virtual void Clear()
        {
        }

        /// <summary>
        /// Disposes any graphics resources used internally by this object, and removes
        /// resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public virtual void Unload()
        {
            for (int index = 0; index < this.ActivePostProcessors.Count; ++index)
                this.ActivePostProcessors[index].Unload();
            this.ActivePostProcessors.Clear();
            this.NeedsUpdate = true;
        }
    }
}

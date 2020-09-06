// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Forward.LightingSystemGameComponent
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows.Forward;

namespace SynapseGaming.LightingSystem.Rendering.Forward
{
    /// <summary>
    /// Provides a self-contained SunBurn rendering environment. For quickly adding
    /// SunBurn to a project with minimal changes and nearly pure XNA code interaction.
    /// </summary>
    public class LightingSystemGameComponent : DrawableGameComponent
    {
        Matrix view = Matrix.Identity;
        readonly SceneState Scene = new SceneState();
        readonly LightingSystemPreferences Preferences = new LightingSystemPreferences();
        readonly GraphicsDeviceManager DeviceMgr;
        readonly LightingSystemManager LightingMgr;
        readonly SceneInterface Interface;
        readonly ShadowMapManager ShadowMgr;

        /// <summary>
        /// Rendering environment's RenderManager. Use to add models and scene objects for rendering.
        /// </summary>
        public RenderManager RenderManager { get; }

        /// <summary>
        /// Rendering environment's LightManager. Use to add scene lights for rendering.
        /// </summary>
        public LightManager LightManager { get; }

        /// <summary>
        /// Rendering environment's PostProcessManager. Use to add post processors that alter scene rendering.
        /// </summary>
        public PostProcessManager PostProcessManager { get; }

        /// <summary>
        /// Rendering environment's editor. Allows in-game editor support and access to properties
        /// that change editor rendering and camera control.
        /// </summary>
        public LightingSystemEditor Editor { get; }

        /// <summary>The scene's current view matrix.</summary>
        public Matrix View { get; set; } = Matrix.Identity;

        /// <summary>The scene's current projection matrix.</summary>
        public Matrix Projection { get; set; } = Matrix.Identity;

        /// <summary>
        /// The scene's current environment such as fog, viewing distance, and HDR information.
        /// </summary>
        public SceneEnvironment Environment { get; set; } = new SceneEnvironment();

        /// <summary>Creates a new LightingSystemGameComponent instance.</summary>
        /// <param name="game"></param>
        /// <param name="graphicsdevicemanager"></param>
        public LightingSystemGameComponent(Game game, GraphicsDeviceManager graphicsdevicemanager)
            : base(game)
        {
            DeviceMgr = graphicsdevicemanager;
            LightingMgr = new LightingSystemManager(Game.Services);
            Interface = new SceneInterface(graphicsdevicemanager);
            RenderManager = new RenderManager(graphicsdevicemanager, Interface);
            LightManager = new LightManager(graphicsdevicemanager);
            ShadowMgr = new ShadowMapManager(graphicsdevicemanager);
            Interface.AddManager(RenderManager);
            Interface.AddManager(LightManager);
            Interface.AddManager(ShadowMgr);
            Editor = new LightingSystemEditor(Game.Services, DeviceMgr, Game);
            Interface.AddManager(Editor);
            PostProcessManager = new PostProcessManager(DeviceMgr);
            Interface.AddManager(PostProcessManager);
            Interface.ApplyPreferences(Preferences);
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            Interface.ApplyPreferences(preferences);
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public void Clear()
        {
            Interface.Clear();
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public void Unload()
        {
            Interface.Unload();
            LightingMgr.Unload();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded. Override this method
        /// to unload any component-specific graphics resources.
        /// </summary>
        protected override void UnloadContent()
        {
            Unload();
            base.UnloadContent();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the DrawableGameComponent and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Unload();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Called when the GameComponent needs to be updated. Override this
        /// method with component-specific update code.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void Update(float deltaTime)
        {
            Interface.Update(deltaTime);
            base.Update(deltaTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override
        /// this method with component-specific drawing code.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void Draw(float deltaTime)
        {
            if (SplashScreenGameComponent.DisplayComplete)
            {
                Matrix projection = Projection;
                Scene.BeginFrameRendering(ref view, ref projection, deltaTime, Environment, true);
                Interface.BeginFrameRendering(Scene);
                RenderManager.Render();
                Interface.EndFrameRendering();
                Scene.EndFrameRendering();
            }
            base.Draw(deltaTime);
        }
    }
}

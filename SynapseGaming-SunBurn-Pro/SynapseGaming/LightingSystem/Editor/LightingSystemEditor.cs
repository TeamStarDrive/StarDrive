// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Editor.LightingSystemEditor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Editor
{
    /// <summary>Adds editor support to SunBurn projects.</summary>
    public class LightingSystemEditor : IRenderableManager, IUpdatableManager, IManagerService
    {
        readonly EditorSettings EditorSettings;

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType => SceneInterface.EditorType;

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
        public int ManagerProcessOrder { get; set; } = 10;

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>The active Game object.</summary>
        public Game Game { get; }

        /// <summary>
        /// Adjusts the scale of on screen light icons allowing support for varying scene scales.
        /// </summary>
        public float IconScale
        {
            get => EditorSettings.IconScale;
            set => EditorSettings.IconScale = value;
        }

        /// <summary>
        /// Determines if user defined code handles in editor camera movement.
        /// If so only object selection and object movement is processed.
        /// </summary>
        public bool UserHandledView
        {
            get => EditorSettings.UserHandledView;
            set => EditorSettings.UserHandledView = value;
        }

        /// <summary>Creates a LightingSystemEditor instance.</summary>
        /// <param name="serviceprovider"></param>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="game"></param>
        public LightingSystemEditor(IServiceProvider serviceprovider, GraphicsDeviceManager graphicsdevicemanager, Game game)
        {
            GraphicsDeviceManager = graphicsdevicemanager;
            Game = game;
            EditorSettings = new EditorSettings();
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public void ApplyPreferences(ILightingSystemPreferences preferences)
        {
        }

        /// <summary>
        /// Processes in editor input control, object selection, and camera movement.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        public void BeginFrameRendering(ISceneState state)
        {
        }

        /// <summary>Finalizes rendering.</summary>
        public void EndFrameRendering()
        {
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public void Unload()
        {
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public void Clear()
        {
        }

        /// <summary>
        /// Register delegate used to reload scene assets when requested by the editor.
        /// </summary>
        /// <param name="del"></param>
        public static void RegisterOnReplaceEffect(EffectReplaceDelegate del)
        {
        }

        /// <summary>
        /// Unregister delegate used to reload scene assets when requested by the editor.
        /// </summary>
        /// <param name="del"></param>
        public static void UnregisterOnReplaceEffect(EffectReplaceDelegate del)
        {
        }

        /// <summary>
        /// Call to start tracking user defined resources in the editor.
        /// </summary>
        /// <param name="resource"></param>
        public static void OnCreateResource(IDisposable resource)
        {
        }

        /// <summary>
        /// Call to stop tracking user defined resources in the editor.
        /// </summary>
        /// <param name="resource"></param>
        public static void OnDisposeResource(IDisposable resource)
        {
        }

        internal delegate void Delegate0(IDisposable resource);

        /// <summary>Used to remap effects that are replaced in editor.</summary>
        /// <param name="currenteffect">The effect to replace.</param>
        /// <param name="neweffect">The new effect.</param>
        public delegate void EffectReplaceDelegate(Effect currenteffect, Effect neweffect);
    }
}

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
  public class LightingSystemEditor : IUnloadable, IManager, IRenderableManager, IUpdatableManager, IManagerService
  {
      private Class32 class32_0;
    private static Delegate0 delegate0_0;
    private static Delegate0 delegate0_1;
    /// <summary>Used to remap effects that are replaced in editor.</summary>
    public static EffectReplaceDelegate ReplaceEffect;
    /// <summary>
    /// Used to reload all scene assets when requested by the editor.
    /// </summary>
    public static ReloadAssetsDelegate ReloadAssets;

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
    /// The assigned key that, when pressed, will be used to launch the in-game editor.
    /// </summary>
    public Keys LaunchKey { get; set; }

      /// <summary>
    /// Adjusts the scale of on screen light icons allowing support for varying scene scales.
    /// </summary>
    public float IconScale
    {
      get => this.class32_0.IconScale;
          set => this.class32_0.IconScale = value;
      }

    /// <summary>
    /// Adjusts the speed of in editor camera movement allowing support for varying scene scales.
    /// </summary>
    public float MoveScale
    {
      get => this.class32_0.MoveScale;
        set => this.class32_0.MoveScale = value;
    }

    /// <summary>
    /// Adjusts the speed of in editor camera rotation to user preference.
    /// </summary>
    public float RotationScale
    {
      get => this.class32_0.RotationScale;
        set => this.class32_0.RotationScale = value;
    }

    /// <summary>
    /// Determines if user defined code handles in editor camera movement.
    /// If so only object selection and object movement is processed.
    /// </summary>
    public bool UserHandledView
    {
      get => this.class32_0.UserHandledView;
        set => this.class32_0.UserHandledView = value;
    }

    /// <summary>
    /// Allows specific processing when the editor attached. Commonly used for editor specific input processing.
    /// </summary>
    public bool EditorAttached => false;

      /// <summary>
    /// Allows specific processing when the game window has input focus, not the editor's controls.
    /// </summary>
    public bool GameHasFocus => true;

      /// <summary>Creates a LightingSystemEditor instance.</summary>
    /// <param name="serviceprovider"></param>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="game"></param>
    public LightingSystemEditor(IServiceProvider serviceprovider, GraphicsDeviceManager graphicsdevicemanager, Game game)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
      this.Game = game;
      this.class32_0 = new Class32();
    }

    /// <summary />
    ~LightingSystemEditor()
    {
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
    /// <param name="gametime"></param>
    public void Update(GameTime gametime)
    {
    }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    public void BeginFrameRendering(ISceneState scenestate)
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

    /// <summary>Opens the SunBurn editor manually.</summary>
    public void LaunchEditor()
    {
    }

    private void method_0()
    {
    }

    internal static void smethod_0(Delegate0 delegate0_2)
    {
      delegate0_0 += delegate0_2;
    }

    internal static void smethod_1(Delegate0 delegate0_2)
    {
      delegate0_1 += delegate0_2;
    }

    internal static void smethod_2(Delegate0 delegate0_2)
    {
      delegate0_0 -= delegate0_2;
    }

    internal static void smethod_3(Delegate0 delegate0_2)
    {
      delegate0_1 -= delegate0_2;
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

    /// <summary>
    /// Used to reload all scene assets when requested by the editor.
    /// </summary>
    public delegate void ReloadAssetsDelegate();
  }
}

// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>Provides base scene shadow management support.</summary>
  public abstract class BaseShadowManager : IUnloadable, IManager, IRenderableManager
  {
    private static ShadowSource shadowSource_0 = new ShadowSource();
    private static DirectionalLight directionalLight_0 = new DirectionalLight();
    private static ShadowGroup shadowGroup_0 = new ShadowGroup();
    private static ShadowGroup shadowGroup_1 = new ShadowGroup();
    private static Dictionary<IShadowSource, ShadowGroup> dictionary_0 = new Dictionary<IShadowSource, ShadowGroup>(32);
    private ISceneState isceneState_0 = (ISceneState) new SynapseGaming.LightingSystem.Core.SceneState();
    private Class21<ShadowGroup> class21_0 = new Class21<ShadowGroup>();
    private IGraphicsDeviceService igraphicsDeviceService_0;

    /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    public IGraphicsDeviceService GraphicsDeviceManager
    {
      get
      {
        return this.igraphicsDeviceService_0;
      }
    }

    /// <summary>The current SceneState used by this object.</summary>
    protected ISceneState SceneState
    {
      get
      {
        return this.isceneState_0;
      }
    }

    /// <summary>Creates a new BaseShadowManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public BaseShadowManager(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
    {
    }

    /// <summary>
    /// Sets up frame information necessary for scene shadowing.
    /// </summary>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      //SplashScreen.CheckProductActivation();
      this.isceneState_0 = scenestate;
    }

    /// <summary>Cleans up frame information.</summary>
    public virtual void EndFrameRendering()
    {
      this.class21_0.method_0();
    }

    /// <summary>
    /// Builds a list of shadow groups based on the provided light list.  Shadow
    /// groups contain a list of all lights that share a common shadow source.
    /// </summary>
    /// <param name="shadowgroups">Destination shadow group list.</param>
    /// <param name="lights">Source light list.</param>
    /// <param name="usedefaultgrouping">Determines if ungrouped lights should be placed in a
    /// single default group (recommended: true for deferred rendering and false for forward).</param>
    protected void BuildShadowGroups(List<ShadowGroup> shadowgroups, List<ILight> lights, bool usedefaultgrouping)
    {
      BaseShadowManager.dictionary_0.Clear();
      BaseShadowManager.shadowSource_0.ShadowType = ShadowType.None;
      BaseShadowManager.shadowGroup_0.Shadow = (IShadow) null;
      BaseShadowManager.shadowGroup_0.Lights.Clear();
      BaseShadowManager.dictionary_0.Add((IShadowSource) BaseShadowManager.shadowSource_0, BaseShadowManager.shadowGroup_0);
      BaseShadowManager.directionalLight_0.ShadowType = ShadowType.None;
      BaseShadowManager.shadowGroup_1.Shadow = (IShadow) null;
      BaseShadowManager.shadowGroup_1.Lights.Clear();
      BaseShadowManager.dictionary_0.Add((IShadowSource) BaseShadowManager.directionalLight_0, BaseShadowManager.shadowGroup_1);
      foreach (ILight light in lights)
      {
        IShadowSource shadowSource = light.ShadowSource;
        if (usedefaultgrouping && (shadowSource == null || light == shadowSource && shadowSource.ShadowType == ShadowType.None))
        {
          if (light is IPointSource)
            BaseShadowManager.shadowGroup_0.Lights.Add(light);
          else
            BaseShadowManager.shadowGroup_1.Lights.Add(light);
        }
        else
        {
          ShadowGroup shadowGroup;
          if (!BaseShadowManager.dictionary_0.TryGetValue(shadowSource, out shadowGroup))
          {
            shadowGroup = this.class21_0.New();
            shadowGroup.Shadow = (IShadow) null;
            shadowGroup.Lights.Clear();
            BaseShadowManager.dictionary_0.Add(shadowSource, shadowGroup);
          }
          shadowGroup.Lights.Add(light);
        }
      }
      if (BaseShadowManager.shadowGroup_1.Lights.Count <= 0)
        BaseShadowManager.dictionary_0.Remove((IShadowSource) BaseShadowManager.directionalLight_0);
      if (BaseShadowManager.shadowGroup_0.Lights.Count <= 0)
        BaseShadowManager.dictionary_0.Remove((IShadowSource) BaseShadowManager.shadowSource_0);
      else
        BaseShadowManager.shadowSource_0.Position = (BaseShadowManager.shadowGroup_0.Lights[0] as IPointSource).Position;
      foreach (KeyValuePair<IShadowSource, ShadowGroup> keyValuePair in BaseShadowManager.dictionary_0)
      {
        keyValuePair.Value.Build(keyValuePair.Key, this.isceneState_0);
        shadowgroups.Add(keyValuePair.Value);
      }
    }

    /// <summary>
    /// Removes resources managed by this object. Commonly used while clearing the scene.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public abstract void Unload();
  }
}

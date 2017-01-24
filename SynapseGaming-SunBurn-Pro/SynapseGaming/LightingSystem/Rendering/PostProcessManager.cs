// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.PostProcessManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Manages all post processors and applies post processing effects to the scene.
  /// </summary>
  public class PostProcessManager : IUnloadable, IManager, IRenderableManager, IManagerService, IPostProcessManager
  {
    private static SurfaceFormat[] surfaceFormat_1 = new SurfaceFormat[7]{ SurfaceFormat.Color, SurfaceFormat.Bgra1010102, SurfaceFormat.Rgba1010102, SurfaceFormat.Rgba32, SurfaceFormat.Rgba64, SurfaceFormat.HalfVector4, SurfaceFormat.Vector4 };
    private static List<SurfaceFormat> list_1 = new List<SurfaceFormat>();
    private int int_0 = 20;
    private bool bool_0 = true;
    private bool bool_1 = true;
    private bool bool_2 = true;
    private List<IPostProcessor> list_0 = new List<IPostProcessor>();
    private IGraphicsDeviceService igraphicsDeviceService_0;
    private Class14 class14_0;
    private int int_1;
    private int int_2;
    private static SurfaceFormat[] surfaceFormat_0;

    /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType
    {
      get
      {
        return SceneInterface.PostProcessManagerType;
      }
    }

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
    public int ManagerProcessOrder
    {
      get
      {
        return this.int_0;
      }
      set
      {
        this.int_0 = value;
      }
    }

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

    /// <summary>
    /// Indicates all post processors initialized correctly and are compatible.
    /// </summary>
    public bool PostProcessorsAreCompatible
    {
      get
      {
        return this.bool_1;
      }
    }

    internal static SurfaceFormat[] AllSupportedFormats
    {
      get
      {
        return PostProcessManager.surfaceFormat_0;
      }
    }

    /// <summary>Creates a PostProcessManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public PostProcessManager(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
      this.class14_0 = new Class14(this.igraphicsDeviceService_0);
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
    {
      this.bool_0 = preferences.PostProcessingDetail != DetailPreference.Off;
      for (int index = 0; index < this.list_0.Count; ++index)
        this.list_0[index].ApplyPreferences(preferences);
    }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      if (!this.bool_0)
        return;
      this.method_0();
      if (!this.bool_1)
        return;
      for (int index = 0; index < this.list_0.Count; ++index)
        this.list_0[index].BeginFrameRendering(scenestate);
    }

    /// <summary>Finalizes rendering and applies all post processing.</summary>
    public virtual void EndFrameRendering()
    {
      if (!this.bool_1 || !this.bool_0)
        return;
      Texture2D mastersource = (Texture2D) null;
      Texture2D lastprocessorsource = (Texture2D) null;
      for (int index = this.list_0.Count - 1; index >= 0; --index)
      {
        lastprocessorsource = this.list_0[index].EndFrameRendering(mastersource, lastprocessorsource);
        if (mastersource == null)
          mastersource = lastprocessorsource;
      }
    }

    private void method_0()
    {
      GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
      this.bool_2 = this.bool_2 | this.class14_0.Changed | this.int_1 != graphicsDevice.Viewport.Width | this.int_2 != graphicsDevice.Viewport.Height;
      if (!this.bool_2 && PostProcessManager.surfaceFormat_0 != null)
        return;
      this.bool_2 = false;
      for (int index = 0; index < this.list_0.Count; ++index)
        this.list_0[index].Unload();
      this.int_1 = graphicsDevice.Viewport.Width;
      this.int_2 = graphicsDevice.Viewport.Height;
      GraphicsDeviceSupport graphicsDeviceSupport = LightingSystemManager.Instance.GetGraphicsDeviceSupport(graphicsDevice);
      PostProcessManager.list_1.Clear();
      foreach (SurfaceFormat index in PostProcessManager.surfaceFormat_1)
      {
        GraphicsDeviceSupport.FormatSupport formatSupport = graphicsDeviceSupport.SurfaceFormat[index];
        if (formatSupport.RenderTarget && formatSupport.Texture && formatSupport.Blending)
          PostProcessManager.list_1.Add(index);
      }
      PostProcessManager.surfaceFormat_0 = PostProcessManager.list_1.ToArray();
      for (int index = 0; index < this.list_0.Count; ++index)
      {
        if (this.list_0[index].Initialize(PostProcessManager.list_1))
        {
          if (index + 1 < this.list_0.Count)
          {
            this.method_1(PostProcessManager.list_1, this.list_0[index].SupportedSourceFormats);
            if (PostProcessManager.list_1.Count < 1)
            {
              this.bool_1 = false;
              return;
            }
          }
        }
        else
        {
          this.bool_1 = false;
          return;
        }
      }
      this.bool_1 = true;
    }

    private void method_1(List<SurfaceFormat> list_2, SurfaceFormat[] surfaceFormat_2)
    {
      for (int index1 = 0; index1 < list_2.Count; ++index1)
      {
        bool flag = false;
        for (int index2 = 0; index2 < surfaceFormat_2.Length; ++index2)
        {
          if (list_2[index1] == surfaceFormat_2[index2])
          {
            flag = true;
            break;
          }
        }
        if (!flag)
        {
          list_2.RemoveAt(index1);
          --index1;
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
      this.list_0.Add(postprocessor);
      this.bool_2 = true;
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
      for (int index = 0; index < this.list_0.Count; ++index)
        this.list_0[index].Unload();
      this.list_0.Clear();
      this.bool_2 = true;
    }
  }
}

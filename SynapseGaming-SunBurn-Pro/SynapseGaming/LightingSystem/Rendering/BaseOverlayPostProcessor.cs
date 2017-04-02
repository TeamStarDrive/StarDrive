// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.BaseOverlayPostProcessor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Post-processor base class used for processors that do not use their own
  /// render targets, and instead use the master frame source data (supplied
  /// by the top most processor) and/or data from the previous processor to
  /// alter the current render target (the target owned by the next processor
  /// down the chain).
  /// </summary>
  public abstract class BaseOverlayPostProcessor : IPostProcessor
  {
    private ISceneState isceneState_0;

    /// <summary>
    /// Render target formats supported by the post processor.
    /// </summary>
    public virtual SurfaceFormat[] SupportedTargetFormats
    {
      get
      {
        return PostProcessManager.AllSupportedFormats;
      }
    }

    /// <summary>
    /// Source texture formats supported by the post processor. Source textures are
    /// provided by the previous post processor in the processing chain.
    /// </summary>
    public virtual SurfaceFormat[] SupportedSourceFormats
    {
      get
      {
        return PostProcessManager.AllSupportedFormats;
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

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public abstract void ApplyPreferences(ILightingSystemPreferences preferences);

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      this.isceneState_0 = scenestate;
    }

    /// <summary>
    /// Applies post processing effects based on the source textures.
    /// </summary>
    /// <param name="mastersource">Texture containing the original scene without any visual processing applied.</param>
    /// <param name="lastprocessorsource">Texture containing the scene with visual processing applied by each
    /// previous post processor in the processing chain.</param>
    /// <returns>Returns a texture containing the post processor's output image.</returns>
    public virtual Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource)
    {
      return lastprocessorsource;
    }

    /// <summary>
    /// Sets up the post processor and tries to find supported formats for its visual processing.
    /// </summary>
    /// <param name="availableformats">List of formats available based on support by all previous
    /// post processor in the processing chain.</param>
    /// <returns>Returns true if the post processor was properly initialized.</returns>
    public virtual bool Initialize(List<SurfaceFormat> availableformats)
    {
      return true;
    }

    /// <summary>
    /// Disposes any graphics resources used internally by this object.
    /// </summary>
    public virtual void Unload()
    {
    }
  }
}

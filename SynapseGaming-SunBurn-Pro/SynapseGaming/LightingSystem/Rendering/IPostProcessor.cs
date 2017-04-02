// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IPostProcessor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface used by objects that perform post render visual processing.
  /// </summary>
  public interface IPostProcessor
  {
    /// <summary>
    /// Render target formats supported by the post processor.
    /// </summary>
    SurfaceFormat[] SupportedTargetFormats { get; }

    /// <summary>
    /// Source texture formats supported by the post processor. Source textures are
    /// provided by the previous post processor in the processing chain.
    /// </summary>
    SurfaceFormat[] SupportedSourceFormats { get; }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    void ApplyPreferences(ILightingSystemPreferences preferences);

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    void BeginFrameRendering(ISceneState scenestate);

    /// <summary>
    /// Applies post processing effects based on the source textures.
    /// </summary>
    /// <param name="mastersource">Texture containing the original scene without any visual processing applied.</param>
    /// <param name="lastprocessorsource">Texture containing the scene with visual processing applied by each
    /// previous post processor in the processing chain.</param>
    /// <returns>Returns a texture containing the post processor's output image.</returns>
    Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource);

    /// <summary>
    /// Sets up the post processor and tries to find supported formats for its visual processing.
    /// </summary>
    /// <param name="availableformats">List of formats available based on support by all previous
    /// post processor in the processing chain.</param>
    /// <returns>Returns true if the post processor was properly initialized.</returns>
    bool Initialize(List<SurfaceFormat> availableformats);

    /// <summary>
    /// Disposes any graphics resources used internally by this object.
    /// </summary>
    void Unload();
  }
}

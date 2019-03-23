// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.IShadowMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Interface that provides a base for all shadow objects that render using shadow mapping.
  /// </summary>
  public interface IShadowMap : IDisposable, IShadow
  {
    /// <summary>
    /// Used to determine if the shadow map contents are valid or if the contents need
    /// to be re-rendered.
    /// 
    /// The default SunBurn shadow mapping implementation renders shadow map contents
    /// every frame, however custom implementations can provide static shadow maps.
    /// 
    /// Please note: if shadow maps are static and the contents are valid DO NOT call
    /// ShadowRenderTargetGroup Begin() and End().  On the Xbox this will invalidate the
    /// render target data.
    /// 
    /// However skipping calls to Begin and End require calling
    /// ShadowRenderTargetGroup.UpdateRenderTargetTexture() to ensure the shadow texture
    /// is up to date.
    /// 
    /// When using the built-in render managers this is all handled automatically.
    /// </summary>
    bool ContentsAreValid { get; set; }

    /// <summary>
    /// Array of surfaces used to render the shadow map. Each surface contains its own
    /// section within a render target. Surfaces are used for multi-part rendering and
    /// level-of-detail.
    /// </summary>
    ShadowMapSurface[] Surfaces { get; }

    /// <summary>
    /// Used to provide a custom render target for the shadow map. Though this allows
    /// custom render targets it also bypasses render target retrieval from the
    /// ShadowMapCache, which can cause higher memory usage.
    /// </summary>
    RenderTarget CustomRenderTarget { get; }

    /// <summary>
    /// Effect used for shadow map rendering. The effect should support both generating
    /// the shadow map and rendering shadows to the scene.
    /// </summary>
    Effect ShadowEffect { get; }

    /// <summary>
    /// Builds the shadow map information based on the provided scene state and shadow
    /// group, visibility, and quality.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="sceneState"></param>
    /// <param name="shadowGroup">Shadow group used as the source for the shadow map.</param>
    /// <param name="visibility"></param>
    /// <param name="shadowQuality">Shadow quality from 1.0 (highest) to 0.0 (lowest).</param>
    void Build(GraphicsDevice device, ISceneState sceneState, ShadowGroup shadowGroup, IShadowMapVisibility visibility, float shadowQuality);

    /// <summary>
    /// Sets up the shadow map for rendering shadows to the scene.
    /// </summary>
    /// <param name="shadowMap"></param>
    void BeginRendering(Texture shadowMap);

    /// <summary>
    /// Sets up the shadow map for rendering shadows to the scene.
    /// </summary>
    /// <param name="shadowMap"></param>
    /// <param name="shadowFx">Custom shadow effect used in rendering.</param>
    void BeginRendering(Texture shadowMap, Effect shadowFx);

    /// <summary>Finalizes rendering.</summary>
    void EndRendering();

    /// <summary>
    /// Determines if the shadow map surface is visible to the provided view frustum.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="viewfrustum"></param>
    /// <returns></returns>
    bool IsSurfaceVisible(int surface, BoundingFrustum viewfrustum);

    /// <summary>
    /// Sets the location in the shadow map render target the surface renders to.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="location">Texel region used by the shadow map surface.</param>
    void SetSurfaceRenderTargetLocation(int surface, Rectangle location);

    /// <summary>
    /// Sets up the shadow map surface for generating the shadow map depth buffer.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    void BeginSurfaceRendering(int surface);

    /// <summary>
    /// Sets up the shadow map surface for generating the shadow map depth buffer.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="shadowFx">Custom shadow effect used in rendering.</param>
    void BeginSurfaceRendering(int surface, Effect shadowFx);

    /// <summary>Finalizes rendering.</summary>
    void EndSurfaceRendering();
  }
}

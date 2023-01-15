// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.OcclusionQueryHelper`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
#pragma warning disable CA2213

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Helper class that provides occlusion testing of object and/or object group bounding areas.  The occlusion
  /// test is asynchronous, calling RunOcclusionQuery does not block the thread, which allows other cpu related
  /// work to be done in parallel.  Calling IsObjectVisible however IS blocking, so try to place as much cpu
  /// related work between these two calls to avoid wasting cpu cycles.
  /// </summary>
  /// <typeparam name="TKey">Object type used to store and lookup query results.</typeparam>
  public sealed class OcclusionQueryHelper<TKey> : IDisposable
  {
    private List<Class20> list_0 = new List<Class20>(32);
    private Dictionary<TKey, Class20> dictionary_0 = new Dictionary<TKey, Class20>(32);
    private GraphicsDevice graphicsDevice_0;
    private Class10 class10_0;

    /// <summary>Creates a new OcclusionQueryHelper instance.</summary>
    /// <param name="device"></param>
    public OcclusionQueryHelper(GraphicsDevice device)
    {
      this.graphicsDevice_0 = device;
      this.class10_0 = new Class10(this.graphicsDevice_0);
    }

    /// <summary>
    /// Adds an object and its bounding area to the occlusion testing list.
    /// </summary>
    /// <param name="key">Object to add to the occlusion testing list.</param>
    /// <param name="worldbounds">Object's world bounding area.</param>
    public void SubmitObject(TKey key, BoundingBox worldbounds)
    {
      if (this.list_0.Count < 1)
      {
        this.dictionary_0.Add(key, new Class20(this.graphicsDevice_0, worldbounds));
      }
      else
      {
        Class20 class20 = this.list_0[0];
        class20.bool_0 = false;
        class20.boundingBox_0 = worldbounds;
        this.dictionary_0.Add(key, class20);
        this.list_0.RemoveAt(0);
      }
    }

    /// <summary>
    /// Asynchronously performs the occlusion test (does not block the thread), which allows other cpu related
    /// work to be done in parallel.
    /// </summary>
    /// <param name="scenestate">Scene state used to render the current view.</param>
    /// <param name="detectionpadding">Used to avoid false "invisible" results by padding
    /// the detection area. A good value is 1.4 (ie: x1.4 or 140%), however this value is
    /// affected by the projection near clipping plane.</param>
    public void RunOcclusionQuery(ISceneState scenestate, float detectionpadding)
    {
      if (this.dictionary_0.Count < 1)
        return;
      ColorWriteChannels colorWriteChannels = this.graphicsDevice_0.RenderState.ColorWriteChannels;
      bool bufferWriteEnable = this.graphicsDevice_0.RenderState.DepthBufferWriteEnable;
      CullMode cullMode = this.graphicsDevice_0.RenderState.CullMode;
      this.graphicsDevice_0.RenderState.ColorWriteChannels = ColorWriteChannels.None;
      this.graphicsDevice_0.RenderState.DepthBufferWriteEnable = false;
      this.graphicsDevice_0.RenderState.CullMode = CullMode.None;
      BasicEffect defaultEffect = this.class10_0.DefaultEffect;
      defaultEffect.World = Matrix.Identity;
      defaultEffect.View = scenestate.View;
      defaultEffect.Projection = scenestate.Projection;
      defaultEffect.Begin();
      defaultEffect.CurrentTechnique.Passes[0].Begin();
      Plane near = scenestate.ViewFrustum.Near;
      foreach (KeyValuePair<TKey, Class20> keyValuePair in this.dictionary_0)
      {
        BoundingBox boundingBox0 = keyValuePair.Value.boundingBox_0;
        BoundingBox boundingBox = CoreUtils.smethod_7(boundingBox0, detectionpadding);
        keyValuePair.Value.bool_0 = boundingBox.Intersects(near) == PlaneIntersectionType.Intersecting;
        if (!keyValuePair.Value.bool_0)
        {
          OcclusionQuery occlusionQuery0 = keyValuePair.Value.occlusionQuery_0;
          if (occlusionQuery0.IsSupported)
          {
            defaultEffect.World = this.class10_0.method_0(boundingBox0);
            defaultEffect.CommitChanges();
            occlusionQuery0.Begin();
            this.class10_0.method_1();
            occlusionQuery0.End();
          }
          else
            break;
        }
      }
      defaultEffect.CurrentTechnique.Passes[0].End();
      defaultEffect.End();
      this.graphicsDevice_0.RenderState.ColorWriteChannels = colorWriteChannels;
      this.graphicsDevice_0.RenderState.DepthBufferWriteEnable = bufferWriteEnable;
      this.graphicsDevice_0.RenderState.CullMode = cullMode;
    }

    /// <summary>
    /// Retrieves the occlusion test results for an object. This call DOES block the
    /// thread until the object results are available.
    /// 
    /// To maximize cpu/gpu parallelization:
    ///   -always retrieve the test results in the same order objects were submitted.
    ///   -always try to perform as much cpu related work between running the test
    ///    and this call.
    /// </summary>
    /// <param name="key">Object to retrieve occlusion test result from.</param>
    /// <returns></returns>
    public bool IsObjectVisible(TKey key)
    {
      if (!this.dictionary_0.ContainsKey(key))
        return true;
      Class20 class20 = this.dictionary_0[key];
      if (class20.bool_0 || !class20.occlusionQuery_0.IsSupported)
        return true;
      while (!class20.occlusionQuery_0.IsComplete)
        Thread.Sleep(0);
      return class20.occlusionQuery_0.PixelCount > 0;
    }

    /// <summary>Removes all objects from the occlusion testing list.</summary>
    public void Clear()
    {
      foreach (KeyValuePair<TKey, Class20> keyValuePair in this.dictionary_0)
        this.list_0.Add(keyValuePair.Value);
      this.dictionary_0.Clear();
    }

    /// <summary>Disposes all related graphics objects.</summary>
    public void Dispose()
    {
      this.Clear();
      foreach (Class20 class20 in this.list_0)
        class20.Dispose();
      this.list_0.Clear();
      class10_0?.Dispose();
      class10_0 = null;
    }

    sealed class Class20 : IDisposable
    {
      public OcclusionQuery occlusionQuery_0;
      public bool bool_0;
      public BoundingBox boundingBox_0;

      public Class20(GraphicsDevice device, BoundingBox bounds)
      {
        this.occlusionQuery_0 = new OcclusionQuery(device);
        this.bool_0 = false;
        this.boundingBox_0 = bounds;
      }

      public void Dispose()
      {
          occlusionQuery_0.Dispose();
      }
    }
  }
}

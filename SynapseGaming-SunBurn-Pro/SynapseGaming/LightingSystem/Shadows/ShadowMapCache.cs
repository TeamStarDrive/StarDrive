// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowMapCache
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns10;
using ns11;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Class that manages render target sections used for shadow mapping.
  /// </summary>
  public class ShadowMapCache
  {
    private Class74[] class74_0 = new Class74[4]{ new Class74(SurfaceFormat.Single, 4), new Class74(SurfaceFormat.HalfSingle, 2), new Class74(SurfaceFormat.HalfVector2, 4), new Class74(SurfaceFormat.Single, 4) };
    private DepthFormat[] depthFormat_0 = new DepthFormat[2]{ DepthFormat.Depth24Stencil8, DepthFormat.Depth24Stencil4 };
    private SurfaceFormat surfaceFormat_0 = SurfaceFormat.Unknown;
      private List<Class72> list_0 = new List<Class72>(8);
    internal Class75 class75_0 = new Class75();
    private int int_0;
    private int int_1;
      private DepthStencilBuffer depthStencilBuffer_0;
    private IGraphicsDeviceService igraphicsDeviceService_0;

    /// <summary>
    /// Gets a depth buffer properly sized and formatted for use with cached render targets.
    /// </summary>
    public DepthStencilBuffer DepthBuffer
    {
      get
      {
        if (this.depthStencilBuffer_0 == null)
        {
          this.method_0();
          this.depthStencilBuffer_0 = new DepthStencilBuffer(this.igraphicsDeviceService_0.GraphicsDevice, this.PageSize, this.PageSize, LightingSystemManager.Instance.GetGraphicsDeviceSupport(this.igraphicsDeviceService_0.GraphicsDevice).FindSupportedFormat(this.depthFormat_0, this.surfaceFormat_0));
        }
        return this.depthStencilBuffer_0;
      }
    }

    /// <summary>
    /// Maximum amount of memory the cache is allowed to consume. This is an
    /// approximate value and the cache may use more memory in certain instances.
    /// </summary>
    public int MaxMemoryUsage { get; private set; }

      /// <summary>
    /// True when smaller half-float format render targets are preferred. These
    /// formats consume less memory and generally perform better, but have lower
    /// accuracy on directional lights.
    /// </summary>
    public bool PreferHalfFloatTextureFormat { get; private set; } = true;

      /// <summary>
    /// Size in pixels of each render target (page) in the cache. For a size of 1024
    /// the actual page dimensions are 1024x1024. Small sizes can reduce performance by
    /// fragmenting the shadow maps, and reduce shadow quality by lowering the maximum
    /// resolution of each shadow map section.
    /// </summary>
    public int PageSize { get; private set; } = 2048;

      /// <summary>Creates a new ShadowMapCache instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="pagesize">Size in pixels of each render target (page) in the cache.
    /// For a size of 1024 the actual page dimensions are 1024x1024. Small sizes can reduce
    /// performance by fragmenting the shadow maps, and reduce shadow quality by lowering
    /// the maximum resolution of each shadow map section.</param>
    /// <param name="maxmemoryusage">Maximum amount of memory the cache is allowed to consume.
    /// This is an approximate value and the cache may use more memory in certain instances.</param>
    /// <param name="preferhalffloat">True when smaller half-float format render targets are
    /// preferred. These formats consume less memory and generally perform better, but have
    /// lower accuracy on directional lights.</param>
    public ShadowMapCache(IGraphicsDeviceService graphicsdevicemanager, int pagesize, int maxmemoryusage, bool preferhalffloat)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
      this.Resize(pagesize, maxmemoryusage, preferhalffloat);
    }

    /// <summary>Resizes shadow maps and memory usage.</summary>
    /// <param name="pagesize">Size in pixels of each render target (page) in the cache.
    /// For a size of 1024 the actual page dimensions are 1024x1024. Small sizes can reduce
    /// performance by fragmenting the shadow maps, and reduce shadow quality by lowering
    /// the maximum resolution of each shadow map section.</param>
    /// <param name="maxmemoryusage">Maximum amount of memory the cache is allowed to consume.
    /// This is an approximate value and the cache may use more memory in certain instances.</param>
    /// <param name="preferhalffloat">True when smaller half-float format render targets are
    /// preferred. These formats consume less memory and generally perform better, but have
    /// lower accuracy on directional lights.</param>
    public void Resize(int pagesize, int maxmemoryusage, bool preferhalffloat)
    {
      this.Unload();
      this.PageSize = pagesize;
      this.MaxMemoryUsage = maxmemoryusage;
      this.PreferHalfFloatTextureFormat = preferhalffloat;
      this.int_1 = 0;
    }

    private void method_0()
    {
      if (this.int_1 > 0)
        return;
      GraphicsDeviceSupport graphicsDeviceSupport = LightingSystemManager.Instance.GetGraphicsDeviceSupport(this.igraphicsDeviceService_0.GraphicsDevice);
      int num = 0;
      if (this.PreferHalfFloatTextureFormat)
        num = 1;
      for (int index = num; index < this.class74_0.Length; ++index)
      {
        Class74 class74 = this.class74_0[index];
        if (graphicsDeviceSupport.SurfaceFormat[class74.surfaceFormat_0].RenderTarget)
        {
          this.surfaceFormat_0 = class74.surfaceFormat_0;
          this.int_0 = class74.int_0;
          this.int_1 = this.PageSize * this.PageSize * this.int_0;
          break;
        }
      }
      if (this.int_1 < 1)
        throw new Exception("Unable to find a valid shadow buffer render target format.");
    }

    /// <summary>
    /// Attempts to reserve the requested shadow map sections in a
    /// single render target. If successful the render target is
    /// returned, otherwise null is returned.
    /// </summary>
    /// <param name="sectionsizes"></param>
    /// <returns></returns>
    public RenderTarget2D ReserveSections(List<Rectangle> sectionsizes)
    {
      this.method_0();
      int num = 0;
      foreach (Rectangle sectionsiz in sectionsizes)
        num += sectionsiz.Width * sectionsiz.Height;
      if (num > this.PageSize * this.PageSize)
        return null;
      foreach (Class72 class72 in this.list_0)
      {
        if (!class72.method_9(sectionsizes))
        {
          if (class72.method_1())
            return null;
        }
        else
        {
          this.method_2(sectionsizes);
          return class72.RenderTarget;
        }
      }
      if (this.list_0.Count > 0 && (this.list_0.Count + 1) * this.int_1 > this.MaxMemoryUsage)
        return null;
      Class72 class72_1 = new Class72(this.igraphicsDeviceService_0.GraphicsDevice, this.PageSize, this.surfaceFormat_0);
      this.list_0.Add(class72_1);
      if (!class72_1.method_9(sectionsizes))
        return null;
      this.method_2(sectionsizes);
      return class72_1.RenderTarget;
    }

    internal float method_1()
    {
      if (this.list_0.Count < 1)
        return 0.0f;
      int num = 0;
      foreach (Class72 class72 in this.list_0)
      {
        if (!class72.method_1())
          ++num;
      }
      return num / (float) this.list_0.Count;
    }

    private void method_2(List<Rectangle> list_1)
    {
      this.class75_0.lightingSystemStatistic_0.AccumulationValue = this.list_0.Count;
      this.class75_0.lightingSystemStatistic_1.AccumulationValue = this.list_0.Count * this.int_1;
      this.class75_0.lightingSystemStatistic_2.AccumulationValue = 0;
      this.class75_0.lightingSystemStatistic_3.AccumulationValue = 0;
      foreach (Class72 class72 in this.list_0)
      {
        if (!class72.method_1())
        {
          ++this.class75_0.lightingSystemStatistic_2.AccumulationValue;
          this.class75_0.lightingSystemStatistic_3.AccumulationValue += this.int_1;
        }
      }
    }

    /// <summary>
    /// Clears all reserved shadow map sections, allowing the sections to be reused
    /// in future shadow maps section requests.
    /// </summary>
    public void ClearReserves()
    {
      foreach (Class72 class72 in this.list_0)
        class72.method_0();
    }

    /// <summary>
    /// Disposes any graphics resources used internally by this object, and clears
    /// all reserved shadow map sections. Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      Disposable.Free(ref this.depthStencilBuffer_0);
      foreach (Class72 class72 in this.list_0)
        class72.Dispose();
      this.list_0.Clear();
    }

    private class Class74
    {
      public SurfaceFormat surfaceFormat_0;
      public int int_0;

      public Class74(SurfaceFormat format, int texelbytes)
      {
        this.surfaceFormat_0 = format;
        this.int_0 = texelbytes;
      }
    }

    internal class Class75
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Shadow_TotalPages", LightingSystemStatisticCategory.Shadowing);
      public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Shadow_TotalMemoryUsage", LightingSystemStatisticCategory.Shadowing);
      public LightingSystemStatistic lightingSystemStatistic_2 = LightingSystemStatistics.GetStatistic("Shadow_ActivePages", LightingSystemStatisticCategory.Shadowing);
      public LightingSystemStatistic lightingSystemStatistic_3 = LightingSystemStatistics.GetStatistic("Shadow_ActiveMemoryUsage", LightingSystemStatisticCategory.Shadowing);
    }
  }
}

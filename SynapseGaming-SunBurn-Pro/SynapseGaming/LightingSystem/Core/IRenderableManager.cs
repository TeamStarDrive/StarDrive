// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IRenderableManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by objects that manage rendering and scene resources.
  /// </summary>
  public interface IRenderableManager : IUnloadable, IManager
  {
    /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    IGraphicsDeviceService GraphicsDeviceManager { get; }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="state"></param>
    void BeginFrameRendering(ISceneState state);

    /// <summary>Finalizes rendering.</summary>
    void EndFrameRendering();
  }
}

// Decompiled with JetBrains decompiler
// Type: ns9.Class62
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Rendering;

namespace Mesh
{
  internal class Class62
  {
      public List<RenderableMesh> All { get; } = new List<RenderableMesh>(128);

      public List<RenderableMesh> NonSkinned { get; } = new List<RenderableMesh>(128);

      public List<RenderableMesh> Skinned { get; } = new List<RenderableMesh>(128);

      public void method_0(RenderableMesh renderableMesh_0)
    {
      if (renderableMesh_0 == null)
        return;
      this.All.Add(renderableMesh_0);
      if (renderableMesh_0.effect is ISkinnedEffect && (renderableMesh_0.effect as ISkinnedEffect).Skinned)
        this.Skinned.Add(renderableMesh_0);
      else
        this.NonSkinned.Add(renderableMesh_0);
    }

    public void method_1()
    {
      this.All.Clear();
      this.Skinned.Clear();
      this.NonSkinned.Clear();
    }
  }
}

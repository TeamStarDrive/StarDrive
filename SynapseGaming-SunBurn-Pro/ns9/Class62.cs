// Decompiled with JetBrains decompiler
// Type: ns9.Class62
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Rendering;
using System.Collections.Generic;

namespace ns9
{
  internal class Class62
  {
    private List<RenderableMesh> list_0 = new List<RenderableMesh>(128);
    private List<RenderableMesh> list_1 = new List<RenderableMesh>(128);
    private List<RenderableMesh> list_2 = new List<RenderableMesh>(128);

    public List<RenderableMesh> All
    {
      get
      {
        return this.list_0;
      }
    }

    public List<RenderableMesh> NonSkinned
    {
      get
      {
        return this.list_1;
      }
    }

    public List<RenderableMesh> Skinned
    {
      get
      {
        return this.list_2;
      }
    }

    public void method_0(RenderableMesh renderableMesh_0)
    {
      if (renderableMesh_0 == null)
        return;
      this.list_0.Add(renderableMesh_0);
      if (renderableMesh_0.effect_0 is ISkinnedEffect && (renderableMesh_0.effect_0 as ISkinnedEffect).Skinned)
        this.list_2.Add(renderableMesh_0);
      else
        this.list_1.Add(renderableMesh_0);
    }

    public void method_1()
    {
      this.list_0.Clear();
      this.list_2.Clear();
      this.list_1.Clear();
    }
  }
}

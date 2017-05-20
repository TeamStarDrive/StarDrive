// Decompiled with JetBrains decompiler
// Type: ns9.Class61
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Rendering;

namespace ns9
{
  internal class Class61 : IComparer<RenderableMesh>
  {
    public int Compare(RenderableMesh renderableMesh_0, RenderableMesh renderableMesh_1)
    {
      if (renderableMesh_0 == renderableMesh_1)
        return 0;
      if (renderableMesh_0 == null)
        return -1;
      if (renderableMesh_1 == null)
        return 1;
      return renderableMesh_0.int_7 - renderableMesh_1.int_7;
    }
  }
}

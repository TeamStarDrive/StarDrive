// Decompiled with JetBrains decompiler
// Type: ns10.Class71
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace ns10
{
  internal class Class71 : BaseShadowManager
  {
    public Class71(IGraphicsDeviceService graphicsdevicemanager)
      : base(graphicsdevicemanager)
    {
    }

    public void method_0(List<ShadowGroup> list_0, List<ILight> list_1, bool bool_0)
    {
      this.BuildShadowGroups(list_0, list_1, true);
    }

    public override void Clear()
    {
    }

    public override void Unload()
    {
    }
  }
}

// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.LightRigReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Content;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Processors
{
  /// <summary />
  public class LightRigReader_Pro : ContentTypeReader<LightRig>
  {
    /// <summary />
    protected override LightRig Read(ContentReader input, LightRig instance)
    {
      LightRig lightRig = new LightRig();
      lightRig.method_0(input.ReadString());
      lightRig.LightRigFile = input.ReadString();
      lightRig.ProjectFile = input.ReadString();
      lightRig.method_1(input.ReadString());
      Class55.smethod_0(input);
      return lightRig;
    }
  }
}

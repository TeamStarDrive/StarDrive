// Decompiled with JetBrains decompiler
// Type: ns6.Class41
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace EmbeddedResources
{
  internal class Class41 : Class39
  {
    public Class41(GraphicsDevice graphicsdevice)
      : base(graphicsdevice)
    {
      this.CurrentTechnique = this.Techniques["HDRDownSampleConvertIntensityTechnique"];
    }
  }
}

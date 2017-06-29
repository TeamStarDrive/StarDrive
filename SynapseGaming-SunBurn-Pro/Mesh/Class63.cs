// Decompiled with JetBrains decompiler
// Type: ns9.Class63
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace Mesh
{
  internal class Class63
  {
      public bool HasRenderableObjects { get; set; } = true;

      public bool Transparent { get; set; }

      public bool DoubleSided { get; set; }

      public bool CustomShadowGeneration { get; set; }

      public Effect Effect { get; set; }

      public Class62 Objects { get; } = new Class62();

      public void method_0()
    {
      this.HasRenderableObjects = true;
      this.Transparent = false;
      this.DoubleSided = false;
      this.CustomShadowGeneration = false;
      this.Effect = null;
      this.Objects.method_1();
    }
  }
}

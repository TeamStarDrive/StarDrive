// Decompiled with JetBrains decompiler
// Type: ns9.Class63
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace ns9
{
  internal class Class63
  {
    private bool bool_0 = true;
    private Class62 class62_0 = new Class62();
    private bool bool_1;
    private bool bool_2;
    private bool bool_3;
    private Effect effect_0;

    public bool HasRenderableObjects
    {
      get
      {
        return this.bool_0;
      }
      set
      {
        this.bool_0 = value;
      }
    }

    public bool Transparent
    {
      get
      {
        return this.bool_1;
      }
      set
      {
        this.bool_1 = value;
      }
    }

    public bool DoubleSided
    {
      get
      {
        return this.bool_2;
      }
      set
      {
        this.bool_2 = value;
      }
    }

    public bool CustomShadowGeneration
    {
      get
      {
        return this.bool_3;
      }
      set
      {
        this.bool_3 = value;
      }
    }

    public Effect Effect
    {
      get
      {
        return this.effect_0;
      }
      set
      {
        this.effect_0 = value;
      }
    }

    public Class62 Objects
    {
      get
      {
        return this.class62_0;
      }
    }

    public void method_0()
    {
      this.bool_0 = true;
      this.bool_1 = false;
      this.bool_2 = false;
      this.bool_3 = false;
      this.effect_0 = (Effect) null;
      this.class62_0.method_1();
    }
  }
}

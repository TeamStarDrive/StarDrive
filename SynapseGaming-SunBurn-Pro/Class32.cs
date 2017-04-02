// Decompiled with JetBrains decompiler
// Type: Class32
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

internal class Class32
{
  private float float_0 = 10f;
  private float float_1 = 10f;
  private float float_2 = 1f;
  private bool bool_0;

  public float IconScale
  {
    get
    {
      return this.float_2;
    }
    set
    {
      this.float_2 = value;
    }
  }

  public float MoveScale
  {
    get
    {
      return this.float_0;
    }
    set
    {
      this.float_0 = value;
    }
  }

  public float RotationScale
  {
    get
    {
      return this.float_1;
    }
    set
    {
      this.float_1 = value;
    }
  }

  public bool UserHandledView
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
}

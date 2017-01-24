// Decompiled with JetBrains decompiler
// Type: ns4.Attribute5
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  internal class Attribute5 : Attribute4
  {
    private int int_0;
    private double double_0;
    private double double_1;
    private double double_2;

    public int DecimalPlaces
    {
      get
      {
        return this.int_0;
      }
    }

    public double MinValue
    {
      get
      {
        return this.double_0;
      }
    }

    public double MaxValue
    {
      get
      {
        return this.double_1;
      }
    }

    public double Increment
    {
      get
      {
        return this.double_2;
      }
    }

    public Attribute5(int decimalplaces, double minvalue, double maxvalue, double increment)
    {
      this.int_0 = decimalplaces;
      this.double_0 = minvalue;
      this.double_1 = maxvalue;
      this.double_2 = increment;
    }
  }
}

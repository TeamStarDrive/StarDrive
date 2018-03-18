// Decompiled with JetBrains decompiler
// Type: ns4.Attribute5
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property)]
  internal class Attribute5 : Attribute4
  {
      public int DecimalPlaces { get; }

      public double MinValue { get; }

      public double MaxValue { get; }

      public double Increment { get; }

      public Attribute5(int decimalplaces, double minvalue, double maxvalue, double increment)
    {
      this.DecimalPlaces = decimalplaces;
      this.MinValue = minvalue;
      this.MaxValue = maxvalue;
      this.Increment = increment;
    }
  }
}

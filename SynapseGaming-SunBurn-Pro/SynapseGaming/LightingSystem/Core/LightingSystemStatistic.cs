// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemStatistic
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Represents a single numeric statistic, which can be rendered on-screen
  /// or saved to file using the LightingSystemStatistics class.
  /// </summary>
  public class LightingSystemStatistic
  {
      /// <summary>
    /// Current accumulating value being generated over this frame. This is the value
    /// to increment when supplying statistic information. For instance if the statistic
    /// tracks object rendering, then whenever an object is rendered increment the AccumulationValue
    /// by one.
    /// </summary>
    public int AccumulationValue;

      /// <summary>Unique display name for the statistic.</summary>
    public string Name { get; } = string.Empty;

      /// <summary>Categories the statistic is assigned to.</summary>
    public LightingSystemStatisticCategory Category { get; }

      /// <summary>
    /// Fully accumulated value generated during the last frame. This is the display value.
    /// </summary>
    public int Value { get; private set; }

      internal LightingSystemStatistic(string string_1, LightingSystemStatisticCategory lightingSystemStatisticCategory_1)
    {
      this.Name = string_1;
      this.Category = lightingSystemStatisticCategory_1;
    }

    internal void method_0()
    {
      this.Value = this.AccumulationValue;
      this.AccumulationValue = 0;
    }
  }
}

// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemPerformance
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// 
  /// </summary>
  public class LightingSystemPerformance
  {
    private static Dictionary<string, TimeTracker> dictionary_0 = new Dictionary<string, TimeTracker>(64);
    private static TimeTracker timeTracker_0 = new TimeTracker();

    internal static Dictionary<string, TimeTracker> TimeTrackers => dictionary_0;

      /// <summary>
    /// 
    /// </summary>
    /// <param name="codearea"></param>
    /// <returns></returns>
    public static TimeTracker Begin(string codearea)
    {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    [Conditional("ENABLE_TIMETRACKER")]
    public static void Reset()
    {
      foreach (KeyValuePair<string, TimeTracker> keyValuePair in dictionary_0)
      {
        if (keyValuePair.Value.IsRunning)
          throw new Exception("TimeTracker not properly ended.");
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string Dump()
    {
      return "";
    }

    /// <summary>
    /// 
    /// </summary>
    public class TimeTracker
    {
      private Stopwatch stopwatch_0 = new Stopwatch();

      internal float TotalMilliseconds => (float) this.stopwatch_0.Elapsed.TotalMilliseconds;

        internal bool IsRunning => this.stopwatch_0.IsRunning;

        /// <summary>
      /// 
      /// </summary>
      [Conditional("ENABLE_TIMETRACKER")]
      public void Begin()
      {
        this.stopwatch_0.Start();
      }

      /// <summary>
      /// 
      /// </summary>
      [Conditional("ENABLE_TIMETRACKER")]
      public void End()
      {
        this.stopwatch_0.Stop();
      }

      /// <summary>
      /// 
      /// </summary>
      [Conditional("ENABLE_TIMETRACKER")]
      public void Reset()
      {
        this.stopwatch_0.Reset();
      }
    }
  }
}

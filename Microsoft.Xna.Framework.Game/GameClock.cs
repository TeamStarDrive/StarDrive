// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameClock
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework
{
  internal class GameClock
  {
    private long baseRealTime;
    private long lastRealTime;
    private bool lastRealTimeValid;
    private int suspendCount;
    private long suspendStartTime;
    private long timeLostToSuspension;
    private TimeSpan currentTimeOffset;
    private TimeSpan currentTimeBase;
    private TimeSpan elapsedTime;
    private TimeSpan elapsedAdjustedTime;

    internal TimeSpan CurrentTime
    {
      get
      {
        return this.currentTimeBase + this.currentTimeOffset;
      }
    }

    internal TimeSpan ElapsedTime
    {
      get
      {
        return this.elapsedTime;
      }
    }

    internal TimeSpan ElapsedAdjustedTime
    {
      get
      {
        return this.elapsedAdjustedTime;
      }
    }

    internal static long Counter
    {
      get
      {
        return Stopwatch.GetTimestamp();
      }
    }

    internal static long Frequency
    {
      get
      {
        return Stopwatch.Frequency;
      }
    }

    public GameClock()
    {
      this.Reset();
    }

    internal void Reset()
    {
      this.currentTimeBase = TimeSpan.Zero;
      this.currentTimeOffset = TimeSpan.Zero;
      this.baseRealTime = GameClock.Counter;
      this.lastRealTimeValid = false;
    }

    internal void Step()
    {
      long counter = GameClock.Counter;
      if (!this.lastRealTimeValid)
      {
        this.lastRealTime = counter;
        this.lastRealTimeValid = true;
      }
      try
      {
        this.currentTimeOffset = GameClock.CounterToTimeSpan(counter - this.baseRealTime);
      }
      catch (OverflowException ex1)
      {
        this.currentTimeBase += this.currentTimeOffset;
        this.baseRealTime = this.lastRealTime;
        try
        {
          this.currentTimeOffset = GameClock.CounterToTimeSpan(counter - this.baseRealTime);
        }
        catch (OverflowException ex2)
        {
          this.baseRealTime = counter;
          this.currentTimeOffset = TimeSpan.Zero;
        }
      }
      try
      {
        this.elapsedTime = GameClock.CounterToTimeSpan(counter - this.lastRealTime);
      }
      catch (OverflowException ex)
      {
        this.elapsedTime = TimeSpan.Zero;
      }
      try
      {
        long num = this.lastRealTime + this.timeLostToSuspension;
        this.elapsedAdjustedTime = GameClock.CounterToTimeSpan(counter - num);
        this.timeLostToSuspension = 0L;
      }
      catch (OverflowException ex)
      {
        this.elapsedAdjustedTime = TimeSpan.Zero;
      }
      this.lastRealTime = counter;
    }

    internal void Suspend()
    {
      ++this.suspendCount;
      if (this.suspendCount != 1)
        return;
      this.suspendStartTime = GameClock.Counter;
    }

    internal void Resume()
    {
      --this.suspendCount;
      if (this.suspendCount > 0)
        return;
      this.timeLostToSuspension += GameClock.Counter - this.suspendStartTime;
      this.suspendStartTime = 0L;
    }

    private static TimeSpan CounterToTimeSpan(long delta)
    {
      long num = 10000000;
      return TimeSpan.FromTicks(checked (delta * num) / GameClock.Frequency);
    }
  }
}

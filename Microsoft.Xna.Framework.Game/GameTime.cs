// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameTime
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework
{
  public class GameTime
  {
    private TimeSpan totalRealTime;
    private TimeSpan totalGameTime;
    private TimeSpan elapsedRealTime;
    private TimeSpan elapsedGameTime;
    private bool isRunningSlowly;

    public TimeSpan TotalRealTime
    {
      get
      {
        return this.totalRealTime;
      }
      internal set
      {
        this.totalRealTime = value;
      }
    }

    public TimeSpan TotalGameTime
    {
      get
      {
        return this.totalGameTime;
      }
      internal set
      {
        this.totalGameTime = value;
      }
    }

    public TimeSpan ElapsedRealTime
    {
      get
      {
        return this.elapsedRealTime;
      }
      internal set
      {
        this.elapsedRealTime = value;
      }
    }

    public TimeSpan ElapsedGameTime
    {
      get
      {
        return this.elapsedGameTime;
      }
      internal set
      {
        this.elapsedGameTime = value;
      }
    }

    public bool IsRunningSlowly
    {
      get
      {
        return this.isRunningSlowly;
      }
      internal set
      {
        this.isRunningSlowly = value;
      }
    }

    public GameTime()
    {
    }

    public GameTime(TimeSpan totalRealTime, TimeSpan elapsedRealTime, TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool isRunningSlowly)
    {
      this.totalRealTime = totalRealTime;
      this.elapsedRealTime = elapsedRealTime;
      this.totalGameTime = totalGameTime;
      this.elapsedGameTime = elapsedGameTime;
      this.isRunningSlowly = isRunningSlowly;
    }

    public GameTime(TimeSpan totalRealTime, TimeSpan elapsedRealTime, TimeSpan totalGameTime, TimeSpan elapsedGameTime)
      : this(totalRealTime, elapsedRealTime, totalGameTime, elapsedGameTime, false)
    {
    }
  }
}

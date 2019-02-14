// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameHost
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework
{
  internal abstract class GameHost
  {
    internal abstract GameWindow Window { get; }

    internal event EventHandler Suspend;

    internal event EventHandler Resume;

    internal event EventHandler Activated;

    internal event EventHandler Deactivated;

    internal event EventHandler Idle;

    internal event EventHandler Exiting;

    internal abstract void Run();
    internal abstract void RunOne();

    internal abstract void Exit();

    protected void OnSuspend()
    {
      if (this.Suspend == null)
        return;
      this.Suspend((object) this, EventArgs.Empty);
    }

    protected void OnResume()
    {
      if (this.Resume == null)
        return;
      this.Resume((object) this, EventArgs.Empty);
    }

    protected void OnActivated()
    {
      if (this.Activated == null)
        return;
      this.Activated((object) this, EventArgs.Empty);
    }

    protected void OnDeactivated()
    {
      if (this.Deactivated == null)
        return;
      this.Deactivated((object) this, EventArgs.Empty);
    }

    protected void OnIdle()
    {
      if (this.Idle == null)
        return;
      this.Idle((object) this, EventArgs.Empty);
    }

    protected void OnExiting()
    {
      if (this.Exiting == null)
        return;
      this.Exiting((object) this, EventArgs.Empty);
    }

    internal virtual bool ShowMissingRequirementMessage(Exception exception)
    {
      return false;
    }
  }
}

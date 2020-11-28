// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GamerServices.GamerServicesComponent
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework.GamerServices
{
  public class GamerServicesComponent : GameComponent
  {
    public GamerServicesComponent(Game game)
      : base(game)
    {
    }

    public override void Initialize()
    {
      GamerServicesDispatcher.WindowHandle = this.Game.Window.Handle;
      GamerServicesDispatcher.InstallingTitleUpdate += new EventHandler<EventArgs>(this.GamerServicesDispatcher_InstallingTitleUpdate);
      GamerServicesDispatcher.Initialize((IServiceProvider) this.Game.Services);
      base.Initialize();
    }

    public override void Update(float deltaTime)
    {
      GamerServicesDispatcher.Update();
      base.Update(deltaTime);
    }

    private void GamerServicesDispatcher_InstallingTitleUpdate(object sender, EventArgs e)
    {
      this.Game.Exit();
    }
  }
}

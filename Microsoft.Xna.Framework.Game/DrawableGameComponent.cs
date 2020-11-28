// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.DrawableGameComponent
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework
{
    public class DrawableGameComponent : GameComponent, IDrawable
    {
        private bool initialized;
        private IGraphicsDeviceService deviceService;

        public bool Visible { get; set; } = true;

        public int DrawOrder { get; set; }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                if (deviceService == null)
                    throw new InvalidOperationException(Resources.PropertyCannotBeCalledBeforeInitialize);
                return deviceService.GraphicsDevice;
            }
        }

        public DrawableGameComponent(Game game) : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!initialized)
            {
                deviceService = Game.Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                if (deviceService == null)
                    throw new InvalidOperationException(Resources.MissingGraphicsDeviceService);
                deviceService.DeviceCreated   += DeviceCreated;
                deviceService.DeviceResetting += DeviceResetting;
                deviceService.DeviceReset     += DeviceReset;
                deviceService.DeviceDisposing += DeviceDisposing;
                if (deviceService.GraphicsDevice != null)
                {
                    LoadContent();
                }
            }
            initialized = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.UnloadContent();
                if (this.deviceService != null)
                {
                    this.deviceService.DeviceCreated   -= DeviceCreated;
                    this.deviceService.DeviceResetting -= DeviceResetting;
                    this.deviceService.DeviceReset     -= DeviceReset;
                    this.deviceService.DeviceDisposing -= DeviceDisposing;
                }
            }
            base.Dispose(disposing);
        }

        private void DeviceResetting(object sender, EventArgs e)
        {
        }

        private void DeviceReset(object sender, EventArgs e)
        {
        }

        private void DeviceCreated(object sender, EventArgs e)
        {
            LoadContent();
        }

        private void DeviceDisposing(object sender, EventArgs e)
        {
            UnloadContent();
        }

        public virtual void Draw(float deltaTime)
        {
        }

        protected virtual void LoadContent()
        {
        }

        protected virtual void UnloadContent()
        {
        }
    }
}

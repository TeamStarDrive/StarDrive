using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    // A simplified dummy game setup
    // for minimal testing
    public class GameDummy : Game
    {
        public GraphicsDeviceManager Graphics;
        public new GameContentManager Content { get; }

        PresentationParameters Presentation => Graphics.GraphicsDevice.PresentationParameters;
        public Vector2 ScreenSize => new Vector2(Presentation.BackBufferWidth, Presentation.BackBufferHeight);

        SpriteBatch batch;
        public SpriteBatch Batch => batch ?? (batch = new SpriteBatch(GraphicsDevice));

        public Form Form => (Form)Control.FromHandle(Window.Handle);

        public GameDummy(int width=800, int height=600, bool show=false)
        {
            base.Content = Content = new GameContentManager(Services, "Game");

            Graphics = new GraphicsDeviceManager(this)
            {
                MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
                MinimumVertexShaderProfile = ShaderProfile.VS_2_0,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = false
            };

            Graphics.PreferredBackBufferWidth = width;
            Graphics.PreferredBackBufferHeight = height;
            Graphics.SynchronizeWithVerticalRetrace = false;
            if (Graphics.IsFullScreen)
                Graphics.ToggleFullScreen();
            Graphics.ApplyChanges();

            Show();
        }

        public void Show()
        {
            Form.Visible = true;
        }

        public void Hide()
        {
            Form.Visible = false;
        }

        public void Create()
        {
            var manager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
            manager?.CreateDevice();
            base.Initialize();
        }
    }
}

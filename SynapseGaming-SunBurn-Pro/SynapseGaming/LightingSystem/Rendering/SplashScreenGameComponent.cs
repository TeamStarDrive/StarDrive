// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SplashScreenGameComponent
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>Displays the SunBurn splash screen.</summary>
    public sealed class SplashScreenGameComponent : DrawableGameComponent
    {

        private readonly SplashScreen Splash;

        /// <summary>
        /// Used to determine when the SunBurn splash screen is finished displaying
        /// and it's safe to begin game rendering.
        /// </summary>
        public static bool DisplayComplete => SplashScreen.DisplayComplete;

        /// <summary>
        /// Used to enable or disable the SunBurn splash screen during development. Enabling the splash
        /// screen helps when making sure the screen displays properly in released projects.
        /// </summary>
        public bool ShowDuringDevelopment
        {
            get => Splash.ShowDuringDevelopment;
            set => Splash.ShowDuringDevelopment = value;
        }

        /// <summary>Creates a new SplashScreenGameComponent instance.</summary>
        /// <param name="game"></param>
        /// <param name="graphicsdevicemanager"></param>
        public SplashScreenGameComponent(Game game, IGraphicsDeviceService graphicsdevicemanager) : base(game)
        {
            DrawOrder = int.MaxValue;
            Splash = new SplashScreen(graphicsdevicemanager);
        }

        /// <summary>
        /// Called when the DrawOrder property changes. Raises the DrawOrderChanged event.
        /// </summary>
        /// <param name="sender">The DrawableGameComponent.</param>
        /// <param name="args">Arguments to the DrawOrderChanged event.</param>

        protected override void OnDrawOrderChanged(object sender, EventArgs e)
        {
            if (DrawOrder != int.MaxValue)
                DrawOrder = int.MaxValue;
            base.OnDrawOrderChanged(sender, e);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded. Override this method to
        /// unload any component-specific graphics resources.
        /// </summary>
        protected override void UnloadContent()
        {
            Splash.Unload();
            base.UnloadContent();
        }

        /// <summary>
        /// Called when the GameComponent needs to be updated. Override this method with
        /// component-specific update code.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            Splash.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        /// with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            Splash.Render(gameTime);
            base.Draw(gameTime);
        }
    }
}

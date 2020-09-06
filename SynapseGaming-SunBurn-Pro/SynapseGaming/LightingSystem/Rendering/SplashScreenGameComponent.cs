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
        readonly SplashScreen Splash;

        /// <summary>
        /// Used to determine when the SunBurn splash screen is finished displaying
        /// and it's safe to begin game rendering.
        /// </summary>
        public static bool DisplayComplete => SplashScreen.DisplayComplete;

        /// <summary>Creates a new SplashScreenGameComponent instance.</summary>
        /// <param name="game"></param>
        /// <param name="graphicsdevicemanager"></param>
        public SplashScreenGameComponent(Game game, IGraphicsDeviceService graphicsdevicemanager) : base(game)
        {
            DrawOrder = int.MaxValue;
            Splash = new SplashScreen(graphicsdevicemanager);
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
        /// <param name="deltaTime"></param>
        public override void Update(float deltaTime)
        {
            Splash.Update(deltaTime);
            base.Update(deltaTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        /// with component-specific drawing code.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void Draw(float deltaTime)
        {
            Splash.Render(deltaTime);
            base.Draw(deltaTime);
        }
    }
}

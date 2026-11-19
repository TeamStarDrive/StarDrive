using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics
{
    // TODO Post-1.60: XNA 3.1 MultiSampleType / MultiSampleQuality / GraphicsDeviceCapabilities
    // / GraphicsAdapter.GetCapabilities removed in MonoGame. The MultiSampleCount on
    // PresentationParameters covers the basics — fine for current usage; extra capability
    // querying (POT-only, max RT size, etc.) is a polish item.
    public class RenderTargets
    {
        /// <summary>
        /// Creates a BackBuffer-Compatible RenderTarget
        /// </summary>
        public static RenderTarget2D Create(GraphicsDevice device, int width, int height,
                                            RenderTargetUsage usage = RenderTargetUsage.DiscardContents)
        {
            PresentationParameters pp = device.PresentationParameters;
            CheckTextureSize(width, height, out width, out height);
            // Match the backbuffer's depth format. The pre-migration default was
            // DepthFormat.None (the migration carried it forward), which silently
            // disabled depth testing for everything drawn into MainTarget — closer
            // ships rendered "behind" farther planets because there was no depth
            // buffer to compare against, and the cloud/atmosphere additive blend
            // in DrawPlanets painted right over them.
            return new RenderTarget2D(device, width, height, mipMap: false,
                                      pp.BackBufferFormat, pp.DepthStencilFormat,
                                      pp.MultiSampleCount, usage);
        }

        /// <summary>
        /// Creates a BackBuffer-Compatible RenderTarget the size of the game's screen
        /// </summary>
        public static RenderTarget2D Create(GraphicsDevice device,
                                            RenderTargetUsage usage = RenderTargetUsage.DiscardContents)
        {
            return Create(device, GameBase.ScreenWidth, GameBase.ScreenHeight, usage);
        }

        public static bool CheckTextureSize(int width, int height, out int newWidth, out int newHeight)
        {
            // TODO Post-1.60: GraphicsDeviceCapabilities removed; restore POT/square-only checks
            // by querying GraphicsProfile / GraphicsDevice limits when needed.
            newWidth = width;
            newHeight = height;
            return false;
        }
    }
}

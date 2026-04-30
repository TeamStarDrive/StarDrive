using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics
{
    // TODO Phase 2: XNA 3.1 MultiSampleType / MultiSampleQuality / GraphicsDeviceCapabilities
    // / GraphicsAdapter.GetCapabilities removed in MonoGame. The MultiSampleCount on
    // PresentationParameters covers the basics; the rest is part of Phase 2 capability work.
    public class RenderTargets
    {
        /// <summary>
        /// Creates a BackBuffer-Compatible RenderTarget
        /// </summary>
        public static RenderTarget2D Create(GraphicsDevice device, int width, int height)
        {
            PresentationParameters pp = device.PresentationParameters;
            CheckTextureSize(width, height, out width, out height);
            return new RenderTarget2D(device, width, height, mipMap: false,
                                      pp.BackBufferFormat, DepthFormat.None,
                                      pp.MultiSampleCount, RenderTargetUsage.DiscardContents);
        }

        /// <summary>
        /// Creates a BackBuffer-Compatible RenderTarget which matches BackBuffer size
        /// </summary>
        public static RenderTarget2D Create(GraphicsDevice device)
        {
            PresentationParameters pp = device.PresentationParameters;
            return Create(device, pp.BackBufferWidth, pp.BackBufferHeight);
        }

        public static bool CheckTextureSize(int width, int height, out int newWidth, out int newHeight)
        {
            // TODO Phase 2: GraphicsDeviceCapabilities removed; restore POT/square-only checks
            // by querying GraphicsProfile / GraphicsDevice limits when needed.
            newWidth = width;
            newHeight = height;
            return false;
        }
    }
}

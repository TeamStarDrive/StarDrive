using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics
{
    public class RenderTargets
    {
        /// <summary>
        /// Creates a BackBuffer-Compatible RenderTarget
        /// </summary>
        public static RenderTarget2D Create(GraphicsDevice device, int width, int height)
        {
            PresentationParameters pp = device.PresentationParameters;
            MultiSampleType type = pp.MultiSampleType;
            SurfaceFormat format = pp.BackBufferFormat;
            GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
            if (!adapter.CheckDeviceFormat(DeviceType.Hardware, adapter.CurrentDisplayMode.Format, 
                TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, format))
            {
                format = device.DisplayMode.Format;
            }
            else if (!adapter.CheckDeviceMultiSampleType(DeviceType.Hardware, format, pp.IsFullScreen, type))
            {
                type = MultiSampleType.None;
            }
            CheckTextureSize(width, height, out width, out height);
            return new RenderTarget2D(device, width, height, 1, format, type, pp.MultiSampleQuality);
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
            bool retVal = false;
            GraphicsDeviceCapabilities caps = GraphicsAdapter.DefaultAdapter.GetCapabilities(DeviceType.Hardware);
            if (caps.TextureCapabilities.RequiresPower2)
            {
                retVal = true;
                double exp = Math.Ceiling(Math.Log(width) / Math.Log(2));
                width = (int)Math.Pow(2, exp);
                exp = Math.Ceiling(Math.Log(height) / Math.Log(2));
                height = (int)Math.Pow(2, exp);
            }
            if (caps.TextureCapabilities.RequiresSquareOnly)
            {
                retVal = true;
                width = Math.Max(width, height);
                height = width;
            }
            newWidth = Math.Min(caps.MaxTextureWidth, width);
            newHeight = Math.Min(caps.MaxTextureHeight, height);
            return retVal;
        }
    }
}

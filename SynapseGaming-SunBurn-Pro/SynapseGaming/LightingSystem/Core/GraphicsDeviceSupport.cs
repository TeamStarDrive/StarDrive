// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.GraphicsDeviceSupport
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Provides information on the device capabilities supported by the current hardware
    /// and if those capabilities are allowed by the current configuration.  This allows
    /// users to select various system specific configurations and for developers to test
    /// a wide variety of configurations on a single machine.
    /// 
    /// Check a capability property to see if it's supported and allowed.  Setting a
    /// capability property to true will allow it only if the current hardware supports it.
    /// </summary>
    public class GraphicsDeviceSupport
    {
        bool IsStencilTwoSided = true;
        readonly bool DeviceSupportsTwoSidedStencil;

        public GraphicsDevice Device { get; }

        /// <summary>
        /// Checks if two sided stencil rendering is supported by the hardware and allowed by the current configuration.
        /// </summary>
        public bool StencilTwoSided
        {
            get => CurrentOrDeviceDefault(IsStencilTwoSided, DeviceSupportsTwoSidedStencil);
            set => IsStencilTwoSided = value;
        }

        
        internal static bool CurrentOrDeviceDefault(bool currentValue, bool isSupported)
        {
            if (!currentValue)
                return false;
            return isSupported;
        }

        /// <summary>The maximum texture size supported by the hardware.</summary>
        public int MaxTextureSize { get; }

        /// <summary>
        /// The maximum anisotropy value supported by the hardware.
        /// </summary>
        public int MaxAnisotropy { get; }

        /// <summary>
        /// The maximum pixel shader version supported by the hardware.
        /// </summary>
        public int PixelShaderMajorVersion { get; }

        /// <summary>
        /// The maximum pixel shader version supported by the hardware.
        /// </summary>
        public int PixelShaderMinorVersion { get; }

        /// <summary>List of surface formats and related hardware support.</summary>
        public Dictionary<SurfaceFormat, FormatSupport> SurfaceFormat { get; } = new Dictionary<SurfaceFormat, FormatSupport>(16);

        internal GraphicsDeviceSupport(GraphicsDevice device)
        {
            Device = device;
            GraphicsDeviceCapabilities capabilities = device.GraphicsDeviceCapabilities;
            DeviceSupportsTwoSidedStencil = capabilities.StencilCapabilities.SupportsTwoSided;
            MaxTextureSize = capabilities.MaxTextureWidth;
            MaxAnisotropy = capabilities.MaxAnisotropy;
            PixelShaderMajorVersion = capabilities.PixelShaderVersion.Major;
            PixelShaderMinorVersion = capabilities.PixelShaderVersion.Minor;

            foreach (SurfaceFormat format in Enum.GetValues(typeof(SurfaceFormat)))
            {
                SurfaceFormat.Add(format, new FormatSupport(Device, format));
            }
        }

        /// <summary>
        /// Finds the first supported surface format in a list of requested formats. Always sort the
        /// requested format list in order of preference to ensure the supported and returned format is the best possible match.
        /// </summary>
        /// <param name="requestedformats">List of requested formats.</param>
        /// <param name="texture">Returned format must support use as a texture.</param>
        /// <param name="rendertarget">Returned format must support use as a render target.</param>
        /// <param name="blending">Returned format must support render target blending.</param>
        /// <param name="filtering">Returned format must support texture filtering.</param>
        /// <returns></returns>
        public SurfaceFormat FindSupportedFormat(SurfaceFormat[] requestedformats, bool texture, bool rendertarget, bool blending, bool filtering)
        {
            for (int index = 0; index < requestedformats.Length; ++index)
            {
                SurfaceFormat requested = requestedformats[index];
                if ((!texture || SurfaceFormat[requested].Texture) &&
                    (!rendertarget || SurfaceFormat[requested].RenderTarget) &&
                    (!blending || SurfaceFormat[requested].Blending) &&
                    (!filtering || SurfaceFormat[requested].Filtering))
                    return requested;
            }
            return Microsoft.Xna.Framework.Graphics.SurfaceFormat.Unknown;
        }

        /// <summary>
        /// Finds the first supported depth format in a list of requested formats. Always sort the requested
        /// format list in order of preference to ensure the supported and returned format is the best possible match.
        /// </summary>
        /// <param name="requestedformats">List of requested formats.</param>
        /// <param name="rendertargetformat">Format of the render target or back buffer the requested format will be paired with.</param>
        /// <returns></returns>
        public DepthFormat FindSupportedFormat(DepthFormat[] requestedformats, SurfaceFormat rendertargetformat)
        {
            for (int index = 0; index < requestedformats.Length; ++index)
            {
                DepthFormat requestedformat = requestedformats[index];
                if (SurfaceFormat[(SurfaceFormat)requestedformat].DepthBuffer && GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(DeviceType.Hardware, Device.DisplayMode.Format, rendertargetformat, requestedformat))
                    return requestedformat;
            }
            return DepthFormat.Unknown;
        }

        /// <summary>
        /// Describes the hardware support for a specific surface format.
        /// </summary>
        public class FormatSupport
        {
            bool DepthBufferEnabled = true;
            bool TextureEnabled = true;
            bool RenderTargetEnabled = true;
            bool BlendingEnable = true;
            bool FilteringEnabled = true;
            readonly bool DepthBufferSupported;
            readonly bool TextureSupported;
            readonly bool RenderTargetSupported;
            readonly bool BlendingSupported;
            readonly bool FilteringSupported;

            /// <summary>
            /// Hardware supports using the format as a depth texture.
            /// </summary>
            public bool DepthBuffer
            {
                get => CurrentOrDeviceDefault(DepthBufferEnabled, DepthBufferSupported);
                set => DepthBufferEnabled = value;
            }

            /// <summary>Hardware supports using the format as a texture.</summary>
            public bool Texture
            {
                get => CurrentOrDeviceDefault(TextureEnabled, TextureSupported);
                set => TextureEnabled = value;
            }

            /// <summary>
            /// Hardware supports using the format as a render target.
            /// </summary>
            public bool RenderTarget
            {
                get => CurrentOrDeviceDefault(RenderTargetEnabled, RenderTargetSupported);
                set => RenderTargetEnabled = value;
            }

            /// <summary>
            /// Hardware supports blending when using the format as a render target.
            /// </summary>
            public bool Blending
            {
                get => CurrentOrDeviceDefault(BlendingEnable, BlendingSupported);
                set => BlendingEnable = value;
            }

            /// <summary>
            /// Hardware supports filtering when using the format as a texture.
            /// </summary>
            public bool Filtering
            {
                get => CurrentOrDeviceDefault(FilteringEnabled, FilteringSupported);
                set => FilteringEnabled = value;
            }

            internal FormatSupport(GraphicsDevice device, SurfaceFormat format)
            {
                GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
                DepthBufferSupported  = adapter.CheckDeviceFormat(DeviceType.Hardware, device.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.DepthStencilBuffer, format);
                TextureSupported      = adapter.CheckDeviceFormat(DeviceType.Hardware, device.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.Texture2D, format);
                RenderTargetSupported = adapter.CheckDeviceFormat(DeviceType.Hardware, device.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, format);
                FilteringSupported    = adapter.CheckDeviceFormat(DeviceType.Hardware, device.DisplayMode.Format, TextureUsage.None, QueryUsages.Filter, ResourceType.Texture2D, format);
                BlendingSupported     = adapter.CheckDeviceFormat(DeviceType.Hardware, device.DisplayMode.Format, TextureUsage.None, QueryUsages.PostPixelShaderBlending, ResourceType.RenderTarget, format);
            }
        }
    }
}

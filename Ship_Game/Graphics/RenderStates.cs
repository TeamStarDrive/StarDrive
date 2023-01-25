using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Graphics
{
    public class RenderStates
    {
        public static void EnableTextureWrap(GraphicsDevice device)
        {
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
        }

        public static void DisableAlphaBlend(GraphicsDevice device)
        {
            device.RenderState.AlphaBlendEnable = false;
        }

        // Common blend modes:
        // src*SrcAlpha + dst*One -- brighter, destination is fully preserved
        // src*SrcAlpha + dst*InvSrcAlpha -- dimmer, part of destination is removed, perfect for classic cutout blends
        //                                   final color = src color * src alpha + dest color * (1-src alpha)
        // https://takinginitiative.wordpress.com/2010/04/09/directx-10-tutorial-6-transparency-and-alpha-blending/
        public static void EnableAlphaBlend(GraphicsDevice device, Blend srcBlend, Blend dstBlend)
        {
            var rs = device.RenderState;
            rs.AlphaBlendEnable = true;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.SourceBlend = srcBlend;
            rs.DestinationBlend = dstBlend;
        }
        
        // src*SrcAlpha + dst*InvSrcAlpha -- dimmer, part of destination is removed, perfect for classic cutout blends
        public static void EnableClassicAlphaBlend(GraphicsDevice device)
        {
            EnableAlphaBlend(device, Blend.SourceAlpha, Blend.InverseSourceAlpha);
        }

        // src*SrcAlpha + dst*One -- brighter, destination is fully preserved
        public static void EnableAdditiveAlphaBlend(GraphicsDevice device)
        {
            EnableAlphaBlend(device, Blend.SourceAlpha, Blend.One);
        }

        /// <summary>
        /// Enables Alpha Testing of pixels.
        /// For example `CompareFunction.Greater` with `referenceAlpha=0` will
        /// keep pixels with alpha greater than referenceAlpha
        /// </summary>
        public static void EnableAlphaTest(GraphicsDevice device, CompareFunction compare, int referenceAlpha = 0)
        {
            var rs = device.RenderState;
            rs.AlphaTestEnable = true;
            rs.AlphaFunction = compare;
            rs.ReferenceAlpha = referenceAlpha;
        }

        public static void DisableAlphaTest(GraphicsDevice device)
        {
            device.RenderState.AlphaTestEnable = false;
        }

        public static void SetCullMode(GraphicsDevice device, CullMode mode)
        {
            device.RenderState.CullMode = mode;
        }

        public static void EnableDepthWrite(GraphicsDevice device)
        {
            var rs = device.RenderState;
            rs.DepthBufferEnable = true;
            rs.DepthBufferWriteEnable = true;
        }

        public static void DisableDepthWrite(GraphicsDevice device)
        {
            var rs = device.RenderState;
            //rs.DepthBufferEnable = true;
            rs.DepthBufferWriteEnable = false;
        }

        public static void EnableMultiSampleAA(GraphicsDevice device)
        {
            device.RenderState.MultiSampleAntiAlias = true;
        }

        public static void EnableScissorTest(GraphicsDevice device, in Rectangle rect)
        {
            device.RenderState.ScissorTestEnable = true;
            device.ScissorRectangle = rect;
        }

        public static void DisableScissorTest(GraphicsDevice device)
        {
            device.RenderState.ScissorTestEnable = false;
        }

        /// <summary>
        /// RenderState with blend and DepthWrite+Culling disabled
        /// additive:true  SrcAlpha/One
        /// additive:false SrcAlpha/InvSrcAlpha 
        /// </summary>
        public static void BasicBlendMode(GraphicsDevice device, bool additive, bool depthWrite)
        {
            EnableTextureWrap(device);
            SetCullMode(device, CullMode.None);

            // for most use cases, alpha-testing should be disabled to enable smooth blending
            DisableAlphaTest(device);

            if (additive)
                EnableAdditiveAlphaBlend(device); // SrcAlpha + One
            else
                EnableClassicAlphaBlend(device);  // SrcAlpha + InvSrcAlpha

            if (depthWrite)
                EnableDepthWrite(device);
            else
                DisableDepthWrite(device);
        }

        /// <summary>
        /// This allows blend mode controls for only the alpha channel,
        /// leaving regular RGB channel alpha unaffected.
        /// </summary>
        public static void EnableSeparateAlphaBlend(GraphicsDevice device, Blend srcBlend, Blend dstBlend)
        {
            var rs = device.RenderState;
            rs.SeparateAlphaBlendEnabled = true; // enables AlphaSourceBlend & AlphaDestinationBlend
            rs.AlphaSourceBlend = srcBlend; // default:One
            rs.AlphaDestinationBlend = dstBlend; // default:One
        }

        /// <summary>
        /// Returns to regular state where regular blend mode applies to all channels
        /// </summary>
        public static void DisableSeparateAlphaChannelBlend(GraphicsDevice device)
        {
            device.RenderState.SeparateAlphaBlendEnabled = false;
        }
    }
}

using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Graphics
{
    // Phase 3.7: composed state-object replacements for the XNA 3.1 device.RenderState
    // sub-properties (AlphaBlendEnable, SourceBlend/DestinationBlend, AlphaTestEnable,
    // AlphaFunction/ReferenceAlpha, etc.) that MonoGame removed. Each call assigns a
    // cached BlendState/DepthStencilState/RasterizerState to GraphicsDevice.
    public class RenderStates
    {
        public static void EnableTextureWrap(GraphicsDevice device)
        {
            device.SamplerStates[0] = SamplerState.LinearWrap;
        }

        public static void DisableAlphaBlend(GraphicsDevice device)
        {
            device.BlendState = BlendState.Opaque;
        }

        // Phase 3.7: cache custom BlendStates by (src,dst) pair. The MainMenu Mars
        // overlays (Lights_edge / Dust / Aurora / Lights_center) call this with
        // (InverseDestinationColor, One) for a screen-like additive that brightens
        // without saturating; without it, the overlays draw in plain alpha-blend
        // and the planet's limb / city-lights effects collapse.
        static readonly ConcurrentDictionary<(Blend, Blend), BlendState> BlendCache = new();
        public static void EnableAlphaBlend(GraphicsDevice device, Blend srcBlend, Blend dstBlend)
        {
            BlendState bs = BlendCache.GetOrAdd((srcBlend, dstBlend), key => new BlendState
            {
                Name = $"Custom-{key.Item1}-{key.Item2}",
                ColorSourceBlend = key.Item1,
                AlphaSourceBlend = key.Item1,
                ColorDestinationBlend = key.Item2,
                AlphaDestinationBlend = key.Item2,
                ColorBlendFunction = BlendFunction.Add,
                AlphaBlendFunction = BlendFunction.Add,
            });
            device.BlendState = bs;
        }

        public static void EnableClassicAlphaBlend(GraphicsDevice device)
        {
            device.BlendState = BlendState.AlphaBlend;
        }

        public static void EnableAdditiveAlphaBlend(GraphicsDevice device)
        {
            device.BlendState = BlendState.Additive;
        }

        public static void EnableAlphaTest(GraphicsDevice device, CompareFunction compare, int referenceAlpha = 0) { }

        public static void DisableAlphaTest(GraphicsDevice device) { }

        static readonly RasterizerState CullCWState   = new() { CullMode = CullMode.CullClockwiseFace };
        static readonly RasterizerState CullCCWState  = new() { CullMode = CullMode.CullCounterClockwiseFace };
        static readonly RasterizerState CullNoneState = new() { CullMode = CullMode.None };

        public static void SetCullMode(GraphicsDevice device, CullMode mode)
        {
            device.RasterizerState = mode switch
            {
                CullMode.CullClockwiseFace        => CullCWState,
                CullMode.CullCounterClockwiseFace => CullCCWState,
                _                                 => CullNoneState,
            };
        }

        public static void EnableDepthWrite(GraphicsDevice device)
        {
            device.DepthStencilState = DepthStencilState.Default;
        }

        public static void DisableDepthWrite(GraphicsDevice device)
        {
            device.DepthStencilState = DepthStencilState.DepthRead;
        }

        public static void EnableMultiSampleAA(GraphicsDevice device) { }

        // Cached rasterizer state mirroring SpriteBatch's default
        // (CullCounterClockwise, FillSolid) but with ScissorTestEnable=true.
        // SpriteBatch.Begin captures the RasterizerState at the call; setting
        // GraphicsDevice.ScissorRectangle without enabling scissor on the
        // bound rasterizer is a no-op under MonoGame (XNA used the live
        // device.RenderState.ScissorTestEnable, which is gone in MG).
        public static readonly RasterizerState ScissorEnabled = new()
        {
            Name = "ScissorEnabled",
            CullMode = CullMode.CullCounterClockwiseFace,
            ScissorTestEnable = true,
        };

        public static void EnableScissorTest(GraphicsDevice device, in Rectangle rect)
        {
            device.ScissorRectangle = rect;
            device.RasterizerState = ScissorEnabled;
        }

        public static void DisableScissorTest(GraphicsDevice device)
        {
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public static void BasicBlendMode(GraphicsDevice device, bool additive, bool depthWrite)
        {
            EnableTextureWrap(device);
            SetCullMode(device, CullMode.None);
            DisableAlphaTest(device);
            if (additive) EnableAdditiveAlphaBlend(device);
            else          EnableClassicAlphaBlend(device);
            if (depthWrite) EnableDepthWrite(device);
            else            DisableDepthWrite(device);
        }

        public static void EnableSeparateAlphaBlend(GraphicsDevice device, Blend srcBlend, Blend dstBlend) { }
        public static void DisableSeparateAlphaChannelBlend(GraphicsDevice device) { }
    }
}

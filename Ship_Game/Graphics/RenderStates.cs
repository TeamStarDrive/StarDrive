using Microsoft.Xna.Framework.Graphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Graphics
{
    // TODO Phase 2: XNA 3.1 device.RenderState (and its sub-properties: AlphaBlendEnable,
    // AlphaTestEnable, CullMode, DepthBufferEnable, ScissorTestEnable, MultiSampleAntiAlias,
    // SeparateAlphaBlendEnabled, SourceBlend/DestinationBlend, AlphaFunction, etc.) was removed
    // in MonoGame and replaced by BlendState / DepthStencilState / RasterizerState objects
    // assigned to GraphicsDevice. The whole call surface here is currently no-ops; rebuild
    // these helpers to compose MonoGame state objects when 3D rendering comes back online.
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

        public static void EnableAlphaBlend(GraphicsDevice device, Blend srcBlend, Blend dstBlend) { }

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

        public static void SetCullMode(GraphicsDevice device, CullMode mode)
        {
            // TODO Phase 2: build a RasterizerState with the requested CullMode.
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

        public static void EnableScissorTest(GraphicsDevice device, in Rectangle rect)
        {
            device.ScissorRectangle = rect;
        }

        public static void DisableScissorTest(GraphicsDevice device) { }

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

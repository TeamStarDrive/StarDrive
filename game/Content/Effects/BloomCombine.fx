//-----------------------------------------------------------------------------
// BloomCombine.fx
//
// Phase 3.7 step 1 hand-rewrite of the XNA 3.1 BloomCombine effect that
// ships as game/Content/Effects/BloomCombine.xnb. Compiles to .mgfxo and
// wins via the .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Stage 4 of the bloom pipeline. Composites the (already extracted +
// double-blurred) bloom RT against the original scene texture with
// per-channel saturation + intensity controls.
//
// Sampler bindings:
//   s0 = BloomSampler  — the blurred bright-pass RT, supplied by
//                        SpriteBatch.Draw(rt1, ...). The SpriteBatch
//                        texture argument always lands on register s0.
//   BaseTexture        — the original scene RT, bound through the
//                        parameter-driven sampler-state below. Bind via
//                        Effect.Parameters["BaseTexture"].SetValue(scene).
//
// Why the asymmetry: the canonical XNA sample bound BaseSampler at s1
// directly via device.Textures[1], which is not portable across modern
// MonoGame on every platform (the SpriteBatch.End flush sometimes
// discards explicit s1 bindings — see BasicFogOfWar §3.3 attempt). The
// parameter-driven sampler-state is the safe MGFX pattern.
//
// VS+PS — see BloomExtract.fx for why we don't ship PS-only.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler BloomSampler : register(s0);

texture BaseTexture;
sampler2D BaseSampler : register(s1) = sampler_state
{
    Texture   = (BaseTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

float BloomIntensity;
float BaseIntensity;
float BloomSaturation;
float BaseSaturation;

struct VSInput  { float4 Position : POSITION0; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };
struct VSOutput { float4 Position : SV_POSITION; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };

VSOutput VSPassthrough(VSInput input)
{
    VSOutput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color    = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 AdjustSaturation(float4 color, float saturation)
{
    // BT.601 luma — same constants as desaturate.fx for consistency.
    float grey = dot(color.rgb, float3(0.3, 0.59, 0.11));
    return lerp(grey.xxxx, color, saturation);
}

float4 PSBloomCombine(VSOutput input) : SV_TARGET
{
    float4 bloom = tex2D(BloomSampler, input.TexCoord);
    float4 base  = tex2D(BaseSampler,  input.TexCoord);

    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base  = AdjustSaturation(base,  BaseSaturation)  * BaseIntensity;

    base *= (1 - saturate(bloom));

    return base + bloom;
}

technique BloomCombine
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_1 PSBloomCombine();
    }
}

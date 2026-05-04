//-----------------------------------------------------------------------------
// scale.fx
//
// Phase 3.3 hand-rewrite of the XNA 3.1 "scale" effect that ships as
// game/Content/Effects/scale.xnb. The original was D3DX fx_2_0; MonoGame's
// MGFX reader rejects that bytecode. The .xnb stays on disk for mod
// compatibility; this .fx is compiled to .mgfxo and wins via the
// .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Behavior matches the original vs_1_1 + ps_2_0 bytecode (disassembly under
// C:/Users/gkapu/.claude/plans/phase3-logs/scale-chunks/):
//
//   VS:  oPos = mul(Position, WorldViewProjection)
//        oT0  = (TexCoord - 0.5) * scale + 0.5    // zoom UV around center
//
//   PS:  uvA   = displacement                      // fixed UV into AlphaMap
//        a    = tex2D(AlphaMapSampler, uvA).r     // mask alpha
//        col  = tex2D(texsampler, oT0)
//        return float4(col.rgb, col.a * a)        // diffuse with masked alpha
//
// Used by ShieldManager.cs to draw shields:
//   - tex          : shield_d.dds diffuse
//   - AlphaMap     : shieldgradient.png   (red channel = alpha mask)
//   - World/View/Projection
//   - scale        : float fed in via Parameters[].SetValue(float) — broadcast
//                    by MonoGame to (s, s); animates UV zoom around center
//   - displacement : float fed in via Parameters[].SetValue(float) — broadcast
//                    by MonoGame to (d, d); walks the gradient texture
//                    diagonally to produce shield wobble.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 View;
float4x4 Projection;
float2   scale;
float2   displacement;

texture tex;
sampler2D texsampler = sampler_state
{
    Texture   = (tex);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

texture AlphaMap;
sampler2D AlphaMapSampler = sampler_state
{
    Texture   = (AlphaMap);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSScale(VSInput input)
{
    VSOutput output;
    float4x4 wvp = mul(mul(World, View), Projection);
    output.Position = mul(input.Position, wvp);
    output.TexCoord = (input.TexCoord - 0.5) * scale + 0.5;
    return output;
}

float4 PSScale(VSOutput input) : COLOR0
{
    float  alphaMask = tex2D(AlphaMapSampler, displacement).r;
    float4 col       = tex2D(texsampler, input.TexCoord);
    return float4(col.rgb, col.a * alphaMask);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSScale();
        PixelShader  = compile ps_4_0_level_9_1 PSScale();
    }
}

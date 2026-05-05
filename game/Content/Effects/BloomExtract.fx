//-----------------------------------------------------------------------------
// BloomExtract.fx
//
// Phase 3.7 step 1 hand-rewrite of the XNA 3.1 BloomExtract effect that
// ships as game/Content/Effects/BloomExtract.xnb. Compiles to .mgfxo and
// wins via the .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Behavior matches the canonical XNA Bloom Sample: keep only the portion
// of the source pixel that exceeds BloomThreshold, rescaled to [0,1].
// Drives stage 1 of the 4-pass bloom pipeline.
//
// Why VS+PS, not PS-only:
// PS-only effects through SpriteBatch (Begin without effect; Pass.Apply
// between Begin and Draw) are unreliable under MonoGame 3.8.1.303 /
// DirectX_11 — the manual Apply doesn't always replace SpriteEffect's PS
// in time for the immediate-mode flush. The canonical MonoGame Bloom
// Sample ships full VS+PS and passes the effect to SpriteBatch.Begin's
// `effect:` argument; that path is exercised by the in-tree tests and
// works deterministically.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler TextureSampler : register(s0);
float BloomThreshold;

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

float4 PSBloomExtract(VSOutput input) : SV_TARGET
{
    float4 c = tex2D(TextureSampler, input.TexCoord);
    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}

technique BloomExtract
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_1 PSBloomExtract();
    }
}

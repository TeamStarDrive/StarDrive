//-----------------------------------------------------------------------------
// GaussianBlur.fx
//
// Phase 3.7 step 1 hand-rewrite of the XNA 3.1 GaussianBlur effect that
// ships as game/Content/Effects/GaussianBlur.xnb. Compiles to .mgfxo and
// wins via the .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Canonical XNA Bloom Sample 15-tap separable Gaussian. Used twice per
// frame: once horizontally (offsets along x), once vertically (offsets
// along y). C# computes the SampleOffsets / SampleWeights arrays from
// the configured BlurAmount + render-target dimensions.
//
// VS+PS — see BloomExtract.fx for why we don't ship PS-only.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler TextureSampler : register(s0);

#define SAMPLE_COUNT 15
float2 SampleOffsets[SAMPLE_COUNT];
float  SampleWeights[SAMPLE_COUNT];

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

float4 PSGaussianBlur(VSOutput input) : SV_TARGET
{
    float4 c = 0;
    [unroll] for (int i = 0; i < SAMPLE_COUNT; i++)
        c += tex2D(TextureSampler, input.TexCoord + SampleOffsets[i]) * SampleWeights[i];
    return c;
}

technique GaussianBlur
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_1 PSGaussianBlur();
    }
}

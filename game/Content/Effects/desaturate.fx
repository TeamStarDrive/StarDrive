//-----------------------------------------------------------------------------
// desaturate.fx
//
// Phase 4.5.A — converted from PS-only to VS+PS to fix the silent-output
// failure mode under MonoGame 3.8.1.303 / DirectX_11. The previous PS-only
// pattern (manual `Pass.Apply()` between SpriteBatch.Begin and Draw in
// immediate mode) doesn't replace SpriteEffect's pixel shader in time for
// the flush — SpriteBatch's default tint passthrough runs instead, which is
// why neither Saturation=0 nor Saturation=100 produced anything resembling
// the original lerp output. Same pattern + fix as Phase 3.7 step 3 BasicFogOfWar
// and project memory entry project_phase37_spritebatch_matrixtransform.md.
//
// Caller (YouLoseScreen.Draw / YouWinScreen.Draw): pass this effect to
// SpriteBatch.Begin's `effect:` argument so our pass IS the SpriteBatch pass,
// and use the Rectangle-form `batch.Draw(tex, dest, color)` — the
// `position+origin+scale` form silently produces no rasterized fragments
// under SpriteBatch+custom-effect+Immediate mode (verified empirically on
// 2026-05-07: forced-red PS produces black with the position form, red with
// the Rectangle form). Set MatrixTransform manually because SpriteBatch only
// auto-populates it on SpriteEffect-typed effects.
//
// Behavior matches the original ps_2_0 bytecode preserved verbatim from the
// pre-conversion shader: saturate(color.a * 4.0) lerp from texture rgb to
// BT.601 luma. Vertex color α = Saturation/255 controls the fade; weight
// runs 0..1 (saturated) so held-state Saturation=0 passes the texture
// through unchanged and fade-in-start Saturation=100 produces full grayscale.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler  TextureSampler : register(s0);

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

float4 PSDesaturate(VSOutput input) : SV_TARGET
{
    float4 tex   = tex2D(TextureSampler, input.TexCoord);
    float  gray  = dot(tex.rgb, float3(0.299, 0.587, 0.114));
    float  weight = saturate(input.Color.a * 4.0);
    return float4(lerp(tex.rgb, gray.xxx, weight), tex.a);
}

technique Desaturate
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_1 PSDesaturate();
    }
}

//-----------------------------------------------------------------------------
// desaturate.fx
//
// Phase 3.3 hand-rewrite of the XNA 3.1 desaturate effect that ships as
// game/Content/Effects/desaturate.xnb. The original was D3DX fx_2_0-compiled,
// which MonoGame's MGFX reader rejects ("This does not appear to be a MonoGame
// MGFX file!"). The XNB is preserved on disk for mod compatibility; this .fx
// is compiled to .mgfxo and wins via the .xnb -> .mgfxo sibling fallback in
// GameContentManager.
//
// Behavior matches the original ps_2_0 bytecode (decoded from the FX blob):
//   c0 = (0.299, 0.587, 0.114, 4.0)             // BT.601 luma + alpha multiplier
//   gray   = dot(tex.rgb, c0.rgb)
//   weight = saturate(vertexAlpha * c0.w)       // SATURATED — see note below
//   out.rgb = lerp(tex.rgb, gray, weight)
//   out.a   = tex.a
//
// Note on the saturate: the original ps_2_0 bytecode does NOT include a
// `_sat` modifier on the `mul` or the `lrp`, so weight runs up to 1.57 at
// full Saturation (alpha 0.39 from Saturation/255). D3D9's lrp specification
// says "t should be in [0,1], result undefined otherwise"; pre-migration
// drivers evidently clamped at the lrp output or earlier in the pipeline.
// Without an explicit saturate, the unclamped extrapolation `−0.57·orig +
// 1.57·luma` rotates warm-toned pixels toward blue (a battle scene becomes
// uniform navy). Saturate caps the lerp at pure-grayscale instead, which is
// the safer behavior and matches what every modern D3D11 driver effectively
// produces from this bytecode.
//
// Used through MonoGame's SpriteBatch by YouLoseScreen / YouWinScreen — the
// sprite VS stays bound (MonoGame's pass.Apply leaves VS untouched when the
// effect pass has none), so this file ships PS-only.
//-----------------------------------------------------------------------------

sampler TextureSampler : register(s0);

float4 PSDesaturate(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(TextureSampler, uv);
    float gray = dot(tex.rgb, float3(0.299, 0.587, 0.114));
    float weight = saturate(color.a * 4.0);   // bytecode c0.w; saturate caps extrapolation
    return float4(lerp(tex.rgb, gray.xxx, weight), tex.a);
}

technique Desaturate
{
    pass Pass1
    {
        PixelShader = compile ps_4_0_level_9_1 PSDesaturate();
    }
}

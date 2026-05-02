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
//   weight = vertexAlpha * c0.w                 // unclamped — see note below
//   out.rgb = lrp(orig, weight, gray) = orig + weight * (gray - orig)
//   out.a   = tex.a                              (D3D9 ps_2_0 clamps output to [0,1])
//
// Note on the unclamped weight: D3D9's `lrp` does NOT saturate `t`, and the
// bytecode here does not insert a `_sat` modifier on the `mul`. With weight up
// to 1.57 (alpha 0.39 from Saturation/255), this is extrapolation, not lerp,
// and produces a *hue-shifted* partial gray rather than pure luminance. That
// is the original visual — verified by side-by-side against pre-migration
// screenshots. Keep the unclamped multiply or the output goes flat-gray and
// the screen looks washed-out.
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
    float weight = color.a * 4.0;     // unclamped — matches original bytecode
    return float4(lerp(tex.rgb, gray.xxx, weight), tex.a);
}

technique Desaturate
{
    pass Pass1
    {
        PixelShader = compile ps_4_0_level_9_1 PSDesaturate();
    }
}

//-----------------------------------------------------------------------------
// Distort.fx
//
// Phase 3.7 step 2: screen-space shield-hit distortion as a SpriteBatch-
// friendly post-process pass. Compiles to .mgfxo and wins via the
// .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Pre-migration SunBurn used a two-stage pipeline (Distorters.fx generated a
// displacement RT from sphere-mesh + heat-haze techniques, then the legacy
// Distort.fx warped the back buffer by sampling that RT). We collapse both
// stages into one PS that takes scene as s0 and accumulates radial heat-haze
// offsets directly from up to MAX_SHIELDS=8 active shield-hit uniforms.
//
// Why merge: MonoGame 3.8.1.303 can't read the back buffer directly (no
// XNA-equivalent ResolveBackBuffer), so even the two-stage path would
// run on a copy of the scene RT; at that point the displacement RT is
// just a per-pixel function of (uv, ShieldData[i]) which we can evaluate
// inline without the extra render target.
//
// Sampler bindings:
//   s0 = SceneSampler — supplied by SpriteBatch.Draw(scene, ...). The
//                       SpriteBatch texture argument always lands on s0.
//
// Uniforms:
//   ShieldData[i].xy = shield center in destination UV space (0..1, 0=top-left)
//   ShieldData[i].z  = shield radius in destination UV space
//   ShieldData[i].w  = shield-hit intensity (0..1, controls offset strength
//                      and ripple amplitude; 0 disables this slot)
//   Time             = animation phase (seconds since renderer start)
//
// VS+PS (not PS-only) because PS-only through SpriteBatch is unreliable
// under MonoGame 3.8.1.303 / DirectX_11; see BloomExtract.fx header for
// the full reasoning.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler  SceneSampler : register(s0);

#define MAX_SHIELDS 8
float4 ShieldData[MAX_SHIELDS];
float  Time;

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

float4 PSDistort(VSOutput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 totalOffset = float2(0.0, 0.0);

    // Unrolled loop. ps_4_0_level_9_1 supports static loops up to a small
    // bound; 8 is well within that. Inactive slots (intensity == 0) cost
    // ~one length() and one early-out; they don't dominate the per-pixel
    // cost when shield hits are infrequent.
    [unroll]
    for (int i = 0; i < MAX_SHIELDS; ++i)
    {
        float intensity = ShieldData[i].w;
        if (intensity <= 0.0)
            continue;

        float2 center = ShieldData[i].xy;
        float  radius = ShieldData[i].z;

        float2 toCenter = uv - center;
        float  dist     = length(toCenter);
        if (dist >= radius)
            continue;

        // Radial direction (avoid div-by-zero at the exact center).
        float2 dir = (dist > 0.0001) ? (toCenter / dist) : float2(0.0, 0.0);

        // Smooth-quadratic falloff from center → edge.
        float ratio   = dist / max(radius, 0.0001);
        float falloff = 1.0 - saturate(ratio);
        falloff *= falloff;

        // Concentric ripple expanding outward over time. The 25.0 factor
        // controls ring frequency; 75.0 the temporal speed (~12 Hz). Combined
        // with the 0.2s timer-driven envelope this reads as a sharp snap
        // matching the pre-migration heat-haze cadence.
        float wobble = sin(Time * 75.0 - ratio * 25.0);

        // Cap per-shield offset at ~5% of UV; multiple overlapping shields
        // accumulate but the saturate at sample time prevents wild reads.
        totalOffset += dir * (wobble * falloff * intensity * 0.05);
    }

    return tex2D(SceneSampler, uv + totalOffset);
}

technique Distort
{
    pass Pass1
    {
        // ps_4_0_level_9_3 (SM3.0 hardware): the 8-slot unrolled loop with
        // sin() and length() per slot busts the 96-instruction ps_2_0 cap
        // (level_9_1 fallback). 9_3 raises it to 512 and matches the profile
        // already used by MeshLighting.fx, so no driver-class regression.
        VertexShader = compile vs_4_0_level_9_3 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_3 PSDistort();
    }
}

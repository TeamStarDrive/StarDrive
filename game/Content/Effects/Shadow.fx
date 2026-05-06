//-----------------------------------------------------------------------------
// Shadow.fx
//
// Phase 3.8.A depth-only shader for the shadow-map pass. Renders mesh
// geometry from the sun light's point of view into a single-channel R32F
// render target; the PS emits each fragment's normalised view-space depth
// (NDC z under orthographic projection) so §3.8.B's lit shader can later
// compare receiver depth vs. occluder depth and shade accordingly.
//
// Conventions (must match ShadowMapComponent.cs):
//   - LightView and LightProjection together map world space into the sun's
//     orthographic clip box. The CPU-side build positions the light camera
//     at sceneCenter - LightDir * sceneRadius and uses a tight ortho sized
//     to the active scene AABB.
//   - Output depth is `clipPos.z / clipPos.w`. Under ortho, w == 1, so this
//     is equivalent to clipPos.z directly; we still divide explicitly so a
//     future move to perspective shadow maps doesn't silently break.
//   - PS writes the depth in R; G/B/A are unused.
//
// Test surface (UnitTests/Graphics/ShadowMapTests.cs Phase A):
//   - Render two known-position boxes and GetData<float> the RT.
//   - Assert the front box's pixels read closer-to-light depth than the
//     back box's pixels at the same texel.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 LightView;
float4x4 LightProjection;

struct VSInput
{
    float4 Position : POSITION0;
};

struct VSOutput
{
    float4 PositionPS : SV_POSITION;
    float2 Depth      : TEXCOORD0;   // (clip.z, clip.w) — PS divides
};

VSOutput VSDepth(VSInput input)
{
    VSOutput output;
    float4 worldPos = mul(input.Position, World);
    float4 viewPos  = mul(worldPos, LightView);
    float4 clipPos  = mul(viewPos, LightProjection);
    output.PositionPS = clipPos;
    output.Depth      = clipPos.zw;
    return output;
}

float4 PSDepth(VSOutput input) : SV_TARGET
{
    float depth = input.Depth.x / input.Depth.y;
    return float4(depth, 0, 0, 0);
}

technique Default
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSDepth();
        PixelShader  = compile ps_4_0_level_9_1 PSDepth();
    }
}

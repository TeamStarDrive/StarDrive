//-----------------------------------------------------------------------------
// Thrust.fx
//
// Phase 3.3 hand-rewrite of the XNA 3.1 "Thrust" effect that ships as
// game/Content/Effects/Thrust.xnb. Original was D3DX fx_2_0; MonoGame's MGFX
// reader rejects that bytecode. The .xnb stays on disk for mod compatibility;
// this .fx is compiled to .mgfxo and wins via the .xnb -> .mgfxo sibling
// fallback in GameContentManager.
//
// Behavior matches the original vs_3_0 + ps_3_0 bytecode (with preshader),
// disassembly preserved at C:/Users/gkapu/.claude/plans/phase3-logs/thrust-chunks/.
//
//   VS:  Position    = mul(localPos, WVP)             // world_matrices[1]
//        TexCoord    = passthrough
//        NormalW     = normalize(mul(localNormal, IST.xyz))   // world_matrices[2]
//        LocalPos    = local position (passthrough .z used by PS)
//
//   PS:  // animated volume noise sample, scrolls in (X,Y) over time
//        uv  = (TexCoord.x + ticks*0.04,
//               TexCoord.y - (heat*20 + 5)*ticks,
//               ticks)
//        n   = tex3D(Noise, uv).r
//        nExp= heat*1.5 + 1.5
//        nP  = pow(n, nExp)
//
//        // cone-length falloff (1 at base, 0 at tip, clamped)
//        f   = saturate(LocalPos.z * -0.5 + 1)
//        fExp= (1 - heat) * 25 + 0.85       // sharp at low heat, soft at high
//        fP  = pow(f, fExp)
//        eP  = pow(f * heat, 3.5)           // end-of-cone heat factor
//
//        // edge silhouette: 1 - |dot(world Y axis of cylinder, world normal)|
//        // (Original normalizes the *4D* column1 of world_matrix, which under
//        //  MonoGame's transpose-on-upload is HLSL row 1. For ships at
//        //  distant world-coords pos.y dominates length(col1), driving
//        //  worldYAxisQuirk → ~0 → silhouette ≈ 1 across the whole cone.
//        //  This is load-bearing: a "more correct" formula collapses
//        //  silhouette over much of the cone surface and the cone disappears.)
//
//        rgb = (nP * 2.5) * thrust_color[1].rgb
//            + (nP * 10 * eP) * thrust_color[0].rgb
//        a   = fP * silhouette
//
// Drawn by Thruster.cs over the ThrustCylinderB mesh per ship engine.
// Parameters bound by C# (must match Thruster.cs LoadAndAssignEffects):
//   - world_matrices : float4x4[3]  ([0]=World, [1]=WVP, [2]=InverseScaleTranspose)
//   - thrust_color   : float4[2]    ([0].rgb=color0, [0].w=heat; [1].rgb=color1)
//   - ticks          : float        // animation timer
//   - noise_texture  : Texture3D    // bound through `Noise` sampler below
//-----------------------------------------------------------------------------

float4x4 world_matrices[3];
float4   thrust_color[2];
float    ticks;

texture noise_texture;
sampler3D Noise = sampler_state
{
    Texture   = (noise_texture);
    AddressU  = Wrap;
    AddressV  = Wrap;
    AddressW  = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 NormalW  : TEXCOORD1;
    float3 LocalPos : TEXCOORD3;   // matches dcl_texcoord3 in original PS
};

VSOutput VSThrust(VSInput input)
{
    VSOutput output;

    // World-View-Projection (world_matrices[1]).
    output.Position = mul(input.Position, world_matrices[1]);

    // World-space normal via inverse-scale-transpose upper 3x3 (world_matrices[2]).
    float3 nW = mul(input.Normal, (float3x3)world_matrices[2]);
    output.NormalW = normalize(nW);

    output.TexCoord = input.TexCoord;
    output.LocalPos = input.Position.xyz;
    return output;
}

float4 PSThrust(VSOutput input) : COLOR0
{
    float heat = thrust_color[0].w;

    // Animated 3D noise (volume) sample — UV scrolls over time, W = ticks.
    float3 noiseUV = float3(input.TexCoord.x + ticks * 0.04,
                            input.TexCoord.y - (heat * 20.0 + 5.0) * ticks,
                            ticks);
    float n = tex3D(Noise, noiseUV).r;

    float nExp = heat * 1.5 + 1.5;
    float nP   = pow(n, nExp);

    // Cone-length falloff: LocalPos.z runs from 0 (base) to ~2 (tip).
    float f    = saturate(input.LocalPos.z * -0.5 + 1.0);
    float fExp = (1.0 - heat) * 25.0 + 0.85;
    float fP   = pow(f, fExp);
    float eP   = pow(f * heat, 3.5);

    // Two-color blend.
    float3 colTip  = (nP * 2.5)        * thrust_color[1].rgb;
    float3 colBase = (nP * 10.0 * eP)  * thrust_color[0].rgb;
    float3 rgb     = colTip + colBase;

    // Silhouette: build the world matrix's column-1 the way the original
    // preshader does (with the 4D-length normalization quirk preserved).
    // world_matrices[0][1] is HLSL row 1, which under MonoGame's
    // transpose-on-upload is C#'s column 1 = (right.y, up.y, -fwd.y, pos.y).
    // For ships at distant world coords, pos.y dominates the 4D length so
    // worldYAxisQuirk collapses to a tiny vector and silhouette → 1.
    float4 col1 = world_matrices[0][1];
    float3 worldYAxisQuirk = col1.xyz / length(col1);
    float silhouette = 1.0 - abs(dot(worldYAxisQuirk, input.NormalW));

    return float4(rgb, fP * silhouette);
}

technique thrust_technique
{
    pass P1
    {
        VertexShader = compile vs_4_0_level_9_1 VSThrust();
        PixelShader  = compile ps_4_0_level_9_1 PSThrust();
    }
}

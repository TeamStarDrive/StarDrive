//-----------------------------------------------------------------------------
// PlanetHalo.fx
//
// Phase 3.3 hand-rewrite of the XNA 3.1 PlanetHalo effect that ships as
// game/Content/Effects/PlanetHalo.xnb. The original was D3DX fx_2_0;
// MonoGame's MGFX reader rejects that bytecode. The .xnb stays on disk for
// mod compatibility; this .fx is compiled to .mgfxo and wins via the
// .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Behavior matches the original vs_2_0 + ps_2_0 bytecode:
//
//   VS:  extrude inverted sphere outward by 30 units along world-space
//        normal, transform with World * View * Projection, compute primary
//        diffuse `max(dot(N, normalize(L)), 0)` and rim term
//        `max(dot(N*0.9 + L*0.1, L), 0)`; pass world normal + camera pos
//        through to PS.
//
//   PS:  rgb   = rim term
//        alpha = saturate(dot(N, normalize(CameraPosition))) * primaryDiffuse
//
// Drawn on MeshSphere (planet sphere) with inverted cull + additive blend
// on clouded planets — see PlanetRenderer.RenderPlanetGlow / line 233-236.
// Parameters bound by C#: World, View, Projection (matrices), CameraPosition,
// DiffuseLightDirection (float3) — names must match Parameters[name] exactly.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 View;
float4x4 Projection;
float3   DiffuseLightDirection;
float3   CameraPosition;

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position  : POSITION0;
    float4 Diffuse   : COLOR0;     // .x = primary diffuse term
    float3 Rim       : COLOR1;     // rim/limb color term
    float2 TexCoord  : TEXCOORD0;  // passthrough (unused by PS)
    float3 NormalW   : TEXCOORD1;  // normalized world-space normal
    float3 CameraPos : TEXCOORD2;  // camera position (PS normalizes)
};

VSOutput VSPlanetHalo(VSInput input)
{
    VSOutput output;

    // World-space transforms.
    float3 normalW  = mul(input.Normal, (float3x3)World);
    float3 normalWN = normalize(normalW);
    float4 posW     = mul(input.Position, World);

    // Extrude outward 30 world-units along the world normal — gives the
    // halo a visible ring beyond the planet's surface mesh.
    float4 posExtruded = float4(posW.xyz + normalWN * 30.0, posW.w);

    // World -> View -> Projection.
    float4 posView = mul(posExtruded, View);
    output.Position = mul(posView, Projection);

    // Primary diffuse, biased so the dark side keeps ~20% halo intensity.
    // The original `max(dot(N, L), 0)` collapses the dark side to zero alpha,
    // which makes the post-migration halo look weaker than pre-migration where
    // some halo is visible all around. `dot*0.4 + 0.6` maps the cosine to
    // [0.2, 1.0]: full strength on the sub-solar point, ~60% at the terminator,
    // ~20% on the anti-solar side. Matches the pre-migration visual without
    // changing the sun-aligned brightness gradient.
    float3 lightDirN  = normalize(DiffuseLightDirection);
    float  dotNL      = dot(normalWN, lightDirN);
    float  primaryLit = saturate(dotNL * 0.4 + 0.6);
    output.Diffuse    = float4(primaryLit, 0, 0, 0);  // PS reads .x only

    // Rim term: dot(N*0.9 + L*0.1, L), unsaturated bias toward limb.
    float3 rimVec = normalWN * 0.9 + DiffuseLightDirection * 0.1;
    float  rimLit = max(dot(rimVec, DiffuseLightDirection), 0);
    output.Rim    = rimLit.xxx;

    output.TexCoord  = input.TexCoord;
    output.NormalW   = normalWN;
    output.CameraPos = CameraPosition;
    return output;
}

float4 PSPlanetHalo(VSOutput input) : COLOR0
{
    float3 camDirN = normalize(input.CameraPos);
    float  facing  = saturate(dot(input.NormalW, camDirN));
    float  alpha   = facing * input.Diffuse.x;
    return float4(input.Rim, alpha);
}

technique Planet
{
    pass P1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPlanetHalo();
        PixelShader  = compile ps_4_0_level_9_1 PSPlanetHalo();
    }
}

//-----------------------------------------------------------------------------
// SkinnedEffect.fx
//
// Phase 3.10.B.5: matrix-palette skinning variant of MeshLighting.fx. The PS
// is byte-for-byte identical to MeshLighting's so skinned ships shade the
// same way as static hulls (per-pixel lighting, normal/specular/emissive
// maps, point lights, shadows). The VS adds a Skin() pre-pass that blends
// per-vertex Position / Normal / Tangent / Binormal through 4 bone indices
// + weights against a Bones[] matrix palette, then proceeds with the same
// World/View/Projection chain as the static path.
//
// Palette layout:
//   float4x3 Bones[64];   // 64 bones × 3 vec4 columns = 192 c-registers
//   total uniforms ≈ 245 c-registers, fits in vs_4_0_level_9_3's 256-slot
//   constant store. 64 is a comfortable cap for StarDrive ship rigs (the
//   busiest hull tested so far peaks around 30 deformer bones).
//
// Skinning convention: skinning matrix is "inverseBindPose * worldCurrent"
// (XNA row-vector × row-major), pre-baked per bone in C# by
// BoneAnimationPlayer.SkinningPalette and uploaded transposed to float4x3
// columns. mul(float4 vertex, float4x3 m) yields a float3 = the skinned
// position; the trailing translation lives in the 4th row of each column.
//
// Vertex declaration the VS expects (NanoMesh writes when SkinnedBones>0):
//   POSITION0     : float3
//   BLENDINDICES0 : Byte4 (sent to HLSL as int4 — see MeshInterface.cs)
//   BLENDWEIGHT0  : float4
//   NORMAL0       : float3
//   TEXCOORD0     : float2
//   TANGENT0      : float3
//   BINORMAL0     : float3
//
// All shading uniforms below are kept in lockstep with MeshLighting.fx so
// LightingEffectBinder + the renderer's CopySharedLighting flow can target
// either effect interchangeably.
//-----------------------------------------------------------------------------

#define MaxBones 64

float4x4 World;
float4x4 View;
float4x4 Projection;

// Phase 3.10.B.5: matrix-palette skin transforms.
// NOTE: float4x3 (4 rows × 3 cols) packs as 3 vec4 in column-major default.
// MonoGame's EffectParameter.SetValue(Matrix[]) handles the row→column
// transpose automatically when the parameter resolves to a float4x3 array.
float4x3 Bones[MaxBones];

// Shadow uniforms — see MeshLighting.fx for the float4-packing rationale
// (single-float uniforms can drop under MGFX 3.8.1 / ps_4_0_level_9_3).
float4x4 LightViewProjection;
float4   ShadowParams = float4(0.0, 0.001, 0.0, 0.0);

texture ShadowMap;
sampler2D ShadowSampler = sampler_state
{
    Texture   = (ShadowMap);
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

float3 DiffuseColor    = float3(1, 1, 1);
float3 EmissiveColor   = float3(0, 0, 0);
float3 SpecularColor   = float3(1, 1, 1);
float  SpecularPower   = 16.0;
// §4.6.B(b) follow-up — see MeshLighting.fx. Skinned effect mirrors the
// per-material spec multiplier so animated hulls (Cordrazine, etc) honor
// their FBX-declared gloss instead of reading flat.
float  SpecularAmount  = 1.0;
// see MeshLighting.fx
float  EmissiveScale   = 1.0;
// see MeshLighting.fx
float4 HullTint        = float4(1, 1, 1, 0);
// see MeshLighting.fx
float4 PartLook        = float4(1, 0, 1, 0);
// see MeshLighting.fx
float4 PartReach       = float4(0, 1, 0, 0);
// see MeshLighting.fx
float4 ReachShape      = float4(0, 0, 1, 1);
float  Alpha           = 1.0;
float3 EyePosition     = float3(0, 0, 0);

bool   LightingEnabled        = false;
bool   TextureEnabled         = false;
bool   EmissiveMapEnabled     = false;
bool   SpecularMapEnabled     = false;
bool   NormalMapEnabled       = false;
bool   TintMapEnabled         = false;
bool   FogEnabled             = false;

float3 AmbientLightColor = float3(0, 0, 0);

float3 DirLight0Direction      = float3(0, -1, 0);
float3 DirLight0DiffuseColor   = float3(0, 0, 0);
float3 DirLight0SpecularColor  = float3(0, 0, 0);

float3 DirLight1Direction      = float3(0, -1, 0);
float3 DirLight1DiffuseColor   = float3(0, 0, 0);
float3 DirLight1SpecularColor  = float3(0, 0, 0);

float3 DirLight2Direction      = float3(0, -1, 0);
float3 DirLight2DiffuseColor   = float3(0, 0, 0);
float3 DirLight2SpecularColor  = float3(0, 0, 0);

// PointLight layout — see MeshLighting.fx for the full rationale. Skinned
// shaders share the same C# binder + parameter cache (SkinnedLightingEffect
// inherits from LightingEffect), so uniform names must match in lockstep.
// §4.6.B follow-up: 8 dynamic slots (was 2) + per-slot SpecularColor restored
// after the FL10.0 bump removed the §4.6 #2 register-cap forcing function.
float4 PointLight0PositionAndRadius = float4(0, 0, 0, 1);
float4 PointLight0DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 PointLight0SpecularColor     = float3(0, 0, 0);

float4 PointLight1PositionAndRadius = float4(0, 0, 0, 1);
float4 PointLight1DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 PointLight1SpecularColor     = float3(0, 0, 0);

float4 PointLight2PositionAndRadius = float4(0, 0, 0, 1);
float4 PointLight2DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 PointLight2SpecularColor     = float3(0, 0, 0);

float4 DynamicLight0PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight0DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight0SpecularColor     = float3(0, 0, 0);
float4 DynamicLight1PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight1DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight1SpecularColor     = float3(0, 0, 0);
float4 DynamicLight2PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight2DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight2SpecularColor     = float3(0, 0, 0);
float4 DynamicLight3PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight3DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight3SpecularColor     = float3(0, 0, 0);
float4 DynamicLight4PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight4DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight4SpecularColor     = float3(0, 0, 0);
float4 DynamicLight5PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight5DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight5SpecularColor     = float3(0, 0, 0);
float4 DynamicLight6PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight6DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight6SpecularColor     = float3(0, 0, 0);
float4 DynamicLight7PositionAndRadius = float4(0, 0, 0, 1);
float4 DynamicLight7DiffuseAndEnabled = float4(0, 0, 0, 0);
float3 DynamicLight7SpecularColor     = float3(0, 0, 0);

float3 FogColor    = float3(0, 0, 0);
float2 FogStartEnd = float2(0.0, 1.0);

texture Texture;
sampler2D TextureSampler = sampler_state
{
    Texture   = (Texture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

texture EmissiveMap;
sampler2D EmissiveSampler = sampler_state
{
    Texture   = (EmissiveMap);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

texture SpecularMap;
sampler2D SpecularSampler = sampler_state
{
    Texture   = (SpecularMap);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

texture NormalMap;
sampler2D NormalSampler = sampler_state
{
    Texture   = (NormalMap);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

texture TintMap;
sampler2D TintSampler = sampler_state
{
    Texture   = (TintMap);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

struct VSInputSkinned
{
    float4 Position     : POSITION0;
    int4   BlendIndices : BLENDINDICES0;
    float4 BlendWeights : BLENDWEIGHT0;
    float3 Normal       : NORMAL0;
    float2 TexCoord     : TEXCOORD0;
    float3 Tangent      : TANGENT0;
    float3 Binormal     : BINORMAL0;
};

struct VSOutput
{
    float4 PositionPS : SV_POSITION;
    float2 TexCoord   : TEXCOORD0;
    float3 PositionWS : TEXCOORD1;
    float3 NormalWS   : TEXCOORD2;
    float3 TangentWS  : TEXCOORD3;
    float3 BinormalWS : TEXCOORD4;
    float  FogFactor  : TEXCOORD5;
    float4 PositionLS : TEXCOORD6;
    // see MeshLighting.fx
    float3 PositionOS : TEXCOORD7;
};

float ComputeFogFactor(float dist)
{
    return saturate((dist - FogStartEnd.x) / (FogStartEnd.y - FogStartEnd.x));
}

// 4-bone weighted blend producing a single 4×3 skinning matrix per vertex.
// Indices are int4 (D3D9 promotes Byte4 inputs to int via the input layout),
// weights are float4. Caller must apply the resulting matrix to position +
// normal + tangent + binormal via mul(..., (float3x3)skinning) for the
// directional bases (the float3x3 cast drops the translation row).
float4x3 ComputeSkinning(int4 indices, float4 weights)
{
    float4x3 skinning =
        Bones[indices.x] * weights.x +
        Bones[indices.y] * weights.y +
        Bones[indices.z] * weights.z +
        Bones[indices.w] * weights.w;
    return skinning;
}

struct LightTerms { float3 Diffuse; float3 Specular; };
// see MeshLighting.fx
static float Shininess;

LightTerms ComputeDirectional(
    float3 normalWS, float3 viewDirWS,
    float3 lightDir, float3 lightDiffuse, float3 lightSpecular)
{
    float3 toLight = -lightDir;
    float ndl = saturate(dot(normalWS, toLight));

    LightTerms terms;
    terms.Diffuse = lightDiffuse * ndl;

    float3 halfWay = normalize(toLight + viewDirWS);
    float ndh = saturate(dot(normalWS, halfWay));
    float specMask = ndl > 0 ? 1.0 : 0.0;
    terms.Specular = lightSpecular * pow(ndh, Shininess) * specMask;

    return terms;
}

// §4.6.B follow-up: returns LightTerms with specular restored — see
// MeshLighting.fx for the full rationale. Identical math.
LightTerms ComputePoint(
    float3 normalWS, float3 positionWS, float3 viewDirWS,
    float4 positionAndRadius, float4 diffuseAndEnabled, float3 specularColor)
{
    LightTerms terms;
    terms.Diffuse  = float3(0, 0, 0);
    terms.Specular = float3(0, 0, 0);

    if (diffuseAndEnabled.w < 0.5) return terms;

    float3 toLightVec = positionAndRadius.xyz - positionWS;
    float dist = length(toLightVec);
    float3 toLight = toLightVec / max(dist, 0.0001);

    float ratio = saturate(dist / max(positionAndRadius.w, 0.0001));
    float atten = saturate(1.0 - ratio * ratio);
    if (atten <= 0.0) return terms;

    float ndl = saturate(dot(normalWS, toLight));
    terms.Diffuse = diffuseAndEnabled.xyz * ndl * atten;

    float3 halfWay = normalize(toLight + viewDirWS);
    float ndh = saturate(dot(normalWS, halfWay));
    float specMask = ndl > 0 ? 1.0 : 0.0;
    terms.Specular = specularColor * pow(ndh, Shininess) * specMask * atten;

    return terms;
}

VSOutput VSSkinned(VSInputSkinned input)
{
    VSOutput output;

    // 1) Skin in object space. The skinning matrix maps bind-pose vertex
    //    positions into the current animated pose, still in object space —
    //    World/View/Projection then carry the result into clip space.
    float4x3 skinning = ComputeSkinning(input.BlendIndices, input.BlendWeights);
    float3 skinnedPosition = mul(input.Position, skinning);
    float3 skinnedNormal   = mul(input.Normal,   (float3x3)skinning);
    float3 skinnedTangent  = mul(input.Tangent,  (float3x3)skinning);
    float3 skinnedBinormal = mul(input.Binormal, (float3x3)skinning);

    // 2) Standard MeshLighting.fx world/view/projection chain on the skinned
    //    object-space attributes. Identical math from here on.
    float4 worldPos = mul(float4(skinnedPosition, 1.0), World);
    float4 viewPos  = mul(worldPos, View);
    output.PositionPS = mul(viewPos, Projection);
    output.PositionWS = worldPos.xyz;
    output.NormalWS   = normalize(mul(skinnedNormal,   (float3x3)World));
    output.TangentWS  = normalize(mul(skinnedTangent,  (float3x3)World));
    output.BinormalWS = normalize(mul(skinnedBinormal, (float3x3)World));
    output.TexCoord   = input.TexCoord;
    output.FogFactor  = ComputeFogFactor(length(viewPos.xyz));
    output.PositionLS = mul(worldPos, LightViewProjection);
    output.PositionOS = skinnedPosition;
    return output;
}

float SampleShadowFactor(float4 positionLS)
{
    if (ShadowParams.x < 0.5) return 1.0;

    float invW = 1.0 / positionLS.w;
    float2 ndcXY = positionLS.xy * invW;
    float  receiverDepth = positionLS.z * invW;

    float2 uv = float2(ndcXY.x * 0.5 + 0.5, -ndcXY.y * 0.5 + 0.5);

    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0 ||
        receiverDepth < 0.0 || receiverDepth > 1.0)
        return 1.0;

    float occluderDepth = tex2D(ShadowSampler, uv).r;
    return (occluderDepth + ShadowParams.y < receiverDepth) ? 0.0 : 1.0;
}

float Luminance(float3 c)
{
    return dot(c, float3(0.3, 0.59, 0.11));
}

// see MeshLighting.fx
float3 TintAtLuminance(float3 tint, float lum)
{
    float3 c = tint + (lum - Luminance(tint));
    float l = Luminance(c);
    float n = min(c.r, min(c.g, c.b));
    float x = max(c.r, max(c.g, c.b));
    if (n < 0.0) c = l + (c - l) * l / max(l - n, 0.00001);
    if (x > 1.0) c = l + (c - l) * (1.0 - l) / max(x - l, 0.00001);
    return c;
}

// three random numbers in [0,1) for a lattice cell
float3 CellRandom(int3 cell)
{
    uint3 v = (uint3)cell * 1664525u + 1013904223u;
    v.x += v.y * v.z; v.y += v.z * v.x; v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z; v.y += v.z * v.x; v.z += v.x * v.y;
    return float3(v >> 8u) * (1.0 / 16777216.0);
}

// smooth noise in [0,1]
float ValueNoise(float3 p)
{
    float3 fl = floor(p);
    int3 c = (int3)fl;
    float3 f = p - fl;
    f = f * f * (3.0 - 2.0 * f);
    float a = CellRandom(c).x,                  b = CellRandom(c + int3(1, 0, 0)).x;
    float d = CellRandom(c + int3(0, 1, 0)).x,  e = CellRandom(c + int3(1, 1, 0)).x;
    float g = CellRandom(c + int3(0, 0, 1)).x,  h = CellRandom(c + int3(1, 0, 1)).x;
    float k = CellRandom(c + int3(0, 1, 1)).x,  m = CellRandom(c + int3(1, 1, 1)).x;
    return lerp(lerp(lerp(a, b, f.x), lerp(d, e, f.x), f.y),
                lerp(lerp(g, h, f.x), lerp(k, m, f.x), f.y), f.z);
}

// the specks along the ragged edge of a part coming back
float SpeckNoise(float3 p)
{
    float n = 0.65 * ValueNoise(p / 7.0) + 0.35 * ValueNoise(p / 2.5);
    return saturate((n - 0.5) * 2.2 + 0.5);
}

// the nearest of the seeds scattered one to a lattice cell: its distance and its random
struct CellHit { float Distance; float Id; };
CellHit NearestSeed(float3 p)
{
    float3 fl = floor(p);
    int3 cell = (int3)fl;
    float3 f = p - fl;
    CellHit hit;
    hit.Distance = 8.0;
    hit.Id = 0.0;
    [loop] for (int z = -1; z <= 1; ++z)
    [loop] for (int y = -1; y <= 1; ++y)
    [loop] for (int x = -1; x <= 1; ++x)
    {
        int3 o = int3(x, y, z);
        float3 r = CellRandom(cell + o);
        float3 d = float3(o) + r - f;
        float dd = dot(d, d);
        if (dd < hit.Distance)
        {
            hit.Distance = dd;
            hit.Id = frac(r.x * 13.37 + r.y * 7.91 + r.z * 3.17);
        }
    }
    hit.Distance = sqrt(hit.Distance);
    return hit;
}

// the inside of a hull torn open: charred metal with red and orange embers glowing into it, as hot as heat says; its own shine and power on any hull
struct Burnt { float3 Color; float3 Glow; float Shine; float Power; };
Burnt BurntInside(float3 p, float heat)
{
    Burnt b;
    b.Color = float3(0.0245, 0.02275, 0.021) * lerp(0.6, 1.2, ValueNoise(p / 10.0));
    b.Shine = 0.12;
    b.Power = 16.0;

    // embers too small to see blend into their mean glow, so they don't shimmer, and need no search
    float pixel = max(length(ddx(p)), length(ddy(p)));
    float blur = saturate(pixel * 0.75 - 0.3);
    b.Glow = float3(1.0, 0.315, 0.0525) * 0.1875 * blur;
    [branch] if (blur < 1.0)
    {
        CellHit ember = NearestSeed(p / 2.0);
        float lit = step(0.25, ember.Id); // three seeds in four
        float r = lerp(0.08, 0.28, frac(ember.Id * 3.1));
        float spot = 1.0 - smoothstep(r * 0.4, r + pixel * 0.5, ember.Distance);
        float soft = 1.0 - smoothstep(r, r * 2.8, ember.Distance);
        float3 color = frac(ember.Id * 7.3) < 0.5 ? float3(1.0, 0.48, 0.07) : float3(1.0, 0.15, 0.035);
        b.Glow += color * (spot * 1.6 + soft * 0.4) * lit * (1.0 - blur);
    }
    b.Glow *= heat;
    return b;
}

float4 PSDefault(VSOutput input, bool isFront : SV_IsFrontFace) : SV_TARGET
{
    bool burnt = !isFront && PartLook.y > 0.5;
    Burnt inside = (Burnt)0;
    if (burnt)
        inside = BurntInside(input.PositionOS, PartLook.z);
    Shininess = burnt ? inside.Power : SpecularPower;
    float3 diffuseColor = burnt ? float3(1, 1, 1) : DiffuseColor;

    float4 texColor = TextureEnabled ? tex2D(TextureSampler, input.TexCoord) : float4(1, 1, 1, 1);

    if (TintMapEnabled)
        texColor.rgb = lerp(texColor.rgb, TintAtLuminance(HullTint.rgb, Luminance(texColor.rgb)),
                            tex2D(TintSampler, input.TexCoord).r * HullTint.w);

    float3 glow = EmissiveMapEnabled ? tex2D(EmissiveSampler, input.TexCoord).rgb : float3(0, 0, 0);
    float3 emissive = EmissiveMapEnabled ? glow * EmissiveScale : EmissiveColor;

    float3 specularMask = SpecularMapEnabled
        ? tex2D(SpecularSampler, input.TexCoord).rgb
        : float3(1, 1, 1);

    float3 specularGain = SpecularColor * specularMask * SpecularAmount;
    if (burnt)
    {
        texColor.rgb = inside.Color;
        emissive = inside.Glow;
        specularGain = float3(inside.Shine, inside.Shine, inside.Shine);
    }

    float3 rgb;
    if (LightingEnabled)
    {
        float3 normalWS;
        if (burnt)
        {
            normalWS = -normalize(input.NormalWS);
        }
        else if (NormalMapEnabled)
        {
            float3 tangentSpaceN = tex2D(NormalSampler, input.TexCoord).rgb * 2.0 - 1.0;
            float3 T = normalize(input.TangentWS);
            float3 B = normalize(input.BinormalWS);
            float3 N = normalize(input.NormalWS);
            normalWS = normalize(tangentSpaceN.x * T + tangentSpaceN.y * B + tangentSpaceN.z * N);
        }
        else
        {
            normalWS = normalize(input.NormalWS);
        }
        float3 viewDirWS = normalize(EyePosition - input.PositionWS);

        float3 ambient = AmbientLightColor * diffuseColor;

        LightTerms l0 = ComputeDirectional(normalWS, viewDirWS,
            DirLight0Direction, DirLight0DiffuseColor, DirLight0SpecularColor);
        LightTerms l1 = ComputeDirectional(normalWS, viewDirWS,
            DirLight1Direction, DirLight1DiffuseColor, DirLight1SpecularColor);
        LightTerms l2 = ComputeDirectional(normalWS, viewDirWS,
            DirLight2Direction, DirLight2DiffuseColor, DirLight2SpecularColor);

        LightTerms p0 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            PointLight0PositionAndRadius, PointLight0DiffuseAndEnabled, PointLight0SpecularColor);
        LightTerms p1 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            PointLight1PositionAndRadius, PointLight1DiffuseAndEnabled, PointLight1SpecularColor);
        LightTerms p2 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            PointLight2PositionAndRadius, PointLight2DiffuseAndEnabled, PointLight2SpecularColor);
        LightTerms d0 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight0PositionAndRadius, DynamicLight0DiffuseAndEnabled, DynamicLight0SpecularColor);
        LightTerms d1 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight1PositionAndRadius, DynamicLight1DiffuseAndEnabled, DynamicLight1SpecularColor);
        LightTerms d2 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight2PositionAndRadius, DynamicLight2DiffuseAndEnabled, DynamicLight2SpecularColor);
        LightTerms d3 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight3PositionAndRadius, DynamicLight3DiffuseAndEnabled, DynamicLight3SpecularColor);
        LightTerms d4 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight4PositionAndRadius, DynamicLight4DiffuseAndEnabled, DynamicLight4SpecularColor);
        LightTerms d5 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight5PositionAndRadius, DynamicLight5DiffuseAndEnabled, DynamicLight5SpecularColor);
        LightTerms d6 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight6PositionAndRadius, DynamicLight6DiffuseAndEnabled, DynamicLight6SpecularColor);
        LightTerms d7 = ComputePoint(normalWS, input.PositionWS, viewDirWS,
            DynamicLight7PositionAndRadius, DynamicLight7DiffuseAndEnabled, DynamicLight7SpecularColor);

        float3 diffuseAcc  = (l0.Diffuse  + l1.Diffuse  + l2.Diffuse
                            + p0.Diffuse  + p1.Diffuse  + p2.Diffuse
                            + d0.Diffuse  + d1.Diffuse  + d2.Diffuse + d3.Diffuse
                            + d4.Diffuse  + d5.Diffuse  + d6.Diffuse + d7.Diffuse) * diffuseColor;
        float3 specularAcc = (l0.Specular + l1.Specular + l2.Specular
                            + p0.Specular + p1.Specular + p2.Specular
                            + d0.Specular + d1.Specular + d2.Specular + d3.Specular
                            + d4.Specular + d5.Specular + d6.Specular + d7.Specular)
                            * specularGain;

        float shadowFactor = SampleShadowFactor(input.PositionLS);
        diffuseAcc  *= shadowFactor;
        specularAcc *= shadowFactor;

        rgb = (ambient + diffuseAcc) * texColor.rgb + specularAcc + emissive;
    }
    else
    {
        rgb = diffuseColor * texColor.rgb + emissive;
    }

    if (FogEnabled)
        rgb = lerp(rgb, FogColor, input.FogFactor);

    return float4(rgb, texColor.a * Alpha);
}

technique Default
{
    pass Pass1
    {
        // §4.6.B: bumped FL9.3 → FL10.0 in lockstep with MeshLighting.fx.
        // Same parameter surface (PointLightN / DynamicLightN packing,
        // FogStartEnd) so the upstream LightingEffect parameter handles
        // resolve identically; the bones[] palette + matrix-skinning VS
        // path was already comfortably under FL9.3's vs cap so no skinned-
        // specific limitation is being lifted, but the profile must match
        // the static effect for a unified DX10 minimum-spec promise.
        VertexShader = compile vs_4_0 VSSkinned();
        PixelShader  = compile ps_4_0 PSDefault();
    }
}

// see MeshLighting.fx
float4 PSDissolve(VSOutput input, bool isFront : SV_IsFrontFace) : SV_TARGET
{
    float2 d = (input.PositionOS.xy - ReachShape.xy) * ReachShape.zw;
    float reach = saturate((length(d) - PartReach.x) / max(PartReach.y - PartReach.x, 0.0001));
    clip(PartLook.x - lerp(reach, SpeckNoise(input.PositionOS), 0.3));
    return PSDefault(input, isFront);
}

technique Dissolve
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VSSkinned();
        PixelShader  = compile ps_4_0 PSDissolve();
    }
}

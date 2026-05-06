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
float  Alpha           = 1.0;
float3 EyePosition     = float3(0, 0, 0);

bool   LightingEnabled        = false;
bool   TextureEnabled         = false;
bool   EmissiveMapEnabled     = false;
bool   SpecularMapEnabled     = false;
bool   NormalMapEnabled       = false;
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

bool   PointLight0Enabled       = false;
float3 PointLight0Position      = float3(0, 0, 0);
float3 PointLight0DiffuseColor  = float3(0, 0, 0);
float3 PointLight0SpecularColor = float3(0, 0, 0);
float  PointLight0Radius        = 1.0;

bool   PointLight1Enabled       = false;
float3 PointLight1Position      = float3(0, 0, 0);
float3 PointLight1DiffuseColor  = float3(0, 0, 0);
float3 PointLight1SpecularColor = float3(0, 0, 0);
float  PointLight1Radius        = 1.0;

bool   PointLight2Enabled       = false;
float3 PointLight2Position      = float3(0, 0, 0);
float3 PointLight2DiffuseColor  = float3(0, 0, 0);
float3 PointLight2SpecularColor = float3(0, 0, 0);
float  PointLight2Radius        = 1.0;

float3 FogColor = float3(0, 0, 0);
float  FogStart = 0.0;
float  FogEnd   = 1.0;

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
};

float ComputeFogFactor(float dist)
{
    return saturate((dist - FogStart) / (FogEnd - FogStart));
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
    terms.Specular = lightSpecular * pow(ndh, SpecularPower) * specMask;

    return terms;
}

LightTerms ComputePoint(
    float3 normalWS, float3 viewDirWS, float3 positionWS,
    bool enabled, float3 lightPos, float3 lightDiffuse, float3 lightSpecular, float lightRadius)
{
    LightTerms terms;
    terms.Diffuse = float3(0, 0, 0);
    terms.Specular = float3(0, 0, 0);
    if (!enabled) return terms;

    float3 toLightVec = lightPos - positionWS;
    float dist = length(toLightVec);
    float3 toLight = toLightVec / max(dist, 0.0001);

    float ratio = saturate(dist / max(lightRadius, 0.0001));
    float atten = saturate(1.0 - ratio * ratio);
    if (atten <= 0.0) return terms;

    float ndl = saturate(dot(normalWS, toLight));
    terms.Diffuse = lightDiffuse * ndl * atten;

    float3 halfWay = normalize(toLight + viewDirWS);
    float ndh = saturate(dot(normalWS, halfWay));
    float specMask = ndl > 0 ? 1.0 : 0.0;
    terms.Specular = lightSpecular * pow(ndh, SpecularPower) * specMask * atten;

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

float4 PSDefault(VSOutput input) : SV_TARGET
{
    float4 texColor = TextureEnabled ? tex2D(TextureSampler, input.TexCoord) : float4(1, 1, 1, 1);

    float3 emissive = EmissiveMapEnabled
        ? tex2D(EmissiveSampler, input.TexCoord).rgb
        : EmissiveColor;

    float3 specularMask = SpecularMapEnabled
        ? tex2D(SpecularSampler, input.TexCoord).rgb
        : float3(1, 1, 1);

    float3 rgb;
    if (LightingEnabled)
    {
        float3 normalWS;
        if (NormalMapEnabled)
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

        float3 ambient = AmbientLightColor * DiffuseColor;

        LightTerms l0 = ComputeDirectional(normalWS, viewDirWS,
            DirLight0Direction, DirLight0DiffuseColor, DirLight0SpecularColor);
        LightTerms l1 = ComputeDirectional(normalWS, viewDirWS,
            DirLight1Direction, DirLight1DiffuseColor, DirLight1SpecularColor);
        LightTerms l2 = ComputeDirectional(normalWS, viewDirWS,
            DirLight2Direction, DirLight2DiffuseColor, DirLight2SpecularColor);

        LightTerms p0 = ComputePoint(normalWS, viewDirWS, input.PositionWS,
            PointLight0Enabled, PointLight0Position,
            PointLight0DiffuseColor, PointLight0SpecularColor, PointLight0Radius);
        LightTerms p1 = ComputePoint(normalWS, viewDirWS, input.PositionWS,
            PointLight1Enabled, PointLight1Position,
            PointLight1DiffuseColor, PointLight1SpecularColor, PointLight1Radius);
        LightTerms p2 = ComputePoint(normalWS, viewDirWS, input.PositionWS,
            PointLight2Enabled, PointLight2Position,
            PointLight2DiffuseColor, PointLight2SpecularColor, PointLight2Radius);

        float3 diffuseAcc  = (l0.Diffuse  + l1.Diffuse  + l2.Diffuse  + p0.Diffuse  + p1.Diffuse  + p2.Diffuse)  * DiffuseColor;
        float3 specularAcc = (l0.Specular + l1.Specular + l2.Specular + p0.Specular + p1.Specular + p2.Specular) * SpecularColor * specularMask;

        float shadowFactor = SampleShadowFactor(input.PositionLS);
        diffuseAcc  *= shadowFactor;
        specularAcc *= shadowFactor;

        rgb = (ambient + diffuseAcc) * texColor.rgb + specularAcc + emissive;
    }
    else
    {
        rgb = DiffuseColor * texColor.rgb + emissive;
    }

    if (FogEnabled)
        rgb = lerp(rgb, FogColor, input.FogFactor);

    return float4(rgb, texColor.a * Alpha);
}

technique Default
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_3 VSSkinned();
        PixelShader  = compile ps_4_0_level_9_3 PSDefault();
    }
}

//-----------------------------------------------------------------------------
// MeshLighting.fx
//
// Phase 3.7 step 4 (Phase A) drop-in replacement for BasicEffect's per-pixel
// lighting path. Mirrors BasicEffect's lighting model 1:1:
//   ambient = AmbientLightColor * DiffuseColor + EmissiveColor
//   diffuse = sum_i(N·L_i_clamped * LightDiffuse_i) * DiffuseColor
//   specul. = sum_i((N·H_i)^SpecularPower * LightSpecular_i) * SpecularColor
//   color   = (ambient + diffuse) * tex.rgb + specul.   // tex unmultiplied
//   alpha   = Alpha * tex.a
// (with fog mixed in afterwards if FogEnabled)
//
// The "ambient/emissive multiplied by texture, specular not" split is the
// BasicEffect convention — keeps highlights reading as reflections rather
// than tinted-diffuse. Phase B will extend with normal/specular/emissive
// map sampling; this Phase A version uses only DiffuseTexture so the swap
// from BasicEffect → MeshLighting is visually a no-op for ships/planets/
// stations and provides a stable platform for the map work.
//
// All parameters are named so callers can use Effect.Parameters[name].
// LightingEffect (Ship_Game/Data/Mesh/SunBurnStubs.cs, rewritten) caches
// EffectParameter handles and exposes BasicEffect-shaped properties for
// LightingEffectBinder, StaticMesh, etc.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 DiffuseColor    = float3(1, 1, 1);
float3 EmissiveColor   = float3(0, 0, 0);
float3 SpecularColor   = float3(1, 1, 1);
float  SpecularPower   = 16.0;
float  Alpha           = 1.0;
float3 EyePosition     = float3(0, 0, 0);

bool   LightingEnabled = false;
bool   TextureEnabled  = false;
bool   FogEnabled      = false;

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

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 PositionPS : SV_POSITION;
    float2 TexCoord   : TEXCOORD0;
    float3 PositionWS : TEXCOORD1;
    float3 NormalWS   : TEXCOORD2;
    float  FogFactor  : TEXCOORD3;
};

float ComputeFogFactor(float dist)
{
    return saturate((dist - FogStart) / (FogEnd - FogStart));
}

// Mirrors BasicEffect's per-light contribution. Returns (diffuse, specular).
// `lightDir` is the SunBurn / BasicEffect convention: direction the light
// travels (so toLight = -lightDir).
struct LightTerms { float3 Diffuse; float3 Specular; };
LightTerms ComputeDirectional(
    float3 normalWS, float3 viewDirWS,
    float3 lightDir, float3 lightDiffuse, float3 lightSpecular)
{
    float3 toLight = -lightDir;
    float ndl = saturate(dot(normalWS, toLight));

    LightTerms terms;
    terms.Diffuse = lightDiffuse * ndl;

    // Half-vector specular, BasicEffect style. Skip when ndl<=0 to avoid the
    // "specular wraps onto the back side" artefact — BasicEffect masks specular
    // by `step(0, ndl)` for the same reason.
    float3 halfWay = normalize(toLight + viewDirWS);
    float ndh = saturate(dot(normalWS, halfWay));
    float specMask = ndl > 0 ? 1.0 : 0.0;
    terms.Specular = lightSpecular * pow(ndh, SpecularPower) * specMask;

    return terms;
}

VSOutput VSDefault(VSInput input)
{
    VSOutput output;
    float4 worldPos = mul(input.Position, World);
    float4 viewPos  = mul(worldPos, View);
    output.PositionPS = mul(viewPos, Projection);
    output.PositionWS = worldPos.xyz;
    // Cheap per-vertex normal-to-world transform; assumes uniform scale (which
    // matches BasicEffect — Phase B will switch to inverse-transpose World3x3
    // if normal-mapping calls for it).
    output.NormalWS   = normalize(mul(input.Normal, (float3x3)World));
    output.TexCoord   = input.TexCoord;
    output.FogFactor  = ComputeFogFactor(length(viewPos.xyz));
    return output;
}

float4 PSDefault(VSOutput input) : SV_TARGET
{
    float4 texColor = TextureEnabled ? tex2D(TextureSampler, input.TexCoord) : float4(1, 1, 1, 1);

    float3 rgb;
    if (LightingEnabled)
    {
        float3 normalWS = normalize(input.NormalWS);
        float3 viewDirWS = normalize(EyePosition - input.PositionWS);

        // BasicEffect splits ambient and emissive contributions: ambient is
        // texture-modulated; emissive is added at the end after the
        // texture/diffuse modulation. We follow the same split below.
        float3 ambient = AmbientLightColor * DiffuseColor;

        LightTerms l0 = ComputeDirectional(normalWS, viewDirWS,
            DirLight0Direction, DirLight0DiffuseColor, DirLight0SpecularColor);
        LightTerms l1 = ComputeDirectional(normalWS, viewDirWS,
            DirLight1Direction, DirLight1DiffuseColor, DirLight1SpecularColor);
        LightTerms l2 = ComputeDirectional(normalWS, viewDirWS,
            DirLight2Direction, DirLight2DiffuseColor, DirLight2SpecularColor);

        float3 diffuseAcc  = (l0.Diffuse  + l1.Diffuse  + l2.Diffuse)  * DiffuseColor;
        float3 specularAcc = (l0.Specular + l1.Specular + l2.Specular) * SpecularColor;

        // Texture modulates ambient + diffuse but NOT specular (BasicEffect
        // convention — keeps highlights reading like reflections).
        rgb = (ambient + diffuseAcc) * texColor.rgb + specularAcc + EmissiveColor;
    }
    else
    {
        // No lighting: just diffuse * texture + emissive.
        rgb = DiffuseColor * texColor.rgb + EmissiveColor;
    }

    if (FogEnabled)
        rgb = lerp(rgb, FogColor, input.FogFactor);

    return float4(rgb, texColor.a * Alpha);
}

technique Default
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSDefault();
        PixelShader  = compile ps_4_0_level_9_1 PSDefault();
    }
}

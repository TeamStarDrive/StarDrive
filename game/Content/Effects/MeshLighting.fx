//-----------------------------------------------------------------------------
// MeshLighting.fx
//
// Phase 3.7 step 4 (Phase A + B + C: full per-mesh lighting) drop-in
// replacement for BasicEffect's per-pixel lighting path, plus per-pixel
// emissive (`_g` glow), specular (`_s`), and tangent-space normal (`_n`)
// map sampling.
//
// Lighting model (per-pixel):
//   ambient = AmbientLightColor * DiffuseColor
//   diffuse = sum_i(N·L_i_clamped * LightDiffuse_i) * DiffuseColor
//   specul. = sum_i((N·H_i)^SpecularPower * LightSpecular_i) * SpecularColor
//                                                            * SpecularMap.rgb
//   emiss.  = EmissiveMap.rgb (if bound) else EmissiveColor
//   color   = (ambient + diffuse) * tex.rgb + specul. + emiss.
//   alpha   = Alpha * tex.a
//
// Where N (the surface normal) is, when NormalMapEnabled:
//   sampled = tex2D(NormalSampler, uv).rgb * 2 - 1   // [0,1]→[-1,1]
//   N       = normalize(sampled.x * T + sampled.y * B + sampled.z * VN)
// And the vertex normal otherwise. T, B, VN are the world-space tangent,
// binormal, and vertex normal (computed at mesh-load time by
// SDNative/SdMesh/SdMeshGroup::ComputeTangentSpace, transformed into world
// space here in the VS).
//
// The "ambient/emissive multiplied by texture, specular not" split is the
// BasicEffect convention — keeps highlights reading as reflections rather
// than tinted-diffuse. Phase B added emissive/specular map sampling;
// Phase C adds normal-mapping for hull surface detail.
//
// Map fall-back semantics: each `*MapEnabled` flag gates the map sample.
// When false, the shader uses the per-material constant (or vertex normal,
// for normal mapping). This keeps the shader compatible with meshes that
// lack tangents in their vertex declaration (test cubes, possible XNB
// stragglers without `_n` content) — the flag stays false, the VS reads
// zero for the missing tangent/binormal but they're never used.
//
// All parameters are named so callers can use Effect.Parameters[name].
// LightingEffect (Ship_Game/Data/Mesh/SunBurnStubs.cs) caches
// EffectParameter handles and exposes BasicEffect-shaped properties for
// LightingEffectBinder, StaticMesh, etc.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 View;
float4x4 Projection;

// Phase 3.8.B: receiver-side shadow sampling. ShadowParams packs the
// per-frame state into a single float4: .x = enable flag (0/1), .y =
// constant depth bias. Packed instead of using two scalars because solo
// float uniforms appear to be at risk of being dropped or not pushed by
// MGFX 3.8.1 under ps_4_0_level_9_3 in some shader-layout configurations
// (observed in §3.8.B development; reproducible if a single-float uniform
// with a 0.0 default sits at the cbuffer tail). A float4 sidesteps that
// entirely. The matrix sits at the top of the cbuffer so neither uniform
// is the trailing entry.
//
// When .x = 1, the receiver re-projects its world position through
// LightViewProjection, looks up the occluder depth at the resulting NDC
// UV, and modulates diffuse + specular by 0/1 based on whether the
// comparison plus .y (bias) places the receiver behind the occluder.
// Ambient + emissive are NOT shadow-masked (ambient stands in for
// indirect light that fills shadows; emissive is the surface's own
// emission).
//
// Bias starts at 0.001 (matches the §3.8.B plan); tune in §3.8.C if
// acne / Peter-Panning surfaces. Single-caster scope: only the dominant
// directional / sun-anchor light drives the shadow texture (see
// LightingEffectBinder + ShadowMapComponent on the C# side).
float4x4 LightViewProjection;
float4   ShadowParams = float4(0.0, 0.001, 0.0, 0.0);

texture ShadowMap;
sampler2D ShadowSampler = sampler_state
{
    Texture   = (ShadowMap);
    // 1-tap point sampling: §3.8.B's MVP. PCF / soft edges are §3.8.C.
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

// Per-pixel point-light slots (3). Each shaded pixel computes its own
// direction to each point and applies smooth-quadratic radius falloff
// (`saturate(1 - (d/Radius)^2)`). That gives automatic per-ship parallax
// AND faithful SunBurn-style multi-light scenes (a system's Key /
// OverSaturationKey / LocalFill all affect the hull, with each light's
// radius determining its reach — OverSaturationKey's small radius means
// it only oversaturates ships near the sun, not the whole hull).
// LightingEffectBinder populates these from the closest system's 3
// PointLights; the slot is inert when not bound.
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

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    // SdMeshGroup writes Tangent+Binormal whenever the FBX has UVs, and
    // MeshInterface.TranslateNativeUsage maps them to TANGENT0/BINORMAL0 in
    // the VertexDeclaration. Meshes without these elements bind zero here —
    // the NormalMapEnabled flag gates whether they're consumed.
    float3 Tangent  : TANGENT0;
    float3 Binormal : BINORMAL0;
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
    // Phase 3.8.B: light-clip-space position for receiver-side shadow
    // sampling. Computed in VS so we don't redo the matrix mul per pixel.
    // Always emitted; PS gates on ShadowMapEnabled before sampling.
    float4 PositionLS : TEXCOORD6;
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

// Per-pixel point-light contribution with smooth-quadratic radius falloff.
// Direction is recomputed from world position so each shaded pixel sees the
// light from its own angle (parallax). Falloff is `saturate(1 - (d/R)^2)`,
// full at d=0 and zero at d=R — close enough to SunBurn's deferred falloff
// that the OverSaturationKey (R=7.5k) only over-saturates hulls near the
// sun while the Key (R=150k) lights the whole orbit.
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

VSOutput VSDefault(VSInput input)
{
    VSOutput output;
    float4 worldPos = mul(input.Position, World);
    float4 viewPos  = mul(worldPos, View);
    output.PositionPS = mul(viewPos, Projection);
    output.PositionWS = worldPos.xyz;
    // World-space normal/tangent/binormal. We use World3x3 directly (not
    // inverse-transpose) — this matches BasicEffect's behavior and is correct
    // under uniform scale, which all StarDrive meshes use. Non-uniform scale
    // would skew the basis, but no current content requires it.
    output.NormalWS   = normalize(mul(input.Normal,   (float3x3)World));
    output.TangentWS  = normalize(mul(input.Tangent,  (float3x3)World));
    output.BinormalWS = normalize(mul(input.Binormal, (float3x3)World));
    output.TexCoord   = input.TexCoord;
    output.FogFactor  = ComputeFogFactor(length(viewPos.xyz));
    // Phase 3.8.B: world-space → light-clip-space for the shadow lookup.
    // Under the depth pass's orthographic projection w==1, but compute the
    // full mul so a future move to a perspective light still works.
    output.PositionLS = mul(worldPos, LightViewProjection);
    return output;
}

// Returns 1 if the surface is lit, 0 if shadowed. Falls through to 1 for
// any of: the shader has no shadow map bound, the surface projects outside
// the light frustum (UV clamp would otherwise sample the edge texel and
// produce phantom shadows along scene edges), or the depth comparison
// places the receiver in front of the occluder.
float SampleShadowFactor(float4 positionLS)
{
    if (ShadowParams.x < 0.5) return 1.0;

    // Perspective divide. Under the §3.8.A ortho projection w == 1, so
    // this is a no-op today; spelled out for the eventual perspective case.
    float invW = 1.0 / positionLS.w;
    float2 ndcXY = positionLS.xy * invW;
    float  receiverDepth = positionLS.z * invW;

    // NDC [-1,1] → texture UV [0,1] with the standard Y flip (DirectX
    // top-left origin vs NDC bottom-left).
    float2 uv = float2(ndcXY.x * 0.5 + 0.5, -ndcXY.y * 0.5 + 0.5);

    // Reject samples outside the light frustum so receivers behind the
    // light camera, off to the side, or beyond the far plane stay lit.
    // Without this, AddressU/V=Clamp would re-use the border occluder
    // depth and shadow the entire off-frustum half-space.
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0 ||
        receiverDepth < 0.0 || receiverDepth > 1.0)
        return 1.0;

    float occluderDepth = tex2D(ShadowSampler, uv).r;
    return (occluderDepth + ShadowParams.y < receiverDepth) ? 0.0 : 1.0;
}

float4 PSDefault(VSOutput input) : SV_TARGET
{
    float4 texColor = TextureEnabled ? tex2D(TextureSampler, input.TexCoord) : float4(1, 1, 1, 1);

    // Per-pixel emissive: glow map (`_g`) is the source of truth when bound;
    // it carries the full emissive color in its RGB channels (cockpit windows,
    // engine bells, panel lights). When no map is bound, fall back to the
    // per-material EmissiveColor constant.
    //
    // Why not multiply by EmissiveColor: FBX-imported ships go through
    // MeshInterface.CreateMaterialEffect which doesn't currently set
    // EmissiveColor (commented-out line, defaults to Vector3.Zero). The map
    // would silently render black if multiplied. SunBurn's original behavior
    // when an emissive map was present was effectively "the map IS the
    // emissive" — multiplication added per-material tint that almost never
    // diverged from white in practice.
    float3 emissive = EmissiveMapEnabled
        ? tex2D(EmissiveSampler, input.TexCoord).rgb
        : EmissiveColor;

    // Per-pixel specular mask: `_s` map controls specularity. Chrome panels
    // → bright; matte hull paint → dim. Sampled into a single multiplier
    // applied to the per-light specular accumulation.
    float3 specularMask = SpecularMapEnabled
        ? tex2D(SpecularSampler, input.TexCoord).rgb
        : float3(1, 1, 1);

    float3 rgb;
    if (LightingEnabled)
    {
        // Per-pixel surface normal: when a normal map is bound, decode the
        // tangent-space sample (RGB encodes [-1,1] via *2-1) and rotate into
        // world space using the TBN basis. Otherwise fall back to the
        // interpolated vertex normal — same as Phase A behavior.
        float3 normalWS;
        if (NormalMapEnabled)
        {
            float3 tangentSpaceN = tex2D(NormalSampler, input.TexCoord).rgb * 2.0 - 1.0;
            float3 T = normalize(input.TangentWS);
            float3 B = normalize(input.BinormalWS);
            float3 N = normalize(input.NormalWS);
            // Mat-mul form: rotated = sampled.x*T + sampled.y*B + sampled.z*N
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

        // Per-pixel point lights — direction recomputed per pixel from
        // world position, smooth-quadratic radius falloff. All 3 sum
        // additively into the same diffuse/specular accumulators.
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

        // Phase 3.8.B: receiver-side shadow gate. Multiplies wholesale into
        // the direct-light accumulators; ambient + emissive remain to fill
        // the shadowed region (same convention BasicEffect's would-be
        // shadow extension used). Branch-free: SampleShadowFactor returns
        // 1.0 when ShadowParams.x is 0, so meshes drawn in scenes without
        // a shadow caster pay only the static-uniform cost.
        float shadowFactor = SampleShadowFactor(input.PositionLS);
        diffuseAcc  *= shadowFactor;
        specularAcc *= shadowFactor;

        // Texture modulates ambient + diffuse but NOT specular (BasicEffect
        // convention — keeps highlights reading like reflections). Emissive
        // is added on top, unmodulated by per-pixel lighting.
        rgb = (ambient + diffuseAcc) * texColor.rgb + specularAcc + emissive;
    }
    else
    {
        // No lighting: just diffuse * texture + emissive.
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
        // ps_4_0_level_9_3 (SM3.0 hardware) — needed because Phase B's per-pixel
        // emissive + specular map sampling pushes the shader past the 64-slot
        // ps_2_0 / level_9_1 limit. Level 9.3 = SM3.0 era (~2005+ hardware),
        // universally available on any modern desktop GPU and integrated.
        VertexShader = compile vs_4_0_level_9_3 VSDefault();
        PixelShader  = compile ps_4_0_level_9_3 PSDefault();
    }
}

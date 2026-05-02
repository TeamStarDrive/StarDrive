//-----------------------------------------------------------------------------
// Distort.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

sampler SceneTexture : register(s0);
sampler DistortionMap : register(s1);

#define SAMPLE_COUNT 15
float2 SampleOffsets[SAMPLE_COUNT];
float SampleWeights[SAMPLE_COUNT];

// The Distortion map represents zero displacement as 0.5, but in an 8 bit color
// channel there is no exact value for 0.5. ZeroOffset adjusts for this error.
const float ZeroOffset = 0.5f / 255.0f;

// Phase 2.8 (mgfxc port): mgfxc's parser does not support function-call arguments
// on `compile` (XNA's fxc did). Split the original `uniform bool distortionBlur`
// specialization into two zero-arg pixel shaders.
float4 Distort_NoBlur_PixelShader(float2 TexCoord : TEXCOORD0) : COLOR0
{
    float2 displacement = tex2D(DistortionMap, TexCoord).rg;

    if ((displacement.x == 0) && (displacement.y == 0))
        return tex2D(SceneTexture, TexCoord);

    // Convert from [0,1] to [-.5, .5)
    displacement -= .5 + ZeroOffset;
    return tex2D(SceneTexture, TexCoord.xy + displacement);
}

float4 Distort_Blur_PixelShader(float2 TexCoord : TEXCOORD0) : COLOR0
{
    float2 displacement = tex2D(DistortionMap, TexCoord).rg;

    if ((displacement.x == 0) && (displacement.y == 0))
        return tex2D(SceneTexture, TexCoord);

    displacement -= .5 + ZeroOffset;

    float4 finalColor = 0;
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        finalColor += tex2D(SceneTexture, TexCoord.xy + displacement +
            SampleOffsets[i]) * SampleWeights[i];
    }
    return finalColor;
}

technique Distort
{
    pass
    {
        PixelShader = compile ps_4_0_level_9_1 Distort_NoBlur_PixelShader();
    }
}

technique DistortBlur
{
    pass
    {
        PixelShader = compile ps_4_0_level_9_1 Distort_Blur_PixelShader();
    }
}
//-----------------------------------------------------------------------------
// ParticleEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


// Camera parameters.
float4x4 View;
float4x4 Projection;
float2 ViewportScale;


// The current time, in seconds.
float CurrentTime;


// Parameters describing how the particles animate.
float Duration;
float DurationRandomness;
float EndVelocity;
float4 MinColor;
float4 MaxColor;


// These float2 parameters describe the min and max of a range.
// The actual value is chosen differently for each particle,
// interpolating between x and y by some random amount.
float2 RotateSpeed;
float2 StartSize;
float2 EndSize;


// Particle texture and sampler.
texture Texture;

sampler Sampler = sampler_state
{
    Texture = (Texture);
    
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    
    AddressU = Clamp;
    AddressV = Clamp;
};


// Vertex shader input structure describes the start position and
// velocity of the particle, and the time at which it was created,
// along with some random values that affect its size and rotation.
struct VertexShaderInput
{
    float2 Corner : POSITION0;
    float3 Position : POSITION1;
    float3 Velocity : NORMAL0;
    float4 Color : COLOR0;
    float4 Random : COLOR1;
    float Scale : TEXCOORD0;
    float Time : TEXCOORD1;
};


// Vertex shader output structure specifies the position and color of the particle.
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : COLOR1;
};

// Apply the camera view and projection transforms.
float4 ToScreenCoords(float3 pos)
{
    return mul(mul(float4(pos, 1), View), Projection);
}

float4 GetPosition(float3 position, float3 velocity,
                   float age, float normalizedAge)
{
    float startVelocity = length(velocity);

    // Work out how fast the particle should be moving at the end of its life,
    // by applying a constant scaling factor to its starting velocity.
    float endVelocity = startVelocity * EndVelocity;
    
    // Our particles have constant acceleration, so given a starting velocity
    // S and ending velocity E, at time T their velocity should be S + (E-S)*T.
    // The particle position is the sum of this velocity over the range 0 to T.
    // To compute the position directly, we must integrate the velocity
    // equation. Integrating S + (E-S)*T for T produces S*T + (E-S)*T*T/2.

    float velocityIntegral = startVelocity * normalizedAge +
                             (endVelocity - startVelocity) * normalizedAge *
                                                             normalizedAge / 2;
     
    position += normalize(velocity) * velocityIntegral * Duration;
    
    return ToScreenCoords(position);
}

float GetParticleSize(float randomValue, float normalizedAge)
{
    // Apply a random factor to make each particle a slightly different size.
    float startSize = lerp(StartSize.x, StartSize.y, randomValue);
    float endSize = lerp(EndSize.x, EndSize.y, randomValue);
    
    // Compute the actual size based on the age of the particle.
    float size = lerp(startSize, endSize, normalizedAge);
    
    // Project the size into screen coordinates.
    return size * Projection._m11;
}

float4 GetStaticParticleColor(float randomValue)
{
    // Apply a random factor to make each particle a slightly different color.
    return lerp(MinColor, MaxColor, randomValue);
}

float4 GetParticleColor(float randomValue, float normalizedAge)
{
    float4 color = GetStaticParticleColor(randomValue);
    
    // Fade the alpha based on the age of the particle. This curve is hard coded
    // to make the particle fade in fairly quickly, then fade out more slowly:
    // plot x*(1-x)*(1-x) for x=0:1 in a graphing program if you want to see what
    // this looks like. The 6.7 scaling factor normalizes the curve so the alpha
    // will reach all the way up to fully solid.
    color.a *= normalizedAge * (1-normalizedAge) * (1-normalizedAge) * 6.7;
    return color;
}

float2x2 GetRandomizedRotation(float randomValue, float age)
{    
    // Apply a random factor to make each particle rotate at a different speed.
    float rotateSpeed = lerp(RotateSpeed.x, RotateSpeed.y, randomValue);
    
    float rotation = rotateSpeed * age;

    // Compute a 2x2 rotation matrix.
    float c = cos(rotation);
    float s = sin(rotation);
    
    return float2x2(c, -s, s, c);
}

float GetParticleAge(float time, float random)
{
    float age = CurrentTime - time;
    // Apply a random factor to make different particles age at different rates.
    age *= 1 + random * DurationRandomness;
    return age;
}

// Normalize the age into the range zero to one.
float GetNormalizedAge(float age)
{
    return saturate(age / Duration);
}

VertexShaderOutput DynamicParticleVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float age = GetParticleAge(input.Time, input.Random.x);
    float normalizedAge = GetNormalizedAge(age);
    float size = GetParticleSize(input.Random.y, normalizedAge) * input.Scale;
    float2x2 rotation = GetRandomizedRotation(input.Random.w, age);

    output.Position = GetPosition(input.Position, input.Velocity, age, normalizedAge);
    // this cleverly scales the Quad corners
    output.Position.xy += mul(input.Corner, rotation) * size * ViewportScale;
    output.Color = GetParticleColor(input.Random.z, normalizedAge) * input.Color;
    output.TextureCoordinate = (input.Corner + 1) / 2;
    return output;
}

VertexShaderOutput DynamicNonRotatingParticleVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float age = GetParticleAge(input.Time, input.Random.x);
    float normalizedAge = GetNormalizedAge(age);
    float size = GetParticleSize(input.Random.y, normalizedAge) * input.Scale;

    output.Position = GetPosition(input.Position, input.Velocity, age, normalizedAge);
    // this cleverly scales the Quad corners
    output.Position.xy += input.Corner * size * ViewportScale;
    output.Color = GetParticleColor(input.Random.z, normalizedAge) * input.Color;
    output.TextureCoordinate = (input.Corner + 1) / 2;
    return output;
}

VertexShaderOutput StaticRotatingParticleVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float age = GetParticleAge(input.Time, input.Random.x);
    float normalizedAge = GetNormalizedAge(age);
    float size = GetParticleSize(input.Random.y, normalizedAge) * input.Scale;
    float2x2 rotation = GetRandomizedRotation(input.Random.w, age);

    output.Position = ToScreenCoords(input.Position);
    // this cleverly scales the Quad corners
    output.Position.xy += mul(input.Corner, rotation) * size * ViewportScale;
    output.Color = GetStaticParticleColor(input.Random.z) * input.Color;
    output.TextureCoordinate = (input.Corner + 1) / 2;
    return output;
}

VertexShaderOutput StaticNonRotatingParticleVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float age = GetParticleAge(input.Time, input.Random.x);
    float normalizedAge = GetNormalizedAge(age);
    float size = GetParticleSize(input.Random.y, normalizedAge) * input.Scale;

    output.Position = ToScreenCoords(input.Position);
    // this cleverly scales the Quad corners
    output.Position.xy += input.Corner * size * ViewportScale;
    output.Color = GetStaticParticleColor(input.Random.z) * input.Color;
    output.TextureCoordinate = (input.Corner + 1) / 2;
    return output;
}

// Pixel shader for drawing particles.
float4 ParticlePixelShader(VertexShaderOutput input) : COLOR0
{
    return tex2D(Sampler, input.TextureCoordinate) * input.Color;
}

// Dynamic particles have velocity, rotation, all features
technique FullDynamicParticles
{
    pass P0
    {
        VertexShader = compile vs_2_0 DynamicParticleVS();
        PixelShader = compile ps_2_0 ParticlePixelShader();
    }
}

// Particles that move but do not rotate
technique DynamicNonRotatingParticles
{
    pass P0
    {
        VertexShader = compile vs_2_0 DynamicNonRotatingParticleVS();
        PixelShader = compile ps_2_0 ParticlePixelShader();
    }
}

// Static particles never move, but they can rotate
technique StaticRotatingParticles
{
    pass P0
    {
        VertexShader = compile vs_2_0 StaticRotatingParticleVS();
        PixelShader = compile ps_2_0 ParticlePixelShader();
    }
}

// Static non-rotating particles, never move, never rotate
technique StaticNonRotatingParticle
{
    pass P0
    {
        VertexShader = compile vs_2_0 StaticNonRotatingParticleVS();
        PixelShader = compile ps_2_0 ParticlePixelShader();
    }
}

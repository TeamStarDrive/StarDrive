//-----------------------------------------------------------------------------
// Simple.fx
// Takes [Position, Color, TexCoords] and draws a textured sprite
//-----------------------------------------------------------------------------

// Camera parameters.
float4x4 ViewProjection;

// Texture and sampler
texture Texture;
bool UseTexture;

sampler Sampler = sampler_state
{
    Texture = (Texture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
};

struct VertexShaderInput
{
    float3 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Coords : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : COLOR1;
};

// Vertex shader
VertexShaderOutput SimpleVertexShader(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(float4(input.Position, 1), ViewProjection);
    output.Color = input.Color;
    output.TextureCoordinate = input.Coords;
    return output;
}

// Pixel shader
float4 SimplePixelShader(VertexShaderOutput input) : COLOR0
{
    if (UseTexture)
    {
        return tex2D(Sampler, input.TextureCoordinate) * input.Color;
    }
    else
    {
        return input.Color;
    }
}

technique Simple
{
    pass P0
    {
        VertexShader = compile vs_2_0 SimpleVertexShader();
        PixelShader = compile ps_2_0 SimplePixelShader();
    }
}

//-----------------------------------------------------------------------------
// Simple.fxh
// Defines a [Position, Color, Coords] type of shader with some common variables
//-----------------------------------------------------------------------------


/**
 * Vertex Shader input
 */
struct SimpleVSInput
{
    float3 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Coords : TEXCOORD0;
};


/**
 * Output of a Vertex Shader
 */
struct SimpleVSOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : COLOR1;
};


/**
 * ViewProjection matrix
 */
float4x4 ViewProjection;


/**
 * Multiplicative Color modifier, allowing to adjust the tone of the entire output
 */
float4 Color;


/**
 * Texture and sampler information
 */
texture Texture;
bool UseTexture;


// simple sampler with some default settings
sampler ClampSampler = sampler_state
{
    Texture = (Texture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler WrapSampler = sampler_state
{
    Texture = (Texture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};


/**
 * A simple vertex shading function
 */
SimpleVSOutput SimpleVertexShader(SimpleVSInput input)
{
    SimpleVSOutput output;
    output.Position = mul(float4(input.Position, 1), ViewProjection);
    output.Color = input.Color * Color;
    output.TextureCoordinate = input.Coords;
    return output;
}


/**
 * A simple pixel shading function
 */
float4 SimplePixelShader(SimpleVSOutput input) : COLOR0
{
    if (UseTexture)
    {
        return tex2D(ClampSampler, input.TextureCoordinate) * input.Color;
    }
    else
    {
        return input.Color;
    }
}

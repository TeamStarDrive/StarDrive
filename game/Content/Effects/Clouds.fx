//-----------------------------------------------------------------------------
// Clouds.fx -- compatible with Simple.fx and thus can be used with SpriteRenderer
// Takes [Position, Color, TexCoords] and draws a textured sprite
//-----------------------------------------------------------------------------
#include "Simple.fxh"

// Clouds position
float2 Position;

SimpleVSOutput CloudsVertexShader(SimpleVSInput input)
{
    SimpleVSOutput output;
    output.Position = mul(float4(input.Position, 1), ViewProjection);
    output.Color = input.Color * Color;
    output.TextureCoordinate = input.Coords;
    return output;
}

float4 CloudsPixelShader(SimpleVSOutput input) : COLOR0
{
    // I think this tries to sample the noise texture twice
    // giving the impression of small-scale detail and large-scale detail
    float2 t0 = input.TextureCoordinate;
    float2 pos = Position * 0.00025;

    // adding to the texture pos makes it bigger
    // reducing the texture pos makes it bigger
    float detailScaler = 0.5;
    float macroScaler = 0.4;

    // make the macro scale move faster compared to detail layer
    // for this we need to add the base pos
    float2 macroSpeed = pos + float2(1.25, -0.15);

    float2 detailCoords = (t0 + pos) * detailScaler;
    float2 macroCoords = (t0 + pos + macroSpeed) * macroScaler;
    float4 detailColor = tex2D(WrapSampler, detailCoords);
    float4 macroColor = tex2D(WrapSampler, macroCoords);

    // detail noise in red/blue
    // macro noise layer in green
    // alpha is a mix
    float a = (detailColor.a * 0.25) + (macroColor.a * 0.15);
    float4 outColor = float4(detailColor.r, macroColor.g, detailColor.b, a);

    // also use per-vertex color modifier
    return outColor * input.Color;
}

// void mainImage( out vec4 fragColor, in vec2 fragCoord )
// {
//     // Normalized pixel coordinates (from 0 to 1)
//     vec2 t0 = fragCoord / iResolution.xy;

//     //vec2 pos = Position * 0.00025;
//     vec2 pos = vec2(iTime * 0.00025, iTime * 0.00025 * 0.25);
    
//     // adding to the texture pos makes it bigger
//     // reducing the texture pos makes it bigger
//     float detailScaler = 0.5;
//     float macroScaler = 0.4;
    
//     // make the macro scale move faster compared to detail layer
//     // for this we need to add the base pos
//     vec2 macroSpeed = pos + vec2(1.25, -0.15);
    
//     vec2 detailCoords = (t0 + pos) * detailScaler;
//     vec2 macroCoords = (t0 + pos + macroSpeed) * macroScaler;
//     vec4 detailColor = texture(iChannel0, detailCoords);
//     vec4 macroColor = texture(iChannel0, macroCoords);

//     // detail noise in red/blue
//     // macro noise layer in green
//     // alpha is a mix
//     float a = (detailColor.a * 0.25) + (macroColor.a * 0.15);
//     vec4 outColor = vec4(detailColor.r, macroColor.g, detailColor.b, a);

//     // Output to screen
//     fragColor = outColor;
// }

technique Technique1
{
    pass P0
    {
        VertexShader = compile vs_2_0 CloudsVertexShader();
        PixelShader = compile ps_2_0 CloudsPixelShader();
    }
}

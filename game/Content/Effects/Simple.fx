//-----------------------------------------------------------------------------
// Simple.fx - for SpriteRenderer
// Takes [Position, Color, TexCoords] and draws a textured sprite
//-----------------------------------------------------------------------------
#include "Simple.fxh"

technique Simple
{
    pass P0
    {
        VertexShader = compile vs_2_0 SimpleVertexShader();
        PixelShader = compile ps_2_0 SimplePixelShader();
    }
}

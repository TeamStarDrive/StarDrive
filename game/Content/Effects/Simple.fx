//-----------------------------------------------------------------------------
// Simple.fx - for SpriteRenderer
// Takes [Position, Color, TexCoords] and draws a textured sprite
//-----------------------------------------------------------------------------
#include "Simple.fxh"

technique Simple
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_1 SimpleVertexShader();
        PixelShader = compile ps_4_0_level_9_1 SimplePixelShader();
    }
}

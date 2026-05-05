//-----------------------------------------------------------------------------
// BeamFX.fx
//
// Phase 3.5 hand-rewrite of the XNA 3.1 "BeamFX" effect that ships as
// game/Content/Effects/BeamFX.xnb. Disassembly was blocked through Phase 3.3
// by an LZX framing bug in EffectXnbDump (frame_size byte order was reversed
// — for the other 4 effects the wrong-endian value happened to exceed
// remaining and got clamped, but BeamFX's clamp didn't fire and corrupted the
// decode). With the framing fixed, the XNB layout is identical to scale.xnb's
// minus the `scale` parameter; the original D3DX shaders disassemble to a
// trivially small program (see beamfx-chunks/ under phase3-logs/).
//
// Original shaders (vs_1_1 + ps_2_0):
//
//   VS:  oPos = mul(Position, WorldViewProjection)
//        oT0  = TexCoord + displacement          // scroll UV along the beam
//
//   PS:  return tex2D(texsampler, oT0)           // sample the beam texture
//
// The XNB also defines `AlphaMap` / `AlphaMapSampler` parameters but neither
// shader references them, so they are dead-on-disk and we drop them here.
// (Beam.cs only binds World/View/Projection/tex/displacement — never AlphaMap
// — which matches the disassembly.)
//
// Used by Beam.cs to draw beam weapons:
//   - tex          : beam texture (Beams/<weaponBeamTexture>.xnb)
//   - World        : identity (per-beam quad lives in world space already)
//   - View / Projection : universe camera matrices
//   - displacement : Vector2(0, scrollOffset) — Beam.Displacement walks 1→0
//                    by 0.05/frame, wrapping back to 1, producing a continuous
//                    flow effect along the beam's V axis.
//-----------------------------------------------------------------------------

float4x4 World;
float4x4 View;
float4x4 Projection;
float2   displacement;

texture tex;
sampler2D texsampler = sampler_state
{
    Texture   = (tex);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Wrap;
    AddressV  = Wrap;
};

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSBeam(VSInput input)
{
    VSOutput output;
    float4x4 wvp = mul(mul(World, View), Projection);
    output.Position = mul(input.Position, wvp);
    output.TexCoord = input.TexCoord + displacement;
    return output;
}

float4 PSBeam(VSOutput input) : COLOR0
{
    return tex2D(texsampler, input.TexCoord);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSBeam();
        PixelShader  = compile ps_4_0_level_9_1 PSBeam();
    }
}

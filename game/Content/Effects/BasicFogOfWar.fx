//-----------------------------------------------------------------------------
// BasicFogOfWar.fx
//
// Phase 3.7 step 3 hand-rewrite of the XNA 3.1 BasicFogOfWar effect that
// ships as game/Content/Effects/BasicFogOfWar.xnb. MonoGame's MGFX reader
// rejects the original D3DX ps_2_0 bytecode; the .xnb stays on disk for mod
// compatibility while this .fx is compiled to .mgfxo and wins via the
// .xnb -> .mgfxo sibling fallback in GameContentManager.
//
// Behavior matches the original 4-instruction PS (disassembly under
// C:/Users/gkapu/.claude/plans/phase3-logs/fogofwar-chunks/):
//
//   PS:  rgb = tex2D(ColorSampler,  uv)         // scene RT
//        a   = tex2D(LightsSampler, uv).r        // lights mask (red channel)
//        return float4(rgb, a)
//
// Caller (UniverseScreen.DrawMainRTWithFogOfWarEffect) clears the back
// buffer to black, then SpriteBatch-draws the scene RT through this effect
// with AlphaBlend on. Result: bright LightsTarget pixels show full scene,
// dark LightsTarget pixels alpha-blend to black (= the fog of war).
//
// Sampler bindings:
//   s0 = ColorSampler  — supplied by SpriteBatch.Draw(scene, ...). The
//                        SpriteBatch texture argument always lands on s0.
//   LightsTexture      — bound through the parameter-driven sampler-state
//                        below. Bind via Effect.Parameters["LightsTexture"]
//                        .SetValue(lightsRT). DO NOT use device.Textures[1]
//                        directly — SpriteBatch.End's flush sometimes
//                        discards explicit s1 bindings under MGFX 3.8.1.303
//                        (this was the failure mode of the 2026-05-02
//                        attempt; see GameContentManager.cs notes).
//
// VS+PS (not PS-only) because PS-only through SpriteBatch is unreliable
// under MonoGame 3.8.1.303 / DirectX_11 — the manual Pass.Apply between
// Begin/Draw doesn't replace SpriteEffect's PS in time for the immediate-
// mode flush. The canonical workaround (used by all our other post-process
// effects: BloomCombine, BloomExtract, scale, Distort) is to ship a real
// VS+PS and pass the effect to SpriteBatch.Begin's `effect:` argument so
// our pass IS the SpriteBatch pass.
//-----------------------------------------------------------------------------

float4x4 MatrixTransform;
sampler  ColorSampler : register(s0);

texture LightsTexture;
sampler2D LightsSampler : register(s1) = sampler_state
{
    Texture   = (LightsTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

struct VSInput  { float4 Position : POSITION0; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };
struct VSOutput { float4 Position : SV_POSITION; float4 Color : COLOR0; float2 TexCoord : TEXCOORD0; };

VSOutput VSPassthrough(VSInput input)
{
    VSOutput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color    = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 PSFogOfWar(VSOutput input) : SV_TARGET
{
    float4 col    = tex2D(ColorSampler,  input.TexCoord);
    float  lights = tex2D(LightsSampler, input.TexCoord).r;
    col.a = lights;
    return col;
}

technique BasicFogOfWar
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VSPassthrough();
        PixelShader  = compile ps_4_0_level_9_1 PSFogOfWar();
    }
}

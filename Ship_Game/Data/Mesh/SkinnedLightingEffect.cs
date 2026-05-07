using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects.Forward;

// Phase 3.10.B.5: skinned variant of LightingEffect, backed by SkinnedLightingEffect.mgfxo.
// The PS is identical to MeshLighting.fx so the per-pixel lighting / map sampling
// surface (DirectionalLight0/1/2, PointLight0/1/2, normal/specular/emissive maps,
// shadow uniforms) is inherited verbatim from the base class. The only addition
// is the matrix-palette skinning happening in the VS pre-pass: this class adds
// SetBoneTransforms(Matrix[]) which pushes a per-instance bone palette to the
// `Bones` parameter consumed by VSSkinned.
public sealed class SkinnedLightingEffect : LightingEffect
{
    // Mirror the .fx's `#define MaxBones 64`. Renderer (B.6) clamps palette size
    // to this when uploading; lightweight ship rigs sit well under it.
    public const int MaxBones = 64;

    static byte[] s_skinnedBytes;
    static readonly object s_loadLock = new();

    readonly EffectParameter pBones;

    public new static bool TryLoadShared(string contentPath)
    {
        lock (s_loadLock)
        {
            if (s_skinnedBytes != null) return true;
            try
            {
                if (File.Exists(contentPath))
                {
                    s_skinnedBytes = File.ReadAllBytes(contentPath);
                    return true;
                }
            }
            catch { /* fall through; ctor will throw with a clear message */ }
            return false;
        }
    }

    static byte[] GetSkinnedBytes()
    {
        if (s_skinnedBytes == null)
            throw new InvalidOperationException(
                "SkinnedLightingEffect: SkinnedLightingEffect.mgfxo bytes not loaded. Call " +
                "SkinnedLightingEffect.TryLoadShared() during startup (e.g. from " +
                "ResourceManager.CreateCoreGfxResources).");
        return s_skinnedBytes;
    }

    public SkinnedLightingEffect(GraphicsDevice device) : base(device, GetSkinnedBytes())
    {
        pBones = Parameters["Bones"];
    }

    // Pushes the per-instance skinning palette to the shader's `Bones[64]`
    // float4x3 array. Caller (BoneAnimationPlayer) supplies the inverseBind *
    // worldCurrent matrices in XNA row-major form; MonoGame's
    // EffectParameter.SetValue(Matrix[]) handles the row-to-column transpose
    // when the underlying parameter is a float4x3 array.
    //
    // Palettes longer than MaxBones are silently truncated — the shader can't
    // address beyond its declared array. A truncation should never happen in
    // practice (StarDrive ship rigs peak around 30 bones); if it ever does,
    // bump MaxBones in both this file and SkinnedLightingEffect.fx in lockstep.
    public void SetBoneTransforms(Matrix[] palette)
    {
        if (pBones == null) return;
        if (palette == null || palette.Length == 0) return;

        if (palette.Length <= MaxBones)
        {
            pBones.SetValue(palette);
            return;
        }

        var clipped = new Matrix[MaxBones];
        Array.Copy(palette, clipped, MaxBones);
        pBones.SetValue(clipped);
    }
}

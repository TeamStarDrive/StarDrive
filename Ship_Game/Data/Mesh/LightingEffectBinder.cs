using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SunBurnLights = SynapseGaming.LightingSystem.Lights;
using XnaDirectionalLight = Microsoft.Xna.Framework.Graphics.DirectionalLight;

namespace Ship_Game.Data.Mesh;

/// <summary>
/// Phase 2.8 sub-phase A6: pushes the submitted lights from the SunBurn-stub
/// LightManager onto the underlying BasicEffect's directional-light slots, plus
/// ambient and fog from SceneEnvironment. This is the bridge between the data
/// carriers added in 2.7 Scope A and the GPU-bound effect parameters.
///
/// Forward-compat hook: when LightingEffect is swapped to a custom MGFX effect
/// in Phase 3, this is the only call site that needs to learn the new parameter
/// names. Renderers (RenderManager.Render, StaticMesh.Draw) keep calling Apply.
/// </summary>
public static class LightingEffectBinder
{
    static bool s_warnedExtraDirectional;

    public static void Apply(LightingEffect fx,
                             IReadOnlyList<SunBurnLights.ILight> lights,
                             SceneEnvironment env,
                             Vector3 cameraPos,
                             Texture2D shadowMap = null,
                             Matrix lightViewProjection = default)
    {
        if (fx == null) return;

        // 3 directional slots + 3 per-pixel PointLight slots (the "Sun" group;
        // see MeshLighting.fx ComputePoint). Point lights are populated from
        // the closest enabled system Key by XY (universe scenes submit one Key
        // per system); the 3 lights at that Key's XY (Key + LocalFill +
        // OverSaturationKey) all bind into the slots so per-pixel parallax +
        // each light's native radius falloff replicate SunBurn's deferred
        // multi-light scene.
        Vector3 ambient = env?.AmbientLightColor ?? new Vector3(0.2f, 0.2f, 0.2f);
        XnaDirectionalLight[] slots = { fx.DirectionalLight0, fx.DirectionalLight1, fx.DirectionalLight2 };
        int dirIndex = 0;

        // Pick the PointLight CLOSEST to the camera as the scene "sun" anchor.
        // Universe submits per-system PointLights; with a globally-brightest
        // selector, the active view could end up lit by a sun from a different
        // system (visible regression: ship lit from the opposite side of the
        // in-scene sun glow). Closest-to-camera correctly selects the system
        // the player is looking at.
        //
        // XY-only distance: camera Z varies hugely with zoom (~-1000 zoomed
        // in, ~-250000 zoomed out); including dz² makes every sun look
        // equidistant when zoomed out and selection becomes order-dependent.
        // The play plane sits at z=0 and all system Keys at z=-5500, so the
        // sun the player is "looking at" is always the one closest in XY.
        //
        // Radius bounds: include only system-scale scene lights.
        //   - Key      (z=-5500, R = sun.Radius ~150k)         ✓
        //   - LocalFill (z=0,    R = sun.Radius ~150k)         ✓
        //   - OverSaturationKey (z=-1500, R = 0.05*sun.Radius
        //     ~7.5k) — included for the per-pixel falloff path; small
        //     radius means it only over-brightens hulls within ~7.5k of
        //     the sun (correct sun-near behavior, like SunBurn).
        //   - Global Fill / Back (z=±150M, R ~150M) — excluded by R < 1M.
        //     They were SunBurn ambient proxies; including them lets a
        //     deep-space camera pick the (0,0,-150M) origin as the sun
        //     and the dominant Z drives every pixel to N·L=1 (uniform
        //     straight-down light, no shading).
        SunBurnLights.PointLight bestSun = null;
        float bestSunDist2 = float.MaxValue;

        if (lights != null)
        {
            for (int i = 0; i < lights.Count; ++i)
            {
                switch (lights[i])
                {
                    case SunBurnLights.DirectionalLight d when d.Enabled:
                        if (dirIndex < slots.Length)
                        {
                            XnaDirectionalLight slot = slots[dirIndex++];
                            slot.Enabled = true;
                            slot.Direction = Vector3.Normalize(d.Direction);
                            slot.DiffuseColor = d.DiffuseColor * d.Intensity;
                            // Phase 3.7: tight specular only. SunIntensity tends to be 2-4
                            // in legacy SunBurn scenes, and BasicEffect's specular term
                            // (per-light * SpecularPower-falloff * uniform-mtl-spec) gets
                            // gain-stacked, blowing out highlights to white over the
                            // already-saturated diffuse. Keep a small specular tint —
                            // texture-derived material spec dominates the highlight color.
                            slot.SpecularColor = d.DiffuseColor * 0.15f;
                        }
                        else if (!s_warnedExtraDirectional)
                        {
                            s_warnedExtraDirectional = true;
                            Log.Warning("LightingEffectBinder: more than 3 directional lights submitted; extras dropped (BasicEffect supports 3).");
                        }
                        break;

                    case SunBurnLights.AmbientLight a:
                        ambient += a.DiffuseColor * a.Intensity;
                        break;

                    case SunBurnLights.PointLight p when p.Enabled && p.Radius >= 1000f && p.Radius < 1_000_000f:
                        float dx = p.Position.X - cameraPos.X;
                        float dy = p.Position.Y - cameraPos.Y;
                        float dist2 = dx*dx + dy*dy;
                        if (dist2 < bestSunDist2)
                        {
                            bestSun = p;
                            bestSunDist2 = dist2;
                        }
                        break;
                }
            }
        }

        // Disable any remaining unfilled slots.
        for (int i = dirIndex; i < slots.Length; ++i)
            slots[i].Enabled = false;

        // Populate the 3 PointLight slots from the closest system's lights
        // (those sharing XY with the chosen Key). Pre-migration SunBurn used
        // 3 PointLights per system: Key (full radius scene light), LocalFill
        // (full radius white fill), OverSaturationKey (small radius, 5×
        // intensity sun-pixel oversaturator). Each had its own falloff in
        // the deferred pass; the shader's per-pixel `1 - (d/R)^2` falloff
        // replicates that — so OverSaturationKey only over-brightens hulls
        // within its 7.5k radius without amplifying every hull 5× as a
        // uniform factor would.
        var slot0 = default(LightingEffect.PointLightSlot);
        var slot1 = default(LightingEffect.PointLightSlot);
        var slot2 = default(LightingEffect.PointLightSlot);
        if (bestSun != null)
        {
            int slotIndex = 0;
            for (int i = 0; i < lights.Count && slotIndex < 3; ++i)
            {
                if (lights[i] is SunBurnLights.PointLight q && q.Enabled
                    && q.Radius >= 1000f && q.Radius < 1_000_000f
                    && q.Position.X == bestSun.Position.X
                    && q.Position.Y == bestSun.Position.Y)
                {
                    var slot = new LightingEffect.PointLightSlot
                    {
                        Enabled = true,
                        Position = q.Position,
                        DiffuseColor = q.DiffuseColor * q.Intensity,
                        SpecularColor = q.DiffuseColor * 0.15f,
                        Radius = q.Radius,
                    };
                    if      (slotIndex == 0) slot0 = slot;
                    else if (slotIndex == 1) slot1 = slot;
                    else                     slot2 = slot;
                    slotIndex++;
                }
            }
        }
        fx.PointLight0 = slot0;
        fx.PointLight1 = slot1;
        fx.PointLight2 = slot2;

        // LightingEnabled gates the per-pixel lighting math entirely. Enable
        // when at least one directional, point light, or non-trivial ambient is set.
        fx.LightingEnabled = dirIndex > 0 || bestSun != null || ambient.LengthSquared() > 0.0001f;
        fx.AmbientLightColor = ambient;

        // Fog
        if (env != null && env.FogEnabled)
        {
            fx.FogEnabled = true;
            fx.FogColor = env.FogColor;
            fx.FogStart = env.FogStart;
            fx.FogEnd = env.FogEnd;
        }
        else
        {
            fx.FogEnabled = false;
        }

        // Phase 3.8.B: shadow uniforms. The depth pre-pass in
        // SceneInterface.RenderScene runs ahead of the binder and feeds
        // its output here; we forward to SharedFx so CopySharedLighting
        // propagates the same shadow texture / light-clip matrix to
        // per-mesh LightingEffects (planet halos, ship Materials).
        fx.ShadowMap           = shadowMap;
        fx.ShadowMapEnabled    = shadowMap != null;
        fx.LightViewProjection = lightViewProjection;

        // Push the BaseMaterialEffect-shadowed material props down to the BasicEffect.
        // (The renderer may have set DiffuseColor / Texture / etc. via the new properties.)
        fx.ApplyToBasicEffect();
    }
}

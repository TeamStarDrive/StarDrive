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
                            // §4.6 #6: SunBurn pre-migration showed clear bright specular
                            // streaks on hull edges; the prior 0.15× damping (Phase 3.7
                            // anti-blow-out) suppressed them entirely, leaving hulls
                            // matte and ambient-dominated (universe nebula green tint
                            // bled through). 1.0 matches SunBurn's full specular path —
                            // SpecularPower (16-64) keeps the highlight tight enough that
                            // SunIntensity 2-4 doesn't actually blow out the hull face;
                            // it only brightens the small N·H peak.
                            slot.SpecularColor = d.DiffuseColor * 1.0f;
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
                        // §4.6.B follow-up: sun PointLight specular restored
                        // (was zero on the GPU under §4.6 #2 packing). 1.0×
                        // matches the directional path (LightingEffectBinder
                        // pushes `d.DiffuseColor * 1.0` for DirLight specular)
                        // and recovers the SunBurn-style highlight streaks on
                        // hulls in universe view, where there's no DirLight.
                        SpecularColor = q.DiffuseColor * 1.0f,
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

        // 8 dynamic transient point-light slots (projectile <Light> color,
        // explosion flashes, shield impacts). Pre-migration SunBurn ran these
        // as deferred light volumes — every dynamic light contributed
        // unconditionally. The migrated forward path runs 8 dedicated shader
        // slots filled from the small-radius bucket (Radius < 1000f) sorted
        // by XY distance to the camera. Off-screen and far-from-camera
        // lights drop out (they wouldn't be visible anyway). XY-only because
        // Z varies hugely with universe zoom; the play plane is at z=0 and
        // projectiles hover near z=-25, so XY distance maps cleanly to
        // "what the player is looking at".
        //
        // §4.6.B follow-up: expanded from 2 slots → 8 (the FL10.0 register
        // pool removed the §4.6 #2 cap), and projectile glow now contributes
        // specular too. Insertion-sort over a fixed-size 8 slot buffer.
        const int DynamicSlotCount = 8;
        var dynSlots = new LightingEffect.PointLightSlot[DynamicSlotCount];
        var dynDist2 = new float[DynamicSlotCount];
        for (int s = 0; s < DynamicSlotCount; ++s) dynDist2[s] = float.MaxValue;
        if (lights != null)
        {
            for (int i = 0; i < lights.Count; ++i)
            {
                if (lights[i] is SunBurnLights.PointLight q && q.Enabled
                    && q.Radius > 0f && q.Radius < 1000f)
                {
                    float dx = q.Position.X - cameraPos.X;
                    float dy = q.Position.Y - cameraPos.Y;
                    float dist2 = dx*dx + dy*dy;

                    // Where in the sorted buffer does this light belong? -1
                    // means it's farther than every retained slot — drop it.
                    int insertAt = -1;
                    for (int s = 0; s < DynamicSlotCount; ++s)
                    {
                        if (dist2 < dynDist2[s]) { insertAt = s; break; }
                    }
                    if (insertAt < 0) continue;

                    // Shift the tail down to make room. The slot at index 7
                    // falls off the end (correctly — it's the worst remaining
                    // candidate among the held set).
                    for (int s = DynamicSlotCount - 1; s > insertAt; --s)
                    {
                        dynSlots[s] = dynSlots[s - 1];
                        dynDist2[s] = dynDist2[s - 1];
                    }
                    dynSlots[insertAt] = new LightingEffect.PointLightSlot
                    {
                        Enabled = true,
                        Position = q.Position,
                        DiffuseColor = q.DiffuseColor * q.Intensity,
                        // 0.6× — projectile bolts already carry a bright
                        // sprite + bloom; the spec adds a tinted glint on
                        // nearby hulls without dominating the highlight read.
                        SpecularColor = q.DiffuseColor * 0.6f,
                        Radius = q.Radius,
                    };
                    dynDist2[insertAt] = dist2;
                }
            }
        }
        fx.DynamicLight0 = dynSlots[0];
        fx.DynamicLight1 = dynSlots[1];
        fx.DynamicLight2 = dynSlots[2];
        fx.DynamicLight3 = dynSlots[3];
        fx.DynamicLight4 = dynSlots[4];
        fx.DynamicLight5 = dynSlots[5];
        fx.DynamicLight6 = dynSlots[6];
        fx.DynamicLight7 = dynSlots[7];

        // §4.6 #12 (post-shadow-disable): minimum ambient floor so hulls
        // don't go fully black when no submitted light reaches them.
        // Pre-migration SunBurn's deferred composite carried a baseline
        // lift (default ambient + tone-curve) that kept unlit surfaces
        // showing faint diffuse texture — the migrated forward path
        // attenuates to zero, leaving only emissive/glow-map texels
        // visible against pure black hull. Component-wise max preserves
        // any stronger authored ambient (MainMenu's violet, etc.) and
        // only lifts channels that fall below the floor. User-tuned to
        // 0.20 against pre-migration footage; raise for more lift in
        // shadow-side hull, lower if the floor visibly washes out the
        // dark texture detail.
        const float MinAmbient = 0.2f;
        ambient = Vector3.Max(ambient, new Vector3(MinAmbient, MinAmbient, MinAmbient));

        // LightingEnabled gates the per-pixel lighting math entirely. Enable
        // when at least one directional, point light, or non-trivial ambient
        // is set. Any of the 8 dynamic slots being filled keeps lighting on.
        // The MinAmbient floor above guarantees ambient is always non-trivial,
        // so this check now mostly exists to skip the shader path on scenes
        // that explicitly don't want lighting at all (none currently).
        bool anyDyn = false;
        for (int s = 0; s < DynamicSlotCount; ++s) anyDyn |= dynSlots[s].Enabled;
        fx.LightingEnabled = dirIndex > 0 || bestSun != null
                          || ambient.LengthSquared() > 0.0001f || anyDyn;
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

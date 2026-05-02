using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
    static bool s_warnedPointLight;
    static bool s_warnedExtraDirectional;

    public static void Apply(LightingEffect fx,
                             IReadOnlyList<SunBurnLights.ILight> lights,
                             SceneEnvironment env)
    {
        if (fx == null) return;

        // BasicEffect ships 3 directional-light slots; collect up to 3 directionals
        // and an aggregated ambient. PointLights are unsupported by BasicEffect and
        // logged-once.
        Vector3 ambient = env?.AmbientLightColor ?? new Vector3(0.2f, 0.2f, 0.2f);
        XnaDirectionalLight[] slots = { fx.DirectionalLight0, fx.DirectionalLight1, fx.DirectionalLight2 };
        int dirIndex = 0;

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
                            slot.SpecularColor = d.DiffuseColor * d.Intensity * 0.5f;
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

                    case SunBurnLights.PointLight p when p.Enabled:
                        if (!s_warnedPointLight)
                        {
                            s_warnedPointLight = true;
                            Log.Warning("LightingEffectBinder: PointLight submitted but BasicEffect has no point-light support; ignored.");
                        }
                        break;
                }
            }
        }

        // Disable any remaining unfilled slots.
        for (int i = dirIndex; i < slots.Length; ++i)
            slots[i].Enabled = false;

        // LightingEnabled gates BasicEffect's per-pixel lighting math entirely.
        // Enable only when at least one directional or non-trivial ambient is set.
        fx.LightingEnabled = dirIndex > 0 || ambient.LengthSquared() > 0.0001f;
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

        // Push the BaseMaterialEffect-shadowed material props down to the BasicEffect.
        // (The renderer may have set DiffuseColor / Texture / etc. via the new properties.)
        fx.ApplyToBasicEffect();
    }
}

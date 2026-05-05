using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SDGraphics;
using Ship_Game.Data.Mesh;
using Ship_Game.Ships;
using Matrix = SDGraphics.Matrix;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Ship_Game;

public sealed class ShieldManager : IDisposable
{
    readonly UniverseScreen Universe;
    public Shield[] VisibleShields = Empty<Shield>.Array;
    Shield[] VisiblePlanetShields = Empty<Shield>.Array;

    // these resources are managed by GameContentManager
    #pragma warning disable CA2213
    StaticMesh ShieldModel;
    Texture2D ShieldTexture;
    Texture2D GradientTexture;
    Effect ShieldEffect;
    #pragma warning restore CA2213
    EffectParameter World, Scale, Displacement;

    public bool IsDisposed { get; private set; }

    public ShieldManager(UniverseScreen u)
    {
        Universe = u;
        LoadContent();
    }

    ~ShieldManager() { Destroy(); }

    void Destroy()
    {
        VisibleShields = null;
        VisiblePlanetShields = null;
        UnloadContent();
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Destroy();
        GC.SuppressFinalize(this);
    }

    void LoadContent()
    {
        GameLoadingScreen.SetStatus("LoadShields");

        // always use the root content manager for the shield manager
        // because this reduces issues with content reloading
        var content = ResourceManager.RootContent;
        ShieldModel = content.LoadStaticMesh("Model/Projectiles/shield");
        ShieldTexture = content.Load<Texture2D>("Model/Projectiles/shield_d.dds");
        GradientTexture = content.Load<Texture2D>("Model/Projectiles/shieldgradient.png");

        ShieldEffect = content.Load<Effect>("Effects/scale");
        if (ShieldEffect == null) return; // defense-in-depth: scale.mgfxo restored §3.3 (2026-05-04), guard catches missing-file regressions

        ShieldEffect.CurrentTechnique = ShieldEffect.Techniques["Technique1"];
        ShieldEffect.Parameters["tex"].SetValue(ShieldTexture);
        ShieldEffect.Parameters["AlphaMap"].SetValue(GradientTexture);

        World = ShieldEffect.Parameters["World"];
        Scale = ShieldEffect.Parameters["scale"];
        Displacement = ShieldEffect.Parameters["displacement"];
    }

    void UnloadContent()
    {
        var content = ResourceManager.RootContent;
        content.Dispose(ref ShieldTexture);
        content.Dispose(ref GradientTexture);
        content.Dispose(ref ShieldEffect);
        content.Dispose(ref ShieldModel);
        World = Scale = Displacement = null;
    }

    public void SetVisibleShields(Shield[] visibleShields)
    {
        VisibleShields = visibleShields;
    }
    public void SetVisiblePlanetShields(Shield[] visibleShields)
    {
        VisiblePlanetShields = visibleShields;
    }

    public void RemoveShieldLights(IEnumerable<ShipModule> shields)
    {
        foreach (ShipModule shield in shields)
            shield.Shield.RemoveLight(Universe);
    }

    public void Update(FixedSimTime timeStep)
    {
        if (IsDisposed)
            return;

        Shield[] shields = VisibleShields;
        Shield[] planetShields = VisiblePlanetShields;

        for (int i = 0; i < planetShields.Length; i++)
        {
            Shield shield = planetShields[i];
            if (shield.LightEnabled)
            {
                shield.UpdateLightIntensity(-2.45f);
                shield.UpdateDisplacement(0.085f);
                shield.UpdateTexScale(-0.185f);
            }
            shield.TickDistortionTimer(timeStep.FixedTime);
        }

        for (int i = 0; i < shields.Length; i++)
        {
            Shield shield = shields[i];
            if (shield.LightEnabled)
            {
                shield.UpdateLightIntensity(-0.002f);
                shield.UpdateDisplacement(0.04f);
                shield.UpdateTexScale(-0.01f);
            }
            shield.TickDistortionTimer(timeStep.FixedTime);
        }
    }

    public void Draw(in Matrix view, in Matrix projection)
    {
        if (IsDisposed)
            return;

        if (ShieldEffect == null) return; // defense-in-depth: scale.mgfxo restored §3.3 (2026-05-04), guard catches missing-file regressions

        if (ShieldEffect.IsDisposed || ShieldTexture.IsDisposed)
        {
            UnloadContent();
            LoadContent();
        }

        ShieldEffect.Parameters["View"].SetValue(view);
        ShieldEffect.Parameters["Projection"].SetValue(projection);

        UniverseScreen u = Universe;
        Shield[] shields = VisibleShields;
        Shield[] planetShields = VisiblePlanetShields;

        for (int i = 0; i < shields.Length; i++)
        {
            Shield shield = shields[i];
            if (shield.LightEnabled && shield.InFrustum(u))
                DrawShield(shield);
        }
        for (int i = 0; i < planetShields.Length; i++)
        {
            Shield shield = planetShields[i];
            if (shield.LightEnabled && shield.InFrustum(u))
                DrawShield(shield);
        }
    }

    // Phase 3.7 step 2 distortion driver. Walks ship + planet shields,
    // pulls active hit data from each, projects center+radius into the
    // destination's UV space, and appends to `output`. Up to MaxShields
    // entries; further hits are dropped silently (the visible shield bubble
    // from DrawShield still draws — we only lose the screen-space ripple
    // for the overflow). The destination is normally the post-bloom RT;
    // its width/height drive the UV projection.
    public void BuildDistortionSources(int destWidth, int destHeight,
                                       List<DistortionComponent.DistortionSource> output)
    {
        if (IsDisposed) return;

        UniverseScreen u = Universe;
        Shield[] shields = VisibleShields;
        Shield[] planetShields = VisiblePlanetShields;

        AppendActive(shields, u, destWidth, destHeight, output);
        if (output.Count >= DistortionComponent.MaxShields) return;
        AppendActive(planetShields, u, destWidth, destHeight, output);
    }

    static void AppendActive(Shield[] arr, UniverseScreen u, int destW, int destH,
                             List<DistortionComponent.DistortionSource> output)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (output.Count >= DistortionComponent.MaxShields) return;

            Shield shield = arr[i];
            if (!shield.TryGetDistortionSource(out Vector3 worldCenter, out float worldRadius, out float intensity))
                continue;
            // Frustum cull — off-screen shields would land at UV outside [0,1]
            // anyway, but skipping them up front keeps the active-slot budget
            // clean for the on-screen ones. IsInFrustum takes SDGraphics.Vector2
            // (the SDGraphics.Vector3 constructor extracts X/Y from worldCenter).
            if (!u.IsInFrustum(worldCenter, worldRadius))
                continue;

            // Project center to screen, project a second point at center+radius
            // along world-X to derive screen-space radius (matches the
            // ProjectToScreenCoords sizing convention used elsewhere).
            Vector2d centerPx = u.ProjectToScreenPosition(worldCenter);
            Vector2d edgePx   = u.ProjectToScreenPosition(new Vector3(worldCenter.X + worldRadius, worldCenter.Y, worldCenter.Z));
            double radiusPx = edgePx.Distance(centerPx);
            if (radiusPx < 4.0) continue; // sub-pixel ripples are invisible

            float invW = 1f / destW;
            float invH = 1f / destH;
            output.Add(new DistortionComponent.DistortionSource
            {
                CenterUV  = new Vector2((float)(centerPx.X * invW), (float)(centerPx.Y * invH)),
                RadiusUV  = (float)(radiusPx * invW), // assume square pixels; UV radius in X
                Intensity = intensity,
            });
        }
    }

    void DrawShield(Shield shield)
    {
        shield.UpdateWorldTransform();

        // scale.fx declares `float2 scale` and `float2 displacement`. MonoGame's
        // EffectParameter.SetValue(float) on a float2 parameter only writes the .x
        // component, leaving .y at 0. shieldgradient.png is a vertical bell curve —
        // row y=0 is pure black — so sampling at (d, 0) returns alphaMask=0 and the
        // shield bubble renders fully transparent. Pass an explicit Vector2(d, d) so
        // both components carry the value, matching the original D3DX scalar-broadcast.
        World.SetValue(shield.World);
        Scale.SetValue(new Vector2(shield.TexScale, shield.TexScale));
        Displacement.SetValue(new Vector2(shield.Displacement, shield.Displacement));

        ShieldModel.Draw(ShieldEffect);
    }
}

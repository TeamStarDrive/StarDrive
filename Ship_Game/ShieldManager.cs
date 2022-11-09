using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Ships;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game;

public sealed class ShieldManager : IDisposable
{
    readonly UniverseScreen Universe;
    Shield[] VisibleShields = Empty<Shield>.Array;
    Shield[] VisiblePlanetShields = Empty<Shield>.Array;
    Model ShieldModel;
    Texture2D ShieldTexture;
    Texture2D GradientTexture;
    Effect ShieldEffect;

    public bool IsDisposed { get; private set; }

    public ShieldManager(UniverseScreen u, GameContentManager content)
    {
        Universe = u;

        GameLoadingScreen.SetStatus("LoadShields");
        ShieldModel = content.Load<Model>("Model/Projectiles/shield");
        ShieldTexture = content.Load<Texture2D>("Model/Projectiles/shield_d.dds");
        GradientTexture = content.Load<Texture2D>("Model/Projectiles/shieldgradient");
        ShieldEffect = content.Load<Effect>("Effects/scale");
    }

    ~ShieldManager() { Destroy(); }

    void Destroy()
    {
        VisibleShields = null;
        VisiblePlanetShields = null;
        ShieldModel = null;
        ShieldTexture?.Dispose(ref ShieldTexture);
        GradientTexture?.Dispose(ref GradientTexture);
        ShieldEffect?.Dispose(ref ShieldEffect);
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Destroy();
        GC.SuppressFinalize(this);
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
        }
    }

    public void Draw(in Matrix view, in Matrix projection)
    {
        if (IsDisposed)
            return;

        UniverseScreen u = Universe;
        Shield[] shields = VisibleShields;
        Shield[] planetShields = VisiblePlanetShields;

        for (int i = 0; i < shields.Length; i++)
        {
            Shield shield = shields[i];
            if (shield.LightEnabled && shield.InFrustum(u))
                DrawShield(shield, view, projection);
        }
        for (int i = 0; i < planetShields.Length; i++)
        {
            Shield shield = planetShields[i];
            if (shield.LightEnabled && shield.InFrustum(u))
                DrawShield(shield, view, projection);
        }
    }

    void DrawShield(Shield shield, in Matrix view, in Matrix projection)
    {
        shield.UpdateWorldTransform();
        ShieldEffect.Parameters["World"].SetValue(shield.World);
        ShieldEffect.Parameters["View"].SetValue(view);
        ShieldEffect.Parameters["Projection"].SetValue(projection);
        ShieldEffect.Parameters["tex"].SetValue(ShieldTexture);
        ShieldEffect.Parameters["AlphaMap"].SetValue(GradientTexture);
        ShieldEffect.Parameters["scale"].SetValue(shield.TexScale);
        ShieldEffect.Parameters["displacement"].SetValue(shield.Displacement);
        ShieldEffect.CurrentTechnique = ShieldEffect.Techniques["Technique1"];

        foreach (ModelMesh mesh in ShieldModel.Meshes)
        {
            foreach (ModelMeshPart part in mesh.MeshParts)
                part.Effect = ShieldEffect;
            mesh.Draw();
        }
    }
}

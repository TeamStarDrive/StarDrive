using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
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
        ShieldModel = Universe.TransientContent.Load<Model>("Model/Projectiles/shield");
        ShieldTexture = Universe.TransientContent.Load<Texture2D>("Model/Projectiles/shield_d.dds");
        GradientTexture = Universe.TransientContent.Load<Texture2D>("Model/Projectiles/shieldgradient");

        ShieldEffect = Universe.TransientContent.Load<Effect>("Effects/scale");
        ShieldEffect.Parameters["tex"].SetValue(ShieldTexture);
        ShieldEffect.Parameters["AlphaMap"].SetValue(GradientTexture);
        ShieldEffect.CurrentTechnique = ShieldEffect.Techniques["Technique1"];

        World = ShieldEffect.Parameters["World"];
        Scale = ShieldEffect.Parameters["scale"];
        Displacement = ShieldEffect.Parameters["displacement"];
    }
    
    void UnloadContent()
    {
        ShieldModel = null;
        ShieldTexture?.Dispose(ref ShieldTexture);
        GradientTexture?.Dispose(ref GradientTexture);
        ShieldEffect?.Dispose(ref ShieldEffect);
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

        if (ShieldEffect.IsDisposed)
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

    void DrawShield(Shield shield)
    {
        shield.UpdateWorldTransform();

        World.SetValue(shield.World);
        Scale.SetValue(shield.TexScale);
        Displacement.SetValue(shield.Displacement);

        foreach (ModelMesh mesh in ShieldModel.Meshes)
        {
            foreach (ModelMeshPart part in mesh.MeshParts)
                part.Effect = ShieldEffect;
            mesh.Draw();
        }
    }
}

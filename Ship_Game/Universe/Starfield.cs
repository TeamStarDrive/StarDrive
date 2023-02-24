using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Rendering;
using SDGraphics.Sprites;
using Ship_Game.Graphics;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using System;

namespace Ship_Game;

public sealed class StarField
{
    struct Star
    {
        public Quad3D Quad;
        public SubTexture Tex;
    }

    const float StarScale = 600;
    const int NumStars = 150;

    readonly Star[] Stars;
    readonly SubTexture[] StarTex;

    public StarField(UniverseState uState)
    {
        StarTex = SunType.GetLoResTextures();
        Stars = new Star[NumStars];

        Create(uState.UniverseRadius, uState.BackgroundSeed);
    }

    public void Draw(SpriteRenderer sr, GameScreen screen)
    {
        sr.Begin(screen.ViewProjection);
        RenderStates.BasicBlendMode(screen.Device, additive: true, depthWrite: false);

        foreach (ref Star star in Stars.AsSpan())
        {
            sr.Draw(star.Tex, star.Quad, Color.White);
        }

        sr.End();
    }

    public void Create(float uRadius, int seed)
    {
        SeededRandom random = new(seed);

        int numFancyStars = 0;
        int numLargeStars = 0;
        int numMedStars = 0;

        int desiredFancyStars = random.Int(20, 40); // 64x64 stars
        int desiredLargeStars = random.Int(10, 20); // detailed 48x48 px stars
        int desiredMedStars = random.Int(30, 50); // detailed 32x32 px stars
        // if we run out of budgets, tiny 16x16 px stars will be used

        float baseScale = 1f;

        foreach (ref Star star in Stars.AsSpan())
        {
            Vector2 pos2d = random.Vector2D(uRadius * 1.5f);
            float zPos = random.Float(1_000_000.0f, 8_000_000.0f);
            Vector3 pos3d = new(pos2d, zPos);

            if (numFancyStars < desiredFancyStars)
            {
                ++numFancyStars;
                star.Tex = random.Item(StarTex);
                baseScale = 0.75f;
            }
            else if (numLargeStars < desiredLargeStars)
            {
                // detailed 48x48 px stars
                ++numLargeStars;
                star.Tex = ResourceManager.LargeStars.RandomTexture(random);
            }
            else if (numMedStars < desiredMedStars)
            {
                // somewhat detailed 32x32 px stars
                ++numMedStars;
                star.Tex = ResourceManager.LargeStars.RandomTexture(random);
                baseScale = 1.5f;
            }
            else
            {
                // tiny 16x16 px stars
                star.Tex = ResourceManager.SmallStars.RandomTexture(random);
                baseScale = 2f;
            }
            
            // use the star's actual pixel size as the base
            Vector2 pixelSize = star.Tex.SizeF;
            if (pixelSize.Y > 64) // force bigger textures to a smaller target size
                pixelSize /= (pixelSize.Y / 64);

            star.Quad = new Quad3D(pos3d, pixelSize * baseScale * StarScale);
        }
    }
}

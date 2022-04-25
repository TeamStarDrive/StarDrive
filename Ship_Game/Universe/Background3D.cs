using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using Ship_Game.Utils;

namespace Ship_Game
{
    public sealed class Background3D : IDisposable
    {
        readonly UniverseScreen Screen;
        readonly Array<BackgroundItem> BGItems = new Array<BackgroundItem>();
        readonly SeededRandom Random;

        public Background3D(UniverseScreen screen)
        {
            Random = new SeededRandom(screen.UState.BackgroundSeed);
            Screen = screen;
            float universeSize = screen.UState.Size;

            float size = Random.AvgFloat(1_000_000 + universeSize / 4f, universeSize);
            Vector2 largeNebPos = Random.Vector2D(-universeSize * 1.5f, universeSize * 2 - size);

            CreateRandomLargeNebula(new RectF(largeNebPos, size, size));

            for (int i = 0; i < 4 + (int) (universeSize / 4_000_000); i++)
            {
                Vector2 nebTopLeft = Random.Vector2D(-universeSize * 1.5f, universeSize * 0.75f);
                CreateSmallNeb(universeSize, nebTopLeft, zPos: Random.Float(500_000f, 5_000_000f));
                CreateSmallNeb(universeSize, nebTopLeft, zPos: Random.Float(300_000f, 500_000f));
                CreateSmallNeb(universeSize, nebTopLeft, zPos: Random.Float(300_000f, 500_000f));
            }

            CreateForegroundStars(universeSize);
        }

        ~Background3D() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        void Destroy()
        {
            for (int i = 0; i < BGItems.Count; ++i)
                BGItems[i].Dispose();
            BGItems.Clear();
        }

        BackgroundItem CreateBGItem(in RectF r, float zDepth, SubTexture nebTexture)
        {
            var neb = new BackgroundItem(nebTexture);
            neb.UpperLeft  = new Vector3(r.X, r.Y, zDepth);
            neb.LowerLeft  = new Vector3(r.X, r.Bottom, zDepth);
            neb.UpperRight = new Vector3(r.Right, r.Y, zDepth);
            neb.LowerRight = new Vector3(r.Right, r.Bottom, zDepth);
            neb.FillVertices();
            neb.LoadContent(Screen.ScreenManager);
            return neb;
        }

        // these are the stars which float on top of simulated universe
        // the negative Z means out of the screen
        void CreateForegroundStars(float universeSize)
        {
            int numStars = (int)(universeSize / 5000.0f);
            for (int i = 0; i < numStars; ++i)
            {
                var position = new Vector3(
                    Random.Float(-1.5f * universeSize, 1.5f * universeSize),
                    Random.Float(-1.5f * universeSize, 1.5f * universeSize),
                    Random.Float(-200_000f, -15_000_000f));
                Screen.Particles.StarParticles.AddParticle(position);
            }
        }

        void CreateRandomLargeNebula(in RectF r)
        {
            const float zStart = 1_500_000f;
            float zPos = 1_500_000f;

            float CreateNebulaPart(in RectF nebR, float nebZ, SubTexture nebTexture,
                                   float minZ, float maxZ, bool starParts = false)
            {
                nebZ += Random.AvgFloat(minZ, maxZ);
                BGItems.Add(CreateBGItem(nebR, nebZ, nebTexture));

                // add a star particle inside of the nebula part
                if (starParts)
                {
                    var randomPos = new Vector3(
                        Random.Float(nebR.X, nebR.Right),
                        Random.Float(nebR.Y, nebR.Bottom),
                        nebZ);
                    Screen.Particles.StarParticles.AddParticle(randomPos);
                }
                return nebZ;
            }

            zPos = CreateNebulaPart(r, zPos, ResourceManager.Texture("hqspace/neb_pointy"), 0, 0);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.BigNebula(2), -200_000, -600_000);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.Texture("hqspace/neb_floaty"), -200_000, -600_000);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 0, 0);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 250_000, 800_000);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 250_000, 800_000);

            SubTexture smoke = ResourceManager.Texture("smoke");
            for (int i = 0; i < 50; i++)
            {
                float rw = Random.AvgFloat(r.W * 0.1f, r.W * 0.4f);
                var b = new RectF(
                    (r.X + Random.Float(r.W * 0.2f, r.W * 0.8f)),
                    (r.Y + Random.Float(r.H * 0.2f, r.H * 0.8f)),
                    rw, rw);
                CreateNebulaPart(b, 0, smoke, zStart, zPos, starParts: true);
            }
        }

        void CreateSmallNeb(float universeSize, Vector2 nebTopLeft, float zPos)
        {
            var neb = ResourceManager.SmallNebulaRandom();
            float xSize = Random.Float(800_000f, universeSize * 0.75f);
            float ySize = (float)neb.Height / neb.Width * xSize;
            zPos += Random.Float(500_000f, 5_000_000f);
            BGItems.Add(CreateBGItem(new RectF(nebTopLeft, xSize, ySize), zPos, neb));
        }

        public void Draw(GraphicsDevice device)
        {
            RenderStates.BasicBlendMode(device, additive:true, depthWrite:false);

            double alpha = Screen.CamPos.Z / (Screen.GetZfromScreenState(UniverseScreen.UnivScreenState.SectorView) * 2);
            float a = (float)alpha.Clamped(0.1, 0.3);

            for (int i = 0; i < BGItems.Count; i++)
            {
                BGItems[i].Draw(device, Screen.View, Screen.Projection, a);
            }
        }
    }
}
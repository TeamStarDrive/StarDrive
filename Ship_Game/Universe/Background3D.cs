using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public sealed class Background3D : IDisposable
    {
        readonly UniverseScreen Screen;
        readonly Array<BackgroundItem> BGItems = new Array<BackgroundItem>();

        public Background3D(UniverseScreen screen)
        {
            Screen = screen;
            float universeSize = screen.UState.Size;

            int size = (int) RandomMath.AvgRandomBetween(1_000_000 + universeSize / 4f, universeSize);
            
            CreateRandomLargeNebula(new Rectangle(
                (int) RandomMath.AvgRandomBetween(-universeSize * 1.5f, universeSize * 2 - size),
                (int) RandomMath.AvgRandomBetween(-universeSize * 1.5f, universeSize * 2 - size),
                size, size));

            for (int i = 0; i < 4 + (int) (universeSize / 4_000_000); i++)
            {
                CreateRandomSmallObject(universeSize);
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

        static void CreateBGItem(Rectangle r, float zDepth, BackgroundItem neb)
        {
            neb.UpperLeft  = new Vector3(r.X, r.Y, zDepth);
            neb.LowerLeft  = neb.UpperLeft + new Vector3(0f, r.Height, 0f);
            neb.UpperRight = neb.UpperLeft + new Vector3(r.Width, 0f, 0f);
            neb.LowerRight = neb.UpperLeft + new Vector3(r.Width, r.Height, 0f);
            neb.FillVertices();
        }

        // these are the stars which float on top of simulated universe
        // the negative Z means out of the screen
        void CreateForegroundStars(float universeSize)
        {
            int numStars = (int)(universeSize / 5000.0f);
            for (int i = 0; i < numStars; ++i)
            {
                var position = new Vector3(
                    RandomMath.RandomBetween(-1.5f * universeSize, 1.5f * universeSize),
                    RandomMath.RandomBetween(-1.5f * universeSize, 1.5f * universeSize),
                    RandomMath.RandomBetween(-200_000f, -15_000_000f));
                Screen.Particles.StarParticles.AddParticle(position);
            }
        }

        void CreateRandomLargeNebula(Rectangle r)
        {
            const float startz = 1_500_000f;
            float zPos = 1_500_000f;

            float CreateNebulaPart(Rectangle nebrect, float nebZ, SubTexture nebTexure, float minZ, float maxZ, bool starParts = false)
            {
                nebZ += RandomMath.AvgRandomBetween(minZ, maxZ); ;
                var neb = new BackgroundItem(nebTexure);
                CreateBGItem(nebrect, nebZ, neb);
                neb.LoadContent(Screen.ScreenManager);
                BGItems.Add(neb);

                // add star particles inside of the nebulae
                if (starParts)
                    Screen.Particles.StarParticles.AddParticle(new Vector3(nebrect.X, nebrect.Y, nebZ));
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
                float rw = RandomMath.AvgRandomBetween(r.Width * 0.1f, r.Width * 0.4f);
                var b = new Rectangle(
                    (int)(r.X + RandomMath.RandomBetween(r.Width * 0.2f, r.Width * 0.8f)),
                    (int)(r.Y + RandomMath.RandomBetween(r.Height * 0.2f, r.Height * 0.8f)),
                    (int)rw,
                    (int)rw);
                CreateNebulaPart(b, 0, smoke, startz, zPos, starParts: true);
            }
        }

        void CreateRandomSmallObject(float universeSize)
        {
            var neb1 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            var nebUpperLeft = new Vector2(
                RandomMath.RandomBetween(-universeSize * 1.5f, universeSize * 0.75f),
                RandomMath.RandomBetween(-universeSize * 1.5f, universeSize * 0.75f));
            float zPos = RandomMath.RandomBetween(500_000f, 5_000_000f);
            float xSize = RandomMath.RandomBetween(800_000f, universeSize * 0.75f);
            float ySize = (float)neb1.Texture.Height / neb1.Texture.Width * xSize;
            neb1.UpperLeft = new Vector3(nebUpperLeft, zPos);
            neb1.LowerLeft = neb1.UpperLeft + new Vector3(0f, ySize, 0f);
            neb1.UpperRight = neb1.UpperLeft + new Vector3(xSize, 0f, 0f);
            neb1.LowerRight = neb1.UpperLeft + new Vector3(xSize, ySize, 0f);
            neb1.FillVertices();
            neb1.LoadContent(Screen.ScreenManager);
            BGItems.Add(neb1);

            var neb2 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            zPos += RandomMath.RandomBetween(300_000f, 500_000f);
            xSize = RandomMath.RandomBetween(800_000f, universeSize * 0.75f);
            ySize = (float)neb2.Texture.Height / neb2.Texture.Width * xSize;
            neb2.UpperLeft = new Vector3(nebUpperLeft, zPos);
            neb2.LowerLeft = neb2.UpperLeft + new Vector3(0f, ySize, 0f);
            neb2.UpperRight = neb2.UpperLeft + new Vector3(xSize, 0f, 0f);
            neb2.LowerRight = neb2.UpperLeft + new Vector3(xSize, ySize, 0f);
            neb2.FillVertices();
            neb2.LoadContent(Screen.ScreenManager);
            BGItems.Add(neb2);

            var neb3 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            zPos += RandomMath.RandomBetween(300_000f, 500_000f);
            xSize = RandomMath.RandomBetween(800_000f, universeSize * 0.75f);
            ySize = neb3.Texture.Height / (float)neb3.Texture.Width * xSize;
            neb3.UpperLeft = new Vector3(nebUpperLeft, zPos);
            neb3.LowerLeft = neb3.UpperLeft + new Vector3(0f, ySize, 0f);
            neb3.UpperRight = neb3.UpperLeft + new Vector3(xSize, 0f, 0f);
            neb3.LowerRight = neb3.UpperLeft + new Vector3(xSize, ySize, 0f);
            neb3.FillVertices();
            neb3.LoadContent(Screen.ScreenManager);
            BGItems.Add(neb3);
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
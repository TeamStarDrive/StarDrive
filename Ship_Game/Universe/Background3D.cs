using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Background3D : IDisposable
    {
        readonly UniverseScreen Screen;
        readonly Array<BackgroundItem> BGItems = new Array<BackgroundItem>();

        public Background3D(UniverseScreen screen)
        {
            Screen = screen;
            int size = (int) RandomMath.AvgRandomBetween(1000000+ screen.UniverseSize / 4f, screen.UniverseSize);
            
            CreateRandomLargeNebula(new Rectangle(
                (int) RandomMath.AvgRandomBetween(-screen.UniverseSize * 1.5f, screen.UniverseSize * 2 - size),
                (int) RandomMath.AvgRandomBetween(-screen.UniverseSize * 1.5f, screen.UniverseSize * 2 - size),
                size, size)); //Updated to take full advantage of the bigger maps -Gretman
            for (int i = 0; i < 4 + (int) (Screen.UniverseSize / 4000000); i++) //And add more nedulas and stuff
                CreateRandomSmallObject();
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

        private void CreateRandomLargeNebula(Rectangle r)
        {
            const float startz = 1500000f;
            float zPos = 1500000f;

            float CreateNebulaPart(Rectangle nebrect, float nebZ, SubTexture nebTexure, float minZ, float maxZ, bool starParts = false)
            {
                nebZ += RandomMath.AvgRandomBetween(minZ, maxZ); ;
                var neb = new BackgroundItem(nebTexure);
                CreateBGItem(nebrect, nebZ, neb);
                neb.LoadContent(Screen.ScreenManager, Screen.view, Screen.projection);
                BGItems.Add(neb);
                if (starParts)
                    Screen.star_particles.AddParticleThreadB(new Vector3(nebrect.X, nebrect.Y, nebZ), Vector3.Zero);
                return nebZ;
            }
            zPos = CreateNebulaPart(r, zPos, ResourceManager.Texture("hqspace/neb_pointy"), 0, 0);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.BigNebula(2), -200000, -600000);
            zPos = CreateNebulaPart(r, zPos, ResourceManager.Texture("hqspace/neb_floaty"), -200000, -600000);            
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 0, 0);            
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 250000, 800000);            
            zPos = CreateNebulaPart(r, zPos, ResourceManager.NebulaMedRandom(), 250000, 800000);

            SubTexture smoke = ResourceManager.Texture("smoke");
            for (int i = 0; i < 50; i++)
            {
                float rw = RandomMath.AvgRandomBetween(r.Width * 0.1f, r.Width * 0.4f);
                var b = new Rectangle(
                    (int)(r.X + RandomMath.RandomBetween(r.Width * 0.2f, r.Width * 0.8f)),
                    (int)(r.Y + RandomMath.RandomBetween(r.Height * 0.2f, r.Height * 0.8f)),
                    (int)rw,
                    (int)rw);
                CreateNebulaPart(b, 0, smoke, startz, zPos, true);
            }
        }

        private void CreateRandomSmallObject()
        {
            var BigNeb_1 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            var nebUpperLeft = new Vector2(
                RandomMath.RandomBetween(-Screen.UniverseSize * 1.5f, Screen.UniverseSize * 0.75f),
                RandomMath.RandomBetween(-Screen.UniverseSize * 1.5f,
                    Screen.UniverseSize * 0.75f)); //More Random Here -Gretman
            float zPos = RandomMath.RandomBetween(500000f, 5000000f);
            float xSize = RandomMath.RandomBetween(800000f, Screen.UniverseSize * 0.75f);
            float ySize = BigNeb_1.Texture.Height / BigNeb_1.Texture.Width * xSize;
            BigNeb_1.UpperLeft = new Vector3(nebUpperLeft, zPos);
            BigNeb_1.LowerLeft = BigNeb_1.UpperLeft + new Vector3(0f, ySize, 0f);
            BigNeb_1.UpperRight = BigNeb_1.UpperLeft + new Vector3(xSize, 0f, 0f);
            BigNeb_1.LowerRight = BigNeb_1.UpperLeft + new Vector3(xSize, ySize, 0f);
            BigNeb_1.FillVertices();
            BigNeb_1.LoadContent(Screen.ScreenManager, Screen.view, Screen.projection);
            BGItems.Add(BigNeb_1);

            var BigNeb_2 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            zPos = zPos + RandomMath.RandomBetween(300000f, 500000f);
            xSize = RandomMath.RandomBetween(800000f, Screen.UniverseSize * 0.75f);
            ySize = BigNeb_2.Texture.Height / BigNeb_2.Texture.Width * xSize;
            BigNeb_2.UpperLeft = new Vector3(nebUpperLeft, zPos);
            BigNeb_2.LowerLeft = BigNeb_2.UpperLeft + new Vector3(0f, ySize, 0f);
            BigNeb_2.UpperRight = BigNeb_2.UpperLeft + new Vector3(xSize, 0f, 0f);
            BigNeb_2.LowerRight = BigNeb_2.UpperLeft + new Vector3(xSize, ySize, 0f);
            BigNeb_2.FillVertices();
            BigNeb_2.LoadContent(Screen.ScreenManager, Screen.view, Screen.projection);
            BGItems.Add(BigNeb_2);

            var BigNeb_3 = new BackgroundItem(ResourceManager.SmallNebulaRandom());
            //zPos = zPos + 200000f;
            zPos = zPos + RandomMath.RandomBetween(300000f, 500000f);
            //xSize = RandomMath.RandomBetween(800000f, 1800000f);
            xSize = RandomMath.RandomBetween(800000f, Screen.UniverseSize * 0.75f);
            ySize = BigNeb_3.Texture.Height / (float)BigNeb_3.Texture.Width * xSize;
            BigNeb_3.UpperLeft = new Vector3(nebUpperLeft, zPos);
            BigNeb_3.LowerLeft = BigNeb_3.UpperLeft + new Vector3(0f, ySize, 0f);
            BigNeb_3.UpperRight = BigNeb_3.UpperLeft + new Vector3(xSize, 0f, 0f);
            BigNeb_3.LowerRight = BigNeb_3.UpperLeft + new Vector3(xSize, ySize, 0f);
            BigNeb_3.FillVertices();
            BigNeb_3.LoadContent(Screen.ScreenManager, Screen.view, Screen.projection);
            BGItems.Add(BigNeb_3);
        }

        public void Draw()
        {
            GraphicsDevice device = Screen.ScreenManager.GraphicsDevice;
            device.SamplerStates[0].AddressU          = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV          = TextureAddressMode.Wrap;
            device.RenderState.AlphaBlendEnable       = true;
            device.RenderState.AlphaBlendOperation    = BlendFunction.Add;
            device.RenderState.SourceBlend            = Blend.SourceAlpha;
            device.RenderState.DestinationBlend       = Blend.One;
            device.RenderState.DepthBufferWriteEnable = false;
            device.RenderState.CullMode               = CullMode.None;

            float alpha = Screen.CamHeight / (Screen.GetZfromScreenState(UniverseScreen.UnivScreenState.SectorView) * 2);
            alpha = alpha.Clamped(0.1f, 0.3f);

            for (int i = 0; i < BGItems.Count; i++)
            {
                BGItems[i].Draw(Screen.ScreenManager, Screen.view, Screen.projection, alpha);
            }

            device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
        }
    }
}
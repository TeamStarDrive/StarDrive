using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Background3D
    {
        private readonly UniverseScreen Screen;

        public Array<BackgroundItem> BGItems = new Array<BackgroundItem>();

        private readonly Array<BackgroundItem> NonAdditiveList = new Array<BackgroundItem>();

        public Background3D(UniverseScreen screen)
        {
            Screen = screen;
            int nebsize = (int) RandomMath.AvgRandomBetween(1000000+ screen.UniverseSize / 4f, screen.UniverseSize);
            
            CreateRandomLargeNebula(new Rectangle(
                (int) RandomMath.AvgRandomBetween(-screen.UniverseSize * 1.5f, screen.UniverseSize * 2 - nebsize),
                (int) RandomMath.AvgRandomBetween(-screen.UniverseSize * 1.5f, screen.UniverseSize * 2 - nebsize),
                nebsize, nebsize)); //Updated to take full advantage of the bigger maps -Gretman
            for (int i = 0; i < 4 + (int) (Screen.UniverseSize / 4000000); i++) //And add more nedulas and stuff
                CreateRandomSmallObject();
        }

        private void CreateBGItem(Rectangle r, float zdepth, BackgroundItem neb)
        {
            neb.UpperLeft = new Vector3(r.X, r.Y, zdepth);
            neb.LowerLeft = neb.UpperLeft + new Vector3(0f, r.Height, 0f);
            neb.UpperRight = neb.UpperLeft + new Vector3(r.Width, 0f, 0f);
            neb.LowerRight = neb.UpperLeft + new Vector3(r.Width, r.Height, 0f);
            neb.FillVertices();
        }

        private void CreateRandomLargeNebula(Rectangle r)
        {
            const float startz = 1500000f;
            float zPos = 1500000f;
            Rectangle r1 = r;

            int GetNebIndex()
            {
                return RandomMath.IntBetween(0, ResourceManager.MedNebulae.Count - 1);
            }

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
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.Texture("hqspace/neb_pointy"), 0, 0);
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.BigNebula(2), -200000, -600000);
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.Texture("hqspace/neb_floaty"), -200000, -600000);            
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.MedNebula(GetNebIndex()), 0, 0);            
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.MedNebula(GetNebIndex()), 250000, 800000);            
            zPos = CreateNebulaPart(r1, zPos, ResourceManager.MedNebula(GetNebIndex()), 250000, 800000);

            SubTexture smoke = ResourceManager.Texture("smoke");
            for (int i = 0; i < 50; i++)
            {
                float rw = RandomMath.AvgRandomBetween(r1.Width * 0.1f, r1.Width * 0.4f);
                Rectangle b = new Rectangle(
                    (int)(r1.X + RandomMath.RandomBetween(r1.Width * 0.2f, r1.Width * 0.8f)),
                    (int)(r1.Y + RandomMath.RandomBetween(r1.Height * 0.2f, r1.Height * 0.8f)),
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
            //float zPos = (float)RandomMath.RandomBetween(200000f, 2500000f);
            //float xSize = RandomMath.RandomBetween(800000f, 1800000f);
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
            //zPos = zPos + 200000f;
            zPos = zPos + RandomMath.RandomBetween(300000f, 500000f);
            //xSize = RandomMath.RandomBetween(800000f, 1800000f);
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
            ySize = BigNeb_3.Texture.Height / BigNeb_3.Texture.Width * xSize;
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
            Screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU          = TextureAddressMode.Wrap;
            Screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV          = TextureAddressMode.Wrap;
            Screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable       = true;
            Screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation    = BlendFunction.Add;
            Screen.ScreenManager.GraphicsDevice.RenderState.SourceBlend            = Blend.SourceAlpha;
            Screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend       = Blend.One;
            Screen.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            Screen.ScreenManager.GraphicsDevice.RenderState.CullMode               = CullMode.None;
            float alpha = Screen.CamHeight / (Screen.GetZfromScreenState(UniverseScreen.UnivScreenState.SectorView) * 2);
            for (int i = 0; i < BGItems.Count; i++)
            {
                BackgroundItem bgi = BGItems[i];

                if (alpha > 0.3f)
                    alpha = 0.3f;
                if (alpha < 0.1f)
                    alpha = 0.1f;
                bgi.Draw(Screen.ScreenManager, Screen.view, Screen.projection, alpha);
            }
            Screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            for (int i = 0; i < NonAdditiveList.Count; i++)
            {
                BackgroundItem bgi = NonAdditiveList[i];
                bgi.Draw(Screen.ScreenManager, Screen.view, Screen.projection, 1f);
            }
        }
    }
}
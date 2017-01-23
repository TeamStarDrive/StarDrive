// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ns0;
using ns11;
using ns6;
using System;
using System.Diagnostics;
using System.IO;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Provides and manages lighting system specific resources
    /// such as lighting textures, effects, and helper models.
    /// At least one instance must be created before interacting
    /// with the lighting system.
    /// </summary>
    public class LightingSystemManager
    {

        private const int int_0 = 32;

        private static LightingSystemManager lightingSystemManager_0;

        private Texture3D texture3D_0;

        private Texture2D texture2D_0;

        private Texture2D texture2D_1;

        private TextureCube textureCube_0;

        private TextureCube textureCube_1;

        private SpriteFont spriteFont_0;

        private SpriteBatch spriteBatch_0;

        private VertexDeclaration vertexDeclaration_0;

        private GraphicsDeviceSupport graphicsDeviceSupport_0;

        private IServiceProvider iserviceProvider_0;

        private LightingSystemManager.Class15 class15_0;



        internal static LightingSystemManager Instance
        {
            get
            {
                if (LightingSystemManager.lightingSystemManager_0 == null)
                    throw new ArgumentException("LightingSystemManager unavailable, please create an instance of the manager before using this object.");
                Class0.CheckProductActivation1();
                return LightingSystemManager.lightingSystemManager_0;
            }
        }

        /// <summary>Returns the edition of the loaded SunBurn assembly.</summary>


        public static string Edition => "BlackBox";

        /// <summary>
        /// Returns the public key token of the loaded SunBurn assembly.
        /// </summary>


        public static string PublicKeyToken => "c23c60523565dbfd";

        /// <summary>Returns the version of the loaded SunBurn assembly.</summary>


        public static string Version => "0.1.0.0";


        private static System.Resources.ResourceManager ResourceManager => Class51.ResourceManager;

        /// <summary>Creates a new LightingSystemManager instance.</summary>
        /// <param name="service"></param>

        public LightingSystemManager(IServiceProvider service)
        {
            LightingSystemManager.lightingSystemManager_0 = this;
            this.iserviceProvider_0 = service;
            this.class15_0 = new LightingSystemManager.Class15(service);
            Class0.CheckProductActivation1();
        }

        /// <summary>Cleans up a deleted LightingSystemManager instance.</summary>

        ~LightingSystemManager()
        {
            this.Unload();
            if (LightingSystemManager.lightingSystemManager_0 != this)
                return;
            LightingSystemManager.lightingSystemManager_0 = (LightingSystemManager)null;
        }

        /// <summary>
        /// Gets the system's prefered render target usage for the current platform.
        /// </summary>
        /// <returns></returns>
        public RenderTargetUsage GetBestRenderTargetUsage()
        {
            return RenderTargetUsage.PlatformContents;
        }

        internal Effect method_0(string string_0)
        {
            return this.class15_0.EmbeddedResourceManager.Load<Effect>(string_0);
        }

        internal Model method_1(string string_0)
        {
            return this.class15_0.EmbeddedResourceManager.Load<Model>(string_0);
        }

        internal Texture2D method_2(string string_0)
        {
            return this.class15_0.EmbeddedResourceManager.Load<Texture2D>(string_0);
        }

        internal Texture2D method_3(GraphicsDevice graphicsDevice_0)
        {
            if (this.texture2D_1 == null)
            {
                MemoryStream memoryStream = new MemoryStream(Class51.SplashScreen);
                this.texture2D_1 = Texture2D.FromFile(graphicsDevice_0, (Stream)memoryStream);
                memoryStream.Close();
                memoryStream.Dispose();
            }
            return this.texture2D_1;
        }

        internal Texture3D method_4(GraphicsDevice graphicsDevice_0)
        {
            if (this.texture3D_0 == null)
            {
                this.texture3D_0 = new Texture3D(graphicsDevice_0, 32, 32, 32, 1, TextureUsage.None, SurfaceFormat.Luminance8);
                byte[] data = new byte[32768];
                float num1 = 1f / (float)Math.Pow(15.0, 2.0);
                float num2 = 15.5f;
                Vector3 vector3_1 = new Vector3(num2, num2, num2);
                for (int index1 = 1; index1 < 31; ++index1)
                {
                    for (int index2 = 1; index2 < 31; ++index2)
                    {
                        for (int index3 = 1; index3 < 31; ++index3)
                        {
                            Vector3 vector3_2 = new Vector3((float)index1, (float)index2, (float)index3);
                            vector3_2 -= vector3_1;
                            float num3 = (float)((1.0 - (double)vector3_2.LengthSquared() * (double)num1) * (double)byte.MaxValue);
                            if ((double)num3 >= 1.0)
                            {
                                int index4 = index1 + index2 * 32 + index3 * 32 * 32;
                                data[index4] = (double)num3 <= (double)byte.MaxValue ? (byte)num3 : byte.MaxValue;
                            }
                        }
                    }
                }
                this.texture3D_0.SetData<byte>(data);
            }
            return this.texture3D_0;
        }

        internal TextureCube method_5(GraphicsDevice graphicsDevice_0)
        {
            if (this.textureCube_0 == null)
            {
                this.textureCube_0 = new TextureCube(graphicsDevice_0, 1, 1, TextureUsage.None, SurfaceFormat.Vector4);
                Vector4[] data = new Vector4[1];
                for (int index = 0; index < 6; ++index)
                {
                    switch (index)
                    {
                        case 0:
                            data[0] = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                            break;
                        case 1:
                            data[0] = new Vector4(-1f, 0.0f, 0.0f, 1f);
                            break;
                        case 2:
                            data[0] = new Vector4(0.0f, 1f, 0.0f, 2f);
                            break;
                        case 3:
                            data[0] = new Vector4(0.0f, -1f, 0.0f, 3f);
                            break;
                        case 4:
                            data[0] = new Vector4(0.0f, 0.0f, 1f, 4f);
                            break;
                        case 5:
                            data[0] = new Vector4(0.0f, 0.0f, -1f, 5f);
                            break;
                    }
                    this.textureCube_0.SetData<Vector4>((CubeMapFace)index, data);
                }
            }
            return this.textureCube_0;
        }

        internal TextureCube method_6(GraphicsDevice graphicsDevice_0)
        {
            if (this.textureCube_1 == null)
            {
                this.textureCube_1 = new TextureCube(graphicsDevice_0, 256, 1, TextureUsage.None, SurfaceFormat.Rgba64);
                Short4[] data = new Short4[this.textureCube_1.Size * this.textureCube_1.Size];
                int size = this.textureCube_1.Size;
                float num1 = 1f / (float)(this.textureCube_1.Size - 1);
                Vector3[] vector3Array1 = new Vector3[6] { new Vector3(1f, 0.0f, 0.0f), new Vector3(-1f, 0.0f, 0.0f), new Vector3(0.0f, 1f, 0.0f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, 0.0f, 1f), new Vector3(0.0f, 0.0f, -1f) };
                Vector3[] vector3Array2 = new Vector3[6] { new Vector3(0.0f, 0.0f, -1f), new Vector3(0.0f, 0.0f, 1f), new Vector3(1f, 0.0f, 0.0f), new Vector3(1f, 0.0f, 0.0f), new Vector3(1f, 0.0f, 0.0f), new Vector3(-1f, 0.0f, 0.0f) };
                Vector3[] vector3Array3 = new Vector3[6] { new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, 0.0f, 1f), new Vector3(0.0f, 0.0f, -1f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, -1f, 0.0f) };
                for (int index1 = 0; index1 < 6; ++index1)
                {
                    for (int index2 = 0; index2 < size; ++index2)
                    {
                        for (int index3 = 0; index3 < size; ++index3)
                        {
                            Vector3 vector3 = vector3Array1[index1] + (float)((double)index3 * (double)num1 * 2.0 - 1.0) * vector3Array2[index1] + (float)((double)index2 * (double)num1 * 2.0 - 1.0) * vector3Array3[index1];
                            vector3.Normalize();
                            float num2;
                            float num3;
                            float num4;
                            switch (index1)
                            {
                                case 0:
                                    num2 = -vector3.X;
                                    num3 = vector3.Z;
                                    num4 = vector3.Y;
                                    break;
                                case 1:
                                    num2 = vector3.X;
                                    num3 = -vector3.Z;
                                    num4 = vector3.Y;
                                    break;
                                case 2:
                                    num2 = -vector3.Y;
                                    num3 = vector3.X;
                                    num4 = vector3.Z;
                                    break;
                                case 3:
                                    num2 = vector3.Y;
                                    num3 = -vector3.X;
                                    num4 = vector3.Z;
                                    break;
                                case 4:
                                    num2 = vector3.Z;
                                    num3 = vector3.X;
                                    num4 = -vector3.Y;
                                    break;
                                default:
                                    num2 = vector3.Z;
                                    num3 = vector3.X;
                                    num4 = vector3.Y;
                                    break;
                            }
                            vector3.X = MathHelper.Clamp((float)((double)num3 / (double)num2 * 0.5 + 0.5), 0.0f, 1f);
                            vector3.Y = MathHelper.Clamp((float)((double)num4 / (double)num2 * 0.5 + 0.5), 0.0f, 1f);
                            data[index2 * size + index3] = new Short4((float)(short)((double)vector3.X * (double)ushort.MaxValue + 0.5), (float)(short)((double)vector3.Y * (double)ushort.MaxValue + 0.5), (float)(short)((double)vector3.Z * (double)ushort.MaxValue + 0.5), 0.0f);
                        }
                    }
                    this.textureCube_1.SetData<Short4>((CubeMapFace)index1, data);
                }
            }
            return this.textureCube_1;
        }

        internal Texture2D method_7(GraphicsDevice graphicsDevice_0)
        {
            if (this.texture2D_0 == null)
            {
                this.texture2D_0 = new Texture2D(graphicsDevice_0, 64, 256, 0, TextureUsage.AutoGenerateMipMap, SurfaceFormat.Luminance16);
                ushort[] data = new ushort[this.texture2D_0.Width * this.texture2D_0.Height];
                float num1 = 1f / (float)(this.texture2D_0.Width - 1);
                float num2 = 1f / (float)(this.texture2D_0.Height - 1);
                int num3 = 0;
                for (int index1 = 0; index1 < this.texture2D_0.Height; ++index1)
                {
                    for (int index2 = 0; index2 < this.texture2D_0.Width; ++index2)
                    {
                        float num4 = (float)index2 * num1;
                        float num5 = (float)index1 * num2;
                        float num6 = num4 * num4;
                        float num7 = num5 * num5;
                        float num8 = 12.56637f * num6 * num7 * num7;
                        if ((double)num8 == 0.0)
                            num8 = 1E-07f;
                        float num9 = 1f / num8;
                        float num10 = num6 * num7;
                        if ((double)num10 == 0.0)
                            num10 = 1E-07f;
                        float num11 = (num7 - 1f) / num10;
                        ushort num12 = (ushort)((double)MathHelper.Clamp(num9 * (float)Math.Exp((double)num11), 0.0f, 1f) * (double)ushort.MaxValue);
                        data[num3++] = num12;
                    }
                }
                this.texture2D_0.SetData<ushort>(data);
            }
            return this.texture2D_0;
        }

        internal SpriteFont method_8()
        {
            if (this.spriteFont_0 != null)
                return this.spriteFont_0;
            this.spriteFont_0 = this.class15_0.EmbeddedResourceManager.Load<SpriteFont>("ConsoleFont");
            return this.spriteFont_0;
        }

        internal SpriteBatch method_9(GraphicsDevice graphicsDevice_0)
        {
            if (this.spriteBatch_0 != null)
                return this.spriteBatch_0;
            this.spriteBatch_0 = new SpriteBatch(graphicsDevice_0);
            return this.spriteBatch_0;
        }

        internal VertexDeclaration method_10(GraphicsDevice graphicsDevice_0)
        {
            if (this.vertexDeclaration_0 == null)
                this.vertexDeclaration_0 = new VertexDeclaration(graphicsDevice_0, VertexPositionColor.VertexElements);
            return this.vertexDeclaration_0;
        }

        /// <summary>
        /// Returns information on the currently configured and supported graphic device features.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public GraphicsDeviceSupport GetGraphicsDeviceSupport(GraphicsDevice device)
        {
            if (this.graphicsDeviceSupport_0 == null)
                this.graphicsDeviceSupport_0 = new GraphicsDeviceSupport(device);
            return this.graphicsDeviceSupport_0;
        }

        /// <summary>
        /// Unloads all lighting system and device specific data.  Must be called
        /// when the device is reset (during Game.UnloadGraphicsContent()).
        /// </summary>
        public void Unload()
        {
            this.class15_0.method_0();
            this.spriteFont_0 = (SpriteFont)null;
            this.graphicsDeviceSupport_0 = (GraphicsDeviceSupport)null;
            Disposable.Free<SpriteBatch>(ref this.spriteBatch_0);
            Disposable.Free<Texture3D>(ref this.texture3D_0);
            Disposable.Free<Texture2D>(ref this.texture2D_0);
            Disposable.Free<Texture2D>(ref this.texture2D_1);
            Disposable.Free<TextureCube>(ref this.textureCube_0);
            Disposable.Free<TextureCube>(ref this.textureCube_1);
            Disposable.Free<VertexDeclaration>(ref this.vertexDeclaration_0);
        }

        private class Class15
        {
            private IServiceProvider iserviceProvider_0;
            private ResourceContentManager resourceContentManager_0;

            public ResourceContentManager EmbeddedResourceManager
            {
                get
                {
                    if (this.resourceContentManager_0 == null)
                        this.resourceContentManager_0 = new ResourceContentManager(this.iserviceProvider_0, LightingSystemManager.ResourceManager);
                    return this.resourceContentManager_0;
                }
            }

            public Class15(IServiceProvider serviceprovider)
            {
                this.iserviceProvider_0 = serviceprovider;
            }

            public void method_0()
            {
                if (this.resourceContentManager_0 == null)
                    return;
                this.resourceContentManager_0.Dispose();
                this.resourceContentManager_0 = (ResourceContentManager)null;
            }
        }
    }
}

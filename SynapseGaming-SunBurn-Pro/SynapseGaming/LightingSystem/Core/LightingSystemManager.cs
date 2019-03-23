// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ns11;
using EmbeddedResources;

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
        static LightingSystemManager instance;

        Texture3D texture3D_0;
        Texture2D texture2D_0;
        Texture2D SplashTex;

        TextureCube textureCube_0;
        TextureCube textureCube_1;

        SpriteFont consoleFont;
        SpriteBatch spriteBatch_0;

        VertexDeclaration vertexDeclaration_0;
        GraphicsDeviceSupport graphicsDeviceSupport_0;

        IServiceProvider Service;
        ResourceContentManager Content;


        internal static LightingSystemManager Instance
        {
            get
            {
                if (instance == null)
                    throw new ArgumentException("LightingSystemManager unavailable, please create an instance of the manager before using this object.");
                return instance;
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

        static System.Resources.ResourceManager ResourceManager => EmbeddedResourceBuilder.ResourceManager;

        /// <summary>Creates a new LightingSystemManager instance.</summary>
        /// <param name="service"></param>

        public LightingSystemManager(IServiceProvider service)
        {
            instance = this;
            Service = service;
        }

        /// <summary>Cleans up a deleted LightingSystemManager instance.</summary>

        ~LightingSystemManager()
        {
            this.Unload();
            if (instance != this)
                return;
            instance = null;
        }

        /// <summary>
        /// Gets the system's prefered render target usage for the current platform.
        /// </summary>
        /// <returns></returns>
        public RenderTargetUsage GetBestRenderTargetUsage()
        {
            return RenderTargetUsage.PlatformContents;
        }

        // Embedded resources are linked from SynapseGaming/LightingSystem/Effects/Resources.resx
        ResourceContentManager EmbeddedContent => Content ?? (Content = new ResourceContentManager(Service, ResourceManager));

        internal Effect EmbeddedEffect(string effectName)
        {
            var effect = EmbeddedContent.Load<Effect>(effectName);
            if (effect.IsDisposed)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
            return effect;
        }

        internal Model EmbeddedModel(string modelName)
        {
            var model = EmbeddedContent.Load<Model>(modelName);
            return model;
        }

        internal Texture2D EmbeddedTexture(string textureName)
        {
            var texture = EmbeddedContent.Load<Texture2D>(textureName);
            return texture;
        }

        internal Texture2D CreateSplashTexture(GraphicsDevice device)
        {
            if (SplashTex == null)
            {
                using (var memoryStream = new MemoryStream(EmbeddedResourceBuilder.SplashScreen))
                    SplashTex = Texture2D.FromFile(device, memoryStream);
            }
            return SplashTex;
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
                            Vector3 vector3_2 = new Vector3(index1, index2, index3);
                            vector3_2 -= vector3_1;
                            float num3 = (float)((1.0 - vector3_2.LengthSquared() * (double)num1) * byte.MaxValue);
                            if (num3 >= 1.0)
                            {
                                int index4 = index1 + index2 * 32 + index3 * 32 * 32;
                                data[index4] = (double)num3 <= (double)byte.MaxValue ? (byte)num3 : byte.MaxValue;
                            }
                        }
                    }
                }
                this.texture3D_0.SetData(data);
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
                    this.textureCube_0.SetData((CubeMapFace)index, data);
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
                float num1 = 1f / (this.textureCube_1.Size - 1);
                Vector3[] vector3Array1 = new Vector3[6] { new Vector3(1f, 0.0f, 0.0f), new Vector3(-1f, 0.0f, 0.0f), new Vector3(0.0f, 1f, 0.0f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, 0.0f, 1f), new Vector3(0.0f, 0.0f, -1f) };
                Vector3[] vector3Array2 = new Vector3[6] { new Vector3(0.0f, 0.0f, -1f), new Vector3(0.0f, 0.0f, 1f), new Vector3(1f, 0.0f, 0.0f), new Vector3(1f, 0.0f, 0.0f), new Vector3(1f, 0.0f, 0.0f), new Vector3(-1f, 0.0f, 0.0f) };
                Vector3[] vector3Array3 = new Vector3[6] { new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, 0.0f, 1f), new Vector3(0.0f, 0.0f, -1f), new Vector3(0.0f, -1f, 0.0f), new Vector3(0.0f, -1f, 0.0f) };
                for (int index1 = 0; index1 < 6; ++index1)
                {
                    for (int index2 = 0; index2 < size; ++index2)
                    {
                        for (int index3 = 0; index3 < size; ++index3)
                        {
                            Vector3 vector3 = vector3Array1[index1] + (float)(index3 * (double)num1 * 2.0 - 1.0) * vector3Array2[index1] + (float)(index2 * (double)num1 * 2.0 - 1.0) * vector3Array3[index1];
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
                            vector3.X = MathHelper.Clamp((float)(num3 / (double)num2 * 0.5 + 0.5), 0.0f, 1f);
                            vector3.Y = MathHelper.Clamp((float)(num4 / (double)num2 * 0.5 + 0.5), 0.0f, 1f);
                            data[index2 * size + index3] = new Short4((short)(vector3.X * (double)ushort.MaxValue + 0.5), (short)(vector3.Y * (double)ushort.MaxValue + 0.5), (short)(vector3.Z * (double)ushort.MaxValue + 0.5), 0.0f);
                        }
                    }
                    this.textureCube_1.SetData((CubeMapFace)index1, data);
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
                float num1 = 1f / (this.texture2D_0.Width - 1);
                float num2 = 1f / (this.texture2D_0.Height - 1);
                int num3 = 0;
                for (int index1 = 0; index1 < this.texture2D_0.Height; ++index1)
                {
                    for (int index2 = 0; index2 < this.texture2D_0.Width; ++index2)
                    {
                        float num4 = index2 * num1;
                        float num5 = index1 * num2;
                        float num6 = num4 * num4;
                        float num7 = num5 * num5;
                        float num8 = 12.56637f * num6 * num7 * num7;
                        if (num8 == 0.0)
                            num8 = 1E-07f;
                        float num9 = 1f / num8;
                        float num10 = num6 * num7;
                        if (num10 == 0.0)
                            num10 = 1E-07f;
                        float num11 = (num7 - 1f) / num10;
                        ushort num12 = (ushort)(MathHelper.Clamp(num9 * (float)Math.Exp(num11), 0.0f, 1f) * (double)ushort.MaxValue);
                        data[num3++] = num12;
                    }
                }
                this.texture2D_0.SetData(data);
            }
            return this.texture2D_0;
        }

        internal SpriteFont ConsoleFont()
        {
            if (consoleFont != null)
                return consoleFont;
            consoleFont = EmbeddedContent.Load<SpriteFont>("ConsoleFont");
            return consoleFont;
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
            Content?.Dispose();
            Content = null;
            consoleFont = null;
            graphicsDeviceSupport_0 = null;
            Disposable.Free(ref spriteBatch_0);
            Disposable.Free(ref texture3D_0);
            Disposable.Free(ref texture2D_0);
            Disposable.Free(ref SplashTex);
            Disposable.Free(ref textureCube_0);
            Disposable.Free(ref textureCube_1);
            Disposable.Free(ref vertexDeclaration_0);
        }
    }
}

﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SplashScreen
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ns11;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Displays the SunBurn splash screen. Used when the XNA Game object is not available, such as WinForm applications.
    /// </summary>
    public sealed class SplashScreen
    {
        private Vector2 vector2_0;
        private Vector2 vector2_1;
        private int FrameId;
        private BasicEffect basicEffect_0;
        private FullFrameQuad fullFrameQuad_0;
        private IGraphicsDeviceService igraphicsDeviceService_0;

        float CurrentTime;
        private const double SplashTime = 2.5;
        private const double ClickTime = 0.5;

        /// <summary>
        /// Used to determine when the SunBurn splash screen is finished displaying
        /// and it's safe to begin game rendering.
        /// </summary>
        public static bool DisplayComplete { get; private set; } = true /*true: force immediate start of the game, skip the splash*/;

        /// <summary>
        /// Used to enable or disable the SunBurn splash screen during development. Enabling the splash
        /// screen helps when making sure the screen displays properly in released projects.
        /// </summary>
        public bool ShowDuringDevelopment { get; set; }

        /// <summary>Creates a new SplashScreen instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public SplashScreen(IGraphicsDeviceService graphicsdevicemanager)
        {
            this.igraphicsDeviceService_0 = graphicsdevicemanager;
        }


        internal static void CheckProductActivation()
        {
            //Class0.CheckProductActivation1();
            if (!DisplayComplete)
                throw new Exception("SunBurn splash screen required for rendering, please display splash screen before calling this method.");
        }


        private void Initialize()
        {
            if (this.basicEffect_0 != null)
                return;
            GraphicsDevice device = this.igraphicsDeviceService_0.GraphicsDevice;
            this.basicEffect_0 = new BasicEffect(device, null);
            this.basicEffect_0.DiffuseColor = Vector3.Zero;
            this.basicEffect_0.AmbientLightColor = Vector3.Zero;
            this.basicEffect_0.EmissiveColor = Vector3.Zero;
            this.basicEffect_0.LightingEnabled = false;
            this.basicEffect_0.FogEnabled = false;
            this.basicEffect_0.VertexColorEnabled = false;
            Texture2D texture2D = LightingSystemManager.Instance.CreateSplashTexture(device);
            this.basicEffect_0.TextureEnabled = true;
            this.basicEffect_0.Texture = texture2D;
            this.basicEffect_0.World = Matrix.Identity;
            this.basicEffect_0.View = Matrix.Identity;
            this.basicEffect_0.Projection = Matrix.Identity;
            Vector2 screenmin = -Vector2.One;
            Vector2 one = Vector2.One;
            float num1 = texture2D.Width / (float)texture2D.Height;
            if (device.Viewport.AspectRatio > (double)num1)
            {
                float num2 = device.Viewport.Height * num1;
                float num3 = num2 / device.Viewport.Width;
                screenmin.X = -num3;
                one.X = num3;
            }
            else
            {
                float num2 = device.Viewport.Width / num1;
                float num3 = num2 / device.Viewport.Height;
                screenmin.Y = -num3;
                one.Y = num3;
            }
            this.fullFrameQuad_0 = new FullFrameQuad(device, device.Viewport.Width, device.Viewport.Height, screenmin, one);
            this.vector2_0 = one * new Vector2(0.2f, 0.75f);
            Vector2 vector2 = new Vector2(device.Viewport.Width, device.Viewport.Height) * 0.5f;
            this.vector2_0 = this.vector2_0 * vector2;
            this.vector2_0.Y += vector2.Y;
            this.vector2_1 = one * new Vector2(0.2f, 0.707f);
            this.vector2_1 = this.vector2_1 * vector2;
            this.vector2_1.Y += vector2.Y;
        }

        /// <summary>Called when graphics resources need to be unloaded.</summary>
        public void Unload()
        {
            Disposable.Free(ref basicEffect_0);
            Disposable.Free(ref fullFrameQuad_0);
        }

        /// <summary>
        /// Called periodically to allow users to click out of the splash screen.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
            if (DisplayComplete || FrameId <= 100)
                return;
            if (CurrentTime > SplashTime)
            {
                DisplayComplete = true;
            }
            else
            {
                if (CurrentTime <= ClickTime)
                    return;
                GamePadState state1 = GamePad.GetState(PlayerIndex.One);
                KeyboardState state2 = Keyboard.GetState();
                if (state1.IsConnected && (state1.IsButtonDown(Buttons.A) || state1.IsButtonDown(Buttons.B)))
                {
                    DisplayComplete = true;
                }
                else if (!state2.IsKeyDown(Keys.Space) && !state2.IsKeyDown(Keys.Enter) && !state2.IsKeyDown(Keys.Escape))
                {
                    if (Mouse.GetState().LeftButton != ButtonState.Pressed)
                        return;
                    DisplayComplete = true;
                }
                else
                    DisplayComplete = true;
            }
        }

        /// <summary>
        /// Renders the SunBurn splash screen (require by the SunBurn license).
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Render(float deltaTime)
        {
            if (this.basicEffect_0 == null)
                Initialize();

            if (DisplayComplete)
                return;

            CurrentTime += deltaTime;
            ++FrameId;
            GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
            graphicsDevice.RenderState.CullMode = CullMode.None;
            graphicsDevice.RenderState.AlphaBlendEnable = false;
            graphicsDevice.RenderState.DepthBufferEnable = false;
            graphicsDevice.Clear(Color.Black);
            float num2 = 0.25f;
            float num3 = 4f;
            this.basicEffect_0.DiffuseColor = Vector3.One * (MathHelper.Clamp((CurrentTime - num2) * num3, 0.0f, 1f) 
                                                           * MathHelper.Clamp((float)(5.0 - (CurrentTime + num2)) * num3, 0.0f, 1f));
            this.fullFrameQuad_0.Render(basicEffect_0);
        }
    }
}

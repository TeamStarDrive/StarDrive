using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Wave;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Linq;
using SgMotion;
using SgMotion.Controllers;

namespace Ship_Game
{
    public sealed class MainMenuScreen : GameScreen
    {
        private IWavePlayer WaveOut;
        private Mp3FileReader Mp3FileReader;
        private BatchRemovalCollection<Comet> CometList = new BatchRemovalCollection<Comet>();
        private Rectangle StarFieldRect = new Rectangle(0, 0, 1920, 1080);
        private Texture2D StarField;
        private readonly Array<Texture2D> LogoAnimation = new Array<Texture2D>();

        private SceneObject MoonObj;
        private Vector3 MoonPosition;
        private Vector3 MoonRotation = new Vector3(264f, 198, 15f);
        private const float MoonScale = 0.7f;
        private SceneObject ShipObj;
        private Vector3 ShipPosition;
        private Vector3 ShipRotation = new Vector3(-116f, -188f, -19f);
        private float ShipScale = MoonScale * 1.75f;

        private Matrix View;
        private Matrix Projection;

        private AnimationController ShipAnim;
        private MouseState CurrentMouse;
        private MouseState PreviousMouse;

        private Rectangle Portrait;
        private Rectangle LogoRect;
        private float Rotate = 3.85f;

        private int AnimationFrame;
        private bool Flip;
        private bool StayOn;
        private int FlareFrames;

        private readonly Texture2D TexComet = ResourceManager.TextureDict["GameScreens/comet"];

        public MainMenuScreen() : base(null /*no parent*/)
        {
            TransitionOnTime  = TimeSpan.FromSeconds(1);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void Draw(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MainMenuScreen mainMenuScreen = this;
            mainMenuScreen.Rotate = mainMenuScreen.Rotate + elapsedTime / 350f;
            if (RandomMath.RandomBetween(0f, 100f) > 99.75)
            {
                Comet c = new Comet()
                {
                    Position = new Vector2(RandomMath.RandomBetween(-100f, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f),
                    Velocity = new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f)
                };
                c.Velocity = Vector2.Normalize(c.Velocity);
                c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
                this.CometList.Add(c);
            }
            Vector2 cometOrigin = new Vector2(TexComet.Width, TexComet.Height) / 2f;
            if (SplashScreen.DisplayComplete )
            {
                ScreenManager.splashScreenGameComponent.Visible = false;
                ScreenManager.sceneState.BeginFrameRendering(this.View, this.Projection, gameTime, ScreenManager.environment, true);
                ScreenManager.editor.BeginFrameRendering(ScreenManager.sceneState);
                try
                {
                    ScreenManager.inter.BeginFrameRendering(ScreenManager.sceneState);
                }
                catch { }
                this.DrawNew(gameTime);
                ScreenManager.inter.RenderManager.Render();
                ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
                ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
                ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
                Viewport viewport = Viewport;
                Vector3 mp = viewport.Project(this.MoonObj.WorldBoundingSphere.Center, this.Projection, this.View, Matrix.Identity);
                var moonFlarePos = new Vector2(mp.X - 40f - 2f, mp.Y - 40f + 24f);
                var origin = new Vector2(184f, 184f);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_flare"], moonFlarePos, null, Color.White, 0f, origin, 0.95f, SpriteEffects.None, 1f);
                ScreenManager.SpriteBatch.End();
                ScreenManager.SpriteBatch.Begin();
                if (AnimationFrame >= 41 && AnimationFrame < 52)
                {
                    float alphaStep = 255f / 12;
                    float alpha = (AnimationFrame - 41) * alphaStep;
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    Rectangle moon1 = new Rectangle((int)moonFlarePos.X - 220, (int)moonFlarePos.Y - 130, 201, 78);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (this.AnimationFrame >= 52 && this.AnimationFrame <= 67)
                {
                    float Alpha = 220f;
                    Rectangle moon1 = new Rectangle((int)moonFlarePos.X - 220, (int)moonFlarePos.Y - 130, 201, 78);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)Alpha));
                }
                if (this.AnimationFrame > 67 && this.AnimationFrame <= 95)
                {
                    float alphaStep = (255f / 28);
                    float alpha = 255f - (AnimationFrame - 67) * alphaStep;
                    if (alpha < 0f)
                    {
                        alpha = 0f;
                    }
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    var moon1 = new Rectangle((int)moonFlarePos.X - 220, (int)moonFlarePos.Y - 130, 201, 78);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (AnimationFrame >= 161 && AnimationFrame < 172)
                {
                    float alphaStep = (255f / 12);
                    float alpha = (AnimationFrame - 161) * alphaStep;
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    var moon1 = new Rectangle((int)moonFlarePos.X - 250, (int)moonFlarePos.Y + 60, 254, 82);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (AnimationFrame >= 172 && AnimationFrame <= 187)
                {
                    const float alpha = 220f;
                    var moon1 = new Rectangle((int)moonFlarePos.X - 250, (int)moonFlarePos.Y + 60, 254, 82);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (this.AnimationFrame > 187 && this.AnimationFrame <= 215)
                {
                    float alphaStep = (255f / 28);
                    float alpha = 255f - (AnimationFrame - 187) * alphaStep;
                    if (alpha < 0f)
                    {
                        alpha = 0f;
                    }
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    Rectangle moon1 = new Rectangle((int)moonFlarePos.X - 250, (int)moonFlarePos.Y + 60, 254, 82);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (this.AnimationFrame >= 232 && this.AnimationFrame < 243)
                {
                    float alphaStep = (255f / 12);
                    float alpha = (AnimationFrame - 232) * alphaStep;
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    var moon1 = new Rectangle((int)moonFlarePos.X + 60, (int)moonFlarePos.Y + 80, 156, 93);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (this.AnimationFrame >= 243 && this.AnimationFrame <= 258)
                {
                    const float alpha = 220f;
                    var moon1 = new Rectangle((int)moonFlarePos.X + 60, (int)moonFlarePos.Y + 80, 156, 93);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)alpha));
                }
                if (this.AnimationFrame > 258 && this.AnimationFrame <= 286)
                {
                    float alphaStep = (255f / 28);
                    float alpha = 255f - (AnimationFrame - 258) * alphaStep;
                    if (alpha < 0f)
                    {
                        alpha = 0f;
                    }
                    if (alpha > 220f)
                    {
                        alpha = 220f;
                    }
                    var moon1 = new Rectangle((int)moonFlarePos.X + 60, (int)moonFlarePos.Y + 80, 156, 93);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)alpha));
                }
                ScreenManager.SpriteBatch.End();
                ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
                foreach (Comet c in this.CometList)
                {
                    float alpha = 255f;
                    if (c.Position.Y > 100f)
                    {
                        alpha = 25500f / c.Position.Y;
                        if (alpha > 255f)
                        {
                            alpha = 255f;
                        }
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/comet2"], c.Position, null, new Color(255, 255, 255, (byte)alpha), c.Rotation, cometOrigin, 0.45f, SpriteEffects.None, 1f);
                    c.Position += c.Velocity * 2400f * elapsedTime;
                    if (c.Position.Y <= 1050f)
                    {
                        continue;
                    }
                    this.CometList.QueuePendingRemoval(c);
                }
                this.CometList.ApplyPendingRemovals();
                ScreenManager.SpriteBatch.End();
                ScreenManager.SpriteBatch.Begin();
                int numEntries = 5;
                int k = 5;
                foreach (UIButton b in this.Buttons)
                {
                    Rectangle r = b.Rect;
                    float transitionOffset = MathHelper.Clamp((TransitionPosition - 0.5f * k / numEntries) / 0.5f, 0f, 1f);
                    k--;
                    if (ScreenState != ScreenState.TransitionOn)
                    {
                        r.X = r.X + (int)transitionOffset * 512;
                    }
                    else
                    {
                        r.X = r.X + (int)(transitionOffset * 512f);
                        if (transitionOffset.AlmostEqual(0f))
                        {
                            GameAudio.PlaySfxAsync("blip_click");
                        }
                    }
                    b.Draw(ScreenManager.SpriteBatch, r);
                }

                GlobalStats.ActiveMod?.DrawMainMenuOverlay(ScreenManager, Portrait);

                ScreenManager.SpriteBatch.Draw(LogoAnimation[0], LogoRect, Color.White);
                if (LogoAnimation.Count > 1)
                {
                    LogoAnimation.RemoveAt(0);
                }
                ScreenManager.SpriteBatch.End();
                ScreenManager.inter.EndFrameRendering();
                ScreenManager.editor.EndFrameRendering();
                ScreenManager.sceneState.EndFrameRendering();
            }
        }

        public void DrawNew(GameTime gameTime)
        {
            Flip = !Flip;
            if (Flip) AnimationFrame += 1;

            // @todo What the hell is this bloody thing?? REFACTOR
            int width  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            ScreenManager.SpriteBatch.Begin();
            Rectangle screenRect = new Rectangle(0, 0, width, height);
            ScreenManager.SpriteBatch.Draw(StarField, StarFieldRect, Color.White);
            Rectangle planetRect = new Rectangle(0, height - 680, 1016, 680);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet"], planetRect, Color.White);
            if (AnimationFrame >= 127 && AnimationFrame < 145)
            {
                float alphaStep = 255f / 18;
                float alpha = (AnimationFrame - 127) * alphaStep;
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid"], planetGridRect, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 145 && AnimationFrame <= 148)
            {
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid"], planetGridRect, Color.White);
            }
            if (AnimationFrame > 148 && AnimationFrame <= 180)
            {
                float alphaStep = 255f / 31;
                float alpha = 255f - (AnimationFrame - 148) * alphaStep;
                if (alpha < 0f) alpha = 0f;
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid"], planetGridRect, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 141 && AnimationFrame <= 149)
            {
                float alphaStep = 255f / 9;
                float alpha = (AnimationFrame - 141) * alphaStep;
                Rectangle grid1Hex = new Rectangle(277, height - 592, 77, 33);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_1"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 149 && AnimationFrame <= 165)
            {
                float alphaStep = 255f / 16;
                float alpha = 255f - (AnimationFrame - 149) * alphaStep;
                Rectangle grid1Hex = new Rectangle(277, height - 592, 77, 33);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_1"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 159 && AnimationFrame <= 168)
            {
                float alphaStep = 255f / 10;
                float alpha = (AnimationFrame - 159) * alphaStep;
                Rectangle grid1Hex = new Rectangle(392, height - 418, 79, 60);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_2"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 168 && AnimationFrame <= 183)
            {
                float alphaStep = 255f / 15;
                float alpha = 255f - (AnimationFrame - 168) * alphaStep;
                Rectangle grid1Hex = new Rectangle(392, height - 418, 79, 60);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_2"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 150 && AnimationFrame <= 158)
            {
                float alphaStep = 255f / 9;
                float alpha = (AnimationFrame - 150) * alphaStep;
                Rectangle grid1Hex = new Rectangle(682, height - 295, 63, 67);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_3"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 158 && AnimationFrame <= 174)
            {
                float alphaStep = 255f / 16;
                float alpha = 255f - (AnimationFrame - 158) * alphaStep;
                Rectangle grid1Hex = new Rectangle(682, height - 295, 63, 67);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_grid_hex_3"], grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 7 || StayOn)
            {
                float alphaStep = 255f / 30;
                float alpha = MathHelper.SmoothStep((AnimationFrame - 1 - 7) * alphaStep, (AnimationFrame - 7) * alphaStep, 0.9f);
                if (alpha > 225f || StayOn)
                {
                    alpha = 225f;
                    StayOn = true;
                }
                Rectangle cornerTl = new Rectangle(31, 30, 608, 340);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/corner_TL"], cornerTl, new Color(Color.White, (byte)alpha));
                Rectangle cornerBr = new Rectangle(width - 551, height - 562, 520, 532);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/corner_BR"], cornerBr, new Color(Color.White, (byte)alpha));
                
                Rectangle version = new Rectangle(205, height - 37, 318, 12);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/version_bar"], version, new Color(Color.White, (byte)alpha));
                Vector2 textPos = new Vector2(20f, version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "StarDrive 15B", textPos, Color.White);

                version = new Rectangle(20+ (int)Fonts.Pirulen12.MeasureString(GlobalStats.ExtendedVersion).X , height - 85, 318, 12);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/version_bar"], version, new Color(Color.White, (byte)alpha));
                textPos = new Vector2(20f, version.Y  +6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, GlobalStats.ExtendedVersion, textPos, Color.White);

                if (GlobalStats.ActiveModInfo != null)
                {
                    string title = GlobalStats.ActiveModInfo.ModName;
                    //if (GlobalStats.ActiveModInfo.Version != null && GlobalStats.ActiveModInfo.Version != "" && !title.Contains(GlobalStats.ActiveModInfo.Version))
                    if (!string.IsNullOrEmpty(GlobalStats.ActiveModInfo.Version) && !title.Contains(GlobalStats.ActiveModInfo.Version))
                        title = string.Concat(title, " - ", GlobalStats.ActiveModInfo.Version);

                    version = new Rectangle(20 + (int)Fonts.Pirulen12.MeasureString(title).X, height - 60, 318, 12);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/version_bar"], version, new Color(Color.White, (byte)alpha));

                    textPos = new Vector2(20f, version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, title, textPos, Color.White);
                }
            }
            if (AnimationFrame > 300)
            {
                AnimationFrame = 0;
            }
            ScreenManager.SpriteBatch.End();
            ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
            if (FlareFrames >= 0 && FlareFrames <= 31)
            {
                float alphaStep = 35f / 32f;
                float alpha = 255f - FlareFrames * alphaStep;
                Rectangle solarFlare = new Rectangle(0, height - 784, 1024, 784);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_solarflare"], solarFlare, new Color((byte)alpha, (byte)alpha, (byte)alpha, 255));
            }
            if (FlareFrames > 31 && FlareFrames <= 62)
            {
                float alphaStep = 35f / 31f;
                float alpha = 220f + (FlareFrames - 31) * alphaStep;
                var solarFlare = new Rectangle(0, height - 784, 1024, 784);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/planet_solarflare"], solarFlare, new Color((byte)alpha, (byte)alpha, (byte)alpha, 255));
            }
            if (Flip)
            {
                FlareFrames += 1;
            }
            if (FlareFrames >= 62)
            {
                FlareFrames = 0;
            }
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["MainMenu/vignette"], screenRect, Color.White);
            ScreenManager.SpriteBatch.End();
        }

        public override void HandleInput(InputState input)
        {
            // Use these controls to reorient the ship and planet in the menu. The new rotation
            // is logged into debug console and can be set as default values later
        #if false
            if (input.CurrentKeyboardState.IsKeyDown(Keys.W)) ShipRotation.X += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.S)) ShipRotation.X -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.A)) ShipRotation.Y += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.D)) ShipRotation.Y -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Q)) ShipRotation.Z += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.E)) ShipRotation.Z -= 0.5f;

            if (input.CurrentKeyboardState.IsKeyDown(Keys.I)) MoonRotation.X += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.K)) MoonRotation.X -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.J)) MoonRotation.Y += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.L)) MoonRotation.Y -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.U)) MoonRotation.Z += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.O)) MoonRotation.Z -= 0.5f;

            // if new keypress, spawn random ship
            if (input.LastKeyboardState.IsKeyUp(Keys.Space) && input.CurrentKeyboardState.IsKeyDown(Keys.Space))
                InitRandomShip();

            if (input.CurrentKeyboardState.GetPressedKeys().Length > 0)
                Log.Info("rot {0}   {1}", ShipRotation, MoonRotation);
        #endif

            if (input.InGameSelect)
            {
                Viewport viewport = Viewport;
                Vector3 nearPoint = viewport.Unproject(new Vector3(input.CursorPosition, 0f), Projection, View, Matrix.Identity);
                Vector3 farPoint  = viewport.Unproject(new Vector3(input.CursorPosition, 1f), Projection, View, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();

                var pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                var pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                if (pickedPosition.InRadius(MoonObj.WorldBoundingSphere.Center, MoonObj.WorldBoundingSphere.Radius))
                {
                    GameAudio.PlaySfxAsync("sd_bomb_impact_01");
                }
            }
            CurrentMouse = input.CurrentMouseState;
            bool okcomet = true;
            foreach (UIButton b in Buttons)
            {
                if (!b.Rect.HitTest(CurrentMouse.X, CurrentMouse.Y))
                {
                    b.State = UIButton.PressState.Default;
                }
                else
                {
                    okcomet = false;
                    if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
                        GameAudio.PlaySfxAsync("mouse_over4");

                    b.State = UIButton.PressState.Hover;
                    if (CurrentMouse.LeftButton == ButtonState.Pressed && PreviousMouse.LeftButton == ButtonState.Pressed)
                        b.State = UIButton.PressState.Pressed;

                    if (CurrentMouse.LeftButton != ButtonState.Pressed || PreviousMouse.LeftButton != ButtonState.Released)
                    {
                        continue;
                    }
                    switch (b.Launches)
                    {
                        case "New Campaign":
                            GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                            OnPlayGame();
                            break;
                        case "Tutorials":
                            GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                            ScreenManager.AddScreen(new TutorialScreen(this));
                            break;
                        case "Load Game":
                            GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                            ScreenManager.AddScreen(new LoadSaveScreen(this));
                            break;
                        case "Options":
                            ScreenManager.AddScreen(new OptionsScreen(this, new Rectangle(0, 0, 600, 600))
                            {
                                TitleText  = Localizer.Token(4),
                                MiddleText = Localizer.Token(4004)
                            });
                            break;
                        case "Mods":
                            ScreenManager.AddScreen(new ModManager(this));
                            break;
                        case "Exit":
                            Game1.Instance.Exit();
                            break;
                        case "Info":
                            GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                            ScreenManager.AddScreen(new InGameWiki(this, new Rectangle(0, 0, 750, 600)));
                            break;
                        
                    }
                }
            }
            if (input.C && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                ScreenManager.AddScreen(new ShipToolScreen());
                ExitScreen();
            }
            if (okcomet && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
            {
                Comet c = new Comet
                {
                    Position = new Vector2(RandomMath.RandomBetween(-100f,
                                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f)
                };
                c.Velocity = c.Position.DirectionToTarget(input.CursorPosition);
                c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
                CometList.Add(c);
            }
            PreviousMouse = input.LastMouseState;
            base.HandleInput(input);
        }


        public override void LoadContent()
        {
            base.LoadContent();
            GameAudio.ConfigureAudioSettings();

            var para = ScreenManager.GraphicsDevice.PresentationParameters;
            var size = new Vector2(para.BackBufferWidth, para.BackBufferHeight);

            const string basepath = "Stardrive Main Logo 2_";
            for (int i = 0; i < 81; i++)
            {
                string remainder = i.ToString("00000.##");
                var logo = TransientContent.Load<Texture2D>(
                    "MainMenu/Stardrive logo/" + basepath + remainder);
                LogoAnimation.Add(logo);
            }

            StarField = TransientContent.Load<Texture2D>(size.Y <= 1080 
                        ? "MainMenu/nebula_stars_bg" : "MainMenu/HR_nebula_stars_bg");
            StarFieldRect = new Rectangle(0, 0, (int)size.X, (int)size.Y);

            Vector2 pos = new Vector2(size.X - 200, size.Y / 2 - 100);
            Buttons.Clear();
            Button(ref pos, "New Campaign", localization: 1);
            //Button(ref pos, "", "Battle Mode");
            Button(ref pos, "Tutorials", localization: 3);
            Button(ref pos, "Load Game", localization: 2);
            Button(ref pos, "Options", localization: 4);
            Button(ref pos, "Mods", "Mods");
            Button(ref pos, "Info", "BlackBox Info");
            Button(ref pos, "Exit", localization: 5);

            ScreenManager.inter.ObjectManager.Clear();
            ScreenManager.inter.LightManager.Clear();

            // @todo Why are these global inits here??
            ShieldManager.LoadContent(Game1.GameContent);
            Beam.BeamEffect = Game1.GameContent.Load<Effect>("Effects/BeamFX");
            BackgroundItem.QuadEffect = new BasicEffect(ScreenManager.GraphicsDevice, (EffectPool)null)
            {
                World = Matrix.Identity,
                View = View,
                Projection = Projection,
                TextureEnabled = true
            };
            Portrait = new Rectangle((int)size.X / 2 - 960, (int)size.Y / 2 - 540, 1920, 1080);

            while (Portrait.Width < size.X && Portrait.Height < size.Y)
            {
                Portrait.Width  += 12;
                Portrait.Height += 7;
                Portrait.X = (int)size.X / 2 - Portrait.Width  / 2;
                Portrait.Y = (int)size.Y / 2 - Portrait.Height / 2;
            }

            ResetMusic();

            LogoRect = new Rectangle((int)size.X - 600, 128, 512, 128);
            MoonPosition = new Vector3(size.X / 2 - 300, LogoRect.Y + 70 - size.Y / 2, 0f);
            ShipPosition = new Vector3(size.X / 2 - 1200, LogoRect.Y + 400 - size.Y / 2, 0f);

            string planet = "Model/SpaceObjects/planet_" + RandomMath.IntBetween(1, 29);
            MoonObj = new SceneObject(TransientContent.Load<Model>(planet).Meshes[0]) { ObjectType = ObjectType.Dynamic };
            MoonObj.AffineTransform(MoonPosition, MoonRotation.DegsToRad(), MoonScale);
            ScreenManager.inter.ObjectManager.Submit(MoonObj);

            InitRandomShip();

            LightRig rig = TransientContent.Load<LightRig>("example/ShipyardLightrig");
            rig.AssignTo(this);
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

            Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateTranslation(0f, 0f, 0f) 
                * Matrix.CreateRotationY(180f.ToRadians())
                * Matrix.CreateRotationX(0f.ToRadians())
                * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));

            Projection = Matrix.CreateOrthographic(size.X, size.Y, 1f, 80000f);

            LoadTestContent();

            Log.Info("MainMenuScreen GameContent {0:0.0}MB", TransientContent.GetLoadedAssetMegabytes());
        }

        // for quick feature testing in the main menu
        private void LoadTestContent()
        {
            //var atlas = TextureAtlas.Load(ScreenManager.Content, "Explosions/smaller/shipExplosion");
            ResetMusic();
        }

        public void OnPlaybackStopped(object sender, EventArgs e)
        {
            if (WaveOut == null) return;
            WaveOut.Dispose();
            Mp3FileReader.Dispose();
        }

        private void OnPlayGame()
        {
            ScreenManager.AddScreen(new RaceDesignScreen(ScreenManager.GraphicsDevice, this));
        }

        private void PlayMp3(string fileName)
        {
            WaveOut = new WaveOut();
            Mp3FileReader = new Mp3FileReader(fileName);
            try
            {
                WaveOut.Init(Mp3FileReader);
                #pragma warning disable CS0618 // Type or member is obsolete
                WaveOut.Volume = GlobalStats.MusicVolume;
                #pragma warning restore CS0618 // Type or member is obsolete
                WaveOut.Play();
                WaveOut.PlaybackStopped += OnPlaybackStopped;
            }
            catch
            {
            }
        }

        public void ResetMusic()
        {
            if (WaveOut != null)
                OnPlaybackStopped(null, null);

            if (GlobalStats.HasMod && GlobalStats.ActiveMod.MainMenuMusic.NotEmpty())
            {
                PlayMp3(GlobalStats.ModPath + GlobalStats.ActiveMod.MainMenuMusic);
                GameAudio.StopGenericMusic();
            }
            else if (ScreenManager.Music.IsStopped)
            {
                ScreenManager.Music = GameAudio.PlayMusic("SD_Theme_Reprise_06");
            }
        }

        private void InitRandomShip()
        {
            if (ShipObj != null) // Allow multiple inits (mostly for testing)
            {
                ScreenManager.inter.ObjectManager.Remove(ShipObj);
                ShipObj.Clear();
                ShipObj = null;
                ShipAnim = null;
            }

            // FrostHand: do we actually need to show Model/Ships/speeder/ship07 in base version? Or could show random ship for base and modded version?
            if (GlobalStats.HasMod && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = RandomMath.InRange(ResourceManager.MainMenuShipList.ModelPaths.Count);
                string modelPath = ResourceManager.MainMenuShipList.ModelPaths[shipIndex];
                ShipObj = new SceneObject(ResourceManager.GetModel(modelPath).Meshes[0]) { ObjectType = ObjectType.Dynamic };
            }
            else
            {
                var hulls = ResourceManager.HullsDict.Values.Where(s
                        => s.Role == ShipData.RoleName.frigate
                        //|| s.Role == ShipData.RoleName.cruiser
                        //|| s.Role == ShipData.RoleName.capital
                        //&& s.ShipStyle != "Remnant"
                        && s.ShipStyle != "Ralyeh").ToArray(); // Ralyeh ships look disgusting in the menu
                var hull = hulls[RandomMath.InRange(hulls.Length)];

                if (hull.Animated) // Support animated meshes if we use them at all
                {
                    SkinnedModel model = ResourceManager.GetSkinnedModel(hull.ModelPath);
                    ShipObj = new SceneObject(model.Model)
                    {
                        ObjectType = ObjectType.Dynamic
                    };
                    ShipAnim = new AnimationController(model.SkeletonBones);
                    ShipAnim.StartClip(model.AnimationClips["Take 001"]);
                }
                else
                {
                    ShipObj = new SceneObject(ResourceManager.GetModel(hull.ModelPath).Meshes[0]) { ObjectType = ObjectType.Dynamic };
                }
            }

            // we want mainmenu ships to have a certain acceptable size:
            ShipScale = 266f / ShipObj.ObjectBoundingSphere.Radius;

            //var bb = ShipObj.GetMeshBoundingBox();
            //float length = bb.Max.Z - bb.Min.Z;
            //float width  = bb.Max.X - bb.Min.X;
            //float height = bb.Max.Y - bb.Min.Y;
            //Log.Info("ship length {0} width {1} height {2}", length, width, height);

            ShipObj.AffineTransform(ShipPosition, ShipRotation.DegsToRad(), ShipScale);
            ScreenManager.inter.ObjectManager.Submit(ShipObj);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ScreenManager.inter.Update(gameTime);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MoonPosition.X += deltaTime * 0.6f; // 0.6 units/s
            MoonRotation.Y += deltaTime * 1.2f;
            MoonObj.AffineTransform(MoonPosition, MoonRotation.DegsToRad(), MoonScale);

            // slow moves the ship across the screen
            ShipRotation.Y += deltaTime * 0.06f;
            ShipPosition   += deltaTime * -ShipRotation.DegreesToUp() * 1.5f; // move forward 1.5 units/s

            // shipObj can be modified while mod is loading
            if (ShipObj != null)
            {
                ShipObj.AffineTransform(ShipPosition, ShipRotation.DegsToRad(), ShipScale);

                // Added by RedFox: support animated ships
                if (ShipAnim != null)
                {
                    ShipObj.SkinBones = ShipAnim.SkinnedBoneTransforms;
                    ShipAnim.Speed = 0.45f;
                    ShipAnim.Update(gameTime.ElapsedGameTime, Matrix.Identity);
                }
            }

            ScreenManager.inter.Update(gameTime);

            if (!GlobalStats.HasMod || GlobalStats.ActiveMod.MainMenuMusic.IsEmpty())
            {
                if (ScreenManager.Music.IsStopped)
                    ResetMusic();
            }

            if (IsExiting && TransitionPosition >= 0.99f && ScreenManager.Music.IsPlaying)
            {
                ScreenManager.Music.Stop();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public class Comet
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
        }

        protected override void Dispose(bool disposing)
        {
            CometList?.Dispose(ref CometList);
            WaveOut?.Dispose(ref WaveOut);
            Mp3FileReader?.Dispose(ref Mp3FileReader);
            base.Dispose(disposing);
        }
    }
}
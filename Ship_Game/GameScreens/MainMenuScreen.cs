using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Wave;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Linq;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Ships;

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

        private Rectangle Portrait;
        private Rectangle LogoRect;
        private float Rotate = 3.85f;

        private int AnimationFrame;
        private bool Flip;
        private bool StayOn;
        private int FlareFrames;

        private bool DebugMeshInspect = false;

        private readonly Texture2D TexComet   = ResourceManager.Texture("GameScreens/comet2");
        private readonly Texture2D MoonFlare  = ResourceManager.Texture("MainMenu/moon_flare");
        private readonly Texture2D AlienText1 = ResourceManager.Texture("MainMenu/moon_1");
        private readonly Texture2D AlienText2 = ResourceManager.Texture("MainMenu/moon_2");
        private readonly Texture2D AlienText3 = ResourceManager.Texture("MainMenu/moon_3");
        private Vector2 MoonFlarePos = Vector2.Zero;


        public MainMenuScreen() : base(null /*no parent*/)
        {
            TransitionOnTime  = TimeSpan.FromSeconds(1);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }



        private FadeInOutAnim[] AlienTextAnim;

        private void DrawAlienTextOverlays()
        {
            if (AlienTextAnim == null)
            {
                var rect1 = new Rectangle((int)MoonFlarePos.X - 220, (int)MoonFlarePos.Y - 130, 201, 78);
                var rect2 = new Rectangle((int)MoonFlarePos.X - 250, (int)MoonFlarePos.Y + 60, 254, 82);
                var rect3 = new Rectangle((int)MoonFlarePos.X + 60,  (int)MoonFlarePos.Y + 80, 156, 93);

                AlienTextAnim = new[]  // fadein...stay...fadeout...end
                {
                    new FadeInOutAnim(AlienText1, rect1, fadeIn:41,  stay:52,  fadeOut:66,  end:95),
                    new FadeInOutAnim(AlienText2, rect2, fadeIn:161, stay:172, fadeOut:188, end:215),
                    new FadeInOutAnim(AlienText3, rect3, fadeIn:232, stay:242, fadeOut:258, end:286),
                };
            }

            ScreenManager.SpriteBatch.Begin();
            foreach (FadeInOutAnim anim in AlienTextAnim)
            {
                if (!anim.InKeyRange(AnimationFrame))
                    continue;
                anim.Draw(ScreenManager.SpriteBatch, AnimationFrame);
                break;
            }
            ScreenManager.SpriteBatch.End();
        }

        private void DrawMoonFlare()
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            ScreenManager.GraphicsDevice.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            ScreenManager.GraphicsDevice.RenderState.BlendFunction    = BlendFunction.Add;
            ScreenManager.SpriteBatch.Draw(MoonFlare, MoonFlarePos, null, Color.White, 0f, new Vector2(184f), 0.95f, SpriteEffects.None, 1f);
            ScreenManager.SpriteBatch.End();
        }

        private void DrawComets(float elapsedTime)
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            foreach (Comet c in CometList)
            {
                float alpha = 255f;
                if (c.Position.Y > 100f)
                {
                    alpha = 25500f / c.Position.Y;
                    if (alpha > 255f)
                        alpha = 255f;
                }
                var color = new Color(255,255,255,(byte)alpha);
                ScreenManager.SpriteBatch.Draw(TexComet, c.Position, null, color, c.Rotation, TexComet.Center(), 0.45f, SpriteEffects.None, 1f);
                c.Position += c.Velocity * 2400f * elapsedTime;
                if (c.Position.Y > 1050f)
                    CometList.QueuePendingRemoval(c);
            }
            CometList.ApplyPendingRemovals();
            ScreenManager.SpriteBatch.End();
        }

        private void DrawButtonsTransition()
        {
            for (int k = Buttons.Count - 1; k >= 0; --k)
            {
                float transitionOffset = MathHelper.Clamp((TransitionPosition - 0.5f * k / (float)Buttons.Count) / 0.5f, 0f, 1f);

                Rectangle r = Buttons[k].Rect;
                r.X += (int)(transitionOffset * 512f);

                if (ScreenState == ScreenState.TransitionOn && transitionOffset.AlmostEqual(0f))
                    GameAudio.PlaySfxAsync("blip_click"); // buttons arrived!

                Buttons[k].Draw(ScreenManager.SpriteBatch, r);
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = this.GameTime;
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Rotate += elapsedTime / 350f;
            if (RandomMath.RandomBetween(0f, 100f) > 99.75)
            {
                var c = new Comet
                {
                    Position = new Vector2(RandomMath.RandomBetween(-100f, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f),
                    Velocity = new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f)
                };
                c.Velocity = Vector2.Normalize(c.Velocity);
                c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
                this.CometList.Add(c);
            }
            if (SplashScreen.DisplayComplete)
            {
                ScreenManager.HideSplashScreen();
                ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);
                DrawNew(gameTime);
                ScreenManager.RenderSceneObjects();

                Vector3 mp = Viewport.Project(MoonObj.WorldBoundingSphere.Center, Projection, View, Matrix.Identity);
                MoonFlarePos = new Vector2(mp.X - 40f - 2f, mp.Y - 40f + 24f);

                DrawMoonFlare();
                DrawAlienTextOverlays();
                DrawComets(elapsedTime);


                ScreenManager.SpriteBatch.Begin();

                DrawButtonsTransition();
                GlobalStats.ActiveMod?.DrawMainMenuOverlay(ScreenManager, Portrait);

                ScreenManager.SpriteBatch.Draw(LogoAnimation[0], LogoRect, Color.White);
                if (LogoAnimation.Count > 1)
                    LogoAnimation.RemoveAt(0);

                ScreenManager.SpriteBatch.End();

                ScreenManager.EndFrameRendering();
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
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet"), planetRect, Color.White);
            if (AnimationFrame >= 127 && AnimationFrame < 145)
            {
                float alphaStep = 255f / 18;
                float alpha = (AnimationFrame - 127) * alphaStep;
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid"), planetGridRect, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 145 && AnimationFrame <= 148)
            {
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid"), planetGridRect, Color.White);
            }
            if (AnimationFrame > 148 && AnimationFrame <= 180)
            {
                float alphaStep = 255f / 31;
                float alpha = 255f - (AnimationFrame - 148) * alphaStep;
                if (alpha < 0f) alpha = 0f;
                Rectangle planetGridRect = new Rectangle(0, height - 640, 972, 640);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid"), planetGridRect, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 141 && AnimationFrame <= 149)
            {
                float alphaStep = 255f / 9;
                float alpha = (AnimationFrame - 141) * alphaStep;
                Rectangle grid1Hex = new Rectangle(277, height - 592, 77, 33);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_1"), grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 149 && AnimationFrame <= 165)
            {
                float alphaStep = 255f / 16;
                float alpha = 255f - (AnimationFrame - 149) * alphaStep;
                Rectangle grid1Hex = new Rectangle(277, height - 592, 77, 33);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_1"), grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 159 && AnimationFrame <= 168)
            {
                float alphaStep = 255f / 10;
                float alpha = (AnimationFrame - 159) * alphaStep;
                Rectangle grid1Hex = new Rectangle(392, height - 418, 79, 60);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_2"), grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 168 && AnimationFrame <= 183)
            {
                float alphaStep = 255f / 15;
                float alpha = 255f - (AnimationFrame - 168) * alphaStep;
                Rectangle grid1Hex = new Rectangle(392, height - 418, 79, 60);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_2"), grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame >= 150 && AnimationFrame <= 158)
            {
                float alphaStep = 255f / 9;
                float alpha = (AnimationFrame - 150) * alphaStep;
                Rectangle grid1Hex = new Rectangle(682, height - 295, 63, 67);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_3"), grid1Hex, new Color(Color.White, (byte)alpha));
            }
            if (AnimationFrame > 158 && AnimationFrame <= 174)
            {
                float alphaStep = 255f / 16;
                float alpha = 255f - (AnimationFrame - 158) * alphaStep;
                Rectangle grid1Hex = new Rectangle(682, height - 295, 63, 67);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/planet_grid_hex_3"), grid1Hex, new Color(Color.White, (byte)alpha));
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
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/corner_TL"), cornerTl, new Color(Color.White, (byte)alpha));
                Rectangle cornerBr = new Rectangle(width - 551, height - 562, 520, 532);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/corner_BR"), cornerBr, new Color(Color.White, (byte)alpha));
                
                Rectangle version = new Rectangle(205, height - 37, 318, 12);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/version_bar"), version, new Color(Color.White, (byte)alpha));
                Vector2 textPos = new Vector2(20f, version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "StarDrive 15B", textPos, Color.White);

                version = new Rectangle(20+ (int)Fonts.Pirulen12.MeasureString(GlobalStats.ExtendedVersion).X , height - 85, 318, 12);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/version_bar"), version, new Color(Color.White, (byte)alpha));
                textPos = new Vector2(20f, version.Y  +6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, GlobalStats.ExtendedVersion, textPos, Color.White);

                if (GlobalStats.ActiveModInfo != null)
                {
                    string title = GlobalStats.ActiveModInfo.ModName;
                    //if (GlobalStats.ActiveModInfo.Version != null && GlobalStats.ActiveModInfo.Version != "" && !title.Contains(GlobalStats.ActiveModInfo.Version))
                    if (!string.IsNullOrEmpty(GlobalStats.ActiveModInfo.Version) && !title.Contains(GlobalStats.ActiveModInfo.Version))
                        title = string.Concat(title, " - ", GlobalStats.ActiveModInfo.Version);

                    version = new Rectangle(20 + (int)Fonts.Pirulen12.MeasureString(title).X, height - 60, 318, 12);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("MainMenu/version_bar"), version, new Color(Color.White, (byte)alpha));

                    textPos = new Vector2(20f, version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, title, textPos, Color.White);
                }
            }
            if (AnimationFrame > 300)
            {
                AnimationFrame = 0;
            }
            ScreenManager.SpriteBatch.End();


            ScreenManager.GraphicsDevice.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            ScreenManager.GraphicsDevice.RenderState.BlendFunction    = BlendFunction.Add;

            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            ScreenManager.GraphicsDevice.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            ScreenManager.GraphicsDevice.RenderState.BlendFunction    = BlendFunction.Add;
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

        public override bool HandleInput(InputState input)
        {
            // Use these controls to reorient the ship and planet in the menu. The new rotation
            // is logged into debug console and can be set as default values later
            if (DebugMeshInspect)
            {
                if (input.IsKeyDown(Keys.W)) ShipRotation.X += 1.0f;
                if (input.IsKeyDown(Keys.S)) ShipRotation.X -= 1.0f;
                if (input.IsKeyDown(Keys.A)) ShipRotation.Y += 1.0f;
                if (input.IsKeyDown(Keys.D)) ShipRotation.Y -= 1.0f;
                if (input.IsKeyDown(Keys.Q)) ShipRotation.Z += 1.0f;
                if (input.IsKeyDown(Keys.E)) ShipRotation.Z -= 1.0f;

                if (input.IsKeyDown(Keys.I)) MoonRotation.X += 1.0f;
                if (input.IsKeyDown(Keys.K)) MoonRotation.X -= 1.0f;
                if (input.IsKeyDown(Keys.J)) MoonRotation.Y += 1.0f;
                if (input.IsKeyDown(Keys.L)) MoonRotation.Y -= 1.0f;
                if (input.IsKeyDown(Keys.U)) MoonRotation.Z += 1.0f;
                if (input.IsKeyDown(Keys.O)) MoonRotation.Z -= 1.0f;

                if (input.ScrollIn)  ShipScale += 0.1f;
                if (input.ScrollOut) ShipScale -= 0.1f;

                // if new keypress, spawn random ship
                if (input.WasKeyPressed(Keys.Space))
                    InitRandomShip();

                if (input.WasAnyKeyPressed)
                    Log.Info($"rot {ShipRotation}   {MoonRotation}");
            }

            // handle buttons and stuff
            if (base.HandleInput(input))
                return true; // something was clicked, return early

            // we didn't hit any buttons or stuff, so just spawn a comet
            if (input.InGameSelect)
            {
                var c = new Comet
                {
                    Position = new Vector2(RandomMath.RandomBetween(-100f,
                        ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f)
                };
                c.Velocity = c.Position.DirectionToTarget(input.CursorPosition);
                c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
                CometList.Add(c);

                // and if we clicked on the moon, then play a cool sfx
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

            return false;
        }


        private void NewGame_Clicked(UIButton button)   => ScreenManager.AddScreen(new RaceDesignScreen(this));
        private void Tutorials_Clicked(UIButton button) => ScreenManager.AddScreen(new TutorialScreen(this));
        private void LoadGame_Clicked(UIButton button)  => ScreenManager.AddScreen(new LoadSaveScreen(this));
        private void Options_Clicked(UIButton button)   => ScreenManager.AddScreen(new OptionsScreen(this));
        private void Mods_Clicked(UIButton button)      => ScreenManager.AddScreen(new ModManager(this));
        private void Info_Clicked(UIButton button)      => ScreenManager.AddScreen(new InGameWiki(this));
        private void VerCheck_Clicked(UIButton button)  => ScreenManager.AddScreen(new VersionChecking(this));
        private void Exit_Clicked(UIButton button)      => Game1.Instance.Exit();
        private void ShipTool_Clicked(UIButton button)  => ScreenManager.AddScreen(new ShipToolScreen(this));
        private void DevSandbox_Clicked(UIButton button)  => ScreenManager.AddScreen(new DeveloperSandbox(this));

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();

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

            BeginVLayout(size.X - 200, size.Y / 2 - 100, UIButton.StyleSize().Y + 15);
                Button(titleId: 1,      click: NewGame_Clicked);
                Button(titleId: 3,      click: Tutorials_Clicked);
                Button(titleId: 2,      click: LoadGame_Clicked);
                Button(titleId: 4,      click: Options_Clicked);
                Button("Mods",          click: Mods_Clicked);
                Button("Dev Sandbox",   click: DevSandbox_Clicked);
                Button("BlackBox Info", click: Info_Clicked);
                Button("Version Check", click: VerCheck_Clicked);
            Button(titleId: 5,      click: Exit_Clicked);
            EndLayout();

            ScreenManager.ClearScene();

            // @todo Why are these global inits here??
            ShieldManager.LoadContent(Game1.GameContent);
            Beam.BeamEffect = Game1.GameContent.Load<Effect>("Effects/BeamFX");
            BackgroundItem.QuadEffect = new BasicEffect(ScreenManager.GraphicsDevice, null)
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
            ShipPosition = new Vector3(-size.X / 4, LogoRect.Y + 400 - size.Y / 2, 0f);

            string planet = "Model/SpaceObjects/planet_" + RandomMath.IntBetween(1, 29);
            MoonObj = new SceneObject(TransientContent.Load<Model>(planet).Meshes[0]) { ObjectType = ObjectType.Dynamic };
            MoonObj.AffineTransform(MoonPosition, MoonRotation.DegsToRad(), MoonScale);
            ScreenManager.AddObject(MoonObj);

            InitRandomShip();

            AssignLightRig("example/ShipyardLightrig");
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

            Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateTranslation(0f, 0f, 0f) 
                * Matrix.CreateRotationY(180f.ToRadians())
                * Matrix.CreateRotationX(0f.ToRadians())
                * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));

            Projection = Matrix.CreateOrthographic(size.X, size.Y, 1f, 80000f);

            LoadTestContent();

            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        // for quick feature testing in the main menu
        private void LoadTestContent()
        {
            //var atlas = TextureAtlas.Load(ScreenManager.Content, "Explosions/smaller/shipExplosion");
            //ResetMusic();
        }

        public void OnPlaybackStopped(object sender, EventArgs e)
        {
            if (WaveOut == null) return;
            WaveOut.Dispose();
            Mp3FileReader.Dispose();
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
                RemoveObject(ShipObj);
                ShipObj.Clear();
                ShipObj = null;
                ShipAnim = null;
            }

            // FrostHand: do we actually need to show Model/Ships/speeder/ship07 in base version? Or could show random ship for base and modded version?
            if (GlobalStats.HasMod && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = RandomMath.InRange(ResourceManager.MainMenuShipList.ModelPaths.Count);
                string modelPath = ResourceManager.MainMenuShipList.ModelPaths[shipIndex];
                ShipObj = ResourceManager.GetSceneMesh(TransientContent, modelPath);
            }
            else if (DebugMeshInspect)
            {
                ShipObj = ResourceManager.GetSceneMesh(TransientContent, "Model/TestShips/Soyo/Soyo.obj");
                //ShipObj = ResourceManager.GetSceneMesh("Model/TestShips/SciFi-MK6/MK6_OBJ.obj");
            }
            else
            {
                ShipData[] hulls = ResourceManager.HullsDict.Values.Where(s
                    => s.Role == ShipData.RoleName.frigate
                        //|| s.Role == ShipData.RoleName.cruiser
                        //|| s.Role == ShipData.RoleName.capital
                        //&& s.ShipStyle != "Remnant"
                        && s.ShipStyle != "Ralyeh").ToArray(); // Ralyeh ships look disgusting in the menu
                ShipData hull = hulls[RandomMath.InRange(hulls.Length)];

                ShipObj = ResourceManager.GetSceneMesh(TransientContent, hull.ModelPath, hull.Animated);
                if (hull.Animated) // Support animated meshes if we use them at all
                {
                    SkinnedModel model = TransientContent.LoadSkinnedModel(hull.ModelPath);
                    ShipAnim = new AnimationController(model.SkeletonBones);
                    ShipAnim.StartClip(model.AnimationClips["Take 001"]);
                }
            }

            // we want mainmenu ships to have a certain acceptable size:
            if (!DebugMeshInspect)
                ShipScale = 266f / ShipObj.ObjectBoundingSphere.Radius;
            else
                ShipScale = 1024f / ShipObj.ObjectBoundingSphere.Radius;

            //var bb = ShipObj.GetMeshBoundingBox();
            //float length = bb.Max.Z - bb.Min.Z;
            //float width  = bb.Max.X - bb.Min.X;
            //float height = bb.Max.Y - bb.Min.Y;
            Log.Info($"ship width: {ShipObj.ObjectBoundingSphere.Radius*2}  scale: {ShipScale}");

            ShipObj.AffineTransform(ShipPosition, ShipRotation.DegsToRad(), ShipScale);
            AddObject(ShipObj);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ScreenManager.UpdateSceneObjects(gameTime);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MoonPosition.X += deltaTime * 0.6f; // 0.6 units/s
            MoonRotation.Y += deltaTime * 1.2f;
            MoonObj.AffineTransform(MoonPosition, MoonRotation.DegsToRad(), MoonScale);

            if (!DebugMeshInspect)
            {
                // slow moves the ship across the screen
                ShipRotation.Y += deltaTime * 0.06f;
                ShipPosition   += deltaTime * -ShipRotation.DegreesToUp() * 1.5f; // move forward 1.5 units/s
            }
            else
            {
                ShipPosition = new Vector3(0f, 0f, 0f);
            }

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

            ScreenManager.UpdateSceneObjects(gameTime);

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

        protected override void Destroy()
        {
            CometList?.Dispose(ref CometList);
            WaveOut?.Dispose(ref WaveOut);
            Mp3FileReader?.Dispose(ref Mp3FileReader);
            base.Destroy();
        }
    }
}
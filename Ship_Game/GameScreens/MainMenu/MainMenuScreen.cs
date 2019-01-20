using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Wave;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.GameScreens.MainMenu
{
    public sealed class MainMenuScreen : GameScreen
    {
        IWavePlayer WaveOut;
        Mp3FileReader Mp3FileReader;
        SpriteAnimation SDLogoAnim;

        SceneObject MoonObj;
        Vector3 MoonPosition;
        Vector3 MoonRotation = new Vector3(264f, 198, 15f);
        const float MoonScale = 0.7f;
        SceneObject ShipObj;

        Vector3 ShipPosition;
        Vector3 ShipRotation = new Vector3(-116f, -188f, -19f);
        float ShipScale = MoonScale * 1.75f;

        AnimationController ShipAnim;

        Rectangle Portrait;

        bool DebugMeshInspect = false;
        
        Texture2D StarField;
        SubTexture Vignette;
        SubTexture BackgroundPlanet;
        SubTexture TexComet, MoonGlow;
        Vector2 MoonGlowPos;

        UIElementContainer VersionArea;

        public MainMenuScreen() : base(null /*no parent*/)
        {
            TransitionOnTime  = TimeSpan.FromSeconds(1);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        protected override void Destroy()
        {
            WaveOut?.Dispose(ref WaveOut);
            Mp3FileReader?.Dispose(ref Mp3FileReader);
            base.Destroy();
        }
        
        SubTexture LoadTexture(string path) => TransientContent.Load<SubTexture>(path);

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            ScreenManager.ClearScene();
            GameAudio.ConfigureAudioSettings();
            ResetMusic();

            SDLogoAnim = Add(new SpriteAnimation(this, "MainMenu/Stardrive logo"));
            SDLogoAnim.StopAtLastFrame = true;
            SDLogoAnim.Rect = new Rectangle(ScreenWidth - 600, 128, 512, 128);

            BackgroundPlanet = LoadTexture("Textures/MainMenu/planet");
            Vignette    = LoadTexture("Textures/MainMenu/vignette");
            TexComet = LoadTexture("Textures/GameScreens/comet2");
            MoonGlow = LoadTexture("Textures/MainMenu/moon_flare");
            
            GlobalStats.ActiveMod?.LoadContent(TransientContent);

            int w = ScreenWidth, h = ScreenHeight;

            string nebula = h <= 1080 ? "MainMenu/nebula_stars_bg" : "MainMenu/HR_nebula_stars_bg";
            StarField = TransientContent.Load<Texture2D>(nebula);

            BeginVLayout(w - 200, h / 2 - 100, UIButton.StyleSize().Y + 15);
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

            // Animate the buttons in and out
            StartTransition<UIButton>(512f, -1f);
            OnExit += () => StartTransition<UIButton>(512f, +1f);

            Portrait = new Rectangle(w / 2 - 960, h / 2 - 540, 1920, 1080);
            while (Portrait.Width < w && Portrait.Height < h)
            {
                Portrait.Width  += 12;
                Portrait.Height += 7;
                Portrait.X = w / 2 - Portrait.Width  / 2;
                Portrait.Y = h / 2 - Portrait.Height / 2;
            }

            MoonPosition = new Vector3(+w / 2f - 300, SDLogoAnim.Y + 70 - h / 2f, 0f);
            ShipPosition = new Vector3(-w / 4f, SDLogoAnim.Y + 400 - h / 2f, 0f);

            PlanetType planetType = ResourceManager.RandomPlanet();
            string planet = planetType.MeshPath;
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

            Projection = Matrix.CreateOrthographic(w, h, 1f, 80000f);
            
            Vector3 mp = Viewport.Project(MoonObj.WorldBoundingSphere.Center, Projection, View, Matrix.Identity);
            MoonGlowPos = new Vector2(mp.X - 40f - 2f, mp.Y - 40f + 24f);

            CreateAnimatedOverlays();
            CreateVersionArea();

            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        void CreateAnimatedOverlays()
        {
            int h = ScreenHeight;
            // alien text markers flashing on top of right hand side moon
            int mx = (int) MoonGlowPos.X, my = (int) MoonGlowPos.Y;
            
            const float moonLoop = 12.0f; // total animation loop sync time
            Panel("MainMenu/moon_1", mx - 220, my - 130).Anim(1.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);
            Panel("MainMenu/moon_2", mx - 250, my + 60).Anim(5.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);
            Panel("MainMenu/moon_3", mx + 60,  my + 80).Anim(7.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);

            // flashing planet hex grid overlays
            const float hexLoop = 10.0f;
            Panel("MainMenu/planet_grid",         0, h-640).InBackground().Anim(4.0f, 3.0f, 0.6f, 1.2f).Alpha().Loop(hexLoop);
            Panel("MainMenu/planet_grid_hex_1", 277, h-592).InBackground().Anim(4.7f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);
            Panel("MainMenu/planet_grid_hex_2", 392, h-418).InBackground().Anim(5.7f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);
            Panel("MainMenu/planet_grid_hex_3", 682, h-295).InBackground().Anim(5.2f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);

            Panel("MainMenu/planet_solarflare", 0, h - 784)
                .InBackAdditive() // behind 3d objects
                .Anim().Loop(4.0f, 1.5f, 1.5f).Color(Color.White.MultiplyRgb(0.85f), Color.White);

            Panel("MainMenu/corner_TL", 31, 30).Anim(2f, 6f, 1f, 1f).Alpha(0.5f).Loop(hexLoop);
            Panel("MainMenu/corner_BR", ScreenWidth-551, h-562).Anim(3f, 6f, 1f, 1f).Alpha(0.5f).Loop(hexLoop);
        }

        void CreateVersionArea()
        {
            VersionArea = Panel(Rectangle.Empty, Color.TransparentBlack);
            VersionArea.StartFadeIn(3.0f, delay: 2.0f);

            VersionArea.Add(new VersionLabel(this, 300, ScreenHeight - 90, "StarDrive 16A"));
            VersionArea.Add(new VersionLabel(this, 300, ScreenHeight - 64, GlobalStats.ExtendedVersion));
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ActiveModInfo.ModName;
                string version = GlobalStats.ActiveModInfo.Version;
                if (version.NotEmpty() && !title.Contains(version))
                    title = title+" - "+version;
                VersionArea.Add(new VersionLabel(this, 300, ScreenHeight - 38, title));
            }
        }

        void NewGame_Clicked(UIButton button)   => ScreenManager.AddScreen(new RaceDesignScreen(this));
        void Tutorials_Clicked(UIButton button) => ScreenManager.AddScreen(new TutorialScreen(this));
        void LoadGame_Clicked(UIButton button)  => ScreenManager.AddScreen(new LoadSaveScreen(this));
        void Options_Clicked(UIButton button)   => ScreenManager.AddScreen(new OptionsScreen(this));
        void Mods_Clicked(UIButton button)      => ScreenManager.AddScreen(new ModManager(this));
        void Info_Clicked(UIButton button)      => ScreenManager.AddScreen(new InGameWiki(this));
        void VerCheck_Clicked(UIButton button)  => ScreenManager.AddScreen(new VersionChecking(this));
        void ShipTool_Clicked(UIButton button)  => ScreenManager.AddScreen(new ShipToolScreen(this));
        void DevSandbox_Clicked(UIButton button)=> ScreenManager.GoToScreen(new DeveloperSandbox(), clear3DObjects:true);
        void Exit_Clicked(UIButton button)      => ExitScreen();

        public override void Draw(SpriteBatch batch)
        {
            if (!SplashScreen.DisplayComplete)
                return;

            ScreenManager.HideSplashScreen();
            ScreenManager.BeginFrameRendering(GameTime, ref View, ref Projection);
            
            // BACK NORMAL
            batch.Begin();
            batch.Draw(StarField, ScreenRect, Color.White); // background
            batch.Draw(BackgroundPlanet, new Rectangle(0, ScreenHeight - 680, 1016, 680), Color.White); // big planet at left side
            DrawElementsAtDepth(batch, DrawDepth.Background);
            GlobalStats.ActiveMod?.DrawMainMenuOverlay(batch, Portrait);
            batch.Draw(Vignette, ScreenRect, Color.White);
            batch.End();
            
            // BACK ADD
            BeginAdditive(batch);
            DrawElementsAtDepth(batch, DrawDepth.BackAdditive);
            batch.End();

            // 3D render
            ScreenManager.RenderSceneObjects();

            // FORE NORMAL
            batch.Begin();
            DrawElementsAtDepth(batch, DrawDepth.Foreground);
            batch.End();

            // FORE ADD
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            Device.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            Device.RenderState.DestinationBlend = Blend.One;
            Device.RenderState.BlendFunction    = BlendFunction.Add;
            batch.Draw(MoonGlow, MoonGlowPos, Color.White, 0f, new Vector2(184f), 0.95f, SpriteEffects.None, 1f);
            batch.End();

            BeginAdditive(batch);
            DrawElementsAtDepth(batch, DrawDepth.ForeAdditive);
            batch.End();

            ScreenManager.EndFrameRendering();

            //DrawMultiLayeredExperimental(ScreenManager);
        }

        public override bool HandleInput(InputState input)
        {
            // Use these controls to reorient the ship and planet in the menu. The new rotation
            // is logged into debug console and can be set as default values later
            if (DebugMeshInspect && IsActive)
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
                Comet c = Add(new Comet(this, TexComet));
                c.SetDirection(c.Pos.DirectionToTarget(input.CursorPosition));

                // and if we clicked on the moon, then play a cool sfx
                Viewport viewport = Viewport;
                Vector3 nearPoint = viewport.Unproject(new Vector3(input.CursorPosition, 0f), Projection, View, Matrix.Identity);
                Vector3 farPoint  = viewport.Unproject(new Vector3(input.CursorPosition, 1f), Projection, View, Matrix.Identity);
                Vector3 direction = nearPoint.DirectionToTarget(farPoint);

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

        public void OnPlaybackStopped(object sender, EventArgs e)
        {
            if (WaveOut == null) return;
            WaveOut?.Dispose(ref WaveOut);
            Mp3FileReader?.Dispose(ref Mp3FileReader);
        }

        void PlayMp3(string fileName)
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

        void InitRandomShip()
        {
            if (ShipObj != null) // Allow multiple init
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
                ShipData[] hulls = ResourceManager.Hulls.Filter(s
                    => s.Role == ShipData.RoleName.frigate
                        //|| s.Role == ShipData.RoleName.cruiser
                        //|| s.Role == ShipData.RoleName.capital
                        //&& s.ShipStyle != "Remnant"
                        && s.ShipStyle != "Ralyeh"); // Ralyeh ships look disgusting in the menu
                ShipData hull = hulls[RandomMath.InRange(hulls.Length)];

                ShipObj = ResourceManager.GetSceneMesh(TransientContent, hull.ModelPath, hull.Animated);
                if (hull.Animated) // Support animated meshes if we use them at all
                {
                    SkinnedModel model = TransientContent.LoadSkinnedModel(hull.ModelPath);
                    ShipAnim = new AnimationController(model.SkeletonBones);
                    ShipAnim.StartClip(model.AnimationClips["Take 001"]);
                }
            }

            // we want main menu ships to have a certain acceptable size:
            ShipScale = 266f / ShipObj.ObjectBoundingSphere.Radius;
            if (DebugMeshInspect) ShipScale *= 4.0f;

            Log.Info($"ship width: {ShipObj.ObjectBoundingSphere.Radius*2}  scale: {ShipScale}");

            ShipObj.AffineTransform(ShipPosition, ShipRotation.DegsToRad(), ShipScale);
            AddObject(ShipObj);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ScreenManager.UpdateSceneObjects(gameTime);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MoonRotation.X -= deltaTime * 0.3f; // degrees/s
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
            
            if (RandomMath.RandomBetween(0f, 100f) > 99.75f)
            {
                Comet c = Add(new Comet(this, TexComet));
                c.SetDirection(new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f));
            }

            if (!GlobalStats.HasMod || GlobalStats.ActiveMod.MainMenuMusic.IsEmpty())
            {
                if (!IsExiting && ScreenManager.Music.IsStopped)
                    ResetMusic();
            }

            if (IsExiting && TransitionPosition >= 0.99f && ScreenManager.Music.IsPlaying)
            {
                ScreenManager.Music.Stop();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}
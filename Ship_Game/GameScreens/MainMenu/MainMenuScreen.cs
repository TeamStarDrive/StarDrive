using System;
using System.IO;
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
        UISpriteElement SDLogoAnim;

        SceneObject MoonObj;
        Vector3 MoonPosition;
        Vector3 MoonRotation = new Vector3(264f, 198, 15f);
        const float MoonScale = 0.7f;
        SceneObject ShipObj;

        Vector3 ShipPosition;
        Vector3 ShipRotation = new Vector3(-116f, -188f, -19f);
        float ShipScale = MoonScale * 1.75f;

        AnimationController ShipAnim;

        bool DebugMeshInspect = false;
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
        
        public class MainMenuLayout
        {
            public bool ShowMoon = true;
            public Vector2 MoonText1 = new Vector2(-220, -130);
            public Vector2 MoonText2 = new Vector2(-250, +60);
            public Vector2 MoonText3 = new Vector2(+60, +80);
            public bool ShowSDLogo = true;
            public bool FreezeSDLogo = true;
            public Vector2 SDLogoPosition = new Vector2(-600, 128);
            public bool ShowPlanet = true;
            public bool ShowPlanetGrid = true;
            public bool ShowPlanetFlare = true;
            public Vector2 PlanetPosition = new Vector2(0, -680);
            public Vector2 PlanetGrid = new Vector2(0, -640);
            public Vector2 PlanetHex1 = new Vector2(277, -592);
            public Vector2 PlanetHex2 = new Vector2(392, -418);
            public Vector2 PlanetHex3 = new Vector2(682, -295);
            public Vector2 PlanetSolarFlare = new Vector2(0, -784);
            public Vector2 CornerTL = new Vector2(31, 30);
            public Vector2 CornerBR = new Vector2(-551, -562);
            public Vector2 ButtonsStart = new Vector2(-200, 0.4f);
        }

        static void OnModChanged(FileInfo info)
        {
            if (!GlobalStats.HasMod) return;
            GlobalStats.LoadModInfo(GlobalStats.ModName);
            if (ScreenManager.CurrentScreen is MainMenuScreen mainMenu)
            {
                mainMenu.LoadContent();
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            ScreenManager.ClearScene();
            GameAudio.ConfigureAudioSettings();
            ResetMusic();

            MainMenuLayout layout = GlobalStats.HasMod ? GlobalStats.ActiveModInfo.Layout : new MainMenuLayout();
            if (GlobalStats.HasMod)
                ScreenManager.AddHotLoadTarget("Mod", GlobalStats.ModFile, OnModChanged);

            // Confusing: Main menu background is in `Content/MainMenu` not `Content/Textures/MainMenu`
            SubTexture nebula = TransientContent.LoadSubTexture("MainMenu/HR_nebula_stars_bg");
            Panel(nebula, ScreenRect).InBackground(); // fill background

            if (layout.ShowPlanet)
                Panel("MainMenu/planet", layout.PlanetPosition).InBackground(); // big planet background

            if (GlobalStats.HasMod)
            {
                GlobalStats.ActiveMod.LoadContent(this);
                Add(GlobalStats.ActiveMod).InBackground();
            }

            Panel("MainMenu/vignette", ScreenRect); // vignette goes on top of everything
            BeginVLayout(layout.ButtonsStart, UIButton.StyleSize().Y + 15);
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
            
            SDLogoAnim = Add(new UISpriteElement(this, "MainMenu/Stardrive logo"));
            SDLogoAnim.Animation.FreezeAtLastFrame = layout.FreezeSDLogo;
            SDLogoAnim.Animation.Looping = !layout.FreezeSDLogo;
            SDLogoAnim.SetAbsPos(layout.SDLogoPosition);
            SDLogoAnim.Visible = layout.ShowSDLogo;
            
            MoonPosition = new Vector3(+ScreenWidth / 2f - 300, 198 - ScreenHeight / 2f, 0f);
            ShipPosition = new Vector3(-ScreenWidth / 4f, 528 - ScreenHeight / 2f, 0f);

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
            Projection = Matrix.CreateOrthographic(ScreenWidth, ScreenHeight, 1f, 80000f);

            Vector2 moonCenter = Viewport.Project(MoonObj.WorldBoundingSphere.Center, Projection, View, Matrix.Identity).ToVec2();

            if (layout.ShowMoon)
            {
                // @todo place automatically depending on planet size?
                UIPanel flare = Panel("MainMenu/moon_flare").InForeAdditive();
                flare.Size *= 0.95f;
                flare.Pos = moonCenter - new Vector2(216f, 193f);
            }
            else
            {
                MoonObj.Visibility = ObjectVisibility.None;
            }

            CreateAnimatedOverlays(moonCenter, layout);
            CreateVersionArea();

            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        void CreateAnimatedOverlays(Vector2 moon, MainMenuLayout layout)
        {
            // alien text markers flashing on top of right hand side moon
            const float moonLoop = 12.0f; // total animation loop sync time
            Panel("MainMenu/moon_1", moon+layout.MoonText1).Anim(1.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);
            Panel("MainMenu/moon_2", moon+layout.MoonText2).Anim(5.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);
            Panel("MainMenu/moon_3", moon+layout.MoonText3).Anim(7.5f, 2.0f, 0.4f, 0.7f).Alpha().Loop(moonLoop);

            // flashing planet hex grid overlays
            const float hexLoop = 10.0f;
            if (layout.ShowPlanetGrid)
            {
                Panel("MainMenu/planet_grid",       layout.PlanetGrid).InBackground().Anim(4.0f, 3.0f, 0.6f, 1.2f).Alpha().Loop(hexLoop);
                Panel("MainMenu/planet_grid_hex_1", layout.PlanetHex1).InBackground().Anim(4.7f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);
                Panel("MainMenu/planet_grid_hex_2", layout.PlanetHex2).InBackground().Anim(5.7f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);
                Panel("MainMenu/planet_grid_hex_3", layout.PlanetHex3).InBackground().Anim(5.2f, 0.9f, 0.3f, 0.5f).Alpha().Loop(hexLoop);
            }
            if (layout.ShowPlanetFlare)
            {
                Panel("MainMenu/planet_solarflare", layout.PlanetSolarFlare)
                    .InBackAdditive() // behind 3d objects
                    .Anim().Loop(4.0f, 1.5f, 1.5f).Color(Color.White.MultiplyRgb(0.85f), Color.White);
            }

            Panel("MainMenu/corner_TL", layout.CornerTL).Anim(2f, 6f, 1f, 1f).Alpha(0.5f).Loop(hexLoop).Sine();
            Panel("MainMenu/corner_BR", layout.CornerBR).Anim(3f, 6f, 1f, 1f).Alpha(0.5f).Loop(hexLoop).Sine();
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
            DrawMultiLayeredExperimental(ScreenManager, draw3D:true);
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

            if (input.DebugMode)
            {
                LoadContent();
                return true;
            }

            // we didn't hit any buttons or stuff, so just spawn a comet
            if (input.InGameSelect)
            {
                Comet c = Add(new Comet(this));
                c.SetDirection(c.Pos.DirectionToTarget(input.CursorPosition));

                // and if we clicked on the moon, then play a cool sfx
                Vector3 nearPoint = Viewport.Unproject(new Vector3(input.CursorPosition, 0f), Projection, View, Matrix.Identity);
                Vector3 farPoint  = Viewport.Unproject(new Vector3(input.CursorPosition, 1f), Projection, View, Matrix.Identity);
                Vector3 direction = nearPoint.DirectionToTarget(farPoint);

                var pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                var picked = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, 
                                         pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                if (picked.InRadius(MoonObj.WorldBoundingSphere.Center, MoonObj.WorldBoundingSphere.Radius))
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
            
            if (RandomMath.RollDice(percent:0.25f)) // 0.25%
            {
                Comet c = Add(new Comet(this));
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
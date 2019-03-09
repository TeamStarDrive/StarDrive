using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.SpriteSystem;
using Ship_Game.UI;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace Ship_Game.GameScreens.MainMenu
{
    public sealed class MainMenuScreen : GameScreen
    {
        readonly MainMenuShip Ship;
        UIElementContainer VersionArea;

        public MainMenuScreen() : base(null /*no parent*/)
        {
            TransitionOnTime  = 1.0f;
            TransitionOffTime = 0.5f;
            Ship = new MainMenuShip(this);
        }
        
        static void OnModChanged(FileInfo info)
        {
            if (!GlobalStats.HasMod)
                return;
            GlobalStats.LoadModInfo(GlobalStats.ModName);
            if (ScreenManager.CurrentScreen is MainMenuScreen mainMenu)
                mainMenu.ReloadContent();
        }

        public override void LoadContent()
        {
            ScreenManager.ClearScene();
            ResetMusic();
            SetupMainMenuLightRig();

            LayoutParser.LoadLayout(this, "UI/MainMenu.yaml", clearElements: true);

            if (GlobalStats.HasMod)
            {
                ScreenManager.AddHotLoadTarget("Mod", GlobalStats.ModFile, OnModChanged);
            }

            Find("planet", out UIPanel planet);

            if (GlobalStats.HasMod)
            {
                GlobalStats.ActiveMod.LoadContent(this);
                Add(GlobalStats.ActiveMod).InBackground();
            }

            if (!Find("buttons", out UIList list))
                list = List(Vector2.Zero);
            if (list.Find("new_game",  out UIButton newGame))   newGame.OnClick   = NewGame_Clicked;
            if (list.Find("tutorials", out UIButton tutorials)) tutorials.OnClick = Tutorials_Clicked;
            if (list.Find("load_game", out UIButton loadGame))  loadGame.OnClick  = LoadGame_Clicked;
            if (list.Find("options",   out UIButton options))   options.OnClick   = Options_Clicked;
            if (list.Find("mods",      out UIButton mods))      mods.OnClick    = Mods_Clicked;
            if (list.Find("sandbox",   out UIButton sandbox))   sandbox.OnClick = DevSandbox_Clicked;
            if (list.Find("info",      out UIButton info))      info.OnClick    = Info_Clicked;
            if (list.Find("version",   out UIButton version))   version.OnClick = VerCheck_Clicked;
            if (list.Find("exit",      out UIButton exit))      exit.OnClick    = Exit_Clicked;
            list.PerformLayout();

            // Animate the buttons in and out
            var animOffset = new Vector2(512f * (ScreenWidth / 1920f), 0);
            list.StartTransition<UIButton>(animOffset, -1, time:0.5f);
            OnExit += () => list.StartTransition<UIButton>(animOffset, +1, time:0.5f);
            
            Ship.InitRandomShip();

            var camPos = new Vector3(0f, 0f, -1000f);
            var lookAt = new Vector3(0f, 0f, 10000f);
            View = Matrix.CreateLookAt(camPos, lookAt, Vector3.Down);
            Projection = Matrix.CreatePerspectiveFieldOfView(0.785f, Viewport.AspectRatio, 10f, 35000f);


            if (Find("blacbox_animated_logo", out UIPanel logo))
            {
                if (Find("logo_text_1", out UIPanel text1)) text1.Pos += logo.Center;
                if (Find("logo_text_2", out UIPanel text2)) text2.Pos += logo.Center;
                if (Find("logo_text_3", out UIPanel text3)) text3.Pos += logo.Center;
            }

            CreateVersionArea();

            //UIGraphView graph = Add(new UIGraphView()
            //{
            //    Name = "test_graph",
            //    Color = Color.TransparentBlack,
            //    Pos = new Vector2(550, Bottom - 500),
            //    Size = new Vector2(500, 250),
            //});
            //var curve = new AnimationCurve(new [] 
            //{
            //    (0.0f, 0.0f),
            //    (0.5f, 1.0f),
            //    (0.7f, 0.2f),
            //    (1.0f, 0.0f),
            //});
            //curve.DrawCurveTo(graph, 0, 1, 0.005f);

            base.LoadContent();
            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        void SetupMainMenuLightRig()
        {
            //AssignLightRig("example/ShipyardLightrig");
            ScreenManager.RemoveAllLights();
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

            var topRightInBackground = new Vector3(2000,-1000,1000);
            var lightYellow = new Color(255,254,224);
            AddLight("MainMenu Sun", lightYellow, 2.0f, topRightInBackground);

            AddLight(new AmbientLight
            {
                Name = "MainMenu AmbientFill",
                DiffuseColor = new Color(20, 30, 55).ToVector3(), // dark violet
                Intensity = 1,
            });
        }

        void AddLight(string name, Color color, float intensity, Vector3 position)
        {
            var light = new PointLight
            {
                Name                = name,
                DiffuseColor        = color.ToVector3(),
                Intensity           = intensity,
                ObjectType          = ObjectType.Static, // RedFox: changed this to Static
                FillLight           = false,
                Radius              = 100000,
                Position            = position,
                Enabled             = true,
                FalloffStrength     = 1f,
                ShadowPerSurfaceLOD = true,
                ShadowQuality = 1f
            };

            light.ShadowType = ShadowType.AllObjects;
            light.World = Matrix.CreateTranslation(light.Position);
            AddLight(light);
        }

        void CreateVersionArea()
        {
            VersionArea = Panel(Rectangle.Empty, Color.TransparentBlack);
            VersionArea.StartFadeIn(3.0f, delay: 2.0f);

            VersionArea.Add(new VersionLabel(this, 300, ScreenHeight - 90, "StarDrive 15B"));
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
        void DevSandbox_Clicked(UIButton button)=> ScreenManager.GoToScreen(new DeveloperSandbox(), clear3DObjects: true);
        void Exit_Clicked(UIButton button)      => ExitScreen();

        public override void Draw(SpriteBatch batch)
        {
            DrawMultiLayeredExperimental(ScreenManager, draw3D:true);
            Ship?.Draw(batch);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            Ship?.HandleInput(input);

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

                // and if we clicked on logo, then play a cool sfx
                if (Find("blacbox_animated_logo", out UIPanel panel) && panel.Rect.HitTest(input.CursorPosition))
                {
                    GameAudio.PlaySfxAsync("sd_bomb_impact_01");
                }
            }

            return false;
        }

        public void ResetMusic()
        {
            GameAudio.ConfigureAudioSettings();
            GameAudio.StopGenericMusic();
            ScreenManager.Music.Stop();

            if (GlobalStats.HasMod && GlobalStats.ActiveMod.MainMenuMusic.NotEmpty())
            {
                ScreenManager.Music = GameAudio.PlayMp3(GlobalStats.ModPath + GlobalStats.ActiveMod.MainMenuMusic);
            }
            else if (ScreenManager.Music.IsStopped)
            {
                ScreenManager.Music = GameAudio.PlayMusic("SD_Theme_Reprise_06");
            }
        }



        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            Ship?.Update(gameTime);
            ScreenManager.UpdateSceneObjects(gameTime);
            
            if (RandomMath.RollDice(percent:0.25f)) // 0.25% (very rare event)
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
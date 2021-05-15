using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;
using Ship_Game.UI;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace Ship_Game.GameScreens.MainMenu
{
    public sealed class MainMenuScreen : GameScreen
    {
        readonly Array<MenuFleet> Fleets = new Array<MenuFleet>();
        UIElementContainer VersionArea;
        Vector3 CamPos;

        public MainMenuScreen() : base(null /*no parent*/)
        {
            CanEscapeFromScreen = false;
            TransitionOnTime  = 1.0f;
            TransitionOffTime = 0.5f;
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
                ScreenManager.AddHotLoadTarget(this, "Mod", GlobalStats.ModFile, OnModChanged);
            }

            Find("planet", out UIPanel planet);

            if (GlobalStats.HasMod)
            {
                GlobalStats.ActiveMod.LoadContent(this);
                Add(GlobalStats.ActiveMod).InBackground();
            }

            if (!Find("buttons", out UIList list))
                list = AddList(Vector2.Zero);
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
            list.StartGroupTransition<UIButton>(animOffset, -1, time:0.5f);
            OnExit += () => list.StartGroupTransition<UIButton>(animOffset, +1, time:0.5f);
            
            FTLManager.LoadContent(this);
            CreateMainMenuFleet();

            CamPos = new Vector3(0f, 0f, -1000f);
            var lookAt = new Vector3(0f, 0f, 10000f);
            View = Matrix.CreateLookAt(CamPos, lookAt, Vector3.Down);
            Projection = Matrix.CreatePerspectiveFieldOfView(0.785f, Viewport.AspectRatio, 10f, 35000f);

            if (Find("blacbox_animated_logo", out UIPanel logo))
            {
                if (Find("logo_text_1", out UIPanel text1)) text1.Pos += logo.Center;
                if (Find("logo_text_2", out UIPanel text2)) text2.Pos += logo.Center;
                if (Find("logo_text_3", out UIPanel text3)) text3.Pos += logo.Center;
            }

            CreateVersionArea();

            base.LoadContent();
            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        void SetupMainMenuLightRig()
        {
            //AssignLightRig("example/ShipyardLightrig");
            ScreenManager.RemoveAllLights();
            ScreenManager.LightRigIdentity = LightRigIdentity.MainMenu;
            ScreenManager.Environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

            var topRightInBackground = new Vector3(26000,-26000,32000);
            var lightYellow = new Color(255,254,224);
            AddLight("MainMenu Sun", lightYellow, 2.0f, topRightInBackground);

            AddLight(new AmbientLight
            {
                Name = "MainMenu AmbientFill",
                DiffuseColor = new Color(40, 60, 110).ToVector3(), // dark violet
                Intensity = 0.75f,
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

            string starDrive = "StarDrive 15B";
            string blackBox = GlobalStats.ExtendedVersionNoHash;
            string modTitle = "";
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ActiveModInfo.ModName;
                string version = GlobalStats.ActiveModInfo.Version;
                if (version.NotEmpty() && !title.Contains(version))
                    modTitle = title+" - "+version;
            }

            string longest = new []{starDrive,blackBox,modTitle}.FindMax(s => s.Length);
            int offset = Math.Max(300, Fonts.Pirulen12.TextWidth(longest).RoundUpToMultipleOf(10) + 10);
            VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 90, starDrive));
            VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 64, blackBox));
            if (modTitle.NotEmpty())
                VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 38, modTitle));
        }

        void CreateMainMenuFleet()
        {
            Fleets.Clear();
            Array<MenuFleet> fleets = YamlParser.DeserializeArray<MenuFleet>("MainMenuFleets.yaml");
            foreach (MenuFleet fleet in fleets)
            {
                fleet.CreateShips(this);
                if (fleet.FleetShips.NotEmpty)
                    Fleets.Add(fleet);
            }
        }
        
        void UpdateMainMenuShips(FixedSimTime timeStep)
        {
            foreach (MenuFleet fleet in Fleets)
            {
                fleet.Update(this, timeStep);
            }
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

        void NewGame_Clicked(UIButton button)   => ScreenManager.AddScreen(new RaceDesignScreen(this));
        void Tutorials_Clicked(UIButton button) => ScreenManager.AddScreen(new TutorialScreen(this));
        void LoadGame_Clicked(UIButton button)  => ScreenManager.AddScreen(new LoadSaveScreen(this));
        void Options_Clicked(UIButton button)   => ScreenManager.AddScreen(new OptionsScreen(this));
        void Mods_Clicked(UIButton button)      => ScreenManager.AddScreen(new ModManager(this));
        void Info_Clicked(UIButton button)      => ScreenManager.AddScreen(new InGameWiki(this));
        void VerCheck_Clicked(UIButton button)  => ScreenManager.AddScreen(new VersionChecking(this));
        void ShipTool_Clicked(UIButton button)  => ScreenManager.AddScreen(new ShipToolScreen());
        void DevSandbox_Clicked(UIButton button)=> ScreenManager.GoToScreen(new DeveloperSandbox(), clear3DObjects: true);
        void Exit_Clicked(UIButton button)      => ExitScreen();


        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            foreach (var fleet in Fleets)
                fleet.HandleInput(input, this);

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

        // We need a simulation time accumulator in order to run sim at arbitrary X fps while UI runs at smooth 60 fps
        float SimTimeSink;

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            GameAudio.Update3DSound(CamPos);

            var simTime = new FixedSimTime(GlobalStats.SimulationFramesPerSecond);

            SimTimeSink += elapsed.RealTime.Seconds;
            while (SimTimeSink >= simTime.FixedTime)
            {
                SimTimeSink -= simTime.FixedTime;
                UpdateMainMenuShips(simTime);
                FTLManager.Update(this, simTime);
            }

            ScreenManager.UpdateSceneObjects(elapsed.RealTime.Seconds);
            
            if (RandomMath.RollDice(percent:0.25f)) // 0.25% (very rare event)
            {
                Comet c = Add(new Comet(this));
                c.SetDirection(new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f));
            }

            if (!IsExiting && ScreenManager.Music.IsStopped)
            {
                ResetMusic();
            }

            if (IsExiting && TransitionPosition >= 0.99f && ScreenManager.Music.IsPlaying)
            {
                ScreenManager.Music.Stop();
            }

            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawMultiLayeredExperimental(ScreenManager, batch, elapsed, draw3D:true);

            foreach (var fleet in Fleets)
                fleet.Draw(batch, this);

            FTLManager.DrawFTLModels(batch, this);
        }
    }
}
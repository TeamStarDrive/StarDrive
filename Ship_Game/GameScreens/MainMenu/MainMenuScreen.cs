using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;
using Ship_Game.GameScreens.Scene;
using Vector2 = SDGraphics.Vector2;
using SDGraphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens.MainMenu
{
    public enum MainMenuType
    {
        Default,
        Victory,
        Defeat,
    }

    public sealed class MainMenuScreen : GameScreen
    {
        SceneInstance Scene;
        UIElementContainer VersionArea;
        MainMenuType Type;

        public MainMenuScreen(MainMenuType type = MainMenuType.Default) : base(null /*no parent*/, toPause: null)
        {
            CanEscapeFromScreen = false;
            TransitionOnTime  = 1.0f;
            TransitionOffTime = 0.5f;
            Type = type;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            Scene?.Dispose();
            base.Dispose(disposing);
        }

        static void OnModChanged(FileInfo info)
        {
            if (!GlobalStats.HasMod)
                return;
            GlobalStats.LoadModInfo(GlobalStats.ModPath);
            if (ScreenManager.CurrentScreen is MainMenuScreen mainMenu)
                mainMenu.ReloadContent();
        }

        public override void LoadContent()
        {
            ScreenManager.ClearScene();
            ResetMusic();

            FileInfo menusDesc = ScreenManager.AddHotLoadTarget(this, "MainMenus.yaml");
            MainMenuDesc menu = YamlParser.Deserialize<MainMenusDescr>(menusDesc).GetDefault();

            UI.LayoutParser.LoadLayout(this, menu.UILayoutFile, clearElements: true);
            CreateVersionArea();

            Add(new AutoUpdateChecker(this));

            if (GlobalStats.HasMod)
            {
                ScreenManager.AddHotLoadTarget(this, new FileInfo(GlobalStats.ModFile), OnModChanged);
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
            if (list.Find("exit",      out UIButton exit))      exit.OnClick    = Exit_Clicked;
            list.PerformLayout();

            // Animate the buttons in and out
            var animOffset = new Vector2(512f * (ScreenWidth / 1920f), 0);
            list.StartGroupTransition<UIButton>(animOffset, -1, time:0.5f);
            OnExit += () => list.StartGroupTransition<UIButton>(animOffset, +1, time:0.5f);

            Scene = SceneInstance.FromFile(this, menu.SceneFile);

            base.LoadContent();
            Log.Info($"MainMenuScreen GameContent {TransientContent.GetLoadedAssetMegabytes():0.0}MB");
        }

        void CreateVersionArea()
        {
            VersionArea = Panel(Rectangle.Empty, Color.TransparentBlack);
            VersionArea.Name = "version_area";
            VersionArea.StartFadeIn(3.0f, delay: 2.0f);

            string starDrive = "StarDrive BlackBox";
            string blackBox = GlobalStats.ExtendedVersionNoHash;
            string modTitle = "";
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ModName;
                string version = GlobalStats.ModVersion;
                if (version.NotEmpty() && !title.Contains(version))
                    modTitle = title+" - "+version;
            }

            string longest = new []{starDrive,blackBox,modTitle}.FindMax(s => s.Length);
            int offset = Math.Max(300, Fonts.Pirulen12.TextWidth(longest).RoundUpToMultipleOf(10) + 10);
            VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 90, starDrive) { Name = "bb_ver_title"});
            VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 64, blackBox) { Name = "bb_ver_number" });
            if (modTitle.NotEmpty())
                VersionArea.Add(new VersionLabel(this, offset, ScreenHeight - 38, modTitle) { Name = "mod_title" });
        }

        public void ResetMusic()
        {
            GameAudio.ConfigureAudioSettings();
            GameAudio.StopGenericMusic();
            ScreenManager.Music.Stop();

            if (Type == MainMenuType.Victory)
            {
                GameAudio.SwitchToRacialMusic();
                ScreenManager.Music = GameAudio.PlayMusic("TitleTheme");
            }
            else if (Type == MainMenuType.Defeat)
            {
                ScreenManager.Music = GameAudio.PlayMusic("Female_02_loop");
            }
            else if (GlobalStats.Defaults.CustomMenuMusic.NotEmpty())
            {
                ScreenManager.Music = GameAudio.PlayMp3(GlobalStats.ModPath + GlobalStats.Defaults.CustomMenuMusic);
            }
            else if (ScreenManager.Music.IsStopped)
            {
                ScreenManager.Music = GameAudio.PlayMusic("SD_Theme_Reprise_06");
            }
        }

        void NewGame_Clicked(UIButton button) 
        {
            if (GlobalStats.HasMod && !GlobalStats.Defaults.Mod.ModFormatSupported)
            {
                ScreenManager.AddScreen(new MessageBoxScreen(this, "Mod format version mismatch. " +
                    "Please update Blackbox and this mod to latest vessions", MessageBoxButtons.Ok, 275));
                return;
            }

            ScreenManager.AddScreen(new RaceDesignScreen(this)); 
        }
        void Tutorials_Clicked(UIButton button) => ScreenManager.AddScreen(new TutorialScreen(this));
        void LoadGame_Clicked(UIButton button)
        {
            if (GlobalStats.HasMod && !GlobalStats.Defaults.Mod.ModFormatSupported)
            {
                ScreenManager.AddScreen(new MessageBoxScreen(this, "Mod format version mismatch. " +
                    "Please update Blackbox and this mod to latest vessions", MessageBoxButtons.Ok, 275));
                return;
            }

            ScreenManager.AddScreen(new LoadSaveScreen(this));
        }
        void Options_Clicked(UIButton button)   => ScreenManager.AddScreen(new OptionsScreen(this));
        void Mods_Clicked(UIButton button)      => ScreenManager.AddScreen(new ModManager(this));
        void Info_Clicked(UIButton button)      => ScreenManager.AddScreen(new InGameWiki(this));
        void DevSandbox_Clicked(UIButton button)=> ScreenManager.GoToScreen(new DeveloperSandbox(), clear3DObjects: true);
        void Exit_Clicked(UIButton button)      => ExitScreen();


        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            if (Scene.HandleInput(input))
                return true;

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
                Comet c = Add(new Comet(this, Scene.Random));
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

        // 24x distance is currently the maximum, seems like 25,000 distance is the cutoff for sound
        const float SoundDistanceMultiplier = 24;

        public override void Update(float fixedDeltaTime)
        {
            if (Scene != null)
            {
                // We set the listener pos further away, this is the only way to reduce SFX volume currently
                var listenerPos = new Vector3(Scene.CameraPos.X, Scene.CameraPos.Y, Scene.CameraPos.Z * SoundDistanceMultiplier);
                GameAudio.Update3DSound(listenerPos);

                var simTime = new FixedSimTime(GlobalStats.SimulationFramesPerSecond);

                SimTimeSink += fixedDeltaTime;
                while (SimTimeSink >= simTime.FixedTime)
                {
                    SimTimeSink -= simTime.FixedTime;
                    Scene.Update(simTime);
                    FTLManager.Update(this, simTime);
                }
            }

            ScreenManager.UpdateSceneObjects(fixedDeltaTime);
            
            if (Scene != null && Scene.Random.RollDice(percent:0.25f)) // 0.25% (very rare event)
            {
                Comet c = Add(new Comet(this, Scene.Random));
                c.SetDirection(new Vector2(Scene.Random.Float(-1f, 1f), 1f));
            }

            if (!IsExiting && ScreenManager.Music.IsStopped)
            {
                ResetMusic();
            }

            if (IsExiting && TransitionPosition >= 0.99f && ScreenManager.Music.IsPlaying)
            {
                ScreenManager.Music.Stop();
            }

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawMultiLayeredExperimental(ScreenManager, batch, elapsed, draw3D:true);
            Scene?.Draw(batch, elapsed);
            FTLManager.DrawFTLModels(batch, this);
        }
    }
}
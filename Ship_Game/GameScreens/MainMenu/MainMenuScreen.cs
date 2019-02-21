using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Ship_Game.UI;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.GameScreens.MainMenu
{
    public sealed class MainMenuScreen : GameScreen
    {
        SceneObject ShipObj;
        Vector3 ShipPosition;
        Vector3 ShipRotation = new Vector3(-116f, -188f, -19f);
        float ShipScale = 0.7f * 1.75f;

        AnimationController ShipAnim;

        bool DebugMeshInspect = false;
        UIElementContainer VersionArea;

        public MainMenuScreen() : base(null /*no parent*/)
        {
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
            LayoutParser.LoadLayout(this, "UI/MainMenu.yaml");

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
            
            InitRandomShip();

            AssignLightRig("example/ShipyardLightrig");
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

            Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateTranslation(0f, 0f, 0f) 
                   * Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateRotationX(0f.ToRadians())
                   * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);
            Projection = Matrix.CreateOrthographic(ScreenWidth, ScreenHeight, 1f, 80000f);

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

                if (input.ScrollIn)  ShipScale += 0.1f;
                if (input.ScrollOut) ShipScale -= 0.1f;

                // if new keypress, spawn random ship
                if (input.KeyPressed(Keys.Space))
                    InitRandomShip();
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

            ShipPosition = new Vector3(-ScreenWidth / 4f, 528 - ScreenHeight / 2f, 0f);
            ShipObj.AffineTransform(ShipPosition, ShipRotation.DegsToRad(), ShipScale);
            AddObject(ShipObj);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ScreenManager.UpdateSceneObjects(gameTime);

            if (!DebugMeshInspect)
            {
                // slow moves the ship across the screen
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
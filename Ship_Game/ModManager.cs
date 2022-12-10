using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
    public sealed class ModManager : GameScreen
    {
        readonly MainMenuScreen MainMenu;
        Rectangle Window;
        SubmenuScrollList<ModsListItem> AllSaves;
        Vector2 TitlePosition;
        UITextEntry EnterNameArea;
        UIButton Visit;
        UIButton UnloadMod;
        UIButton CurrentButton;

        ScrollList<ModsListItem> ModsList;
        ModEntry SelectedMod;

        public ModManager(MainMenuScreen mainMenu) : base(mainMenu, toPause: null)
        {
            MainMenu = mainMenu;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class ModsListItem : ScrollListItem<ModsListItem>
        {
            public readonly ModEntry Mod;
            public ModsListItem(ModEntry mod)
            {
                Mod = mod;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Mod.DrawListElement(batch, Rect);
            }
        }

        public override void LoadContent()
        {
            Window = new(ScreenWidth / 2 - 425, ScreenHeight / 2 - 300, 850, 600);
            Add(new Menu1(Window));

            RectF sub = new(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            Add(new Submenu(sub, GameText.LoadModification));

            RectF scrollList = new(sub.X, sub.Y + 90, sub.W, Window.Height - sub.H - 50);
            LoadMods(scrollList);

            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            EnterNameArea = Add(new UITextEntry(TitlePosition, Fonts.Arial12Bold, ""));
            EnterNameArea.SetColors(Color.Orange, Color.White);

            ButtonSmall(sub.X + sub.W - 88, EnterNameArea.Y - 2, text:GameText.Load, click: OnLoadClicked);
            Visit = Button(Window.X + 3, Window.Y + Window.Height + 20, text:GameText.LoadModsWeb, click: OnVisitClicked);
            UnloadMod = Button(Window.X + Window.Width - 172, Window.Y + Window.Height + 20, "Unload Mod", click:OnUnloadModClicked);
            UnloadMod.Enabled = GlobalStats.HasMod;

            base.LoadContent();
        }

        void LoadMods(RectF scrollList)
        {
            AllSaves = Add(new SubmenuScrollList<ModsListItem>(scrollList, GameText.LoadModification, 140));
            ModsList = AllSaves.List;
            ModsList.EnableItemHighlight = true;
            ModsList.OnClick = OnModItemClicked;
            foreach (DirectoryInfo info in Dir.GetDirs("Mods", SearchOption.TopDirectoryOnly))
            {
                string modFile = $"Mods/{info.Name}/Globals.yaml";
                try
                {
                    var file = new FileInfo(modFile);
                    GamePlayGlobals modSettings = GamePlayGlobals.Deserialize(file);
                    var e = new ModEntry(modSettings);
                    e.LoadPortrait(MainMenu);
                    ModsList.AddItem(new ModsListItem(e));
                }
                catch (Exception ex)
                {
                    Log.Warning($"Load error in file {modFile}: {ex.Message}");
                    ex.Data.Add("Load Error in file", modFile);
                }
            }
        }

        void OnModItemClicked(ModsListItem item)
        {
            SelectedMod = item.Mod;
            EnterNameArea.Text = SelectedMod.Mod.Name;
            Visit.Text = SelectedMod.Settings.URL.IsEmpty() ? Localizer.Token(GameText.LoadModsWeb) : "Goto Mod URL";
        }

        public override bool HandleInput(InputState input)
        {
            if (CurrentButton == null && (input.Escaped || input.RightMouseClick))
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsExiting)
                return;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }

        void OnLoadClicked(UIButton b)
        {
            if (SelectedMod == null || !SelectedMod.IsSupported)
            {
                GameAudio.NegativeClick();
                return;
            }
            CurrentButton = b;
            b.Text = "Loading";
            LoadModTask();
        }

        void OnVisitClicked(UIButton b)
        {
            if (!string.IsNullOrEmpty(SelectedMod?.Settings.URL))
            {
                try
                {
                    SteamManager.ActivateOverlayWebPage(SelectedMod.Settings.URL);
                }
                catch
                {
                    Process.Start(SelectedMod.Settings.URL);
                }
            }
        }

        void OnUnloadModClicked(UIButton b)
        {
            ClearMods();
        }

        void ClearMods()
        {
            if (!GlobalStats.HasMod)
                return;

            Log.Info("ModManager.ClearMods");
            GlobalStats.SetActiveModNoSave(null);
            // reload the whole game
            ScreenManager.GoToScreen(new GameLoadingScreen(showSplash: false, resetResources: true), clear3DObjects: true);
        }

        void LoadModTask()
        {
            Log.Info($"ModManager.LoadMod {SelectedMod.Mod.Path}");
            GlobalStats.SetActiveModNoSave(SelectedMod);
            // reload the whole game
            ScreenManager.GoToScreen(new GameLoadingScreen(showSplash: false, resetResources: true), clear3DObjects: true);
        }
    }
}

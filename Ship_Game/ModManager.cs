using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class ModManager : GameScreen
    {
        readonly MainMenuScreen MainMenu;
        Rectangle Window;
        Menu1 SaveMenu;
        Submenu NameSave;
        Submenu AllSaves;
        Vector2 TitlePosition;
        Vector2 EnternamePos;
        UITextEntry EnterNameArea;
        UIButton Visit;
        UIButton UnloadMod;
        UIButton CurrentButton;

        ScrollList2<ModsListItem> ModsList;
        ModEntry SelectedMod;

        public ModManager(MainMenuScreen mainMenu) : base(mainMenu)
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
            Window = new Rectangle(ScreenWidth / 2 - 425, ScreenHeight / 2 - 300, 850, 600);
            SaveMenu = new Menu1(Window);
            Rectangle sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            NameSave = new Submenu(sub);
            NameSave.AddTab(Localizer.Token(4013));
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, Window.Height - sub.Height - 50);
            AllSaves = new Submenu(scrollList);
            AllSaves.AddTab(Localizer.Token(4013));

            LoadMods();
            EnternamePos  = TitlePosition;
            EnterNameArea = new UITextEntry {Text = ""};
            EnterNameArea.ClickableArea = new Rectangle((int)EnternamePos.X, (int)EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, text:8, click: OnLoadClicked);
            Visit = Button(Window.X + 3, Window.Y + Window.Height + 20, text:4015, click: OnVisitClicked);
            Button(Window.X + 200, Window.Y + Window.Height + 20, text:4044, click: OnShipToolClicked);
            UnloadMod = Button(Window.X + Window.Width - 172, Window.Y + Window.Height + 20, "Unload Mod", click:OnUnloadModClicked);
            UnloadMod.Enabled = GlobalStats.HasMod;

            base.LoadContent();
        }

        void LoadMods()
        {
            ModsList = Add(new ScrollList2<ModsListItem>(AllSaves, 140));
            ModsList.EnableItemHighlight = true;
            ModsList.OnClick = OnModItemClicked;
            var ser = new XmlSerializer(typeof(ModInformation));
            foreach (DirectoryInfo info in Dir.GetDirs("Mods", SearchOption.TopDirectoryOnly))
            {
                string modFile = $"Mods/{info.Name}/{info.Name}.xml";
                try
                {                    
                    var file = new FileInfo(modFile);
                    var modInfo = ser.Deserialize<ModInformation>(file);
                    if (modInfo == null)
                        throw new FileNotFoundException($"Mod XML not found: {modFile}");

                    var e = new ModEntry(modInfo);
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
            EnterNameArea.Text = SelectedMod.ModName;
            Visit.Text = SelectedMod.mi.URL.IsEmpty() ? Localizer.Token(4015) : "Goto Mod URL";
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
            SaveMenu.Draw(batch, elapsed);
            NameSave.Draw(batch, elapsed);
            AllSaves.Draw(batch, elapsed);
            EnterNameArea.Draw(batch, elapsed, Fonts.Arial12Bold, EnternamePos,
                               (EnterNameArea.Hover ? Color.White : Color.Orange));
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
            if (string.IsNullOrEmpty(SelectedMod?.mi.URL))
            {
                try
                {
                    SteamManager.ActivateOverlayWebPage("https://bitbucket.org/codegremlins/combinedarms/downloads/");
                }
                catch
                {
                    Process.Start("https://bitbucket.org/codegremlins/combinedarms/downloads/");
                }
            }
            else
            {
                try
                {
                    SteamManager.ActivateOverlayWebPage(SelectedMod.mi.URL);
                }
                catch
                {
                    Process.Start(SelectedMod.mi.URL);
                }
            }
        }

        void OnShipToolClicked(UIButton b)
        {
            ScreenManager.GoToScreen(new ShipToolScreen(this), clear3DObjects: true);
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
            Log.Info($"ModManager.LoadMod {SelectedMod.ModName}");
            GlobalStats.SetActiveModNoSave(SelectedMod);
            // reload the whole game
            ScreenManager.GoToScreen(new GameLoadingScreen(showSplash: false, resetResources: true), clear3DObjects: true);
        }
    }
}

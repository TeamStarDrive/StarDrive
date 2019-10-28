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
        MainMenuScreen mmscreen;
        Rectangle Window;
        Menu1 SaveMenu;
        Submenu NameSave;
        Submenu AllSaves;
        Vector2 TitlePosition;
        Vector2 EnternamePos;
        UITextEntry EnterNameArea;
        UIButton Load;
        UIButton Disable;
        UIButton Visit;
        UIButton shiptool;
        UIButton CurrentButton;

        ScrollList<ModsListItem> ModsSL;
        ModEntry SelectedMod;

        public ModManager(MainMenuScreen mmscreen) : base(mmscreen)
        {
            this.mmscreen = mmscreen;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class ModsListItem : ScrollList<ModsListItem>.Entry
        {
            public ModEntry Mod;
            public ModsListItem(ModEntry mod)
            {
                Mod = mod;
            }
            public override void Draw(SpriteBatch batch)
            {
                Mod.DrawListElement(batch, Rect);
            }
        }

        public override void LoadContent()
        {
            Window = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 425, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 850, 600);
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

            Load     = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, titleId:8, click: OnLoadClicked);
            Visit    = Button(Window.X + 3, Window.Y + Window.Height + 20, titleId:4015, click: OnVisitClicked);
            shiptool = Button(Window.X + 200, Window.Y + Window.Height + 20, titleId:4044, click: OnShipToolClicked);
            Disable  = Button(Window.X + Window.Width - 172, Window.Y + Window.Height + 20, titleId:4016, click:OnDisableClicked);

            base.LoadContent();
        }

        void LoadMods()
        {
            ModsSL = new ScrollList<ModsListItem>(AllSaves, 140);
            ModsSL.OnClick = OnModItemClicked;

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
                    e.LoadPortrait(mmscreen);
                    ModsSL.AddItem(new ModsListItem(e));
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
            return ModsSL.HandleInput(input) && base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (IsExiting)
                return;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw(batch);
            NameSave.Draw(batch);
            AllSaves.Draw(batch);
            ModsSL.Draw(batch);
            EnterNameArea.Draw(batch, Fonts.Arial12Bold, EnternamePos, (EnterNameArea.Hover ? Color.White : Color.Orange));
            base.Draw(batch);
            batch.End();
        }

        void OnLoadClicked(UIButton b)
        {
            if (SelectedMod == null)
                return;
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
                    SteamManager.ActivateOverlayWebPage("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
                }
                catch
                {
                    Process.Start("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
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
            //ScreenManager.AddScreen(new ShipToolScreen(this));
            ScreenManager.GoToScreen(new ShipToolScreen(this), clear3DObjects: true);
        }

        void OnDisableClicked(UIButton b)
        {
            ClearMods();
        }

        void ClearMods()
        {
            if (!GlobalStats.HasMod)
                return;

            Log.Info("ModManager.ClearMods");
            ExitScreen();
            // @note This will trigger game unload and reload
            ResourceManager.LoadItAll(ScreenManager, null, reset:true);
        }

        void LoadModTask()
        {
            Log.Info($"ModManager.LoadMod {SelectedMod.ModName}");
            GlobalStats.SetActiveModNoSave(SelectedMod);
            ExitScreen();
            // @note This will trigger game unload and reload
            ResourceManager.LoadItAll(ScreenManager, SelectedMod, reset:true);
        }
    }
}

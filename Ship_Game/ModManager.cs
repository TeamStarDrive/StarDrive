using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace Ship_Game
{
    public sealed class ModManager : GameScreen
    {
        private MainMenuScreen mmscreen;
        private Rectangle Window;
        private Menu1 SaveMenu;
        private Submenu NameSave;
        private Submenu AllSaves;
        private Vector2 TitlePosition;
        private Vector2 EnternamePos;
        private UITextEntry EnterNameArea;
        private UIButton Load;
        private UIButton Disable;
        private UIButton Visit;
        private UIButton shiptool;
        private Selector selector;
        private UIButton CurrentButton;

        private ScrollList ModsSL;
        private ModEntry SelectedMod;

        public ModManager(MainMenuScreen mmscreen) : base(mmscreen)
        {
            this.mmscreen = mmscreen;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (IsExiting)
                return;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            SaveMenu.Draw();
            NameSave.Draw();
            AllSaves.Draw();
            foreach (ScrollList.Entry e in ModsSL.VisibleEntries)
            {
                e.Get<ModEntry>().Draw(ScreenManager, e.Rect);
            }
            ModsSL.Draw(ScreenManager.SpriteBatch);
            EnterNameArea.Draw(Fonts.Arial12Bold, ScreenManager.SpriteBatch, EnternamePos, 
                Game1.Instance.GameTime, (EnterNameArea.Hover ? Color.White : Color.Orange));

            base.Draw(batch);
            selector?.Draw(ScreenManager.SpriteBatch);
            ScreenManager.SpriteBatch.End();
        }

        private void OnLoadClicked(UIButton b)
        {
            if (SelectedMod == null)
                return;
            CurrentButton = b;
            b.Text = "Loading";
            LoadModTask();
        }

        private void OnVisitClicked(UIButton b)
        {
            if (string.IsNullOrEmpty(SelectedMod?.mi.URL)) try
            {
                SteamManager.ActivateOverlayWebPage("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
            }
            catch
            {
                Process.Start("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
            }
            else try
            {
                SteamManager.ActivateOverlayWebPage(SelectedMod.mi.URL);
            }
            catch
            {
                Process.Start(SelectedMod.mi.URL);
            }
        }

        private void OnShipToolClicked(UIButton b)
        {
            ScreenManager.AddScreen(new ShipToolScreen(this));
        }

        private void OnDisableClicked(UIButton b)
        {
            ClearMods();
        }

        public override bool HandleInput(InputState input)
        {
            selector?.RemoveFromParent();
            selector = null;
            
            if (CurrentButton == null && (input.Escaped || input.RightMouseClick))
            {
                ExitScreen();
                return true;    
            }

            if (CurrentButton != null)
            {
                CurrentButton.State = UIButton.PressState.Pressed;
                return false;
            }
            ModsSL.HandleInput(input);
            foreach (ScrollList.Entry e in ModsSL.AllEntries)
            {
                if (!e.CheckHover(input.CursorPosition))
                    continue;

                selector = e.CreateSelector();
                if (!input.InGameSelect)
                    continue;

                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                SelectedMod = (ModEntry)e.item;
                EnterNameArea.Text = SelectedMod.ModName;
                Visit.Text = SelectedMod.mi.URL.IsEmpty() ? Localizer.Token(4015) : "Goto Mod URL";
            }
            return base.HandleInput(input);
        }
        private void ClearMods()
        {
            if (!GlobalStats.HasMod)
                return;

            Log.Info("ModManager.ClearMods");
            GlobalStats.LoadModInfo("");
            ResourceManager.LoadItAll();
            mmscreen.LoadContent();
            ExitScreen();
            mmscreen.ResetMusic();
        }
        private void LoadModTask()
        {
            Log.Info($"ModManager.LoadMod {SelectedMod.ModName}");
            GlobalStats.LoadModInfo(SelectedMod);
            ResourceManager.LoadItAll();
            mmscreen.LoadContent();
            ExitScreen();
            mmscreen.ResetMusic();
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
            ModsSL = new ScrollList(AllSaves, 140);

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

        private void LoadMods()
        {
            var ser = new XmlSerializer(typeof(ModInformation));
            foreach (FileInfo info in Dir.GetFilesNoSub("Mods"))
            {
                if (!info.Name.EndsWith(".xml"))
                    continue;
                try
                {                    
                    var modInfo = new FileInfo($"Mods\\{info.NameNoExt()}\\{info.Name}");
                    ModsSL.AddItem(new ModEntry(ser.Deserialize<ModInformation>(modInfo.Exists ? modInfo : info)));
                }
                catch (Exception ex)
                {
                    Log.Warning($"Load error in file {info.Name}");
                    ex.Data.Add("Load Error in file", info.Name);
                }
            }

        }
    }
}

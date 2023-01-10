using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public abstract class GenericLoadSaveScreen : GameScreen
    {
        protected Rectangle Window;
        protected Menu1 SaveMenu;
        protected Submenu NameSave;
        protected SubmenuScrollList<SaveLoadListItem> AllSaves;
        protected Vector2 TitlePosition;
        protected UITextEntry EnterNameArea;
        protected ScrollList<SaveLoadListItem> SavesSL;
        protected UIButton DoBtn;
        protected UIButton ExportBtn;
        public enum SLMode { Load, Save }
        protected SLMode Mode;

        protected string InitText;
        protected string Title;
        protected string OverwriteText = "";
        protected string Path = "";
        protected string TabText;

        protected FileData SelectedFile;
        protected int EntryHeight = 55; // element height
        protected bool ShowSaveExport;

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText, bool showSaveExport = false)
            : base(parent, toPause: parent as UniverseScreen)
        {
            Mode = mode;
            InitText = initText;
            Title = title;
            TabText = tabText;
            ShowSaveExport = showSaveExport;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText, string overwriteText, bool showSaveExport = false) 
            : this(parent, mode, initText, title, tabText, showSaveExport:showSaveExport)
        {
            OverwriteText = overwriteText;
        }

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText, int entryHeight) 
            : this(parent, mode, initText, title, tabText)
        {
            EntryHeight = entryHeight;
        }

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText, string overwriteText, int entryHeight) 
            : this(parent, mode, initText, title, tabText, overwriteText)
        {
            EntryHeight = entryHeight;
        }

        public virtual void DoSave()
        {
        }

        protected void DeleteFile(FileData toDelete)
        {
            GameAudio.EchoAffirmative();
            
            try
            {
                toDelete.FileLink.Delete(); // delete the file
            } catch { }

            SavesSL.RemoveFirstIf(item => item.Data == toDelete);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            SaveMenu.Draw(batch, elapsed);
            NameSave.Draw(batch, elapsed);
            AllSaves.Draw(batch, elapsed);

            base.Draw(batch, elapsed);

            batch.SafeEnd();
        }

        protected virtual void Load()
        {
        }

        protected abstract void InitSaveList(); // To be implemented in subclasses

        public override void LoadContent()
        {
            Window = new Rectangle(ScreenWidth / 2 - 300, ScreenHeight / 2 - 300, 600, 600);
            SaveMenu = new Menu1(Window);
            CloseButton(Window.X + Window.Width - 35, Window.Y + 10);

            RectF sub = new(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            NameSave = new Submenu(sub, Title);
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);

            RectF scrollList = new(sub.X, sub.Y + 90, sub.W, Window.Height - sub.H - 50);

            AllSaves = Add(new SubmenuScrollList<SaveLoadListItem>(scrollList, TabText, EntryHeight));
            SavesSL = AllSaves.List;
            SavesSL.OnClick = OnSaveLoadItemClicked;
            SavesSL.OnDoubleClick = OnSaveLoadItemDoubleClicked;
            SavesSL.EnableItemHighlight = true;
            InitSaveList();

            EnterNameArea = Add(new UITextEntry(TitlePosition, Fonts.Arial20Bold, InitText));
            EnterNameArea.Enabled = (Mode == SLMode.Save); // Only enable name field change when saving

            string title = Mode == SLMode.Save ? "Save" : "Load";
            DoBtn = ButtonSmall(sub.X + sub.W - 88, EnterNameArea.Y - 2, title, b =>
            {
                if (Mode == SLMode.Save)
                    TrySave();
                else if (Mode == SLMode.Load)
                    Load();
            });

            if (ShowSaveExport)
            {
                var exportBtn = ButtonBigDip(sub.X + sub.W - 200, EnterNameArea.Y - 48, "Export Save", b => ExportSave());
                exportBtn.Tooltip = GameText.ThisWillLetYouEasily;
            }
            base.LoadContent();
        }

        protected virtual void OnSaveLoadItemClicked(SaveLoadListItem item)
        {
            SwitchFile(item.Data);
        }
        
        protected virtual void OnSaveLoadItemDoubleClicked(SaveLoadListItem item)
        {
            SwitchFile(item.Data);
            if (Mode == SLMode.Save)
                TrySave();
            else if (Mode == SLMode.Load)
                Load();
        }


        protected void SwitchFile(FileData file)
        {
            SelectedFile = file;
            GameAudio.AcceptClick();
            EnterNameArea.Text = file.FileName;
        }

        void OverWriteAccepted()
        {
            DoSave();
        }

        bool IsSaveOk()
        {
            foreach (SaveLoadListItem item in SavesSL.AllEntries)
                if (EnterNameArea.Text == item.Data.FileName) // check if item already exists
                    return false;
            return true;
        }

        void TrySave()
        {
            if (EnterNameArea.Text.IsEmpty())
            {
                GameAudio.NegativeClick();
                ScreenManager.AddScreen(new MessageBoxScreen(this, "Please enter file name", MessageBoxButtons.Ok));
            }
            else if (IsSaveOk())
            {
                DoSave();
            }
            else
            {
                ScreenManager.AddScreen(new MessageBoxScreen(this, OverwriteText)
                {
                    Accepted = OverWriteAccepted
                });
            }
        }

        protected void AddItemsToSaveSL(IEnumerable<FileData> files)
        {
            foreach (FileData data in files)
                SavesSL.AddItem(new SaveLoadListItem(this, data));
        }

        protected void ExportSave()
        {
            if (SelectedFile == null)
            {
                GameAudio.NegativeClick();
                return;
            }

            string savedFileName = ExportSave(SelectedFile);

            string message = $"The selected save was exported to your desktop as {savedFileName}";
            int messageWidth = ((int)Fonts.Arial12Bold.MeasureString(savedFileName).X + 20).UpperBound(400);
            ScreenManager.AddScreen(new MessageBoxScreen(this, message, MessageBoxButtons.Ok, messageWidth));
        }
        
        string ExportSave(FileData save)
        {
            Log.FlushAllLogs();

            string fileName = save.FileName;
            var dirInfo = new DirectoryInfo(Path + "/" + fileName);
            dirInfo.Create();
            string tmpDir = dirInfo.FullName;

            save.FileLink.CopyTo($"{tmpDir}/{save.FileName}{save.FileLink.Extension}", overwrite:true);

            // also add both logfiles
            if (File.Exists(Log.LogFilePath))
                File.Copy(Log.LogFilePath, $"{tmpDir}/blackbox.log", overwrite:true);
            if (File.Exists(Log.OldLogFilePath))
                File.Copy(Log.OldLogFilePath, $"{tmpDir}/blackbox.old.log", overwrite:true);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outZip = $"{GetDebugVersionString()}_{fileName}.zip";
            HelperFunctions.CompressDir(dirInfo, $"{desktop}/{outZip}");
            dirInfo.Delete(true);

            return outZip;
        }

        static string GetDebugVersionString()
        {
            string blackBox = GlobalStats.ExtendedVersionNoHash.Replace(":", "").Replace(" ", "_").Replace("/", "_");
            string modTitle = "";
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ModName;
                string version = GlobalStats.Defaults.Mod.Version;
                if (version.NotEmpty() && !title.Contains(version))
                    modTitle = title + "-" + version;

                modTitle = modTitle.Replace(":", "").Replace(" ", "_");
                return $"{blackBox}_{modTitle}";
            }
            return blackBox;
        }

        protected class SaveLoadListItem : ScrollListItem<SaveLoadListItem>
        {
            readonly GenericLoadSaveScreen Screen;
            public FileData Data;
            public SaveLoadListItem(GenericLoadSaveScreen screen, FileData data)
            {
                Screen = screen;
                Data = data;
                AddCancel(new Vector2(-30, 0), "Delete Save File", OnDeleteClicked);
            }
            void OnDeleteClicked()
            {
                var toDelete = Data;
                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, "Confirm Delete:")
                {
                    Accepted = () => Screen.DeleteFile(toDelete)
                });
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                float iconHeight = (int)(Height * 0.89f);
                float iconWidth = (int)Data.Icon.GetWidthFromHeightAspect(iconHeight);
                batch.Draw(Data.Icon, Pos, new Vector2(iconWidth, iconHeight), Data.IconColor);

                var tCursor = new Vector2(X + 50f, Y);
                var mainColor = Data.Enabled ? Color.Orange : Color.Gray;
                batch.DrawString(Fonts.Arial20Bold, Data.FileName, tCursor, mainColor);

                tCursor.Y += Fonts.Arial20Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.Info, tCursor, Color.White);

                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.ExtraInfo, tCursor, Color.White);
            }
        }

        protected class FileData
        {
            public string FileName;
            public string Info;
            public string ExtraInfo;
            public SubTexture Icon;
            public Color IconColor;
            public FileInfo FileLink;
            public object Data;
            public bool Enabled = true; // new feature: show incompatible entries as grayed out and unselectable

            public FileData(FileInfo fileLink, object data, 
                string fileName, string info, string extraInfo, SubTexture icon, Color iconColor)
            {
                FileName = fileName;
                Info = info;
                ExtraInfo = extraInfo;
                FileLink = fileLink;
                Data = data;
                Icon = icon ?? ResourceManager.Texture("ShipIcons/Wisp");
                IconColor = iconColor;
            }

            public static FileData FromSaveHeader(FileInfo file, HeaderData header)
            {
                string info = $"{header.PlayerName} StarDate {header.StarDate}";
                string extraInfo = header.RealDate;

                IEmpireData empire = ResourceManager.AllRaces.FirstOrDefault(e => e.Name == header.PlayerName)
                                  ?? ResourceManager.AllRaces[0];
                return new FileData(file, header, header.SaveName, info, extraInfo,
                                       empire.Traits.FlagIcon, empire.Traits.Color);
            }
        }
    }
}
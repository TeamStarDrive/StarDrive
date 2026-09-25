using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
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

        protected virtual bool DeleteFile(FileData toDelete)
        {
            try
            {
                toDelete.FileLink.Delete(); // delete the file
            } 
            catch 
            {
                GameAudio.NegativeClick();
                return false;
            }

            GameAudio.EchoAffirmative();
            SavesSL.RemoveFirstIf(item => item.Data == toDelete);
            return true;
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

        protected void AddItemsToSaveSL(IEnumerable<FileData> files, bool addCancel = true)
        {
            foreach (FileData data in files)
                SavesSL.AddItem(new SaveLoadListItem(this, data, addCancel));
        }

        protected void ExportSave()
        {
            if (SelectedFile == null)
            {
                GameAudio.NegativeClick();
                return;
            }

            if (!CanExportSave(SelectedFile, out string refusal))
            {
                GameAudio.NegativeClick();
                ScreenManager.AddScreen(new MessageBoxScreen(this, refusal, MessageBoxButtons.Ok));
                return;
            }

            string savedFileName;
            try
            {
                savedFileName = ExportSave(SelectedFile);
            }
            catch (Exception e)
            {
                Log.Warning($"Save export failed: {e.Message}");
                GameAudio.NegativeClick();
                ScreenManager.AddScreen(new MessageBoxScreen(this,
                    $"The save could not be exported.\n\n{e.Message}", MessageBoxButtons.Ok));
                return;
            }

            string message = $"The selected save was exported to your desktop as {savedFileName}";
            int messageWidth = ((int)Fonts.Arial12Bold.MeasureString(savedFileName).X + 20).UpperBound(400);
            ScreenManager.AddScreen(new MessageBoxScreen(this, message, MessageBoxButtons.Ok, messageWidth));
        }

        static bool CanExportSave(FileData save, out string refusal)
        {
            refusal = null;
            if (save.Data is not HeaderData header)
                return true;

            if (header.Version != SavedGame.SaveGameVersion)
            {
                refusal = $"This save is save format version {header.Version}, and you are running "
                        + $"version {SavedGame.SaveGameVersion}.\n\n"
                        + "It cannot be exported. The exported archive is named after the build and mod "
                        + "you are running now, so it would claim to be something it is not, and the logs "
                        + "packed with it would come from a build that never loaded this save.\n\n"
                        + "Export it from the build that wrote it.";
                return false;
            }

            if (header.ModName != GlobalStats.ModName)
            {
                string saveMod = header.ModName.NotEmpty() ? header.ModName : "no mod";
                string runningMod = GlobalStats.ModName.NotEmpty() ? GlobalStats.ModName : "no mod";
                refusal = $"This save was made with {saveMod}, and you are running {runningMod}.\n\n"
                        + "It cannot be exported, for the same reason: the archive is named after the mod "
                        + "you are running now and would misreport what is inside it.\n\n"
                        + "Activate that mod and export it from there.";
                return false;
            }

            return true;
        }
        
        string ExportSave(FileData save)
        {
            Log.FlushAllLogs();

            string fileName = save.FileName;
            var dirInfo = new DirectoryInfo(Path + "/" + fileName);
            dirInfo.Create();
            try
            {
                return CompressSaveTo(save, dirInfo);
            }
            finally
            {
                try { dirInfo.Delete(true); }
                catch (Exception e) { Log.Warning($"Could not remove export staging dir: {e.Message}"); }
            }
        }

        static string CompressSaveTo(FileData save, DirectoryInfo dirInfo)
        {
            string tmpDir = dirInfo.FullName;

            save.FileLink.CopyTo($"{tmpDir}/{save.FileName}{save.FileLink.Extension}", overwrite:true);

            // also add both logfiles
            if (File.Exists(Log.LogFilePath))
                File.Copy(Log.LogFilePath, $"{tmpDir}/blackbox.log", overwrite:true);
            if (File.Exists(Log.OldLogFilePath))
                File.Copy(Log.OldLogFilePath, $"{tmpDir}/blackbox.old.log", overwrite:true);

            // include the user's colony blueprints for the current mod/BBplus context
            string modScope = BlueprintsTemplate.CurrentModName;
            string blueprintsSrc = Dir.StarDriveAppData + "/Colony Blueprints/" + modScope;
            if (Directory.Exists(blueprintsSrc))
            {
                string blueprintsDest = $"{tmpDir}/blueprints/{modScope}";
                Directory.CreateDirectory(blueprintsDest);
                foreach (FileInfo bp in Dir.GetFiles(blueprintsSrc, "yaml"))
                    bp.CopyTo($"{blueprintsDest}/{bp.Name}", overwrite:true);
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outZip = $"{GetDebugVersionString()}_{save.FileName}.zip";
            HelperFunctions.CompressDir(dirInfo, $"{desktop}/{outZip}");
            return outZip;
        }

        static string GetDebugVersionString()
        {
            string blackBox = GlobalStats.ExtendedVersionNoHash.Replace(":", "").Replace(" ", "_").Replace("/", "_");
            if (GlobalStats.HasMod)
            {
                string title = GlobalStats.ModName;
                string version = GlobalStats.Defaults.Mod.Version;
                string modTitle = version.NotEmpty() && !title.Contains(version) ? title + "-" + version : title;
                modTitle = modTitle.Replace(":", "").Replace(" ", "_");
                return $"{blackBox}_{modTitle}";
            }
            return blackBox;
        }

        protected class SaveLoadListItem : ScrollListItem<SaveLoadListItem>
        {
            readonly GenericLoadSaveScreen Screen;
            public FileData Data;
            public SaveLoadListItem(GenericLoadSaveScreen screen, FileData data, bool addCencel)
            {
                Screen = screen;
                Data = data;
                if (addCencel)
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
                base.Draw(batch, elapsed);

                float iconHeight = (int)(Height * 0.89f);
                float iconWidth = (int)Data.Icon.GetWidthFromHeightAspect(iconHeight);
                batch.Draw(Data.Icon, Pos, new Vector2(iconWidth, iconHeight), Data.IconColor);

                var tCursor = new Vector2(X + 50f, Y);
                var mainColor = Data.Enabled ? Data.FileNameColor : Color.Gray;
                batch.DrawString(Fonts.Arial20Bold, Data.FileName, tCursor, mainColor);

                tCursor.Y += Fonts.Arial20Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.Info, tCursor, Data.InfoColor);

                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.ExtraInfo, tCursor, Data.InfoColor);

                if (Hovered && Data.Tooltip.NotEmpty())
                    ToolTip.CreateTooltip(Data.Tooltip, "", null, maxWidth:400);
            }
        }

        protected class FileData
        {
            public string FileName;
            public string Info;
            public string ExtraInfo;
            public string Tooltip;
            public SubTexture Icon;
            public Color IconColor;
            public FileInfo FileLink;
            public object Data;
            public bool Enabled = true; // new feature: show incompatible entries as grayed out and unselectable
            public Color InfoColor = Color.White;
            public Color FileNameColor = Color.Orange;

            public FileData(FileInfo fileLink, object data,
                string fileName, string info, string extraInfo, string tooltip, SubTexture icon, Color iconColor)
            {
                FileName = fileName;
                Info = info;
                ExtraInfo = extraInfo;
                Tooltip = tooltip;
                FileLink = fileLink;
                Data = data;
                Icon = icon ?? ResourceManager.Texture("ShipIcons/Wisp");
                IconColor = iconColor;
            }

            public static FileData FromSaveHeader(FileInfo file, HeaderData header)
            {
                string info = $"{header.PlayerName} StarDate {header.StarDate}";
                string extraInfo = header.RealDate;
                string tooltip = file.Name;

                // headers that carry the player's flag draw it directly; older headers read
                // -1 and fall back to the race-name lookup below, which shows the default
                // flag for custom and renamed races. Flag() is null if the index is not in
                // the loaded atlas (a modded save listed in the unfiltered Save dialog), and
                // the lookup is a better guess than the ctor's generic icon fallback
                SubTexture flag = header.FlagIndex >= 0 ? ResourceManager.Flag(header.FlagIndex) : null;
                if (flag != null)
                    return new(file, header, header.SaveName, info, extraInfo, tooltip,
                               flag, header.EmpireColor);

                IEmpireData empire = ResourceManager.AllRaces.FirstOrDefault(e => e.Name == header.PlayerName)
                                  ?? ResourceManager.AllRaces[0];
                // only the icon was missing, so keep the header's own colour when it has one
                Color tint = header.FlagIndex >= 0 ? header.EmpireColor : empire.Traits.Color;
                return new(file, header, header.SaveName, info, extraInfo, tooltip,
                           empire.Traits.FlagIcon, tint);
            }
        }
    }
}
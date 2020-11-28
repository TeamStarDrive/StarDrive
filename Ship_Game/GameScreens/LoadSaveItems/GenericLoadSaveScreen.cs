using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public abstract class GenericLoadSaveScreen : GameScreen
    {
        protected Rectangle Window;
        protected Menu1 SaveMenu;
        protected Submenu NameSave;
        protected Submenu AllSaves;
        protected Vector2 TitlePosition;
        protected UITextEntry EnterNameArea;
        protected ScrollList2<SaveLoadListItem> SavesSL;
        protected UIButton DoBtn;
        public enum SLMode { Load, Save }
        protected SLMode Mode;

        protected string InitText;
        protected string Title;
        protected string OverwriteText = "";
        protected string Path = "";
        protected string TabText;

        protected FileData FileToDelete;
        protected FileData SelectedFile;
        protected int EntryHeight = 55; // element height

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText)
            : base(parent)
        {
            Mode = mode;
            InitText = initText;
            Title = title;
            TabText = tabText;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        protected GenericLoadSaveScreen(
            GameScreen parent, SLMode mode, string initText, string title, string tabText, string overwriteText) 
            : this(parent, mode, initText, title, tabText)
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

        protected virtual void DeleteFile()
        {
            GameAudio.EchoAffirmative();
            
            try
            {
                FileToDelete.FileLink.Delete(); // delete the file
            } catch { }

            SavesSL.RemoveFirstIf(item => item.Data == FileToDelete);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw(batch, elapsed);
            NameSave.Draw(batch, elapsed);
            AllSaves.Draw(batch, elapsed);

            base.Draw(batch, elapsed);

            batch.End();
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

            var sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            NameSave = new Submenu(sub);
            NameSave.AddTab(Title);
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            var scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, Window.Height - sub.Height - 50);
            AllSaves = new Submenu(scrollList);
            AllSaves.AddTab(TabText);
            SavesSL = Add(new ScrollList2<SaveLoadListItem>(AllSaves, EntryHeight));
            SavesSL.OnClick = OnSaveLoadItemClicked;
            SavesSL.EnableItemHighlight = true;
            InitSaveList();

            EnterNameArea = Add(new UITextEntry(TitlePosition, Fonts.Arial20Bold, InitText));
            EnterNameArea.InputEnabled = (Mode == SLMode.Save); // Only enable name field change when saving

            string title = Mode == SLMode.Save ? "Save" : "Load";
            DoBtn = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.Y - 2, title, b =>
            {
                if (Mode == SLMode.Save)
                    TrySave();
                else if (Mode == SLMode.Load)
                    Load();
            });

            base.LoadContent();
        }

        protected virtual void OnSaveLoadItemClicked(SaveLoadListItem item)
        {
            SwitchFile(item.Data);
        }


        protected void SwitchFile(FileData file)
        {
            if (SLMode.Load == Mode)
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

        protected void AddItemToSaveSL(FileInfo info, SubTexture icon)
        {
            var data = new FileData(info, info, info.NameNoExt(), "", "", icon, Color.White);
            var item = new SaveLoadListItem(this, data);
            SavesSL.AddItem(item);
        }

        protected void AddItemsToSaveSL(IEnumerable<FileData> files)
        {
            foreach (FileData data in files)
                SavesSL.AddItem(new SaveLoadListItem(this, data));
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
                Screen.FileToDelete = Data;
                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, "Confirm Delete:")
                {
                    Accepted = Screen.DeleteFile
                });
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                float iconHeight = (int)(Height * 0.89f);
                batch.Draw(Data.Icon, Pos, new Vector2(iconHeight*Data.Icon.AspectRatio, iconHeight), Data.IconColor);

                var tCursor = new Vector2(X + 50f, Y);
                batch.DrawString(Fonts.Arial20Bold, Data.FileName, tCursor, Color.Orange);

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
        }
    }
}
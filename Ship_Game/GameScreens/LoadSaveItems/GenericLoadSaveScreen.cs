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
        protected Vector2 Cursor = Vector2.Zero;

        protected Rectangle Window;

        protected Menu1 SaveMenu;

        protected Submenu NameSave;

        protected Submenu AllSaves;

        protected Vector2 TitlePosition;

        protected Vector2 EnternamePos;

        protected UITextEntry EnterNameArea;

        protected ScrollList<SaveLoadListItem> SavesSL;

        protected UIButton DoBtn;

        public enum SLMode { Load, Save }

        protected SLMode mode;

        protected string InitText;
        protected string TitleText;
        protected string OverWriteText = "";
        protected string Path = "";
        protected string TabText;

        protected CloseButton close;
        protected FileInfo fileToDel;
        protected FileData selectedFile;
        protected int eHeight = 55;      // element height

        public GenericLoadSaveScreen(GameScreen parent, SLMode mode, string InitText, string TitleText, string TabText)
            : base(parent)
        {
            this.mode = mode;
            this.InitText = InitText;
            this.TitleText = TitleText;
            this.TabText = TabText;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public GenericLoadSaveScreen(GameScreen parent, SLMode mode, string InitText, string TitleText, string TabText, string OverWriteText) 
            : this(parent, mode, InitText, TitleText, TabText)
        {
            this.OverWriteText = OverWriteText;
        }

        public GenericLoadSaveScreen(GameScreen parent, SLMode mode, string InitText, string TitleText, string TabText, int eHeight) 
            : this(parent, mode, InitText, TitleText, TabText)
        {
            this.eHeight = eHeight;
        }

        public GenericLoadSaveScreen(GameScreen parent, SLMode mode, string InitText, string TitleText, string TabText, string OverWriteText, int eHeight) 
            : this(parent, mode, InitText, TitleText, TabText, OverWriteText)
        {
            this.eHeight = eHeight;
        }

        public virtual void DoSave()
        {
            //SavedGame savedGame = new SavedGame(this.screen, this.EnterNameArea.Text);
            //this.ExitScreen();
        }

        protected virtual void DeleteFile(object sender, EventArgs e)
        {
            GameAudio.EchoAffirmative();
            
            try
            {
                fileToDel.Delete();        // delete the file
            } catch { }

            int iAT = SavesSL.FirstVisibleIndex;
            LoadContent();
            SavesSL.FirstVisibleIndex = iAT;

        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw();
            NameSave.Draw(batch);
            AllSaves.Draw(batch);
            SavesSL.Draw(batch);
            EnterNameArea.Draw(Fonts.Arial12Bold, batch, EnternamePos, GameTime, (EnterNameArea.Hover ? Color.White : Color.Orange));

            base.Draw(batch);

            close.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        protected virtual void Load()
        {
        }
        
        protected abstract void InitSaveList(); // To be implemented in subclasses

        public override void LoadContent()
        {
            Window = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            SaveMenu = new Menu1(Window);
            close = new CloseButton(this, new Rectangle(Window.X + Window.Width - 35, Window.Y + 10, 20, 20));
            var sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            NameSave = new Submenu(sub);
            NameSave.AddTab(TitleText);
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            var scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, Window.Height - sub.Height - 50);
            AllSaves = new Submenu(scrollList);
            AllSaves.AddTab(TabText);
            SavesSL = new ScrollList<SaveLoadListItem>(AllSaves, eHeight, ListControls.Cancel);
            SavesSL.OnClick = OnSaveLoadItemClicked;
            InitSaveList();

            EnternamePos = TitlePosition;
            EnterNameArea = new UITextEntry();

            EnterNameArea.Text = InitText;
            EnterNameArea.ClickableArea = new Rectangle((int)EnternamePos.X, (int)EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            string title = mode == SLMode.Save ? "Save" : "Load";
            DoBtn = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, title, b =>
            {
                if (mode == SLMode.Save)
                    TrySave();
                else if (mode == SLMode.Load)
                    Load();
            });

            base.LoadContent();
        }

        protected virtual void OnSaveLoadItemClicked(SaveLoadListItem item)
        {
            if (item.WasCancelHovered(Input)) // handle file delete
            {
                fileToDel = item.Data.FileLink;
                var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                messageBox.Accepted += DeleteFile;
                ScreenManager.AddScreen(messageBox);
            }
            else
            {
                SwitchFile(item.Data);
            }
        }


        protected void SwitchFile(FileData file)
        {
            if (SLMode.Load == mode)
                selectedFile = file;

            GameAudio.AcceptClick();
            EnterNameArea.Text = file.FileName;
        }


        public override bool HandleInput(InputState input)
        {
            if (SavesSL.HandleInput(input))
                return true;

            if (input.Escaped || input.RightMouseClick || close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }

            if (SLMode.Save == mode) // Only check name field change when saving
            {
                if (!EnterNameArea.ClickableArea.HitTest(MousePos))
                {
                    EnterNameArea.Hover = false;
                }
                else
                {
                    EnterNameArea.Hover = true;
                    if (input.LeftMouseClick)
                    {
                        EnterNameArea.HandlingInput = true;
                        EnterNameArea.Text = "";
                    }
                }
                if (EnterNameArea.HandlingInput)
                {
                    EnterNameArea.HandleTextInput(ref EnterNameArea.Text, input);
                    if (input.IsKeyDown(Keys.Enter))
                    {
                        EnterNameArea.HandlingInput = false;
                    }
                }
            }
            return base.HandleInput(input);
        }

        void OverWriteAccepted(object sender, EventArgs e)
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
            if (IsSaveOk())
            {
                DoSave();
            }
            else
            {
                var messageBox = new MessageBoxScreen(this, OverWriteText);
                messageBox.Accepted += OverWriteAccepted;
                ScreenManager.AddScreen(messageBox);
            }
        }

        protected void AddItemToSaveSL(FileInfo info)
        {
            var item = new SaveLoadListItem(this, new FileData(info, info, info.NameNoExt()));
            SavesSL.AddItem(item);
        }

        protected void AddItemsToSaveSL(IEnumerable<FileData> files)
        {
            foreach (FileData data in files)
                SavesSL.AddItem(new SaveLoadListItem(this, data));
        }

        protected class SaveLoadListItem : ScrollList<SaveLoadListItem>.Entry
        {
            readonly GenericLoadSaveScreen Screen;
            public FileData Data;
            public SaveLoadListItem(GenericLoadSaveScreen screen, FileData data)
            {
                Screen = screen;
                Data = data;
            }
            public override void Draw(SpriteBatch batch)
            {
                var cursor = new Vector2(Screen.AllSaves.X + 20, Y - 7);

                batch.Draw(Data.icon, new Rectangle((int)cursor.X, (int)cursor.Y, 29, 30), Color.White);

                var tCursor = new Vector2(cursor.X + 40f, cursor.Y + 3f);
                batch.DrawString(Fonts.Arial20Bold, Data.FileName, tCursor, Color.Orange);

                tCursor.Y += Fonts.Arial20Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.Info, tCursor, Color.White);

                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, Data.ExtraInfo, tCursor, Color.White);

                DrawCancel(batch, "Delete File");
            }
        }

        protected class FileData
        {
            public string FileName;
            public string Info;
            public string ExtraInfo;
            public SubTexture icon;
            public FileInfo FileLink;
            public object Data;

            public FileData()
            {
            }

            public FileData(FileInfo fileLink, object data, string fileName)
            {
                icon = ResourceManager.Texture("ShipIcons/Wisp");
                FileName = fileName;
                Info = "";
                ExtraInfo = "";
                FileLink = fileLink;
                Data = data;
            }

            public FileData(FileInfo fileLink, object data, string fileName, string info, string extraInfo)
            {
                icon = ResourceManager.Texture("ShipIcons/Wisp");
                FileName = fileName;
                Info = info;
                ExtraInfo = extraInfo;
                FileLink = fileLink;
                Data = data;
            }

            public FileData(FileInfo fileLink, object data, string fileName, string info, string extraInfo, SubTexture icon)
            {
                this.icon = icon;
                FileName = fileName;
                Info = info;
                ExtraInfo = extraInfo;
                FileLink = fileLink;
                Data = data;
            }
        }
    }
}
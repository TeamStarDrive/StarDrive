using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

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

        protected ScrollList SavesSL;

        protected UIButton DoBtn;

        public enum SLMode { Load, Save };

        protected SLMode mode;

        protected string InitText;
        protected string TitleText;
        protected string OverWriteText = "";
        protected string Path = "";
        protected string TabText;

        protected CloseButton close;

        protected MouseState currentMouse;

        protected MouseState previousMouse;

        protected Selector selector;

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
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
            GameAudio.PlaySfxAsync("echo_affirm");
            
            try
            {
                this.fileToDel.Delete();        // delete the file
            } catch { }

            int iAT = this.SavesSL.FirstVisibleIndex;
            this.LoadContent();
            this.SavesSL.FirstVisibleIndex = iAT;

        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw();
            NameSave.Draw();
            AllSaves.Draw();
            var bCursor = new Vector2(AllSaves.Menu.X + 20, AllSaves.Menu.Y + 20);
            foreach (ScrollList.Entry e in SavesSL.VisibleEntries)
            {
                var data = (FileData)e.item;
                bCursor.Y = (float)e.Y - 7;
                batch.Draw(data.icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                batch.DrawString(Fonts.Arial20Bold, data.FileName, tCursor, Color.Orange);
                tCursor.Y += Fonts.Arial20Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, data.Info, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12Bold, data.ExtraInfo, tCursor, Color.White);
                e.DrawCancel(batch, Input, "Delete File");
            }
            SavesSL.Draw(batch);
            EnterNameArea.Draw(Fonts.Arial12Bold, batch, EnternamePos, GameTime, (EnterNameArea.Hover ? Color.White : Color.Orange));

            base.Draw(batch);

            selector?.Draw(batch);
            close.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        protected virtual void Load()
        {

        }

        protected void SwitchFile(ScrollList.Entry e)
        {
            if( SLMode.Load == this.mode )
                this.selectedFile = (e.item as FileData);

            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            this.EnterNameArea.Text = (e.item as FileData).FileName;
        }


        public override bool HandleInput(InputState input)
        {
            currentMouse = input.MouseCurr;
            SavesSL.HandleInput(input);
            selector = null;
            foreach (ScrollList.Entry e in SavesSL.VisibleEntries)
            {
                if (!e.CheckHover(input))
                    continue;

                selector = e.CreateSelector();
                if (e.WasCancelHovered(Input) && input.InGameSelect) // handle file delete
                {
                    fileToDel = e.Get<FileData>().FileLink;
                    var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                    messageBox.Accepted += DeleteFile;
                    ScreenManager.AddScreen(messageBox);
                }
                else if (input.InGameSelect)
                {
                    SwitchFile(e);
                }
            }
            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
            {
                this.ExitScreen();
                return true;
            }
            if (SLMode.Save == mode)       // Only check name field change when saving
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
            previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        protected abstract void InitSaveList();        // To be implemented in subclasses

        public override void LoadContent()
        {
            this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            this.SaveMenu = new Menu1(this.Window);
            this.close = new CloseButton(this, new Rectangle(this.Window.X + this.Window.Width - 35, this.Window.Y + 10, 20, 20));
            var sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
            this.NameSave = new Submenu(sub);
            this.NameSave.AddTab(this.TitleText);
            this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
            var scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
            this.AllSaves = new Submenu(scrollList);
            this.AllSaves.AddTab(this.TabText);
            SavesSL = new ScrollList(AllSaves, eHeight, ListControls.Cancel);
            InitSaveList();

            EnternamePos = TitlePosition;
            EnterNameArea = new UITextEntry();

            EnterNameArea.Text = this.InitText;
            EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

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

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            DoSave();
        }

        private bool IsSaveOk()
        {
            foreach (FileData data in SavesSL.AllItems<FileData>())
                if (EnterNameArea.Text == data.FileName) // check if item already exists
                    return false;
            return true;
        }

        private void TrySave()
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

        protected class FileData
        {
            public string FileName;
            public string Info;
            public string ExtraInfo;
            public Texture2D icon;
            public FileInfo FileLink;
            public object Data;

            public FileData()
            {
            }

            public FileData(FileInfo fileLink, object data, string fileName)
            {
                this.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];
                this.FileName = fileName;
                this.Info = "";
                this.ExtraInfo = "";
                this.FileLink = fileLink;
                this.Data = data;
            }

            public FileData(FileInfo fileLink, object data, string fileName, string info, string extraInfo)
            {
                this.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];
                this.FileName = fileName;
                this.Info = info;
                this.ExtraInfo = extraInfo;
                this.FileLink = fileLink;
                this.Data = data;
            }

            public FileData(FileInfo fileLink, object data, string fileName, string info, string extraInfo, Texture2D icon)
            {
                this.icon = icon;
                this.FileName = fileName;
                this.Info = info;
                this.ExtraInfo = extraInfo;
                this.FileLink = fileLink;
                this.Data = data;
            }
        }
    }
}
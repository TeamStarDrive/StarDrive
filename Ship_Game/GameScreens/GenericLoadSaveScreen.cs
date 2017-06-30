using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Ship_Game
{
    public class GenericLoadSaveScreen : GameScreen
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

        protected string InitText = "";

        protected string TitleText = "";

        protected string OverWriteText = "";

        protected string Path = "";

        protected string TabText = "";

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

        protected override void Destroy()
        {
            SavesSL?.Dispose(ref SavesSL);
            base.Destroy();
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

            int iAT = this.SavesSL.indexAtTop;
            this.Buttons.Clear();
            this.LoadContent();
            this.SavesSL.indexAtTop = iAT;

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.SaveMenu.Draw();
            this.NameSave.Draw();
            this.AllSaves.Draw();
            Vector2 bCursor = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
            for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.SavesSL.Entries[i];
                FileData data = e.item as FileData;
                bCursor.Y = (float)e.clickRect.Y - 7;
                base.ScreenManager.SpriteBatch.Draw(data.icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, data.FileName, tCursor, Color.Orange);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial20Bold.LineSpacing;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data.Info, tCursor, Color.White);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data.ExtraInfo, tCursor, Color.White);
                if (e.clickRectHover != 1)
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete"], e.cancel, Color.White);
                }
                else
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"], e.cancel, Color.White);
                    if (e.cancel.HitTest(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
                    {
                        base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover2"], e.cancel, Color.White);
                        ToolTip.CreateTooltip("Delete File");
                    }
                }
            }
            this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
            this.EnterNameArea.Draw(Fonts.Arial12Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, GameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
            foreach (UIButton b in this.Buttons)
            {
                b.Draw(base.ScreenManager.SpriteBatch);
            }
            if (this.selector != null)
            {
                this.selector.Draw(ScreenManager.SpriteBatch);
            }
            this.close.Draw(ScreenManager.SpriteBatch);
            ToolTip.Draw(ScreenManager.SpriteBatch);
            base.ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
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
            this.currentMouse = input.MouseCurr;
            Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            this.SavesSL.HandleInput(input);
            this.selector = null;
            for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.SavesSL.Entries[i];
                if (!e.clickRect.HitTest(MousePos))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    if (e.clickRectHover == 0)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    this.selector = new Selector(e.clickRect);
                    if (e.cancel.HitTest(MousePos) && input.InGameSelect)        // handle file delete
                    {
                        this.fileToDel = (e.item as FileData).FileLink;
                        MessageBoxScreen messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                        messageBox.Accepted += DeleteFile;
                        base.ScreenManager.AddScreen(messageBox);
                    } else if (input.InGameSelect)
                    {
                        this.SwitchFile(e);
                    }
                }
            }
            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
            {
                this.ExitScreen();
                return true;
            }
            foreach (UIButton b in this.Buttons)
            {
                if (!b.Rect.HitTest(MousePos))
                {
                    b.State = UIButton.PressState.Default;
                }
                else
                {
                    b.State = UIButton.PressState.Hover;
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        b.State = UIButton.PressState.Pressed;
                    }
                    if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
                    {
                        continue;
                    }
                    string text = b.Launches;
                    if (text == null)
                    {
                        continue;
                    }
                    if (text == "DoBtn")
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        if(mode == SLMode.Save)
                            this.TrySave();
                        else if (mode == SLMode.Load)
                            this.Load();
                    }
                }
            }
            if (SLMode.Save == this.mode)       // Only check name field change when saving
            {
                if (!this.EnterNameArea.ClickableArea.HitTest(MousePos))
                {
                    this.EnterNameArea.Hover = false;
                }
                else
                {
                    this.EnterNameArea.Hover = true;
                    if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        this.EnterNameArea.HandlingInput = true;
                        this.EnterNameArea.Text = "";
                    }
                }
                if (this.EnterNameArea.HandlingInput)
                {
                    this.EnterNameArea.HandleTextInput(ref this.EnterNameArea.Text);
                    if (input.KeysCurr.IsKeyDown(Keys.Enter))
                    {
                        this.EnterNameArea.HandlingInput = false;
                    }
                }
            }
            this.previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        protected virtual void SetSavesSL()        // To be overridden in subclasses
        {

        }

        public override void LoadContent()
        {
            this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            this.SaveMenu = new Menu1(this.Window);
            this.close = new CloseButton(this, new Rectangle(this.Window.X + this.Window.Width - 35, this.Window.Y + 10, 20, 20));
            Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
            this.NameSave = new Submenu(sub);
            this.NameSave.AddTab(this.TitleText);
            this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
            Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
            this.AllSaves = new Submenu(scrollList);
            this.AllSaves.AddTab(this.TabText);
            this.SavesSL = new ScrollList(this.AllSaves, this.eHeight, true, false, false, false);

            this.SetSavesSL();

            this.SavesSL.indexAtTop = 0;
            this.EnternamePos = this.TitlePosition;
            this.EnterNameArea = new UITextEntry();

            EnterNameArea.Text = this.InitText;
            EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            DoBtn = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, "DoBtn", mode == SLMode.Save ? "Save" : "Load");

            base.LoadContent();
        }

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            this.DoSave();
        }

        private void TrySave()
        {
            bool SaveOK = true;

            foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text == (entry.item as FileData).FileName)       // check if item already exists
                {
                    SaveOK = false;
                    break;
                }
            }

            if (SaveOK)
            {
                this.DoSave();
            }
            else
            {
                MessageBoxScreen messageBox = new MessageBoxScreen(this, OverWriteText);
                messageBox.Accepted += OverWriteAccepted;
                base.ScreenManager.AddScreen(messageBox);
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
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

            /*public FileData(FileInfo fileLink, object data)
            {
                this.icon = ResourceManager.TextureDict["ShipIcons/Wisp"];
                this.FileName = "";
                this.Info = "";
                this.ExtraInfo = "";
                this.FileLink = fileLink;
                this.Data = data;
            }*/

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
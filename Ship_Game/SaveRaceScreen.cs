using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class SaveRaceScreen : GameScreen, IDisposable
    {
        private Vector2 Cursor = Vector2.Zero;

        private RaceDesignScreen screen;

        private List<UIButton> Buttons = new List<UIButton>();

        //private Submenu subSave;

        private Rectangle Window;

        private Menu1 SaveMenu;

        private Submenu NameSave;

        private Submenu AllSaves;

        private Vector2 TitlePosition;

        private Vector2 EnternamePos;

        private UITextEntry EnterNameArea;

        private ScrollList SavesSL;

        private UIButton Save;

        private CloseButton close;

        //private FileInfo activeFile;

        private MouseState currentMouse;

        private MouseState previousMouse;

        private Selector selector;

        private EmpireData data;

        //private float transitionElapsedTime;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        public SaveRaceScreen(RaceDesignScreen screen, EmpireData data)
        {
            this.screen = screen;
            this.data = data;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SaveRaceScreen() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.SavesSL != null)
                        this.SavesSL.Dispose();

                }
                this.SavesSL = null;
                this.disposed = true;
            }
        }

        public void DoSave()
        {
            data.Traits.Name = this.EnterNameArea.Text;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            XmlSerializer Serializer = new XmlSerializer(typeof(EmpireData));
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Races/", data.Traits.Name, ".xml"));
            Serializer.Serialize(WriteFileStream, data);
            //WriteFileStream.Close();
            WriteFileStream.Dispose();
            this.ExitScreen();
        }

        public override void Draw(GameTime gameTime)
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
                EmpireData data = e.item as EmpireData;
                bCursor.Y = (float)e.clickRect.Y;
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, data.Traits.Name, tCursor, Color.Orange);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial20Bold.LineSpacing;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, String.Concat("Base: ", data.PortraitName), tCursor, Color.White);
            }
            this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
            this.EnterNameArea.Draw(Fonts.Arial12Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
            foreach (UIButton b in this.Buttons)
            {
                b.Draw(base.ScreenManager.SpriteBatch);
            }
            if (this.selector != null)
            {
                this.selector.Draw();
            }
            this.close.Draw(base.ScreenManager);
            base.ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
        }


        public override void HandleInput(InputState input)
        {
            this.currentMouse = input.CurrentMouseState;
            Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            this.SavesSL.HandleInput(input);
            this.selector = null;
            for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.SavesSL.Entries[i];
                if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    if (e.clickRectHover == 0)
                    {
                        AudioManager.PlayCue("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    this.selector = new Selector(base.ScreenManager, e.clickRect);
                    if (input.InGameSelect)
                    {
                        this.data = e.item as EmpireData;
                        AudioManager.PlayCue("sd_ui_accept_alt3");
                        this.EnterNameArea.Text = (e.item as EmpireData).Traits.Name;
                    }
                }
            }
            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
            {
                this.ExitScreen();
            }
            foreach (UIButton b in this.Buttons)
            {
                if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
                {
                    b.State = UIButton.PressState.Normal;
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
                    string text = b.Text;
                    string str = text;
                    if (text == null)
                    {
                        continue;
                    }
                    if (str == "Save")
                    {
                        AudioManager.PlayCue("echo_affirm");
                        this.TrySave();
                    }
                    else if (str == "Exit to Windows")
                    {
                        Game1.Instance.Exit();
                    }
                }
            }
            if (!HelperFunctions.CheckIntersection(this.EnterNameArea.ClickableArea, MousePos))
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
                if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter))
                {
                    this.EnterNameArea.HandlingInput = false;
                }
            }
            this.previousMouse = input.LastMouseState;
            base.HandleInput(input);
        }

        public override void LoadContent()
        {
            this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
            this.close = new CloseButton(new Rectangle(this.Window.X + this.Window.Width - 35, this.Window.Y + 10, 20, 20));
            Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
            this.NameSave = new Submenu(base.ScreenManager, sub);
            this.NameSave.AddTab("Save Race As...");
            Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
            this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
            Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
            this.AllSaves = new Submenu(base.ScreenManager, scrollList);
            this.AllSaves.AddTab("All Saves");
            this.SavesSL = new ScrollList(this.AllSaves, 55);
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            List<EmpireData> saves = new List<EmpireData>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Races/"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
                    EmpireData data = (EmpireData)serializer1.Deserialize(file);
                    saves.Add(data);
                    file.Dispose();
                }
                catch
                {
                    file.Dispose();
                }
                //Label0:
                //  continue;
            }
            IOrderedEnumerable<EmpireData> sortedList =
                from data in saves
                orderby data.Traits.Name descending
                select data;
            foreach (EmpireData data in sortedList)
            {
                this.SavesSL.AddItem(data);
            }
            this.SavesSL.indexAtTop = 0;
            this.EnternamePos = this.TitlePosition;
            this.EnterNameArea = new UITextEntry();

            EnterNameArea.Text = this.data.Traits.Name;
            EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            this.Save = new UIButton()
            {
                Rect = new Rectangle(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
                Text = "Save"
            };
            this.Buttons.Add(this.Save);
            Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
            base.LoadContent();
            //base.LoadContent();
        }

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            this.DoSave();
        }

        private void TrySave()
        {
            bool needOverWriteConfirmation = true;
            bool SaveOK = true;
            foreach (ScrollList.Entry entry in this.SavesSL.Entries)
            {
                if (this.EnterNameArea.Text != (entry.item as EmpireData).Traits.Name)
                {
                    continue;
                }
                needOverWriteConfirmation = true;
                SaveOK = false;
            }
            if (SaveOK)
            {
                this.DoSave();
                return;
            }
            if (needOverWriteConfirmation)
            {
                MessageBoxScreen messageBox = new MessageBoxScreen("Saved Race already exists.  Overwrite?");
                messageBox.Accepted += new EventHandler<EventArgs>(this.OverWriteAccepted);
                base.ScreenManager.AddScreen(messageBox);
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}
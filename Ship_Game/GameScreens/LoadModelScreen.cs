using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class ModelData
    {
        public string Name;
        public FileInfo FileInfo;
    }

    public sealed class LoadModelScreen : GameScreen
    {
        private ShipToolScreen screen;
        //private MainMenuScreen mmscreen;
        //private Submenu subSave;
        private Rectangle Window;
        private Menu1 SaveMenu;
        private Submenu AllSaves;
        private Vector2 TitlePosition;
        private ScrollList SavesSL;
        private Selector selector;
        private FileInfo activeFile;

        //private float transitionElapsedTime;

        public LoadModelScreen(ShipToolScreen screen) : base(screen)
        {
            this.screen = screen;
            IsPopup = true;
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Begin();
            SaveMenu.Draw();
            AllSaves.Draw(batch);
            var bCursor = new Vector2(AllSaves.Menu.X + 20, AllSaves.Menu.Y + 20);
            foreach (ScrollList.Entry e in SavesSL.VisibleExpandedEntries)
            {
                bCursor.Y = e.Y;
                if (e.item is ModuleHeader header)
                {
                    header.Draw(ScreenManager, bCursor);
                }
                else if (e.item is ModelData data)
                {
                    batch.Draw(ResourceManager.Texture("ShipIcons/Wisp"),
                        new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial20Bold, data.Name, tCursor, Color.Orange);
                }
            }
            SavesSL.Draw(batch);
            base.Draw(batch);
            selector?.Draw(batch);
            batch.End();
        }

        private void OnLoadClicked()
        {
            if (activeFile != null)
            {
                screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(activeFile));
            }
            else
            {
                GameAudio.PlaySfxAsync("UI_Misc20");
            }
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            selector = null;
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
            }

            SavesSL.HandleInput(input);
            foreach (ScrollList.Entry e in SavesSL.VisibleExpandedEntries)
            {
                if (!e.CheckHover(input.CursorPosition))
                    continue;

                selector = e.CreateSelector();
                if (!input.InGameSelect)
                    continue;
                if (!(e.item is ModuleHeader moduleHeader) || !moduleHeader.HandleInput(input, e))
                {
                    var modelData = e.item as ModelData;
                    if (modelData == null)
                        continue;
                    LoadModel(modelData);
                }
                else return true; // scrollList entry clicked
            }
            return base.HandleInput(input);
        }

        private bool LoadModel(ModelData modelData)
        {
            try
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");

                string relativePath = modelData.FileInfo.RelPath().Replace("Content\\", "");
                screen.LoadModel(relativePath);

                activeFile = modelData.FileInfo;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void LoadContent()
        {
            Window = new Rectangle(0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 400, 600);
            var sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            var scrollList = new Rectangle(sub.X, sub.Y, sub.Width, Window.Height - 45);

            SaveMenu = new Menu1(Window);
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            AllSaves = new Submenu(scrollList);
            AllSaves.AddTab("Load Model");

            SavesSL = new ScrollList(AllSaves, 55);
            SavesSL.AddItem(new ModuleHeader("XNB Models"));
            SavesSL.AddItem(new ModuleHeader("OBJ Models"));

            FileInfo[] xnbModels = ResourceManager.GetAllXnbModelFiles("Model/Ships");
            FileInfo[] objModels = ResourceManager.GatherFilesUnified("Model/Ships", "obj");
            foreach (FileInfo file in xnbModels)
            {
                SavesSL.EntryAt(0).AddSubItem(new ModelData { Name = file.Name, FileInfo = file });
            }
            foreach (FileInfo file in objModels)
            {
                SavesSL.EntryAt(1).AddSubItem(new ModelData { Name = file.Name, FileInfo = file });
            }
            base.LoadContent();
        }
    }
}
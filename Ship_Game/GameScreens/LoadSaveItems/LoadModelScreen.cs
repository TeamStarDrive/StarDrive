using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class ModelData
    {
        public string Name;
        public FileInfo FileInfo;
    }

    public sealed class LoadModelScreen : GameScreen
    {
        ShipToolScreen Screen;
        Rectangle Window;
        Menu1 SaveMenu;
        Submenu AllSaves;
        ScrollList<LoadModelListItem> SavesSL;
        FileInfo activeFile;

        public LoadModelScreen(ShipToolScreen screen) : base(screen)
        {
            Screen = screen;
            IsPopup = true;
        }

        class LoadModelListItem : ScrollListItem<LoadModelListItem>
        {
            public ModelData Model;
            public LoadModelListItem(ModelData model) { Model = model; }
            public LoadModelListItem(string headerText) : base(headerText) {}
            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);
                if (Model != null)
                {
                    batch.Draw(ResourceManager.Texture("ShipIcons/Wisp"),
                    new Rectangle((int)X, (int)Y, 29, 30), Color.White);
                    var tCursor = new Vector2(X + 40f, Y + 3f);
                    batch.DrawString(Fonts.Arial20Bold, Model.Name, tCursor, Color.Orange);
                }
            }
        }

        public override void LoadContent()
        {
            Window = new Rectangle(0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 400, 600);
            var sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            var scrollList = new Rectangle(sub.X, sub.Y, sub.Width, Window.Height - 45);

            SaveMenu = new Menu1(Window);
            AllSaves = new Submenu(scrollList);
            AllSaves.AddTab("Load Model");

            SavesSL = new ScrollList<LoadModelListItem>(AllSaves, 55);
            SavesSL.OnClick = OnLoadModelClicked;
            SavesSL.AddItem(new LoadModelListItem("XNB Models"));
            SavesSL.AddItem(new LoadModelListItem("OBJ Models"));
            SavesSL.AddItem(new LoadModelListItem("FBX Models"));

            Array<FileInfo> xnbModels = ResourceManager.GetAllXnbModelFiles("Model/Ships");
            FileInfo[] objModels = ResourceManager.GatherFilesUnified("Model/Ships", "obj");
            FileInfo[] fbxModels = ResourceManager.GatherFilesUnified("Model/Ships", "fbx");
            foreach (FileInfo file in xnbModels)
            {
                SavesSL[0].AddSubItem(new LoadModelListItem(new ModelData{ Name = file.Name, FileInfo = file }));
            }
            foreach (FileInfo file in objModels)
            {
                SavesSL[1].AddSubItem(new LoadModelListItem(new ModelData{ Name = file.Name, FileInfo = file }));
            }
            foreach (FileInfo file in fbxModels)
            {
                SavesSL[2].AddSubItem(new LoadModelListItem(new ModelData{ Name = file.Name, FileInfo = file }));
            }
            base.LoadContent();
        }

        void OnLoadModelClicked(LoadModelListItem item)
        {
            LoadModel(item.Model);
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Begin();
            SaveMenu.Draw(batch);
            AllSaves.Draw(batch);
            SavesSL.Draw(batch);
            base.Draw(batch);
            batch.End();
        }

        void OnLoadClicked()
        {
            if (activeFile != null)
            {
                Screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(activeFile));
            }
            else
            {
                GameAudio.NegativeClick();
            }
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return SavesSL.HandleInput(input) && base.HandleInput(input);
        }

        bool LoadModel(ModelData modelData)
        {
            try
            {
                GameAudio.AcceptClick();

                string relativePath = modelData.FileInfo.RelPath().Replace("Content\\", "");
                Screen.LoadModel(relativePath);

                activeFile = modelData.FileInfo;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
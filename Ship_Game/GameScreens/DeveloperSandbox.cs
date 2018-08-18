using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        GameScreen BaseScreen;
        UniverseScreen UniverseScreen;
        public DeveloperSandbox(GameScreen parent) : base(parent)
        {
            parent.ExitScreen();
            IsPopup = true;
        }

        public override void Update(float deltaTime)
        {
            ScreenState = ScreenState.Active;
            base.Update(deltaTime);
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            ResourceManager.LoadItAll();
            UniverseData universeSandBox = new UniverseData
            {
                Size = new Vector2(1000000f)
            };
            
            foreach(var empireData in ResourceManager.Empires)
            {
                Empire empireFromEmpireData = EmpireManager.CreateEmpireFromEmpireData(empireData);
                universeSandBox.EmpireList.Add(empireFromEmpireData);
                EmpireManager.Add(empireFromEmpireData);
            }

            UniverseScreen = new UniverseScreen(universeSandBox);


            //var playerEmpire = new Empire()
            //{
            //    EmpireColor = currentObjectColor,
            //    data = SelectedData
            //};
            //playerEmpire.data.SpyModifier = RaceSummary.SpyMultiplier;
            //playerEmpire.data.Traits.Spiritual = RaceSummary.Spiritual;
            //RaceSummary.Adj1 = SelectedData.Traits.Adj1;
            //RaceSummary.Adj2 = SelectedData.Traits.Adj2;
            //playerEmpire.data.Traits = RaceSummary;
            //playerEmpire.EmpireColor = currentObjectColor;

            //empire.Initialize();



            //Universe = new UniverseScreen(Data)
            //{
            //    player = PlayerEmpire,
            //    CamPos = new Vector3(-playerShip.Center.X, playerShip.Center.Y, 5000f),
            //    ScreenManager = ScreenManager,
            //    GameDifficulty = Difficulty,
            //    GameScale = Scale
            //};
        }
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, false, false);
            HandleInput();

        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Begin();
            base.Draw(batch);
            batch.End();

        }

        //after Creating the universe
        public void HandleInput()
        {
            if (Input.Escaped)
            {

            }
            base.HandleInput(Input);
        }

        //as a normal game screen
        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ExitScreen();
                ScreenManager.AddScreen(new MainMenuScreen());
                return true;
            }
            if (Input.LeftMouseClick)
            {
                //ScreenManager.AddScreen(new LoadSaveScreen(this));
                ScreenManager.AddScreen(UniverseScreen);
            }
            return base.HandleInput(input);
        }
    }
}

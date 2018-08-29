using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            EmpireManager.Clear();
            ResourceManager.LoadItAll();
            UniverseData universeSandBox = new UniverseData
            {
                Size = new Vector2(1000000f)
            };
            UniverseData.UniverseWidth = universeSandBox.Size.X * 2;
            CurrentGame.StartNew(universeSandBox);
            var claimedSpots = new Array<Vector2>();
            foreach (var empireData in ResourceManager.Empires)
            {
                Empire empireFromEmpireData = EmpireManager.CreateEmpireFromEmpireData(empireData);
                universeSandBox.EmpireList.Add(empireFromEmpireData);
                EmpireManager.Add(empireFromEmpireData);
                empireFromEmpireData.data.CurrentAutoScout = empireFromEmpireData.data.ScoutShip;
                empireFromEmpireData.data.CurrentAutoColony = empireFromEmpireData.data.ColonyShip;
                empireFromEmpireData.data.CurrentAutoFreighter = empireFromEmpireData.data.FreighterShip;
                empireFromEmpireData.data.CurrentConstructor = empireFromEmpireData.data.ConstructorShip;
                GenerateRandomSysPos(10000, claimedSpots, universeSandBox);
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                foreach (Empire e in EmpireManager.Empires)
                {
                    if (empire == e)
                        continue;

                    var r = new Relationship(e.data.Traits.Name);
                    empire.AddRelationships(e, r);

                }
            }

            int empireIndex = 0;
            foreach (var position in claimedSpots)
            {


                var solarSystem = new SolarSystem();
                solarSystem.GenerateStartingSystem($"SandBox-{empireIndex}", universeSandBox, 1, universeSandBox.EmpireList[empireIndex]);
                universeSandBox.SolarSystemsList.Add(solarSystem);
                solarSystem.Position = position;
                solarSystem.OwnerList.Add(universeSandBox.EmpireList[empireIndex]);
                foreach (Planet planet2 in solarSystem.PlanetList)
                    planet2.SetExploredBy(universeSandBox.EmpireList[empireIndex]);
                empireIndex++;
            }
            foreach(var system in universeSandBox.SolarSystemsList)
            {
                do
                {
                    var ss = universeSandBox.SolarSystemsList.FindMaxFiltered(
                        check => !system.FiveClosestSystems.Contains(check)
                        , range => -system.Position.SqDist(range.Position));
                    system.FiveClosestSystems.Add(ss);
                }
                while (system.FiveClosestSystems.Count < 5);
            }
            universeSandBox.EmpireList.First.isPlayer = true;
            universeSandBox.playerShip = Ship.CreateShipAtPoint("Unarmed Scout", universeSandBox.EmpireList.First, claimedSpots[0]);
            universeSandBox.MasterShipList.Add(universeSandBox.playerShip);

            foreach (var system in universeSandBox.SolarSystemsList)
                SubmitSceneObjectsForRendering(universeSandBox, system);
            UniverseScreen = new UniverseScreen(universeSandBox)
                { PlayerEmpire = universeSandBox.EmpireList.First };
            UniverseScreen.player = UniverseScreen.PlayerEmpire;
            UniverseScreen.ResetLighting();
            //ScreenManager.AddScreen(UniverseScreen);

        }

        public Vector2 GenerateRandomSysPos(float spacing, Array<Vector2> claimedSpots, UniverseData data)
        {
            float safteyBreak = 1;
            Vector2 sysPos;
            do
            {
                spacing *= safteyBreak;
                sysPos = RandomMath.Vector2D(data.Size.X - 100000f);
                safteyBreak *= .97f;
            } while (!SystemPosOK(sysPos, spacing, claimedSpots, data));

            claimedSpots.Add(sysPos);
            return sysPos;
        }

        private bool SystemPosOK(Vector2 sysPos, float spacing, Array<Vector2> claimedSpots, UniverseData data)
        {
            foreach (Vector2 vector2 in claimedSpots)
            {
                if (Vector2.Distance(vector2, sysPos) < spacing
                    || sysPos.X > data.Size.X || sysPos.Y > data.Size.Y
                    || sysPos.X < -data.Size.X || sysPos.Y < -data.Size.Y)
                    return false;
            }
            return true;
        }
        private void SubmitSceneObjectsForRendering(UniverseData data, SolarSystem solarSystem)
        {
            SolarSystem wipSystem = solarSystem;

            foreach (Planet planet in wipSystem.PlanetList)
            {
                planet.ParentSystem = wipSystem;
                planet.Center += wipSystem.Position;
                planet.InitializePlanetMesh(this);
            }
            foreach (Asteroid asteroid in wipSystem.AsteroidsList)
            {
                asteroid.Position3D.X += wipSystem.Position.X;
                asteroid.Position3D.Y += wipSystem.Position.Y;
                asteroid.Initialize();
                AddObject(asteroid.So);
            }
            foreach (Moon moon in wipSystem.MoonList)
            {
                moon.Initialize();
                AddObject(moon.So);
            }
            foreach (Ship ship in wipSystem.ShipList)
            {
                ship.Position = ship.loyalty.GetPlanets()[0].Center + new Vector2(6000f, 2000f);
                ship.InitializeShip();
            }
        }


    }
}

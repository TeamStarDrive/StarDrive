using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        const int NumEmpires = 1;

        UniverseScreen Universe;
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
                ScreenManager.AddScreen(Universe);
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            EmpireManager.Clear();
            ResourceManager.LoadItAll();
            var sandbox = new UniverseData { Size = new Vector2(500000f) };
            UniverseData.UniverseWidth = sandbox.Size.X * 2;
            CurrentGame.StartNew(sandbox);
            var claimedSpots = new Array<Vector2>();

            for (int i = 0; i < NumEmpires && i < ResourceManager.Empires.Count; ++i)
            {
                Empire e = EmpireManager.CreateEmpireFromEmpireData(ResourceManager.Empires[i]);
                sandbox.EmpireList.Add(e);
                EmpireManager.Add(e);
                e.data.CurrentAutoScout = e.data.ScoutShip;
                e.data.CurrentAutoColony = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor = e.data.ConstructorShip;
                GenerateRandomSysPos(10000, claimedSpots, sandbox);
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                foreach (Empire e in EmpireManager.Empires)
                    if (empire != e) empire.AddRelationships(e, new Relationship(e.data.Traits.Name));
            }

            for (int empireIndex = 0; empireIndex < sandbox.EmpireList.Count; ++empireIndex)
            {
                Empire e = sandbox.EmpireList[empireIndex];
                var system = new SolarSystem();
                system.OwnerList.Add(e);
                sandbox.SolarSystemsList.Add(system);
                system.Position = claimedSpots[empireIndex];
                system.GenerateStartingSystem($"SandBox-{empireIndex}", sandbox, 1, e);
                foreach (Planet p in system.PlanetList)
                    p.SetExploredBy(e);
            }
            foreach(SolarSystem system in sandbox.SolarSystemsList)
            {
                system.FiveClosestSystems = sandbox.SolarSystemsList.FindMinItemsFiltered(5,
                                                filter => filter != system,
                                                select => select.Position.SqDist(system.Position));
            }
            sandbox.EmpireList.First.isPlayer = true;
            sandbox.playerShip = Ship.CreateShipAtPoint("Unarmed Scout", sandbox.EmpireList.First, claimedSpots[0]);
            sandbox.MasterShipList.Add(sandbox.playerShip);

            foreach (SolarSystem system in sandbox.SolarSystemsList)
                SubmitSceneObjectsForRendering(sandbox, system);
            Universe = new UniverseScreen(sandbox) { PlayerEmpire = sandbox.EmpireList.First };
            Universe.player = Universe.PlayerEmpire;
            Universe.NoEliminationVictory = true; // SandBox mode doesn't have elimination victory
            Universe.ResetLighting();
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
        private void SubmitSceneObjectsForRendering(UniverseData data, SolarSystem wipSystem)
        {
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

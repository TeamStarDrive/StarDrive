using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.Sandbox;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        const int NumOpponents = 0;
        const bool PlayerIsCybernetic = false;
        string PlayerPreference = "Kulrathi";
        DeveloperUniverse Universe;
        TaskResult<UniverseData> CreateTask;

        public DeveloperSandbox() : base(null)
        {
            IsPopup = true;
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            CreateTask = Parallel.Run(CreateSandboxUniverse);
        }
        
        // as a normal game screen
        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true); // no return
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(float deltaTime)
        {
            if (CreateTask != null)
            {
                Thread.Sleep(10); // @note This hugely speeds up loading
                if (CreateTask?.IsComplete == true)
                {
                    UniverseData sandbox = CreateTask.Result;
                    CreateTask = null;
                    Universe = new DeveloperUniverse(sandbox) { PlayerEmpire = sandbox.EmpireList.First };
                    ScreenManager.GoToScreen(Universe, clear3DObjects:false);
                }
            }
            ScreenState = ScreenState.Active;
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.Begin();
            base.Draw(batch);
            batch.End();
        }

        public class DeveloperUniverse : UniverseScreen
        {
            public DeveloperUniverse(UniverseData sandbox) : base(sandbox)
            {
                player = PlayerEmpire;
                NoEliminationVictory = true; // SandBox mode doesn't have elimination victory
                Paused = false;
                ResetLighting();
            }
            public override void LoadContent()
            {
                base.LoadContent();

                // nice zoom in effect, we set the cam height to super high
                CamHeight *= 10000.0f;
                CamDestination.Z = 100000.0f; // and set a lower destination
            }
        }

        UniverseData CreateSandboxUniverse()
        {
            Stopwatch s = Stopwatch.StartNew();
            EmpireManager.Clear();

            var sandbox = new UniverseData { Size = new Vector2(500000f) };
            CurrentGame.StartNew(sandbox, pace:1f);

            bool PlayerFilter(IEmpireData d)
            {
                if (PlayerPreference.NotEmpty())
                    return d.Name.Contains(PlayerPreference);
                return d.IsCybernetic == PlayerIsCybernetic;
            }

            IEmpireData player = RandomMath.RandItem(ResourceManager.MajorRaces.Filter(PlayerFilter));

            IEmpireData[] opponents = ResourceManager.MajorRaces.Filter(data => data.Name != player.Name);

            var races = new Array<IEmpireData>(opponents);
            races.Shuffle();
            races.Resize(Math.Min(races.Count, NumOpponents)); // truncate
            races.Insert(0, player);

            foreach (IEmpireData data in races)
            {
                Empire e = sandbox.CreateEmpire(data);
                if (data == player) e.isPlayer = true;

                e.data.CurrentAutoScout     = e.data.ScoutShip;
                e.data.CurrentAutoColony    = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor   = e.data.ConstructorShip;

                // Now, generate system for our empire:
                var system = new SolarSystem();
                system.Position = GenerateRandomSysPos(10000, sandbox);
                system.GenerateStartingSystem(e.data.Traits.HomeSystemName, sandbox, 1f, e);
                system.OwnerList.Add(e);
                sandbox.SolarSystemsList.Add(system);

                foreach (Planet p in system.PlanetList)
                {
                    if (e.isPlayer)
                        p.colonyType = Planet.ColonyType.Colony; // this is required to disable governors... for some reason
                    p.SetExploredBy(e);
                }
            }

            foreach (IEmpireData data in ResourceManager.MinorRaces) // init minor races
            {
                sandbox.CreateEmpire(data);
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                foreach (Empire e in EmpireManager.Empires)
                    if (empire != e) empire.AddRelationships(e, new Relationship(e.data.Traits.Name));
            }

            foreach(SolarSystem system in sandbox.SolarSystemsList)
            {
                system.FiveClosestSystems = sandbox.SolarSystemsList.FindMinItemsFiltered(5,
                                            filter => filter != system,
                                            select => select.Position.SqDist(system.Position));
            }

            Empire playerEmpire = sandbox.EmpireList.First;
            Planet homePlanet = playerEmpire.GetPlanets()[0];
            sandbox.playerShip = Ship.CreateShipAtPoint("Unarmed Scout", playerEmpire, homePlanet.Center);
            sandbox.playerShip.VanityName = "Developer's Scout";
            sandbox.MasterShipList.Add(sandbox.playerShip); // there is no universe yet, add manually

            // @note Auto-added to empire
            Vector2 debugDir = playerEmpire.GetOwnedSystems()[0].Position.DirectionToTarget(homePlanet.Center);
            var debugPlatform = new PredictionDebugPlatform("Kinetic Platform", EmpireManager.Remnants, homePlanet.Center + debugDir * 5000f);
            Log.Assert(debugPlatform.HasModules, "Failed to create DebugPlatform");
            sandbox.MasterShipList.Add(debugPlatform); // there is no universe yet, add manually

            foreach (SolarSystem system in sandbox.SolarSystemsList)
                SubmitSceneObjectsForRendering(system);

            Log.Info($"CreateSandboxUniverse elapsed:{s.Elapsed.TotalMilliseconds}");
            return sandbox;
        }

        public Vector2 GenerateRandomSysPos(float spacing, UniverseData data)
        {
            Vector2 sysPos = Vector2.Zero;
            for (int i = 0; i < 20; ++i) // max 20 tries
            {
                sysPos = RandomMath.Vector2D(data.Size.X - 100000f);
                if (data.FindSolarSystemAt(sysPos) == null)
                    return sysPos; // we got it!
            }
            return sysPos;
        }

        void SubmitSceneObjectsForRendering(SolarSystem wipSystem)
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

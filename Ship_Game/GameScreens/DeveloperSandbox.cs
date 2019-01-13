using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        public const int NumOpponents = 1;
        public const bool PlayerIsCybernetic = false;
        public TaskResult<UniverseData> DataLoader;

        public DeveloperSandbox() : base(null)
        {
            IsPopup = true;
        }

        public override void Update(float deltaTime)
        {
            if (DataLoader != null)
            {
                // This speeds up loading ~7x; perhaps without Sleep, rendering is too intensive?
                Thread.Sleep(10);
                if (DataLoader?.IsComplete == true)
                {
                    UniverseData data = DataLoader.Result;
                    DataLoader = null;
                    var universe = new SandboxUniverse(data) { PlayerEmpire = data.EmpireList.First };
                    ScreenManager.GoToScreen(universe);
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

        // as a normal game screen
        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ScreenManager.GoToScreen(new MainMenuScreen());
                return true;
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);

            if (DataLoader == null)
                DataLoader = Parallel.Run(CreateSandboxUniverse);
        }
        
        UniverseData CreateSandboxUniverse()
        {
            Stopwatch s = Stopwatch.StartNew();
            EmpireManager.Clear();
            ResourceManager.LoadItAll();
            var sandbox = new UniverseData { Size = new Vector2(500000f) };
            CurrentGame.StartNew(sandbox);
            var claimedSpots = new Array<Vector2>();

            EmpireData player = RandomMath.RandItem(ResourceManager.MajorRaces.Filter(
                                            d => d.IsCybernetic == PlayerIsCybernetic));

            EmpireData[] opponents = ResourceManager.MajorRaces.Filter(data => data != player);

            var races = new Array<EmpireData>(opponents);
            races.Shuffle();
            races.Resize(Math.Min(races.Count, NumOpponents)); // truncate
            races.Insert(0, player);

            foreach (EmpireData data in races)
            {
                Empire e = sandbox.CreateEmpire(data);
                if (data == player) e.isPlayer = true;

                e.data.CurrentAutoScout     = e.data.ScoutShip;
                e.data.CurrentAutoColony    = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor   = e.data.ConstructorShip;

                // Now, generate system for our empire:
                var system = new SolarSystem();
                system.Position = GenerateRandomSysPos(10000, claimedSpots, sandbox);
                system.GenerateStartingSystem(data.Traits.HomeSystemName, sandbox, 1f, e);
                system.OwnerList.Add(e);
                sandbox.SolarSystemsList.Add(system);

                foreach (Planet p in system.PlanetList)
                {
                    if (e.isPlayer)
                        p.colonyType = Planet.ColonyType.Colony; // this is required to disable governors... for some reason
                    p.SetExploredBy(e);
                }
            }

            foreach (EmpireData data in ResourceManager.MinorRaces) // init minor races
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

            sandbox.playerShip = Ship.CreateShipAtPoint("Unarmed Scout", sandbox.EmpireList.First, claimedSpots[0]);
            sandbox.MasterShipList.Add(sandbox.playerShip);

            foreach (SolarSystem system in sandbox.SolarSystemsList)
                SubmitSceneObjectsForRendering(system);

            return sandbox;
        }

        public Vector2 GenerateRandomSysPos(float spacing, Array<Vector2> claimedSpots, UniverseData data)
        {
            float safetyBreak = 1;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = RandomMath.Vector2D(data.Size.X - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing, claimedSpots, data));

            claimedSpots.Add(sysPos);
            return sysPos;
        }

        static bool SystemPosOK(Vector2 sysPos, float spacing, Array<Vector2> claimedSpots, UniverseData data)
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

    
    class SandboxUniverse : UniverseScreen
    {
        public SandboxUniverse(UniverseData sandbox) : base(sandbox)
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
            MaxCamHeight = 2000000.0f;
        }
    }
}

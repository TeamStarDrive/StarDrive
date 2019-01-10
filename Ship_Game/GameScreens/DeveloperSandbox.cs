using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        const int NumEmpires = 2;
        const bool PlayerIsCybernetic = false;
        MicroUniverse Universe;

        public DeveloperSandbox() : base(null)
        {
            IsPopup = false;
        }

        public override void Update(float deltaTime)
        {
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

        class MicroUniverse : UniverseScreen
        {
            public MicroUniverse(UniverseData sandbox) : base(sandbox)
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

        public override void LoadContent()
        {
            Label(20, 20, "Developer Debug Sandbox (WIP, press ESC to quit)", Fonts.Arial20Bold);
            EmpireManager.Clear();
            ResourceManager.LoadItAll();
            var sandbox = new UniverseData { Size = new Vector2(500000f) };
            UniverseData.UniverseWidth = sandbox.Size.X * 2;
            CurrentGame.StartNew(sandbox);
            var claimedSpots = new Array<Vector2>();

            EmpireData FindRandomEmpire(bool notFaction, bool cybernetic)
            {
                EmpireData[] candidates = ResourceManager.Empires.Filter(data =>
                {
                    if (cybernetic && !data.IsCybernetic) return false;
                    if (notFaction && data.IsFaction)     return false;
                    return !sandbox.EmpireList.Any(e => e.data == data);
                });
                return RandomMath.RandItem(candidates);
            }

            for (int i = 0; i < NumEmpires; ++i)
            {
                bool player = (i == 0);
                EmpireData data = FindRandomEmpire(notFaction: player,
                                                   cybernetic: player && PlayerIsCybernetic);

                Empire e = EmpireManager.CreateEmpireFromEmpireData(data);
                sandbox.EmpireList.Add(e);
                EmpireManager.Add(e);
                e.data.CurrentAutoScout = e.data.ScoutShip;
                e.data.CurrentAutoColony = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor = e.data.ConstructorShip;
                GenerateRandomSysPos(10000, claimedSpots, sandbox);
            }
            sandbox.EmpireList.First.isPlayer = true;

            foreach (Empire empire in EmpireManager.Empires)
            {
                foreach (Empire e in EmpireManager.Empires)
                    if (empire != e) empire.AddRelationships(e, new Relationship(e.data.Traits.Name));
            }

            for (int i = 0; i < sandbox.EmpireList.Count; ++i)
            {
                Empire e = sandbox.EmpireList[i];
                var system = new SolarSystem();
                system.OwnerList.Add(e);
                sandbox.SolarSystemsList.Add(system);
                system.Position = claimedSpots[i];
                system.GenerateStartingSystem($"SandBox-{i}", sandbox, 1, e);
                foreach (Planet p in system.PlanetList)
                {
                    if (p.Owner == EmpireManager.Player)
                    {
                        p.colonyType = Planet.ColonyType.Colony; // this is required to disable governors... for some reason
                    }
                    p.SetExploredBy(e);
                }
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

            Universe = new MicroUniverse(sandbox) { PlayerEmpire = sandbox.EmpireList.First };
            ScreenManager.AddScreen(Universe);
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
}

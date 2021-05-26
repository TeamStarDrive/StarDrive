using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.GameScreens.Sandbox;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal class DeveloperSandbox : GameScreen
    {
        const int NumOpponents = 0;
        const bool PlayerIsCybernetic = false;
        string PlayerPreference = "United";
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

        public override void Update(float fixedDeltaTime)
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
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }

        public class DeveloperUniverse : UniverseScreen
        {
            readonly UniverseData SandBox;
            public DeveloperUniverse(UniverseData sandbox) : base(sandbox, EmpireManager.Empires[0])
            {
                SandBox = sandbox;
                NoEliminationVictory = true; // SandBox mode doesn't have elimination victory
                Paused = false;
            }
            public override void LoadContent()
            {
                base.LoadContent();

                // nice zoom in effect, we set the cam height to super high
                CamHeight *= 10000.0f;
                CamDestination.Z = 100000.0f; // and set a lower destination

                Empire playerEmpire = SandBox.EmpireList.First;
                Planet homePlanet   = playerEmpire.GetPlanets()[0];

                Vector2 debugDir  = playerEmpire.GetOwnedSystems()[0].Position.DirectionToTarget(homePlanet.Center);
                string platformId = "Kinetic Platform";
                //string platformId = "Beam Platform L4";

                // @note Auto-added to Universe
                var debugPlatform = new PredictionDebugPlatform(platformId, EmpireManager.Remnants, homePlanet.Center + debugDir * 5000f);

                Log.Write(ConsoleColor.DarkMagenta, "DeveloperUniverse.LoadContent");
            }
        }

        bool PlayerFilter(IEmpireData d)
        {
            if (PlayerPreference.NotEmpty())
            {
                return d.ArchetypeName.Contains(PlayerPreference)
                    || d.Name.Contains(PlayerPreference);
            }
            return d.IsCybernetic == PlayerIsCybernetic;
        }

        UniverseData CreateSandboxUniverse()
        {
            Stopwatch s = Stopwatch.StartNew();
            EmpireManager.Clear();

            var sandbox = new UniverseData();
            sandbox.GravityWells = true;
            CurrentGame.StartNew(sandbox, pace:1f, 1, 0, 1);

            IEmpireData player = RandomMath.RandItem(ResourceManager.MajorRaces.Filter(PlayerFilter));
            IEmpireData[] opponents = ResourceManager.MajorRaces.Filter(
                               data => data.ArchetypeName != player.ArchetypeName);

            var races = new Array<IEmpireData>(opponents);
            races.Shuffle();
            races.Resize(Math.Min(races.Count, NumOpponents)); // truncate
            races.Insert(0, player);

            foreach (IEmpireData data in races)
            {
                Empire e = sandbox.CreateEmpire(data, isPlayer: (data == player));
                e.data.CurrentAutoScout     = e.data.ScoutShip;
                e.data.CurrentAutoColony    = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor   = e.data.ConstructorShip;

                // Now, generate system for our empire:
                var system = new SolarSystem();
                system.Position = GenerateRandomSysPos(10000, sandbox);
                system.GenerateStartingSystem(e.data.Traits.HomeSystemName, 1f, e);
                system.OwnerList.Add(e);
                sandbox.SolarSystemsList.Add(system);
            }

            foreach (IEmpireData data in ResourceManager.MinorRaces) // init minor races
            {
                sandbox.CreateEmpire(data, isPlayer: false);
            }

            Empire.InitializeRelationships(EmpireManager.Empires, UniverseData.GameDifficulty.Normal);

            foreach (SolarSystem system in sandbox.SolarSystemsList)
            {
                system.FiveClosestSystems = sandbox.SolarSystemsList.FindMinItemsFiltered(5,
                                            filter => filter != system,
                                            select => select.Position.SqDist(system.Position));
            }

            foreach (SolarSystem system in sandbox.SolarSystemsList)
                SubmitSceneObjectsForRendering(system);

            var progress = new ProgressCounter();
            ShipDesignUtils.MarkDesignsUnlockable(progress);
            ResearchScreenNew.UnlockAllResearch(EmpireManager.Player, unlockBonusTechs: true);
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
                planet.InitializePlanetMesh(this);
            }
            foreach (Asteroid asteroid in wipSystem.AsteroidsList)
            {
                asteroid.Position += wipSystem.Position;
            }
        }
    }
}

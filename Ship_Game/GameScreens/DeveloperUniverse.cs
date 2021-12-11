using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;

namespace Ship_Game
{
    public class DeveloperUniverse : UniverseScreen
    {
        readonly UniverseData SandBox;

        public DeveloperUniverse(UniverseData sandbox, Empire player, bool paused) : base(sandbox, player)
        {
            SandBox = sandbox;
            NoEliminationVictory = true; // SandBox mode doesn't have elimination victory
            Paused = paused;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // nice zoom in effect, we set the cam height to super high
            CamPos.Z *= 10000.0;
            CamDestination.Z = 100000.0; // and set a lower destination

            // DISABLED these since they are not that useful anymore

            //Empire playerEmpire = SandBox.EmpireList.First;
            //Planet homePlanet   = playerEmpire.GetPlanets()[0];

            //Vector2 debugDir  = playerEmpire.GetOwnedSystems()[0].Position.DirectionToTarget(homePlanet.Center);
            //string platformId = "Kinetic Platform";
            //string platformId = "Beam Platform L4";

            // @note Auto-added to Universe
            //var debugPlatform = new PredictionDebugPlatform(platformId, EmpireManager.Remnants, homePlanet.Center + debugDir * 5000f);

            Log.Write(ConsoleColor.DarkMagenta, "DeveloperUniverse.LoadContent");
        }
        
        public static UniverseData Create(string playerPreference = "United",
                                          int numOpponents = 1)
        {
            var s = Stopwatch.StartNew();
            EmpireManager.Clear();

            var sandbox = new UniverseData();
            sandbox.GravityWells = true;
            CurrentGame.StartNew(sandbox, pace:1f, 1, 0, 1);

            IEmpireData[] candidates = ResourceManager.MajorRaces.Filter(d => PlayerFilter(d, playerPreference));
            IEmpireData player = candidates[0];
            IEmpireData[] opponents = ResourceManager.MajorRaces.Filter(data => data.ArchetypeName != player.ArchetypeName);

            var races = new Array<IEmpireData>(opponents);
            races.Shuffle();
            races.Resize(Math.Min(races.Count, numOpponents)); // truncate
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

            ShipDesignUtils.MarkDesignsUnlockable();
            Log.Info($"CreateSandboxUniverse elapsed:{s.Elapsed.TotalMilliseconds}");
            return sandbox;
        }
        
        static bool PlayerFilter(IEmpireData d, string playerPreference)
        {
            if (playerPreference.NotEmpty())
            {
                return d.ArchetypeName.Contains(playerPreference)
                    || d.Name.Contains(playerPreference);
            }
            return true;
        }

        static Vector2 GenerateRandomSysPos(float spacing, UniverseData data)
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

        static void SubmitSceneObjectsForRendering(SolarSystem wipSystem)
        {
            foreach (Planet planet in wipSystem.PlanetList)
            {
                planet.InitializePlanetMesh();
            }
            foreach (Asteroid asteroid in wipSystem.AsteroidsList)
            {
                asteroid.Position += wipSystem.Position;
            }
        }
    }
}
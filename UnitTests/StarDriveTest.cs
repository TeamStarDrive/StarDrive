using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Ships;

namespace UnitTests
{
    /// <summary>
    /// Automatic setup for StarDrive unit tests
    /// </summary>
    public class StarDriveTest : IDisposable
    {
        public static string StarDriveAbsolutePath { get; private set; }
        static StarDriveTest()
        {
            SetGameDirectory();
            try
            {
                var xna2 = Assembly.LoadFile(
                    $"{StarDriveAbsolutePath}\\Microsoft.Xna.Framework.dll");
                Console.WriteLine($"XNA Path: {xna2.Location}");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n{e.FileName}\n{e.FusionLog}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n");
                throw;
            }
        }

        public static void SetGameDirectory()
        {
            Directory.SetCurrentDirectory("../../../stardrive");
            StarDriveAbsolutePath = Directory.GetCurrentDirectory();
        }

        public GameDummy Game { get; private set; }
        public GameContentManager Content { get; private set; }
        public UniverseScreen Universe { get; private set; }
        public Empire Player { get; private set; }
        public Empire Enemy { get; private set; }

        public void CreateGameInstance()
        {
            Game = new GameDummy();
            Game.Create();
            Content = Game.Content;
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Game?.Dispose();
            Empire.Universe = Universe = null;
            Game = null;
        }

        public void CreateUniverseAndPlayerEmpire(out Empire player)
        {
            var data = new UniverseData();
            Player = player = data.CreateEmpire(ResourceManager.MajorRaces[0]);
            Empire.Universe = Universe = new UniverseScreen(data, player);
            Universe.player = player;
            Enemy = EmpireManager.CreateRebelsFromEmpireData(ResourceManager.MajorRaces[0], Player);
        }

        public void LoadStarterShips(string[] shipList = null)
        {
            ResourceManager.LoadStarterShipsForTesting(shipList);
        }

        public void LoadStarterShipVulcan()
        {
            LoadStarterShips(new[]
            {
                "Vulcan Scout",
                "Rocket Scout"
            });
        }
        
        public Ship SpawnShip(string shipName, Empire empire, Vector2 position, float rotation = 0f)
        {
            var target = Ship.CreateShipAtPoint(shipName, empire, position);
            target.Rotation = rotation;
            target.InFrustum = true; // force module pos update
            target.UpdateShipStatus(0.01f); // update module pos
            return target;
        }

        public void LoadPlanetContent()
        {
            ResourceManager.LoadPlanetContentForTesting();
        }

        public void LoadTechContent()
        {
            ResourceManager.LoadTechContentForTesting();
        }

        public static void AddDummyPlanetToEmpire(Empire empire)
        {
            AddDummyPlanet(out Planet p);
            empire.AddPlanet(p);
            p.Type = ResourceManager.PlanetOrRandom(0);
        }

        public static void AddDummyPlanet(out Planet p)
        {
            p        = new Planet();
            var s    = new SolarSystem();
            s.PlanetList.Add(p);
            p.ParentSystem = s;
        }

        public static void AddHomeWorldToEmpire(Empire empire, out Planet p)
        {
            AddDummyPlanet(out p);
            p.GenerateNewHomeWorld(empire);
        }
    }
}

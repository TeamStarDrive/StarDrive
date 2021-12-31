using System.Collections.Generic;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        static int StressTestLoadIndex;

        // Stress-test the resource manager, and load lots of models into memory.
        void StressTestShipLoading()
        {
            var spawnedMeshes = new HashSet<string>();
            void SpawnUniqueMeshes(RoleName role)
            {
                foreach (Ship ship in ResourceManager.ShipTemplates)
                {
                    if (ship.DesignRole == role && !spawnedMeshes.Contains(ship.BaseHull.ModelPath))
                    {
                        Ship.CreateShipAtPoint(this, ship.Name, player, mouseWorldPos + RandomMath.Vector2D(500f));
                        spawnedMeshes.Add(ship.BaseHull.ModelPath);
                    }
                }
            }

            switch (StressTestLoadIndex++)
            {
                case 0:
                    SpawnUniqueMeshes(RoleName.fighter);
                    SpawnUniqueMeshes(RoleName.scout);
                    SpawnUniqueMeshes(RoleName.freighter);
                    break;
                case 1:
                    SpawnUniqueMeshes(RoleName.corvette);
                    SpawnUniqueMeshes(RoleName.gunboat);
                    break;
                case 2:
                    SpawnUniqueMeshes(RoleName.frigate);
                    break;
                case 3:
                    SpawnUniqueMeshes(RoleName.cruiser);
                    break;
                case 4:
                    SpawnUniqueMeshes(RoleName.capital);
                    SpawnUniqueMeshes(RoleName.carrier);
                    SpawnUniqueMeshes(RoleName.battleship);
                    SpawnUniqueMeshes(RoleName.station);
                    break;
                default:
                    StressTestLoadIndex = 0;
                    break;
            }
        }
    }
}
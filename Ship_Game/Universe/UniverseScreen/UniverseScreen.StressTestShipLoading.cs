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
            void SpawnUniqueMeshes(ShipData.RoleName role)
            {
                foreach (Ship ship in ResourceManager.GetShipTemplates())
                {
                    if (ship.DesignRole == role && !spawnedMeshes.Contains(ship.shipData.ModelPath))
                    {
                        Ship.CreateShipAtPoint(ship.Name, player, mouseWorldPos + RandomMath.Vector2D(500f));
                        spawnedMeshes.Add(ship.shipData.ModelPath);
                    }
                }
            }

            switch (StressTestLoadIndex++)
            {
                case 0:
                    SpawnUniqueMeshes(ShipData.RoleName.fighter);
                    SpawnUniqueMeshes(ShipData.RoleName.scout);
                    SpawnUniqueMeshes(ShipData.RoleName.freighter);
                    break;
                case 1:
                    SpawnUniqueMeshes(ShipData.RoleName.corvette);
                    SpawnUniqueMeshes(ShipData.RoleName.gunboat);
                    break;
                case 2:
                    SpawnUniqueMeshes(ShipData.RoleName.frigate);
                    break;
                case 3:
                    SpawnUniqueMeshes(ShipData.RoleName.cruiser);
                    break;
                case 4:
                    SpawnUniqueMeshes(ShipData.RoleName.capital);
                    SpawnUniqueMeshes(ShipData.RoleName.carrier);
                    SpawnUniqueMeshes(ShipData.RoleName.battleship);
                    SpawnUniqueMeshes(ShipData.RoleName.station);
                    break;
                default:
                    StressTestLoadIndex = 0;
                    break;
            }
        }
    }
}
using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class UniverseData
    {
        public string loadFogPath;

        public Array<SolarSystem> SolarSystemsList = new Array<SolarSystem>();

        public Vector2 Size = new Vector2(500000f);

        public GameDifficulty difficulty = GameDifficulty.Normal;
        public GalSize GalaxySize        = GalSize.Medium;

        public float FTLSpeedModifier = 1f;
        public float EnemyFTLSpeedModifier = 1f;
        public float FTLInSystemModifier = 1f;
        public bool FTLinNeutralSystem = true;

        public bool GravityWells;

        public Array<Ship> MasterShipList = new Array<Ship>();

        // All Projectiles AND Beams
        public Array<Projectile> MasterProjectileList = new Array<Projectile>();

        public Array<Empire> EmpireList = new Array<Empire>();

        public enum GameDifficulty
        {
            Easy,
            Normal,
            Hard,
            Brutal
        }

        public Empire CreateEmpire(IEmpireData readOnlyData)
        {
            if (EmpireManager.GetEmpireByName(readOnlyData.Name) != null)
                throw new InvalidOperationException($"BUG: Empire already created! {readOnlyData.Name}");
            Empire e = EmpireManager.CreateEmpireFromEmpireData(readOnlyData);
            EmpireList.Add(e);
            EmpireManager.Add(e);
            return e;
        }

        public SolarSystem FindSolarSystemAt(Vector2 point)
        {
            foreach (SolarSystem s in SolarSystemsList)
            {
                if (point.InRadius(s.Position, s.Radius*2))
                    return s;
            }
            return null;
        }

        public bool FindSystem(in Guid systemGuid, out SolarSystem foundSystem)
        {
            return (foundSystem = FindSystemOrNull(systemGuid)) != null;
        }

        public SolarSystem FindSystemOrNull(in Guid systemGuid)
        {
            if (systemGuid != Guid.Empty)
            {
                foreach (SolarSystem s in SolarSystemsList)
                    if (s.guid == systemGuid)
                        return s;
            }
            return null;
        }

        public bool FindPlanet(in Guid planetGuid, out Planet foundPlanet)
        {
            return (foundPlanet = FindPlanetOrNull(planetGuid)) != null;
        }

        public Planet FindPlanetOrNull(in Guid planetGuid)
        {
            if (planetGuid != Guid.Empty)
            {
                foreach (SolarSystem s in SolarSystemsList)
                {
                    Planet p = s.FindPlanet(planetGuid);
                    if (p != null)
                        return p;
                }
            }
            return null;
        }

        public bool FindShip(in Guid shipGuid, out Ship foundShip)
        {
            return (foundShip = FindShipOrNull(shipGuid)) != null;
        }

        public Ship FindShipOrNull(in Guid shipGuid)
        {
            if (shipGuid != Guid.Empty)
            {
                foreach (Ship ship in MasterShipList)
                    if (ship.guid == shipGuid)
                        return ship;
            }
            return null;
        }

        public bool FindShip(Empire empire, in Guid shipGuid, out Ship foundShip)
        {
            return (foundShip = FindShipOrNull(empire, shipGuid)) != null;
        }
        
        public Ship FindShipOrNull(Empire empire, in Guid shipGuid)
        {
            if (shipGuid != Guid.Empty)
            {
                var ownedShips = empire.GetShips();
                foreach (Ship ship in ownedShips)
                    if (ship.guid == shipGuid)
                        return ship;
            }
            return null;
        }

        public GameplayObject FindObjectOrNull(in Guid objectGuid)
        {
            if (objectGuid != Guid.Empty)
            {
                foreach (Ship ship in MasterShipList)
                    if (ship.guid == objectGuid)
                        return ship;

                // TODO: implement Projectile and Beam search
            }
            return null;
        }
    }
}
using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;

namespace Ship_Game
{
    public sealed class UniverseData
    {
        public string loadFogPath;

        public Array<SolarSystem> SolarSystemsList = new Array<SolarSystem>();

        public Vector2 Size;

        public GameDifficulty difficulty = GameDifficulty.Normal;

        public float FTLSpeedModifier = 1f;
        public float EnemyFTLSpeedModifier = 1f;
        public float FTLInSystemModifier = 1f;
        public bool FTLinNeutralSystem = true;

        public bool GravityWells;

        public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();

        public Ship playerShip;

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

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (SolarSystem s in SolarSystemsList)
            {
                Planet p = s.FindPlanet(planetGuid);
                if (p != null)
                    return p;
            }
            return null;
        }

        public bool FindShip(in Guid shipGuid, out Ship foundShip)
        {
            foreach (Ship ship in MasterShipList)
            {
                if (ship.guid == shipGuid)
                {
                    foundShip = ship;
                    return true;
                }
            }
            foundShip = null;
            return false;
        }
    }
}
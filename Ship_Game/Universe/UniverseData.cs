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

        public Empire CreateEmpire(EmpireData data)
        {
            if (EmpireManager.GetEmpireByName(data.Traits.Name) != null)
                throw new InvalidOperationException($"BUG: Empire already created! {data.Traits.Name}");
            Empire e = EmpireManager.CreateEmpireFromEmpireData(data);
            EmpireList.Add(e);
            EmpireManager.Add(e);
            return e;
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
    }
}
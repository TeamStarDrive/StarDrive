namespace Ship_Game
{
    public sealed class ShipRole
    {
        public string Name = "";

        public int Localization;

        public float Upkeep = 1;

        public float KillExp = 1;

        public float KillExpPerLevel = 1;

        public float ExpPerLevel = 1;

        public float DamageRelations = 1;

        public bool Protected;

        public bool NoBuild = false;

        public Array<Race> RaceList;

        public class Race
        {
            public string ShipType;
            public int Localization;
            public float Upkeep;
            public float KillExp;
            public float KillExpPerLevel;
            public float ExpPerLevel;
        }

        public ShipRole()
        {
            RaceList = new Array<Race>();
        }
    }
}

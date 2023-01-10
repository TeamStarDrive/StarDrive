using SDUtils;
using Ship_Game.Ships;

namespace Ship_Game
{
    // Loaded from `Content/ShipRoles/`
    public sealed class ShipRole
    {
        public string Name = "";
        public int Localization;

        public float KillExp = 1;
        public float KillExpPerLevel = 1;
        public float ExpPerLevel = 1;
        public float DamageRelations = 1;

        public bool Protected;

        //I believe this is for race specific hulls
        public Array<Race> RaceList = new();

        public class Race
        {
            public string ShipType;
            public int Localization;
            public float Upkeep;
            public float KillExp;
            public float KillExpPerLevel;
            public float ExpPerLevel;
        }

        public static Race GetExpSettings(Ship ship)
        {
            if (ResourceManager.ShipRoles.TryGetValue(ship.ShipData.HullRole, out ShipRole role))
            {
                var expSettings = role.RaceList.Find(r => r.ShipType == ship.Loyalty.data.Traits.ShipType);
                if (expSettings != null)
                    return expSettings;
                return new Race
                {
                    KillExp = role.KillExp,
                    KillExpPerLevel = role.KillExpPerLevel,
                    ExpPerLevel = role.ExpPerLevel
                };
            }
            return new Race
            {
                KillExp         = 1,
                KillExpPerLevel = 1,
                ExpPerLevel     = 1
            };
        }
        public static float GetMaxExpValue()
        {
            float max = float.MinValue;
            foreach (var kv in ResourceManager.ShipRoles)
            {
                if (kv.Value.KillExp > max)
                    max = kv.Value.KillExp;
            }
            return max;
        }
        public static LocalizedText GetRoleName(RoleName role, string shipType)
        {
            if (!ResourceManager.ShipRoles.TryGetValue(role, out ShipRole shipRole))
                return LocalizedText.None;

            foreach (Race race in shipRole.RaceList)
                if (race.ShipType == shipType)
                    return new LocalizedText(race.Localization);

            return new LocalizedText(shipRole.Localization);
        }
    }
}

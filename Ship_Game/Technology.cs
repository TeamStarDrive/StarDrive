using System.Xml.Serialization;

namespace Ship_Game
{
	public sealed class Technology
    {
        public string UID;
        public string IconPath;

        /// This is purely used for logging/debugging to mark where the Technology was loaded from
        [XmlIgnore] public string DebugSourceFile = "<unknown>.xml";

        public int RootNode;
        public float Cost;
        public bool Secret;
        public bool Discovered;
        public bool Unlockable;

        public TechnologyType TechnologyType = TechnologyType.General;

        public int NameIndex;
        public int DescriptionIndex;


        public Array<LeadsToTech> LeadsTo                = new Array<LeadsToTech>();
        public Array<LeadsToTech> ComesFrom              = new Array<LeadsToTech>();
        public Array<UnlockedMod> ModulesUnlocked        = new Array<UnlockedMod>();
        public Array<UnlockedBuilding> BuildingsUnlocked = new Array<UnlockedBuilding>();
        public Array<UnlockedBonus> BonusUnlocked        = new Array<UnlockedBonus>();
        public Array<UnlockedTroop> TroopsUnlocked       = new Array<UnlockedTroop>();
        public Array<UnlockedHull> HullsUnlocked         = new Array<UnlockedHull>();
        public Array<TriggeredEvent> EventsTriggered     = new Array<TriggeredEvent>();
        public Array<RevealedTech> TechsRevealed         = new Array<RevealedTech>();

        //Added by McShooterz to allow for techs with more than one level
        public int MaxLevel = 1;

        //added by McShooterz: Racial Tech variables
        public Array<RequiredRace> RaceRestrictions = new Array<RequiredRace>();
        public Array<RequiredRace> RaceExclusions = new Array<RequiredRace>();
        public struct RequiredRace
        {
            public string ShipType;
            public string RacialTrait;
        }

        //added by McShooterz: Alternate Tach variables
        public bool Militaristic;
        public bool unlockFrigates;
        public bool unlockCruisers;
        public bool unlockBattleships;
        public bool unlockCorvettes;

        public struct LeadsToTech
        {
            public string UID;
            public LeadsToTech(string techID)
            {
                UID = techID;
            }
        }

        public class UnlockedBonus
        {
            public string Name;
            public string Type;
            public string BonusType;
            public Array<string> Tags;
            public float Bonus;
            public string Description;
            public int BonusIndex;
            public int BonusNameIndex;
        }

        public struct UnlockedBuilding
        {
            public string Name;
            public string Type;
        }

        public struct UnlockedHull
        {
            public string Name;
            public string ShipType;
        }

        public struct UnlockedMod
        {
            public string ModuleUID;
            public string Type;
        }

        public struct UnlockedTroop
        {
            public string Name;
            public string Type;
        }

        public struct TriggeredEvent
        {
            public string EventUID;
            public string Type;
            public string CustomMessage;
        }

        public struct RevealedTech
        {
            public string RevUID;
            public string Type;
        }
        public Building[] GetBuildings()
        {
            Array<Building> buildings = new Array<Building>();
            foreach (UnlockedBuilding buildingName in BuildingsUnlocked)
            {
                buildings.AddUniqueRef(ResourceManager.GetBuildingTemplate(buildingName.Name));
            }
            return buildings.ToArray();
        }
    }
}
using System.Collections.Generic;
using System.Xml.Serialization;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class Technology
    {
        public string UID;
        public string IconPath;

        /// This is purely used for logging/debugging to mark where the Technology was loaded from
        [XmlIgnore] public string DebugSourceFile = "<unknown>.xml";
        [XmlIgnore] public float ActualCost => Cost * CurrentGame.Pace;

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
        public Array<RequiredRace> RaceExclusions   = new Array<RequiredRace>();
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
            var buildings = new HashSet<Building>();
            foreach (UnlockedBuilding buildingName in BuildingsUnlocked)
            {
                buildings.Add(ResourceManager.GetBuildingTemplate(buildingName.Name));
            }
            return buildings.ToArray();
        }

        // @param baseValue base value per research point
        public float DiplomaticValueTo(Empire them, float valuePerTechCost = 0.01f)
        {
            float value = ActualCost * valuePerTechCost;

            // Technologists appreciate tech scores +25% higher:
            if (them.data.EconomicPersonality.Name == "Technologists")
                value *= 1.25f;
            return value;
        }

        public static TechnologyType GetTechnologyTypeFromUnlocks(Technology tech)
        {
            if (tech.ModulesUnlocked.Count > 0)
            {
                return GetModuleTechnologyType(tech);
            }

            if (tech.HullsUnlocked.Count > 0)
            {
                return GetHullTechnologyType(tech);
            }

            if (tech.BonusUnlocked.Count > 0)
            {
                return GetBonusTechnologyType(tech);
            }

            if (tech.BuildingsUnlocked.Count > 0)
            {
                return GetBuildingTechnologyType(tech);
            }

            if (tech.TroopsUnlocked.Count > 0)
            {
                return TechnologyType.GroundCombat;
            }

            return TechnologyType.General;
        }

        static TechnologyType GetBuildingTechnologyType(Technology tech)
        {
            foreach (UnlockedBuilding buildingU in tech.BuildingsUnlocked)
            {
                if (!ResourceManager.GetBuilding(buildingU.Name, out Building building))
                {
                    Log.Warning($"Tech {tech.UID} unlock unavailable : {buildingU.Name}");
                    continue;
                }

                if (building.AllowInfantry || building.isWeapon || building.IsSensor ||
                    building.PlanetaryShieldStrengthAdded > 0 || building.CombatStrength > 0 || building.Strength > 0)
                    return TechnologyType.GroundCombat;

                if (building.AllowShipBuilding || building.PlusFlatProductionAmount > 0 ||
                    building.PlusProdPerRichness > 0 || building.StorageAdded > 0 || building.PlusFlatProductionAmount > 0)
                    return TechnologyType.Industry;

                if (building.PlusTaxPercentage > 0 || building.CreditsPerColonist > 0)
                    return TechnologyType.Economic;

                if (building.PlusFlatResearchAmount > 0 || building.PlusResearchPerColonist > 0)
                    return TechnologyType.Research;

                if (building.PlusFoodPerColonist > 0 || building.PlusFlatFoodAmount > 0 ||
                    building.PlusFoodPerColonist > 0 || building.MaxPopIncrease > 0 ||
                    building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0 || building.IsBiospheres)
                    return TechnologyType.Colonization;
            }

            return TechnologyType.General;
        }

        static TechnologyType GetBonusTechnologyType(Technology tech)
        {
            foreach (UnlockedBonus unlockedBonus in tech.BonusUnlocked)
            {
                switch (unlockedBonus.Type)
                {
                    case "SHIPMODULE":
                    case "HULL": return TechnologyType.ShipGeneral;
                    case "TROOP": return TechnologyType.GroundCombat;
                    case "BUILDING": return TechnologyType.Colonization;
                    case "ADVANCE": return TechnologyType.ShipGeneral;
                }

                switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
                {
                    case "Xeno Compilers":
                    case "Research Bonus":
                        return TechnologyType.Research;
                    case "FTL Spool Bonus":
                    case "Set FTL Drain Modifier":
                    case "Trade Tariff":
                    case "Bonus Money Per Trade":
                    case "Slipstreams":
                    case "In Borders FTL Bonus":
                    case "StarDrive Enhancement":
                    case "FTL Speed Bonus":
                    case "FTL Efficiency":
                    case "FTL Efficiency Bonus":
                    case "Civilian Maintenance":
                    case "Privatization":
                    case "Production Bonus":
                    case "Construction Bonus":
                    case "Consumption Bonus":
                    case "Tax Bonus":
                    case "Maintenance Bonus":
                        return TechnologyType.Economic;
                    case "Top Guns":
                    case "Bonus Fighter Levels":
                    case "Mass Reduction":
                    case "Percent Mass Adjustment":
                    case "STL Speed Bonus":
                    case "ArmourMass":
                        return TechnologyType.ShipGeneral;
                    case "Resistance is Futile":
                    case "Super Soldiers":
                    case "Troop Strength Modifier Bonus":
                    case "Allow Assimilation":
                        return TechnologyType.GroundCombat;
                    case "Cryogenic Suspension":
                    case "Increased Lifespans":
                    case "Population Growth Bonus":
                    case "Set Population Growth Min":
                    case "Set Population Growth Max":
                    case "Spy Offense":
                    case "Spy Offense Roll Bonus":
                    case "Spy Defense":
                    case "Spy Defense Roll Bonus":
                    case "Xenolinguistic Nuance":
                    case "Diplomacy Bonus":
                    case "Passenger Modifier":
                        return TechnologyType.Colonization;
                    case "Ordnance Effectiveness":
                    case "Ordnance Effectiveness Bonus":
                    case "Tachyons":
                    case "Sensor Range Bonus":
                    case "Fuel Cell Upgrade":
                    case "Ship Experience Bonus":
                    case "Power Flow Bonus":
                    case "Shield Power Bonus":
                    case "Fuel Cell Bonus":
                        return TechnologyType.ShipGeneral;
                    case "Missile Armor":
                    case "Missile HP Bonus":
                    case "Hull Strengthening":
                    case "Module HP Bonus":
                    case "ECM Bonus":
                    case "Missile Dodge Change Bonus":
                    case "Reaction Drive Upgrade":
                    case "Reactive Armor":
                    case "Repair Bonus":
                    case "Kulrathi Might":
                    case "Armor Explosion Reduction":
                        return TechnologyType.ShipDefense;
                    case "Armor Piercing":
                    case "Armor Phasing":
                    case "Weapon_Speed":
                    case "Weapon_Damage":
                    case "Weapon_ExplosionRadius":
                    case "Weapon_TurnSpeed":
                    case "Weapon_Rate":
                    case "Weapon_Range":
                    case "Weapon_ShieldDamage":
                    case "Weapon_ArmorDamage":
                    case "Weapon_HP":
                    case "Weapon_ShieldPenetration":
                    case "Weapon_ArmourPenetration":
                        return TechnologyType.ShipWeapons;
                }
            }

            return TechnologyType.ShipGeneral;
        }

        static TechnologyType GetHullTechnologyType(Technology tech)
        {
            foreach (UnlockedHull hull in tech.HullsUnlocked)
            {
                ShipData.RoleName role = ResourceManager.Hull(hull.Name).Role;
                if (role == ShipData.RoleName.freighter
                    || role == ShipData.RoleName.platform
                    || role == ShipData.RoleName.construction
                    || role == ShipData.RoleName.station)
                    return TechnologyType.Industry;
            }

            return TechnologyType.ShipHull;
        }

        static TechnologyType GetModuleTechnologyType(Technology tech)
        {
            foreach (UnlockedMod moduleU in tech.ModulesUnlocked)
            {
                if (!ResourceManager.GetModuleTemplate(moduleU.ModuleUID, out ShipModule module))
                {
                    Log.Warning($"Tech {tech.UID} unlock unavailable : {moduleU.ModuleUID}");
                    continue;
                }

                if (module.InstalledWeapon != null
                    || module.MaximumHangarShipSize > 0
                    || module.Is(ShipModuleType.Hangar))
                    return TechnologyType.ShipWeapons;
                if (module.ShieldPower >= 1f
                    || module.Is(ShipModuleType.Armor)
                    || module.Is(ShipModuleType.Countermeasure)
                    || module.Is(ShipModuleType.Shield))
                    return TechnologyType.ShipDefense;
            }

            return TechnologyType.ShipGeneral;
        }
    }
}
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class Technology
    {
        public string UID;
        public string IconPath;

        /// This is purely used for logging/debugging to mark where the Technology was loaded from
        [XmlIgnore] public string DebugSourceFile = "<unknown>.xml";
        [XmlIgnore] public float ActualCost => Cost * CurrentGame.Pace;

        [XmlIgnore] public Technology[] Children;
        [XmlIgnore] public Technology[] Parents;

        public static readonly Technology Dummy = new Technology();

        public int RootNode;
        public float Cost;
        public bool Secret;
        public bool Discovered;
        public bool Unlockable;

        [XmlIgnore] public Array<TechnologyType> TechnologyTypes = new Array<TechnologyType>();
        public TechnologyType TechnologyType { get => TechnologyTypes[0]; set => TechnologyTypes.Add(value); }

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

        //added by McShooterz: Racial Tech variables.
        //This hides the tech from all races except for the ones in the RaceRequirements list
        public Array<RaceRequirements> HiddenFromAllExcept = new Array<RaceRequirements>();
        //this hides the tech from the races in the RaceRequirements list.
        public Array<RaceRequirements> HiddenFrom   = new Array<RaceRequirements>();
        //This unlocks the tech at game start for the races in the RaceRequirements list.
        //This will override the other two restrictors.
        public Array<RaceRequirements> UnlockedAtGameStart  = new Array<RaceRequirements>();

        //This is used with the tech restrictors above to list races or traits that will create
        //rules for the restriction
        public struct RaceRequirements
        {
            public string ShipType;
            public RacialTrait.NameOfTrait RacialTrait;
        }

        public struct LeadsToTech
        {
            public string UID;
            public LeadsToTech(string techID)
            {
                UID = techID;
            }
        }

        public bool AnyChildrenDiscovered(Empire empire)
            => Children.Any(tech => empire.GetTechEntry(tech.UID).Discovered);
        public Technology[] DiscoveredChildren(Empire empire)
        => Children.Filter(tech => empire.GetTechEntry(tech.UID).Discovered);

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


        Technology[] ResolveLeadsToTechs(string what, Array<LeadsToTech> leads)
        {
            var resolved = new Array<Technology>();
            foreach (LeadsToTech leadsTo in leads)
            {
                if (ResourceManager.TryGetTech(leadsTo.UID, out Technology child))
                {
                    resolved.Add(child);
                }
                else
                {
                    Log.Warning(ConsoleColor.DarkRed, $"Tech '{UID}' {what} '{leadsTo.UID}' does not exist!");
                }
            }
            return resolved.ToArray();
        }

        public void ResolveLeadsToTechs()
        {
            Children = ResolveLeadsToTechs("LeadsTo", LeadsTo);
            Parents  = ResolveLeadsToTechs("ComesFrom", ComesFrom);
        }

        public static void GetTechnologyTypesFromUnlocks(Technology tech, Array<TechnologyType> technologyTypes)
        {
            if (tech.ModulesUnlocked.Count > 0)
            {
                foreach (TechnologyType mType in GetModuleTechnologyType(tech))
                    technologyTypes.AddUnique(mType);
            }

            if (tech.HullsUnlocked.Count > 0)
                foreach (TechnologyType hType in GetHullTechnologyType(tech))
                    technologyTypes.AddUnique(hType);

            if (tech.BonusUnlocked.Count > 0)
                foreach (TechnologyType uType in GetBonusTechnologyType(tech))
                    technologyTypes.AddUnique(uType);

            if (tech.BuildingsUnlocked.Count > 0)
                technologyTypes.AddUnique(GetBuildingTechnologyType(tech));

            if (tech.TroopsUnlocked.Count > 0)
                technologyTypes.AddUnique(TechnologyType.GroundCombat);

            if (tech.ModulesUnlocked.Count == 0 && tech.HullsUnlocked.Count == 0 && tech.BonusUnlocked.Count == 0
                && tech.BuildingsUnlocked.Count == 0 && tech.TroopsUnlocked.Count == 0)
                technologyTypes.AddUnique(TechnologyType.General);
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
                    building.PlanetaryShieldStrengthAdded > 0 || building.CombatStrength > 0 || building.CanAttack)
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

        static Array<TechnologyType> GetBonusTechnologyType(Technology tech)
        {
            var techTypes = new Array<TechnologyType>();
            foreach (UnlockedBonus unlockedBonus in tech.BonusUnlocked)
            {
                switch (unlockedBonus.Type)
                {
                    case "SHIPMODULE":
                    case "HULL":     techTypes.AddUnique(TechnologyType.ShipGeneral);  break;
                    case "TROOP":    techTypes.AddUnique(TechnologyType.GroundCombat); break;
                    case "BUILDING": techTypes.AddUnique(TechnologyType.Colonization); break;
                    case "ADVANCE":  techTypes.AddUnique(TechnologyType.ShipGeneral);  break;
                }

                switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
                {
                    case "Xeno Compilers":
                    case "Research Bonus":
                        techTypes.AddUnique(TechnologyType.Research); break;
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
                        techTypes.AddUnique(TechnologyType.Economic); break;
                    case "Top Guns":
                    case "Bonus Fighter Levels":
                    case "Mass Reduction":
                    case "Percent Mass Adjustment":
                    case "STL Speed Bonus":
                    case "ArmourMass":
                        techTypes.AddUnique(TechnologyType.ShipGeneral); break;
                    case "Resistance is Futile":
                    case "Super Soldiers":
                    case "Troop Strength Modifier Bonus":
                    case "Allow Assimilation":
                        techTypes.AddUnique(TechnologyType.GroundCombat); break;
                    case "Cryogenic Suspension":
                        techTypes.AddUnique(TechnologyType.ShipGeneral); break;
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
                        techTypes.AddUnique(TechnologyType.Colonization); break;
                    case "Ordnance Effectiveness":
                    case "Ordnance Effectiveness Bonus":
                    case "Tachyons":
                    case "Sensor Range Bonus":
                    case "Fuel Cell Upgrade":
                    case "Ship Experience Bonus":
                    case "Power Flow Bonus":
                    case "Shield Power Bonus":
                    case "Fuel Cell Bonus":
                        techTypes.AddUnique(TechnologyType.ShipGeneral); break;
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
                        techTypes.AddUnique(TechnologyType.ShipGeneral); break;
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
                        techTypes.AddUnique(TechnologyType.ShipGeneral); break;
                    default:
                        techTypes.AddUnique(TechnologyType.General); break;
                }
            }

            return techTypes;
        }

        static Array<TechnologyType> GetHullTechnologyType(Technology tech)
        {
            var techTypes = new Array<TechnologyType>();
            foreach (UnlockedHull unlockedHull in tech.HullsUnlocked)
            {
                if (!ResourceManager.Hull(unlockedHull.Name, out ShipData hull))
                    continue;

                if (hull.IsShipyard)
                    techTypes.AddUnique(TechnologyType.Industry);

                if (hull.Role == ShipData.RoleName.construction ||
                    hull.Role == ShipData.RoleName.freighter)
                    techTypes.AddUnique(TechnologyType.Industry);

                if (hull.Role == ShipData.RoleName.station ||
                    hull.Role == ShipData.RoleName.platform ||
                    hull.Role == ShipData.RoleName.freighter)
                    techTypes.AddUnique(TechnologyType.ShipHull);
            }

            return techTypes;
        }

        static Array<TechnologyType> GetModuleTechnologyType(Technology tech)
        {
            var techTypes = new Array<TechnologyType>();
            foreach (UnlockedMod moduleU in tech.ModulesUnlocked)
            {
                if (!ResourceManager.GetModuleTemplate(moduleU.ModuleUID, out ShipModule module))
                {
                    Log.Warning($"Tech {tech.UID} unlock unavailable : {moduleU.ModuleUID}");
                    continue;
                }

                ModuleTechType(module,techTypes);
            }

            return techTypes;
        }

        private static void ModuleTechType(ShipModule module, Array<TechnologyType> techTypes)
        {
            bool General = true;
            if (module.InstalledWeapon != null
                || module.MaximumHangarShipSize > 0
                || module.Is(ShipModuleType.Hangar))
            {
                techTypes.AddUnique(TechnologyType.ShipWeapons);
                General = false;
            }
            if (module.shield_power_max >= 1f
                || module.Is(ShipModuleType.Armor)
                || module.Is(ShipModuleType.Countermeasure)
                || module.Is(ShipModuleType.Shield))
            {
                techTypes.AddUnique(TechnologyType.ShipDefense);
                General = false;
            }
            if(General) techTypes.AddUnique(TechnologyType.ShipGeneral);
        }
    }
}
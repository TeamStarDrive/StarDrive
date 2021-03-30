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
        [XmlIgnore] public float ActualCost => ResearchMultiplier() * CurrentGame.Pace;

        [XmlIgnore] public Technology[] Children;
        [XmlIgnore] public Technology[] Parents;

        public static readonly Technology Dummy = new Technology();

        public int RootNode;
        public float Cost;
        public bool Secret;
        public bool Discovered;
        public bool Unlockable;

        [XmlIgnore] public SortedSet<TechnologyType> TechnologyTypes = new SortedSet<TechnologyType>();
        [XmlIgnore] public int NumStuffUnlocked => ModulesUnlocked.Count + BuildingsUnlocked.Count 
                                                   + BonusUnlocked.Count + TroopsUnlocked.Count 
                                                   + HullsUnlocked.Filter(h => h.ShipType == EmpireManager.Player.data.Traits.ShipType).Length;

        public int NameIndex;
        public int DescriptionIndex;

        public LocalizedText Name => new LocalizedText(NameIndex);
        public LocalizedText Description => new LocalizedText(DescriptionIndex);

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

        float ResearchMultiplier()
        {
            if (!GlobalStats.ModChangeResearchCost)
                return Cost;

            int idealNumPlayers     = (int)(CurrentGame.GalaxySize) + 3;
            float galSizeModifier   = ((int)CurrentGame.GalaxySize / 2f).LowerBound(0.25f);
            float starsModifier     = CurrentGame.StarsModifier;
            float extraPlanetsMod   = 1 + CurrentGame.ExtraPlanets * 0.25f;
            float playerRatio       = (float)idealNumPlayers / CurrentGame.NumMajorEmpires;
            float settingsRatio     = galSizeModifier * extraPlanetsMod * playerRatio * starsModifier;

            if (settingsRatio.Less(1))
                return (Cost * settingsRatio.LowerBound(0.5f)).RoundTo10();

            float costRatio         = GetCostRatio();
            float multiplierToUse   = 1 + settingsRatio * costRatio;

            return (Cost * multiplierToUse.Clamped(1, 25)).RoundDownTo10();

            float GetCostRatio()
            {
                if (settingsRatio.LessOrEqual(1))
                    return 0;

                int costThreshold = GlobalStats.ActiveModInfo.CostBasedOnSizeThreshold;
                return Cost > costThreshold ? 1 : Cost / costThreshold;
            }
        }

        public Building[] GetBuildings()
        {
            var buildings = new HashSet<Building>();
            foreach (UnlockedBuilding buildingName in BuildingsUnlocked)
            {
                // NOTE: BuildingsUnlocked may have invalid entries after loading from save
                if (ResourceManager.GetBuilding(buildingName.Name, out Building b))
                    buildings.Add(b);
            }
            return buildings.ToArray();
        }

        // @param baseValue base value per research point
        public float DiplomaticValueTo(Empire us, Empire offeringEmpire, float valuePerTechCost = 0.01f)
        {
            float value = Cost * valuePerTechCost;
            if (us.isPlayer) // Some modifiers vs. Player based on Difficulty and AI personality
                value *= offeringEmpire.PersonalityModifiers.TechValueModifier 
                         * offeringEmpire.DifficultyModifiers.TechValueModifier;

            return value;
        }

        Technology[] ResolveLeadsToTechs(Array<LeadsToTech> leads)
        {
            var resolved = new Array<Technology>();
            foreach (LeadsToTech leadsTo in leads)
            {
                if (ResourceManager.TryGetTech(leadsTo.UID, out Technology child))
                    resolved.Add(child);
                else
                    Log.Warning(ConsoleColor.DarkRed, $"Tech '{UID}' LeadsTo '{leadsTo.UID}' does not exist!");
            }

            return resolved.ToArray();
        }

        Technology[] ResolveComeFromTechs(Array<LeadsToTech> parents)
        {
            var resolved = new Array<Technology>();
            foreach (LeadsToTech comesFrom in parents)
            {
                if (ResourceManager.TryGetTech(comesFrom.UID, out Technology parent))
                {
                    resolved.Add(parent);
                    resolved.AddRange(parent.ResolveComeFromTechs(parent.ComesFrom));
                }
                else
                {
                    Log.Warning(ConsoleColor.DarkRed, $"Tech '{UID}' ComesFrom '{comesFrom.UID}' does not exist!");
                }
            }
            return resolved.ToArray();
        }

        public void ResolveLeadsToTechs()
        {
            Children = ResolveLeadsToTechs(LeadsTo);
            Parents  = ResolveComeFromTechs(ComesFrom);

        }

        public void UpdateTechnologyTypesFromUnlocks()
        {
            ISet<TechnologyType> types = TechnologyTypes;
            if (ModulesUnlocked.Count > 0)   GetModuleTechTypes(types);
            if (HullsUnlocked.Count > 0)     GetHullTechTypes(types);
            if (BonusUnlocked.Count > 0)     GetBonusTechTypes(types);
            if (BuildingsUnlocked.Count > 0) GetBuildingTechnologyType(types);
            if (TroopsUnlocked.Count > 0)    types.Add(TechnologyType.GroundCombat);
            if (types.Count == 0)            types.Add(TechnologyType.General);
        }

        void GetBuildingTechnologyType(ISet<TechnologyType> types)
        {
            foreach (UnlockedBuilding buildingU in BuildingsUnlocked)
            {
                if (!ResourceManager.GetBuilding(buildingU.Name, out Building building))
                {
                    Log.Warning($"Tech {UID} unlock unavailable : {buildingU.Name}");
                    continue;
                }

                if (building.AllowInfantry || building.isWeapon || building.IsSensor ||
                    building.PlanetaryShieldStrengthAdded > 0 || building.CombatStrength > 0 || building.CanAttack)
                {
                    types.Add(TechnologyType.GroundCombat);
                }

                if (building.AllowShipBuilding || building.PlusFlatProductionAmount > 0 ||
                    building.PlusProdPerRichness > 0 || building.StorageAdded > 0 || building.PlusFlatProductionAmount > 0)
                {
                    types.Add(TechnologyType.Industry);
                }

                if (building.PlusTaxPercentage > 0 || building.CreditsPerColonist > 0)
                {
                    types.Add(TechnologyType.Economic);
                }

                if (building.PlusFlatResearchAmount > 0 || building.PlusResearchPerColonist > 0)
                {
                    types.Add(TechnologyType.Research);
                }

                if (building.PlusFoodPerColonist > 0 || building.PlusFlatFoodAmount > 0 ||
                    building.PlusFoodPerColonist > 0 || building.MaxPopIncrease > 0 ||
                    building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0 || building.IsBiospheres)
                {
                    types.Add(TechnologyType.Colonization);
                }
            }
        }

        void GetBonusTechTypes(ISet<TechnologyType> types)
        {
            foreach (UnlockedBonus unlockedBonus in BonusUnlocked)
            {
                switch (unlockedBonus.Type)
                {
                    case "SHIPMODULE":
                    case "HULL":     types.Add(TechnologyType.ShipGeneral);  break;
                    case "TROOP":    types.Add(TechnologyType.GroundCombat); break;
                    case "BUILDING": types.Add(TechnologyType.Colonization); break;
                    case "ADVANCE":  types.Add(TechnologyType.ShipGeneral);  break;
                }

                switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
                {
                    case "Xeno Compilers":
                    case "Research Bonus":
                        types.Add(TechnologyType.Research); break;
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
                        types.Add(TechnologyType.Economic); break;
                    case "Top Guns":
                    case "Bonus Fighter Levels":
                    case "Mass Reduction":
                    case "Percent Mass Adjustment":
                    case "STL Speed Bonus":
                    case "ArmourMass":
                        types.Add(TechnologyType.ShipGeneral); break;
                    case "Resistance is Futile":
                    case "Super Soldiers":
                    case "Troop Strength Modifier Bonus":
                    case "Allow Assimilation":
                        types.Add(TechnologyType.GroundCombat); break;
                    case "Cryogenic Suspension":
                        types.Add(TechnologyType.ShipGeneral); break;
                    case "Increased Lifespans":
                    case "Population Growth Bonus":
                    case "Set Population Growth Min":
                    case "Set Population Growth Max":
                        /*
                    case "Spy Offense":
                    case "Spy Offense Roll Bonus":
                    case "Spy Defense":
                    case "Spy Defense Roll Bonus":*/
                    case "Xenolinguistic Nuance":
                    case "Diplomacy Bonus":
                    case "Passenger Modifier":
                        types.Add(TechnologyType.Colonization); break;
                    case "Ordnance Effectiveness":
                    case "Ordnance Effectiveness Bonus":
                    case "Tachyons":
                    case "Sensor Range Bonus":
                    case "Fuel Cell Upgrade":
                    case "Ship Experience Bonus":
                    case "Power Flow Bonus":
                    case "Shield Power Bonus":
                    case "Fuel Cell Bonus":
                        types.Add(TechnologyType.ShipGeneral); break;
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
                        types.Add(TechnologyType.ShipGeneral); break;
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
                        types.Add(TechnologyType.ShipGeneral); break;
                    default:
                        types.Add(TechnologyType.General); break;
                }
            }
        }

        void GetHullTechTypes(ISet<TechnologyType> types)
        {
            foreach (UnlockedHull unlockedHull in HullsUnlocked)
            {
                if (ResourceManager.Hull(unlockedHull.Name, out ShipData hull))
                {
                    if (hull.IsShipyard)
                        types.Add(TechnologyType.Industry);

                    if (hull.Role == ShipData.RoleName.construction ||
                        hull.Role == ShipData.RoleName.freighter)
                        types.Add(TechnologyType.Industry);

                    if (hull.Role == ShipData.RoleName.station ||
                        hull.Role == ShipData.RoleName.platform ||
                        hull.Role == ShipData.RoleName.freighter ||
                        hull.Role >= ShipData.RoleName.fighter)
                        types.Add(TechnologyType.ShipHull);
                }
            }
        }

        void GetModuleTechTypes(ISet<TechnologyType> types)
        {
            foreach (UnlockedMod moduleU in ModulesUnlocked)
            {
                if (ResourceManager.GetModuleTemplate(moduleU.ModuleUID, out ShipModule module))
                {
                    bool genericShipTech = true;
                    if (module.InstalledWeapon != null
                        || module.MaximumHangarShipSize > 0
                        || module.Is(ShipModuleType.Hangar))
                    {
                        types.Add(TechnologyType.ShipWeapons);
                        genericShipTech = false;
                    }
                    if (module.shield_power_max >= 1f
                        || module.Is(ShipModuleType.Armor)
                        || module.Is(ShipModuleType.Countermeasure)
                        || module.Is(ShipModuleType.Shield))
                    {
                        types.Add(TechnologyType.ShipDefense);
                        genericShipTech = false;
                    }

                    if (genericShipTech)
                        types.Add(TechnologyType.ShipGeneral);
                }
                else
                    Log.Warning($"Tech {UID} unlock unavailable : {moduleU.ModuleUID}");
            }
        }
    }
}
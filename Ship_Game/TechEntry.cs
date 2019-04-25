using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class TechEntry
    {
        [Serialize(0)] public string UID;
        [Serialize(1)] public float Progress;
        [Serialize(2)] public bool Discovered;
        [Serialize(3)] public bool Unlocked;
        [Serialize(4)] public int Level;
        [Serialize(5)] public string AcquiredFrom = "";
        [Serialize(6)] public bool shipDesignsCanuseThis = true;

        [XmlIgnore][JsonIgnore]
        public float TechCost => Tech.ActualCost * (float)Math.Max(1, Math.Pow(2.0, Level));

        //add initializer for tech
        [XmlIgnore][JsonIgnore]
        public Technology Tech { get; private set; }

        [XmlIgnore][JsonIgnore]
        public bool IsRoot => Tech.RootNode == 1;

        [XmlIgnore][JsonIgnore]
        public Array<string> ConqueredSource = new Array<string>();
        public TechnologyType TechnologyType => Tech.TechnologyType;
        [XmlIgnore][JsonIgnore] public Array<TechnologyType>  TechnologyTypes => Tech.TechnologyTypes;
        public int MaxLevel => Tech.MaxLevel;
        [XmlIgnore][JsonIgnore]
        readonly Dictionary<TechnologyType, float> TechTypeCostLookAhead = new Dictionary<TechnologyType, float>();

        public static readonly TechEntry None = new TechEntry("");
        
        public TechEntry()
        {
            foreach (TechnologyType techType in Enum.GetValues(typeof(TechnologyType)))
                TechTypeCostLookAhead.Add(techType, 0);
        }

        public TechEntry(string uid) : this()
        {
            UID = uid;
            ResolveTech();
        }

        public void ResolveTech()
        {
            Tech = UID.NotEmpty() ? ResourceManager.TechTree[UID] : Technology.Dummy;
        }

        public bool UnlocksFoodBuilding => GoodsBuildingUnlocked(Goods.Food);
        bool GoodsBuildingUnlocked(Goods good)
        {
            foreach (Building building in Tech.GetBuildings())
            {
                switch (good)
                {
                    case Goods.None:
                        break;
                    case Goods.Production:
                        break;
                    case Goods.Food:
                    {
                        if (building.PlusFlatFoodAmount > 0
                            || building.PlusFoodPerColonist > 0
                            || building.PlusTerraformPoints > 0
                            || building.MaxFertilityOnBuild > 0)
                            return true;
                        break;
                    }
                    case Goods.Colonists:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(good), good, null);
                }
            }
            return false;
        }

        public bool IsShipTech()
        {
            if (IsTechnologyType(TechnologyType.ShipDefense)) return true;
            if (IsTechnologyType(TechnologyType.ShipGeneral)) return true;
            if (IsTechnologyType(TechnologyType.ShipHull))    return true;
            if (IsTechnologyType(TechnologyType.ShipWeapons)) return true;
            return false;
        }

        public bool IsTechnologyType(TechnologyType techType)
        {
            return TechnologyTypes.Contains(techType);
        }

        public float CostOfNextTechWithType(TechnologyType techType) => TechTypeCostLookAhead[techType];

        public void SetLookAhead(Empire empire)
        {
            foreach (TechnologyType techType in Enum.GetValues(typeof(TechnologyType)))
            {
                TechTypeCostLookAhead[techType] = LookAheadCost(techType, empire);
            }
        }

        float LookAheadCost(TechnologyType techType, Empire empire)
        {
            if (!Discovered) return 0;

            //if current tech == wanted type return this techs cost
            if (!Unlocked && Tech.TechnologyTypes.Contains(techType))
                return TechCost;
            
            //look through all leadtos and find a future tech with this type.
            //return the cost to get to this tech
            float cost = 0;
            if (Tech.LeadsTo.Count == 0)
                return 0;
            foreach (Technology.LeadsToTech leadsTo in Tech.LeadsTo)
            {
                float tempCost = empire.GetTechEntry(leadsTo.UID).LookAheadCost(techType, empire);
                if (tempCost > 0) cost = cost > 0 ? Math.Min(tempCost, cost) : tempCost;
            }
            return cost == 0 ? 0 : TechCost + cost;
        }

        bool CheckSource(string unlockType, Empire empire)
        {
            if (unlockType == null)
                return true;
            if (unlockType == "ALL")
                return true;
            if (unlockType == AcquiredFrom)
                return true;
            if (unlockType == empire.data.Traits.ShipType)
                return true;
            if (ConqueredSource.Contains(unlockType))
                return true;
            return false;
        }

        public Array<string> UnLockHulls(Empire empire)
        {
            var techHulls = Tech.HullsUnlocked;
            if (techHulls.IsEmpty) return null;
            var hullList = new Array<string>();

            foreach (Technology.UnlockedHull unlockedHull in techHulls)
            {
                if (!CheckSource(unlockedHull.ShipType, empire))
                    continue;

                empire.UnlockEmpireHull(unlockedHull.Name, UID);
                hullList.Add(unlockedHull.Name);

            }
            empire.UpdateShipsWeCanBuild(hullList);
            return hullList;
        }

        public Array<Technology.UnlockedMod> GetUnlockableModules(Empire empire)
        {
            var modulesUnlocked = new Array<Technology.UnlockedMod>();
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in Tech.ModulesUnlocked)
            {
                if (!CheckSource(unlockedMod.Type, empire))
                    continue;
                modulesUnlocked.Add(unlockedMod);
            }
            return modulesUnlocked;
        }

        public Array<Technology.UnlockedHull> GetUnlockableHulls(Empire empire)
        {
            var modulesUnlocked = new Array<Technology.UnlockedHull>();
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedHull unlockedHull in Tech.HullsUnlocked)
            {
                if (!CheckSource(unlockedHull.ShipType, empire))
                    continue;
                modulesUnlocked.Add(unlockedHull);
            }
            return modulesUnlocked;
        }

        public void UnlockModules(Empire empire)
        {
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in GetUnlockableModules(empire))
                empire.UnlockEmpireShipModule(unlockedMod.ModuleUID, UID);
        }

        public void UnlockTroops(Empire empire) //  Array<Technology.UnlockedTroop> unlockedTroops, string shipType, string techType = null)
        {
            foreach (Technology.UnlockedTroop unlockedTroop in Tech.TroopsUnlocked)
            {
                if (!CheckSource(unlockedTroop.Type, empire))
                    continue;

                empire.UnlockEmpireTroop(unlockedTroop.Name);

            }
        }
        public void UnlockBuildings(Empire empire)
        {
            foreach (Technology.UnlockedBuilding unlockedBuilding in Tech.BuildingsUnlocked)
            {
                if (!CheckSource(unlockedBuilding.Type, empire))
                    continue;
                empire.UnlockEmpireBuilding(unlockedBuilding.Name);
            }
        }
        public bool TriggerAnyEvents(Empire empire)
        {
            bool triggered = false;
            if (empire.isPlayer)
            {
                foreach (Technology.TriggeredEvent triggeredEvent in Tech.EventsTriggered)
                {
                    string type = triggeredEvent.Type;
                    if (CheckSource(type, empire))
                        continue;
                    if (triggeredEvent.CustomMessage != null)
                        Empire.Universe.NotificationManager.AddNotify(triggeredEvent, triggeredEvent.CustomMessage);
                    else
                        Empire.Universe.NotificationManager.AddNotify(triggeredEvent);
                    triggered = true;
                }
            }
            return triggered;
        }

        public int CountTechsToOneInList(HashSet<string> techList, Empire empire)
        {
            int count = 0;
            foreach (Technology.LeadsToTech leadTo in Tech.LeadsTo)
            {
                if (techList.Contains(leadTo.UID))
                {
                    count++;
                    return count + empire.GetTechEntry(leadTo.UID).CountTechsToOneInList(techList, empire);
                }
            }
            return count;

        }

        public void ForceFullyResearched()
        {
            Progress = Tech.ActualCost;
            Unlocked = true;
        }

        public void ForceNeedsFullResearch()
        {
            Progress = 0;
            Unlocked = false;
        }

        public bool Unlock(Empire empire)
        {
            if (!SetDiscovered(empire))
                return false;
            if (Tech.MaxLevel > 1)
            {
                Level++;
                if (Level == Tech.MaxLevel)
                {
                    Progress = TechCost ;
                    Unlocked = true;
                }
                else
                {
                    Unlocked = false;
                    Progress = 0;
                }
            }
            else
            {
                Progress = Tech.ActualCost;
                Unlocked = true;
            }
            DoRevealedTechs(empire);
            TriggerAnyEvents(empire);
            UnlockModules(empire);
            UnlockTroops(empire);
            UnLockHulls(empire);
            UnlockBuildings(empire);
            UnlockBonus(empire);
            return true;
        }

        public void UnlockFromSave(Empire empire)
        {
            Progress = TechCost;
            Unlocked = true;
            DoRevealedTechs(empire);
            UnlockModules(empire);
            UnlockTroops(empire);
            UnLockHulls(empire);
            UnlockBuildings(empire);
        }

        public void LoadShipModelsFromDiscoveredTech(Empire empire)
        {
            if (!Discovered || Tech.HullsUnlocked.IsEmpty)
                return;

            foreach (var hullName in Tech.HullsUnlocked)
            {
                if (ResourceManager.Hull(hullName.Name, out ShipData shipData) &&
                    shipData.ShipStyle == empire.data.Traits.ShipType)
                    shipData.PreLoadModel();
            }
        }

        public bool IsUnlockedAtGameStart(Empire empire)
        {
            return InRaceRequirementsArray(empire, Tech.UnlockedAtGameStart);
        }

        private static bool InRaceRequirementsArray(Empire empire, IEnumerable<Technology.RaceRequirements> requiredRace)
        {
            foreach (Technology.RaceRequirements item in requiredRace)
            {
                if (item.ShipType == empire.data.Traits.ShipType)
                    return true;

                switch (item.RacialTrait) {
                    case RacialTrait.NameOfTrait.None:
                        break;
                    case RacialTrait.NameOfTrait.Cybernetic:
                        if (empire.data.Traits.Cybernetic > 0)
                            return true;
                        break;
                    case RacialTrait.NameOfTrait.Militaristic:
                        if (empire.data.Traits.Militaristic > 0)
                            return true;
                        break;
                }
            }
            return false;
        }

        public bool IsHidden(Empire empire)
        {
            if (IsUnlockedAtGameStart(empire))
                return false;
            if (Tech.HiddenFrom.Count > 0 && InRaceRequirementsArray(empire, Tech.HiddenFrom))
                return true;
            if (Tech.HiddenFromAllExcept.Count > 0 && !InRaceRequirementsArray(empire, Tech.HiddenFromAllExcept))
                return true;
            return false;
        }

        public void DoRevealedTechs(Empire empire)
        {
            // Added by The Doctor - reveal specified 'secret' techs with unlocking of techs, via Technology XML
            foreach (Technology.RevealedTech revealedTech in Tech.TechsRevealed)
            {
                if (!CheckSource(revealedTech.Type, empire))
                    continue;
                if (!IsHidden(empire))
                    Discovered = true;
            }
            if (Tech.Secret && Tech.RootNode == 0)
            {
                foreach (Technology.LeadsToTech leadsToTech in Tech.LeadsTo)
                {
                    //added by McShooterz: Prevent Racial tech from being discovered by unintentional means
                    TechEntry tech = empire.GetTechEntry(leadsToTech.UID);
                    if (!tech.IsHidden(empire))
                        tech.Discovered = true;
                }
            }
        }

        public void SetDiscovered(bool discovered = true) => Discovered = discovered;


        public bool SetDiscovered(Empire empire, bool discoverForward = true)
        {
            if (IsHidden(empire))
                return false;

            Discovered = true;
            DiscoverToRoot(empire);
            if (Tech.Secret) return true;
            if (discoverForward)
            {
                foreach (Technology.LeadsToTech leadsToTech in Tech.LeadsTo)
                {
                    TechEntry tech = empire.GetTechEntry(leadsToTech.UID);
                    if (!tech.IsHidden(empire))
                        tech.SetDiscovered(empire);
                }
            }
            return true;
        }

        public TechEntry DiscoverToRoot(Empire empire)
        {
            TechEntry tech = this;
            while (tech.Tech.RootNode != 1)
            {
                var rootTech = tech.GetPreReq(empire);
                if (rootTech == null)
                    break;
                rootTech.SetDiscovered(empire, false);
                if ((tech = rootTech).Tech.RootNode != 1 || !tech.Discovered)
                    continue;
                rootTech.Unlocked = true;
                return rootTech;
            }
            return tech;
        }

        public TechEntry GetPreReq(Empire empire)
        {
            foreach (var keyValuePair in empire.TechnologyDict)
            {
                Technology technology = keyValuePair.Value.Tech;
                foreach (Technology.LeadsToTech leadsToTech in technology.LeadsTo)
                {
                    if (leadsToTech.UID != UID) continue;
                    if (keyValuePair.Value.Tech.RootNode ==1 || !keyValuePair.Value.IsHidden(empire))
                        return keyValuePair.Value;

                    return keyValuePair.Value.GetPreReq(empire);
                }
            }
            return null;
        }

        public bool HasPreReq(Empire empire)
        {
            TechEntry preReq = GetPreReq(empire);

            if (preReq == null || preReq.UID.IsEmpty())
                return false;

            if (preReq.Unlocked || !preReq.Discovered)
                return true;

            return false;
        }

        public TechEntry FindNextDiscoveredTech(Empire empire)
        {
            if (Discovered)
                return this;
            foreach (Technology child in Tech.Children)
            {
                TechEntry discovered = empire.GetTechEntry(child)
                                             .FindNextDiscoveredTech(empire);
                if (discovered != null)
                    return discovered;
            }
            return null;
        }

        public TechEntry[] FindChildEntries(Empire empire)
        {
            return Tech.Children.Select(empire.GetTechEntry);
        }

        public TechEntry[] GetPlayerChildEntries()
        {
            return Tech.Children.Select(EmpireManager.Player.GetTechEntry);
        }

        public Array<TechEntry> GetFirstDiscoveredEntries()
        {
            var entries = new Array<TechEntry>();
            foreach (TechEntry child in GetPlayerChildEntries())
            {
                TechEntry discovered = child.FindNextDiscoveredTech(EmpireManager.Player);
                if (discovered != null) entries.Add(discovered);
            }
            return entries;
        }

        public void UnlockBonus(Empire empire)
        {
            var data = empire.data;
            if (Tech.BonusUnlocked.Count < 1)
                return;
            //update ship stats if a bonus was unlocked

            empire.TriggerAllShipStatusUpdate();

            foreach (Technology.UnlockedBonus unlockedBonus in Tech.BonusUnlocked)
            {
                //Added by McShooterz: Race Specific bonus
                string type = unlockedBonus.Type;
                if (type != null && type != data.Traits.ShipType && type != AcquiredFrom)
                    continue;

                if (unlockedBonus.Tags.Count <= 0)
                {
                    UnlockOtherBonuses(empire, unlockedBonus);
                    continue;
                }
                foreach (string index in unlockedBonus.Tags)
                {
                    var tagmod = data.WeaponTags[index];
                    switch (unlockedBonus.BonusType)
                    {
                        case "Weapon_Speed"            : tagmod.Speed             += unlockedBonus.Bonus; continue;
                        case "Weapon_Damage"           : tagmod.Damage            += unlockedBonus.Bonus; continue;
                        case "Weapon_ExplosionRadius"  : tagmod.ExplosionRadius   += unlockedBonus.Bonus; continue;
                        case "Weapon_TurnSpeed"        : tagmod.Turn              += unlockedBonus.Bonus; continue;
                        case "Weapon_Rate"             : tagmod.Rate              += unlockedBonus.Bonus; continue;
                        case "Weapon_Range"            : tagmod.Range             += unlockedBonus.Bonus; continue;
                        case "Weapon_ShieldDamage"     : tagmod.ShieldDamage      += unlockedBonus.Bonus; continue;
                        case "Weapon_ArmorDamage"      : tagmod.ArmorDamage       += unlockedBonus.Bonus; continue;
                        case "Weapon_HP"               : tagmod.HitPoints         += unlockedBonus.Bonus; continue;
                        case "Weapon_ShieldPenetration": tagmod.ShieldPenetration += unlockedBonus.Bonus; continue;
                        case "Weapon_ArmourPenetration": tagmod.ArmourPenetration += unlockedBonus.Bonus; continue;
                        default                        : continue;
                    }
                }
            }
        }

        private void UnlockOtherBonuses(Empire empire, Technology.UnlockedBonus unlockedBonus)
        {
            var data = empire.data;
            switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
            {
                case "Xeno Compilers":
                case "Research Bonus": data.Traits.ResearchMod += unlockedBonus.Bonus; break;
                case "FTL Spool Bonus":
                    if (unlockedBonus.Bonus < 1) data.SpoolTimeModifier *= 1.0f - unlockedBonus.Bonus; // i.e. if there is a 0.2 (20%) bonus unlocked, the spool modifier is 1-0.2 = 0.8* existing spool modifier...
                    else if (unlockedBonus.Bonus >= 1) data.SpoolTimeModifier = 0f; // insta-warp by modifier
                    break;
                case "Top Guns":
                case "Bonus Fighter Levels":
                    data.BonusFighterLevels += (int)unlockedBonus.Bonus;
                    empire.IncreaseEmpireShipRoleLevel(ShipData.RoleName.fighter, (int)unlockedBonus.Bonus);
                    break;
                case "Mass Reduction":
                case "Percent Mass Adjustment": data.MassModifier += unlockedBonus.Bonus; break;
                case "ArmourMass": data.ArmourMassModifier += unlockedBonus.Bonus; break;
                case "Resistance is Futile":
                case "Allow Assimilation": data.Traits.Assimilators = true; break;
                case "Cryogenic Suspension":
                case "Passenger Modifier": data.Traits.PassengerModifier += unlockedBonus.Bonus; break;
                case "ECM Bonus":
                case "Missile Dodge Change Bonus": data.MissileDodgeChance += unlockedBonus.Bonus; break;
                case "Set FTL Drain Modifier": data.FTLPowerDrainModifier = unlockedBonus.Bonus; break;
                case "Super Soldiers":
                case "Troop Strength Modifier Bonus": data.Traits.GroundCombatModifier += unlockedBonus.Bonus; break;
                case "Fuel Cell Upgrade":
                case "Fuel Cell Bonus": data.FuelCellModifier += unlockedBonus.Bonus; break;
                case "Trade Tariff":
                case "Bonus Money Per Trade": data.Traits.Mercantile += unlockedBonus.Bonus; break;
                case "Missile Armor":
                case "Missile HP Bonus": data.MissileHPModifier += unlockedBonus.Bonus; break;
                case "Hull Strengthening":
                case "Module HP Bonus":
                    data.Traits.ModHpModifier += unlockedBonus.Bonus;
                    EmpireShipBonuses.RefreshBonuses(empire); // RedFox: This will refresh all empire module stats
                    break;
                case "Reaction Drive Upgrade":
                case "STL Speed Bonus": data.SubLightModifier += unlockedBonus.Bonus; break;
                case "Reactive Armor":
                case "Armor Explosion Reduction": data.ExplosiveRadiusReduction += unlockedBonus.Bonus; break;
                case "Slipstreams":
                case "In Borders FTL Bonus": data.Traits.InBordersSpeedBonus += unlockedBonus.Bonus; break;
                case "StarDrive Enhancement":
                case "FTL Speed Bonus": data.FTLModifier += unlockedBonus.Bonus * data.FTLModifier; break;
                case "FTL Efficiency":
                case "FTL Efficiency Bonus": data.FTLPowerDrainModifier -= unlockedBonus.Bonus * data.FTLPowerDrainModifier; break;
                case "Spy Offense":
                case "Spy Offense Roll Bonus": data.OffensiveSpyBonus += unlockedBonus.Bonus; break;
                case "Spy Defense":
                case "Spy Defense Roll Bonus": data.DefensiveSpyBonus += unlockedBonus.Bonus; break;
                case "Increased Lifespans":
                case "Population Growth Bonus": data.Traits.ReproductionMod += unlockedBonus.Bonus; break;
                case "Set Population Growth Min": data.Traits.PopGrowthMin = unlockedBonus.Bonus; break;
                case "Set Population Growth Max": data.Traits.PopGrowthMax = unlockedBonus.Bonus; break;
                case "Xenolinguistic Nuance":
                case "Diplomacy Bonus": data.Traits.DiplomacyMod += unlockedBonus.Bonus; break;
                case "Ordnance Effectiveness":
                case "Ordnance Effectiveness Bonus": data.OrdnanceEffectivenessBonus += unlockedBonus.Bonus; break;
                case "Tachyons":
                case "Sensor Range Bonus": data.SensorModifier += unlockedBonus.Bonus; break;
                case "Privatization": data.Privatization = true; break;
                // Doctor                           : Adding an actually configurable amount of civilian maintenance modification; privatisation is hardcoded at 50% but have left it in for back-compatibility.
                case "Civilian Maintenance": data.CivMaintMod -= unlockedBonus.Bonus; break;
                case "Armor Piercing":
                case "Armor Phasing": data.ArmorPiercingBonus += (int)unlockedBonus.Bonus; break;
                case "Kulrathi Might":
                    data.Traits.ModHpModifier += unlockedBonus.Bonus;
                    EmpireShipBonuses.RefreshBonuses(empire); // RedFox: This will refresh all empire module stats
                    break;
                case "Subspace Inhibition": data.Inhibitors = true; break;
                // Added by McShooterz              : New Bonuses
                case "Production Bonus": data.Traits.ProductionMod += unlockedBonus.Bonus; break;
                case "Construction Bonus": data.Traits.ShipCostMod -= unlockedBonus.Bonus; break;
                case "Consumption Bonus": data.Traits.ConsumptionModifier -= unlockedBonus.Bonus; break;
                case "Tax Bonus": data.Traits.TaxMod += unlockedBonus.Bonus; break;
                case "Repair Bonus": data.Traits.RepairMod += unlockedBonus.Bonus; break;
                case "Maintenance Bonus": data.Traits.MaintMod -= unlockedBonus.Bonus; break;
                case "Power Flow Bonus": data.PowerFlowMod += unlockedBonus.Bonus; break;
                case "Shield Power Bonus": data.ShieldPowerMod += unlockedBonus.Bonus; break;
                case "Ship Experience Bonus": data.ExperienceMod += unlockedBonus.Bonus; break;
                case "Kinetic Shield Penetration Chance Bonus": data.ShieldPenBonusChance += unlockedBonus.Bonus; break;
            }
        }
    }
}
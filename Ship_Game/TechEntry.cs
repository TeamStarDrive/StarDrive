using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class TechEntry : IEquatable<TechEntry>
    {
        [Serialize(0)] public string UID;
        [Serialize(1)] public float Progress;
        [Serialize(2)] public bool Discovered;
        [Serialize(3)] public bool Unlocked;
        [Serialize(4)] public int Level;
        [Serialize(5)] public string AcquiredFrom { private get; set; }
        [Serialize(6)] public bool shipDesignsCanuseThis = true;
        [Serialize(7)] public Array<string> WasAcquiredFrom;


        public bool Locked => !Unlocked;

        /// <summary>
        /// Checks if list  contains restricted trade type. 
        /// </summary>
        private bool AllowRacialTrade(Empire us, Empire them)
        {
            return us.GetRelations(them).AllowRacialTrade();
        }

        [XmlIgnore][JsonIgnore]
        public float TechCost
        {
            get
            {
                float cost      = Tech.ActualCost;
                float techLevel = (float)Math.Max(1, Math.Pow(2.0, Level));
                int rootTech    = Tech.RootNode * 100;
                return cost * (techLevel + rootTech);
            }
        }

        [XmlIgnore][JsonIgnore]
        public float PercentResearched => Progress / TechCost;

        // add initializer for tech
        [XmlIgnore][JsonIgnore]
        public Technology Tech { get; private set; }

        [XmlIgnore][JsonIgnore]
        public bool IsRoot => Tech.RootNode == 1;

        [XmlIgnore][JsonIgnore]
        public Array<string> ConqueredSource = new Array<string>();

        [XmlIgnore][JsonIgnore]
        public TechnologyType TechnologyType => Tech.TechnologyTypes.First();

        public bool IsTechnologyType(TechnologyType type) => Tech.TechnologyTypes.Contains(type);

        [XmlIgnore][JsonIgnore]
        public int MaxLevel => Tech.MaxLevel;

        [XmlIgnore][JsonIgnore]
        readonly Dictionary<TechnologyType, float> TechTypeCostLookAhead = new Dictionary<TechnologyType, float>();

        public static readonly TechEntry None = new TechEntry("");

        public override string ToString()
            => $"TechEntry Disc={Discovered} Unl={Unlocked} {UID}";

        public TechEntry()
        {
            WasAcquiredFrom = new Array<string>();
            if (AcquiredFrom.NotEmpty())
                WasAcquiredFrom.AddUnique(AcquiredFrom);

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

        /// <summary>
        /// Returns empire research not used.
        /// Cybernetic gets a break on food buildings here. 
        /// </summary>
        public float AddToProgress(float researchToApply, Empire us, out bool unLocked)
        {
            float modifier = us.data.Traits.ResearchMultiplierForTech(this, us);
            return AddToProgress(researchToApply, modifier, out unLocked);
        }

        public float AddToProgress(float researchToApply, float modifier, out bool unLocked)
        {
            float techCost = Tech.ActualCost * modifier;
            Progress += researchToApply;
            float excessResearch = Math.Max(0, Progress - techCost);
            Progress -= excessResearch;
            unLocked = Progress.AlmostEqual(techCost);
            return excessResearch;
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

        public bool IsOnlyShipTech()
        {
            return !ContainsNonShipTechOrBonus();
        }

        public bool IsOnlyNonShipTech()
        {
            return !ContainsShipTech();
        }

        public bool ContainsShipTech()
        {
            return IsTechnologyType(TechnologyType.ShipDefense)
                || IsTechnologyType(TechnologyType.ShipGeneral)
                || IsTechnologyType(TechnologyType.ShipHull)
                || IsTechnologyType(TechnologyType.ShipWeapons);
        }

        public bool ContainsNonShipTechOrBonus()
        {
            return IsTechnologyType(TechnologyType.General)
                || IsTechnologyType(TechnologyType.Colonization)
                || IsTechnologyType(TechnologyType.Economic)
                || IsTechnologyType(TechnologyType.Research)
                || IsTechnologyType(TechnologyType.GroundCombat)
                || IsTechnologyType(TechnologyType.Industry)
                || Tech.BonusUnlocked.NotEmpty;
        }

        public bool ContainsHullTech()
        {
            return IsTechnologyType(TechnologyType.ShipHull);
        }

        public float CostOfNextTechWithType(TechnologyType techType) => TechTypeCostLookAhead[techType];

        public void SetLookAhead(Empire empire)
        {
            foreach (TechnologyType techType in Enum.GetValues(typeof(TechnologyType)))
            {
                TechTypeCostLookAhead[techType] = LookAheadCost(techType, empire);
            }
        }

        public bool SpiedFrom(Empire them) => WasAcquiredFrom.Contains(them.data.Traits.ShipType);
        public bool SpiedFromAnyBut(Empire them) => WasAcquiredFrom.Count > 1 && !WasAcquiredFrom.Contains(them.data.Traits.ShipType);

        public bool TheyCanUseThis(Empire us, Empire them)
        {
            var theirTech        = them.GetTechEntry(this);
            bool theyCanUnlockIt = !theirTech.Unlocked && (them.HavePreReq(UID) ||
                                                           AllowRacialTrade(us, them) && theirTech.IsRoot);
            bool notHasContent   = AllowRacialTrade(us, them) && ContentRestrictedTo(us) 
                                                  && !theirTech.SpiedFrom(us) 
                                                  && (theirTech.IsRoot || them.HavePreReq(UID));
            return theyCanUnlockIt || notHasContent;
        }

        bool ContentRestrictedTo(Empire empire)
        {
            bool hulls     = Tech.HullsUnlocked.Any(item => item.ShipType == empire.data.Traits.ShipType);
            bool buildings = Tech.BuildingsUnlocked.Any(item => item.Type == empire.data.Traits.ShipType);
            bool troops    = Tech.TroopsUnlocked.Any(item => item.Type == empire.data.Traits.ShipType);
            bool modules   = Tech.ModulesUnlocked.Any(item => item.Type == empire.data.Traits.ShipType);
            bool bonus     = Tech.BonusUnlocked.Any(item => item.Type == empire.data.Traits.ShipType);
            return hulls || buildings || troops || modules || bonus;
        }

        float LookAheadCost(TechnologyType techType, Empire empire)
        {
            if (!Discovered)
                return 0;

            // if current tech == wanted type return this techs cost
            if (!Unlocked && IsTechnologyType(techType))
                return TechCost;

            // look through all leadtos and find a future tech with this type.
            // return the cost to get to this tech
            if (Tech.LeadsTo.Count == 0)
                return 0;

            float cost = 0;
            foreach (Technology.LeadsToTech leadsTo in Tech.LeadsTo)
            {
                float tempCost = empire.GetTechEntry(leadsTo.UID).LookAheadCost(techType, empire);
                if (tempCost > 0) cost = cost > 0 ? Math.Min(tempCost, cost) : tempCost;
            }
            return cost.AlmostZero() ? 0 : TechCost + cost;
        }

        bool CheckSource(string unlockType, Empire empire)
        {
            if (unlockType == null)
                return true;
            if (unlockType == "ALL")
                return true;
            if (WasAcquiredFrom.Contains(unlockType))
                return true;
            if (unlockType == empire.data.Traits.ShipType)
                return true;
            if (ConqueredSource.Contains(unlockType))
                return true;
            return false;
        }

        public Array<string> GetUnLockableHulls(Empire empire)
        {
            var techHulls = Tech.HullsUnlocked;
            var hullList = new Array<string>();
            if (techHulls.IsEmpty) return hullList;

            foreach (Technology.UnlockedHull unlockedHull in techHulls)
            {
                if (!CheckSource(unlockedHull.ShipType, empire))
                    continue;

                hullList.Add(unlockedHull.Name);
            }
            return hullList;
        }

        public Array<string> UnLockHulls(Empire us, Empire them)
        {
            var hullList = GetUnLockableHulls(them);
            if (hullList.IsEmpty) return null;

            foreach(var hull in hullList) us.UnlockEmpireHull(hull, UID);

            us.UpdateShipsWeCanBuild(hullList);
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

        public void UnlockModules(Empire us, Empire them)
        {
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in GetUnlockableModules(them))
                us.UnlockEmpireShipModule(unlockedMod.ModuleUID, UID);
        }

        public void UnlockTroops(Empire us, Empire them)
        {
            if (us != them) return;
            foreach (Technology.UnlockedTroop unlockedTroop in Tech.TroopsUnlocked)
            {
                if (CheckSource(unlockedTroop.Type, them))
                    us.UnlockEmpireTroop(unlockedTroop.Name);
            }
        }
        public void UnlockBuildings(Empire us, Empire them)
        {
            foreach (Technology.UnlockedBuilding unlockedBuilding in Tech.BuildingsUnlocked)
            {
                if (CheckSource(unlockedBuilding.Type, them))
                    us.UnlockEmpireBuilding(unlockedBuilding.Name);
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
                    if (!CheckSource(type, empire))
                    {
                        if (triggeredEvent.CustomMessage != null)
                            Empire.Universe.NotificationManager.AddNotify(triggeredEvent, triggeredEvent.CustomMessage);
                        else
                            Empire.Universe.NotificationManager.AddNotify(triggeredEvent);
                        triggered = true;
                    }
                }
            }
            return triggered;
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

        /// <summary>
        /// return <see langword="true"/> if <see langword="unlocked"/>  here.
        /// MultiLevel tech can have unlock flag <see langword="true"/> and then set <see langword="false"/>
        /// on level up. 
        /// </summary>
        /// <returns></returns>
        bool SetMultiLevelUnlockFlag()
        {
            if (Level >= Tech.MaxLevel) return false;

            Level++;
            if (Level == Tech.MaxLevel)
            {
                Progress = TechCost;
                Unlocked = true;
            }
            else
            {
                Unlocked = false;
                Progress = 0;
            }
            return true;
        }

        /// <returns>true if tech was unlocked by this method</returns>
        bool SetUnlockFlag()
        {
            if (Tech.MaxLevel > 1) return SetMultiLevelUnlockFlag();
            if (Unlocked) return false;

            Progress    = Tech.ActualCost;
            Unlocked    = true;
            return true;
        }

        public bool Unlock(Empire us, Empire them = null)
        {
            if (!SetDiscovered(us))
                return false;

            them = them ?? us;
            bool techWasUnlocked = SetUnlockFlag();
            UnlockTechContentOnly(us, them, techWasUnlocked);
            TriggerAnyEvents(us);
            return true;
        }

        public void UnlockWithBonus(Empire us, Empire them, bool unlockBonus)
        {
            them = them ?? us;
  
            unlockBonus = SetUnlockFlag() && unlockBonus;
            UnlockTechContentOnly(us, them, unlockBonus);
            if (them != us)
                WasAcquiredFrom.AddUnique(them.data.Traits.ShipType);
        }

        public void UnlockByConquest(Empire us, Empire conqueredEmpire, TechEntry conqueredTech)
        {
            if (!Discovered && conqueredTech.Discovered)
            {
                SetDiscovered(us);
            }

            if (!Unlocked && conqueredTech.Unlocked)
            {
                Unlock(us);
                ConqueredSource.AddUnique(conqueredEmpire.data.Traits.ShipType);
                WasAcquiredFrom.AddUnique(conqueredEmpire.data.Traits.ShipType);
            }
        }

        /// <summary>
        /// This will unlock the content of the tech. Hulls/modules/buildings etc.
        /// "us" is the empire the tech is being unlocked for.
        /// "them" is the racial specific tech of a different Empire.
        /// "bonusUnlock" sets if bonuses will unlock too.
        /// be careful bonuses currently stack and should only be unlocked once. 
        /// </summary>
        /// <param name="us"></param>
        /// <param name="them"></param>
        /// <param name="bonusUnlock"></param>
        /// <returns></returns>
        void UnlockTechContentOnly(Empire us, Empire them, bool bonusUnlock = true)
        {
            DoRevealedTechs(us);
            if (bonusUnlock) UnlockBonus(us, them);
            UnlockModules(us, them);
            UnlockTroops(us, them);
            UnLockHulls(us, them);
            UnlockBuildings(us, them);

            // Finally, remove this tech from our ResearchQueue
            us.Research.RemoveFromQueue(UID);
        }

        public bool UnlockFromSpy(Empire us, Empire them)
        {
            if (!Unlocked && (RandomMath.RollDice(50) || !ContentRestrictedTo(them)))
            {
                AddToProgress(TechCost * 0.25f, us, out bool unLocked);
                if (unLocked) us.UnlockTech(this, TechUnlockType.Normal, null);
                return unLocked;
            }

            if (WasAcquiredFrom.AddUnique(them.data.Traits.ShipType))
            {
                if (!ContentRestrictedTo(them) && Tech.BonusUnlocked.NotEmpty
                                               && Tech.BuildingsUnlocked.IsEmpty
                                               && Tech.ModulesUnlocked.IsEmpty
                                               && Tech.HullsUnlocked.IsEmpty && Tech.TroopsUnlocked.IsEmpty)
                {
                    Unlock(us);
                }
                else
                {
                    UnlockTechContentOnly(us, them);
                }
                return true;
            }
            return false;
        }

        public bool UnlockFromScrap(Empire us, Empire them)
        {
            if (Locked)
            {
                float percentToAdd = 0.25f * (1 + us.data.Traits.ModHpModifier); // skilled or bad engineers
                AddToProgress(TechCost * percentToAdd, us, out bool unLocked);
                if (unLocked)
                    us.UnlockTech(this, TechUnlockType.Normal, null);

                return true;
            }

            if (WasAcquiredFrom.AddUnique(them.data.Traits.ShipType))
            {
                UnlockTechContentOnly(us, them);
                return true;
            }

            return false;
        }

        public bool UnlockFromDiplomacy(Empire us, Empire them)
        {
            if (!Unlocked)
            {
                Unlock(us);
                return true;
            }

            if (WasAcquiredFrom.AddUnique(them.data.Traits.ShipType))
            {
                UnlockTechContentOnly(us, them);
                return true;
            }
            return false;
        }

        void UnlockFromSave(Empire us, Empire them, bool contentOnly)
        {
            if (!contentOnly)
            {
                Progress = TechCost;
                Unlocked = true;
            }
            UnlockTechContentOnly(us, them, false);
        }

        void UnlockAcquiredContent(Empire us)
        {
            foreach (var empireName in WasAcquiredFrom)
            {
                var them = EmpireManager.GetEmpireByName(empireName);
                if (them != null)
                    UnlockFromSave(us, them, true);
            }
        }

        public void UnlockFromSave(Empire us, bool unlockBonuses)
        {
            UnlockAcquiredContent(us);

            if (Unlocked)
            {
                Unlocked = false;
                if (unlockBonuses)
                    Unlock(us);
                else
                    UnlockFromSave(us, us, false);
            }
        }
        public void DebugUnlockFromTechScreen(Empire us, Empire them, bool bonusUnlock = true)
        {
            SetDiscovered(us);
            UnlockWithBonus(us, them, bonusUnlock);
        }

        public bool IsUnlockedAtGameStart(Empire empire) 
            => InRaceRequirementsArray(empire, Tech.UnlockedAtGameStart);

        private static bool InRaceRequirementsArray(Empire empire, IEnumerable<Technology.RaceRequirements> requiredRace)
        {
            foreach (Technology.RaceRequirements item in requiredRace)
            {
                if (item.ShipType == empire.data.Traits.ShipType)
                    return true;

                switch (item.RacialTrait)
                {
                    case RacialTrait.NameOfTrait.None:
                        break;
                    case RacialTrait.NameOfTrait.Cybernetic:
                        if (empire.data.Traits.Cybernetic > 0)
                            return true;

                        break;
                    case RacialTrait.NameOfTrait.NonCybernetic:
                        if (empire.data.Traits.Cybernetic == 0)
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
                if (CheckSource(revealedTech.Type, empire))
                {
                    TechEntry tech = empire.GetTechEntry(revealedTech.RevUID);
                    if (!tech.IsHidden(empire))
                        tech.Discovered = true;
                }
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
            if (Tech.Secret) 
                return true;

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
                if ((tech = rootTech).Tech.RootNode == 1 && tech.Discovered)
                {
                    rootTech.Unlocked = true;
                    return rootTech;
                }
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

        public void UnlockBonus(Empire empire, Empire them)
        {
            if (Tech.BonusUnlocked.Count < 1)
                return;

            EmpireData theirData = them.data;
            string theirShipType = theirData.Traits.ShipType;

            // update ship stats if a bonus was unlocked
            empire.TriggerAllShipStatusUpdate();

            foreach (Technology.UnlockedBonus unlockedBonus in Tech.BonusUnlocked)
            {
                // Added by McShooterz: Race Specific bonus
                string type = unlockedBonus.Type;

                bool bonusRestrictedToThem = type != null && type == theirShipType;
                bool bonusRestrictedToUs = empire == them && bonusRestrictedToThem;
                bool bonusComesFromOtherEmpireAndNotRestrictedToThem = empire != them && !bonusRestrictedToThem;

                if (!bonusRestrictedToUs && bonusRestrictedToThem && !WasAcquiredFrom.Contains(theirShipType))
                    continue;

                if (bonusComesFromOtherEmpireAndNotRestrictedToThem)
                    continue;

                if (unlockedBonus.Tags.Count <= 0)
                {
                    UnlockOtherBonuses(empire, unlockedBonus);
                    continue;
                }

                foreach (string tag in unlockedBonus.Tags)
                {
                    ApplyWeaponTagBonusToEmpire(theirData, tag, unlockedBonus);
                }
            }
        }

        static void ApplyWeaponTagBonusToEmpire(EmpireData data, string tag, Technology.UnlockedBonus unlocked)
        {
            if (!Enum.TryParse(tag, out WeaponTag weaponTag))
            {
                Log.Error($"No such weapon tag type: '{tag}'");
                return;
            }

            WeaponTagModifier mod = data.WeaponTags[weaponTag];
            switch (unlocked.BonusType)
            {
                default: return;
                case "Weapon_Speed"            : mod.Speed             += unlocked.Bonus; break;
                case "Weapon_Damage"           : mod.Damage            += unlocked.Bonus; break;
                case "Weapon_ExplosionRadius"  : mod.ExplosionRadius   += unlocked.Bonus; break;
                case "Weapon_TurnSpeed"        : mod.Turn              += unlocked.Bonus; break;
                case "Weapon_Rate"             : mod.Rate              += unlocked.Bonus; break;
                case "Weapon_Range"            : mod.Range             += unlocked.Bonus; break;
                case "Weapon_ShieldDamage"     : mod.ShieldDamage      += unlocked.Bonus; break;
                case "Weapon_ArmorDamage"      : mod.ArmorDamage       += unlocked.Bonus; break;
                case "Weapon_HP"               : mod.HitPoints         += unlocked.Bonus; break;
                case "Weapon_ShieldPenetration": mod.ShieldPenetration += unlocked.Bonus; break;
                case "Weapon_ArmourPenetration": mod.ArmourPenetration += unlocked.Bonus; break;
            }
        }

        static void UnlockOtherBonuses(Empire empire, Technology.UnlockedBonus unlockedBonus)
        {
            EmpireData data = empire.data;
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
                case "Diplomacy Bonus": data.OngoingDiplomaticModifier += unlockedBonus.Bonus; break;
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
                case "Shield Power Bonus": 
                    data.ShieldPowerMod += unlockedBonus.Bonus;
                    EmpireShipBonuses.RefreshBonuses(empire); 
                    break;
                case "Ship Experience Bonus": data.ExperienceMod += unlockedBonus.Bonus; break;
                case "Kinetic Shield Penetration Chance Bonus": data.ShieldPenBonusChance += unlockedBonus.Bonus; break;
                case "Tax Goods": data.Traits.TaxGoods = true; break;
                case "Smart Missiles": data.Traits.SmartMissiles = true; break; // Fb - Smart re target
                case "Minimum Troop Level": data.MinimumTroopLevel += (int)unlockedBonus.Bonus; break; // FB Minimum Troop Level Bonus
                case "Bomb Environment Damage Bonus": data.BombEnvironmentDamageMultiplier += unlockedBonus.Bonus; break;
            }
        }

        public bool Equals(TechEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UID == other.UID;
        }

        public override bool Equals(object obj)
        {
            return obj is TechEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return UID.GetHashCode();
        }
    }
}
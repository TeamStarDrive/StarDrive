using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game
{
    /// <summary>
    /// A tech entry which is bound to a specific Faction's tech tree.
    /// It references a `Technology` template via `UID`
    /// </summary>
    [StarDataType]
    public sealed class TechEntry
    {
        [StarData] public string UID;
        [StarData] public float Progress;
        [StarData] public bool Discovered;

        // true if Tech has been unlocked at least once (multi-level tech Level > 0)
        [StarData] public bool Unlocked;
        [StarData] public int Level;
        [StarData] public string AcquiredFrom { private get; set; }
        // TODO: fix variable name, but keep savegame compatibility
        [StarData] public bool shipDesignsCanuseThis = true;
        [StarData] public Array<string> WasAcquiredFrom = new();
        [StarData] public UniverseState Universe;
        [StarData] public Empire Owner; // empire that owns this TechEntry

        public Technology Tech { get; private set; }
        public Array<string> ConqueredSource = new();

        readonly Map<TechnologyType, float> TechTypeCostLookAhead = new(DefaultLookAhead);

        static readonly Map<TechnologyType, float> DefaultLookAhead = new(GetTechnologyTypes().Select(t => (t,0f)));
        static TechnologyType[] GetTechnologyTypes() => (TechnologyType[])Enum.GetValues(typeof(TechnologyType));
        
        public static readonly TechEntry None = new("", null, null);

        public override string ToString()
            => $"TechEntry {UID}: Discovered={Discovered} Unlocked={Unlocked}({Level}/{MaxLevel}) CanResearch={CanBeResearched}";

        /// Can this tech be researched further?
        /// For multi-level techs this will be true when Level > 0 and until Level >= MaxLevel
        public bool CanBeResearched => !Unlocked || (0 < Level && Level < MaxLevel);
        public bool MultiLevelComplete => Level == MaxLevel && MaxLevel > 1 || MaxLevel == 1;

        public float PercentResearched => Progress / TechCost;
        public bool IsRoot => Tech.IsRootNode;
        public TechnologyType TechnologyType => Tech.TechnologyTypes.First();
        public bool IsTechnologyType(TechnologyType type) => Tech.TechnologyTypes.Contains(type);
        public int MaxLevel => Tech.MaxLevel;
        public float MultiLevelCostMultiplier => Tech.MultiLevelCostMultiplier;
        public bool IsMultiLevel => Tech.MaxLevel > 1;
        public bool Locked => !Unlocked;

        // cached tech cost, because this is called a lot
        float StoredTechCost;
        int StoredTechCostLevel = -1;

        // the Actual Tech Cost
        public float TechCost
        {
            get
            {
                if (StoredTechCostLevel != Level)
                {
                    StoredTechCostLevel = Level;
                    
                    float cost = Tech.ActualCost(Universe);
                    float techLevel = (float)Math.Max(1, Math.Pow(MultiLevelCostMultiplier, Level.UpperBound(MaxLevel-1)));
                    int rootTech = Tech.IsRootNode ? 100 : 0;
                    StoredTechCost = cost * (techLevel + rootTech);
                }
                return StoredTechCost;
            }
        }

        [StarDataConstructor]
        public TechEntry(UniverseState us, Empire owner)
        {
            Universe = us;
            Owner = owner;
        }

        public TechEntry(string uid, UniverseState us, Empire owner) : this(us, owner)
        {
            UID = uid;
            Universe = us;
            ResolveTech();
        }

        public TechEntry(TechEntry clone, Empire newOwner)
        {
            UID = clone.UID;
            Progress = clone.Progress;
            Discovered = clone.Discovered;
            Unlocked = clone.Unlocked;
            Level = clone.Level;
            AcquiredFrom = clone.AcquiredFrom;
            shipDesignsCanuseThis = clone.shipDesignsCanuseThis;
            WasAcquiredFrom = new(clone.WasAcquiredFrom);
            Tech = clone.Tech;
            ConqueredSource = new(clone.ConqueredSource);
            TechTypeCostLookAhead = new(clone.TechTypeCostLookAhead);
            Universe = clone.Universe;
            Owner = newOwner;
        }

        [StarDataDeserialized]
        public void OnDeserialized(UniverseState us)
        {
            // Owner will be checked during Empire.OnDeserialized -> InitEmpireUnlocks()
            Universe = us;
            ResolveTech();
        }

        public void ResolveTech()
        {
            if (UID.NotEmpty())
            {
                if (ResourceManager.TryGetTech(UID, out Technology tech))
                    Tech = tech;
                else
                    Log.Warning($"Failed to resolve Technology UID={UID}");
            }

            // always fall back to a Dummy Technology
            Tech ??= Technology.Dummy;
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
            float techCost = TechCost * modifier;
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

        public bool IsMilitary()
        {
            return ContainsShipTech() || IsTechnologyType(TechnologyType.GroundCombat);
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

        public bool IsPrimaryShipTech() => IsTechTypeShipTech(Tech.TechnologyTypes.First());

        bool IsTechTypeShipTech(TechnologyType techType)
        {
            switch (techType)
            {
                case TechnologyType.ShipHull:
                case TechnologyType.ShipDefense:
                case TechnologyType.ShipWeapons:
                case TechnologyType.ShipGeneral:
                    return true;
                default:
                    return false;
            }
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
            foreach (TechnologyType techType in DefaultLookAhead.Keys)
            {
                TechTypeCostLookAhead[techType] = LookAheadCost(techType, empire);
            }
        }

        public bool SpiedFrom(Empire them) => WasAcquiredFrom.Contains(them.data.Traits.ShipType);
        
        /// <summary>Checks if list  contains restricted trade type</summary>
        static bool AllowRacialTrade(Empire us, Empire them)
        {
            return us.GetRelations(them).AllowRacialTrade();
        }

        public bool TheyCanUseThis(Empire us, Empire them)
        {
            var theirTech        = them.GetTechEntry(UID);
            bool theyCanUnlockIt = !theirTech.Unlocked && (them.HavePreReq(UID) ||
                                                           AllowRacialTrade(us, them) && theirTech.IsRoot);
            bool notHasContent   = AllowRacialTrade(us, them) && ContentRestrictedTo(us) 
                                                  && !theirTech.SpiedFrom(us) 
                                                  && (theirTech.IsRoot || them.HavePreReq(UID));
            return theyCanUnlockIt || notHasContent;
        }

        // checks if any of the bonuses are exclusively restricted for this Empire
        bool ContentRestrictedTo(Empire empire)
        {
            bool hulls     = Tech.HullsUnlocked.Any(item => Technology.IsTypeRestrictedTo(item.ShipType, empire));
            bool buildings = Tech.BuildingsUnlocked.Any(item => Technology.IsTypeRestrictedTo(item.Type, empire));
            bool troops    = Tech.TroopsUnlocked.Any(item => Technology.IsTypeRestrictedTo(item.Type, empire));
            bool modules   = Tech.ModulesUnlocked.Any(item => Technology.IsTypeRestrictedTo(item.Type, empire));
            bool bonus     = Tech.BonusUnlocked.Any(item => Technology.IsTypeRestrictedTo(item.Type, empire));
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
            if (Technology.IsTypeUnlockableBy(unlockType, empire))
                return true;
            if (WasAcquiredFrom.Contains(unlockType))
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
                if (CheckSource(unlockedHull.ShipType, empire))
                    hullList.Add(unlockedHull.Name);
            }
            return hullList;
        }

        public Array<string> UnLockHulls(Empire us, Empire them)
        {
            var hullList = GetUnLockableHulls(them);
            if (hullList.IsEmpty) return null;

            foreach(var hull in hullList)
                us.UnlockEmpireHull(hull, UID);

            us.UpdateShipsWeCanBuild(hullList);
            return hullList;
        }

        public Array<Technology.UnlockedMod> GetUnlockableModules(Empire empire)
        {
            var modulesUnlocked = new Array<Technology.UnlockedMod>();
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in Tech.ModulesUnlocked)
            {
                if (CheckSource(unlockedMod.Type, empire))
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
                us.UnlockEmpireShipModule(unlockedMod.ModuleUID);
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
                            empire.Universe.Notifications.AddNotify(triggeredEvent, triggeredEvent.CustomMessage);
                        else
                            empire.Universe.Notifications.AddNotify(triggeredEvent);
                        triggered = true;
                    }
                }
            }
            return triggered;
        }

        public void ForceFullyResearched()
        {
            Progress = TechCost;
            Unlocked = true;
        }

        public void ForceNeedsFullResearch()
        {
            Progress = 0;
            Unlocked = false;
        }

        /// <returns>True if tech was unlocked by this method</returns>
        bool TrySetUnlocked()
        {
            if (this == None)
                return false; // cannot unlock None technology

            // NOTE: Multi-Level techs; this feature is not very well implemented
            //       There is some basic refactoring here, but we have no idea who
            //       wrote this feature in the first place
            if (Tech.MaxLevel > 1)
            {
                if (Level >= Tech.MaxLevel)
                    return false;

                // it must be marked as unlocked, otherwise ship designs that rely on it
                // cannot be built
                Unlocked = true;
                ++Level;

                if (Level == Tech.MaxLevel)
                    Progress = TechCost;
                else
                    Progress = 0;

                return true;
            }

            if (Unlocked)
                return false;

            ForceFullyResearched();
            return true;
        }

        // If this tech is Unlocked, it will be Locked
        // Any side-effects to the empire must be removed with other means
        public void ResetUnlockedTech()
        {
            if (Locked || IsRoot) // ignore Locked or Root nodes
                return;

            Progress = 0;
            Unlocked = false;
            Level = 0;
        }

        public bool Unlock(Empire us)
        {
            if (!SetDiscovered(us))
                return false;

            bool wasUnlocked = TrySetUnlocked();
            UnlockTechContentOnly(us, us, bonusUnlock: wasUnlocked);

            foreach (Empire e in us.Universe.MajorEmpires)
            {
                if (e.data.AbsorbedBy == us.data.Traits.Name)
                    UnlockTechContentOnly(us, e, false);
            }

            TriggerAnyEvents(us);
            return true;
        }

        public void UnlockWithBonus(Empire us, Empire them, bool unlockBonus)
        {
            bool wasUnlocked = TrySetUnlocked();
            UnlockTechContentOnly(us, them, bonusUnlock: wasUnlocked && unlockBonus);
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
        /// be careful bonuses currently stack and should only be unlocked once. 
        /// Remove from Queue is relevant only for Multi Level Tech unlocks from save
        /// </summary>
        /// <param name="us">the empire the tech is being unlocked for</param>
        /// <param name="them">the racial specific tech of a different Empire</param>
        /// <param name="bonusUnlock">bonuses will unlock too</param>
        /// <param name="removeFromQueue"></param>
        void UnlockTechContentOnly(Empire us, Empire them, bool bonusUnlock = true, bool removeFromQueue = true)
        {
            DoRevealedTechs(us);
            if (bonusUnlock)
            {
                UnlockBonus(us);
            }
            UnlockModules(us, them);
            UnlockTroops(us, them);
            UnLockHulls(us, them);
            UnlockBuildings(us, them);

            // Finally, remove this tech from our ResearchQueue
            if (removeFromQueue)
                us.Research.RemoveFromQueue(UID);
        }

        public bool UnlockFromSpy(Empire us, Empire them)
        {
            if (!Unlocked && (us.Random.RollDice(50) || !ContentRestrictedTo(them)))
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
            bool removeFromQueue = true;
            if (!contentOnly)
            {
                // FB: Do not remove from queue if a multi level tech has some levels researched, Gee.
                if (IsMultiLevel && !MultiLevelComplete)
                    removeFromQueue = false;
                else
                    Progress = TechCost;

                Unlocked = true;
            }

            UnlockTechContentOnly(us, them, bonusUnlock: false, removeFromQueue: removeFromQueue);
        }

        void UnlockAcquiredContent(Empire us)
        {
            if (Locked)
                return;

            foreach (Empire e in us.Universe.MajorEmpires)
            {
                if (e.data.AbsorbedBy == us.data.Traits.Name)
                    UnlockFromSave(us, e, true);
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
            // FB: Don't unlock bonus only tech, leave them so modders can unlock specific techs later.
            if (!bonusUnlock && Tech.UnlocksBonusOnly())
                return;

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
            if (this == None) // this is a None technology, and should never unlock
                return true;
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
            
            if (Tech.Secret && !Tech.IsRootNode)
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

        void DiscoverToRoot(Empire empire)
        {
            TechEntry tech = this;
            while (!tech.Tech.IsRootNode)
            {
                var rootTech = tech.GetPreReq(empire);
                if (rootTech == null)
                    break;

                rootTech.SetDiscovered(empire, false);
                tech = rootTech;
                if (tech.Tech.IsRootNode && tech.Discovered)
                {
                    rootTech.ForceFullyResearched();
                    return;
                }
            }
        }

        public TechEntry GetPreReq(Empire empire)
        {
            foreach (TechEntry entry in empire.TechEntries)
            {
                foreach (Technology.LeadsToTech leadsToTech in entry.Tech.LeadsTo)
                {
                    if (leadsToTech.UID == UID)
                    {
                        if (entry.Tech.IsRootNode || !entry.IsHidden(empire))
                            return entry;
                        return entry.GetPreReq(empire);
                    }
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
            if (Tech == null || Tech == Technology.Dummy)
                return null;

            if (Discovered)
                return this;
            foreach (Technology child in Tech.Children)
            {
                if (empire.TryGetTechEntry(child.UID, out TechEntry discovered))
                {
                    discovered = discovered.FindNextDiscoveredTech(empire);
                    if (discovered != null)
                        return discovered;
                }
                else
                {
                    Log.Error($"Empire {empire.Name} missing TechEntry for Tech={child.UID}");
                }
            }
            return null;
        }

        public TechEntry[] Children
        {
            get
            {
                if (Tech == null || Tech == Technology.Dummy || this == None)
                    return Empty<TechEntry>.Array;

                var children = new TechEntry[Tech.Children.Length];
                bool gotNulls = false;
                for (int i = 0; i < Tech.Children.Length; ++i)
                {
                    Technology t = Tech.Children[i];
                    if (Owner.TryGetTechEntry(t.UID, out TechEntry entry))
                    {
                        children[i] = entry;
                    }
                    else
                    {
                        Log.Error($"TechEntry.Children: Failed to find Tech: ({t.UID})");
                        gotNulls = true;
                    }
                }
                return gotNulls ? children.Filter(e => e != null) : children;
            }
        }

        public bool CanWeUnlockBonus(string unlockType, Empire empire)
        {
            // guaranteed that we can unlock this by our own
            if (Technology.IsTypeUnlockableBy(unlockType, empire))
                return true;
            // if technology was acquired by some nefarious means, then we get the bonus
            if (WasAcquiredFrom.Contains(unlockType))
                return true;
            return false;
        }

        public void UnlockBonus(Empire empire)
        {
            bool bonusWasUnlocked = false;

            foreach (Technology.UnlockedBonus unlockedBonus in Tech.BonusUnlocked)
            {
                if (CanWeUnlockBonus(unlockedBonus.Type, empire))
                {
                    bonusWasUnlocked = true;
                    if (unlockedBonus.Tags.Count <= 0)
                    {
                        UnlockOtherBonuses(empire, unlockedBonus);
                    }
                    else
                    {
                        foreach (string tag in unlockedBonus.Tags)
                        {
                            ApplyWeaponTagBonusToEmpire(empire.data, tag, unlockedBonus);
                        }
                    }
                }
            }

            if (bonusWasUnlocked) // update ship stats if a bonus was unlocked:
            {
                empire.TriggerAllShipStatusUpdate();
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
                    {
                        data.BonusFighterLevels += (int)unlockedBonus.Bonus;
                        data.RoleLevels[(int)RoleName.corvette - 1] += (int)unlockedBonus.Bonus;
                        data.RoleLevels[(int)RoleName.drone - 1]    += (int)unlockedBonus.Bonus;

                        var roles = new[]{ RoleName.fighter, RoleName.corvette, RoleName.drone};

                        empire.IncreaseEmpireShipRoleLevel(roles, (int)unlockedBonus.Bonus);
                        break;
                    }
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
                case "Module HP Bonus": data.Traits.ModHpModifier += unlockedBonus.Bonus; break;
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
                case "Sensor Range Bonus":
                    data.SensorModifier += unlockedBonus.Bonus;
                    empire.UpdateNetPlanetIncomes();
                    empire.ForceUpdateSensorRadiuses = true;
                    break;
                case "Privatization": data.Privatization = true; break;
                // Doctor                           : Adding an actually configurable amount of civilian maintenance modification; privatisation is hardcoded at 50% but have left it in for back-compatibility.
                case "Civilian Maintenance": data.CivMaintMod -= unlockedBonus.Bonus; break;
                case "Armor Piercing":
                case "Armor Phasing": data.ArmorPiercingBonus += (int)unlockedBonus.Bonus; break;
                case "Kulrathi Might":  data.Traits.ModHpModifier += unlockedBonus.Bonus; break;
                case "Subspace Inhibition": data.Inhibitors = true; break;
                // Added by McShooterz              : New Bonuses
                case "Production Bonus": data.Traits.ProductionMod += unlockedBonus.Bonus; break;
                case "Construction Bonus": data.Traits.ShipCostMod -= unlockedBonus.Bonus; break;
                case "Consumption Bonus": data.Traits.ConsumptionModifier -= unlockedBonus.Bonus; break;
                case "Tax Bonus": data.Traits.TaxMod += unlockedBonus.Bonus; break;
                case "Repair Bonus": data.Traits.RepairMod += unlockedBonus.Bonus; break;
                case "Maintenance Bonus": data.Traits.MaintMod -= unlockedBonus.Bonus; break;
                case "Ship Maintenance Bonus": data.Traits.ShipMaintMultiplier -= unlockedBonus.Bonus; break;
                case "Construction Ships Build Rate": data.Traits.ConstructionRateMultiplier += unlockedBonus.Bonus; break;
                case "Builder Ships Build Rate": data.Traits.BuilderShipConstructionMultiplier += unlockedBonus.Bonus; break;
                case "Power Flow Bonus": data.PowerFlowMod += unlockedBonus.Bonus; break;
                case "Shield Power Bonus": data.ShieldPowerMod += unlockedBonus.Bonus; break;
                case "Ship Experience Bonus": data.ExperienceMod += unlockedBonus.Bonus; break;
                case "Kinetic Shield Penetration Chance Bonus": data.ShieldPenBonusChance += unlockedBonus.Bonus; break;
                case "Tax Goods": data.Traits.TaxGoods = true; break;
                case "Smart Missiles": data.Traits.SmartMissiles = true; break; // Fb - Smart re target
                case "Minimum Troop Level": data.MinimumTroopLevel += (int)unlockedBonus.Bonus; break; // FB Minimum Troop Level Bonus
                case "Bomb Environment Damage Bonus": data.BombEnvironmentDamageMultiplier += unlockedBonus.Bonus; break;
                case "Terraforming": data.Traits.TerraformingLevel = ((int)unlockedBonus.Bonus).Clamped(0,3); break;
                case "Counter Enemy Planet Inhibition Bonus": 
                    data.Traits.EnemyPlanetInhibitionPercentCounter = (data.Traits.EnemyPlanetInhibitionPercentCounter
                                                                       + unlockedBonus.Bonus).Clamped(0, 0.75f); break;
                case "ShipRoleLevels":
                {
                        var roles = new Array<RoleName>();
                        foreach (var tag in unlockedBonus.Tags)
                        {
                            bool foundRole = Enum.TryParse(tag, out RoleName roleName);
                            if (foundRole)
                            {
                                roles.Add(roleName);
                                data.RoleLevels[(int)roleName -1] += (int)unlockedBonus.Bonus;
                            }
                        }
                        empire.IncreaseEmpireShipRoleLevel(roles.ToArray(), (int)unlockedBonus.Bonus);
                        break;
                }
            }

            EmpireHullBonuses.RefreshBonuses(empire); // RedFox: This will refresh all empire module stats

            // post refresh actions
            switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
            {
                case "Hull Strengthening":
                case "Module HP Bonus": empire.ApplyModuleHealthTechBonus(unlockedBonus.Bonus); break;
            }
        }
    }
}

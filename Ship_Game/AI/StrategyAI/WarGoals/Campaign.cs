using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    /// <summary>
    /// This is a intermediate class that sits between the very generic GoalBase class and more detailed usage.
    /// all items requiring saving should be put here.
    /// temporary items should be restored in the restore from save process.
    /// This point of this class is to ease the use and hide the campaign internals. 
    /// </summary>
    public class Campaign : GoalBase
    {
        public enum CampaignType
        {
            None,
            Capture,
            CaptureBorder,
            CaptureAll,
            Defense,
            Destroy,
            Blockade,
            Feint,
            Attrition,
            SystemDefense

        }
        
        public CampaignType Type;
        protected War OwnerWar;
        protected Empire Them;
        public Array<Guid> SystemGuids             = new Array<Guid>();
        protected Array<SolarSystem> TargetSystems = new Array<SolarSystem>();
        public Array<Guid> ShipGuids               = new Array<Guid>();
        protected Array<Ship> TargetShips          = new Array<Ship>();
        public Array<Guid> PlanetGuids             = new Array<Guid>();
        protected Array<Planet> TargetPlanets      = new Array<Planet>();
        public AO RallyAO;
        public bool IsCoreCampaign                 = true;
        protected Theater OwnerTheater;
        protected WarTasks Tasks => Owner.GetEmpireAI().WarTasks;
        public Campaign() { }
        public int GetPriority()      => OwnerTheater.Priority;
        public bool WarMatch(War war) => war == OwnerWar;
        public WarType GetWarType()   => OwnerWar?.WarType ?? WarType.EmpireDefense;
        public WarState GetWarState() => OwnerWar?.GetWarScoreState() ?? WarState.NotApplicable;
        public Theater GetTheater()   => OwnerTheater;
        public War GetWar()           => OwnerWar;

        public bool IsRecoveredCorrectlyFromSave() => OwnerTheater != null;
        /// <summary>
        /// this is a restore from save constructor. 
        /// the expanded class is saved as a campaign. So when restored  from save the expanded class must be recreated.  
        /// this constructor takes the generic campaign and uses that and other data fields to recreated the expanded class. 
        /// </summary>
        public Campaign(Campaign campaign, Theater theater) : base(campaign, campaign.Type.ToString())
        {
            Type           = campaign.Type;
            OwnerWar       = theater.GetWar();
            Owner          = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            OwnerName      = OwnerWar.UsName;
            Them           = EmpireManager.GetEmpireByName(OwnerWar.ThemName);
            if (Owner == null || Them == null)
                Log.Warning("no empires");
            UID            = $"{Type} - {ID}";
            SystemGuids    = campaign.SystemGuids;
            ShipGuids      = campaign.ShipGuids;
            PlanetGuids    = campaign.PlanetGuids;
            RallyAO        = campaign.RallyAO ?? theater.RallyAO;
            IsCoreCampaign = campaign.IsCoreCampaign;
            OwnerTheater   = theater;
            RestoreFromSave(theater);
        }

        /// <summary>
        /// This is the normal constructor. Ideally this class is never manually created.
        /// Instead the instance creator should be used. 
        /// </summary>
        protected Campaign(CampaignType campaignType, Theater theater) : base(campaignType.ToString())
        {
            Type         = campaignType;
            OwnerWar     = theater.GetWar();
            Owner        = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            Them         = EmpireManager.GetEmpireByName(OwnerWar.ThemName);
            UID          = campaignType.ToString();
            OwnerTheater = theater;
        }

        /// <summary>
        /// Standard campaign instance creator.  
        /// </summary>
        public static Campaign CreateInstance(CampaignType campaignType, Theater theater)
        {
            switch (campaignType)
            {
                case CampaignType.Capture:       return new Capture(campaignType, theater);
                case CampaignType.CaptureBorder: return new CaptureBorderPlanets(campaignType, theater);
                case CampaignType.CaptureAll:    return new CaptureAllPlanets(campaignType, theater);
                case CampaignType.Defense:       return new Defense(campaignType, theater);
                case CampaignType.SystemDefense: return new SystemDefense(campaignType, theater);
            }
            return null;
        }

        /// <summary>
        /// campaign instance creator used by the save restore process. This will restore the campaign to the expanded class.
        /// </summary>
        public static Campaign CreateInstanceFromSave(Campaign campaign, Theater theater)
        {
            switch (campaign.Type)
            {
                case CampaignType.None:
                    campaign.RestoreFromSave(theater);
                    return campaign;
                case CampaignType.Capture:
                    return new Capture(campaign, theater);
                case CampaignType.CaptureBorder:
                    return new CaptureBorderPlanets(campaign, theater);
                case CampaignType.CaptureAll:
                    return new CaptureAllPlanets(campaign, theater);
                case CampaignType.Defense:
                    return new Defense(campaign, theater);
                case CampaignType.Destroy:
                    break;
                case CampaignType.Blockade:
                    break;
                case CampaignType.Feint:
                    break;
                case CampaignType.Attrition:
                    break;
                case CampaignType.SystemDefense:
                    return new SystemDefense(campaign, theater);
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return null;
        }

        public override string UID { get; }

        public override GoalStep Evaluate()
        {
            RallyAO   = RallyAO ?? OwnerTheater.RallyAO;
            GoalStep state = GoalStep.TryAgain;
            if (OwnerWar.WarTheaters.ActiveTheaters.Contains(OwnerTheater))
                state = base.Evaluate();
            return state;
        }

        protected override void RemoveThisGoal() => OwnerTheater.RemoveCampaign(this);

        /// <summary>
        /// Adds a target system to the list and also adds the guid for save and save restore. 
        /// </summary>
        public void AddTargetSystem(SolarSystem system)
        {
            SystemGuids.AddUnique(system.guid);
            TargetSystems.AddUnique(system);
        }

        /// <summary>
        /// Adds a list of systems to the attack targets."
        /// </summary>
        public void AddTargetSystems(IList<SolarSystem> systems)
        {
            for (int i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                AddTargetSystem(system);
            }
        }

        /// <summary>
        /// Specific parts that need to be restored. 
        /// </summary>
        void RestoreFromSave(Theater war)
        {
            TargetSystems = SolarSystem.GetSolarSystemsFromGuids(SystemGuids);
            TargetShips   = Ship.GetShipsFromGuids(ShipGuids);
            TargetPlanets = Planet.GetPlanetsFromGuids(PlanetGuids);
            OwnerWar      = war.GetWar();
            Them          = EmpireManager.GetEmpireByName(war.GetWar().ThemName);
            RestoreFromSave(war.GetWar().UsName);
        }

        /// <summary>
        /// base restore.
        /// </summary>
        protected override void RestoreFromSave(string ownerName)
        {
            Owner = EmpireManager.GetEmpireByName(ownerName);
        }

        protected virtual GoalStep SetupRallyPoint()
        {
            if (OwnerTheater.RallyAO == null) return GoalStep.TryAgain;
            RallyAO = OwnerTheater.RallyAO;
            return GoalStep.GoToNextStep;
        }

        protected void UpdateTargetSystemList() => UpdateTargetSystemList(TargetSystems);

        protected void UpdateTargetSystemList(Array<SolarSystem> solarSystems)
        {
            for (int x = 0; x < solarSystems.Count; x++)
            {
                var s = solarSystems[x];
                if (!s.HasPlanetsOwnedBy(Them))
                    solarSystems.RemoveAt(x);
            }
        }

        protected bool HaveConqueredTarget(SolarSystem system) => !system.OwnerList.Contains(Them);

        protected GoalStep CreateTargetSystemList(Array<SolarSystem> targets)
        {
            // attempt to sort targets by systems in AO by war type.
            // the create a winnable targets list evaluating each system 

            var winnableTargets = new Array<SolarSystem>();
            
            foreach (var system in targets)
            {
                if (HaveConqueredTarget(system)
                    || EmpireManager.Remnants.GetFleetsDict().Values.ToArray()
                        .Any(f => f.FleetTask?.TargetPlanet?.ParentSystem == system))
                {
                    continue;
                }

                float defense  = Owner.GetEmpireAI().ThreatMatrix.PingNetRadarStr(system.Position, system.Radius, Owner);
                if (defense  < Owner.EmpireShipLists.CurrentUseableStrength)
                {
                    winnableTargets.Add(system);
                    Owner.EmpireShipLists.CurrentUseableStrength -= defense;
                }
            }

            //var currentTarget = targets.Find(s=>Tasks.IsAlreadyAssaultingSystem(s));

            if (winnableTargets.NotEmpty) // && currentTarget == null)
            {
                TargetSystems = new Array<SolarSystem>();
                SystemGuids   = new Array<Guid>();
                var systems    = SortSystemsByWarType(winnableTargets);
                foreach (var target in systems)
                {
                    AddTargetSystem(target);    
                }
            }
            //else if (currentTarget != null)
            //    AddTargetSystem(currentTarget);

            return GoalStep.GoToNextStep;
        }

        protected void AttackSystemsInList(int fleetsPerTarget = 1) => AttackSystemsInList(TargetSystems, this, fleetsPerTarget);

        protected void AttackSystemsInList(Array<SolarSystem> currentTargets,Campaign campaign, int fleetsPerTarget = 1)
        {
            if (currentTargets.Count == 0) return;

            int priority = OwnerTheater.Priority;

            foreach (SolarSystem system in currentTargets)
            {
                int contestedSystemMod = system.HasPlanetsOwnedBy(Them) ? 2 : 0;

                Tasks.StandardAssault(system, priority - contestedSystemMod, campaign,  fleetsPerTarget);
                if (OwnerWar.WarType != WarType.EmpireDefense)
                    priority += 4;
            }
        }

        protected virtual GoalStep AttackSystems()
        {
            if (Owner.GetOwnedSystems().Count == 0)                   return GoalStep.RestartGoal;
            AttackSystemsInList();
            return GoalStep.GoToNextStep;
        }

        SolarSystem[] SortSystemsByWarType(Array<SolarSystem> systems)
        {
            switch (OwnerWar.WarType)
            {
                default:
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:
                case WarType.SkirmishWar:    return systems.Sorted(s => SystemValueByDistance(s, OwnerWar.WarType));
                case WarType.DefensiveWar:
                case WarType.EmpireDefense:  return systems.Sorted(s => s.Position.SqDist(RallyAO.Center));
                case WarType.BorderConflict:
                    var sharedSystems = systems.Filter(s => s.HasPlanetsOwnedBy(Them) && s.HasPlanetsOwnedBy(Owner));
                    return sharedSystems.Length > 0
                        ? sharedSystems.Sorted(s => s.Position.SqDist(RallyAO.Center))
                        : systems.Sorted(s => s.Position.SqDist(RallyAO.Center));
            }

        }

        float SystemValueByDistance(SolarSystem system, WarType warType)
        {
            float systemValue;
            float distance = system.Position.Distance(RallyAO.Center);
            switch (warType)
            {
                default:
                case WarType.ImperialistWar: systemValue = SystemPotentialValueToUs(system);       break;
                case WarType.GenocidalWar:   systemValue = SystemPotentialValueToThem(system);     break;
                case WarType.SkirmishWar:    systemValue = TheirOwnedPlanetPotentialValue(system); break;
            }

            float netValue = systemValue / distance.LowerBound(1);
            return netValue;
        }

        float SystemPotentialValueToUs(SolarSystem system)   => system.PlanetList.Sum(p => p.ColonyPotentialValue(Owner));
        float SystemPotentialValueToThem(SolarSystem system) => system.PlanetList.Sum(p => p.ColonyPotentialValue(Them));
        float TheirOwnedPlanetPotentialValue(SolarSystem system)
        {
            var planetList = system.PlanetList.Filter(p => p.Owner == Them);
            return planetList.Length > 0 ? planetList.Sum(p => p.ColonyValue) : 0;
        }
    }
}
 
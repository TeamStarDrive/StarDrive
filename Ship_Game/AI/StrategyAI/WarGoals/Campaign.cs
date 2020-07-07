using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
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

        public Campaign() { }

        /// <summary>
        /// this is a restore from save constructor. 
        /// the expanded class is saved as a campaign. So when restored  from save the expanded class must be recreated.  
        /// this constructor takes the generic campaign and uses that and other data fields to recreated the expanded class. 
        /// </summary>
        public Campaign(Campaign campaign, War war) : base(campaign)
        {
            Type           = campaign.Type;
            OwnerWar       = war;
            Owner          = EmpireManager.GetEmpireByName(war.UsName);
            Them           = EmpireManager.GetEmpireByName(war.ThemName);
            UID            = $"{Type.ToString()} - {ID}";
            SystemGuids    = campaign.SystemGuids;
            RallyAO        = campaign.RallyAO;
            IsCoreCampaign = campaign.IsCoreCampaign;
            RestoreFromSave(war);
        }

        /// <summary>
        /// This is the normal constructor. Ideally this class is never manually created.
        /// Instead the instance creator should be used. 
        /// </summary>
        protected Campaign(CampaignType campaignType, War war) : base(campaignType.ToString())
        {
            Type     = campaignType;
            OwnerWar = war;
            Owner    = EmpireManager.GetEmpireByName(war.UsName);
            Them     = EmpireManager.GetEmpireByName(war.ThemName);
            UID      = campaignType.ToString();
        }

        /// <summary>
        /// Standard campaign instance creator.  
        /// </summary>
        public static Campaign CreateInstance(CampaignType campaignType, War war)
        {
            switch (campaignType)
            {
                case CampaignType.Capture:       return new Capture(campaignType, war);
                case CampaignType.CaptureBorder: return new CaptureBorderPlanets(campaignType, war);
                case CampaignType.CaptureAll:    return new CaptureAllPlanets(campaignType, war);
                case CampaignType.Defense:       return new Defense(campaignType, war);
            }
            return null;
        }

        /// <summary>
        /// campaign instance creator used by the save restore process. This will restore the campaign to the expanded class.
        /// </summary>
        public static Campaign CreateInstanceFromSave(Campaign campaign, War war)
        {
            switch (campaign.Type)
            {
                case CampaignType.None:
                    campaign.RestoreFromSave(war);
                    return campaign;
                case CampaignType.Capture:
                    return new Capture(campaign, war);
                case CampaignType.CaptureBorder:
                    return new CaptureBorderPlanets(campaign, war);
                case CampaignType.CaptureAll:
                    return new CaptureAllPlanets(campaign, war);
                case CampaignType.Defense:
                    return new Defense(campaign, war);
                case CampaignType.Destroy:
                    break;
                case CampaignType.Blockade:
                    break;
                case CampaignType.Feint:
                    break;
                case CampaignType.Attrition:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return null;
        }

        public override string UID { get; }
        
        protected override void RemoveThisGoal() => OwnerWar.RemoveCampaign(this);

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
        void RestoreFromSave(War war)
        {
            TargetSystems = SolarSystem.GetSolarSystemsFromGuids(SystemGuids);
            OwnerWar      = war;
            Them          = EmpireManager.GetEmpireByName(war.ThemName);
            RestoreFromSave(war.UsName);
        }

        /// <summary>
        /// base restore.
        /// </summary>
        protected override void RestoreFromSave(string ownerName)
        {
            Owner = EmpireManager.GetEmpireByName(ownerName);
        }

        /// <summary>
        /// Creates an empire AO for the rally position.
        /// The AO will allow the AO process to protect and maintain the AO rally.
        /// Should be called after attack targets are assigned.
        /// </summary>
        protected virtual GoalStep SetupRallyPoint()
        {
            float closestRallyPoint          = float.MaxValue;
            SolarSystem rallySystem          = null;
            Planet rallyPlanet               = null;

            if (TargetSystems.IsEmpty) return GoalStep.RestartGoal;

            foreach (var system in TargetSystems)
            {
                Planet nearestSafeRallyPoint = Owner.FindNearestSafeRallyPoint(system.Position);
                float systemDistanceToRally  = nearestSafeRallyPoint.ParentSystem.Position.Distance(system.Position);

                if (closestRallyPoint > systemDistanceToRally)
                {
                    rallyPlanet            = nearestSafeRallyPoint;
                    closestRallyPoint      = systemDistanceToRally;
                    rallySystem            = nearestSafeRallyPoint.ParentSystem;
                }
            }
            if (rallySystem != null && rallyPlanet.Owner == Owner)
            {
                if (!Owner.GetAOCoreWorlds().Contains(rallyPlanet) && RallyAO?.CoreWorld?.ParentSystem != rallyPlanet.ParentSystem)
                {
                    AO newAO = new AO(rallyPlanet, Owner.GetProjectorRadius(rallyPlanet));
                    Owner.GetEmpireAI().AreasOfOperations.Add(newAO);
                    RallyAO = newAO;
                }
                if (RallyAO == null)
                    RallyAO = Owner.GetAOFromCoreWorld(rallyPlanet);
                return GoalStep.GoToNextStep;
            }
            return GoalStep.TryAgain;
        }

        protected void UpdateTargetSystemList() => UpdateTargetSystemList(TargetSystems);

        protected void UpdateTargetSystemList(Array<SolarSystem> solarSystems)
        {
            for (int x = 0; x < solarSystems.Count; x++)
            {
                var s = solarSystems[x];
                if (s.OwnerList.Contains(Them))
                    continue;
                solarSystems.RemoveAt(x);
            }
        }

        protected float PercentageCleared()
        {
            return (float)TargetSystems.Sum(s => s.OwnerList.Contains(Them) ? 0 : 1) / TargetSystems.Count.LowerBound(1);
        }

        protected bool HaveConqueredTargets()
        {
            foreach (var system in TargetSystems)
            {
                if (!HaveConqueredTarget(system))
                    return false;
            }
            return true;
        }

        protected bool HaveConqueredTarget(SolarSystem system) => !system.OwnerList.Contains(Them);

        protected GoalStep CreateTargetList(Array<SolarSystem> targets)
        {
            Vector2 empireCenter     = Owner.GetWeightedCenter();
            var fleets               = Owner.AllFleetsReady();
            float strength           = fleets.AccumulatedStrength * Owner.GetWarOffensiveRatio();
            var winnableTarget       = new Array<SolarSystem>();
            var allTargets           = new Array<SolarSystem>();
            float allTargetsStrength = strength;

            // these loops are not cheap but the frequency of the calcs should be pretty low.
            float minDistanceToThem  = Owner.MinDistanceToNearestOwnedSystemIn(Them.GetOwnedSystems(), out SolarSystem nearestSystem);
            float numberOfTargets    = targets.Count.LowerBound(1);
            float averageImportance  = targets.Sum(s => s.WarValueTo(Owner)) / numberOfTargets;
            
            // goal here is to sort the targets by closeness and value.
            // we will emphasize above average war targets and nearby planets. 
            foreach (var system in targets.Sorted(s =>
            {
                float warValueRatio   = s.WarValueTo(Owner) / averageImportance;
                // high value targets will be worth more
                warValueRatio        *= warValueRatio < 1 ? 1 : 2;
                float distance        = s.Position.SqDist(empireCenter);
                float rangeRatio      = minDistanceToThem / distance;
                // sorted sorts ascend so we multiply by negative 1 to make the high value targets effectively smaller.
                return (warValueRatio - rangeRatio) * -1f;
            }))
            {
                if (HaveConqueredTarget(system)) continue;

                float defense = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(system.Position, Owner.GetProjectorRadius(), Owner);
                float rangeMod = system.Position.Distance(empireCenter) / minDistanceToThem;
                if (defense * rangeMod < strength)
                {
                    winnableTarget.Add(system);
                    strength -= defense;
                    allTargetsStrength -= defense;
                    continue;
                }

                if (allTargetsStrength > 0)
                {
                    allTargets.Add(system);
                    allTargetsStrength -= defense;
                }

                if (strength < 0) break;
            }

            if (winnableTarget.NotEmpty)
            {
                for (int i = 0; i < winnableTarget.Count * Owner.GetWarOffensiveRatio(); i++)
                {
                    var system = winnableTarget[i];
                    AddTargetSystem(system);
                }
            }
            else if (allTargets.NotEmpty)
            {
                AddTargetSystem(allTargets.FindClosestTo(empireCenter));
            }
            else
            {
                return GoalStep.GoalComplete;
            }
            return GoalStep.GoToNextStep;
        }

        protected void AttackSystemsInList(int fleetsPerTarget = 1) => AttackSystemsInList(TargetSystems, fleetsPerTarget);

        protected void AttackSystemsInList(Array<SolarSystem> currentTargets, int fleetsPerTarget = 1)
        {
            int priorityMod = 0;

            var tasks = new WarTasks(Owner, Them);

            foreach (var system in currentTargets)
            {
                tasks.StandardAssault(system, OwnerWar.Priority() + priorityMod, fleetsPerTarget);
                priorityMod++;
            }
            Owner.GetEmpireAI().AddPendingTasks(tasks.GetNewTasks());
        }

        protected virtual GoalStep AttackSystems()
        {
            if (Owner.GetOwnedSystems().Count == 0) return GoalStep.GoalFailed;
            if (HaveConqueredTargets() || PercentageCleared() > 0.5f) return GoalStep.RestartGoal;
            if (RallyAO == null || RallyAO.CoreWorld?.Owner != Owner) return GoalStep.RestartGoal;

            AttackSystemsInList();
            return GoalStep.TryAgain;
        }
    }
}
 
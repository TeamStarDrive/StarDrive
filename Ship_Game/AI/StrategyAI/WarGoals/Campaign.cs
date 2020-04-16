using System;
using System.Collections.Generic;
using Ship_Game.AI.Tasks;

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
        public Array<Guid> SystemGuids = new Array<Guid>();
        protected Array<SolarSystem> TargetSystems = new Array<SolarSystem>();
        public AO RallyAO;

        public Campaign() { }

        /// <summary>
        /// this is a restore from save constructor. 
        /// the expanded class is saved as a campaign. So when restored  from save the expanded class must be recreated.  
        /// this constructor takes the generic campaign and uses that and other data fields to recreated the expanded class. 
        /// </summary>
        public Campaign(Campaign campaign, War war) : base(campaign)
        {
            Type          = campaign.Type;
            OwnerWar      = war;
            Owner         = EmpireManager.GetEmpireByName(war.UsName);
            Them          = EmpireManager.GetEmpireByName(war.ThemName);
            UID           = $"{Type.ToString()} - {ID}";
            SystemGuids   = campaign.SystemGuids;
            RallyAO       = campaign.RallyAO;
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
            UID = campaignType.ToString();
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
                    break;
                case CampaignType.CaptureBorder:
                    return new CaptureBorderPlanets(campaign, war);
                    break;
                case CampaignType.CaptureAll:
                    return new CaptureAllPlanets(campaign, war);
                    break;
                case CampaignType.Defense:
                    return new Defense(campaign, war);
                    break;
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
        /// <param name="system"></param>
        public void AddTargetSystem(SolarSystem system)
        {
            SystemGuids.AddUnique(system.guid);
            TargetSystems.AddUnique(system);
        }

        /// <summary>
        /// Adds a list of systems to the attack targets."
        /// </summary>
        /// <param name="systems"></param>
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
        /// <param name="war"></param>
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
        protected GoalStep SetupRallyPoint()
        {
            float rallyDistanceToTargets     = float.MaxValue;
            SolarSystem rallySystem = null;
            Planet rallyPlanet      = null;
            float aoDistanceToTargets        = float.MaxValue;

            foreach (var system in TargetSystems)
            {
                Planet rally = Owner.FindNearestSafeRallyPoint(system.Position);
                float distance = rally.ParentSystem.Position.Distance(system.Position);

                if (rallyDistanceToTargets > distance)
                {
                    rallyPlanet   = rally;
                    rallyDistanceToTargets = distance;
                    rallySystem   = rally.ParentSystem;
                    aoDistanceToTargets    = Owner.GetEmpireAI().DistanceToClosestAO(system.Position);
                }
            }
            if (rallySystem == null || rallyPlanet.Owner != Owner) return GoalStep.TryAgain;

            float arbitraryMinDistance = Owner.GetProjectorRadius(rallyPlanet);

            if (RallyAO?.CoreWorld != rallyPlanet)
            {
                if (aoDistanceToTargets - rallyDistanceToTargets > arbitraryMinDistance * 5)
                {
                    AO newAO = new AO(rallyPlanet, Owner.GetProjectorRadius(rallyPlanet));
                    Owner.GetEmpireAI().AreasOfOperations.Add(newAO);
                    RallyAO = newAO;
                }
            }
            return GoalStep.GoToNextStep;
        }
    }
}
 
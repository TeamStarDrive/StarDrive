using System;
using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class Theater 
    {
        public Guid TheaterGuid = Guid.NewGuid();
        public AO TheaterAO;
        public int Priority;
        War OwnerWar;
        protected Array<SolarSystem> Systems => TheaterAO.GetAoSystems();
        Ship[] Ships;
        ThreatMatrix.Pin[] Pins;
        Empire Us;
        public Array<Campaign.CampaignType> CampaignsWanted;
        public Array<Campaign> Campaigns;
        TheatersOfWar Theaters;
        bool Initialized;
        bool Remove = false;
        public War GetWar() => OwnerWar;

        Empire Them => OwnerWar.Them;

        public Theater() { }
        public Theater (AO ao, TheatersOfWar theaters)
        {
            TheaterAO       = ao;
            Theaters        = theaters;
            OwnerWar        = Theaters.GetWar();
            Ships           = new Ship[0];
            Us              = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            CampaignsWanted = new Array<Campaign.CampaignType>();
            Campaigns       = new Array<Campaign>();
        }
        public Theater (AO ao, Array<Campaign.CampaignType> campaignTypes, TheatersOfWar theaters) : this(ao,theaters)
        {
            CampaignsWanted = campaignTypes;
        }

        public void Evaluate()
        {
            if (Remove)
            {
                for (int i = Campaigns.Count - 1; i >= 0; i--)
                {
                    var campaign = Campaigns[i];
                    campaign.PurgeTasks();
                }
                Campaigns.Clear();
                return;
            }

            TheaterAO.Update();

            if (CampaignsWanted.Contains(Campaign.CampaignType.SystemDefense))
            {
                Pins    = Us.GetEmpireAI().ThreatMatrix.GetEnemyPinsInAO(TheaterAO, Us).ToArray();
            }

            CreateWantedCampaigns();
            RemoveUnwantedCampaigns();

            for (int i = 0; i < Campaigns.Count; i++)
            {
                var campaign = Campaigns[i];
                campaign.Evaluate();
            }
        }

        public void MarkForRemoval()
        {
            Remove = true;
        }

        public void RestoreFromSave(TheatersOfWar war)
        {
            OwnerWar = war.GetWar();
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            TheaterAO.InitFromSave(Us);
            for (int i = 0; i < Campaigns.Count; i++)
            {
                var campaign = Campaigns[i];
                Campaigns[i] = Campaign.CreateInstanceFromSave(campaign, this);
            }

            CreateWantedCampaigns();
        }

        public Array<SolarSystem> GetSystems()                          => Systems;
        public Ship[] GetShips()                                   => Ships;
        public ThreatMatrix.Pin[] GetPins() => Pins;

        /// <summary>
        /// Adds the type of the campaign. Campaign will be created at next update.
        /// </summary>
        public void AddCampaignType(Campaign.CampaignType type)    => CampaignsWanted.AddUnique(type);

        /// <summary>
        /// Removes the type of the campaign. Campaign will be removed at next update.
        /// </summary>
        public bool RemoveCampaignType(Campaign.CampaignType type) => CampaignsWanted.Remove(type);

        void RemoveUnwantedCampaigns()
        {
            for (int i = Campaigns.Count - 1; i >= 0; i--)
            {
                var campaign = Campaigns[i];
                if (CampaignsWanted.Contains(campaign.Type)) continue;
                RemoveCampaign(campaign);
            }
        }

        public Array<Campaign.CampaignType> GetWantedCampaignsCopy()   => CampaignsWanted.Clone();

        public bool RemoveCampaign(Campaign campaign) => Campaigns.Remove(campaign);

        public bool HasCampaignType(Campaign.CampaignType type) => Campaigns.Any(c => c.Type == type);
        
        bool AddCampaign(Campaign.CampaignType campaignType)
        {
            if (HasCampaignType(campaignType)) return false;
            Campaigns.Add(Campaign.CreateInstance(campaignType, this));
            return true;
        }

        void CreateWantedCampaigns()
        {
            for (int i = 0; i < CampaignsWanted.Count; i++)
            {
                var campaignType = CampaignsWanted[i];
                AddCampaign(campaignType);
            }
        }

        public DebugTextBlock DebugText(DebugTextBlock debug, string pad, string pad2)
        {
            for (int i = 0; i < Campaigns.Count; i++)
            {
                var campaign = Campaigns[i];
                debug.AddLine($"{pad}Campaign : {campaign}");
                if (campaign.RallyAO?.CoreWorld != null)
                    debug.AddLine($"{pad2}Rally : {campaign.RallyAO.CoreWorld}");
                debug.AddLine($"{pad2}Targets : {campaign.SystemGuids.Count}");
                var systems = SolarSystem.GetSolarSystemsFromGuids(campaign.SystemGuids);
                foreach (var system in systems)
                {
                    if (system.OwnerList.Contains(Them))
                    {
                        debug.AddLine($"{pad2}Target : {system}");
                    }
                    else
                    {
                        debug.AddLine($"{pad2}Taken  : {system}");
                    }
                }
                debug.AddLine($"{pad2}Step : {campaign.StepName}");
            }
            return debug;
        }

    }
}
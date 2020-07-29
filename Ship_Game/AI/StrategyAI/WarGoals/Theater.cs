﻿using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
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
        bool Initialized;
        bool Remove = false;
        public AO RallyAO { get; private set; }
        public War GetWar() => OwnerWar;

        [XmlIgnore] [JsonIgnore] public float WarValue => TheaterAO.GetWarAttackValueOfSystemsInAOTo(Us);

        Empire Them => OwnerWar.Them;

        public Theater() { }
        public Theater (AO ao, TheatersOfWar theaters)
        {
            TheaterAO       = ao;
            OwnerWar        = theaters.GetWar();
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
            SetupRallyPoint();

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

        public void SetTheaterPriority(float baseDistance, Vector2 position)
        {
            // empire defense
            if (Us==Them)
            {
                Priority = 2;
                return;
            }
            // trying to figure out how to incorporate planet value but all it does is attack homeworlds right now. 
            // so remarking that code and just going by distance. 
            //float totalWarValue          = OwnerWar.WarTheaters.WarValue.LowerBound(1); 
            //float theaterValue           = WarValue.Clamped(1, totalWarValue);
            float distanceFromPosition   = TheaterAO.Center.Distance(position);
            float distanceMod            =  distanceFromPosition / baseDistance;
            //float warValueMod            = theaterValue / (totalWarValue * distanceMod);
            
            Priority                     = (int)(OwnerWar.Priority().LowerBound(1) * distanceMod).UpperBound(9);
        }

        public DebugTextBlock DebugText(DebugTextBlock debug, string pad, string pad2)
        {
            debug.AddLine($"{pad}TheaterPri : {Priority}");
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

        float FindTheirNearestSystemToPoint(Vector2 point)
        {
            return Them.FindNearestOwnedSystemTo(point)?.Position.SqDist(point) ?? 1000000;
        }

        /// <summary>
        /// Creates an empire AO for the rally position.
        /// The AO will allow the AO process to protect and maintain the AO rally.
        /// Should be called after attack targets are assigned.
        /// </summary>
        protected void SetupRallyPoint()
        {
            //if (TheaterAO.GetPlanets().Length == 0) return;

            float closestRallyPoint          = float.MaxValue;
            SolarSystem rallySystem          = null;
            Planet rallyPlanet               = null;
            var aoManager = Us.GetEmpireAI().OffensiveForcePoolManager;

            rallyPlanet = Us.FindNearestRallyPoint(TheaterAO.Center) ?? Us.Capital;


            // createEmpire AO
            if (rallyPlanet.Owner == Us)
            {
                if (!aoManager.IsPlanetCoreWorld(rallyPlanet) && RallyAO?.CoreWorld?.ParentSystem != rallyPlanet.ParentSystem)
                {
                    var newAO = aoManager.CreateAO(rallyPlanet, Us.GetProjectorRadius(rallyPlanet));
                    RallyAO = newAO;
                }
                if (RallyAO == null)
                    RallyAO = aoManager.GetAOContaining(rallyPlanet);       
            }
        }
    }
}
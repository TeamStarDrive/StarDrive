using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class War
    {
        public WarType WarType;
        public float OurStartingStrength;
        public float TheirStartingStrength;
        public float OurStartingGroundStrength;
        public int OurStartingColonies;
        public float TheirStartingGroundStrength;
        public float StrengthKilled;
        public float StrengthLost;
        public float TroopsKilled;
        public float TroopsLost;
        public int ColoniesWon;
        public int ColoniesLost;
        public Array<string> AlliesCalled = new Array<string>();
        public Array<Guid> ContestedSystemsGUIDs = new Array<Guid>();
        public float TurnsAtWar;
        public float EndStarDate;
        public float StartDate;
        private Empire Us;
        public string UsName;
        public string ThemName;
        public Array<Campaign> Campaigns;
        public bool Initialized;

        [JsonIgnore][XmlIgnore]
        public Empire Them { get; private set; }
        public int StartingNumContestedSystems;
        [JsonIgnore][XmlIgnore]
        public SolarSystem[] ContestedSystems { get; private set; }
        [JsonIgnore][XmlIgnore]
        public float LostColonyPercent => ColoniesLost / (OurStartingColonies + 0.01f + ColoniesWon);
        [JsonIgnore][XmlIgnore]
        public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.LowerBound(0.01f);
        [JsonIgnore][XmlIgnore]
        public float SpaceWarKd => StrengthKilled / (StrengthLost + 0.01f);

        int ContestedSystemCount => ContestedSystems.Count(s => s.OwnerList.Contains(Them));

        readonly Array<SolarSystem> HistoricLostSystems = new Array<SolarSystem>();
        public IReadOnlyList<SolarSystem> GetHistoricLostSystems() => HistoricLostSystems;
        Relationship OurRelationToThem;

        WarTasks Tasks;

        public void RemoveCampaign(Campaign campaign) => Campaigns.Remove(campaign);

        public int Priority()
        {
            if (Them.isFaction) return 1;
            var warState = GetBorderConflictState();
            return 10 - (int)warState;
        }

        public War()
        {
        }

        public War(Empire us, Empire them, float starDate, WarType warType)
        {
            StartDate = starDate;
            Us        = us;
            Them      = them;
            UsName    = us.data.Traits.Name;
            ThemName  = them.data.Traits.Name;
            WarType   = warType;

            OurStartingStrength         = us.CurrentMilitaryStrength;
            OurStartingGroundStrength   = us.CurrentTroopStrength;
            OurStartingColonies         = us.GetPlanets().Count;
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems            = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            ContestedSystemsGUIDs       = FindContestedSystemGUIDs();
            StartingNumContestedSystems = ContestedSystemsGUIDs.Count;
            OurRelationToThem           = us.GetRelations(them);
            PopulateHistoricLostSystems();
        }

        public static War CreateInstance(Empire owner, Empire target, WarType warType)
        {
            var war = new War(owner, target, Empire.Universe.StarDate, warType);
            war.CreateCampaigns();
            return war;
        }

        void CreateCampaigns()
        {
            Campaigns = new Array<Campaign>();
            CreateCoreCampaigns();

            var defense = Campaign.CreateInstance(Campaign.CampaignType.Defense, this);
            Campaigns.Add(defense);
            

            Initialized = true;
        }

        void CreateCoreCampaigns()
        {
            switch (WarType)
            {
                case WarType.BorderConflict:
                    {
                        var campaign = Campaign.CreateInstance(Campaign.CampaignType.CaptureBorder, this);
                        campaign.AddTargetSystems(GetTheirBorderSystems());
                        Campaigns.Add(campaign);
                        break;
                    }
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:
                    {
                        var campaign = Campaign.CreateInstance(Campaign.CampaignType.CaptureAll, this);
                        Campaigns.Add(campaign);
                        break;
                    }
                case WarType.DefensiveWar:
                    {
                        var campaign = Campaign.CreateInstance(Campaign.CampaignType.Defense, this);
                        campaign.AddTargetSystems(Us.GetOwnedSystems().Filter(s =>
                        {
                            if (s.OwnerList.Contains(Them))
                                return Us.GetEmpireAI().IsInOurAOs(s.Position);
                            return false;
                        }));
                        campaign.IsCoreCampaign = true;
                        Campaigns.Add(campaign);
                        break;
                    }
                case WarType.SkirmishWar:
                    {
                        var campaign = Campaign.CreateInstance(Campaign.CampaignType.Capture, this);
                        campaign.AddTargetSystems(GetTheirBorderSystems());
                        campaign.AddTargetSystems(GetTheirNearSystems());
                        Campaigns.Add(campaign);
                        break;
                    }
            }
        }

        void PopulateHistoricLostSystems()
        {
            foreach (var lostSystem in OurRelationToThem.GetPlanetsLostFromWars())
            {
                if (lostSystem.OwnerList.Contains(Them))
                    HistoricLostSystems.AddUniqueRef(lostSystem);
            }
        }

        public SolarSystem[] GetTheirBorderSystems() => Them.GetBorderSystems(Us, true)
                                .Filter(s => Us.GetEmpireAI().IsInOurAOs(s.Position));
        public SolarSystem[] GetTheirNearSystems() => Them.GetBorderSystems(Us, true).ToArray();

        Array<Guid> FindContestedSystemGUIDs()
        {
            var contestedSystemGUIDs = new Array<Guid>();
            var systems = ContestedSystems;
            for (int x = 0; x < systems.Length; x++) contestedSystemGUIDs.Add(systems[x].guid);
            return contestedSystemGUIDs;
        }

        public WarState GetBorderConflictState() => GetBorderConflictState(null);

        public WarState GetBorderConflictState(Array<Planet> coloniesOffered)
        {
            if (StartingNumContestedSystems == 0)
                return GetWarScoreState();

            int contestedSystemDifference = GetContestedSystemDifferential(coloniesOffered);

            if (contestedSystemDifference == StartingNumContestedSystems)
                return WarState.EvenlyMatched;

            //winning
            if (contestedSystemDifference > 0)
            {
                if (contestedSystemDifference == StartingNumContestedSystems)
                    return WarState.Dominating;
                return WarState.WinningSlightly;
            }

            //losing
            if (contestedSystemDifference == -StartingNumContestedSystems)
                return WarState.LosingBadly;

            return WarState.LosingSlightly;
        }

        public int GetContestedSystemDifferential(Array<Planet> coloniesOffered)
        {
            // -- if we arent there
            // ++ if they arent there but we are
            int offeredCleanSystems = 0;
            if (coloniesOffered != null)
                foreach (Planet planet in coloniesOffered)
                {
                    var system = planet.ParentSystem;
                    if (!system.OwnerList.Contains(Them))
                        offeredCleanSystems++;
                }

            int reclaimedSystems = offeredCleanSystems + ContestedSystems
                                       .Count(s => !s.OwnerList.Contains(Them) && s.OwnerList.Contains(Us));

            int lostSystems = ContestedSystems
                .Count(s => !s.OwnerList.Contains(Us) && s.OwnerList.Contains(Them));

            return reclaimedSystems - lostSystems;
        }

        public WarState GetWarScoreState()
        {
            if (Them.isFaction) return WarState.NotApplicable;
            float lostColonyPercent = LostColonyPercent;
            float spaceWarKd        = SpaceWarKd;

            //They Are weaker than us
            if (TotalThreatAgainst <= 1f)
            {
                if (lostColonyPercent > 0.5f) return WarState.LosingBadly;
                if (lostColonyPercent > 0.25f) return WarState.LosingSlightly;
                if (lostColonyPercent > 0.1f) return WarState.EvenlyMatched;

                if (spaceWarKd.AlmostZero())   return WarState.ColdWar;
                if (spaceWarKd > 1.5f)         return WarState.Dominating;
                if (spaceWarKd > 0.75f)        return WarState.WinningSlightly;
                if (spaceWarKd > 0.35f)        return WarState.EvenlyMatched;
                if (spaceWarKd > 0.15)         return WarState.LosingSlightly;
                return WarState.LosingBadly;
            }

            if (lostColonyPercent > 0.5f) return WarState.LosingBadly;

            if (lostColonyPercent.AlmostZero() && spaceWarKd.AlmostZero())
                return WarState.ColdWar;

            if (spaceWarKd.AlmostZero()) return WarState.ColdWar;
            if (spaceWarKd > 1.25f)    return WarState.Dominating;
            if (spaceWarKd > 1.0f) return WarState.WinningSlightly;
            if (spaceWarKd > 0.85f) return WarState.EvenlyMatched;
            if (spaceWarKd > 0.5)   return WarState.LosingSlightly;

            return WarState.LosingBadly;
        }

        public void SetCombatants(Empire u, Empire t)
        {
            Us = u;
            Them = t;
        }

        public void RestoreFromSave(bool activeWar)
        {
            ContestedSystems = new SolarSystem[ContestedSystemsGUIDs.Count];
            for (int i = 0; i < ContestedSystemsGUIDs.Count; i++)
            {
                var guid = ContestedSystemsGUIDs[i];
                SolarSystem solarSystem = Empire.Universe.SolarSystemDict[guid];
                ContestedSystems[i] = solarSystem;
            }
            Us                = EmpireManager.GetEmpireByName(UsName);
            Them              = EmpireManager.GetEmpireByName(ThemName);
            OurRelationToThem = Us.GetRelations(Them);
            
            if (activeWar)
            {
                PopulateHistoricLostSystems();

                if (!Initialized)
                {
                    CreateCampaigns();
                }
                else
                {
                    for (int i = 0; i < Campaigns.Count; i++)
                    {
                        Campaigns[i] = Campaign.CreateInstanceFromSave(Campaigns[i], this);
                    }
                }
            }
            else
            {
                Campaigns = null;
            }
        }

        public WarState ConductWar()
        {
            for (int i = 0; i < Campaigns.Count; i++)
            {
                var campaign = Campaigns[i];
                campaign?.Evaluate();
            }

            if (Campaigns.IsEmpty)
            {
                CreateCampaigns();
            }
            else if (Campaigns.Find(c => c.IsCoreCampaign) == null)
            {
                CreateCoreCampaigns();
            }
            else
            {
                if (!Campaigns.Any(d=>d.Type == Campaign.CampaignType.Defense))
                {
                    if (ContestedSystemCount > 0)
                    {
                        var defense = Campaign.CreateInstance(Campaign.CampaignType.Defense, this);
                        Campaigns.Add(defense);
                    }
                }
            }

            return GetWarScoreState();
        }

        public void ShipWeLost(Ship target)
        {
            if (Them != target.LastDamagedBy?.GetLoyalty()) return;
            StrengthLost += target.GetStrength();
        }

        public void ShipWeKilled(Ship target)
        {
            if (Them != target.loyalty) return;
            StrengthKilled += target.GetStrength();
        }

        public void PlanetWeLost(Empire attacker, Planet colony)
        {
            if (attacker == Them)
            {
                ColoniesLost++;
            }
        }

        public void PlanetWeWon(Empire loser, Planet colony)
        {
            if (loser == Them)
            {
                ColoniesWon++;
            }
        }

        bool AddToContestedSystems(SolarSystem system)
        {
            if (ContestedSystemsGUIDs.AddUnique(system.guid))
            {
                var contested = new SolarSystem[ContestedSystemsGUIDs.Count];
                ContestedSystems.CopyTo(contested, 0);
                contested[ContestedSystemsGUIDs.Count - 1] = system;
                ContestedSystems = contested;
                return true;
            }
            return false;
        }

        public void WarDebugData(ref DebugTextBlock debug)
        {
            string pad = "     ";
            string pad2 = pad + "  *";
            debug.AddLine($"{pad}WarType:{WarType}");
            debug.AddLine($"{pad}WarState:{GetWarScoreState()}");
            debug.AddLine($"{pad}With: {ThemName}");
            debug.AddLine($"{pad}ThreatRatio = % {(int)(TotalThreatAgainst * 100)}");
            debug.AddLine($"{pad}StartDate {StartDate}");
            debug.AddLine($"{pad}Their Strength killed:{StrengthKilled}");
            debug.AddLine($"{pad}Our Strength killed:{StrengthLost}");
            debug.AddLine($"{pad}KillDeath: {(int)StrengthKilled} / {(int)StrengthLost} = % {(int)(SpaceWarKd * 100)}");
            debug.AddLine($"{pad}Colonies Lost : {ColoniesLost}");
            debug.AddLine($"{pad}Colonies Won : {ColoniesWon}");
            debug.AddLine($"{pad}Colonies Lost Percentage :% {(int)(LostColonyPercent * 100)}.00");

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

            foreach (var system in ContestedSystems)
            {
                bool ourForcesPresent = system.OwnerList.Contains(Us);
                bool theirForcesPresent = system.OwnerList.Contains(Them);
                int value = (int)system.PlanetList.Sum(p => p.ColonyBaseValue(Us));
                bool hasFleetTask = Us.GetEmpireAI().IsAssaultingSystem(system);
                debug.AddLine($"{pad2}System: {system.Name}  value:{value}  task:{hasFleetTask}");
                debug.AddLine($"{pad2}OurForcesPresent:{ourForcesPresent}  TheirForcesPresent:{theirForcesPresent}");
            }

            foreach (MilitaryTask task in Us.GetEmpireAI().GetMilitaryTasksTargeting(Them))
            {
                debug.AddLine($"{pad} Type:{task.type}");
                debug.AddLine($"{pad2} System: {task.TargetPlanet.ParentSystem.Name}");
                debug.AddLine($"{pad2} Has Fleet: {task.WhichFleet}");
                debug.AddLine($"{pad2} Fleet MinStr: {(int)task.MinimumTaskForceStrength}");
            }
        }
    }
}
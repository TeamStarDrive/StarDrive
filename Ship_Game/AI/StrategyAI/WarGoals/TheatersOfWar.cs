using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.Debug;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class TheatersOfWar 
    {
        Guid TheatersOfWarGuid = Guid.NewGuid();
        War OwnerWar;
        Empire Them => OwnerWar.Them;
        Empire Us;
        public bool Initialized;
        public Array<Theater> Theaters = new Array<Theater>();
        public War GetWar() => OwnerWar;
        int TheirSystemCount = 0;
        public TheatersOfWar (War war)
        {
            if (war is null) return;
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
        }

        public void RestoreFromSave(War war)
        {
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            foreach (var theater in Theaters)
                theater.RestoreFromSave(this);
        }

        public void Evaluate()
        {
            float totalValue = Us.GetEmpireAI().ValueOfAllWarSystems();

            for (int i = Theaters.Count - 1; i >= 0; i--)
            {
                var theater = Theaters[i];
                theater.TheaterAO.Update();
                float value = theater.TheaterAO.GetWarValueOfSystemsInAOTo(Us).Clamped(1, totalValue);
                theater.Priority = 5 - (int)((value / totalValue) * 5) + OwnerWar.Priority();
                theater.Evaluate();
            }
            // if there system count changes rebuild the AO
            int theirSystems = Them.GetOwnedSystems().Count;
            if (!Initialized || theirSystems > TheirSystemCount)
            {
                TheirSystemCount = theirSystems;
                Initialize();
            }
        }

        public void Initialize()
        {
            Initialized = true;
            CreateTheaters();
        }

        public void AddTheater(AO ao, Array<Campaign.CampaignType> campaignTypes)
        {
            
            Theater theater = new Theater(ao, this);
            Theaters.Add(theater);
            foreach (var ct in campaignTypes)
                theater.AddCampaignType(ct);
        }

        public void AddTheaters(Array<AO> aos, Array<Campaign.CampaignType> campaignTypes)
        {
            foreach (var ao in aos)
            {
                Theater theater = new Theater(ao,campaignTypes, this);
                Theaters.Add(theater);
            }
        }

        public void RemoveTheater(Theater theater)
        {
            theater.MarkForRemoval();
        }

        void CreateTheaters()
        {
            var campaignTypes = new Array<Campaign.CampaignType>();
            var aos           = new Array<AO>();
            bool replaceExistingAOs = false;
            switch (OwnerWar.WarType)
            {
                case WarType.BorderConflict:
                    {
                        campaignTypes.AddUnique(Campaign.CampaignType.CaptureBorder);
                        aos = CreateBorderAOs();
                        break;
                    }
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:
                    {
                        campaignTypes.AddUnique(Campaign.CampaignType.CaptureAll);
                        campaignTypes.AddUnique(Campaign.CampaignType.SystemDefense);
                        aos = CreateImperialisticAO();
                        break;
                    }
                case WarType.DefensiveWar:
                    aos                = CreateDefensiveAO();
                    campaignTypes.AddUnique(Campaign.CampaignType.CaptureAll);
                    replaceExistingAOs = true;
                    break;
                case WarType.SkirmishWar:
                    campaignTypes.AddUnique(Campaign.CampaignType.CaptureBorder);
                    aos                = CreateBorderAOs();
                    break;
                case WarType.EmpireDefense:
                    campaignTypes.AddUnique(Campaign.CampaignType.Defense);
                    campaignTypes.AddUnique(Campaign.CampaignType.SystemDefense);
                    aos                = CreateBorderAOs();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (replaceExistingAOs) Theaters = new Array<Theater>();

            foreach (var ao in aos)
            {
                AddTheater(ao, campaignTypes);
            }
        }

        Array<AO> CreateBorderAOs()
        {
            var systems = OwnerWar.GetTheirNearSystems();
            return CreateAOsForSystems(systems);
        }

        Array<AO> CreateDefensiveAO()
        {
            return new Array<AO> { Us.EmpireAO() };
        }

        Array<AO> CreateEmpireDefenseAO()
        {
            return new Array<AO> { Us.EmpireAO() };
        }

        Array<AO> CreateImperialisticAO()
        {
            var systems = Them.GetOwnedSystems().ToArray();
            return CreateAOsForSystems(systems);
        }

        Array<AO> CreateAOsForSystems(SolarSystem[] systems)
        {
            var theirAo     = Them.EmpireAO();
            var UsAO        = Us.GetWeightedCenter();
            float minAoSize = 600000 * 3;
            float maxAoSize = Empire.Universe.Width / 4;
            var aoSize      = (theirAo.Radius / 2).Clamped(minAoSize, maxAoSize);
            var aos         = new Array<AO>();
            foreach (var theater in Theaters)
            {
                aos.Add(theater.TheaterAO);
            }
            var newAos = new Array<AO>();
            SolarSystem system = null;
            do
            {
                systems = FilterSystemsByAO(aos, systems);
                system = systems.FindClosestTo(systems.Length, UsAO);
                if (system != null)
                {
                    var newAo = new AO(Us, system.Position, aoSize);
                    newAos.Add(newAo);
                    aos.Add(newAo);
                    newAo.SetupPlanetsInAO();
                }
            }
            while (system != null);

            return newAos;
        }

        SolarSystem[] FilterSystemsByAO(Array<AO> aos, SolarSystem[] systems)
        {
            var filterSystems = systems.Filter(s =>
            {
                bool good = true;
                foreach (var ao in aos)
                {
                    if (s.Position.InRadius(ao))
                    {
                        good = false;
                        break;
                    }
                }
                return good;
            });
            return filterSystems;
        }

        public DebugTextBlock DebugText(DebugTextBlock debug, string pad1, string pad2)
        {
            debug.AddLine($"Theaters : {Theaters.Count}");
            foreach(var theater in Theaters)
            {
                debug = theater.DebugText(debug, pad1, pad2);
            }
            return debug;
        }

        public void AddCaptureAll()
        {
            var campaignTypes = new Array<Campaign.CampaignType>();
            campaignTypes.Add(Campaign.CampaignType.CaptureAll);
            var aos = CreateImperialisticAO();

            foreach (var ao in aos)
            {
                AddTheater(ao, campaignTypes);
            }
        }
    }
}
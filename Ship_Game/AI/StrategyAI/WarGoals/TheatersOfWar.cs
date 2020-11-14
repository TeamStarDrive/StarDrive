using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
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
        [XmlIgnore][JsonIgnore] public float WarValue => Theaters.Sum(t => t.TheaterAO.GetWarAttackValueOfSystemsInAOTo(Us));
        
        public TheatersOfWar (War war)
        {
            if (war is null) return;
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
        }

        public int MinPriority() => Theaters.FindMin(t=>t.Priority)?.Priority ?? 100;

        public void RestoreFromSave(War war)
        {
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            foreach (var theater in Theaters)
                theater.RestoreFromSave(this);
        }

        public void Evaluate()
        {
            float baseDistance = TheaterClosestToItsRally();
            for (int i = Theaters.Count - 1; i >= 0; i--)
            {
                var theater = Theaters[i];
                theater.SetTheaterPriority(baseDistance);
            }

            Theater[] theaters = Theaters.SortedDescending(t=> t.Priority);
            for (int i = 0; i < theaters.Length; i++) 
                theaters[i].Evaluate();

            // if there system count changes rebuild the AO
            int theirSystems = Them.GetOwnedSystems().Count;
            if (!Initialized || theirSystems > 0 && theirSystems != TheirSystemCount)
            {
                TheirSystemCount = theirSystems;
                Initialize();
            }
        }

        float TheaterClosestToItsRally()
        {
            Vector2 defaultPosition = Us.WeightedCenter;
            float closest = float.MaxValue;
            for (int i = 0; i < Theaters.Count; i++)
            {
                var theater      = Theaters[i];
                Vector2 position = theater.RallyAO?.Center ?? defaultPosition;
                float distanceToThem = float.MaxValue;
                foreach (var p in Them.GetPlanets())
                {
                    float theaterDistance = p.Center.Distance(theater.TheaterAO.Center);
                    distanceToThem = Math.Min(distanceToThem, theaterDistance);
                }

                float distance   = theater.TheaterAO.Center.Distance(position) + distanceToThem - theater.WarValue;
                closest          = Math.Min(closest, distance);
            }

            return closest;
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
                    //{
                    //    campaignTypes.AddUnique(Campaign.CampaignType.CaptureBorder);
                    //    aos = CreateBorderAOs();
                    //    break;
                    //}
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:
                case WarType.DefensiveWar:
                    {
                        campaignTypes.AddUnique(Campaign.CampaignType.CaptureAll);
                        campaignTypes.AddUnique(Campaign.CampaignType.SystemDefense);
                        aos = CreateImperialisticAO();
                        break;
                    }
                // until defensive wars can end remarking this. 
                //case WarType.DefensiveWar:
                //    aos                = CreateBorderAOs();
                //    campaignTypes.AddUnique(Campaign.CampaignType.CaptureAll);
                //    break;
                case WarType.SkirmishWar:
                    campaignTypes.AddUnique(Campaign.CampaignType.CaptureBorder);
                    aos                = CreateBorderAOs();
                    break;
                case WarType.EmpireDefense:
                    campaignTypes.AddUnique(Campaign.CampaignType.Defense);
                    campaignTypes.AddUnique(Campaign.CampaignType.SystemDefense);
                    campaignTypes.AddUnique(Campaign.CampaignType.CaptureAll);
                    aos                = CreateDefensiveAO();
                    replaceExistingAOs = true;
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
            var UsAO        = Us.WeightedCenter;
            float minAoSize = 600000;
            float maxAoSize = (Empire.Universe.UniverseSize / 4).LowerBound(minAoSize);
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
            debug.AddLine($"WarValue : {WarValue}");
            int minPriority = Theaters.FindMin(t => t.Priority)?.Priority ?? 10;
            for (int i = 0; i < Theaters.Count; i++)
            {
                var theater = Theaters[i];
                if (theater.Priority > minPriority) continue;
                debug.AddLine($"Theater : {i} WarValue : {theater.WarValue}");
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
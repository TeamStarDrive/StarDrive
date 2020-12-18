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
        [XmlIgnore] [JsonIgnore]
        public Theater[] ActiveTheaters;
        [XmlIgnore][JsonIgnore] public float WarValue => Theaters.Sum(t => t.TheaterAO.GetWarAttackValueOfSystemsInAOTo(Us));
        
        public TheatersOfWar (War war)
        {
            if (war is null) return;
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
        }

        public int MinPriority() => Theaters.FindMinFiltered(t=>t.Active(), t=> t.Priority)?.Priority ?? 100;

        public void RestoreFromSave(War war)
        {
            OwnerWar = war;
            Us       = EmpireManager.GetEmpireByName(OwnerWar.UsName);
            foreach (var theater in Theaters)
                theater.RestoreFromSave(this);
        }

        void SetActiveTheaters()
        {
            if (OwnerWar.WarType == WarType.EmpireDefense || Theaters.Count == 1 && 
                (ActiveTheaters is null || ActiveTheaters.Length < 1 || ActiveTheaters[0] != Theaters[0]))
            {
                ActiveTheaters = Theaters.ToArray();
                return;
            }

            for (int i = 0; i < ActiveTheaters?.Length; i++)
            {
                var theater = ActiveTheaters[i];
                if (theater.TheaterAO.GetAllPlanets().Any(p => p.Owner == Them))
                    return;
            }

            if (OwnerWar.GetPriority() <= Us.GetEmpireAI().MinWarPriority)
            {
                Vector2 ourCenter = Us.WeightedCenter;
                Vector2 theirCenter = Them.WeightedCenter;

                if (OwnerWar.WarType == WarType.GenocidalWar || OwnerWar.WarType == WarType.ImperialistWar)
                {
                    theirCenter = Them.GetOwnedSystems().FindMax(s => s.WarValueTo(Us)).Position;
                }

                var possibleTheaters = Theaters.GroupByFiltered(t => (int)(t.TheaterAO.Center.Distance(ourCenter) * 0.000005f + t.TheaterAO.Center.Distance(theirCenter) * 0.000005f)
                    , t => t.Active() && t.Priority <= OwnerWar.LowestTheaterPriority);

                //Theater[] theaters = Theaters.Filter(t => t.Active() && t.Priority <= OwnerWar.LowestTheaterPriority);
                var theaters = possibleTheaters.FirstOrDefault().Value?.ToArray();

                if (theaters?.Length > 0)
                    ActiveTheaters = new Theater[1] { theaters.SortedDescending(t => t.WarValue)[0] };
                else
                    ActiveTheaters = new Theater[0];
            }
            else
                ActiveTheaters = new Theater[0];
        }

        public void Evaluate()
        {
            float baseDistance = TheaterClosestToItsRally();
            for (int i = Theaters.Count - 1; i >= 0; i--)
            {
                var theater = Theaters[i];
                theater.SetTheaterPriority(baseDistance);
            }

            SetActiveTheaters();
            // HACK! just need to fix the border planet war so that it deals with having taken the border planets. 
            if (OwnerWar.WarType == WarType.BorderConflict && ActiveTheaters.Length == 0)
            {
                OwnerWar.WarType = WarType.ImperialistWar;
                Initialized = false;
            }

            for (int i = 0; i < Theaters.Count; i++)
            {
                var theater = Theaters[i];
                Theaters[i].Evaluate();
            }

            // if there system count changes rebuild the AO
            int theirSystems = Them.GetOwnedSystems().Count;
            if (!Initialized || Theaters.Count == 0 || theirSystems > 0 && theirSystems != TheirSystemCount )
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
                    campaignTypes.AddUnique(Campaign.CampaignType.Defense);
                    campaignTypes.AddUnique(Campaign.CampaignType.SystemDefense);
                    aos                = CreateImperialisticAO();
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

            if (TheirSystemCount == 0)
            {
                var theirBases = Us.GetEmpireAI().ThreatMatrix.GetPins().Filter(p => p.Ship?.IsPlatformOrStation == true && p.Ship?.loyalty == Them);
                foreach (var theirBase in theirBases)
                {
                    var ao = new AO(Them, theirBase.Ship.Center, Us.GetProjectorRadius() * 2);
                    aos.Add(ao);
                }
            }

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
            debug.AddLine($"WarValue : {WarValue:n0}");
            
            for (int i = 0; i < (ActiveTheaters?.Length ?? 0); i++)
            {
                var theater = ActiveTheaters[i];
                debug.AddLine($"Theater : {i} WarValue : {theater.WarValue:n0}");
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
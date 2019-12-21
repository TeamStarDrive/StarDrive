using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game
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
        private Empire Them;
        public int StartingNumContestedSystems;
        [JsonIgnore][XmlIgnore]
        public SolarSystem[] ContestedSystems { get; private set; }
        [JsonIgnore][XmlIgnore]
        public float LostColonyPercent =>  ColoniesLost / (OurStartingColonies+ ColoniesWon + 0.01f);
        [JsonIgnore][XmlIgnore]
        public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.ClampMin(0.01f);
        [JsonIgnore][XmlIgnore]
        public float SpaceWarKd => StrengthKilled / (StrengthLost + 0.01f);
        
        [JsonIgnore][XmlIgnore]
        int ContestedSystemCount => ContestedSystems.Count(s => s.OwnerList.Contains(Them));

        public War()
        {
        }

        public War(Empire us, Empire them, float starDate)
        {
            StartDate = starDate;
            Us        = us;
            Them      = them;
            UsName    = us.data.Traits.Name;
            ThemName  = them.data.Traits.Name;

            OurStartingStrength         = us.CurrentMilitaryStrength;
            OurStartingGroundStrength   = us.CurrentTroopStrength;
            OurStartingColonies         = us.GetPlanets().Count;
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems            = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            ContestedSystemsGUIDs       = FindContestedSystemGUIDs();
            StartingNumContestedSystems = ContestedSystemsGUIDs.Count;
        }

        

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
            float lostColonyPercent = LostColonyPercent;
            float spaceWarKd        = SpaceWarKd;

            if (TotalThreatAgainst <= 1f)
            {
                if (ColoniesLost > 0)
                {
                    if (lostColonyPercent.AlmostZero()) return WarState.ColdWar;
                    if (lostColonyPercent > 1.25f) return WarState.Dominating;
                    if (lostColonyPercent < 0.5f) return WarState.LosingSlightly;
                    if (lostColonyPercent < 0.75f) return WarState.LosingBadly;
                }
                if (spaceWarKd.AlmostZero())   return WarState.ColdWar;
                if (spaceWarKd > 1.5f)         return WarState.Dominating;
                if (spaceWarKd > 0.75f)        return WarState.WinningSlightly;
                if (spaceWarKd > 0.35f)        return WarState.EvenlyMatched;
                if (spaceWarKd > 0.15)         return WarState.LosingSlightly;
                return WarState.LosingBadly;
            }

            if (ColoniesLost > 0)
            {
                if (lostColonyPercent < 0.5f) return WarState.LosingSlightly;
                if (lostColonyPercent < 0.75f) return WarState.LosingBadly;
            }
            if (StrengthKilled < 250f && StrengthLost < 250f && Us.GetPlanets().Count == OurStartingColonies)
                return WarState.ColdWar;
            if (spaceWarKd.AlmostZero()) return WarState.ColdWar;
            if (spaceWarKd > 2f)    return WarState.Dominating;
            if (spaceWarKd > 1.15f) return WarState.WinningSlightly;
            if (spaceWarKd > 0.85f) return WarState.EvenlyMatched;
            if (spaceWarKd > 0.5)   return WarState.LosingSlightly;

            return WarState.LosingBadly;
        }

        private float TotalThreatAgainstUs()
        {
            float totalThreatAgainstUs = 0f;
            foreach (KeyValuePair<Empire, Relationship> r in Us.AllRelations)
            {
                if (r.Key.isFaction || r.Key.data.Defeated || !r.Value.AtWar)
                {
                    continue;
                }

                totalThreatAgainstUs = totalThreatAgainstUs + r.Key.MilitaryScore;
            }

            return totalThreatAgainstUs;
        }

        public void SetCombatants(Empire u, Empire t)
        {
            Us = u;
            Them = t;
        }

        public void RestoreFromSave()
        {
            ContestedSystems = new SolarSystem[ContestedSystemsGUIDs.Count];
            for (int i = 0; i < ContestedSystemsGUIDs.Count; i++)
            {
                var guid = ContestedSystemsGUIDs[i];
                SolarSystem solarSystem = Empire.Universe.SolarSystemDict[guid];
                ContestedSystems[i] = solarSystem;
            }
        }

        public WarState ConductWar()
        {
            switch(WarType)
            {
                case WarType.BorderConflict: return ConductBorderConflict();
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:
                case WarType.DefensiveWar:
                case WarType.SkirmishWar:
                    return ConductSkirmishWar();
            }
            return WarState.ColdWar;
        }

        private WarState ConductBorderConflict()
        {
            if (ContestedSystemCount == 0) return ConductSkirmishWar();

            var sortedContestSystems = ContestedSystems;
            sortedContestSystems.Sort(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            StandardAssault(sortedContestSystems, int.MaxValue);

            return GetWarScoreState();
        }

        WarState ConductSkirmishWar()
        {
            var targetSystemsInAO = Us.GetBorderSystems(Them).Filter(s => Us.GetEmpireAI().IsInOurAOs(s.Position));
            var targetSystemsNotInAO = Us.GetBorderSystems(Them).Filter(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            targetSystemsNotInAO.Sorted(s => Us.GetEmpireAI().DistanceToClosestAO(s.Position));

            StandardAssault(targetSystemsInAO, int.MaxValue);
            StandardAssault(targetSystemsNotInAO, 3);
            return GetWarScoreState();
        }

        void StandardAssault(SolarSystem[] systemsToAttack, int maxInvasions)
        {
            var threatMatrix = Us.GetEmpireAI().ThreatMatrix;
            foreach (var system in systemsToAttack)
            {
                if (!IsAlreadyAssaultingSystem(system))
                {
                    var theirPlanets = system.PlanetList.Filter(p => p.Owner == Them);
                    foreach (var planet in theirPlanets.Sorted(p => p.ColonyBaseValue(Us)))
                    {
                        float AORadius = 3500f;
                        float strWanted = threatMatrix.PingRadarStr(planet.Center, AORadius, Us);
                        Us.GetEmpireAI().TaskList.Add(new MilitaryTask(planet, Us, strWanted));
                    }
                }
            }
        }

        bool IsAlreadyAssaultingSystem(SolarSystem system)
        {
            using (Us.GetEmpireAI().TaskList.AcquireReadLock())
                return Us.GetEmpireAI().TaskList.Any(task => task.type == MilitaryTask.TaskType.AssaultPlanet && 
                                                             task.TargetPlanet?.ParentSystem == system);
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
                ColoniesLost++;
        }

        public void PlanetWeWon(Empire loser, Planet colony)
        {
            if (loser == Them)
                ColoniesWon++;
        }

    }
}
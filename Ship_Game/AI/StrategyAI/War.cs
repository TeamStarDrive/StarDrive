using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
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
        public float LostColonyPercent => ColoniesLost / (OurStartingColonies + 0.01f + ColoniesWon);
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
                case WarType.DefensiveWar:
                case WarType.BorderConflict: return ConductBorderConflictWar();
                
                case WarType.SkirmishWar:    return ConductSkirmishWar();

                case WarType.ImperialistWar:
                case WarType.GenocidalWar:   return ConductImperialisticWar();
            }
            return WarState.ColdWar;
        }

        private WarState AttackContestedSystems()
        {
            if (ContestedSystemCount == 0) return WarState.NotApplicable;

            var sortedContestSystems = ContestedSystems;
            sortedContestSystems.Sort(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            StandardAssault(sortedContestSystems);

            return GetWarScoreState();
        }

        WarState ConductBorderConflictWar()
        {
            var warState = AttackContestedSystems();
            if (warState == WarState.NotApplicable)
                warState = ConductSkirmishWar();
            return warState;
        }

        WarState ConductSkirmishWar()
        {
            var targetSystemsInAO = Us.GetBorderSystems(Them).Filter(s => Us.GetEmpireAI().IsInOurAOs(s.Position));
            var targetSystemsNotInAO = Us.GetBorderSystems(Them).Filter(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            targetSystemsNotInAO.Sorted(s => Us.GetEmpireAI().DistanceToClosestAO(s.Position));

            
            if (!StandardAssault(targetSystemsInAO) && !StandardAssault(targetSystemsNotInAO))
                ConductImperialisticWar();
            return GetWarScoreState();
        }

        WarState ConductImperialisticWar()
        {
            AttackContestedSystems();
            var targetSystems = Them.GetOwnedSystems().SortedDescending(s =>
                {
                    return s.PlanetList.Sum(p => p.ColonyBaseValue(Us));
                });
            StandardAssault(targetSystems);
            return GetWarScoreState();
        }

        bool StandardAssault(SolarSystem[] systemsToAttack)
        {
            var threatMatrix = Us.GetEmpireAI().ThreatMatrix;
            bool targetFound = false;
            foreach (var system in systemsToAttack)
            {
                if (!IsAlreadyAssaultingSystem(system))
                {
                    var theirPlanets = system.PlanetList.Filter(p => p.Owner == Them);
                    foreach (var planet in theirPlanets.Sorted(p => p.ColonyBaseValue(Us)))
                    {
                        float AORadius = 3500f;
                        float strWanted = threatMatrix.PingRadarStr(planet.Center, AORadius, Us);
                        strWanted += planet.TotalGeodeticOffense;
                        Us.GetEmpireAI().TaskList.Add(new MilitaryTask(planet, Us, strWanted));
                        targetFound = true;
                    }
                }
            }
            return targetFound;
        }

        bool IsAlreadyAssaultingSystem(SolarSystem system)
        {
            using (Us.GetEmpireAI().TaskList.AcquireReadLock())
                return Us.GetEmpireAI().TaskList.Any(task => task.type == MilitaryTask.TaskType.AssaultPlanet && 
                                                             task.TargetPlanet?.ParentSystem == system);
        }
        public MilitaryTask[] TasksForThisWar()
        {
            using (Us.GetEmpireAI().TaskList.AcquireReadLock())
                return Us.GetEmpireAI().TaskList.Filter(task =>
                    task.TargetPlanet?.Owner == Them);
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
                if(ContestedSystemsGUIDs.AddUnique(colony.ParentSystem.guid))
                {
                    var contested = new SolarSystem[ContestedSystemsGUIDs.Count];
                    ContestedSystems.CopyTo(contested, 0);
                    contested[ContestedSystemsGUIDs.Count -1] = colony.ParentSystem;
                    ContestedSystems = contested;
                }
            }
        }

        public void PlanetWeWon(Empire loser, Planet colony)
        {
            if (loser == Them)
                ColoniesWon++;
        }

        public DebugTextBlock WarDebugData(DebugTextBlock debug)
        {
            string pad = "     ";
            string pad2 = pad + "  *";
            debug.AddLine($"{pad}War Type:{WarType}");
            debug.AddLine($"{pad}War State:{GetWarScoreState()}");
            debug.AddLine($"{pad}With : {ThemName}");
            debug.AddLine($"{pad}Threat Ratio = % {(int)(TotalThreatAgainst * 100)}");
            debug.AddLine($"{pad}StartDate {StartDate}");
            debug.AddLine($"{pad}Their Strength killed:{StrengthKilled}");
            debug.AddLine($"{pad}Our Strength killed:{StrengthLost}");
            debug.AddLine($"{pad}KillDeath: {(int)StrengthKilled} / {(int)StrengthLost} = % {(int)(SpaceWarKd * 100)}");
            debug.AddLine($"{pad}Colonies Lost : {ColoniesLost}");
            debug.AddLine($"{pad}Colonies Won : {ColoniesWon}");
            debug.AddLine($"{pad}Colonies Lost Percentage :% {(int)(LostColonyPercent * 100)}.00");

            foreach (var system in ContestedSystems)
            {
                bool weAreThere = system.OwnerList.Contains(Us);
                bool theyAreThere = system.OwnerList.Contains(Them);
                int value = (int)system.PlanetList.Sum(p => p.ColonyBaseValue(Us));
                bool hasFleetTask = IsAlreadyAssaultingSystem(system);
                debug.AddLine($"{pad2}System : {system.Name} : value :{value}");
                debug.AddLine($"{pad2}We are there :{weAreThere} They are there {theyAreThere}");
            }
            foreach (var fleet in TasksForThisWar())
            {
                debug.AddLine($"{pad} Type:{fleet.type}");
                debug.AddLine($"{pad2} System: {fleet.TargetPlanet.ParentSystem.Name}");
                debug.AddLine($"{pad2} Has Fleet: {fleet.WhichFleet}");
                debug.AddLine($"{pad2} Fleet MinStr: {(int)fleet.MinimumTaskForceStrength}");
                
            }
            return debug;
        }

    }
}
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI;
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
        [JsonIgnore][XmlIgnore]
        public Empire Them { get; private set; }
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
            switch (WarType)
            {
                case WarType.DefensiveWar:
                case WarType.BorderConflict: Us.GetEmpireAI().AddPendingTasks(ConductBorderConflictWar()); break;
                case WarType.SkirmishWar:    Us.GetEmpireAI().AddPendingTasks(ConductSkirmishWar()); break;
                case WarType.ImperialistWar:
                case WarType.GenocidalWar:   Us.GetEmpireAI().AddPendingTasks(ConductImperialisticWar()); break;
            }
            return GetWarScoreState();
        }

        Array<MilitaryTask> AttackContestedSystems()
        {
            if (ContestedSystemCount == 0)
                return new Array<MilitaryTask>();

            var sortedContestSystems = ContestedSystems;
            sortedContestSystems.Sort(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            return StandardAssault(sortedContestSystems);
        }

        Array<MilitaryTask> ConductBorderConflictWar()
        {
            var tasks = new Array<MilitaryTask>();
            tasks.AddRange(AttackContestedSystems());
            return tasks.IsEmpty ? ConductSkirmishWar() : tasks;
        }

        Array<MilitaryTask> ConductSkirmishWar()
        {
            var targetSystemsInAO = Us.GetBorderSystems(Them).Filter(s => Us.GetEmpireAI().IsInOurAOs(s.Position));
            var targetSystemsNotInAO = Us.GetBorderSystems(Them).Filter(s => !Us.GetEmpireAI().IsInOurAOs(s.Position));
            targetSystemsNotInAO.Sorted(s => Us.GetEmpireAI().DistanceToClosestAO(s.Position));

            var tasks = StandardAssault(targetSystemsInAO);
            tasks.AddRange(StandardAssault(targetSystemsNotInAO));
            if (tasks.IsEmpty)
                tasks.AddRange(ConductImperialisticWar());
            return tasks;
        }

        Array<MilitaryTask> ConductImperialisticWar()
        {
            var tasks = AttackContestedSystems();

            tasks.AddRange(StandardAssault(Them.GetOwnedSystems().ToArray()));
            return tasks;
        }

        Array<MilitaryTask> StandardAssault(SolarSystem[] systemsToAttack)
        {
            var tasks = new Array<MilitaryTask>();
            systemsToAttack.Sort(s => s.PlanetList.Sum(p => p.ColonyBaseValue(Us)));

            foreach (var system in systemsToAttack)
            {
                foreach(var planet in system.PlanetList.SortedDescending(p=> p.ColonyBaseValue(Us)))
                {
                    if (planet.Owner == Them)
                    {
                        if (!IsAlreadyAssaultingPlanet(planet))
                        {
                            tasks.Add(new MilitaryTask(planet, Us));
                        }
                    }
                }
            }

            return tasks;
        }

        bool IsAlreadyAssaultingSystem(SolarSystem system)
        {
            return Us.GetEmpireAI().IsAlreadyAssaultingSystem(system);
        }

        bool IsAlreadyAssaultingPlanet(Planet planetToAssault)
        {
            return Us.GetEmpireAI().IsAlreadyAssaultingPlanet(planetToAssault);
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

            foreach (var system in ContestedSystems)
            {
                bool ourForcesPresent = system.OwnerList.Contains(Us);
                bool theirForcesPresent = system.OwnerList.Contains(Them);
                int value = (int)system.PlanetList.Sum(p => p.ColonyBaseValue(Us));
                bool hasFleetTask = IsAlreadyAssaultingSystem(system);
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
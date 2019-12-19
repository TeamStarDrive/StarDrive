using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public sealed class War
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
        SolarSystem[] ContestedSystemList;
        public SolarSystem[] ContestedSystems
        {
            get
            {
                //Compatibility hack remove this at first save version change.
                Log.Assert(SavedGame.SaveGameVersion == 4
                    , $"This prop is not needed as a prop once the save version changes.");
                if (ContestedSystemList.Length == 0 && ContestedSystemsGUIDs.NotEmpty)
                {
                    ContestedSystemList = new SolarSystem[ContestedSystemList.Length];
                    for (int i = 0; i < ContestedSystemsGUIDs.Count; i++)
                    {
                        var guid = ContestedSystemsGUIDs[i];
                        SolarSystem solarSystem = Empire.Universe.SolarSystemDict[guid];
                        ContestedSystemList[i] = solarSystem;
                    }
                }
                return ContestedSystemList;
            }
            set => ContestedSystemList = value;
        }
        public float LostColonyPercent => Us.GetPlanets().Count / (OurStartingColonies + 0.01f);
        public float TotalThreatAgainst => TotalThreatAgainstUs() / Us.MilitaryScore.ClampMin(0.01f);
        float SpaceWarKd => StrengthKilled / (StrengthLost + 0.01f);

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

            int reclaimedSystems = offeredCleanSystems + ContestedSystemList
                                       .Count(s => !s.OwnerList.Contains(Them) && s.OwnerList.Contains(Us));

            int lostSystems = ContestedSystemList
                .Count(s => !s.OwnerList.Contains(Us) && s.OwnerList.Contains(Them));

            return reclaimedSystems - lostSystems;
        }

        public WarState GetWarScoreState()
        {
            float lostColonyPercent = LostColonyPercent;
            float spaceWarKd        = SpaceWarKd;

            if (TotalThreatAgainst <= 1f)
            {
                if (lostColonyPercent > 1.25f) return WarState.Dominating;
                if (lostColonyPercent < 0.75f) return WarState.LosingSlightly;
                if (lostColonyPercent < 0.5f)  return WarState.LosingBadly;
                if (spaceWarKd.AlmostZero())   return WarState.Dominating;
                if (spaceWarKd > 1.5f)         return WarState.Dominating;
                if (spaceWarKd > 0.75f)        return WarState.WinningSlightly;
                if (spaceWarKd > 0.35f)        return WarState.EvenlyMatched;
                if (spaceWarKd > 0.15)         return WarState.LosingSlightly;
                return WarState.LosingBadly;
            }

            if (lostColonyPercent < 0.75f) return WarState.LosingSlightly;
            if (lostColonyPercent < 0.5f)  return WarState.LosingBadly;
            if (StrengthKilled < 250f && StrengthLost < 250f && Us.GetPlanets().Count == OurStartingColonies)
                return WarState.ColdWar;

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
    }
}
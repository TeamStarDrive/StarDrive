namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class WarScore
    {
        readonly War OwnerWar;
        int StartingNumContestedSystems => OwnerWar.StartingNumContestedSystems;
        public SolarSystem[] ContestedSystems => OwnerWar.ContestedSystems;
        Empire Them => OwnerWar.Them;
        Empire Us;
        float LostColonyPercent => OwnerWar.LostColonyPercent;
        float SpaceWarKd => OwnerWar.SpaceWarKd;
        float TotalThreatAgainst => OwnerWar.TotalThreatAgainst;


        public WarScore(War war, Empire us)
        {
            OwnerWar = war;
            Us       = us;
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
            //if (Them.isFaction) return WarState.NotApplicable;
            float lostColonyPercent = LostColonyPercent;
            float spaceWarKd = SpaceWarKd;

            //They Are weaker than us
            if (TotalThreatAgainst <= 1f)
            {
                if (lostColonyPercent > 0.5f) return WarState.LosingBadly;
                if (lostColonyPercent > 0.25f) return WarState.LosingSlightly;
                if (lostColonyPercent > 0.1f) return WarState.EvenlyMatched;

                if (spaceWarKd.AlmostZero()) return WarState.ColdWar;
                if (spaceWarKd > 1.5f) return WarState.Dominating;
                if (spaceWarKd > 0.75f) return WarState.WinningSlightly;
                if (spaceWarKd > 0.35f) return WarState.EvenlyMatched;
                if (spaceWarKd > 0.15) return WarState.LosingSlightly;
                return WarState.LosingBadly;
            }

            if (lostColonyPercent > 0.5f) return WarState.LosingBadly;

            if (lostColonyPercent.AlmostZero() && spaceWarKd.AlmostZero())
                return WarState.ColdWar;

            if (spaceWarKd.AlmostZero()) return WarState.ColdWar;
            if (spaceWarKd > 1.25f) return WarState.Dominating;
            if (spaceWarKd > 1.0f) return WarState.WinningSlightly;
            if (spaceWarKd > 0.85f) return WarState.EvenlyMatched;
            if (spaceWarKd > 0.5) return WarState.LosingSlightly;

            return WarState.LosingBadly;
        }
    }
}
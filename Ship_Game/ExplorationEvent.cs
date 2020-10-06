using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class ExplorationEvent
    {
        public string Name;

        public Array<Outcome> PotentialOutcomes;

        public void TriggerOutcome(Empire triggerer, Outcome triggeredOutcome)
        {
            triggeredOutcome.CheckOutComes(null , null, triggerer,null);
        }

        public void TriggerPlanetEvent(Planet p, Empire triggeredBy, PlanetGridSquare eventLocation,
            UniverseScreen screen)
        {
            int random = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(triggeredBy)) continue;
                random += outcome.Chance;
            }            
            random = RandomMath.InRange(random);
            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(triggeredBy)) continue;
                cursor = cursor + outcome.Chance;
                if (random > cursor) continue;
                triggeredOutcome = outcome;
                if (triggeredBy.isPlayer) outcome.AlreadyTriggered = true;
                break;
            }
            if (triggeredOutcome != null)
            {
                EventPopup popup = null;
                if (triggeredBy == EmpireManager.Player)
                    popup = new EventPopup(screen, triggeredBy, this, triggeredOutcome,false, p);
                triggeredOutcome.CheckOutComes(p, eventLocation, triggeredBy,popup);
                if (popup != null)
                {
                    screen.ScreenManager.AddScreenDeferred(popup);
                    GameAudio.PlaySfxAsync("sd_notify_alert");
                }
            }
        }

        public Outcome GetRandomOutcome()
        {
            int ranMax = PotentialOutcomes.Filter(outcome => !outcome.OnlyTriggerOnce || !outcome.AlreadyTriggered)
                .Sum(outcome => outcome.Chance);

            int random = (int)RandomMath.RandomBetween(0, ranMax);
            Outcome triggeredOutcome = new Outcome();
            int cursor = 0;

            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.OnlyTriggerOnce && outcome.AlreadyTriggered)
                    continue;
                cursor = cursor + outcome.Chance;
                if (random <= cursor)
                {
                    triggeredOutcome = outcome;
                    outcome.AlreadyTriggered = true;
                    break;
                }
            }
            return triggeredOutcome;
        }
        public void TriggerExplorationEvent(UniverseScreen screen)
        {
            Outcome triggeredOutcome = GetRandomOutcome();

            Empire empire = EmpireManager.Player;
            screen.ScreenManager.AddScreenDeferred(new EventPopup(screen, empire, this, triggeredOutcome, false));
            TriggerOutcome(empire, triggeredOutcome);
        }
    }
}
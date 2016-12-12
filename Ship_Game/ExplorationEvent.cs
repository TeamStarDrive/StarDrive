using Ship_Game.Gameplay;
using System.Collections.Generic;

namespace Ship_Game
{
    public sealed class ExplorationEvent
    {
        public string Name;

        public List<Outcome> PotentialOutcomes;

        public void TriggerOutcome(Empire triggerer, Outcome triggeredOutcome)
        {
            triggeredOutcome.CheckOutComes(null, triggeredOutcome, null, triggerer);
        }

        public void TriggerPlanetEvent(Planet p, Empire triggerer, PlanetGridSquare eventLocation, Empire playerEmpire,
            UniverseScreen screen)
        {
            int ranMax = 0;
            int ranMin = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.onlyTriggerOnce && outcome.alreadyTriggered && triggerer.isPlayer) continue;
                ranMax += outcome.Chance;
            }
            int random = (int) RandomMath.RandomBetween(ranMin, ranMax);
            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.onlyTriggerOnce && outcome.alreadyTriggered && triggerer.isPlayer) continue;
                cursor = cursor + outcome.Chance;
                if (random > cursor) continue;
                triggeredOutcome = outcome;
                if (triggerer.isPlayer) outcome.alreadyTriggered = true;
                break;
            }
            triggeredOutcome?.CheckOutComes(p, triggeredOutcome, eventLocation, triggerer);
            if (triggerer == playerEmpire)
            {
                screen.ScreenManager.AddScreen(new EventPopup(screen, playerEmpire, this, triggeredOutcome));
                AudioManager.PlayCue("sd_notify_alert");
            }
        }

        

        
    }
}
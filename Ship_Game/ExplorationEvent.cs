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
            int random = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(triggerer)) continue;
                random += outcome.Chance;
            }            
            random = RandomMath.InRange(random);
            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(triggerer)) continue;
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
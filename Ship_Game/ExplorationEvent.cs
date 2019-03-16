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

        public void TriggerPlanetEvent(Planet p, Empire trigger, PlanetGridSquare eventLocation,
            UniverseScreen screen)
        {
            int random = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(trigger)) continue;
                random += outcome.Chance;
            }            
            random = RandomMath.InRange(random);
            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in PotentialOutcomes)
            {
                if (outcome.InValidOutcome(trigger)) continue;
                cursor = cursor + outcome.Chance;
                if (random > cursor) continue;
                triggeredOutcome = outcome;
                if (trigger.isPlayer) outcome.AlreadyTriggered = true;
                break;
            }
            if (triggeredOutcome != null)
            {
                EventPopup popup = null;
                if (trigger == EmpireManager.Player)
                    popup = new EventPopup(screen, trigger, this, triggeredOutcome,false);
                triggeredOutcome.CheckOutComes(p, eventLocation, trigger,popup);
                if (popup != null)
                {
                    screen.ScreenManager.AddScreen(popup);
                    GameAudio.PlaySfxAsync("sd_notify_alert");
                }
            }
        }
    }
}
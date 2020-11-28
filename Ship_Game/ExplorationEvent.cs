using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class ExplorationEvent
    {
        public string Name;
        public int StoryStep;
        public Remnants.RemnantStory Story;
        public bool AllRemnantStories; // This event step is relevant for all Remnant Stories
        public bool TriggerWhenOnlyRemnantsLeft; // Trigger this when all empires are defeated but the Remnants

        public Array<Outcome> PotentialOutcomes;

        public void TriggerOutcome(Empire triggerer, Outcome triggeredOutcome)
        {
            triggeredOutcome.CheckOutComes(null , null, triggerer,null);
        }

        public void TriggerPlanetEvent(Planet p, Empire triggeredBy, PlanetGridSquare eventLocation,
            UniverseScreen screen)
        {
            int random = 0;
            // do not include hostile ship spawns in systems with a capital, these just mess up the game.
            var potentialOutcomes = p.ParentSystem.PlanetList.Any(planet => planet.Habitable && planet.HasCapital)
                ? PotentialOutcomes.Filter(o => o.PirateShipsToSpawn.Count == 0 && o.RemnantShipsToSpawn.Count == 0)
                : PotentialOutcomes.ToArray();


            foreach (Outcome outcome in potentialOutcomes)
            {
                if (outcome.InValidOutcome(triggeredBy)) continue;
                random += outcome.Chance;
            }            
            random = RandomMath.InRange(random);
            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in potentialOutcomes)
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
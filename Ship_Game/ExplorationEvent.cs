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

        public void TriggerOutcome(Empire triggeredBy, Outcome triggeredOutcome)
        {
            triggeredOutcome.CheckOutComes(null , null, triggeredBy,null);
        }

        public void TriggerPlanetEvent(Planet p, short outcomeNum, Empire triggeredBy,
            PlanetGridSquare eventLocation, UniverseScreen screen)
        {
            int cursor = 0;
            Outcome triggeredOutcome      = null;
            var filteredPotentialOutcomes = FilteredPotentialOutcomes(p);
            int sumChances                = filteredPotentialOutcomes.Sum(o => o.Chance);

            // for save compatibility - can be removed in 2022 :)
            if (outcomeNum == 0 && eventLocation.EventOnTile) 
                outcomeNum = 1; 
            // **************************************************

            if (outcomeNum > sumChances)
            {
                Log.Warning($"outcomeNum ({outcomeNum}) was larger than sum of all chance in tile for event {Name} on {p.Name}. Setting to 1\n" +
                            "This can occur if an event outcome chances were changed and a save was loaded");
                outcomeNum = 1;
            }

            foreach (Outcome outcome in FilteredPotentialOutcomes(p))
            {
                cursor += outcome.Chance;
                if (outcomeNum <= cursor) 
                {
                    triggeredOutcome = outcome;
                    break;
                }
            }

            if (triggeredOutcome != null)
            {
                EventPopup popup = null;
                if (triggeredBy == EmpireManager.Player)
                    popup = new EventPopup(screen, triggeredBy, this, triggeredOutcome, false, p);

                triggeredOutcome.CheckOutComes(p, eventLocation, triggeredBy, popup);
                if (popup != null)
                {
                    screen.ScreenManager.AddScreen(popup);
                    GameAudio.PlaySfxAsync("sd_notify_alert");
                }
            }
        }

        public short SetOutcomeNum(Planet p)
        {
            var filteredPotentialOutcomes = FilteredPotentialOutcomes(p);
            if (filteredPotentialOutcomes.Length == 0)
                return 0;

            int numTotalOutcomeChance = filteredPotentialOutcomes.Sum(o => o.Chance);
            return (short)RandomMath.RollDie(numTotalOutcomeChance); // 1 to total chance
        }

        Outcome[] FilteredPotentialOutcomes(Planet p)
        {
            // do not include hostile ship spawns in systems with a capital, these just mess up the game.
            // starting system is for checking at game creation and the planet list capital for mid game.
            // since IsStartingSystem value is not saved.
            return p.ParentSystem.IsStartingSystem || p.ParentSystem.PlanetList.Any(planet => planet.Habitable && planet.HasCapital)
                     ? PotentialOutcomes.Filter(o => o.PirateShipsToSpawn.Count == 0 && o.RemnantShipsToSpawn.Count == 0)
                     : PotentialOutcomes.ToArray();
        }

        public void DebugTriggerOutcome(Planet p, Empire triggeredBy, Outcome outcome,
                                        PlanetGridSquare eventLocation)
        {
            var popup = new EventPopup(Empire.Universe, triggeredBy, this, outcome, false, p);
            outcome.CheckOutComes(p, eventLocation, triggeredBy, popup);
            Empire.Universe.ScreenManager.AddScreen(popup);
            GameAudio.PlaySfxAsync("sd_notify_alert");
        }

        private Outcome GetRandomOutcome()
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
            screen.ScreenManager.AddScreen(new EventPopup(screen, empire, this, triggeredOutcome, false));
            TriggerOutcome(empire, triggeredOutcome);
        }
    }
}
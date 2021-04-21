using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Encounter
    {
        // TODO: What is serialized here??
        public int Step;
        public bool FactionInitiated;
        public bool PlayerInitiated;
        public bool FirstContact;
        public string FileName; // for debugging
        public string Name;
        public string Faction;
        public string DescriptionText;
        public Array<Message> MessageList;
        public float PercentMoneyDemanded; // Custom Percent of victim income

        public static void ShowEncounterPopUpPlayerInitiated(Empire faction, UniverseScreen screen) =>
            ShowEncounterPopUp(faction, screen, playerInitiated: true);

        public static void ShowEncounterPopUpFactionInitiated(Empire faction, UniverseScreen screen) =>
            ShowEncounterPopUp(faction, screen, playerInitiated: false);

        static void ShowEncounterPopUp(Empire faction, UniverseScreen screen, bool playerInitiated)
        {
            if (faction == null)
            {
                Log.Error("ShowEncounter faction was null");
                return;
            }

            Empire player    = EmpireManager.Player;
            Relationship rel = player.GetRelations(faction);
            int requiredStep = playerInitiated ? rel.PlayerContactStep : rel.FactionContactStep;

            Encounter[] encounters = playerInitiated ? ResourceManager.Encounters.Filter(e => e.PlayerInitiated) 
                                                     : ResourceManager.Encounters.Filter(e => e.FactionInitiated);
            
            if (GetEncounter(encounters, faction, requiredStep, out Encounter encounter))
            {
                EncounterPopup.Show(screen, player, faction, encounter);
            }
            else
            {
                string initiation = playerInitiated ? "Player Initiated" : "Faction Initiated";
                Log.Warning($"Encounter not found for {faction.Name}, {initiation}, Step: {requiredStep}");
            }
        }

        public static bool GetEncounterForAI(Empire faction, int requireStep, out Encounter encounter)
        {
            Encounter[] encounters = ResourceManager.Encounters.Filter(e => e.FactionInitiated);
            GetEncounter(encounters, faction, requireStep, out encounter);
            return encounter != null;
        }

        static bool GetEncounter(Encounter[] encounters, Empire faction, int requiredStep, out Encounter encounter)
        {
            foreach (Encounter e in encounters)
            {
                if (faction.data.Traits.Name == e.Faction && requiredStep == e.Step)
                {
                    encounter = e;
                    return true;
                }
            }
            encounter = null;
            return false;
        }
    }
}
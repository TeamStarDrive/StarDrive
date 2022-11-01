using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Empires.Components
{
    public class EmpireFirstContact
    {
        // used as a trigger for launching first contact diplomacy
        SmallBitSet ReadyForFirstContact;

        public EmpireFirstContact()
        {
        }

        public void SetReadyForContact(Empire other)
        {
            ReadyForFirstContact.Set(other.Id);
        }

        public void CheckForFirstContacts(Empire owner)
        {
            if (!ReadyForFirstContact.IsAnyBitsSet)
                return;

            foreach (Empire e in owner.Universe.Empires)
            {
                if (ReadyForFirstContact.IsSet(e.Id))
                {
                    ReadyForFirstContact.Unset(e.Id);
                    if (!owner.IsKnown(e))
                    {
                        DoFirstContact(owner, e);
                        return;
                    }
                }
            }
        }

        void DoFirstContact(Empire owner, Empire them)
        {
            Relationship usToThem = owner.GetRelations(them);
            usToThem.SetInitialStrength(them.data.Traits.DiplomacyMod * 100f);

            owner.SetRelationsAsKnown(usToThem, them);

            if (!them.IsKnown(owner)) // do THEY know us?
                DoFirstContact(them, them:owner);

            if (owner.Universe.Debug)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && owner.isPlayer)
                return;

            if (owner.isPlayer)
            {
                if (them.IsFaction)
                    DoFactionFirstContact(them);
                else
                    DiplomacyScreen.Show(them, "First Contact");
            }
        }

        void DoFactionFirstContact(Empire e)
        {
            var factionContacts = ResourceManager.Encounters.Filter(enc => enc.Faction == e.data.Traits.Name);
            if (factionContacts.Length == 0)
                return; // no dialogs for this faction, no use to look for first contact

            var firstContacts = factionContacts.Filter(enc => enc.FirstContact);
            if (firstContacts.Length > 0)
            {
                Encounter encounter = firstContacts.First();
                EncounterPopup.Show(e.Universe.Screen, e.Universe.Player, e, encounter);
            }
            else
            {
                Log.Warning($"Could not find First Contact Encounter for {e.Name}, " +
                            "make sure this faction has <FirstContact>true</FirstContact> in one of it's encounter dialog XMLs");
            }
        }
    }
}

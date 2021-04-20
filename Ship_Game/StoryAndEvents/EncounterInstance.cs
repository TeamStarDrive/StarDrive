using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.StoryAndEvents
{
    /// <summary>
    /// This tracks the state of a single encounter
    /// </summary>
    public class EncounterInstance
    {
        readonly Encounter Encounter;
        public readonly Empire Player;
        public readonly Empire TargetEmpire;

        // This is the currently displayed dialog in the encounter.
        // It will change according to the flow and decisions in the encounter
        public Message CurrentDialog { get; private set; }
        public SolarSystem SystemToDiscuss { get; set; }

        public EncounterInstance(Encounter e, Empire player, Empire targetEmpire)
        {
            if (targetEmpire == null)
                throw new ArgumentNullException(nameof(targetEmpire));

            Encounter = e;
            Player = player;
            TargetEmpire = targetEmpire;

            // The first message is always the initial message shown to players
            CurrentDialog = Encounter.MessageList[0];
        }

        public void OnResponseItemClicked(ResponseListItem item)
        {
            Response r = item.Response;
            if (r.DefaultIndex != -1)
            {
                CurrentDialog = Encounter.MessageList[r.DefaultIndex];
            }
            else
            {
                int money = NetMoneyDemand(r.MoneyToThem);
                bool ok = !(money > 0 && Player.Money < money);
                if (r.RequiredTech != null && !Player.HasUnlocked(r.RequiredTech))
                    ok = false;
                if (r.FailIfNotAlluring && Player.data.Traits.DiplomacyMod < 0.2)
                    ok = false;

                if (!ok)
                {
                    CurrentDialog = Encounter.MessageList[r.FailIndex];
                }
                else
                {
                    CurrentDialog = Encounter.MessageList[r.SuccessIndex];
                    if (money > 0 && Player.Money >= money)
                    {
                        Player.AddMoney(-money);
                    }
                }
            }

            if (CurrentDialog.SetWar)
                TargetEmpire.GetEmpireAI().DeclareWarFromEvent(Player, WarType.SkirmishWar);

            if (CurrentDialog.EndWar)
                TargetEmpire.GetEmpireAI().EndWarFromEvent(Player);

            Relationship rel = Player.GetRelations(TargetEmpire);
            if (CurrentDialog.SetPlayerContactStep > 0)
                rel.PlayerContactStep = CurrentDialog.SetPlayerContactStep;

            if (CurrentDialog.SetFactionContactStep > 0)
                rel.FactionContactStep = CurrentDialog.SetFactionContactStep;
        }

        int NetMoneyDemand(int demandFromMessage)
        {
            if (Encounter.PercentMoneyDemanded > 0 && TargetEmpire.WeArePirates)
                return TargetEmpire.Pirates.GetMoneyModifier(Player, Encounter.PercentMoneyDemanded);

            return demandFromMessage;
        }

        int CustomMoneyDemand => NetMoneyDemand(0); // For the parser only

        
        public string GetEncounterText(float maxLineWidth, Graphics.Font font)
        {
            // Note: CurrentDialog changes when player selects a new decision
            string text = CurrentDialog.LocalizedText.NotEmpty()
                        ? Localizer.Token(CurrentDialog.LocalizedText)
                        : CurrentDialog.Text;

            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; ++i)
                words[i] = ParseEncounterKeyword(words[i]);

            string textToParse = string.Join(" ", words);
            return font.ParseText(textToParse, maxLineWidth);
        }

        string ParseEncounterKeyword(string keyword)
        {
            if (keyword.IsEmpty())
                return "";

            string suffix = "";
            char punctuation = keyword[keyword.Length-1];
            if (punctuation == '.' || punctuation == ',' ||
                punctuation == '?' || punctuation == '!')
            {
                suffix = punctuation.ToString();
                keyword = keyword.Substring(0, keyword.Length - 1);
            }

            switch (keyword)
            {
                default: return keyword;
                case "SING": return Player.data.Traits.Singular + suffix;
                case "PLURAL": return Player.data.Traits.Plural + suffix;
                case "TARSYS": return (SystemToDiscuss?.Name ?? "System") + suffix;
                case "TAREMP": return TargetEmpire.data.Traits.Name + suffix;
                case "ADJ1": return Player.data.Traits.Adj1 + suffix;
                case "ADJ2": return Player.data.Traits.Adj2 + suffix;
                case "MONEY": return CustomMoneyDemand.String();
            }
        }
    }
}

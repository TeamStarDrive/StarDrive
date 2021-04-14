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
        public Message Message { get; private set; }
        public SolarSystem SystemToDiscuss { get; set; }

        public EncounterInstance(Encounter e, Empire player, Empire targetEmpire)
        {
            if (targetEmpire == null)
                throw new ArgumentNullException(nameof(targetEmpire));

            Encounter = e;
            Player = player;
            TargetEmpire = targetEmpire;

            // The first message is always the initial message shown to players
            Message = Encounter.MessageList[0];
        }

        public void OnResponseItemClicked(ResponseListItem item)
        {
            Response r = item.Response;
            if (r.DefaultIndex != -1)
            {
                Message = Encounter.MessageList[r.DefaultIndex];
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
                    Message = Encounter.MessageList[r.FailIndex];
                }
                else
                {
                    Message = Encounter.MessageList[r.SuccessIndex];
                    if (money > 0 && Player.Money >= money)
                    {
                        Player.AddMoney(-money);
                    }
                }
            }

            if (Message.SetWar)
                TargetEmpire.GetEmpireAI().DeclareWarFromEvent(Player, WarType.SkirmishWar);

            if (Message.EndWar)
                TargetEmpire.GetEmpireAI().EndWarFromEvent(Player);

            Relationship rel = Player.GetRelations(TargetEmpire);
            if (Message.SetPlayerContactStep > 0)
                rel.PlayerContactStep = Message.SetPlayerContactStep;

            if (Message.SetFactionContactStep > 0)
                rel.FactionContactStep = Message.SetFactionContactStep;
        }

        int NetMoneyDemand(int demandFromMessage)
        {
            if (Encounter.PercentMoneyDemanded > 0 && TargetEmpire.WeArePirates)
                return TargetEmpire.Pirates.GetMoneyModifier(Player, Encounter.PercentMoneyDemanded);

            return demandFromMessage;
        }

        int CustomMoneyDemand => NetMoneyDemand(0); // For the parser only

        
        public string ParseCurrentEncounterText(float maxLineWidth, Graphics.Font font)
        {
            string[] words = Message.Text.Split(' ');
            for (int i = 0; i < words.Length; ++i)
                words[i] = ParseEncounterKeyword(words[i]);

            return font.ParseText(words, maxLineWidth);
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

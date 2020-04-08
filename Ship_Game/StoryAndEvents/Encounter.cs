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
        public string Name;
        public string Faction;
        public string DescriptionText;
        public Array<Message> MessageList;
        public int CurrentMessageId;
        public int MoneyRequested;


        Empire playerEmpire;
        SolarSystem sysToDiscuss;
        Empire empToDiscuss;

        public Message Current => MessageList[CurrentMessageId];

        public void OnResponseItemClicked(ResponseListItem item)
        {
            Response r = item.Response;
            if (r.DefaultIndex != -1)
            {
                CurrentMessageId = r.DefaultIndex;
            }
            else
            {
                int money = r.MoneyToThem.LowerBound(MoneyRequested);
                bool ok = !(money > 0 && playerEmpire.Money < money);
                if (r.RequiredTech != null && !playerEmpire.HasUnlocked(r.RequiredTech))
                    ok = false;
                if (r.FailIfNotAlluring && playerEmpire.data.Traits.DiplomacyMod < 0.2)
                    ok = false;
                if (!ok)
                {
                    CurrentMessageId = r.FailIndex;
                }
                else
                {
                    CurrentMessageId = r.SuccessIndex;
                    if (money > 0 && playerEmpire.Money >= money)
                    {
                        playerEmpire.AddMoney(-money);
                    }
                }
            }

            if (MessageList[CurrentMessageId].SetWar)
            {
                empToDiscuss.GetEmpireAI().DeclareWarFromEvent(playerEmpire, WarType.SkirmishWar);
            }

            if (MessageList[CurrentMessageId].EndWar)
            {
                empToDiscuss.GetEmpireAI().EndWarFromEvent(playerEmpire);
            }

            Relationship rel = playerEmpire.GetRelations(empToDiscuss);
            Message message = MessageList[CurrentMessageId];
            if (message.SetPlayerContactStep > 0)
                rel.PlayerContactStep = message.SetPlayerContactStep;

            if (message.SetFactionContactStep > 0)
                rel.FactionContactStep = message.SetFactionContactStep;
        }

        public string ParseCurrentEncounterText(float maxLineWidth, SpriteFont font)
        {
            Message current = Current;
            string[] wordArray = current.Text.Split(' ');
            for (int i = 0; i < wordArray.Length; ++i)
                wordArray[i] = ParseEncounterKeyword(wordArray[i]);

            return font.ParseText(wordArray, maxLineWidth);
        }

        string ParseEncounterKeyword(string keyword)
        {
            switch (keyword)
            {
                default: return keyword;
                case "SING": return playerEmpire.data.Traits.Singular;
                case "SING.": return playerEmpire.data.Traits.Singular + ".";
                case "SING,": return playerEmpire.data.Traits.Singular + ",";
                case "SING?": return playerEmpire.data.Traits.Singular + "?";
                case "SING!": return playerEmpire.data.Traits.Singular + "!";
                case "PLURAL": return playerEmpire.data.Traits.Plural;
                case "PLURAL.": return playerEmpire.data.Traits.Plural + ".";
                case "PLURAL,": return playerEmpire.data.Traits.Plural + ",";
                case "PLURAL?": return playerEmpire.data.Traits.Plural + "?";
                case "PLURAL!": return playerEmpire.data.Traits.Plural + "!";
                case "TARSYS": return sysToDiscuss.Name;
                case "TARSYS.": return sysToDiscuss.Name + ".";
                case "TARSYS,": return sysToDiscuss.Name + ",";
                case "TARSYS?": return sysToDiscuss.Name + "?";
                case "TARSYS!": return sysToDiscuss.Name + "!";
                case "TAREMP": return empToDiscuss.data.Traits.Name;
                case "TAREMP.": return empToDiscuss.data.Traits.Name + ".";
                case "TAREMP,": return empToDiscuss.data.Traits.Name + ",";
                case "TAREMP?": return empToDiscuss.data.Traits.Name + "?";
                case "TAREMP!": return empToDiscuss.data.Traits.Name + "!";
                case "ADJ1": return playerEmpire.data.Traits.Adj1;
                case "ADJ1.": return playerEmpire.data.Traits.Adj1 + ".";
                case "ADJ1,": return playerEmpire.data.Traits.Adj1 + ",";
                case "ADJ1?": return playerEmpire.data.Traits.Adj1 + "?";
                case "ADJ1!": return playerEmpire.data.Traits.Adj1 + "!";
                case "ADJ2": return playerEmpire.data.Traits.Adj2;
                case "ADJ2.": return playerEmpire.data.Traits.Adj2 + ".";
                case "ADJ2,": return playerEmpire.data.Traits.Adj2 + ",";
                case "ADJ2?": return playerEmpire.data.Traits.Adj2 + "?";
                case "ADJ2!": return playerEmpire.data.Traits.Adj2 + "!";
                case "MONEY": return MoneyRequested.String();
            }
        }

        public void SetPlayerEmpire(Empire e)
        {
            playerEmpire = e;
        }

        public void SetSys(SolarSystem s)
        {
            sysToDiscuss = s;
        }

        public void SetTarEmp(Empire e)
        {
            empToDiscuss = e;
        }

        public static void ShowEncounterPopUpPlayerInitiated(Empire faction, UniverseScreen screen, float moneyMod = 1) =>
            ShowEncounterPopUp(faction, screen, playerInitiated: true, moneyMod);

        public static void ShowEncounterPopUpFactionInitiated(Empire faction, UniverseScreen screen, float moneyMod = 1) =>
            ShowEncounterPopUp(faction, screen, playerInitiated: false, moneyMod);

        static void ShowEncounterPopUp(Empire faction, UniverseScreen screen, bool playerInitiated, float moneyModifier)
        {
            if (faction == null)
                return;

            Empire player    = EmpireManager.Player;
            Relationship rel = player.GetRelations(faction);
            int requiredStep = playerInitiated ? rel.PlayerContactStep : rel.FactionContactStep;


            Encounter[] encounters = playerInitiated ? ResourceManager.Encounters.Filter(e => e.PlayerInitiated) 
                                                     : ResourceManager.Encounters.Filter(e => e.FactionInitiated);
            
            foreach (Encounter e in encounters)
            {
                string empireName = faction.data.Traits.Name;
                if (empireName == e.Faction && requiredStep == e.Step)
                {
                    e.MoneyRequested = (e.MoneyRequested * moneyModifier).RoundTo10();
                    EncounterPopup.Show(screen, player, faction, e);
                    break;
                }
            }
        }
    }
}
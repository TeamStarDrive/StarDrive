using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class Encounter
    {
        // TODO: What is serialized here??
        public int Step;
        public string Name;
        public string Faction;
        public string DescriptionText;
        public Array<Message> MessageList;
        public int CurrentMessageId;


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
                bool ok = !(r.MoneyToThem > 0 && playerEmpire.Money < r.MoneyToThem);
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
                    if (r.MoneyToThem > 0 && playerEmpire.Money >= r.MoneyToThem)
                    {
                        playerEmpire.AddMoney(-r.MoneyToThem);
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

            playerEmpire.GetRelations(empToDiscuss).EncounterStep =
                MessageList[CurrentMessageId].SetEncounterStep;
        }

        public string ParseCurrentEncounterText(float maxLineWidth, SpriteFont font)
        {
            Message current = Current;
            string[] wordArray = current.text.Split(' ');
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
                case "SING.": return playerEmpire.data.Traits.Singular+".";
                case "SING,": return playerEmpire.data.Traits.Singular+",";
                case "SING?": return playerEmpire.data.Traits.Singular+"?";
                case "SING!": return playerEmpire.data.Traits.Singular+"!";
                case "PLURAL": return playerEmpire.data.Traits.Plural;
                case "PLURAL.": return playerEmpire.data.Traits.Plural+".";
                case "PLURAL,": return playerEmpire.data.Traits.Plural+",";
                case "PLURAL?": return playerEmpire.data.Traits.Plural+"?";
                case "PLURAL!": return playerEmpire.data.Traits.Plural+"!";
                case "TARSYS": return sysToDiscuss.Name;
                case "TARSYS.": return sysToDiscuss.Name+".";
                case "TARSYS,": return sysToDiscuss.Name+",";
                case "TARSYS?": return sysToDiscuss.Name+"?";
                case "TARSYS!": return sysToDiscuss.Name+"!";
                case "TAREMP": return empToDiscuss.data.Traits.Name;
                case "TAREMP.": return empToDiscuss.data.Traits.Name+".";
                case "TAREMP,": return empToDiscuss.data.Traits.Name+",";
                case "TAREMP?": return empToDiscuss.data.Traits.Name+"?";
                case "TAREMP!": return empToDiscuss.data.Traits.Name+"!";
                case "ADJ1": return playerEmpire.data.Traits.Adj1;
                case "ADJ1.": return playerEmpire.data.Traits.Adj1+".";
                case "ADJ1,": return playerEmpire.data.Traits.Adj1+",";
                case "ADJ1?": return playerEmpire.data.Traits.Adj1+"?";
                case "ADJ1!": return playerEmpire.data.Traits.Adj1+"!";
                case "ADJ2": return playerEmpire.data.Traits.Adj2;
                case "ADJ2.": return playerEmpire.data.Traits.Adj2+".";
                case "ADJ2,": return playerEmpire.data.Traits.Adj2+",";
                case "ADJ2?": return playerEmpire.data.Traits.Adj2+"?";
                case "ADJ2!": return playerEmpire.data.Traits.Adj2+"!";
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
    }
}
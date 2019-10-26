using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class Encounter
    {
        public int Step;
        public string Name;
        public string Faction;
        public string DescriptionText;
        public Array<Message> MessageList;
        public int CurrentMessage;

        Rectangle ResponseRect;
        Rectangle BlackRect;
        ScrollList<ResponseListItem> ResponseSL;
        Empire playerEmpire;
        SolarSystem sysToDiscuss;
        Empire empToDiscuss;

        public void LoadContent(ScreenManager screenManager, Rectangle fitRect)
        {
            BlackRect = new Rectangle(fitRect.X, fitRect.Y, fitRect.Width, 240);
            ResponseRect = new Rectangle(fitRect.X, BlackRect.Y + BlackRect.Height + 10, fitRect.Width, 180);
            var resp = new Submenu(ResponseRect);
            ResponseSL = new ScrollList<ResponseListItem>(resp, 20);
            LoadResponseScrollList();
        }

        void LoadResponseScrollList()
        {
            ResponseSL.Reset();
            foreach (Response r in MessageList[CurrentMessage].ResponseOptions)
            {
                ResponseSL.AddItem(new ResponseListItem(r));
            }
            ResponseSL.OnClick = OnResponseItemClicked;
        }

        void OnResponseItemClicked(ResponseListItem item)
        {
            Response r = item.Response;
            if (r.DefaultIndex != -1)
            {
                CurrentMessage = r.DefaultIndex;
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
                    CurrentMessage = r.FailIndex;
                }
                else
                {
                    CurrentMessage = r.SuccessIndex;
                    if (r.MoneyToThem > 0 && playerEmpire.Money >= r.MoneyToThem)
                    {
                        playerEmpire.AddMoney(-r.MoneyToThem);
                    }
                }
            }

            if (MessageList[CurrentMessage].SetWar)
            {
                empToDiscuss.GetEmpireAI().DeclareWarFromEvent(playerEmpire, WarType.SkirmishWar);
            }

            if (MessageList[CurrentMessage].EndWar)
            {
                empToDiscuss.GetEmpireAI().EndWarFromEvent(playerEmpire);
            }

            playerEmpire.GetRelations(empToDiscuss).EncounterStep =
                MessageList[CurrentMessage].SetEncounterStep;

            LoadResponseScrollList();
        }

        public void Draw(SpriteBatch batch)
        {
            batch.FillRectangle(BlackRect, Color.Black);
            batch.FillRectangle(ResponseRect, Color.Black);
            Vector2 TheirTextPos = new Vector2(BlackRect.X + 10, BlackRect.Y + 10);
            string theirText = ParseTextEncounters(MessageList[CurrentMessage].text, BlackRect.Width - 20, Fonts.Verdana12Bold);
            TheirTextPos.X = (int)TheirTextPos.X;
            TheirTextPos.Y = (int)TheirTextPos.Y;
            batch.DrawString(Fonts.Verdana12Bold, theirText, TheirTextPos, Color.White);
            if (MessageList[CurrentMessage].EndTransmission)
            {
                var responsePos = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 10);
                batch.DrawString(Fonts.Arial12Bold, "Escape or Right Click to End Transmission:", responsePos, Color.White);
            }
            else
            {
                var drawCurs = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 10);
                batch.DrawString(Fonts.Arial12Bold, "Your Response:", drawCurs, Color.White);
                ResponseSL.Draw(batch);
            }
        }

        public bool HandleInput(InputState input, GameScreen caller)
        {
            if (ResponseSL.HandleInput(input))
                return true;

            if (MessageList[CurrentMessage].EndTransmission &&
                (input.Escaped || input.RightMouseClick))
            {
                caller.ExitScreen();
                return true;
            }

            return false;
        }

        string ParseTextEncounters(string text, float maxLineWidth, SpriteFont font)
        {
            string[] wordArray = text.Split(' ');
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
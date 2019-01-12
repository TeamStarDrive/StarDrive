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

        private Rectangle MainRect;

        private Rectangle ResponseRect;

        private Rectangle TopRect;

        private Rectangle BlackRect;

        private ScrollList ResponseSL;

        private Empire playerEmpire;

        private SolarSystem sysToDiscuss;

        private Empire empToDiscuss;

        private bool CheckIfWeCanMeetDemand()
        {
            if (MessageList[CurrentMessage].MoneyDemanded > playerEmpire.Money)
            {
                return false;
            }
            return true;
        }

        public void Draw(ScreenManager ScreenManager)
        {
            ScreenManager.SpriteBatch.FillRectangle(BlackRect, Color.Black);
            ScreenManager.SpriteBatch.FillRectangle(ResponseRect, Color.Black);
            Vector2 TheirTextPos = new Vector2(BlackRect.X + 10, BlackRect.Y + 10);
            string theirText = parseText(MessageList[CurrentMessage].text, BlackRect.Width - 20, Fonts.Verdana12Bold);
            TheirTextPos.X = (int)TheirTextPos.X;
            TheirTextPos.Y = (int)TheirTextPos.Y;
            ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12Bold, theirText, TheirTextPos, Color.White);
            if (MessageList[CurrentMessage].EndTransmission)
            {
                var responsePos = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 10);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Escape or Right Click to End Transmission:", responsePos, Color.White);
            }
            else
            {
                ResponseSL.Draw(ScreenManager.SpriteBatch);
                var drawCurs = new Vector2(ResponseRect.X + 10, ResponseRect.Y + 10);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Your Response:", drawCurs, Color.White);
                drawCurs.X += 10f;
                int i = ResponseSL.FirstVisibleIndex;
                foreach (ScrollList.Entry e in ResponseSL.VisibleEntries)
                {
                    drawCurs.Y = e.Y;
                    drawCurs.X = (int)drawCurs.X;
                    ++i;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        $"{i}. {((Response)e.item).Text}", drawCurs,
                        (e.Hovered ? Color.LightGray : Color.White));
                }
            }
        }

        public void HandleInput(InputState input, GameScreen caller)
        {
            foreach (ScrollList.Entry e in ResponseSL.VisibleEntries)
            {
                if (e.CheckHover(input))
                {
                    if (!input.InGameSelect || !(e.item is Response r)) continue;
                    if (r.DefaultIndex != -1)
                    {
                        CurrentMessage = r.DefaultIndex;
                    }
                    else
                    {
                        bool ok = !(r.MoneyToThem > 0 && playerEmpire.Money < r.MoneyToThem);
                        if (r.RequiredTech != null && !playerEmpire.GetTDict()[r.RequiredTech].Unlocked)
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
                    playerEmpire.GetRelations(empToDiscuss).EncounterStep = MessageList[CurrentMessage].SetEncounterStep;
                    SetResponses();
                    break;
                }
            }
            if (MessageList[CurrentMessage].EndTransmission && (input.Escaped || input.MouseCurr.RightButton == ButtonState.Released && input.MousePrev.RightButton == ButtonState.Pressed))
            {
                caller.ExitScreen();
            }
        }

        public void LoadContent(ScreenManager screenManager, Rectangle fitRect)
        {
            MainRect = new Rectangle(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
            TopRect = new Rectangle(MainRect.X, MainRect.Y, MainRect.Width, 28);
            BlackRect = new Rectangle(fitRect.X, fitRect.Y, fitRect.Width, 240);
            ResponseRect = new Rectangle(fitRect.X, BlackRect.Y + BlackRect.Height + 10, fitRect.Width, 180);
            var resp = new Submenu(ResponseRect);
            ResponseSL = new ScrollList(resp, 20);
            SetResponses();
        }

        private string parseText(string text, float Width, SpriteFont font)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] wordArray = text.Split(' ');
            for (int i = 0; i < wordArray.Length; i++)
            {
                if (wordArray[i] == "SING")
                {
                    wordArray[i] = playerEmpire.data.Traits.Singular;
                }
                else if (wordArray[i] == "SING.")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Singular, ".");
                }
                else if (wordArray[i] == "SING,")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Singular, ",");
                }
                else if (wordArray[i] == "SING?")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Singular, "?");
                }
                else if (wordArray[i] == "SING!")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Singular, "!");
                }
                if (wordArray[i] == "PLURAL")
                {
                    wordArray[i] = playerEmpire.data.Traits.Plural;
                }
                else if (wordArray[i] == "PLURAL.")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Plural, ".");
                }
                else if (wordArray[i] == "PLURAL,")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Plural, ",");
                }
                else if (wordArray[i] == "PLURAL?")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Plural, "?");
                }
                else if (wordArray[i] == "PLURAL!")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Plural, "!");
                }
                if (wordArray[i] == "TARSYS")
                {
                    wordArray[i] = sysToDiscuss.Name;
                }
                else if (wordArray[i] == "TARSYS.")
                {
                    wordArray[i] = string.Concat(sysToDiscuss.Name, ".");
                }
                else if (wordArray[i] == "TARSYS,")
                {
                    wordArray[i] = string.Concat(sysToDiscuss.Name, ",");
                }
                else if (wordArray[i] == "TARSYS?")
                {
                    wordArray[i] = string.Concat(sysToDiscuss.Name, "?");
                }
                else if (wordArray[i] == "TARSYS!")
                {
                    wordArray[i] = string.Concat(sysToDiscuss.Name, "!");
                }
                if (wordArray[i] == "TAREMP")
                {
                    wordArray[i] = empToDiscuss.data.Traits.Name;
                }
                else if (wordArray[i] == "TAREMP.")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, ".");
                }
                else if (wordArray[i] == "TAREMP,")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, ",");
                }
                else if (wordArray[i] == "TAREMP?")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, "?");
                }
                else if (wordArray[i] == "TAREMP!")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, "!");
                }
                if (wordArray[i] == "ADJ1")
                {
                    wordArray[i] = playerEmpire.data.Traits.Adj1;
                }
                else if (wordArray[i] == "ADJ1.")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj1, ".");
                }
                else if (wordArray[i] == "ADJ1,")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj1, ",");
                }
                else if (wordArray[i] == "ADJ1?")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj1, "?");
                }
                else if (wordArray[i] == "ADJ1!")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj1, "!");
                }
                if (wordArray[i] == "ADJ2")
                {
                    wordArray[i] = playerEmpire.data.Traits.Adj2;
                }
                else if (wordArray[i] == "ADJ2.")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj2, ".");
                }
                else if (wordArray[i] == "ADJ2,")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj2, ",");
                }
                else if (wordArray[i] == "ADJ2?")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj2, "?");
                }
                else if (wordArray[i] == "ADJ2!")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Adj2, "!");
                }
            }
            string[] strArrays = wordArray;
            for (int j = 0; j < strArrays.Length; j++)
            {
                string word = strArrays[j];
                if (font.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }

        public void SetPlayerEmpire(Empire e)
        {
            playerEmpire = e;
        }

        private void SetResponses()
        {
            ResponseSL.Reset();
            foreach (Response r in MessageList[CurrentMessage].ResponseOptions)
            {
                ResponseSL.AddItem(r);
            }
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
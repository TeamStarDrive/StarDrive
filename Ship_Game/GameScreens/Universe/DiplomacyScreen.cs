using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ship_Game.GameScreens;

namespace Ship_Game
{
    public sealed class DiplomacyScreen : GameScreen
    {
        private Empire them;

        private Empire playerEmpire;

        private string whichDialogue;

        private bool WarDeclared;

        public GenericButton SendOffer;

        private Rectangle Portrait;

        private Vector2 TextCursor;

        private SolarSystem sysToDiscuss;

        private Planet pToDiscuss;

        private DialogState dState;

        private Menu2 Player;

        private readonly Array<GenericButton> GenericButtons = new Array<GenericButton>();

        private GenericButton DeclareWar;

        private GenericButton Negotiate;

        private GenericButton Discuss;

        private GenericButton Exit;

        private Rectangle DialogRect;

        private Rectangle UsRect;

        private Rectangle ThemRect;

        private Rectangle BigTradeRect;

        private ScrollList StatementsSL;

        private ScrollList OurItemsSL;

        private ScrollList TheirItemsSL;

        private GenericButton Accept;

        private GenericButton Reject;

        private GenericButton Trust;

        private GenericButton Anger;

        private GenericButton Fear;

        private Array<GenericButton> TAFButtons = new Array<GenericButton>();

        private Rectangle Attitude_Pleading_Rect;

        private Rectangle Attitude_Respectful_Rect;

        private Rectangle Attitude_Threaten_Rect;

        private GenericButton ap;

        private GenericButton ar;

        private GenericButton at;

        private Vector2 EmpireNamePos;

        private Rectangle R;

        private Rectangle BridgeRect;

        private ScrollList OfferTextSL;

        private Rectangle Negotiate_Right;

        private Rectangle Negotiate_Left;

        private Rectangle ToneContainerRect;

        private Rectangle AccRejRect;

        private Rectangle TrustRect;

        private Rectangle AngerRect;

        private Rectangle FearRect;

        private Offer.Attitude Attitude = Offer.Attitude.Respectful;

        public Offer OurOffer = new Offer();

        public Offer TheirOffer = new Offer();

        public Empire empToDiscuss;

        ScreenMediaPlayer RacialVideo;

        private string TheirText;

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which) : base(parent)
        {
            float TheirOpinionOfUs;
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            IsPopup = true;
            string str = which;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "Conquered_Player":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Declare War Imperialism":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Imperialism Break NA":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense BrokenNA":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Compliment Military":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Compliment Military Better":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Insult Military":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Declare War BC":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    default:
                    {
                        TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                        if (TheirOpinionOfUs < 0f)
                        {
                            TheirOpinionOfUs = 0f;
                        }
                        TheirText = GetDialogue(TheirOpinionOfUs);
                        TransitionOnTime = 1.0f;
                        return;
                    }
                }
            }
            else
            {
                TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                if (TheirOpinionOfUs < 0f)
                {
                    TheirOpinionOfUs = 0f;
                }
                TheirText = GetDialogue(TheirOpinionOfUs);
                TransitionOnTime = 1.0f;
                return;
            }
            TransitionOnTime = 1.0f;
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, bool EndOnly) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            IsPopup = true;
            TheirText = GetDialogueByName(which);
            dState = DialogState.End;
            TransitionOnTime = 1.0f;
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, Offer ourOffer, Offer theirOffer) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            IsPopup = true;
            dState = DialogState.TheirOffer;
            TheirText = GetDialogueByName(which);
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            TransitionOnTime = 1.0f;
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, Offer ourOffer, Offer theirOffer, Empire taremp) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            empToDiscuss = taremp;
            whichDialogue = which;
            IsPopup = true;
            dState = DialogState.TheirOffer;
            TheirText = GetDialogueByName(which);
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            TransitionOnTime = 1.0f;
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, Planet p) : base(parent)
        {
            float TheirOpinionOfUs;
            e.GetRelations(us).turnsSinceLastContact = 0;
            pToDiscuss = p;
            sysToDiscuss = p.ParentSystem;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            TransitionOnTime = 1.0f;
            string str = which;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Declare War Defense")
                {
                    TheirText = GetDialogueByName(which);
                    dState = DialogState.End;
                    WarDeclared = true;
                    return;
                }

                if (str1 == "Declare War BC")
                {
                    TheirText = GetDialogueByName(which);
                    dState = DialogState.End;
                    WarDeclared = true;
                    return;
                }

                if (str1 != "Declare War BC TarSys")
                {
                    TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                    if (TheirOpinionOfUs < 0f)
                        TheirOpinionOfUs = 0f;
                    TheirText = GetDialogue(TheirOpinionOfUs);
                    return;
                }
                TheirText = GetDialogueByName(which);
                dState = DialogState.End;
                WarDeclared = true;
                return;
            }
            TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
            if (TheirOpinionOfUs < 0f)
                TheirOpinionOfUs = 0f;
            TheirText = GetDialogue(TheirOpinionOfUs);
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, SolarSystem s) : base(parent)
        {
            float TheirOpinionOfUs;
            e.GetRelations(us).turnsSinceLastContact = 0;
            sysToDiscuss = s;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            TransitionOnTime = 1.0f;
            string str = which;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "Invaded NA Pact":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Invaded Start War":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War BC":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War BC TarSys":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Stole Claim":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Stole Claim 2":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    case "Stole Claim 3":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DialogState.End;
                        break;
                    }
                    default:
                    {
                        TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                        if (TheirOpinionOfUs < 0f)
                            TheirOpinionOfUs = 0f;
                        TheirText = GetDialogue(TheirOpinionOfUs);
                        return;
                    }
                }
            }
            else
            {
                TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                if (TheirOpinionOfUs < 0f)
                {
                    TheirOpinionOfUs = 0f;
                }
                TheirText = GetDialogue(TheirOpinionOfUs);
            }
        }

        public static void Stole1stColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim");
        public static void Stole2ndColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 2");
        public static void Stole3rdColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 3");

        private static void StoleColonyClaim(Planet claimedPlanet, Empire victim, string type)
        {
            var dipScreen =  new DiplomacyScreen(Empire.Universe, victim, Empire.Universe.PlayerEmpire, type, claimedPlanet.ParentSystem);

            Empire.Universe.ScreenManager.AddScreen(dipScreen);
        }


        private void DoNegotiationResponse(string answer)
        {
            StatementsSL.Reset();
            TheirText = "";
            if (TheirOffer.NAPact && them.GetRelations(playerEmpire).HaveRejectedNapact)
            {
                TheirText = string.Concat(GetDialogueByName("ComeAround_NAPACT"), "\n\n");
            }
            else if (TheirOffer.TradeTreaty && them.GetRelations(playerEmpire).HaveRejected_TRADE)
            {
                TheirText = string.Concat(GetDialogueByName("ComeAround_TRADE"), "\n\n");
            }
            DiplomacyScreen diplomacyScreen = this;
            diplomacyScreen.TheirText = string.Concat(diplomacyScreen.TheirText, GetDialogueByName(answer));
            dState = DialogState.Them;
        }

        public override void Draw(SpriteBatch batch)
        {
            string text;
            Vector2 position;
            Vector2 drawCurs;
            if (!IsActive)
                return;

            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 4 / 5);
            batch.Begin();
            if (RacialVideo.IsPlaying)
            {
                Color color = Color.White;
                if (WarDeclared || playerEmpire.GetRelations(them).AtWar)
                {
                    color.B = 100;
                    color.G = 100;
                }
                RacialVideo.Draw(batch, color);
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", them.PortraitName)), Portrait, Color.White);
            }
            HelperFunctions.DrawDropShadowText(batch, them.data.Traits.Name, EmpireNamePos, Fonts.Pirulen20);
            if (dState == DialogState.Negotiate)
            {
                Rectangle stripe = new Rectangle(0, R.Y, 1920, R.Height);
                ScreenManager.SpriteBatch.FillRectangle(stripe, new Color(0, 0, 0, 150));
            }
            else
            {
                Rectangle stripe = new Rectangle(0, DialogRect.Y, 1920, R.Height);
                ScreenManager.SpriteBatch.FillRectangle(stripe, new Color(0, 0, 0, 150));
            }
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("GameScreens/Bridge"), BridgeRect, Color.White);
            foreach (GenericButton taf in TAFButtons)
            {
                taf.DrawWithShadowCaps(batch);
                TrustRect.Width = (int)them.GetRelations(playerEmpire).Trust;
                if (TrustRect.Width < 1)
                {
                    TrustRect.Width = 1;
                }
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), TrustRect, Color.Green);
                AngerRect.Width = (int)them.GetRelations(playerEmpire).TotalAnger;
                if (AngerRect.Width > 100)
                {
                    AngerRect.Width = 100;
                }
                if (AngerRect.Width < 1)
                {
                    AngerRect.Width = 1;
                }
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), AngerRect, Color.Yellow);
                FearRect.Width = (int)them.GetRelations(playerEmpire).Threat;
                if (FearRect.Width < 1)
                {
                    FearRect.Width = 1;
                }
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), FearRect, Color.Red);
            }
            switch (dState)
            {
                case DialogState.Them:
                {
                    var selector = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    text = parseText(TheirText, (DialogRect.Width - 25), Fonts.Consolas18);
                    position = new Vector2((ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    HelperFunctions.ClampVectorToInt(ref position);
                    DrawDropShadowText(text, position, Fonts.Consolas18);
                    goto case DialogState.Choosing;
                }
                case DialogState.Choosing:
                {
                    if (dState == DialogState.End || dState == DialogState.TheirOffer)
                    {
                        Exit.DrawWithShadowCaps(batch);
                    }
                    else
                    {
                        int numEntries = 4;
                        int k = 4;
                        foreach (GenericButton b in GenericButtons)
                        {
                            Rectangle r = b.R;
                            float transitionOffset = ((TransitionPosition - 0.5f * k / numEntries) / 0.5f).Clamped(0f, 1f);
                            k--;
                            if (ScreenState != ScreenState.TransitionOn)
                            {
                                r.X = r.X + (int)transitionOffset * 512;
                            }
                            else
                            {
                                r.X = r.X + (int)(transitionOffset * 512f);
                            }
                            b.TransitionCaps(r);
                            b.DrawWithShadowCaps(batch);
                        }
                    }
                    Vector2 pos = new Vector2((Portrait.X + 200), (Portrait.Y + 200));
                    //{ no idea how this managed to compile in the first place
                        pos.Y = pos.Y + (Fonts.Pirulen16.LineSpacing + 15);
                        pos.X = pos.X - 8f;
                    //};*/

                    pos.Y = pos.Y + (Fonts.Pirulen16.LineSpacing + 15);
                    pos.X = pos.X - 8f;
                    pos.Y = pos.Y + (Fonts.Pirulen16.LineSpacing + 15);
                    pos.X = pos.X - 8f;
                    ToolTip.Draw(ScreenManager.SpriteBatch);
                    ScreenManager.SpriteBatch.End();
                    return;
                }
                case DialogState.Discuss:
                {
                    var selector1 = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    StatementsSL.Draw(ScreenManager.SpriteBatch);
                    drawCurs = TextCursor;
                    foreach (ScrollList.Entry e in StatementsSL.VisibleEntries)
                    {
                        if (!e.Hovered && e.item is DialogOption option)
                        {
                            option.Update(drawCurs);
                            option.Draw(batch, Fonts.Consolas18);
                            drawCurs.Y += (Fonts.Consolas18.LineSpacing + 5);
                        }
                    }
                    goto case DialogState.Choosing;
                }
                case DialogState.Negotiate:
                {
                    drawCurs = new Vector2((R.X + 15), (R.Y + 10));
                    TheirOffer.Them = them;
                    string txt = OurOffer.FormulateOfferText(Attitude, TheirOffer);
                    OfferTextSL.Reset();
                    HelperFunctions.parseTextToSL(txt, (DialogRect.Width - 30), Fonts.Consolas18, ref OfferTextSL);
                    foreach (ScrollList.Entry e in OfferTextSL.VisibleEntries)
                    {
                        DrawDropShadowText(e.Get<string>(), new Vector2(drawCurs.X, e.Y - 33), Fonts.Consolas18);
                    }
                    if (!TheirOffer.IsBlank() || !OurOffer.IsBlank() || OurOffer.Alliance)
                    {
                        SendOffer.DrawWithShadow(batch);
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Right"), Negotiate_Right, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Left"), Negotiate_Left, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Tone"), ToneContainerRect, Color.White);
                    DrawOurItems();
                    DrawTheirItems();
                    OfferTextSL.Draw(ScreenManager.SpriteBatch);
                    ap.Transition(Attitude_Pleading_Rect);
                    ap.Draw(ScreenManager);
                    at.Transition(Attitude_Threaten_Rect);
                    at.Draw(ScreenManager);
                    ar.Transition(Attitude_Respectful_Rect);
                    ar.Draw(ScreenManager);
                    drawCurs = new Vector2((UsRect.X + 10), (UsRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 2));
                    ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Localizer.Token(1221), drawCurs, Color.White);
                    drawCurs = new Vector2((ThemRect.X + 10), (ThemRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 2));
                    ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Localizer.Token(1222), drawCurs, Color.White);
                    goto case DialogState.Choosing;
                }
                case DialogState.TheirOffer:
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/AcceptReject"), AccRejRect, Color.White);
                    text = parseText(TheirText, DialogRect.Width - 20, Fonts.Consolas18);
                    position = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    DrawDropShadowText(text, position, Fonts.Consolas18);
                    Accept.DrawWithShadow(batch);
                    Reject.DrawWithShadow(batch);
                    goto case DialogState.Choosing;
                }
                case DialogState.End:
                {
                    Selector selector2 = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    text = parseText(TheirText, DialogRect.Width - 20, Fonts.Consolas18);
                    position = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    HelperFunctions.ClampVectorToInt(ref position);
                    DrawDropShadowText(text, position, Fonts.Consolas18);
                    goto case DialogState.Choosing;
                }
                default:
                {
                    goto case DialogState.Choosing;
                }
            }
        }

        private void DrawDropShadowText(string Text, Vector2 Pos, SpriteFont Font)
        {
            Vector2 offset = new Vector2(2f, 2f);
            ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
            ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, Color.White);
        }

        private void DrawOurItems()
        {
            OurItemsSL.Draw(ScreenManager.SpriteBatch);
            var drawCurs = new Vector2((UsRect.X + 10), (UsRect.Y + Fonts.Pirulen12.LineSpacing + 10));
            foreach (ScrollList.Entry e in OurItemsSL.VisibleExpandedEntries)
            {
                if (!e.Hovered && e.item is ItemToOffer item)
                {
                    item.Update(drawCurs);
                    item.Draw(ScreenManager.SpriteBatch, Fonts.Arial12Bold);
                    drawCurs.Y = drawCurs.Y + (Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
        }

        private void DrawSpecialText1612(string Text, Vector2 Pos)
        {
            Vector2 offset = new Vector2(2f, 2f);
            ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos + offset, Color.Black);
            ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos, Color.White);
        }

        private void DrawTheirItems()
        {
            TheirItemsSL.Draw(ScreenManager.SpriteBatch);
            Vector2 drawCurs = new Vector2(ThemRect.X + 10, ThemRect.Y + Fonts.Pirulen12.LineSpacing + 10);
            foreach (ScrollList.Entry e in TheirItemsSL.VisibleExpandedEntries)
            {
                if (!e.Hovered && e.item is ItemToOffer item)
                {
                    item.Update(drawCurs);
                    item.Draw(ScreenManager.SpriteBatch, Fonts.Arial12Bold);
                    drawCurs.Y = drawCurs.Y + (Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
        }

        public override void ExitScreen()
        {
            //if (!them.data.ModRace)
            //{
            //    MusicPlaying.Stop();
            //}
            //GameAudio.SwitchBackToGenericMusic();
            //if (VideoFile != null)
            //{
            //    VideoPlaying.Stop();
            //}
            //if (VideoPlaying != null)
            //{
            //    VideoFile = null;
            //    while (!VideoPlaying.IsDisposed)
            //    {
            //        VideoPlaying.Dispose();
            //    }
            //}
            //VideoPlaying = null;
            base.ExitScreen();
            Dispose();
        }



        private class DiplomacyItemsLayout
        {
            private readonly ScrollList List;
            private ScrollList.Entry Current;
            private Vector2 Cursor;

            public DiplomacyItemsLayout(ScrollList list, Rectangle rect)
            {
                List = list;
                Current = null;
                Cursor = new Vector2((rect.X + 10), (rect.Y + Fonts.Pirulen12.LineSpacing + 2));
            }

            private void AddItem(int tokenId, string response)
            {
                Current = List.AddItem(new ItemToOffer(tokenId, response, Cursor));
                Cursor.Y += (Fonts.Arial12Bold.LineSpacing + 5);
            }

            public void AddSubItem(string name, string response, string inquiry)
            {
                var item = new ItemToOffer(name, response, Cursor) { SpecialInquiry = inquiry };
                Cursor.Y += (Fonts.Arial12Bold.LineSpacing + 5);
                Current.AddSubItem(item);
            }

            public void AddCategory(int categoryId, Action populateSubItems)
            {
                AddItem(categoryId, "");
                Cursor.X += 10f;
                populateSubItems();
                Cursor.X -= 10f;
            }

            public void AddRelationItems(Relationship relations)
            {
                if (!relations.AtWar)
                {
                    if (!relations.Treaty_NAPact)      AddItem(1214, "NAPact");
                    if (!relations.Treaty_Trade)       AddItem(1215, "TradeTreaty");
                    if (!relations.Treaty_OpenBorders) AddItem(1216, "OpenBorders");

                    if (relations.Treaty_Trade && relations.Treaty_NAPact && !relations.Treaty_Alliance)
                        AddItem(2045, "OfferAlliance");
                }
                else
                {
                    AddItem(1213, "Peace Treaty");
                }
            }
        }

        private static void FillItems(Empire empire, Empire other, ScrollList list, Rectangle rect)
        {
            list.Reset();
            var layout = new DiplomacyItemsLayout(list, rect);

            layout.AddRelationItems(empire.GetRelations(other));
            Array<TechEntry> tradableTech = empire.GetEmpireAI().TradableTechs(other);
            layout.AddCategory(1217, () =>
            {
                foreach (TechEntry entry in tradableTech)
                {
                    // Added by McShooterz: prevent root nodes from being traded
                    //if (entry.Unlocked && !other.HasUnlocked(entry) &&
                    //    other.HavePreReq(entry.UID) && !entry.IsRoot)
                    {
                        Technology tech = entry.Tech;
                        layout.AddSubItem($"{Localizer.Token(tech.NameIndex)}: {(int) tech.ActualCost}", "Tech", entry.UID);
                    }
                }
            });

            layout.AddCategory(1218, () =>
            {
                foreach (Artifact artifact in empire.data.OwnedArtifacts)
                    layout.AddSubItem(Localizer.Token(artifact.NameIndex), "Artifacts", artifact.Name);
            });

            layout.AddCategory(1219, () =>
            {
                foreach (Planet p in empire.GetPlanets())
                    layout.AddSubItem(p.Name, "Colony", p.Name);
            });
        }

        private void CreateOurOffer()
        {
            OurOffer = new Offer();
            FillItems(playerEmpire, them, OurItemsSL, UsRect);
        }

        private void CreateTheirOffer()
        {
            TheirOffer = new Offer { Them = them };
            FillItems(them, playerEmpire, TheirItemsSL, ThemRect);
        }

        public string GetDialogue(float attitude)
        {
            //string neutral;
            if (playerEmpire.GetRelations(them).AtWar)
            {
                switch (them.GetRelations(playerEmpire).ActiveWar.GetWarScoreState())
                {
                    case WarState.ColdWar:         return GetDialogueByName("Greeting_AtWar");
                    case WarState.LosingBadly:     return GetDialogueByName("AtWar_Losing");
                    case WarState.LosingSlightly:  return GetDialogueByName("AtWar_Losing");
                    case WarState.EvenlyMatched:   return GetDialogueByName("Greeting_AtWar");
                    case WarState.WinningSlightly: return GetDialogueByName("AtWar_Winning");
                    case WarState.Dominating:      return GetDialogueByName("AtWar_Winning");
                    default:                       return GetDialogueByName("Greeting_AtWar");
                }
            }

            foreach (DialogLine dialogLine in them.dd.Dialogs)
            {
                if (dialogLine.DialogType == whichDialogue)
                {
                    if (attitude >= 40.0 && attitude < 60.0)
                        return dialogLine.Neutral;
                    if (attitude >= 60.0)
                        return dialogLine.Friendly;
                    return dialogLine.Hostile;
                }
            }
            return "";
        }

        public string GetDialogueByName(string name)
        {
            var sb = new StringBuilder();
            foreach (DialogLine dl in them.dd.Dialogs)
            {
                if (dl.DialogType != name)
                    continue;

                if (dl.Default.NotEmpty())
                    sb.Append(dl.Default);

                switch (them.data.DiplomaticPersonality.Name ?? "")
                {
                    case "Aggressive": sb.Append(dl.DL_Agg);  break;
                    case "Ruthless":   sb.Append(dl.DL_Ruth); break;
                    case "Honorable":  sb.Append(dl.DL_Hon);  break;
                    case "Xenophobic": sb.Append(dl.DL_Xeno); break;
                    case "Pacifist":   sb.Append(dl.DL_Pac);  break;
                    case "Cunning":    sb.Append(dl.DL_Cunn); break;
                }

                switch (them.data.EconomicPersonality.Name ?? "")
                {
                    case "Expansionists":  sb.Append(dl.DL_Exp);  break;
                    case "Technologists":  sb.Append(dl.DL_Tech); break;
                    case "Militarists":    sb.Append(dl.DL_Mil);  break;
                    case "Industrialists": sb.Append(dl.DL_Ind);  break;
                    case "Generalists":    sb.Append(dl.DL_Gen);  break;
                }
            }
            return sb.ToString();
        }

        public override bool HandleInput(InputState input)
        {
            if (new Rectangle(TrustRect.X - (int)Fonts.Pirulen16.MeasureString("Trust").X, TrustRect.Y, (int)Fonts.Pirulen16.MeasureString("Trust").X + TrustRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(47);
            if (new Rectangle(AngerRect.X - (int)Fonts.Pirulen16.MeasureString("Anger").X, AngerRect.Y, (int)Fonts.Pirulen16.MeasureString("Anger").X + AngerRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(48);
            if (new Rectangle(FearRect.X - (int)Fonts.Pirulen16.MeasureString("Fear").X, FearRect.Y, (int)Fonts.Pirulen16.MeasureString("Fear").X + FearRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(49);
            if (Exit.HandleInput(input) && dState != DialogState.TheirOffer)
            {
                ExitScreen();
                return true;
            }
            if (dState == DialogState.End)
                return false;

            if (dState != DialogState.TheirOffer)
            {
                if (!playerEmpire.GetRelations(them).Treaty_Peace)
                {
                    if (DeclareWar != null && DeclareWar.HandleInput(input))
                    {
                        StatementsSL.Reset();
                        dState = DialogState.End;
                        if (playerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            TheirText = GetDialogueByName("WarDeclared_FeelsBetrayed");
                            playerEmpire.GetEmpireAI().DeclareWarOn(them, WarType.ImperialistWar);
                            them.GetEmpireAI().GetWarDeclaredOnUs(playerEmpire, WarType.ImperialistWar);
                        }
                        else
                        {
                            TheirText = GetDialogueByName("WarDeclared_Generic");
                            playerEmpire.GetEmpireAI().DeclareWarOn(them, WarType.ImperialistWar);
                            them.GetEmpireAI().GetWarDeclaredOnUs(playerEmpire, WarType.ImperialistWar);
                        }
                        playerEmpire.GetEmpireAI().DeclareWarOn(them, WarType.ImperialistWar);
                    }
                }
                else if (DeclareWar != null && DeclareWar.R.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(128);

                if (Discuss != null && Discuss.HandleInput(input))
                {
                    StatementsSL.Reset();
                    dState = DialogState.Discuss;
                    foreach (StatementSet statementSet in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
                    {
                        if (statementSet.Name == "Ordinary Discussion")
                        {
                            int n = 1;
                            Vector2 cursor = TextCursor;
                            foreach (DialogOption opt1 in statementSet.DialogOptions)
                            {
                                string str = opt1.SpecialInquiry.NotEmpty() ? GetDialogueByName(opt1.SpecialInquiry) : opt1.Words;
                                var o2 = new DialogOption(n++, str, cursor, Fonts.Consolas18);
                                o2.Words = parseText(str, (DialogRect.Width - 20), Fonts.Consolas18);
                                o2.Response = opt1.Response;
                                StatementsSL.AddItem(o2);
                                cursor.Y += (Fonts.Consolas18.LineSpacing + 5);
                            }
                        }
                    }
                }
                if (dState == DialogState.Discuss)
                {
                    StatementsSL.HandleInput(input);
                    foreach (DialogOption option in StatementsSL.AllItems<DialogOption>())
                    {
                        if (option.HandleInput(input) != null)
                        {
                            Respond(option);
                            break;
                        }
                    }
                }
                if (dState == DialogState.Negotiate)
                {
                    if ((!TheirOffer.IsBlank() || !OurOffer.IsBlank() || TheirOffer.Alliance) && SendOffer.HandleInput(input))
                    {
                        DoNegotiationResponse(them.GetEmpireAI().AnalyzeOffer(OurOffer, TheirOffer, playerEmpire, Attitude));
                        OurOffer   = new Offer();
                        TheirOffer = new Offer { Them = them };
                    }

                    OfferTextSL.HandleInput(input);
                    OurItemsSL.HandleInput(input);
                    TheirItemsSL.HandleInput(input);

                    HandleItemToOffer(input, OurItemsSL, TheirItemsSL, OurOffer, TheirOffer);
                    HandleItemToOffer(input, TheirItemsSL, OurItemsSL, TheirOffer, OurOffer);

                    if (ap.HandleInput(input))
                    {
                        ap.ToggleOn = true;
                        ar.ToggleOn = false;
                        at.ToggleOn = false;
                        Attitude = Offer.Attitude.Pleading;
                    }
                    if (ar.HandleInput(input))
                    {
                        ar.ToggleOn = true;
                        ap.ToggleOn = false;
                        at.ToggleOn = false;
                        Attitude = Offer.Attitude.Respectful;
                    }
                    if (at.HandleInput(input))
                    {
                        at.ToggleOn = true;
                        ap.ToggleOn = false;
                        ar.ToggleOn = false;
                        Attitude = Offer.Attitude.Threaten;
                    }
                }
                if (Negotiate.HandleInput(input))
                {
                    dState = DialogState.Negotiate;
                    CreateOurOffer();
                    CreateTheirOffer();
                }
            }
            if (dState == DialogState.TheirOffer)
            {
                if (Accept.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null)
                        TheirOffer.ValueToModify.Value = false;
                    if (OurOffer.ValueToModify != null)
                        OurOffer.ValueToModify.Value = true;
                    dState = DialogState.End;
                    TheirText = GetDialogueByName(TheirOffer.AcceptDL);
                    playerEmpire.GetEmpireAI().AcceptOffer(TheirOffer, OurOffer, playerEmpire, them);
                }
                if (Reject.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null)
                        TheirOffer.ValueToModify.Value = true;
                    if (OurOffer.ValueToModify != null)
                        OurOffer.ValueToModify.Value = false;
                    dState = DialogState.End;
                    TheirText = GetDialogueByName(TheirOffer.RejectDL);
                }
            }
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        private void HandleItemToOffer(InputState input, ScrollList ours, ScrollList theirs, Offer ourOffer, Offer theirOffer)
        {
            // Note: ItemToOffer.HandleInput CAN modify OurItemsSL entries
            //       so we need to grab a copy
            ScrollList.Entry[] entries = ours.AllExpandedEntries.ToArray();
            foreach (ScrollList.Entry e in entries)
            {
                var item = (ItemToOffer)e.item;
                string response = item.HandleInput(input, e);
                ProcessResponse(item, response, theirs, ourOffer, theirOffer);
            }
        }

        private static ItemToOffer FindItemToOffer(ScrollList items, string response)
        {
            foreach (ScrollList.Entry entry in items.AllEntries)
                if (entry.TryGet(out ItemToOffer item))
                    if (item.Response == response)
                        return item;
            return null;
        }

        private void ProcessResponse(ItemToOffer item, string response, ScrollList theirs, Offer ourOffer, Offer theirOffer)
        {
            switch (response)
            {
                case "NAPact":
                    ourOffer.NAPact  = !ourOffer.NAPact;
                    theirOffer.NAPact = ourOffer.NAPact;
                    FindItemToOffer(theirs, "NAPact").Selected = item.Selected;
                    return;
                case "We Declare War":
                    ourOffer.NAPact  = !ourOffer.NAPact;
                    theirOffer.NAPact = ourOffer.NAPact;
                    FindItemToOffer(theirs, "NAPact").Selected = item.Selected;
                    return;
                case "Peace Treaty":
                    ourOffer.PeaceTreaty = !ourOffer.PeaceTreaty;
                    theirOffer.PeaceTreaty = ourOffer.PeaceTreaty;
                    FindItemToOffer(theirs, "Peace Treaty").Selected = item.Selected;
                    return;
                case "OfferAlliance":
                    ourOffer.Alliance  = !ourOffer.Alliance;
                    theirOffer.Alliance = ourOffer.Alliance;
                    FindItemToOffer(theirs, "OfferAlliance").Selected = item.Selected;
                    return;
                case "OpenBorders": ourOffer.OpenBorders = !ourOffer.OpenBorders;            return;
                case "Declare War": item.ChangeSpecialInquiry(ourOffer.EmpiresToWarOn);      return;
                case "Tech":        item.ChangeSpecialInquiry(ourOffer.TechnologiesOffered); return;
                case "Artifacts":   item.ChangeSpecialInquiry(ourOffer.ArtifactsOffered);    return;
                case "Colony":      item.ChangeSpecialInquiry(ourOffer.ColoniesOffered);     return;
                case "TradeTreaty":
                    ourOffer.TradeTreaty  = !ourOffer.TradeTreaty;
                    theirOffer.TradeTreaty = ourOffer.TradeTreaty;
                    FindItemToOffer(theirs, "TradeTreaty").Selected = item.Selected;
                    return;
            }
        }

        public override void LoadContent()
        {
            Rectangle prect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 659, 0, 1318, 757);
            BridgeRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
            Player = new Menu2(prect);
            Portrait = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
            Vector2 Cursor = new Vector2(Portrait.X + Portrait.Width - 85, Portrait.Y + 140);
            EmpireNamePos = new Vector2(Cursor.X - Fonts.Pirulen20.MeasureString(them.data.Traits.Name).X, Portrait.Y + 40);
            if (!playerEmpire.GetRelations(them).AtWar)
            {
                DeclareWar = new GenericButton(Cursor, Localizer.Token(1200), Fonts.Pirulen20, Fonts.Pirulen16);
                GenericButtons.Add(DeclareWar);
                Cursor.Y = Cursor.Y + 25f;
                Discuss = new GenericButton(Cursor, Localizer.Token(1201), Fonts.Pirulen20, Fonts.Pirulen16);
                GenericButtons.Add(Discuss);
                Cursor.Y = Cursor.Y + 25f;
            }
            Negotiate = new GenericButton(Cursor, Localizer.Token(1202), Fonts.Pirulen20, Fonts.Pirulen16);
            GenericButtons.Add(Negotiate);
            Cursor.Y = Cursor.Y + 25f;
            Exit = new GenericButton(Cursor, Localizer.Token(1203), Fonts.Pirulen20, Fonts.Pirulen16);
            GenericButtons.Add(Exit);
            Cursor = new Vector2(Portrait.X + 115, Portrait.Y + 160);
            Trust = new GenericButton(Cursor, Localizer.Token(1204), Fonts.Pirulen16, Fonts.Pirulen12)
            {
                ToggleOn = true
            };
            TAFButtons.Add(Trust);
            TrustRect = new Rectangle(Portrait.X + 125, Trust.R.Y + 2, 100, Trust.R.Height);
            Cursor.Y = Cursor.Y + 25f;
            Anger = new GenericButton(Cursor, Localizer.Token(1205), Fonts.Pirulen16, Fonts.Pirulen12)
            {
                ToggleOn = true
            };
            AngerRect = new Rectangle(Portrait.X + 125, Anger.R.Y + 2, 100, Anger.R.Height);
            TAFButtons.Add(Anger);
            Cursor.Y = Cursor.Y + 25f;
            Fear = new GenericButton(Cursor, Localizer.Token(1206), Fonts.Pirulen16, Fonts.Pirulen12)
            {
                ToggleOn = true
            };
            TAFButtons.Add(Fear);
            FearRect = new Rectangle(Portrait.X + 125, Fear.R.Y + 2, 100, Fear.R.Height);
            Cursor.Y = Cursor.Y + 25f;
            DialogRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 350, Portrait.Y + Portrait.Height - 110, 700, 55);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight < 820)
            {
                DialogRect.Y = Portrait.Y + Portrait.Height - 100;
            }
            R = DialogRect;
            R.Height = R.Height + 75;
            if (R.Y + R.Height > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                R.Y = R.Y - (R.Y + R.Height - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight + 2);
            }
            Rectangle blerdybloo = R;
            blerdybloo.Height = blerdybloo.Height - 40;
            Submenu ot = new Submenu(blerdybloo);
            OfferTextSL = new ScrollList(ot, Fonts.Consolas18.LineSpacing + 2, true);
            Attitude_Pleading_Rect = new Rectangle(R.X + 45, R.Y + R.Height - 48, 180, 48);
            Attitude_Respectful_Rect = new Rectangle(R.X + 250 + 5, R.Y + R.Height - 48, 180, 48);
            Attitude_Threaten_Rect = new Rectangle(R.X + 450 + 15, R.Y + R.Height - 48, 180, 48);
            ToneContainerRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 324, Attitude_Pleading_Rect.Y, 648, 48);
            ap = new GenericButton(Attitude_Pleading_Rect, Localizer.Token(1207), Fonts.Pirulen12);
            ar = new GenericButton(Attitude_Respectful_Rect, Localizer.Token(1209), Fonts.Pirulen12)
            {
                ToggleOn = true
            };
            at = new GenericButton(Attitude_Threaten_Rect, Localizer.Token(1208), Fonts.Pirulen12);
            AccRejRect = new Rectangle(R.X + R.Width / 2 - 220, R.Y + R.Height - 48, 440, 48);
            Accept = new GenericButton(new Rectangle(AccRejRect.X, AccRejRect.Y, 220, 48), Localizer.Token(1210), Fonts.Pirulen12);
            Reject = new GenericButton(new Rectangle(AccRejRect.X + 220, AccRejRect.Y, 220, 48), Localizer.Token(1211), Fonts.Pirulen12);
            //Negotiate_Right = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 192, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
            Negotiate_Right = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 242, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
            Negotiate_Left = new Rectangle(0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
            BigTradeRect = new Rectangle(DialogRect.X + 75, DialogRect.Y - 202, DialogRect.Width - 150, 200);
            UsRect = new Rectangle(Negotiate_Right.X + 20, Negotiate_Right.Y + 35, BigTradeRect.Width / 2 - 9, 300);
            ThemRect = new Rectangle(Negotiate_Left.X + 15, Negotiate_Left.Y + 35, BigTradeRect.Width / 2 - 10, 300);
            SendOffer = new GenericButton(new Rectangle(R.X + R.Width / 2 - 90, R.Y - 40, 180, 33), Localizer.Token(1212), Fonts.Pirulen20);
            var themsub = new Submenu(ThemRect);
            TheirItemsSL = new ScrollList(themsub, Fonts.Consolas18.LineSpacing + 5, true);
            var ussub = new Submenu(UsRect);
            OurItemsSL = new ScrollList(ussub, Fonts.Consolas18.LineSpacing + 5, true);
            var sub = new Submenu(blerdybloo);
            StatementsSL = new ScrollList(sub, Fonts.Consolas18.LineSpacing + 2, true);

            PlayRaceVideoAndMusic();

            TextCursor = new Vector2(DialogRect.X + 5, DialogRect.Y + 5);
        }

        private string parseText(string text, float Width, SpriteFont font)
        {
            Width = Width - 5f;
            if (text == null)
            {
                return string.Concat("Debug info: Error. Expected ", whichDialogue);
            }
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
                else if (wordArray[i] == "SING, ")
                {
                    wordArray[i] = string.Concat(playerEmpire.data.Traits.Singular, ", ");
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
                if (wordArray[i] == "TECH_DEMAND")
                {
                    wordArray[i] = Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex);
                }
                else if (wordArray[i] == "TECH_DEMAND.")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), ".");
                }
                else if (wordArray[i] == "TECH_DEMAND,")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), ",");
                }
                else if (wordArray[i] == "TECH_DEMAND?")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), "?");
                }
                else if (wordArray[i] == "TECH_DEMAND!")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), "!");
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

        private string parseTextToSL(string text, float Width, SpriteFont font, ref ScrollList List)
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
                if (wordArray[i] == "TARSYS")
                {
                    wordArray[i] = empToDiscuss.data.Traits.Name;
                }
                else if (wordArray[i] == "TARSYS.")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, ".");
                }
                else if (wordArray[i] == "TARSYS,")
                {
                    wordArray[i] = string.Concat(empToDiscuss.data.Traits.Name, ",");
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
                if (wordArray[i] == "TECH_DEMAND")
                {
                    wordArray[i] = Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex);
                }
                else if (wordArray[i] == "TECH_DEMAND.")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), ".");
                }
                else if (wordArray[i] == "TECH_DEMAND,")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), ",");
                }
                else if (wordArray[i] == "TECH_DEMAND?")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), "?");
                }
                else if (wordArray[i] == "TECH_DEMAND!")
                {
                    wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[OurOffer.TechnologiesOffered[0]].NameIndex), "!");
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
            string[] lineArray = returnString.Split('\n');
            for (int i = 0; i < lineArray.Length; i++)
            {
                string sent = lineArray[i];
                if (sent.Length > 0)
                {
                    OfferTextSL.AddItem(sent);
                }
                else if (string.IsNullOrEmpty(sent) && lineArray.Length > i + 1 && !string.IsNullOrEmpty(lineArray[i + 1]))
                {
                    OfferTextSL.AddItem("\n");
                }
            }
            OfferTextSL.AddItem(line);
            return string.Concat(returnString, line);
        }

        private void Respond(DialogOption resp)
        {
            string Name = resp.Response;
            if (resp.Target is Empire)
                empToDiscuss = resp.Target as Empire;
            switch (Name)
            {
                case "Target_Opinion":
                    if (empToDiscuss == null)
                        break;
                    StatementsSL.Reset();
                    float strength = playerEmpire.GetRelations(them).GetStrength();
                    if (strength >= 65.0)
                        TheirText = GetDialogueByName("Opinion_Positive_" + empToDiscuss.data.Traits.ShipType);
                    else if (strength < 65.0 && strength >= 40.0)
                        TheirText = GetDialogueByName("Opinion_Neutral_" + empToDiscuss.data.Traits.ShipType);
                    else if (strength < 40.0)
                        TheirText = GetDialogueByName("Opinion_Negative_" + empToDiscuss.data.Traits.ShipType);
                    dState = DialogState.Them;
                    break;
                case "EmpireDiscuss":
                    foreach (StatementSet set in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
                    {
                        if (set.Name == "EmpireDiscuss")
                        {
                            StatementsSL.Reset();
                            int n = 1;
                            Vector2 Cursor = TextCursor;
                            foreach (DialogOption dialogOption1 in set.DialogOptions)
                            {
                                DialogOption dialogOption2 = new DialogOption(n, dialogOption1.Words, Cursor, Fonts.Consolas18);
                                dialogOption2.Words = parseText(dialogOption1.Words, DialogRect.Width - 20, Fonts.Consolas18);
                                StatementsSL.AddItem(dialogOption2);
                                dialogOption2.Response = dialogOption1.Response;
                                dialogOption2.Target = empToDiscuss;
                                Cursor.Y += Fonts.Consolas18.LineSpacing + 5;
                                ++n;
                            }
                        }
                    }
                    break;
                case "Hardcoded_EmpireChoose":
                    StatementsSL.Reset();
                    Vector2 cursor1 = TextCursor;
                    int n1 = 1;
                    foreach (KeyValuePair<Empire, Relationship> keyValuePair in them.AllRelations)
                    {
                        if (keyValuePair.Value.Known && !keyValuePair.Key.isFaction && (keyValuePair.Key != playerEmpire && !keyValuePair.Key.data.Defeated) && playerEmpire.GetRelations(keyValuePair.Key).Known)
                        {
                            DialogOption dialogOption = new DialogOption(n1, Localizer.Token(2220) + " " + keyValuePair.Key.data.Traits.Name, cursor1, Fonts.Consolas18);
                            dialogOption.Target = keyValuePair.Key;
                            dialogOption.Words = parseText(dialogOption.Words, DialogRect.Width - 20, Fonts.Consolas18);
                            dialogOption.Response = "EmpireDiscuss";
                            cursor1.Y += Fonts.Consolas18.LineSpacing + 5;
                            StatementsSL.AddItem(dialogOption);
                            ++n1;
                        }
                    }
                    if (StatementsSL.NumEntries != 0)
                        break;
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName("Dunno_Anybody");
                    dState = DialogState.Them;
                    break;
                case "Hardcoded_War_Analysis":
                    TheirText = "";
                    dState = DialogState.Them;
                    if (empToDiscuss == null)
                        break;
                    if (!playerEmpire.GetRelations(empToDiscuss).AtWar)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_YouAreNotAtWar");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (playerEmpire.GetRelations(them).AtWar)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_WeAreAtWar");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (them.GetRelations(playerEmpire).Treaty_Alliance)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Allied_OK");
                        diplomacyScreen.TheirText = str;
                        them.GetEmpireAI().DeclareWarOn(empToDiscuss, WarType.ImperialistWar);
                        empToDiscuss.GetEmpireAI().GetWarDeclaredOnUs(them, WarType.ImperialistWar);
                        break;
                    }
                    else if (them.GetRelations(playerEmpire).GetStrength() < 30.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Reject_PoorRelations");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (them.data.DiplomaticPersonality.Name == "Pacifist" || them.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Reject_Pacifist");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (playerEmpire.GetRelations(them).GetStrength() > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Allied_DECLINE");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (them.GetRelations(playerEmpire).GetStrength() > 60.0 && empToDiscuss.MilitaryScore < (double)them.MilitaryScore)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_OK");
                        diplomacyScreen.TheirText = str;
                        them.GetEmpireAI().DeclareWarOn(empToDiscuss, WarType.ImperialistWar);
                        empToDiscuss.GetEmpireAI().GetWarDeclaredOnUs(them, WarType.ImperialistWar);
                        break;
                    }
                    else
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Reject_TooDangerous");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                case "Hardcoded_Federation_Analysis":
                    StatementsSL.Reset();
                    TheirText = "";
                    dState = DialogState.Them;
                    if (!them.GetRelations(playerEmpire).Treaty_Alliance)
                    {
                        if (them.GetRelations(playerEmpire).TurnsKnown < 50)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_JustMet");
                            diplomacyScreen.TheirText = str;
                            break;
                        }

                        if (them.GetRelations(playerEmpire).GetStrength() >= 75.0)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_NoAlliance");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_RelationsPoor");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                    }
                    else if (them.GetRelations(playerEmpire).TurnsAllied < 100)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_AllianceTooYoung");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else
                    {
                        if (them.GetRelations(playerEmpire).TurnsAllied < 100)
                            break;
                        if (them.TotalScore > playerEmpire.TotalScore * 0.800000011920929 && them.GetRelations(playerEmpire).Threat < 0.0)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_WeAreTooStrong");
                            diplomacyScreen.TheirText = str;
                            break;
                        }

                        Array<Empire> warTargets = new Array<Empire>();
                        Array<Empire> list2 = new Array<Empire>();
                        foreach (KeyValuePair<Empire, Relationship> keyValuePair in them.AllRelations)
                        {
                            if (!keyValuePair.Key.isFaction && keyValuePair.Value.AtWar)
                                warTargets.Add(keyValuePair.Key);

                            if (!keyValuePair.Key.isFaction && keyValuePair.Value.GetStrength() > 75.0 &&
                                playerEmpire.TryGetRelations(keyValuePair.Key, out Relationship relations) && relations.AtWar)
                                list2.Add(keyValuePair.Key);
                        }
                        if (warTargets.Count > 0)
                        {
                            IOrderedEnumerable<Empire> orderedEnumerable = warTargets.OrderByDescending(emp => emp.TotalScore);
                            if (orderedEnumerable.Count() <= 0)
                                break;
                            empToDiscuss = orderedEnumerable.First();
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_Quest_DestroyEnemy");
                            diplomacyScreen.TheirText = str;
                            them.GetRelations(playerEmpire).FedQuest = new FederationQuest
                            {
                                EnemyName = empToDiscuss.data.Traits.Name
                            };
                            break;
                        }

                        if (list2.Count > 0)
                        {
                            var orderedEnumerable = list2.OrderByDescending(emp => them.GetRelations(emp).GetStrength());
                            if (!orderedEnumerable.Any())
                                break;
                            empToDiscuss = orderedEnumerable.First();
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_Quest_AllyFriend");
                            diplomacyScreen.TheirText = str;
                            them.GetRelations(playerEmpire).FedQuest = new FederationQuest
                            {
                                type = QuestType.AllyFriend,
                                EnemyName = empToDiscuss.data.Traits.Name
                            };
                            break;
                        }
                        else
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_Accept");
                            diplomacyScreen.TheirText = str;
                            playerEmpire.AbsorbEmpire(them);
                            break;
                        }
                    }
                case "Hardcoded_Grievances":
                    StatementsSL.Reset();
                    TheirText = "";
                    float num = them.GetRelations(playerEmpire).GetStrength();
                    if (num < 0.0)
                        num = 0.0f;
                    if (them.GetRelations(playerEmpire).TurnsKnown < 20)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("Opinion_JustMetUs");
                        diplomacyScreen.TheirText = str;
                    }
                    else if (num > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("Opinion_NoProblems");
                        diplomacyScreen.TheirText = str;
                    }
                    else if (them.GetRelations(playerEmpire).WarHistory.Count > 0 && them.GetRelations(playerEmpire).WarHistory[them.GetRelations(playerEmpire).WarHistory.Count - 1].EndStarDate - (double)them.GetRelations(playerEmpire).WarHistory[them.GetRelations(playerEmpire).WarHistory.Count - 1].StartDate < 50.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("PROBLEM_RECENTWAR");
                        diplomacyScreen.TheirText = str;
                    }
                    else if (num >= 0.0)
                    {
                        bool flag = false;
                        if (them.GetRelations(playerEmpire).Anger_TerritorialConflict + (double)them.GetRelations(playerEmpire).Anger_FromShipsInOurBorders > them.data.DiplomaticPersonality.Territorialism / 2)
                        {
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            flag = true;
                            if (them.GetRelations(playerEmpire).Threat > 75.0)
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_Territorial");
                                diplomacyScreen2.TheirText = str2;
                                DiplomacyScreen diplomacyScreen3 = this;
                                string str3 = diplomacyScreen3.TheirText + GetDialogueByName("Problem_AlsoMilitary");
                                diplomacyScreen3.TheirText = str3;
                            }
                            else if (them.GetRelations(playerEmpire).Threat < -20.0 && (them.data.DiplomaticPersonality.Name == "Ruthless" || them.data.DiplomaticPersonality.Name == "Aggressive"))
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_Territorial");
                                diplomacyScreen2.TheirText = str2;
                                DiplomacyScreen diplomacyScreen3 = this;
                                string str3 = diplomacyScreen3.TheirText + GetDialogueByName("Problem_AlsoMilitaryWeak");
                                diplomacyScreen3.TheirText = str3;
                            }
                            else
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_JustTerritorial");
                                diplomacyScreen2.TheirText = str2;
                            }
                        }
                        else if (them.GetRelations(playerEmpire).Threat > 75.0)
                        {
                            flag = true;
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            DiplomacyScreen diplomacyScreen2 = this;
                            string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_PrimaryMilitary");
                            diplomacyScreen2.TheirText = str2;
                        }
                        else if (them.GetRelations(playerEmpire).Threat < -20.0 && (them.data.DiplomaticPersonality.Name == "Ruthless" || them.data.DiplomaticPersonality.Name == "Aggressive"))
                        {
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            DiplomacyScreen diplomacyScreen2 = this;
                            string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_MilitaryWeak");
                            diplomacyScreen2.TheirText = str2;
                        }
                        if (!flag)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Opinion_NothingMajor");
                            diplomacyScreen.TheirText = str;
                        }
                    }
                    dState = DialogState.Them;
                    break;
                default:
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName(Name);
                    dState = DialogState.Them;
                    break;
            }
        }

        void PlayRaceVideoAndMusic()
        {
            if (RacialVideo == null)
                RacialVideo = new ScreenMediaPlayer(TransientContent);
            RacialVideo.PlayVideoAndMusic(them, WarDeclared);
            RacialVideo.Rect = Portrait;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            RacialVideo.Update(this);

            if (Discuss != null) Discuss.ToggleOn = dState == DialogState.Discuss;
            Negotiate.ToggleOn = dState == DialogState.Negotiate;

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        protected override void Destroy()
        {
            RacialVideo.Dispose();
            base.Destroy();
        }

        private enum DialogState
        {
            Them,
            Choosing,
            Discuss,
            Negotiate,
            TheirOffer,
            End
        }

        private enum TradeProposals
        {
            TradePact,
            NAPact,
            Peace,
            OpenBorders
        }
    }
}
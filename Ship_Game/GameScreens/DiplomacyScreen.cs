using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private DiplomacyScreen.DialogState dState;

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

        //private int cNum;

        private string TheirText;

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which) : base(parent)
        {
            float TheirOpinionOfUs;            
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            base.IsPopup = true;
            string str = which;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "Conquered_Player":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Declare War Imperialism":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Imperialism Break NA":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense BrokenNA":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Compliment Military":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Compliment Military Better":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Insult Military":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Declare War BC":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
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
                        base.TransitionOnTime = TimeSpan.FromSeconds(1);
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
                base.TransitionOnTime = TimeSpan.FromSeconds(1);
                return;
            }
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, bool EndOnly) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            base.IsPopup = true;
            TheirText = GetDialogueByName(which);
            dState = DiplomacyScreen.DialogState.End;
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, Offer ourOffer, Offer theirOffer) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            base.IsPopup = true;
            dState = DiplomacyScreen.DialogState.TheirOffer;
            TheirText = GetDialogueByName(which);
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, Offer ourOffer, Offer theirOffer, Empire taremp) : base(parent)
        {
            e.GetRelations(us).turnsSinceLastContact = 0;
            them = e;
            playerEmpire = us;
            empToDiscuss = taremp;
            whichDialogue = which;
            base.IsPopup = true;
            dState = DiplomacyScreen.DialogState.TheirOffer;
            TheirText = GetDialogueByName(which);
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
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
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
            string str = which;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Declare War Defense")
                {
                    TheirText = GetDialogueByName(which);
                    dState = DiplomacyScreen.DialogState.End;
                    WarDeclared = true;
                    base.TransitionOnTime = TimeSpan.FromSeconds(1);
                    return;
                }
                else if (str1 == "Declare War BC")
                {
                    TheirText = GetDialogueByName(which);
                    dState = DiplomacyScreen.DialogState.End;
                    WarDeclared = true;
                    base.TransitionOnTime = TimeSpan.FromSeconds(1);
                    return;
                }
                else
                {
                    if (str1 != "Declare War BC TarSys")
                    {
                        TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
                        if (TheirOpinionOfUs < 0f)
                        {
                            TheirOpinionOfUs = 0f;
                        }
                        TheirText = GetDialogue(TheirOpinionOfUs);
                        base.TransitionOnTime = TimeSpan.FromSeconds(1);
                        return;
                    }
                    TheirText = GetDialogueByName(which);
                    dState = DiplomacyScreen.DialogState.End;
                    WarDeclared = true;
                    base.TransitionOnTime = TimeSpan.FromSeconds(1);
                    return;
                }
            }
            TheirOpinionOfUs = them.GetRelations(playerEmpire).GetStrength();
            if (TheirOpinionOfUs < 0f)
            {
                TheirOpinionOfUs = 0f;
            }
            TheirText = GetDialogue(TheirOpinionOfUs);
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
        }

        public DiplomacyScreen(GameScreen parent, Empire e, Empire us, string which, SolarSystem s) : base(parent)
        {
            float TheirOpinionOfUs;
            e.GetRelations(us).turnsSinceLastContact = 0;
            sysToDiscuss = s;
            them = e;
            playerEmpire = us;
            whichDialogue = which;
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
            string str = which;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "Invaded NA Pact":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Invaded Start War":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War Defense":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War BC":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Declare War BC TarSys":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        WarDeclared = true;
                        break;
                    }
                    case "Stole Claim":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Stole Claim 2":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
                        break;
                    }
                    case "Stole Claim 3":
                    {
                        TheirText = GetDialogueByName(which);
                        dState = DiplomacyScreen.DialogState.End;
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
                        base.TransitionOnTime = TimeSpan.FromSeconds(1);
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
                base.TransitionOnTime = TimeSpan.FromSeconds(1);
                return;
            }
            base.TransitionOnTime = TimeSpan.FromSeconds(1);
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
            dState = DiplomacyScreen.DialogState.Them;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            string text;
            Vector2 Position;
            Vector2 drawCurs;
            if (!base.IsActive)
            {
                return;
            }
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 4 / 5);
            base.ScreenManager.SpriteBatch.Begin();
            if (!string.IsNullOrEmpty(them.data.Traits.VideoPath))
            {
                if (VideoPlaying.State != MediaState.Stopped)
                {
                    VideoTexture = VideoPlaying.GetTexture();
                }
                if (VideoTexture != null)
                {
                    Color color = Color.White;
                    if (WarDeclared || playerEmpire.GetRelations(them).AtWar)
                    {
                        color.B = 100;
                        color.G = 100;
                    }
                    base.ScreenManager.SpriteBatch.Draw(VideoTexture, Portrait,null, color);
                }
            }
            else
            {
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", them.PortraitName)], Portrait, Color.White);
            }
            HelperFunctions.DrawDropShadowText(base.ScreenManager, them.data.Traits.Name, EmpireNamePos, Fonts.Pirulen20);
            if (dState == DiplomacyScreen.DialogState.Negotiate)
            {
                Rectangle stripe = new Rectangle(0, R.Y, 1920, R.Height);
                base.ScreenManager.SpriteBatch.FillRectangle(stripe, new Color(0, 0, 0, 150));
            }
            else
            {
                Rectangle stripe = new Rectangle(0, DialogRect.Y, 1920, R.Height);
                base.ScreenManager.SpriteBatch.FillRectangle(stripe, new Color(0, 0, 0, 150));
            }
            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Bridge"], BridgeRect, Color.White);
            foreach (GenericButton taf in TAFButtons)
            {
                taf.DrawWithShadowCaps(base.ScreenManager);
                TrustRect.Width = (int)them.GetRelations(playerEmpire).Trust;
                if (TrustRect.Width < 1)
                {
                    TrustRect.Width = 1;
                }
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], TrustRect, Color.Green);
                AngerRect.Width = (int)them.GetRelations(playerEmpire).TotalAnger;
                if (AngerRect.Width > 100)
                {
                    AngerRect.Width = 100;
                }
                if (AngerRect.Width < 1)
                {
                    AngerRect.Width = 1;
                }
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], AngerRect, Color.Yellow);
                FearRect.Width = (int)them.GetRelations(playerEmpire).Threat;
                if (FearRect.Width < 1)
                {
                    FearRect.Width = 1;
                }
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], FearRect, Color.Red);
            }
            switch (dState)
            {
                case DiplomacyScreen.DialogState.Them:
                {
                    var selector = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    text = parseText(TheirText, (DialogRect.Width - 25), Fonts.Consolas18);
                    Position = new Vector2((ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    HelperFunctions.ClampVectorToInt(ref Position);
                    DrawDropShadowText(text, Position, Fonts.Consolas18);
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
                case DiplomacyScreen.DialogState.Choosing:
                {
                    if (dState == DiplomacyScreen.DialogState.End || dState == DiplomacyScreen.DialogState.TheirOffer)
                    {
                        Exit.DrawWithShadowCaps(ScreenManager);
                    }
                    else
                    {
                        int numEntries = 4;
                        int k = 4;
                        foreach (GenericButton b in GenericButtons)
                        {
                            Rectangle r = b.R;
                            float transitionOffset = MathHelper.Clamp((TransitionPosition - 0.5f * (float)k / (float)numEntries) / 0.5f, 0f, 1f);
                            k--;
                            if (ScreenState != Ship_Game.ScreenState.TransitionOn)
                            {
                                r.X = r.X + (int)transitionOffset * 512;
                            }
                            else
                            {
                                r.X = r.X + (int)(transitionOffset * 512f);
                            }
                            b.TransitionCaps(r);
                            b.DrawWithShadowCaps(ScreenManager);
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
                    base.ScreenManager.SpriteBatch.End();
                    return;
                }
                case DiplomacyScreen.DialogState.Discuss:
                {
                    var selector1 = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    StatementsSL.Draw(ScreenManager.SpriteBatch);
                    drawCurs = TextCursor;
                    foreach (ScrollList.Entry e in StatementsSL.VisibleEntries)
                    {
                        if (e.clickRectHover == 0 && e.item is DialogOption option)
                        {
                            option.Update(drawCurs);
                            option.Draw(ScreenManager, Fonts.Consolas18);
                            drawCurs.Y += (Fonts.Consolas18.LineSpacing + 5);
                        }
                    }
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
                case DiplomacyScreen.DialogState.Negotiate:
                {
                    drawCurs = new Vector2((R.X + 15), (R.Y + 10));
                    TheirOffer.Them = them;
                    string txt = OurOffer.FormulateOfferText(Attitude, TheirOffer);
                    OfferTextSL.Reset();
                    HelperFunctions.parseTextToSL(txt, (DialogRect.Width - 30), Fonts.Consolas18, ref OfferTextSL);
                    foreach (ScrollList.Entry e in OfferTextSL.VisibleEntries)
                    {
                        drawCurs.Y = (e.clickRect.Y - 33);
                        DrawDropShadowText(e.item as string, drawCurs, Fonts.Consolas18);
                    }
                    if (!TheirOffer.IsBlank() || !OurOffer.IsBlank() || OurOffer.Alliance)
                    {
                        SendOffer.DrawWithShadow(ScreenManager);
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Right"], Negotiate_Right, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Left"], Negotiate_Left, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Tone"], ToneContainerRect, Color.White);
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
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
                case DiplomacyScreen.DialogState.TheirOffer:
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/AcceptReject"], AccRejRect, Color.White);
                    text = parseText(TheirText, (float)(DialogRect.Width - 20), Fonts.Consolas18);
                    Position = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    DrawDropShadowText(text, Position, Fonts.Consolas18);
                    Accept.DrawWithShadow(base.ScreenManager);
                    Reject.DrawWithShadow(base.ScreenManager);
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
                case DiplomacyScreen.DialogState.End:
                {
                    Selector selector2 = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    text = parseText(TheirText, (float)(DialogRect.Width - 20), Fonts.Consolas18);
                    Position = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    HelperFunctions.ClampVectorToInt(ref Position);
                    DrawDropShadowText(text, Position, Fonts.Consolas18);
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
                default:
                {
                    goto case DiplomacyScreen.DialogState.Choosing;
                }
            }
        }

        private void DrawDropShadowText(string Text, Vector2 Pos, SpriteFont Font)
        {
            Vector2 offset = new Vector2(2f, 2f);
            base.ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
            base.ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, Color.White);
        }

        private void DrawOurItems()
        {
            OurItemsSL.Draw(base.ScreenManager.SpriteBatch);
            var drawCurs = new Vector2((UsRect.X + 10), (UsRect.Y + Fonts.Pirulen12.LineSpacing + 10));
            foreach (ScrollList.Entry e in OurItemsSL.VisibleExpandedEntries)
            {
                if (e.clickRectHover == 0 && e.item is ItemToOffer item)
                {
                    item.Update(drawCurs);
                    item.Draw(base.ScreenManager.SpriteBatch, Fonts.Arial12Bold);
                    drawCurs.Y = drawCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
        }

        private void DrawSpecialText1612(string Text, Vector2 Pos)
        {
            Vector2 offset = new Vector2(2f, 2f);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos + offset, Color.Black);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos, Color.White);
        }

        private void DrawTheirItems()
        {
            TheirItemsSL.Draw(base.ScreenManager.SpriteBatch);
            Vector2 drawCurs = new Vector2((float)(ThemRect.X + 10), (float)(ThemRect.Y + Fonts.Pirulen12.LineSpacing + 10));
            foreach (ScrollList.Entry e in TheirItemsSL.VisibleExpandedEntries)
            {
                if (e.clickRectHover == 0 && e.item is ItemToOffer item)
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

        private void FillOurItems()
        {
            ItemToOffer item;
            OurItemsSL.Reset();
            Vector2 newCurs = new Vector2((float)(UsRect.X + 10), (float)(UsRect.Y + Fonts.Pirulen12.LineSpacing + 2));
            if (!playerEmpire.GetRelations(them).AtWar)
            {
                if (!playerEmpire.GetRelations(them).Treaty_NAPact)
                {
                    item = new ItemToOffer(Localizer.Token(1214), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "NAPact"
                    };
                    OurItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (!playerEmpire.GetRelations(them).Treaty_Trade)
                {
                    item = new ItemToOffer(Localizer.Token(1215), newCurs, Fonts.Arial12Bold);
                    OurItemsSL.AddItem(item);
                    item.Response = "TradeTreaty";
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (!them.GetRelations(playerEmpire).Treaty_OpenBorders)
                {
                    item = new ItemToOffer(Localizer.Token(1216), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "OpenBorders"
                    };
                    OurItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (playerEmpire.GetRelations(them).Treaty_Trade && playerEmpire.GetRelations(them).Treaty_NAPact && !playerEmpire.GetRelations(them).Treaty_Alliance)
                {
                    item = new ItemToOffer(Localizer.Token(2045), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "OfferAlliance"
                    };
                    OurItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
            else
            {
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(1213), newCurs, Fonts.Arial12Bold)
                {
                    Response = "Peace Treaty"
                };
                OurItemsSL.AddItem(item1);
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            item = new ItemToOffer(Localizer.Token(1217), newCurs, Fonts.Arial12Bold);
            ScrollList.Entry e = OurItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (KeyValuePair<string, TechEntry> Technology in playerEmpire.GetTDict())
            {
                //Added by McShooterz: prevent root nodes from being traded
                if (!Technology.Value.Unlocked || them.GetTDict()[Technology.Key].Unlocked || !them.HavePreReq(Technology.Key) || Technology.Value.Tech.RootNode == 1)
                {
                    continue;
                }
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(ResourceManager.TechTree[Technology.Key].NameIndex), newCurs, Fonts.Arial12Bold);
                item1.words += ": " + (int)ResourceManager.TechTree[Technology.Key].Cost;
                e.AddItem(item1);
                item1.Response = "Tech";
                item1.SpecialInquiry = Technology.Key;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
            item = new ItemToOffer(Localizer.Token(1218), newCurs, Fonts.Arial12Bold);
            e = OurItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (Ship_Game.Artifact Artifact in playerEmpire.data.OwnedArtifacts)
            {
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(Artifact.NameIndex), newCurs, Fonts.Arial12Bold);
                e.AddItem(item1);
                item1.Response = "Artifacts";
                item1.SpecialInquiry = Artifact.Name;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
            item = new ItemToOffer(Localizer.Token(1219), newCurs, Fonts.Arial12Bold);
            e = OurItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (Planet p in playerEmpire.GetPlanets())
            {
                ItemToOffer item1 = new ItemToOffer(p.Name, newCurs, Fonts.Arial12Bold);
                e.AddItem(item1);
                item1.Response = "Colony";
                item1.SpecialInquiry = p.Name;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
        }

        private void FillTheirItems()
        {
            ItemToOffer item;
            TheirItemsSL.Reset();
            Vector2 newCurs = new Vector2((float)(ThemRect.X + 10), (float)(ThemRect.Y + Fonts.Pirulen12.LineSpacing + 2));
            if (!playerEmpire.GetRelations(them).AtWar)
            {
                if (!playerEmpire.GetRelations(them).Treaty_NAPact)
                {
                    item = new ItemToOffer(Localizer.Token(1214), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "NAPact"
                    };
                    TheirItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (!playerEmpire.GetRelations(them).Treaty_Trade)
                {
                    item = new ItemToOffer(Localizer.Token(1215), newCurs, Fonts.Arial12Bold);
                    TheirItemsSL.AddItem(item);
                    item.Response = "TradeTreaty";
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (!playerEmpire.GetRelations(them).Treaty_OpenBorders)
                {
                    item = new ItemToOffer(Localizer.Token(1216), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "OpenBorders"
                    };
                    TheirItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
                if (playerEmpire.GetRelations(them).Treaty_Trade && playerEmpire.GetRelations(them).Treaty_NAPact && !playerEmpire.GetRelations(them).Treaty_Alliance)
                {
                    item = new ItemToOffer(Localizer.Token(2045), newCurs, Fonts.Arial12Bold)
                    {
                        Response = "OfferAlliance"
                    };
                    TheirItemsSL.AddItem(item);
                    newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
            else
            {
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(1213), newCurs, Fonts.Arial12Bold);
                TheirItemsSL.AddItem(item1);
                item1.Response = "Peace Treaty";
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            item = new ItemToOffer(Localizer.Token(1217), newCurs, Fonts.Arial12Bold);
            ScrollList.Entry e = TheirItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (KeyValuePair<string, TechEntry> Technology in them.GetTDict())
            {
                //added by McShooterz: Prevents Racial techs from being traded
                if (!Technology.Value.Unlocked || playerEmpire.GetTDict()[Technology.Key].Unlocked || !playerEmpire.HavePreReq(Technology.Key) || Technology.Value.Tech.RootNode == 1)
                {
                    continue;
                }
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(ResourceManager.TechTree[Technology.Key].NameIndex), newCurs, Fonts.Arial12Bold);
                item1.words += ": " + (int)ResourceManager.TechTree[Technology.Key].Cost;
                e.AddItem(item1);
                item1.Response = "Tech";
                item1.SpecialInquiry = Technology.Key;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
            item = new ItemToOffer(Localizer.Token(1218), newCurs, Fonts.Arial12Bold);
            e = TheirItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (Ship_Game.Artifact Artifact in them.data.OwnedArtifacts)
            {
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(Artifact.NameIndex), newCurs, Fonts.Arial12Bold);
                e.AddItem(item1);
                item1.Response = "Artifacts";
                item1.SpecialInquiry = Artifact.Name;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
            item = new ItemToOffer(Localizer.Token(1219), newCurs, Fonts.Arial12Bold);
            e = TheirItemsSL.AddItem(item);
            newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            newCurs.X = newCurs.X + 10f;
            foreach (Planet p in them.GetPlanets())
            {
                ItemToOffer item1 = new ItemToOffer(p.Name, newCurs, Fonts.Arial12Bold);
                e.AddItem(item1);
                item1.Response = "Colony";
                item1.SpecialInquiry = p.Name;
                newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
            }
            newCurs.X = newCurs.X - 10f;
        }

        /*protected override void Finalize()
        {
            try
            {
                Dispose(false);
            }
            finally
            {
                base.Finalize();
            }
        }*/

   

        public string GetDialogue(float Attitude)
        {
            //string neutral;
            if (playerEmpire.GetRelations(them).AtWar)
            {
                switch (them.GetRelations(playerEmpire).ActiveWar.GetWarScoreState())
                {
                    case WarState.ColdWar:
                    {
                        return GetDialogueByName("Greeting_AtWar");
                    }
                    case WarState.LosingBadly:
                    {
                        return GetDialogueByName("AtWar_Losing");
                    }
                    case WarState.LosingSlightly:
                    {
                        return GetDialogueByName("AtWar_Losing");
                    }
                    case WarState.EvenlyMatched:
                    {
                        return GetDialogueByName("Greeting_AtWar");
                    }
                    case WarState.WinningSlightly:
                    {
                        return GetDialogueByName("AtWar_Winning");
                    }
                    case WarState.Dominating:
                    {
                        return GetDialogueByName("AtWar_Winning");
                    }
                }
                return GetDialogueByName("Greeting_AtWar");
            }
            else
            {
                foreach (DialogLine dialogLine in them.dd.Dialogs)
                {
                    if (dialogLine.DialogType == whichDialogue)
                    {
                        if ((double)Attitude >= 40.0 && (double)Attitude < 60.0)
                            return dialogLine.Neutral;
                        if ((double)Attitude >= 60.0)
                            return dialogLine.Friendly;
                        else
                            return dialogLine.Hostile;
                    }
                }
                return "";
            }
        }

        public string GetDialogueByName(string Name)
        {
            string resp = "";
            foreach (DialogLine dl in them.dd.Dialogs)
            {
                if (dl.DialogType != Name)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(dl.Default))
                {
                    resp = string.Concat(resp, dl.Default);
                }
                string name = them.data.DiplomaticPersonality.Name;
                string str = name;
                if (name != null)
                {
                    if (str == "Aggressive")
                    {
                        resp = string.Concat(resp, dl.DL_Agg);
                    }
                    else if (str == "Ruthless")
                    {
                        resp = string.Concat(resp, dl.DL_Ruth);
                    }
                    else if (str == "Honorable")
                    {
                        resp = string.Concat(resp, dl.DL_Hon);
                    }
                    else if (str == "Xenophobic")
                    {
                        resp = string.Concat(resp, dl.DL_Xeno);
                    }
                    else if (str == "Pacifist")
                    {
                        resp = string.Concat(resp, dl.DL_Pac);
                    }
                    else if (str == "Cunning")
                    {
                        resp = string.Concat(resp, dl.DL_Cunn);
                    }
                }
                string name1 = them.data.EconomicPersonality.Name;
                string str1 = name1;
                if (name1 == null)
                {
                    continue;
                }
                if (str1 == "Expansionists")
                {
                    resp = string.Concat(resp, dl.DL_Exp);
                }
                else if (str1 == "Technologists")
                {
                    resp = string.Concat(resp, dl.DL_Tech);
                }
                else if (str1 == "Militarists")
                {
                    resp = string.Concat(resp, dl.DL_Mil);
                }
                else if (str1 == "Industrialists")
                {
                    resp = string.Concat(resp, dl.DL_Ind);
                }
                else if (str1 == "Generalists")
                {
                    resp = string.Concat(resp, dl.DL_Gen);
                }
            }
            return resp;
        }

        public override bool HandleInput(InputState input)
        {
            if (new Rectangle(TrustRect.X - (int)Fonts.Pirulen16.MeasureString("Trust").X, TrustRect.Y, (int)Fonts.Pirulen16.MeasureString("Trust").X + TrustRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(47);
            if (new Rectangle(AngerRect.X - (int)Fonts.Pirulen16.MeasureString("Anger").X, AngerRect.Y, (int)Fonts.Pirulen16.MeasureString("Anger").X + AngerRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(48);
            if (new Rectangle(FearRect.X - (int)Fonts.Pirulen16.MeasureString("Fear").X, FearRect.Y, (int)Fonts.Pirulen16.MeasureString("Fear").X + FearRect.Width, 14).HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(49);
            if (Exit.HandleInput(input) && dState != DiplomacyScreen.DialogState.TheirOffer)
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
                        dState = DiplomacyScreen.DialogState.End;
                        if (playerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            TheirText = GetDialogueByName("WarDeclared_FeelsBetrayed");
                            playerEmpire.GetGSAI().DeclareWarOn(them, WarType.ImperialistWar);
                            them.GetGSAI().GetWarDeclaredOnUs(playerEmpire, WarType.ImperialistWar);
                        }
                        else
                        {
                            TheirText = GetDialogueByName("WarDeclared_Generic");
                            playerEmpire.GetGSAI().DeclareWarOn(them, WarType.ImperialistWar);
                            them.GetGSAI().GetWarDeclaredOnUs(playerEmpire, WarType.ImperialistWar);
                        }
                        playerEmpire.GetGSAI().DeclareWarOn(them, WarType.ImperialistWar);
                    }
                }
                else if (DeclareWar != null && DeclareWar.R.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(128);
                if (Discuss != null && Discuss.HandleInput(input))
                {
                    StatementsSL.Reset();
                    dState = DialogState.Discuss;
                    foreach (StatementSet statementSet in ResourceManager.DDDict["SharedDiplomacy"].StatementSets)
                    {
                        if (statementSet.Name == "Ordinary Discussion")
                        {
                            int n = 1;
                            Vector2 Cursor = TextCursor;
                            foreach (DialogOption dialogOption1 in statementSet.DialogOptions)
                            {
                                string str = dialogOption1.Words;
                                if (!string.IsNullOrEmpty(dialogOption1.SpecialInquiry))
                                    str = GetDialogueByName(dialogOption1.SpecialInquiry);
                                DialogOption dialogOption2 = new DialogOption(n, str, Cursor, Fonts.Consolas18);
                                dialogOption2.Words = parseText(str, (float)(DialogRect.Width - 20), Fonts.Consolas18);
                                StatementsSL.AddItem((object)dialogOption2);
                                dialogOption2.Response = dialogOption1.Response;
                                Cursor.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
                                ++n;
                            }
                        }
                    }
                }
                if (dState == DiplomacyScreen.DialogState.Discuss)
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
                if (dState == DiplomacyScreen.DialogState.Negotiate)
                {
                    if ((!TheirOffer.IsBlank() || !OurOffer.IsBlank() || TheirOffer.Alliance) && SendOffer.HandleInput(input))
                    {
                        DoNegotiationResponse(them.GetGSAI().AnalyzeOffer(OurOffer, TheirOffer, playerEmpire, Attitude));
                        OurOffer = new Offer();
                        TheirOffer = new Offer();
                        TheirOffer.Them = them;
                    }
                    OfferTextSL.HandleInput(input);
                    OurItemsSL.HandleInput(input);
                    foreach (ScrollList.Entry e in OurItemsSL.AllExpandedEntries)
                    {
                        var ourOffer = (ItemToOffer)e.item;
                        switch (ourOffer.HandleInput(input, e))
                        {
                            case "NAPact":
                                OurOffer.NAPact   = !OurOffer.NAPact;
                                TheirOffer.NAPact = OurOffer.NAPact;
                                foreach (ItemToOffer theirOffer in TheirItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (theirOffer.Response == "NAPact")
                                        theirOffer.Selected = ourOffer.Selected;
                                }
                                continue;
                            case "We Declare War":
                                OurOffer.NAPact   = !OurOffer.NAPact;
                                TheirOffer.NAPact = OurOffer.NAPact;
                                foreach (ItemToOffer theirOffer in TheirItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (theirOffer.Response == "NAPact")
                                        theirOffer.Selected = ourOffer.Selected;
                                }
                                continue;
                            case "Peace Treaty":
                                OurOffer.PeaceTreaty   = !OurOffer.PeaceTreaty;
                                TheirOffer.PeaceTreaty = OurOffer.PeaceTreaty;
                                foreach (ItemToOffer theirOffer in TheirItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (theirOffer.Response == "Peace Treaty")
                                        theirOffer.Selected = ourOffer.Selected;
                                }
                                continue;
                            case "OfferAlliance":
                                OurOffer.Alliance   = !OurOffer.Alliance;
                                TheirOffer.Alliance = OurOffer.Alliance;
                                foreach (ItemToOffer theirOffer in TheirItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (theirOffer.Response == "OfferAlliance")
                                        theirOffer.Selected = ourOffer.Selected;
                                }
                                continue;
                            case "OpenBorders":
                                OurOffer.OpenBorders = !OurOffer.OpenBorders;
                                continue;
                            case "Tech":
                                if (ourOffer.Selected)
                                    OurOffer.TechnologiesOffered.Add(ourOffer.SpecialInquiry);
                                else
                                    OurOffer.TechnologiesOffered.Remove(ourOffer.SpecialInquiry);
                                continue;
                            case "Artifacts":
                                if (ourOffer.Selected)
                                    OurOffer.ArtifactsOffered.Add(ourOffer.SpecialInquiry);
                                else
                                    OurOffer.ArtifactsOffered.Remove(ourOffer.SpecialInquiry);
                                continue;
                            case "Colony":
                                if (ourOffer.Selected)
                                    OurOffer.ColoniesOffered.Add(ourOffer.SpecialInquiry);
                                else
                                    OurOffer.ColoniesOffered.Remove(ourOffer.SpecialInquiry);
                                continue;
                            case "TradeTreaty":
                                OurOffer.TradeTreaty   = !OurOffer.TradeTreaty;
                                TheirOffer.TradeTreaty = OurOffer.TradeTreaty;
                                foreach (ScrollList.Entry entry in TheirItemsSL.AllExpandedEntries)
                                {
                                    if ((entry.item as ItemToOffer).Response == "TradeTreaty")
                                        (entry.item as ItemToOffer).Selected = ourOffer.Selected;
                                }
                                continue;
                        }
                    }
                    OurItemsSL.Update();
                    TheirItemsSL.HandleInput(input);
                    foreach (ScrollList.Entry e in TheirItemsSL.AllExpandedEntries)
                    {
                        var theirOffer = (ItemToOffer)e.item;
                        switch (theirOffer.HandleInput(input, e))
                        {
                            case "NAPact":
                                TheirOffer.NAPact = !TheirOffer.NAPact;
                                OurOffer.NAPact   = TheirOffer.NAPact;
                                foreach (ItemToOffer ourOffer in OurItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (ourOffer.Response == "NAPact")
                                        ourOffer.Selected = theirOffer.Selected;
                                }
                                continue;
                            case "Declare War":
                                if (theirOffer.Selected)
                                    TheirOffer.EmpiresToWarOn.Add(theirOffer.SpecialInquiry);
                                else
                                    TheirOffer.EmpiresToWarOn.Remove(theirOffer.SpecialInquiry);
                                continue;
                            case "Peace Treaty":
                                TheirOffer.PeaceTreaty = !TheirOffer.PeaceTreaty;
                                OurOffer.PeaceTreaty   = TheirOffer.PeaceTreaty;
                                foreach (ItemToOffer ourOffer in OurItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (ourOffer.Response == "Peace Treaty")
                                        ourOffer.Selected = theirOffer.Selected;
                                }
                                continue;
                            case "OfferAlliance":
                                TheirOffer.Alliance = !TheirOffer.Alliance;
                                OurOffer.Alliance   = TheirOffer.Alliance;
                                foreach (ItemToOffer ourOffer in OurItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (ourOffer.Response == "OfferAlliance")
                                        ourOffer.Selected = theirOffer.Selected;
                                }
                                continue;
                            case "Colony":
                                if (theirOffer.Selected)
                                    TheirOffer.ColoniesOffered.Add(theirOffer.SpecialInquiry);
                                else
                                    TheirOffer.ColoniesOffered.Remove(theirOffer.SpecialInquiry);
                                continue;
                            case "Tech":
                                if (theirOffer.Selected)
                                    TheirOffer.TechnologiesOffered.Add(theirOffer.SpecialInquiry);
                                else
                                    TheirOffer.TechnologiesOffered.Remove(theirOffer.SpecialInquiry);
                                continue;
                            case "Artifacts":
                                if (theirOffer.Selected)
                                    TheirOffer.ArtifactsOffered.Add(theirOffer.SpecialInquiry);
                                else
                                    TheirOffer.ArtifactsOffered.Remove(theirOffer.SpecialInquiry);
                                continue;
                            case "OpenBorders":
                                TheirOffer.OpenBorders = !TheirOffer.OpenBorders;
                                continue;
                            case "TradeTreaty":
                                TheirOffer.TradeTreaty = !TheirOffer.TradeTreaty;
                                OurOffer.TradeTreaty   = TheirOffer.TradeTreaty;
                                foreach (ItemToOffer ourOffer in OurItemsSL.VisibleExpandedItems<ItemToOffer>())
                                {
                                    if (ourOffer.Response == "TradeTreaty")
                                        ourOffer.Selected = theirOffer.Selected;
                                }
                                continue;
                            default:
                                continue;
                        }
                    }
                    TheirItemsSL.Update();
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
                    dState = DiplomacyScreen.DialogState.Negotiate;
                    OurOffer = new Offer();
                    TheirOffer = new Offer();
                    TheirOffer.Them = them;
                    FillOurItems();
                    FillTheirItems();
                }
            }
            if (dState == DiplomacyScreen.DialogState.TheirOffer)
            {
                if (Accept.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null)
                        TheirOffer.ValueToModify.Value = false;
                    if (OurOffer.ValueToModify != null)
                        OurOffer.ValueToModify.Value = true;
                    dState = DiplomacyScreen.DialogState.End;
                    TheirText = GetDialogueByName(TheirOffer.AcceptDL);
                    playerEmpire.GetGSAI().AcceptOffer(TheirOffer, OurOffer, playerEmpire, them);
                }
                if (Reject.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null)
                        TheirOffer.ValueToModify.Value = true;
                    if (OurOffer.ValueToModify != null)
                        OurOffer.ValueToModify.Value = false;
                    dState = DiplomacyScreen.DialogState.End;
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

        public override void LoadContent()
        {
            Rectangle prect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 659, 0, 1318, 757);
            BridgeRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
            Player = new Menu2(prect);
            Portrait = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
            Vector2 Cursor = new Vector2((float)(Portrait.X + Portrait.Width - 85), (float)(Portrait.Y + 140));
            EmpireNamePos = new Vector2(Cursor.X - Fonts.Pirulen20.MeasureString(them.data.Traits.Name).X, (float)(Portrait.Y + 40));
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
            Cursor = new Vector2((float)(Portrait.X + 115), (float)(Portrait.Y + 160));
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
            DialogRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 350, Portrait.Y + Portrait.Height - 110, 700, 55);
            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight < 820)
            {
                DialogRect.Y = Portrait.Y + Portrait.Height - 100;
            }
            R = DialogRect;
            R.Height = R.Height + 75;
            if (R.Y + R.Height > base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                R.Y = R.Y - (R.Y + R.Height - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight + 2);
            }
            Rectangle blerdybloo = R;
            blerdybloo.Height = blerdybloo.Height - 40;
            Submenu ot = new Submenu(blerdybloo);
            OfferTextSL = new ScrollList(ot, Fonts.Consolas18.LineSpacing + 2, true);
            Attitude_Pleading_Rect = new Rectangle(R.X + 45, R.Y + R.Height - 48, 180, 48);
            Attitude_Respectful_Rect = new Rectangle(R.X + 250 + 5, R.Y + R.Height - 48, 180, 48);
            Attitude_Threaten_Rect = new Rectangle(R.X + 450 + 15, R.Y + R.Height - 48, 180, 48);
            ToneContainerRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 324, Attitude_Pleading_Rect.Y, 648, 48);
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
            Negotiate_Right = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 242, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
            Negotiate_Left = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
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

            PlayVideo(them.data.Traits.VideoPath);
            GameAudio.PauseGenericMusic();            
            PlayEmpireMusic(them,WarDeclared);                
            
            
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
            string[] wordArray = text.Split(new char[] { ' ' });
            for (int i = 0; i < (int)wordArray.Length; i++)
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
            for (int j = 0; j < (int)strArrays.Length; j++)
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
            string[] wordArray = text.Split(new char[] { ' ' });
            for (int i = 0; i < (int)wordArray.Length; i++)
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
            for (int j = 0; j < (int)strArrays.Length; j++)
            {
                string word = strArrays[j];
                if (font.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            string[] lineArray = returnString.Split(new char[] { '\n' });
            for (int i = 0; i < (int)lineArray.Length; i++)
            {
                string sent = lineArray[i];
                if (sent.Length > 0)
                {
                    OfferTextSL.AddItem(sent);
                }
                else if (string.IsNullOrEmpty(sent) && (int)lineArray.Length > i + 1 && !string.IsNullOrEmpty(lineArray[i + 1]))
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
                    if ((double)strength >= 65.0)
                        TheirText = GetDialogueByName("Opinion_Positive_" + empToDiscuss.data.Traits.ShipType);
                    else if ((double)strength < 65.0 && (double)strength >= 40.0)
                        TheirText = GetDialogueByName("Opinion_Neutral_" + empToDiscuss.data.Traits.ShipType);
                    else if ((double)strength < 40.0)
                        TheirText = GetDialogueByName("Opinion_Negative_" + empToDiscuss.data.Traits.ShipType);
                    dState = DiplomacyScreen.DialogState.Them;
                    break;
                case "EmpireDiscuss":
                    foreach (StatementSet set in ResourceManager.DDDict["SharedDiplomacy"].StatementSets)
                    {
                        if (set.Name == "EmpireDiscuss")
                        {
                            StatementsSL.Reset();
                            int n = 1;
                            Vector2 Cursor = TextCursor;
                            foreach (DialogOption dialogOption1 in set.DialogOptions)
                            {
                                DialogOption dialogOption2 = new DialogOption(n, dialogOption1.Words, Cursor, Fonts.Consolas18);
                                dialogOption2.Words = parseText(dialogOption1.Words, (float)(DialogRect.Width - 20), Fonts.Consolas18);
                                StatementsSL.AddItem((object)dialogOption2);
                                dialogOption2.Response = dialogOption1.Response;
                                dialogOption2.Target = (object)empToDiscuss;
                                Cursor.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
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
                            dialogOption.Target = (object)keyValuePair.Key;
                            dialogOption.Words = parseText(dialogOption.Words, (float)(DialogRect.Width - 20), Fonts.Consolas18);
                            dialogOption.Response = "EmpireDiscuss";
                            cursor1.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
                            StatementsSL.AddItem((object)dialogOption);
                            ++n1;
                        }
                    }
                    if (StatementsSL.NumEntries != 0)
                        break;
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName("Dunno_Anybody");
                    dState = DiplomacyScreen.DialogState.Them;
                    break;
                case "Hardcoded_War_Analysis":
                    TheirText = "";
                    dState = DiplomacyScreen.DialogState.Them;
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
                        them.GetGSAI().DeclareWarOn(empToDiscuss, WarType.ImperialistWar);
                        empToDiscuss.GetGSAI().GetWarDeclaredOnUs(them, WarType.ImperialistWar);
                        break;
                    }
                    else if ((double)them.GetRelations(playerEmpire).GetStrength() < 30.0)
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
                    else if ((double)playerEmpire.GetRelations(them).GetStrength() > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_Allied_DECLINE");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if ((double)them.GetRelations(playerEmpire).GetStrength() > 60.0 && (double)empToDiscuss.MilitaryScore < (double)them.MilitaryScore)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("JoinWar_OK");
                        diplomacyScreen.TheirText = str;
                        them.GetGSAI().DeclareWarOn(empToDiscuss, WarType.ImperialistWar);
                        empToDiscuss.GetGSAI().GetWarDeclaredOnUs(them, WarType.ImperialistWar);
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
                    dState = DiplomacyScreen.DialogState.Them;
                    if (!them.GetRelations(playerEmpire).Treaty_Alliance)
                    {
                        if (them.GetRelations(playerEmpire).TurnsKnown < 50)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_JustMet");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else if ((double)them.GetRelations(playerEmpire).GetStrength() >= 75.0)
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
                        if ((double)them.TotalScore > (double)playerEmpire.TotalScore * 0.800000011920929 && (double)them.GetRelations(playerEmpire).Threat < 0.0)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_WeAreTooStrong");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else
                        {
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
                                IOrderedEnumerable<Empire> orderedEnumerable = ((IEnumerable<Empire>)warTargets).OrderByDescending<Empire, int>((Func<Empire, int>)(emp => emp.TotalScore));
                                if (((IEnumerable<Empire>)orderedEnumerable).Count<Empire>() <= 0)
                                    break;
                                empToDiscuss = ((IEnumerable<Empire>)orderedEnumerable).First<Empire>();
                                DiplomacyScreen diplomacyScreen = this;
                                string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_Quest_DestroyEnemy");
                                diplomacyScreen.TheirText = str;
                                them.GetRelations(playerEmpire).FedQuest = new FederationQuest()
                                {
                                    EnemyName = empToDiscuss.data.Traits.Name
                                };
                                break;
                            }
                            else if (list2.Count > 0)
                            {
                                var orderedEnumerable = list2.OrderByDescending(emp => them.GetRelations(emp).GetStrength());
                                if (!orderedEnumerable.Any())
                                    break;
                                empToDiscuss = orderedEnumerable.First();
                                DiplomacyScreen diplomacyScreen = this;
                                string str = diplomacyScreen.TheirText + GetDialogueByName("Federation_Quest_AllyFriend");
                                diplomacyScreen.TheirText = str;
                                them.GetRelations(playerEmpire).FedQuest = new FederationQuest()
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
                    }
                case "Hardcoded_Grievances":
                    StatementsSL.Reset();
                    TheirText = "";
                    float num = them.GetRelations(playerEmpire).GetStrength();
                    if ((double)num < 0.0)
                        num = 0.0f;
                    if (them.GetRelations(playerEmpire).TurnsKnown < 20)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("Opinion_JustMetUs");
                        diplomacyScreen.TheirText = str;
                    }
                    else if ((double)num > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("Opinion_NoProblems");
                        diplomacyScreen.TheirText = str;
                    }
                    else if (them.GetRelations(playerEmpire).WarHistory.Count > 0 && (double)them.GetRelations(playerEmpire).WarHistory[them.GetRelations(playerEmpire).WarHistory.Count - 1].EndStarDate - (double)them.GetRelations(playerEmpire).WarHistory[them.GetRelations(playerEmpire).WarHistory.Count - 1].StartDate < 50.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + GetDialogueByName("PROBLEM_RECENTWAR");
                        diplomacyScreen.TheirText = str;
                    }
                    else if ((double)num >= 0.0)
                    {
                        bool flag = false;
                        if ((double)them.GetRelations(playerEmpire).Anger_TerritorialConflict + (double)them.GetRelations(playerEmpire).Anger_FromShipsInOurBorders > (double)(them.data.DiplomaticPersonality.Territorialism / 2))
                        {
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            flag = true;
                            if ((double)them.GetRelations(playerEmpire).Threat > 75.0)
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_Territorial");
                                diplomacyScreen2.TheirText = str2;
                                DiplomacyScreen diplomacyScreen3 = this;
                                string str3 = diplomacyScreen3.TheirText + GetDialogueByName("Problem_AlsoMilitary");
                                diplomacyScreen3.TheirText = str3;
                            }
                            else if ((double)them.GetRelations(playerEmpire).Threat < -20.0 && (them.data.DiplomaticPersonality.Name == "Ruthless" || them.data.DiplomaticPersonality.Name == "Aggressive"))
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
                        else if ((double)them.GetRelations(playerEmpire).Threat > 75.0)
                        {
                            flag = true;
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            DiplomacyScreen diplomacyScreen2 = this;
                            string str2 = diplomacyScreen2.TheirText + GetDialogueByName("Problem_PrimaryMilitary");
                            diplomacyScreen2.TheirText = str2;
                        }
                        else if ((double)them.GetRelations(playerEmpire).Threat < -20.0 && (them.data.DiplomaticPersonality.Name == "Ruthless" || them.data.DiplomaticPersonality.Name == "Aggressive"))
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
                    dState = DiplomacyScreen.DialogState.Them;
                    break;
                default:
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName(Name);
                    dState = DiplomacyScreen.DialogState.Them;
                    break;
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (IsActive)
            {
                if (VideoPlaying.State == MediaState.Paused)
                {
                    VideoPlaying.Resume();
                }
                if (!them.data.ModRace)
                {
                    if (MusicPlaying.IsPaused)
                    {
                        MusicPlaying.Resume();
                    }
                    else if (MusicPlaying.IsStopped)
                    {
                        if (them.data.MusicCue.NotEmpty())
                        {
                            MusicPlaying = GameAudio.PlayMusic(WarDeclared ? "Stardrive_Combat 1c_114BPM" : them.data.MusicCue);
                        }
                    }
                }
            }
            else
            {
                if (VideoPlaying.State == MediaState.Playing)
                {
                    VideoPlaying.Pause();
                }
                if (!them.data.ModRace && MusicPlaying.IsPlaying)
                {
                    MusicPlaying.Pause();
                }
            }

            if (Discuss != null) Discuss.ToggleOn = dState == DialogState.Discuss;

            Negotiate.ToggleOn = dState == DialogState.Negotiate;

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
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
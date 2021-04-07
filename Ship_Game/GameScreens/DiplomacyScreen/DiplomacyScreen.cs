using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class DiplomacyScreen : GameScreen
    {
        Rectangle Portrait;
        DialogState DState;

        readonly Array<GenericButton> GenericButtons = new Array<GenericButton>();

        GenericButton SendOffer;
        GenericButton DeclareWar;
        GenericButton Negotiate;
        GenericButton Discuss;
        GenericButton Exit;

        Rectangle DialogRect;

        ScrollList2<DialogOptionListItem> StatementsSL;
        DiplomacyOffersComponent OurOffersList; // NAPact, Peace Treaty, Open Borders...
        DiplomacyOffersComponent TheirOffersList;

        GenericButton Accept;
        GenericButton Reject;
        GenericButton Trust;
        GenericButton Anger;
        GenericButton Fear;
        Array<GenericButton> TAFButtons = new Array<GenericButton>();

        Rectangle Attitude_Pleading_Rect;
        Rectangle Attitude_Respectful_Rect;
        Rectangle Attitude_Threaten_Rect;

        GenericButton OurAttitudeBtn_Pleading;
        GenericButton OurAttitudeBtn_Respectful;
        GenericButton OurAttitudeBtn_Threaten;
        Vector2 EmpireNamePos;
        ScrollList2<TextListItem> OfferTextSL;

        Rectangle R;
        Rectangle BridgeRect;
        //Rectangle Negotiate_Right;
        //Rectangle Negotiate_Left;
        Rectangle ToneContainerRect;

        Rectangle AccRejRect;
        Rectangle TrustRect;
        Rectangle AngerRect;
        Rectangle FearRect;
        ScreenMediaPlayer RacialVideo;

        readonly Empire Them;
        readonly Empire Us;
        readonly Relationship ThemAndUs; // relationships between Them and Us
        readonly Relationship UsAndThem; // between Us and Them
        readonly string WhichDialog;
        readonly bool WarDeclared;
        string TheirText;

        Offer.Attitude Attitude = Offer.Attitude.Respectful;
        Offer OurOffer          = new Offer();
        Offer TheirOffer        = new Offer();
        Empire EmpireToDiscuss;
        readonly SolarSystem SysToDiscuss;


        // BASE constructor
        DiplomacyScreen(GameScreen parent, Empire them, Empire us, string whichDialog) : base(parent)
        {
            Them                            = them;
            Us                              = us;
            ThemAndUs                       = them.GetRelations(us);
            ThemAndUs.turnsSinceLastContact = 0;
            UsAndThem                       = us.GetRelations(them);
            WhichDialog                     = whichDialog;
            IsPopup                         = true;
            TransitionOnTime                = 1.0f;
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, GameScreen parent)
            : this(parent, them, us, whichDialog)
        {
            switch (whichDialog)
            {
                case "Declare War Imperialism":
                case "Declare War Imperialism Break NA":
                case "Declare War Defense":
                case "Declare War Defense BrokenNA":
                case "Declare War BC":
                    TheirText   = GetDialogueByName(whichDialog);
                    DState      = DialogState.End;
                    WarDeclared = true;
                    break;
                case "Conquered_Player":
                case "Compliment Military":
                case "Compliment Military Better":
                case "Insult Military":
                    TheirText   = GetDialogueByName(whichDialog);
                    DState      = DialogState.End;
                    break;
                default:
                    TheirText   = GetDialogueFromAttitude();
                    break;
            }
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Empire empireToDiscuss, bool endOnly)
            : this(Empire.Universe, them, us, whichDialog)
        {
            TheirText       = GetDialogueByName(whichDialog);
            DState          = DialogState.End;
            EmpireToDiscuss = empireToDiscuss;
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Offer ourOffer, Offer theirOffer, Empire targetEmpire)
            : this(Empire.Universe, them, us, whichDialog)
        {
            TheirText       = GetDialogueByName(whichDialog);
            DState          = DialogState.TheirOffer;
            OurOffer        = ourOffer;
            TheirOffer      = theirOffer;
            EmpireToDiscuss = targetEmpire;
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Planet p)
            : this(Empire.Universe, them, us, whichDialog)
        {
            SysToDiscuss = p.ParentSystem;

            switch (whichDialog)
            {
                case "Declare War Defense":
                case "Declare War BC":
                case "Declare War BC TarSys":
                    TheirText   = GetDialogueByName(whichDialog);
                    DState      = DialogState.End;
                    WarDeclared = true;
                    break;
                default:
                    TheirText = GetDialogueFromAttitude();
                    break;
            }
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, SolarSystem s)
            : this(Empire.Universe, them, us, whichDialog)
        {
            SysToDiscuss = s;

            switch (whichDialog)
            {
                case "Invaded NA Pact":
                case "Invaded Start War":
                case "Declare War Defense":
                case "Declare War BC":
                case "Declare War BC TarSys":
                    TheirText   = GetDialogueByName(whichDialog);
                    DState      = DialogState.End;
                    WarDeclared = true;
                    break;
                case "Stole Claim":
                case "Stole Claim 2":
                case "Stole Claim 3":
                    TheirText = GetDialogueByName(whichDialog);
                    DState    = DialogState.End;
                    break;
                default:
                    TheirText = GetDialogueFromAttitude();
                    break;
            }
        }

        // The screen is loaded during next frame by using deferred add
        static void AddScreen(GameScreen screen) => ScreenManager.Instance.AddScreen(screen);
        static Empire Player => Empire.Universe.PlayerEmpire;

        public static void Show(Empire them, string which, GameScreen parent)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, Player, which, parent));
        }

        public static void Show(Empire them, Empire us, string which)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, Empire.Universe));
        }

        public static void Show(Empire them, string which)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, Player, which, Empire.Universe));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, null, endOnly:true));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which, Empire empireToDiscuss)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, empireToDiscuss, endOnly:true));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, Player, which, ourOffer, theirOffer, null));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer, Empire empireToDiscuss)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, Player, which, ourOffer, theirOffer, empireToDiscuss));
        }

        public static void Show(Empire them, Empire us, string which, Planet planet)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, planet));
        }

        public static void Show(Empire them, Empire us, string which, SolarSystem s)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, s));
        }

        public static void Show(Empire them, string which, SolarSystem s)
        {
            if (Empire.Universe.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, Player, which, s));
        }

        public static void Stole1stColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim");
        public static void Stole2ndColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 2");
        public static void Stole3rdColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 3");

        static void StoleColonyClaim(Planet claimedPlanet, Empire victim, string dialog)
        {
            ScreenManager.Instance.AddScreen(new DiplomacyScreen(victim, Empire.Universe.PlayerEmpire, dialog, claimedPlanet.ParentSystem));
        }

        public static void ContactPlayerFromDiplomacyQueue(Empire responder, string dialog)
        {
            ScreenManager.Instance.AddScreen(new DiplomacyScreen(responder, 
                Empire.Universe.PlayerEmpire, dialog, null, endOnly: true));
        }


        void DoNegotiationResponse(string answer)
        {
            StatementsSL.Reset();
            TheirText = "";
            if (TheirOffer.NAPact && ThemAndUs.HaveRejectedNaPact)
            {
                TheirText = GetDialogueByName("ComeAround_NAPACT") + "\n\n";
            }
            else if (TheirOffer.TradeTreaty && ThemAndUs.HaveRejected_TRADE)
            {
                TheirText = GetDialogueByName("ComeAround_TRADE") + "\n\n";
            }
            TheirText += GetDialogueByName(answer);
            DState     = DialogState.Them;
        }

        Vector2 GetCenteredTextPosition(Rectangle r, string text, SpriteFont font)
        {
            return new Vector2(r.CenterTextX(text, font), r.CenterY());
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 4 / 5);
            batch.Begin();

            DrawBackground(batch);
            base.Draw(batch, elapsed);
            foreach (GenericButton taf in TAFButtons)
            {
                taf.DrawWithShadowCaps(batch);

                TrustRect.Width = (int)ThemAndUs.Trust.Clamped(1, 100);
                batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), TrustRect, Color.Green);

                AngerRect.Width = (int)ThemAndUs.TotalAnger.Clamped(1, 100);
                batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), AngerRect, Color.Yellow);

                FearRect.Width = (int)ThemAndUs.Threat.Clamped(1, 100);
                batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), FearRect, Color.Red);
            }

            OfferTextSL.Visible = false;

            switch (DState)
            {
                case DialogState.Them:
                {
                    string text      = ParseTextDiplomacy(TheirText, (DialogRect.Width - 25));
                    Vector2 position = GetCenteredTextPosition(DialogRect, text, Fonts.Consolas18);
                    batch.DrawDropShadowText(text, position, Fonts.Consolas18);
                    break;
                }
                case DialogState.Discuss:
                {
                    break;
                }
                case DialogState.Negotiate:
                {
                    TheirOffer.Them = Them;
                    string txt      = OurOffer.FormulateOfferText(Attitude, TheirOffer);
                    OfferTextSL.ResetWithParseText(Fonts.Consolas18, txt, DialogRect.Width - 30);
                    OfferTextSL.Visible = true;

                    base.Draw(batch, elapsed);
                    
                    if (!TheirOffer.IsBlank() || !OurOffer.IsBlank() || OurOffer.Alliance)
                    {
                        SendOffer.DrawWithShadow(batch);
                    }
                    batch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Tone"), ToneContainerRect, Color.White);
                    
                    OurAttitudeBtn_Pleading.Draw(ScreenManager);
                    OurAttitudeBtn_Threaten.Draw(ScreenManager);
                    OurAttitudeBtn_Respectful.Draw(ScreenManager);
                    break;
                }
                case DialogState.TheirOffer:
                {
                    batch.Draw(ResourceManager.Texture("UI/AcceptReject"), AccRejRect, Color.White);
                    string text      = ParseTextDiplomacy(TheirText, DialogRect.Width - 25);
                    Vector2 position = GetCenteredTextPosition(DialogRect, text, Fonts.Consolas18);
                    batch.DrawDropShadowText(text, position, Fonts.Consolas18);
                    Accept.DrawWithShadow(batch);
                    Reject.DrawWithShadow(batch);
                    break;
                }
                case DialogState.End:
                {
                    string text      = ParseTextDiplomacy(TheirText, DialogRect.Width - 25);
                    Vector2 position = GetCenteredTextPosition(DialogRect, text, Fonts.Consolas18);
                    batch.DrawDropShadowText(text, position, Fonts.Consolas18);
                    break;
                }
            }

            if (DState == DialogState.End || DState == DialogState.TheirOffer)
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
                        r.X += (int)transitionOffset * 512;
                    }
                    else
                    {
                        r.X += (int)(transitionOffset * 512f);
                    }
                    b.TransitionCaps(r);
                    b.DrawWithShadowCaps(batch);
                }
            }

            var pos = new Vector2((Portrait.X + 200), (Portrait.Y + 200));
            pos.Y  += (Fonts.Pirulen16.LineSpacing + 15);
            pos.X  -= 8f;
            pos.Y  += (Fonts.Pirulen16.LineSpacing + 15);
            pos.X  -= 8f;
            pos.Y  += (Fonts.Pirulen16.LineSpacing + 15);
            pos.X  -= 8f;
            base.Draw(batch, elapsed);
            batch.End();
        }

        void DrawBackground(SpriteBatch batch)
        {
            if (RacialVideo.Size != Vector2.Zero)
            {
                if (RacialVideo.ReadyToPlay)
                {
                    Color color = Color.White;
                    if (WarDeclared || UsAndThem.AtWar)
                    {
                        color.B = 100;
                        color.G = 100;
                    }

                    RacialVideo.Draw(batch, color);
                }
            }
            // the size will be zero if video is null. 
            else
            {
                batch.Draw(Them.data.PortraitTex, Portrait, Color.White);
            }

            batch.DrawDropShadowText1(Them.data.Traits.Name, EmpireNamePos, Fonts.Pirulen20, Them.EmpireColor);
            batch.FillRectangle(new Rectangle(0, R.Y, 1920, R.Height), new Color(0, 0, 0, 150));
            batch.Draw(ResourceManager.Texture("GameScreens/Bridge"), BridgeRect, Color.White);
        }

        void BeginNegotiations()
        {
            StatementsSL.Reset();
            StatementsSL.Visible = false;

            OurOffer   = new Offer();
            TheirOffer = new Offer { Them = Them };
            OurOffersList.StartNegotiation(TheirOffersList, OurOffer, TheirOffer);
            TheirOffersList.StartNegotiation(OurOffersList, TheirOffer, OurOffer);
        }

        string GetDialogueFromAttitude()
        {
            float theirOpinionOfUs = Math.Max(0, ThemAndUs.GetStrength());
            return GetDialogue(theirOpinionOfUs);
        }

        string GetDialogue(float attitude)
        {
            if (UsAndThem.AtWar)
            {
                switch (ThemAndUs.ActiveWar.GetWarScoreState())
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

            foreach (DialogLine dialogLine in Them.dd.Dialogs)
            {
                if (dialogLine.DialogType == WhichDialog)
                {
                    if (attitude >= 40.0 && attitude < 60.0) return dialogLine.Neutral;
                    if (attitude >= 60.0)                    return dialogLine.Friendly;
                                                             return dialogLine.Hostile;
                }
            }
            return "";
        }

        public string GetDialogueByName(string name)
        {
            var sb = new StringBuilder();
            foreach (DialogLine dl in Them.dd.Dialogs)
            {
                if (dl.DialogType != name)
                    continue;

                string space = "";
                if (dl.Default.NotEmpty())
                {
                    sb.Append(dl.Default);
                    space = " ";
                }

                switch (Them.data.DiplomaticPersonality.Name ?? "")
                {
                    case "Aggressive": sb.Append(space + dl.DL_Agg);  break;
                    case "Ruthless":   sb.Append(space + dl.DL_Ruth); break;
                    case "Honorable":  sb.Append(space + dl.DL_Hon);  break;
                    case "Xenophobic": sb.Append(space + dl.DL_Xeno); break;
                    case "Pacifist":   sb.Append(space + dl.DL_Pac);  break;
                    case "Cunning":    sb.Append(space + dl.DL_Cunn); break;
                }

                switch (Them.data.EconomicPersonality.Name ?? "")
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

        void OnDiscussStatementClicked(DialogOptionListItem item)
        {
            Respond(item.Option);
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            if (TrustRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GameText.ThisIndicatesHowMuchA);
            if (AngerRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GameText.ThisIndicatesHowAngryA);
            if (FearRect.HitTest(input.CursorPosition))  ToolTip.CreateTooltip(GameText.ThisIndicatesHowMuchA2);

            if (Exit.HandleInput(input) && DState != DialogState.TheirOffer)
            {
                Ship_Game.Audio.GameAudio.SwitchBackToGenericMusic();
                ExitScreen();
                return true;
            }

            if (DState == DialogState.End) return false;

            if (DState != DialogState.Negotiate) TheirOffersList.Visible = OurOffersList.Visible = false;

            if (DState != DialogState.TheirOffer)
            {
                if (!UsAndThem.Treaty_Peace)
                {
                    if (DeclareWar != null && DeclareWar.HandleInput(input))
                    {
                        StatementsSL.Reset();
                        DState = DialogState.End;
                        if (UsAndThem.Treaty_NAPact)
                        {
                            TheirText = GetDialogueByName("WarDeclared_FeelsBetrayed");
                            Us.GetEmpireAI().DeclareWarOn(Them, WarType.ImperialistWar);
                            Them.GetEmpireAI().GetWarDeclaredOnUs(Us, WarType.ImperialistWar);
                        }
                        else
                        {
                            TheirText = GetDialogueByName("WarDeclared_Generic");
                            Us.GetEmpireAI().DeclareWarOn(Them, WarType.ImperialistWar);
                            Them.GetEmpireAI().GetWarDeclaredOnUs(Us, WarType.ImperialistWar);
                        }
                    }
                }
                else if (DeclareWar != null && DeclareWar.R.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(GameText.YouCurrentlyHaveAPeace);
                }

                if (Discuss != null && Discuss.HandleInput(input))
                {
                    StatementsSL.Reset();
                    StatementsSL.OnClick = OnDiscussStatementClicked;
                    StatementsSL.Visible = true;
                    DState = DialogState.Discuss;
                    foreach (StatementSet set in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
                    {
                        if (set.Name == "Ordinary Discussion")
                        {
                            int n = 1;
                            foreach (DialogOption opt1 in set.DialogOptions)
                            {
                                string str = opt1.SpecialInquiry.NotEmpty() ? GetDialogueByName(opt1.SpecialInquiry) : opt1.Words;
                                var opt2   = new DialogOption(n++, str)
                                {
                                    Words    = ParseTextDiplomacy(str, (DialogRect.Width - 25)),
                                    Response = opt1.Response
                                };

                                var optionList = new DialogOptionListItem(opt2);
                                
                                StatementsSL.AddItem(optionList);
                            }
                        }
                    }
                }

                if (DState == DialogState.Discuss)
                {
                    if (StatementsSL.HandleInput(input))
                        return true;
                }
                
                if (DState == DialogState.Negotiate)
                {
                    if ((!TheirOffer.IsBlank() || !OurOffer.IsBlank() || TheirOffer.Alliance) && SendOffer.HandleInput(input))
                    {
                        DoNegotiationResponse(Them.GetEmpireAI().AnalyzeOffer(OurOffer, TheirOffer, Us, Attitude));
                        OurOffer   = new Offer();
                        TheirOffer = new Offer { Them = Them };
                    }

                    if (OurAttitudeBtn_Pleading.HandleInput(input))
                    {
                        OurAttitudeBtn_Pleading.ToggleOn   = true;
                        OurAttitudeBtn_Respectful.ToggleOn = false;
                        OurAttitudeBtn_Threaten.ToggleOn   = false;
                        Attitude                           = Offer.Attitude.Pleading;
                    }
                    if (OurAttitudeBtn_Respectful.HandleInput(input))
                    {
                        OurAttitudeBtn_Respectful.ToggleOn = true;
                        OurAttitudeBtn_Pleading.ToggleOn   = false;
                        OurAttitudeBtn_Threaten.ToggleOn   = false;
                        Attitude                           = Offer.Attitude.Respectful;
                    }
                    if (OurAttitudeBtn_Threaten.HandleInput(input))
                    {
                        OurAttitudeBtn_Threaten.ToggleOn   = true;
                        OurAttitudeBtn_Pleading.ToggleOn   = false;
                        OurAttitudeBtn_Respectful.ToggleOn = false;
                        Attitude                           = Offer.Attitude.Threaten;
                    }
                }
                if (Negotiate.HandleInput(input))
                {
                    DState = DialogState.Negotiate;
                    BeginNegotiations();
                }
            }
            if (DState == DialogState.TheirOffer)
            {
                if (Accept.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null) TheirOffer.ValueToModify.Value = false;
                    if (OurOffer.ValueToModify != null)   OurOffer.ValueToModify.Value = true;

                    DState = DialogState.End;
                    TheirText = GetDialogueByName(TheirOffer.AcceptDL);
                    Us.GetEmpireAI().AcceptOffer(OurOffer, TheirOffer, Us, Them, Attitude);
                }
                if (Reject.HandleInput(input))
                {
                    if (TheirOffer.ValueToModify != null) TheirOffer.ValueToModify.Value = true;
                    if (OurOffer.ValueToModify != null)   OurOffer.ValueToModify.Value = false;
                    
                    DState    = DialogState.End;
                    TheirText = GetDialogueByName(TheirOffer.RejectDL);
                }
            }

            if (input.RightMouseClick) // prevent right click from closing this screen
                return true;

            return base.HandleInput(input);
        }

        GenericButton Button(ref Vector2 cursor, int locId)
        {
            var button = new GenericButton(cursor, Localizer.Token(locId), Fonts.Pirulen20, Fonts.Pirulen16);
            GenericButtons.Add(button);
            cursor.Y += 25f;
            return button;
        }

        GenericButton TAFButton(ref Vector2 cursor, int locId, bool toggleOn = false)
        {
            var button = new GenericButton(cursor, Localizer.Token(locId), Fonts.Pirulen16, Fonts.Pirulen12) { ToggleOn = toggleOn };
            TAFButtons.Add(button);
            cursor.Y += 25f;
            return button;
        }

        public override void LoadContent()
        {
            int bridgeWidth  = Math.Min(1920, ScreenWidth);
            int bridgeHeight = Math.Min(1080, ScreenHeight);
            BridgeRect = new Rectangle(ScreenWidth/2 - bridgeWidth/2,
                                       ScreenHeight/2 - bridgeHeight/2, bridgeWidth, bridgeHeight);

            int portraitWidth  = (int)(bridgeWidth * (1280f/1920f));
            int portraitHeight = (int)(bridgeHeight * (1280f/1920f));
            Portrait = new Rectangle(ScreenWidth/2 - portraitWidth/2,
                                     ScreenHeight/2 - portraitHeight/2, portraitWidth, portraitHeight);

            var cursor = new Vector2(Portrait.X + Portrait.Width - 85, Portrait.Y + 140);
            EmpireNamePos = new Vector2(cursor.X - Fonts.Pirulen20.MeasureString(Them.data.Traits.Name).X, Portrait.Y + 40);
            if (!UsAndThem.AtWar)
            {
                DeclareWar = Button(ref cursor, locId: 1200);
                Discuss    = Button(ref cursor, locId: 1201);
            }

            Negotiate = Button(ref cursor, locId: 1202);
            Exit      = Button(ref cursor, locId: 1203);

            cursor = new Vector2(Portrait.X + 115, Portrait.Y + 160);

            Trust = TAFButton(ref cursor, locId: 1204, toggleOn: true);
            Anger = TAFButton(ref cursor, locId: 1205, toggleOn: true);
            Fear  = TAFButton(ref cursor, locId: 1206, toggleOn: true);

            TrustRect  = new Rectangle(Portrait.X + 125, Trust.R.Y + 2, 100, Trust.R.Height);
            AngerRect  = new Rectangle(Portrait.X + 125, Anger.R.Y + 2, 100, Anger.R.Height);
            FearRect   = new Rectangle(Portrait.X + 125, Fear.R.Y + 2, 100, Fear.R.Height);
            DialogRect = new Rectangle(Portrait.X + 175, Portrait.Bottom - 175, Portrait.Width - 350, 150);

            if (ScreenHeight < 820)
            {
                DialogRect.Y = Portrait.Y + Portrait.Height - 100;
            }
            R = DialogRect;
            R.Height += 50;
            if (R.Y + R.Height > ScreenHeight)
            {
                R.Y -= (R.Y + R.Height - ScreenHeight + 2);
            }

            Attitude_Respectful_Rect = new Rectangle(DialogRect.CenterX() - 90, DialogRect.Bottom +20, 180, 48);
            Attitude_Pleading_Rect   = new Rectangle(Attitude_Respectful_Rect.Left - 200, Attitude_Respectful_Rect.Top , 180, 48);
            Attitude_Threaten_Rect   = new Rectangle(Attitude_Respectful_Rect.Left + 200, Attitude_Respectful_Rect.Top, 180, 48);

            ToneContainerRect = new Rectangle(ScreenWidth / 2 - 324, Attitude_Pleading_Rect.Y, 648, 48);
            OurAttitudeBtn_Pleading   = new GenericButton(Attitude_Pleading_Rect,   Localizer.Token(GameText.Pleading), Fonts.Pirulen12);
            OurAttitudeBtn_Respectful = new GenericButton(Attitude_Respectful_Rect, Localizer.Token(GameText.Respectful), Fonts.Pirulen12) { ToggleOn = true };
            OurAttitudeBtn_Threaten   = new GenericButton(Attitude_Threaten_Rect,   Localizer.Token(GameText.Threatening), Fonts.Pirulen12);

            AccRejRect = new Rectangle(R.X + R.Width / 2 - 220, R.Y + R.Height - 48, 440, 48);
            Accept = new GenericButton(new Rectangle(AccRejRect.X, AccRejRect.Y, 220, 48), Localizer.Token(GameText.Accept), Fonts.Pirulen12);
            Reject = new GenericButton(new Rectangle(AccRejRect.X + 220, AccRejRect.Y, 220, 48), Localizer.Token(GameText.Reject), Fonts.Pirulen12);


            SendOffer = new GenericButton(new Rectangle(R.X + R.Width / 2 - 90, R.Y - 40, 180, 33), Localizer.Token(GameText.SendOffer), Fonts.Pirulen20);

            var offerTextMenu = new Rectangle(R.X, R.Y, R.Width, R.Height - 30);
            OfferTextSL  = Add(new ScrollList2<TextListItem>(offerTextMenu, Fonts.Consolas18.LineSpacing + 2));
            StatementsSL = Add(new ScrollList2<DialogOptionListItem>(offerTextMenu, Fonts.Consolas18.LineSpacing + 2));

            SubTexture ourBkg   = TransientContent.LoadTextureOrDefault("Textures/GameScreens/Negotiate_Right");
            SubTexture theirBkg = TransientContent.LoadTextureOrDefault("Textures/GameScreens/Negotiate_Left");
            int offerW = 220;
            int offerH = 280;
            int offerY = BridgeRect.Bottom - offerH;
            var usRect   = new Rectangle(BridgeRect.Right - (5 + offerW), offerY, offerW, offerH);
            var themRect = new Rectangle(BridgeRect.Left + 5, offerY, offerW, offerH);
            
            OurOffersList   = Add(new DiplomacyOffersComponent(Us, Them, usRect, ourBkg));
            TheirOffersList = Add(new DiplomacyOffersComponent(Them, Us, themRect, theirBkg));
        }

        string ParseTextDiplomacy(string text, float maxLineWidth)
        {
            if (text == null)
            {
                Log.Error("ParseTextDiplomacy: text was null");
                return "Debug info: Error. Expected " + WhichDialog;
            }

            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
                words[i] = ConvertDiplomacyKeyword(words[i]);

            return Fonts.Consolas18.ParseText(words, maxLineWidth);
        }

        string UsSingular          => Us?.data.Traits.Singular ?? "HUMAN";
        string UsPlural            => Us?.data.Traits.Plural   ?? "HUMANS";
        string EmpireToDiscussName => EmpireToDiscuss?.data.Traits.Name ?? "EMPIRE";
        string SysToDiscussName    => SysToDiscuss?.Name ?? "SYSTEM";
        string TechDemanded
        {
            get
            {
                string offered = OurOffer?.TechnologiesOffered.Count > 0
                               ? OurOffer.TechnologiesOffered[0] : "TECH";
                if (ResourceManager.TryGetTech(offered, out Technology tech))
                    return Localizer.Token(tech.NameIndex);
                return offered;
            }
        }

        string ConvertDiplomacyKeyword(string keyword)
        {
            switch (keyword)
            {
                default: return keyword; // it wasn't a keyword
                case "SING":   return UsSingular;
                case "SING.":  return UsSingular + ".";
                case "SING,":  return UsSingular + ",";
                case "SING?":  return UsSingular + "?";
                case "SING!":  return UsSingular + "!";

                case "PLURAL":  return UsPlural;
                case "PLURAL.": return UsPlural + ".";
                case "PLURAL,": return UsPlural + ",";
                case "PLURAL?": return UsPlural + "?";
                case "PLURAL!": return UsPlural + "!";

                case "TARSYS":  return SysToDiscussName;
                case "TARSYS.": return SysToDiscussName + ".";
                case "TARSYS,": return SysToDiscussName + ",";
                case "TARSYS?": return SysToDiscussName + "?";
                case "TARSYS!": return SysToDiscussName + "!";

                case "TAREMP":  return EmpireToDiscussName;
                case "TAREMP.": return EmpireToDiscussName + ".";
                case "TAREMP,": return EmpireToDiscussName + ",";
                case "TAREMP?": return EmpireToDiscussName + "?";
                case "TAREMP!": return EmpireToDiscussName + "!";

                case "TECH_DEMAND":  return TechDemanded;
                case "TECH_DEMAND.": return TechDemanded + ".";
                case "TECH_DEMAND,": return TechDemanded + ",";
                case "TECH_DEMAND?": return TechDemanded + "?";
                case "TECH_DEMAND!": return TechDemanded + "!";
            }
        }

        void Respond(DialogOption resp)
        {
            string responseName = resp.Response;
            if (resp.Target is Empire empire)
                EmpireToDiscuss = empire;

            switch (responseName)
            {
                case "Target_Opinion"               : RespondTargetOpinion(); break;
                case "EmpireDiscuss"                : RespondEmpireDiscuss(); break;
                case "Hardcoded_EmpireChoose"       : RespondHardcodedEmpireChoose(); break;
                case "Hardcoded_War_Analysis"       : RespondHardcodedWarAnalysis(); break;
                case "Hardcoded_Federation_Analysis": RespondHardcodedFederationAnalysis(); break;
                case "Hardcoded_Grievances"         : RespondHardcodedGrievances(); break;
                default:
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName(responseName);
                    DState = DialogState.Them;
                    break;
            }
        }

        void RespondHardcodedGrievances()
        {
            StatementsSL.Reset();
            TheirText = "";
            float num = Math.Max(0, ThemAndUs.GetStrength());
            if (ThemAndUs.TurnsKnown < 20)
            {
                TheirText += GetDialogueByName("Opinion_JustMetUs");
            }
            else if (num > 60f)
            {
                TheirText += GetDialogueByName("Opinion_NoProblems");
            }
            else if (ThemAndUs.WarHistory.Count > 0 &&
                     ThemAndUs.WarHistory[ThemAndUs.WarHistory.Count - 1].EndStarDate -
                     ThemAndUs.WarHistory[ThemAndUs.WarHistory.Count - 1].StartDate < 50f)
            {
                TheirText += GetDialogueByName("PROBLEM_RECENTWAR");
            }
            else if (num >= 0.0)
            {
                bool flag = false;
                if (ThemAndUs.Anger_TerritorialConflict + ThemAndUs.Anger_FromShipsInOurBorders >
                    Them.data.DiplomaticPersonality.Territorialism / 2f)
                {
                    TheirText += GetDialogueByName("Opinion_Problems");
                    flag = true;
                    if (ThemAndUs.Threat > 75f)
                    {
                        TheirText += GetDialogueByName("Problem_Territorial");
                        TheirText += GetDialogueByName("Problem_AlsoMilitary");
                    }
                    else if (ThemAndUs.Threat < -20f && (Them.IsRuthless || Them.IsAggressive))
                    {
                        TheirText += GetDialogueByName("Problem_Territorial");
                        TheirText += GetDialogueByName("Problem_AlsoMilitaryWeak");
                    }
                    else
                    {
                        TheirText += GetDialogueByName("Problem_JustTerritorial");
                    }
                }
                else if (ThemAndUs.Threat > 75f)
                {
                    flag = true;
                    TheirText += GetDialogueByName("Opinion_Problems");
                    TheirText += GetDialogueByName("Problem_PrimaryMilitary");
                }
                else if (ThemAndUs.Threat < -20f && (Them.IsRuthless || Them.IsAggressive))
                {
                    TheirText += GetDialogueByName("Opinion_Problems");
                    TheirText += GetDialogueByName("Problem_MilitaryWeak");
                }

                if (!flag)
                {
                    TheirText += GetDialogueByName("Opinion_NothingMajor");
                }
            }

            DState = DialogState.Them;
        } 

        void RespondHardcodedFederationAnalysis()
        {
            StatementsSL.Reset();
            TheirText = "";
            DState = DialogState.Them;
            if (!ThemAndUs.Treaty_Alliance)
            {
                if (ThemAndUs.TurnsKnown < 50)
                    TheirText += GetDialogueByName("Federation_JustMet");
                else if (ThemAndUs.GetStrength() >= 75f)
                    TheirText += GetDialogueByName("Federation_NoAlliance");
                else
                    TheirText += GetDialogueByName("Federation_RelationsPoor");
            }
            else if (ThemAndUs.TurnsAllied < ThemAndUs.GetTurnsForFederationWithPlayer(Them))
            {
                TheirText += GetDialogueByName("Federation_AllianceTooYoung");
            }
            else
            {
                if (ThemAndUs.TurnsAllied < ThemAndUs.GetTurnsForFederationWithPlayer(Them))
                    return;

                if (Them.TotalScore > Us.TotalScore * 0.75f || ThemAndUs.Threat < 0f)
                {
                    TheirText += GetDialogueByName("Federation_WeAreTooStrong");
                    return;
                }

                var theirWarTargets = new Array<Empire>();
                var ourWarTargets   = new Array<Empire>();

                foreach ((Empire other, Relationship rel) in Them.AllRelations)
                {
                    if (!other.isFaction && rel.AtWar)
                        theirWarTargets.Add(other);

                    if (!other.isFaction && rel.GetStrength() > 75f && Us.IsAtWarWith(other))
                    {
                        ourWarTargets.Add(other);
                    }
                }

                if (theirWarTargets.Count > 0)
                {
                    // enemy of my enemy is a friend
                    EmpireToDiscuss = theirWarTargets.FindMax(e => e.TotalScore);

                    if (EmpireToDiscuss != null)
                    {
                        TheirText += GetDialogueByName("Federation_Quest_DestroyEnemy");
                        ThemAndUs.FedQuest = new FederationQuest
                        {
                            EnemyName = EmpireToDiscuss.data.Traits.Name
                        };
                    }
                }
                else if (ourWarTargets.Count > 0)
                {
                    EmpireToDiscuss = ourWarTargets.FindMax(e => Them.GetRelations(e).GetStrength());

                    if (EmpireToDiscuss != null)
                    {
                        TheirText += GetDialogueByName("Federation_Quest_AllyFriend");
                        ThemAndUs.FedQuest = new FederationQuest
                        {
                            type = QuestType.AllyFriend,
                            EnemyName = EmpireToDiscuss.data.Traits.Name
                        };
                    }
                }
                else
                {
                    TheirText += GetDialogueByName("Federation_Accept");
                    Us.AbsorbEmpire(Them);
                }
            }
        }

        void RespondHardcodedWarAnalysis()
        {
            TheirText = "";
            DState    = DialogState.Them;

            if (EmpireToDiscuss == null)
                return;

            if (!Us.IsAtWarWith(EmpireToDiscuss))
            {
                TheirText += GetDialogueByName("JoinWar_YouAreNotAtWar");
            }
            else if (UsAndThem.AtWar)
            {
                TheirText += GetDialogueByName("JoinWar_WeAreAtWar");
            }
            else if (ThemAndUs.Treaty_Alliance)
            {
                if (Them.ProcessAllyCallToWar(Us, EmpireToDiscuss, out string dialog))
                {
                    TheirText += GetDialogueByName(dialog);
                    Them.GetEmpireAI().DeclareWarOn(EmpireToDiscuss, WarType.ImperialistWar);
                    EmpireToDiscuss.GetEmpireAI().GetWarDeclaredOnUs(Them, WarType.ImperialistWar);
                }
                else
                {
                    TheirText += GetDialogueByName(dialog);
                }
            }
            else if (ThemAndUs.GetStrength() < 30f)
            {
                TheirText += GetDialogueByName("JoinWar_Reject_PoorRelations");
            }
            else if (Them.IsPacifist || Them.IsHonorable)
            {
                TheirText += GetDialogueByName("JoinWar_Reject_Pacifist");
            }
            else if (UsAndThem.GetStrength() > 60f)
            {
                TheirText += GetDialogueByName("JoinWar_Allied_DECLINE");
            }
            else if (ThemAndUs.GetStrength() > 60f && Us.OffensiveStrength > Us.KnownEmpireStrength(EmpireToDiscuss))
            {
                TheirText += GetDialogueByName("JoinWar_OK");
                Them.GetEmpireAI().DeclareWarOn(EmpireToDiscuss, WarType.ImperialistWar);
                EmpireToDiscuss.GetEmpireAI().GetWarDeclaredOnUs(Them, WarType.ImperialistWar);
            }
            else
            {
                TheirText += GetDialogueByName("JoinWar_Reject_TooDangerous");
            }
        }

        void RespondHardcodedEmpireChoose()
        {
            StatementsSL.Reset();
            int n1 = 1;
            foreach ((Empire other, Relationship rel) in Them.AllRelations)
            {
                if (other != Us && rel.Known && !other.isFaction
                    && !other.data.Defeated && Us.IsKnown(other))
                {
                    var option = new DialogOption(n1, Localizer.Token(GameText.LetsDiscuss) + " " + other.data.Traits.Name)
                    {
                        Target = other
                    };
                    option.Words    = ParseTextDiplomacy(option.Words, DialogRect.Width - 25);
                    option.Response = "EmpireDiscuss";
                    StatementsSL.AddItem(new DialogOptionListItem(option));
                    ++n1;
                }
            }

            if (StatementsSL.NumEntries == 0)
            {
                StatementsSL.Reset();
                TheirText = GetDialogueByName("Dunno_Anybody");
                DState    = DialogState.Them;
            }
        }

        void RespondEmpireDiscuss()
        {
            foreach (StatementSet set in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
            {
                if (set.Name == "EmpireDiscuss")
                {
                    StatementsSL.Reset();
                    int n = 1;
                    foreach (DialogOption option1 in set.DialogOptions)
                    {
                        var option2 = new DialogOption(n, option1.Words)
                        {
                            Words    = ParseTextDiplomacy(option1.Words, DialogRect.Width - 25),
                            Response = option1.Response,
                            Target   = EmpireToDiscuss
                        };
                        StatementsSL.AddItem(new DialogOptionListItem(option2));
                        ++n;
                    }
                }
            }
        }

        void RespondTargetOpinion()
        {
            if (EmpireToDiscuss == null)
                return;

            StatementsSL.Reset();
            float strength = UsAndThem.GetStrength();
            if (strength >= 65.0)
                TheirText = GetDialogueByName("Opinion_Positive_" + EmpireToDiscuss.data.Traits.ShipType);
            else if (strength < 65.0 && strength >= 40.0)
                TheirText = GetDialogueByName("Opinion_Neutral_" + EmpireToDiscuss.data.Traits.ShipType);
            else if (strength < 40.0)
                TheirText = GetDialogueByName("Opinion_Negative_" + EmpireToDiscuss.data.Traits.ShipType);
            DState = DialogState.Them;
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (!Visible)
                return;

            if (RacialVideo == null)
                RacialVideo = new ScreenMediaPlayer(TransientContent);

            if (!RacialVideo.PlaybackFailed)
                RacialVideo.PlayVideoAndMusic(Them, WarDeclared);

            RacialVideo.Rect = Portrait;
            RacialVideo.Update(this);

            if (Discuss != null) Discuss.ToggleOn = DState == DialogState.Discuss;
            Negotiate.ToggleOn = DState == DialogState.Negotiate;

            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }

        protected override void Destroy()
        {
            RacialVideo?.Dispose();
            base.Destroy();
        }

        enum DialogState
        {
            Them,
            Choosing,
            Discuss,
            Negotiate,
            TheirOffer,
            End
        }
    }
}

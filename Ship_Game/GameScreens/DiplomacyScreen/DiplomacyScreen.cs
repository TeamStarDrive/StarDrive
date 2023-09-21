using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Text;
using SDGraphics;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using SDUtils;
#pragma warning disable CA2213

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class DiplomacyScreen : GameScreen
    {
        Rectangle Portrait;
        DialogState DState;

        readonly Array<GenericButton> GenericButtons = new();

        GenericButton SendOffer;
        GenericButton DeclareWar; // OPTIONAL
        GenericButton Negotiate;
        GenericButton Discuss; // OPTIONAL
        GenericButton Exit;

        RectF DialogRect;
        float DialogAreaWidth => DialogRect.W - 30;

        // NOTE: these two overlap each other, so they are disabled/enabled according which one is active
        ScrollList<DialogOptionListItem> DiscussionSL;
        UITextBox DialogTextBox;

        DiplomacyOffersComponent OurOffersList; // NAPact, Peace Treaty, Open Borders...
        DiplomacyOffersComponent TheirOffersList;

        RectF AccRejRect;
        GenericButton Accept;
        GenericButton Reject;

        GenericButton Trust;
        GenericButton Anger;
        GenericButton Threat;

        GenericButton OurAttitudeBtn_Pleading;
        GenericButton OurAttitudeBtn_Respectful;
        GenericButton OurAttitudeBtn_Threaten;
        Vector2 EmpireNamePos;

        Rectangle R;
        Rectangle BridgeRect;
        RectF ToneContainerRect;

        Rectangle TrustRect;
        Rectangle AngerRect;
        Rectangle ThreatRect;
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

        readonly Empire[] AlliedEmpiresAtWar;
        readonly Empire[] EmpiresTheyAreAlliedWith;

        // BASE constructor
        DiplomacyScreen(GameScreen parent, Empire them, Empire us, string whichDialog, UniverseScreen toPause)
            : base(parent, toPause: toPause)
        {
            Us = us;
            Them = them;
            ThemAndUs = them.GetRelations(us);
            UsAndThem = us.GetRelations(them);
            ThemAndUs.turnsSinceLastContact = 0;
            WhichDialog = whichDialog;
            IsPopup = true;
            TransitionOnTime = 1.0f;
            CanEscapeFromScreen = false; // don't allow right click escape from this screen

            AlliedEmpiresAtWar = GetAlliedEmpiresTheyAreAtWarWith(them, us);
            EmpiresTheyAreAlliedWith = GetAiAlliedEmpires(them, us);
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, GameScreen parent)
            : this(parent, them, us, whichDialog, us.Universe.Screen)
        {
            switch (whichDialog)
            {
                case "Declare War Imperialism":
                case "Declare War Imperialism Break NA":
                case "Declare War Defense":
                case "Declare War Defense BrokenNA":
                case "Declare War BC":
                    SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
                    WarDeclared = true;
                    break;
                case "Conquered_Player":
                case "Compliment Military":
                case "Compliment Military Better":
                case "Insult Military":
                case "CUTTING_DEALS_WITH_ENEMY":
                case "TRIED_CUTTING_DEALS_WITH_ENEMY":
                case "PLAYER_ALLIED_WAR_CONTRIBUTION_WARNING":
                case "PLAYER_ALLIED_WAR_CONTRIBUTION_ACTION":
                    SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
                    break;
                default:
                    SetDialogText(GetDialogueFromAttitude());
                    break;
            }
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Empire empireToDiscuss, bool endOnly)
            : this(them.Universe.Screen, them, us, whichDialog, toPause: them.Universe.Screen)
        {
            SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
            EmpireToDiscuss = empireToDiscuss;
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Offer ourOffer, Offer theirOffer, Empire targetEmpire)
            : this(them.Universe.Screen, them, us, whichDialog, toPause: them.Universe.Screen)
        {
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            EmpireToDiscuss = targetEmpire;
            SetDialogText(GetDialogueByName(whichDialog), DialogState.TheirOffer);
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Planet p)
            : this(them.Universe.Screen, them, us, whichDialog, toPause: them.Universe.Screen)
        {
            SysToDiscuss = p.System;

            switch (whichDialog)
            {
                case "Declare War Defense":
                case "Declare War BC":
                case "Declare War BC TarSys":
                    SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
                    WarDeclared = true;
                    break;
                default:
                    SetDialogText(GetDialogueFromAttitude());
                    break;
            }
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, SolarSystem s)
            : this(them.Universe.Screen, them, us, whichDialog, toPause: them.Universe.Screen)
        {
            SysToDiscuss = s;

            switch (whichDialog)
            {
                case "Invaded NA Pact":
                case "Invaded Start War":
                case "Declare War Defense":
                case "Declare War BC":
                case "Declare War BC TarSys":
                    SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
                    WarDeclared = true;
                    break;
                case "Stole Claim":
                case "Stole Claim 2":
                case "Stole Claim 3":
                    SetDialogText(GetDialogueByName(whichDialog), DialogState.End);
                    break;
                default:
                    SetDialogText(GetDialogueFromAttitude());
                    break;
            }
        }

        // The screen is loaded during next frame by using deferred add
        static void AddScreen(GameScreen screen) => ScreenManager.Instance.AddScreen(screen);

        public static void Show(Empire them, string which, GameScreen parent)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, them.Universe.Player, which, parent));
        }

        public static void Show(Empire them, Empire us, string which)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, them.Universe.Screen));
        }

        public static void Show(Empire them, string which)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, them.Universe.Player, which, them.Universe.Screen));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, null, endOnly:true));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which, Empire empireToDiscuss)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, empireToDiscuss, endOnly:true));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, them.Universe.Player, which, ourOffer, theirOffer, null));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer, Empire empireToDiscuss)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, them.Universe.Player, which, ourOffer, theirOffer, empireToDiscuss));
        }

        public static void Show(Empire them, Empire us, string which, Planet planet)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, planet));
        }

        public static void Show(Empire them, Empire us, string which, SolarSystem s)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, us, which, s));
        }

        public static void Show(Empire them, string which, SolarSystem s)
        {
            if (them.Universe.Screen.CanShowDiplomacyScreen)
                AddScreen(new DiplomacyScreen(them, them.Universe.Player, which, s));
        }

        public static void Stole1stColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim");
        public static void Stole2ndColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 2");
        public static void Stole3rdColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 3");

        static void StoleColonyClaim(Planet claimedPlanet, Empire victim, string dialog)
        {
            ScreenManager.Instance.AddScreen(new DiplomacyScreen(victim, victim.Universe.Player, dialog, claimedPlanet.System));
        }

        public static void ContactPlayerFromDiplomacyQueue(Empire responder, string dialog)
        {
            ScreenManager.Instance.AddScreen(new DiplomacyScreen(responder, responder.Universe.Player, dialog, null, endOnly: true));
        }
        
        public override void LoadContent()
        {
            RemoveAll(); // enables content reloading

            int bridgeWidth  = Math.Min(1920, ScreenWidth);
            int bridgeHeight = Math.Min(1080, ScreenHeight);
            BridgeRect = new Rectangle(ScreenWidth/2 - bridgeWidth/2,
                                       ScreenHeight/2 - bridgeHeight/2, bridgeWidth, bridgeHeight);

            int portraitWidth  = (int)(bridgeWidth * (1280f / 1920f));
            int portraitHeight = (int)(bridgeHeight * (1280f / 1920f));
            Portrait = new Rectangle(ScreenWidth/2 - portraitWidth/2,
                                     ScreenHeight/2 - portraitHeight/2, portraitWidth, portraitHeight);

            var cursor = new Vector2(Portrait.X + Portrait.Width - 85, Portrait.Y + 140);
            EmpireNamePos = new(cursor.X - Fonts.Pirulen20.MeasureString(Them.data.Traits.Name).X, Portrait.Y + 40);
            if (!UsAndThem.AtWar)
            {
                DeclareWar = Button(ref cursor, GameText.DeclareWar);
                DeclareWar.Tooltip = GameText.YouCurrentlyHaveAPeace;
                DeclareWar.OnClick = OnDeclareWarClicked;

                Discuss = Button(ref cursor, GameText.Discuss);
                Discuss.OnClick = OnDiscussButtonClicked;
            }

            Negotiate = Button(ref cursor, GameText.Negotiate);
            Negotiate.OnClick = OnNegotiateClicked;
            Exit = Button(ref cursor, GameText.End);
            Exit.OnClick = OnExitClicked;

            cursor = new(Portrait.X + 115, Portrait.Y + 160);

            Trust = TAFButton(ref cursor, GameText.Trust, toggleOn: true);
            Anger = TAFButton(ref cursor, GameText.Anger, toggleOn: true);
            Threat  = TAFButton(ref cursor, GameText.Threat, toggleOn: true);

            TrustRect  = new RectF(Portrait.X + 125, Trust.Y + 2, 100, Trust.Height);
            AngerRect  = new RectF(Portrait.X + 125, Anger.Y + 2, 100, Anger.Height);
            ThreatRect   = new RectF(Portrait.X + 125, Threat.Y + 2, 100, Threat.Height);
            DialogRect = new RectF(Portrait.X + 175, Portrait.Bottom - 175, Portrait.Width - 350, 150);

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

            RectF Attitude_Respectful_Rect = new(DialogRect.CenterX - 90, DialogRect.Bottom +20, 180, 48);
            RectF Attitude_Pleading_Rect   = new(Attitude_Respectful_Rect.Left - 200, Attitude_Respectful_Rect.Top , 180, 48);
            RectF Attitude_Threaten_Rect   = new(Attitude_Respectful_Rect.Left + 200, Attitude_Respectful_Rect.Top, 180, 48);

            ToneContainerRect = new(ScreenWidth / 2 - 324, Attitude_Pleading_Rect.Y, 648, 48);
            OurAttitudeBtn_Pleading   = Add(new GenericButton(Attitude_Pleading_Rect,   GameText.Pleading, Fonts.Pirulen12));
            OurAttitudeBtn_Respectful = Add(new GenericButton(Attitude_Respectful_Rect, GameText.Respectful, Fonts.Pirulen12) { ToggleOn = true });
            OurAttitudeBtn_Threaten   = Add(new GenericButton(Attitude_Threaten_Rect,   GameText.Threatening, Fonts.Pirulen12));
            OurAttitudeBtn_Pleading.OnClick = OnAttitudePleadingClicked;
            OurAttitudeBtn_Respectful.OnClick = OnAttitudeRespectfulClicked;
            OurAttitudeBtn_Threaten.OnClick = OnAttitudeThreatenClicked;

            AccRejRect = new(R.CenterX() - 220, R.Y + R.Height - 48, 440, 48);
            Accept = Add(new GenericButton(new(AccRejRect.X, AccRejRect.Y, 220, 48), GameText.Accept, Fonts.Pirulen12));
            Accept.OnClick = OnAcceptClicked;
            Accept.ButtonStyle = GenericButton.Style.Shadow;

            Reject = Add(new GenericButton(new(AccRejRect.X + 220, AccRejRect.Y, 220, 48), GameText.Reject, Fonts.Pirulen12));
            Reject.OnClick = OnRejectClicked;
            Reject.ButtonStyle = GenericButton.Style.Shadow;

            SendOffer = Add(new GenericButton(new(R.X + R.Width / 2 - 90, R.Y - 40, 180, 33), GameText.SendOffer, Fonts.Pirulen20));
            SendOffer.ButtonStyle = GenericButton.Style.Shadow;
            SendOffer.OnClick = OnSendOfferClicked;

            RectF offerTextMenu = new(R.X, R.Y, R.Width, R.Height - 30);
            DialogTextBox = Add(new UITextBox(offerTextMenu, useBorder:false));
            DialogTextBox.SetLines(TheirText, Fonts.Consolas18, Color.White);

            DiscussionSL = Add(new ScrollList<DialogOptionListItem>(offerTextMenu, 18));
            DiscussionSL.OnClick = (item) => Respond(item.Option);

            SubTexture ourBkg   = TransientContent.LoadTextureOrDefault("Textures/GameScreens/Negotiate_Right");
            SubTexture theirBkg = TransientContent.LoadTextureOrDefault("Textures/GameScreens/Negotiate_Left");
            int offerW = 220;
            int offerH = 280;
            int offerY = BridgeRect.Bottom - offerH;
            var usRect   = new Rectangle(BridgeRect.Right - (5 + offerW), offerY, offerW, offerH);
            var themRect = new Rectangle(BridgeRect.Left + 5, offerY, offerW, offerH);
            
            OurOffersList = Add(new DiplomacyOffersComponent(Us, Them, usRect, ourBkg));
            OurOffersList.OnOfferChanged = OnOfferChanged;
            TheirOffersList = Add(new DiplomacyOffersComponent(Them, Us, themRect, theirBkg));
            TheirOffersList.OnOfferChanged = OnOfferChanged;
        }

        GenericButton Button(ref Vector2 cursor, GameText title)
        {
            var button = Add(new GenericButton(cursor, title, Fonts.Pirulen20, Fonts.Pirulen16)
            {
                ToggleOnColor = Color.DarkOrange,
                ButtonStyle = GenericButton.Style.Shadow,
            });
            GenericButtons.Add(button);
            cursor.Y += 25f;
            return button;
        }

        GenericButton TAFButton(ref Vector2 cursor, GameText title, bool toggleOn = false)
        {
            var button = Add(new GenericButton(cursor, title, Fonts.Pirulen16, Fonts.Pirulen12)
            {
                ToggleOn = toggleOn,
                ToggleOnColor = Color.DarkOrange,
                ButtonStyle = GenericButton.Style.Shadow,
            });
            cursor.Y += 25f;
            return button;
        }

        Empire[] GetAlliedEmpiresTheyAreAtWarWith(Empire them, Empire us)
        {
            Empire ai = !them.isPlayer ? them : us;
            var empires = new Array<Empire>();
            foreach (Empire empire in them.Universe.MajorEmpiresAtWarWith(ai))
            {
                if (empire.IsAlliedWith(them.Universe.Player))
                    empires.Add(empire);
            }

            return empires.ToArray();
        }

        Empire[] GetAiAlliedEmpires(Empire them, Empire us)
        {
            Empire ai = !them.isPlayer ? them : us;
            var empires = new Array<Empire>();
            foreach (Empire empire in them.Universe.GetAllies(ai))
            {
                if (CanViewAlliance(empire) || CanViewAlliance(ai))
                    empires.Add(empire);
            }

            return empires.ToArray();
        }

        bool CanViewAlliance(Empire e)
        {
            // The player can view peer alliances if it has some relations with this empire
            // Or with the empire's other trade partners.
            var player = e.Universe.Player;
            return e.IsTradeTreaty(player) 
                   || e.IsOpenBordersTreaty(player) 
                   || e.IsAlliedWith(player) 
                   || e.IsNAPactWith(player)
                   || e.Universe.ActiveNonPlayerMajorEmpires.Any(other => other.IsTradeTreaty(e) && other.IsTradeTreaty(player));
        }

        void DoNegotiationResponse(string answer)
        {
            string text = "";
            if (TheirOffer.NAPact && ThemAndUs.HaveRejectedNaPact)
            {
                text = GetDialogueByName("ComeAround_NAPACT") + "\n\n";
            }
            else if (TheirOffer.TradeTreaty && ThemAndUs.HaveRejected_TRADE)
            {
                text = GetDialogueByName("ComeAround_TRADE") + "\n\n";
            }
            text += GetDialogueByName(answer);

            SetDialogText(text, DialogState.Them);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 4 / 5);
            batch.SafeBegin();

            DrawBackground(batch);

            // draw Trust/Anger/Threat bar graph
            TrustRect.Width = (int)ThemAndUs.Trust.Clamped(1, 100);
            batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), TrustRect, Color.Green);
            AngerRect.Width = (int)ThemAndUs.TotalAnger.Clamped(1, 100);
            batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), AngerRect, Color.Yellow);
            ThreatRect.Width = (int)ThemAndUs.Threat.Clamped(1, 100);
            batch.Draw(ResourceManager.Texture("UI/bw_bargradient_2"), ThreatRect, Color.Red);

            DrawAlliesAndWars(batch);

            // set visibility of UIElementV2-s
            OurAttitudeBtn_Pleading.Visible = DState == DialogState.Negotiate;
            OurAttitudeBtn_Threaten.Visible = DState == DialogState.Negotiate;
            OurAttitudeBtn_Respectful.Visible = DState == DialogState.Negotiate;

            SendOffer.Visible = DState == DialogState.Negotiate && (!TheirOffer.IsBlank() || !OurOffer.IsBlank() || OurOffer.Alliance);
            Accept.Visible = DState == DialogState.TheirOffer;
            Reject.Visible = DState == DialogState.TheirOffer;

            TheirOffersList.Visible = OurOffersList.Visible = (DState == DialogState.Negotiate);

            if (DeclareWar != null)
                DeclareWar.Enabled = !UsAndThem.Treaty_Peace;

            bool genericButtonsVisible = DState != DialogState.End;

            foreach (GenericButton b in GenericButtons)
            {
                // TODO: group non-exit buttons together under a single element
                if (b != Exit)
                    b.Visible = genericButtonsVisible;
            }
            
            // TODO: need to fix this ordering issue
            DrawDialogText(batch);

            base.Draw(batch, elapsed);
            
            batch.SafeEnd();
        }

        void DrawDialogText(SpriteBatch batch)
        {
            switch (DState)
            {
                case DialogState.TheirOffer:
                    batch.Draw(ResourceManager.Texture("UI/AcceptReject"), AccRejRect, Color.White);
                    break;
                case DialogState.Negotiate:
                    batch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Tone"), ToneContainerRect, Color.White);
                    break;
            }
        }

        void SetDialogText(string text, DialogState? state = null)
        {
            DState = state ?? DState;
            if (DiscussionSL != null) // null during constructor init
            {
                DiscussionSL.Reset(); // clear option statements
                DiscussionSL.Enabled = false; // because OfferTextSL overlaps StatementsSL
            }

            if (TheirText != text) // only reset the items if necessary
            {
                TheirText = text;
                if (DialogTextBox != null) // null during constructor init
                {
                    DialogTextBox.SetLines(TheirText, Fonts.Consolas18, Color.White);
                    DialogTextBox.Enabled = true; // because OfferTextSL overlaps StatementsSL
                }
            }
        }

        void SetDialogText(Array<DialogOption> options, DialogState? state = null)
        {
            DState = state ?? DState;
            DialogTextBox.Clear();
            DialogTextBox.Enabled = false; // because OfferTextSL overlaps StatementsSL

            TheirText = "";
            DiscussionSL.SetItems(options.Select(o => new DialogOptionListItem(o, DialogAreaWidth)));
            DiscussionSL.Enabled = true; // because OfferTextSL overlaps StatementsSL
        }

        void DrawAlliesAndWars(SpriteBatch batch)
        {
            Font font = Fonts.Arial12Bold;
            Vector2 cursor = new Vector2(Portrait.X + 40,  ThreatRect.Y + 50);
            foreach (Empire empire in AlliedEmpiresAtWar)
            {
                batch.DrawDropShadowText($"They are at war with your ally, {empire.Name}", cursor, font, empire.EmpireColor, 1);
                cursor.Y += font.LineSpacing + 2;
            }

            if (AlliedEmpiresAtWar.Length > 0)
                cursor.Y += font.LineSpacing + 5;

            foreach (Empire empire in EmpiresTheyAreAlliedWith)
            {
                batch.DrawDropShadowText($"They are allied with {empire.Name}", cursor, font, empire.EmpireColor, 1);
                cursor.Y += font.LineSpacing + 2;
            }
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
            OurOffer   = new Offer();
            TheirOffer = new Offer { Them = Them };
            OurOffersList.StartNegotiation(TheirOffersList, OurOffer, TheirOffer);
            TheirOffersList.StartNegotiation(OurOffersList, TheirOffer, OurOffer);

            SetDialogText("", DialogState.Negotiate);
        }

        void OnOfferChanged()
        {
            string dialogText = OurOffer.FormulateOfferText(Attitude, TheirOffer);
            SetDialogText(dialogText);
        }

        string GetDialogueFromAttitude()
        {
            float theirOpinionOfUs = Math.Max(0, ThemAndUs.GetStrength());
            return ParseTextDiplomacy(GetDialogue(theirOpinionOfUs));
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
            return ParseTextDiplomacy(sb.ToString());
        }

        void OnSendOfferClicked(GenericButton b)
        {
            DoNegotiationResponse(Them.AI.AnalyzeOffer(OurOffer, TheirOffer, Us, Attitude));
            OurOffer   = new Offer();
            TheirOffer = new Offer { Them = Them };
        }

        void OnAttitudePleadingClicked(GenericButton b)
        {
            OurAttitudeBtn_Pleading.ToggleOn   = true;
            OurAttitudeBtn_Respectful.ToggleOn = false;
            OurAttitudeBtn_Threaten.ToggleOn   = false;
            Attitude = Offer.Attitude.Pleading;
            OnOfferChanged();
        }

        void OnAttitudeRespectfulClicked(GenericButton b)
        {
            OurAttitudeBtn_Pleading.ToggleOn   = false;
            OurAttitudeBtn_Respectful.ToggleOn = true;
            OurAttitudeBtn_Threaten.ToggleOn   = false;
            Attitude = Offer.Attitude.Respectful;
            OnOfferChanged();
        }

        void OnAttitudeThreatenClicked(GenericButton b)
        {
            OurAttitudeBtn_Pleading.ToggleOn   = false;
            OurAttitudeBtn_Respectful.ToggleOn = false;
            OurAttitudeBtn_Threaten.ToggleOn   = true;
            Attitude = Offer.Attitude.Threaten;
            OnOfferChanged();
        }

        void OnNegotiateClicked(GenericButton b)
        {
            BeginNegotiations();
        }

        void OnAcceptClicked(GenericButton b)
        {
            if (TheirOffer.ValueToModify != null) TheirOffer.ValueToModify.Value = false;
            if (OurOffer.ValueToModify != null)   OurOffer.ValueToModify.Value = true;

            SetDialogText(GetDialogueByName(TheirOffer.AcceptDL), DialogState.End);
            Us.AI.AcceptOffer(OurOffer, TheirOffer, Us, Them, Attitude);
        }

        void OnRejectClicked(GenericButton b)
        {
            if (TheirOffer.ValueToModify != null) TheirOffer.ValueToModify.Value = true;
            if (OurOffer.ValueToModify != null)   OurOffer.ValueToModify.Value = false;
            
            SetDialogText(GetDialogueByName(TheirOffer.RejectDL), DialogState.End);
        }

        void OnDeclareWarClicked(GenericButton b)
        {
            if (UsAndThem.Treaty_NAPact)
            {
                SetDialogText(GetDialogueByName("WarDeclared_FeelsBetrayed"), DialogState.End);
                Us.AI.DeclareWarOn(Them, WarType.ImperialistWar);
            }
            else
            {
                SetDialogText(GetDialogueByName("WarDeclared_Generic"), DialogState.End);
                Us.AI.DeclareWarOn(Them, WarType.ImperialistWar);
            }
        }

        void OnDiscussButtonClicked(GenericButton b)
        {
            Array<DialogOption> options = new();
            foreach (StatementSet set in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
            {
                if (set.Name == "Ordinary Discussion")
                {
                    int n = 1;
                    foreach (DialogOption opt1 in set.DialogOptions)
                    {
                        string str = opt1.SpecialInquiry.NotEmpty() ? GetDialogueByName(opt1.SpecialInquiry) : opt1.Words;
                        options.Add(new DialogOption(n++, ParseTextDiplomacy(str))
                        {
                            Response = opt1.Response
                        });
                    }
                }
            }

            SetDialogText(options, DialogState.Discuss);
        }

        void OnExitClicked(GenericButton b)
        {
            Audio.GameAudio.SwitchBackToGenericMusic();
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            // tooltips for Trust/Anger/Threat bar graphs
            if (TrustRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GameText.ThisIndicatesHowMuchA);
            if (AngerRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GameText.ThisIndicatesHowAngryA);
            if (ThreatRect.HitTest(input.CursorPosition))  ToolTip.CreateTooltip(GameText.ThisIndicatesHowMuchA2);

            return base.HandleInput(input);
        }

        // parses text for any diplomacy placeholders
        string ParseTextDiplomacy(string text)
        {
            if (text == null)
            {
                Log.Error("ParseTextDiplomacy: text was null");
                return "Debug info: Error. Expected " + WhichDialog;
            }

            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
                words[i] = ConvertDiplomacyKeyword(words[i]);

            return string.Join(" ", words);
        }

        string UsSingular => Us?.data.Traits.Singular ?? "HUMAN";
        string UsPlural   => Us?.data.Traits.Plural   ?? "HUMANS";
        string EmpireToDiscussName => EmpireToDiscuss?.data.Traits.Name ?? "EMPIRE";
        string SysToDiscussName    => SysToDiscuss?.Name ?? "SYSTEM";

        string TechDemanded
        {
            get
            {
                string offered = OurOffer?.TechnologiesOffered.Count > 0
                               ? OurOffer.TechnologiesOffered[0] : "TECH";
                if (ResourceManager.TryGetTech(offered, out Technology tech))
                    return tech.Name.Text;
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

            // TODO: refactor this to return text, so we have cleaner SetDialogText
            switch (responseName)
            {
                case "Target_Opinion"               : RespondTargetOpinion(); break;
                case "EmpireDiscuss"                : RespondEmpireDiscuss(); break;
                case "Hardcoded_EmpireChoose"       : RespondHardcodedEmpireChoose(); break;
                case "Hardcoded_War_Analysis"       : RespondHardcodedWarAnalysis(); break;
                case "Hardcoded_Federation_Analysis": RespondHardcodedFederationAnalysis(); break;
                case "Hardcoded_Grievances"         : RespondHardcodedGrievances(); break;
                default:
                    SetDialogText(GetDialogueByName(responseName), DialogState.Them);
                    break;
            }
        }

        void RespondHardcodedGrievances()
        {
            string text = "";
            float num = Math.Max(0, ThemAndUs.GetStrength());
            if (ThemAndUs.TurnsKnown < 20)
            {
                text += GetDialogueByName("Opinion_JustMetUs");
            }
            else if (num > 60f)
            {
                text += GetDialogueByName("Opinion_NoProblems");
            }
            else if (ThemAndUs.WarHistory.Count > 0 &&
                     ThemAndUs.WarHistory[ThemAndUs.WarHistory.Count - 1].EndStarDate -
                     ThemAndUs.WarHistory[ThemAndUs.WarHistory.Count - 1].StartDate < 50f)
            {
                text += GetDialogueByName("PROBLEM_RECENTWAR");
            }
            else if (num >= 0.0)
            {
                bool flag = false;
                if (ThemAndUs.Anger_TerritorialConflict + ThemAndUs.Anger_FromShipsInOurBorders >
                    Them.data.DiplomaticPersonality.Territorialism / 2f)
                {
                    text += GetDialogueByName("Opinion_Problems");
                    flag = true;
                    if (ThemAndUs.Threat > 75f)
                    {
                        text += GetDialogueByName("Problem_Territorial");
                        text += GetDialogueByName("Problem_AlsoMilitary");
                    }
                    else if (ThemAndUs.Threat < -20f && (Them.IsRuthless || Them.IsAggressive))
                    {
                        text += GetDialogueByName("Problem_Territorial");
                        text += GetDialogueByName("Problem_AlsoMilitaryWeak");
                    }
                    else
                    {
                        text += GetDialogueByName("Problem_JustTerritorial");
                    }
                }
                else if (ThemAndUs.Threat > 75f)
                {
                    flag = true;
                    text += GetDialogueByName("Opinion_Problems");
                    text += GetDialogueByName("Problem_PrimaryMilitary");
                }
                else if (ThemAndUs.Threat < -20f && (Them.IsRuthless || Them.IsAggressive))
                {
                    text += GetDialogueByName("Opinion_Problems");
                    text += GetDialogueByName("Problem_MilitaryWeak");
                }

                if (!flag)
                {
                    text += GetDialogueByName("Opinion_NothingMajor");
                }
            }

            SetDialogText(text, DialogState.Them);
        } 

        void RespondHardcodedFederationAnalysis()
        {
            string text = "";

            if (!ThemAndUs.Treaty_Alliance)
            {
                if (ThemAndUs.TurnsKnown < 50 * Them.Universe.P.Pace)
                    text += GetDialogueByName("Federation_JustMet");
                else if (ThemAndUs.GetStrength() >= 75f)
                    text += GetDialogueByName("Federation_NoAlliance");
                else
                    text += GetDialogueByName("Federation_RelationsPoor");
            }
            else if (ThemAndUs.TurnsAllied < ThemAndUs.GetTurnsForFederationWithPlayer(Them))
            {
                text += GetDialogueByName("Federation_AllianceTooYoung");
            }
            else if (Them.TotalScore > Us.TotalScore * 1.5f && Relationship.Is3RdPartyBiggerThenUs(Us, Them))
            {
                    var theirWarTargets = new Array<Empire>();
                    var ourWarTargets = new Array<Empire>();

                    foreach (Relationship rel in Them.AllRelations)
                    {
                        if (!rel.Them.IsFaction && rel.AtWar)
                            theirWarTargets.Add(rel.Them);

                        if (!rel.Them.IsFaction && rel.GetStrength() > 75f && Us.IsAtWarWith(rel.Them))
                        {
                            ourWarTargets.Add(rel.Them);
                        }
                    }

                    if (theirWarTargets.Count > 0)
                    {
                        // enemy of my enemy is a friend
                        EmpireToDiscuss = theirWarTargets.FindMax(e => e.TotalScore);

                        if (EmpireToDiscuss != null)
                        {
                            text += GetDialogueByName("Federation_Quest_DestroyEnemy");
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
                            text += GetDialogueByName("Federation_Quest_AllyFriend");
                            ThemAndUs.FedQuest = new FederationQuest
                            {
                                type = QuestType.AllyFriend,
                                EnemyName = EmpireToDiscuss.data.Traits.Name
                            };
                        }
                    }
                    else
                    {
                        text += GetDialogueByName("Federation_Accept");
                        Us.AbsorbEmpire(Them);
                    }
            }
            else
            {
                text += GetDialogueByName("Federation_WeAreTooStrong");
            }

            SetDialogText(text, DialogState.Them);
        }

        void RespondHardcodedWarAnalysis()
        {
            string text = "";

            if (EmpireToDiscuss != null)
            {
                if (!Us.IsAtWarWith(EmpireToDiscuss))
                {
                    text += GetDialogueByName("JoinWar_YouAreNotAtWar");
                }
                else if (UsAndThem.AtWar)
                {
                    text += GetDialogueByName("JoinWar_WeAreAtWar");
                }
                else if (ThemAndUs.Treaty_Alliance)
                {
                    if (Them.ProcessAllyCallToWar(Us, EmpireToDiscuss, out string dialog))
                    {
                        text += GetDialogueByName(dialog);
                        Them.AI.DeclareWarOn(EmpireToDiscuss, WarType.ImperialistWar, Us.isPlayer);
                        EmpireToDiscuss.AI.GetWarDeclaredOnUs(Them, WarType.ImperialistWar);
                    }
                    else
                    {
                        text += GetDialogueByName(dialog);
                    }
                }
                else if (ThemAndUs.GetStrength() < 30f)
                {
                    text += GetDialogueByName("JoinWar_Reject_PoorRelations");
                }
                else if (Them.IsPacifist || Them.IsHonorable)
                {
                    text += GetDialogueByName("JoinWar_Reject_Pacifist");
                }
                else if (UsAndThem.GetStrength() > 60f)
                {
                    text += GetDialogueByName("JoinWar_Allied_DECLINE");
                }
                else if (ThemAndUs.GetStrength() > 60f && Us.OffensiveStrength > Us.KnownEmpireStrength(EmpireToDiscuss))
                {
                    text += GetDialogueByName("JoinWar_OK");
                    Them.AI.DeclareWarOn(EmpireToDiscuss, WarType.ImperialistWar, Us.isPlayer);
                    EmpireToDiscuss.AI.GetWarDeclaredOnUs(Them, WarType.ImperialistWar);
                }
                else
                {
                    text += GetDialogueByName("JoinWar_Reject_TooDangerous");
                }
            }

            SetDialogText(text, DialogState.Them);
        }

        void RespondHardcodedEmpireChoose()
        {
            Array<DialogOption> options = new();
            int n1 = 1;
            foreach (Relationship rel in Them.AllRelations)
            {
                if (rel.Them != Us && rel.Known && !rel.Them.IsFaction
                    && !rel.Them.IsDefeated && Us.IsKnown(rel.Them))
                {
                    options.Add(new(n1, ParseTextDiplomacy(Localizer.Token(GameText.LetsDiscuss) + " " + rel.Them.data.Traits.Name))
                    {
                        Target = rel.Them,
                        Response = "EmpireDiscuss",
                    });
                    ++n1;
                }
            }
            
            SetDialogText(options);

            if (DiscussionSL.NumEntries == 0)
            {
                SetDialogText(GetDialogueByName("Dunno_Anybody"), DialogState.Them);
            }
        }

        void RespondEmpireDiscuss()
        {
            Array<DialogOption> options = new();

            foreach (StatementSet set in ResourceManager.GetDiplomacyDialog("SharedDiplomacy").StatementSets)
            {
                if (set.Name == "EmpireDiscuss")
                {
                    int n = 1;
                    foreach (DialogOption option1 in set.DialogOptions)
                    {
                        options.Add(new DialogOption(n++, ParseTextDiplomacy(option1.Words))
                        {
                            Response = option1.Response,
                            Target = EmpireToDiscuss
                        });
                    }
                }
            }
            
            SetDialogText(options);
        }

        void RespondTargetOpinion()
        {
            string text = "";
            if (EmpireToDiscuss != null)
            {
                float strength = UsAndThem.GetStrength();
                if (strength >= 65.0)
                    text = GetDialogueByName("Opinion_Positive_" + EmpireToDiscuss.data.Traits.ShipType);
                else if (strength < 65.0 && strength >= 40.0)
                    text = GetDialogueByName("Opinion_Neutral_" + EmpireToDiscuss.data.Traits.ShipType);
                else if (strength < 40.0)
                    text = GetDialogueByName("Opinion_Negative_" + EmpireToDiscuss.data.Traits.ShipType);
            }
            SetDialogText(text, DialogState.Them);
        }

        public override void Update(float fixedDeltaTime)
        {
            RacialVideo ??= new(TransientContent);

            if (!RacialVideo.PlaybackFailed)
                RacialVideo.PlayVideoAndMusic(Them, WarDeclared);

            RacialVideo.Rect = Portrait;
            RacialVideo.Update(this);

            if (Discuss != null) // Discuss button is optional
                Discuss.ToggleOn = DState == DialogState.Discuss;

            Negotiate.ToggleOn = DState == DialogState.Negotiate;

            base.Update(fixedDeltaTime);
        }

        protected override void Dispose(bool disposing)
        {
            RacialVideo?.Dispose();
            base.Dispose(disposing);
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

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
        Rectangle Portrait;
        Vector2 TextCursor;
        DialogState dState;
        Menu2 PlayerMenu;

        readonly Array<GenericButton> GenericButtons = new Array<GenericButton>();

        GenericButton SendOffer;
        GenericButton DeclareWar;
        GenericButton Negotiate;
        GenericButton Discuss;
        GenericButton Exit;

        Rectangle DialogRect;
        Rectangle UsRect;
        Rectangle ThemRect;
        Rectangle BigTradeRect;

        ScrollList StatementsSL;
        ScrollList OurItemsSL;
        ScrollList TheirItemsSL;

        GenericButton Accept;
        GenericButton Reject;
        GenericButton Trust;
        GenericButton Anger;
        GenericButton Fear;
        Array<GenericButton> TAFButtons = new Array<GenericButton>();

        Rectangle Attitude_Pleading_Rect;
        Rectangle Attitude_Respectful_Rect;
        Rectangle Attitude_Threaten_Rect;

        GenericButton ap;
        GenericButton ar;
        GenericButton at;
        Vector2 EmpireNamePos;
        ScrollList OfferTextSL;

        Rectangle R;
        Rectangle BridgeRect;
        Rectangle Negotiate_Right;
        Rectangle Negotiate_Left;
        Rectangle ToneContainerRect;

        Rectangle AccRejRect;
        Rectangle TrustRect;
        Rectangle AngerRect;
        Rectangle FearRect;
        ScreenMediaPlayer RacialVideo;

        Empire Them;
        Empire Us;
        Relationship ThemAndUs; // relationships between Them and Us
        Relationship UsAndThem; // between Us and Them
        string WhichDialog;
        bool WarDeclared;
        string TheirText;

        Offer.Attitude Attitude = Offer.Attitude.Respectful;
        Offer OurOffer = new Offer();
        Offer TheirOffer = new Offer();
        Empire EmpireToDiscuss;
        SolarSystem SysToDiscuss;


        // BASE constructor
        DiplomacyScreen(GameScreen parent, Empire them, Empire us, string whichDialog) : base(parent)
        {
            Them = them;
            Us = us;
            ThemAndUs = them.GetRelations(us);
            ThemAndUs.turnsSinceLastContact = 0;
            UsAndThem = us.GetRelations(them);
            WhichDialog = whichDialog;
            IsPopup = true;
            TransitionOnTime = 1.0f;
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
                    TheirText = GetDialogueByName(whichDialog);
                    dState = DialogState.End;
                    WarDeclared = true;
                    break;
                case "Conquered_Player":
                case "Compliment Military":
                case "Compliment Military Better":
                case "Insult Military":
                    TheirText = GetDialogueByName(whichDialog);
                    dState = DialogState.End;
                    break;
                default:
                    TheirText = GetDialogueFromAttitude();
                    break;
            }
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Empire empireToDiscuss, bool endOnly)
            : this(Empire.Universe, them, us, whichDialog)
        {
            TheirText = GetDialogueByName(whichDialog);
            dState = DialogState.End;
            EmpireToDiscuss = empireToDiscuss;
        }

        DiplomacyScreen(Empire them, Empire us, string whichDialog, Offer ourOffer, Offer theirOffer, Empire targetEmpire)
            : this(Empire.Universe, them, us, whichDialog)
        {
            TheirText = GetDialogueByName(whichDialog);
            dState = DialogState.TheirOffer;
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
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
                    TheirText = GetDialogueByName(whichDialog);
                    dState = DialogState.End;
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
                    TheirText = GetDialogueByName(whichDialog);
                    dState = DialogState.End;
                    WarDeclared = true;
                    break;
                case "Stole Claim":
                case "Stole Claim 2":
                case "Stole Claim 3":
                    TheirText = GetDialogueByName(whichDialog);
                    dState = DialogState.End;
                    break;
                default:
                    TheirText = GetDialogueFromAttitude();
                    break;
            }
        }

        // The screen is loaded during next frame by using deferred add
        static void AddScreen(GameScreen screen) => ScreenManager.Instance.AddScreenDeferred(screen);
        static Empire Player => Empire.Universe.PlayerEmpire;

        public static void Show(Empire them, string which, GameScreen parent)
        {
            AddScreen(new DiplomacyScreen(them, Player, which, parent));
        }

        public static void Show(Empire them, Empire us, string which)
        {
            AddScreen(new DiplomacyScreen(them, us, which, Empire.Universe));
        }

        public static void Show(Empire them, string which)
        {
            AddScreen(new DiplomacyScreen(them, Player, which, Empire.Universe));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which)
        {
            AddScreen(new DiplomacyScreen(them, us, which, null, endOnly:true));
        }

        public static void ShowEndOnly(Empire them, Empire us, string which, Empire empireToDiscuss)
        {
            AddScreen(new DiplomacyScreen(them, us, which, empireToDiscuss, endOnly:true));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer)
        {
            AddScreen(new DiplomacyScreen(them, Player, which, ourOffer, theirOffer, null));
        }

        public static void Show(Empire them, string which, Offer ourOffer, Offer theirOffer, Empire empireToDiscuss)
        {
            AddScreen(new DiplomacyScreen(them, Player, which, ourOffer, theirOffer, empireToDiscuss));
        }

        public static void Show(Empire them, Empire us, string which, Planet planet)
        {
            AddScreen(new DiplomacyScreen(them, us, which, planet));
        }

        public static void Show(Empire them, Empire us, string which, SolarSystem s)
        {
            AddScreen(new DiplomacyScreen(them, us, which, s));
        }

        public static void Show(Empire them, string which, SolarSystem s)
        {
            AddScreen(new DiplomacyScreen(them, Player, which, s));
        }

        public static void Stole1stColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim");
        public static void Stole2ndColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 2");
        public static void Stole3rdColonyClaim(Planet claimedPlanet, Empire victim) => StoleColonyClaim(claimedPlanet, victim, "Stole Claim 3");

        static void StoleColonyClaim(Planet claimedPlanet, Empire victim, string dialog)
        {
            ScreenManager.Instance.AddScreenDeferred(new DiplomacyScreen(victim, Empire.Universe.PlayerEmpire, dialog, claimedPlanet.ParentSystem));
        }


        void DoNegotiationResponse(string answer)
        {
            StatementsSL.Reset();
            TheirText = "";
            if (TheirOffer.NAPact && ThemAndUs.HaveRejectedNapact)
            {
                TheirText = GetDialogueByName("ComeAround_NAPACT") + "\n\n";
            }
            else if (TheirOffer.TradeTreaty && ThemAndUs.HaveRejected_TRADE)
            {
                TheirText = GetDialogueByName("ComeAround_TRADE") + "\n\n";
            }
            TheirText += GetDialogueByName(answer);
            dState = DialogState.Them;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!IsActive)
                return;

            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 4 / 5);
            batch.Begin();
            if (RacialVideo.IsPlaying)
            {
                Color color = Color.White;
                if (WarDeclared || UsAndThem.AtWar)
                {
                    color.B = 100;
                    color.G = 100;
                }
                RacialVideo.Draw(batch, color);
            }
            else
            {
                batch.Draw(ResourceManager.Texture("Portraits/"+Them.PortraitName), Portrait, Color.White);
            }

            HelperFunctions.DrawDropShadowText(batch, Them.data.Traits.Name, EmpireNamePos, Fonts.Pirulen20);
            if (dState == DialogState.Negotiate)
            {
                batch.FillRectangle(new Rectangle(0, R.Y, 1920, R.Height), new Color(0, 0, 0, 150));
            }
            else
            {
                batch.FillRectangle(new Rectangle(0, DialogRect.Y, 1920, R.Height), new Color(0, 0, 0, 150));
            }

            batch.Draw(ResourceManager.Texture("GameScreens/Bridge"), BridgeRect, Color.White);
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
            switch (dState)
            {
                case DialogState.Them:
                {
                    string text = ParseDiplomacyText(TheirText, (DialogRect.Width - 25), Fonts.Consolas18);
                    var position = new Vector2((ScreenWidth / 2f) - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
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
                    pos.Y += (Fonts.Pirulen16.LineSpacing + 15);
                    pos.X -= 8f;
                    pos.Y += (Fonts.Pirulen16.LineSpacing + 15);
                    pos.X -= 8f;
                    pos.Y += (Fonts.Pirulen16.LineSpacing + 15);
                    pos.X -= 8f;

                    batch.End();
                    return;
                }
                case DialogState.Discuss:
                {
                    var selector1 = new Selector(DialogRect, new Color(0, 0, 0, 220));
                    StatementsSL.Draw(batch);
                    var drawCurs = TextCursor;
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
                    var drawCurs = new Vector2((R.X + 15), (R.Y + 10));
                    TheirOffer.Them = Them;
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
                    batch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Right"), Negotiate_Right, Color.White);
                    batch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Left"), Negotiate_Left, Color.White);
                    batch.Draw(ResourceManager.Texture("GameScreens/Negotiate_Tone"), ToneContainerRect, Color.White);
                    DrawOurItems();
                    DrawTheirItems();
                    OfferTextSL.Draw(batch);
                    ap.Transition(Attitude_Pleading_Rect);
                    ap.Draw(ScreenManager);
                    at.Transition(Attitude_Threaten_Rect);
                    at.Draw(ScreenManager);
                    ar.Transition(Attitude_Respectful_Rect);
                    ar.Draw(ScreenManager);
                    drawCurs = new Vector2((UsRect.X + 10), (UsRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 2));
                    batch.DrawString(Fonts.Pirulen12, Localizer.Token(1221), drawCurs, Color.White);
                    drawCurs = new Vector2((ThemRect.X + 10), (ThemRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 2));
                    batch.DrawString(Fonts.Pirulen12, Localizer.Token(1222), drawCurs, Color.White);
                    goto case DialogState.Choosing;
                }
                case DialogState.TheirOffer:
                {
                    batch.Draw(ResourceManager.Texture("UI/AcceptReject"), AccRejRect, Color.White);
                    string text = ParseDiplomacyText(TheirText, DialogRect.Width - 20, Fonts.Consolas18);
                    var position = new Vector2(ScreenWidth / 2f - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
                    DrawDropShadowText(text, position, Fonts.Consolas18);
                    Accept.DrawWithShadow(batch);
                    Reject.DrawWithShadow(batch);
                    goto case DialogState.Choosing;
                }
                case DialogState.End:
                {
                    string text = ParseDiplomacyText(TheirText, DialogRect.Width - 20, Fonts.Consolas18);
                    var position = new Vector2(ScreenWidth / 2f - Fonts.Consolas18.MeasureString(text).X / 2f, TextCursor.Y);
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

        void DrawDropShadowText(string Text, Vector2 Pos, SpriteFont Font)
        {
            var offset = new Vector2(2f, 2f);
            ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
            ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, Color.White);
        }

        void DrawOurItems()
        {
            OurItemsSL.Draw(ScreenManager.SpriteBatch);
            var drawCurs = new Vector2((UsRect.X + 10), (UsRect.Y + Fonts.Pirulen12.LineSpacing + 10));
            foreach (ScrollList.Entry e in OurItemsSL.VisibleExpandedEntries)
            {
                if (!e.Hovered && e.item is ItemToOffer item)
                {
                    item.Update(drawCurs);
                    item.Draw(ScreenManager.SpriteBatch, Fonts.Arial12Bold);
                    drawCurs.Y += (Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
        }

        void DrawTheirItems()
        {
            TheirItemsSL.Draw(ScreenManager.SpriteBatch);
            Vector2 drawCurs = new Vector2(ThemRect.X + 10, ThemRect.Y + Fonts.Pirulen12.LineSpacing + 10);
            foreach (ScrollList.Entry e in TheirItemsSL.VisibleExpandedEntries)
            {
                if (!e.Hovered && e.item is ItemToOffer item)
                {
                    item.Update(drawCurs);
                    item.Draw(ScreenManager.SpriteBatch, Fonts.Arial12Bold);
                    drawCurs.Y += (Fonts.Arial12Bold.LineSpacing + 5);
                }
            }
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
            Dispose();
        }


        class DiplomacyItemsLayout
        {
            readonly ScrollList List;
            ScrollList.Entry Current;
            Vector2 Cursor;

            public DiplomacyItemsLayout(ScrollList list, Rectangle rect)
            {
                List = list;
                Current = null;
                Cursor = new Vector2((rect.X + 10), (rect.Y + Fonts.Pirulen12.LineSpacing + 2));
            }

            void AddItem(int tokenId, string response)
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

        static void FillItems(Empire empire, Empire other, ScrollList list, Rectangle rect)
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

        void CreateOurOffer()
        {
            OurOffer = new Offer();
            FillItems(Us, Them, OurItemsSL, UsRect);
        }

        void CreateTheirOffer()
        {
            TheirOffer = new Offer { Them = Them };
            FillItems(Them, Us, TheirItemsSL, ThemRect);
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
            foreach (DialogLine dl in Them.dd.Dialogs)
            {
                if (dl.DialogType != name)
                    continue;

                if (dl.Default.NotEmpty())
                    sb.Append(dl.Default);

                switch (Them.data.DiplomaticPersonality.Name ?? "")
                {
                    case "Aggressive": sb.Append(dl.DL_Agg);  break;
                    case "Ruthless":   sb.Append(dl.DL_Ruth); break;
                    case "Honorable":  sb.Append(dl.DL_Hon);  break;
                    case "Xenophobic": sb.Append(dl.DL_Xeno); break;
                    case "Pacifist":   sb.Append(dl.DL_Pac);  break;
                    case "Cunning":    sb.Append(dl.DL_Cunn); break;
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
                if (!UsAndThem.Treaty_Peace)
                {
                    if (DeclareWar != null && DeclareWar.HandleInput(input))
                    {
                        StatementsSL.Reset();
                        dState = DialogState.End;
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
                    ToolTip.CreateTooltip(128);
                }

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
                                var opt2 = new DialogOption(n++, str, cursor, Fonts.Consolas18);
                                opt2.Words = ParseDiplomacyText(str, (DialogRect.Width - 20), Fonts.Consolas18);
                                opt2.Response = opt1.Response;
                                StatementsSL.AddItem(opt2);
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
                        DoNegotiationResponse(Them.GetEmpireAI().AnalyzeOffer(OurOffer, TheirOffer, Us, Attitude));
                        OurOffer   = new Offer();
                        TheirOffer = new Offer { Them = Them };
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
                    Us.GetEmpireAI().AcceptOffer(TheirOffer, OurOffer, Us, Them);
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

        void HandleItemToOffer(InputState input, ScrollList ours, ScrollList theirs, Offer ourOffer, Offer theirOffer)
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

        static ItemToOffer FindItemToOffer(ScrollList items, string response)
        {
            foreach (ScrollList.Entry entry in items.AllEntries)
                if (entry.TryGet(out ItemToOffer item))
                    if (item.Response == response)
                        return item;
            return null;
        }

        void ProcessResponse(ItemToOffer item, string response, ScrollList theirs, Offer ourOffer, Offer theirOffer)
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
            BridgeRect = new Rectangle(ScreenWidth / 2 - 960, ScreenHeight / 2 - 540, 1920, 1080);
            PlayerMenu = new Menu2(new Rectangle(ScreenWidth / 2 - 659, 0, 1318, 757));
            Portrait = new Rectangle(ScreenWidth / 2 - 640, ScreenHeight / 2 - 360, 1280, 720);

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

            TrustRect = new Rectangle(Portrait.X + 125, Trust.R.Y + 2, 100, Trust.R.Height);
            AngerRect = new Rectangle(Portrait.X + 125, Anger.R.Y + 2, 100, Anger.R.Height);
            FearRect = new Rectangle(Portrait.X + 125, Fear.R.Y + 2, 100, Fear.R.Height);
            DialogRect = new Rectangle(ScreenWidth / 2 - 350, Portrait.Y + Portrait.Height - 110, 700, 55);

            if (ScreenHeight < 820)
            {
                DialogRect.Y = Portrait.Y + Portrait.Height - 100;
            }
            R = DialogRect;
            R.Height += 75;
            if (R.Y + R.Height > ScreenHeight)
            {
                R.Y -= (R.Y + R.Height - ScreenHeight + 2);
            }

            Rectangle blerdybloo = R;
            blerdybloo.Height -= 40;
            var ot = new Submenu(blerdybloo);

            OfferTextSL = new ScrollList(ot, Fonts.Consolas18.LineSpacing + 2, true);
            Attitude_Pleading_Rect   = new Rectangle(R.X + 45,       R.Y + R.Height - 48, 180, 48);
            Attitude_Respectful_Rect = new Rectangle(R.X + 250 + 5,  R.Y + R.Height - 48, 180, 48);
            Attitude_Threaten_Rect   = new Rectangle(R.X + 450 + 15, R.Y + R.Height - 48, 180, 48);
            ToneContainerRect = new Rectangle(ScreenWidth / 2 - 324, Attitude_Pleading_Rect.Y, 648, 48);
            ap = new GenericButton(Attitude_Pleading_Rect,   Localizer.Token(1207), Fonts.Pirulen12);
            ar = new GenericButton(Attitude_Respectful_Rect, Localizer.Token(1209), Fonts.Pirulen12) { ToggleOn = true };
            at = new GenericButton(Attitude_Threaten_Rect,   Localizer.Token(1208), Fonts.Pirulen12);
            AccRejRect = new Rectangle(R.X + R.Width / 2 - 220, R.Y + R.Height - 48, 440, 48);
            Accept = new GenericButton(new Rectangle(AccRejRect.X, AccRejRect.Y, 220, 48), Localizer.Token(1210), Fonts.Pirulen12);
            Reject = new GenericButton(new Rectangle(AccRejRect.X + 220, AccRejRect.Y, 220, 48), Localizer.Token(1211), Fonts.Pirulen12);

            Negotiate_Right = new Rectangle(ScreenWidth - 242, ScreenHeight - 280, 192, 280);
            Negotiate_Left = new Rectangle(0, ScreenHeight - 280, 192, 280);
            BigTradeRect = new Rectangle(DialogRect.X + 75, DialogRect.Y - 202, DialogRect.Width - 150, 200);

            UsRect = new Rectangle(Negotiate_Right.X + 20, Negotiate_Right.Y + 35, BigTradeRect.Width / 2 - 9, 300);
            ThemRect = new Rectangle(Negotiate_Left.X + 15, Negotiate_Left.Y + 35, BigTradeRect.Width / 2 - 10, 300);
            SendOffer = new GenericButton(new Rectangle(R.X + R.Width / 2 - 90, R.Y - 40, 180, 33), Localizer.Token(1212), Fonts.Pirulen20);

            TheirItemsSL = new ScrollList(new Submenu(ThemRect), Fonts.Consolas18.LineSpacing + 5, true);
            OurItemsSL   = new ScrollList(new Submenu(UsRect), Fonts.Consolas18.LineSpacing + 5, true);
            StatementsSL = new ScrollList(new Submenu(blerdybloo), Fonts.Consolas18.LineSpacing + 2, true);
            TextCursor = new Vector2(DialogRect.X + 5, DialogRect.Y + 5);

            PlayRaceVideoAndMusic();
        }

        string ParseDiplomacyText(string text, float width, SpriteFont font)
        {
            if (text == null)
            {
                return "Debug info: Error. Expected " + WhichDialog;
            }

            width -= 5f;
            string[] words = text.Split(' ');

            for (int i = 0; i < words.Length; i++)
                words[i] = ConvertDiplomacyKeyword(words[i]);

            var completeText = new StringBuilder();
            string line = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                if (font.TextWidth(line + words[i]) > width)
                {
                    completeText.Append(line).Append('\n');
                    line = string.Empty;
                }
                line = line + words[i] + ' ';
            }
            completeText.Append(line);
            return completeText.ToString();
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
                case "Target_Opinion": RespondTargetOpinion(); break;
                case "EmpireDiscuss": RespondEmpireDiscuss(); break;
                case "Hardcoded_EmpireChoose": RespondHardcodedEmpireChoose(); break;
                case "Hardcoded_War_Analysis": RespondHardcodedWarAnalysis(); break;
                case "Hardcoded_Federation_Analysis": RespondHardcodedFederationAnalysis(); break;
                case "Hardcoded_Grievances": RespondHardcodedGrievances(); break;
                default:
                    StatementsSL.Reset();
                    TheirText = GetDialogueByName(responseName);
                    dState = DialogState.Them;
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
                    else if (ThemAndUs.Threat < -20f && (Them.data.DiplomaticPersonality.Name == "Ruthless" ||
                                                         Them.data.DiplomaticPersonality.Name == "Aggressive"))
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
                else if (ThemAndUs.Threat < -20f && (Them.data.DiplomaticPersonality.Name == "Ruthless" ||
                                                     Them.data.DiplomaticPersonality.Name == "Aggressive"))
                {
                    TheirText += GetDialogueByName("Opinion_Problems");
                    TheirText += GetDialogueByName("Problem_MilitaryWeak");
                }

                if (!flag)
                {
                    TheirText += GetDialogueByName("Opinion_NothingMajor");
                }
            }

            dState = DialogState.Them;
        }

        void RespondHardcodedFederationAnalysis()
        {
            StatementsSL.Reset();
            TheirText = "";
            dState = DialogState.Them;
            if (!ThemAndUs.Treaty_Alliance)
            {
                if (ThemAndUs.TurnsKnown < 50)
                    TheirText += GetDialogueByName("Federation_JustMet");
                else if (ThemAndUs.GetStrength() >= 75f)
                    TheirText += GetDialogueByName("Federation_NoAlliance");
                else
                    TheirText += GetDialogueByName("Federation_RelationsPoor");
            }
            else if (ThemAndUs.TurnsAllied < 100)
            {
                TheirText += GetDialogueByName("Federation_AllianceTooYoung");
            }
            else
            {
                if (ThemAndUs.TurnsAllied < 100)
                    return;

                if (Them.TotalScore > Us.TotalScore * 0.8f && ThemAndUs.Threat < 0f)
                {
                    TheirText += GetDialogueByName("Federation_WeAreTooStrong");
                    return;
                }

                var theirWarTargets = new Array<Empire>();
                var ourWarTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Relationship> keyValuePair in Them.AllRelations)
                {
                    if (!keyValuePair.Key.isFaction && keyValuePair.Value.AtWar)
                        theirWarTargets.Add(keyValuePair.Key);

                    if (!keyValuePair.Key.isFaction && keyValuePair.Value.GetStrength() > 75f &&
                        Us.TryGetRelations(keyValuePair.Key, out Relationship relations) && relations.AtWar)
                    {
                        ourWarTargets.Add(keyValuePair.Key);
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
            dState = DialogState.Them;
            if (EmpireToDiscuss == null)
                return;

            if (!Us.GetRelations(EmpireToDiscuss).AtWar)
            {
                TheirText += GetDialogueByName("JoinWar_YouAreNotAtWar");
            }
            else if (UsAndThem.AtWar)
            {
                TheirText += GetDialogueByName("JoinWar_WeAreAtWar");
            }
            else if (ThemAndUs.Treaty_Alliance)
            {
                TheirText += GetDialogueByName("JoinWar_Allied_OK");
                Them.GetEmpireAI().DeclareWarOn(EmpireToDiscuss, WarType.ImperialistWar);
                EmpireToDiscuss.GetEmpireAI().GetWarDeclaredOnUs(Them, WarType.ImperialistWar);
            }
            else if (ThemAndUs.GetStrength() < 30f)
            {
                TheirText += GetDialogueByName("JoinWar_Reject_PoorRelations");
            }
            else if (Them.data.DiplomaticPersonality.Name == "Pacifist" || Them.data.DiplomaticPersonality.Name == "Honorable")
            {
                TheirText += GetDialogueByName("JoinWar_Reject_Pacifist");
            }
            else if (UsAndThem.GetStrength() > 60f)
            {
                TheirText += GetDialogueByName("JoinWar_Allied_DECLINE");
            }
            else if (ThemAndUs.GetStrength() > 60f && EmpireToDiscuss.MilitaryScore < (double) Them.MilitaryScore)
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
            Vector2 cursor1 = TextCursor;
            int n1 = 1;
            foreach (KeyValuePair<Empire, Relationship> rel in Them.AllRelations)
            {
                if (rel.Value.Known && !rel.Key.isFaction && (rel.Key != Us && !rel.Key.data.Defeated) &&
                    Us.GetRelations(rel.Key).Known)
                {
                    var option = new DialogOption(n1, Localizer.Token(2220) + " " + rel.Key.data.Traits.Name, cursor1,
                        Fonts.Consolas18);
                    option.Target = rel.Key;
                    option.Words = ParseDiplomacyText(option.Words, DialogRect.Width - 20, Fonts.Consolas18);
                    option.Response = "EmpireDiscuss";
                    cursor1.Y += Fonts.Consolas18.LineSpacing + 5;
                    StatementsSL.AddItem(option);
                    ++n1;
                }
            }

            if (StatementsSL.NumEntries == 0)
            {
                StatementsSL.Reset();
                TheirText = GetDialogueByName("Dunno_Anybody");
                dState = DialogState.Them;
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
                    Vector2 Cursor = TextCursor;
                    foreach (DialogOption option1 in set.DialogOptions)
                    {
                        var option2 = new DialogOption(n, option1.Words, Cursor, Fonts.Consolas18);
                        option2.Words = ParseDiplomacyText(option1.Words, DialogRect.Width - 20, Fonts.Consolas18);
                        StatementsSL.AddItem(option2);
                        option2.Response = option1.Response;
                        option2.Target = EmpireToDiscuss;
                        Cursor.Y += Fonts.Consolas18.LineSpacing + 5;
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
            dState = DialogState.Them;
        }

        void PlayRaceVideoAndMusic()
        {
            if (RacialVideo == null)
                RacialVideo = new ScreenMediaPlayer(TransientContent);
            RacialVideo.PlayVideoAndMusic(Them, WarDeclared);
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
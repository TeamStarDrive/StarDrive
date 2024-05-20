using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using Ship_Game.Data;
using System.Linq;

namespace Ship_Game
{
    public partial class RaceDesignScreen : GameScreen
    {
        readonly MainMenuScreen MainMenu;
        readonly Array<TraitEntry> AllTraits = new();
        RacialTrait RaceSummary = new();
        UniverseParams P = new();

        Rectangle FlagLeft;
        Rectangle FlagRight;
        Menu2 TitleBar;
        Menu1 NameMenu;
        EnvPreferencesPanel EnvMenu;
        SubmenuScrollList<TraitsListItem> Traits;
        ScrollList<TraitsListItem> TraitsList;
        UIColorPicker Picker;

        UIButton ModeBtn;
        Rectangle FlagRect;
        ScrollList<RaceArchetypeListItem> ChooseRaceList;
        UITextBox DescriptionTextList;

        UILabel NumSystemsLabel;
        UILabel ExtraPlanetsLabel;
        UILabel PerformanceWarning;
        int FlagIndex;
        public int TotalPointsUsed { get; private set; }

        public IEmpireData SelectedData { get; private set; }

        UITextEntry NameEntry;
        UITextEntry SingEntry;
        UITextEntry PlurEntry;
        UITextEntry SysEntry;
        string RaceName    { get => NameEntry.Text; set => NameEntry.Text = value; }
        string Singular    { get => SingEntry.Text; set => SingEntry.Text = value; }
        string Plural      { get => PlurEntry.Text; set => PlurEntry.Text = value; }
        string HomeSysName { get => SysEntry.Text;  set => SysEntry.Text  = value; }
        string HomeWorldName = "Earth";

        public RaceDesignScreen(MainMenuScreen mainMenu) : base(mainMenu, toPause: null)
        {
            IsPopup = true; // it has to be a popup, otherwise the MainMenuScreen will not be drawn
            MainMenu = mainMenu;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            foreach (RacialTraitOption t in ResourceManager.RaceTraits.TraitList)
                AllTraits.Add(new TraitEntry { Trait = t });
        }

        RacialTrait GetRacialTraits()
        {
            RacialTrait t = RaceSummary.GetClone();
            t.Singular = Singular;
            t.Plural   = Plural;
            t.HomeSystemName = HomeSysName;
            t.HomeworldName  = HomeWorldName;
            t.Color     = Picker.CurrentColor;
            t.FlagIndex = FlagIndex;
            t.Name      = RaceName;
            t.ShipType  = SelectedData.ShipType;
            t.VideoPath = SelectedData.VideoPath;

            Array<string> traitOptions = AllTraits.FilterSelect(trait => trait.Selected, trait => trait.Trait.TraitName).ToArrayList();
            TraitSet traitset = new TraitSet();
            traitset.TraitOptions = traitOptions;
            t.TraitSets.Add(traitset);
            t.TraitSets[0].TraitOptions = AllTraits.FilterSelect(trait => trait.Selected, trait => trait.Trait.TraitName).ToArrayList();
            return t;
        }
        
        public void SetCustomSetup(UniverseParams settings)
        {
            P = settings;
        }
        
        Graphics.Font DescriptionTextFont => LowRes ? Fonts.Arial10 : Fonts.Arial12;

        public override void LoadContent()
        {
            TitleBar = Add(new Menu2(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80));
            var titlePos = new Vector2(TitleBar.CenterX - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.DesignYourRace)).X / 2f,
                                       TitleBar.CenterY - Fonts.Laserian14.LineSpacing / 2);
            Add(new UILabel(titlePos, GameText.DesignYourRace, Fonts.Laserian14, Colors.Cream));

            NameMenu = Add(new Menu1(ScreenWidth / 2 - (int)(ScreenWidth * 0.5f) / 2,
                                (int)TitleBar.Bottom + 5, (int)(ScreenWidth * 0.5f), 150, withSub:false));

            var flagPos = new Vector2(NameMenu.Right - 80 - 100, NameMenu.Y + 30);
            FlagRect = new Rectangle((int)flagPos.X, (int)flagPos.Y + 15, 80, 80);
            
            Add(new UILabel(flagPos, GameText.FlagColor, Fonts.Arial14Bold, Color.BurlyWood));
            
            SelectedData = GetDefaultRace(); //SelectedData is used to populate the UI

            UIList raceCustomizatioForm = AddList(new Vector2(NameMenu.X + 40, NameMenu.Y + 30));
            raceCustomizatioForm.Padding = new Vector2(4,4);

            const float padRight = 200f;
            var splitItemWidth = NameMenu.Width - FlagRect.Width - padRight;
            NameEntry = AddSplitter(raceCustomizatioForm, "{EmpireName}: ", SelectedData.Name,splitItemWidth);
            SingEntry = AddSplitter(raceCustomizatioForm, "{RaceNameSingular}: ", SelectedData.Singular, splitItemWidth);
            PlurEntry = AddSplitter(raceCustomizatioForm, "{RaceNamePlural}: ", SelectedData.Plural, splitItemWidth);
            SysEntry = AddSplitter(raceCustomizatioForm,  "{HomeSystemName}: ", SelectedData.HomeSystemName, splitItemWidth);
            HomeWorldName = SelectedData.HomeWorldName;

            RectF traitsList = new(ScreenWidth / 2 - (int)(ScreenWidth * 0.5f) / 2, 
                                  (int)NameMenu.Bottom + 5,
                                  (int)(ScreenWidth * 0.5f), 
                                  (int)(ScreenHeight - TitleBar.Bottom - 0.28f*ScreenHeight));
            if (traitsList.H > 580)
                traitsList.H = 580;

            LocalizedText[] traitNames = { GameText.Physical, GameText.Sociological, GameText.HistoryAndTradition, "Environment" };
            Traits = Add(new SubmenuScrollList<TraitsListItem>(traitsList.Bevel(-20), traitNames));
            Traits.OnTabChange = OnTraitsTabChanged;
            Traits.SetBackground(new Menu1(traitsList));

            TraitsList = Traits.List;
            TraitsList.EnableItemHighlight = true;
            TraitsList.OnClick = OnTraitsListItemClicked;

            RectF chooseRace = new(5, (int)traitsList.Y, (int)traitsList.X - 10, (int)traitsList.H);
            ChooseRaceList = Add(new ScrollList<RaceArchetypeListItem>(chooseRace, 135));
            ChooseRaceList.SetBackground(new Menu1(chooseRace));
            ChooseRaceList.OnClick = OnRaceArchetypeItemClicked;

            foreach (IEmpireData e in ResourceManager.MajorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            Graphics.Font font = LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            float labelX = LowRes ? NameMenu.Right + 20 : NameMenu.Right + 300;
            float labelY = LowRes ? NameMenu.Y - 50 : NameMenu.Y + 3;
            NumSystemsLabel = Add(new UILabel(labelX, labelY, $"Solar Systems: {GetSystemsNum()}"));
            NumSystemsLabel.Font  = font;
            NumSystemsLabel.Color = Color.SteelBlue;

            ExtraPlanetsLabel = Add(new UILabel(NumSystemsLabel.X, NumSystemsLabel.Y + font.LineSpacing + 3, ""));
            ExtraPlanetsLabel.Font  = font;
            ExtraPlanetsLabel.Color = Color.Green;

            PerformanceWarning = Add(new UILabel(NameMenu.Right + 20, NameMenu.Y - 20 , ""));
            PerformanceWarning.Font = font;

            UIList optionButtons = AddList(NameMenu.Right + 40 - 22, NameMenu.Y);
            optionButtons.CaptureInput = true;
            optionButtons.Padding      = new Vector2(2,3);
            optionButtons.Color        = Color.Black.Alpha(0.5f);

            var customStyle = new UIButton.StyleTextures();
            // [ btn_title : ]  lbl_text
            UIButton AddOption(string title, Action<UIButton> onClick,
                               Func<UILabel, string> getText, LocalizedText tip = default)
            {
                var button = new UIButton(customStyle, new Vector2(160, 18), LocalizedText.Parse(title))
                {
                    Font              = Fonts.Arial11Bold, OnClick = onClick,
                    Tooltip           = tip, TextAlign = ButtonTextAlign.Right,
                    AcceptRightClicks = true, TextShadows = true,
                };
                optionButtons.AddSplit(button, new UILabel(getText, Fonts.Arial11Bold)).Split = 180;
                return button;
            }

            string galaxySizeTip = "Sets the scale of the generated galaxy";
            if (GlobalStats.Defaults.ChangeResearchCostBasedOnSize)
                galaxySizeTip += ". Scale other than Medium will increase/decrease research cost of technologies.";
            
            string solarSystemsTip = "Number of Solar Systems packed into the Universe";
            if (GlobalStats.Defaults.ChangeResearchCostBasedOnSize)
                solarSystemsTip += ". Technology research costs will scale up or down as well";

            string opponentsTip = "Sets the number of AI opponents you must face";
            if (GlobalStats.Defaults.ChangeResearchCostBasedOnSize)
                opponentsTip += ". On a large scale galaxy, this might also affect research cost of technologies.";

            AddOption("{GalaxySize} : ",   OnGalaxySizeClicked,  _ => P.GalaxySize.ToString(), tip:galaxySizeTip);
            AddOption("{SolarSystems} : ", OnNumberStarsClicked, _ => P.StarsCount.ToString(), tip:solarSystemsTip);
            AddOption("{Opponents} : ",  OnNumOpponentsClicked,  _ => P.NumOpponents.ToString(), tip:opponentsTip);
            ModeBtn = AddOption("{GameMode} : ",   OnGameModeClicked, _ => GetModeText().Text, tip:GetModeTip());
            AddOption("{Pacing} : ", OnPacingClicked, _ => (P.Pace == 1f) ? "1x" : $"{P.Pace:0.##}x slower", tip:GameText.TheGamesPaceModifiesThe);
            AddOption("{Difficulty} : ", OnDifficultyClicked, _ => P.Difficulty.ToString(),
                tip:"Hard and above increase AI Aggressiveness and gives them extra bonuses");
            AddOption("{RemnantPresence} : ", OnExtraRemnantClicked, _ => P.ExtraRemnant.ToString(),
                tip:"This sets the intensity of Ancient Remnants presence. If you feel overwhelmed by their advanced technology, reduce this to Rare.");

            RectF description = new(traitsList.Right + 5, traitsList.Y, chooseRace.W, traitsList.H);
            DescriptionTextList = Add(new UITextBox(description, useBorder:false, DescriptionTextFont));
            DescriptionTextList.ItemsList.SetBackground(new Menu1(description));
            DescriptionTextList.ItemsList.ItemPadding = new Vector2(10, 0);

            Add(new SelectedTraitsSummary(this));

            Picker = Add(new UIColorPicker(new Rectangle(ScreenWidth / 2 - 310, ScreenHeight / 2 - 280, 620, 560)));
            Picker.Visible = false;

            ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, text:GameText.Engage, click: OnEngageClicked);
            ButtonMedium(10, ScreenHeight - 40, text:GameText.Abort, click: OnAbortClicked);

            const int containerMarginBottom = 10;
            const int containerPaddingLeft = 10;
            DescriptionTextList.ButtonMedium("Clear Traits", OnClearClicked)
                .SetLocalPos(containerPaddingLeft, DescriptionTextList.Height + containerMarginBottom);

            DoRaceDescription();
            SetRacialTraits(SelectedData.Traits);


            var envRect = new Rectangle(5, (int)TitleBar.Bottom + 5, (int)ChooseRaceList.Width, 150);
            EnvMenu = Add(new EnvPreferencesPanel(this, RaceSummary, envRect));
            ChooseRaceList.ButtonMedium("Load Race", OnLoadRaceClicked)
                .SetLocalPos(ChooseRaceList.Width / 2 - 142, ChooseRaceList.Height + 10);
            ChooseRaceList.ButtonMedium("Save Race", OnSaveRaceClicked)
                .SetLocalPos(ChooseRaceList.Width / 2 + 10, ChooseRaceList.Height + 10);

            var pos = new Vector2(ScreenWidth / 2 - 84, traitsList.Y + traitsList.H + 10);
            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, text: GameText.RuleOptions, click: OnRuleOptionsClicked);

            ChooseRaceList.SlideInFromOffset(offset:new(-ChooseRaceList.Width, 0), TransitionOnTime);
            DescriptionTextList.SlideInFromOffset(offset:new(DescriptionTextList.Width, 0), TransitionOnTime);
            EnvMenu.SlideInFromOffset(offset:new(-EnvMenu.Width, 0), TransitionOnTime);

            OnExit += () =>
            {
                ChooseRaceList.SlideOutToOffset(offset:new(-ChooseRaceList.Width, 0), TransitionOffTime);
                DescriptionTextList.SlideOutToOffset(offset:new(DescriptionTextList.Width, 0), TransitionOffTime);
                EnvMenu.SlideOutToOffset(offset:new(-EnvMenu.Width, 0), TransitionOffTime);
            };

            base.LoadContent();
        }
        
        /// <summary>
        /// Extracted from LoadContent() verbatim, defaults to first race if e.Singular == "Human" unavailable
        /// else will throw OutOfBounds
        /// </summary>
        IEmpireData GetDefaultRace()
        {
            var empires = ResourceManager.MajorRaces;
            foreach (IEmpireData e in empires)
                if (e.Singular == "Human")
                    return e;
            return empires[0];
        }

        UITextEntry AddSplitter(UIList list, string title, string inputText, float width)
        {
            const float splitAt = 205f;
            var label = new UILabel(LocalizedText.Parse(title), Fonts.Arial14Bold, Color.BurlyWood);
            var input = new UITextEntry(Vector2.Zero, Fonts.Arial14Bold, inputText)
            {
                Width = width - splitAt,
                DrawUnderline = true,
                Color = Colors.Cream
            };
                
            list.AddSplit(label, input).Split = splitAt;
            return input;
        }

        int GetSystemsNum()
        {
            (int numStars, _) = GetNumStars(P.StarsCount, P.GalaxySize, P.NumOpponents);
            return numStars;
        }

        public static (int NumStars, float StarNumModifier)
            GetNumStars(StarsAbundance abundance, GalSize galaxySize, int numOpponents)
        {
            float starNumModifier;
            switch (abundance)
            {
                case StarsAbundance.VeryRare:    starNumModifier = 0.3f;  break;
                case StarsAbundance.Rare:        starNumModifier = 0.5f;  break;
                case StarsAbundance.Uncommon:    starNumModifier = 0.8f;  break;
                default:
                case StarsAbundance.Normal:      starNumModifier = 1f;    break;
                case StarsAbundance.Abundant:    starNumModifier = 1.1f;  break;
                case StarsAbundance.Crowded:     starNumModifier = 1.25f; break;
                case StarsAbundance.Packed:      starNumModifier = 1.5f;  break;
                case StarsAbundance.SuperPacked: starNumModifier = 1.8f;  break;
            }


            int numSystemsFromSize;
            switch (galaxySize)
            {
                default:
                case GalSize.Tiny:      numSystemsFromSize = 16;  break;
                case GalSize.Small:     numSystemsFromSize = 36;  break;
                case GalSize.Medium:    numSystemsFromSize = 60;  break;
                case GalSize.Large:     numSystemsFromSize = 80;  break;
                case GalSize.Huge:      numSystemsFromSize = 100; break;
                case GalSize.Epic:      numSystemsFromSize = 120; break;
                case GalSize.TrulyEpic: numSystemsFromSize = 150; break;
            }

            int numStars = (int)(numSystemsFromSize * starNumModifier)
                         + ((int)galaxySize + 1) * numOpponents;
            return (numStars, starNumModifier);
        }

        public void OnTraitsTabChanged(int tabIndex)
        {
            string category;
            switch (tabIndex)
            {
                default:
                case 0: category = "Physical"; break;
                case 1: category = "Sociological"; break;
                case 2: category = "HistoryAndTradition";  break;
                case 3: category = "Environment"; break;
            }

            TraitsListItem[] traits = AllTraits.FilterSelect(t => t.Trait.Category == category,
                                                             t => new TraitsListItem(this, t));
            TraitsList.SetItems(traits);
        }

        void OnRuleOptionsClicked(UIButton b)
        {
            ScreenManager.AddScreen(new RuleOptionsScreen(this, P));
        }

        void OnAbortClicked(UIButton b)
        {
            ExitScreen();
        }

        void OnClearClicked(UIButton b)
        {
            foreach (TraitEntry trait in AllTraits)
                trait.Selected = false;
            TotalPointsUsed = P.RacialTraitPoints;
        }

        void OnLoadRaceClicked(UIButton b)
        {
            ScreenManager.AddScreen(new LoadRaceScreen(this));
        }

        void OnSaveRaceClicked(UIButton b)
        {
            ScreenManager.AddScreen(new SaveRaceScreen(this, GetRacialTraits()));
        }

        void OnLoadSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new LoadNewGameSetupScreen(this));
        }

        void OnSaveSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new SaveNewGameSetupScreen(this, P));
        }

        // If we had a left mouse click, increment forward, otherwise decrement
        int OptionIncrement => Input.LeftMouseReleased ? 1 : -1;

        void OnGalaxySizeClicked(UIButton b)
        {
            P.GalaxySize = P.GalaxySize.IncrementWithWrap(OptionIncrement);
        }

        LocalizedText GetModeText()
        {
            switch (P.Mode)
            {
                default:
                case GameMode.Random:        return GameText.RandomGameMode;
                case GameMode.Sandbox:       return GameText.Sandbox;
                case GameMode.Elimination:   return GameText.CapitalElimination;
                case GameMode.Corners:       return GameText.Corners;
                case GameMode.BigClusters:   return GameText.BigClustersGame;
                case GameMode.SmallClusters: return GameText.SmallClustersGame;
                case GameMode.Ring:          return GameText.RingGalaxyGame;
            }
        }

        LocalizedText GetModeTip()
        {
            switch (P.Mode)
            {
                default:
                case GameMode.Random:        return GameText.InRandomGameMode;
                case GameMode.Sandbox:       return GameText.InTheSandboxGameMode;
                case GameMode.Elimination:   return GameText.InTheCapitalEliminationGame;
                case GameMode.Corners:       return GameText.CornersIsARaceMatch;
                case GameMode.BigClusters:   return GameText.EachEmpireStartsInA;
                case GameMode.SmallClusters: return GameText.TheGalaxyWillBeConsisted;
                case GameMode.Ring:          return GameText.RingGalaxyGameTip;
            }
        }

        void OnGameModeClicked(UIButton b)
        {
            P.Mode = P.Mode.IncrementWithWrap(OptionIncrement);
            if (P.Mode == GameMode.Corners) P.NumOpponents = 3;
            ModeBtn.Tooltip = GetModeTip();
        }

        void OnNumberStarsClicked(UIButton b)
        {
            P.StarsCount = P.StarsCount.IncrementWithWrap(OptionIncrement);
        }

        void OnNumOpponentsClicked(UIButton b)
        {
            int maxOpponents = P.Mode == GameMode.Corners ? 3 : GlobalStats.Defaults.MaxOpponents;
            P.NumOpponents += OptionIncrement;
            if (P.NumOpponents > maxOpponents) P.NumOpponents = 1;
            else if (P.NumOpponents < 1)       P.NumOpponents = maxOpponents;
        }

        void OnPacingClicked(UIButton b)
        {
            P.Pace += OptionIncrement*0.5f;
            if (P.Pace > 10f) P.Pace = 1f;
            if (P.Pace < 1f) P.Pace = 10f;
        }
        
        void OnDifficultyClicked(UIButton b)
        {
            P.Difficulty = P.Difficulty.IncrementWithWrap(OptionIncrement);
        }
        
        void OnExtraRemnantClicked(UIButton b)
        {
            P.ExtraRemnant = P.ExtraRemnant.IncrementWithWrap(OptionIncrement);
        }

        public override bool HandleInput(InputState input)
        {
            if (Picker.Visible)
                return Picker.HandleInput(input);

            if (FlagRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                Picker.Visible = !Picker.Visible;
                return true;
            }

            if (FlagRight.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                if (ResourceManager.NumFlags - 1 <= FlagIndex)
                    FlagIndex = 0;
                else
                    FlagIndex += 1;
                GameAudio.BlipClick();
                return true;
            }

            if (FlagLeft.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                if (FlagIndex <= 0)
                    FlagIndex = ResourceManager.NumFlags - 1;
                else
                    FlagIndex -= 1;
                GameAudio.BlipClick();
                return true;
            }

            return base.HandleInput(input);
        }

        void OnTraitsListItemClicked(TraitsListItem item)
        {
            TraitEntry t = item.Trait;
            if (t.Selected && TotalPointsUsed + t.Trait.Cost >= 0)
            {
                t.Selected = !t.Selected;
                TotalPointsUsed += t.Trait.Cost;
                GameAudio.BlipClick();
            }
            else if (TotalPointsUsed - t.Trait.Cost < 0 || t.Selected || t.Excluded)
            {
                GameAudio.NegativeClick();
            }
            else
            {
                bool ok = true;
                foreach (TraitEntry ex in AllTraits)
                {
                    if (t.Trait.Excludes.Contains(ex.Trait.TraitName) && ex.Selected)
                        ok = false;
                }
                if (ok)
                {
                    t.Selected = true;
                    TotalPointsUsed -= t.Trait.Cost;
                    GameAudio.BlipClick();
                }
            }

            UpdateTraits();
            DoRaceDescription();
            EnvMenu.UpdatePreferences(RaceSummary);
        }

        void OnRaceArchetypeItemClicked(RaceArchetypeListItem item)
        {
            SelectedData = item.EmpireData;
            SetRacialTraits(SelectedData.Traits);
            UpdateTraits();
            DoRaceDescription();
            EnvMenu.UpdateArchetype(SelectedData, RaceSummary);
        }

        void OnEngageClicked(UIButton b)
        {
            if (P.Mode == GameMode.Elimination)
                P.EliminationMode = true;

            RaceSummary.Color          = Picker.CurrentColor;
            RaceSummary.Singular       = Singular;
            RaceSummary.Plural         = Plural;
            RaceSummary.HomeSystemName = HomeSysName;
            RaceSummary.HomeworldName  = HomeWorldName;
            RaceSummary.Name           = RaceName;
            RaceSummary.FlagIndex      = FlagIndex;
            RaceSummary.ShipType       = SelectedData.ShipType;
            RaceSummary.VideoPath      = SelectedData.VideoPath;
            RaceSummary.Adj1           = SelectedData.Adj1;
            RaceSummary.Adj2           = SelectedData.Adj2;

            P.PlayerData = SelectedData.CreateInstance(copyTraits: false);
            P.PlayerData.SpyModifier = RaceSummary.SpyMultiplier;
            P.PlayerData.Traits      = RaceSummary;
            P.PlayerData.DiplomaticPersonality = new DTrait();

            (P.NumSystems, P.StarsModifier) = GetNumStars(P.StarsCount, P.GalaxySize, P.NumOpponents);
            var nextScreen = new FoeSelectionScreen(MainMenu, P);
            //var ng = new CreatingNewGameScreen(MainMenu, P);

            ScreenManager.GoToScreen(nextScreen, clear3DObjects:true);
        }

        public override void Update(float fixedDeltaTime)
        {
            CreateRaceSummary();

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            int numSystems = GetSystemsNum();
            NumSystemsLabel.Text = $"Solar Systems: {numSystems}";
            ShowPerformanceWarning(numSystems);
            ShowExtraPlanetsNum(P.ExtraPlanets);

            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, Picker.CurrentColor);
            FlagLeft  = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);

            batch.SafeEnd();
        }

        void ShowExtraPlanetsNum(int extraPlanets)
        {
            ExtraPlanetsLabel.Visible = extraPlanets > 0;
            ExtraPlanetsLabel.Text = $"Extra Planets: {extraPlanets}";
        }

        void ShowPerformanceWarning(int numSystems)
        {
            PerformanceWarning.Visible = numSystems >= 100;
            if (numSystems >= 200)
            {
                PerformanceWarning.Color = NumSystemsLabel.Color = Color.Orange;
                PerformanceWarning.Text = "Warning, performance issues are expected mid to late game.";
            }
            else if (numSystems >= 100)
            {
                PerformanceWarning.Color = NumSystemsLabel.Color = Color.Yellow;
                PerformanceWarning.Text = "Warning, you might experience performance issues late game.";

            }
            else
            {
                NumSystemsLabel.Color = Color.SteelBlue;
            }
        }

        class SelectedTraitsSummary : UIElementV2
        {
            readonly RaceDesignScreen Screen;
            readonly Graphics.Font Font;
            public SelectedTraitsSummary(RaceDesignScreen screen)
            {
                Screen = screen;
                Font = screen.LowRes ? Fonts.Arial10 : Fonts.Arial12Bold;
            }

            public override bool HandleInput(InputState input)
            {
                return false;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                float start = Screen.DescriptionTextList.ItemsList.NumEntries > 0
                            ? Screen.DescriptionTextList.ItemsList.ItemAtBottom.Bottom
                            : Screen.DescriptionTextList.Y;

                var r = new Vector2(Screen.DescriptionTextList.X + 20, start + 20);
                string title = Localizer.Token(GameText.PointsToSpend);
                batch.DrawString(Font, $"{title}: {Screen.TotalPointsUsed}", r, Color.White);
                r.Y += (Font.LineSpacing + 8);
                Vector2 cursor = r;

                int line = 0;
                int maxLines = Screen.LowRes ? 7 : 9;
                bool switchedToNegative = false;
                foreach (TraitEntry t in Screen.AllTraits.OrderByDescending(t => t.Trait.Cost))
                {
                    if (t.Trait.Cost < 0 && !switchedToNegative)
                    {
                        switchedToNegative = true;
                        line = 0;
                        cursor.Y = r.Y;
                        cursor.X += Font.TextWidth(title) + (Screen.LowRes ? 50 : 100);
                    }

                    if (t.Selected)
                    {
                        batch.DrawString(Font, $"({t.Trait.Cost}) {t.Trait.LocalizedName.Text}", cursor,
                                               (t.Trait.Cost > 0 ? Color.ForestGreen: Color.Red));
                        cursor.Y += (Font.LineSpacing + 2);
                        line++;
                    }
                }
            }
        }
        
        public enum GameMode
        {
            Sandbox, Random, Ring, SmallClusters, BigClusters, Elimination, Corners
        }

        public enum StarsAbundance
        {
            VeryRare, Rare, Uncommon, Normal, Abundant, Crowded, Packed, SuperPacked
        }
    }

    public enum GalSize
    {
        Tiny, Small, Medium, Large, Huge, Epic, TrulyEpic
    }

    public enum ExtraRemnantPresence
    {
        VeryRare,Rare, Normal, More, MuchMore, Everywhere
    }
}

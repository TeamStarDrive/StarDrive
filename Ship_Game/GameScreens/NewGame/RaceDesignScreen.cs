using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public partial class RaceDesignScreen : GameScreen
    {
        readonly MainMenuScreen MainMenu;
        readonly Array<TraitEntry> AllTraits = new Array<TraitEntry>();
        RacialTrait RaceSummary = new RacialTrait();

        Rectangle FlagLeft;
        Rectangle FlagRight;
        Menu2 TitleBar;
        Menu1 NameMenu;
        Menu1 EnvMenu;
        Submenu Traits;
        ScrollList2<TraitsListItem> TraitsList;
        UIColorPicker Picker;

        UIButton ModeBtn;
        UILabel EnvPerfTitle;
        UILabel EnvPerfBest;
        UIPanel BestPlanetIcon;
        Rectangle FlagRect;
        ScrollList2<RaceArchetypeListItem> ChooseRaceList;
        ScrollList2<TextListItem> DescriptionTextList;

        GameMode Mode;
        StarNum StarEnum = StarNum.Normal;
        GalSize GalaxySize = GalSize.Medium;
        int Pacing = 100;
        int NumOpponents;
        ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;
        float StarNumModifier = 1;
        UILabel NumSystemsLabel;
        UILabel ExtraPlanetsLabel;
        UILabel PerformanceWarning;
        int FlagIndex;
        public int TotalPointsUsed { get; private set; } = 8;

        UniverseData.GameDifficulty SelectedDifficulty = UniverseData.GameDifficulty.Normal;
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

        UILabel TerranEnvEntry;
        UILabel SteppeEnvEntry;
        UILabel OceanicEnvEntry;
        UILabel SwampEnvEntry;
        UILabel TundraEnvEntry;
        UILabel IceEnvEntry;
        UILabel DesertEnvEntry;
        UILabel BarrenEnvEntry;

        string TerranEnvPerf  { set => TerranEnvEntry.Text  = value; }
        string SteppeEnvPerf  { set => SteppeEnvEntry.Text  = value; }
        string OceanicEnvPerf { set => OceanicEnvEntry.Text = value; }
        string SwampEnvPerf   { set => SwampEnvEntry.Text   = value; }
        string TundraEnvPerf  { set => TundraEnvEntry.Text  = value; }
        string IceEnvPerf     { set => IceEnvEntry.Text     = value; }
        string DesertEnvPerf  { set => DesertEnvEntry.Text  = value; }
        string BarrenEnvPerf  { set => BarrenEnvEntry.Text  = value; }

        public RaceDesignScreen(MainMenuScreen mainMenu) : base(mainMenu)
        {
            IsPopup = true; // it has to be a popup, otherwise the MainMenuScreen will not be drawn
            MainMenu = mainMenu;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            foreach (RacialTrait t in ResourceManager.RaceTraits.TraitList)
            {
                AllTraits.Add(new TraitEntry { trait = t });
            }
            GlobalStats.Statreset();
            int maxOpponentsLimit = ResourceManager.MajorRaces.Count - 1;
            NumOpponents = GlobalStats.ActiveMod?.mi?.DefaultNumOpponents.UpperBound(maxOpponentsLimit) ?? maxOpponentsLimit;
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
            return t;
        }
        
        public void SetCustomSetup(UniverseData.GameDifficulty gameDifficulty,
            StarNum numStars, GalSize galaxySize, int pacing,
            ExtraRemnantPresence extraRemnants, int numOpponents, GameMode mode)
        {
            SelectedDifficulty        = gameDifficulty;
            StarEnum     = numStars;
            GalaxySize   = galaxySize;
            Pacing       = pacing;
            ExtraRemnant = extraRemnants;
            NumOpponents = numOpponents;
            Mode         = mode;
        }
        
        Graphics.Font DescriptionTextFont => LowRes ? Fonts.Arial10 : Fonts.Arial12;

        public override void LoadContent()
        {
            TitleBar = Add(new Menu2(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80));
            var titlePos = new Vector2(TitleBar.CenterX - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.DesignYourRace)).X / 2f,
                                       TitleBar.CenterY - Fonts.Laserian14.LineSpacing / 2);
            Add(new UILabel(titlePos, GameText.DesignYourRace, Fonts.Laserian14, Colors.Cream));

            NameMenu = Add(new Menu1(ScreenWidth / 2 - (int)(ScreenWidth * 0.5f) / 2,
                                (int)TitleBar.Bottom + 5, (int)(ScreenWidth * 0.5f), 150, withSub:true));

            var flagPos = new Vector2(NameMenu.Right - 80 - 100, NameMenu.Y + 30);
            FlagRect = new Rectangle((int)flagPos.X + 16, (int)flagPos.Y + 15, 80, 80);
            
            Add(new UILabel(flagPos, GameText.FlagColor, Fonts.Arial14Bold, Color.BurlyWood));

            UIList raceTitle = AddList(new Vector2(NameMenu.X + 40, NameMenu.Y + 30));
            raceTitle.Padding = new Vector2(4,4);
            UITextEntry AddSplitter(string title, string inputText)
            {
                var input = new UITextEntry(inputText) {Color = Colors.Cream};
                var label = new UILabel(LocalizedText.Parse(title), Fonts.Arial14Bold, Color.BurlyWood);
                raceTitle.AddSplit(label, input).Split = 205f;
                return input;
            }

            foreach (IEmpireData e in ResourceManager.MajorRaces)
                if (e.Singular == "Human")
                    SelectedData = e;

            NameEntry     = AddSplitter("{EmpireName}: ", SelectedData.Name);
            SingEntry     = AddSplitter("{RaceNameSingular}: ", SelectedData.Singular);
            PlurEntry     = AddSplitter("{RaceNamePlural}: ", SelectedData.Plural);
            SysEntry      = AddSplitter("{HomeSystemName}: ", SelectedData.HomeSystemName);
            HomeWorldName = SelectedData.HomeWorldName;

            var traitsList = new Rectangle(ScreenWidth / 2 - (int)(ScreenWidth * 0.5f) / 2, 
                                         (int)NameMenu.Bottom + 5,
                                         (int)(ScreenWidth * 0.5f), 
                                         (int)(ScreenHeight - TitleBar.Bottom - 0.28f*ScreenHeight));
            if (traitsList.Height > 580)
                traitsList.Height = 580;

            Traits = new Submenu(traitsList.Bevel(-20));
            Traits.AddTab(Localizer.Token(GameText.Physical));
            Traits.AddTab(Localizer.Token(GameText.Sociological));
            Traits.AddTab(Localizer.Token(GameText.HistoryAndTradition));
            Traits.OnTabChange = OnTraitsTabChanged;
            Traits.Background = new Menu1(traitsList); 

            TraitsList = Add(new ScrollList2<TraitsListItem>(Traits, 40));
            TraitsList.EnableItemHighlight = true;
            TraitsList.OnClick = OnTraitsListItemClicked;

            var chooseRace = new Menu1(5, traitsList.Y, traitsList.X - 10, traitsList.Height);
            ChooseRaceList = Add(new ScrollList2<RaceArchetypeListItem>(chooseRace, 135));
            ChooseRaceList.OnClick = OnRaceArchetypeItemClicked;

            foreach (IEmpireData e in ResourceManager.MajorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            Graphics.Font font       = LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            float labelX          = LowRes ? NameMenu.Right + 20 : NameMenu.Right + 300;
            float labelY          = LowRes ? NameMenu.Y - 50 : NameMenu.Y + 3;
            NumSystemsLabel       = Add(new UILabel(labelX, labelY, $"Solar Systems: {GetSystemsNum()}"));
            NumSystemsLabel.Font  = font;
            NumSystemsLabel.Color = Color.SteelBlue;

            ExtraPlanetsLabel       = Add(new UILabel(NumSystemsLabel.X, NumSystemsLabel.Y + font.LineSpacing + 3, ""));
            ExtraPlanetsLabel.Font  = font;
            ExtraPlanetsLabel.Color = Color.Green;

            PerformanceWarning      = Add(new UILabel(NameMenu.Right + 20, NameMenu.Y - 20 , ""));
            PerformanceWarning.Font = font;

            UIList optionButtons       = AddList(NameMenu.Right + 40 - 22, NameMenu.Y);
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
            if (GlobalStats.ModChangeResearchCost)
                galaxySizeTip += ". Scale other than Medium will increase/decrease research cost of technologies.";

            string solarSystemsTip = "Number of Solar Systems packed into the Universe";
            if (GlobalStats.ModChangeResearchCost)
                solarSystemsTip += ". Technology research costs will scale up or down as well";

            string opponentsTip = "Sets the number of AI opponents you must face";
            if (GlobalStats.ModChangeResearchCost)
                opponentsTip += ". On a large scale galaxy, this might also affect research cost of technologies.";

            AddOption("{GalaxySize} : ",   OnGalaxySizeClicked,  label => GalaxySize.ToString(), tip: galaxySizeTip);
            AddOption("{SolarSystems} : ", OnNumberStarsClicked, label => StarEnum.ToString(), tip: solarSystemsTip);
            AddOption("{Opponents} : ",  OnNumOpponentsClicked,  label => NumOpponents.ToString(), tip: opponentsTip);
            ModeBtn = AddOption("{GameMode} : ",   OnGameModeClicked, label => GetModeText().Text, tip:GetModeTip());
            AddOption("{Pacing} : ",     OnPacingClicked,     label => Pacing+"%", tip:GameText.TheGamesPaceModifiesThe);
            AddOption("{Difficulty} : ", OnDifficultyClicked, label => SelectedDifficulty.ToString(),
                tip:"Hard and Brutal increase AI Aggressiveness and gives them extra bonuses");
            AddOption("{RemnantPresence} : ", OnExtraRemnantClicked, label => ExtraRemnant.ToString(),
                tip:"This sets the intensity of Ancient Remnants presence. If you feel overwhelmed by their advanced technology, reduce this to Rare.");

            var description = new Menu1(traitsList.Right + 5, traitsList.Y, chooseRace.Rect.Width, traitsList.Height);
            DescriptionTextList = Add(new ScrollList2<TextListItem>(description, DescriptionTextFont.LineSpacing));
            DescriptionTextList.EnableItemEvents = false;
            Add(new SelectedTraitsSummary(this));

            Picker = Add(new UIColorPicker(new Rectangle(ScreenWidth / 2 - 310, ScreenHeight / 2 - 280, 620, 560)));
            Picker.Visible = false;

            ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, text:GameText.Engage, click: OnEngageClicked);
            ButtonMedium(10, ScreenHeight - 40, text:GameText.Abort, click: OnAbortClicked);
            DescriptionTextList.ButtonMedium("Clear Traits", OnClearClicked).SetRelPos(DescriptionTextList.Width - 150, DescriptionTextList.Height - 40);

            DoRaceDescription();
            SetRacialTraits(SelectedData.Traits);

            EnvMenu = Add(new Menu1(5, (int)TitleBar.Bottom + 5, (int)ChooseRaceList.Width+5, 150, withSub: false));
            var envTitlePos  = new Vector2(40, TitleBar.Bottom + 20);
            EnvPerfTitle     = Add(new UILabel(envTitlePos, "Environment Preferences", LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold, Color.BurlyWood));
            EnvPerfBest      = Add(new UILabel(envTitlePos, "Best Planet Type", LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold, Color.BurlyWood));
            EnvPerfBest.SetRelPos(envTitlePos.X + (LowRes ? 175 : 275), envTitlePos.Y);
            CreatePlanetIcon();

            UIList envPerf1  = AddList(new Vector2(envTitlePos.X-20, envTitlePos.Y + 30));
            envPerf1.Padding = new Vector2(4, 4);
            UIList envPerf2  = AddList(new Vector2(envTitlePos.X + (LowRes ? 60 : 120), envTitlePos.Y + 30));
            envPerf2.Padding = envPerf1.Padding;
            UILabel AddEnvSplitter(string title, float envPerfInput, UIList list)
            {
                Color color = Color.White;
                if (envPerfInput.Greater(1)) color = Color.Green;
                if (envPerfInput.Less(1))    color = Color.Red;

                var input = new UILabel(envPerfInput.String(2)) { Color = color, Font = font };
                var label = new UILabel(LocalizedText.Parse(title), font, Color.Wheat);
                list.AddSplit(label, input).Split = LowRes ? 50 :80;
                return input;
            }

            TerranEnvEntry  = AddEnvSplitter("{Terran}: ", SelectedData.EnvPerfTerran, envPerf1);
            SteppeEnvEntry  = AddEnvSplitter("{Steppe}: ", SelectedData.EnvPerfSteppe, envPerf1);
            OceanicEnvEntry = AddEnvSplitter("{Oceanic}: ", SelectedData.EnvPerfOceanic, envPerf1);
            SwampEnvEntry   = AddEnvSplitter("{Swamp}: ", SelectedData.EnvPerfSwamp, envPerf1);

            TundraEnvEntry  = AddEnvSplitter("{Tundra}: ", SelectedData.EnvPerfTundra, envPerf2);
            IceEnvEntry     = AddEnvSplitter("{Ice}: ", SelectedData.EnvPerfIce, envPerf2);
            DesertEnvEntry  = AddEnvSplitter("{Desert}: ", SelectedData.EnvPerfDesert, envPerf2);
            BarrenEnvEntry  = AddEnvSplitter("{Barren}: ", SelectedData.EnvPerfBarren, envPerf2);

            var pos = new Vector2(ScreenWidth / 2 - 84, traitsList.Y + traitsList.Height + 10);

            SetEnvPerfVisibility(envPerf1, envPerf2);

            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, text: GameText.RuleOptions, click: OnRuleOptionsClicked);

            ChooseRaceList.StartTransitionFrom(ChooseRaceList.Pos - new Vector2(ChooseRaceList.Width, 0), TransitionOnTime);
            DescriptionTextList.StartTransitionFrom(DescriptionTextList.Pos + new Vector2(DescriptionTextList.Width, 0), TransitionOnTime);
            EnvMenu.StartTransitionFrom(EnvMenu.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);
            envPerf1.StartTransitionFrom(envPerf1.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);
            envPerf2.StartTransitionFrom(envPerf2.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);
            BestPlanetIcon.StartTransitionFrom(BestPlanetIcon.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);
            EnvPerfTitle.StartTransitionFrom(EnvPerfTitle.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);
            EnvPerfBest.StartTransitionFrom(EnvPerfBest.Pos - new Vector2(EnvMenu.Width, 0), TransitionOnTime);

            OnExit += () =>
            {
                ChooseRaceList.StartTransitionTo(ChooseRaceList.Pos - new Vector2(ChooseRaceList.Width, 0), TransitionOffTime);
                DescriptionTextList.StartTransitionTo(DescriptionTextList.Pos + new Vector2(DescriptionTextList.Width, 0), TransitionOffTime);
                EnvMenu.StartTransitionTo(EnvMenu.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
                envPerf1.StartTransitionTo(envPerf1.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
                envPerf2.StartTransitionTo(envPerf2.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
                BestPlanetIcon.StartTransitionTo(BestPlanetIcon.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
                EnvPerfTitle.StartTransitionTo(EnvPerfTitle.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
                EnvPerfBest.StartTransitionTo(EnvPerfBest.Pos - new Vector2(EnvMenu.Width, 0), TransitionOffTime);
            };

            base.LoadContent();
        }

        int GetSystemsNum()
        {
            StarNumModifier = ((int)StarEnum + 1) * 0.25f;
            int numSystemsFromSize;
            switch (GalaxySize)
            {
                default:
                case GalSize.Tiny:      numSystemsFromSize = 16;  break;
                case GalSize.Small:     numSystemsFromSize = 36;  break;
                case GalSize.Medium:    numSystemsFromSize = 60;  break;
                case GalSize.Large:     numSystemsFromSize = 80;  break;
                case GalSize.Huge:      numSystemsFromSize = 96;  break;
                case GalSize.Epic:      numSystemsFromSize = 112; break;
                case GalSize.TrulyEpic: numSystemsFromSize = 124; break;
            }

            return (int)(numSystemsFromSize * StarNumModifier) + ((int)GalaxySize + 1) * NumOpponents;
        }

        void SetEnvPerfVisibility(UIList list1, UIList list2)
        {
            bool visible = GlobalStats.HasMod && GlobalStats.ActiveModInfo.DisplayEnvPerfInRaceDesign;
            float raceLoadSaveY = visible ? -190 : - 40;

            ChooseRaceList.ButtonMedium("Load Race", OnLoadRaceClicked).SetRelPos(ChooseRaceList.Width / 2 - 142, raceLoadSaveY);
            ChooseRaceList.ButtonMedium("Save Race", OnSaveRaceClicked).SetRelPos(ChooseRaceList.Width / 2 + 10, raceLoadSaveY);

            list1.Visible          = visible;
            list2.Visible          = visible;
            EnvPerfTitle.Visible   = visible;
            EnvMenu.Visible        = visible;
            EnvPerfBest.Visible    = visible;
            BestPlanetIcon.Visible = visible;
        }

        SubTexture GetBestPlanetTex(IEmpireData data)
        {
            string path;
            switch (data.PreferredEnvPlanet)
            {
                default:
                case PlanetCategory.Terran:  path = "Planets/25"; break;
                case PlanetCategory.Steppe:  path = "Planets/18"; break;
                case PlanetCategory.Oceanic: path = "Planets/21"; break;
                case PlanetCategory.Swamp:   path = "Planets/19"; break;
                case PlanetCategory.Tundra:  path = "Planets/11"; break;
                case PlanetCategory.Ice:     path = "Planets/17"; break;
                case PlanetCategory.Desert:  path = "Planets/14"; break;
                case PlanetCategory.Barren:  path = "Planets/16"; break;
            }

            return ResourceManager.Texture(path);
        }

        public void OnTraitsTabChanged(int tabIndex)
        {
            string category;
            switch (tabIndex)
            {
                default:
                case 0: category = "Physical"; break;
                case 1: category = "Industry"; break;
                case 2: category = "Special";  break;
            }

            TraitsListItem[] traits = AllTraits.FilterSelect(t => t.trait.Category == category,
                                                             t => new TraitsListItem(this, t));
            TraitsList.SetItems(traits);
        }

        void OnRuleOptionsClicked(UIButton b)
        {
            ScreenManager.AddScreen(new RuleOptionsScreen(this));
        }

        void OnAbortClicked(UIButton b)
        {
            ExitScreen();
        }

        void OnClearClicked(UIButton b)
        {
            foreach (TraitEntry trait in AllTraits)
                trait.Selected = false;
            TotalPointsUsed = 8;
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
            ScreenManager.AddScreen(new SaveNewGameSetupScreen(this,
                SelectedDifficulty, StarEnum, GalaxySize, Pacing, ExtraRemnant, NumOpponents, Mode));
        }

        // If we had a left mouse click, increment forward, otherwise decrement
        int OptionIncrement => Input.LeftMouseReleased ? 1 : -1;

        void OnGalaxySizeClicked(UIButton b)
        {
            GalaxySize = GalaxySize.IncrementWithWrap(OptionIncrement);
        }

        LocalizedText GetModeText()
        {
            switch (Mode)
            {
                default:
                case GameMode.Sandbox:       return GameText.Sandbox;
                case GameMode.Elimination:   return GameText.CapitalElimination;
                case GameMode.Corners:       return GameText.Corners;
                case GameMode.BigClusters:   return GameText.BigClustersGame;
                case GameMode.SmallClusters: return GameText.SmallClustersGame;
            }
        }

        LocalizedText GetModeTip()
        {
            switch (Mode)
            {
                default:
                case GameMode.Sandbox:       return GameText.InTheSandboxGameMode;
                case GameMode.Elimination:   return GameText.InTheCapitalEliminationGame;
                case GameMode.Corners:       return GameText.CornersIsARaceMatch;
                case GameMode.BigClusters:   return GameText.EachEmpireStartsInA;
                case GameMode.SmallClusters: return GameText.TheGalaxyWillBeConsisted;
            }
        }

        void OnGameModeClicked(UIButton b)
        {
            Mode = Mode.IncrementWithWrap(OptionIncrement);
            if (Mode == GameMode.Corners) NumOpponents = 3;
            ModeBtn.Tooltip = GetModeTip();
        }

        void OnNumberStarsClicked(UIButton b)
        {
            StarEnum = StarEnum.IncrementWithWrap(OptionIncrement);
        }

        void OnNumOpponentsClicked(UIButton b)
        {
            int maxOpponents = Mode == GameMode.Corners ? 3 : GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
            NumOpponents += OptionIncrement;
            if (NumOpponents > maxOpponents) NumOpponents = 1;
            else if (NumOpponents < 1)       NumOpponents = maxOpponents;
        }

        void OnPacingClicked(UIButton b)
        {
            Pacing += 25*OptionIncrement;
            if (Pacing > 400) Pacing = 100;
            if (Pacing < 100) Pacing = 400;
        }
        
        void OnDifficultyClicked(UIButton b)
        {
            SelectedDifficulty = SelectedDifficulty.IncrementWithWrap(OptionIncrement);
        }
        
        void OnExtraRemnantClicked(UIButton b)
        {
            ExtraRemnant = ExtraRemnant.IncrementWithWrap(OptionIncrement);
        }

        public override bool HandleInput(InputState input)
        {
            if (EnvPerfTitle.Visible && EnvPerfTitle.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip("Some races have modifiers to their Max Population and Fertility based on the planet type.");
            }

            if (EnvPerfBest.Visible && EnvPerfBest.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip("This is the best suited environment for this race, Terraforming a planet will transform it to this planet type.");
            }

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
            if (t.Selected && TotalPointsUsed + t.trait.Cost >= 0)
            {
                t.Selected = !t.Selected;
                TotalPointsUsed += t.trait.Cost;
                GameAudio.BlipClick();
                foreach (TraitEntry ex in AllTraits)
                    if (t.trait.Excludes == ex.trait.TraitName)
                        ex.Excluded = false;
            }
            else if (TotalPointsUsed - t.trait.Cost < 0 || t.Selected)
            {
                GameAudio.NegativeClick();
            }
            else
            {
                bool ok = true;
                foreach (TraitEntry ex in AllTraits)
                {
                    if (t.trait.Excludes == ex.trait.TraitName && ex.Selected)
                        ok = false;
                }
                if (ok)
                {
                    t.Selected = true;
                    TotalPointsUsed -= t.trait.Cost;
                    GameAudio.BlipClick();
                    foreach (TraitEntry ex in AllTraits)
                    {
                        if (t.trait.Excludes == ex.trait.TraitName)
                            ex.Excluded = true;
                    }
                }
            }
            DoRaceDescription();
        }

        void OnRaceArchetypeItemClicked(RaceArchetypeListItem item)
        {
            SelectedData = item.EmpireData;
            SetRacialTraits(SelectedData.Traits);
            RefreshEnvPerf(SelectedData);
        }

        void RefreshEnvPerf(IEmpireData data)
        {
            if (!GlobalStats.HasMod || !GlobalStats.ActiveModInfo.DisplayEnvPerfInRaceDesign)
                return;

            TerranEnvPerf  = data.EnvPerfTerran.String(2);
            SteppeEnvPerf  = data.EnvPerfSteppe.String(2);
            OceanicEnvPerf = data.EnvPerfOceanic.String(2);
            SwampEnvPerf   = data.EnvPerfSwamp.String(2);
            TundraEnvPerf  = data.EnvPerfTundra.String(2);
            IceEnvPerf     = data.EnvPerfIce.String(2);
            DesertEnvPerf  = data.EnvPerfDesert.String(2);
            BarrenEnvPerf  = data.EnvPerfBarren.String(2);
            RefreshEnvColors(TerranEnvEntry, data.EnvPerfTerran);
            RefreshEnvColors(SteppeEnvEntry, data.EnvPerfSteppe);
            RefreshEnvColors(OceanicEnvEntry, data.EnvPerfOceanic);
            RefreshEnvColors(SwampEnvEntry, data.EnvPerfSwamp);
            RefreshEnvColors(TundraEnvEntry, data.EnvPerfTundra);
            RefreshEnvColors(IceEnvEntry, data.EnvPerfIce);
            RefreshEnvColors(DesertEnvEntry, data.EnvPerfDesert);
            RefreshEnvColors(BarrenEnvEntry, data.EnvPerfBarren);
            CreatePlanetIcon();
        }

        void CreatePlanetIcon()
        {
            BestPlanetIcon?.RemoveFromParent(true);

            int planetIconSize = LowRes ? 80 : 100;
            var planetIcon = new Rectangle((int)EnvPerfBest.RelPos.X, (int)EnvPerfBest.Pos.Y + 20, planetIconSize, planetIconSize);
            BestPlanetIcon = Add(new UIPanel(planetIcon, GetBestPlanetTex(SelectedData))
            {
                Tooltip = Planet.TextCategory(SelectedData.PreferredEnvPlanet)
            });
        }

        void RefreshEnvColors(UILabel entry, float value)
        {
            Color color                   = Color.White;
            if (value.Greater(1)) color = Color.Green;
            if (value.Less(1))    color = Color.Red;

            entry.Color = color;
        }

        void OnEngageClicked(UIButton b)
        {
            if (Mode == GameMode.Elimination) GlobalStats.EliminationMode = true;

            GlobalStats.ExtraRemnantGS = ExtraRemnant;
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

            var player = new Empire
            {
                EmpireColor = Picker.CurrentColor,
                data        = SelectedData.CreateInstance(copyTraits: false)
            };
            player.data.SpyModifier = RaceSummary.SpyMultiplier;
            player.data.Traits      = RaceSummary;
            player.data.DiplomaticPersonality = new DTrait();

            float pace = Pacing / 100f;
            var ng = new CreatingNewGameScreen(player, GalaxySize, GetSystemsNum(), StarNumModifier, 
                                               NumOpponents, Mode, pace, SelectedDifficulty, MainMenu);

            ScreenManager.GoToScreen(ng, clear3DObjects:true);
        }

        public override void Update(float fixedDeltaTime)
        {
            CreateRaceSummary();

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            int numSystems       = GetSystemsNum();
            NumSystemsLabel.Text = $"Solar Systems: {numSystems}";
            ShowPerformanceWarning(numSystems);
            ShowExtraPlanetsNum(GlobalStats.ExtraPlanets);

            batch.Begin();
            base.Draw(batch, elapsed);
            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, Picker.CurrentColor);
            FlagLeft  = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);

            batch.End();
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
                Font = screen.LowRes ? Fonts.Arial10 : Fonts.Arial14Bold;
            }

            public override bool HandleInput(InputState input)
            {
                return false;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                float start = Screen.DescriptionTextList.NumEntries > 0
                            ? Screen.DescriptionTextList.ItemAtBottom.Bottom
                            : Screen.DescriptionTextList.Y;

                var r = new Vector2(Screen.DescriptionTextList.X + 20, start + 20);
                string title = Localizer.Token(GameText.PointsToSpend);
                batch.DrawString(Font, $"{title}: {Screen.TotalPointsUsed}", r, Color.White);
                r.Y += (Font.LineSpacing + 8);
                Vector2 cursor = r;

                int line = 0;
                int maxLines = Screen.LowRes ? 7 : 9;
                foreach (TraitEntry t in Screen.AllTraits)
                {
                    if (line == maxLines)
                    {
                        line = 0;
                        cursor.Y = r.Y;
                        cursor.X += Font.TextWidth(title) + 8;
                    }
                    if (t.Selected)
                    {
                        batch.DrawString(Font, $"{Localizer.Token(t.trait.TraitName)} {t.trait.Cost}", cursor,
                                               (t.trait.Cost > 0 ? new Color(59, 137, 59) : Color.Crimson));
                        cursor.Y += (Font.LineSpacing + 2);
                        line++;
                    }
                }
            }
        }
        
        public enum GameMode
        {
            Sandbox, SmallClusters, BigClusters, Corners, Elimination
        }

        public enum StarNum
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

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
        Submenu Traits;
        ScrollList2<TraitsListItem> TraitsList;
        UIColorPicker Picker;

        UIButton ModeBtn;

        Rectangle FlagRect;
        ScrollList2<RaceArchetypeListItem> ChooseRaceList;
        ScrollList2<TextListItem> DescriptionTextList;

        GameMode Mode;
        StarNum StarEnum = StarNum.Normal;
        GalSize GalaxySize = GalSize.Medium;
        int Pacing = 100;
        int NumOpponents;
        ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;

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
        int PreferredEnvDescription;

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
            NumOpponents = GlobalStats.ActiveMod?.mi?.MaxOpponents ?? ResourceManager.MajorRaces.Count-1;
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
        
        SpriteFont DescriptionTextFont => LowRes ? Fonts.Arial10 : Fonts.Arial12;

        public override void LoadContent()
        {
            TitleBar = Add(new Menu2(ScreenWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80));
            var titlePos = new Vector2(TitleBar.CenterX - Fonts.Laserian14.MeasureString(Localizer.Token(18)).X / 2f,
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

            NameEntry  = AddSplitter("{EmpireName}: ", SelectedData.Name);
            SingEntry = AddSplitter("{RaceNameSingular}: ", SelectedData.Singular);
            PlurEntry = AddSplitter("{RaceNamePlural}: ", SelectedData.Plural);
            SysEntry = AddSplitter("{HomeSystemName}: ", SelectedData.HomeSystemName);
            HomeWorldName = SelectedData.HomeWorldName;

            var traitsList = new Rectangle(ScreenWidth / 2 - (int)(ScreenWidth * 0.5f) / 2, 
                                         (int)NameMenu.Bottom + 5,
                                         (int)(ScreenWidth * 0.5f), 
                                         (int)(ScreenHeight - TitleBar.Bottom - 0.28f*ScreenHeight));
            if (traitsList.Height > 580)
                traitsList.Height = 580;

            Traits = new Submenu(traitsList.Bevel(-20));
            Traits.AddTab(Localizer.Token(19));
            Traits.AddTab(Localizer.Token(20));
            Traits.AddTab(Localizer.Token(21));
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

            UIList optionButtons = AddList(NameMenu.Right + 40 - 22, NameMenu.Y - 30);
            optionButtons.CaptureInput = true;
            optionButtons.Padding = new Vector2(2,3);
            optionButtons.Color = Color.Black.Alpha(0.5f);

            var customStyle = new UIButton.StyleTextures();
            // [ btn_title : ]  lbl_text
            UIButton AddOption(string title, Action<UIButton> onClick,
                               Func<UILabel, string> getText, ToolTipText tip = default)
            {
                var button = new UIButton(customStyle, new Vector2(160, 18), LocalizedText.Parse(title))
                {
                    Font = Fonts.Arial11Bold, OnClick = onClick,
                    Tooltip = tip, TextAlign = ButtonTextAlign.Right,
                    AcceptRightClicks = true, TextShadows = true,
                };
                optionButtons.AddSplit(button, new UILabel(getText, Fonts.Arial11Bold)).Split = 180;
                return button;
            }

            AddOption("{GalaxySize} : ",   OnGalaxySizeClicked,  label => GalaxySize.ToString(),
                tip:"Sets the scale of the generated galaxy");
            AddOption("{SolarSystems} : ", OnNumberStarsClicked, label => StarEnum.ToString(),
                tip:"Number of Solar Systems packed into the Universe");
            AddOption("{Opponents} : ",  OnNumOpponentsClicked,  label => NumOpponents.ToString(),
                tip:"Sets the number of AI opponents you must face");
            ModeBtn = AddOption("{GameMode} : ",   OnGameModeClicked, label => GetModeText().Text, tip:GetModeTip());
            AddOption("{Pacing} : ",     OnPacingClicked,     label => Pacing+"%", tip:GameTips.Pacing);
            AddOption("{Difficulty} : ", OnDifficultyClicked, label => SelectedDifficulty.ToString(),
                tip:"Hard and Brutal increase AI Aggressiveness and gives them extra bonuses");
            AddOption("{RemnantPresence} : ", OnExtraRemnantClicked, label => ExtraRemnant.ToString(),
                tip:"This sets the intensity of Ancient Remnants presence. If you feel overwhelmed by their advanced technology, reduce this to Rare");

            var description = new Menu1(traitsList.Right + 5, traitsList.Y, chooseRace.Rect.Width, traitsList.Height);
            DescriptionTextList = Add(new ScrollList2<TextListItem>(description, DescriptionTextFont.LineSpacing));
            DescriptionTextList.EnableItemEvents = false;
            Add(new SelectedTraitsSummary(this));

            Picker = Add(new UIColorPicker(new Rectangle(ScreenWidth / 2 - 310, ScreenHeight / 2 - 280, 620, 560)));
            Picker.Visible = false;

            ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, text:22, click: OnEngageClicked);
            ButtonMedium(10, ScreenHeight - 40, text:23, click: OnAbortClicked);
            DescriptionTextList.ButtonMedium("Clear Traits", OnClearClicked).SetRelPos(DescriptionTextList.Width - 150, DescriptionTextList.Height - 40);

            DoRaceDescription();
            SetRacialTraits(SelectedData.Traits);

            ChooseRaceList.ButtonMedium("Load Race", OnLoadRaceClicked).SetRelPos(ChooseRaceList.Width/2 - 142, -40);
            ChooseRaceList.ButtonMedium("Save Race", OnSaveRaceClicked).SetRelPos(ChooseRaceList.Width/2 + 10, -40);

            var pos = new Vector2(ScreenWidth / 2 - 84, traitsList.Y + traitsList.Height + 10);

            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, text: 4006, click: OnRuleOptionsClicked);

            ChooseRaceList.StartTransitionFrom(ChooseRaceList.Pos - new Vector2(ChooseRaceList.Width, 0), TransitionOnTime);
            DescriptionTextList.StartTransitionFrom(DescriptionTextList.Pos + new Vector2(DescriptionTextList.Width, 0), TransitionOnTime);

            OnExit += () =>
            {
                ChooseRaceList.StartTransitionTo(ChooseRaceList.Pos - new Vector2(ChooseRaceList.Width, 0), TransitionOffTime);
                DescriptionTextList.StartTransitionTo(DescriptionTextList.Pos + new Vector2(DescriptionTextList.Width, 0), TransitionOffTime);
            };

            base.LoadContent();
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
                case GameMode.Sandbox: return GameText.Sandbox;
                case GameMode.Elimination: return GameText.CapitalElimination;
                case GameMode.Corners: return GameText.Corners;
            }
        }

        ToolTipText GetModeTip()
        {
            switch (Mode)
            {
                default:
                case GameMode.Sandbox: return GameTips.Sandbox;
                case GameMode.Elimination: return GameTips.EliminationGameMode;
                case GameMode.Corners: return GameTips.CornersGame;
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
        }

        void OnEngageClicked(UIButton b)
        {
            if (Mode == GameMode.Elimination) GlobalStats.EliminationMode = true;
            if (Mode == GameMode.Corners) GlobalStats.CornersGame = true;

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
            RaceSummary.Adj1 = SelectedData.Adj1;
            RaceSummary.Adj2 = SelectedData.Adj2;

            var player = new Empire
            {
                EmpireColor = Picker.CurrentColor,
                data = SelectedData.CreateInstance(copyTraits: false)
            };
            player.data.SpyModifier = RaceSummary.SpyMultiplier;
            player.data.Traits = RaceSummary;

            float modifier = 1f;
            switch (StarEnum)
            {
                case StarNum.VeryRare:    modifier = 0.25f; break;
                case StarNum.Rare:        modifier = 0.50f; break;
                case StarNum.Uncommon:    modifier = 0.75f; break;
                case StarNum.Normal:      modifier = 1.00f; break;
                case StarNum.Abundant:    modifier = 1.25f; break;
                case StarNum.Crowded:     modifier = 1.50f; break;
                case StarNum.Packed:      modifier = 1.75f; break;
                case StarNum.SuperPacked: modifier = 2.00f; break;
            }

            float pace = Pacing / 100f;
            var ng = new CreatingNewGameScreen(player, GalaxySize.ToString(), modifier, 
                                               NumOpponents, Mode, pace, SelectedDifficulty, MainMenu);
            ScreenManager.GoToScreen(ng, clear3DObjects:true);
        }

        public override void Update(float deltaTime)
        {
            CreateRaceSummary();

            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            
            base.Draw(batch);

            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, Picker.CurrentColor);
            FlagLeft = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);

            batch.End();
        }

        class SelectedTraitsSummary : UIElementV2
        {
            readonly RaceDesignScreen Screen;
            readonly SpriteFont Font;
            public SelectedTraitsSummary(RaceDesignScreen screen)
            {
                Screen = screen;
                Font = screen.LowRes ? Fonts.Arial10 : Fonts.Arial14Bold;
            }

            public override bool HandleInput(InputState input)
            {
                return false;
            }

            public override void Draw(SpriteBatch batch)
            {
                float start = Screen.DescriptionTextList.NumEntries > 0
                            ? Screen.DescriptionTextList.ItemAtBottom.Bottom
                            : Screen.DescriptionTextList.Y;

                var r = new Vector2(Screen.DescriptionTextList.X + 20, start + 20);
                string title = Localizer.Token(30);
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
        
        public enum GalSize
        {
            Tiny, Small, Medium, Large, Huge, Epic
        }

        public enum GameMode
        {
            Sandbox, Elimination, Corners
        }

        public enum StarNum
        {
            VeryRare, Rare, Uncommon, Normal, Abundant, Crowded, Packed, SuperPacked
        }
    }

    public enum ExtraRemnantPresence
    {
        Rare, Normal, More, MuchMore, Everywhere
    }
}
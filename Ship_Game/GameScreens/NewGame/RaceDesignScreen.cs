using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
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
        Menu1 TraitsBg;
        UIColorPicker Picker;
        ShipColorPreview ShipPreview;

        Rectangle FlagRect;
        ScrollList<RaceArchetypeListItem> ChooseRaceList;
        UITextBox DescriptionTextList;

        public int FlagIndex { get; set; }
        public Color PlayerColor { get => Picker.CurrentColor; private set => Picker.CurrentColor = value; }

        // a color the player takes other than the race's own ticks the player's hull color box; only the player unticks it
        public void PickPlayerColor(Color color)
        {
            PlayerColor = color;
            if (color != SelectedData.Traits.Color)
                P.PlayerHullColors = true;
        }

        public UniverseParams Params => P;
        // the borders of the upper and lower panel rows, which the Game Setup panels line up with
        public (int Top, int UpperBottom, int LowerTop) PanelRows { get; private set; }
        public int TotalPointsUsed { get; private set; }

        public IEmpireData SelectedData { get; private set; }

        UIButton GameSetupButton;
        UIButton AbortButton;
        // Game Setup's Back and Engage come out of these two
        public Vector2 AbortPos => AbortButton.Pos;
        public Vector2 GameSetupPos => GameSetupButton.Pos;
        public void ShowButtonRow(bool show) => GameSetupButton.Visible = AbortButton.Visible = show;
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
        
        // a setup holds no chosen opponents, so a loaded one keeps those chosen before it, as far as its opponent count allows
        public void SetCustomSetup(UniverseParams settings)
        {
            foreach (IEmpireData race in P.SelectedOpponents)
                settings.SelectedOpponents.Add(race);
            foreach (var look in P.OpponentLooks)
                settings.OpponentLooks[look.Key] = look.Value;
            P = settings;
            PickPlayerColor(PlayerColor); // a color other than the race's own ticks the box again whatever the setup holds
            VerifySelectedOpponents();
        }
        
        Graphics.Font DescriptionTextFont => LowRes ? Fonts.Arial10 
                                                    : HiRes ? Fonts.Arial14Bold : Fonts.Arial12;

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
                                  (int)(ScreenHeight - TitleBar.Bottom - 0.25f*ScreenHeight));
            PanelRows = ((int)NameMenu.Y, (int)NameMenu.Bottom, (int)traitsList.Y);

            LocalizedText[] traitNames = { GameText.Physical, GameText.Sociological, GameText.HistoryAndTradition, "Environment" };
            Traits = Add(new SubmenuScrollList<TraitsListItem>(traitsList.Bevel(-20, -10), traitNames));
            Traits.OnTabChange = OnTraitsTabChanged;

            TraitsList = Traits.List;
            TraitsList.EnableItemHighlight = true;
            TraitsList.OnClick = OnTraitsListItemClicked;
            RectF traitsListBg = new(traitsList.X, traitsList.Y, traitsList.W, traitsList.H);
            var traitsBg = new Menu1(traitsListBg);
            TraitsList.SetBackground(traitsBg);
            // Anchor at abs pos: SetBackground sets a wrong-sign LocalPos that
            // would re-position the Menu1 on the next PerformLayout (triggered by
            // SetItems when a trait tab is clicked). Pinning here makes
            // UpdatePosAndSize no-op so the bg stays put. The slide-in/out is
            // run as a separate animation below to keep the bg in sync with Traits.
            traitsBg.SetAbsPos(traitsListBg.X, traitsListBg.Y);
            TraitsBg = traitsBg;

            RectF chooseRace = new(5, (int)traitsList.Y, (int)traitsList.X - 10, (int)traitsList.H);
            ChooseRaceList = Add(new ScrollList<RaceArchetypeListItem>(chooseRace, 135));
            ChooseRaceList.SetBackground(new Menu1(chooseRace));
            ChooseRaceList.OnClick = OnRaceArchetypeItemClicked;

            foreach (IEmpireData e in ResourceManager.MajorRaces)
                ChooseRaceList.AddItem(new RaceArchetypeListItem(this, e));

            RectF description = new(traitsList.Right + 5, traitsList.Y, chooseRace.W, traitsList.H);
            DescriptionTextList = Add(new UITextBox(description, useBorder:false, DescriptionTextFont));
            DescriptionTextList.ItemsList.SetBackground(new Menu1(description));
            DescriptionTextList.ItemsList.ItemPadding = new Vector2(10, 0);

            Add(new SelectedTraitsSummary(this));

            Picker = Add(new UIColorPicker(new Rectangle(ScreenWidth / 2 - 310, ScreenHeight / 2 - 280, 620, 560)));
            Picker.Visible = false;
            // the cruiser goes under the picker, above the buttons
            int previewTop = ScreenHeight / 2 + 290;
            ShipPreview = Add(new ShipColorPreview(new Rectangle(ScreenWidth / 2 - 310, previewTop, 620, ScreenHeight - 50 - previewTop),
                Picker, TransientContent, () => SelectedData, () => P.PlayerHullColors));

            const int containerMarginBottom = 10;
            const int containerPaddingLeft = 10;
            DescriptionTextList.ButtonMedium("Clear Traits", OnClearClicked)
                .SetLocalPos(containerPaddingLeft, DescriptionTextList.Height + containerMarginBottom);

            GameSetupButton = Button(ButtonStyle.Military, ScreenWidth - 180, ScreenHeight - 40, GameText.GameSetup, click: OnGameSetupClicked);
            AbortButton = Button(ButtonStyle.BigDip, 10, ScreenHeight - 40, GameText.Abort, click: OnAbortClicked);

            DoRaceDescription();
            SetRacialTraits(SelectedData.Traits);


            var envRect = new Rectangle(5, (int)TitleBar.Bottom + 5, (int)ChooseRaceList.Width, 150);
            EnvMenu = Add(new EnvPreferencesPanel(this, RaceSummary, envRect));
            ChooseRaceList.ButtonMedium("Load Race", OnLoadRaceClicked)
                .SetLocalPos(ChooseRaceList.Width / 2 - 152, ChooseRaceList.Height + containerMarginBottom);
            ChooseRaceList.ButtonMedium("Save Race", OnSaveRaceClicked)
                .SetLocalPos(ChooseRaceList.Width / 2 - 10, ChooseRaceList.Height + containerMarginBottom);

            ChooseRaceList.SlideInFromOffset(offset:new(-ChooseRaceList.Width, 0), TransitionOnTime);
            DescriptionTextList.SlideInFromOffset(offset:new(DescriptionTextList.Width, 0), TransitionOnTime);
            EnvMenu.SlideInFromOffset(offset:new(-EnvMenu.Width, 0), TransitionOnTime);
            Traits.SlideInFromOffset(offset: new(0, Traits.Height), TransitionOnTime);
            // TraitsBg is pinned via SetAbsPos so it can't ride Traits' slide via the
            // layout cascade — run a parallel animation on the bg directly.
            TraitsBg.SlideInFromOffset(offset: new(0, Traits.Height), TransitionOnTime);

            OnExit += () =>
            {
                ChooseRaceList.SlideOutToOffset(offset:new(-ChooseRaceList.Width, 0), TransitionOffTime);
                DescriptionTextList.SlideOutToOffset(offset:new(DescriptionTextList.Width, 0), TransitionOffTime);
                EnvMenu.SlideOutToOffset(offset:new(-EnvMenu.Width, 0), TransitionOffTime);
                Traits.SlideOutToOffset(offset: new(0, Traits.Height), TransitionOffTime);
                TraitsBg.SlideOutToOffset(offset: new(0, Traits.Height), TransitionOffTime);
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

        void OnGameSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new GameSetupScreen(this));
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

        public override bool HandleInput(InputState input)
        {
            if (Picker.Visible)
            {
                if (ShipPreview.HandleInput(input))
                    return true;
                Color before = PlayerColor;
                bool captured = Picker.HandleInput(input);
                if (PlayerColor != before)
                    PickPlayerColor(PlayerColor);
                return captured;
            }

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
            VerifySelectedOpponents();
            EnvMenu.UpdateArchetype(SelectedData, RaceSummary);
        }

        public void VerifySelectedOpponents()
        {
            P.DropOpponent(SelectedData);
            for (int i = P.SelectedOpponents.Count - 1; i >= P.NumOpponents; --i)
                P.DropOpponent(P.SelectedOpponents[i]);
        }

        public void Engage()
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

            RaceSummary.TraitSets.Clear();
            RaceSummary.TraitSets.Add(new TraitSet
            {
                TraitOptions = AllTraits.FilterSelect(t => t.Selected, t => t.Trait.TraitName).ToArrayList()
            });

            P.PlayerData = SelectedData.CreateInstance(copyTraits: false);
            P.PlayerData.SpyModifier = RaceSummary.SpyMultiplier;
            P.PlayerData.Traits      = RaceSummary;
            P.PlayerData.DiplomaticPersonality = new DTrait();

            (P.NumSystems, P.StarsModifier) = GetNumStars(P.StarsCount, P.GalaxySize, P.NumOpponents);
            var ng = new CreatingNewGameScreen(MainMenu, P);

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

            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, Picker.CurrentColor);
            FlagLeft  = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);

            batch.SafeEnd();
        }

        class SelectedTraitsSummary : UIElementV2
        {
            readonly RaceDesignScreen Screen;
            readonly Graphics.Font Font;
            public SelectedTraitsSummary(RaceDesignScreen screen)
            {
                Screen = screen;
                Font = screen.LowRes ? Fonts.Arial10 
                                     : screen.HiRes ? Fonts.Arial14Bold : Fonts.Arial12Bold;
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
                bool switchedToNegative = false;
                foreach (TraitEntry t in Screen.AllTraits.OrderByDescending(t => t.Trait.Cost))
                {
                    if (t.Trait.Cost < 0 && !switchedToNegative)
                    {
                        switchedToNegative = true;
                        line = 0;
                        cursor.Y = r.Y;
                        cursor.X += Font.TextWidth(title) + (Screen.LowRes ? 50 : Screen.HiRes ? 150 : 100);
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
            Sandbox, SpiralTwoArm, SpiralFourArm, SpiralBarred, SpiralMagellanic, Random, Ring, SmallClusters, BigClusters, Elimination, Corners
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

using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.UI;
using Ship_Game.Universe;
using static Ship_Game.RaceDesignScreen;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.NewGame
{
    public class GameSetupScreen : GameScreen
    {
        readonly RaceDesignScreen RaceScreen;
        public UniverseParams Params => RaceScreen.Params;
        ScrollList<SelectOpponentListItem> ChooseRaceList;
        UILabel RandomOpponentsCount;
        UILabel NumSystemsLabel;
        UILabel ExtraPlanetsLabel;
        UILabel PerformanceWarning;
        string LateGameWarning;
        string MidGameWarning;
        UIButton ModeBtn;
        UIColorPicker Picker;
        ShipColorPreview ShipPreview;
        IEmpireData PickingColorFor;
        bool PickingPlayerColor;
        bool PickerAwaitsRelease;
        SubTexture PlayerPortrait;
        Rectangle PlayerPortraitRect;
        Rectangle PlayerFlagRect;

        public GameSetupScreen(RaceDesignScreen raceScreen) : base(raceScreen, toPause: null)
        {
            IsPopup = true;
            TransitionOnTime = 0.75f;
            TransitionOffTime = 0.25f;
            RaceScreen = raceScreen;
        }

        public override void LoadContent()
        {
            const int leftWidth = 430, listWidth = 580, pickerWidth = 620, pickerHeight = 560, gap = 10, pad = 25;
            // the color picker opens right of the list, so the panels move left where the screen is too narrow for all three
            int leftX = Math.Min(ScreenWidth / 2 - (leftWidth + gap + listWidth) / 2,
                                 ScreenWidth - gap - pickerWidth - gap - listWidth - gap - leftWidth);
            int listX = leftX + leftWidth + gap;
            int titleY = LowRes ? 10 : 44;
            (int top, int upperBottom, int lowerTop, int bottom) = RaceScreen.PanelRows;

            Menu2 titleBar = Add(new Menu2(new Rectangle(leftX, titleY, leftWidth + gap + listWidth, 80), Color.Black));
            float titleWidth = Fonts.Laserian14.MeasureString(Localizer.Token(GameText.GameSetup)).X;
            Add(new UILabel(new Vector2(titleBar.CenterX - titleWidth / 2f, titleBar.CenterY - Fonts.Laserian14.LineSpacing / 2),
                            GameText.GameSetup, Fonts.Laserian14, Color.Wheat));

            var empirePanel = new Rectangle(leftX, top, leftWidth, upperBottom - top);
            var galaxyPanel = new Rectangle(leftX, lowerTop, leftWidth, 300);
            var extensionsPanel = new Rectangle(leftX, galaxyPanel.Bottom + gap, leftWidth, bottom - galaxyPanel.Bottom - gap);
            Add(new Menu1(empirePanel));
            Add(new Menu1(galaxyPanel));
            Add(new Menu1(extensionsPanel));

            AddHeading(empirePanel, GameText.YourEmpire);
            PlayerPortrait = ResourceManager.Texture("Races/" + RaceScreen.SelectedData.VideoPath);
            PlayerPortraitRect = new Rectangle(empirePanel.X + pad, empirePanel.Y + 38, (int)PlayerPortrait.GetWidthFromHeightAspect(80), 80);
            PlayerFlagRect = new Rectangle(PlayerPortraitRect.Right + 32, PlayerPortraitRect.Y, 80, 80);
            UIList empireBoxes = AddList(new Vector2(empirePanel.X + pad, PlayerPortraitRect.Bottom + 3));
            empireBoxes.AddCheckbox(() => Params.PlayerHullColors, title: GameText.YourHullColor, tooltip: GameText.YourHullColorTip);

            AddHeading(galaxyPanel, GameText.Galaxy);
            AddGalaxyOptions(new Vector2(galaxyPanel.X + pad, galaxyPanel.Y + 40));
            Graphics.Font labelFont = Fonts.Arial12Bold;
            int labelsX = galaxyPanel.X + pad + 20;
            int labelsY = galaxyPanel.Y + 40 + 7 * 21 + 10;
            NumSystemsLabel = Add(new UILabel(new Vector2(labelsX, labelsY), "", labelFont, Color.SteelBlue));
            ExtraPlanetsLabel = Add(new UILabel(new Vector2(labelsX, labelsY + labelFont.LineSpacing + 3), "", labelFont, Color.Green));
            PerformanceWarning = Add(new UILabel(new Vector2(labelsX, labelsY + 2 * (labelFont.LineSpacing + 3)), "", labelFont));
            float warningWidth = galaxyPanel.Right - pad - labelsX;
            LateGameWarning = labelFont.ParseText("Warning, you might experience performance issues late game.", warningWidth);
            MidGameWarning = labelFont.ParseText("Warning, performance issues are expected mid to late game.", warningWidth);

            AddHeading(extensionsPanel, GameText.GameExtensions);
            UIList extensions = AddList(new Vector2(extensionsPanel.X + pad, extensionsPanel.Y + 40));
            extensions.Padding = new Vector2(2f, 10f);
            extensions.AddCheckbox(() => Params.Pirates, title: GameText.PiratesExtension, tooltip: GameText.PiratesExtensionTip);
            extensions.AddCheckbox(() => Params.RemnantStory, title: GameText.RemnantStoryExtension, tooltip: GameText.RemnantStoryExtensionTip);
            extensions.AddCheckbox(() => Params.Infiltration, title: GameText.InfiltrationExtension, tooltip: GameText.InfiltrationExtensionTip);
            extensions.AddCheckbox(() => Params.ResearchStations, title: GameText.ResearchStationsExtension, tooltip: GameText.ResearchStationsExtensionTip);
            extensions.AddCheckbox(() => Params.MiningOps, title: GameText.MiningOpsExtension, tooltip: GameText.MiningOpsExtensionTip);
            bool salvageOpsEnabled = GlobalStats.Defaults.EnableSalvageOps;
            extensions.AddCheckbox(() => Params.SalvageOps, title: GameText.SalvageOpsExtension,
                                   tooltip: salvageOpsEnabled ? GameText.SalvageOpsExtensionTip : GameText.DisableSalvageOpsModTip).Locked = !salvageOpsEnabled;
            Button(ButtonStyle.Default, extensionsPanel.X + extensionsPanel.Width / 2 - 84, extensionsPanel.Bottom - 39,
                   GameText.RuleOptions, click: OnRuleOptionsClicked);

            const int boxesHeight = 40;
            string hint = Fonts.Arial12.ParseText(GameText.ChooseOpponentsHint, listWidth - 90);
            int countY = top + 16 + hint.Split('\n').Length * Fonts.Arial12.LineSpacing + 6;
            RectF racesListRect = new(listX, top, listWidth, bottom - top);
            RectF background = new(racesListRect.X + 10, countY + 26, racesListRect.W - 20, bottom - countY - 36 - boxesHeight);
            Add(new Menu1(racesListRect));
            Add(new UILabel(new Vector2(listX + 30, top + 16), hint, Fonts.Arial12, Colors.Cream));
            ChooseRaceList = Add(new ScrollList<SelectOpponentListItem>(background, 135));
            ChooseRaceList.OnClick = OnRaceItemSelected;
            ChooseRaceList.OnDoubleClick = OnRaceItemSelected;
            RandomOpponentsCount = Add(new UILabel(new Rectangle(listX + 30, countY, 200, 30), "", Fonts.Arial20Bold, Color.White));
            IEmpireData[] majorRaces = ResourceManager.MajorRaces.Filter(
                                       data => data.ArchetypeName != RaceScreen.SelectedData.ArchetypeName);
            foreach (IEmpireData e in majorRaces)
                ChooseRaceList.AddItem(new SelectOpponentListItem(this, e));
            UIList opponentBoxes = AddList(new Vector2(racesListRect.X + 30, background.Bottom + 15));
            opponentBoxes.AddCheckbox(() => Params.OpponentHullColors, title: GameText.OpponentsHullColor, tooltip: GameText.OpponentsHullColorTip);

            Button(ButtonStyle.BigDip, leftX, bottom + 10, GameText.Back, click: _ => ExitScreen());
            Button(ButtonStyle.Military, listX + listWidth - 168, bottom + 10, GameText.Engage, click: _ => RaceScreen.Engage());

            Picker = Add(new UIColorPicker(new Rectangle(listX + listWidth + gap, top, pickerWidth, pickerHeight)));
            Picker.Visible = false;
            int previewTop = top + pickerHeight + gap;
            ShipPreview = Add(new ShipColorPreview(new Rectangle(listX + listWidth + gap, previewTop, pickerWidth, bottom - previewTop),
                Picker, TransientContent, () => PickingPlayerColor ? RaceScreen.SelectedData : PickingColorFor,
                () => PickingPlayerColor ? Params.PlayerHullColors : Params.OpponentHullColors && Params.IsColorPicked(PickingColorFor)));

            base.LoadContent();
        }

        void AddHeading(in Rectangle panel, LocalizedText text)
        {
            Add(new UILabel(new Vector2(panel.X + 25, panel.Y + 10), text, Fonts.Arial20Bold, Color.Wheat));
        }

        void AddGalaxyOptions(Vector2 pos)
        {
            UIList optionButtons = AddList(pos);
            optionButtons.CaptureInput = true;
            optionButtons.Padding      = new Vector2(2,3);

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

            AddOption("{GalaxySize} : ",   OnGalaxySizeClicked,  _ => Params.GalaxySize.ToString(), tip:galaxySizeTip);
            AddOption("{SolarSystems} : ", OnNumberStarsClicked, _ => Params.StarsCount.ToString(), tip:solarSystemsTip);
            AddOption("{Opponents} : ",  OnNumOpponentsClicked,  _ => Params.NumOpponents.ToString(), tip:opponentsTip);
            ModeBtn = AddOption("{GameMode} : ",   OnGameModeClicked, _ => GetModeText().Text, tip:GetModeTip());
            AddOption("{Pacing} : ", OnPacingClicked, _ => (Params.Pace == 1f) ? "1x" : $"{Params.Pace:0.##}x slower", tip:GameText.TheGamesPaceModifiesThe);
            AddOption("{Difficulty} : ", OnDifficultyClicked, _ => Params.Difficulty.ToString(),
                tip:"Hard and above increase AI Aggressiveness and gives them extra bonuses");
            AddOption("{RemnantPresence} : ", OnExtraRemnantClicked, _ => Params.ExtraRemnant.ToString(),
                tip:"This sets the intensity of Ancient Remnants presence. If you feel overwhelmed by their advanced technology, reduce this to Rare.");
        }

        // If we had a left mouse click, increment forward, otherwise decrement
        int OptionIncrement => Input.LeftMouseReleased ? 1 : -1;

        void OnGalaxySizeClicked(UIButton b)
        {
            Params.GalaxySize = Params.GalaxySize.IncrementWithWrap(OptionIncrement);
        }

        LocalizedText GetModeText()
        {
            switch (Params.Mode)
            {
                default:
                case GameMode.Random:        return GameText.RandomGameMode;
                case GameMode.Sandbox:       return GameText.Sandbox;
                case GameMode.Elimination:   return GameText.CapitalElimination;
                case GameMode.Corners:       return GameText.Corners;
                case GameMode.BigClusters:   return GameText.BigClustersGame;
                case GameMode.SmallClusters: return GameText.SmallClustersGame;
                case GameMode.Ring:           return GameText.RingGalaxyGame;
                case GameMode.SpiralTwoArm:     return GameText.SpiralTwoArmGalaxyGame;
                case GameMode.SpiralFourArm:    return GameText.SpiralFourArmGalaxyGame;
                case GameMode.SpiralBarred:     return GameText.SpiralBarredGalaxyGame;
                case GameMode.SpiralMagellanic: return GameText.SpiralMagellanicGalaxyGame;
            }
        }

        LocalizedText GetModeTip()
        {
            switch (Params.Mode)
            {
                default:
                case GameMode.Random:         return GameText.InRandomGameMode;
                case GameMode.Sandbox:        return GameText.InTheSandboxGameMode;
                case GameMode.Elimination:    return GameText.InTheCapitalEliminationGame;
                case GameMode.Corners:        return GameText.CornersIsARaceMatch;
                case GameMode.BigClusters:    return GameText.EachEmpireStartsInA;
                case GameMode.SmallClusters:  return GameText.TheGalaxyWillBeConsisted;
                case GameMode.Ring:           return GameText.RingGalaxyGameTip;
                case GameMode.SpiralTwoArm:     return GameText.SpiralTwoArmGalaxyGameTip;
                case GameMode.SpiralFourArm:    return GameText.SpiralFourArmGalaxyGameTip;
                case GameMode.SpiralBarred:     return GameText.SpiralBarredGalaxyGameTip;
                case GameMode.SpiralMagellanic: return GameText.SpiralMagellanicGalaxyGameTip;
            }
        }

        void OnGameModeClicked(UIButton b)
        {
            Params.Mode = Params.Mode.IncrementWithWrap(OptionIncrement);
            if (Params.Mode == GameMode.Corners)
            {
                Params.NumOpponents = 3;
                RaceScreen.VerifySelectedOpponents();
            }
        }

        void OnNumberStarsClicked(UIButton b)
        {
            Params.StarsCount = Params.StarsCount.IncrementWithWrap(OptionIncrement);
        }

        void OnNumOpponentsClicked(UIButton b)
        {
            int maxOpponents = Params.Mode == GameMode.Corners ? 3 : GlobalStats.Defaults.MaxOpponents;
            Params.NumOpponents += OptionIncrement;
            if (Params.NumOpponents > maxOpponents) Params.NumOpponents = 1;
            else if (Params.NumOpponents < 1)       Params.NumOpponents = maxOpponents;
            RaceScreen.VerifySelectedOpponents();
        }

        void OnPacingClicked(UIButton b)
        {
            Params.Pace += OptionIncrement*0.5f;
            if (Params.Pace > 10f) Params.Pace = 1f;
            if (Params.Pace < 1f) Params.Pace = 10f;
        }

        void OnDifficultyClicked(UIButton b)
        {
            Params.Difficulty = Params.Difficulty.IncrementWithWrap(OptionIncrement);
        }

        void OnExtraRemnantClicked(UIButton b)
        {
            Params.ExtraRemnant = Params.ExtraRemnant.IncrementWithWrap(OptionIncrement);
        }

        void OnRuleOptionsClicked(UIButton b)
        {
            ScreenManager.AddScreen(new RuleOptionsScreen(this, RaceScreen));
        }

        public override bool HandleInput(InputState input)
        {
            if (Picker.Visible)
            {
                // the click that opened the picker picks no color
                if (PickerAwaitsRelease)
                {
                    PickerAwaitsRelease = input.LeftMouseDown;
                    return true;
                }
                return ShipPreview.HandleInput(input) || Picker.HandleInput(input);
            }

            if (input.LeftMouseClick)
            {
                if (PlayerFlagRect.HitTest(input.CursorPosition))
                {
                    OpenPlayerColorPicker();
                    return true;
                }
                if (FlagLeftArrow(PlayerFlagRect).HitTest(input.CursorPosition))
                {
                    StepPlayerFlag(-1);
                    return true;
                }
                if (FlagRightArrow(PlayerFlagRect).HitTest(input.CursorPosition))
                {
                    StepPlayerFlag(+1);
                    return true;
                }
            }

            if (input.RightMouseClick && ChooseRaceList.HitTest(input.CursorPosition))
                return true;

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.Draw(PlayerPortrait, PlayerPortraitRect, Color.White);
            batch.DrawRectangle(PlayerPortraitRect, RaceScreen.PlayerColor, thickness: 2);
            DrawFlag(batch, PlayerFlagRect, RaceScreen.FlagIndex, RaceScreen.PlayerColor);
            batch.SafeEnd();
        }

        public override void Update(float fixedDeltaTime)
        {
            RandomOpponentsCount.Text = $"Random Opponents: {Params.NumOpponents - Params.SelectedOpponents.Count}";
            RandomOpponentsCount.Color = Params.SelectedOpponents.Count == Params.NumOpponents ? Color.Gray : Color.Green;
            int numSystems = GetNumStars(Params.StarsCount, Params.GalaxySize, Params.NumOpponents).NumStars;
            NumSystemsLabel.Text = $"Solar Systems: {numSystems}";
            ShowPerformanceWarning(numSystems);
            ExtraPlanetsLabel.Visible = Params.ExtraPlanets > 0;
            ExtraPlanetsLabel.Text = $"Extra Planets: {Params.ExtraPlanets}";
            ModeBtn.Tooltip = GetModeTip();
            if (Picker.Visible)
            {
                if (PickingPlayerColor && Picker.CurrentColor != RaceScreen.PlayerColor)
                    RaceScreen.PickPlayerColor(Picker.CurrentColor);
                else if (PickingColorFor != null && Picker.CurrentColor != LookOf(PickingColorFor).Color)
                    Params.OpponentLooks[PickingColorFor] = (Picker.CurrentColor, LookOf(PickingColorFor).Flag);
            }
            base.Update(fixedDeltaTime);
        }

        void ShowPerformanceWarning(int numSystems)
        {
            PerformanceWarning.Visible = numSystems >= 100;
            if (numSystems >= 200)
            {
                PerformanceWarning.Color = NumSystemsLabel.Color = Color.Orange;
                PerformanceWarning.Text = MidGameWarning;
            }
            else if (numSystems >= 100)
            {
                PerformanceWarning.Color = NumSystemsLabel.Color = Color.Yellow;
                PerformanceWarning.Text = LateGameWarning;
            }
            else
            {
                NumSystemsLabel.Color = Color.SteelBlue;
            }
        }

        public static Rectangle FlagLeftArrow(in Rectangle flag) => new(flag.X - 20, flag.CenterY() - 10, 20, 20);
        public static Rectangle FlagRightArrow(in Rectangle flag) => new(flag.Right, flag.CenterY() - 10, 20, 20);

        // a flag in its color, with the arrows that step through the flags
        public static void DrawFlag(SpriteBatch batch, in Rectangle flag, int flagIndex, Color color)
        {
            batch.Draw(ResourceManager.Flag(flagIndex), flag, color);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeftArrow(flag), Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRightArrow(flag), Color.BurlyWood);
        }

        static int StepFlagIndex(int flag, int step)
        {
            int numFlags = ResourceManager.NumFlags;
            return ((flag + step) % numFlags + numFlags) % numFlags;
        }

        // the color and flag a chosen opponent plays with: the ones picked for it, else its race's own
        public (Color Color, int Flag) LookOf(IEmpireData race)
            => Params.OpponentLooks.TryGetValue(race, out (Color Color, int Flag) look) ? look : (race.Traits.Color, race.Traits.FlagIndex);

        public void PickColor(IEmpireData race)
        {
            PickingColorFor = race;
            PickingPlayerColor = false;
            OpenPicker(LookOf(race).Color);
        }

        void OpenPlayerColorPicker()
        {
            PickingColorFor = null;
            PickingPlayerColor = true;
            OpenPicker(RaceScreen.PlayerColor);
        }

        void OpenPicker(Color color)
        {
            Picker.CurrentColor = color;
            Picker.Visible = true;
            PickerAwaitsRelease = true;
        }

        public void StepFlag(IEmpireData race, int step)
        {
            (Color color, int flag) = LookOf(race);
            Params.OpponentLooks[race] = (color, StepFlagIndex(flag, step));
            GameAudio.BlipClick();
        }

        void StepPlayerFlag(int step)
        {
            RaceScreen.FlagIndex = StepFlagIndex(RaceScreen.FlagIndex, step);
            GameAudio.BlipClick();
        }

        void OnRaceItemSelected(SelectOpponentListItem item)
        {
            if (Params.DropOpponent(item.EmpireData))
                return;

            if (Params.SelectedOpponents.Count >= Params.NumOpponents)
                GameAudio.NegativeClick();
            else
                Params.SelectedOpponents.Add(item.EmpireData);
        }
    }
}

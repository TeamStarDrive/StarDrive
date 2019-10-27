using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public partial class RaceDesignScreen : GameScreen, IListScreen
    {
        protected MainMenuScreen MainMenu;
        protected Array<TraitEntry> AllTraits = new Array<TraitEntry>();
        protected RacialTrait RaceSummary = new RacialTrait();

        int GameScale = 1;
        GameMode Mode;
        StarNum StarEnum = StarNum.Normal;
        GalSize GalaxySize = GalSize.Medium;
        
        protected Rectangle FlagLeft;
        protected Rectangle FlagRight;
        protected Rectangle GalaxySizeRect;
        Rectangle NumberStarsRect;
        Rectangle NumOpponentsRect;
        protected Menu2 TitleBar;
        protected Vector2 TitlePos;
        protected Menu1 Left;
        protected Menu1 NameMenu;
        protected Menu1 Description;

        protected bool LowRes;

        protected Submenu Traits;
        protected Submenu NameSub;

        protected ScrollList<TraitsListItem> traitsSL;
        protected Menu1 ColorSelectMenu;

        protected UITextEntry RaceName = new UITextEntry();
        protected UITextEntry SingEntry = new UITextEntry();
        protected UITextEntry PlurEntry = new UITextEntry();
        protected UITextEntry HomeSystemEntry = new UITextEntry();

        protected Vector2 RaceNamePos;
        protected Vector2 FlagPos;

        protected Rectangle FlagRect;
        Menu1 ChooseRaceMenu;
        ScrollList<RaceArchetypeListItem> RaceArchetypeSL;
        Submenu arch;
        Rectangle PacingRect;

        int Pacing = 100;

        Rectangle ScaleRect = new Rectangle();
        Rectangle dslrect;
        Rectangle GameModeRect;
        Rectangle DifficultyRect;

        public Map<IEmpireData, SubTexture> TextureDict { get; } = new Map<IEmpireData, SubTexture>();

        ScrollList<TextListItem> DescriptionSL;
        protected UIButton Engage;
        protected UIButton Abort;
        protected UIButton ClearTraits;

        int numOpponents;
        protected RacialTrait tipped;
        protected float tTimer = 0.35f;

        protected int FlagIndex;
        public int TotalPointsUsed { get; private set; } = 8;
        protected bool DrawingColorSelector;

        UniverseData.GameDifficulty SelectedDifficulty = UniverseData.GameDifficulty.Normal;
        public IEmpireData SelectedData { get; private set; }
        protected Color currentObjectColor = Color.White;
        protected Rectangle ColorSelector;

        protected string Singular = "Human";
        protected string Plural = "Humans";
        protected string HomeWorldName = "Earth";
        protected string HomeSystemName = "Sol";
        protected int PreferredEnvDescription;

        Rectangle ExtraRemnantRect; // Added by Gretman
        ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;


        public RaceDesignScreen(MainMenuScreen mainMenu) : base(mainMenu)
        {
            MainMenu = mainMenu;
            IsPopup = true;
            TransitionOnTime = 1.0f;
            TransitionOffTime = 0.25f;
            foreach (RacialTrait t in ResourceManager.RaceTraits.TraitList)
            {
                AllTraits.Add(new TraitEntry { trait = t });
            }
            GlobalStats.Statreset();
            numOpponents = GlobalStats.ActiveMod?.mi?.MaxOpponents ?? ResourceManager.MajorRaces.Count-1;
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);
            Rectangle r = ChooseRaceMenu.Menu;
            if (IsTransitioning)
            {
                r.X -= (int)(transitionOffset * 256f);
            }
            ChooseRaceMenu.Update(r);
            ChooseRaceMenu.subMenu = null;
            ChooseRaceMenu.Draw();
            RaceArchetypeSL.TransitionUpdate(r);
            RaceArchetypeSL.Draw(batch);
            r = dslrect;
            if (IsTransitioning)
            {
                r.X += (int)(transitionOffset * 256f);
            }
            DescriptionSL.TransitionUpdate(r);

            GameTime gameTime = StarDriveGame.Instance.GameTime;

            NameMenu.Draw();
            var c = new Color(255, 239, 208);
            NameSub.Draw(batch);
            batch.DrawString((GlobalStats.NotEnglishOrSpanish ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(31), ": "), RaceNamePos, Color.BurlyWood);
            Vector2 rpos = RaceNamePos;
            rpos.X = rpos.X + 205f;
            if (!RaceName.HandlingInput)
            {
                RaceName.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, (RaceName.Hover ? Color.White : c));
            }
            else
            {
                RaceName.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, Color.BurlyWood);
            }
            RaceName.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(RaceName.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            batch.DrawString((GlobalStats.NotEnglishOrSpanish ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(26), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!SingEntry.HandlingInput)
            {
                SingEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, (SingEntry.Hover ? Color.White : c));
            }
            else
            {
                SingEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, Color.BurlyWood);
            }
            SingEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(SingEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            batch.DrawString((GlobalStats.IsGermanOrPolish || GlobalStats.IsRussian || GlobalStats.IsFrench ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(27), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!PlurEntry.HandlingInput)
            {
                PlurEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, (PlurEntry.Hover ? Color.White : c));
            }
            else
            {
                PlurEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, Color.BurlyWood);
            }
            PlurEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(PlurEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            batch.DrawString((GlobalStats.IsGermanOrPolish || GlobalStats.IsRussian || GlobalStats.IsFrench ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(28), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!HomeSystemEntry.HandlingInput)
            {
                HomeSystemEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, (HomeSystemEntry.Hover ? Color.White : c));
            }
            else
            {
                HomeSystemEntry.Draw(Fonts.Arial14Bold, batch, rpos, gameTime, Color.BurlyWood);
            }
            HomeSystemEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(HomeSystemEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            batch.DrawString(Fonts.Arial14Bold, Localizer.Token(29), FlagPos, Color.BurlyWood);
            FlagRect = new Rectangle((int)FlagPos.X + 16, (int)FlagPos.Y + 15, 80, 80);
            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, currentObjectColor);
            FlagLeft = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);
            r = Description.Menu;
            if (IsTransitioning)
            {
                r.X += (int)(transitionOffset * 400f);
            }
            Description.Update(r);
            Description.subMenu = null;
            Description.Draw();
            rpos = new Vector2((r.X + 20), (Description.Menu.Y + 20));
            DescriptionSL.Draw(batch);

            Vector2 drawCurs = rpos;
            rpos = drawCurs;
            rpos.Y += (2 + Fonts.Arial14Bold.LineSpacing);
            batch.DrawString(Fonts.Arial14Bold, string.Concat(Localizer.Token(30), ": ", TotalPointsUsed), rpos, Color.White);
            rpos.Y += (Fonts.Arial14Bold.LineSpacing + 8);
            int numTraits = 0;
            foreach (TraitEntry t in AllTraits)
            {
                if (numTraits == 9)
                {
                    rpos = drawCurs;
                    rpos.X += 145f;
                    rpos.Y += (2 + Fonts.Arial14Bold.LineSpacing);
                    rpos.Y += (Fonts.Arial14Bold.LineSpacing + 2);
                }
                if (!t.Selected)
                {
                    continue;
                }
                batch.DrawString(Fonts.Arial14Bold, string.Concat(Localizer.Token(t.trait.TraitName), " ", t.trait.Cost), rpos, (t.trait.Cost > 0 ? new Color(59, 137, 59) : Color.Crimson));
                rpos.Y += (Fonts.Arial14Bold.LineSpacing + 2);
                numTraits++;
            }
            TitleBar.Draw(batch);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(18), TitlePos, c);
            Left.Draw();
            Traits.Draw(batch);
            traitsSL.Draw(batch);

            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(24), ": "), new Vector2(GalaxySizeRect.X, GalaxySizeRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, GalaxySize.ToString(), new Vector2(GalaxySizeRect.X + 190 - Fonts.Arial12.MeasureString(GalaxySize.ToString()).X, GalaxySizeRect.Y), Color.BurlyWood);
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(25), " : "), new Vector2(NumberStarsRect.X, NumberStarsRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, StarEnum.ToString(), new Vector2(NumberStarsRect.X + 190 - Fonts.Arial12.MeasureString(StarEnum.ToString()).X, NumberStarsRect.Y), Color.BurlyWood);
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2102), " : "), new Vector2(NumOpponentsRect.X, NumOpponentsRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, numOpponents.ToString(), new Vector2(NumOpponentsRect.X + 190 - Fonts.Arial12.MeasureString(numOpponents.ToString()).X, NumOpponentsRect.Y), Color.BurlyWood);
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2105), " : "), new Vector2(GameModeRect.X, GameModeRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2133), " : "), new Vector2(PacingRect.X, PacingRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, string.Concat(Pacing.ToString(), "%"), new Vector2(PacingRect.X + 190 - Fonts.Arial12.MeasureString(string.Concat(Pacing.ToString(), "%")).X, PacingRect.Y), Color.BurlyWood);
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2139), " : "), new Vector2(DifficultyRect.X, DifficultyRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, SelectedDifficulty.ToString(), new Vector2(DifficultyRect.X + 190 - Fonts.Arial12.MeasureString(SelectedDifficulty.ToString()).X, DifficultyRect.Y), Color.BurlyWood);

            //Added by Gretman
            string ExtraRemnantString = string.Concat(Localizer.Token(4101), " : ");
            batch.DrawString(Fonts.Arial12, ExtraRemnantString, new Vector2(ExtraRemnantRect.X, ExtraRemnantRect.Y), Color.White);
            batch.DrawString(Fonts.Arial12, ExtraRemnant.ToString(), new Vector2(ExtraRemnantRect.X + 190 - Fonts.Arial12.MeasureString(ExtraRemnant.ToString()).X, ExtraRemnantRect.Y), Color.BurlyWood);

            string txt = "";
            int tip = 0;
            if (Mode == GameMode.Sandbox)
            {
                txt = Localizer.Token(2103);
                tip = 112;
                batch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (Mode == GameMode.Elimination)
            {
                txt = Localizer.Token(6093);
                tip = 165;
                batch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (Mode == GameMode.Corners)    //Added by Gretman
            {
                txt = Localizer.Token(4102);
                tip = 229;
                batch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            if (ScaleRect.HitTest(Input.CursorPosition))
            {
                ToolTip.CreateTooltip(125);
            }
            if (PacingRect.HitTest(Input.CursorPosition))
            {
                ToolTip.CreateTooltip(126);
            }

            if (DrawingColorSelector)
            {
                DrawColorSelector();
            }
            base.Draw(batch);
            if (IsActive)
            {
                ToolTip.Draw(batch);
            }
            batch.End();
        }

        protected void DrawColorSelector()
        {
            ColorSelectMenu.Draw();
            int yPosition = ColorSelector.Y + 20;
            int xPositionStart = ColorSelector.X + 20;
            for (int i = 0; i <= 255; i++)
            {
                for (int j = 0; j <= 255; j++)
                {
                    Color thisColor = new Color(Convert.ToByte(i), Convert.ToByte(j), currentObjectColor.B);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Particles/spark"), new Rectangle(2 * j + xPositionStart, yPosition, 2, 2), thisColor);
                    if (thisColor.R == currentObjectColor.R && thisColor.G == currentObjectColor.G)
                    {
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Particles/spark"), new Rectangle(2 * j + xPositionStart, yPosition, 2, 2), Color.Red);
                    }
                }
                yPosition = yPosition + 2;
            }
            yPosition = ColorSelector.Y + 10;
            for (int i = 0; i <= 255; i++)
            {
                Color thisColor = new Color(currentObjectColor.R, currentObjectColor.G, Convert.ToByte(i));
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Particles/spark"), new Rectangle(ColorSelector.X + 10 + 575, yPosition, 20, 2), thisColor);
                if (thisColor.B == currentObjectColor.B)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Particles/spark"), new Rectangle(ColorSelector.X + 10 + 575, yPosition, 20, 2), Color.Red);
                }
                yPosition = yPosition + 2;
            }
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
            ScreenManager.AddScreen(new LoadSetupScreen(this));
        }

        void OnSaveSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new SaveSetupScreen(this, SelectedDifficulty, StarEnum, GalaxySize, Pacing,
                ExtraRemnant, numOpponents, Mode));
        }

        public override bool HandleInput(InputState input)
        {
            if (DescriptionSL.HandleInput(input))
                return true;

            if (!DrawingColorSelector)
            {
                if (RaceArchetypeSL.HandleInput(input))
                    return true;

                if (Traits.HandleInput(input))
                    ResetLists();

                if (!RaceName.ClickableArea.HitTest(input.CursorPosition))
                {
                    RaceName.Hover = false;
                }
                else
                {
                    RaceName.Hover = true;
                    if (input.LeftMouseClick && !SingEntry.HandlingInput && !PlurEntry.HandlingInput && !HomeSystemEntry.HandlingInput)
                    {
                        RaceName.HandlingInput = true;
                    }
                }
                if (!SingEntry.ClickableArea.HitTest(input.CursorPosition))
                {
                    SingEntry.Hover = false;
                }
                else
                {
                    SingEntry.Hover = true;
                    if (input.LeftMouseClick && !RaceName.HandlingInput && !PlurEntry.HandlingInput && !HomeSystemEntry.HandlingInput)
                    {
                        SingEntry.HandlingInput = true;
                    }
                }
                if (!PlurEntry.ClickableArea.HitTest(input.CursorPosition))
                {
                    PlurEntry.Hover = false;
                }
                else
                {
                    PlurEntry.Hover = true;
                    if (input.LeftMouseClick && !RaceName.HandlingInput && !SingEntry.HandlingInput && !HomeSystemEntry.HandlingInput)
                    {
                        PlurEntry.HandlingInput = true;
                    }
                }
                if (!HomeSystemEntry.ClickableArea.HitTest(input.CursorPosition))
                {
                    HomeSystemEntry.Hover = false;
                }
                else
                {
                    HomeSystemEntry.Hover = true;
                    if (input.LeftMouseClick && !RaceName.HandlingInput && !SingEntry.HandlingInput && !PlurEntry.HandlingInput)
                    {
                        HomeSystemEntry.HandlingInput = true;
                    }
                }
                if (RaceName.HandlingInput)
                {
                    RaceName.HandleTextInput(ref RaceName.Text, input);
                }
                if (SingEntry.HandlingInput)
                {
                    SingEntry.HandleTextInput(ref SingEntry.Text, input);
                }
                if (PlurEntry.HandlingInput)
                {
                    PlurEntry.HandleTextInput(ref PlurEntry.Text, input);
                }
                if (HomeSystemEntry.HandlingInput)
                {
                    HomeSystemEntry.HandleTextInput(ref HomeSystemEntry.Text, input);
                }

                if (traitsSL.HandleInput(input))
                    return true;

                if (GalaxySizeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    GalaxySize = (GalSize)((int)GalaxySize + (int)GalSize.Small);
                    if (GalaxySize > GalSize.TrulyEpic)   //Resurrecting TrulyEpic Map UniverseRadius -Gretman
                    {
                        GalaxySize = GalSize.Tiny;
                    }
                }
                if (GameModeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    Mode += 1;
                    if (Mode == GameMode.Corners) numOpponents = 3;
                    if (Mode > GameMode.Corners)  //Updated by Gretman
                    {
                        Mode = GameMode.Sandbox;
                    }
                }
                if (NumberStarsRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    RaceDesignScreen starEnum = this;
                    starEnum.StarEnum = (StarNum)((int)starEnum.StarEnum + (int)StarNum.Rare);
                    if (StarEnum > StarNum.SuperPacked)
                    {
                        StarEnum = StarNum.VeryRare;
                    }
                }
                if (NumOpponentsRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    int maxOpponents = Mode == GameMode.Corners ? 3 : GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
                    numOpponents += 1;
                    if (numOpponents > maxOpponents)                    
                        numOpponents = 1;
                }
                if (ScaleRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.BlipClick();
                        GameScale += 1;
                        if (GameScale > 6)
                            GameScale = 1;
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.BlipClick();
                        GameScale -= 1;
                        if (GameScale < 1)
                            GameScale = 6;
                    }
                }
                if (PacingRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.BlipClick();
                        Pacing += 25;
                        if (Pacing > 400)
                            Pacing = 100;
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.BlipClick();
                        Pacing -= 25;
                        if (Pacing < 100)
                            Pacing = 400;
                    }
                }
                if (DifficultyRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.BlipClick();
                        SelectedDifficulty = (UniverseData.GameDifficulty)((int)SelectedDifficulty + (int)UniverseData.GameDifficulty.Normal);
                        if (SelectedDifficulty > UniverseData.GameDifficulty.Brutal)
                            SelectedDifficulty = UniverseData.GameDifficulty.Easy;
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.BlipClick();
                        SelectedDifficulty = (UniverseData.GameDifficulty)((int)SelectedDifficulty - (int)UniverseData.GameDifficulty.Normal);
                        if (SelectedDifficulty < UniverseData.GameDifficulty.Easy)
                            SelectedDifficulty = UniverseData.GameDifficulty.Brutal;
                    }
                }

                if (ExtraRemnantRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.BlipClick();
                        ++ExtraRemnant;
                        if (ExtraRemnant > ExtraRemnantPresence.Everywhere)
                            ExtraRemnant = ExtraRemnantPresence.Rare;
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.BlipClick();
                        --ExtraRemnant;
                        if (ExtraRemnant < ExtraRemnantPresence.Rare)
                            ExtraRemnant = ExtraRemnantPresence.Everywhere;
                    }
                }

                if (FlagRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    DrawingColorSelector = !DrawingColorSelector;
                }
                if (FlagRight.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    if (ResourceManager.NumFlags - 1 <= FlagIndex)
                        FlagIndex = 0;
                    else
                        FlagIndex = FlagIndex + 1;
                    GameAudio.BlipClick();
                }
                if (FlagLeft.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    if (FlagIndex <= 0)
                        FlagIndex = ResourceManager.NumFlags - 1;
                    else
                        FlagIndex = FlagIndex - 1;
                    GameAudio.BlipClick();
                }
            }
            else if (!ColorSelector.HitTest(input.CursorPosition))
            {
                if (input.LeftMouseClick)
                {
                    DrawingColorSelector = false;
                }
            }
            else if (input.LeftMouseDown)
            {
                int yPosition = ColorSelector.Y + 10;
                int xPositionStart = ColorSelector.X + 10;
                for (int i = 0; i <= 255; i++)
                {
                    for (int j = 0; j <= 255; j++)
                    {
                        var thisColor = new Color((byte)i, (byte)j, RaceSummary.B);
                        var colorRect = new Rectangle(2 * j + xPositionStart - 4, yPosition - 4, 8, 8);
                        if (colorRect.HitTest(input.CursorPosition))
                        {
                            currentObjectColor = thisColor;
                        }
                    }
                    yPosition = yPosition + 2;
                }
                yPosition = ColorSelector.Y + 10;
                for (int i = 0; i <= 255; i++)
                {
                    var thisColor = new Color(currentObjectColor.R, currentObjectColor.G, Convert.ToByte(i));
                    var colorRect = new Rectangle(ColorSelector.X + 10 + 575, yPosition, 20, 2);
                    if (colorRect.HitTest(input.CursorPosition))
                    {
                        currentObjectColor = thisColor;
                    }
                    yPosition += 2;
                }
            }
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }


        RacialTrait GetRacialTraits()
        {
            RacialTrait t = RaceSummary.GetClone();
            t.Singular = SingEntry.Text;
            t.Plural = PlurEntry.Text;
            t.HomeSystemName = HomeSystemEntry.Text;
            t.R = currentObjectColor.R;
            t.G = currentObjectColor.G;
            t.B = currentObjectColor.B;
            t.FlagIndex = FlagIndex;
            t.HomeworldName = HomeWorldName;
            t.Name = RaceName.Text;
            t.ShipType  = SelectedData.ShipType;
            t.VideoPath = SelectedData.VideoPath;
            return t;
        }

        public override void LoadContent()
        {
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366 || ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 720)
            {
                LowRes = true;
            }
            Rectangle titleRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 203, (LowRes ? 10 : 44), 406, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(18)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            Rectangle nameRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - (int)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f) / 2, titleRect.Y + titleRect.Height + 5, (int)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f), 150);
            NameMenu = new Menu1(nameRect);
            Rectangle nsubRect = new Rectangle(nameRect.X + 20, nameRect.Y - 5, nameRect.Width - 40, nameRect.Height - 15);
            NameSub = new Submenu(nsubRect);
            ColorSelector = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 310, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 280, 620, 560);
            ColorSelectMenu = new Menu1(ColorSelector);
            RaceNamePos = new Vector2(nameRect.X + 40, nameRect.Y + 30);
            FlagPos = new Vector2(nameRect.X + nameRect.Width - 80 - 100, nameRect.Y + 30);
            Rectangle leftRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - (int)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f) / 2, nameRect.Y + nameRect.Height + 5, (int)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f), ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - (int)(0.28f * ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight));
            if (leftRect.Height > 580)
            {
                leftRect.Height = 580;
            }
            Left = new Menu1(leftRect);

            Rectangle ChooseRaceRect = new Rectangle(5, (LowRes ? nameRect.Y : leftRect.Y), leftRect.X - 10, (LowRes ? leftRect.Y + leftRect.Height - nameRect.Y : leftRect.Height));
            ChooseRaceMenu = new Menu1(ChooseRaceRect);
            Rectangle smaller = ChooseRaceRect;
            smaller.Y = smaller.Y - 20;
            smaller.Height = smaller.Height + 20;

            arch = new Submenu(smaller);
            RaceArchetypeSL = new ScrollList<RaceArchetypeListItem>(arch, 135);
            RaceArchetypeSL.OnClick = OnRaceArchetypeItemClicked;

            foreach (IEmpireData e in ResourceManager.MajorRaces)
            {
                RaceArchetypeSL.AddItem(new RaceArchetypeListItem(this, e));
                if (e.VideoPath.NotEmpty())
                    TextureDict.Add(e, ResourceManager.Texture("Races/" + e.VideoPath));
                if (e.Singular == "Human")
                    SelectedData = e;
            }

            RaceName.Text = SelectedData.Name;
            SingEntry.Text = SelectedData.Singular;
            PlurEntry.Text = SelectedData.Plural;
            HomeSystemEntry.Text = SelectedData.HomeSystemName;
            HomeWorldName = SelectedData.HomeWorldName;
            GalaxySizeRect = new Rectangle(nameRect.X + nameRect.Width + 40 - 22, nameRect.Y - 15, (int)Fonts.Arial12.MeasureString("Galaxy UniverseRadius                                   ").X, Fonts.Arial12.LineSpacing);
            NumberStarsRect = new Rectangle(GalaxySizeRect.X, GalaxySizeRect.Y + Fonts.Arial12.LineSpacing + 10, GalaxySizeRect.Width, GalaxySizeRect.Height);
            NumOpponentsRect = new Rectangle(NumberStarsRect.X, NumberStarsRect.Y + Fonts.Arial12.LineSpacing + 10, NumberStarsRect.Width, NumberStarsRect.Height);
            GameModeRect = new Rectangle(NumOpponentsRect.X, NumOpponentsRect.Y + Fonts.Arial12.LineSpacing + 10, NumberStarsRect.Width, NumOpponentsRect.Height);
            PacingRect = new Rectangle(GameModeRect.X, GameModeRect.Y + Fonts.Arial12.LineSpacing + 10, GameModeRect.Width, GameModeRect.Height);
            DifficultyRect = new Rectangle(PacingRect.X, PacingRect.Y + Fonts.Arial12.LineSpacing + 10, PacingRect.Width, PacingRect.Height);
            
            //Gretman - Remnant Presence button, relative to Difficulty button
            ExtraRemnantRect = new Rectangle(DifficultyRect.X, DifficultyRect.Y + Fonts.Arial12.LineSpacing + 10, DifficultyRect.Width, DifficultyRect.Height);

            Rectangle dRect = new Rectangle(leftRect.X + leftRect.Width + 5, leftRect.Y, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - leftRect.X - leftRect.Width - 10, leftRect.Height);
            Description = new Menu1(ScreenManager, dRect, true);
            dslrect = new Rectangle(leftRect.X + leftRect.Width + 5, leftRect.Y, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - leftRect.X - leftRect.Width - 10, leftRect.Height - 160);
            Submenu dsub = new Submenu(dslrect);
            DescriptionSL = new ScrollList<TextListItem>(dsub, Fonts.Arial12.LineSpacing);
            Rectangle psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
            Traits = new Submenu(psubRect);
            Traits.AddTab(Localizer.Token(19));
            Traits.AddTab(Localizer.Token(20));
            Traits.AddTab(Localizer.Token(21));
            int size = 55;
            if (GlobalStats.NotGerman && ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                size = 65;
            }
            if (GlobalStats.IsRussian || GlobalStats.IsPolish)
            {
                size = 70;
            }

            traitsSL = new ScrollList<TraitsListItem>(Traits, size);
            traitsSL.OnClick = OnTraitsListItemClicked;
            foreach (TraitEntry t in AllTraits)
            {
                if (t.trait.Category == "Physical")
                    traitsSL.AddItem(new TraitsListItem(this, t));
            }

            Engage      = ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, titleId:22, click: OnEngageClicked);
            Abort       = ButtonMedium(10, ScreenHeight - 40, titleId:23, click: OnAbortClicked);
            ClearTraits = ButtonMedium(ScreenWidth - 150,
                            Description.Menu.Y + Description.Menu.Height - 40, "Clear Traits", OnClearClicked);

            DoRaceDescription();
            SetRacialTraits(SelectedData.Traits);

            ButtonMedium(smaller.X + (smaller.Width / 2) - 142, smaller.Y - 20, "Load Race", OnLoadRaceClicked);
            ButtonMedium(smaller.X + (smaller.Width / 2) + 10, smaller.Y - 20, "Save Race", OnSaveRaceClicked);

            var pos = new Vector2(ScreenWidth / 2 - 84, leftRect.Y + leftRect.Height + 10);

            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, titleId: 4006, click: OnRuleOptionsClicked);

            base.LoadContent();
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
            Singular                   = SingEntry.Text;
            Plural                     = PlurEntry.Text;
            HomeSystemName             = HomeSystemEntry.Text;
            RaceSummary.R              = currentObjectColor.R;
            RaceSummary.G              = currentObjectColor.G;
            RaceSummary.B              = currentObjectColor.B;
            RaceSummary.Singular       = Singular;
            RaceSummary.Plural         = Plural;
            RaceSummary.HomeSystemName = HomeSystemName;
            RaceSummary.HomeworldName  = HomeWorldName;
            RaceSummary.Name           = RaceName.Text;
            RaceSummary.FlagIndex      = FlagIndex;
            RaceSummary.ShipType       = SelectedData.ShipType;
            RaceSummary.VideoPath      = SelectedData.VideoPath;
            RaceSummary.Adj1 = SelectedData.Adj1;
            RaceSummary.Adj2 = SelectedData.Adj2;

            var player = new Empire
            {
                EmpireColor = currentObjectColor,
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
                                               numOpponents, Mode, pace, GameScale, SelectedDifficulty, MainMenu);
            ScreenManager.GoToScreen(ng, clear3DObjects:true);
        }

        public void ResetLists() // IListScreen.ResetLists()
        {
            traitsSL.Reset();
            if (Traits.Tabs[0].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Physical")
                        traitsSL.AddItem(new TraitsListItem(this, t));
            }
            else if (Traits.Tabs[1].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Industry")
                        traitsSL.AddItem(new TraitsListItem(this, t));
            }
            else if (Traits.Tabs[2].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Special")
                        traitsSL.AddItem(new TraitsListItem(this, t));
            }
        }

        public void SetCustomSetup(UniverseData.GameDifficulty gameDifficulty, StarNum StarEnum, GalSize Galaxysize, int Pacing, ExtraRemnantPresence ExtraRemnant, int numOpponents, GameMode mode)
        {
            SelectedDifficulty        = gameDifficulty;
            this.StarEnum     = StarEnum;
            this.GalaxySize   = Galaxysize;
            this.Pacing       = Pacing;
            this.ExtraRemnant = ExtraRemnant;
            this.numOpponents = numOpponents;
            this.Mode         = mode;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!DrawingColorSelector)
            {
                bool overSomething = false;
                foreach (TraitEntry t in AllTraits)
                {
                    if (!t.rect.HitTest(Input.CursorPosition))
                        continue;
                    overSomething = true;
                    RaceDesignScreen raceDesignScreen = this;
                    raceDesignScreen.tTimer = raceDesignScreen.tTimer - elapsedTime;
                    if (tTimer > 0f)
                        continue;
                    tipped = t.trait;
                }
                if (!overSomething)
                {
                    tTimer = 0.35f;
                    tipped = null;
                }
            }

            CreateRaceSummary();
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        
        public enum GalSize
        {
            Tiny, Small, Medium, Large,
            Huge, Epic, TrulyEpic       // Reenabled by Gretman, to make use of the new negative map sizes
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
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public class RaceDesignScreen : GameScreen, IListScreen
    {
        protected RacialTrait RaceSummary = new RacialTrait();
        private int GameScale = 1;

        protected RacialTraits rt;
        protected MainMenuScreen mmscreen;

        private GameMode mode = 0; //was unassigned

        protected Rectangle FlagLeft;
        protected Rectangle FlagRight;

        protected Array<TraitEntry> AllTraits = new Array<TraitEntry>();

        protected Rectangle GalaxySizeRect;

        private StarNum StarEnum = StarNum.Normal;

        public GalSize Galaxysize = GalSize.Medium;

        private Rectangle NumberStarsRect;
        private Rectangle NumOpponentsRect;

        protected Menu2 TitleBar;
        protected Vector2 TitlePos;
        protected Menu1 Left;
        protected Menu1 Name;
        protected Menu1 Description;

        protected bool LowRes;

        protected Submenu Traits;
        protected Submenu NameSub;

        protected ScrollList traitsSL;
        protected Selector selector;
        protected Menu1 ColorSelectMenu;

        protected UITextEntry RaceName = new UITextEntry();
        protected UITextEntry SingEntry = new UITextEntry();
        protected UITextEntry PlurEntry = new UITextEntry();
        protected UITextEntry HomeSystemEntry = new UITextEntry();

        protected Vector2 RaceNamePos;
        protected Vector2 FlagPos;

        protected Rectangle FlagRect;
        private Menu1 ChooseRaceMenu;
        private ScrollList RaceArchetypeSL;
        private Submenu arch;
        private Rectangle PacingRect;

        private int Pacing = 100;

        private Rectangle ScaleRect = new Rectangle();
        private Rectangle dslrect;
        private Rectangle GameModeRect;
        private Rectangle DifficultyRect;

        private Map<EmpireData, SubTexture> TextureDict = new Map<EmpireData, SubTexture>();

        private ScrollList DescriptionSL;
        private UIButton RulesOptions;
        protected UIButton Engage;
        protected UIButton Abort;
        protected UIButton ClearTraits;

        private int numOpponents = 7;
        protected RacialTrait tipped;
        protected float tTimer = 0.35f;
        public string rd = "";
        private Keys[] keysToCheck = { Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z, Keys.Back, Keys.Space, Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9, Keys.OemMinus, Keys.OemQuotes };

        private KeyboardState currentKeyboardState;
        private KeyboardState lastKeyboardState;

        protected int FlagIndex;
        protected int TotalPointsUsed = 8;
        protected bool DrawingColorSelector;

        public UniverseData.GameDifficulty difficulty = UniverseData.GameDifficulty.Normal;
        private EmpireData SelectedData;
        protected Color currentObjectColor = Color.White;
        protected Rectangle ColorSelector;

        protected string Singular = "Human";
        protected string Plural = "Humans";
        protected string HomeWorldName = "Earth";
        protected string HomeSystemName = "Sol";

        private Rectangle ExtraRemnantRect; //Added by Gretman
        public ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;

        private UIButton SaveRace;  // Added by EVWeb
        private UIButton LoadRace;
        private UIButton SaveSetup;
        private UIButton LoadSetup;

        public RaceDesignScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            GlobalStats.Statreset();
        }

        public RaceDesignScreen(MainMenuScreen mmscreen) : base(mmscreen)
        {
            this.mmscreen = mmscreen;
            IsPopup = true;
            rt = ResourceManager.RaceTraits;
            TransitionOnTime = TimeSpan.FromSeconds(1);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            foreach (RacialTrait t in rt.TraitList)
            {
                var te = new TraitEntry
                {
                    trait = t
                };
                AllTraits.Add(te);
            }
            GlobalStats.Statreset();
            numOpponents = GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
        }

        private void AddKeyToText(ref string text, Keys key)
        {
            string newChar = "";
            if (text.Length >= 60 && key != Keys.Back)
            {
                return;
            }
            Keys key1 = key;
            if (key1 > Keys.Space)
            {
                switch (key1)
                {
                    case Keys.A:
                    {
                        newChar = string.Concat(newChar, "a");
                        break;
                    }
                    case Keys.B:
                    {
                        newChar = string.Concat(newChar, "b");
                        break;
                    }
                    case Keys.C:
                    {
                        newChar = string.Concat(newChar, "c");
                        break;
                    }
                    case Keys.D:
                    {
                        newChar = string.Concat(newChar, "d");
                        break;
                    }
                    case Keys.E:
                    {
                        newChar = string.Concat(newChar, "e");
                        break;
                    }
                    case Keys.F:
                    {
                        newChar = string.Concat(newChar, "f");
                        break;
                    }
                    case Keys.G:
                    {
                        newChar = string.Concat(newChar, "g");
                        break;
                    }
                    case Keys.H:
                    {
                        newChar = string.Concat(newChar, "h");
                        break;
                    }
                    case Keys.I:
                    {
                        newChar = string.Concat(newChar, "i");
                        break;
                    }
                    case Keys.J:
                    {
                        newChar = string.Concat(newChar, "j");
                        break;
                    }
                    case Keys.K:
                    {
                        newChar = string.Concat(newChar, "k");
                        break;
                    }
                    case Keys.L:
                    {
                        newChar = string.Concat(newChar, "l");
                        break;
                    }
                    case Keys.M:
                    {
                        newChar = string.Concat(newChar, "m");
                        break;
                    }
                    case Keys.N:
                    {
                        newChar = string.Concat(newChar, "n");
                        break;
                    }
                    case Keys.O:
                    {
                        newChar = string.Concat(newChar, "o");
                        break;
                    }
                    case Keys.P:
                    {
                        newChar = string.Concat(newChar, "p");
                        break;
                    }
                    case Keys.Q:
                    {
                        newChar = string.Concat(newChar, "q");
                        break;
                    }
                    case Keys.R:
                    {
                        newChar = string.Concat(newChar, "r");
                        break;
                    }
                    case Keys.S:
                    {
                        newChar = string.Concat(newChar, "s");
                        break;
                    }
                    case Keys.T:
                    {
                        newChar = string.Concat(newChar, "t");
                        break;
                    }
                    case Keys.U:
                    {
                        newChar = string.Concat(newChar, "u");
                        break;
                    }
                    case Keys.V:
                    {
                        newChar = string.Concat(newChar, "v");
                        break;
                    }
                    case Keys.W:
                    {
                        newChar = string.Concat(newChar, "w");
                        break;
                    }
                    case Keys.X:
                    {
                        newChar = string.Concat(newChar, "x");
                        break;
                    }
                    case Keys.Y:
                    {
                        newChar = string.Concat(newChar, "y");
                        break;
                    }
                    case Keys.Z:
                    {
                        newChar = string.Concat(newChar, "z");
                        break;
                    }
                    case Keys.LeftWindows:
                    case Keys.RightWindows:
                    case Keys.Apps:
                    case Keys.B | Keys.Back | Keys.CapsLock | Keys.D | Keys.F | Keys.H | Keys.ImeConvert | Keys.J | Keys.L | Keys.N | Keys.P | Keys.R | Keys.RightWindows | Keys.T | Keys.V | Keys.X | Keys.Z:
                    case Keys.Sleep:
                    {
                        break;
                    }
                    case Keys.NumPad0:
                    {
                        newChar = string.Concat(newChar, "0");
                        break;
                    }
                    case Keys.NumPad1:
                    {
                        newChar = string.Concat(newChar, "1");
                        break;
                    }
                    case Keys.NumPad2:
                    {
                        newChar = string.Concat(newChar, "2");
                        break;
                    }
                    case Keys.NumPad3:
                    {
                        newChar = string.Concat(newChar, "3");
                        break;
                    }
                    case Keys.NumPad4:
                    {
                        newChar = string.Concat(newChar, "4");
                        break;
                    }
                    case Keys.NumPad5:
                    {
                        newChar = string.Concat(newChar, "5");
                        break;
                    }
                    case Keys.NumPad6:
                    {
                        newChar = string.Concat(newChar, "6");
                        break;
                    }
                    case Keys.NumPad7:
                    {
                        newChar = string.Concat(newChar, "7");
                        break;
                    }
                    case Keys.NumPad8:
                    {
                        newChar = string.Concat(newChar, "8");
                        break;
                    }
                    case Keys.NumPad9:
                    {
                        newChar = string.Concat(newChar, "9");
                        break;
                    }
                    default:
                    {
                        if (key1 == Keys.OemMinus)
                        {
                            newChar = string.Concat(newChar, "-");
                            break;
                        }

                        if (key1 == Keys.OemQuotes)
                        {
                            newChar = string.Concat(newChar, "'");
                        }

                        break;
                    }
                }
            }
            else
            {
                if (key1 == Keys.Back)
                {
                    if (text.Length != 0)
                    {
                        text = text.Remove(text.Length - 1);
                    }
                    return;
                }
                if (key1 == Keys.Space)
                {
                    newChar = string.Concat(newChar, " ");
                }
            }
            if (currentKeyboardState.IsKeyDown(Keys.RightShift) || currentKeyboardState.IsKeyDown(Keys.LeftShift) || lastKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                newChar = newChar.ToUpper();
            }
            text = string.Concat(text, newChar);
        }

        private bool CheckKey(Keys theKey)
        {
            return lastKeyboardState.IsKeyDown(theKey) && currentKeyboardState.IsKeyUp(theKey);
        }

        protected void DoRaceDescription()
        {
            UpdateSummary();
            rd = "";
            RaceDesignScreen raceDesignScreen = this;
            string str = raceDesignScreen.rd;
            string[] text = { str, RaceName.Text, Localizer.Token(1300), Plural, ". " };
            raceDesignScreen.rd = string.Concat(text);
            if (RaceSummary.Cybernetic <= 0)
            {
                RaceDesignScreen raceDesignScreen1 = this;
                raceDesignScreen1.rd = string.Concat(raceDesignScreen1.rd, Plural, Localizer.Token(1302));
            }
            else
            {
                RaceDesignScreen raceDesignScreen2 = this;
                raceDesignScreen2.rd = string.Concat(raceDesignScreen2.rd, Plural, Localizer.Token(1301));
            }
            if (RaceSummary.Aquatic <= 0)
            {
                rd = rd + Localizer.Token(1304);
            }
            else
            {
                rd = rd + Localizer.Token(1303);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.PopGrowthMin > 0f)
                {
                    rd += Localizer.Token(1305);
                }
                else if (RaceSummary.PopGrowthMin ==0 && RaceSummary.PopGrowthMax==0)
                {
                    rd += Localizer.Token(1307);
                }
                else
                {
                    rd += Localizer.Token(1306);
                }
            }
            else if (RaceSummary.PopGrowthMin > 0f)
            {
                rd += Localizer.Token(1308);
            }
            else if (RaceSummary.PopGrowthMin == 0 && RaceSummary.PopGrowthMax == 0)
            {
                rd += Localizer.Token(1310);
            }
            else
            {
                rd += Localizer.Token(1309);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.ConsumptionModifier > 0f)
                {
                    rd += Localizer.Token(1311);
                }
                else if (RaceSummary.ConsumptionModifier >= 0f)
                {
                    rd += Localizer.Token(1313);
                }
                else
                {
                    rd += Localizer.Token(1312);
                }
            }
            else if (RaceSummary.ConsumptionModifier > 0f)
            {
                rd += Localizer.Token(1314);
            }
            else if (RaceSummary.ConsumptionModifier >= 0f)
            {
                rd += Localizer.Token(1316);
            }
            else
            {
                rd += Localizer.Token(1315);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.GroundCombatModifier > 0f)
                {
                    if (RaceSummary.DiplomacyMod > 0f)
                    {
                        RaceDesignScreen raceDesignScreen17 = this;
                        raceDesignScreen17.rd = string.Concat(raceDesignScreen17.rd, Localizer.Token(1317), Plural, Localizer.Token(1318));
                    }
                    else if (RaceSummary.DiplomacyMod >= 0f)
                    {
                        RaceDesignScreen raceDesignScreen18 = this;
                        raceDesignScreen18.rd = string.Concat(raceDesignScreen18.rd, Localizer.Token(1317), Plural, Localizer.Token(1320));
                    }
                    else
                    {
                        RaceDesignScreen raceDesignScreen19 = this;
                        raceDesignScreen19.rd = string.Concat(raceDesignScreen19.rd, Localizer.Token(1317), Plural, Localizer.Token(1319));
                    }
                }
                else if (RaceSummary.GroundCombatModifier >= 0f)
                {
                    if (RaceSummary.GroundCombatModifier == 0f)
                    {
                        if (RaceSummary.DiplomacyMod > 0f)
                        {
                            RaceDesignScreen raceDesignScreen20 = this;
                            raceDesignScreen20.rd = string.Concat(raceDesignScreen20.rd, Localizer.Token(1317), Plural, Localizer.Token(1324));
                        }
                        else if (RaceSummary.DiplomacyMod >= 0f)
                        {
                            RaceDesignScreen raceDesignScreen21 = this;
                            raceDesignScreen21.rd = string.Concat(raceDesignScreen21.rd, Localizer.Token(1317), Plural, Localizer.Token(1326));
                        }
                        else
                        {
                            RaceDesignScreen raceDesignScreen22 = this;
                            raceDesignScreen22.rd = string.Concat(raceDesignScreen22.rd, Localizer.Token(1317), Plural, Localizer.Token(1325));
                        }
                    }
                }
                else if (RaceSummary.DiplomacyMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen23 = this;
                    raceDesignScreen23.rd = string.Concat(raceDesignScreen23.rd, Localizer.Token(1317), Plural, Localizer.Token(1321));
                }
                else if (RaceSummary.DiplomacyMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen24 = this;
                    raceDesignScreen24.rd = string.Concat(raceDesignScreen24.rd, Localizer.Token(1317), Plural, Localizer.Token(1323));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen25 = this;
                    raceDesignScreen25.rd = string.Concat(raceDesignScreen25.rd, Localizer.Token(1317), Plural, Localizer.Token(1322));
                }
            }
            else if (RaceSummary.GroundCombatModifier > 0f)
            {
                if (RaceSummary.DiplomacyMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen26 = this;
                    raceDesignScreen26.rd = string.Concat(raceDesignScreen26.rd, Localizer.Token(1327), Plural, Localizer.Token(1328));
                }
                else if (RaceSummary.DiplomacyMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen27 = this;
                    raceDesignScreen27.rd = string.Concat(raceDesignScreen27.rd, Localizer.Token(1317), Plural, Localizer.Token(1330));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen28 = this;
                    raceDesignScreen28.rd = string.Concat(raceDesignScreen28.rd, Localizer.Token(1327), Plural, Localizer.Token(1329));
                }
            }
            else if (RaceSummary.GroundCombatModifier >= 0f)
            {
                if (RaceSummary.GroundCombatModifier == 0f)
                {
                    if (RaceSummary.DiplomacyMod > 0f)
                    {
                        RaceDesignScreen raceDesignScreen29 = this;
                        raceDesignScreen29.rd = string.Concat(raceDesignScreen29.rd, Localizer.Token(1317), Plural, Localizer.Token(1334));
                    }
                    else if (RaceSummary.DiplomacyMod >= 0f)
                    {
                        RaceDesignScreen raceDesignScreen30 = this;
                        raceDesignScreen30.rd = string.Concat(raceDesignScreen30.rd, Localizer.Token(1336), Plural, Localizer.Token(1337));
                    }
                    else
                    {
                        RaceDesignScreen raceDesignScreen31 = this;
                        raceDesignScreen31.rd = string.Concat(raceDesignScreen31.rd, Localizer.Token(1327), Plural, Localizer.Token(1335));
                    }
                }
            }
            else if (RaceSummary.DiplomacyMod > 0f)
            {
                RaceDesignScreen raceDesignScreen32 = this;
                raceDesignScreen32.rd = string.Concat(raceDesignScreen32.rd, Localizer.Token(1317), Plural, Localizer.Token(1331));
            }
            else if (RaceSummary.DiplomacyMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen33 = this;
                raceDesignScreen33.rd = string.Concat(raceDesignScreen33.rd, Localizer.Token(1317), Plural, Localizer.Token(1333));
            }
            else
            {
                RaceDesignScreen raceDesignScreen34 = this;
                raceDesignScreen34.rd = string.Concat(raceDesignScreen34.rd, Localizer.Token(1317), Plural, Localizer.Token(1332));
            }
            if (RaceSummary.GroundCombatModifier < 0f || RaceSummary.DiplomacyMod <= 0f)
            {
                if (RaceSummary.ResearchMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen35 = this;
                    raceDesignScreen35.rd = string.Concat(raceDesignScreen35.rd, Localizer.Token(1338), Plural, Localizer.Token(1339));
                }
                else if (RaceSummary.ResearchMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen36 = this;
                    raceDesignScreen36.rd = string.Concat(raceDesignScreen36.rd, Plural, Localizer.Token(1342));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen37 = this;
                    raceDesignScreen37.rd = string.Concat(raceDesignScreen37.rd, Localizer.Token(1340), Plural, Localizer.Token(1341));
                }
            }
            else if (RaceSummary.GroundCombatModifier <= 0f && RaceSummary.DiplomacyMod <= 0f)
            {
                if (RaceSummary.ResearchMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen38 = this;
                    raceDesignScreen38.rd = string.Concat(raceDesignScreen38.rd, Plural, Localizer.Token(1344));
                }
                else if (RaceSummary.ResearchMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen39 = this;
                    raceDesignScreen39.rd = string.Concat(raceDesignScreen39.rd, Plural, Localizer.Token(1342));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen40 = this;
                    raceDesignScreen40.rd = string.Concat(raceDesignScreen40.rd, Plural, Localizer.Token(1341));
                }
            }
            else if (RaceSummary.ResearchMod > 0f)
            {
                RaceDesignScreen raceDesignScreen41 = this;
                raceDesignScreen41.rd = string.Concat(raceDesignScreen41.rd, Localizer.Token(1343), Plural, Localizer.Token(1344));
            }
            else if (RaceSummary.ResearchMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen42 = this;
                raceDesignScreen42.rd = string.Concat(raceDesignScreen42.rd, Plural, Localizer.Token(1342));
            }
            else
            {
                RaceDesignScreen raceDesignScreen43 = this;
                raceDesignScreen43.rd = string.Concat(raceDesignScreen43.rd, Plural, Localizer.Token(1341));
            }
            RaceDesignScreen raceDesignScreen44 = this;
            raceDesignScreen44.rd = string.Concat(raceDesignScreen44.rd, "\n \n");
            if (RaceSummary.TaxMod > 0f)
            {
                RaceDesignScreen raceDesignScreen45 = this;
                raceDesignScreen45.rd = string.Concat(raceDesignScreen45.rd, Singular, Localizer.Token(1345));
                if (RaceSummary.MaintMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen46 = this;
                    raceDesignScreen46.rd = string.Concat(raceDesignScreen46.rd, Localizer.Token(1346), Plural, ". ");
                }
                else if (RaceSummary.MaintMod <= 0f)
                {
                    rd += Localizer.Token(1348);
                }
                else
                {
                    rd += Localizer.Token(1347);
                }
            }
            else if (RaceSummary.TaxMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen49 = this;
                raceDesignScreen49.rd = string.Concat(raceDesignScreen49.rd, Singular, Localizer.Token(1354));
                if (RaceSummary.MaintMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen50 = this;
                    raceDesignScreen50.rd = string.Concat(raceDesignScreen50.rd, Localizer.Token(1355), Singular, Localizer.Token(1356));
                }
                else if (RaceSummary.MaintMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen51 = this;
                    raceDesignScreen51.rd = string.Concat(raceDesignScreen51.rd, Plural, Localizer.Token(1357));
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen52 = this;
                raceDesignScreen52.rd = string.Concat(raceDesignScreen52.rd, Localizer.Token(1349), Singular, Localizer.Token(1350));
                if (RaceSummary.MaintMod < 0f)
                {
                    rd += Localizer.Token(1351);
                }
                else if (RaceSummary.MaintMod <= 0f)
                {
                    RaceDesignScreen raceDesignScreen54 = this;
                    raceDesignScreen54.rd = string.Concat(raceDesignScreen54.rd, ", ", Plural, Localizer.Token(1353));
                }
                else
                {
                    rd += Localizer.Token(1352);
                }
            }
            if (RaceSummary.ProductionMod > 0f)
            {
                RaceDesignScreen raceDesignScreen56 = this;
                raceDesignScreen56.rd = string.Concat(raceDesignScreen56.rd, Singular, Localizer.Token(1358));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    rd += Localizer.Token(1359);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    rd += Localizer.Token(1361);
                }
                else
                {
                    rd += Localizer.Token(1360);
                }
            }
            else if (RaceSummary.ProductionMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen60 = this;
                raceDesignScreen60.rd = string.Concat(raceDesignScreen60.rd, Plural, Localizer.Token(1366));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    rd += Localizer.Token(1367);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    rd += Localizer.Token(1369);
                }
                else
                {
                    rd += Localizer.Token(1368);
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen64 = this;
                raceDesignScreen64.rd = string.Concat(raceDesignScreen64.rd, Plural, Localizer.Token(1362));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    rd += Localizer.Token(1363);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    rd += Localizer.Token(1365);
                }
                else
                {
                    rd += Localizer.Token(1364);
                }
            }
            if (RaceSummary.SpyMultiplier > 0f)
            {
                RaceDesignScreen raceDesignScreen68 = this;
                raceDesignScreen68.rd = string.Concat(raceDesignScreen68.rd, Plural, Localizer.Token(1381));
            }
            else if (RaceSummary.SpyMultiplier < 0f)
            {
                RaceDesignScreen raceDesignScreen69 = this;
                raceDesignScreen69.rd = string.Concat(raceDesignScreen69.rd, Plural, Localizer.Token(1382));
            }
            if (RaceSummary.Spiritual > 0f)
            {
                rd += Localizer.Token(1383);
            }
            RaceDesignScreen raceDesignScreen71 = this;
            raceDesignScreen71.rd = string.Concat(raceDesignScreen71.rd, "\n \n");
            if (RaceSummary.HomeworldSizeMod > 0f)
            {
                RaceDesignScreen raceDesignScreen72 = this;
                string str1 = raceDesignScreen72.rd;
                string[] strArrays = { str1, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1372) };
                raceDesignScreen72.rd = string.Concat(strArrays);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    rd += Localizer.Token(1373);
                }
            }
            else if (RaceSummary.HomeworldSizeMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen74 = this;
                string str2 = raceDesignScreen74.rd;
                string[] strArrays1 = { str2, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1375) };
                raceDesignScreen74.rd = string.Concat(strArrays1);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    rd += Localizer.Token(1373);
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen76 = this;
                string str3 = raceDesignScreen76.rd;
                string[] strArrays2 = { str3, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1374) };
                raceDesignScreen76.rd = string.Concat(strArrays2);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    rd += Localizer.Token(1373);
                }
            }
            if (RaceSummary.BonusExplored > 0)
            {
                rd += Localizer.Token(1376);
            }
            if (RaceSummary.Militaristic > 0)
            {
                rd += Localizer.Token(1377);
                if (RaceSummary.ShipCostMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen80 = this;
                    raceDesignScreen80.rd = string.Concat(raceDesignScreen80.rd, Localizer.Token(1378), Singular, Localizer.Token(1379));
                }
            }
            else if (RaceSummary.ShipCostMod < 0f)
            {
                RaceDesignScreen raceDesignScreen81 = this;
                raceDesignScreen81.rd = string.Concat(raceDesignScreen81.rd, Plural, Localizer.Token(1380));
            }
            DescriptionSL.Reset();
            HelperFunctions.parseTextToSL(rd, Description.Menu.Width - 50, Fonts.Arial12, ref DescriptionSL);
        }

        static float DotSpaceWidth;

        // Creates padded text: "Vulgar Animals . . . . . . . . . . . ."
        static string PaddedWithDots(int localizedNameId, float totalWidth)
        {
            if (DotSpaceWidth <= 0f)
                DotSpaceWidth = Fonts.Arial14Bold.MeasureString(" .").X;

            string name = Localizer.Token(localizedNameId);
            float nameWidth = Fonts.Arial14Bold.MeasureString(name).X;
            int numDots = (int)Math.Ceiling((totalWidth - nameWidth) / DotSpaceWidth);

            var sb = new StringBuilder(name, name.Length + numDots*2);
            for (int i = 0; i < numDots; ++i)
                sb.Append(" .");

            return sb.ToString();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);
            Rectangle r = ChooseRaceMenu.Menu;
            if (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.TransitionOff)
            {
                r.X = r.X - (int)(transitionOffset * 256f);
            }
            ChooseRaceMenu.Update(r);
            ChooseRaceMenu.subMenu = null;
            ChooseRaceMenu.Draw();
            RaceArchetypeSL.TransitionUpdate(r);
            RaceArchetypeSL.Draw(ScreenManager.SpriteBatch);
            r = dslrect;
            if (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.TransitionOff)
            {
                r.X = r.X + (int)(transitionOffset * 256f);
            }
            DescriptionSL.TransitionUpdate(r);
            if (!IsExiting)
            {
                var raceCursor = new Vector2(r.X + 10, ChooseRaceMenu.Menu.Y + 10);
                
                foreach (ScrollList.Entry e in RaceArchetypeSL.VisibleEntries)
                {
                    var data = e.item as EmpireData;
                    raceCursor.Y = e.Y;
                    if (LowRes)
                    {
                        var portrait = new Rectangle(e.CenterX - 128, (int)raceCursor.Y, 256, 128);
                        ScreenManager.SpriteBatch.Draw(TextureDict[data], portrait, Color.White);
                        if (SelectedData == data)
                        {
                            ScreenManager.SpriteBatch.DrawRectangle(portrait, Color.BurlyWood);
                        }
                    }
                    else
                    {
                        var portrait = new Rectangle(e.CenterX - 176, (int)raceCursor.Y, 352, 128);
                        ScreenManager.SpriteBatch.Draw(TextureDict[data], portrait, Color.White);
                        if (SelectedData == data)
                        {
                            ScreenManager.SpriteBatch.DrawRectangle(portrait, Color.BurlyWood);
                        }
                    }
                }
            }

            GameTime gameTime = StarDriveGame.Instance.GameTime;

            Name.Draw();
            Color c = new Color(255, 239, 208);
            NameSub.Draw(batch);
            batch.DrawString((GlobalStats.NotEnglishOrSpanish ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(31), ": "), RaceNamePos, Color.BurlyWood);
            Vector2 rpos = RaceNamePos;
            rpos.X = rpos.X + 205f;
            if (!RaceName.HandlingInput)
            {
                RaceName.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, (RaceName.Hover ? Color.White : c));
            }
            else
            {
                RaceName.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, Color.BurlyWood);
            }
            RaceName.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(RaceName.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            batch.DrawString((GlobalStats.NotEnglishOrSpanish ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(26), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!SingEntry.HandlingInput)
            {
                SingEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, (SingEntry.Hover ? Color.White : c));
            }
            else
            {
                SingEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, Color.BurlyWood);
            }
            SingEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(SingEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            ScreenManager.SpriteBatch.DrawString((GlobalStats.IsGermanOrPolish || GlobalStats.IsRussian || GlobalStats.IsFrench ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(27), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!PlurEntry.HandlingInput)
            {
                PlurEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, (PlurEntry.Hover ? Color.White : c));
            }
            else
            {
                PlurEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, Color.BurlyWood);
            }
            PlurEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(PlurEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            rpos.X = RaceNamePos.X;
            rpos.Y = rpos.Y + (Fonts.Arial14Bold.LineSpacing + 2);
            ScreenManager.SpriteBatch.DrawString((GlobalStats.IsGermanOrPolish || GlobalStats.IsRussian || GlobalStats.IsFrench ? Fonts.Arial12 : Fonts.Arial14Bold), string.Concat(Localizer.Token(28), ": "), rpos, Color.BurlyWood);
            rpos.X = rpos.X + 205f;
            if (!HomeSystemEntry.HandlingInput)
            {
                HomeSystemEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, (HomeSystemEntry.Hover ? Color.White : c));
            }
            else
            {
                HomeSystemEntry.Draw(Fonts.Arial14Bold, ScreenManager.SpriteBatch, rpos, gameTime, Color.BurlyWood);
            }
            HomeSystemEntry.ClickableArea = new Rectangle((int)rpos.X, (int)rpos.Y, (int)Fonts.Arial14Bold.MeasureString(HomeSystemEntry.Text).X + 20, Fonts.Arial14Bold.LineSpacing);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(29), FlagPos, Color.BurlyWood);
            FlagRect = new Rectangle((int)FlagPos.X + 16, (int)FlagPos.Y + 15, 80, 80);
            batch.Draw(ResourceManager.Flag(FlagIndex), FlagRect, currentObjectColor);
            FlagLeft = new Rectangle(FlagRect.X - 20, FlagRect.Y + 40 - 10, 20, 20);
            FlagRight = new Rectangle(FlagRect.X + FlagRect.Width, FlagRect.Y + 40 - 10, 20, 20);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/leftArrow"), FlagLeft, Color.BurlyWood);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/rightArrow"), FlagRight, Color.BurlyWood);
            r = Description.Menu;
            if (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.TransitionOff)
            {
                r.X += (int)(transitionOffset * 400f);
            }
            Description.Update(r);
            Description.subMenu = null;
            Description.Draw();
            rpos = new Vector2((r.X + 20), (Description.Menu.Y + 20));
            DescriptionSL.Draw(ScreenManager.SpriteBatch);
            Vector2 drawCurs = rpos;
            foreach (ScrollList.Entry e in DescriptionSL.VisibleEntries)
            {
                if (!e.Hovered)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, e.item as string, drawCurs, Color.White);
                    drawCurs.Y += Fonts.Arial12.LineSpacing;
                }
            }
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
            if (Traits.Tabs[0].Selected || Traits.Tabs[1].Selected || Traits.Tabs[2].Selected)
            {
                var bCursor = new Vector2(Traits.Menu.X + 20, Traits.Menu.Y + 45);
                foreach (ScrollList.Entry e in traitsSL.VisibleEntries)
                {
                    string name = PaddedWithDots((e.item as TraitEntry).trait.TraitName, Traits.Menu.Width - 70);

                    if (e.Hovered)
                    {
                        bCursor.Y = (e.Y - 5);
                        var tCursor = new Vector2(bCursor.X, bCursor.Y + 3f);

                        var drawColor = new Color(95, 95, 95, 95);
                        if (!(e.item as TraitEntry).Selected)
                        {
                            drawColor = new Color(95, 95, 95, 95);
                        }
                        if ((e.item as TraitEntry).Selected)
                        {
                            drawColor = ((e.item as TraitEntry).trait.Cost > 0 ? Color.ForestGreen : Color.Crimson);
                        }
                        else if ((e.item as TraitEntry).Excluded)
                        {
                            drawColor = new Color(95, 95, 95, 95);
                        }
                        else if (TotalPointsUsed >= 0 && TotalPointsUsed - (e.item as TraitEntry).trait.Cost >= 0 || (e.item as TraitEntry).trait.Cost < 0)
                        {
                            drawColor = ((e.item as TraitEntry).trait.Cost > 0 ? Color.MediumSeaGreen : Color.LightCoral);
                        }
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, name, tCursor, drawColor);
                        Vector2 curs = bCursor;
                        curs.X = curs.X + (Traits.Menu.Width - 45 - Fonts.Arial14Bold.MeasureString((e.item as TraitEntry).trait.Cost.ToString()).X);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, (e.item as TraitEntry).trait.Cost.ToString(), curs, drawColor);
                        tCursor.Y = tCursor.Y + Fonts.Arial14Bold.LineSpacing;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(Localizer.Token((e.item as TraitEntry).trait.Description), Traits.Menu.Width - 45), tCursor, drawColor);
                        
                        e.DrawPlus(ScreenManager.SpriteBatch);
                    }
                    else
                    {
                        bCursor.Y = e.Y - 5;
                        var tCursor = new Vector2(bCursor.X, bCursor.Y + 3f);
                        var drawColor = new Color(95, 95, 95, 95);

                        if (!(e.item as TraitEntry).Selected)
                        {
                            drawColor = new Color(95, 95, 95, 95);
                        }
                        if ((e.item as TraitEntry).Selected)
                        {
                            drawColor = ((e.item as TraitEntry).trait.Cost > 0 ? Color.ForestGreen : Color.Crimson);
                        }
                        else if ((e.item as TraitEntry).Excluded)
                        {
                            drawColor = new Color(95, 95, 95, 95);
                        }
                        else if (TotalPointsUsed >= 0 && TotalPointsUsed - (e.item as TraitEntry).trait.Cost >= 0 || (e.item as TraitEntry).trait.Cost < 0)
                        {
                            drawColor = ((e.item as TraitEntry).trait.Cost > 0 ? Color.MediumSeaGreen : Color.LightCoral);
                        }
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, name, tCursor, drawColor);
                        Vector2 curs = bCursor;
                        curs.X = curs.X + (Traits.Menu.Width - 45 - Fonts.Arial14Bold.MeasureString((e.item as TraitEntry).trait.Cost.ToString()).X);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, (e.item as TraitEntry).trait.Cost.ToString(), curs, drawColor);
                        tCursor.Y = tCursor.Y + Fonts.Arial14Bold.LineSpacing;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(Localizer.Token((e.item as TraitEntry).trait.Description), Traits.Menu.Width - 45), tCursor, drawColor);
                        e.DrawPlus(ScreenManager.SpriteBatch);
                    }

                    e.CheckHover(Input.CursorPosition);
                }
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(24), ": "), new Vector2(GalaxySizeRect.X, GalaxySizeRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Galaxysize.ToString(), new Vector2(GalaxySizeRect.X + 190 - Fonts.Arial12.MeasureString(Galaxysize.ToString()).X, GalaxySizeRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(25), " : "), new Vector2(NumberStarsRect.X, NumberStarsRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, StarEnum.ToString(), new Vector2(NumberStarsRect.X + 190 - Fonts.Arial12.MeasureString(StarEnum.ToString()).X, NumberStarsRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2102), " : "), new Vector2(NumOpponentsRect.X, NumOpponentsRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, numOpponents.ToString(), new Vector2(NumOpponentsRect.X + 190 - Fonts.Arial12.MeasureString(numOpponents.ToString()).X, NumOpponentsRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2105), " : "), new Vector2(GameModeRect.X, GameModeRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2133), " : "), new Vector2(PacingRect.X, PacingRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Pacing.ToString(), "%"), new Vector2(PacingRect.X + 190 - Fonts.Arial12.MeasureString(string.Concat(Pacing.ToString(), "%")).X, PacingRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2139), " : "), new Vector2(DifficultyRect.X, DifficultyRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, difficulty.ToString(), new Vector2(DifficultyRect.X + 190 - Fonts.Arial12.MeasureString(difficulty.ToString()).X, DifficultyRect.Y), Color.BurlyWood);

            //Added by Gretman
            string ExtraRemnantString = string.Concat(Localizer.Token(4101), " : ");
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, ExtraRemnantString, new Vector2(ExtraRemnantRect.X, ExtraRemnantRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, ExtraRemnant.ToString(), new Vector2(ExtraRemnantRect.X + 190 - Fonts.Arial12.MeasureString(ExtraRemnant.ToString()).X, ExtraRemnantRect.Y), Color.BurlyWood);

            string txt = "";
            int tip = 0;
            //if (this.mode == RaceDesignScreen.GameMode.PreWarp)
            //{
            //    txt = "Pre-Warp";
            //    tip = 111;
            //    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2((float)(this.GameModeRect.X + 190) - Fonts.Arial12.MeasureString(txt).X, (float)this.GameModeRect.Y), Color.BurlyWood);
            //    if (MathExt.HitTest(this.GameModeRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
            //    {
            //        ToolTip.CreateTooltip("Play with a new, hardcore ruleset that makes radical changes to the StarDrive FTL systems", base.ScreenManager);
            //    }
            //}
            //else 
                if (mode == GameMode.Sandbox)
            {
                txt = Localizer.Token(2103);
                tip = 112;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (mode == GameMode.Elimination)
            {
                txt = Localizer.Token(6093);
                tip = 165;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (mode == GameMode.Corners)    //Added by Gretman
            {
                txt = Localizer.Token(4102);
                tip = 229;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            //else if (this.mode == RaceDesignScreen.GameMode.Warlords)
            //{
            //    txt = "War Lords";//Localizer.Token(2103);
            //    tip = 112;
            //    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2((float)(this.GameModeRect.X + 190) - Fonts.Arial12.MeasureString(txt).X, (float)this.GameModeRect.Y), Color.BurlyWood);
            //    if (MathExt.HitTest(this.GameModeRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
            //    {
            //        ToolTip.CreateTooltip(tip, base.ScreenManager);
            //    }
            //}
            if (ScaleRect.HitTest(Input.CursorPosition))
            {
                ToolTip.CreateTooltip(125);
            }
            if (PacingRect.HitTest(Input.CursorPosition))
            {
                ToolTip.CreateTooltip(126);
            }

            selector?.Draw(batch);
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

        private void OnEngageClicked(UIButton b)
        {
            OnEngage();
        }
        private void OnRuleOptionsClicked(UIButton b)
        {
            ScreenManager.AddScreen(new RuleOptionsScreen(this));
        }
        private void OnAbortClicked(UIButton b)
        {
            ExitScreen();
        }
        private void OnClearClicked(UIButton b)
        {
            foreach (TraitEntry trait in AllTraits)
                trait.Selected = false;
            TotalPointsUsed = 8;
        }
        private void OnLoadRaceClicked(UIButton b)
        {
            ScreenManager.AddScreen(new LoadRaceScreen(this));
        }
        private void OnSaveRaceClicked(UIButton b)
        {
            ScreenManager.AddScreen(new SaveRaceScreen(this, GetRacialTraits()));
        }
        private void OnLoadSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new LoadSetupScreen(this));
        }
        private void OnSaveSetupClicked(UIButton b)
        {
            ScreenManager.AddScreen(new SaveSetupScreen(this, difficulty, StarEnum, Galaxysize, Pacing,
                ExtraRemnant, numOpponents, mode));
        }

        public override bool HandleInput(InputState input)
        {
            DescriptionSL.HandleInput(input);
            if (!DrawingColorSelector)
            {
                selector = null;
                foreach (ScrollList.Entry e in RaceArchetypeSL.AllEntries)
                {
                    if (e.WasClicked(input))
                    {
                        SelectedData = e.item as EmpireData;
                        GameAudio.PlaySfxAsync("echo_affirm");
                        SetEmpireData(SelectedData.Traits);
                    }
                }
                RaceArchetypeSL.HandleInput(input);
                Traits.HandleInput(input);
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
                traitsSL.HandleInput(input);
                foreach (ScrollList.Entry f in traitsSL.VisibleEntries)
                {
                    if (!f.CheckHover(input.CursorPosition))
                        continue;

                    selector = f.CreateSelector();
                    var t = f.Get<TraitEntry>();
                    if (input.LeftMouseClick)
                    {
                        if (t.Selected && TotalPointsUsed + t.trait.Cost >= 0)
                        {
                            t.Selected = !t.Selected;
                            RaceDesignScreen totalPointsUsed = this;
                            totalPointsUsed.TotalPointsUsed = totalPointsUsed.TotalPointsUsed + t.trait.Cost;
                            GameAudio.PlaySfxAsync("blip_click");
                            foreach (TraitEntry ex in AllTraits)
                                if (t.trait.Excludes == ex.trait.TraitName)
                                    ex.Excluded = false;
                        }
                        else if (TotalPointsUsed - t.trait.Cost < 0 || t.Selected)
                        {
                            GameAudio.PlaySfxAsync("UI_Misc20");
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
                                GameAudio.PlaySfxAsync("blip_click");
                                foreach (TraitEntry ex in AllTraits)
                                {
                                    if (t.trait.Excludes == ex.trait.TraitName)
                                        ex.Excluded = true;
                                }
                            }
                        }
                        DoRaceDescription();
                    }
                }
                if (GalaxySizeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.PlaySfxAsync("blip_click");
                    RaceDesignScreen galaxysize = this;
                    galaxysize.Galaxysize = (GalSize)((int)galaxysize.Galaxysize + (int)GalSize.Small);
                    if (Galaxysize > GalSize.TrulyEpic)   //Resurrecting TrulyEpic Map UniverseRadius -Gretman
                    {
                        Galaxysize = GalSize.Tiny;
                    }
                }
                if (GameModeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.PlaySfxAsync("blip_click");
                    //RaceDesignScreen gamemode = this;
                    mode = mode + 1;
                    if (mode == GameMode.Corners) numOpponents = 3;
                    if (mode > GameMode.Corners)  //Updated by Gretman
                    {
                        mode = GameMode.Sandbox;
                    }
                }
                if (NumberStarsRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.PlaySfxAsync("blip_click");
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
                    int maxOpponents = mode == GameMode.Corners ? 3 
                        : GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
                    numOpponents = numOpponents + 1;
                    
                    if (numOpponents > maxOpponents)                    
                        numOpponents = 1;
                    
                }
                //MathExt.HitTest(this.GameModeRect, mousePos); // I believe this is here by mistake, since the returned value would do nothing... - Gretman
                if (ScaleRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen gameScale = this;
                        gameScale.GameScale = gameScale.GameScale + 1;
                        if (GameScale > 6)
                        {
                            GameScale = 1;
                        }
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen gameScale1 = this;
                        gameScale1.GameScale = gameScale1.GameScale - 1;
                        if (GameScale < 1)
                        {
                            GameScale = 6;
                        }
                    }
                }
                if (PacingRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen pacing = this;
                        pacing.Pacing = pacing.Pacing + 25;
                        if (Pacing > 400)
                        {
                            Pacing = 100;
                        }
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen pacing1 = this;
                        pacing1.Pacing = pacing1.Pacing - 25;
                        if (Pacing < 100)
                        {
                            Pacing = 400;
                        }
                    }
                }
                if (DifficultyRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen raceDesignScreen2 = this;
                        raceDesignScreen2.difficulty = (UniverseData.GameDifficulty)((int)raceDesignScreen2.difficulty + (int)UniverseData.GameDifficulty.Normal);
                        if (difficulty > UniverseData.GameDifficulty.Brutal)
                        {
                            difficulty = UniverseData.GameDifficulty.Easy;
                        }
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        RaceDesignScreen raceDesignScreen3 = this;
                        raceDesignScreen3.difficulty = (UniverseData.GameDifficulty)((int)raceDesignScreen3.difficulty - (int)UniverseData.GameDifficulty.Normal);
                        if (difficulty < UniverseData.GameDifficulty.Easy)
                        {
                            difficulty = UniverseData.GameDifficulty.Brutal;
                        }
                    }
                }

                //Gretman - Remnant Presece Button
                if (ExtraRemnantRect.HitTest(input.CursorPosition))
                {
                    if (input.LeftMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        ++ExtraRemnant;
                        if (ExtraRemnant > ExtraRemnantPresence.Everywhere)
                            ExtraRemnant = ExtraRemnantPresence.Rare;
                    }
                    if (input.RightMouseClick)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        --ExtraRemnant;
                        if (ExtraRemnant < 0)
                            ExtraRemnant = ExtraRemnantPresence.Everywhere;
                    }
                }// Done adding stuff - Gretman

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
                    GameAudio.PlaySfxAsync("blip_click");
                }
                if (FlagLeft.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    if (FlagIndex <= 0)
                        FlagIndex = ResourceManager.NumFlags - 1;
                    else
                        FlagIndex = FlagIndex - 1;
                    GameAudio.PlaySfxAsync("blip_click");
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
                        var thisColor = new Color(Convert.ToByte(i), Convert.ToByte(j), currentObjectColor.B);
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
                    yPosition = yPosition + 2;
                }
            }
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }


        private RacialTrait GetRacialTraits()
        {
            var traits = new RacialTrait();
            traits = RaceSummary.GetClone();
            traits.Singular = SingEntry.Text;
            traits.Plural = PlurEntry.Text;
            traits.HomeSystemName = HomeSystemEntry.Text;
            traits.R = currentObjectColor.R;
            traits.G = currentObjectColor.G;
            traits.B = currentObjectColor.B;
            traits.FlagIndex = FlagIndex;
            traits.HomeworldName = HomeWorldName;
            traits.Name = RaceName.Text;
            traits.ShipType = SelectedData.Traits.ShipType;
            traits.VideoPath = SelectedData.Traits.VideoPath;
            return traits;
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
            Name = new Menu1(nameRect);
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
            RaceArchetypeSL = new ScrollList(arch, 135);

            foreach (EmpireData e in ResourceManager.Empires)
            {
                if (e.Faction == 1 || e.MinorRace)
                {
                    continue;
                }
                RaceArchetypeSL.AddItem(e);
                if (string.IsNullOrEmpty(e.Traits.VideoPath))
                {
                    continue;
                }
                TextureDict.Add(e, ResourceManager.Texture("Races/"+e.Traits.VideoPath));
            }
            foreach (EmpireData e in ResourceManager.Empires)
            {
                if (e.Traits.Singular == "Human") SelectedData = e;
            }
            RaceName.Text = SelectedData.Traits.Name;
            SingEntry.Text = SelectedData.Traits.Singular;
            PlurEntry.Text = SelectedData.Traits.Plural;
            HomeSystemEntry.Text = SelectedData.Traits.HomeSystemName;
            HomeWorldName = SelectedData.Traits.HomeworldName;
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
            DescriptionSL = new ScrollList(dsub, Fonts.Arial12.LineSpacing);
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
            traitsSL = new ScrollList(Traits, size);
            foreach (TraitEntry t in AllTraits)
            {
                if (t.trait.Category == "Physical")
                    traitsSL.AddItem(t);
            }

            Engage      = ButtonMedium(ScreenWidth - 140, ScreenHeight - 40, titleId:22, click: OnEngageClicked);
            Abort       = ButtonMedium(10, ScreenHeight - 40, titleId:23, click: OnAbortClicked);
            ClearTraits = ButtonMedium(ScreenWidth - 150,
                            Description.Menu.Y + Description.Menu.Height - 40, "Clear Traits", OnClearClicked);

            DoRaceDescription();
            SetEmpireData(SelectedData.Traits);

            LoadRace  = ButtonMedium(smaller.X + (smaller.Width / 2) - 142, smaller.Y - 20, "Load Race", OnLoadRaceClicked);
            SaveRace  = ButtonMedium(smaller.X + (smaller.Width / 2) + 10, smaller.Y - 20, "Save Race", OnSaveRaceClicked);

            var pos = new Vector2(ScreenWidth / 2 - 84, leftRect.Y + leftRect.Height + 10);

            LoadSetup = ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            SaveSetup = ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            RulesOptions = Button(pos.X, pos.Y, titleId: 4006, click: OnRuleOptionsClicked);

            base.LoadContent();
        }

        protected virtual void OnEngage()
        {
            if (mode == GameMode.Elimination) GlobalStats.EliminationMode = true;
            if (mode == GameMode.Corners) GlobalStats.CornersGame = true;

            GlobalStats.ExtraRemnantGS = (int)ExtraRemnant;
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
            RaceSummary.ShipType       = SelectedData.Traits.ShipType;
            RaceSummary.VideoPath      = SelectedData.Traits.VideoPath;
            var playerEmpire = new Empire
            {
                EmpireColor = currentObjectColor,
                data = SelectedData
            };
            playerEmpire.data.SpyModifier = RaceSummary.SpyMultiplier;
            playerEmpire.data.Traits.Spiritual = RaceSummary.Spiritual;
            RaceSummary.Adj1 = SelectedData.Traits.Adj1;
            RaceSummary.Adj2 = SelectedData.Traits.Adj2;
            playerEmpire.data.Traits = RaceSummary;
            playerEmpire.EmpireColor = currentObjectColor;

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
            var ng = new CreatingNewGameScreen(playerEmpire, Galaxysize.ToString(), modifier, 
                                               numOpponents, mode, pace, GameScale, difficulty, mmscreen);
            ScreenManager.GoToScreen(ng, clear3DObjects:true);
        }


        public virtual void ResetLists()
        {
            traitsSL.Reset();
            if (Traits.Tabs[0].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Physical")
                        traitsSL.AddItem(t);
            }
            else if (Traits.Tabs[1].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Industry")
                        traitsSL.AddItem(t);
            }
            else if (Traits.Tabs[2].Selected)
            {
                foreach (TraitEntry t in AllTraits)
                    if (t.trait.Category == "Special")
                        traitsSL.AddItem(t);
            }
        }

        public void SetCustomSetup(UniverseData.GameDifficulty gameDifficulty, StarNum StarEnum, GalSize Galaxysize, int Pacing, ExtraRemnantPresence ExtraRemnant, int numOpponents, GameMode mode)
        {
            difficulty        = gameDifficulty;
            this.StarEnum     = StarEnum;
            this.Galaxysize   = Galaxysize;
            this.Pacing       = Pacing;
            this.ExtraRemnant = ExtraRemnant;
            this.numOpponents = numOpponents;
            this.mode         = mode;
        }

        public void SetCustomEmpireData(RacialTrait traits)    // Sets the empire data externally, checks for fields that are default so don't overwrite
        {
            foreach (EmpireData origRace in RaceArchetypeSL.AllItems<EmpireData>())
            {
                if (origRace.Traits.ShipType == traits.ShipType)
                {
                    if (traits.Name == origRace.Traits.Name)
                        traits.Name = RaceName.Text;
                    if (traits.Singular == origRace.Traits.Singular)
                        traits.Singular = SingEntry.Text;
                    if (traits.Plural == origRace.Traits.Plural)
                        traits.Plural = PlurEntry.Text;
                    if (traits.HomeSystemName == origRace.Traits.HomeSystemName)
                        traits.HomeSystemName = HomeSystemEntry.Text;
                    if (traits.FlagIndex == origRace.Traits.FlagIndex)
                        traits.FlagIndex = FlagIndex;

                    if (traits.R == origRace.Traits.R && traits.G == origRace.Traits.G && traits.B == origRace.Traits.B)
                    {
                        traits.R = currentObjectColor.R;
                        traits.G = currentObjectColor.G;
                        traits.B = currentObjectColor.B;
                    }
                    break;
                }
            }
            SetEmpireData(traits);
        }

        private void SetEmpireData(RacialTrait traits)
        {
            RaceSummary.ShipType = traits.ShipType;
            FlagIndex = traits.FlagIndex;
            currentObjectColor = new Color((byte)traits.R, (byte)traits.G, (byte)traits.B, 255);
            RaceName.Text        = traits.Name;
            SingEntry.Text       = traits.Singular;
            PlurEntry.Text       = traits.Plural;
            HomeSystemEntry.Text = traits.HomeSystemName;
            HomeSystemName       = traits.HomeSystemName;
            HomeWorldName        = traits.HomeworldName;
            TotalPointsUsed      = 8;
            foreach (TraitEntry t in AllTraits)
            {
                t.Selected = false;
                //Added by McShooterz: Searches for new trait tags
                if ((traits.ConsumptionModifier > 0f || traits.PhysicalTraitGluttonous) && t.trait.ConsumptionModifier > 0f 
                    || t.trait.ConsumptionModifier < 0f && (traits.ConsumptionModifier < 0f || traits.PhysicalTraitEfficientMetabolism)
                    || (traits.DiplomacyMod > 0f || traits.PhysicalTraitAlluring) && t.trait.DiplomacyMod > 0f 
                    || t.trait.DiplomacyMod < 0f && (traits.DiplomacyMod < 0f || traits.PhysicalTraitRepulsive)
                    || (traits.EnergyDamageMod > 0f || traits.PhysicalTraitEagleEyed) && t.trait.EnergyDamageMod > 0f
                    || t.trait.EnergyDamageMod < 0f && (traits.EnergyDamageMod < 0f || traits.PhysicalTraitBlind)
                    || (traits.MaintMod > 0f || traits.SociologicalTraitWasteful) && t.trait.MaintMod > 0f 
                    || t.trait.MaintMod < 0f && (traits.MaintMod < 0f || traits.SociologicalTraitEfficient)
                    || (traits.PopGrowthMax > 0f || traits.PhysicalTraitLessFertile) && t.trait.PopGrowthMax > 0f 
                    || (traits.PopGrowthMin > 0f || traits.PhysicalTraitFertile) && t.trait.PopGrowthMin > 0f 
                    || (traits.ResearchMod > 0f || traits.PhysicalTraitSmart) && t.trait.ResearchMod > 0f 
                    || t.trait.ResearchMod < 0f && (traits.ResearchMod < 0f || traits.PhysicalTraitDumb)
                    || t.trait.ShipCostMod < 0f && (traits.ShipCostMod < 0f || traits.HistoryTraitNavalTraditions) 
                    || (traits.TaxMod > 0f || traits.SociologicalTraitMeticulous) && t.trait.TaxMod > 0f 
                    || t.trait.TaxMod < 0f && (traits.TaxMod < 0f || traits.SociologicalTraitCorrupt)
                    || (traits.ProductionMod > 0f || traits.SociologicalTraitIndustrious) && t.trait.ProductionMod > 0f 
                    || t.trait.ProductionMod < 0f && (traits.ProductionMod < 0f || traits.SociologicalTraitLazy)
                    || (traits.ModHpModifier > 0f || traits.SociologicalTraitSkilledEngineers) && t.trait.ModHpModifier > 0f 
                    || t.trait.ModHpModifier < 0f && (traits.ModHpModifier < 0f || traits.SociologicalTraitHaphazardEngineers)
                    || (traits.Mercantile > 0f || traits.SociologicalTraitMercantile) && t.trait.Mercantile > 0f  
                    || (traits.GroundCombatModifier > 0f || traits.PhysicalTraitSavage) && t.trait.GroundCombatModifier > 0f 
                    || t.trait.GroundCombatModifier < 0f && (traits.GroundCombatModifier < 0f || traits.PhysicalTraitTimid)
                    || (traits.Cybernetic > 0 || traits.HistoryTraitCybernetic) && t.trait.Cybernetic > 0 
                    || (traits.DodgeMod > 0f || traits.PhysicalTraitReflexes) && t.trait.DodgeMod > 0f 
                    || t.trait.DodgeMod < 0f && (traits.DodgeMod < 0f || traits.PhysicalTraitPonderous) 
                    || (traits.HomeworldSizeMod > 0f || traits.HistoryTraitHugeHomeWorld) && t.trait.HomeworldSizeMod > 0f 
                    || t.trait.HomeworldSizeMod < 0f && (traits.HomeworldSizeMod < 0f || traits.HistoryTraitSmallHomeWorld)
                    || t.trait.HomeworldFertMod < 0f && (traits.HomeworldFertMod < 0f || traits.HistoryTraitPollutedHomeWorld) && t.trait.HomeworldRichMod == 0f
                    || t.trait.HomeworldFertMod < 0f && (traits.HomeworldRichMod > 0f || traits.HistoryTraitIndustrializedHomeWorld) && t.trait.HomeworldRichMod != 0f
                    || (traits.Militaristic > 0 || traits.HistoryTraitMilitaristic) && t.trait.Militaristic > 0 
                    || (traits.PassengerModifier > 1 || traits.HistoryTraitManifestDestiny) && t.trait.PassengerModifier > 1 
                    || (traits.BonusExplored > 0 || traits.HistoryTraitAstronomers) && t.trait.BonusExplored > 0 
                    || (traits.Spiritual > 0f || traits.HistoryTraitSpiritual) && t.trait.Spiritual > 0f 
                    || (traits.Prototype > 0 || traits.HistoryTraitPrototypeFlagship) && t.trait.Prototype > 0 
                    || (traits.Pack || traits.HistoryTraitPackMentality) && t.trait.Pack 
                    || (traits.SpyMultiplier > 0f || traits.HistoryTraitDuplicitous) && t.trait.SpyMultiplier > 0f 
                    || (traits.SpyMultiplier < 0f || traits.HistoryTraitHonest) && t.trait.SpyMultiplier < 0f)
                {
                    t.Selected = true;
                    TotalPointsUsed -= t.trait.Cost;
                }
                if (!t.Selected)
                {
                    continue;
                }
                SetExclusions(t);
            }
            DoRaceDescription();
        }

        private void SetExclusions(TraitEntry t)
        {
            foreach (TraitEntry ex in AllTraits)
                if (t.trait.Excludes == ex.trait.TraitName)
                    ex.Excluded = true;
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

            UpdateSummary();
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        private void UpdateSummary()
        {
            Singular = SingEntry.Text;
            Plural = PlurEntry.Text;
            HomeSystemName = HomeSystemEntry.Text;
            RaceSummary = new RacialTrait();
            foreach (TraitEntry t in AllTraits)
            {
                if (!t.Selected)
                    continue;
                //Added by McShooterz: code cleaning
                RaceSummary.ConsumptionModifier    += t.trait.ConsumptionModifier;
                RaceSummary.DiplomacyMod           += t.trait.DiplomacyMod;
                RaceSummary.EnergyDamageMod        += t.trait.EnergyDamageMod;
                RaceSummary.MaintMod               += t.trait.MaintMod;
                RaceSummary.ReproductionMod        += t.trait.ReproductionMod;
                RaceSummary.PopGrowthMax           += t.trait.PopGrowthMax;
                RaceSummary.PopGrowthMin           += t.trait.PopGrowthMin;
                RaceSummary.ResearchMod            += t.trait.ResearchMod;
                RaceSummary.ShipCostMod            += t.trait.ShipCostMod;
                RaceSummary.TaxMod                 += t.trait.TaxMod;
                RaceSummary.ProductionMod          += t.trait.ProductionMod;
                RaceSummary.ModHpModifier          += t.trait.ModHpModifier;
                RaceSummary.Mercantile             += t.trait.Mercantile;
                RaceSummary.GroundCombatModifier   += t.trait.GroundCombatModifier;
                RaceSummary.Cybernetic             += t.trait.Cybernetic;
                RaceSummary.Blind                  += t.trait.Blind;
                RaceSummary.DodgeMod               += t.trait.DodgeMod;
                RaceSummary.HomeworldFertMod       += t.trait.HomeworldFertMod;
                RaceSummary.HomeworldRichMod       += t.trait.HomeworldRichMod;
                RaceSummary.HomeworldSizeMod       += t.trait.HomeworldSizeMod;
                RaceSummary.Militaristic           += t.trait.Militaristic;
                RaceSummary.BonusExplored          += t.trait.BonusExplored;
                RaceSummary.Prototype              += t.trait.Prototype;
                RaceSummary.Spiritual              += t.trait.Spiritual;
                RaceSummary.SpyMultiplier          += t.trait.SpyMultiplier;
                RaceSummary.RepairMod              += t.trait.RepairMod;
                RaceSummary.PassengerModifier      += t.trait.PassengerBonus;

                if (t.trait.Pack)
                    RaceSummary.Pack = t.trait.Pack;
            }
        }
        
        public enum Difficulty
        {
            Easy, Normal, Hard, Brutal
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

        public enum ExtraRemnantPresence
        {
            Rare, Normal, More, MuchMore, Everywhere
        }
    }
}
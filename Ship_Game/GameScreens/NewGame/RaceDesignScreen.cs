using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public class RaceDesignScreen : GameScreen, IListScreen
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
        Menu1 ChooseRaceMenu;
        ScrollList RaceArchetypeSL;
        Submenu arch;
        Rectangle PacingRect;

        int Pacing = 100;

        Rectangle ScaleRect = new Rectangle();
        Rectangle dslrect;
        Rectangle GameModeRect;
        Rectangle DifficultyRect;

        Map<IEmpireData, SubTexture> TextureDict = new Map<IEmpireData, SubTexture>();

        ScrollList DescriptionSL;
        protected UIButton Engage;
        protected UIButton Abort;
        protected UIButton ClearTraits;

        int numOpponents;
        protected RacialTrait tipped;
        protected float tTimer = 0.35f;
        string RaceDescr = "";

        protected int FlagIndex;
        protected int TotalPointsUsed = 8;
        protected bool DrawingColorSelector;

        UniverseData.GameDifficulty SelectedDifficulty = UniverseData.GameDifficulty.Normal;
        IEmpireData SelectedData;
        protected Color currentObjectColor = Color.White;
        protected Rectangle ColorSelector;

        protected string Singular = "Human";
        protected string Plural = "Humans";
        protected string HomeWorldName = "Earth";
        protected string HomeSystemName = "Sol";

        Rectangle ExtraRemnantRect; //Added by Gretman
        ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;

        public RaceDesignScreen(MainMenuScreen mainMenu) : base(mainMenu)
        {
            MainMenu = mainMenu;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(1);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            foreach (RacialTrait t in ResourceManager.RaceTraits.TraitList)
            {
                AllTraits.Add(new TraitEntry { trait = t });
            }
            GlobalStats.Statreset();
            numOpponents = GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
        }

        protected void DoRaceDescription()
        {
            UpdateSummary();
            RaceDescr = "";
            RaceDesignScreen raceDesignScreen = this;
            string str = raceDesignScreen.RaceDescr;
            string[] text = { str, RaceName.Text, Localizer.Token(1300), Plural, ". " };
            raceDesignScreen.RaceDescr = string.Concat(text);
            if (RaceSummary.Cybernetic <= 0)
            {
                RaceDesignScreen raceDesignScreen1 = this;
                raceDesignScreen1.RaceDescr = string.Concat(raceDesignScreen1.RaceDescr, Plural, Localizer.Token(1302));
            }
            else
            {
                RaceDesignScreen raceDesignScreen2 = this;
                raceDesignScreen2.RaceDescr = string.Concat(raceDesignScreen2.RaceDescr, Plural, Localizer.Token(1301));
            }
            if (RaceSummary.Aquatic <= 0)
            {
                RaceDescr = RaceDescr + Localizer.Token(1304);
            }
            else
            {
                RaceDescr = RaceDescr + Localizer.Token(1303);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.PopGrowthMin > 0f)
                {
                    RaceDescr += Localizer.Token(1305);
                }
                else if (RaceSummary.PopGrowthMin ==0 && RaceSummary.PopGrowthMax==0)
                {
                    RaceDescr += Localizer.Token(1307);
                }
                else
                {
                    RaceDescr += Localizer.Token(1306);
                }
            }
            else if (RaceSummary.PopGrowthMin > 0f)
            {
                RaceDescr += Localizer.Token(1308);
            }
            else if (RaceSummary.PopGrowthMin == 0 && RaceSummary.PopGrowthMax == 0)
            {
                RaceDescr += Localizer.Token(1310);
            }
            else
            {
                RaceDescr += Localizer.Token(1309);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.ConsumptionModifier > 0f)
                {
                    RaceDescr += Localizer.Token(1311);
                }
                else if (RaceSummary.ConsumptionModifier >= 0f)
                {
                    RaceDescr += Localizer.Token(1313);
                }
                else
                {
                    RaceDescr += Localizer.Token(1312);
                }
            }
            else if (RaceSummary.ConsumptionModifier > 0f)
            {
                RaceDescr += Localizer.Token(1314);
            }
            else if (RaceSummary.ConsumptionModifier >= 0f)
            {
                RaceDescr += Localizer.Token(1316);
            }
            else
            {
                RaceDescr += Localizer.Token(1315);
            }
            if (RaceSummary.Cybernetic <= 0)
            {
                if (RaceSummary.GroundCombatModifier > 0f)
                {
                    if (RaceSummary.DiplomacyMod > 0f)
                    {
                        RaceDesignScreen raceDesignScreen17 = this;
                        raceDesignScreen17.RaceDescr = string.Concat(raceDesignScreen17.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1318));
                    }
                    else if (RaceSummary.DiplomacyMod >= 0f)
                    {
                        RaceDesignScreen raceDesignScreen18 = this;
                        raceDesignScreen18.RaceDescr = string.Concat(raceDesignScreen18.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1320));
                    }
                    else
                    {
                        RaceDesignScreen raceDesignScreen19 = this;
                        raceDesignScreen19.RaceDescr = string.Concat(raceDesignScreen19.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1319));
                    }
                }
                else if (RaceSummary.GroundCombatModifier >= 0f)
                {
                    if (RaceSummary.GroundCombatModifier == 0f)
                    {
                        if (RaceSummary.DiplomacyMod > 0f)
                        {
                            RaceDesignScreen raceDesignScreen20 = this;
                            raceDesignScreen20.RaceDescr = string.Concat(raceDesignScreen20.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1324));
                        }
                        else if (RaceSummary.DiplomacyMod >= 0f)
                        {
                            RaceDesignScreen raceDesignScreen21 = this;
                            raceDesignScreen21.RaceDescr = string.Concat(raceDesignScreen21.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1326));
                        }
                        else
                        {
                            RaceDesignScreen raceDesignScreen22 = this;
                            raceDesignScreen22.RaceDescr = string.Concat(raceDesignScreen22.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1325));
                        }
                    }
                }
                else if (RaceSummary.DiplomacyMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen23 = this;
                    raceDesignScreen23.RaceDescr = string.Concat(raceDesignScreen23.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1321));
                }
                else if (RaceSummary.DiplomacyMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen24 = this;
                    raceDesignScreen24.RaceDescr = string.Concat(raceDesignScreen24.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1323));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen25 = this;
                    raceDesignScreen25.RaceDescr = string.Concat(raceDesignScreen25.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1322));
                }
            }
            else if (RaceSummary.GroundCombatModifier > 0f)
            {
                if (RaceSummary.DiplomacyMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen26 = this;
                    raceDesignScreen26.RaceDescr = string.Concat(raceDesignScreen26.RaceDescr, Localizer.Token(1327), Plural, Localizer.Token(1328));
                }
                else if (RaceSummary.DiplomacyMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen27 = this;
                    raceDesignScreen27.RaceDescr = string.Concat(raceDesignScreen27.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1330));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen28 = this;
                    raceDesignScreen28.RaceDescr = string.Concat(raceDesignScreen28.RaceDescr, Localizer.Token(1327), Plural, Localizer.Token(1329));
                }
            }
            else if (RaceSummary.GroundCombatModifier >= 0f)
            {
                if (RaceSummary.GroundCombatModifier == 0f)
                {
                    if (RaceSummary.DiplomacyMod > 0f)
                    {
                        RaceDesignScreen raceDesignScreen29 = this;
                        raceDesignScreen29.RaceDescr = string.Concat(raceDesignScreen29.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1334));
                    }
                    else if (RaceSummary.DiplomacyMod >= 0f)
                    {
                        RaceDesignScreen raceDesignScreen30 = this;
                        raceDesignScreen30.RaceDescr = string.Concat(raceDesignScreen30.RaceDescr, Localizer.Token(1336), Plural, Localizer.Token(1337));
                    }
                    else
                    {
                        RaceDesignScreen raceDesignScreen31 = this;
                        raceDesignScreen31.RaceDescr = string.Concat(raceDesignScreen31.RaceDescr, Localizer.Token(1327), Plural, Localizer.Token(1335));
                    }
                }
            }
            else if (RaceSummary.DiplomacyMod > 0f)
            {
                RaceDesignScreen raceDesignScreen32 = this;
                raceDesignScreen32.RaceDescr = string.Concat(raceDesignScreen32.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1331));
            }
            else if (RaceSummary.DiplomacyMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen33 = this;
                raceDesignScreen33.RaceDescr = string.Concat(raceDesignScreen33.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1333));
            }
            else
            {
                RaceDesignScreen raceDesignScreen34 = this;
                raceDesignScreen34.RaceDescr = string.Concat(raceDesignScreen34.RaceDescr, Localizer.Token(1317), Plural, Localizer.Token(1332));
            }
            if (RaceSummary.GroundCombatModifier < 0f || RaceSummary.DiplomacyMod <= 0f)
            {
                if (RaceSummary.ResearchMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen35 = this;
                    raceDesignScreen35.RaceDescr = string.Concat(raceDesignScreen35.RaceDescr, Localizer.Token(1338), Plural, Localizer.Token(1339));
                }
                else if (RaceSummary.ResearchMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen36 = this;
                    raceDesignScreen36.RaceDescr = string.Concat(raceDesignScreen36.RaceDescr, Plural, Localizer.Token(1342));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen37 = this;
                    raceDesignScreen37.RaceDescr = string.Concat(raceDesignScreen37.RaceDescr, Localizer.Token(1340), Plural, Localizer.Token(1341));
                }
            }
            else if (RaceSummary.GroundCombatModifier <= 0f && RaceSummary.DiplomacyMod <= 0f)
            {
                if (RaceSummary.ResearchMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen38 = this;
                    raceDesignScreen38.RaceDescr = string.Concat(raceDesignScreen38.RaceDescr, Plural, Localizer.Token(1344));
                }
                else if (RaceSummary.ResearchMod >= 0f)
                {
                    RaceDesignScreen raceDesignScreen39 = this;
                    raceDesignScreen39.RaceDescr = string.Concat(raceDesignScreen39.RaceDescr, Plural, Localizer.Token(1342));
                }
                else
                {
                    RaceDesignScreen raceDesignScreen40 = this;
                    raceDesignScreen40.RaceDescr = string.Concat(raceDesignScreen40.RaceDescr, Plural, Localizer.Token(1341));
                }
            }
            else if (RaceSummary.ResearchMod > 0f)
            {
                RaceDesignScreen raceDesignScreen41 = this;
                raceDesignScreen41.RaceDescr = string.Concat(raceDesignScreen41.RaceDescr, Localizer.Token(1343), Plural, Localizer.Token(1344));
            }
            else if (RaceSummary.ResearchMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen42 = this;
                raceDesignScreen42.RaceDescr = string.Concat(raceDesignScreen42.RaceDescr, Plural, Localizer.Token(1342));
            }
            else
            {
                RaceDesignScreen raceDesignScreen43 = this;
                raceDesignScreen43.RaceDescr = string.Concat(raceDesignScreen43.RaceDescr, Plural, Localizer.Token(1341));
            }
            RaceDesignScreen raceDesignScreen44 = this;
            raceDesignScreen44.RaceDescr = string.Concat(raceDesignScreen44.RaceDescr, "\n \n");
            if (RaceSummary.TaxMod > 0f)
            {
                RaceDesignScreen raceDesignScreen45 = this;
                raceDesignScreen45.RaceDescr = string.Concat(raceDesignScreen45.RaceDescr, Singular, Localizer.Token(1345));
                if (RaceSummary.MaintMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen46 = this;
                    raceDesignScreen46.RaceDescr = string.Concat(raceDesignScreen46.RaceDescr, Localizer.Token(1346), Plural, ". ");
                }
                else if (RaceSummary.MaintMod <= 0f)
                {
                    RaceDescr += Localizer.Token(1348);
                }
                else
                {
                    RaceDescr += Localizer.Token(1347);
                }
            }
            else if (RaceSummary.TaxMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen49 = this;
                raceDesignScreen49.RaceDescr = string.Concat(raceDesignScreen49.RaceDescr, Singular, Localizer.Token(1354));
                if (RaceSummary.MaintMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen50 = this;
                    raceDesignScreen50.RaceDescr = string.Concat(raceDesignScreen50.RaceDescr, Localizer.Token(1355), Singular, Localizer.Token(1356));
                }
                else if (RaceSummary.MaintMod > 0f)
                {
                    RaceDesignScreen raceDesignScreen51 = this;
                    raceDesignScreen51.RaceDescr = string.Concat(raceDesignScreen51.RaceDescr, Plural, Localizer.Token(1357));
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen52 = this;
                raceDesignScreen52.RaceDescr = string.Concat(raceDesignScreen52.RaceDescr, Localizer.Token(1349), Singular, Localizer.Token(1350));
                if (RaceSummary.MaintMod < 0f)
                {
                    RaceDescr += Localizer.Token(1351);
                }
                else if (RaceSummary.MaintMod <= 0f)
                {
                    RaceDesignScreen raceDesignScreen54 = this;
                    raceDesignScreen54.RaceDescr = string.Concat(raceDesignScreen54.RaceDescr, ", ", Plural, Localizer.Token(1353));
                }
                else
                {
                    RaceDescr += Localizer.Token(1352);
                }
            }
            if (RaceSummary.ProductionMod > 0f)
            {
                RaceDesignScreen raceDesignScreen56 = this;
                raceDesignScreen56.RaceDescr = string.Concat(raceDesignScreen56.RaceDescr, Singular, Localizer.Token(1358));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    RaceDescr += Localizer.Token(1359);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    RaceDescr += Localizer.Token(1361);
                }
                else
                {
                    RaceDescr += Localizer.Token(1360);
                }
            }
            else if (RaceSummary.ProductionMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen60 = this;
                raceDesignScreen60.RaceDescr = string.Concat(raceDesignScreen60.RaceDescr, Plural, Localizer.Token(1366));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    RaceDescr += Localizer.Token(1367);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    RaceDescr += Localizer.Token(1369);
                }
                else
                {
                    RaceDescr += Localizer.Token(1368);
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen64 = this;
                raceDesignScreen64.RaceDescr = string.Concat(raceDesignScreen64.RaceDescr, Plural, Localizer.Token(1362));
                if (RaceSummary.ModHpModifier > 0f)
                {
                    RaceDescr += Localizer.Token(1363);
                }
                else if (RaceSummary.ModHpModifier >= 0f)
                {
                    RaceDescr += Localizer.Token(1365);
                }
                else
                {
                    RaceDescr += Localizer.Token(1364);
                }
            }
            if (RaceSummary.SpyMultiplier > 0f)
            {
                RaceDesignScreen raceDesignScreen68 = this;
                raceDesignScreen68.RaceDescr = string.Concat(raceDesignScreen68.RaceDescr, Plural, Localizer.Token(1381));
            }
            else if (RaceSummary.SpyMultiplier < 0f)
            {
                RaceDesignScreen raceDesignScreen69 = this;
                raceDesignScreen69.RaceDescr = string.Concat(raceDesignScreen69.RaceDescr, Plural, Localizer.Token(1382));
            }
            if (RaceSummary.Spiritual > 0f)
            {
                RaceDescr += Localizer.Token(1383);
            }
            RaceDesignScreen raceDesignScreen71 = this;
            raceDesignScreen71.RaceDescr = string.Concat(raceDesignScreen71.RaceDescr, "\n \n");
            if (RaceSummary.HomeworldSizeMod > 0f)
            {
                RaceDesignScreen raceDesignScreen72 = this;
                string str1 = raceDesignScreen72.RaceDescr;
                string[] strArrays = { str1, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1372) };
                raceDesignScreen72.RaceDescr = string.Concat(strArrays);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    RaceDescr += Localizer.Token(1373);
                }
            }
            else if (RaceSummary.HomeworldSizeMod >= 0f)
            {
                RaceDesignScreen raceDesignScreen74 = this;
                string str2 = raceDesignScreen74.RaceDescr;
                string[] strArrays1 = { str2, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1375) };
                raceDesignScreen74.RaceDescr = string.Concat(strArrays1);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    RaceDescr += Localizer.Token(1373);
                }
            }
            else
            {
                RaceDesignScreen raceDesignScreen76 = this;
                string str3 = raceDesignScreen76.RaceDescr;
                string[] strArrays2 = { str3, Localizer.Token(1370), Singular, Localizer.Token(1371), HomeWorldName, Localizer.Token(1374) };
                raceDesignScreen76.RaceDescr = string.Concat(strArrays2);
                if (RaceSummary.HomeworldFertMod < 0f)
                {
                    RaceDescr += Localizer.Token(1373);
                }
            }
            if (RaceSummary.BonusExplored > 0)
            {
                RaceDescr += Localizer.Token(1376);
            }
            if (RaceSummary.Militaristic > 0)
            {
                RaceDescr += Localizer.Token(1377);
                if (RaceSummary.ShipCostMod < 0f)
                {
                    RaceDesignScreen raceDesignScreen80 = this;
                    raceDesignScreen80.RaceDescr = string.Concat(raceDesignScreen80.RaceDescr, Localizer.Token(1378), Singular, Localizer.Token(1379));
                }
            }
            else if (RaceSummary.ShipCostMod < 0f)
            {
                RaceDesignScreen raceDesignScreen81 = this;
                raceDesignScreen81.RaceDescr = string.Concat(raceDesignScreen81.RaceDescr, Plural, Localizer.Token(1380));
            }
            DescriptionSL.Reset();
            HelperFunctions.parseTextToSL(RaceDescr, Description.Menu.Width - 50, Fonts.Arial12, ref DescriptionSL);
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
                r.X -= (int)(transitionOffset * 256f);
            }
            ChooseRaceMenu.Update(r);
            ChooseRaceMenu.subMenu = null;
            ChooseRaceMenu.Draw();
            RaceArchetypeSL.TransitionUpdate(r);
            RaceArchetypeSL.Draw(ScreenManager.SpriteBatch);
            r = dslrect;
            if (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.TransitionOff)
            {
                r.X += (int)(transitionOffset * 256f);
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
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, GalaxySize.ToString(), new Vector2(GalaxySizeRect.X + 190 - Fonts.Arial12.MeasureString(GalaxySize.ToString()).X, GalaxySizeRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(25), " : "), new Vector2(NumberStarsRect.X, NumberStarsRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, StarEnum.ToString(), new Vector2(NumberStarsRect.X + 190 - Fonts.Arial12.MeasureString(StarEnum.ToString()).X, NumberStarsRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2102), " : "), new Vector2(NumOpponentsRect.X, NumOpponentsRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, numOpponents.ToString(), new Vector2(NumOpponentsRect.X + 190 - Fonts.Arial12.MeasureString(numOpponents.ToString()).X, NumOpponentsRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2105), " : "), new Vector2(GameModeRect.X, GameModeRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2133), " : "), new Vector2(PacingRect.X, PacingRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Pacing.ToString(), "%"), new Vector2(PacingRect.X + 190 - Fonts.Arial12.MeasureString(string.Concat(Pacing.ToString(), "%")).X, PacingRect.Y), Color.BurlyWood);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(2139), " : "), new Vector2(DifficultyRect.X, DifficultyRect.Y), Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, SelectedDifficulty.ToString(), new Vector2(DifficultyRect.X + 190 - Fonts.Arial12.MeasureString(SelectedDifficulty.ToString()).X, DifficultyRect.Y), Color.BurlyWood);

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
                if (Mode == GameMode.Sandbox)
            {
                txt = Localizer.Token(2103);
                tip = 112;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (Mode == GameMode.Elimination)
            {
                txt = Localizer.Token(6093);
                tip = 165;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
                if (GameModeRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(tip);
                }
            }
            else if (Mode == GameMode.Corners)    //Added by Gretman
            {
                txt = Localizer.Token(4102);
                tip = 229;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, new Vector2(GameModeRect.X + 190 - Fonts.Arial12.MeasureString(txt).X, GameModeRect.Y), Color.BurlyWood);
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

        void OnEngageClicked(UIButton b)
        {
            OnEngage();
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
            DescriptionSL.HandleInput(input);
            if (!DrawingColorSelector)
            {
                selector = null;
                foreach (ScrollList.Entry e in RaceArchetypeSL.AllEntries)
                {
                    if (e.WasClicked(input))
                    {
                        SelectedData = e.item as EmpireData;
                        GameAudio.EchoAffirmative();
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
                }
                if (GalaxySizeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    RaceDesignScreen galaxysize = this;
                    galaxysize.GalaxySize = (GalSize)((int)galaxysize.GalaxySize + (int)GalSize.Small);
                    if (GalaxySize > GalSize.TrulyEpic)   //Resurrecting TrulyEpic Map UniverseRadius -Gretman
                    {
                        GalaxySize = GalSize.Tiny;
                    }
                }
                if (GameModeRect.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    GameAudio.BlipClick();
                    //RaceDesignScreen gamemode = this;
                    Mode = Mode + 1;
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
                    int maxOpponents = Mode == GameMode.Corners ? 3 
                        : GlobalStats.ActiveMod?.mi?.MaxOpponents ?? 7;
                    numOpponents = numOpponents + 1;
                    
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
                        if (ExtraRemnant < 0)
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
            t.ShipType = SelectedData.Traits.ShipType;
            t.VideoPath = SelectedData.Traits.VideoPath;
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

            foreach (IEmpireData e in ResourceManager.Empires)
            {
                if (!e.IsFactionOrMinorRace)
                {
                    RaceArchetypeSL.AddItem(e);
                    if (e.Traits.VideoPath.NotEmpty())
                        TextureDict.Add(e, ResourceManager.Texture("Races/" + e.Traits.VideoPath));
                }
            }
            foreach (IEmpireData e in ResourceManager.Empires)
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

            ButtonMedium(smaller.X + (smaller.Width / 2) - 142, smaller.Y - 20, "Load Race", OnLoadRaceClicked);
            ButtonMedium(smaller.X + (smaller.Width / 2) + 10, smaller.Y - 20, "Save Race", OnSaveRaceClicked);

            var pos = new Vector2(ScreenWidth / 2 - 84, leftRect.Y + leftRect.Height + 10);

            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, titleId: 4006, click: OnRuleOptionsClicked);

            base.LoadContent();
        }

        protected virtual void OnEngage()
        {
            if (Mode == GameMode.Elimination) GlobalStats.EliminationMode = true;
            if (Mode == GameMode.Corners) GlobalStats.CornersGame = true;

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
            var player = new Empire
            {
                EmpireColor = currentObjectColor,
                data = SelectedData.CreateInstance()
            };
            player.data.SpyModifier = RaceSummary.SpyMultiplier;
            player.data.Traits.Spiritual = RaceSummary.Spiritual;
            RaceSummary.Adj1 = SelectedData.Traits.Adj1;
            RaceSummary.Adj2 = SelectedData.Traits.Adj2;
            player.data.Traits = RaceSummary;
            player.EmpireColor = currentObjectColor;

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
            SelectedDifficulty        = gameDifficulty;
            this.StarEnum     = StarEnum;
            this.GalaxySize   = Galaxysize;
            this.Pacing       = Pacing;
            this.ExtraRemnant = ExtraRemnant;
            this.numOpponents = numOpponents;
            this.Mode         = mode;
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

        void SetEmpireData(RacialTrait traits)
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

        void SetExclusions(TraitEntry t)
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

        void UpdateSummary()
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
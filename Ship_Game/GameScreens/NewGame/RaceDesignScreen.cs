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
        protected Menu1 NameMenu;
        protected Menu1 Description;

        protected bool LowRes;

        protected Submenu Traits;
        protected Submenu NameSub;

        protected ScrollList<TraitsListItem> traitsSL;
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
        ScrollList<RaceArchetypeListItem> RaceArchetypeSL;
        Submenu arch;
        Rectangle PacingRect;

        int Pacing = 100;

        Rectangle ScaleRect = new Rectangle();
        Rectangle dslrect;
        Rectangle GameModeRect;
        Rectangle DifficultyRect;

        Map<IEmpireData, SubTexture> TextureDict = new Map<IEmpireData, SubTexture>();

        ScrollList<TextListItem> DescriptionSL;
        protected UIButton Engage;
        protected UIButton Abort;
        protected UIButton ClearTraits;

        int numOpponents;
        protected RacialTrait tipped;
        protected float tTimer = 0.35f;

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


        class RaceTextBuilder
        {
            readonly string PluralText, SingularText;
            readonly StringBuilder Sb = new StringBuilder();
            public override string ToString() => Sb.ToString();
            public RaceTextBuilder(string plural, string singular)
            {
                PluralText = plural;
                SingularText = singular;
            }
            public RaceTextBuilder Add(int token)      { Sb.Append(Localizer.Token(token));  return this; }
            public RaceTextBuilder Add(string text)    { Sb.Append(text);                    return this; }
            public RaceTextBuilder Plural(int token)   { Sb.Append(PluralText).Append(Localizer.Token(token));   return this; }
            public RaceTextBuilder Plural(string text) { Sb.Append(PluralText).Append(text);                     return this; }
            public RaceTextBuilder Singular(int token) { Sb.Append(SingularText).Append(Localizer.Token(token)); return this; }
        }


        protected void DoRaceDescription()
        {
            CreateRaceSummary();
            RacialTrait race = RaceSummary;

            var b = new RaceTextBuilder(Plural, Singular);

            b.Add(RaceName.Text).Add(1300).Plural(". ");
            b.Plural(race.IsOrganic ? 1302 : 1301);
            b.Add(race.Aquatic <= 0 ? 1304 : 1303);
          
            var growth = race.IsOrganic ? (Hi:1305, Avg:1307, Lo:1306) : (Hi:1308, Avg:1310, Lo:1309);
            if      (race.PopGrowthMin > 0f) b.Add(growth.Hi);
            else if (race.PopGrowthMin < 0f || race.PopGrowthMax < 0f) b.Add(growth.Lo);
            else b.Add(growth.Avg);

            var consumption = race.IsOrganic ? (Hi:1311, Avg:1313, Lo:1312) : (Hi:1314, Avg:1316, Lo:1315);
            if      (race.ConsumptionModifier > 0f) b.Add(consumption.Hi);
            else if (race.ConsumptionModifier < 0f) b.Add(consumption.Lo);
            else                                    b.Add(consumption.Avg);


            var combat = race.IsOrganic ? (Majestic:(1317,1324),  // Physically, HUMANS possess a non-threatening countenance..... However, they are exceedingly charismatic and naturally draw others towards them
                                           Grotesque:(1317,1325), // Physically, HUMANS possess a non-threatening countenance..... However, they are offensively mannered with many odiferous
                                           Tolerable:(1317,1326)) // Physically, HUMANS have no particular strengths or weaknesses
                                        : (Majestic:(1317,1334),  // Physically, HUMANS seem to have made great efforts to mechanically improve upon the grace and beauty of their bodies
                                           Grotesque:(1327,1335), // In rejecting their organic forms,HUMANS have developed a cold disdain for organic life forms and are particularly uncharismatic
                                           Tolerable:(1336,1337));// Although they have replaced their organic bodies with cybernetics, HUMANS have not extensively improved on their original organic structure
            
            if (race.GroundCombatModifier > 0f)
                combat = race.IsOrganic ? (Majestic:(1317,1318),  // Physically, HUMANS are both beautiful and terrifying, with an enigmatic sort of lethal majesty
                                           Grotesque:(1317,1319), // Physically, HUMANS appear both lethal and grotesque, as they tend to project a sense of instinctive
                                           Tolerable:(1317,1320)) // Physically, HUMANS appear somewhat bestial, with obvious signs of their great strength
                                        : (Majestic:(1327,1328),  // In rejecting their organic forms, HUMANS have become incredible machines of grace and lethality, with a sleek, deadly, and beautiful appearance
                                           Grotesque:(1327,1329), // In rejecting their organic forms, HUMANS have become confusing technological monstrosities with deadly-looking appendages and a decidedly hostile appearance
                                           Tolerable:(1327,1330));// In rejecting their organic forms, HUMANS look like machines designed for war without any particular focus put on appearance
            
            if (race.GroundCombatModifier < 0f)
                combat = race.IsOrganic ? (Majestic:(1317,1321),  // Physically, HUMANS are creatures of fragile beauty, with a delicate and ephemeral stature
                                           Grotesque:(1317,1322), // Physically, HUMANS appear gnarled and weak, with a countenance both hideous and scrawny
                                           Tolerable:(1317,1323)) // Physically, HUMANS seem to have evolved without much need for speed or strength and as a result are somewhat frail
                                        : (Majestic:(1317,1331),  // Physically, HUMANS come off as friendly little robots that could only be described as cute, but fragile
                                           Grotesque:(1317,1332), // Physically, HUMANS are spindly, insect-like machines that click and hiss in menacing ways. They look like they could be crunched under a boot
                                           Tolerable:(1317,1333));// Physically, HUMANS look like computers with wheels and treads attached, and do not seem armed or armored in any way

            if      (race.DiplomacyMod > 0f)  b.Add(combat.Majestic.Item1).Plural(combat.Majestic.Item2);
            else if (race.DiplomacyMod < 0f)  b.Add(combat.Grotesque.Item1).Plural(combat.Grotesque.Item2);
            else                              b.Add(combat.Tolerable.Item1).Plural(combat.Tolerable.Item2);


            if (race.GroundCombatModifier < 0f || race.DiplomacyMod <= 0f) // if bad stats before, consider these prefixes:
            {
                if (race.ResearchMod > 0f) b.Add(1338); // "However, "
                if (race.ResearchMod < 0f) b.Add(1340); // "To make matters worse, "
            }
            else if (race.GroundCombatModifier > 0f || race.DiplomacyMod > 0f) // if good stats before, consider these prefixes:
            {
                if (race.ResearchMod > 0f) b.Add(1343); // "Furthermore, "
                if (race.ResearchMod < 0f) b.Add(1338); // "However, "
            }
            if      (race.ResearchMod > 0f) b.Plural(1344); // | HUMANS | are extremely intelligent for a starfaring race and have a natural curiosity about the universe
            else if (race.ResearchMod < 0f) b.Plural(1341); // | HUMANS | are somewhat dumb for a starfaring race and have difficulty being creative
            else                            b.Plural(1342); // | HUMANS | possess an average intelligence for a starfaring race

            b.Add("\n \n");
            if (race.TaxMod > 0f)
            {
                b.Singular(1345); // | HUMAN | government is efficient and effective
                if      (race.MaintMod < 0f) b.Add(1346).Plural(". "); // , which reflects the methodical and practical nature of | HUMANS |.
                else if (race.MaintMod > 0f) b.Add(1347); // but this is counterbalanced by a wasteful, consumer-oriented society
                else                         b.Add(1348); // and its citizens are relatively intelligent about resource allocation
            }
            else if (race.TaxMod < 0f)
            {
                b.Add(1349).Singular(1350); // While | HUMAN | government can be somewhat corrupt
                if      (race.MaintMod < 0f) b.Add(1351); // , its private sector makes up for the government's wastefulness with its efficiency. 
                else if (race.MaintMod > 0f) b.Add(1352); // , this corruption pales in comparison to recklessness of such a wasteful, consumer-oriented society.
                else                         b.Add(", ").Plural(1353); // , | HUMANS | are nevertheless a practical race when it comes to resource allocation.
            }
            else
            {
                b.Singular(1354); // | HUMAN | government is functional and stable
                if      (race.MaintMod < 0f) b.Add(1355).Singular(1356); // Even so, | HUMAN | society at large is very environmentally concious and practical about resource allocation.
                else if (race.MaintMod > 0f) b.Plural(1357); // | HUMANS | also tend to be a wasteful people who are neglectful of their investments
            }

            if (race.ProductionMod > 0f)
            {
                b.Singular(1358); // | HUMAN | work ethic is strong
                if      (race.ModHpModifier > 0f) b.Add(1359); // and their engineers and shipwrights produce components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(1360); // yet their engineering skills are poor, producing goods and ships of somewhat inferior quality
                else                              b.Add(1361); // and their engineers and shipwrights are generally competent
            }
            else if (race.ProductionMod >= 0f)
            {
                b.Plural(1366); // | HUMANS | are neither particularly industrious nor particularly lazy
                if      (race.ModHpModifier > 0f) b.Add(1367); // but their engineers and shipwrights are highly skilled and produce components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(1368); // . Overall, their engineering skills are somwhat poor compared to peer races and tend to produce ships of inferior quality. 
                else                              b.Add(1369); // and their engineers and shipwrights are generally competent
            }
            else
            {
                b.Singular(1362); // | HUMAN | workers are generally quite lazy
                if      (race.ModHpModifier > 0f) b.Add(1363); // yet their engineers and shipwrights are highly skilled, producing components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(1364); // and their engineering skills are poor, producing goods and ships of somewhat inferior quality
                else                              b.Add(1365); // but their engineers and shipwrights are generally competent
            }

            if      (race.SpyMultiplier > 0f) b.Plural(1381); // | HUMANS | possess a somewhat duplicitous nature and as a result make excellent spies
            else if (race.SpyMultiplier < 0f) b.Plural(1382); // | HUMANS | live very open and honest lives, and as a result have a poor understanding of the concept of espionage

            if (race.Spiritual > 0f) b.Add(1383);

            b.Add("\n \n").Add(1370).Singular(1371).Add(HomeWorldName); // The | HUMAN | homeworld of | EARTH |
            if      (race.HomeworldSizeMod > 0f) b.Add(1372); // is very large for a terran planet
            else if (race.HomeworldSizeMod < 0f) b.Add(1374); // is very small for a terran planet
            else                                 b.Add(1375); // is of an average size for a terran planet

            if (race.HomeworldFertMod < 0f) b.Add(1373); // The planet is extremely polluted from centuries of environmental abuses
            if (race.BonusExplored > 0) b.Add(1376); // Historically, this race has looked to the stars for many centuries

            if (race.Militaristic > 0)
            {
                b.Add(1377); // A strong history of military conflict has made the military a central part of this race's culture.
                if (race.ShipCostMod < 0f) b.Add(1378).Singular(1379); // Furthermore, | HUMAN | naval tradition is very strong,
            }
            else if (race.ShipCostMod < 0f)
            {
                b.Plural(1380); // | HUMANS | are natural sailors and shipwrights
            }

            b.Plural(PreferredEnvDescription);

            DescriptionSL.ResetWithParseText(Fonts.Arial12, b.ToString(), Description.Menu.Width - 50);
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
                        batch.Draw(TextureDict[data], portrait, Color.White);
                        if (SelectedData == data)
                        {
                            batch.DrawRectangle(portrait, Color.BurlyWood);
                        }
                    }
                    else
                    {
                        var portrait = new Rectangle(e.CenterX - 176, (int)raceCursor.Y, 352, 128);
                        batch.Draw(TextureDict[data], portrait, Color.White);
                        if (SelectedData == data)
                        {
                            batch.DrawRectangle(portrait, Color.BurlyWood);
                        }
                    }
                }
            }

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
            foreach (ScrollList.Entry e in DescriptionSL.VisibleEntries)
            {
                if (!e.Hovered)
                {
                    batch.DrawString(Fonts.Arial12, e.item as string, drawCurs, Color.White);
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
                var bCursor = new Vector2(Traits.X + 20, Traits.Y + 45);
                foreach (ScrollList.Entry e in traitsSL.VisibleEntries)
                {
                    string name = PaddedWithDots((e.item as TraitEntry).trait.TraitName, Traits.Rect.Width - 70);

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
                        batch.DrawString(Fonts.Arial14Bold, name, tCursor, drawColor);
                        Vector2 curs = bCursor;
                        curs.X = curs.X + (Traits.Width - 45 - Fonts.Arial14Bold.MeasureString((e.item as TraitEntry).trait.Cost.ToString()).X);
                        batch.DrawString(Fonts.Arial14Bold, (e.item as TraitEntry).trait.Cost.ToString(), curs, drawColor);
                        tCursor.Y = tCursor.Y + Fonts.Arial14Bold.LineSpacing;
                        batch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(Localizer.Token((e.item as TraitEntry).trait.Description), Traits.Width - 45), tCursor, drawColor);
                        
                        e.DrawPlus(batch);
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
                        batch.DrawString(Fonts.Arial14Bold, name, tCursor, drawColor);
                        Vector2 curs = bCursor;
                        curs.X = curs.X + (Traits.Width - 45 - Fonts.Arial14Bold.MeasureString((e.item as TraitEntry).trait.Cost.ToString()).X);
                        batch.DrawString(Fonts.Arial14Bold, (e.item as TraitEntry).trait.Cost.ToString(), curs, drawColor);
                        tCursor.Y = tCursor.Y + Fonts.Arial14Bold.LineSpacing;
                        batch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(Localizer.Token((e.item as TraitEntry).trait.Description), Traits.Width - 45), tCursor, drawColor);
                        e.DrawPlus(batch);
                    }

                    e.CheckHover(Input.CursorPosition);
                }
            }
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
                        SetRacialTraits(SelectedData.Traits);
                    }
                }
                RaceArchetypeSL.HandleInput(input);
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
            RaceArchetypeSL = new ScrollList(arch, 135);

            foreach (IEmpireData e in ResourceManager.MajorRaces)
            {
                RaceArchetypeSL.AddItem(e);
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
            SetRacialTraits(SelectedData.Traits);

            ButtonMedium(smaller.X + (smaller.Width / 2) - 142, smaller.Y - 20, "Load Race", OnLoadRaceClicked);
            ButtonMedium(smaller.X + (smaller.Width / 2) + 10, smaller.Y - 20, "Save Race", OnSaveRaceClicked);

            var pos = new Vector2(ScreenWidth / 2 - 84, leftRect.Y + leftRect.Height + 10);

            ButtonMedium(pos.X - 142, pos.Y, "Load Setup", OnLoadSetupClicked);
            ButtonMedium(pos.X + 178, pos.Y, "Save Setup", OnSaveSetupClicked);
            Button(pos.X, pos.Y, titleId: 4006, click: OnRuleOptionsClicked);

            base.LoadContent();
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

                    if (traits.R == origRace.Traits.R && 
                        traits.G == origRace.Traits.G &&
                        traits.B == origRace.Traits.B)
                    {
                        traits.R = currentObjectColor.R;
                        traits.G = currentObjectColor.G;
                        traits.B = currentObjectColor.B;
                    }
                    break;
                }
            }
            SetRacialTraits(traits);
        }

        void SetRacialTraits(RacialTrait traits)
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
            PreferredEnvDescription = traits.PreferredEnvDescription;
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

            CreateRaceSummary();
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        void CreateRaceSummary()
        {
            Singular = SingEntry.Text;
            Plural = PlurEntry.Text;
            HomeSystemName = HomeSystemEntry.Text;
            RaceSummary = new RacialTrait();

            TraitEntry entry = AllTraits.Find(t => t.Selected);
            if (entry == null)
            {
                Log.Warning("No traits selected");
                return;
            }
            
            foreach (TraitEntry t in AllTraits)
            {
                if (!t.Selected) continue;
                RacialTrait trait = t.trait;
                RaceSummary.ConsumptionModifier    += trait.ConsumptionModifier;
                RaceSummary.DiplomacyMod           += trait.DiplomacyMod;
                RaceSummary.EnergyDamageMod        += trait.EnergyDamageMod;
                RaceSummary.MaintMod               += trait.MaintMod;
                RaceSummary.ReproductionMod        += trait.ReproductionMod;
                RaceSummary.PopGrowthMax           += trait.PopGrowthMax;
                RaceSummary.PopGrowthMin           += trait.PopGrowthMin;
                RaceSummary.ResearchMod            += trait.ResearchMod;
                RaceSummary.ShipCostMod            += trait.ShipCostMod;
                RaceSummary.TaxMod                 += trait.TaxMod;
                RaceSummary.ProductionMod          += trait.ProductionMod;
                RaceSummary.ModHpModifier          += trait.ModHpModifier;
                RaceSummary.Mercantile             += trait.Mercantile;
                RaceSummary.GroundCombatModifier   += trait.GroundCombatModifier;
                RaceSummary.Cybernetic             += trait.Cybernetic;
                RaceSummary.Blind                  += trait.Blind;
                RaceSummary.DodgeMod               += trait.DodgeMod;
                RaceSummary.HomeworldFertMod       += trait.HomeworldFertMod;
                RaceSummary.HomeworldRichMod       += trait.HomeworldRichMod;
                RaceSummary.HomeworldSizeMod       += trait.HomeworldSizeMod;
                RaceSummary.Militaristic           += trait.Militaristic;
                RaceSummary.BonusExplored          += trait.BonusExplored;
                RaceSummary.Prototype              += trait.Prototype;
                RaceSummary.Spiritual              += trait.Spiritual;
                RaceSummary.SpyMultiplier          += trait.SpyMultiplier;
                RaceSummary.RepairMod              += trait.RepairMod;
                RaceSummary.PassengerModifier      += trait.PassengerBonus;
                if (trait.Pack)
                    RaceSummary.Pack = trait.Pack;
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
    }

    public enum ExtraRemnantPresence
    {
        Rare, Normal, More, MuchMore, Everywhere
    }
}
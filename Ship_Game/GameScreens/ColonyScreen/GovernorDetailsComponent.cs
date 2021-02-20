using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    class GovernorDetailsComponent : UIElementContainer
    {
        private readonly GameScreen Screen;
        private readonly SubTexture PortraitShine = ResourceManager.Texture("Portraits/portrait_shine");
        private Planet Planet;
        private DrawableSprite PortraitSprite;
        private UIPanel Portrait;
        private UILabel WorldType, WorldDescription;
        private DropOptions<Planet.ColonyType> ColonyTypeList;
        private UICheckBox GovOrbitals, AutoTroops, GovNoScrap, Quarantine, ManualOrbitals, GovGround;
        private UICheckBox OverrideCiv, OverrideGrd, OverrideSpc;
        private FloatSlider Garrison;
        private FloatSlider ManualPlatforms;
        private FloatSlider ManualShipyards;
        private FloatSlider ManualStations;
        private readonly Submenu Tabs;

        UIButton LaunchAllTroops;
        UIButton LaunchSingleTroop;
        UIButton CallTroops;
        UIButton BuildPlatform;
        UIButton BuildStation;
        UIButton BuildShipyard;
        private float ButtonUpdateTimer;   // updates buttons once per second
        UILabel PlatformsText;
        UILabel StationsText;
        UILabel ShipyardsText;
        UILabel NoGovernor;
        UILabel ColonyRank;
        UILabel BudgetSum;
        UILabel BudgetPercent;
        UILabel NoGovernorCivExpense;
        UILabel NoGovernorGrdExpense;
        UILabel NoGovernorSpcExpense;


        private readonly SpriteFont Font14 = Fonts.Arial14Bold;
        private readonly SpriteFont Font12 = Fonts.Arial12Bold;
        private readonly SpriteFont Font10 = Fonts.Arial10;
        private readonly SpriteFont Font8  = Fonts.Arial8Bold;
        private SpriteFont Font;
        private SpriteFont FontBig;
        private bool OverrideCivBudget, OverrideGrdBudget, OverrideSpcBudget;

        Rectangle CivBudgetRect;
        Rectangle GrdBudgetRect;
        Rectangle SpcBudgetRect;
        Rectangle CivBudgetTexRect;
        Rectangle GrdBudgetTexRect;
        Rectangle SpcBudgetTexRect;
        ProgressBar CivBudgetBar;
        ProgressBar GrdBudgetBar;
        ProgressBar SpcBudgetBar;
        UITextEntry ManualCivBudget;
        UITextEntry ManualGrdBudget;
        UITextEntry ManualSpcBudget;

        bool GovernorOn      => Planet.GovernorOn;
        bool GovernorOff     => Planet.GovernorOff;
        bool GovernorTabView => Tabs.SelectedIndex == 0;
        bool DefenseTabView  => Tabs.SelectedIndex == 1;
        bool BudgetTabView   => Tabs.SelectedIndex == 2;
        

        public GovernorDetailsComponent(GameScreen screen, Planet p, in Rectangle rect) : base(rect)
        {
            Screen = screen;
            SetPlanetDetails(p);
            Tabs = Add(new Submenu(rect));
            Tabs.AddTab(new LocalizedText(4209).Text); // Governor
            Tabs.AddTab(new LocalizedText(4210).Text); // Defense
            Tabs.AddTab(new LocalizedText(4225).Text); // Budget
        }

        public void SetPlanetDetails(Planet p)
        {
            Log.Assert(p != null, "GovernorDetailsComponent Planet cannot be null");
            if (Planet == p || p == null)
                return;

            Planet = p;
            RemoveAll(); // delete all components

            // NOTE: Using RootContent here to avoid lag from resource unloading and reloading
            PortraitSprite = DrawableSprite.SubTex(ResourceManager.RootContent, $"Portraits/{Planet.Owner.data.PortraitName}");

            Portrait         = Add(new UIPanel(PortraitSprite));
            WorldType        = Add(new UILabel(Planet.WorldType, Fonts.Arial12Bold));
            WorldDescription = Add(new UILabel(Fonts.Arial12Bold));

            Font    = Font12;
            FontBig = Font14;
            if      (Screen.Width < 1600) {Font = Font8;  FontBig = Font10; }
            else if (Screen.Width < 1920) {Font = Font10; FontBig = Font12; }

            GovOrbitals    = Add(new UICheckBox(() => Planet.GovOrbitals, Font, title:1960, tooltip:1961));
            AutoTroops     = Add(new UICheckBox(() => Planet.AutoBuildTroops, Font, title:1956, tooltip:1957));
            GovNoScrap     = Add(new UICheckBox(() => Planet.DontScrapBuildings, Font, title:1941, tooltip:1942));
            Quarantine     = Add(new UICheckBox(() => Planet.Quarantine, Font, title: 1888, tooltip: 1887));
            ManualOrbitals = Add(new UICheckBox(() => Planet.ManualOrbitals, Font, title: 4201, tooltip: 4202));
            GovGround      = Add(new UICheckBox(() => Planet.GovGroundDefense, Font, title: 4207, tooltip: 4208));
            OverrideCiv    = Add(new UICheckBox(() => OverrideCivBudget, Font, title: 4226, tooltip: 4227));
            OverrideGrd    = Add(new UICheckBox(() => OverrideGrdBudget, Font, title: 4226, tooltip: 4227));
            OverrideSpc    = Add(new UICheckBox(() => OverrideSpcBudget, Font, title: 4226, tooltip: 4227));

            Garrison        = Slider(200, 200, 160, 40, new LocalizedText(4211).Text, 0, 25,Planet.GarrisonSize);
            ManualPlatforms = Slider(200, 200, 120, 40, new LocalizedText(4212).Text, 0, 15, Planet.WantedPlatforms);
            ManualShipyards = Slider(200, 200, 120, 40, "", 0, 2, Planet.WantedShipyards);
            ManualStations  = Slider(200, 200, 120, 40, "", 0, 10, Planet.WantedStations);

            Garrison.Tip        = 1903;
            ManualPlatforms.Tip = 4204;
            ManualShipyards.Tip = 4205;
            ManualStations.Tip  = 4206;

            // Dropdown will go on top of everything else
            ColonyTypeList = Add(new DropOptions<Planet.ColonyType>(100, 18));
            ColonyTypeList.AddOption(option:"--", Planet.ColonyType.Colony);
            ColonyTypeList.AddOption(option:4064, Planet.ColonyType.Core);
            ColonyTypeList.AddOption(option:4065, Planet.ColonyType.Industrial);
            ColonyTypeList.AddOption(option:4066, Planet.ColonyType.Agricultural);
            ColonyTypeList.AddOption(option:4067, Planet.ColonyType.Research);
            ColonyTypeList.AddOption(option:4068, Planet.ColonyType.Military);
            ColonyTypeList.AddOption(option:5087, Planet.ColonyType.TradeHub);
            ColonyTypeList.ActiveValue = Planet.colonyType;
            ColonyTypeList.OnValueChange = OnColonyTypeChanged;

            ButtonUpdateTimer = 1;
            LaunchAllTroops   = Button(ButtonStyle.Default, new LocalizedText(4213).Text, OnLaunchTroopsClicked);
            LaunchSingleTroop = Button(ButtonStyle.Default, new LocalizedText(4214).Text, OnLaunchSingleTroopClicked);
            CallTroops        = Button(ButtonStyle.Default, new LocalizedText(4215).Text, OnSendTroopsClicked);

            LaunchAllTroops.Tooltip   = new LocalizedText(1952).Text;
            LaunchSingleTroop.Tooltip = new LocalizedText(1950).Text;
            CallTroops.Tooltip        = new LocalizedText(1949).Text;

            BuildShipyard = Button(ButtonStyle.Medium, new LocalizedText(4216).Text, OnBuildShipyardClick);
            BuildStation  = Button(ButtonStyle.Medium, new LocalizedText(4217).Text, OnBuildStationClick);
            BuildPlatform = Button(ButtonStyle.Medium, new LocalizedText(4218).Text, OnBuildPlatformClick);

            BuildShipyard.Tooltip = new LocalizedText(1948).Text;
            BuildStation.Tooltip  = new LocalizedText(1947).Text;
            BuildPlatform.Tooltip = new LocalizedText(1946).Text;

            PlatformsText    = Add(new UILabel(" "));
            ShipyardsText    = Add(new UILabel(" "));
            StationsText     = Add(new UILabel(" "));
            NoGovernor       = Add(new UILabel(new LocalizedText(4219).Text, Font, Color.Gray));
            ColonyRank       = Add(new UILabel(" ", Font, Color.LightGreen));

            CivBudgetRect    = new Rectangle((int)X + 57, (int)Y + 40, (int)(Width*0.33f), 20);
            GrdBudgetRect    = new Rectangle((int)X + 57, (int)Y + 70, (int)(Width*0.33f), 20);
            SpcBudgetRect    = new Rectangle((int)X + 57, (int)Y + 100, (int)(Width*0.33f), 20);
            CivBudgetTexRect = new Rectangle((int)X + 5, (int)Y + 38, 47, 23);
            GrdBudgetTexRect = new Rectangle((int)X + 5, (int)Y + 68, 47, 23);
            SpcBudgetTexRect = new Rectangle((int)X + 5, (int)Y + 96, 47, 23);

            CivBudgetBar = new ProgressBar(CivBudgetRect);
            GrdBudgetBar = new ProgressBar(GrdBudgetRect);
            SpcBudgetBar = new ProgressBar(SpcBudgetRect);

            CivBudgetBar.Faction10Values = true;
            GrdBudgetBar.Faction10Values = true;
            SpcBudgetBar.Faction10Values = true;
            CivBudgetBar.color           = "green";
            SpcBudgetBar.color           = "blue";

            ManualCivBudget       = Add(new UITextEntry(Planet.ManualCivilianBudget.String(2)));
            ManualGrdBudget       = Add(new UITextEntry(Planet.ManualGrdDefBudget.String(2)));
            ManualSpcBudget       = Add(new UITextEntry(Planet.ManualSpcDefBudget.String(2)));
            ManualCivBudget.Color = Color.MediumSeaGreen;
            ManualSpcBudget.Color = Color.SteelBlue;

            ManualCivBudget.Font          = ManualGrdBudget.Font          = ManualSpcBudget.Font          = Font;
            ManualCivBudget.MaxCharacters = ManualGrdBudget.MaxCharacters = ManualSpcBudget.MaxCharacters = 6;
            ManualCivBudget.AllowPeriod   = ManualGrdBudget.AllowPeriod   = ManualSpcBudget.AllowPeriod   = true;

            BudgetSum     = Add(new UILabel(" ", FontBig, Color.White));
            BudgetPercent = Add(new UILabel(" ", FontBig, Color.White));

            NoGovernorCivExpense = Add(new UILabel(" ", FontBig, Color.MediumSeaGreen));
            NoGovernorGrdExpense = Add(new UILabel(" ", FontBig, Color.DarkOrange));
            NoGovernorSpcExpense = Add(new UILabel(" ", FontBig, Color.SteelBlue));

            base.PerformLayout();
        }

        public override void PerformLayout()
        {
            float aspect  = PortraitSprite.Size.X / PortraitSprite.Size.Y;
            float height  = (float)Math.Round(Height * 0.6f);
            Portrait.Size = new Vector2((float)Math.Round(aspect*height), height);
            Portrait.Pos  = new Vector2(X + 10, Y + 30);

            WorldType.Pos         = new Vector2(Portrait.Right + 10, Portrait.Y);
            ColonyTypeList.Pos    = new Vector2(WorldType.X, Portrait.Y + 16);
            WorldDescription.Pos  = new Vector2(WorldType.X, Portrait.Y + 40);
            WorldDescription.Text = GetParsedDescription();
            Quarantine.Pos        = new Vector2(Portrait.X, Bottom - 24);
            GovNoScrap.Pos        = new Vector2(TopRight.X - 250, Bottom - 24);

            AutoTroops.Pos        = new Vector2(TopLeft.X + 10, Y + 30);
            Garrison.Pos          = new Vector2(TopLeft.X + 20, Y + 50);
            CallTroops.Pos        = new Vector2(TopLeft.X + 10, Bottom - 30);
            LaunchSingleTroop.Pos = new Vector2(TopLeft.X + 10, Bottom - 60);
            LaunchAllTroops.Pos   = new Vector2(TopLeft.X + 10, Bottom - 90);
            ColonyRank.Pos        = new Vector2(TopLeft.X + 200, Y + 30);
            NoGovernor.Pos        = ColonyRank.Pos;
            GovGround.Pos         = new Vector2(TopLeft.X + 200, Y + 50);
            GovOrbitals.Pos       = new Vector2(TopLeft.X + 200, Y + 70);
            ManualOrbitals.Pos    = new Vector2(TopLeft.X + 200, Y + 90);
            BuildPlatform.Pos     = new Vector2(TopLeft.X + 200, Bottom - 90);
            BuildShipyard.Pos     = new Vector2(TopLeft.X + 200, Bottom - 60);
            BuildStation.Pos      = new Vector2(TopLeft.X + 200, Bottom - 30);
            Vector2 manualOffset  = new Vector2(125, -15);
            ManualPlatforms.Pos   = BuildPlatform.Pos + manualOffset;
            ManualShipyards.Pos   = BuildShipyard.Pos + manualOffset;
            ManualStations.Pos    = BuildStation.Pos + manualOffset;

            BudgetSum.Pos         = new Vector2(TopLeft.X + 8, Y + 130);
            BudgetPercent.Pos     = new Vector2(TopLeft.X + CivBudgetRect.Width + 15, Y + 130);
            OverrideCiv.Pos       = new Vector2(CivBudgetRect.X + CivBudgetRect.Width + 10, CivBudgetRect.Y + 2);
            OverrideGrd.Pos       = new Vector2(GrdBudgetRect.X + GrdBudgetRect.Width + 10, GrdBudgetRect.Y + 2);
            OverrideSpc.Pos       = new Vector2(SpcBudgetRect.X + SpcBudgetRect.Width + 10, SpcBudgetRect.Y + 2);
            ManualCivBudget.Pos   = new Vector2(OverrideCiv.X + OverrideCiv.Width + 20, OverrideCiv.Y);
            ManualGrdBudget.Pos   = new Vector2(OverrideGrd.X + OverrideGrd.Width + 20, OverrideGrd.Y);
            ManualSpcBudget.Pos   = new Vector2(OverrideSpc.X + OverrideSpc.Width + 20, OverrideSpc.Y);

            NoGovernorCivExpense.Pos = new Vector2(TopLeft.X + 60, Y + 40);
            NoGovernorGrdExpense.Pos = new Vector2(TopLeft.X + 60, Y + 70);
            NoGovernorSpcExpense.Pos = new Vector2(TopLeft.X + 60, Y + 100);

            OverrideCivBudget = Planet.ManualCivilianBudget.Greater(0);
            OverrideGrdBudget = Planet.ManualGrdDefBudget.Greater(0);
            OverrideSpcBudget = Planet.ManualSpcDefBudget.Greater(0);

            GovOrbitals.OnChange = cb =>
            {
                if (cb.Checked)
                {
                    UpdateOrbitalTextPos();
                    UpdateGovOrbitalStats();
                }
            };

            OverrideCiv.OnChange = cb =>
            {
                var budget = new PlanetBudget(Planet);
                Planet.SetManualCivBudget(cb.Checked ? budget.CivilianAlloc : 0);
            };

            OverrideGrd.OnChange = cb =>
            {
                var budget = new PlanetBudget(Planet);
                Planet.SetManualGroundDefBudget(cb.Checked ? budget.GrdDefAlloc : 0);
            };

            OverrideSpc.OnChange = cb =>
            {
                var budget = new PlanetBudget(Planet);
                Planet.SetManualSpaceDefBudget(cb.Checked ? budget.SpcDefAlloc : 0);
            };

            UpdateButtons();
            UpdateGovOrbitalStats();
            UpdateBudgets();


            base.PerformLayout(); // update all the sub-elements, like checkbox rects
        }

        string GetParsedDescription()
        {
            float maxWidth = (Right - 10 - WorldType.X);
            return Fonts.Arial12Bold.ParseText(Planet.ColonyTypeInfoText.Text, maxWidth);
        }

        void OnColonyTypeChanged(Planet.ColonyType type)
        {
            Planet.colonyType = type;
            WorldType.Text = Planet.WorldType;
            WorldDescription.Text = GetParsedDescription();
        }

        public override void Update(float fixedDeltaTime)
        {
            if (Planet.Owner != null)
            {
                WorldDescription.Visible = GovernorTabView && Planet.Owner.isPlayer;
                ColonyTypeList.Visible   = GovernorTabView && Planet.Owner.isPlayer;
                Portrait.Visible         = GovernorTabView;
                WorldType.Visible        = GovernorTabView;
                Quarantine.Visible       = GovernorTabView && Planet.Owner.isPlayer;
                Quarantine.TextColor     = Planet.Quarantine ? Color.Red : Color.Gray;

                // Not for trade hubs, which do not build structures anyway
                GovNoScrap.Visible = GovernorTabView && Planet.colonyType != Planet.ColonyType.TradeHub && GovernorOn && Planet.Owner.isPlayer;

                int numTroopsCanLaunch    = Planet.NumTroopsCanLaunchFor(EmpireManager.Player);
                Planet.GarrisonSize       = (int)Garrison.AbsoluteValue;
                CallTroops.Visible        = DefenseTabView && Planet.Owner.isPlayer;
                LaunchSingleTroop.Visible = DefenseTabView && CallTroops.Visible && numTroopsCanLaunch > 0;
                LaunchAllTroops.Visible   = DefenseTabView && CallTroops.Visible && numTroopsCanLaunch > 1;
                Garrison.Visible          = DefenseTabView && Planet.Owner.isPlayer;
                AutoTroops.Visible        = DefenseTabView && Planet.Owner.isPlayer;
                GovOrbitals.Visible       = DefenseTabView && Planet.Owner.isPlayer && GovernorOn;
                GovGround.Visible         = GovOrbitals.Visible;
                BuildPlatform.Visible     = DefenseTabView && Planet.Owner.isPlayer && (!Planet.GovOrbitals || GovernorOff);
                BuildShipyard.Visible     = BuildPlatform.Visible;
                BuildStation.Visible      = BuildPlatform.Visible;
                PlatformsText.Visible     = DefenseTabView;
                ShipyardsText.Visible     = DefenseTabView;
                StationsText.Visible      = DefenseTabView;
                NoGovernor.Visible        = DefenseTabView && GovernorOff;
                ManualOrbitals.Visible    = DefenseTabView && Planet.GovOrbitals && GovernorOn;
                ColonyRank.Visible        = DefenseTabView && GovernorOn;
                ManualPlatforms.Visible   = DefenseTabView && Planet.ManualOrbitals && Planet.GovOrbitals && GovernorOn;
                ManualShipyards.Visible   = ManualPlatforms.Visible;
                ManualStations.Visible    = ManualPlatforms.Visible;
                GovOrbitals.TextColor     = Planet.GovOrbitals        ? Color.White : Color.Gray;
                GovGround.TextColor       = Planet.GovGroundDefense   ? Color.White : Color.Gray;
                ManualOrbitals.TextColor  = Planet.ManualOrbitals     ? Color.White : Color.Gray;
                AutoTroops.TextColor      = Planet.AutoBuildTroops    ? Color.White : Color.Gray;
                GovNoScrap.TextColor      = Planet.DontScrapBuildings ? Color.White : Color.Gray;
                OverrideCiv.TextColor     = OverrideCivBudget ? Color.White : Color.Gray;
                OverrideGrd.TextColor     = OverrideGrdBudget ? Color.White : Color.Gray;
                OverrideSpc.TextColor     = OverrideSpcBudget ? Color.White : Color.Gray;

                if (ManualOrbitals.Visible && Planet.ManualOrbitals)
                {
                    Planet.SetWantedPlatforms((byte)ManualPlatforms.AbsoluteValue);
                    Planet.SetWantedShipyards((byte)ManualShipyards.AbsoluteValue);
                    Planet.SetWantedStations((byte)ManualStations.AbsoluteValue);
                }
                else
                {
                    ManualPlatforms.AbsoluteValue = Planet.WantedPlatforms;
                    ManualShipyards.AbsoluteValue = Planet.WantedShipyards;
                    ManualStations.AbsoluteValue  = Planet.WantedStations;
                }

                BudgetSum.Visible       = BudgetTabView;
                BudgetPercent.Visible   = BudgetTabView && GovernorOn;
                OverrideCiv.Visible     = BudgetTabView && GovernorOn && Planet.Owner.isPlayer;
                OverrideGrd.Visible     = OverrideCiv.Visible;
                OverrideSpc.Visible     = OverrideCiv.Visible;
                ManualCivBudget.Visible = OverrideCiv.Visible && OverrideCiv.Checked;
                ManualGrdBudget.Visible = OverrideGrd.Visible && OverrideGrd.Checked;
                ManualSpcBudget.Visible = OverrideSpc.Visible && OverrideSpc.Checked;

                NoGovernorCivExpense.Visible = BudgetTabView && GovernorOff;
                NoGovernorGrdExpense.Visible = NoGovernorCivExpense.Visible;
                NoGovernorSpcExpense.Visible = NoGovernorCivExpense.Visible;
            }

            UpdateButtonTimer(fixedDeltaTime);
            base.Update(fixedDeltaTime);
        }

        void UpdateButtonTimer(float elapsedTime)
        {
            ButtonUpdateTimer -= elapsedTime;
            if (ButtonUpdateTimer > 0f)
                return;

            ButtonUpdateTimer = 1;
            UpdateButtons();
            UpdateGovOrbitalStats();
            UpdateBudgets();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            switch (Tabs.SelectedIndex)
            {
                case 0: DrawGovernorTab(batch); break;
                case 1: DrawTroopsTab(batch);   break;
                case 2: DrawBudgetsTab(batch);  break;
            }
        }

        void DrawGovernorTab(SpriteBatch batch)
        {
            // Governor portrait overlay stuff
            Portrait.Color = Planet.colonyType == Planet.ColonyType.Colony ? new Color(64, 64, 64) : Color.White;
            Color borderColor;
            switch (Planet.colonyType)
            {
                default:                             borderColor = Color.White; break;
                case Planet.ColonyType.TradeHub:     borderColor = Color.Yellow; break;
                case Planet.ColonyType.Colony:       borderColor = new Color(64, 64, 64); break;
                case Planet.ColonyType.Industrial:   borderColor = Color.Orange; break;
                case Planet.ColonyType.Agricultural: borderColor = Color.Green; break;
                case Planet.ColonyType.Research:     borderColor = Color.CornflowerBlue; break;
                case Planet.ColonyType.Military:     borderColor = Color.Red; break;
            }

            Portrait.Border = borderColor;
            batch.Draw(PortraitShine, Portrait.Rect);
        }

        void UpdateOrbitalTextPos()
        {
            if ((Planet.GovOrbitals || !Planet.Owner.isPlayer) && GovernorOn)
            {
                PlatformsText.Pos = new Vector2(BuildPlatform.X, BuildPlatform.Y + 3);
                ShipyardsText.Pos = new Vector2(BuildShipyard.X, BuildShipyard.Y + 3);
                StationsText.Pos  = new Vector2(BuildStation.X, BuildStation.Y + 3);
            }
            else
            {
                PlatformsText.Pos = new Vector2(BuildPlatform.X + BuildPlatform.Width + 20, BuildPlatform.Y + 3);
                ShipyardsText.Pos = new Vector2(BuildShipyard.X + BuildShipyard.Width + 20, BuildShipyard.Y + 3);
                StationsText.Pos  = new Vector2(BuildStation.X + BuildStation.Width + 20, BuildStation.Y + 3);
            }
        }

        void DrawTroopsTab(SpriteBatch batch)
        {
            var lineColor = new Color(118, 102, 67, 255);
            Vector2 top   = new Vector2(X + 190, Y + 30);
            Vector2 bot   = new Vector2(X + 190, Bottom - 5);

            UpdateOrbitalTextPos();
            batch.DrawLine(top, bot, lineColor);
        }

        void DrawBudgetsTab(SpriteBatch batch)
        {
            if (GovernorOn)
            {
                CivBudgetBar.Draw(batch);
                GrdBudgetBar.Draw(batch);
                SpcBudgetBar.Draw(batch);
            }

            batch.Draw(ResourceManager.Texture("NewUI/BudgetCiv"), CivBudgetTexRect);
            batch.Draw(ResourceManager.Texture("NewUI/BudgetGround"), GrdBudgetTexRect);
            batch.Draw(ResourceManager.Texture("NewUI/BudgetSpace"), SpcBudgetTexRect);
        }

        void OnSendTroopsClicked(UIButton b)
        {
            if (EmpireManager.Player.GetTroopShipForRebase(out Ship troopShip, Planet))
            {
                GameAudio.EchoAffirmative();
                troopShip.AI.OrderRebase(Planet, true);
                UpdateButtons();
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }

        void OnLaunchTroopsClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in Planet.TilesList)
            {
                if (pgs.TroopsAreOnTile && pgs.LockOnPlayerTroop(out Troop troop) && troop.CanMove)
                {
                    play = true;
                    troop.Launch(pgs);
                }
            }

            if (play)
            {
                GameAudio.TroopTakeOff();
                UpdateButtons();
            }
            else
                GameAudio.NegativeClick();
        }

        void OnLaunchSingleTroopClicked(UIButton b)
        {
            var potentialTroops = Planet.TroopsHere.Filter(t => t.Loyalty == EmpireManager.Player && t.CanMove);
            if (potentialTroops.Length == 0)
                GameAudio.NegativeClick();
            else
            {
                Troop troop = potentialTroops.RandItem();
                troop.Launch();
                GameAudio.TroopTakeOff();
                UpdateButtons();
            }
        }

        void UpdateButtons()
        {
            if (Planet.Owner != Empire.Universe.player)
                return;

            int troopsLanding = Planet.Owner.GetShips()
                .Filter(s => s != null && s.TroopCount > 0 && s.AI.State != AIState.Resupply && s.AI.State != AIState.Orbit)
                .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == Planet));

            if (troopsLanding > 0)
            {
                CallTroops.Text = $"{new LocalizedText(4220).Text} {troopsLanding}"; // "Incoming Troops
                CallTroops.Style = ButtonStyle.Military;
            }
            else
            {
                CallTroops.Text = new LocalizedText(4215).Text; // "Call Troops"
                CallTroops.Style = ButtonStyle.Default;
            }

            UpdateButtonText(LaunchAllTroops, Planet.TroopsHere.Count(t => t.CanMove), new LocalizedText(4213).Text);
        }

        void UpdateGovOrbitalStats()
        {
            if (Planet.Owner != Empire.Universe.player
                && !EmpireManager.Player.data.MoleList.Any(m => m.PlanetGuid == Planet.guid))
            {
                return;
            }

            int rank             = Planet.GetColonyRank();
            int currentPlatforms = Planet.NumPlatforms + Planet.OrbitalsBeingBuilt(ShipData.RoleName.platform);
            int currentStations  = Planet.NumStations + Planet.OrbitalsBeingBuilt(ShipData.RoleName.station);
            int currentShipyards = Planet.NumShipyards + Planet.ShipyardsBeingBuilt();
            ColonyRank.Text      = $"{new LocalizedText(4221).Text} {rank}/15";

            if ((Planet.GovOrbitals || !Planet.Owner.isPlayer) && GovernorOn)
            {
                PlatformsText.Text  = $"{new LocalizedText(4222).Text} {currentPlatforms}/{Planet.WantedPlatforms}";
                ShipyardsText.Text  = $"{new LocalizedText(4223).Text} {currentShipyards}/{Planet.WantedShipyards}";
                StationsText.Text   = $"{new LocalizedText(4224).Text} {currentStations}/{Planet.WantedStations}";
                PlatformsText.Color = GetColor(currentPlatforms, Planet.WantedPlatforms);
                ShipyardsText.Color = GetColor(currentShipyards, Planet.WantedShipyards);
                StationsText.Color  = GetColor(currentStations, Planet.WantedStations);
            }
            else
            {
                PlatformsText.Text  = $"{currentPlatforms}";
                ShipyardsText.Text  = $"{currentShipyards}";
                StationsText.Text   = $"{currentStations}";
                PlatformsText.Color = ShipyardsText.Color = StationsText.Color = Color.White;
            }

            // local method
            Color GetColor(int num, int maxNum)
            {
                if (num == 0)      return Color.Gray;
                if (num < maxNum)  return Color.Yellow;
                if (num == maxNum) return Color.Green;

                return Color.OrangeRed;
            }
        }

        void UpdateCivBudget(PlanetBudget budget)
        {
            if (ManualCivBudget.HandlingInput)
                return;

            if (BudgetTabView && OverrideCivBudget && ManualCivBudget.Visible
                && float.TryParse(ManualCivBudget.Text, out float value)
                && value > 0 && value < 250)
            {
                Planet.SetManualCivBudget(value);
            }
            else
            {
                ManualCivBudget.Text = budget.CivilianAlloc.String(2);
            }
        }

        void UpdateGrdBudget(PlanetBudget budget)
        {
            if (ManualGrdBudget.HandlingInput)
                return;

            if (BudgetTabView && OverrideGrdBudget && ManualGrdBudget.Visible
                && float.TryParse(ManualGrdBudget.Text, out float value)
                && value > 0 && value < 250)
            {
                Planet.SetManualGroundDefBudget(value);
            }
            else
            {
                ManualGrdBudget.Text = budget.GrdDefAlloc.String(2);
            }
        }

        void UpdateSpcBudget(PlanetBudget budget)
        {
            if (ManualSpcBudget.HandlingInput)
                return;

            if (BudgetTabView && OverrideSpcBudget && ManualSpcBudget.Visible
                && float.TryParse(ManualSpcBudget.Text, out float value)
                && value > 0 && value < 250)
            {
                Planet.SetManualSpaceDefBudget(value);
            }
            else
            {
                ManualSpcBudget.Text = budget.SpcDefAlloc.String(2);
            }
        }

        void UpdateBudgets()
        {
            var budget = new PlanetBudget(Planet);

            UpdateCivBudget(budget);
            UpdateGrdBudget(budget);
            UpdateSpcBudget(budget);

            CivBudgetBar.Max      = budget.CivilianAlloc;
            CivBudgetBar.Progress = Planet.CivilianBuildingsMaintenance;
            GrdBudgetBar.Max      = budget.GrdDefAlloc;
            GrdBudgetBar.Progress = Planet.GroundDefMaintenance;
            SpcBudgetBar.Max      = budget.SpcDefAlloc;
            SpcBudgetBar.Progress = Planet.SpaceDefMaintenance;

            float spent = Planet.CivilianBuildingsMaintenance + Planet.GroundDefMaintenance + Planet.SpaceDefMaintenance;
            if (GovernorOn)
            {
                float percentSpent  = spent / budget.TotalAlloc.LowerBound(0.01f) * 100;
                BudgetSum.Text      = $"{new LocalizedText(4234).Text} {spent.String(1)}" +
                                      $" {new LocalizedText(4235).Text} {budget.TotalAlloc.String(1)} BC/Y";
                BudgetPercent.Text  = $" ({percentSpent.String(1)}%)";
                BudgetPercent.Color = GetColor();
            }
            else
            {
                NoGovernorCivExpense.Text = $"{Planet.CivilianBuildingsMaintenance.String(2)} BC/Y";
                NoGovernorGrdExpense.Text = $"{Planet.GroundDefMaintenance.String(2)} BC/Y";
                NoGovernorSpcExpense.Text = $"{Planet.SpaceDefMaintenance.String(2)} BC/Y";
                BudgetSum.Text            = $"{new LocalizedText(4234).Text} {spent.String(2)} BC/Y";
                BudgetPercent.Text        = "";
            }

            // Local Method
            Color GetColor()
            {
                if (GovernorOff)
                    return Color.White;

                if (spent.AlmostZero()) return Color.Gray;
                if (spent < 25)         return Color.Green;
                if (spent < 50)         return Color.GreenYellow;
                if (spent < 75)         return Color.Yellow;
                if (spent < 100)        return Color.Orange;

                return Color.OrangeRed;
            }
        }

        void UpdateButtonText(UIButton button, int value, string defaultText)
        {
            button.Text = value > 0 ? $"{defaultText} ({value})" : defaultText;
        }

        void OnBuildPlatformClick(UIButton b)
        {
            if (BuildOrbital(Planet.Owner.BestPlatformWeCanBuild))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        void OnBuildStationClick(UIButton b)
        {
            if (BuildOrbital(Planet.Owner.BestStationWeCanBuild))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        void OnBuildShipyardClick(UIButton b)
        {
            string shipyardName = ResourceManager.ShipsDict[Planet.Owner.data.DefaultShipyard].Name;
            Ship shipyard       = ResourceManager.GetShipTemplate(shipyardName);

            if (Planet.Owner.CanBuildShipyards && BuildOrbital(shipyard))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        bool BuildOrbital(Ship orbital)
        {
            if (orbital == null || Planet.IsOutOfOrbitalsLimit(orbital))
                return false;

            Planet.AddOrbital(orbital);
            return true;
        }

        public override bool HandleInput(InputState input)
        {
            if (GovOrbitals.HitTest(input.CursorPosition))
                UpdateGovOrbitalStats();

            if (ColonyRank.Visible && ColonyRank.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(279);

            if (BudgetTabView)
            {
                if      (CivBudgetTexRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GovernorOn ? 280 : 283);
                else if (GrdBudgetTexRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GovernorOn ? 281 : 284);
                else if (SpcBudgetTexRect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(GovernorOn ? 282 : 285);
            }

            ManualCivBudget.HandlingInput = ManualCivBudget.HitTest(input.CursorPosition);
            ManualGrdBudget.HandlingInput = ManualGrdBudget.HitTest(input.CursorPosition);
            ManualSpcBudget.HandlingInput = ManualSpcBudget.HitTest(input.CursorPosition);

            return base.HandleInput(input);
        }
    }
}

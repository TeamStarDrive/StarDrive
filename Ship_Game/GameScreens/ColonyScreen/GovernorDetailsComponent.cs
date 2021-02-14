using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
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
        private UICheckBox GovOrbitals, AutoTroops, GovNoScrap, Quarantine, ManualOrbitals;
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

        public GovernorDetailsComponent(GameScreen screen, Planet p, in Rectangle rect) : base(rect)
        {
            Screen = screen;
            SetPlanetDetails(p);
            Tabs = Add(new Submenu(rect));
            Tabs.AddTab("Governor"); // "Assign Labor"
            Tabs.AddTab("Defense"); // "Assign Labor"
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

            Portrait  = Add(new UIPanel(PortraitSprite));
            WorldType = Add(new UILabel(Planet.WorldType, Fonts.Arial12Bold));
            WorldDescription = Add(new UILabel(Fonts.Arial12Bold));
            
            GovOrbitals    = Add(new UICheckBox(() => Planet.GovOrbitals, Fonts.Arial12Bold, title:1960, tooltip:1961));
            AutoTroops     = Add(new UICheckBox(() => Planet.AutoBuildTroops, Fonts.Arial12Bold, title:1956, tooltip:1957));
            GovNoScrap     = Add(new UICheckBox(() => Planet.DontScrapBuildings, Fonts.Arial12Bold, title:1941, tooltip:1942));
            Quarantine     = Add(new UICheckBox(() => Planet.Quarantine, Fonts.Arial12Bold, title: 1888, tooltip: 1887));
            ManualOrbitals = Add(new UICheckBox(() => Planet.ManualOrbitals, Fonts.Arial12Bold, title: 4201, tooltip: 4202));

            Garrison        = Slider(200, 200, 160, 40, "Garrison Size", 0, 25,Planet.GarrisonSize);
            ManualPlatforms = Slider(200, 200, 120, 40, "Manual Limit", 0, 15, Planet.WantedPlatforms);
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
            LaunchAllTroops   = Button(ButtonStyle.Default,"Launch All Troops", OnLaunchTroopsClicked);
            LaunchSingleTroop = Button(ButtonStyle.Default, "Launch Single Troop", OnLaunchSingleTroopClicked);
            CallTroops        = Button(ButtonStyle.Default, "Call Troops", OnSendTroopsClicked);

            LaunchAllTroops.Tooltip   = new LocalizedText(1952).Text;
            LaunchSingleTroop.Tooltip = new LocalizedText(1950).Text;
            CallTroops.Tooltip        = new LocalizedText(1949).Text;

            BuildShipyard = Button(ButtonStyle.Medium, "Build Shipyard", OnBuildShipyardClick);
            BuildStation  = Button(ButtonStyle.Medium, "Build Station", OnBuildStationClick);
            BuildPlatform = Button(ButtonStyle.Medium, "Build Platform", OnBuildPlatformClick);

            BuildShipyard.Tooltip = new LocalizedText(1948).Text;
            BuildStation.Tooltip  = new LocalizedText(1947).Text;
            BuildPlatform.Tooltip = new LocalizedText(1946).Text;

            PlatformsText    = Add(new UILabel(" "));
            ShipyardsText    = Add(new UILabel(" "));
            StationsText     = Add(new UILabel(" "));
            NoGovernor       = Add(new UILabel("No Governor"));
            NoGovernor.Font  = Fonts.Arial12Bold;
            NoGovernor.Color = Color.Gray;
            ColonyRank       = Add(new UILabel(" "));
            ColonyRank.Font  = Fonts.Arial12Bold;
            ColonyRank.Color = Color.SteelBlue;


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

            AutoTroops.Pos        = new Vector2(TopLeft.X + 10, Y + 40);
            Garrison.Pos          = new Vector2(TopLeft.X + 20, Y + 70);
            CallTroops.Pos        = new Vector2(TopLeft.X + 10, Bottom - 30);
            LaunchSingleTroop.Pos = new Vector2(TopLeft.X + 10, Bottom - 60);
            LaunchAllTroops.Pos   = new Vector2(TopLeft.X + 10, Bottom - 90);
            GovOrbitals.Pos       = new Vector2(TopLeft.X + 200, Y + 40);
            NoGovernor.Pos        = new Vector2(GovOrbitals.X, GovOrbitals.Y);
            ColonyRank.Pos        = new Vector2(TopLeft.X + 200, Y + 65);
            ManualOrbitals.Pos    = new Vector2(TopLeft.X + 200, Y + 90);
            BuildPlatform.Pos     = new Vector2(TopLeft.X + 200, Bottom - 90);
            BuildShipyard.Pos     = new Vector2(TopLeft.X + 200, Bottom - 60);
            BuildStation.Pos      = new Vector2(TopLeft.X + 200, Bottom - 30);
            Vector2 manualOffset  = new Vector2(125, -15);
            ManualPlatforms.Pos   = BuildPlatform.Pos + manualOffset;
            ManualShipyards.Pos   = BuildShipyard.Pos + manualOffset;
            ManualStations.Pos    = BuildStation.Pos + manualOffset;

            UpdateButtons();
            UpdateGovOrbitalStats();
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
                WorldDescription.Visible = Tabs.SelectedIndex == 0 && Planet.Owner.isPlayer;
                ColonyTypeList.Visible   = Tabs.SelectedIndex == 0 && Planet.Owner.isPlayer;
                Portrait.Visible         = Tabs.SelectedIndex == 0 && Planet.Owner.isPlayer;
                WorldType.Visible        = Tabs.SelectedIndex == 0 && Planet.Owner.isPlayer;
                Quarantine.Visible       = Tabs.SelectedIndex == 0 && Planet.Owner.isPlayer;
                Quarantine.TextColor     = Planet.Quarantine ? Color.Red : Color.White;

                // not for trade hubs, which do not build structures anyway
                GovNoScrap.Visible = Tabs.SelectedIndex == 0 && Planet.colonyType != Planet.ColonyType.TradeHub 
                                                             && Planet.colonyType != Planet.ColonyType.Colony;

                int numTroopsCanLaunch    = Planet.NumTroopsCanLaunchFor(EmpireManager.Player);
                Planet.GarrisonSize       = (int)Garrison.AbsoluteValue;
                CallTroops.Visible        = Tabs.SelectedIndex == 1 && Planet.Owner == Empire.Universe.player;
                LaunchSingleTroop.Visible = Tabs.SelectedIndex == 1 && CallTroops.Visible && numTroopsCanLaunch > 0;
                LaunchAllTroops.Visible   = Tabs.SelectedIndex == 1 && CallTroops.Visible && numTroopsCanLaunch > 1;
                Garrison.Visible          = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer;
                AutoTroops.Visible        = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer;
                GovOrbitals.Visible       = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer && Planet.colonyType != Planet.ColonyType.Colony;
                BuildPlatform.Visible     = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer && !Planet.GovOrbitals;
                BuildShipyard.Visible     = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer && !Planet.GovOrbitals;
                BuildStation.Visible      = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer && !Planet.GovOrbitals;
                PlatformsText.Visible     = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer;
                ShipyardsText.Visible     = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer;
                StationsText.Visible      = Tabs.SelectedIndex == 1 && Planet.Owner.isPlayer;
                NoGovernor.Visible        = Tabs.SelectedIndex == 1 && Planet.colonyType == Planet.ColonyType.Colony;
                ManualOrbitals.Visible    = Tabs.SelectedIndex == 1 && Planet.colonyType != Planet.ColonyType.Colony && Planet.GovOrbitals;
                ColonyRank.Visible        = Tabs.SelectedIndex == 1 && ManualOrbitals.Visible;
                ManualPlatforms.Visible   = Tabs.SelectedIndex == 1 && Planet.ManualOrbitals && Planet.GovOrbitals;
                ManualShipyards.Visible   = Tabs.SelectedIndex == 1 && Planet.ManualOrbitals && Planet.GovOrbitals;
                ManualStations.Visible    = Tabs.SelectedIndex == 1 && Planet.ManualOrbitals && Planet.GovOrbitals;

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
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            switch (Tabs.SelectedIndex)
            {
                case 0: DrawGovernorTab(batch); break;
                case 1: DrawTroopsTab(batch);   break;
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

        void DrawTroopsTab(SpriteBatch batch)
        {
            var lineColor = new Color(118, 102, 67, 255);
            Vector2 top   = new Vector2(X + 190, Y + 30);
            Vector2 bot   = new Vector2(X + 190, Bottom - 5);

            if (Planet.GovOrbitals)
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

            batch.DrawLine(top, bot, lineColor);
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
                CallTroops.Text = $"Incoming Troops: {troopsLanding}";
                CallTroops.Style = ButtonStyle.Military;
            }
            else
            {
                CallTroops.Text = "Call Troops";
                CallTroops.Style = ButtonStyle.Default;
            }

            UpdateButtonText(LaunchAllTroops, Planet.TroopsHere.Count(t => t.CanMove), "Launch All Troops");
        }

        void UpdateGovOrbitalStats()
        {
            if (Planet.Owner != Empire.Universe.player || Planet.colonyType == Planet.ColonyType.Colony)
                return;

            int rank             = Planet.GetColonyRank();
            int currentPlatforms = Planet.NumPlatforms + Planet.OrbitalsBeingBuilt(ShipData.RoleName.platform);
            int currentStations  = Planet.NumStations + Planet.OrbitalsBeingBuilt(ShipData.RoleName.station);
            int currentShipyards = Planet.NumShipyards + Planet.ShipyardsBeingBuilt();
            ColonyRank.Text      = $"Governor Colony Rank: {rank}/15";

            if (Planet.GovOrbitals)
            {
                PlatformsText.Text  = $"Platforms: {currentPlatforms}/{Planet.WantedPlatforms}";
                ShipyardsText.Text  = $"Shipyards: {currentShipyards}/{Planet.WantedShipyards}";
                StationsText.Text   = $"Stations: {currentStations}/{Planet.WantedStations}";
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
                if (num == 0)     
                    return Color.Gray;

                return num < maxNum ? Color.Yellow : Color.Green;
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

            if (BuildOrbital(shipyard))
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

            return base.HandleInput(input);
        }
    }
}

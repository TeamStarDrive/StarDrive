using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Ship_Game.Ships;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.CombatTactics.UI;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using System.Linq;
using Ship_Game.Data.Yaml;
using System.IO;
using Ship_Game.GameScreens.FleetDesign;
using SynapseGaming.LightingSystem.Lights;
// SunBurn + MonoGame both define DirectionalLight; we want SunBurn's here.
using DirectionalLight = SynapseGaming.LightingSystem.Lights.DirectionalLight;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class FleetDesignScreen : GameScreen
    {
        public override bool HelpKeyOpensCodex => true;

        public readonly UniverseScreen Universe;
        public readonly EmpireUIOverlay EmpireUI;
        public readonly Empire Player;

        Menu2 TitleBar;
        Menu2 ShipDesigns;
        Vector2 TitlePos;
        Vector2 ShipDesignsTitlePos;
        Menu1 LeftMenu;
        Menu1 RightMenu;

        // never null, if a fleet doesn't exist, an empty one is created
        public Fleet SelectedFleet;

        // the currently dragged Ship Design
        // it might be a ship that we own, or it could be a Ship template
        #pragma warning disable CA2213 // managed by Content Manager
        Ship ActiveShipDesign;
        #pragma warning restore CA2213

        ScrollList<FleetDesignShipListItem> ShipSL;
        BlueButton RequisitionForces;
        BlueButton SaveDesign;
        BlueButton LoadDesign;
        BlueButton AutoArrange;
        RectF SelectedStuffRect;
        RectF OperationsRect;
        RectF PrioritiesRect;

        // Both the per-node Target Priority sliders and the Operational Radius
        // slider are currently disconnected from gameplay in practice — combat
        // state changes overwrite the weights and the OrdersRadius slider has a
        // relative/absolute unit mismatch that the AI ignores. Hide the panels
        // until the underlying systems are reworked; flip back to true when the
        // weights actually persist and OrdersRadius is honored.
        static bool ShowTargetingPanels = false;
        RectF FleetOverviewRect;
        UITextBox FleetOverviewText;
        WeightSlider SliderAssist;
        WeightSlider SliderVulture;
        WeightSlider SliderDefend;
        WeightSlider SliderDps;
        WeightSlider SliderArmor;
        WeightSlider SliderShield;

        FloatSlider OperationalRadius;
        SizeSlider SliderSize;
        public SubmenuScrollList<FleetDesignShipListItem> SubShips;
        Array<Ship> ActiveShips = new();

        Vector3 CamPos = new(0f, 0f, 14000f);
        Vector3 DesiredCamPos = new(0f, 0f, 14000f);

        readonly Array<ClickableSquad> ClickableSquads = new();

        readonly UITextEntry FleetNameEntry;
        Selector StuffSelector;
        Selector OperationsSelector;
        Selector PrioritySelector;
        readonly Array<ClickableNode> ClickableNodes = new();
        Fleet.Squad SelectedSquad;
        Fleet.Squad HoveredSquad;
        RectF SelectionBox;
        readonly Array<FleetDataNode> SelectedNodeList = new();
        readonly Array<FleetDataNode> HoveredNodeList = new();
        readonly ShipInfoOverlayComponent ShipInfoOverlay;
        FleetStanceButtons OrdersButtons;

        public FleetDesignScreen(UniverseScreen u, EmpireUIOverlay empireUI, string audioCue = "")
            : base(u, toPause: u)
        {
            Universe = u;
            EmpireUI = empireUI;
            Player = u.Player;

            TransitionOnTime = 0.75f;
            Player.UpdateShipsWeCanBuild();
            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this, u.UState));

            FleetNameEntry = new();
            FleetNameEntry.OnTextChanged = (text) => SelectedFleet.Name = text;
            FleetNameEntry.SetColors(Colors.Cream, Color.Orange);
            
            GameAudio.PlaySfxAsync(audioCue);

            // choose the first active fleet we have, or default to first fleet key
            Fleet anyFleet = Player.ActiveFleets.ToArrayList().Sorted(f => f.Key).FirstOrDefault();
            int fleetId = (anyFleet?.Key ?? Empire.FirstFleetKey).Clamped(Empire.FirstFleetKey, Empire.LastFleetKey);
            ChangeFleet(fleetId);
        }

        public void ChangeFleet(int fleetKey)
        {
            SelectedNodeList.Clear();
            RemoveSceneObjects(SelectedFleet);

            Fleet fleet = Player.GetFleetOrNull(fleetKey) ?? Player.CreateFleet(fleetKey, null);
            SelectedFleet = fleet;

            var toRemove = new Array<FleetDataNode>();
            foreach (FleetDataNode node in fleet.DataNodes)
            {
                if (node.Ship == null)
                {
                    if (!ResourceManager.Ships.Exists(node.ShipName) || !Player.WeCanBuildThis(node.ShipName))
                        toRemove.Add(node);
                }
            }

            var squadsToRemove = new Array<Fleet.Squad>();
            foreach (FleetDataNode node in toRemove)
            {
                fleet.DataNodes.Remove(node);
                foreach (Fleet.Squad squad in AllSquads)
                {
                    squad.DataNodes.Remove(node);
                    if (squad.DataNodes.Count == 0)
                        squadsToRemove.Add(squad);
                }
            }

            foreach (Array<Fleet.Squad> flanks in fleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in squadsToRemove)
                    if (flanks.Contains(squad))
                        flanks.Remove(squad);
            }
            
            foreach (FleetDataNode node in fleet.DataNodes)
            {
                if (node.Ship != null)
                    node.Ship.RelativeFleetOffset = node.RelativeFleetOffset;
            }

            FleetNameEntry.Size = FleetNameEntry.Font.MeasureString(fleet.Name);
        }

        public void OnSubShipsTabChanged(int tabIndex)
        {
            ResetLists();
        }

        // FleetDesign screen became visible
        public override void BecameActive()
        {
            AssignLightRig(LightRigIdentity.FleetDesign);
            SetupFleetDesignLighting();
        }

        // Post-migration AssignLightRig is a stub — it clears all lights and
        // submits none (the SunBurn .lightRig content type was discontinued,
        // see MIGRATION_LIMITATIONS entry #7). Without an explicit rig the
        // fleet view renders unlit, so ship hulls read dim and flat-spec.
        // Mirrors the 3-directional Key / Fill / Back rig + ambient that
        // ShipDesignScreen uses (SetupShipyardLighting).
        void SetupFleetDesignLighting()
        {
            AddLight(new DirectionalLight
            {
                Name         = "FleetDesign Key",
                Direction    = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f),
                DiffuseColor = new Vector3(1f, 1f, 1f),
                Intensity    = 1.75f,
                Enabled      = true,
            }, dynamic: false);

            AddLight(new DirectionalLight
            {
                Name         = "FleetDesign Fill",
                Direction    = new Vector3(0.7198464f, 0.3420201f, 0.6040227f),
                DiffuseColor = new Vector3(0.85f, 0.88f, 0.92f),
                Intensity    = 0.8f,
                Enabled      = true,
            }, dynamic: false);

            AddLight(new DirectionalLight
            {
                Name         = "FleetDesign Back",
                Direction    = new Vector3(0.4545195f, -0.7660444f, 0.4545195f),
                DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f),
                Intensity    = 0.5f,
                Enabled      = true,
            }, dynamic: false);

            AddLight(new AmbientLight
            {
                Name         = "FleetDesign Ambient",
                DiffuseColor = Color.White.ToVector3(),
                Intensity    = 0.15f,
            }, dynamic: false);
        }

        // We opened another screen like Shipyard, or just exited this screen
        public override void BecameInActive()
        {
            RemoveSceneObjects(SelectedFleet);
        }

        public override void LoadContent()
        {
            Add(new CloseButton(ScreenWidth - 38, 97));
            SetPerspectiveProjection(maxDistance: 100_000);

            Graphics.Font titleFont = Fonts.Laserian14;
            Graphics.Font arial20 = Fonts.Arial20Bold;
            Graphics.Font arial12 = Fonts.Arial12Bold;

            RectF titleRect = new(2, 44, 250, 80);
            TitleBar = new(titleRect);
            TitlePos = new(titleRect.CenterX - titleFont.TextWidth("Fleet Hotkeys") / 2f,
                           titleRect.CenterY - titleFont.LineSpacing / 2f);

            RectF leftRect = new(20, titleRect.Bottom + 5, titleRect.W, 500);
            LeftMenu = new(leftRect, true);
            
            Add(new FleetButtonsList(leftRect, this, Universe,
                onClick: InputSelectFleet,
                onHotKey: InputSelectFleet,
                isSelected: (b) => SelectedFleet?.Key == b.FleetKey
            ));

            RectF shipRect = new(ScreenWidth - 282, 140, 280, 80);
            ShipDesigns = new Menu2(shipRect);
            ShipDesignsTitlePos = new(shipRect.CenterX - titleFont.TextWidth("Ship Designs") / 2f, shipRect.CenterY - titleFont.LineSpacing / 2);
            RectF shipDesignsRect = new(ScreenWidth - shipRect.W - 2, shipRect.Bottom + 5, shipRect.W, 500);
            RightMenu = new(shipDesignsRect);

            LocalizedText[] subShipsTabs = { "Designs", "Owned" };
            SubShips = Add(new SubmenuScrollList<FleetDesignShipListItem>(shipDesignsRect, subShipsTabs));
            SubShips.SetBackground(Colors.TransparentBlackFill);
            SubShips.SelectedIndex = 0;
            SubShips.OnTabChange = OnSubShipsTabChanged;

            ShipSL = SubShips.List;
            ShipSL.OnClick = OnDesignShipItemClicked;
            ShipSL.OnDoubleClick = OnDesignShipItemDoubleClicked;
            ShipSL.EnableItemHighlight = true;
            ShipSL.OnHovered = (item) =>
            {
                if (item == null) // deselected?
                {
                    ToolTip.Clear();
                    ShipInfoOverlay.ShowToLeftOf(Vector2.Zero, null); // hide it
                    return;
                }
                string tooltip = "Drag and drop this Ship into the Fleet our double click to auto-add to a squad";
                ToolTip.CreateTooltip(tooltip, "", item.BotLeft, minShowTime:2f);
                ShipInfoOverlay.ShowToLeftOf(item.Pos, item.Ship?.ShipData ?? item.Design);
            };

            ResetLists();
            SelectedStuffRect = new(ScreenWidth / 2 - 220, -13 + ScreenHeight - 210, 440, 210);

            var ordersBarPos = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 65);
            OrdersButtons = new(this, ordersBarPos);
            Add(OrdersButtons);

            RequisitionForces = Add(new BlueButton(new(SelectedStuffRect.X + 240, SelectedStuffRect.Y + arial20.LineSpacing + 20 - 10), "Requisition..."));
            SaveDesign = Add(new BlueButton(new(SelectedStuffRect.X + 240, SelectedStuffRect.Y + arial20.LineSpacing + 20 + 30), "Save Design..."));
            LoadDesign = Add(new BlueButton(new(SelectedStuffRect.X + 240, SelectedStuffRect.Y + arial20.LineSpacing + 20 + 70), "Load Design..."));
            AutoArrange = Add(new BlueButton(new(SelectedStuffRect.X + 240, SelectedStuffRect.Y + arial20.LineSpacing + 20 + 110), "Auto Arrange..."));

            RequisitionForces.OnClick = (b) => ScreenManager.AddScreen(new RequisitionScreen(this));
            SaveDesign.OnClick = (b) => ScreenManager.AddScreen(new SaveFleetDesignScreen(this, SelectedFleet));
            LoadDesign.OnClick = (b) => ScreenManager.AddScreen(new LoadFleetDesignScreen(this));
            AutoArrange.OnClick = (b) => SelectedFleet.AutoArrange();   

            OperationsRect = new(SelectedStuffRect.Right + 2, SelectedStuffRect.Y + 30, 360, SelectedStuffRect.H - 30);

            float slidersX1 = OperationsRect.X + 15;
            float slidersX2 = OperationsRect.X + 15 + 180;
            float slidersY = OperationsRect.Y + arial12.LineSpacing + 20;
            
            WeightSlider NewSlider(float x, float y, string title, LocalizedText tooltip)
            {
                return new(new(x, y, 150, 40), title, tooltip);
            }

            SliderAssist  = NewSlider(slidersX1, slidersY, "Assist Nearby Weight", GameText.AMeasureOfHowCooperative);
            SliderDefend  = NewSlider(slidersX1, slidersY+50, "Defend Nearby Weight", GameText.AMeasureOfHowProtective);
            SliderVulture = NewSlider(slidersX1, slidersY+100, "Target Damaged Weight", GameText.AMeasureOfHowOpportunistic);

            SliderArmor  = NewSlider(slidersX2, slidersY, "Target Armored Weight", GameText.TheWeightGivenToTargeting);
            SliderShield = NewSlider(slidersX2, slidersY+50, "Target Shielded Weight", GameText.TheWeightGivenToTargeting2);
            SliderDps    = NewSlider(slidersX2, slidersY+100, "Target DPS Weight", GameText.TheWeightGivenToTargeting3);

            PrioritiesRect = new(SelectedStuffRect.X - OperationsRect.W - 2, OperationsRect.Y, OperationsRect.Size);

            // Fleet Design Overview panel: same vertical span as the First Fleet panel,
            // at PrioritiesRect's X so it sits left of First Fleet. Hidden when a node is selected.
            FleetOverviewRect = new RectF(PrioritiesRect.X, SelectedStuffRect.Y, PrioritiesRect.W, SelectedStuffRect.H);
            float headerH = Fonts.Pirulen12.LineSpacing + 15;
            RectF overviewTextRect = new(FleetOverviewRect.X + 10,
                                          FleetOverviewRect.Y + headerH,
                                          FleetOverviewRect.W - 20,
                                          FleetOverviewRect.H - headerH - 10);
            FleetOverviewText = Add(new UITextBox(overviewTextRect, useBorder: false));
            FleetOverviewText.ItemsList.ItemPadding = Vector2.Zero;
            FleetOverviewText.SetLines(Localizer.Token(GameText.AddShipDesignsToThis));

            RectF oprect = new(PrioritiesRect.X + 15, PrioritiesRect.Y + arial12.LineSpacing + 20, 300, 40);
            OperationalRadius = new FloatSlider(oprect, "Operational Radius", max: 500000, value: 10000)
            {
                RelativeValue = 0.2f,
                Tip = GameText.DefinesTheAreaInWhich
            };

            RectF sizerect = new(PrioritiesRect.X + 15, PrioritiesRect.Y + arial12.LineSpacing + 70, 300, 40);
            SliderSize = new SizeSlider(sizerect, "Target UniverseRadius Preference");
            SliderSize.SetAmount(0.5f);
            SliderSize.Tooltip = GameText.DeterminesWhetherAShipPrefers;

            base.LoadContent();
        }

        void RemoveSceneObjects(Fleet fleet)
        {
            if (fleet != null)
                foreach (Ship ship in fleet.Ships)
                    ship.RemoveSceneObject();
        }

        public void LoadFleetDesign(FileInfo file)
        {
            FleetDesign data = YamlParser.Deserialize<FleetDesign>(file);
            RemoveSceneObjects(SelectedFleet);
            int currentFleetKey = SelectedFleet.Key;
            SelectedFleet.Reset(fleeIfInCombat: false, clearOrders: true);
            SelectedFleet.DataNodes.Clear();
            ClickableNodes.Clear();
            SelectedFleet.AllFlanks.ForEach(f => f.Clear());

            SelectedFleet.Name = data.Name;
            SelectedFleet.FleetIconIndex = data.FleetIconIndex;
            foreach (FleetDataDesignNode node in data.Nodes)
            {
                SelectedFleet.DataNodes.Add(new FleetDataNode(node));
            }
            SelectedFleet.Key = currentFleetKey;
        }

        public void ResetLists()
        {
            ActiveShipDesign = null; // this must be reset if tabs change

            // only valid and complete designs allowed, ignore platforms/stations/freighters
            static bool CanShowDesign(IShipDesign s) => s.IsValidDesign && s.GetCompletionPercent() == 100
                                                     && !s.IsPlatformOrStation;

            // allow player to add ships which already exist in the universe and don't have a fleet
            static bool CanShowShip(Ship s) => s.Fleet == null && s.IsAlive && CanShowDesign(s.ShipData);

            if (SubShips.SelectedIndex == 0) // ShipsWeCanBuild
            {
                IShipDesign[] designs = Player.ShipsWeCanBuildSnapshot.Filter(CanShowDesign);
                ShipSL.Reset();
                InitShipSL(designs);
            }
            else if (SubShips.SelectedIndex == 1) // Owned Ships
            {
                ActiveShips.Assign(Player.OwnedShips.Filter(CanShowShip));
                ShipSL.Reset();
                InitShipSL(ActiveShips);
            }
        }

        // init with ship design templates
        void InitShipSL(IReadOnlyList<IShipDesign> designs)
        {
            var roles = new HashSet<string>();
            foreach (IShipDesign s in designs)
                if (IsCandidateShip(s))
                    roles.Add(s.GetRole());
            
            var roleItems = roles.ToArrayList();
            roleItems.Sort();
            foreach (string role in roleItems)
            {
                FleetDesignShipListItem header = new(this, role);
                ShipSL.AddItem(header);
                foreach (IShipDesign d in Player.ShipsWeCanBuildSnapshot)
                    if (IsCandidateShip(d) && d.GetRole() == header.HeaderText)
                        header.AddSubItem(new FleetDesignShipListItem(this, d));
            }
        }

        // init with actually owned ships that are waiting for orders
        void InitShipSL(IReadOnlyList<Ship> aliveShips)
        {
            var roles = new HashSet<string>();
            foreach (Ship s in aliveShips)
                if (IsCandidateShip(s.ShipData))
                    roles.Add(s.ShipData.GetRole());

            var roleItems = roles.ToArrayList();
            roleItems.Sort();
            foreach (string role in roleItems)
            {
                FleetDesignShipListItem header = new(this, role);
                ShipSL.AddItem(header);
                foreach (Ship s in aliveShips)
                    if (IsCandidateShip(s.ShipData) && s.ShipData.GetRole() == header.HeaderText)
                        header.AddSubItem(new FleetDesignShipListItem(this, s));
            }
        }

        static bool IsCandidateShip(IShipDesign s)
        {
            return s.Role != RoleName.troop
                && s.Role is not (RoleName.ssp or RoleName.construction);
        }

        public struct ClickableNode
        {
            public FleetDataNode NodeToClick;
            public Vector2 ScreenPos;
            public float Radius;
        }

        struct ClickableSquad
        {
            public Fleet.Squad Squad;
            public RectF Rect; // rect on screen
        }
    }
}
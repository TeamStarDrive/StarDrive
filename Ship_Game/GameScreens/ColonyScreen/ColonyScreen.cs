using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class ColonyScreen : PlanetScreen
    {
        public Planet P;
        ToggleButton PlayerDesignsToggle;
        Menu2 TitleBar;
        Vector2 TitlePos;
        Menu1 LeftMenu;
        Menu1 RightMenu;
        Submenu PlanetInfo;
        Submenu pDescription;
        Submenu pStorage;
        Submenu pFacilities;
        Submenu BuildableTabs;
        UITextEntry PlanetName = new UITextEntry();
        Rectangle PlanetIcon;
        public EmpireUIOverlay eui;
        ToggleButton LeftColony;
        ToggleButton RightColony;
        UIButton LaunchAllTroops;
        UIButton LaunchSingleTroop;
        UIButton BuildPlatform;
        UIButton BuildStation;
        UIButton BuildShipyard;
        UIButton CallTroops;  //fbedard
        UITextEntry FilterBuildableItems;
        Rectangle GridPos;
        Submenu subColonyGrid;
        Submenu FilterFrame;
        UIButton ClearFilter;
        UILabel BlockadeLabel;

        ScrollList2<BuildableListItem> BuildableList;
        ScrollList2<ConstructionQueueScrollListItem> ConstructionQueue;
        DropDownMenu foodDropDown;
        DropDownMenu prodDropDown;
        ProgressBar FoodStorage;
        ProgressBar ProdStorage;
        Rectangle FoodStorageIcon;
        Rectangle ProfStorageIcon;
        float ButtonUpdateTimer;   // updates buttons once per second
        string PlatformsStats = "Platforms:";
        string StationsStats  = "Stations:";
        string ShipyardsStats = "Shipyards:";

        AssignLaborComponent AssignLabor;
        readonly ShipInfoOverlayComponent ShipInfoOverlay;
        readonly GovernorDetailsComponent GovernorDetails;

        object DetailInfo;
        Building ToScrap;

        public bool ClickedTroop;
        int EditHoverState;

        Rectangle EditNameButton;
        readonly SpriteFont Font8  = Fonts.Arial8Bold;
        readonly SpriteFont Font12 = Fonts.Arial12Bold;
        readonly SpriteFont Font20 = Fonts.Arial20Bold;
        readonly SpriteFont TextFont;
        public readonly Empire Player = EmpireManager.Player;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI) : base(parent)
        {
            P = p;
            eui = empUI;
            empUI.empire.UpdateShipsWeCanBuild();
            TextFont = LowRes ? Font8 : Font12;
            var titleBar = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleBar);
            LeftColony = new ToggleButton(new Vector2(titleBar.X + 25, titleBar.Y + 24), ToggleButtonStyle.ArrowLeft);
            RightColony = new ToggleButton(new Vector2(titleBar.X + titleBar.Width - 39, titleBar.Y + 24), ToggleButtonStyle.ArrowRight);
            TitlePos = new Vector2(titleBar.X + titleBar.Width / 2 - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, titleBar.Y + titleBar.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            LeftMenu = new Menu1(2, titleBar.Y + titleBar.Height + 5, titleBar.Width, ScreenHeight - (titleBar.Y + titleBar.Height) - 7);
            RightMenu = new Menu1(titleBar.Right + 10, titleBar.Y, ScreenWidth / 3 - 15, ScreenHeight - titleBar.Y - 2);
            Add(new CloseButton(RightMenu.Right - 52, RightMenu.Y + 22));
            PlanetInfo = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20, (int)(0.4f * LeftMenu.Width), (int)(0.25f * (LeftMenu.Height - 80)));
            PlanetInfo.AddTab(title:326);
            pDescription = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));


            var labor = new RectF(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + 20,
                                  0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            AssignLabor = Add(new AssignLaborComponent(P, labor, useTitleFrame: true));

            pStorage = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + labor.H + 40, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            pStorage.AddTab(title:328);

            Vector2 blockadePos = new Vector2(pStorage.X + 20, pStorage.Y + 35);
            BlockadeLabel = Add(new UILabel(blockadePos, "Blockade!", Fonts.Pirulen16, Color.Red));
            FoodStorage = new ProgressBar(pStorage.X + 100, pStorage.Y + 25 + 0.33f*(pStorage.Height - 25), 0.4f*pStorage.Width, 18);
            FoodStorage.Max = p.Storage.Max;
            FoodStorage.Progress = p.FoodHere;
            FoodStorage.color = "green";
            foodDropDown = new DropDownMenu(pStorage.X + 100 + 0.4f * pStorage.Width + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*pStorage.Width, 18);
            foodDropDown.AddOption(Localizer.Token(329));
            foodDropDown.AddOption(Localizer.Token(330));
            foodDropDown.AddOption(Localizer.Token(331));
            foodDropDown.ActiveIndex = (int)p.FS;
            var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
            FoodStorageIcon = new Rectangle((int)pStorage.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
            ProdStorage = new ProgressBar(pStorage.X + 100, pStorage.Y + 25 + 0.66f*(pStorage.Height - 25), 0.4f*pStorage.Width, 18);
            ProdStorage.Max = p.Storage.Max;
            ProdStorage.Progress = p.ProdHere;
            var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
            ProfStorageIcon = new Rectangle((int)pStorage.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
            prodDropDown = new DropDownMenu(pStorage.X + 100 + 0.4f*pStorage.Width + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*pStorage.Width, 18);
            prodDropDown.AddOption(Localizer.Token(329));
            prodDropDown.AddOption(Localizer.Token(330));
            prodDropDown.AddOption(Localizer.Token(331));
            prodDropDown.ActiveIndex = (int)p.PS;

            subColonyGrid = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            subColonyGrid.AddTab(Localizer.Token(332));
            pFacilities = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, subColonyGrid.Bottom + 20, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height - 20 - subColonyGrid.Height - 40);
            pFacilities.AddTab(Localizer.Token(333));
            pFacilities.AddTab("Statistics");
            pFacilities.AddTab("Trade");

            ButtonUpdateTimer = 1;
            LaunchAllTroops   = Button(subColonyGrid.Right - 175, subColonyGrid.Y - 5, "Launch All Troops", OnLaunchTroopsClicked);
            LaunchSingleTroop = Button(subColonyGrid.Right - LaunchAllTroops.Rect.Width - 185,
                                       subColonyGrid.Y - 5, "Launch Single Troop", OnLaunchSingleTroopClicked);

            CallTroops        = Button(subColonyGrid.Right - LaunchSingleTroop.Rect.Width - 365,
                                       subColonyGrid.Y - 5, "Call Troops", OnSendTroopsClicked);

            LaunchAllTroops.Tooltip   = Localizer.Token(1952);
            LaunchSingleTroop.Tooltip = Localizer.Token(1950);
            CallTroops.Tooltip        = Localizer.Token(1949);

            BuildShipyard = Button(pFacilities.Right - 175, pFacilities.Y - 5, "Build Shipyard", OnBuildShipyardClick);
            BuildStation  = Button(pFacilities.Right - LaunchAllTroops.Rect.Width - 185,
                                   pFacilities.Y - 5, "Build Station", OnBuildStationClick);

            BuildPlatform = Button(pFacilities.Right - LaunchSingleTroop.Rect.Width - 365,
                                   pFacilities.Y - 5, "Build Platform", OnBuildPlatformClick);

            BuildShipyard.Tooltip = Localizer.Token(1948);
            BuildStation.Tooltip  = Localizer.Token(1947);
            BuildPlatform.Tooltip = Localizer.Token(1946);
            BuildShipyard.Style   = ButtonStyle.BigDip;
            BuildStation.Style    = ButtonStyle.BigDip;
            BuildPlatform.Style   = ButtonStyle.BigDip;
            BuildPlatform.Visible = false;
            BuildStation.Visible  = false;
            BuildShipyard.Visible = false;
            UpdateGovOrbitalStats();
            UpdateButtons();

            FilterBuildableItems = Add(new UITextEntry(new Vector2(RightMenu.X + 80, RightMenu.Y + 17), ""));
            FilterBuildableItems.Font = Font12;
            FilterBuildableItems.ClickableArea = new Rectangle((int)RightMenu.X + 75, (int)RightMenu.Y+ 15, (int)RightMenu.Width - 400, 42);
            FilterFrame = Add(new Submenu(RightMenu.X + 70, RightMenu.Y-10, RightMenu.Width - 400, 42));
            Label(FilterFrame.Pos + new Vector2(-45,25), "Filter:", Font12, Color.White);
            var customStyle = new UIButton.StyleTextures("NewUI/icon_clear_filter", "NewUI/icon_clear_filter_hover");
            ClearFilter = Add(new UIButton(customStyle, new Vector2(17, 17), "")
            {
                Font    = Font12,
                Tooltip = GameText.ClearBuildableItemsFilter,
                OnClick = OnClearFilterClick,
                Pos     = new Vector2(FilterFrame.Pos.X + FilterFrame.Width + 10, FilterFrame.Pos.Y + 25)
            });

            BuildableTabs = new Submenu(RightMenu.X + 20, RightMenu.Y + 40, 
                                        RightMenu.Width - 40, 0.5f*(RightMenu.Height-40));
            BuildableTabs.OnTabChange = OnBuildableTabChanged;

            BuildableList = Add(new ScrollList2<BuildableListItem>(BuildableTabs));
            BuildableList.EnableItemHighlight = true;
            BuildableList.OnDoubleClick       = OnBuildableItemDoubleClicked;
            BuildableList.OnHovered           = OnBuildableHoverChange;
            BuildableList.OnDragOut           = OnBuildableListDrag;

            PlayerDesignsToggle = Add(new ToggleButton(new Vector2(BuildableTabs.Right - 270, BuildableTabs.Y),
                                                       ToggleButtonStyle.Grid, "SelectionBox/icon_grid"));
            PlayerDesignsToggle.IsToggled = GlobalStats.ShowAllDesigns;
            PlayerDesignsToggle.Tooltip = 2225;
            PlayerDesignsToggle.OnClick = OnPlayerDesignsToggleClicked;

            ResetBuildableTabs();

            var queue = new Submenu(RightMenu.X + 20, RightMenu.Y + 60 + BuildableTabs.Height, RightMenu.Width - 40, RightMenu.Height - BuildableTabs.Height - 75);
            queue.AddTab(Localizer.Token(337));

            ConstructionQueue = Add(new ScrollList2<ConstructionQueueScrollListItem>(queue));
            ConstructionQueue.EnableItemHighlight = true;
            ConstructionQueue.OnDragReorder = OnConstructionItemReorder;
            int iconSize = LowRes ? 80 : 128;
            int iconOffsetX = LowRes ? 100 : 148;
            int iconOffsetY = LowRes ? 0 : 25;

            PlanetIcon = new Rectangle((int)PlanetInfo.Right - iconOffsetX, 
                (int)PlanetInfo.Y + ((int)PlanetInfo.Height - iconOffsetY) / 2 - iconSize/2 + (LowRes ? 0 : 25), iconSize, iconSize);

            GridPos = new Rectangle(subColonyGrid.Rect.X + 10, subColonyGrid.Rect.Y + 30, subColonyGrid.Rect.Width - 20, subColonyGrid.Rect.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.X * width, GridPos.Y + planetGridSquare.Y * height, width, height);
            
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 20;

            if (p.Owner != null)
            {
                DetailInfo = p.Description;
                GovernorDetails = Add(new GovernorDetailsComponent(this, p, pDescription.Rect));
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            P.RefreshBuildingsWeCanBuildHere();
        }

        public float TerraformTargetFertility()
        {
            float fertilityOnBuild = P.BuildingList.Sum(b => b.MaxFertilityOnBuild);
            return (1 + fertilityOnBuild*Player.PlayerPreferredEnvModifier).LowerBound(0);
        }

        string TerraformPotential(out Color color)
        {
            color                       = Color.LightGreen;
            float targetFertility       = TerraformTargetFertility();
            int numUninhabitableTiles   = P.TilesList.Count(t => t.CanTerraform && !t.Biosphere);
            int numBiospheres           = P.TilesList.Count(t => t.BioCanTerraform);
            float minEstimatedMaxPop    = P.PotentialMaxPopBillionsFor(Player);
            float maxPopWithBiospheres  = P.PotentialMaxPopBillionsFor(Player, true);

            string text        = "Terraformer Process Stages:\n";
            string initialText = text;

            if (numUninhabitableTiles > 0)
                text += $"  * Make {numUninhabitableTiles} tiles habitable\n";

            if (P.Category != Player.data.PreferredEnv)
                text += $"  * Terraform the planet to {Player.data.PreferredEnv}.\n";

            if (numBiospheres > 0)
                text += $"  * Remove {numBiospheres} Biospheres.\n";

            if (targetFertility.AlmostZero())
            {
                text += "  * Max Fertility will be 0 due to negative effecting environment buildings.\n";
                color = Color.Red;
            }
            else if (targetFertility.Less(1))
            {
                text += $"  * Max Fertility will only be changed to {targetFertility} due to negative effecting environment buildings.\n";
            }
            else if (targetFertility.Greater(P.MaxFertilityFor(Player))) // better new fertility max
            {
                text += $"  * Max Fertility will be changed to {targetFertility}.\n";
            }

            if (minEstimatedMaxPop > maxPopWithBiospheres)
                text += $"  * Expected Max Population will be {(minEstimatedMaxPop).String(2)} Billion colonists.\n";

            if (text == initialText)
            {
                color = Color.Yellow;
                text = "Terraformers will have no effect on this planet.";
            }
            else
            {
                text += $"  * Current Maximum Terraformers: {P.TerraformerLimit}\n";
            }

            return text;
        }

        void OnSendTroopsClicked(UIButton b)
        {
            if (eui.empire.GetTroopShipForRebase(out Ship troopShip, P))
            {
                GameAudio.EchoAffirmative();
                troopShip.AI.OrderRebase(P, true);
                UpdateButtons();
            }
            else
                GameAudio.NegativeClick();
        }

        void OnLaunchTroopsClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsAreOnTile && pgs.LockOnPlayerTroop(out Troop troop) && troop.CanMove)
                {
                    play = true;
                    troop.Launch(pgs);
                    ClickedTroop = true;
                    DetailInfo = null;
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
            var potentialTroops = P.TroopsHere.Filter(t => t.Loyalty == EmpireManager.Player && t.CanMove);
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

        void ScrapAccepted()
        {
            if (ToScrap != null)
                P.ScrapBuilding(ToScrap);

            Update(0f);
        }
    }
}
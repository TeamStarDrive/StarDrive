using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public partial class ColonyScreen : PlanetScreen
    {
        public Planet P;
        readonly ToggleButton PlayerDesignsToggle;
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu1 LeftMenu;
        readonly Menu1 RightMenu;
        readonly Submenu PlanetInfo;
        readonly Submenu PDescription;
        readonly Submenu PStorage;
        readonly Submenu PFacilities;
        readonly Submenu BuildableTabs;
        readonly UITextEntry PlanetName = new UITextEntry();
        readonly Rectangle PlanetIcon;
        public EmpireUIOverlay Eui;
        readonly ToggleButton LeftColony;
        readonly ToggleButton RightColony;
        readonly UITextEntry FilterBuildableItems;
        readonly Rectangle GridPos;
        readonly Submenu SubColonyGrid;
        readonly Submenu FilterFrame;
        readonly UIButton ClearFilter;
        readonly UILabel BlockadeLabel;

        readonly ScrollList2<BuildableListItem> BuildableList;
        readonly ScrollList2<ConstructionQueueScrollListItem> ConstructionQueue;
        readonly DropDownMenu FoodDropDown;
        readonly DropDownMenu ProdDropDown;
        readonly ProgressBar FoodStorage;
        readonly ProgressBar ProdStorage;
        readonly Rectangle FoodStorageIcon;
        readonly Rectangle ProfStorageIcon;

        AssignLaborComponent AssignLabor;
        readonly ShipInfoOverlayComponent ShipInfoOverlay;
        readonly GovernorDetailsComponent GovernorDetails;

        object DetailInfo;
        Building ToScrap;
        PlanetGridSquare BioToScrap;

        public bool ClickedTroop;
        int EditHoverState;

        Rectangle EditNameButton;
        readonly SpriteFont Font8  = Fonts.Arial8Bold;
        readonly SpriteFont Font12 = Fonts.Arial12Bold;
        readonly SpriteFont Font14 = Fonts.Arial14Bold;
        readonly SpriteFont Font20 = Fonts.Arial20Bold;
        readonly SpriteFont TextFont;
        public readonly Empire Player = EmpireManager.Player;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI, int governorTabSelected = 0) : base(parent)
        {
            P = p;
            Eui = empUI;
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
            PlanetInfo = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20, (int)(0.4f * LeftMenu.Width), (int)(0.23f * (LeftMenu.Height - 80)));
            PlanetInfo.AddTab(title:326);
            PDescription = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 40 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));


            var labor = new RectF(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + PDescription.Height + 40,
                                  0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            AssignLabor = Add(new AssignLaborComponent(P, labor, useTitleFrame: true));

            PStorage = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + PDescription.Height + labor.H + 60, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            PStorage.AddTab(title:328);

            Vector2 blockadePos = new Vector2(PStorage.X + 20, PStorage.Y + 35);
            BlockadeLabel = Add(new UILabel(blockadePos, "Blockade!", Fonts.Pirulen16, Color.Red));
            FoodStorage = new ProgressBar(PStorage.X + 100, PStorage.Y + 25 + 0.33f*(PStorage.Height - 25), 0.4f*PStorage.Width, 18);
            FoodStorage.Max = p.Storage.Max;
            FoodStorage.Progress = p.FoodHere;
            FoodStorage.color = "green";
            FoodDropDown = new DropDownMenu(PStorage.X + 100 + 0.4f * PStorage.Width + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*PStorage.Width, 18);
            FoodDropDown.AddOption(Localizer.Token(329));
            FoodDropDown.AddOption(Localizer.Token(330));
            FoodDropDown.AddOption(Localizer.Token(331));
            FoodDropDown.ActiveIndex = (int)p.FS;
            var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
            FoodStorageIcon = new Rectangle((int)PStorage.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
            ProdStorage = new ProgressBar(PStorage.X + 100, PStorage.Y + 25 + 0.66f*(PStorage.Height - 25), 0.4f*PStorage.Width, 18);
            ProdStorage.Max = p.Storage.Max;
            ProdStorage.Progress = p.ProdHere;
            var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
            ProfStorageIcon = new Rectangle((int)PStorage.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
            ProdDropDown = new DropDownMenu(PStorage.X + 100 + 0.4f*PStorage.Width + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*PStorage.Width, 18);
            ProdDropDown.AddOption(Localizer.Token(329));
            ProdDropDown.AddOption(Localizer.Token(330));
            ProdDropDown.AddOption(Localizer.Token(331));
            ProdDropDown.ActiveIndex = (int)p.PS;

            SubColonyGrid = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            SubColonyGrid.AddTab(Localizer.Token(332));
            PFacilities = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, SubColonyGrid.Bottom + 20, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height - 20 - SubColonyGrid.Height - 40);
            PFacilities.AddTab(new LocalizedText(4198)); // Statistics
            //PFacilities.AddTab(new LocalizedText(4199).Text); // Trade
            PFacilities.AddTab(new LocalizedText(4200)); // Description

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
            if (p.Owner.isPlayer || Empire.Universe.Debug)
                BuildableList.OnDragOut = OnBuildableListDrag;

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
            if (p.Owner.isPlayer || Empire.Universe.Debug)
                ConstructionQueue.OnDragReorder = OnConstructionItemReorder;

            int iconSize = LowRes ? 80 : 128;
            int iconOffsetX = LowRes ? 100 : 148;
            int iconOffsetY = LowRes ? 0 : 25;

            PlanetIcon = new Rectangle((int)PlanetInfo.Right - iconOffsetX, 
                (int)PlanetInfo.Y + ((int)PlanetInfo.Height - iconOffsetY) / 2 - iconSize/2 + (LowRes ? 0 : 25), iconSize, iconSize);

            GridPos = new Rectangle(SubColonyGrid.Rect.X + 10, SubColonyGrid.Rect.Y + 30, SubColonyGrid.Rect.Width - 20, SubColonyGrid.Rect.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.X * width, GridPos.Y + planetGridSquare.Y * height, width, height);
            
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 20;

            if (p.Owner != null)
            {
                DetailInfo = p.Description;
                GovernorDetails = Add(new GovernorDetailsComponent(this, p, PDescription.Rect, governorTabSelected));
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
            int numVolcanoes            = P.TilesList.Count(t => t.VolcanoHere);
            int numUninhabitableTiles   = P.TilesList.Count(t => t.CanTerraform && !t.Biosphere);
            int numBiospheres           = P.TilesList.Count(t => t.BioCanTerraform);
            float minEstimatedMaxPop    = P.PotentialMaxPopBillionsFor(Player);
            float maxPopWithBiospheres  = P.PotentialMaxPopBillionsFor(Player, true);

            string text        = "Terraformer Process Stages:\n";
            string initialText = text;

            if (numUninhabitableTiles > 0)
                text += $"  * Remove {numVolcanoes} Volcano.\n";

            if (numUninhabitableTiles > 0)
                text += $"  * Make {numUninhabitableTiles} tiles habitable.\n";

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
                text += $"  * Expected Max Population is {(minEstimatedMaxPop).String(2)} Billion colonists.\n";

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

        void ScrapAccepted()
        {
            if (ToScrap != null)
                P.ScrapBuilding(ToScrap);

            Update(0f);
        }

        void ScrapBioAccepted()
        {
            if (BioToScrap != null)
                P.DestroyBioSpheres(BioToScrap, !BioToScrap.Building?.CanBuildAnywhere == true);

            Update(0f);
            BioToScrap = null;
        }

    }
}
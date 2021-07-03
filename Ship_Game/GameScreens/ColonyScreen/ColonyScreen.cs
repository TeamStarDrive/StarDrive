using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Graphics;

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
        readonly Submenu PStorage;
        readonly Submenu PFacilities;
        readonly Submenu BuildableTabs;
        readonly UITextEntry PlanetName;
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
        readonly UILabel StarvationLabel;
        readonly Rectangle PlanetShieldIconRect;
        readonly ProgressBar PlanetShieldBar;

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

        Rectangle EditNameButton;
        readonly Graphics.Font Font8  = Fonts.Arial8Bold;
        readonly Graphics.Font Font12 = Fonts.Arial12Bold;
        readonly Graphics.Font Font14 = Fonts.Arial14Bold;
        readonly Graphics.Font Font20 = Fonts.Arial20Bold;
        readonly Graphics.Font TextFont;
        public readonly Empire Player = EmpireManager.Player;

        UILabel TerraformTitle;
        UILabel TerraformStatusTitle;
        UILabel TerraformStatus;
        UILabel TerraformersHereTitle;
        UILabel TerraformersHere;
        UILabel VolcanoTerraformTitle;
        UILabel TileTerraformTitle;
        UILabel PlanetTerraformTitle;
        UILabel VolcanoTerraformDone;
        UILabel TileTerraformDone;
        UILabel PlanetTerraformDone;
        ProgressBar VolcanoTerraformBar;
        ProgressBar TileTerraformBar;
        ProgressBar PlanetTerraformBar;

        UILabel TargetFertilityTitle;
        UILabel TargetFertility;
        UILabel EstimatedMaxPopTitle;
        UILabel EstimatedMaxPop;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI, int governorTabSelected = 0) : base(parent)
        {
            P = p;
            Eui = empUI;
            empUI.Player.UpdateShipsWeCanBuild();
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
            PlanetInfo.AddTab(title:GameText.PlanetInfo);
            Submenu pDescription = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 40 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));


            var labor = new RectF(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + 40,
                                  0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            AssignLabor = Add(new AssignLaborComponent(P, labor, useTitleFrame: true));

            PStorage = new Submenu(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + labor.H + 60, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            PStorage.AddTab(title:GameText.Storage);

            Vector2 blockadePos = new Vector2(PStorage.X + 20, PStorage.Y + 35);
            BlockadeLabel = Add(new UILabel(blockadePos, "Blockade!", Fonts.Pirulen16, Color.Red));
            Vector2 starvationPos = new Vector2(PStorage.X + 200, PStorage.Y + 35);
            StarvationLabel = Add(new UILabel(starvationPos, "Starvation!", Fonts.Pirulen16, Color.Red));
            FoodStorage = new ProgressBar(PStorage.X + 100, PStorage.Y + 25 + 0.33f*(PStorage.Height - 25), 0.4f*PStorage.Width, 18);
            FoodStorage.Max = p.Storage.Max;
            FoodStorage.Progress = p.FoodHere;
            FoodStorage.color = "green";
            FoodDropDown = new DropDownMenu(PStorage.X + 100 + 0.4f * PStorage.Width + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*PStorage.Width, 18);
            FoodDropDown.AddOption(Localizer.Token(GameText.Store));
            FoodDropDown.AddOption(Localizer.Token(GameText.Import));
            FoodDropDown.AddOption(Localizer.Token(GameText.Export));
            FoodDropDown.ActiveIndex = (int)p.FS;
            var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
            FoodStorageIcon = new Rectangle((int)PStorage.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
            ProdStorage = new ProgressBar(PStorage.X + 100, PStorage.Y + 25 + 0.66f*(PStorage.Height - 25), 0.4f*PStorage.Width, 18);
            ProdStorage.Max = p.Storage.Max;
            ProdStorage.Progress = p.ProdHere;
            var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
            ProfStorageIcon = new Rectangle((int)PStorage.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
            ProdDropDown = new DropDownMenu(PStorage.X + 100 + 0.4f*PStorage.Width + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, 0.2f*PStorage.Width, 18);
            ProdDropDown.AddOption(Localizer.Token(GameText.Store));
            ProdDropDown.AddOption(Localizer.Token(GameText.Import));
            ProdDropDown.AddOption(Localizer.Token(GameText.Export));
            ProdDropDown.ActiveIndex = (int)p.PS;

            SubColonyGrid = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            SubColonyGrid.AddTab(Localizer.Token(GameText.Colony));
            PFacilities = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, SubColonyGrid.Bottom + 20, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height - 20 - SubColonyGrid.Height - 40);
            PFacilities.AddTab(GameText.Statistics2); // Statistics
            PFacilities.AddTab(GameText.Description); // Description
            //PFacilities.AddTab(GameText.Trade2); // Trade
            if (Player.data.Traits.TerraformingLevel > 0)
                PFacilities.AddTab(GameText.Terraforming); // Terraforming

            FilterBuildableItems = Add(new UITextEntry(new Vector2(RightMenu.X + 75, RightMenu.Y + 15), Font12, ""));
            FilterBuildableItems.AutoCaptureOnHover = true;
            
            FilterFrame = Add(new Submenu(RightMenu.X + 70, RightMenu.Y-10, RightMenu.Width - 400, 42));
            Label(FilterFrame.Pos + new Vector2(-45,25), "Filter:", Font12, Color.White);
            var customStyle = new UIButton.StyleTextures("NewUI/icon_clear_filter", "NewUI/icon_clear_filter_hover");
            ClearFilter = Add(new UIButton(customStyle, new Vector2(17, 17), "")
            {
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
            PlayerDesignsToggle.Tooltip = GameText.ToggleToDisplayOnlyPlayerdesigned;
            PlayerDesignsToggle.OnClick = OnPlayerDesignsToggleClicked;

            ResetBuildableTabs();

            var queue = new Submenu(RightMenu.X + 20, RightMenu.Y + 60 + BuildableTabs.Height, RightMenu.Width - 40, RightMenu.Height - BuildableTabs.Height - 75);
            queue.AddTab(Localizer.Token(GameText.ConstructionQueue));

            ConstructionQueue = Add(new ScrollList2<ConstructionQueueScrollListItem>(queue));
            ConstructionQueue.EnableItemHighlight = true;
            if (p.Owner.isPlayer || Empire.Universe.Debug)
                ConstructionQueue.OnDragReorder = OnConstructionItemReorder;

            int iconSize = LowRes ? 80 : 128;
            int iconOffsetX = LowRes ? 100 : 148;
            int iconOffsetY = LowRes ? 0 : 25;

            PlanetIcon = new Rectangle((int)PlanetInfo.Right - iconOffsetX, 
                (int)PlanetInfo.Y + ((int)PlanetInfo.Height - iconOffsetY) / 2 - iconSize/2 + (LowRes ? 0 : 25), iconSize, iconSize);

            Rectangle planetShieldBarRect = new Rectangle(PlanetIcon.X, PlanetInfo.Rect.Y + 4, PlanetIcon.Width, 20);
            PlanetShieldBar = new ProgressBar(planetShieldBarRect)
            {
                color = "blue"
            };

            PlanetShieldIconRect = new Rectangle(planetShieldBarRect.X - 30, planetShieldBarRect.Y-2, 20, 20);

            GridPos = new Rectangle(SubColonyGrid.Rect.X + 10, SubColonyGrid.Rect.Y + 30, SubColonyGrid.Rect.Width - 20, SubColonyGrid.Rect.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.X * width, GridPos.Y + planetGridSquare.Y * height, width, height);
            
            PlanetName = Add(new UITextEntry(p.Name));
            PlanetName.Color = Colors.Cream;
            PlanetName.MaxCharacters = 20;
            PlanetName.OnTextChanged = OnPlanetNameChanged;
            PlanetName.OnTextSubmit = OnPlanetNameSubmit;

            if (p.Owner != null)
            {
                DetailInfo = p.Description;
                GovernorDetails = Add(new GovernorDetailsComponent(this, p, pDescription.Rect, governorTabSelected));
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            P.RefreshBuildingsWeCanBuildHere();
            CreateTerraformingDetails(new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35));
        }

        void CreateTerraformingDetails(Vector2 pos)
        {
            Font font    = LowRes ? Font8 : Font14;
            int spacing  = font.LineSpacing + 2;
            int barWidth = (int)(PFacilities.Width * 0.33f);

            TerraformTitle = Add(new UILabel(pos, $"Terraforming Operations - Level {P.Owner.data.Traits.TerraformingLevel}", LowRes ? Font12 : Font20, Color.White));
            TerraformTitle.Visible = false;

            Vector2 statusTitlePos       = new Vector2(pos.X, pos.Y + spacing*2);
            TerraformStatusTitle         = Add(new UILabel(statusTitlePos, "Status: ", font, Color.Gray));
            TerraformStatusTitle.Visible = false;

            float indent = font.MeasureString(TerraformStatusTitle.Text).X + 125;

            Vector2 statusPos       = new Vector2(pos.X + indent, pos.Y + spacing*2);
            TerraformStatus         = Add(new UILabel(statusPos, " ", font, Color.Gray));
            TerraformStatus.Visible = false;

            Vector2 numTerraformersTitlePos = new Vector2(pos.X, TerraformStatusTitle.Y + spacing);
            TerraformersHereTitle           = Add(new UILabel(numTerraformersTitlePos, "Terraformers:", font, Color.Gray));
            TerraformersHereTitle.Visible   = false;

            Vector2 numTerraformersPos    = new Vector2(pos.X + indent, numTerraformersTitlePos.Y);
            TerraformersHere              = Add(new UILabel(numTerraformersPos, " ", font, Color.White));
            TerraformersHereTitle.Visible = false;

            Vector2 terraVolcanoTitlePos  = new Vector2(pos.X, numTerraformersTitlePos.Y + spacing*2);
            VolcanoTerraformTitle         = Add(new UILabel(terraVolcanoTitlePos, " ", font, Color.Gray));
            VolcanoTerraformTitle.Visible = false;

            Vector2 terraVolcanoPos      = new Vector2(pos.X + indent, terraVolcanoTitlePos.Y);
            VolcanoTerraformDone         = Add(new UILabel(terraVolcanoPos, "Done", font, Color.Green));
            VolcanoTerraformDone.Visible = false;

            Rectangle terraVolcanoRect = new Rectangle((int)terraVolcanoPos.X, (int)terraVolcanoPos.Y, barWidth, 20);
            VolcanoTerraformBar        = new ProgressBar(terraVolcanoRect)
            {
                Max            = 100,
                DrawPercentage = true
            };

            Vector2 terraTileTitlePos     = new Vector2(pos.X, terraVolcanoTitlePos.Y + spacing);
            TileTerraformTitle            = Add(new UILabel(terraTileTitlePos, " ", font, Color.Gray));
            TileTerraformTitle.Visible    = false;

            Vector2 terraTilePos      = new Vector2(pos.X + indent, terraTileTitlePos.Y);
            TileTerraformDone         = Add(new UILabel(terraTilePos, "Done", font, Color.Green));
            TileTerraformDone.Visible = false;

            Rectangle terraTileRect = new Rectangle((int)terraTilePos.X, (int)terraTilePos.Y, barWidth, 20);
            TileTerraformBar        = new ProgressBar(terraTileRect)
            {
                Max            = 100,
                color          = "green",
                DrawPercentage = true
            };

            Vector2 terraPlanetTitlePos   = new Vector2(pos.X, terraTileTitlePos.Y + spacing);
            PlanetTerraformTitle          = Add(new UILabel(terraPlanetTitlePos, "Planet:", font, Color.Gray));
            PlanetTerraformTitle.Visible  = false;

            Vector2 terraPlanetPos      = new Vector2(pos.X + indent, terraPlanetTitlePos.Y);
            PlanetTerraformDone         = Add(new UILabel(terraPlanetPos, "Done", font, Color.Green));
            PlanetTerraformDone.Visible = false;

            Rectangle terraPlanetRect = new Rectangle((int)terraPlanetPos.X, (int)terraPlanetPos.Y, barWidth, 20);
            PlanetTerraformBar        = new ProgressBar(terraPlanetRect)
            {
                Max            = 100,
                color          = "blue",
                DrawPercentage = true
            };

            Vector2 targetFertilityTitlePos = new Vector2(pos.X, terraPlanetTitlePos.Y + spacing * 2);
            TargetFertilityTitle            = Add(new UILabel(targetFertilityTitlePos, "Target Fertility:", font, Color.Gray));

            Vector2 targetFertilityPos = new Vector2(pos.X + indent, targetFertilityTitlePos.Y);
            TargetFertility            = Add(new UILabel(targetFertilityPos, "", font, Color.LightGreen));

            Vector2 estimatedMaxPopTitlePos = new Vector2(pos.X, targetFertilityTitlePos.Y + spacing);
            EstimatedMaxPopTitle            = Add(new UILabel(estimatedMaxPopTitlePos, "Estimated Population:", font, Color.Gray));

            Vector2 estimatedMaxPopPos = new Vector2(pos.X + indent, estimatedMaxPopTitlePos.Y);
            EstimatedMaxPop            = Add(new UILabel(estimatedMaxPopPos, "", font, Color.Green));
        }

        void OnPlanetNameSubmit(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                int ringnum = 1 + P.ParentSystem.RingList.IndexOf(r => r.planet == P);
                P.Name = string.Concat(P.ParentSystem.Name, " ", RomanNumerals.ToRoman(ringnum));
                PlanetName.Reset(P.Name);
            }
            else
            {
                P.Name = name;
            }
        }

        void OnPlanetNameChanged(string name)
        {
            P.Name = name;
        }

        public float TerraformTargetFertility()
        {
            float fertilityOnBuild = P.BuildingList.Sum(b => b.MaxFertilityOnBuild);
            return (1 + fertilityOnBuild*Player.PlayerPreferredEnvModifier).LowerBound(0);
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

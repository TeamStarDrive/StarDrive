using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Graphics;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.UI;
using System;
using SDUtils;

namespace Ship_Game
{
    public partial class ColonyScreen : PlanetScreen
    {
        readonly ToggleButton PlayerDesignsToggle;
        readonly Menu2 TitleBar;
        readonly Vector2 TitlePos;
        readonly Menu1 LeftMenu;
        readonly Menu1 RightMenu;
        readonly Submenu PlanetInfo;
        readonly Submenu PStorage;
        readonly Submenu PFacilities;
        readonly UITextEntry PlanetName;
        readonly Rectangle PlanetIcon;
        public EmpireUIOverlay Eui;
        readonly ToggleButton LeftColony;
        readonly ToggleButton RightColony;
        readonly UITextEntry FilterBuildableItems;
        readonly Rectangle GridPos;
        readonly Submenu SubColonyGrid;
        readonly UILabel BlockadeLabel;
        readonly UILabel StarvationLabel;
        readonly Rectangle PlanetShieldIconRect;
        readonly ProgressBar PlanetShieldBar;
        readonly UILabel FilterBuildableItemsLabel;
        
        readonly SubmenuScrollList<BuildableListItem> BuildableTabs;
        readonly ScrollList<BuildableListItem> BuildableList;
        readonly ScrollList<ConstructionQueueScrollListItem> ConstructionQueue;
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
        readonly Font Font8  = Fonts.Arial8Bold;
        readonly Font Font12 = Fonts.Arial12Bold;
        readonly Font Font14 = Fonts.Arial14Bold;
        readonly Font Font20 = Fonts.Arial20Bold;
        readonly Font TextFont;

        UILabel TradeTitle;
        UILabel IncomingTradeTitle;
        UILabel OutgoingTradeTitle;
        UILabel ManualImportTitle;
        UILabel ManualExportTitle;
        UIPanel IncomingFoodPanel;
        UIPanel IncomingProdPanel;
        UIPanel IncomingColoPanel;
        UIPanel OutgoingFoodPanel;
        UIPanel OutgoingProdPanel;
        UIPanel OutgoingColoPanel;
        ProgressBar IncomingFoodBar;
        ProgressBar IncomingProdBar;
        ProgressBar IncomingColoBar;
        ProgressBar OutgoingFoodBar;
        ProgressBar OutgoingProdBar;
        ProgressBar OutgoingColoBar;
        UILabel IncomingFoodAmount;
        UILabel IncomingProdAmount;
        UILabel IncomingColoAmount;
        FloatSlider ImportFoodSlotSlider;
        FloatSlider ImportProdSlotSlider;
        FloatSlider ImportColoSlotSlider;
        FloatSlider ExportFoodSlotSlider;
        FloatSlider ExportProdSlotSlider;
        FloatSlider ExportColoSlotSlider;

        UILabel TerraformTitle;
        UILabel TerraformStatusTitle;
        UILabel TerraformStatus;
        UILabel TerraformersHereTitle;
        UILabel TerraformersHere;
        UILabel TerrainTerraformTitle;
        UILabel TileTerraformTitle;
        UILabel PlanetTerraformTitle;
        UILabel VolcanoTerraformDone;
        UILabel TileTerraformDone;
        UILabel PlanetTerraformDone;
        ProgressBar TerrainTerraformBar;
        ProgressBar TileTerraformBar;
        ProgressBar PlanetTerraformBar;

        UILabel TargetFertilityTitle;
        UILabel TargetFertility;
        UILabel EstimatedMaxPopTitle;
        UILabel EstimatedMaxPop;

        UILabel DysonSwarmTypeTitle;
        UIButton DysonSwarmStartButton;
        UIButton DysonSwarmKillButton;
        UIPanel DysonSwarmControllerPanel;
        UIPanel DysonSwarmPanel;
        ProgressBar DysonSwarmControllerProgress;
        ProgressBar DysonSwarmProgress;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI, 
            int governorTabSelected = 0, int facilitiesTabSelected = 0)
            : base(parent, p)
        {
            Eui = empUI;
            Player.UpdateShipsWeCanBuild();
            TextFont = LowRes ? Font8 : Font12;

            var titleBar = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleBar);

            LeftColony = Add(new ToggleButton(titleBar.X + 25, titleBar.Y + 24, ToggleButtonStyle.ArrowLeft));
            LeftColony.Tooltip = GameText.ViewPreviousColony;
            LeftColony.OnClick = b => OnChangeColony(-1);

            RightColony = Add(new ToggleButton(titleBar.Right - 39, titleBar.Y + 24, ToggleButtonStyle.ArrowRight));
            RightColony.Tooltip = GameText.ViewNextColony;
            RightColony.OnClick = b => OnChangeColony(+1);

            TitlePos = new Vector2(titleBar.X + titleBar.Width / 2 - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, titleBar.Y + titleBar.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            LeftMenu = new Menu1(2, titleBar.Y + titleBar.Height + 5, titleBar.Width, ScreenHeight - (titleBar.Y + titleBar.Height) - 7);
            RightMenu = new Menu1(titleBar.Right + 10, titleBar.Y, ScreenWidth / 3 - 15, ScreenHeight - titleBar.Y - 2);
            Add(new CloseButton(RightMenu.Right - 52, RightMenu.Y + 22));

            RectF planetInfoR = new(LeftMenu.X + 20, LeftMenu.Y + 20, 
                                    (int)(0.4f * LeftMenu.Width),
                                    (int)(0.23f * (LeftMenu.Height - 80)));
            PlanetInfo = new(planetInfoR, GameText.PlanetInfo);
            Submenu pDescription = new(LeftMenu.X + 20, LeftMenu.Y + 40 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            var labor = new RectF(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + 40,
                                  0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            AssignLabor = Add(new AssignLaborComponent(P, labor, useTitleFrame: true));

            RectF pStorageR = new(LeftMenu.X + 20, LeftMenu.Y + 20 + PlanetInfo.Height + pDescription.Height + labor.H + 60, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            PStorage = new(pStorageR, GameText.Storage);

            Vector2 blockadePos = new Vector2(PStorage.X + 20, PStorage.Y + 35);
            BlockadeLabel = Add(new UILabel(blockadePos, Localizer.Token(GameText.Blockade2), Fonts.Pirulen16, Color.Red));
            BlockadeLabel.Tooltip = GameText.IndicatesThatThisPlanetIs;
            
            Vector2 starvationPos = new Vector2(PStorage.X + 200, PStorage.Y + 35);
            StarvationLabel = Add(new UILabel(starvationPos, Localizer.Token(GameText.Starvation), Fonts.Pirulen16, Color.Red));
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

            RectF subColonyR = new(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y, 
                                   LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            SubColonyGrid = new(subColonyR, GameText.Colony);

            RectF pFacilitiesR = new(LeftMenu.X + 20 + PlanetInfo.Width + 20,
                                     SubColonyGrid.Bottom + 20,
                                     LeftMenu.Width - 60 - PlanetInfo.Width,
                                     LeftMenu.Height - 20 - SubColonyGrid.Height - 40);

            PFacilities = base.Add(new Submenu(pFacilitiesR));
            PopulatePfacilitieTabs();
            PFacilities.OnTabChange = OnPFacilitiesTabChange;
            // FB - sticky tab selection on colony change via arrows
            if (facilitiesTabSelected < PFacilities.Tabs.Count)
                PFacilities.SelectedIndex = facilitiesTabSelected;

            var filterBgRect = new RectF(RightMenu.X + 70, RightMenu.Y + 15, RightMenu.Width - 400, 20);
            var filterRect = new RectF(filterBgRect.X + 5, filterBgRect.Y, filterBgRect.W, filterBgRect.H);
            FilterBuildableItems = Add(new UITextEntry(filterRect, Font12, ""));
            FilterBuildableItems.AutoCaptureOnHover = true;
            FilterBuildableItems.Background = new Submenu(filterBgRect);
            Vector2 filterLabelPos = new Vector2(RightMenu.X + 25, filterRect.Y+2);
            FilterBuildableItemsLabel = Add(new UILabel(filterLabelPos, "Filter:", Font12, Color.Gray));
            
            var customStyle = new UIButton.StyleTextures("NewUI/icon_clear_filter", "NewUI/icon_clear_filter_hover2");
            Add(new UIButton(customStyle, new Vector2(17, 17), "")
            {
                Tooltip = GameText.ClearBuildableItemsFilter,
                OnClick = OnClearFilterClick,
                Pos     = new Vector2(filterRect.Right + 10, filterRect.Y + 3)
            });

            RectF buildableR = new(RightMenu.X + 20, RightMenu.Y + 40, 
                                   RightMenu.Width - 40, 0.5f*(RightMenu.Height-40));
            BuildableTabs = base.Add(new SubmenuScrollList<BuildableListItem>(buildableR, BuildingsTabText));
            BuildableTabs.OnTabChange = OnBuildableTabChanged;

            BuildableList = BuildableTabs.List;
            BuildableList.EnableItemHighlight = true;
            BuildableList.OnDoubleClick = OnBuildableItemDoubleClicked;
            BuildableList.OnHovered = OnBuildableHoverChange;

            if (p.OwnerIsPlayer || p.Universe.Debug)
                BuildableList.OnDragOut = OnBuildableListDrag;

            PlayerDesignsToggle = Add(new ToggleButton(new Vector2(BuildableTabs.Right - 270, BuildableTabs.Y-1),
                                                       ToggleButtonStyle.Grid, "SelectionBox/icon_grid"));
            PlayerDesignsToggle.IsToggled = !Universe.P.ShowAllDesigns;
            PlayerDesignsToggle.Tooltip = GameText.ToggleToDisplayOnlyPlayerdesigned;
            PlayerDesignsToggle.OnClick = OnPlayerDesignsToggleClicked;
            ResetBuildableTabs();

            float queueBottom = RightMenu.Bottom - 20;
            float queueTop = BuildableTabs.Bottom + 10;
            RectF queueR = new(RightMenu.X + 20, queueTop, RightMenu.Width - 40, queueBottom - queueTop);
            var queue = base.Add(new SubmenuScrollList<ConstructionQueueScrollListItem>(queueR, GameText.ConstructionQueue));

            ConstructionQueue = queue.List;
            ConstructionQueue.EnableItemHighlight = true;
            ConstructionQueue.OnHovered = OnConstructionItemHovered;
            if (p.OwnerIsPlayer || p.Universe.Debug)
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
                GovernorDetails = Add(new GovernorDetailsComponent(this, (UniverseScreen)parent, p, pDescription.RectF, governorTabSelected));
            }
            else
            {
                p.Universe.Screen.LookingAtPlanet = false;
            }

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this, Universe));
            P.RefreshBuildingsWeCanBuildHere();
            Vector2 detailsVector = new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35);
            CreateTradeDetails(detailsVector);
            CreateTerraformingDetails(detailsVector);
            CreateDysonSwarmDetails(detailsVector);
        }

        void PopulatePfacilitieTabs()
        {
            PFacilities.ClearTabs();
            PFacilities.AddTab(GameText.Statistics2);
            PFacilities.AddTab(GameText.Description);
            PFacilities.AddTab(GameText.Trade2);

            if (Player.data.Traits.TerraformingLevel > 0 || P.Terraformable)
                PFacilities.AddTab(GameText.BB_Tech_Terraforming_Name);

            if (DysonSwarmTabAllowed)
                PFacilities.AddTab(GameText.DysonSwarm);
        }

        void AddLabel(ref UILabel uiLabel, Vector2 pos, LocalizedText text, Font font, Color color)
        {
            if (uiLabel == null)
                uiLabel = Add(new UILabel(pos, text, font, color));

            uiLabel.Visible = false;
        }

        void AddButton(ref UIButton button, Vector2 pos, LocalizedText text, ButtonStyle buttonStyle, LocalizedText tip) 
        {
            if (button == null)
                button = Add(new UIButton(buttonStyle, pos, text));

            button.Visible = false;
            button.Tooltip = tip;
        }

        void AddPanel(ref UIPanel panel, Vector2 pos, string texPath, int size, LocalizedText tip)
        {
            if (panel == null)
                panel = Add(new UIPanel(pos, ResourceManager.Texture(texPath)));

            panel.Size    = new Vector2(size, size);
            panel.Visible = false;
            panel.Tooltip = tip;
        }

        void AddProgressBar(ref ProgressBar bar, Rectangle rect, float max, string colorStr, bool percentage = false)
        {
            if (bar == null)
            {
                bar = new ProgressBar(rect)
                {
                    Max            = max,
                    color          = colorStr,
                    DrawPercentage = percentage
                };
            }
        }

        void AddUiSlider(ref FloatSlider slider, Rectangle rect, LocalizedText text, float min, float max, float value, LocalizedText tip)
        {
            if (slider == null)
            {
                slider            = Slider(rect, text, min, max, value);
                slider.Visible    = false;
                slider.ZeroString = Localizer.Token(GameText.Automatic);
                slider.Tip        = tip;
            }
        }

        void CreateDysonSwarmDetails(Vector2 pos)
        {
            Font font = LowRes ? Font8 : Font14;
            int spacing = font.LineSpacing + 10;
            int barWidth = (int)(PFacilities.Width * 0.5f);
            float indent = 30;
            float indentTradeAmount = indent + barWidth + 5;

            AddLabel(ref DysonSwarmTypeTitle, pos, P.System.DysonSwarm.DysonSwarmTypeTitle, font, Color.White);

            Vector2 buttonsPos = new Vector2(pos.X, pos.Y + spacing);
            AddButton(ref DysonSwarmStartButton, buttonsPos, GameText.BuildDysonSwarm, ButtonStyle.Default, GameText.BuildDysonSwarmTip);
            AddButton(ref DysonSwarmKillButton, buttonsPos, GameText.KillDysonSwarm, ButtonStyle.Military, GameText.KillDysonSwarmTip);

            // Controller Progress
            Vector2 controllerProgressPos = new Vector2(pos.X, buttonsPos.Y + spacing + 3);
            AddPanel(ref DysonSwarmControllerPanel, controllerProgressPos, "NewUI/icon_food", font.LineSpacing, GameText.DysonSwarmControllerProgressTip);
            Rectangle dysonSwarmControllerProgressRect = new Rectangle((int)(controllerProgressPos.X + indent), 
                                                                       (int)controllerProgressPos.Y, 
                                                                       barWidth, 20);
            AddProgressBar(ref DysonSwarmControllerProgress, dysonSwarmControllerProgressRect, 100, "green", percentage: true);
            
            // Swarm Progress
            Vector2 swarmProgressPos = new Vector2(controllerProgressPos.X, controllerProgressPos.Y + spacing + 3);
            AddPanel(ref DysonSwarmPanel, swarmProgressPos, "NewUI/icon_food", font.LineSpacing, GameText.DysonSwarmProgressTip);
            Rectangle dysonSwarmProgressRect = new Rectangle((int)(swarmProgressPos.X + indent),
                                                             (int)swarmProgressPos.Y,
                                                             barWidth, 20);
            AddProgressBar(ref DysonSwarmProgress, dysonSwarmProgressRect, P.System.DysonSwarm.RequiredSwarmSats, "yellow");
        }

        void CreateTradeDetails(Vector2 pos)
        {
            Font font       = LowRes ? Font8 : Font14;
            int spacing     = font.LineSpacing + 10;
            int barWidth    = (int)(PFacilities.Width * 0.33f);
            int sliderWidth = (int)(PFacilities.Width * 0.33f);
            int sliderSize  = 30;
            float indent    = 30;
            float indentTradeAmount = indent + barWidth + 5;
            float indentSlider      = indentTradeAmount + 60;

            AddLabel(ref TradeTitle, pos, GameText.ColonyTrade, LowRes ? Font14 : Font20, Color.White);

            Vector2 incomingTitlePos = new Vector2(pos.X, pos.Y + spacing * (LowRes ? 1 : 1.5f));
            AddLabel(ref IncomingTradeTitle, incomingTitlePos, GameText.IncomingFreighters, font, Color.Gray);

            Vector2 manualImportTitlePos = new Vector2(pos.X + indentSlider - 10, incomingTitlePos.Y);
            AddLabel(ref ManualImportTitle, manualImportTitlePos, Localizer.Token(GameText.ManualImport), font, Color.Gray);

            // Incoming food
            Vector2 incomingFoodPos = new Vector2(pos.X, incomingTitlePos.Y + spacing + 3);
            AddPanel(ref IncomingFoodPanel, incomingFoodPos, "NewUI/icon_food", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle incomingFoodRect = new Rectangle((int)(incomingFoodPos.X + indent), (int)incomingFoodPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingFoodBar, incomingFoodRect, P.FoodImportSlots, "green");
            Vector2 incomingFoodAmountPos = new Vector2(pos.X + indentTradeAmount, incomingFoodPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingFoodAmount, incomingFoodAmountPos, "", Font8, Color.White);
            Rectangle importFoodSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingFoodPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportFoodSlotSlider, importFoodSlotsRect, "", 0, 20, P.ManualFoodImportSlots, GameText.ManualTradeSlotTip);

            // Incoming Prod
            Vector2 incomingProdPos = new Vector2(pos.X, incomingFoodPos.Y + spacing);
            AddPanel(ref IncomingProdPanel, incomingProdPos, "NewUI/icon_production", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle incomingProdRect = new Rectangle((int)(incomingProdPos.X + indent), (int)incomingProdPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingProdBar, incomingProdRect, P.ProdImportSlots, "brown");
            Vector2 incomingProdAmountPos = new Vector2(pos.X + indentTradeAmount, incomingProdPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingProdAmount, incomingProdAmountPos, "", Font8, Color.White);
            Rectangle importProdSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingProdPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportProdSlotSlider, importProdSlotsRect, "", 0, 20, P.ManualProdImportSlots, GameText.ManualTradeSlotTip);

            // Incoming Colonists
            Vector2 incomingColoPos = new Vector2(pos.X, incomingProdPos.Y + spacing);
            AddPanel(ref IncomingColoPanel, incomingColoPos, "UI/icon_pop", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle incomingColoRect = new Rectangle((int)(incomingColoPos.X + indent), (int)incomingColoPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingColoBar, incomingColoRect, P.ColonistsImportSlots, "blue");
            Vector2 incomingColoAmountPos = new Vector2(pos.X + indentTradeAmount, incomingColoPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingColoAmount, incomingColoAmountPos, "", Font8, Color.White);
            Rectangle importColoSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingColoPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportColoSlotSlider, importColoSlotsRect, "", 0, 20, P.ManualColoImportSlots, GameText.ManualTradeSlotTip);

            Vector2 outgoingTitlePos = new Vector2(pos.X, incomingColoAmountPos.Y + spacing * (LowRes ? 1 : 1.5f));
            AddLabel(ref OutgoingTradeTitle, outgoingTitlePos, GameText.OutgoingFreighters, font, Color.Gray);

            Vector2 manualExportTitlePos = new Vector2(pos.X + indentSlider - 10, outgoingTitlePos.Y);
            AddLabel(ref ManualExportTitle, manualExportTitlePos, GameText.ManualExport, font, Color.Gray);

            // Outgoing food
            Vector2 outgoingFoodPos = new Vector2(pos.X, outgoingTitlePos.Y + spacing + 3);
            AddPanel(ref OutgoingFoodPanel, outgoingFoodPos, "NewUI/icon_food", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle outgoingFoodRect = new Rectangle((int)(outgoingFoodPos.X + indent), (int)outgoingFoodPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingFoodBar, outgoingFoodRect, P.FoodExportSlots, "green");
            Rectangle exportFoodSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingFoodPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportFoodSlotSlider, exportFoodSlotsRect, "", 0, 25, P.ManualFoodExportSlots, GameText.ManualTradeSlotTip);

            // Outgoing Prod
            Vector2 outgoingProdPos = new Vector2(pos.X, outgoingFoodPos.Y + spacing);
            AddPanel(ref OutgoingProdPanel, outgoingProdPos, "NewUI/icon_production", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle outgoingProdRect = new Rectangle((int)(outgoingProdPos.X + indent), (int)outgoingProdPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingProdBar, outgoingProdRect, P.ProdExportSlots, "brown");
            Rectangle exportProdSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingProdPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportProdSlotSlider, exportProdSlotsRect, "", 0, 25, P.ManualProdExportSlots, GameText.ManualTradeSlotTip);

            // Outgoing Colonists
            Vector2 outgoingColoPos = new Vector2(pos.X, outgoingProdPos.Y + spacing);
            AddPanel(ref OutgoingColoPanel, outgoingColoPos, "UI/icon_pop", font.LineSpacing, GameText.IncomingOutGoingTip);
            Rectangle outgoingColoRect = new Rectangle((int)(outgoingColoPos.X + indent), (int)outgoingColoPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingColoBar, outgoingColoRect, P.ColonistsExportSlots, "blue");
            Rectangle exportColoSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingColoPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportColoSlotSlider, exportColoSlotsRect, "", 0, 25, P.ManualColoExportSlots, GameText.ManualTradeSlotTip);
        }

        void CreateTerraformingDetails(Vector2 pos)
        {
            Font font    = LowRes ? Font8 : Font14;
            int spacing  = font.LineSpacing + 2;
            int barWidth = (int)(PFacilities.Width * 0.33f);

            AddLabel(ref TerraformTitle, pos, "", LowRes ? Font14 : Font20, Color.White);

            Vector2 statusTitlePos = new Vector2(pos.X, pos.Y + spacing*2);
            AddLabel(ref TerraformStatusTitle, statusTitlePos, GameText.TerraformingStatus, font, Color.White);

            float indent = font.MeasureString(TerraformStatusTitle.Text).X + 125;

            Vector2 statusPos = new Vector2(pos.X + indent, pos.Y + spacing*2);
            AddLabel(ref TerraformStatus, statusPos, " ", font, Color.Gray);

            Vector2 numTerraformersTitlePos = new Vector2(pos.X, TerraformStatusTitle.Y + spacing);
            AddLabel(ref TerraformersHereTitle, numTerraformersTitlePos, GameText.TerraformersHere, font, Color.Gray);

            Vector2 numTerraformersPos = new Vector2(pos.X + indent, numTerraformersTitlePos.Y);
            AddLabel(ref TerraformersHere, numTerraformersPos, " ", font, Color.White);

            Vector2 terraVolcanoTitlePos = new Vector2(pos.X, numTerraformersTitlePos.Y + spacing*2);
            AddLabel(ref TerrainTerraformTitle, terraVolcanoTitlePos, " ", font, Color.Gray);

            Vector2 terraVolcanoPos = new Vector2(pos.X + indent, terraVolcanoTitlePos.Y);
            AddLabel(ref VolcanoTerraformDone, terraVolcanoPos, GameText.TerraformersDone, font, Color.Green);

            Rectangle terraVolcanoRect = new Rectangle((int)terraVolcanoPos.X, (int)terraVolcanoPos.Y, barWidth, 20);
            AddProgressBar(ref TerrainTerraformBar, terraVolcanoRect, 100, "brown", percentage: true);

            Vector2 terraTileTitlePos = new Vector2(pos.X, terraVolcanoTitlePos.Y + spacing);
            AddLabel(ref TileTerraformTitle, terraTileTitlePos, " ", font, Color.Gray);

            Vector2 terraTilePos = new Vector2(pos.X + indent, terraTileTitlePos.Y);
            AddLabel(ref TileTerraformDone, terraTilePos, GameText.TerraformersDone, font, Color.Green);

            Rectangle terraTileRect = new Rectangle((int)terraTilePos.X, (int)terraTilePos.Y, barWidth, 20);
            AddProgressBar(ref TileTerraformBar, terraTileRect, 100, "green", percentage: true);

            Vector2 terraPlanetTitlePos = new Vector2(pos.X, terraTileTitlePos.Y + spacing);
            AddLabel(ref PlanetTerraformTitle, terraPlanetTitlePos, GameText.TerraformPlanet, font, Color.Gray);

            Vector2 terraPlanetPos = new Vector2(pos.X + indent, terraPlanetTitlePos.Y);
            AddLabel(ref PlanetTerraformDone, terraPlanetPos, GameText.TerraformersDone, font, Color.Green);

            Rectangle terraPlanetRect = new Rectangle((int)terraPlanetPos.X, (int)terraPlanetPos.Y, barWidth, 20);
            AddProgressBar(ref PlanetTerraformBar, terraPlanetRect, 100, "blue", percentage: true);

            Vector2 targetFertilityTitlePos = new Vector2(pos.X, terraPlanetTitlePos.Y + spacing * 2);
            AddLabel(ref TargetFertilityTitle, targetFertilityTitlePos, GameText.TerraformTargetFert, font, Color.Gray);

            Vector2 targetFertilityPos = new Vector2(pos.X + indent, targetFertilityTitlePos.Y);
            AddLabel(ref TargetFertility, targetFertilityPos, "", font, Color.Green);

            Vector2 estimatedMaxPopTitlePos = new Vector2(pos.X, targetFertilityTitlePos.Y + spacing);
            AddLabel(ref EstimatedMaxPopTitle, estimatedMaxPopTitlePos, GameText.TerraformEsPop, font, Color.Gray);

            Vector2 estimatedMaxPopPos = new Vector2(pos.X + indent, estimatedMaxPopTitlePos.Y);
            AddLabel(ref EstimatedMaxPop, estimatedMaxPopPos, "", font, Color.Green);
        }

        void OnPlanetNameSubmit(string name)
        {
            P.Name = name;
            if (string.IsNullOrWhiteSpace(P.Name))
            {
                P.Name = P.GetDefaultPlanetName();
                PlanetName.Reset(P.Name);
            }
        }

        void OnPlanetNameChanged(string name)
        {
            P.Name = name;
        }

        public float TerraformTargetFertility()
        {
            float fertilityOnBuild = P.SumBuildings(b => b.MaxFertilityOnBuild);
            return (1 + fertilityOnBuild*Player.PlayerPreferredEnvModifier).LowerBound(0);
        }

        void ScrapAccepted()
        {
            if (ToScrap != null)
            {
                P.ScrapBuilding(ToScrap);
                P.RefreshBuildingsWeCanBuildHere();
                ToScrap = null;
            }
        }

        void ScrapBioAccepted()
        {
            if (BioToScrap != null)
            {
                P.DestroyBioSpheres(BioToScrap, !BioToScrap.Building?.CanBuildAnywhere == true);
                P.RefreshBuildingsWeCanBuildHere();
                BioToScrap = null;
            }
        }
    }
}

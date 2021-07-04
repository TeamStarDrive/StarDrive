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

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI, 
            int governorTabSelected = 0, int facilitiesTabSelected = 0) : base(parent)
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
            BlockadeLabel = Add(new UILabel(blockadePos, Localizer.Token(GameText.Blockade2), Fonts.Pirulen16, Color.Red));
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

            SubColonyGrid = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            SubColonyGrid.AddTab(Localizer.Token(GameText.Colony));
            PFacilities = new Submenu(LeftMenu.X + 20 + PlanetInfo.Width + 20, SubColonyGrid.Bottom + 20, LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height - 20 - SubColonyGrid.Height - 40);
            PFacilities.AddTab(GameText.Statistics2); // Statistics
            PFacilities.AddTab(GameText.Description); // Description
            PFacilities.AddTab(GameText.Trade2); // Trade
            if (Player.data.Traits.TerraformingLevel > 0)
                PFacilities.AddTab(GameText.BB_Tech_Terraforming_Name); // Terraforming

            if (facilitiesTabSelected < PFacilities.Tabs.Count)
            {
                // FB - sticky tab selection on colony change via arrows
                PFacilitiesPlayerTabSelected =
                PFacilities.SelectedIndex    = facilitiesTabSelected;
            }

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
            Vector2 detailsVector = new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35);
            CreateTradeDetails(detailsVector);
            CreateTerraformingDetails(detailsVector);
        }

        void AddLabel(ref UILabel uiLabel, Vector2 pos, string text, Font font, Color color)
        {
            if (uiLabel == null)
                uiLabel = Add(new UILabel(pos, text, font, color));

            uiLabel.Visible = false;
        }

        void AddPanel(ref UIPanel panel, Vector2 pos, string texPath, int size, LocalizedText tip)
        {
            if (panel == null)
                panel = Add(new UIPanel(pos, ResourceManager.Texture(texPath)));

            panel.Size    = new Vector2(size, size);
            panel.Visible = false;
            panel.Tooltip = tip;
        }

        void AddProgressBar(ref ProgressBar bar, Rectangle rect, float max, string color, bool percentage = false)
        {
            if (bar == null)
            {
                bar = new ProgressBar(rect)
                {
                    Max            = max,
                    color          = color,
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
                slider.ZeroString = Localizer.Token(4329);
                slider.Tip        = tip;
            }
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

            AddLabel(ref TradeTitle, pos, Localizer.Token(4332), LowRes ? Font14 : Font20, Color.White);

            Vector2 incomingTitlePos = new Vector2(pos.X, pos.Y + spacing * (LowRes ? 1 : 1.5f));
            AddLabel(ref IncomingTradeTitle, incomingTitlePos, Localizer.Token(4330), font, Color.Gray);

            Vector2 manualImportTitlePos = new Vector2(pos.X + indentSlider - 10, incomingTitlePos.Y);
            AddLabel(ref ManualImportTitle, manualImportTitlePos, Localizer.Token(4333), font, Color.Gray);

            // Incoming food
            Vector2 incomingFoodPos = new Vector2(pos.X, incomingTitlePos.Y + spacing + 3);
            AddPanel(ref IncomingFoodPanel, incomingFoodPos, "NewUI/icon_food", font.LineSpacing, Localizer.Token(4335));
            Rectangle incomingFoodRect = new Rectangle((int)(incomingFoodPos.X + indent), (int)incomingFoodPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingFoodBar, incomingFoodRect, P.FoodImportSlots, "green");
            Vector2 incomingFoodAmountPos = new Vector2(pos.X + indentTradeAmount, incomingFoodPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingFoodAmount, incomingFoodAmountPos, "", Font8, Color.White);
            Rectangle importFoodSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingFoodPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportFoodSlotSlider, importFoodSlotsRect, "", 0, 20, P.ManualFoodImportSlots, Localizer.Token(4336));

            // Incoming Prod
            Vector2 incomingProdPos = new Vector2(pos.X, incomingFoodPos.Y + spacing);
            AddPanel(ref IncomingProdPanel, incomingProdPos, "NewUI/icon_production", font.LineSpacing, Localizer.Token(4335));
            Rectangle incomingProdRect = new Rectangle((int)(incomingProdPos.X + indent), (int)incomingProdPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingProdBar, incomingProdRect, P.ProdImportSlots, "brown");
            Vector2 incomingProdAmountPos = new Vector2(pos.X + indentTradeAmount, incomingProdPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingProdAmount, incomingProdAmountPos, "", Font8, Color.White);
            Rectangle importProdSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingProdPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportProdSlotSlider, importProdSlotsRect, "", 0, 20, P.ManualProdImportSlots, Localizer.Token(4336));

            // Incoming Colonists
            Vector2 incomingColoPos = new Vector2(pos.X, incomingProdPos.Y + spacing);
            AddPanel(ref IncomingColoPanel, incomingColoPos, "UI/icon_pop", font.LineSpacing, Localizer.Token(4335));
            Rectangle incomingColoRect = new Rectangle((int)(incomingColoPos.X + indent), (int)incomingColoPos.Y, barWidth, 20);
            AddProgressBar(ref IncomingColoBar, incomingColoRect, P.ColonistsImportSlots, "blue");
            Vector2 incomingColoAmountPos = new Vector2(pos.X + indentTradeAmount, incomingColoPos.Y + (LowRes ? 0 : 2));
            AddLabel(ref IncomingColoAmount, incomingColoAmountPos, "", Font8, Color.White);
            Rectangle importColoSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(incomingColoPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ImportColoSlotSlider, importColoSlotsRect, "", 0, 20, P.ManualColoImportSlots, Localizer.Token(4336));

            Vector2 outgoingTitlePos = new Vector2(pos.X, incomingColoAmountPos.Y + spacing * (LowRes ? 1 : 1.5f));
            AddLabel(ref OutgoingTradeTitle, outgoingTitlePos, Localizer.Token(4331), font, Color.Gray);

            Vector2 manualExportTitlePos = new Vector2(pos.X + indentSlider - 10, outgoingTitlePos.Y);
            AddLabel(ref ManualExportTitle, manualExportTitlePos, Localizer.Token(4334), font, Color.Gray);

            // Outgoing food
            Vector2 outgoingFoodPos = new Vector2(pos.X, outgoingTitlePos.Y + spacing + 3);
            AddPanel(ref OutgoingFoodPanel, outgoingFoodPos, "NewUI/icon_food", font.LineSpacing, Localizer.Token(4335));
            Rectangle outgoingFoodRect = new Rectangle((int)(outgoingFoodPos.X + indent), (int)outgoingFoodPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingFoodBar, outgoingFoodRect, P.FoodExportSlots, "green");
            Rectangle exportFoodSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingFoodPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportFoodSlotSlider, exportFoodSlotsRect, "", 0, 20, P.ManualFoodExportSlots, Localizer.Token(4336));

            // Outgoing Prod
            Vector2 outgoingProdPos = new Vector2(pos.X, outgoingFoodPos.Y + spacing);
            AddPanel(ref OutgoingProdPanel, outgoingProdPos, "NewUI/icon_production", font.LineSpacing, Localizer.Token(4335));
            Rectangle outgoingProdRect = new Rectangle((int)(outgoingProdPos.X + indent), (int)outgoingProdPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingProdBar, outgoingProdRect, P.ProdExportSlots, "brown");
            Rectangle exportProdSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingProdPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportProdSlotSlider, exportProdSlotsRect, "", 0, 20, P.ManualProdExportSlots, Localizer.Token(4336));

            // Outgoing Colonists
            Vector2 outgoingColoPos = new Vector2(pos.X, outgoingProdPos.Y + spacing);
            AddPanel(ref OutgoingColoPanel, outgoingColoPos, "UI/icon_pop", font.LineSpacing, Localizer.Token(4335));
            Rectangle outgoingColoRect = new Rectangle((int)(outgoingColoPos.X + indent), (int)outgoingColoPos.Y, barWidth, 20);
            AddProgressBar(ref OutgoingColoBar, outgoingColoRect, P.ColonistsExportSlots, "blue");
            Rectangle exportColoSlotsRect = new Rectangle((int)(pos.X + indentSlider), (int)(outgoingColoPos.Y-12), sliderWidth, sliderSize);
            AddUiSlider(ref ExportColoSlotSlider, exportColoSlotsRect, "", 0, 20, P.ManualColoExportSlots, Localizer.Token(4336));
        }

        void CreateTerraformingDetails(Vector2 pos)
        {
            Font font    = LowRes ? Font8 : Font14;
            int spacing  = font.LineSpacing + 2;
            int barWidth = (int)(PFacilities.Width * 0.33f);

            TerraformTitle = Add(new UILabel(pos, "", LowRes ? Font14 : Font20, Color.White));
            TerraformTitle.Visible = false;

            Vector2 statusTitlePos       = new Vector2(pos.X, pos.Y + spacing*2);
            TerraformStatusTitle         = Add(new UILabel(statusTitlePos, Localizer.Token(GameText.TerraformingStatus), font, Color.Gray));
            TerraformStatusTitle.Visible = false;

            float indent = font.MeasureString(TerraformStatusTitle.Text).X + 125;

            Vector2 statusPos       = new Vector2(pos.X + indent, pos.Y + spacing*2);
            TerraformStatus         = Add(new UILabel(statusPos, " ", font, Color.Gray));
            TerraformStatus.Visible = false;

            Vector2 numTerraformersTitlePos = new Vector2(pos.X, TerraformStatusTitle.Y + spacing);
            TerraformersHereTitle           = Add(new UILabel(numTerraformersTitlePos, Localizer.Token(GameText.TerraformersHere), font, Color.Gray));
            TerraformersHereTitle.Visible   = false;

            Vector2 numTerraformersPos    = new Vector2(pos.X + indent, numTerraformersTitlePos.Y);
            TerraformersHere              = Add(new UILabel(numTerraformersPos, " ", font, Color.White));
            TerraformersHereTitle.Visible = false;

            Vector2 terraVolcanoTitlePos  = new Vector2(pos.X, numTerraformersTitlePos.Y + spacing*2);
            VolcanoTerraformTitle         = Add(new UILabel(terraVolcanoTitlePos, " ", font, Color.Gray));
            VolcanoTerraformTitle.Visible = false;

            Vector2 terraVolcanoPos      = new Vector2(pos.X + indent, terraVolcanoTitlePos.Y);
            VolcanoTerraformDone         = Add(new UILabel(terraVolcanoPos, Localizer.Token(GameText.TerraformersDone), font, Color.Green));
            VolcanoTerraformDone.Visible = false;

            Rectangle terraVolcanoRect = new Rectangle((int)terraVolcanoPos.X, (int)terraVolcanoPos.Y, barWidth, 20);
            VolcanoTerraformBar        = new ProgressBar(terraVolcanoRect)
            {
                Max            = 100,
                DrawPercentage = true
            };

            Vector2 terraTileTitlePos  = new Vector2(pos.X, terraVolcanoTitlePos.Y + spacing);
            TileTerraformTitle         = Add(new UILabel(terraTileTitlePos, " ", font, Color.Gray));
            TileTerraformTitle.Visible = false;

            Vector2 terraTilePos      = new Vector2(pos.X + indent, terraTileTitlePos.Y);
            TileTerraformDone         = Add(new UILabel(terraTilePos, Localizer.Token(GameText.TerraformersDone), font, Color.Green));
            TileTerraformDone.Visible = false;

            Rectangle terraTileRect = new Rectangle((int)terraTilePos.X, (int)terraTilePos.Y, barWidth, 20);
            TileTerraformBar        = new ProgressBar(terraTileRect)
            {
                Max            = 100,
                color          = "green",
                DrawPercentage = true
            };

            Vector2 terraPlanetTitlePos   = new Vector2(pos.X, terraTileTitlePos.Y + spacing);
            PlanetTerraformTitle          = Add(new UILabel(terraPlanetTitlePos, Localizer.Token(GameText.TerraformPlanet), font, Color.Gray));
            PlanetTerraformTitle.Visible  = false;

            Vector2 terraPlanetPos      = new Vector2(pos.X + indent, terraPlanetTitlePos.Y);
            PlanetTerraformDone         = Add(new UILabel(terraPlanetPos, Localizer.Token(GameText.TerraformersDone), font, Color.Green));
            PlanetTerraformDone.Visible = false;

            Rectangle terraPlanetRect = new Rectangle((int)terraPlanetPos.X, (int)terraPlanetPos.Y, barWidth, 20);
            PlanetTerraformBar        = new ProgressBar(terraPlanetRect)
            {
                Max            = 100,
                color          = "blue",
                DrawPercentage = true
            };

            Vector2 targetFertilityTitlePos = new Vector2(pos.X, terraPlanetTitlePos.Y + spacing * 2);
            TargetFertilityTitle            = Add(new UILabel(targetFertilityTitlePos, Localizer.Token(GameText.TerraformTargetFert), font, Color.Gray));
            TargetFertilityTitle.Visible    = false;

            Vector2 targetFertilityPos = new Vector2(pos.X + indent, targetFertilityTitlePos.Y);
            TargetFertility            = Add(new UILabel(targetFertilityPos, "", font, Color.LightGreen));
            TargetFertility.Visible    = false;

            Vector2 estimatedMaxPopTitlePos = new Vector2(pos.X, targetFertilityTitlePos.Y + spacing);
            EstimatedMaxPopTitle            = Add(new UILabel(estimatedMaxPopTitlePos, Localizer.Token(GameText.TerraformEsPop), font, Color.Gray));
            EstimatedMaxPopTitle.Visible    = false;

            Vector2 estimatedMaxPopPos = new Vector2(pos.X + indent, estimatedMaxPopTitlePos.Y);
            EstimatedMaxPop            = Add(new UILabel(estimatedMaxPopPos, "", font, Color.Green));
            EstimatedMaxPop.Visible    = false;
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

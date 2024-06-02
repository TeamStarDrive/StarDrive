using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using SDUtils;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Ship_Game.Audio;
using Font = Ship_Game.Graphics.Font;
using Ship_Game.Universe.SolarBodies;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public partial class BlueprintsScreen : GameScreen
    {
        public bool PlanAreaHovered { get; private set; }
        readonly Array<BlueprintsTile> TilesList = new(35);
        readonly Menu1 LeftMenu;
        readonly Menu1 RightMenu;
        readonly Submenu SubBlueprintsOptions;
        readonly Submenu PlanStats;
        readonly UILabel BlueprintsName;
        readonly UILabel CannotBuildTroopsWarning, CannotBuildShipsWarning;
        readonly UICheckBox ExclusiveCheckbox;
        public bool Exclusive;
        readonly Submenu SubPlanArea;
        readonly FloatSlider InitPopulationSlider;
        readonly FloatSlider InitFertilitySlider;
        readonly FloatSlider InitRichnessSlider;
        readonly FloatSlider InitTaxSlider;
        readonly UIButton SaveBlueprints;
        readonly UIButton LoadBlueprints;
        readonly UIButton LinkBlueprints;
        readonly UIButton UnlinkBlueprints;
        float InitPopulationBillion = 5;
        float InitFertility = 1;
        float InitRichness = 1;
        float InitTax = 0.25f;

        readonly ScrollList<BlueprintsBuildableListItem> BuildableList;
        readonly ScrollList<BlueprintsChainListItem> BlueprintsChainList;
        readonly DropOptions<Planet.ColonyType> SwitchColonyType;
        readonly UILabel LinkBlueprintsName;

        Building HoveredBuilding;
        readonly Font Font8 = Fonts.Arial8Bold;
        readonly Font Font12 = Fonts.Arial12Bold;
        readonly Font Font14 = Fonts.Arial14Bold;
        readonly Font Font20 = Fonts.Arial20Bold;
        readonly Font TextFont;
        readonly Font BigFont;
        public readonly Empire Player;

        float PlannedGrossMoney;
        float PlannedMaintenance;
        float PlannedNetIncome;
        float PlannedFertility;
        float PlannedPopulation;
        float PlannedFlatFood;
        float PlannedFoodPerCol;
        float PlannedFlatProd;
        float PlannedProdPerCol;
        float PlannedFlatResearch;
        float PlannedResearchPerCol;
        float PlannnedInfrastructure;
        float PlannedRepairPerTurn;
        float PlannedStorage;
        float PlannedShields;
        int PlanetLevel;

        bool CanBuildTroops;
        bool CanBuildShips;

        UILabel PlannedFertilityLbl, PlannedPopLbl;

        readonly GovernorDetailsComponent GovernorTab;

        public BlueprintsScreen(UniverseScreen parent, Empire player, BlueprintsTemplate template = null, GovernorDetailsComponent govTab = null) 
            : base(parent, toPause: parent)
        {
            Player = player;
            GovernorTab = govTab;
            TextFont = LowRes ? Font8 : Font12;
            BigFont = LowRes ? Font12 : Font14;
            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            base.Add(new Menu2(titleRect));
            Vector2 titlePos = new(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString
                (Localizer.Token(GameText.ColonyBlueprintsTitle)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);

            base.Add(new UILabel(titlePos, GameText.ColonyBlueprintsTitle, Fonts.Laserian14));
            LeftMenu = base.Add(new Menu1(2, titleRect.Y + titleRect.Height + 5, titleRect.Width, ScreenHeight - (titleRect.Y + titleRect.Height) - 7));
            RightMenu = base.Add(new Menu1(titleRect.Right + 10, titleRect.Y, ScreenWidth / 3 - 15, ScreenHeight - titleRect.Y - 2));
            Add(new CloseButton(RightMenu.Right - 52, RightMenu.Y + 22));

            RectF blueprintsStatsR = new(LeftMenu.X + 20, LeftMenu.Y + 20,
                                        (int)(0.4f * LeftMenu.Width),
                                        (int)(0.23f * (LeftMenu.Height - 80)));
            SubBlueprintsOptions = base.Add(new Submenu(blueprintsStatsR, GameText.BlueprintsOptions));

            RectF planAreaR = new(LeftMenu.X + 20 + SubBlueprintsOptions.Width + 20, SubBlueprintsOptions.Y,
                                   LeftMenu.Width - 60 - SubBlueprintsOptions.Width, LeftMenu.Height * 0.5f);
            SubPlanArea = base.Add(new Submenu(planAreaR, GameText.CurrentBlueprintsSubMenu));

            RectF blueprintsStatsRect = new(LeftMenu.X + 20 + SubBlueprintsOptions.Width + 20,
                                        SubPlanArea.Bottom + 20,
                                        LeftMenu.Width - 60 - SubBlueprintsOptions.Width,
                                        LeftMenu.Height - 20 - SubPlanArea.Height - 40);
            PlanStats = base.Add(new Submenu(blueprintsStatsRect, GameText.Statistics2));


            RectF buildableMenuR = new(RightMenu.X + 20, RightMenu.Y + 20,
                                   RightMenu.Width - 40, RightMenu.Height - 40);
            base.Add(new Submenu(buildableMenuR, GameText.Buildings));

            RectF experimentalR = new(LeftMenu.X + 20, LeftMenu.Y + 40 + SubBlueprintsOptions.Height, 0.4f * LeftMenu.Width, 0.294f * (LeftMenu.Height - 80));
            base.Add(new Submenu(experimentalR, GameText.BlueprintsSimulation));

            RectF chainR = new(LeftMenu.X + 20, 
                               blueprintsStatsRect.Y,
                               0.4f * LeftMenu.Width,
                               LeftMenu.Height - 20 - SubPlanArea.Height - 40);
            base.Add(new Submenu(chainR, GameText.LinksChain));


            float blueprintsOptionsX = SubBlueprintsOptions.X + 10;
            BlueprintsName = base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 30), 
                GameText.NewBlueprints, Font14, Color.Gold));
            ExclusiveCheckbox = base.Add(new UICheckBox(blueprintsOptionsX, SubBlueprintsOptions.Y + 40 + Font14.LineSpacing,
                () => Exclusive, TextFont, GameText.ExclusiveBlueprints, GameText.ExclusiveBlueprintsTip));

            ExclusiveCheckbox.TextColor = Color.Wheat;
            CannotBuildShipsWarning = base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 35 + Font14.LineSpacing*5),
                GameText.BlueprintsCannotBuildShips, Font12, Color.Pink));
            CannotBuildTroopsWarning = base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 35 + Font14.LineSpacing * 6),
                GameText.BlueprintsCannotBuildTroops, Font12, Color.Pink));

            Vector2 savePos = new(blueprintsOptionsX, SubBlueprintsOptions.Y + 170);
            SaveBlueprints = base.Add(new UIButton(ButtonStyle.Small, savePos, GameText.Save));
            SaveBlueprints.OnClick = (b) => OnSaveBlueprintsClick();
            Vector2 loadPos = new(blueprintsOptionsX + SubBlueprintsOptions.Width - 90, SubBlueprintsOptions.Y + 170);
            LoadBlueprints = base.Add(new UIButton(ButtonStyle.Small, loadPos, GameText.Load));
            LoadBlueprints.OnClick = (b) => OnLoadBlueprintsClick();
            Vector2 linkPos = new(blueprintsOptionsX + SubBlueprintsOptions.Width - 170, SubBlueprintsOptions.Y + 170);
            LinkBlueprints = base.Add(new UIButton(ButtonStyle.Small, linkPos, "Link"));
            LinkBlueprints.OnClick = (b) => OnLinkBlueprintsClick();
            LinkBlueprints.Enabled = false;
            Vector2 unlinkPos = new(blueprintsOptionsX + SubBlueprintsOptions.Width - 250, SubBlueprintsOptions.Y + 170);
            UnlinkBlueprints = base.Add(new UIButton(ButtonStyle.Small, unlinkPos, "Unlink"));
            UnlinkBlueprints.OnClick = (b) => OnUnlinkBlueprintsClick();
            UnlinkBlueprints.Enabled = false;

            RectF initPopR = new(blueprintsOptionsX, experimentalR.Y + 40, SubBlueprintsOptions.Width*0.6, 50);
            InitPopulationSlider = SliderDecimal1(initPopR, GameText.Population, 0.1f, 20, InitPopulationBillion);
            InitPopulationSlider.OnChange = (s) => { InitPopulationBillion = s.AbsoluteValue.RoundToFractionOf10(); RecalculateGeneralStats(); };
            PlannedPopLbl = base.Add(new UILabel(Font14));

            RectF initFertR = new(blueprintsOptionsX, experimentalR.Y + 90, SubBlueprintsOptions.Width * 0.6, 50);
            InitFertilitySlider = SliderDecimal1(initFertR, GameText.Fertility, 0, 3, InitFertility);
            InitFertilitySlider.OnChange = (s) => { InitFertility = s.AbsoluteValue.RoundToFractionOf10(); RecalculateGeneralStats(); };
            PlannedFertilityLbl = base.Add(new UILabel(Font14));

            RectF initRichR = new(blueprintsOptionsX, experimentalR.Y + 140, SubBlueprintsOptions.Width * 0.6, 50);
            InitRichnessSlider = SliderDecimal1(initRichR, GameText.MineralRichness, 0, 5, InitRichness);
            InitRichnessSlider.OnChange = (s) => { InitRichness = s.AbsoluteValue.RoundToFractionOf10(); RecalculateGeneralStats(); };

            RectF initTaxR = new(blueprintsOptionsX, experimentalR.Y + 190, SubBlueprintsOptions.Width * 0.6, 50);
            InitTaxSlider = Slider(SliderStyle.Percent, initTaxR, GameText.TaxRate, 0, 1, InitTax);
            InitTaxSlider.OnChange =(s) => { InitTax = s.AbsoluteValue; RecalculateGeneralStats(); };


            RectF buildableR = new(buildableMenuR.X, buildableMenuR.Y+20, buildableMenuR.W, buildableMenuR.H -20);
            BuildableList = base.Add(new ScrollList<BlueprintsBuildableListItem>(buildableR, 40));
            BuildableList.EnableItemHighlight = true;
            BuildableList.OnDoubleClick = OnBuildableItemDoubleClicked;
            BuildableList.EnableDragOutEvents = true;
            BuildableList.OnDragOut = OnBuildableListDrag;
            RectF chainlistR = new(chainR.X, chainR.Y + 20, chainR.W, chainR.H - 20);
            BlueprintsChainList = base.Add(new ScrollList<BlueprintsChainListItem>(chainlistR, 40));
            BlueprintsChainList.EnableItemHighlight = true;


            base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 50 + Font14.LineSpacing * 3),
                "Linked Blueprints:", TextFont, Color.Wheat, GameText.LinkBlueprintsTip));
            LinkBlueprintsName = base.Add(new UILabel(new Vector2(blueprintsOptionsX + 150, SubBlueprintsOptions.Y + 50 + Font14.LineSpacing * 3),
                "", TextFont, Color.White));

            base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 45 + Font14.LineSpacing * 2),
                "Switch Governor to:", TextFont, Color.Wheat, GameText.ExclusiveBlueprintsTip));
            SwitchColonyType = base.Add(Add(new DropOptions<Planet.ColonyType>(blueprintsOptionsX + 150, SubBlueprintsOptions.Y + 45 + Font14.LineSpacing * 2, 100, 18)));
            SwitchColonyType.AddOption(option: "--", Planet.ColonyType.Colony);
            SwitchColonyType.AddOption(option: GameText.Core, Planet.ColonyType.Core);
            SwitchColonyType.AddOption(option: GameText.Industrial, Planet.ColonyType.Industrial);
            SwitchColonyType.AddOption(option: GameText.Agricultural, Planet.ColonyType.Agricultural);
            SwitchColonyType.AddOption(option: GameText.Research, Planet.ColonyType.Research);
            SwitchColonyType.AddOption(option: GameText.Military, Planet.ColonyType.Military);
            SwitchColonyType.ActiveValue = Planet.ColonyType.Colony;

            Rectangle gridPos = new Rectangle(SubPlanArea.Rect.X + 10, SubPlanArea.Rect.Y + 30, 
                                              SubPlanArea.Rect.Width - 20, SubPlanArea.Rect.Height - 35);
            CreateBlueprintsTiles(gridPos);
            RefreshBuildableList();
            if (template != null)
            {
                LoadBlueprintsTemplate(template);
                LoadBlueprints.Visible = false;
                BlueprintsName.Text = template.Name;
            }
        }

        public override void PerformLayout()
        {
            PlannedPopLbl.Pos       = new Vector2(InitPopulationSlider.Right, InitPopulationSlider.Y+23);
            PlannedFertilityLbl.Pos = new Vector2(InitFertilitySlider.Right, InitFertilitySlider.Y+23);
            base.PerformLayout();
        }

        void CreateBlueprintsTiles(Rectangle gridPos)
        {
            int width = gridPos.Width / SolarSystemBody.TileMaxX;
            int height = gridPos.Height / SolarSystemBody.TileMaxY;

            for (int y = 0; y < SolarSystemBody.TileMaxY; y++)
                for (int x = 0; x < SolarSystemBody.TileMaxX; x++)
                {
                    UIPanel panel = base.Add(new UIPanel(new Rectangle(gridPos.X + x * width, gridPos.Y + y * height, width, height), Color.White));
                    TilesList.Add(new BlueprintsTile(panel));
                }
        }

        void OnBuildableItemDoubleClicked(BlueprintsBuildableListItem item)
        {
            if (!TryAddBuilding(item.Building, true))
                GameAudio.NegativeClick();
        }

        bool TryAddBuilding(Building b, bool unlocked)
        {
            BlueprintsTile tile = TilesList.Find(t => t.IsFree);
            if (tile != null)
            {
                tile.AddBuilding(b, unlocked);
                RefreshBuildableList();
                return true;
            }

            return false;
        }

        void RefreshBuildableList()
        {
            BuildableList.Reset();
            AddOutpost();
            foreach (Building b in Player.GetUnlockedBuildings().Sorted(b => b.Name))
            {
                if (b.IsSuitableForBlueprints && !TilesList.Any(t => t.BuildingNameHereIS(b.Name)))
                {
                    b.UpdateOffense(PlanetLevel, Player.Universe);
                    BuildableList.AddItem(new BlueprintsBuildableListItem(this, b));
                }
            }

            RecalculateGeneralStats();
        }

        void RefreshChainList()
        {
            BlueprintsChainList.Reset();
            BuildChain(LinkBlueprintsName.Text.Text);

            void BuildChain(string linkTo)
            {
                if (linkTo?.NotEmpty() == true
                    && ResourceManager.TryGetBlueprints(linkTo, out BlueprintsTemplate linked))
                {
                    BlueprintsChainList.AddItem(new BlueprintsChainListItem(linked));
                    BuildChain(linked.LinkTo);
                }
            }
        }

        void AddOutpost()
        {
            Building outpost = ResourceManager.GetBuildingTemplate(Building.OutpostId);
            if (outpost != null)
            {
                TilesList[0].AddBuilding(outpost);
            }
            else
            {
                Log.Error($"Blueprints Screen - Outpost building template not found! " +
                    "Check that the correct building exists in the buildings directory");
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            DrawHoveredBuildListBuildingInfo(batch);
            DrawPlanStatistics(batch);
            batch.SafeEnd();
        }

        void DrawHoveredBuildListBuildingInfo(SpriteBatch batch)
        {
            if (HoveredBuilding == null)
                return;

            Vector2 bCursor = new Vector2(PlanStats.X + 15, PlanStats.Y + 35);
            Color color = Color.Wheat;
            batch.DrawString(Font20, HoveredBuilding.TranslatedName, bCursor, color);
            bCursor.Y += Font20.LineSpacing + 5;
            string selectionText = TextFont.ParseText(HoveredBuilding.DescriptionText.Text, PlanStats.Width - 40);
            batch.DrawString(TextFont, selectionText, bCursor, color);
            bCursor.Y += TextFont.MeasureString(selectionText).Y + Font20.LineSpacing;
            ColonyScreen.DrawBuildingStaticInfo(ref bCursor, batch, TextFont, Player, PlannedFertility, 
                InitRichness, Player.data.Traits.PreferredEnv, HoveredBuilding);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, TextFont, HoveredBuilding.ActualShipRepair(PlanetLevel), 
                "NewUI/icon_queue_rushconstruction", GameText.ShipRepair);
            ColonyScreen.DrawBuildingWeaponStats(ref bCursor, batch, TextFont, HoveredBuilding, PlanetLevel);
        }

        void DrawPlanStatistics(SpriteBatch batch)
        {
            if (HoveredBuilding != null)
                return;

            Vector2 bCursor = new Vector2(PlanStats.X + 15, PlanStats.Y + 35);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedGrossMoney,
                "UI/icon_money_22", GameText.GrossIncome);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, -PlannedMaintenance,
                "UI/icon_money_22", GameText.Expenditure2);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedNetIncome,
                "UI/icon_money_22", GameText.NetIncome);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedFoodPerCol,
                "NewUI/icon_food", GameText.NetFoodPerColonistAllocated);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedFlatFood,
                "NewUI/icon_food", GameText.NetFlatFoodGeneratedPer);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedProdPerCol,
                "NewUI/icon_production", GameText.NetProductionPerColonistAllocated);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedFlatProd,
                "NewUI/icon_production", GameText.NetFlatProductionGeneratedPer);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedFlatResearch,
                "NewUI/icon_science", GameText.NetResearchPerColonistAllocated);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedResearchPerCol,
                "NewUI/icon_science", GameText.NetFlatResearchGeneratedPer);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannnedInfrastructure,
                "NewUI/icon_queue_rushconstruction", GameText.MaximumProductionToQueuePer);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedStorage,
                "NewUI/icon_storage_production", GameText.Storage);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedShields,
                "NewUI/icon_storage_production", GameText.PlanetaryShieldStrengthAdded);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, BigFont, PlannedRepairPerTurn,
                "NewUI/icon_storage_production", GameText.ShipRepair);
        }


        void RecalculateGeneralStats()
        {
            float tax = InitTax;
            float taxInverted = 1 - tax;

            float taxRateMultiplier = 1f + Player.data.Traits.TaxMod;
            Building[] plannedBuildings = TilesList.FilterSelect(t => t.HasBuilding, t => t.Building);
            PlannedMaintenance = 0;
            PlannedNetIncome   = 0;
            PlannedFertility   = InitFertility;
            PlannedPopulation  = InitPopulationBillion + plannedBuildings.Sum(b => b.MaxPopIncrease)*0.001f;
            PlannedGrossMoney  = PlannedPopulation;
            PlannedFlatFood        = 0;
            PlannedFoodPerCol      = 0;
            PlannedFlatProd        = 0;
            PlannedProdPerCol      = 0;
            PlannedFlatResearch    = 0;
            PlannedResearchPerCol  = 0;
            PlannnedInfrastructure = 1;
            PlannedRepairPerTurn   = 0;
            PlannedStorage         = 0;
            PlannedShields         = 0;
            CanBuildShips = false;
            CanBuildTroops = false;


            foreach (Building b in plannedBuildings)
            {
                PlannedGrossMoney += b.Income + b.CreditsPerColonist*PlannedPopulation;
                taxRateMultiplier += b.PlusTaxPercentage;
                PlannedMaintenance += b.Maintenance;
                PlannedFertility += b.MaxFertilityOnBuildFor(Player, Player.data.PreferredEnvPlanet);
                PlannedFlatFood += b.PlusFlatFoodAmount;
                PlannedFoodPerCol += b.PlusFoodPerColonist;
                PlannedFlatProd += b.PlusFlatProductionAmount + b.PlusProdPerRichness*InitRichness;
                PlannedProdPerCol += b.PlusProdPerColonist;
                PlannedFlatResearch += b.PlusFlatResearchAmount;
                PlannedResearchPerCol += b.PlusResearchPerColonist;
                PlannnedInfrastructure += b.Infrastructure;
                PlannedStorage += b.StorageAdded;
                PlannedShields += b.PlanetaryShieldStrengthAdded;
                PlannedRepairPerTurn += b.ActualShipRepair(PlanetLevel);
                CanBuildTroops |= b.AllowInfantry;
                CanBuildShips |= b.AllowShipBuilding || b.IsSpacePort;
                b.UpdateOffense(PlanetLevel, Player.Universe);
            }

            PlannedGrossMoney  *= 1+taxRateMultiplier;
            PlannedMaintenance *= Player.data.Traits.MaintMultiplier;
            PlannedNetIncome = PlannedGrossMoney - PlannedMaintenance;
            PlannedShields *= 1 + Player.data.ShieldPowerMod;
            PlannedFertility = PlannedFertility.LowerBound(0);
            PlanetLevel = Planet.GetLevel(PlannedPopulation);

            float foodConsumptionPerColonist = Player.NonCybernetic ? 1 + Player.data.Traits.ConsumptionModifier : 0;
            PlannedFoodPerCol = ColonyResource.FoodYieldFormula(PlannedFertility, PlannedFoodPerCol) - foodConsumptionPerColonist;
            float productionTax = Player.IsCybernetic ? tax * 0.5f : tax;

            float ProdConsumptionPerColonist = Player.IsCybernetic ? 1 + Player.data.Traits.ConsumptionModifier : 0;
            PlannedFlatProd *= (1 - productionTax);
            PlannedProdPerCol = ColonyResource.ProdYieldFormula(InitRichness, PlannedProdPerCol, Player) 
                * (1 - productionTax) - ProdConsumptionPerColonist;

            float researchMultiplier = 1 + Player.data.Traits.ResearchMod;
            PlannedFlatResearch = PlannedFlatResearch.LowerBound(0) * researchMultiplier * taxInverted * Player.data.Traits.ResearchTaxMultiplier;
            PlannedResearchPerCol *= researchMultiplier * taxInverted * Player.data.Traits.ResearchTaxMultiplier;

            CannotBuildShipsWarning.Visible = !CanBuildShips && plannedBuildings.Length > 1;
            CannotBuildTroopsWarning.Visible = !CanBuildTroops && plannedBuildings.Length > 1;

            PlannedPopLbl.Text = $"({PlannedPopulation})";
            PlannedFertilityLbl.Text = $"({PlannedFertility})";
        }

        public override void Update(float elapsedTime)
        {
            SaveBlueprints.Enabled = TilesList.Count(t => t.HasBuilding) > 1;
            UpdateShipAndTroopBuioildWarnings();
            PlannedPopLbl.Visible = PlannedPopulation.NotEqual(InitPopulationBillion);
            PlannedFertilityLbl.Visible = PlannedFertility.NotEqual(InitFertility);
            PlannedPopLbl.Color = PlannedPopulation >= InitPopulationBillion ? Color.LightGreen : Color.Pink;
            PlannedFertilityLbl.Color = PlannedFertility >= InitFertility ? Color.LightGreen : Color.Pink;
            UnlinkBlueprints.Enabled = LinkBlueprintsName.Text != "";
            base.Update(elapsedTime);
        }

        void UpdateShipAndTroopBuioildWarnings()
        {
            if (CannotBuildShipsWarning.Visible)
                CannotBuildShipsWarning.Color = Exclusive ? Color.Red : Color.Yellow;

            if (CannotBuildTroopsWarning.Visible)
                CannotBuildTroopsWarning.Color = Exclusive ? Color.Red : Color.Yellow;

        }

        public override bool HandleInput(InputState input)
        {
            if (input.OpenScreenSaveMenu && SaveBlueprints.Enabled)
            {
                OnSaveBlueprintsClick();
                return true;
            }

            PlanAreaHovered = BuildableList.IsDragging && SubPlanArea.HitTest(Input.CursorPosition);
            HoveredBuilding = GetHoveredBuildingFromBuildableList(input);
            foreach (BlueprintsTile tile in TilesList)
            {
                if (tile.HasBuilding && tile.Panel.HitTest(input.CursorPosition))
                {
                    HoveredBuilding = BuildableList.IsDragging ? null : tile.Building;
                    if (HoveredBuilding != null)
                        tile.UpdatePanelColor(true);

                    if (Input.RightMouseClick)
                    {
                        if (!tile.Building.IsCapitalOrOutpost)
                        {
                            tile.RemoveBuilding();
                            BlueprintsTemplate AfterRemove = CreateBlueprintsTemplate(); // rearrange building list in UI
                            LoadBlueprintsTemplate(AfterRemove);
                            RefreshBuildableList();
                            GameAudio.AffirmativeClick();
                        }
                        else
                        {
                            GameAudio.NegativeClick();
                        }
                    }
                }
                else
                {
                    tile.UpdatePanelColor(false);
                }
            }

            return base.HandleInput(input);
        }

        BlueprintsTemplate CreateBlueprintsTemplate()
        {
            HashSet<string> plannedBuildings = TilesList.FilterSelect(t => t.HasBuilding && !t.Building.IsOutpost, t => t.Building.Name).ToHashSet();
            return new BlueprintsTemplate(BlueprintsName.Text.Text, Exclusive, LinkBlueprintsName.Text.Text, plannedBuildings, SwitchColonyType.ActiveValue);
        }

        void OnSaveBlueprintsClick()
        {
            ScreenManager.AddScreen(new SaveLoadBlueprintsScreen(this, CreateBlueprintsTemplate()));
        }

        void OnLoadBlueprintsClick()
        {
            ScreenManager.AddScreen(new SaveLoadBlueprintsScreen(this));
        }

        void OnLinkBlueprintsClick()
        {
            ScreenManager.AddScreen(new LinkBlueprintsScreen(this, BlueprintsName.Text.Text));
        }

        void OnUnlinkBlueprintsClick()
        {
            LinkBlueprintsName.Text = "";
            RefreshChainList();
        }

        public void AfterBluprintsSave(BlueprintsTemplate template)
        {
            BlueprintsName.Text = template.Name;
            Player.Universe.RefreshEmpiresPlanetsBlueprints(template, delete: false);
            GovernorTab?.OnBlueprintsChanged(template);
            LinkBlueprints.Enabled = true;
            RefreshChainList();
        }

        public void AfterBluprintsDelete(BlueprintsTemplate template)
        {
            Player.Universe.RefreshEmpiresPlanetsBlueprints(template, delete: true);
            LinkBlueprints.Enabled = BlueprintsName.Text != template.Name;
        } 

        public void RemoveAllBlueprintsLinkTo(BlueprintsTemplate template)
        {
            Player.Universe.RefreshEmpiresPlanetsBlueprints(template, delete: false);
            if (LinkBlueprintsName.Text.Text == template.Name)
                    LinkBlueprintsName.Text = "";
        }

        public void OnBlueprintsLinked(BlueprintsTemplate linkedBlueprints)
        {
            LinkBlueprintsName.Text = linkedBlueprints.Name;
            RefreshChainList();
        }

        public void LoadBlueprintsTemplate(BlueprintsTemplate template)
        {
            ClearPlannedBuildings();
            LinkBlueprintsName.Text = "";
            LinkBlueprints.Enabled = true;
            BlueprintsName.Text = template.Name;
            Exclusive = template.Exclusive;
            SwitchColonyType.ActiveValue = template.ColonyType;
            if (template.LinkTo!= null && ResourceManager.TryGetBlueprints(template.LinkTo, out _)) 
                LinkBlueprintsName.Text = template.LinkTo;

            AddOutpost();
            foreach (string name in template.PlannedBuildings) 
            {
                var b = ResourceManager.GetBuildingTemplate(name);
                if (b != null) 
                    TryAddBuilding(b, Player.IsBuildingUnlocked(name));
            }

            RefreshChainList();
        }

        void ClearPlannedBuildings()
        {
            foreach (BlueprintsTile tile in TilesList)
                tile.RemoveBuilding();
        }

        void OnBuildableListDrag(BlueprintsBuildableListItem item, DragEvent evt, bool outside)
        {
            if (evt != DragEvent.End)
                return;

            if (outside && item != null) // TODO: somehow `item` can be null, not sure how it happens
            {
                if (PlanAreaHovered)
                {
                    OnBuildableItemDoubleClicked(item);
                    return;
                }
            }

            GameAudio.NegativeClick();
        }

        Building GetHoveredBuildingFromBuildableList(InputState input)
        {
            if (BuildableList.HitTest(input.CursorPosition))
            {
                foreach (BlueprintsBuildableListItem e in BuildableList.AllEntries)
                {
                    if (e.Hovered && e.Building != null)
                        return e.Building;
                }
            }

            return null; // default: use Plan Statistics
        }

        public static Color GetBlueprintsIconColor(BlueprintsTemplate template)
        {
            switch (template.ColonyType)
            {
                case Planet.ColonyType.Research:     return Color.CornflowerBlue;
                case Planet.ColonyType.Industrial:   return Color.Orange;
                case Planet.ColonyType.Agricultural: return Color.Green;
                case Planet.ColonyType.Military:     return Color.Red;
                case Planet.ColonyType.Core:         return Color.White;
                default:                             return Color.Yellow;
            }
        }

        public override void ExitScreen()
        {
            TilesList.Clear();
            base.ExitScreen();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ColonyScreen : PlanetScreen, IListScreen
    {
        public Planet p;
        public ToggleButton playerDesignsToggle;

        private Menu2 TitleBar;
        private Vector2 TitlePos;
        private Menu1 LeftMenu;
        private Menu1 RightMenu;
        private Submenu PlanetInfo;
        private Submenu pDescription;
        private Submenu pLabor;
        private Submenu pStorage;
        private Submenu pFacilities;
        private Submenu build;
        private Submenu queue;
        private UICheckBox GovSliders;
        private UICheckBox GovBuildings;
        private UITextEntry PlanetName = new UITextEntry();
        private Rectangle PlanetIcon;
        private EmpireUIOverlay eui;
        private bool LowRes;
        private Lock FoodLock;
        private Lock ProdLock;
        private Lock ResLock;
        private float ClickTimer;
        private float TimerDelay = 0.25f;
        private ToggleButton LeftColony;
        private ToggleButton RightColony;
        private UIButton launchTroops;
        private UIButton SendTroops;  //fbedard
        private DropOptions<int> GovernorDropdown;
        public CloseButton close;
        private Rectangle MoneyRect;
        private Array<ThreeStateButton> ResourceButtons = new Array<ThreeStateButton>();
        private ScrollList CommoditiesSL;
        private Rectangle gridPos;
        private Submenu subColonyGrid;
        private ScrollList buildSL;
        private ScrollList QSL;
        private DropDownMenu foodDropDown;
        private DropDownMenu prodDropDown;
        private ProgressBar FoodStorage;
        private ProgressBar ProdStorage;
        private Rectangle foodStorageIcon;
        private Rectangle profStorageIcon;
        private ColonySlider ColonySliderFood;
        private ColonySlider ColonySliderProd;
        private ColonySlider ColonySliderRes;

        private object detailInfo;
        private Building toScrap;
        private ScrollList.Entry ActiveBuildingEntry;

        public bool ClickedTroop;
        private float fPercentLast;
        private float pPercentLast;
        private float rPercentLast;

        private bool draggingSlider1;
        private bool draggingSlider2;
        private bool draggingSlider3;
        private Selector selector;
        private int buildingsHereLast;
        private int buildingsCanBuildLast;
        private int shipsCanBuildLast;
        public bool Reset;
        private int editHoverState;

        private Rectangle edit_name_button;
        private Array<Building> BuildingsCanBuild = new Array<Building>();
        private GenericButton ChangeGovernor = new GenericButton(new Rectangle(), Localizer.Token(370), Fonts.Pirulen16);
        private MouseState currentMouse;
        private MouseState previousMouse;
        private bool rmouse;
        private static bool popup;  //fbedard

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI) : base(parent)
        {
            empUI.empire.UpdateShipsWeCanBuild();
            eui = empUI;
            this.p = p;
            if (ScreenWidth <= 1366)
                LowRes = true;
            Rectangle theMenu1 = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(theMenu1);
            LeftColony = new ToggleButton(new Vector2(theMenu1.X + 25, theMenu1.Y + 24), ToggleButtonStyle.ArrowLeft);
            RightColony = new ToggleButton(new Vector2(theMenu1.X + theMenu1.Width - 39, theMenu1.Y + 24), ToggleButtonStyle.ArrowRight);
            TitlePos = new Vector2(theMenu1.X + theMenu1.Width / 2 - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, theMenu1.Y + theMenu1.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            Rectangle theMenu2 = new Rectangle(2, theMenu1.Y + theMenu1.Height + 5, theMenu1.Width, ScreenHeight - (theMenu1.Y + theMenu1.Height) - 7);
            LeftMenu = new Menu1(theMenu2);
            Rectangle theMenu3 = new Rectangle(theMenu1.X + theMenu1.Width + 10, theMenu1.Y, ScreenWidth / 3 - 15, ScreenHeight - theMenu1.Y - 2);
            RightMenu = new Menu1(theMenu3);
            var iconMoney = ResourceManager.Texture("NewUI/icon_money");
            MoneyRect = new Rectangle(theMenu2.X + theMenu2.Width - 75, theMenu2.Y + 20, iconMoney.Width, iconMoney.Height);
            close = new CloseButton(this, new Rectangle(theMenu3.X + theMenu3.Width - 52, theMenu3.Y + 22, 20, 20));
            Rectangle theMenu4 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            PlanetInfo = new Submenu(theMenu4);
            PlanetInfo.AddTab(Localizer.Token(326));
            Rectangle theMenu5 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pDescription = new Submenu(theMenu5);
            Rectangle theMenu6 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pLabor = new Submenu(theMenu6);
            pLabor.AddTab(Localizer.Token(327));
            float num1 = (int)(theMenu6.Width * 0.600000023841858);
            while (num1 % 10.0 != 0.0)
                ++num1;
            Rectangle rectangle1 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.25 * (theMenu6.Height - 25)), (int)num1, 6);
            ColonySliderFood = new ColonySlider();
            ColonySliderFood.sRect = rectangle1;
            ColonySliderFood.amount = p.FarmerPercentage;
            FoodLock = new Lock();
            var foodLockTex = ResourceManager.Texture(FoodLock.Path);
            FoodLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 50, ColonySliderFood.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            if (p.Owner != null && p.Owner.data.Traits.Cybernetic > 0)
                p.FoodLocked = true;
            FoodLock.Locked = p.FoodLocked;
            Rectangle rectangle2 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.5 * (theMenu6.Height - 25)), (int)num1, 6);
            ColonySliderProd = new ColonySlider();
            ColonySliderProd.sRect = rectangle2;
            ColonySliderProd.amount = p.WorkerPercentage;
            ProdLock = new Lock();
            ProdLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 50, ColonySliderProd.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            ProdLock.Locked = p.ProdLocked;
            Rectangle rectangle3 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.75 * (theMenu6.Height - 25)), (int)num1, 6);
            ColonySliderRes = new ColonySlider();
            ColonySliderRes.sRect = rectangle3;
            ColonySliderRes.amount = p.ResearcherPercentage;
            ResLock = new Lock();
            ResLock.LockRect = new Rectangle(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width + 50, ColonySliderRes.sRect.Y + 2 + ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            ResLock.Locked = p.ResLocked;
            Rectangle theMenu7 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + theMenu6.Height + 40, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pStorage = new Submenu(theMenu7);
            pStorage.AddTab(Localizer.Token(328));
            Empire.Universe.ShipsInCombat.Visible = false;
            Empire.Universe.PlanetsInCombat.Visible = false;

            if (GlobalStats.HardcoreRuleset)
            {
                int num2 = (theMenu7.Width - 40) / 4;
                ResourceButtons.Add(new ThreeStateButton(p.FS, "Food", new Vector2(theMenu7.X + 20, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(p.PS, "Production", new Vector2(theMenu7.X + 20 + num2, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "Fissionables", new Vector2(theMenu7.X + 20 + num2 * 2, theMenu7.Y + 30)));
                ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "ReactorFuel", new Vector2(theMenu7.X + 20 + num2 * 3, theMenu7.Y + 30)));
            }
            else
            {
                FoodStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.330000013113022 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
                FoodStorage.Max = p.MaxStorage;
                FoodStorage.Progress = p.SbCommodities.FoodHereActual;
                FoodStorage.color = "green";
                foodDropDown = LowRes ? new DropDownMenu(new Rectangle(theMenu7.X + 90 + (int)(0.400000005960464 * theMenu7.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18)) : new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                foodDropDown.AddOption(Localizer.Token(329));
                foodDropDown.AddOption(Localizer.Token(330));
                foodDropDown.AddOption(Localizer.Token(331));
                foodDropDown.ActiveIndex = (int)p.FS;
                var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
                foodStorageIcon = new Rectangle(theMenu7.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
                ProdStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.660000026226044 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
                ProdStorage.Max = p.MaxStorage;
                ProdStorage.Progress = p.ProductionHere;
                var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
                profStorageIcon = new Rectangle(theMenu7.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
                prodDropDown = LowRes ? new DropDownMenu(new Rectangle(theMenu7.X + 90 + (int)(0.400000005960464 * theMenu7.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18)) : new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                prodDropDown.AddOption(Localizer.Token(329));
                prodDropDown.AddOption(Localizer.Token(330));
                prodDropDown.AddOption(Localizer.Token(331));
                prodDropDown.ActiveIndex = (int)p.PS;
            }
            Rectangle theMenu8 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu4.Y, theMenu2.Width - 60 - theMenu4.Width, (int)(theMenu2.Height * 0.5));
            subColonyGrid = new Submenu(theMenu8);
            subColonyGrid.AddTab(Localizer.Token(332));
            Rectangle theMenu9 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu8.Y + theMenu8.Height + 20, theMenu2.Width - 60 - theMenu4.Width, theMenu2.Height - 20 - theMenu8.Height - 40);
            pFacilities = new Submenu(theMenu9);
            pFacilities.AddTab(Localizer.Token(333));

            launchTroops = Button(theMenu9.X + theMenu9.Width - 175, theMenu9.Y - 5, "Launch Troops", OnLaunchTroopsClicked);
            SendTroops = Button(theMenu9.X + theMenu9.Width - launchTroops.Rect.Width - 185,
                                theMenu9.Y - 5, "Send Troops", OnSendTroopsClicked);

            CommoditiesSL = new ScrollList(pFacilities, 40);
            Rectangle theMenu10 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20, theMenu3.Width - 40, (int)(0.5 * (theMenu3.Height - 60)));
            build = new Submenu(theMenu10);
            build.AddTab(Localizer.Token(334));
            buildSL = new ScrollList(build);
            playerDesignsToggle = new ToggleButton(
                new Vector2(build.Menu.X + build.Menu.Width - 270, build.Menu.Y),
                ToggleButtonStyle.Grid, "SelectionBox/icon_grid");

            playerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
            if (p.HasShipyard)
                build.AddTab(Localizer.Token(335));
            if (p.AllowInfantry)
                build.AddTab(Localizer.Token(336));
            Rectangle theMenu11 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20 + 20 + theMenu10.Height, theMenu3.Width - 40, theMenu3.Height - 40 - theMenu10.Height - 20 - 3);
            queue = new Submenu(theMenu11);
            queue.AddTab(Localizer.Token(337));

            QSL = new ScrollList(queue, ListOptions.Draggable);

            PlanetIcon = new Rectangle(theMenu4.X + theMenu4.Width - 148, theMenu4.Y + (theMenu4.Height - 25) / 2 - 64 + 25, 128, 128);
            gridPos = new Rectangle(subColonyGrid.Menu.X + 10, subColonyGrid.Menu.Y + 30, subColonyGrid.Menu.Width - 20, subColonyGrid.Menu.Height - 35);
            int width = gridPos.Width / 7;
            int height = gridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(gridPos.X + planetGridSquare.x * width, gridPos.Y + planetGridSquare.y * height, width, height);
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 12;
            if (p.Owner != null)
            {
                shipsCanBuildLast = p.Owner.ShipsWeCanBuild.Count;
                buildingsHereLast = p.BuildingList.Count;
                buildingsCanBuildLast = BuildingsCanBuild.Count;
                detailInfo = p.Description;
                Rectangle rectangle4 = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
                Rectangle rectangle5 = new Rectangle(rectangle4.X + rectangle4.Width + 20, rectangle4.Y + rectangle4.Height - 15, (int)Fonts.Pirulen16.MeasureString(Localizer.Token(370)).X, Fonts.Pirulen16.LineSpacing);
                GovernorDropdown = new DropOptions<int>(this, new Rectangle(rectangle5.X + 30, rectangle5.Y + 30, 100, 18));
                GovernorDropdown.AddOption("--", 1);
                GovernorDropdown.AddOption(Localizer.Token(4064), 0);
                GovernorDropdown.AddOption(Localizer.Token(4065), 2);
                GovernorDropdown.AddOption(Localizer.Token(4066), 4);
                GovernorDropdown.AddOption(Localizer.Token(4067), 3);
                GovernorDropdown.AddOption(Localizer.Token(4068), 5);
                GovernorDropdown.AddOption(Localizer.Token(5087), 6);
                GovernorDropdown.ActiveIndex = GetIndex(p);
                if ((Planet.ColonyType)GovernorDropdown.ActiveValue != this.p.colonyType)
                {
                    this.p.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                    if (this.p.colonyType == Planet.ColonyType.Colony)
                    {
                        this.p.GovernorOn = false;
                        this.p.FoodLocked = false;
                        this.p.ProdLocked = false;
                        this.p.ResLocked = false;
                    }
                    else
                    {
                        this.p.FoodLocked = true;
                        this.p.ProdLocked = true;
                        this.p.ResLocked = true;
                        this.p.GovernorOn = true;
                    }
                }

                // @todo add localization
                GovBuildings = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Fonts.Arial12Bold.LineSpacing * 2 + 15, 
                                            () => p.GovBuildings, Fonts.Arial12Bold, "Governor manages buildings", 0);

                GovSliders = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Fonts.Arial12Bold.LineSpacing + 10,
                                          () => p.GovSliders, Fonts.Arial12Bold, "Governor manages labor sliders", 0);
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }
        }

        private void AddTroopToQ()
        {
            QueueItem qItem = new QueueItem(p)
            {
                isTroop = true,
                troopType = "Terran/Space Marine",
                Cost = ResourceManager.GetTroopCost("Terran/Space Marine"),
                productionTowards = 0f
            };
            p.ConstructionQueue.Add(qItem);
        }

        public override void Draw(SpriteBatch batch)
        {
            ClickTimer += (float)GameTime.ElapsedGameTime.TotalSeconds;
            if (p.Owner == null)
                return;
            p.UpdateIncomes(false);
            LeftMenu.Draw();
            RightMenu.Draw();
            TitleBar.Draw();
            LeftColony.Draw(ScreenManager);
            RightColony.Draw(ScreenManager);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(369), TitlePos, new Color(byte.MaxValue, 239, 208));
            if (!GlobalStats.HardcoreRuleset)
            {
                FoodStorage.Max = p.MaxStorage;
                FoodStorage.Progress = p.SbCommodities.FoodHereActual;
                ProdStorage.Max = p.MaxStorage;
                ProdStorage.Progress = p.ProductionHere;
            }
            PlanetInfo.Draw();
            pDescription.Draw();
            pLabor.Draw();
            pStorage.Draw();
            subColonyGrid.Draw();
            var destinationRectangle1 = new Rectangle(gridPos.X, gridPos.Y + 1, gridPos.Width - 4, gridPos.Height - 3);
            batch.Draw(ResourceManager.Texture("PlanetTiles/" + p.GetTile()), destinationRectangle1, Color.White);
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.Habitable)
                    batch.FillRectangle(pgs.ClickRect, new Color(0, 0, 0, 200));
                batch.DrawRectangle(pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
                if (pgs.building != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    if(pgs.building.IsPlayerAdded)
                    {
                        batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.building.Icon + "_64x64"), destinationRectangle2, Color.WhiteSmoke);
                    }
                    else
                    
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.building.Icon + "_64x64"), destinationRectangle2, Color.White);
                }
                else if (pgs.QItem != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.QItem.Building.Icon + "_64x64"), destinationRectangle2, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 128));
                }
                DrawPGSIcons(pgs);
            }
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
            {
                if (planetGridSquare.highlighted)
                    batch.DrawRectangle(planetGridSquare.ClickRect, Color.White, 2f);
            }
            if (ActiveBuildingEntry != null)
            {
                MouseState state2 = Mouse.GetState();
                var r = new Rectangle(state2.X, state2.Y, 48, 48);
                var building = ActiveBuildingEntry.Get<Building>();
                batch.Draw(ResourceManager.Texture($"Buildings/icon_{building.Icon}_48x48"), r, Color.White);
            }
            pFacilities.Draw();
            launchTroops.Visible = p.Owner == Empire.Universe.player && p.TroopsHere.Count > 0;

            //fbedard: Display button
            if (p.Owner == Empire.Universe.player)
            {
                int troopsInvading = eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0)
                    .Where(ai => ai.AI.State != AIState.Resupply)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));
                if (troopsInvading > 0)
                    SendTroops.Text = "Landing: " + troopsInvading;
                else
                    SendTroops.Text = "Send Troops";
            }
            DrawDetailInfo(new Vector2(pFacilities.Menu.X + 15, pFacilities.Menu.Y + 35));
            build.Draw();
            queue.Draw();

            if (build.Tabs[0].Selected)
            {
                DrawBuildingsWeCanBuild(batch);
            }
            else if (p.HasShipyard && build.Tabs[1].Selected)
            {
                DrawBuildableShipsList(batch);
            }
            else if (!p.HasShipyard && p.AllowInfantry && build.Tabs[1].Selected)
            {
                DrawBuildTroopsList(batch);
            }
            else if (build.Tabs.Count > 2 && build.Tabs[2].Selected)
            {
                DrawBuildTroopsListDup(batch);
            }

            DrawConstructionQueue(batch);

            buildSL.Draw(batch);
            selector?.Draw(batch);
            string format = "0.#";
            batch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), new Rectangle(ColonySliderFood.sRect.X, ColonySliderFood.sRect.Y, (int)(ColonySliderFood.amount * (double)ColonySliderFood.sRect.Width), 6), new Rectangle(ColonySliderFood.sRect.X, ColonySliderFood.sRect.Y, (int)(ColonySliderFood.amount * (double)ColonySliderFood.sRect.Width), 6), p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            batch.DrawRectangle(ColonySliderFood.sRect, ColonySliderFood.Color);
            Rectangle rectangle1 = new Rectangle(ColonySliderFood.sRect.X - 40, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            batch.Draw(ResourceManager.Texture("NewUI/icon_food"), rectangle1, p.Owner.data.Traits.Cybernetic > 0 ? new Color(110, 110, 110, byte.MaxValue) : Color.White);
            if (rectangle1.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
            {
                ToolTip.CreateTooltip(p.Owner.data.Traits.Cybernetic == 0 ? 70 : 77);
            }

            batch.Draw(ColonySliderFood.cState == "normal"
                    ? ResourceManager.Texture("NewUI/slider_crosshair")
                    : ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderFood.cursor,
                    p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);

            for (int index = 0; index < 11; ++index)
            {
                Vector2 position1 = new Vector2(ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width / 10 * index, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height + 2);
                if (ColonySliderFood.state == "normal")
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute"), position1, p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
                else
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), position1, p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            }
            Vector2 position2 = new Vector2(pLabor.Menu.X + pLabor.Menu.Width - 20, ColonySliderFood.sRect.Y + ColonySliderFood.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            if (LowRes)
                position2.X -= 15f;
            string text1 = p.Owner.data.Traits.Cybernetic == 0 ? p.GetNetFoodPerTurn().ToString(format) : "Unnecessary";
            position2.X -= Fonts.Arial12Bold.MeasureString(text1).X;
            if (p.NetFoodPerTurn - (double)p.Consumption < 0.0 && p.Owner.data.Traits.Cybernetic != 1 && text1 != "0")
                batch.DrawString(Fonts.Arial12Bold, text1, position2, Color.LightPink);
            else
                batch.DrawString(Fonts.Arial12Bold, text1, position2, new Color(byte.MaxValue, 239, 208));
            batch.Draw(ResourceManager.Texture("NewUI/slider_grd_brown"), new Rectangle(ColonySliderProd.sRect.X, ColonySliderProd.sRect.Y, (int)(ColonySliderProd.amount * (double)ColonySliderProd.sRect.Width), 6), new Rectangle(ColonySliderProd.sRect.X, ColonySliderProd.sRect.Y, (int)(ColonySliderProd.amount * (double)ColonySliderProd.sRect.Width), 6), Color.White);
            batch.DrawRectangle(ColonySliderProd.sRect, ColonySliderProd.Color);
            Rectangle rectangle2 = new Rectangle(ColonySliderProd.sRect.X - 40, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), rectangle2, Color.White);
            if (rectangle2.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(71);
            if (ColonySliderProd.cState == "normal")
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), ColonySliderProd.cursor, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderProd.cursor, Color.White);
            for (int index = 0; index < 11; ++index)
            {
                Vector2 position1 = new Vector2(ColonySliderFood.sRect.X + ColonySliderProd.sRect.Width / 10 * index, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height + 2);
                if (ColonySliderProd.state == "normal")
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute"), position1, Color.White);
                else
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), position1, Color.White);
            }
            position2 = new Vector2(pLabor.Menu.X + pLabor.Menu.Width - 20, ColonySliderProd.sRect.Y + ColonySliderProd.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            if (LowRes)
                position2.X -= 15f;
            float num4;
            string str1;
            if (p.Owner.data.Traits.Cybernetic == 0)
            {
                str1 = p.NetProductionPerTurn.ToString(format);
            }
            else
            {
                num4 = p.NetProductionPerTurn - p.Consumption;
                str1 = num4.ToString(format);
            }
            string text2 = str1;
            if (p.CrippledTurns > 0)
            {
                text2 = Localizer.Token(2202);
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            }
            else if (p.RecentCombat)
            {
                text2 = Localizer.Token(2257);
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            }
            else
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            if (p.CrippledTurns > 0 || p.RecentCombat || p.Owner.data.Traits.Cybernetic != 0 && p.NetProductionPerTurn - (double)p.Consumption < 0.0 && text2 != "0")
                batch.DrawString(Fonts.Arial12Bold, text2, position2, Color.LightPink);
            else
                batch.DrawString(Fonts.Arial12Bold, text2, position2, new Color(byte.MaxValue, 239, 208));
            batch.Draw(ResourceManager.Texture("NewUI/slider_grd_blue"), new Rectangle(ColonySliderRes.sRect.X, ColonySliderRes.sRect.Y, (int)(ColonySliderRes.amount * (double)ColonySliderRes.sRect.Width), 6), new Rectangle(ColonySliderRes.sRect.X, ColonySliderRes.sRect.Y, (int)(ColonySliderRes.amount * (double)ColonySliderRes.sRect.Width), 6), Color.White);
            batch.DrawRectangle(ColonySliderRes.sRect, ColonySliderRes.Color);
            Rectangle rectangle3 = new Rectangle(ColonySliderRes.sRect.X - 40, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - ResourceManager.Texture("NewUI/icon_science").Height / 2, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
            batch.Draw(ResourceManager.Texture("NewUI/icon_science"), rectangle3, Color.White);
            if (rectangle3.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(72);
            if (ColonySliderRes.cState == "normal")
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), ColonySliderRes.cursor, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), ColonySliderRes.cursor, Color.White);
            for (int index = 0; index < 11; ++index)
            {
                Vector2 position1 = new Vector2(ColonySliderFood.sRect.X + ColonySliderRes.sRect.Width / 10 * index, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height + 2);
                if (ColonySliderRes.state == "normal")
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute"), position1, Color.White);
                else
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), position1, Color.White);
            }
            position2 = new Vector2(pLabor.Menu.X + pLabor.Menu.Width - 20, ColonySliderRes.sRect.Y + ColonySliderRes.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            if (LowRes)
                position2.X -= 15f;
            string text3 = p.NetResearchPerTurn.ToString(format);
            position2.X -= Fonts.Arial12Bold.MeasureString(text3).X;
            batch.DrawString(Fonts.Arial12Bold, text3, position2, new Color(byte.MaxValue, 239, 208));
            if (p.Owner.data.Traits.Cybernetic == 0)
            {
                if (!FoodLock.Hover && !FoodLock.Locked)
                    batch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 50));
                else if (FoodLock.Hover && !FoodLock.Locked)
                    batch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150));
                else
                    batch.Draw(ResourceManager.Texture(FoodLock.Path), FoodLock.LockRect, Color.White);
            }
            if (!ProdLock.Hover && !ProdLock.Locked)
                batch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 50));
            else if (ProdLock.Hover && !ProdLock.Locked)
                batch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150));
            else
                batch.Draw(ResourceManager.Texture(ProdLock.Path), ProdLock.LockRect, Color.White);
            if (!ResLock.Hover && !ResLock.Locked)
                batch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 50));
            else if (ResLock.Hover && !ResLock.Locked)
                batch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150));
            else
                batch.Draw(ResourceManager.Texture(ResLock.Path), ResLock.LockRect, Color.White);
            batch.Draw(ResourceManager.Texture("Planets/" + p.PlanetType), PlanetIcon, Color.White);
            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            Vector2 vector2_2 = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
            p.Name = PlanetName.Text;
            PlanetName.Draw(Fonts.Arial20Bold, batch, vector2_2, GameTime, new Color(byte.MaxValue, 239, 208));
            edit_name_button = new Rectangle((int)(vector2_2.X + (double)Fonts.Arial20Bold.MeasureString(p.Name).X + 12.0), (int)(vector2_2.Y + (double)(Fonts.Arial20Bold.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (editHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), edit_name_button, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), edit_name_button, Color.White);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 768)
                vector2_2.Y += Fonts.Arial20Bold.LineSpacing * 2;
            else
                vector2_2.Y += Fonts.Arial20Bold.LineSpacing;
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Fonts.Arial12Bold, p.Type, position3, new Color(byte.MaxValue, 239, 208));
            vector2_2.Y += Fonts.Arial12Bold.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            SpriteBatch spriteBatch1 = batch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            num4 = p.Population / 1000f;
            string str2 = num4.ToString(format);
            string str3 = " / ";
            num4 = (float)((p.MaxPopulation + (double)p.MaxPopBonus) / 1000.0);
            string str4 = num4.ToString(format);
            string text4 = str2 + str3 + str4;
            Vector2 position4 = position3;
            Color color = new Color(byte.MaxValue, 239, 208);
            spriteBatch1.DrawString(arial12Bold, text4, position4, color);
            Rectangle rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385) + ":").X, Fonts.Arial12Bold.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += Fonts.Arial12Bold.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.Fertility.ToString(format), position3, new Color(byte.MaxValue, 239, 208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386) + ":").X, Fonts.Arial12Bold.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += Fonts.Arial12Bold.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.MineralRichness.ToString(format), position3, new Color(byte.MaxValue, 239, 208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387) + ":").X, Fonts.Arial12Bold.LineSpacing);


            // The Doctor: For planet income breakdown

            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

            float grossIncome = p.GrossIncome;
            float grossUpkeep = p.GrossUpkeep;
            float netIncome = p.NetIncome;

            Vector2 positionGIncome = vector2_2;
            positionGIncome.X = vector2_2.X + 1;
            positionGIncome.Y = vector2_2.Y + 28;
            Vector2 positionGrossIncome = position3;
            positionGrossIncome.Y = position3.Y + 28;
            positionGrossIncome.X = position3.X + 1;

            batch.DrawString(Fonts.Arial10, gIncome + ":", positionGIncome, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossIncome.ToString("F2") + " BC/Y", positionGrossIncome, Color.LightGray);

            Vector2 positionGUpkeep = positionGIncome;
            positionGUpkeep.Y = positionGIncome.Y + (Fonts.Arial12.LineSpacing);
            Vector2 positionGrossUpkeep = positionGrossIncome;
            positionGrossUpkeep.Y = positionGrossIncome.Y + (Fonts.Arial12.LineSpacing);

            batch.DrawString(Fonts.Arial10, gUpkeep + ":", positionGUpkeep, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossUpkeep.ToString("F2") + " BC/Y", positionGrossUpkeep, Color.LightGray);

            Vector2 positionNIncome = positionGUpkeep;
            positionNIncome.X = positionGUpkeep.X - 1;
            positionNIncome.Y = positionGUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);
            Vector2 positionNetIncome = positionGrossUpkeep;
            positionNetIncome.X = positionGrossUpkeep.X - 1;
            positionNetIncome.Y = positionGrossUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);

            batch.DrawString(Fonts.Arial12, (netIncome > 0.0 ? nIncome : nLosses) + ":", positionNIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);
            batch.DrawString(Fonts.Arial12Bold, netIncome.ToString("F2") + " BC/Y", positionNetIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);

            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);

            if (ResourceManager.TextureLoaded("Portraits/" + p.Owner.data.PortraitName))
            {
                Rectangle rectangle4 = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
                while (rectangle4.Y + rectangle4.Height > pDescription.Menu.Y + 30 + pDescription.Menu.Height - 30)
                {
                    rectangle4.Height -= (int)(0.100000001490116 * rectangle4.Height);
                    rectangle4.Width -= (int)(0.100000001490116 * rectangle4.Width);
                }
                batch.Draw(ResourceManager.Texture("Portraits/" + p.Owner.data.PortraitName), rectangle4, Color.White);
                batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), rectangle4, Color.White);
                batch.DrawRectangle(rectangle4, Color.Orange);
                if (p.colonyType == Planet.ColonyType.Colony)
                    batch.Draw(ResourceManager.Texture("NewUI/x_red"), rectangle4, Color.White);
                Vector2 position5 = new Vector2((rectangle4.X + rectangle4.Width + 15), rectangle4.Y);
                Vector2 vector2_3 = position5;
                switch (p.colonyType)
                {
                    case Planet.ColonyType.Core:         Localizer.Token(372); break;
                    case Planet.ColonyType.Colony:       Localizer.Token(376); break;
                    case Planet.ColonyType.Industrial:   Localizer.Token(373); break;
                    case Planet.ColonyType.Research:     Localizer.Token(375); break;
                    case Planet.ColonyType.Agricultural: Localizer.Token(371); break;
                    case Planet.ColonyType.Military:     Localizer.Token(374); break;
                    case Planet.ColonyType.TradeHub:     Localizer.Token(393); break;
                }
                batch.DrawString(Fonts.Arial12Bold, "Governor", position5, Color.White);
                position5.Y = GovernorDropdown.Rect.Y + 25;

                int ColonyTypeLocalization()
                {
                    switch (p.colonyType)
                    {
                        default:
                        case Planet.ColonyType.Core: return 378;
                        case Planet.ColonyType.Colony: return 382;
                        case Planet.ColonyType.Industrial: return 379;
                        case Planet.ColonyType.Research: return 381;
                        case Planet.ColonyType.Agricultural: return 377;
                        case Planet.ColonyType.Military: return 380;
                        case Planet.ColonyType.TradeHub: return 394;
                    }
                }

                string text5 = Fonts.Arial12Bold.ParseText(Localizer.Token(ColonyTypeLocalization()), (pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                batch.DrawString(Fonts.Arial12Bold, text5, position5, Color.White);

                GovernorDropdown.SetAbsPos(vector2_3.X, vector2_3.Y + Fonts.Arial12Bold.LineSpacing + 5);
                GovernorDropdown.Reset();
                GovernorDropdown.Draw(batch);
            }
            if (GlobalStats.HardcoreRuleset)
            {
                foreach (ThreeStateButton threeStateButton in ResourceButtons)
                    threeStateButton.Draw(ScreenManager, (int)p.GetGoodAmount(threeStateButton.Good));
            }
            else
            {
                FoodStorage.Progress = p.SbCommodities.FoodHereActual;
                ProdStorage.Progress = p.ProductionHere;
                if      (p.FS == Planet.GoodState.STORE)  foodDropDown.ActiveIndex = 0;
                else if (p.FS == Planet.GoodState.IMPORT) foodDropDown.ActiveIndex = 1;
                else if (p.FS == Planet.GoodState.EXPORT) foodDropDown.ActiveIndex = 2;
                if (p.Owner.data.Traits.Cybernetic == 0)
                {
                    FoodStorage.Draw(batch);
                    foodDropDown.Draw(batch);
                }
                else
                {
                    FoodStorage.DrawGrayed(batch);
                    foodDropDown.DrawGrayed(batch);
                }
                ProdStorage.Draw(batch);
                if      (p.PS == Planet.GoodState.STORE)  prodDropDown.ActiveIndex = 0;
                else if (p.PS == Planet.GoodState.IMPORT) prodDropDown.ActiveIndex = 1;
                else if (p.PS == Planet.GoodState.EXPORT) prodDropDown.ActiveIndex = 2;
                prodDropDown.Draw(batch);
                if (!LowRes)
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), foodStorageIcon, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), profStorageIcon, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), foodStorageIcon, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), profStorageIcon, Color.White);
                }
            }

            base.Draw(batch);

            if (ScreenManager.NumScreens == 2)
                popup = true;

            close.Draw(batch);

            if (foodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (profStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(74);
        }

        private void DrawBuildTroopsListDup(SpriteBatch batch)
        {
            Vector2 vector2_1;
            if (Reset)
            {
                buildSL.Reset();
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (p.Owner.WeCanBuildTroop(troopType))
                        buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), true, false);
                }

                Reset = false;
            }

            Texture2D iconProd = ResourceManager.Texture("NewUI/icon_production");
            vector2_1 = new Vector2(build.Menu.X + 20, build.Menu.Y + 45);
            foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
            {
                vector2_1.Y = entry.Y;
                var troop = entry.Get<Troop>();
                if (!entry.Hovered)
                {
                    troop.Draw(batch, new Rectangle((int) vector2_1.X, (int) vector2_1.Y, 29, 30));
                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);
                    position.X = entry.Right - 100;
                    Rectangle destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, ((int) troop.GetCost()).ToString(), position, Color.White);
                    entry.DrawPlusEdit(batch);
                }
                else
                {
                    vector2_1.Y = entry.Y;
                    troop.Draw(batch, new Rectangle((int) vector2_1.X, (int) vector2_1.Y, 29, 30));
                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);
                    position.X = entry.Right - 100;
                    Rectangle destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, ((int) troop.GetCost()).ToString(), position, Color.White);
                    entry.DrawPlusEdit(batch);
                }
            }
        }

        private void DrawBuildableShipsList(SpriteBatch batch)
        {
            var added = new HashSet<string>();
            if (shipsCanBuildLast != p.Owner.ShipsWeCanBuild.Count || Reset)
            {
                buildSL.Reset();

                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.ColoniserMenu)
                {
                    added.Add("Coloniser");
                    buildSL.AddItem(new ModuleHeader("Coloniser"));
                }

                foreach (string shipToBuild in p.Owner.ShipsWeCanBuild)
                {
                    var ship = ResourceManager.GetShipTemplate(shipToBuild);
                    var role = ResourceManager.ShipRoles[ship.shipData.Role];
                    var header = Localizer.GetRole(ship.DesignRole, p.Owner);
                    if (role.Protected || role.NoBuild )
                        continue;
                    if ((GlobalStats.ShowAllDesigns || ship.IsPlayerDesign) && !added.Contains(header))
                    {
                        added.Add(header);
                        buildSL.AddItem(new ModuleHeader(header));
                    }
                }

                Reset = false;

                // @todo This sorting looks quite heavy...
                IOrderedEnumerable<KeyValuePair<string, Ship>> orderedShips =
                    ResourceManager.ShipsDict
                        .OrderBy(s => !s.Value.IsPlayerDesign)
                        .ThenBy(kv => kv.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                        .ThenBy(kv => kv.Value.BaseHull.ShipStyle)
                        .ThenByDescending(kv => kv.Value.GetTechScore(out int[] _))
                        .ThenBy(kv => kv.Value.Name)
                        .ThenBy(kv => kv.Key);
                KeyValuePair<string, Ship>[] ships = orderedShips.ToArray();

                foreach (ScrollList.Entry entry in buildSL.AllEntries)
                {
                    string header = entry.Get<ModuleHeader>().Text;

                    foreach (KeyValuePair<string, Ship> kv in ships)
                    {
                        if (!EmpireManager.Player.ShipsWeCanBuild.Contains(kv.Key))
                            continue;

                        if (Localizer.GetRole(kv.Value.DesignRole, EmpireManager.Player) != header
                            || kv.Value.Deleted
                            || ResourceManager.ShipRoles[kv.Value.shipData.Role].Protected
                            || kv.Value.shipData.CarrierShip) 
                        {
                            continue;
                        }

                        Ship ship = kv.Value;
                        if ((GlobalStats.ShowAllDesigns || ship.IsPlayerDesign) &&
                            Localizer.GetRole(ship.DesignRole, p.Owner) == header)
                            entry.AddSubItem(ship, addAndEdit: true);
                    }
                }
            }

            var topLeft = new Vector2((build.Menu.X + 20), (build.Menu.Y + 45));
            foreach (ScrollList.Entry entry in buildSL.VisibleExpandedEntries)
            {
                topLeft.Y = entry.Y;
                if (entry.TryGet(out ModuleHeader header))
                    header.Draw(ScreenManager, topLeft);
                else if (!entry.Hovered)
                {
                    var ship = entry.Get<Ship>();
                    batch.Draw(ship.BaseHull.Icon, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30), Color.White);
                    var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold,
                        ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform
                            ? ship.Name + " " + Localizer.Token(2041)
                            : ship.Name, position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;

                    var role = ship.BaseHull.Name;
                    batch.DrawString(Fonts.Arial8Bold, role, position, Color.Orange);
                    position.X = position.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    batch.DrawString(Fonts.Arial8Bold,
                        $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", position, Color.Orange);


                    //Forgive my hacks this code of nightmare must GO!
                    position.X = (entry.Right - 120);
                    var iconProd = ResourceManager.Texture("NewUI/icon_production");
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                    position = new Vector2((destinationRectangle2.X - 60),
                        (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    // Use correct upkeep method depending on mod settings
                    string upkeep;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = ship.GetMaintCostRealism(p.Owner).ToString("F2");
                    }
                    else
                    {
                        upkeep = ship.GetMaintCost(p.Owner).ToString("F2");
                    }

                    batch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, ((int) (ship.GetCost(p.Owner) * p.ShipBuildingModifier)).ToString(),
                        position, Color.White);
                }
                else
                {
                    var ship = entry.Get<Ship>();

                    topLeft.Y = entry.Y;
                    batch.Draw(ship.BaseHull.Icon, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30), Color.White);
                    Vector2 position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold,
                        ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform
                            ? ship.Name + " " + Localizer.Token(2041)
                            : ship.Name, position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;

                    //var role = Localizer.GetRole(ship.shipData.HullRole, EmpireManager.Player);
                    var role = ship.BaseHull.Name;
                    batch.DrawString(Fonts.Arial8Bold, role, position, Color.Orange);
                    position.X = position.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    batch.DrawString(Fonts.Arial8Bold,
                        $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", position, Color.Orange);

                    position.X = (entry.Right - 120);
                    Texture2D iconProd = ResourceManager.Texture("NewUI/icon_production");
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                    position = new Vector2((destinationRectangle2.X - 60),
                        (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    // Use correct upkeep method depending on mod settings
                    string upkeep;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = entry.Get<Ship>().GetMaintCostRealism(p.Owner).ToString("F2");
                    }
                    else
                    {
                        upkeep = entry.Get<Ship>().GetMaintCost(p.Owner).ToString("F2");
                    }

                    batch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12Bold,
                        ((int) (entry.Get<Ship>().GetCost(p.Owner) * p.ShipBuildingModifier)).ToString(), position,
                        Color.White);
                    entry.DrawPlusEdit(batch);
                }
            }

            playerDesignsToggle.Draw(ScreenManager);
        }

        private void DrawBuildTroopsList(SpriteBatch batch)
        {
            if (Reset)
            {
                buildSL.Reset();
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (p.Owner.WeCanBuildTroop(troopType))
                        buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), true, false);
                }

                Reset = false;
            }

            Texture2D iconProd = ResourceManager.Texture("NewUI/icon_production");
            var topLeft = new Vector2((build.Menu.X + 20), (build.Menu.Y + 45));
            foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
            {
                topLeft.Y = entry.Y;
                var troop = entry.Get<Troop>();
                if (!entry.Hovered)
                {
                    troop.Draw(batch, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30));
                    var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12Bold, ((int) troop.GetCost()).ToString(), position, Color.White);

                    entry.DrawPlusEdit(batch);
                }
                else
                {
                    topLeft.Y = entry.Y;
                    troop.Draw(batch, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30));
                    var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                    position.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12Bold, ((int) troop.GetCost()).ToString(), position, Color.White);

                    entry.DrawPlusEdit(batch);
                }
            }
        }

        private void DrawConstructionQueue(SpriteBatch batch)
        {
            QSL.SetItems(p.ConstructionQueue);
            QSL.DrawDraggedEntry(batch);

            foreach (ScrollList.Entry entry in QSL.VisibleExpandedEntries)
            {
                entry.CheckHoverNoSound(Input.CursorPosition);

                var qi = entry.Get<QueueItem>();
                var position = new Vector2(entry.X + 40f, entry.Y);
                DrawText(ref position, qi.DisplayText);
                var r = new Rectangle((int)position.X, (int)position.Y, LowRes ? 120 : 150, 18);

                if (qi.isBuilding)
                {
                    Texture2D icon = ResourceManager.Texture($"Buildings/icon_{qi.Building.Icon}_48x48");
                    batch.Draw(icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
                    new ProgressBar(r, qi.Cost, qi.productionTowards).Draw(batch);
                }
                else if (qi.isShip)
                {
                    batch.Draw(qi.sData.Icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
                    new ProgressBar(r, qi.Cost * p.ShipBuildingModifier, qi.productionTowards).Draw(batch);
                }
                else if (qi.isTroop)
                {
                    Troop template = ResourceManager.GetTroopTemplate(qi.troopType);
                    template.Draw(batch, new Rectangle(entry.X, entry.Y, 29, 30));
                    new ProgressBar(r, qi.Cost, qi.productionTowards).Draw(batch);
                }

                entry.DrawUpDownApplyCancel(batch, Input);
                entry.DrawPlus(batch);
            }

            QSL.Draw(batch);
        }

        private void DrawBuildingsWeCanBuild(SpriteBatch batch)
        {
            Array<Building> buildingsWeCanBuildHere = p.GetBuildingsCanBuild();
            if (p.BuildingList.Count != buildingsHereLast || buildingsCanBuildLast != buildingsWeCanBuildHere.Count || Reset)
            {
                BuildingsCanBuild = buildingsWeCanBuildHere;
                buildSL.SetItems(BuildingsCanBuild);
                Reset = false;
            }

            foreach (ScrollList.Entry entry in buildSL.VisibleExpandedEntries)
            {
                if (!entry.TryGet(out Building building))
                    continue;

                Texture2D icon = ResourceManager.Texture($"Buildings/icon_{building.Icon}_48x48");
                Texture2D iconProd = ResourceManager.Texture("NewUI/icon_production");

                bool unprofitable = !p.WeCanAffordThis(building, p.colonyType) && building.Maintenance > 0f;
                Color buildColor = unprofitable ? Color.IndianRed : Color.White;
                if (entry.Hovered) buildColor = Color.White; // hover color

                string descr = Localizer.Token(building.ShortDescriptionIndex) + (unprofitable ? " (unprofitable)" : "");
                descr = Fonts.Arial8Bold.ParseText(descr, LowRes ? 200f : 280f);

                var position = new Vector2(build.Menu.X + 60f, entry.Y - 4f);

                batch.Draw(icon, new Rectangle(entry.X, entry.Y, 29, 30), buildColor);
                DrawText(ref position, building.NameTranslationIndex, buildColor);

                if (!entry.Hovered)
                {
                    batch.DrawString(Fonts.Arial8Bold, descr, position, unprofitable ? Color.Chocolate : Color.Green);
                    position.X = (entry.Right - 100);
                    var r = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, r, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                    position = new Vector2( (r.X - 60),
                         (1 + r.Y + r.Height / 2 -
                                 Fonts.Arial12Bold.LineSpacing / 2));
                    string maintenance = building.Maintenance.ToString("F2");
                    batch.DrawString(Fonts.Arial8Bold, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                    // ~~~~

                    position = new Vector2((r.X + 26),
                        (r.Y + r.Height / 2 -
                                 Fonts.Arial12Bold.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12Bold,
                        ((int) building.Cost * UniverseScreen.GamePaceStatic).ToString(CultureInfo.InvariantCulture), position, Color.White);

                    entry.DrawPlus(batch);
                }
                else
                {
                    batch.DrawString(Fonts.Arial8Bold, descr, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var r = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, r, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                    position = new Vector2((r.X - 60),
                                           (1 + r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    float actualMaint = building.Maintenance + building.Maintenance * p.Owner.data.Traits.MaintMod;
                    string maintenance = actualMaint.ToString("F2");
                    batch.DrawString(Fonts.Arial8Bold, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2((r.X + 26),
                        (r.Y + r.Height / 2 -
                                 Fonts.Arial12Bold.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12Bold,
                        ((int) building.Cost * UniverseScreen.GamePaceStatic).ToString(CultureInfo.InvariantCulture), position, Color.White);
                    entry.DrawPlus(batch);
                }

                entry.CheckHover(currentMouse);
            }
        }

        private Color TextColor { get; } = new Color(255, 239, 208);

        private void DrawText(ref Vector2 cursor, int tokenId)
        {
            DrawText(ref cursor, tokenId, Color.White);
        }

        private void DrawText(ref Vector2 cursor, string text)
        {
            DrawText(ref cursor, text, Color.White);
        }

        private void DrawText(ref Vector2 cursor, int tokenId, Color color)
        {
            DrawText(ref cursor, Localizer.Token(tokenId), color);
        }

        private void DrawText(ref Vector2 cursor, string text, Color color)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, color);
            cursor.Y += Fonts.Arial12Bold.LineSpacing;
        }

        private void DrawTitledLine(ref Vector2 cursor, int titleId, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(titleId) +": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, textCursor, TextColor);
            cursor.Y += Fonts.Arial12Bold.LineSpacing;
        }

        private void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        private string MultiLineFormat(string text)
        {
            return Fonts.Arial12Bold.ParseText(text, pFacilities.Menu.Width - 40);
        }

        private string MultiLineFormat(int token)
        {
            return MultiLineFormat(Localizer.Token(token));
        }

        private void DrawMultiLine(ref Vector2 cursor, string text, Color color)
        {
            string multiline = MultiLineFormat(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, multiline, cursor, color);
            cursor.Y += (Fonts.Arial12Bold.MeasureString(multiline).Y + Fonts.Arial12Bold.LineSpacing);
        }

        private void DrawCommoditiesArea(Vector2 bCursor)
        {
            string text = MultiLineFormat(4097);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, bCursor, TextColor);
        }

        private void DrawDetailInfo(Vector2 bCursor)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            object[] plusFlatFoodAmount;
            float plusFlatPopulation;
            if (pFacilities.Tabs.Count > 1 && pFacilities.Tabs[1].Selected)
            {
                DrawCommoditiesArea(bCursor);
                return;
            }
            if (detailInfo is Troop t)
            {
                spriteBatch.DrawString(Fonts.Arial20Bold, t.DisplayNameEmpire(p.Owner), bCursor, TextColor);
                bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 2);
                string strength = t.Strength < t.ActualStrengthMax ? t.Strength + "/" + t.ActualStrengthMax
                                                                   : t.ActualStrengthMax.String(1);

                DrawMultiLine(ref bCursor, t.Description);
                DrawTitledLine(ref bCursor, 338, t.TargetType);
                DrawTitledLine(ref bCursor, 339, strength);
                DrawTitledLine(ref bCursor, 2218, t.NetHardAttack.ToString());
                DrawTitledLine(ref bCursor, 2219, t.NetSoftAttack.ToString());
                DrawTitledLine(ref bCursor, 6008, t.BoardingStrength.ToString());
                DrawTitledLine(ref bCursor, 6023, t.Level.ToString());
            }
            if (detailInfo is string)
            {
                DrawMultiLine(ref bCursor, p.Description);

                string desc = "";
                if (p.Owner.data.Traits.Cybernetic != 0)  desc = Localizer.Token(2028);
                else if (p.FS == Planet.GoodState.EXPORT) desc = Localizer.Token(2025);
                else if (p.FS == Planet.GoodState.IMPORT) desc = Localizer.Token(2026);
                else if (p.FS == Planet.GoodState.STORE)  desc = Localizer.Token(2027);
                DrawMultiLine(ref bCursor, desc);

                desc = "";
                if      (p.PS == Planet.GoodState.EXPORT) desc = Localizer.Token(345);
                else if (p.PS == Planet.GoodState.IMPORT) desc = Localizer.Token(346);
                else if (p.PS == Planet.GoodState.STORE)  desc = Localizer.Token(347);
                DrawMultiLine(ref bCursor, desc);

                bool cybernetic = p.Owner.data.Traits.Cybernetic != 0;
                float production = cybernetic ? p.ProductionHere + p.NetProductionPerTurn : p.FoodHere + p.NetFoodPerTurn;
                if (production - p.Consumption < 0f)
                    DrawMultiLine(ref bCursor, Localizer.Token(344), Color.LightPink);
            }
            else if (detailInfo is PlanetGridSquare pgs)
            {
                if (pgs.building == null && pgs.Habitable && pgs.Biosphere)
                {
                    spriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(348), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                    spriteBatch.DrawString(Fonts.Arial12Bold, MultiLineFormat(349), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (pgs.building == null && pgs.Habitable)
                {
                    spriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(350), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                    spriteBatch.DrawString(Fonts.Arial12Bold, MultiLineFormat(349), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (!pgs.Habitable && pgs.building == null)
                {
                    if (p.Type == "Barren")
                    {
                        spriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(351), bCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                        spriteBatch.DrawString(Fonts.Arial12Bold, MultiLineFormat(352), bCursor, new Color(255, 239, 208));
                        return;
                    }
                    spriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(351), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                    spriteBatch.DrawString(Fonts.Arial12Bold, MultiLineFormat(353), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (pgs.building != null)
                {
                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    spriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    spriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(pgs.building.NameTranslationIndex), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                    string text = MultiLineFormat(pgs.building.DescriptionIndex);
                    spriteBatch.DrawString(Fonts.Arial12Bold, text, bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(text).Y + Fonts.Arial20Bold.LineSpacing);
                    if (pgs.building.PlusFlatFoodAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteFont arial12Bold = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatFoodAmount, " ", Localizer.Token(354) };
                        spriteBatch.DrawString(arial12Bold, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFoodPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = spriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFoodPerColonist, " ", Localizer.Token(2042) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.IsSensor && pgs.building.SensorRange != 0f)
                    {
                        Rectangle fIcon;
                        if (ResourceManager.TextureLoaded("NewUI/icon_sensors"))
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_sensors").Width, ResourceManager.Texture("NewUI/icon_sensors").Height);
                            spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_sensors"), fIcon, Color.White);
                        }
                        else
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("transparent").Width, ResourceManager.Texture("Textures/transparent").Height);
                            spriteBatch.Draw(ResourceManager.Texture("transparent"), fIcon, Color.White);
                        }
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = spriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "", pgs.building.SensorRange, " ", Localizer.Token(6000) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.IsProjector && pgs.building.ProjectorRange != 0f)
                    {
                        Rectangle fIcon;
                        if (ResourceManager.TextureLoaded("NewUI/icon_projection"))
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_projection").Width, ResourceManager.Texture("NewUI/icon_projection").Height);
                            spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_projection"), fIcon, Color.White);
                        }
                        else
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("transparent").Width, ResourceManager.Texture("Textures/transparent").Height);
                            spriteBatch.Draw(ResourceManager.Texture("transparent"), fIcon, Color.White);
                        }
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = spriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "", pgs.building.ProjectorRange, " ", Localizer.Token(6001) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatProductionAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch2 = spriteBatch;
                        SpriteFont arial12Bold1 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatProductionAmount, " ", Localizer.Token(355) };
                        spriteBatch2.DrawString(arial12Bold1, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusProdPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch3 = spriteBatch;
                        SpriteFont spriteFont1 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusProdPerColonist, " ", Localizer.Token(356) };
                        spriteBatch3.DrawString(spriteFont1, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatPopulation != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.Texture("NewUI/icon_population").Width, ResourceManager.Texture("NewUI/icon_population").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_population"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch4 = spriteBatch;
                        SpriteFont arial12Bold2 = Fonts.Arial12Bold;
                        plusFlatPopulation = pgs.building.PlusFlatPopulation / 1000f;
                        spriteBatch4.DrawString(arial12Bold2, string.Concat("+", plusFlatPopulation.ToString("#.00"), " ", Localizer.Token(2043)), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatResearchAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch5 = spriteBatch;
                        SpriteFont spriteFont2 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatResearchAmount, " ", Localizer.Token(357) };
                        spriteBatch5.DrawString(spriteFont2, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusResearchPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch6 = spriteBatch;
                        SpriteFont arial12Bold3 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusResearchPerColonist, " ", Localizer.Token(358) };
                        spriteBatch6.DrawString(arial12Bold3, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusTaxPercentage != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_money").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch7 = spriteBatch;
                        SpriteFont spriteFont3 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+ ", pgs.building.PlusTaxPercentage * 100f, "% ", Localizer.Token(359) };
                        spriteBatch7.DrawString(spriteFont3, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.MinusFertilityOnBuild != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.LightPink);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(360), ": ", pgs.building.MinusFertilityOnBuild), tCursor, Color.LightPink);
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlanetaryShieldStrengthAdded != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.Texture("NewUI/icon_planetshield").Width, ResourceManager.Texture("NewUI/icon_planetshield").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_planetshield"), fIcon, Color.Green);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(361), ": "), tCursor, Color.White);
                        tCursor.X = tCursor.X + Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(361), ": ")).X;
                        spriteBatch.DrawString(Fonts.Arial12Bold, pgs.building.PlanetaryShieldStrengthAdded.ToString(), tCursor, Color.LightGreen);
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.CreditsPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_money").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(362), ": ", pgs.building.CreditsPerColonist), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusProdPerRichness != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(363), ": ", pgs.building.PlusProdPerRichness), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.CombatStrength > 0)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("Ground_UI/Ground_Attack").Width, ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        spriteBatch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(364), ": ", pgs.building.CombatStrength), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.Maintenance > 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch8 = spriteBatch;
                        SpriteFont arial12Bold4 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "-", pgs.building.Maintenance + pgs.building.Maintenance * p.Owner.data.Traits.MaintMod, " ", Localizer.Token(365) };
                        spriteBatch8.DrawString(arial12Bold4, string.Concat(plusFlatFoodAmount), tCursor, Color.LightPink);
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.ShipRepair != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_queue_rushconstruction"), fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                        spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("+", pgs.building.ShipRepair, " ", Localizer.Token(6137)), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 12);
                    }
                    if (pgs.building.Scrappable)
                    {
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                        spriteBatch.DrawString(Fonts.Arial12Bold, "You may scrap this building by right clicking it", bCursor, Color.White);
                    }
                }
            }
            else if (detailInfo is ScrollList.Entry entry)
            {
                var temp = entry.Get<Building>();
                spriteBatch.DrawString(Fonts.Arial20Bold, temp.Name, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
                string text = MultiLineFormat(temp.DescriptionIndex);
                spriteBatch.DrawString(Fonts.Arial12Bold, text, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(text).Y + Fonts.Arial20Bold.LineSpacing);
                if (temp.PlusFlatFoodAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch9 = spriteBatch;
                    SpriteFont spriteFont4 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatFoodAmount, " ", Localizer.Token(354) };
                    spriteBatch9.DrawString(spriteFont4, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFoodPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = spriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFoodPerColonist, " ", Localizer.Token(2042) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.IsSensor && temp.SensorRange != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_sensors").Width, ResourceManager.Texture("NewUI/icon_sensors").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_sensors"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = spriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "", temp.SensorRange, " ", Localizer.Token(6000) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.IsProjector && temp.ProjectorRange != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_projection").Width, ResourceManager.Texture("NewUI/icon_projection").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_projection"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = spriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "", temp.ProjectorRange, " ", Localizer.Token(6001) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatProductionAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch11 = spriteBatch;
                    SpriteFont spriteFont5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatProductionAmount, " ", Localizer.Token(355) };
                    spriteBatch11.DrawString(spriteFont5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusProdPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch12 = spriteBatch;
                    SpriteFont arial12Bold6 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusProdPerColonist, " ", Localizer.Token(356) };
                    spriteBatch12.DrawString(arial12Bold6, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatResearchAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch13 = spriteBatch;
                    SpriteFont spriteFont6 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatResearchAmount, " ", Localizer.Token(357) };
                    spriteBatch13.DrawString(spriteFont6, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusResearchPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch14 = spriteBatch;
                    SpriteFont arial12Bold7 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusResearchPerColonist, " ", Localizer.Token(358) };
                    spriteBatch14.DrawString(arial12Bold7, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatPopulation != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.Texture("NewUI/icon_population").Width, ResourceManager.Texture("NewUI/icon_population").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_population"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch15 = spriteBatch;
                    SpriteFont spriteFont7 = Fonts.Arial12Bold;
                    plusFlatPopulation = temp.PlusFlatPopulation / 1000f;
                    spriteBatch15.DrawString(spriteFont7, string.Concat("+", plusFlatPopulation.ToString("#.00"), " ", Localizer.Token(2043)), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusTaxPercentage != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_money").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch16 = spriteBatch;
                    SpriteFont arial12Bold8 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+ ", temp.PlusTaxPercentage * 100f, "% ", Localizer.Token(359) };
                    spriteBatch16.DrawString(arial12Bold8, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.MinusFertilityOnBuild != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.LightPink);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(360), ": ", temp.MinusFertilityOnBuild), tCursor, Color.LightPink);
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlanetaryShieldStrengthAdded != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.Texture("NewUI/icon_planetshield").Width, ResourceManager.Texture("NewUI/icon_planetshield").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_planetshield"), fIcon, Color.Green);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(361), ": "), tCursor, Color.White);
                    tCursor.X = tCursor.X + Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(361), ": ")).X;
                    spriteBatch.DrawString(Fonts.Arial12Bold, temp.PlanetaryShieldStrengthAdded.ToString(), tCursor, Color.LightGreen);
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.CreditsPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_money").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(362), ": ", temp.CreditsPerColonist), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusProdPerRichness != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(363), ": ", temp.PlusProdPerRichness), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.CombatStrength > 0)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("Ground_UI/Ground_Attack").Width, ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                    spriteBatch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(364), ": ", temp.CombatStrength), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.Maintenance > 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch17 = spriteBatch;
                    SpriteFont spriteFont8 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "-", temp.Maintenance + temp.Maintenance * p.Owner.data.Traits.MaintMod, " ", Localizer.Token(365) };
                    spriteBatch17.DrawString(spriteFont8, string.Concat(plusFlatFoodAmount), tCursor, Color.LightPink);
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.ShipRepair != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/icon_queue_rushconstruction"), fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + fIcon.Width + 5f, bCursor.Y + 3f);
                    spriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("+", temp.ShipRepair, " ", Localizer.Token(6137)), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
        }

        private void DrawTroopLevel(Troop troop, Rectangle rect)
        {
            SpriteFont font = Fonts.Arial12Bold;
            var levelRect   = new Rectangle(rect.X + 30, rect.Y + 22, font.LineSpacing, font.LineSpacing + 5);
            var pos         = new Vector2((rect.X + 15 + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                                         (1 + rect.Y + 5 + rect.Height / 2 - font.LineSpacing / 2));

            ScreenManager.SpriteBatch.FillRectangle(levelRect, new Color(0, 0, 0, 200));
            ScreenManager.SpriteBatch.DrawRectangle(levelRect, troop.GetOwner().EmpireColor);
            ScreenManager.SpriteBatch.DrawString(font, troop.Level.ToString(), pos, Color.Gold);
        }

        private void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                Troop troop        = pgs.TroopsHere[0];
                pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 48, pgs.ClickRect.Y, 48, 48);
                troop.DrawIcon(ScreenManager.SpriteBatch, pgs.TroopClickRect);
                if (troop.Level > 0)
                    DrawTroopLevel(troop, pgs.TroopClickRect);
            }
            float numFood = 0f;
            float numProd = 0f;
            float numRes  = 0f;
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood = numFood + pgs.building.PlusFoodPerColonist * p.Population / 1000f * p.FarmerPercentage;
                    numFood = numFood + pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd = numProd + pgs.building.PlusFlatProductionAmount;
                    numProd = numProd + pgs.building.PlusProdPerColonist * p.Population / 1000f * p.WorkerPercentage;
                }
                if (pgs.building.PlusProdPerRichness > 0f)
                {
                    numProd = numProd + pgs.building.PlusProdPerRichness * p.MineralRichness;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes = numRes + pgs.building.PlusResearchPerColonist * p.Population / 1000f * p.ResearcherPercentage;
                    numRes = numRes + pgs.building.PlusFlatResearchAmount;
                }
            }
            float total = numFood + numProd + numRes;
            float totalSpace = pgs.ClickRect.Width - 30;
            float spacing = totalSpace / total;
            Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.Texture("NewUI/icon_food").Height, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            for (int i = 0; (float)i < numFood; i++)
            {
                if (numFood - i <= 0f || numFood - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), rect, Color.White);
                }
                else
                {
                    Rectangle? nullable = null;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), new Vector2(rect.X, rect.Y), nullable, Color.White, 0f, Vector2.Zero, numFood - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numProd; i++)
            {
                if (numProd - i <= 0f || numProd - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), rect, Color.White);
                }
                else
                {
                    Rectangle? nullable1 = null;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), new Vector2(rect.X, rect.Y), nullable1, Color.White, 0f, Vector2.Zero, numProd - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numRes; i++)
            {
                if (numRes - i <= 0f || numRes - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rect, Color.White);
                }
                else
                {
                    Rectangle? nullable2 = null;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), new Vector2(rect.X, rect.Y), nullable2, Color.White, 0f, Vector2.Zero, numRes - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
        }

        public static int GetIndex(Planet p)
        {
            switch (p.colonyType)
            {
                case Planet.ColonyType.Colony: return 0;
                case Planet.ColonyType.Core: return 1;
                case Planet.ColonyType.Industrial: return 2;
                case Planet.ColonyType.Agricultural: return 3;
                case Planet.ColonyType.Research: return 4;
                case Planet.ColonyType.Military: return 5;
                case Planet.ColonyType.TradeHub: return 6;
            }
            return 0;
        }

        private void HandleDetailInfo(InputState input)
        {
            detailInfo = null;
            foreach (ScrollList.Entry e in buildSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(input))
                {
                    if (e.Is<Building>())   detailInfo = e; // @todo Why are we storing Entry here???
                    else if (e.Is<Troop>()) detailInfo = e.item;
                }
            }
            if (detailInfo == null)
                detailInfo = p.Description;
        }

        public override bool HandleInput(InputState input)
        {
            pFacilities.HandleInputNoReset();
            if (RightColony.Rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2279));
            }
            if (LeftColony.Rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2280));
            }
            // Changed by MadMudMonster: only respond to mouse press, not release
            if ((input.Right || RightColony.HandleInput(input) && input.LeftMouseClick)
                && (Empire.Universe.Debug || p.Owner == EmpireManager.Player))
            {
                try
                {
                    int thisindex = p.Owner.GetPlanets().IndexOf(p);
                    thisindex = (thisindex >= p.Owner.GetPlanets().Count - 1 ? 0 : thisindex + 1);
                    if (p.Owner.GetPlanets()[thisindex] != p)
                    {
                        p = p.Owner.GetPlanets()[thisindex];
                        Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, p, eui);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Colony Screen HandleInput(). Likely null reference.");
                }
                if (input.MouseCurr.RightButton != ButtonState.Released || previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }
            // Changed by MadMudMonster: only respond to mouse press, not release
            if ((input.Left || LeftColony.HandleInput(input) && input.LeftMouseClick)
                && (Empire.Universe.Debug || p.Owner == EmpireManager.Player))
            {
                int thisindex = p.Owner.GetPlanets().IndexOf(p);
                thisindex = (thisindex <= 0 ? p.Owner.GetPlanets().Count - 1 : thisindex - 1);
                if (p.Owner.GetPlanets()[thisindex] != p)
                {
                    //Console.Write("Switch Colony Screen");
                    //Console.WriteLine(thisindex);
                    //System.Threading.Thread.Sleep(1000);

                    p = p.Owner.GetPlanets()[thisindex];
                    Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, p, eui);
                }
                if (input.MouseCurr.RightButton != ButtonState.Released || previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }
            p.UpdateIncomes(false);
            HandleDetailInfo(input);
            currentMouse = Mouse.GetState();
            Vector2 MousePos = new Vector2(currentMouse.X, currentMouse.Y);
            buildSL.HandleInput(input);
            build.HandleInput(this);
            if (p.Owner != EmpireManager.Player)
            {
                HandleDetailInfo(input);
                if (input.MouseCurr.RightButton != ButtonState.Released || previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }

            if (!edit_name_button.HitTest(MousePos))
            {
                editHoverState = 0;
            }
            else
            {
                editHoverState = 1;
                if (input.LeftMouseClick)
                {
                    PlanetName.HandlingInput = true;
                }
            }
            if (!PlanetName.HandlingInput)
            {
                GlobalStats.TakingInput = false;
                bool empty = true;
                string text = PlanetName.Text;
                int num = 0;
                while (num < text.Length)
                {
                    if (text[num] == ' ')
                    {
                        num++;
                    }
                    else
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty)
                {
                    int ringnum = 1;
                    foreach (SolarSystem.Ring ring in p.ParentSystem.RingList)
                    {
                        if (ring.planet == p)
                        {
                            PlanetName.Text = string.Concat(p.ParentSystem.Name, " ", NumberToRomanConvertor.NumberToRoman(ringnum));
                        }
                        ringnum++;
                    }
                }
            }
            else
            {
                GlobalStats.TakingInput = true;
                PlanetName.HandleTextInput(ref PlanetName.Text, input);
            }
            GovernorDropdown.HandleInput(input);
            if (GovernorDropdown.ActiveValue != (int)p.colonyType)
            {
                p.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                if (p.colonyType != Planet.ColonyType.Colony)
                {
                    p.FoodLocked = true;
                    p.ProdLocked = true;
                    p.ResLocked = true;
                    p.GovernorOn = true;
                }
                else
                {
                    p.GovernorOn = false;
                    p.FoodLocked = false;
                    p.ProdLocked = false;
                    p.ResLocked = false;
                }
            }
            HandleSlider(input);
            if (p.HasShipyard && build.Tabs.Count > 1 && build.Tabs[1].Selected)
            {
                if (playerDesignsToggle.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2225));
                }
                if (playerDesignsToggle.HandleInput(input) && !input.LeftMouseReleased)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
                    if (GlobalStats.ShowAllDesigns)
                    {
                        playerDesignsToggle.Active = true;
                    }
                    else
                    {
                        playerDesignsToggle.Active = false;
                    }
                    Reset = true;
                }
            }
            if (p.colonyType != Planet.ColonyType.Colony)
            {
                FoodLock.Locked = true;
                ProdLock.Locked = true;
                ResLock.Locked = true;
            }
            else
            {
                if (!FoodLock.LockRect.HitTest(MousePos) || p.Owner == null || p.Owner.data.Traits.Cybernetic != 0)
                {
                    FoodLock.Hover = false;
                }
                else
                {
                    if (FoodLock.Locked)
                    {
                        FoodLock.Hover = false;
                        if (input.LeftMouseClick)
                        {
                            p.FoodLocked = false;
                            FoodLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        FoodLock.Hover = true;
                        if (input.LeftMouseClick)
                        {
                            p.FoodLocked = true;
                            FoodLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
                if (!ProdLock.LockRect.HitTest(MousePos))
                {
                    ProdLock.Hover = false;
                }
                else
                {
                    if (ProdLock.Locked)
                    {
                        ProdLock.Hover = false;
                        if (input.LeftMouseClick)
                        {
                            p.ProdLocked = false;
                            ProdLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        ProdLock.Hover = true;
                        if (input.LeftMouseClick)
                        {
                            p.ProdLocked = true;
                            ProdLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
                if (!ResLock.LockRect.HitTest(MousePos))
                {
                    ResLock.Hover = false;
                }
                else
                {
                    if (ResLock.Locked)
                    {
                        ResLock.Hover = false;
                        if (input.LeftMouseClick)
                        {
                            p.ResLocked = false;
                            ResLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        ResLock.Hover = true;
                        if (input.LeftMouseClick)
                        {
                            p.ResLocked = true;
                            ResLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
            }
            selector = null;
            ClickedTroop = false;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (!pgs.ClickRect.HitTest(MousePos))
                {
                    pgs.highlighted = false;
                }
                else
                {
                    if (!pgs.highlighted)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    pgs.highlighted = true;
                }
                if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(MousePos))
                {
                    continue;
                }
                detailInfo = pgs.TroopsHere[0];
                if (input.RightMouseClick && pgs.TroopsHere[0].GetOwner() == EmpireManager.Player)
                {
                    GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    Ship.CreateTroopShipAtPoint(p.Owner.data.DefaultTroopShip, p.Owner, p.Center, pgs.TroopsHere[0]);
                    p.TroopsHere.Remove(pgs.TroopsHere[0]);
                    pgs.TroopsHere[0].SetPlanet(null);
                    pgs.TroopsHere.Clear();
                    ClickedTroop = true;
                    detailInfo = null;
                    rmouse = true;
                }
                return true;                
            }
            if (!ClickedTroop)
            {
                foreach (PlanetGridSquare pgs in p.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        detailInfo = pgs;
                        var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                        if (pgs.building != null  && bRect.HitTest(input.CursorPosition) &&  Input.RightMouseClick)
                        {
                            if (pgs.building.Scrappable)
                            {
                                toScrap = pgs.building;
                                string message = string.Concat("Do you wish to scrap ", Localizer.Token(pgs.building.NameTranslationIndex), "? Half of the building's construction cost will be recovered to your storage.");
                                var messageBox = new MessageBoxScreen(Empire.Universe, message);
                                messageBox.Accepted += ScrapAccepted;
                                ScreenManager.AddScreen(messageBox);                                
                                
                            }
                            rmouse = true;
                            ClickedTroop = true;
                            return true;
                        }
                    }
                    if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(input.CursorPosition))
                    {
                        continue;
                    }
                    detailInfo = pgs.TroopsHere;
                }
            }
            if (!GlobalStats.HardcoreRuleset)
            {
                if (foodDropDown.r.HitTest(MousePos) && input.LeftMouseClick)
                {
                    foodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    p.FS = (Planet.GoodState)((int)p.FS + (int)Planet.GoodState.IMPORT);
                    if (p.FS > Planet.GoodState.EXPORT)
                        p.FS = Planet.GoodState.STORE;
                }
                if (prodDropDown.r.HitTest(MousePos) && input.LeftMouseClick)
                {
                    prodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    p.PS = (Planet.GoodState)((int)p.PS + (int)Planet.GoodState.IMPORT);
                    if (p.PS > Planet.GoodState.EXPORT)
                        p.PS = Planet.GoodState.STORE;
                }
            }
            else
            {
                foreach (ThreeStateButton b in ResourceButtons)
                    b.HandleInput(input, ScreenManager);
            }

            HandleConstructionQueueInput(input);

            if (ActiveBuildingEntry != null)
            {
                foreach (PlanetGridSquare pgs in p.TilesList)
                {
                    if (!pgs.ClickRect.HitTest(MousePos) || currentMouse.LeftButton != ButtonState.Released || previousMouse.LeftButton != ButtonState.Pressed)
                    {
                        continue;
                    }
                    if (pgs.Habitable && pgs.building == null && pgs.QItem == null && (ActiveBuildingEntry.item as Building).Name != "Biospheres")
                    {
                        QueueItem qi = new QueueItem(p);
                        //p.SbProduction.AddBuildingToCQ(this.ActiveBuildingEntry.item as Building, PlayerAdded: true);
                        qi.isBuilding = true;
                        qi.Building = ActiveBuildingEntry.item as Building;       //ResourceManager.GetBuilding((this.ActiveBuildingEntry.item as Building).Name);
                        qi.IsPlayerAdded = true;
                        qi.Cost = ResourceManager.BuildingsDict[qi.Building.Name].Cost * UniverseScreen.GamePaceStatic;
                        qi.productionTowards = 0f;
                        qi.pgs = pgs;
                        //};
                        pgs.QItem = qi;
                        p.ConstructionQueue.Add(qi);
                        ActiveBuildingEntry = null;
                        break;
                    }

                    if (pgs.Habitable || pgs.Biosphere || pgs.QItem != null || !(ActiveBuildingEntry.item as Building).CanBuildAnywhere)
                    {
                        GameAudio.PlaySfxAsync("UI_Misc20");
                        ActiveBuildingEntry = null;
                        break;
                    }

                    {
                        QueueItem qi = new QueueItem(p);
                        //{
                        qi.isBuilding = true;
                        qi.Building = ActiveBuildingEntry.item as Building;
                        qi.Cost = qi.Building.Cost *UniverseScreen.GamePaceStatic; //ResourceManager.BuildingsDict[qi.Building.Name].Cost 
                        qi.productionTowards = 0f;
                        qi.pgs = pgs;
                        qi.IsPlayerAdded = true;
                        //};
                        pgs.QItem = qi;
                        p.ConstructionQueue.Add(qi);
                        ActiveBuildingEntry = null;
                        break;
                    }
                }
                if (ActiveBuildingEntry != null)
                {
                    foreach (QueueItem qi in p.ConstructionQueue)
                    {
                        if (!qi.isBuilding || qi.Building.Name != (ActiveBuildingEntry.item as Building).Name || !(ActiveBuildingEntry.item as Building).Unique)
                        {
                            continue;
                        }
                        ActiveBuildingEntry = null;
                        break;
                    }
                }
                if (currentMouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Released)
                {
                    ClickedTroop = true;
                    ActiveBuildingEntry = null;
                }
                if (input.LeftMouseClick)
                {
                    ClickedTroop = true;
                    ActiveBuildingEntry = null;
                }
            }
            foreach (ScrollList.Entry e in buildSL.VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader header)
                {
                    header.HandleInput(input, e);
                }
                else if (e.CheckHover(input))
                {
                    selector = e.CreateSelector();

                    if (input.LeftMouseHeldDown && e.item is Building && ActiveBuildingEntry == null)
                    {
                        ActiveBuildingEntry = e;
                    }
                    if (input.LeftMouseReleased)
                    {
                        if (ClickTimer >= TimerDelay)
                        {
                            ClickTimer = 0f;
                        }
                        else
                        {
                            if (!e.WasPlusHovered(input))
                            {
                                var qi = new QueueItem(p);
                                if (e.TryGet(out Ship ship))
                                {
                                    qi.isShip = true;
                                    qi.sData = ship.shipData;
                                    qi.Cost = ship.GetCost(p.Owner);
                                    qi.productionTowards = 0f;
                                    p.ConstructionQueue.Add(qi);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.TryGet(out Troop troop))
                                {
                                    qi.isTroop = true;
                                    qi.troopType = troop.Name;
                                    qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                                    qi.productionTowards = 0f;
                                    p.ConstructionQueue.Add(qi);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.TryGet(out Building building))
                                {
                                    p.AddBuildingToCQ(building, true);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                            }
                        }
                    }
                }
                if (e.CheckPlus(input))
                {
                    ToolTip.CreateTooltip(51);
                    if (input.LeftMouseClick)
                    {
                        var qi = new QueueItem(p);
                        if (e.item is Building building)
                        {
                            //Building b = ResourceManager.GetBuilding((e.item as Building).Name);
                            //b.IsPlayerAdded =true;
                            p.AddBuildingToCQ(building, true);
                        }
                        else if (e.item is Ship ship)
                        {
                            qi.isShip = true;
                            qi.sData = ship.shipData;
                            qi.Cost = ship.GetCost(p.Owner);
                            qi.productionTowards = 0f;
                            p.ConstructionQueue.Add(qi);
                        }
                        else if (e.item is Troop troop)
                        {
                            qi.isTroop = true;
                            qi.troopType = troop.Name;
                            qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                            qi.productionTowards = 0f;
                            p.ConstructionQueue.Add(qi);
                        }
                    }
                }
                if (e.CheckEdit(input))
                {
                    ToolTip.CreateTooltip(52);
                    if (input.LeftMouseClick)
                    {
                        var sdScreen = new ShipDesignScreen(Empire.Universe, eui);
                        ScreenManager.AddScreen(sdScreen);
                        sdScreen.ChangeHull((e.item as Ship).shipData);
                    }
                }
            }
            shipsCanBuildLast = p.Owner.ShipsWeCanBuild.Count;
            buildingsHereLast = p.BuildingList.Count;
            buildingsCanBuildLast = BuildingsCanBuild.Count;

            if (popup)
            {
                if (input.MouseCurr.RightButton != ButtonState.Released || input.MousePrev.RightButton != ButtonState.Released)
                    return true;
                popup = false;
            }
            else 
                {
                if (input.RightMouseClick && !ClickedTroop) rmouse = false;
                if (!rmouse && (input.MouseCurr.RightButton != ButtonState.Released || previousMouse.RightButton != ButtonState.Released))
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                previousMouse = currentMouse;
                }
            /*
            if (input.RightMouseClick && !this.ClickedTroop) rmouse = false;
            if (!rmouse && (input.CurrentMouseState.RightButton != ButtonState.Released || this.previousMouse.RightButton != ButtonState.Released))
            {
                Empire.Universe.ShipsInCombat.Active = true;
                Empire.Universe.PlanetsInCombat.Active = true;
            }
            this.previousMouse = this.currentMouse; 
            */
            return base.HandleInput(input);
        }

        private void OnSendTroopsClicked(UIButton b)
        {
            Array<Ship> troopShips;
            using (eui.empire.GetShips().AcquireReadLock())
                troopShips = new Array<Ship>(eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0
                                    && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                                    && troop.fleet == null && !troop.InCombat)
                    .OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));

            Array<Planet> planetTroops = new Array<Planet>(eui.empire.GetPlanets()
                .Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, p.Center))
                .Where(Name => Name.Name != p.Name));

            if (troopShips.Count > 0)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                troopShips.First().AI.OrderRebase(p, true);
            }
            else if (planetTroops.Count > 0)
            {
                var troops = planetTroops.First().TroopsHere;
                using (troops.AcquireWriteLock())
                {
                    Ship troop = troops.First().Launch();
                    if (troop != null)
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        troop.AI.OrderRebase(p, true);
                    }
                }
            }
            else
            {
                GameAudio.PlaySfxAsync("blip_click");
            }
        }

        private void OnLaunchTroopsClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                    continue;

                play = true;
                Ship.CreateTroopShipAtPoint(p.Owner.data.DefaultTroopShip, p.Owner, p.Center, pgs.TroopsHere[0]);
                p.TroopsHere.Remove(pgs.TroopsHere[0]);
                pgs.TroopsHere[0].SetPlanet(null);
                pgs.TroopsHere.Clear();
                ClickedTroop = true;
                detailInfo = null;
            }

            if (play)
            {
                GameAudio.PlaySfxAsync("sd_troop_takeoff");
            }
        }

        private void HandleConstructionQueueInput(InputState input)
        {
            int i = QSL.FirstVisibleIndex;
            foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(Input.CursorPosition))
                {
                    selector = e.CreateSelector();
                }

                if (e.WasUpHovered(input))
                {
                    ToolTip.CreateTooltip(63);
                    if (!input.IsCtrlKeyDown || input.LeftMouseDown || input.LeftMouseReleased)
                    {
                        if (input.LeftMouseClick && i > 0)
                        {
                            QueueItem item = p.ConstructionQueue[i - 1];
                            p.ConstructionQueue[i - 1] = p.ConstructionQueue[i];
                            p.ConstructionQueue[i] = item;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else if (i > 0)
                    {
                        QueueItem item = p.ConstructionQueue[i];
                        p.ConstructionQueue.Remove(item);
                        p.ConstructionQueue.Insert(0, item);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        break;
                    }
                }

                if (e.WasDownHovered(input))
                {
                    ToolTip.CreateTooltip(64);
                    if (!input.IsCtrlKeyDown || input.LeftMouseDown || input.LeftMouseReleased) // @todo WTF??
                    {
                        if (input.LeftMouseClick && i + 1 < QSL.NumExpandedEntries)
                        {
                            QueueItem item = p.ConstructionQueue[i + 1];
                            p.ConstructionQueue[i + 1] = p.ConstructionQueue[i];
                            p.ConstructionQueue[i] = item;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else if (i + 1 < QSL.NumExpandedEntries)
                    {
                        QueueItem item = p.ConstructionQueue[i];
                        p.ConstructionQueue.Remove(item);
                        p.ConstructionQueue.Insert(0, item);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        break;
                    }
                }

                if (e.WasApplyHovered(input) && !p.RecentCombat && p.CrippledTurns <= 0)
                {
                    if (!input.IsCtrlKeyDown || input.LeftMouseDown || input.LeftMouseReleased) // @todo WTF??
                    {
                        if (input.LeftMouseClick)
                        {
                            GameAudio.PlaySfxAsync(p.ApplyStoredProduction(i) ? "sd_ui_accept_alt3" : "UI_Misc20");
                        }
                    }
                    else if (p.ProductionHere == 0f)
                    {
                        GameAudio.PlaySfxAsync("UI_Misc20");
                    }
                    else
                    {
                        p.ApplyAllStoredProduction(i);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                }

                if (e.WasCancelHovered(input) && input.LeftMouseClick)
                {
                    var item = e.Get<QueueItem>();
                    p.ProductionHere += item.productionTowards;

                    if (item.pgs != null)
                    {
                        item.pgs.QItem = null;
                    }

                    if (item.Goal != null)
                    {
                        if (item.Goal is BuildConstructionShip)
                        {
                            p.Owner.GetGSAI().Goals.Remove(item.Goal);
                        }

                        if (item.Goal.GetFleet() != null)
                            p.Owner.GetGSAI().Goals.Remove(item.Goal);
                    }

                    p.ConstructionQueue.Remove(item);
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
                ++i;
            }

            QSL.HandleInput(input, p);
        }

        private void HandleSlider(InputState input)
        {
            Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
            if (p.Owner.data.Traits.Cybernetic == 0)
            {
                if (ColonySliderFood.sRect.HitTest(mousePos) || draggingSlider1)
                {
                    ColonySliderFood.state = "hover";
                    ColonySliderFood.Color = new Color(164, 154, 133);
                }
                else
                {
                    ColonySliderFood.state = "normal";
                    ColonySliderFood.Color = new Color(72, 61, 38);
                }
                if (ColonySliderFood.cursor.HitTest(mousePos) || draggingSlider1)
                {
                    ColonySliderFood.cState = "hover";
                }
                else
                {
                    ColonySliderFood.cState = "normal";
                }
            }
            if (ColonySliderProd.sRect.HitTest(mousePos) || draggingSlider2)
            {
                ColonySliderProd.state = "hover";
                ColonySliderProd.Color = new Color(164, 154, 133);
            }
            else
            {
                ColonySliderProd.state = "normal";
                ColonySliderProd.Color = new Color(72, 61, 38);
            }
            if (ColonySliderProd.cursor.HitTest(mousePos) || draggingSlider2)
            {
                ColonySliderProd.cState = "hover";
            }
            else
            {
                ColonySliderProd.cState = "normal";
            }
            if (ColonySliderRes.sRect.HitTest(mousePos) || draggingSlider3)
            {
                ColonySliderRes.state = "hover";
                ColonySliderRes.Color = new Color(164, 154, 133);
            }
            else
            {
                ColonySliderRes.state = "normal";
                ColonySliderRes.Color = new Color(72, 61, 38);
            }
            if (ColonySliderRes.cursor.HitTest(mousePos) || draggingSlider3)
            {
                ColonySliderRes.cState = "hover";
            }
            else
            {
                ColonySliderRes.cState = "normal";
            }
            if (ColonySliderFood.cursor.HitTest(mousePos) && (!ProdLock.Locked || !ResLock.Locked) && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed && !FoodLock.Locked)
            {
                draggingSlider1 = true;
            }
            if (ColonySliderProd.cursor.HitTest(mousePos) && (!FoodLock.Locked || !ResLock.Locked) && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed && !ProdLock.Locked)
            {
                draggingSlider2 = true;
            }
            if (ColonySliderRes.cursor.HitTest(mousePos) && (!ProdLock.Locked || !FoodLock.Locked) && currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed && !ResLock.Locked)
            {
                draggingSlider3 = true;
            }
            if (draggingSlider1 && !FoodLock.Locked && (!ProdLock.Locked || !ResLock.Locked))
            {
                ColonySliderFood.cursor.X = currentMouse.X;
                if (ColonySliderFood.cursor.X > ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width)
                {
                    ColonySliderFood.cursor.X = ColonySliderFood.sRect.X + ColonySliderFood.sRect.Width;
                }
                else if (ColonySliderFood.cursor.X < ColonySliderFood.sRect.X)
                {
                    ColonySliderFood.cursor.X = ColonySliderFood.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider1 = false;
                }
                fPercentLast = p.FarmerPercentage;
                p.FarmerPercentage = (ColonySliderFood.cursor.X - (float)ColonySliderFood.sRect.X) / ColonySliderFood.sRect.Width;
                float difference = fPercentLast - p.FarmerPercentage;
                if (!ProdLock.Locked && !ResLock.Locked)
                {
                    Planet workerPercentage = p;
                    workerPercentage.WorkerPercentage = workerPercentage.WorkerPercentage + difference / 2f;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet farmerPercentage = p;
                        farmerPercentage.FarmerPercentage = farmerPercentage.FarmerPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                    Planet researcherPercentage = p;
                    researcherPercentage.ResearcherPercentage = researcherPercentage.ResearcherPercentage + difference / 2f;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet planet = p;
                        planet.FarmerPercentage = planet.FarmerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (ProdLock.Locked && !ResLock.Locked)
                {
                    p.ResearcherPercentage += difference;
                    if (p.ResearcherPercentage < 0f)
                    {
                        p.FarmerPercentage += p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (!ProdLock.Locked && ResLock.Locked)
                {
                    Planet workerPercentage1 = p;
                    p.WorkerPercentage += difference;
                    if (p.WorkerPercentage < 0f)
                    {
                        p.FarmerPercentage += p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                }
            }
            if (draggingSlider2 && !ProdLock.Locked && (!FoodLock.Locked || !ResLock.Locked))
            {
                ColonySliderProd.cursor.X = currentMouse.X;
                if (ColonySliderProd.cursor.X > ColonySliderProd.sRect.X + ColonySliderProd.sRect.Width)
                {
                    ColonySliderProd.cursor.X = ColonySliderProd.sRect.X + ColonySliderProd.sRect.Width;
                }
                else if (ColonySliderProd.cursor.X < ColonySliderProd.sRect.X)
                {
                    ColonySliderProd.cursor.X = ColonySliderProd.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider2 = false;
                }
                pPercentLast = p.WorkerPercentage;
                p.WorkerPercentage = (ColonySliderProd.cursor.X - (float)ColonySliderProd.sRect.X) / ColonySliderProd.sRect.Width;
                float difference = pPercentLast - p.WorkerPercentage;
                if (!FoodLock.Locked && !ResLock.Locked)
                {
                    Planet farmerPercentage2 = p;
                    farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + difference / 2f;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet workerPercentage2 = p;
                        workerPercentage2.WorkerPercentage = workerPercentage2.WorkerPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                    Planet researcherPercentage2 = p;
                    researcherPercentage2.ResearcherPercentage = researcherPercentage2.ResearcherPercentage + difference / 2f;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet planet2 = p;
                        planet2.WorkerPercentage = planet2.WorkerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (FoodLock.Locked && !ResLock.Locked)
                {
                    Planet researcherPercentage3 = p;
                    researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
                    if (p.ResearcherPercentage < 0f)
                    {
                        Planet workerPercentage3 = p;
                        workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + p.ResearcherPercentage;
                        p.ResearcherPercentage = 0f;
                    }
                }
                else if (!FoodLock.Locked && ResLock.Locked)
                {
                    Planet farmerPercentage3 = p;
                    farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + difference;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet planet3 = p;
                        planet3.WorkerPercentage = planet3.WorkerPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
            }
            if (draggingSlider3 && !ResLock.Locked && (!FoodLock.Locked || !ProdLock.Locked))
            {
                ColonySliderRes.cursor.X = currentMouse.X;
                if (ColonySliderRes.cursor.X > ColonySliderRes.sRect.X + ColonySliderRes.sRect.Width)
                {
                    ColonySliderRes.cursor.X = ColonySliderRes.sRect.X + ColonySliderRes.sRect.Width;
                }
                else if (ColonySliderRes.cursor.X < ColonySliderRes.sRect.X)
                {
                    ColonySliderRes.cursor.X = ColonySliderRes.sRect.X;
                }
                if (input.LeftMouseUp)
                {
                    draggingSlider3 = false;
                }
                rPercentLast = p.ResearcherPercentage;
                p.ResearcherPercentage = (ColonySliderRes.cursor.X - (float)ColonySliderRes.sRect.X) / ColonySliderRes.sRect.Width;
                float difference = rPercentLast - p.ResearcherPercentage;
                if (!ProdLock.Locked && !FoodLock.Locked)
                {
                    Planet workerPercentage4 = p;
                    workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + difference / 2f;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet researcherPercentage4 = p;
                        researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                    Planet farmerPercentage4 = p;
                    farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference / 2f;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet planet4 = p;
                        planet4.ResearcherPercentage = planet4.ResearcherPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
                else if (ProdLock.Locked && !FoodLock.Locked)
                {
                    Planet farmerPercentage5 = p;
                    farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference;
                    if (p.FarmerPercentage < 0f)
                    {
                        Planet researcherPercentage5 = p;
                        researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + p.FarmerPercentage;
                        p.FarmerPercentage = 0f;
                    }
                }
                else if (!ProdLock.Locked && FoodLock.Locked)
                {
                    Planet workerPercentage5 = p;
                    workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference;
                    if (p.WorkerPercentage < 0f)
                    {
                        Planet planet5 = p;
                        planet5.ResearcherPercentage = planet5.ResearcherPercentage + p.WorkerPercentage;
                        p.WorkerPercentage = 0f;
                    }
                }
            }

            //MathHelper.Clamp(p.FarmerPercentage, 0f, 1f);
            //MathHelper.Clamp(p.WorkerPercentage, 0f, 1f);
            //MathHelper.Clamp(p.ResearcherPercentage, 0f, 1f);

            ColonySliderFood.amount = p.FarmerPercentage;
            ColonySliderProd.amount = p.WorkerPercentage;
            ColonySliderRes.amount = p.ResearcherPercentage;

            ColonySliderFood.cursor = CursorRectForSlider(ColonySliderFood);
            ColonySliderProd.cursor = CursorRectForSlider(ColonySliderProd);
            ColonySliderRes.cursor = CursorRectForSlider(ColonySliderRes);

            p.UpdateIncomes(false);
        }

        private static Rectangle CursorRectForSlider(ColonySlider colonySlider)
        {
            Texture2D crosshairTex = ResourceManager.Texture("NewUI/slider_crosshair");
            int posX = colonySlider.sRect.X + (int)(colonySlider.sRect.Width * colonySlider.amount) - crosshairTex.Width / 2;
            int posY = colonySlider.sRect.Y + colonySlider.sRect.Height / 2 - crosshairTex.Height / 2;
            return new Rectangle(posX, posY, crosshairTex.Width, crosshairTex.Height);
        }

        public void ResetLists()
        {
            Reset = true;
        }

        private void ScrapAccepted(object sender, EventArgs e)
        {
            if (toScrap != null)
            {
                toScrap.ScrapBuilding(p);
            }
            Update(0f);
        }

        public override void Update(float elapsedTime)
        {
            p.UpdateIncomes(false);
            if (!p.CanBuildInfantry())
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (p.HasShipyard)
                    {
                        build.AddTab(Localizer.Token(335));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                        continue;
                    add = false;
                    foreach (Troop troop in buildSL.VisibleItems<Troop>())
                        troop.Update(elapsedTime);
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (p.HasShipyard)
                    {
                        build.AddTab(Localizer.Token(335));
                    }
                    build.AddTab(Localizer.Token(336));
                }
            }
            if (!p.HasShipyard)
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (p.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(336));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    add = false;
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    build.AddTab(Localizer.Token(335));
                    if (p.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(336));
                    }
                }
            }
        }

        public class Lock
        {
            public Rectangle LockRect;
            public bool Locked;
            public bool Hover;
            public string Path;

            public Lock()
            {
                Path = "NewUI/icon_lock";
            }
        }

        public class ColonySlider
        {
            public Rectangle sRect;
            public float amount;
            public Rectangle cursor;
            public Color Color = new Color(72, 61, 38);
            public string state  = "normal";
            public string cState = "normal";
        }
    }
}
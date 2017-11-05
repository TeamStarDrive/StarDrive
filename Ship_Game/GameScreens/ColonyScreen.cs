using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class ColonyScreen : PlanetScreen, IDisposable, IListScreen
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
            this.eui = empUI;
            this.p = p;
            if (ScreenWidth <= 1366)
                this.LowRes = true;
            Rectangle theMenu1 = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            this.TitleBar = new Menu2(theMenu1);
            this.LeftColony = new ToggleButton(new Rectangle(theMenu1.X + 25, theMenu1.Y + 24, 14, 35), "SelectionBox/button_arrow_left", "SelectionBox/button_arrow_left", "SelectionBox/button_arrow_left_hover", "SelectionBox/button_arrow_left_hover", "");
            this.RightColony = new ToggleButton(new Rectangle(theMenu1.X + theMenu1.Width - 39, theMenu1.Y + 24, 14, 35), "SelectionBox/button_arrow_right", "SelectionBox/button_arrow_right", "SelectionBox/button_arrow_right_hover", "SelectionBox/button_arrow_right_hover", "");
            this.TitlePos = new Vector2((float)(theMenu1.X + theMenu1.Width / 2) - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, (float)(theMenu1.Y + theMenu1.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            Rectangle theMenu2 = new Rectangle(2, theMenu1.Y + theMenu1.Height + 5, theMenu1.Width, ScreenHeight - (theMenu1.Y + theMenu1.Height) - 7);
            this.LeftMenu = new Menu1(theMenu2);
            Rectangle theMenu3 = new Rectangle(theMenu1.X + theMenu1.Width + 10, theMenu1.Y, ScreenWidth / 3 - 15, ScreenHeight - theMenu1.Y - 2);
            this.RightMenu = new Menu1(theMenu3);
            var iconMoney = ResourceManager.TextureDict["NewUI/icon_money"];
            this.MoneyRect = new Rectangle(theMenu2.X + theMenu2.Width - 75, theMenu2.Y + 20, iconMoney.Width, iconMoney.Height);
            this.close = new CloseButton(this, new Rectangle(theMenu3.X + theMenu3.Width - 52, theMenu3.Y + 22, 20, 20));
            Rectangle theMenu4 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20, (int)(0.400000005960464 * (double)theMenu2.Width), (int)(0.25 * (double)(theMenu2.Height - 80)));
            this.PlanetInfo = new Submenu(theMenu4);
            this.PlanetInfo.AddTab(Localizer.Token(326));
            Rectangle theMenu5 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height, (int)(0.400000005960464 * (double)theMenu2.Width), (int)(0.25 * (double)(theMenu2.Height - 80)));
            this.pDescription = new Submenu(theMenu5);
            Rectangle theMenu6 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + 20, (int)(0.400000005960464 * (double)theMenu2.Width), (int)(0.25 * (double)(theMenu2.Height - 80)));
            this.pLabor = new Submenu(theMenu6);
            this.pLabor.AddTab(Localizer.Token(327));
            float num1 = (float)(int)((double)theMenu6.Width * 0.600000023841858);
            while ((double)num1 % 10.0 != 0.0)
                ++num1;
            Rectangle rectangle1 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.25 * (double)(theMenu6.Height - 25)), (int)num1, 6);
            this.ColonySliderFood = new ColonyScreen.ColonySlider();
            this.ColonySliderFood.sRect = rectangle1;
            this.ColonySliderFood.amount = p.FarmerPercentage;
            this.FoodLock = new ColonyScreen.Lock();
            var foodLockTex = ResourceManager.TextureDict[this.FoodLock.Path];
            this.FoodLock.LockRect = new Rectangle(this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width + 50, this.ColonySliderFood.sRect.Y + 2 + this.ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            if (p.Owner != null && p.Owner.data.Traits.Cybernetic > 0)
                p.FoodLocked = true;
            this.FoodLock.Locked = p.FoodLocked;
            Rectangle rectangle2 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.5 * (double)(theMenu6.Height - 25)), (int)num1, 6);
            this.ColonySliderProd = new ColonyScreen.ColonySlider();
            this.ColonySliderProd.sRect = rectangle2;
            this.ColonySliderProd.amount = p.WorkerPercentage;
            this.ProdLock = new ColonyScreen.Lock();
            this.ProdLock.LockRect = new Rectangle(this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width + 50, this.ColonySliderProd.sRect.Y + 2 + this.ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            this.ProdLock.Locked = p.ProdLocked;
            Rectangle rectangle3 = new Rectangle(theMenu6.X + 60, theMenu6.Y + 25 + (int)(0.75 * (double)(theMenu6.Height - 25)), (int)num1, 6);
            this.ColonySliderRes = new ColonyScreen.ColonySlider();
            this.ColonySliderRes.sRect = rectangle3;
            this.ColonySliderRes.amount = p.ResearcherPercentage;
            this.ResLock = new ColonyScreen.Lock();
            this.ResLock.LockRect = new Rectangle(this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width + 50, this.ColonySliderRes.sRect.Y + 2 + this.ColonySliderFood.sRect.Height / 2 - foodLockTex.Height / 2, foodLockTex.Width, foodLockTex.Height);
            this.ResLock.Locked = p.ResLocked;
            Rectangle theMenu7 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + theMenu6.Height + 40, (int)(0.400000005960464 * (double)theMenu2.Width), (int)(0.25 * (double)(theMenu2.Height - 80)));
            this.pStorage = new Submenu(theMenu7);
            this.pStorage.AddTab(Localizer.Token(328));
            Empire.Universe.ShipsInCombat.Visible = false;
            Empire.Universe.PlanetsInCombat.Visible = false;

            if (GlobalStats.HardcoreRuleset)
            {
                int num2 = (theMenu7.Width - 40) / 4;
                this.ResourceButtons.Add(new ThreeStateButton(p.fs, "Food", new Vector2((float)(theMenu7.X + 20), (float)(theMenu7.Y + 30))));
                this.ResourceButtons.Add(new ThreeStateButton(p.ps, "Production", new Vector2((float)(theMenu7.X + 20 + num2), (float)(theMenu7.Y + 30))));
                this.ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "Fissionables", new Vector2((float)(theMenu7.X + 20 + num2 * 2), (float)(theMenu7.Y + 30))));
                this.ResourceButtons.Add(new ThreeStateButton(Planet.GoodState.EXPORT, "ReactorFuel", new Vector2((float)(theMenu7.X + 20 + num2 * 3), (float)(theMenu7.Y + 30))));
            }
            else
            {
                this.FoodStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.330000013113022 * (double)(theMenu7.Height - 25)), (int)(0.400000005960464 * (double)theMenu7.Width), 18));
                this.FoodStorage.Max = p.MAX_STORAGE;
                this.FoodStorage.Progress = p.FoodHere;
                this.FoodStorage.color = "green";
                this.foodDropDown = this.LowRes ? new DropDownMenu(new Rectangle(theMenu7.X + 90 + (int)(0.400000005960464 * (double)theMenu7.Width) + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * (double)theMenu7.Width), 18)) : new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * (double)theMenu7.Width) + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * (double)theMenu7.Width), 18));
                this.foodDropDown.AddOption(Localizer.Token(329));
                this.foodDropDown.AddOption(Localizer.Token(330));
                this.foodDropDown.AddOption(Localizer.Token(331));
                this.foodDropDown.ActiveIndex = (int)p.fs;
                var iconStorageFood = ResourceManager.TextureDict["NewUI/icon_storage_food"];
                this.foodStorageIcon = new Rectangle(theMenu7.X + 20, this.FoodStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
                this.ProdStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.660000026226044 * (double)(theMenu7.Height - 25)), (int)(0.400000005960464 * (double)theMenu7.Width), 18));
                this.ProdStorage.Max = p.MAX_STORAGE;
                this.ProdStorage.Progress = p.ProductionHere;
                var iconStorageProd = ResourceManager.TextureDict["NewUI/icon_storage_production"];
                this.profStorageIcon = new Rectangle(theMenu7.X + 20, this.ProdStorage.pBar.Y + this.ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
                this.prodDropDown = this.LowRes ? new DropDownMenu(new Rectangle(theMenu7.X + 90 + (int)(0.400000005960464 * (double)theMenu7.Width) + 20, this.ProdStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * (double)theMenu7.Width), 18)) : new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * (double)theMenu7.Width) + 20, this.ProdStorage.pBar.Y + this.FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * (double)theMenu7.Width), 18));
                this.prodDropDown.AddOption(Localizer.Token(329));
                this.prodDropDown.AddOption(Localizer.Token(330));
                this.prodDropDown.AddOption(Localizer.Token(331));
                this.prodDropDown.ActiveIndex = (int)p.ps;
            }
            Rectangle theMenu8 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu4.Y, theMenu2.Width - 60 - theMenu4.Width, (int)((double)theMenu2.Height * 0.5));
            this.subColonyGrid = new Submenu(theMenu8);
            this.subColonyGrid.AddTab(Localizer.Token(332));
            Rectangle theMenu9 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu8.Y + theMenu8.Height + 20, theMenu2.Width - 60 - theMenu4.Width, theMenu2.Height - 20 - theMenu8.Height - 40);
            this.pFacilities = new Submenu(theMenu9);
            this.pFacilities.AddTab(Localizer.Token(333));

            launchTroops = Button(theMenu9.X + theMenu9.Width - 175, theMenu9.Y - 5, "Launch Troops", "Launch Troops");
            SendTroops = Button(theMenu9.X + theMenu9.Width - launchTroops.Rect.Width - 185,
                                theMenu9.Y - 5, "Send Troops", "Send Troops");

            this.CommoditiesSL = new ScrollList(this.pFacilities, 40);
            Rectangle theMenu10 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20, theMenu3.Width - 40, (int)(0.5 * (double)(theMenu3.Height - 60)));
            this.build = new Submenu(theMenu10);
            this.build.AddTab(Localizer.Token(334));
            this.buildSL = new ScrollList(this.build);
            this.playerDesignsToggle = new ToggleButton(new Rectangle(this.build.Menu.X + this.build.Menu.Width - 270, this.build.Menu.Y, 29, 20), "SelectionBox/button_grid_active", "SelectionBox/button_grid_inactive", "SelectionBox/button_grid_hover", "SelectionBox/button_grid_pressed", "SelectionBox/icon_grid");
            this.playerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
            if (p.HasShipyard)
                this.build.AddTab(Localizer.Token(335));
            if (p.AllowInfantry)
                this.build.AddTab(Localizer.Token(336));
            Rectangle theMenu11 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20 + 20 + theMenu10.Height, theMenu3.Width - 40, theMenu3.Height - 40 - theMenu10.Height - 20 - 3);
            this.queue = new Submenu(theMenu11);
            this.queue.AddTab(Localizer.Token(337));
            this.QSL = new ScrollList(this.queue);
            this.QSL.IsDraggable = true;
            this.PlanetIcon = new Rectangle(theMenu4.X + theMenu4.Width - 148, theMenu4.Y + (theMenu4.Height - 25) / 2 - 64 + 25, 128, 128);
            this.gridPos = new Rectangle(this.subColonyGrid.Menu.X + 10, this.subColonyGrid.Menu.Y + 30, this.subColonyGrid.Menu.Width - 20, this.subColonyGrid.Menu.Height - 35);
            int width = this.gridPos.Width / 7;
            int height = this.gridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(this.gridPos.X + planetGridSquare.x * width, this.gridPos.Y + planetGridSquare.y * height, width, height);
            this.PlanetName.Text = p.Name;
            this.PlanetName.MaxCharacters = 12;
            if (p.Owner != null)
            {
                this.shipsCanBuildLast = p.Owner.ShipsWeCanBuild.Count;
                this.buildingsHereLast = p.BuildingList.Count;
                this.buildingsCanBuildLast = this.BuildingsCanBuild.Count;
                this.detailInfo = (object)p.Description;
                Rectangle rectangle4 = new Rectangle(this.pDescription.Menu.X + 10, this.pDescription.Menu.Y + 30, 124, 148);
                Rectangle rectangle5 = new Rectangle(rectangle4.X + rectangle4.Width + 20, rectangle4.Y + rectangle4.Height - 15, (int)Fonts.Pirulen16.MeasureString(Localizer.Token(370)).X, Fonts.Pirulen16.LineSpacing);
                this.GovernorDropdown = new DropOptions<int>(this, new Rectangle(rectangle5.X + 30, rectangle5.Y + 30, 100, 18));
                this.GovernorDropdown.AddOption("--", 1);
                this.GovernorDropdown.AddOption(Localizer.Token(4064), 0);
                this.GovernorDropdown.AddOption(Localizer.Token(4065), 2);
                this.GovernorDropdown.AddOption(Localizer.Token(4066), 4);
                this.GovernorDropdown.AddOption(Localizer.Token(4067), 3);
                this.GovernorDropdown.AddOption(Localizer.Token(4068), 5);
                this.GovernorDropdown.AddOption(Localizer.Token(5087), 6);
                this.GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(p);
                if ((Planet.ColonyType)this.GovernorDropdown.ActiveValue != this.p.colonyType)
                {
                    this.p.colonyType = (Planet.ColonyType)this.GovernorDropdown.ActiveValue;
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
            QueueItem qItem = new QueueItem()
            {
                isTroop = true,
                troopType = "Terran/Space Marine",
                Cost = ResourceManager.GetTroopCost("Terran/Space Marine"),
                productionTowards = 0f
            };
            p.ConstructionQueue.Add(qItem);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            this.ClickTimer += (float)GameTime.ElapsedGameTime.TotalSeconds;
            if (this.p.Owner == null)
                return;
            this.p.UpdateIncomes(false);
            Vector2 pos;
            // ISSUE: explicit reference operation
            // ISSUE: variable of a reference type
            //Vector2& local1 = @pos;
            MouseState state1 = Mouse.GetState();
            //double num1 = (double) state1.X;
            //state1 = Mouse.GetState();
            //double num2 = (double) state1.Y;
            // ISSUE: explicit reference operation
            //^local1 = new Vector2((float) num1, (float) num2);
            //interpreting code as:
            //vector2 *local1 = &pos;
            //*local1 = new vector2();
            //equivalent to:
            //pos=new vector2();
            //cant be right, the value for pos is used but never set
            //reconstructed from jd:
            pos = new Vector2(state1.X, state1.Y);
            this.LeftMenu.Draw();
            this.RightMenu.Draw();
            this.TitleBar.Draw();
            this.LeftColony.Draw(this.ScreenManager);
            this.RightColony.Draw(this.ScreenManager);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(369), this.TitlePos, new Color(byte.MaxValue, (byte)239, (byte)208));
            if (!GlobalStats.HardcoreRuleset)
            {
                this.FoodStorage.Max = this.p.MAX_STORAGE;
                this.FoodStorage.Progress = this.p.FoodHere;
                this.ProdStorage.Max = this.p.MAX_STORAGE;
                this.ProdStorage.Progress = this.p.ProductionHere;
            }
            this.PlanetInfo.Draw();
            this.pDescription.Draw();
            this.pLabor.Draw();
            this.pStorage.Draw();
            this.subColonyGrid.Draw();
            Rectangle destinationRectangle1 = new Rectangle(this.gridPos.X, this.gridPos.Y + 1, this.gridPos.Width - 4, this.gridPos.Height - 3);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetTiles/" + this.p.GetTile()], destinationRectangle1, Color.White);
            foreach (PlanetGridSquare pgs in this.p.TilesList)
            {
                if (!pgs.Habitable)
                    this.ScreenManager.SpriteBatch.FillRectangle(pgs.ClickRect, new Color((byte)0, (byte)0, (byte)0, (byte)200));
                this.ScreenManager.SpriteBatch.DrawRectangle(pgs.ClickRect, new Color((byte)211, (byte)211, (byte)211, (byte)70), 2f);
                if (pgs.building != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    if(pgs.building.IsPlayerAdded)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + pgs.building.Icon + "_64x64"], destinationRectangle2, Color.WhiteSmoke);
                    }
                    else
                    
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + pgs.building.Icon + "_64x64"], destinationRectangle2, Color.White);
                }
                else if (pgs.QItem != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + pgs.QItem.Building.Icon + "_64x64"], destinationRectangle2, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 128));
                }
                this.DrawPGSIcons(pgs);
            }
            foreach (PlanetGridSquare planetGridSquare in this.p.TilesList)
            {
                if (planetGridSquare.highlighted)
                    this.ScreenManager.SpriteBatch.DrawRectangle(planetGridSquare.ClickRect, Color.White, 2f);
            }
            if (this.ActiveBuildingEntry != null)
            {
                Rectangle destinationRectangle2;
                // ISSUE: explicit reference operation
                // ISSUE: variable of a reference type
                //Rectangle& local2 = @destinationRectangle2;
                MouseState state2 = Mouse.GetState();
                int x = state2.X;
                //state2 = Mouse.GetState();
                int y = state2.Y;
                int width = 48;
                int height = 48;
                // ISSUE: explicit reference operation
                destinationRectangle2 = new Rectangle(state2.X, state2.Y, width, height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + (this.ActiveBuildingEntry.item as Building).Icon + "_48x48"], destinationRectangle2, Color.White);
            }
            this.pFacilities.Draw();
            if (this.p.Owner == Empire.Universe.player && this.p.TroopsHere.Count > 0)
                this.launchTroops.Draw(this.ScreenManager.SpriteBatch);
            //fbedard: Display button
            if (this.p.Owner == Empire.Universe.player)
            {
               
                int troopsInvading = eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0)
                    .Where(ai => ai.AI.State != AIState.Resupply)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));
                if (troopsInvading > 0)
                    SendTroops.Text = "Landing: " + troopsInvading;
                else
                    SendTroops.Text = "Send Troops";
                SendTroops.Draw(ScreenManager.SpriteBatch);
            }
            var vector2_1 = new Vector2(pFacilities.Menu.X + 15, pFacilities.Menu.Y + 35);
            DrawDetailInfo(vector2_1);
            build.Draw();
            queue.Draw();
            if (build.Tabs[0].Selected)
            {
                Array<Building> buildingsWeCanBuildHere = this.p.GetBuildingsWeCanBuildHere();
                if (this.p.BuildingList.Count != this.buildingsHereLast || this.buildingsCanBuildLast != buildingsWeCanBuildHere.Count || this.Reset)
                {
                    BuildingsCanBuild = buildingsWeCanBuildHere;
                    buildSL.Reset();
                    buildSL.indexAtTop = 0;
                    foreach (Building building in BuildingsCanBuild)
                        buildSL.AddItem(building, 0, 0);
                    Reset = false;
                }
                vector2_1 = new Vector2((float)(this.build.Menu.X + 20), (float)(this.build.Menu.Y + 45));
                for (int index = this.buildSL.indexAtTop; index < this.buildSL.Copied.Count; ++index)
                {
                    if (index < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay)
                    {
#if !DEBUG
              try
              { 
#endif
                        ScrollList.Entry entry = this.buildSL.Copied[index];
                        if (entry != null && entry.item is Building)
                        {
                            if (entry.clickRectHover == 0)
                            {
                                bool wontbuild = !p.WeCanAffordThis(entry.item as Building, p.colonyType);
                                vector2_1.Y = entry.clickRect.Y;
                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + (entry.item as Building).Icon + "_48x48"], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), wontbuild ? Color.SlateGray : Color.White);
                                Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y - 4f);
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token((entry.item as Building).NameTranslationIndex), position,  wontbuild ? Color.SlateGray : Color.White);
                                position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, HelperFunctions.ParseText(Fonts.Arial8Bold, Localizer.Token((entry.item as Building).ShortDescriptionIndex), this.LowRes ? 200f : 280f), position, wontbuild ? Color.Chocolate : Color.Orange);
                                position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                var iconProd = ResourceManager.TextureDict["NewUI/icon_production"];
                                Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - iconProd.Height / 2 - 5, iconProd.Width, iconProd.Height);
                                this.ScreenManager.SpriteBatch.Draw(iconProd, destinationRectangle2, Color.White);

                                // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                                position = new Vector2((float)(destinationRectangle2.X - 60), (float)(1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                string maintenance = (entry.item as Building).Maintenance.ToString("F2");
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                                // ~~~~

                                position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((float)(int)(entry.item as Building).Cost * UniverseScreen.GamePaceStatic).ToString(), position, Color.White);
                                if (entry.Plus != 0)
                                {
                                    if (entry.PlusHover == 0)
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], entry.addRect, Color.White);
                                    else
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                }
                            }
                            else
                            {
                                vector2_1.Y = (float)entry.clickRect.Y;
                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + (entry.item as Building).Icon + "_48x48"], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), Color.White);
                                Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y - 4f);
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token((entry.item as Building).NameTranslationIndex), position, Color.White);
                                position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, HelperFunctions.ParseText(Fonts.Arial8Bold, Localizer.Token((entry.item as Building).ShortDescriptionIndex), this.LowRes ? 200f : 280f), position, Color.Orange);
                                position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                var iconProd = ResourceManager.TextureDict["NewUI/icon_production"];
                                Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - iconProd.Height / 2 - 5, iconProd.Width, iconProd.Height);
                                this.ScreenManager.SpriteBatch.Draw(iconProd, destinationRectangle2, Color.White);

                                // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                                position = new Vector2((float)(destinationRectangle2.X - 60), (float)(1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                float actualMaint = (entry.item as Building).Maintenance + (entry.item as Building).Maintenance * this.p.Owner.data.Traits.MaintMod;
                                string maintenance = actualMaint.ToString("F2");
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                                // ~~~

                                position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((float)(int)(entry.item as Building).Cost * UniverseScreen.GamePaceStatic).ToString(), position, Color.White);
                                if (entry.Plus != 0)
                                {
                                    if (entry.PlusHover == 0)
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], entry.addRect, Color.White);
                                    else
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                }
                            }
                            if (entry.clickRect.HitTest(new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y)))
                                entry.clickRectHover = 1;
                        }
#if !DEBUG
                    }
            catch
            {
            }  
#endif
                    }
                    else
                        break;
                }
            }
            else if (this.p.HasShipyard && this.build.Tabs[1].Selected)
            {
                Array<string> list = new Array<string>();
                if (this.shipsCanBuildLast != this.p.Owner.ShipsWeCanBuild.Count || this.Reset)
                {
                    this.buildSL.Reset();

                    if (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.ColoniserMenu)
                    {
                        list.Add("Coloniser");
                        this.buildSL.AddItem((object)new ModuleHeader("Coloniser"));
                    }

                    foreach (string ship in this.p.Owner.ShipsWeCanBuild)
                    {
                        if (ResourceManager.ShipRoles[ResourceManager.ShipsDict[ship].shipData.Role].Protected || ResourceManager.ShipRoles[ResourceManager.ShipsDict[ship].shipData.Role].NoBuild)
                            continue;
                        if ((GlobalStats.ShowAllDesigns || ResourceManager.ShipsDict[ship].IsPlayerDesign) && !list.Contains(Localizer.GetRole(ResourceManager.ShipsDict[ship].shipData.Role, this.p.Owner)))
                        {
                            list.Add(Localizer.GetRole(ResourceManager.ShipsDict[ship].shipData.Role, this.p.Owner));
                            this.buildSL.AddItem((object)new ModuleHeader(Localizer.GetRole(ResourceManager.ShipsDict[ship].shipData.Role, this.p.Owner)));
                        }
                    }
                    this.buildSL.indexAtTop = 0;
                    this.Reset = false;
                    for (int index1 = 0; index1 < this.buildSL.Entries.Count; ++index1)
                    {
                        ScrollList.Entry entry = this.buildSL.Entries[index1];
                        if (entry != null)
                        {
                            foreach (string ship in this.p.Owner.ShipsWeCanBuild)
                            {
                                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.ColoniserMenu)
                                {
                                    if ((GlobalStats.ShowAllDesigns || ResourceManager.ShipsDict[ship].IsPlayerDesign) && ResourceManager.ShipsDict[ship].isColonyShip && (entry.item as ModuleHeader).Text == "Coloniser")
                                    {
                                        entry.AddItem((object)ResourceManager.ShipsDict[ship], 1, 1);
                                    }
                                    else if ((GlobalStats.ShowAllDesigns || ResourceManager.ShipsDict[ship].IsPlayerDesign) && !ResourceManager.ShipsDict[ship].isColonyShip && Localizer.GetRole(ResourceManager.ShipsDict[ship].shipData.Role, this.p.Owner) == (entry.item as ModuleHeader).Text)
                                    {
                                        entry.AddItem((object)ResourceManager.ShipsDict[ship], 1, 1);
                                    }
                                }
                                else
                                {
                                    if ((GlobalStats.ShowAllDesigns || ResourceManager.ShipsDict[ship].IsPlayerDesign) && Localizer.GetRole(ResourceManager.ShipsDict[ship].shipData.Role, this.p.Owner) == (entry.item as ModuleHeader).Text)
                                    {
                                        entry.AddItem((object)ResourceManager.ShipsDict[ship], 1, 1);
                                    }
                                }
                            }
                        }
                    }
                }
                vector2_1 = new Vector2((float)(this.build.Menu.X + 20), (float)(this.build.Menu.Y + 45));
                for (int index = this.buildSL.indexAtTop; index < this.buildSL.Copied.Count; ++index)
                {
                    if (index < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay)
                    {
                        try
                        {
                            ScrollList.Entry entry = this.buildSL.Copied[index];
                            if (entry != null)
                            {
                                vector2_1.Y = (float)entry.clickRect.Y;
                                if (entry.item is ModuleHeader)
                                    (entry.item as ModuleHeader).Draw(this.ScreenManager, vector2_1);
                                else if (entry.clickRectHover == 0)
                                {
                                    var iconPath = ResourceManager.HullsDict[(entry.item as Ship).GetShipData().Hull].IconPath;
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[iconPath], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), Color.White);
                                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Ship).shipData.Role == ShipData.RoleName.station || (entry.item as Ship).shipData.Role == ShipData.RoleName.platform ? (entry.item as Ship).Name + " " + Localizer.Token(2041) : (entry.item as Ship).Name, position, Color.White);
                                    position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole((entry.item as Ship).shipData.Role, this.p.Owner), position, Color.Orange);
                                    position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 120);
                                    var iconProd = ResourceManager.TextureDict["NewUI/icon_production"];
                                    Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - iconProd.Height / 2 - 5, iconProd.Width, iconProd.Height);
                                    this.ScreenManager.SpriteBatch.Draw(iconProd, destinationRectangle2, Color.White);

                                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                                    position = new Vector2((float)(destinationRectangle2.X - 60), (float)(1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    // Use correct upkeep method depending on mod settings
                                    string upkeep = "Doctor rocks";
                                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                                    {
                                        upkeep = (entry.item as Ship).GetMaintCostRealism(this.p.Owner).ToString("F2");
                                    }
                                    else
                                    {
                                        upkeep = (entry.item as Ship).GetMaintCost(this.p.Owner).ToString("F2");
                                    }
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                                    // ~~~

                                    position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)((entry.item as Ship).GetCost(this.p.Owner)*this.p.ShipBuildingModifier)).ToString(), position, Color.White);
                                }
                                else
                                {
                                    vector2_1.Y = (float)entry.clickRect.Y;
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(entry.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), Color.White);
                                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Ship).shipData.Role == ShipData.RoleName.station || (entry.item as Ship).shipData.Role == ShipData.RoleName.platform ? (entry.item as Ship).Name + " " + Localizer.Token(2041) : (entry.item as Ship).Name, position, Color.White);
                                    position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole((entry.item as Ship).shipData.Role, this.p.Owner), position, Color.Orange);
                                    position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 120);
                                    Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2 - 5, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle2, Color.White);

                                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                                    position = new Vector2((float)(destinationRectangle2.X - 60), (float)(1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    // Use correct upkeep method depending on mod settings
                                    string upkeep = "Doctor rocks";
                                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                                    {
                                        upkeep = (entry.item as Ship).GetMaintCostRealism(this.p.Owner).ToString("F2");
                                    }
                                    else
                                    {
                                        upkeep = (entry.item as Ship).GetMaintCost(this.p.Owner).ToString("F2");
                                    }
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                                    // ~~~

                                    position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)((entry.item as Ship).GetCost(this.p.Owner) * this.p.ShipBuildingModifier)).ToString(), position, Color.White);
                                    if (entry.Plus != 0)
                                    {
                                        if (entry.PlusHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], entry.addRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                    }
                                    if (entry.Edit != 0)
                                    {
                                        if (entry.EditHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], entry.editRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], entry.editRect, Color.White);
                                    }
                                    if (entry.clickRect.Y == 0)   //those checks look broken in either decompiler
                                    {
                                        //int num3 = 11 + 1;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                        break;
                }
                this.playerDesignsToggle.Draw(this.ScreenManager);
            }
            else if (!p.HasShipyard && p.AllowInfantry && build.Tabs[1].Selected)
            {
                if (Reset)
                {
                    buildSL.Reset();
                    buildSL.indexAtTop = 0;
                    foreach (string troopType in ResourceManager.TroopTypes)
                    {
                        if (p.Owner.WeCanBuildTroop(troopType))
                            buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), 1, 0);
                    }
                    Reset = false;
                }
                vector2_1 = new Vector2((float)(this.build.Menu.X + 20), (float)(this.build.Menu.Y + 45));
                for (int index = this.buildSL.indexAtTop; index < this.buildSL.Entries.Count; ++index)
                {
                    if (index < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay)
                    {
                        try
                        {
                            ScrollList.Entry entry = this.buildSL.Entries[index];
                            if (entry != null)
                            {
                                vector2_1.Y = (float)entry.clickRect.Y;
                                if (entry.clickRectHover == 0)
                                {
                                    if (entry.item is Troop)
                                    {
                                        (entry.item as Troop).Draw(this.ScreenManager.SpriteBatch, new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30));
                                        Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Troop).Name, position, Color.White);
                                        position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (entry.item as Troop).Class, position, Color.Orange);
                                        position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                        Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2 - 5, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle2, Color.White);
                                        position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)(entry.item as Troop).GetCost()).ToString(), position, Color.White);
                                        if (entry.Plus != 0)
                                        {
                                            if (entry.PlusHover == 0)
                                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], entry.addRect, Color.White);
                                            else
                                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                        }
                                        if (entry.Edit != 0)
                                        {
                                            if (entry.EditHover == 0)
                                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], entry.editRect, Color.White);
                                            else
                                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], entry.editRect, Color.White);
                                        }
                                        if (entry.clickRect.Y == 0)
                                        {
                                            //int num3 = 11 + 1;
                                        }
                                    }
                                }
                                else
                                {
                                    vector2_1.Y = (float)entry.clickRect.Y;
                                    (entry.item as Troop).Draw(this.ScreenManager.SpriteBatch, new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30));
                                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Troop).Name, position, Color.White);
                                    position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (entry.item as Troop).Class, position, Color.Orange);
                                    position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                    Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2 - 5, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle2, Color.White);
                                    position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)(entry.item as Troop).GetCost()).ToString(), position, Color.White);
                                    if (entry.Plus != 0)
                                    {
                                        if (entry.PlusHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], entry.addRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                    }
                                    if (entry.Edit != 0)
                                    {
                                        if (entry.EditHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], entry.editRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], entry.editRect, Color.White);
                                    }
                                    if (entry.clickRect.Y == 0)
                                    {
                                        //int num3 = 11 + 1;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                        break;
                }
            }
            else if (build.Tabs.Count > 2 && build.Tabs[2].Selected)
            {
                if (Reset)
                {
                    buildSL.Reset();
                    buildSL.indexAtTop = 0;
                    foreach (string troopType in ResourceManager.TroopTypes)
                    {
                        if (p.Owner.WeCanBuildTroop(troopType))
                            buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), 1, 0);
                    }
                    Reset = false;
                }
                vector2_1 = new Vector2(build.Menu.X + 20, build.Menu.Y + 45);
                for (int index = buildSL.indexAtTop; index < buildSL.Entries.Count; ++index)
                {
                    if (index < buildSL.indexAtTop + buildSL.entriesToDisplay)
                    {
                        try
                        {
                            ScrollList.Entry entry = this.buildSL.Entries[index];
                            if (entry != null)
                            {
                                vector2_1.Y = (float)entry.clickRect.Y;
                                if (entry.clickRectHover == 0)
                                {
                                    (entry.item as Troop).Draw(this.ScreenManager.SpriteBatch, new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30));
                                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Troop).Name, position, Color.White);
                                    position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (entry.item as Troop).Class, position, Color.Orange);
                                    position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                    Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2 - 5, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle2, Color.White);
                                    position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)(entry.item as Troop).GetCost()).ToString(), position, Color.White);
                                    if (entry.Plus != 0)
                                    {
                                        if (entry.PlusHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], entry.addRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                    }
                                    if (entry.Edit != 0)
                                    {
                                        if (entry.EditHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], entry.editRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], entry.editRect, Color.White);
                                    }
                                    if (entry.clickRect.Y == 0)
                                    {
                                        //int num3 = 11 + 1;
                                    }
                                }
                                else
                                {
                                    vector2_1.Y = (float)entry.clickRect.Y;
                                    (entry.item as Troop).Draw(this.ScreenManager.SpriteBatch, new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30));
                                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (entry.item as Troop).Name, position, Color.White);
                                    position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (entry.item as Troop).Class, position, Color.Orange);
                                    position.X = (float)(entry.clickRect.X + entry.clickRect.Width - 100);
                                    Rectangle destinationRectangle2 = new Rectangle((int)position.X, entry.clickRect.Y + entry.clickRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2 - 5, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle2, Color.White);
                                    position = new Vector2((float)(destinationRectangle2.X + 26), (float)(destinationRectangle2.Y + destinationRectangle2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((int)(entry.item as Troop).GetCost()).ToString(), position, Color.White);
                                    if (entry.Plus != 0)
                                    {
                                        if (entry.PlusHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], entry.addRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                                    }
                                    if (entry.Edit != 0)
                                    {
                                        if (entry.EditHover == 0)
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], entry.editRect, Color.White);
                                        else
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], entry.editRect, Color.White);
                                    }
                                    if (entry.clickRect.Y == 0)
                                    {
                                        //int num3 = 11 + 1;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                        break;
                }
            }
            this.QSL.Entries.Clear();
            foreach (object o in (Array<QueueItem>)this.p.ConstructionQueue)
                this.QSL.AddQItem(o);
            for (int index = this.QSL.indexAtTop; index < this.QSL.Copied.Count && index < this.QSL.indexAtTop + this.QSL.entriesToDisplay; ++index)
            {
                ScrollList.Entry entry = this.QSL.Copied[index];
                if (entry != null)
                {
                    vector2_1.Y = (float)entry.clickRect.Y;
                    if (entry.clickRect.HitTest(pos))
                        entry.clickRectHover = 1;

                    var qi = (entry.item as QueueItem);
                    if (qi.isBuilding)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + qi.Building.Icon + "_48x48"], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), Color.White);
                        Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(qi.Building.NameTranslationIndex), position, Color.White);
                        position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        Rectangle r = new Rectangle((int)position.X, (int)position.Y, 150, 18);
                        if (this.LowRes)
                            r.Width = 120;
                        new ProgressBar(r)
                        {
                            Max = qi.Cost,
                            Progress = qi.productionTowards
                        }.Draw(this.ScreenManager.SpriteBatch);
                    }
                    else if (qi.isShip)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[qi.sData.Hull].IconPath], new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30), Color.White);
                        Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.DisplayName != null ? qi.DisplayName : qi.sData.Name, position, Color.White);
                        position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        Rectangle r = new Rectangle((int)position.X, (int)position.Y, 150, 18);
                        if (this.LowRes)
                            r.Width = 120;
                        new ProgressBar(r)
                        {
                            Max = (int)(qi.Cost * this.p.ShipBuildingModifier),
                            Progress = qi.productionTowards
                        }.Draw(this.ScreenManager.SpriteBatch);
                    }
                    else if (qi.isTroop)
                    {
                        Troop template = ResourceManager.GetTroopTemplate(qi.troopType);
                        template.Draw(this.ScreenManager.SpriteBatch, new Rectangle((int)vector2_1.X, (int)vector2_1.Y, 29, 30));
                        Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, qi.troopType, position, Color.White);
                        position.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        Rectangle r = new Rectangle((int)position.X, (int)position.Y, 150, 18);
                        if (this.LowRes)
                            r.Width = 120;
                        new ProgressBar(r)
                        {
                            Max = qi.Cost,
                            Progress = qi.productionTowards
                        }.Draw(this.ScreenManager.SpriteBatch);
                    }
                    if (entry.clickRectHover == 1)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_up_hover1"], entry.up, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_down_hover1"], entry.down, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover1"], entry.apply, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"], entry.cancel, Color.White);
                        if (entry.up.HitTest(pos))
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_up_hover2"], entry.up, Color.White);
                        if (entry.down.HitTest(pos) && Empire.Universe.IsActive)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_down_hover2"], entry.down, Color.White);
                        if (entry.apply.HitTest(pos) && Empire.Universe.IsActive)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"], entry.apply, Color.White);
                            ToolTip.CreateTooltip(50);
                        }
                        if (entry.cancel.HitTest(pos) && Empire.Universe.IsActive)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover2"], entry.cancel, Color.White);
                            ToolTip.CreateTooltip(53);
                        }
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"], entry.up, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_arrow_down"], entry.down, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"], entry.apply, Color.White);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete"], entry.cancel, Color.White);
                    }
                    if (this.QSL.DraggedEntry != null && entry.clickRect == this.QSL.DraggedEntry.clickRect)
                        this.ScreenManager.SpriteBatch.FillRectangle(entry.clickRect, new Color((byte)0, (byte)0, (byte)0, (byte)150));
                    if (entry.PlusHover == 0)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], entry.addRect, Color.White);
                    else
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], entry.addRect, Color.White);
                }
            }
            this.QSL.Draw(this.ScreenManager.SpriteBatch);
            this.buildSL.Draw(this.ScreenManager.SpriteBatch);
            if (this.selector != null)
                this.selector.Draw(ScreenManager.SpriteBatch);
            string format = "0.#";
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.ColonySliderFood.sRect.X, this.ColonySliderFood.sRect.Y, (int)((double)this.ColonySliderFood.amount * (double)this.ColonySliderFood.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderFood.sRect.X, this.ColonySliderFood.sRect.Y, (int)((double)this.ColonySliderFood.amount * (double)this.ColonySliderFood.sRect.Width), 6)), this.p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderFood.sRect, this.ColonySliderFood.Color);
            Rectangle rectangle1 = new Rectangle(this.ColonySliderFood.sRect.X - 40, this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], rectangle1, this.p.Owner.data.Traits.Cybernetic > 0 ? new Color((byte)110, (byte)110, (byte)110, byte.MaxValue) : Color.White);
            if (rectangle1.HitTest(pos) && Empire.Universe.IsActive)
            {
                if (this.p.Owner.data.Traits.Cybernetic == 0)
                    ToolTip.CreateTooltip(70);
                else
                    ToolTip.CreateTooltip(77);
            }
            if (this.ColonySliderFood.cState == "normal")
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderFood.cursor, this.p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderFood.cursor, this.p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            Vector2 position1 = new Vector2();
            for (int index = 0; index < 11; ++index)
            {
                position1 = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width / 10 * index), (float)(this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height + 2));
                if (this.ColonySliderFood.state == "normal")
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], position1, this.p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], position1, this.p.Owner.data.Traits.Cybernetic > 0 ? Color.DarkGray : Color.White);
            }
            Vector2 position2 = new Vector2((float)(this.pLabor.Menu.X + this.pLabor.Menu.Width - 20), (float)(this.ColonySliderFood.sRect.Y + this.ColonySliderFood.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            if (this.LowRes)
                position2.X -= 15f;
            string text1 = this.p.Owner.data.Traits.Cybernetic == 0 ? this.p.GetNetFoodPerTurn().ToString(format) : "Unnecessary";
            position2.X -= Fonts.Arial12Bold.MeasureString(text1).X;
            if ((double)this.p.NetFoodPerTurn - (double)this.p.consumption < 0.0 && this.p.Owner.data.Traits.Cybernetic != 1 && text1 != "0")
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text1, position2, Color.LightPink);
            else
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text1, position2, new Color(byte.MaxValue, (byte)239, (byte)208));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_brown"], new Rectangle(this.ColonySliderProd.sRect.X, this.ColonySliderProd.sRect.Y, (int)((double)this.ColonySliderProd.amount * (double)this.ColonySliderProd.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderProd.sRect.X, this.ColonySliderProd.sRect.Y, (int)((double)this.ColonySliderProd.amount * (double)this.ColonySliderProd.sRect.Width), 6)), Color.White);
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderProd.sRect, this.ColonySliderProd.Color);
            Rectangle rectangle2 = new Rectangle(this.ColonySliderProd.sRect.X - 40, this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], rectangle2, Color.White);
            if (rectangle2.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(71);
            if (this.ColonySliderProd.cState == "normal")
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderProd.cursor, Color.White);
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderProd.cursor, Color.White);
            for (int index = 0; index < 11; ++index)
            {
                position1 = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderProd.sRect.Width / 10 * index), (float)(this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height + 2));
                if (this.ColonySliderProd.state == "normal")
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], position1, Color.White);
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], position1, Color.White);
            }
            position2 = new Vector2((float)(this.pLabor.Menu.X + this.pLabor.Menu.Width - 20), (float)(this.ColonySliderProd.sRect.Y + this.ColonySliderProd.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            if (this.LowRes)
                position2.X -= 15f;
            float num4;
            string str1;
            if (this.p.Owner.data.Traits.Cybernetic == 0)
            {
                str1 = this.p.NetProductionPerTurn.ToString(format);
            }
            else
            {
                num4 = this.p.NetProductionPerTurn - this.p.consumption;
                str1 = num4.ToString(format);
            }
            string text2 = str1;
            if (this.p.Crippled_Turns > 0)
            {
                text2 = Localizer.Token(2202);
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            }
            else if (this.p.RecentCombat)
            {
                text2 = Localizer.Token(2257);
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            }
            else
                position2.X -= Fonts.Arial12Bold.MeasureString(text2).X;
            if (this.p.Crippled_Turns > 0 || this.p.RecentCombat || this.p.Owner.data.Traits.Cybernetic != 0 && (double)this.p.NetProductionPerTurn - (double)this.p.consumption < 0.0 && text2 != "0")
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, position2, Color.LightPink);
            else
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, position2, new Color(byte.MaxValue, (byte)239, (byte)208));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_blue"], new Rectangle(this.ColonySliderRes.sRect.X, this.ColonySliderRes.sRect.Y, (int)((double)this.ColonySliderRes.amount * (double)this.ColonySliderRes.sRect.Width), 6), new Rectangle?(new Rectangle(this.ColonySliderRes.sRect.X, this.ColonySliderRes.sRect.Y, (int)((double)this.ColonySliderRes.amount * (double)this.ColonySliderRes.sRect.Width), 6)), Color.White);
            this.ScreenManager.SpriteBatch.DrawRectangle(this.ColonySliderRes.sRect, this.ColonySliderRes.Color);
            Rectangle rectangle3 = new Rectangle(this.ColonySliderRes.sRect.X - 40, this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_science"].Height / 2, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rectangle3, Color.White);
            if (rectangle3.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(72);
            if (this.ColonySliderRes.cState == "normal")
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], this.ColonySliderRes.cursor, Color.White);
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], this.ColonySliderRes.cursor, Color.White);
            for (int index = 0; index < 11; ++index)
            {
                position1 = new Vector2((float)(this.ColonySliderFood.sRect.X + this.ColonySliderRes.sRect.Width / 10 * index), (float)(this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height + 2));
                if (this.ColonySliderRes.state == "normal")
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], position1, Color.White);
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], position1, Color.White);
            }
            position2 = new Vector2((float)(this.pLabor.Menu.X + this.pLabor.Menu.Width - 20), (float)(this.ColonySliderRes.sRect.Y + this.ColonySliderRes.sRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
            if (this.LowRes)
                position2.X -= 15f;
            string text3 = this.p.NetResearchPerTurn.ToString(format);
            position2.X -= Fonts.Arial12Bold.MeasureString(text3).X;
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text3, position2, new Color(byte.MaxValue, (byte)239, (byte)208));
            if (this.p.Owner.data.Traits.Cybernetic == 0)
            {
                if (!this.FoodLock.Hover && !this.FoodLock.Locked)
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)50));
                else if (this.FoodLock.Hover && !this.FoodLock.Locked)
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)150));
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.FoodLock.Path], this.FoodLock.LockRect, Color.White);
            }
            if (!this.ProdLock.Hover && !this.ProdLock.Locked)
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)50));
            else if (this.ProdLock.Hover && !this.ProdLock.Locked)
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)150));
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ProdLock.Path], this.ProdLock.LockRect, Color.White);
            if (!this.ResLock.Hover && !this.ResLock.Locked)
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)50));
            else if (this.ResLock.Hover && !this.ResLock.Locked)
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)150));
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ResLock.Path], this.ResLock.LockRect, Color.White);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Planets/" + (object)this.p.planetType], this.PlanetIcon, Color.White);
            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            Vector2 vector2_2 = new Vector2((float)(this.PlanetInfo.Menu.X + 20), (float)(this.PlanetInfo.Menu.Y + 45));
            this.p.Name = this.PlanetName.Text;
            this.PlanetName.Draw(Fonts.Arial20Bold, this.ScreenManager.SpriteBatch, vector2_2, GameTime, new Color(byte.MaxValue, (byte)239, (byte)208));
            this.edit_name_button = new Rectangle((int)((double)vector2_2.X + (double)Fonts.Arial20Bold.MeasureString(this.p.Name).X + 12.0), (int)((double)vector2_2.Y + (double)(Fonts.Arial20Bold.LineSpacing / 2) - (double)(ResourceManager.TextureDict["NewUI/icon_build_edit"].Height / 2)) - 2, ResourceManager.TextureDict["NewUI/icon_build_edit"].Width, ResourceManager.TextureDict["NewUI/icon_build_edit"].Height);
            if (this.editHoverState == 0 && !this.PlanetName.HandlingInput)
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], this.edit_name_button, Color.White);
            else
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], this.edit_name_button, Color.White);
            if (this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 768)
                vector2_2.Y += (float)(Fonts.Arial20Bold.LineSpacing * 2);
            else
                vector2_2.Y += (float)Fonts.Arial20Bold.LineSpacing;
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.Type, position3, new Color(byte.MaxValue, (byte)239, (byte)208));
            vector2_2.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            num4 = this.p.Population / 1000f;
            string str2 = num4.ToString(format);
            string str3 = " / ";
            num4 = (float)(((double)this.p.MaxPopulation + (double)this.p.MaxPopBonus) / 1000.0);
            string str4 = num4.ToString(format);
            string text4 = str2 + str3 + str4;
            Vector2 position4 = position3;
            Color color = new Color(byte.MaxValue, (byte)239, (byte)208);
            spriteBatch1.DrawString(arial12Bold, text4, position4, color);
            Rectangle rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385) + ":").X, Fonts.Arial12Bold.LineSpacing);
            if (rect.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.Fertility.ToString(format), position3, new Color(byte.MaxValue, (byte)239, (byte)208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386) + ":").X, Fonts.Arial12Bold.LineSpacing);
            if (rect.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.MineralRichness.ToString(format), position3, new Color(byte.MaxValue, (byte)239, (byte)208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387) + ":").X, Fonts.Arial12Bold.LineSpacing);


            // The Doctor: For planet income breakdown

            Color zeroText = new Color(byte.MaxValue, (byte)239, (byte)208);
            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

            float grossIncome = (this.p.GrossMoneyPT + this.p.GrossMoneyPT * this.p.Owner.data.Traits.TaxMod) * this.p.Owner.data.TaxRate + this.p.PlusFlatMoneyPerTurn + (this.p.Population / 1000f * this.p.PlusCreditsPerColonist);
            float grossUpkeep = (float)((double)this.p.TotalMaintenanceCostsPerTurn + (double)this.p.TotalMaintenanceCostsPerTurn * (double)this.p.Owner.data.Traits.MaintMod);
            float netIncome = (float)(grossIncome - grossUpkeep);

            Vector2 positionGIncome = vector2_2;
            positionGIncome.X = vector2_2.X + 1;
            positionGIncome.Y = vector2_2.Y + 28;
            Vector2 positionGrossIncome = position3;
            positionGrossIncome.Y = position3.Y + 28;
            positionGrossIncome.X = position3.X + 1;

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, gIncome + ":", positionGIncome, Color.LightGray);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, grossIncome.ToString("F2") + " BC/Y", positionGrossIncome, Color.LightGray);

            Vector2 positionGUpkeep = positionGIncome;
            positionGUpkeep.Y = positionGIncome.Y + (Fonts.Arial12.LineSpacing);
            Vector2 positionGrossUpkeep = positionGrossIncome;
            positionGrossUpkeep.Y = positionGrossIncome.Y + (Fonts.Arial12.LineSpacing);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, gUpkeep + ":", positionGUpkeep, Color.LightGray);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, grossUpkeep.ToString("F2") + " BC/Y", positionGrossUpkeep, Color.LightGray);

            Vector2 positionNIncome = positionGUpkeep;
            positionNIncome.X = positionGUpkeep.X - 1;
            positionNIncome.Y = positionGUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);
            Vector2 positionNetIncome = positionGrossUpkeep;
            positionNetIncome.X = positionGrossUpkeep.X - 1;
            positionNetIncome.Y = positionGrossUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, (netIncome > 0.0 ? nIncome : nLosses) + ":", positionNIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, netIncome.ToString("F2") + " BC/Y", positionNetIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);




            if (rect.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);
            if (ResourceManager.TextureDict.ContainsKey("Portraits/" + this.p.Owner.data.PortraitName))
            {
                Rectangle rectangle4 = new Rectangle(this.pDescription.Menu.X + 10, this.pDescription.Menu.Y + 30, 124, 148);
                while (rectangle4.Y + rectangle4.Height > this.pDescription.Menu.Y + 30 + this.pDescription.Menu.Height - 30)
                {
                    rectangle4.Height -= (int)(0.100000001490116 * (double)rectangle4.Height);
                    rectangle4.Width -= (int)(0.100000001490116 * (double)rectangle4.Width);
                }
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/" + this.p.Owner.data.PortraitName], rectangle4, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], rectangle4, Color.White);
                this.ScreenManager.SpriteBatch.DrawRectangle(rectangle4, Color.Orange);
                if (this.p.colonyType == Planet.ColonyType.Colony)
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/x_red"], rectangle4, Color.White);
                Vector2 position5 = new Vector2((float)(rectangle4.X + rectangle4.Width + 15), (float)rectangle4.Y);
                Vector2 vector2_3 = position5;
                switch (this.p.colonyType)
                {
                    case Planet.ColonyType.Core:
                        Localizer.Token(372);
                        break;
                    case Planet.ColonyType.Colony:
                        Localizer.Token(376);
                        break;
                    case Planet.ColonyType.Industrial:
                        Localizer.Token(373);
                        break;
                    case Planet.ColonyType.Research:
                        Localizer.Token(375);
                        break;
                    case Planet.ColonyType.Agricultural:
                        Localizer.Token(371);
                        break;
                    case Planet.ColonyType.Military:
                        Localizer.Token(374);
                        break;
                    case Planet.ColonyType.TradeHub:
                        Localizer.Token(393);
                        break;
                }
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Governor", position5, Color.White);
                position5.Y = (float)(this.GovernorDropdown.Rect.Y + 25);
                string text5 = "";
                switch (this.p.colonyType)
                {
                    case Planet.ColonyType.Core:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(378), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.Colony:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(382), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.Industrial:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(379), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.Research:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(381), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.Agricultural:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(377), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.Military:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(380), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                    case Planet.ColonyType.TradeHub:
                        text5 = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(394), (float)(this.pDescription.Menu.Width - 50 - rectangle4.Width - 5));
                        break;
                }
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text5, position5, Color.White);

                GovernorDropdown.SetAbsPos(vector2_3.X, vector2_3.Y + Fonts.Arial12Bold.LineSpacing + 5);
                GovernorDropdown.Reset();
                GovernorDropdown.Draw(ScreenManager.SpriteBatch);
            }
            if (GlobalStats.HardcoreRuleset)
            {
                foreach (ThreeStateButton threeStateButton in this.ResourceButtons)
                    threeStateButton.Draw(this.ScreenManager, this.p.GetGoodAmount(threeStateButton.Good));
            }
            else
            {
                this.FoodStorage.Progress = this.p.FoodHere;
                this.ProdStorage.Progress = this.p.ProductionHere;
                if (this.p.fs == Planet.GoodState.STORE)
                    this.foodDropDown.ActiveIndex = 0;
                else if (this.p.fs == Planet.GoodState.IMPORT)
                    this.foodDropDown.ActiveIndex = 1;
                else if (this.p.fs == Planet.GoodState.EXPORT)
                    this.foodDropDown.ActiveIndex = 2;
                if (this.p.Owner.data.Traits.Cybernetic == 0)
                {
                    this.FoodStorage.Draw(this.ScreenManager.SpriteBatch);
                    this.foodDropDown.Draw(this.ScreenManager.SpriteBatch);
                }
                else
                {
                    this.FoodStorage.DrawGrayed(this.ScreenManager.SpriteBatch);
                    this.foodDropDown.DrawGrayed(this.ScreenManager.SpriteBatch);
                }
                this.ProdStorage.Draw(this.ScreenManager.SpriteBatch);
                if (this.p.ps == Planet.GoodState.STORE)
                    this.prodDropDown.ActiveIndex = 0;
                else if (this.p.ps == Planet.GoodState.IMPORT)
                    this.prodDropDown.ActiveIndex = 1;
                else if (this.p.ps == Planet.GoodState.EXPORT)
                    this.prodDropDown.ActiveIndex = 2;
                this.prodDropDown.Draw(this.ScreenManager.SpriteBatch);
                if (!this.LowRes)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_storage_food"], this.foodStorageIcon, Color.White);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_storage_production"], this.profStorageIcon, Color.White);
                }
                else
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_storage_food"], this.foodStorageIcon, Color.White);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_storage_production"], this.profStorageIcon, Color.White);
                }
            }

            if (this.ScreenManager.NumScreens == 2)
                popup = true;

            this.close.Draw(spriteBatch);

            if (this.foodStorageIcon.HitTest(pos) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (!this.profStorageIcon.HitTest(pos) || !Empire.Universe.IsActive)
                return;
            ToolTip.CreateTooltip(74);
        }

        private void DrawCommoditiesArea(Vector2 bCursor)
        {
            string desc = this.parseText(Localizer.Token(4097), (float)(this.pFacilities.Menu.Width - 40));
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
        }

        private void DrawDetailInfo(Vector2 bCursor)
        {
            object[] plusFlatFoodAmount;
            float plusFlatPopulation;
            if (this.pFacilities.Tabs.Count > 1 && this.pFacilities.Tabs[1].Selected)
            {
                this.DrawCommoditiesArea(bCursor);
                return;
            }
            if (this.detailInfo is Troop)
            {
                Troop t = this.detailInfo as Troop;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, t.Name, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
                string desc = this.parseText(t.Description, (float)(this.pFacilities.Menu.Width - 40));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + (float)Fonts.Arial12Bold.LineSpacing);
                Vector2 tCursor = bCursor;
                tCursor.X = bCursor.X + 100f;
                desc = string.Concat(Localizer.Token(338), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.TargetType, tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                tCursor.Y = bCursor.Y;
                desc = string.Concat(Localizer.Token(339), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Strength.ToString("0."), tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                tCursor.Y = bCursor.Y;
                desc = string.Concat(Localizer.Token(2218), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.GetHardAttack().ToString(), tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                tCursor.Y = bCursor.Y;
                desc = string.Concat(Localizer.Token(2219), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                //added by McShooterz: bug fix where hard attack value was used in place of soft attack value
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.GetSoftAttack().ToString(), tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                //added by McShooterz: adds boarding strength to troop info in colony screen
                tCursor.Y = bCursor.Y;
                desc = string.Concat(Localizer.Token(6008), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.BoardingStrength.ToString(), tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                //Added by McShooterz: display troop level
                tCursor.Y = bCursor.Y;
                desc = string.Concat(Localizer.Token(6023), ": ");
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, t.Level.ToString(), tCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
            }
            if (this.detailInfo is string)
            {
                string desc = this.parseText(this.p.Description, (float)(this.pFacilities.Menu.Width - 40));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + (float)Fonts.Arial12Bold.LineSpacing);
                desc = "";
                if (this.p.Owner.data.Traits.Cybernetic != 0)
                {
                    desc = string.Concat(desc, Localizer.Token(2028));
                }
                else if (this.p.fs == Planet.GoodState.EXPORT)
                {
                    desc = string.Concat(desc, Localizer.Token(2025));
                }
                else if (this.p.fs == Planet.GoodState.IMPORT)
                {
                    desc = string.Concat(desc, Localizer.Token(2026));
                }
                else if (this.p.fs == Planet.GoodState.STORE)
                {
                    desc = string.Concat(desc, Localizer.Token(2027));
                }
                desc = this.parseText(desc, (float)(this.pFacilities.Menu.Width - 40));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + Fonts.Arial12Bold.MeasureString(desc).Y;
                desc = "";
                bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                if (this.p.ps == Planet.GoodState.EXPORT)
                {
                    desc = string.Concat(desc, Localizer.Token(345));
                }
                else if (this.p.ps == Planet.GoodState.IMPORT)
                {
                    desc = string.Concat(desc, Localizer.Token(346));
                }
                else if (this.p.ps == Planet.GoodState.STORE)
                {
                    desc = string.Concat(desc, Localizer.Token(347));
                }
                desc = this.parseText(desc, (float)(this.pFacilities.Menu.Width - 40));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, new Color(255, 239, 208));
                if (this.p.Owner.data.Traits.Cybernetic == 0)
                {
                    if (this.p.FoodHere + this.p.NetFoodPerTurn - this.p.consumption < 0f)
                    {
                        bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + (float)Fonts.Arial12Bold.LineSpacing);
                        desc = this.parseText(Localizer.Token(344), (float)(this.pFacilities.Menu.Width - 40));
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, Color.LightPink);
                        return;
                    }
                }
                else if (this.p.ProductionHere + this.p.NetProductionPerTurn - this.p.consumption < 0f)
                {
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + (float)Fonts.Arial12Bold.LineSpacing);
                    desc = this.parseText(Localizer.Token(344), (float)(this.pFacilities.Menu.Width - 40));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, bCursor, Color.LightPink);
                    return;
                }
            }
            else if (this.detailInfo is PlanetGridSquare)
            {
                PlanetGridSquare pgs = this.detailInfo as PlanetGridSquare;
                if (pgs.building == null && pgs.Habitable && pgs.Biosphere)
                {
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(348), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.parseText(Localizer.Token(349), (float)(this.pFacilities.Menu.Width - 40)), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (pgs.building == null && pgs.Habitable)
                {
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(350), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.parseText(Localizer.Token(349), (float)(this.pFacilities.Menu.Width - 40)), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (!pgs.Habitable && pgs.building == null)
                {
                    if (this.p.Type == "Barren")
                    {
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(351), bCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.parseText(Localizer.Token(352), (float)(this.pFacilities.Menu.Width - 40)), bCursor, new Color(255, 239, 208));
                        return;
                    }
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(351), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.parseText(Localizer.Token(353), (float)(this.pFacilities.Menu.Width - 40)), bCursor, new Color(255, 239, 208));
                    return;
                }
                if (pgs.building != null)
                {
                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/GC_Square Selection"], bRect, Color.White);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(pgs.building.NameTranslationIndex), bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                    string text = this.parseText(Localizer.Token(pgs.building.DescriptionIndex), (float)(this.pFacilities.Menu.Width - 40));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, bCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(text).Y + (float)Fonts.Arial20Bold.LineSpacing);
                    if (pgs.building.PlusFlatFoodAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
                        SpriteFont arial12Bold = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatFoodAmount, " ", Localizer.Token(354) };
                        spriteBatch.DrawString(arial12Bold, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFoodPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFoodPerColonist, " ", Localizer.Token(2042) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.IsSensor && pgs.building.SensorRange != 0f)
                    {
                        Rectangle fIcon;
                        if (ResourceManager.TextureDict.ContainsKey("NewUI/icon_sensors"))
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_sensors"].Width, ResourceManager.TextureDict["NewUI/icon_sensors"].Height);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_sensors"], fIcon, Color.White);
                        }
                        else
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["transparent"].Width, ResourceManager.TextureDict["Textures/transparent"].Height);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["transparent"], fIcon, Color.White);
                        }
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "", pgs.building.SensorRange, " ", Localizer.Token(6000) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.IsProjector && pgs.building.ProjectorRange != 0f)
                    {
                        Rectangle fIcon;
                        if (ResourceManager.TextureDict.ContainsKey("NewUI/icon_projection"))
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_projection"].Width, ResourceManager.TextureDict["NewUI/icon_projection"].Height);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_projection"], fIcon, Color.White);
                        }
                        else
                        {
                            fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["transparent"].Width, ResourceManager.TextureDict["Textures/transparent"].Height);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["transparent"], fIcon, Color.White);
                        }
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "", pgs.building.ProjectorRange, " ", Localizer.Token(6001) };
                        spriteBatch1.DrawString(spriteFont, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatProductionAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch2 = this.ScreenManager.SpriteBatch;
                        SpriteFont arial12Bold1 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatProductionAmount, " ", Localizer.Token(355) };
                        spriteBatch2.DrawString(arial12Bold1, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusProdPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch3 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont1 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusProdPerColonist, " ", Localizer.Token(356) };
                        spriteBatch3.DrawString(spriteFont1, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatPopulation != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.TextureDict["NewUI/icon_population"].Width, ResourceManager.TextureDict["NewUI/icon_population"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_population"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch4 = this.ScreenManager.SpriteBatch;
                        SpriteFont arial12Bold2 = Fonts.Arial12Bold;
                        plusFlatPopulation = pgs.building.PlusFlatPopulation / 1000f;
                        spriteBatch4.DrawString(arial12Bold2, string.Concat("+", plusFlatPopulation.ToString("#.00"), " ", Localizer.Token(2043)), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusFlatResearchAmount != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch5 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont2 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusFlatResearchAmount, " ", Localizer.Token(357) };
                        spriteBatch5.DrawString(spriteFont2, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusResearchPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch6 = this.ScreenManager.SpriteBatch;
                        SpriteFont arial12Bold3 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+", pgs.building.PlusResearchPerColonist, " ", Localizer.Token(358) };
                        spriteBatch6.DrawString(arial12Bold3, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusTaxPercentage != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch7 = this.ScreenManager.SpriteBatch;
                        SpriteFont spriteFont3 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "+ ", pgs.building.PlusTaxPercentage * 100f, "% ", Localizer.Token(359) };
                        spriteBatch7.DrawString(spriteFont3, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.MinusFertilityOnBuild != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.LightPink);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(360), ": ", pgs.building.MinusFertilityOnBuild), tCursor, Color.LightPink);
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlanetaryShieldStrengthAdded != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.TextureDict["NewUI/icon_planetshield"].Width, ResourceManager.TextureDict["NewUI/icon_planetshield"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_planetshield"], fIcon, Color.Green);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(361), ": "), tCursor, Color.White);
                        tCursor.X = tCursor.X + Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(361), ": ")).X;
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, pgs.building.PlanetaryShieldStrengthAdded.ToString(), tCursor, Color.LightGreen);
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.CreditsPerColonist != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(362), ": ", pgs.building.CreditsPerColonist), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.PlusProdPerRichness != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(363), ": ", pgs.building.PlusProdPerRichness), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.CombatStrength > 0)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(364), ": ", pgs.building.CombatStrength), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.Maintenance > 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        SpriteBatch spriteBatch8 = this.ScreenManager.SpriteBatch;
                        SpriteFont arial12Bold4 = Fonts.Arial12Bold;
                        plusFlatFoodAmount = new object[] { "-", pgs.building.Maintenance + pgs.building.Maintenance * this.p.Owner.data.Traits.MaintMod, " ", Localizer.Token(365) };
                        spriteBatch8.DrawString(arial12Bold4, string.Concat(plusFlatFoodAmount), tCursor, Color.LightPink);
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                    }
                    if (pgs.building.ShipRepair != 0f)
                    {
                        Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"], fIcon, Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("+", pgs.building.ShipRepair, " ", Localizer.Token(6137)), tCursor, new Color(255, 239, 208));
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 12);
                    }
                    if (pgs.building.Scrappable)
                    {
                        bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "You may scrap this building by right clicking it", bCursor, Color.White);
                        return;
                    }
                }
            }
            else if (this.detailInfo is ScrollList.Entry)
            {
                Building temp = (this.detailInfo as ScrollList.Entry).item as Building;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, temp.Name, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
                string text = this.parseText(Localizer.Token(temp.DescriptionIndex), (float)(this.pFacilities.Menu.Width - 40));
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, bCursor, new Color(255, 239, 208));
                bCursor.Y = bCursor.Y + (Fonts.Arial12Bold.MeasureString(text).Y + (float)Fonts.Arial20Bold.LineSpacing);
                if (temp.PlusFlatFoodAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch9 = this.ScreenManager.SpriteBatch;
                    SpriteFont spriteFont4 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatFoodAmount, " ", Localizer.Token(354) };
                    spriteBatch9.DrawString(spriteFont4, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFoodPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFoodPerColonist, " ", Localizer.Token(2042) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.IsSensor && temp.SensorRange != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_sensors"].Width, ResourceManager.TextureDict["NewUI/icon_sensors"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_sensors"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "", temp.SensorRange, " ", Localizer.Token(6000) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.IsProjector && temp.ProjectorRange != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_projection"].Width, ResourceManager.TextureDict["NewUI/icon_projection"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_projection"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch10 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "", temp.ProjectorRange, " ", Localizer.Token(6001) };
                    spriteBatch10.DrawString(arial12Bold5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatProductionAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch11 = this.ScreenManager.SpriteBatch;
                    SpriteFont spriteFont5 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatProductionAmount, " ", Localizer.Token(355) };
                    spriteBatch11.DrawString(spriteFont5, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusProdPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch12 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold6 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusProdPerColonist, " ", Localizer.Token(356) };
                    spriteBatch12.DrawString(arial12Bold6, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatResearchAmount != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch13 = this.ScreenManager.SpriteBatch;
                    SpriteFont spriteFont6 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusFlatResearchAmount, " ", Localizer.Token(357) };
                    spriteBatch13.DrawString(spriteFont6, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusResearchPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch14 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold7 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+", temp.PlusResearchPerColonist, " ", Localizer.Token(358) };
                    spriteBatch14.DrawString(arial12Bold7, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusFlatPopulation != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.TextureDict["NewUI/icon_population"].Width, ResourceManager.TextureDict["NewUI/icon_population"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_population"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch15 = this.ScreenManager.SpriteBatch;
                    SpriteFont spriteFont7 = Fonts.Arial12Bold;
                    plusFlatPopulation = temp.PlusFlatPopulation / 1000f;
                    spriteBatch15.DrawString(spriteFont7, string.Concat("+", plusFlatPopulation.ToString("#.00"), " ", Localizer.Token(2043)), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusTaxPercentage != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch16 = this.ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold8 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "+ ", temp.PlusTaxPercentage * 100f, "% ", Localizer.Token(359) };
                    spriteBatch16.DrawString(arial12Bold8, string.Concat(plusFlatFoodAmount), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.MinusFertilityOnBuild != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], fIcon, Color.LightPink);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(360), ": ", temp.MinusFertilityOnBuild), tCursor, Color.LightPink);
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlanetaryShieldStrengthAdded != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X - 4, (int)bCursor.Y - 4, ResourceManager.TextureDict["NewUI/icon_planetshield"].Width, ResourceManager.TextureDict["NewUI/icon_planetshield"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_planetshield"], fIcon, Color.Green);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(361), ": "), tCursor, Color.White);
                    tCursor.X = tCursor.X + Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(361), ": ")).X;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, temp.PlanetaryShieldStrengthAdded.ToString(), tCursor, Color.LightGreen);
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.CreditsPerColonist != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(362), ": ", temp.CreditsPerColonist), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.PlusProdPerRichness != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(363), ": ", temp.PlusProdPerRichness), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.CombatStrength > 0)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(364), ": ", temp.CombatStrength), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.Maintenance > 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    SpriteBatch spriteBatch17 = this.ScreenManager.SpriteBatch;
                    SpriteFont spriteFont8 = Fonts.Arial12Bold;
                    plusFlatFoodAmount = new object[] { "-", temp.Maintenance + temp.Maintenance * this.p.Owner.data.Traits.MaintMod, " ", Localizer.Token(365) };
                    spriteBatch17.DrawString(spriteFont8, string.Concat(plusFlatFoodAmount), tCursor, Color.LightPink);
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
                if (temp.ShipRepair != 0f)
                {
                    Rectangle fIcon = new Rectangle((int)bCursor.X, (int)bCursor.Y, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction"], fIcon, Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + (float)fIcon.Width + 5f, bCursor.Y + 3f);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("+", temp.ShipRepair, " ", Localizer.Token(6137)), tCursor, new Color(255, 239, 208));
                    bCursor.Y = bCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
        }

        private void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_biosphere_48x48"], biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 48, pgs.ClickRect.Y, 48, 48);
                pgs.TroopsHere[0].DrawIcon(this.ScreenManager.SpriteBatch, pgs.TroopClickRect);
            }
            float numFood = 0f;
            float numProd = 0f;
            float numRes = 0f;
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood = numFood + pgs.building.PlusFoodPerColonist * this.p.Population / 1000f * this.p.FarmerPercentage;
                    numFood = numFood + pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd = numProd + pgs.building.PlusFlatProductionAmount;
                    numProd = numProd + pgs.building.PlusProdPerColonist * this.p.Population / 1000f * this.p.WorkerPercentage;
                }
                if (pgs.building.PlusProdPerRichness > 0f)
                {
                    numProd = numProd + pgs.building.PlusProdPerRichness * this.p.MineralRichness;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes = numRes + pgs.building.PlusResearchPerColonist * this.p.Population / 1000f * this.p.ResearcherPercentage;
                    numRes = numRes + pgs.building.PlusFlatResearchAmount;
                }
            }
            float total = numFood + numProd + numRes;
            float totalSpace = (float)(pgs.ClickRect.Width - 30);
            float spacing = totalSpace / total;
            Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.TextureDict["NewUI/icon_food"].Height, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
            for (int i = 0; (float)i < numFood; i++)
            {
                if (numFood - (float)i <= 0f || numFood - (float)i >= 1f)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable = null;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], new Vector2((float)rect.X, (float)rect.Y), nullable, Color.White, 0f, Vector2.Zero, numFood - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numProd; i++)
            {
                if (numProd - (float)i <= 0f || numProd - (float)i >= 1f)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable1 = null;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], new Vector2((float)rect.X, (float)rect.Y), nullable1, Color.White, 0f, Vector2.Zero, numProd - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numRes; i++)
            {
                if (numRes - (float)i <= 0f || numRes - (float)i >= 1f)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable2 = null;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], new Vector2((float)rect.X, (float)rect.Y), nullable2, Color.White, 0f, Vector2.Zero, numRes - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
        }

        public static int GetIndex(Planet p)
        {
            switch (p.colonyType)
            {
                case Planet.ColonyType.Core:
                    {
                        return 1;
                    }
                case Planet.ColonyType.Colony:
                    {
                        return 0;
                    }
                case Planet.ColonyType.Industrial:
                    {
                        return 2;
                    }
                case Planet.ColonyType.Research:
                    {
                        return 4;
                    }
                case Planet.ColonyType.Agricultural:
                    {
                        return 3;
                    }
                case Planet.ColonyType.Military:
                    {
                        return 5;
                    }
                case Planet.ColonyType.TradeHub:
                    {
                        return 6;
                    }
            }
            return 0;
        }

        private void HandleDetailInfo(InputState input)
        {
            this.detailInfo = null;
            for (int i = this.buildSL.indexAtTop; i < this.buildSL.Copied.Count && i < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.buildSL.Copied[i];
                if (e.clickRect.HitTest(input.CursorPosition))
                {
                    if (e.item is Building)
                    {
                        this.detailInfo = e;
                    }
                    if (e.item is Troop)
                    {
                        this.detailInfo = e.item;
                    }
                }
            }
            if (this.detailInfo == null)
            {
                this.detailInfo = this.p.Description;
            }
        }

        public override bool HandleInput(InputState input)
        {
            this.pFacilities.HandleInputNoReset();
            if (this.RightColony.Rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2279));
            }
            if (this.LeftColony.Rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2280));
            }
            if ((input.Right || this.RightColony.HandleInput(input)) && (Empire.Universe.Debug || this.p.Owner == EmpireManager.Player))
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
                if (input.MouseCurr.RightButton != ButtonState.Released || this.previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }
            if ((input.Left || this.LeftColony.HandleInput(input)) && (Empire.Universe.Debug || this.p.Owner == EmpireManager.Player))
            {
                int thisindex = this.p.Owner.GetPlanets().IndexOf(this.p);
                thisindex = (thisindex <= 0 ? this.p.Owner.GetPlanets().Count - 1 : thisindex - 1);
                if (this.p.Owner.GetPlanets()[thisindex] != this.p)
                {
                    this.p = this.p.Owner.GetPlanets()[thisindex];
                    Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, p, eui);
                }
                if (input.MouseCurr.RightButton != ButtonState.Released || this.previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }
            this.p.UpdateIncomes(false);
            this.HandleDetailInfo(input);
            this.currentMouse = Mouse.GetState();
            Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            this.buildSL.HandleInput(input);
            this.buildSL.Update();
            this.build.HandleInput(this);
            if (this.p.Owner != EmpireManager.Player)
            {
                this.HandleDetailInfo(input);
                if (input.MouseCurr.RightButton != ButtonState.Released || this.previousMouse.RightButton != ButtonState.Released)
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                return true;
            }
            if (!this.launchTroops.Rect.HitTest(input.CursorPosition))
            {
                this.launchTroops.State = UIButton.PressState.Default;
            }
            else
            {
                this.launchTroops.State = UIButton.PressState.Hover;
                if (input.InGameSelect)
                {
                    bool play = false;
                    foreach (PlanetGridSquare pgs in this.p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                        {
                            continue;
                        }

                        play = true;

                        Ship.CreateTroopShipAtPoint(this.p.Owner.data.DefaultTroopShip, this.p.Owner, this.p.Center, pgs.TroopsHere[0]);
                        this.p.TroopsHere.Remove(pgs.TroopsHere[0]);
                        pgs.TroopsHere[0].SetPlanet(null);
                        pgs.TroopsHere.Clear();
                        this.ClickedTroop = true;
                        this.detailInfo = null;
                    }
                    if (play)
                    {

                        GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    }
                }
            }
            //fbedard: Click button to send troops
            if (!this.SendTroops.Rect.HitTest(input.CursorPosition))
            {
                this.SendTroops.State = UIButton.PressState.Default;
            }
            else
            {
                this.SendTroops.State = UIButton.PressState.Hover;
                if (input.InGameSelect)
                {
                    Array<Ship> troopShips;
                    using (eui.empire.GetShips().AcquireReadLock())
                        troopShips = new Array<Ship>(this.eui.empire.GetShips()
                        .Where(troop => troop.TroopList.Count > 0
                            && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                            && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, this.p.Center)));

                    Array<Planet> planetTroops = new Array<Planet>(this.eui.empire.GetPlanets()
                        .Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, this.p.Center))
                        .Where(Name => Name.Name != this.p.Name));

                    if (troopShips.Count > 0)
                    {
                        GameAudio.PlaySfxAsync("echo_affirm");
                        troopShips.First().AI.OrderRebase(this.p,true);
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
                                troop.AI.OrderRebase(this.p,true);
                            }
                        }
                    }
                    else
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                    }
                }
            }
            if (!this.edit_name_button.HitTest(MousePos))
            {
                this.editHoverState = 0;
            }
            else
            {
                this.editHoverState = 1;
                if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                {
                    this.PlanetName.HandlingInput = true;
                }
            }
            if (!this.PlanetName.HandlingInput)
            {
                GlobalStats.TakingInput = false;
                bool empty = true;
                string text = this.PlanetName.Text;
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
                    foreach (SolarSystem.Ring ring in this.p.ParentSystem.RingList)
                    {
                        if (ring.planet == this.p)
                        {
                            this.PlanetName.Text = string.Concat(this.p.ParentSystem.Name, " ", NumberToRomanConvertor.NumberToRoman(ringnum));
                        }
                        ringnum++;
                    }
                }
            }
            else
            {
                GlobalStats.TakingInput = true;
                this.PlanetName.HandleTextInput(ref this.PlanetName.Text, input);
            }
            this.GovernorDropdown.HandleInput(input);
            if (this.GovernorDropdown.ActiveValue != (int)this.p.colonyType)
            {
                this.p.colonyType = (Planet.ColonyType)this.GovernorDropdown.ActiveValue;
                if (this.p.colonyType != Planet.ColonyType.Colony)
                {
                    this.p.FoodLocked = true;
                    this.p.ProdLocked = true;
                    this.p.ResLocked = true;
                    this.p.GovernorOn = true;
                }
                else
                {
                    this.p.GovernorOn = false;
                    this.p.FoodLocked = false;
                    this.p.ProdLocked = false;
                    this.p.ResLocked = false;
                }
            }
            this.HandleSlider();
            if (this.p.HasShipyard && this.build.Tabs.Count > 1 && this.build.Tabs[1].Selected)
            {
                if (this.playerDesignsToggle.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2225));
                }
                if (this.playerDesignsToggle.HandleInput(input))
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
                    if (GlobalStats.ShowAllDesigns)
                    {
                        this.playerDesignsToggle.Active = true;
                    }
                    else
                    {
                        this.playerDesignsToggle.Active = false;
                    }
                    this.Reset = true;
                }
            }
            if (this.p.colonyType != Planet.ColonyType.Colony)
            {
                this.FoodLock.Locked = true;
                this.ProdLock.Locked = true;
                this.ResLock.Locked = true;
            }
            else
            {
                if (!this.FoodLock.LockRect.HitTest(MousePos) || this.p.Owner == null || this.p.Owner.data.Traits.Cybernetic != 0)
                {
                    this.FoodLock.Hover = false;
                }
                else
                {
                    if (this.FoodLock.Locked)
                    {
                        this.FoodLock.Hover = false;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.FoodLocked = false;
                            this.FoodLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        this.FoodLock.Hover = true;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.FoodLocked = true;
                            this.FoodLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
                if (!this.ProdLock.LockRect.HitTest(MousePos))
                {
                    this.ProdLock.Hover = false;
                }
                else
                {
                    if (this.ProdLock.Locked)
                    {
                        this.ProdLock.Hover = false;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.ProdLocked = false;
                            this.ProdLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        this.ProdLock.Hover = true;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.ProdLocked = true;
                            this.ProdLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
                if (!this.ResLock.LockRect.HitTest(MousePos))
                {
                    this.ResLock.Hover = false;
                }
                else
                {
                    if (this.ResLock.Locked)
                    {
                        this.ResLock.Hover = false;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.ResLocked = false;
                            this.ResLock.Locked = false;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else
                    {
                        this.ResLock.Hover = true;
                        if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                        {
                            this.p.ResLocked = true;
                            this.ResLock.Locked = true;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    ToolTip.CreateTooltip(69);
                }
            }
            this.selector = null;
            this.ClickedTroop = false;
            foreach (PlanetGridSquare pgs in this.p.TilesList)
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
                this.detailInfo = pgs.TroopsHere[0];
                if (input.RightMouseClick && pgs.TroopsHere[0].GetOwner() == EmpireManager.Player)
                {
                    GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    Ship.CreateTroopShipAtPoint(this.p.Owner.data.DefaultTroopShip, this.p.Owner, this.p.Center, pgs.TroopsHere[0]);
                    this.p.TroopsHere.Remove(pgs.TroopsHere[0]);
                    pgs.TroopsHere[0].SetPlanet(null);
                    pgs.TroopsHere.Clear();
                    this.ClickedTroop = true;
                    this.detailInfo = null;
                    rmouse = true;
                }
                return true;                
            }
            if (!this.ClickedTroop)
            {
                foreach (PlanetGridSquare pgs in this.p.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        this.detailInfo = pgs;
                        Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                        if (pgs.building != null && pgs.building.Scrappable && bRect.HitTest(input.CursorPosition) && input.RightMouseClick)
                        {
                            this.toScrap = pgs.building;
                            string message = string.Concat("Do you wish to scrap ", Localizer.Token(pgs.building.NameTranslationIndex), "? Half of the building's construction cost will be recovered to your storage.");
                            MessageBoxScreen messageBox = new MessageBoxScreen(Empire.Universe, message);
                            messageBox.Accepted += new EventHandler<EventArgs>(this.ScrapAccepted);
                            this.ScreenManager.AddScreen(messageBox);
                            this.ClickedTroop = true;
                            rmouse = true;
                            return true;
                        }
                    }
                    if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(input.CursorPosition))
                    {
                        continue;
                    }
                    this.detailInfo = pgs.TroopsHere;
                }
            }
            if (!GlobalStats.HardcoreRuleset)
            {
                if (this.foodDropDown.r.HitTest(MousePos) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                {
                    this.foodDropDown.Toggle();
                    Planet planet = this.p;
                    planet.fs = (Planet.GoodState)((int)planet.fs + (int)Planet.GoodState.IMPORT);
                    if (this.p.fs > Planet.GoodState.EXPORT)
                    {
                        this.p.fs = Planet.GoodState.STORE;
                    }
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
                if (this.prodDropDown.r.HitTest(MousePos) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                {
                    this.prodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    Planet planet1 = this.p;
                    planet1.ps = (Planet.GoodState)((int)planet1.ps + (int)Planet.GoodState.IMPORT);
                    if (this.p.ps > Planet.GoodState.EXPORT)
                    {
                        this.p.ps = Planet.GoodState.STORE;
                    }
                }
            }
            else
            {
                foreach (ThreeStateButton b in this.ResourceButtons)
                {
                    b.HandleInput(input, this.ScreenManager);
                }
            }
            for (int i = this.QSL.indexAtTop; i < this.QSL.Copied.Count && i < this.QSL.indexAtTop + this.QSL.entriesToDisplay; i++)
            {
                try
                {
                    ScrollList.Entry e = this.QSL.Copied[i];
                    if (!e.clickRect.HitTest(MousePos))
                    {
                        e.clickRectHover = 0;
                    }
                    else
                    {
                        this.selector = new Selector(e.clickRect);
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
                    if (e.up.HitTest(MousePos))
                    {
                        ToolTip.CreateTooltip(63);
                        if (!input.KeysCurr.IsKeyDown(Keys.RightControl) && !input.KeysCurr.IsKeyDown(Keys.LeftControl) || this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
                        {
                            if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released && i > 0)
                            {
                                object tmp = this.p.ConstructionQueue[i - 1];
                                this.p.ConstructionQueue[i - 1] = this.p.ConstructionQueue[i];
                                this.p.ConstructionQueue[i] = tmp as QueueItem;
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            }
                        }
                        else if (i > 0)
                        {
                            var item = p.ConstructionQueue[i];
                            p.ConstructionQueue.Remove(item);
                            p.ConstructionQueue.Insert(0, item);
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            break;
                        }
                    }
                    if (e.down.HitTest(MousePos))
                    {
                        ToolTip.CreateTooltip(64);
                        if (!input.KeysCurr.IsKeyDown(Keys.RightControl) && !input.KeysCurr.IsKeyDown(Keys.LeftControl) || this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
                        {
                            if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released && i + 1 < this.QSL.Copied.Count)
                            {
                                object tmp = this.p.ConstructionQueue[i + 1];
                                this.p.ConstructionQueue[i + 1] = this.p.ConstructionQueue[i];
                                this.p.ConstructionQueue[i] = tmp as QueueItem;
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            }
                        }
                        else if (i + 1 < this.QSL.Copied.Count)
                        {
                            var item = p.ConstructionQueue[i];
                            p.ConstructionQueue.Remove(item);
                            p.ConstructionQueue.Insert(0, item);
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            break;
                        }
                    }
                    if (e.apply.HitTest(MousePos) && !this.p.RecentCombat && this.p.Crippled_Turns <= 0)
                    {
                        if (!input.KeysCurr.IsKeyDown(Keys.RightControl) && !input.KeysCurr.IsKeyDown(Keys.LeftControl) || this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
                        {
                            if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                            {
                                
                                if (this.p.ApplyStoredProduction(i))
                                {
                                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                }
                                else
                                {
                                    GameAudio.PlaySfxAsync("UI_Misc20");
                                }
                            }
                        }
                        //else if (Empire.Universe.Debug)
                        //{
                        //    this.p.ApplyProductiontoQueue(this.p.ConstructionQueue[i].Cost - this.p.ConstructionQueue[i].productionTowards, i);
                        //}
                        else if (this.p.ProductionHere == 0f)
                        {
                            GameAudio.PlaySfxAsync("UI_Misc20");
                        }
                        else
                        {
                            this.p.ApplyAllStoredProduction(i);
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    if (e.cancel.HitTest(MousePos) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                    {
                        Planet productionHere2 = this.p;
                        productionHere2.ProductionHere = productionHere2.ProductionHere + (e.item as QueueItem).productionTowards;
                        if (this.p.ProductionHere > this.p.MAX_STORAGE)
                        {
                            this.p.ProductionHere = this.p.MAX_STORAGE;
                        }
                        QueueItem item = (e.item as QueueItem);
                        if (item.pgs != null)
                        {
                            (e.item as QueueItem).pgs.QItem = null;
                        }
                        if(item.Goal !=null)
                        {
                            if(item.Goal.GoalName=="BuildConstructionShip")
                            {
                                p.Owner.GetGSAI().Goals.Remove(item.Goal);
                                
                            }
                            if(item.Goal.GetFleet() !=null)
                                p.Owner.GetGSAI().Goals.Remove(item.Goal);

                        }
                        this.p.ConstructionQueue.Remove(e.item as QueueItem);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                }
                catch
                {
                }
            }
            this.QSL.HandleInput(input, this.p);
            if (this.ActiveBuildingEntry != null)
            {
                foreach (PlanetGridSquare pgs in this.p.TilesList)
                {
                    if (!pgs.ClickRect.HitTest(MousePos) || this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
                    {
                        continue;
                    }
                    if (pgs.Habitable && pgs.building == null && pgs.QItem == null && (this.ActiveBuildingEntry.item as Building).Name != "Biospheres")
                    {
                        QueueItem qi = new QueueItem();
                        //{
                        qi.isBuilding = true;
                        qi.Building = this.ActiveBuildingEntry.item as Building;       //ResourceManager.GetBuilding((this.ActiveBuildingEntry.item as Building).Name);
                        qi.IsPlayerAdded = true;
                        qi.Cost = ResourceManager.BuildingsDict[qi.Building.Name].Cost * UniverseScreen.GamePaceStatic;
                        qi.productionTowards = 0f;
                        qi.pgs = pgs;
                        //};
                        pgs.QItem = qi;
                        this.p.ConstructionQueue.Add(qi);
                        this.ActiveBuildingEntry = null;
                        break;
                    }
                    else if (pgs.Habitable || pgs.Biosphere || pgs.QItem != null || !(this.ActiveBuildingEntry.item as Building).CanBuildAnywhere)
                    {
                        GameAudio.PlaySfxAsync("UI_Misc20");
                        this.ActiveBuildingEntry = null;
                        break;
                    }
                    else
                    {
                        QueueItem qi = new QueueItem();
                        //{
                        qi.isBuilding = true;
                        qi.Building = this.ActiveBuildingEntry.item as Building;
                        qi.Cost = qi.Building.Cost *UniverseScreen.GamePaceStatic; //ResourceManager.BuildingsDict[qi.Building.Name].Cost 
                        qi.productionTowards = 0f;
                        qi.pgs = pgs;
                        qi.IsPlayerAdded = true;
                        //};
                        pgs.QItem = qi;
                        this.p.ConstructionQueue.Add(qi);
                        this.ActiveBuildingEntry = null;
                        break;
                    }
                }
                if (this.ActiveBuildingEntry != null)
                {
                    foreach (QueueItem qi in this.p.ConstructionQueue)
                    {
                        if (!qi.isBuilding || !(qi.Building.Name == (this.ActiveBuildingEntry.item as Building).Name) || !(this.ActiveBuildingEntry.item as Building).Unique)
                        {
                            continue;
                        }
                        this.ActiveBuildingEntry = null;
                        break;
                    }
                }
                if (this.currentMouse.RightButton == ButtonState.Pressed && this.previousMouse.RightButton == ButtonState.Released)
                {
                    this.ClickedTroop = true;
                    this.ActiveBuildingEntry = null;
                }
                if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
                {
                    this.ClickedTroop = true;
                    this.ActiveBuildingEntry = null;
                }
            }
            for (int i = this.buildSL.indexAtTop; i < this.buildSL.Copied.Count && i < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.buildSL.Copied[i];
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).HandleInput(input, e);
                }
                else if (!e.clickRect.HitTest(MousePos))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    this.selector = new Selector(e.clickRect);
                    if (e.clickRectHover == 0)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed && e.item is Building && this.ActiveBuildingEntry == null)
                    {
                        this.ActiveBuildingEntry = e;
                    }
                    if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        if (this.ClickTimer >= this.TimerDelay)
                        {
                            this.ClickTimer = 0f;
                        }
                        else
                        {
                            Rectangle rectangle = e.addRect;
                            if (!e.addRect.HitTest(input.CursorPosition))
                            {
                                QueueItem qi = new QueueItem();
                                if (e.item is Ship)
                                {
                                    qi.isShip = true;
                                    qi.sData = (e.item as Ship).GetShipData();
                                    qi.Cost = (e.item as Ship).GetCost(this.p.Owner);
                                    qi.productionTowards = 0f;
                                    this.p.ConstructionQueue.Add(qi);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.item is Troop troop)
                                {
                                    qi.isTroop = true;
                                    qi.troopType = troop.Name;
                                    qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                                    qi.productionTowards = 0f;
                                    this.p.ConstructionQueue.Add(qi);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.item is Building)
                                {
                                    //Building waitaddstuff = ResourceManager.GetBuilding((e.item as Building).Name);
                                    //waitaddstuff.IsPlayerAdded=true;
                                    this.p.AddBuildingToCQ(e.item as Building,true);
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                            }
                        }
                    }
                }
                Rectangle rectangle1 = e.addRect;
                if (!e.addRect.HitTest(MousePos))
                {
                    e.PlusHover = 0;
                }
                else
                {
                    e.PlusHover = 1;
                    ToolTip.CreateTooltip(51);
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                    {
                        QueueItem qi = new QueueItem();
                        if (e.item is Building)
                        {
                            //Building b = ResourceManager.GetBuilding((e.item as Building).Name);
                            //b.IsPlayerAdded =true;
                            this.p.AddBuildingToCQ(e.item as Building,true);
                        }
                        else if (e.item is Ship)
                        {
                            qi.isShip = true;
                            qi.sData = (e.item as Ship).GetShipData();
                            qi.Cost = (e.item as Ship).GetCost(this.p.Owner);
                            qi.productionTowards = 0f;
                            this.p.ConstructionQueue.Add(qi);
                        }
                        else if (e.item is Troop troop)
                        {
                            qi.isTroop = true;
                            qi.troopType = troop.Name;
                            qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                            qi.productionTowards = 0f;
                            this.p.ConstructionQueue.Add(qi);
                        }
                    }
                }
                Rectangle rectangle2 = e.editRect;
                if (!e.editRect.HitTest(MousePos))
                {
                    e.EditHover = 0;
                }
                else
                {
                    e.EditHover = 1;
                    ToolTip.CreateTooltip(52);
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                    {
                        ShipDesignScreen sdScreen = new ShipDesignScreen(Empire.Universe, this.eui);
                        this.ScreenManager.AddScreen(sdScreen);
                        sdScreen.ChangeHull((e.item as Ship).GetShipData());
                    }
                }
            }
            this.shipsCanBuildLast = this.p.Owner.ShipsWeCanBuild.Count;
            this.buildingsHereLast = this.p.BuildingList.Count;
            this.buildingsCanBuildLast = this.BuildingsCanBuild.Count;

            if (popup)
            {
                if (input.MouseCurr.RightButton != ButtonState.Released || input.MousePrev.RightButton != ButtonState.Released)
                    return true;
                popup = false;
            }
            else 
                {
                if (input.RightMouseClick && !this.ClickedTroop) rmouse = false;
                if (!rmouse && (input.MouseCurr.RightButton != ButtonState.Released || this.previousMouse.RightButton != ButtonState.Released))
                {
                    Empire.Universe.ShipsInCombat.Visible = true;
                    Empire.Universe.PlanetsInCombat.Visible = true;
                }
                this.previousMouse = this.currentMouse;
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
            return false;
        }

        private void HandleSlider()
        {
            Vector2 mousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            if (this.p.Owner.data.Traits.Cybernetic == 0)
            {
                if (this.ColonySliderFood.sRect.HitTest(mousePos) || this.draggingSlider1)
                {
                    this.ColonySliderFood.state = "hover";
                    this.ColonySliderFood.Color = new Color(164, 154, 133);
                }
                else
                {
                    this.ColonySliderFood.state = "normal";
                    this.ColonySliderFood.Color = new Color(72, 61, 38);
                }
                if (this.ColonySliderFood.cursor.HitTest(mousePos) || this.draggingSlider1)
                {
                    this.ColonySliderFood.cState = "hover";
                }
                else
                {
                    this.ColonySliderFood.cState = "normal";
                }
            }
            if (this.ColonySliderProd.sRect.HitTest(mousePos) || this.draggingSlider2)
            {
                this.ColonySliderProd.state = "hover";
                this.ColonySliderProd.Color = new Color(164, 154, 133);
            }
            else
            {
                this.ColonySliderProd.state = "normal";
                this.ColonySliderProd.Color = new Color(72, 61, 38);
            }
            if (this.ColonySliderProd.cursor.HitTest(mousePos) || this.draggingSlider2)
            {
                this.ColonySliderProd.cState = "hover";
            }
            else
            {
                this.ColonySliderProd.cState = "normal";
            }
            if (this.ColonySliderRes.sRect.HitTest(mousePos) || this.draggingSlider3)
            {
                this.ColonySliderRes.state = "hover";
                this.ColonySliderRes.Color = new Color(164, 154, 133);
            }
            else
            {
                this.ColonySliderRes.state = "normal";
                this.ColonySliderRes.Color = new Color(72, 61, 38);
            }
            if (this.ColonySliderRes.cursor.HitTest(mousePos) || this.draggingSlider3)
            {
                this.ColonySliderRes.cState = "hover";
            }
            else
            {
                this.ColonySliderRes.cState = "normal";
            }
            if (this.ColonySliderFood.cursor.HitTest(mousePos) && (!this.ProdLock.Locked || !this.ResLock.Locked) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed && !this.FoodLock.Locked)
            {
                this.draggingSlider1 = true;
            }
            if (this.ColonySliderProd.cursor.HitTest(mousePos) && (!this.FoodLock.Locked || !this.ResLock.Locked) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed && !this.ProdLock.Locked)
            {
                this.draggingSlider2 = true;
            }
            if (this.ColonySliderRes.cursor.HitTest(mousePos) && (!this.ProdLock.Locked || !this.FoodLock.Locked) && this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed && !this.ResLock.Locked)
            {
                this.draggingSlider3 = true;
            }
            if (this.draggingSlider1 && !this.FoodLock.Locked && (!this.ProdLock.Locked || !this.ResLock.Locked))
            {
                this.ColonySliderFood.cursor.X = this.currentMouse.X;
                if (this.ColonySliderFood.cursor.X > this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width)
                {
                    this.ColonySliderFood.cursor.X = this.ColonySliderFood.sRect.X + this.ColonySliderFood.sRect.Width;
                }
                else if (this.ColonySliderFood.cursor.X < this.ColonySliderFood.sRect.X)
                {
                    this.ColonySliderFood.cursor.X = this.ColonySliderFood.sRect.X;
                }
                if (this.currentMouse.LeftButton == ButtonState.Released)
                {
                    this.draggingSlider1 = false;
                }
                this.fPercentLast = this.p.FarmerPercentage;
                this.p.FarmerPercentage = ((float)this.ColonySliderFood.cursor.X - (float)this.ColonySliderFood.sRect.X) / (float)this.ColonySliderFood.sRect.Width;
                float difference = this.fPercentLast - this.p.FarmerPercentage;
                if (!this.ProdLock.Locked && !this.ResLock.Locked)
                {
                    Planet workerPercentage = this.p;
                    workerPercentage.WorkerPercentage = workerPercentage.WorkerPercentage + difference / 2f;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet farmerPercentage = this.p;
                        farmerPercentage.FarmerPercentage = farmerPercentage.FarmerPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                    Planet researcherPercentage = this.p;
                    researcherPercentage.ResearcherPercentage = researcherPercentage.ResearcherPercentage + difference / 2f;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet planet = this.p;
                        planet.FarmerPercentage = planet.FarmerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (this.ProdLock.Locked && !this.ResLock.Locked)
                {
                    Planet researcherPercentage1 = this.p;
                    researcherPercentage1.ResearcherPercentage = researcherPercentage1.ResearcherPercentage + difference;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet farmerPercentage1 = this.p;
                        farmerPercentage1.FarmerPercentage = farmerPercentage1.FarmerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (!this.ProdLock.Locked && this.ResLock.Locked)
                {
                    Planet workerPercentage1 = this.p;
                    workerPercentage1.WorkerPercentage = workerPercentage1.WorkerPercentage + difference;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet planet1 = this.p;
                        planet1.FarmerPercentage = planet1.FarmerPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                }
            }
            if (this.draggingSlider2 && !this.ProdLock.Locked && (!this.FoodLock.Locked || !this.ResLock.Locked))
            {
                this.ColonySliderProd.cursor.X = this.currentMouse.X;
                if (this.ColonySliderProd.cursor.X > this.ColonySliderProd.sRect.X + this.ColonySliderProd.sRect.Width)
                {
                    this.ColonySliderProd.cursor.X = this.ColonySliderProd.sRect.X + this.ColonySliderProd.sRect.Width;
                }
                else if (this.ColonySliderProd.cursor.X < this.ColonySliderProd.sRect.X)
                {
                    this.ColonySliderProd.cursor.X = this.ColonySliderProd.sRect.X;
                }
                if (this.currentMouse.LeftButton == ButtonState.Released)
                {
                    this.draggingSlider2 = false;
                }
                this.pPercentLast = this.p.WorkerPercentage;
                this.p.WorkerPercentage = ((float)this.ColonySliderProd.cursor.X - (float)this.ColonySliderProd.sRect.X) / (float)this.ColonySliderProd.sRect.Width;
                float difference = this.pPercentLast - this.p.WorkerPercentage;
                if (!this.FoodLock.Locked && !this.ResLock.Locked)
                {
                    Planet farmerPercentage2 = this.p;
                    farmerPercentage2.FarmerPercentage = farmerPercentage2.FarmerPercentage + difference / 2f;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet workerPercentage2 = this.p;
                        workerPercentage2.WorkerPercentage = workerPercentage2.WorkerPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                    Planet researcherPercentage2 = this.p;
                    researcherPercentage2.ResearcherPercentage = researcherPercentage2.ResearcherPercentage + difference / 2f;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet planet2 = this.p;
                        planet2.WorkerPercentage = planet2.WorkerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (this.FoodLock.Locked && !this.ResLock.Locked)
                {
                    Planet researcherPercentage3 = this.p;
                    researcherPercentage3.ResearcherPercentage = researcherPercentage3.ResearcherPercentage + difference;
                    if (this.p.ResearcherPercentage < 0f)
                    {
                        Planet workerPercentage3 = this.p;
                        workerPercentage3.WorkerPercentage = workerPercentage3.WorkerPercentage + this.p.ResearcherPercentage;
                        this.p.ResearcherPercentage = 0f;
                    }
                }
                else if (!this.FoodLock.Locked && this.ResLock.Locked)
                {
                    Planet farmerPercentage3 = this.p;
                    farmerPercentage3.FarmerPercentage = farmerPercentage3.FarmerPercentage + difference;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet planet3 = this.p;
                        planet3.WorkerPercentage = planet3.WorkerPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
            }
            if (this.draggingSlider3 && !this.ResLock.Locked && (!this.FoodLock.Locked || !this.ProdLock.Locked))
            {
                this.ColonySliderRes.cursor.X = this.currentMouse.X;
                if (this.ColonySliderRes.cursor.X > this.ColonySliderRes.sRect.X + this.ColonySliderRes.sRect.Width)
                {
                    this.ColonySliderRes.cursor.X = this.ColonySliderRes.sRect.X + this.ColonySliderRes.sRect.Width;
                }
                else if (this.ColonySliderRes.cursor.X < this.ColonySliderRes.sRect.X)
                {
                    this.ColonySliderRes.cursor.X = this.ColonySliderRes.sRect.X;
                }
                if (this.currentMouse.LeftButton == ButtonState.Released)
                {
                    this.draggingSlider3 = false;
                }
                this.rPercentLast = this.p.ResearcherPercentage;
                this.p.ResearcherPercentage = ((float)this.ColonySliderRes.cursor.X - (float)this.ColonySliderRes.sRect.X) / (float)this.ColonySliderRes.sRect.Width;
                float difference = this.rPercentLast - this.p.ResearcherPercentage;
                if (!this.ProdLock.Locked && !this.FoodLock.Locked)
                {
                    Planet workerPercentage4 = this.p;
                    workerPercentage4.WorkerPercentage = workerPercentage4.WorkerPercentage + difference / 2f;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet researcherPercentage4 = this.p;
                        researcherPercentage4.ResearcherPercentage = researcherPercentage4.ResearcherPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
                    }
                    Planet farmerPercentage4 = this.p;
                    farmerPercentage4.FarmerPercentage = farmerPercentage4.FarmerPercentage + difference / 2f;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet planet4 = this.p;
                        planet4.ResearcherPercentage = planet4.ResearcherPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
                else if (this.ProdLock.Locked && !this.FoodLock.Locked)
                {
                    Planet farmerPercentage5 = this.p;
                    farmerPercentage5.FarmerPercentage = farmerPercentage5.FarmerPercentage + difference;
                    if (this.p.FarmerPercentage < 0f)
                    {
                        Planet researcherPercentage5 = this.p;
                        researcherPercentage5.ResearcherPercentage = researcherPercentage5.ResearcherPercentage + this.p.FarmerPercentage;
                        this.p.FarmerPercentage = 0f;
                    }
                }
                else if (!this.ProdLock.Locked && this.FoodLock.Locked)
                {
                    Planet workerPercentage5 = this.p;
                    workerPercentage5.WorkerPercentage = workerPercentage5.WorkerPercentage + difference;
                    if (this.p.WorkerPercentage < 0f)
                    {
                        Planet planet5 = this.p;
                        planet5.ResearcherPercentage = planet5.ResearcherPercentage + this.p.WorkerPercentage;
                        this.p.WorkerPercentage = 0f;
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

        private string parseText(string text, float Width)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(new char[] { ' ' });
            for (int i = 0; i < (int)strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (Fonts.Arial12Bold.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }

        public void ResetLists()
        {
            this.Reset = true;
        }

        private void ScrapAccepted(object sender, EventArgs e)
        {
            if (this.toScrap != null)
            {
                this.p.ScrapBuilding(this.toScrap);
            }
            this.Update(0f);
        }

        public override void Update(float elapsedTime)
        {
            this.p.UpdateIncomes(false);
            if (!this.p.CanBuildInfantry())
            {
                bool remove = false;
                foreach (Submenu.Tab tab in this.build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    this.build.Tabs.Clear();
                    this.build.AddTab(Localizer.Token(334));
                    if (this.p.HasShipyard)
                    {
                        this.build.AddTab(Localizer.Token(335));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in this.build.Tabs)
                {
                    if (tab.Title != Localizer.Token(336))
                    {
                        continue;
                    }
                    add = false;
                    for (int index = this.buildSL.indexAtTop; index < this.buildSL.Entries.Count; ++index)
                    {
                        if (index < this.buildSL.indexAtTop + this.buildSL.entriesToDisplay)
                        {
                            try
                            {
                                ScrollList.Entry entry = this.buildSL.Entries[index];
                                if (entry != null)
                                {
                                    if (entry.item is Troop troop) troop.Update(elapsedTime);
                                }
                            }
                            catch
                            { }

                        }
                    }

                }
                if (add)
                {
                    this.build.Tabs.Clear();
                    this.build.AddTab(Localizer.Token(334));
                    if (this.p.HasShipyard)
                    {
                        this.build.AddTab(Localizer.Token(335));
                    }
                    this.build.AddTab(Localizer.Token(336));
                }
            }
            if (!this.p.HasShipyard)
            {
                bool remove = false;
                foreach (Submenu.Tab tab in this.build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    this.build.Tabs.Clear();
                    this.build.AddTab(Localizer.Token(334));
                    if (this.p.AllowInfantry)
                    {
                        this.build.AddTab(Localizer.Token(336));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in this.build.Tabs)
                {
                    if (tab.Title != Localizer.Token(335))
                    {
                        continue;
                    }
                    add = false;
                }
                if (add)
                {
                    this.build.Tabs.Clear();
                    this.build.AddTab(Localizer.Token(334));
                    this.build.AddTab(Localizer.Token(335));
                    if (this.p.AllowInfantry)
                    {
                        this.build.AddTab(Localizer.Token(336));
                        return;
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

            public Color Color = new Color((byte)72, (byte)61, (byte)38);
            public string state  = "normal";
            public string cState = "normal";

            public ColonySlider()
            {
            }
        }

        protected override void Destroy()
        {
            buildSL?.Dispose(ref buildSL);
            QSL?.Dispose(ref QSL);
            CommoditiesSL?.Dispose(ref CommoditiesSL);
            base.Destroy();
        }
    }
}
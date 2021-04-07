using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Ship_Game
{
    public sealed class ColonyScreen : PlanetScreen, IListScreen
    {
        public Planet P;
        private ToggleButton PlayerDesignsToggle;

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
        private Rectangle GridPos;
        private Submenu subColonyGrid;
        private ScrollList buildSL;
        private ScrollList QSL;
        private DropDownMenu foodDropDown;
        private DropDownMenu prodDropDown;
        private ProgressBar FoodStorage;
        private ProgressBar ProdStorage;
        private Rectangle FoodStorageIcon;
        private Rectangle ProfStorageIcon;

        ColonySliderGroup Sliders;

        private object DetailInfo;
        private Building ToScrap;
        private ScrollList.Entry ActiveBuildingEntry;

        public bool ClickedTroop;
        public bool Reset;
        private int ShipsCanBuildLast;
        private int EditHoverState;

        private Selector Selector;
        private Rectangle EditNameButton;
        private Array<Building> BuildingsCanBuild = new Array<Building>();
        private GenericButton ChangeGovernor = new GenericButton(new Rectangle(), Localizer.Token(GameText.Change), Fonts.Pirulen16);
        private static bool Popup;  //fbedard
        private readonly SpriteFont Font8 = Fonts.Arial8Bold;
        private readonly SpriteFont Font12 = Fonts.Arial12Bold;
        private readonly SpriteFont Font20 = Fonts.Arial20Bold;

        public ColonyScreen(GameScreen parent, Planet p, EmpireUIOverlay empUI) : base(parent)
        {
            P = p;
            empUI.empire.UpdateShipsWeCanBuild();
            eui = empUI;
            var theMenu1 = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(theMenu1);
            LeftColony = new ToggleButton(new Vector2(theMenu1.X + 25, theMenu1.Y + 24), ToggleButtonStyle.ArrowLeft);
            RightColony = new ToggleButton(new Vector2(theMenu1.X + theMenu1.Width - 39, theMenu1.Y + 24), ToggleButtonStyle.ArrowRight);
            TitlePos = new Vector2(theMenu1.X + theMenu1.Width / 2 - Fonts.Laserian14.MeasureString("Colony Overview").X / 2f, theMenu1.Y + theMenu1.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            var theMenu2 = new Rectangle(2, theMenu1.Y + theMenu1.Height + 5, theMenu1.Width, ScreenHeight - (theMenu1.Y + theMenu1.Height) - 7);
            LeftMenu = new Menu1(theMenu2);
            var theMenu3 = new Rectangle(theMenu1.X + theMenu1.Width + 10, theMenu1.Y, ScreenWidth / 3 - 15, ScreenHeight - theMenu1.Y - 2);
            RightMenu = new Menu1(theMenu3);
            var iconMoney = ResourceManager.Texture("NewUI/icon_money");
            MoneyRect = new Rectangle(theMenu2.X + theMenu2.Width - 75, theMenu2.Y + 20, iconMoney.Width, iconMoney.Height);
            close = new CloseButton(this, new Rectangle(theMenu3.X + theMenu3.Width - 52, theMenu3.Y + 22, 20, 20));
            var theMenu4 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            PlanetInfo = new Submenu(theMenu4);
            PlanetInfo.AddTab(Localizer.Token(GameText.PlanetInfo));
            var theMenu5 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pDescription = new Submenu(theMenu5);

            var laborPanel = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pLabor = new Submenu(laborPanel);
            pLabor.AddTab(Localizer.Token(GameText.AssignLabor));

            CreateSliders(laborPanel);

            var theMenu7 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + laborPanel.Height + 40, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pStorage = new Submenu(theMenu7);
            pStorage.AddTab(Localizer.Token(GameText.Storage));

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
                FoodStorage.Max = p.Storage.Max;
                FoodStorage.Progress = p.FoodHere;
                FoodStorage.color = "green";
                foodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                foodDropDown.AddOption(Localizer.Token(GameText.Store));
                foodDropDown.AddOption(Localizer.Token(GameText.Import));
                foodDropDown.AddOption(Localizer.Token(GameText.Export));
                foodDropDown.ActiveIndex = (int)p.FS;
                var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
                FoodStorageIcon = new Rectangle(theMenu7.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
                ProdStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.660000026226044 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
                ProdStorage.Max = p.Storage.Max;
                ProdStorage.Progress = p.ProdHere;
                var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
                ProfStorageIcon = new Rectangle(theMenu7.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
                prodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
                prodDropDown.AddOption(Localizer.Token(GameText.Store));
                prodDropDown.AddOption(Localizer.Token(GameText.Import));
                prodDropDown.AddOption(Localizer.Token(GameText.Export));
                prodDropDown.ActiveIndex = (int)p.PS;
            }
            var theMenu8 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu4.Y, theMenu2.Width - 60 - theMenu4.Width, (int)(theMenu2.Height * 0.5));
            subColonyGrid = new Submenu(theMenu8);
            subColonyGrid.AddTab(Localizer.Token(GameText.Colony));
            var theMenu9 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu8.Y + theMenu8.Height + 20, theMenu2.Width - 60 - theMenu4.Width, theMenu2.Height - 20 - theMenu8.Height - 40);
            pFacilities = new Submenu(theMenu9);
            pFacilities.AddTab(Localizer.Token(GameText.Detail));

            launchTroops = Button(theMenu9.X + theMenu9.Width - 175, theMenu9.Y - 5, "Launch Troops", OnLaunchTroopsClicked);
            SendTroops = Button(theMenu9.X + theMenu9.Width - launchTroops.Rect.Width - 185,
                                theMenu9.Y - 5, "Send Troops", OnSendTroopsClicked);

            CommoditiesSL = new ScrollList(pFacilities, 40);
            var theMenu10 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20, theMenu3.Width - 40, (int)(0.5 * (theMenu3.Height - 60)));
            build = new Submenu(theMenu10);
            build.AddTab(Localizer.Token(GameText.Buildings));
            buildSL = new ScrollList(build);
            PlayerDesignsToggle = new ToggleButton(
                new Vector2(build.Menu.X + build.Menu.Width - 270, build.Menu.Y),
                ToggleButtonStyle.Grid, "SelectionBox/icon_grid");

            PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
            if (p.HasSpacePort)
                build.AddTab(Localizer.Token(GameText.Ships));
            if (p.AllowInfantry)
                build.AddTab(Localizer.Token(GameText.Troops));
            var theMenu11 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20 + 20 + theMenu10.Height, theMenu3.Width - 40, theMenu3.Height - 40 - theMenu10.Height - 20 - 3);
            queue = new Submenu(theMenu11);
            queue.AddTab(Localizer.Token(GameText.ConstructionQueue));

            QSL = new ScrollList(queue, ListOptions.Draggable);

            PlanetIcon = new Rectangle(theMenu4.X + theMenu4.Width - 148, theMenu4.Y + (theMenu4.Height - 25) / 2 - 64 + 25, 128, 128);
            GridPos = new Rectangle(subColonyGrid.Menu.X + 10, subColonyGrid.Menu.Y + 30, subColonyGrid.Menu.Width - 20, subColonyGrid.Menu.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.x * width, GridPos.Y + planetGridSquare.y * height, width, height);
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 12;
            if (p.Owner != null)
            {
                ShipsCanBuildLast = p.Owner.ShipsWeCanBuild.Count;
                DetailInfo = p.Description;
                var rectangle4 = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
                var rectangle5 = new Rectangle(rectangle4.X + rectangle4.Width + 20, rectangle4.Y + rectangle4.Height - 15, (int)Fonts.Pirulen16.MeasureString(Localizer.Token(GameText.Change)).X, Fonts.Pirulen16.LineSpacing);
                GovernorDropdown = new DropOptions<int>(this, new Rectangle(rectangle5.X + 30, rectangle5.Y + 30, 100, 18));
                GovernorDropdown.AddOption("--", 1);
                GovernorDropdown.AddOption(Localizer.Token(GameText.Core), 0);
                GovernorDropdown.AddOption(Localizer.Token(GameText.Industrial), 2);
                GovernorDropdown.AddOption(Localizer.Token(GameText.Agricultural), 4);
                GovernorDropdown.AddOption(Localizer.Token(GameText.Research), 3);
                GovernorDropdown.AddOption(Localizer.Token(GameText.Military), 5);
                GovernorDropdown.AddOption(Localizer.Token(GameText.TradeHub), 6);
                GovernorDropdown.ActiveIndex = GetIndex(p);

                P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;

                // @todo add localization
                GovBuildings = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Font12.LineSpacing * 2 + 15, 
                                            () => p.GovBuildings, Font12, "Governor manages buildings", 0);

                GovSliders = new UICheckBox(this, rectangle5.X - 10, rectangle5.Y - Font12.LineSpacing + 10,
                                          () => p.GovSliders, Font12, "Governor manages labor sliders", 0);
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            ClickTimer += (float)GameTime.ElapsedGameTime.TotalSeconds;
            if (P.Owner == null)
                return;
            P.UpdateIncomes(false);
            LeftMenu.Draw();
            RightMenu.Draw();
            TitleBar.Draw(batch);
            LeftColony.Draw(ScreenManager);
            RightColony.Draw(ScreenManager);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.ColonyOverview), TitlePos, new Color(255, 239, 208));
            if (!GlobalStats.HardcoreRuleset)
            {
                FoodStorage.Max = P.Storage.Max;
                FoodStorage.Progress = P.FoodHere;
                ProdStorage.Max = P.Storage.Max;
                ProdStorage.Progress = P.ProdHere;
            }
            PlanetInfo.Draw();
            pDescription.Draw();
            pLabor.Draw();
            pStorage.Draw();
            subColonyGrid.Draw();
            var destinationRectangle1 = new Rectangle(GridPos.X, GridPos.Y + 1, GridPos.Width - 4, GridPos.Height - 3);
            batch.Draw(ResourceManager.Texture("PlanetTiles/" + P.PlanetTileId), destinationRectangle1, Color.White);
            foreach (PlanetGridSquare pgs in P.TilesList)
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
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.QItem.Building.Icon + "_64x64"), destinationRectangle2, new Color(255, 255, 255, 128));
                }
                DrawPGSIcons(pgs);
            }
            foreach (PlanetGridSquare planetGridSquare in P.TilesList)
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
            launchTroops.Visible = P.Owner == Empire.Universe.player && P.TroopsHere.Count > 0;

            //fbedard: Display button
            if (P.Owner == Empire.Universe.player)
            {
                int troopsInvading = eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0)
                    .Where(ai => ai.AI.State != AIState.Resupply)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == P));
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
            else if (P.HasSpacePort && build.Tabs[1].Selected)
            {
                DrawBuildableShipsList(batch);
            }
            else if (!P.HasSpacePort && P.AllowInfantry && build.Tabs[1].Selected)
            {
                DrawBuildTroopsList(batch);
            }
            else if (build.Tabs.Count > 2 && build.Tabs[2].Selected)
            {
                DrawBuildTroopsListDup(batch);
            }

            DrawConstructionQueue(batch);

            buildSL.Draw(batch);
            Selector?.Draw(batch);

            DrawSliders(batch);


            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);
            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            Vector2 vector2_2 = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
            P.Name = PlanetName.Text;
            PlanetName.Draw(Font20, batch, vector2_2, GameTime, new Color(255, 239, 208));
            EditNameButton = new Rectangle((int)(vector2_2.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(vector2_2.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (EditHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), EditNameButton, Color.White);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 768)
                vector2_2.Y += Font20.LineSpacing * 2;
            else
                vector2_2.Y += Font20.LineSpacing;
            batch.DrawString(Font12, Localizer.Token(GameText.Class) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, P.CategoryName, position3, new Color(255, 239, 208));
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(GameText.Population) + ":", vector2_2, Color.Orange);
            var color = new Color(255, 239, 208);
            batch.DrawString(Font12, P.PopulationString, position3, color);
            var rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(GameText.Population) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(GameText.Fertility) + ":", vector2_2, Color.Orange);
            if (P.Fertility.AlmostEqual(P.MaxFertility))
                batch.DrawString(Font12, P.Fertility.String(), position3, color);
            else
            {
                Color fertColor = P.Fertility < P.MaxFertility ? Color.LightGreen : Color.Pink;
                batch.DrawString(Font12, $"{P.Fertility.String()} / {P.MaxFertility.String()}", position3, fertColor);
            }

            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(GameText.Fertility) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(GameText.Richness) + ":", vector2_2, Color.Orange);
            batch.DrawString(Font12, P.MineralRichness.String(), position3, new Color(255, 239, 208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(GameText.Richness) + ":").X, Font12.LineSpacing);


            // The Doctor: For planet income breakdown

            string gIncome = Localizer.Token(GameText.GrossIncome);
            string gUpkeep = Localizer.Token(GameText.Expenditure2);
            string nIncome = Localizer.Token(GameText.NetIncome);
            string nLosses = Localizer.Token(GameText.NetLosses);

            float grossIncome = P.Money.GrossRevenue;
            float grossUpkeep = P.Money.Maintenance;
            float netIncome   = P.Money.NetRevenue;

            Vector2 positionGIncome = vector2_2;
            positionGIncome.X = vector2_2.X + 1;
            positionGIncome.Y = vector2_2.Y + 28;
            Vector2 positionGrossIncome = position3;
            positionGrossIncome.Y = position3.Y + 28;
            positionGrossIncome.X = position3.X + 1;

            batch.DrawString(Fonts.Arial10, gIncome + ":", positionGIncome, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossIncome.String(2) + " BC/Y", positionGrossIncome, Color.LightGray);

            Vector2 positionGUpkeep = positionGIncome;
            positionGUpkeep.Y = positionGIncome.Y + (Fonts.Arial12.LineSpacing);
            Vector2 positionGrossUpkeep = positionGrossIncome;
            positionGrossUpkeep.Y += (Fonts.Arial12.LineSpacing);

            batch.DrawString(Fonts.Arial10, gUpkeep + ":", positionGUpkeep, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossUpkeep.String(2) + " BC/Y", positionGrossUpkeep, Color.LightGray);

            Vector2 positionNIncome = positionGUpkeep;
            positionNIncome.X = positionGUpkeep.X - 1;
            positionNIncome.Y = positionGUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);
            Vector2 positionNetIncome = positionGrossUpkeep;
            positionNetIncome.X = positionGrossUpkeep.X - 1;
            positionNetIncome.Y = positionGrossUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);

            batch.DrawString(Fonts.Arial12, (netIncome > 0.0 ? nIncome : nLosses) + ":", positionNIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);
            batch.DrawString(Font12, netIncome.String(2) + " BC/Y", positionNetIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);

            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);

            var portrait = new Rectangle(pDescription.Menu.X + 10, pDescription.Menu.Y + 30, 124, 148);
            while (portrait.Bottom > pDescription.Menu.Bottom)
            {
                portrait.Height -= (int)(0.1 * portrait.Height);
                portrait.Width  -= (int)(0.1 * portrait.Width);
            }
            batch.Draw(ResourceManager.Texture($"Portraits/{P.Owner.data.PortraitName}"), portrait, Color.White);
            batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), portrait, Color.White);
            batch.DrawRectangle(portrait, Color.Orange);
            if (P.colonyType == Planet.ColonyType.Colony)
                batch.Draw(ResourceManager.Texture("NewUI/x_red"), portrait, Color.White);

            // WorldType
            // [dropdown]
            // ColonTypeInfoText
            var description = new Rectangle(portrait.Right + 15, portrait.Y,
                                            pDescription.Menu.Right - portrait.Right - 20,
                                            pDescription.Menu.Height - 60);

            var descCursor = new Vector2(description.X, description.Y);
            batch.DrawString(Font12, P.WorldType, descCursor, Color.White);
            descCursor.Y += Font12.LineSpacing + 5;

            GovernorDropdown.Pos = descCursor;
            GovernorDropdown.Reset();
            descCursor.Y += GovernorDropdown.Height + 5;

            string colonyTypeInfo = Font12.ParseText(P.ColonyTypeInfoText, description.Width);
            batch.DrawString(Font12, colonyTypeInfo, descCursor, Color.White);
            GovernorDropdown.Draw(batch); // draw dropdown on top of other text

            if (GlobalStats.HardcoreRuleset)
            {
                foreach (ThreeStateButton threeStateButton in ResourceButtons)
                    threeStateButton.Draw(ScreenManager, (int)P.GetGoodAmount(threeStateButton.Good));
            }
            else
            {
                FoodStorage.Progress = P.FoodHere;
                ProdStorage.Progress = P.ProdHere;
                if      (P.FS == Planet.GoodState.STORE)  foodDropDown.ActiveIndex = 0;
                else if (P.FS == Planet.GoodState.IMPORT) foodDropDown.ActiveIndex = 1;
                else if (P.FS == Planet.GoodState.EXPORT) foodDropDown.ActiveIndex = 2;
                if (P.NonCybernetic)
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
                if      (P.PS == Planet.GoodState.STORE)  prodDropDown.ActiveIndex = 0;
                else if (P.PS == Planet.GoodState.IMPORT) prodDropDown.ActiveIndex = 1;
                else if (P.PS == Planet.GoodState.EXPORT) prodDropDown.ActiveIndex = 2;
                prodDropDown.Draw(batch);
                batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), FoodStorageIcon, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), ProfStorageIcon, Color.White);
            }

            base.Draw(batch);

            if (ScreenManager.NumScreens == 2)
                Popup = true;

            close.Draw(batch);

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(74);
        }

        void DrawBuildTroopsListDup(SpriteBatch batch)
        {
            Vector2 vector2_1;
            if (Reset)
            {
                buildSL.Reset();
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (P.Owner.WeCanBuildTroop(troopType))
                        buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), true, false);
                }

                Reset = false;
            }

            SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");
            vector2_1 = new Vector2(build.Menu.X + 20, build.Menu.Y + 45);
            foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
            {
                vector2_1.Y = entry.Y;
                var troop = entry.Get<Troop>();
                if (!entry.Hovered)
                {
                    troop.Draw(batch, new Rectangle((int) vector2_1.X, (int) vector2_1.Y, 29, 30));
                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                    batch.DrawString(Font12, troop.DisplayNameEmpire(P.Owner), position, Color.White);
                    position.Y += Font12.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);
                    position.X = entry.Right - 100;
                    Rectangle destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Font12.LineSpacing / 2);
                    batch.DrawString(Font12, ((int) troop.ActualCost).ToString(), position, Color.White);
                    entry.DrawPlusEdit(batch);
                }
                else
                {
                    vector2_1.Y = entry.Y;
                    troop.Draw(batch, new Rectangle((int) vector2_1.X, (int) vector2_1.Y, 29, 30));
                    Vector2 position = new Vector2(vector2_1.X + 40f, vector2_1.Y + 3f);
                    batch.DrawString(Font12, troop.DisplayNameEmpire(P.Owner), position, Color.White);
                    position.Y += Font12.LineSpacing;
                    batch.DrawString(Font8, troop.Class, position, Color.Orange);
                    position.X = entry.Right - 100;
                    Rectangle destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Font12.LineSpacing / 2);
                    batch.DrawString(Font12, ((int) troop.ActualCost).ToString(), position, Color.White);
                    entry.DrawPlusEdit(batch);
                }
            }
        }

        private void DrawBuildableShipsList(SpriteBatch batch)
        {
            var added = new HashSet<string>();
            if (ShipsCanBuildLast != P.Owner.ShipsWeCanBuild.Count || Reset)
            {
                buildSL.Reset();

                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.ColoniserMenu)
                {
                    added.Add("Coloniser");
                    buildSL.AddItem(new ModuleHeader("Coloniser"));
                }

                foreach (string shipToBuild in P.Owner.ShipsWeCanBuild)
                {
                    var ship = ResourceManager.GetShipTemplate(shipToBuild);
                    var role = ResourceManager.ShipRoles[ship.shipData.Role];
                    var header = Localizer.GetRole(ship.DesignRole, P.Owner);
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
                            Localizer.GetRole(ship.DesignRole, P.Owner) == header)
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
                    batch.DrawString(Font12,
                        ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform
                            ? ship.Name + " " + Localizer.Token(GameText.OrbitsPlanet)
                            : ship.Name, position, Color.White);
                    position.Y += Font12.LineSpacing;

                    var role = ship.BaseHull.Name;
                    batch.DrawString(Font8, role, position, Color.Orange);
                    position.X = position.X + Font8.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    batch.DrawString(Font8,
                        $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", position, Color.Orange);


                    //Forgive my hacks this code of nightmare must GO!
                    position.X = (entry.Right - 120);
                    var iconProd = ResourceManager.Texture("NewUI/icon_production");
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                    position = new Vector2((destinationRectangle2.X - 60),
                        (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                    // Use correct upkeep method depending on mod settings
                    string upkeep;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = ship.GetMaintCostRealism(P.Owner).ToString("F2");
                    }
                    else
                    {
                        upkeep = ship.GetMaintCost(P.Owner).ToString("F2");
                    }

                    batch.DrawString(Font8, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2(destinationRectangle2.X + 26,
                        destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                        Font12.LineSpacing / 2);
                    batch.DrawString(Font12, ((int) (ship.GetCost(P.Owner) * P.ShipBuildingModifier)).ToString(),
                        position, Color.White);
                }
                else
                {
                    var ship = entry.Get<Ship>();

                    topLeft.Y = entry.Y;
                    batch.Draw(ship.BaseHull.Icon, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30), Color.White);
                    Vector2 position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Font12,
                        ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform
                            ? ship.Name + " " + Localizer.Token(GameText.OrbitsPlanet)
                            : ship.Name, position, Color.White);
                    position.Y += Font12.LineSpacing;

                    //var role = Localizer.GetRole(ship.shipData.HullRole, EmpireManager.Player);
                    var role = ship.BaseHull.Name;
                    batch.DrawString(Font8, role, position, Color.Orange);
                    position.X = position.X + Font8.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    batch.DrawString(Font8,
                        $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", position, Color.Orange);

                    position.X = (entry.Right - 120);
                    SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                    position = new Vector2((destinationRectangle2.X - 60),
                        (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                    // Use correct upkeep method depending on mod settings
                    string upkeep;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        upkeep = entry.Get<Ship>().GetMaintCostRealism(P.Owner).ToString("F2");
                    }
                    else
                    {
                        upkeep = entry.Get<Ship>().GetMaintCost(P.Owner).ToString("F2");
                    }

                    batch.DrawString(Font8, string.Concat(upkeep, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                    batch.DrawString(Font12,
                        ((int) (entry.Get<Ship>().GetCost(P.Owner) * P.ShipBuildingModifier)).ToString(), position,
                        Color.White);
                    entry.DrawPlusEdit(batch);
                }
            }

            PlayerDesignsToggle.Draw(ScreenManager);
        }

        private void DrawBuildTroopsList(SpriteBatch batch)
        {
            if (Reset)
            {
                buildSL.Reset();
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (P.Owner.WeCanBuildTroop(troopType))
                        buildSL.AddItem(ResourceManager.GetTroopTemplate(troopType), true, false);
                }

                Reset = false;
            }

            SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");
            var topLeft = new Vector2((build.Menu.X + 20), (build.Menu.Y + 45));
            foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
            {
                topLeft.Y = entry.Y;
                var troop = entry.Get<Troop>();
                if (!entry.Hovered)
                {
                    troop.Draw(batch, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30));
                    var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Font12, troop.DisplayNameEmpire(P.Owner), position, Color.White);
                    position.Y += Font12.LineSpacing;
                    batch.DrawString(Font8, troop.Class, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                    batch.DrawString(Font12, ((int) troop.ActualCost).ToString(), position, Color.White);

                    entry.DrawPlusEdit(batch);
                }
                else
                {
                    topLeft.Y = entry.Y;
                    troop.Draw(batch, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30));
                    var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                    batch.DrawString(Font12, troop.DisplayNameEmpire(P.Owner), position, Color.White);
                    position.Y += Font12.LineSpacing;
                    batch.DrawString(Font8, troop.Class, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var destinationRectangle2 = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, destinationRectangle2, Color.White);
                    position = new Vector2((destinationRectangle2.X + 26),
                        (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                    batch.DrawString(Font12, ((int) troop.ActualCost).ToString(), position, Color.White);

                    entry.DrawPlusEdit(batch);
                }
            }
        }

        private void DrawConstructionQueue(SpriteBatch batch)
        {
            QSL.SetItems(P.ConstructionQueue);
            QSL.DrawDraggedEntry(batch);

            foreach (ScrollList.Entry entry in QSL.VisibleExpandedEntries)
            {
                entry.CheckHoverNoSound(Input.CursorPosition);

                var qi = entry.Get<QueueItem>();
                var position = new Vector2(entry.X + 40f, entry.Y);
                DrawText(ref position, qi.DisplayText);
                var r = new Rectangle((int)position.X, (int)position.Y, 150, 18);

                if (qi.isBuilding)
                {
                    SubTexture icon = ResourceManager.Texture($"Buildings/icon_{qi.Building.Icon}_48x48");
                    batch.Draw(icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
                    new ProgressBar(r, qi.Cost, qi.productionTowards).Draw(batch);
                }
                else if (qi.isShip)
                {
                    batch.Draw(qi.sData.Icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
                    new ProgressBar(r, qi.Cost * P.ShipBuildingModifier, qi.productionTowards).Draw(batch);
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
            BuildingsCanBuild = P.GetBuildingsCanBuild();
            if (Reset || buildSL.NumEntries != BuildingsCanBuild.Count)
            {
                buildSL.SetItems(BuildingsCanBuild);
                Reset = false;
            }

            foreach (ScrollList.Entry entry in buildSL.VisibleExpandedEntries)
            {
                if (!entry.TryGet(out Building b))
                    continue;

                SubTexture icon = ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48");
                SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

                bool unprofitable = !P.WeCanAffordThis(b, P.colonyType) && b.Maintenance > 0f;
                Color buildColor = unprofitable ? Color.IndianRed : Color.White;
                if (entry.Hovered) buildColor = Color.White; // hover color

                string descr = Localizer.Token(b.ShortDescriptionIndex) + (unprofitable ? " (unprofitable)" : "");
                descr = Font8.ParseText(descr, 280f);

                var position = new Vector2(build.Menu.X + 60f, entry.Y - 4f);

                batch.Draw(icon, new Rectangle(entry.X, entry.Y, 29, 30), buildColor);
                DrawText(ref position, b.NameTranslationIndex, buildColor);

                if (!entry.Hovered)
                {
                    batch.DrawString(Font8, descr, position, unprofitable ? Color.Chocolate : Color.Green);
                    position.X = (entry.Right - 100);
                    var r = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, r, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                    position = new Vector2( (r.X - 60),
                         (1 + r.Y + r.Height / 2 -
                                 Font12.LineSpacing / 2));
                    string maintenance = b.Maintenance.ToString("F2");
                    batch.DrawString(Font8, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                    // ~~~~

                    position = new Vector2((r.X + 26),
                        (r.Y + r.Height / 2 -
                                 Font12.LineSpacing / 2));
                    batch.DrawString(Font12, b.ActualCost.String(), position, Color.White);
                    entry.DrawPlus(batch);
                }
                else
                {
                    batch.DrawString(Font8, descr, position, Color.Orange);
                    position.X = (entry.Right - 100);
                    var r = new Rectangle((int) position.X, entry.CenterY - iconProd.Height / 2 - 5,
                        iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, r, Color.White);

                    // The Doctor - adds new UI information in the build menus for the per tick upkeep of building

                    position = new Vector2((r.X - 60),
                                           (1 + r.Y + r.Height / 2 - Font12.LineSpacing / 2));
                    float actualMaint = b.Maintenance + b.Maintenance * P.Owner.data.Traits.MaintMod;
                    string maintenance = actualMaint.ToString("F2");
                    batch.DrawString(Font8, string.Concat(maintenance, " BC/Y"), position, Color.Salmon);

                    // ~~~

                    position = new Vector2((r.X + 26),
                        (r.Y + r.Height / 2 -
                                 Font12.LineSpacing / 2));
                    batch.DrawString(Font12, b.ActualCost.String(), position, Color.White);
                    entry.DrawPlus(batch);
                }

                entry.CheckHover(Input.CursorPosition);
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
            ScreenManager.SpriteBatch.DrawString(Font12, text, cursor, color);
            cursor.Y += Font12.LineSpacing;
        }

        private void DrawTitledLine(ref Vector2 cursor, int titleId, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Font12, Localizer.Token(titleId) +": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Font12, text, textCursor, TextColor);
            cursor.Y += Font12.LineSpacing;
        }

        private void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        private string MultiLineFormat(string text)
        {
            return Font12.ParseText(text, pFacilities.Menu.Width - 40);
        }

        private string MultiLineFormat(int token)
        {
            return MultiLineFormat(Localizer.Token(token));
        }

        private void DrawMultiLine(ref Vector2 cursor, string text, Color color)
        {
            string multiline = MultiLineFormat(text);
            ScreenManager.SpriteBatch.DrawString(Font12, multiline, cursor, color);
            cursor.Y += (Font12.MeasureString(multiline).Y + Font12.LineSpacing);
        }

        private void DrawCommoditiesArea(Vector2 bCursor)
        {
            string text = MultiLineFormat(4097);
            ScreenManager.SpriteBatch.DrawString(Font12, text, bCursor, TextColor);
        }

        private void DrawDetailInfo(Vector2 bCursor)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            if (pFacilities.Tabs.Count > 1 && pFacilities.Tabs[1].Selected)
            {
                DrawCommoditiesArea(bCursor);
                return;
            }
            Color color = Color.Wheat;
            switch (DetailInfo)
            {
                case Troop t:
                    spriteBatch.DrawString(Font20, t.DisplayNameEmpire(P.Owner), bCursor, TextColor);
                    bCursor.Y += Font20.LineSpacing + 2;
                    string strength = t.Strength < t.ActualStrengthMax ? t.Strength + "/" + t.ActualStrengthMax
                        : t.ActualStrengthMax.String(1);

                    DrawMultiLine(ref bCursor, t.Description);
                    DrawTitledLine(ref bCursor, 338, t.TargetType);
                    DrawTitledLine(ref bCursor, 339, strength);
                    DrawTitledLine(ref bCursor, 2218, t.NetHardAttack.ToString());
                    DrawTitledLine(ref bCursor, 2219, t.NetSoftAttack.ToString());
                    DrawTitledLine(ref bCursor, 6008, t.BoardingStrength.ToString());
                    DrawTitledLine(ref bCursor, 6023, t.Level.ToString());
                    break;

                case string _:
                    DrawMultiLine(ref bCursor, P.Description);
                    string desc = "";
                    if (P.IsCybernetic)  desc = Localizer.Token(GameText.TheOccupantsOfThisPlanet);
                    else switch (P.FS)
                    {
                        case Planet.GoodState.EXPORT:
                            desc = Localizer.Token(GameText.ThisColonyIsSetTo);
                            break;
                        case Planet.GoodState.IMPORT:
                            desc = Localizer.Token(GameText.ThisColonyIsSetTo2);
                            break;
                        case Planet.GoodState.STORE:
                            desc = Localizer.Token(GameText.ThisPlanetIsNeitherImporting);
                            break;
                    }

                    DrawMultiLine(ref bCursor, desc);
                    desc = "";
                    switch (P.PS)
                    {
                        case Planet.GoodState.EXPORT:
                            desc = Localizer.Token(GameText.ThisPlanetIsManuallyExporting);
                            break;
                        case Planet.GoodState.IMPORT:
                            desc = Localizer.Token(GameText.ThisPlanetIsManuallyImporting);
                            break;
                        case Planet.GoodState.STORE:
                            desc = Localizer.Token(GameText.ThisPlanetIsManuallyStoring);
                            break;
                    }
                    DrawMultiLine(ref bCursor, desc);
                    if (P.IsStarving)
                        DrawMultiLine(ref bCursor, Localizer.Token(GameText.ThisPlanetsPopulationIsShrinking), Color.LightPink);
                    DrawPlanetStat(ref bCursor, spriteBatch);
                    break;

                case PlanetGridSquare pgs:
                    switch (pgs.building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            spriteBatch.DrawString(Font20, Localizer.Token(GameText.HabitableBiosphere), bCursor, color);
                            bCursor.Y +=Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                        case null when pgs.Habitable:
                            spriteBatch.DrawString(Font20, Localizer.Token(GameText.HabitableLand), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                    }

                    if (!pgs.Habitable && pgs.building == null)
                    {
                        if (P.IsBarrenType)
                        {
                            spriteBatch.DrawString(Font20, Localizer.Token(GameText.UninhabitableLand), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(352), bCursor, color);
                            return;
                        }
                        spriteBatch.DrawString(Font20, Localizer.Token(GameText.UninhabitableLand), bCursor, color);
                        bCursor.Y += Font20.LineSpacing + 5;
                        spriteBatch.DrawString(Font12, MultiLineFormat(353), bCursor, color);
                        return;
                    }

                    if (pgs.building == null)
                        return;

                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    spriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    spriteBatch.DrawString(Font20, Localizer.Token(pgs.building.NameTranslationIndex), bCursor, color);
                    bCursor.Y   += Font20.LineSpacing + 5;
                    string buildingDescription  = MultiLineFormat(pgs.building.DescriptionIndex);
                    spriteBatch.DrawString(Font12, buildingDescription, bCursor, color);
                    bCursor.Y   += Font12.MeasureString(buildingDescription).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, spriteBatch, pgs.building);
                    if (!pgs.building.Scrappable)
                        return;

                    bCursor.Y = bCursor.Y + (Font12.LineSpacing + 10);
                    spriteBatch.DrawString(Font12, "You may scrap this building by right clicking it", bCursor, Color.White);
                    break;

                case ScrollList.Entry entry:
                    var selectedBuilding = entry.Get<Building>();
                    spriteBatch.DrawString(Font20, selectedBuilding.Name, bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionIndex);
                    spriteBatch.DrawString(Font12, selectionText, bCursor, color);
                    bCursor.Y += Font12.MeasureString(selectionText).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, spriteBatch, selectedBuilding);
                    break;
            }
        }

        private void DrawPlanetStat(ref Vector2 cursor, SpriteBatch spriteBatch)
        {
            DrawBuildingInfo(ref cursor, spriteBatch, P.Food.NetYieldPerColonist,
                ResourceManager.Texture("NewUI/icon_food"), "food per colonist allocated to Food Production after taxes");
            DrawBuildingInfo(ref cursor, spriteBatch, P.Food.NetFlatBonus,
                ResourceManager.Texture("NewUI/icon_food"), "flat food added generated per turn after taxes");
            DrawBuildingInfo(ref cursor, spriteBatch, P.Prod.NetYieldPerColonist,
                ResourceManager.Texture("NewUI/icon_production"), "production per colonist allocated to Industry after taxes");
            DrawBuildingInfo(ref cursor, spriteBatch, P.Prod.NetFlatBonus,
                ResourceManager.Texture("NewUI/icon_production"), "flat production added generated per turn after taxes");
            DrawBuildingInfo(ref cursor, spriteBatch, P.Res.NetYieldPerColonist,
                ResourceManager.Texture("NewUI/icon_science"), "research per colonist allocated to Science before taxes");
            DrawBuildingInfo(ref cursor, spriteBatch, P.Res.NetFlatBonus,
                ResourceManager.Texture("NewUI/icon_science"), "flat research added generated per turn after taxes");
        }

        private void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch spriteBatch, Building building)
        {
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusFlatFoodAmount,
                ResourceManager.Texture("NewUI/icon_food"), Localizer.Token(GameText.FoodPerTurn));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusFoodPerColonist,
                ResourceManager.Texture("NewUI/icon_food"), Localizer.Token(GameText.FoodPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.SensorRange,
                ResourceManager.Texture("NewUI/icon_sensors"), Localizer.Token(GameText.SensorRange), signs: false);
            DrawBuildingInfo(ref bCursor, spriteBatch, building.ProjectorRange,
                ResourceManager.Texture("NewUI/icon_projection"), Localizer.Token(GameText.SubspaceProjectionArea), signs: false);
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusFlatProductionAmount,
                ResourceManager.Texture("NewUI/icon_production"), Localizer.Token(GameText.ProductionPerTurn));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusProdPerColonist,
                ResourceManager.Texture("NewUI/icon_production"), Localizer.Token(GameText.ProductionPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusFlatPopulation / 1000,
                ResourceManager.Texture("NewUI/icon_population"), Localizer.Token(GameText.ColonistsPerTurn));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusFlatResearchAmount,
                ResourceManager.Texture("NewUI/icon_science"), Localizer.Token(GameText.ResearchPerTurn));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusResearchPerColonist,
                ResourceManager.Texture("NewUI/icon_science"), Localizer.Token(GameText.ResearchPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusTaxPercentage * 100,
                ResourceManager.Texture("NewUI/icon_money"), Localizer.Token(GameText.IncreaseToTaxIncomes), percent: true);
            DrawBuildingInfo(ref bCursor, spriteBatch, -building.MinusFertilityOnBuild,
                ResourceManager.Texture("NewUI/icon_food"), Localizer.Token(GameText.MaxFertilityChangeOnBuild));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlanetaryShieldStrengthAdded,
                ResourceManager.Texture("NewUI/icon_planetshield"), Localizer.Token(GameText.PlanetaryShieldStrengthAdded));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.CreditsPerColonist,
                ResourceManager.Texture("NewUI/icon_money"), Localizer.Token(GameText.CreditsAddedPerColonist));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.PlusProdPerRichness,
                ResourceManager.Texture("NewUI/icon_production"), Localizer.Token(GameText.ProductionPerRichness));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.ShipRepair * 10 * P.Level,
                ResourceManager.Texture("NewUI/icon_queue_rushconstruction"), Localizer.Token(GameText.ShipRepair));
            DrawBuildingInfo(ref bCursor, spriteBatch, building.CombatStrength,
                ResourceManager.Texture("Ground_UI/Ground_Attack"), Localizer.Token(GameText.CombatStrength));
            float maintenance = -(building.Maintenance + building.Maintenance * P.Owner.data.Traits.MaintMod);
            DrawBuildingInfo(ref bCursor, spriteBatch, maintenance,
                ResourceManager.Texture("NewUI/icon_money"), Localizer.Token(GameText.CreditsPerTurnInMaintenance));
            if (building.TheWeapon == null)
                return;

            DrawBuildingInfo(ref bCursor, spriteBatch, building.TheWeapon.Range,
                ResourceManager.Texture("UI/icon_offense"), "Range", signs: false);
            DrawBuildingInfo(ref bCursor, spriteBatch, building.TheWeapon.DamageAmount,
                ResourceManager.Texture("UI/icon_offense"), "Damage", signs: false);
            DrawBuildingInfo(ref bCursor, spriteBatch, building.TheWeapon.DamageAmount,
                ResourceManager.Texture("UI/icon_offense"), "EMP Damage", signs: false);
            DrawBuildingInfo(ref bCursor, spriteBatch, building.TheWeapon.NetFireDelay,
                ResourceManager.Texture("UI/icon_offense"), "Fire Delay", signs: false);
        }

        private void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch spriteBatch, float value, SubTexture texture, 
                                      string toolTip, bool percent = false, bool signs = true)
        {
            if (value.AlmostEqual(0))
                return;

            SpriteFont font    = Font12;
            Rectangle fIcon    = new Rectangle((int)cursor.X, (int)cursor.Y, texture.Width, texture.Height);
            Vector2 tCursor    = new Vector2(cursor.X + fIcon.Width + 5f, cursor.Y + 3f);
            string plusOrMinus = "";
            Color color = Color.White;
            if (signs)
            {
                plusOrMinus = value < 0 ? "- " : "+";
                color = value < 0 ? Color.Pink : Color.LightGreen;
            }
            spriteBatch.Draw(texture, fIcon, Color.White);
            SpriteBatch spriteBatch2 = spriteBatch;
            string percentage = percent ? "% " : " ";
            var valueobj             = new object[] { plusOrMinus, Math.Round(Math.Abs(value), 2), percentage, toolTip };
            spriteBatch2.DrawString(font, string.Concat(valueobj), tCursor, color);
            cursor.Y += font.LineSpacing + 10;
        }

        private void DrawTroopLevel(Troop troop, Rectangle rect)
        {
            SpriteFont font = Font12;
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
                    numFood = numFood + pgs.building.PlusFoodPerColonist * P.PopulationBillion * P.Food.Percent;
                    numFood = numFood + pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd = numProd + pgs.building.PlusFlatProductionAmount;
                    numProd = numProd + pgs.building.PlusProdPerColonist * P.PopulationBillion * P.Prod.Percent;
                }
                if (pgs.building.PlusProdPerRichness > 0f)
                {
                    numProd = numProd + pgs.building.PlusProdPerRichness * P.MineralRichness;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes = numRes + pgs.building.PlusResearchPerColonist * P.PopulationBillion * P.Res.Percent;
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
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numFood - i, SpriteEffects.None, 1f);
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
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numProd - i, SpriteEffects.None, 1f);
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
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numRes - i, SpriteEffects.None, 1f);
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
            DetailInfo = null;
            foreach (ScrollList.Entry e in buildSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(input))
                {
                    if (e.Is<Building>())   DetailInfo = e; // @todo Why are we storing Entry here???
                    else if (e.Is<Troop>()) DetailInfo = e.item;
                }
            }
            if (DetailInfo == null)
                DetailInfo = P.Description;
        }

        public override bool HandleInput(InputState input)
        {
            pFacilities.HandleInputNoReset();

            if (HandleCycleColoniesLeftRight(input))
                return true;

            P.UpdateIncomes(false);
            HandleDetailInfo(input);
            buildSL.HandleInput(input);
            build.HandleInput(this);

            // AI specific
            if (P.Owner != EmpireManager.Player)
            {
                HandleDetailInfo(input);
                return true;
            }

            HandlePlanetNameChangeTextBox(input);

            GovernorDropdown.HandleInput(input);
            P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;

            HandleSliders(input);

            if (P.HasSpacePort && build.Tabs.Count > 1 && build.Tabs[1].Selected)
            {
                if (PlayerDesignsToggle.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(GameText.ToggleToDisplayOnlyPlayerdesigned));
                }
                if (PlayerDesignsToggle.HandleInput(input) && !input.LeftMouseReleased)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
                    PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
                    Reset = true;
                }
            }

            Selector = null;
            if (HandleTroopSelect(input))
                return true;

            HandleExportImportButtons(input);
            HandleConstructionQueueInput(input);
            HandleDragBuildingOntoTile(input);
            HandleBuildListClicks(input);

            ShipsCanBuildLast = P.Owner.ShipsWeCanBuild.Count;

            if (Popup)
            {
                if (!input.RightMouseHeldUp)
                    return true;
                else
                    Popup = false;
            }
            return base.HandleInput(input);
        }

        bool HandleTroopSelect(InputState input)
        {
            ClickedTroop = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
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
                    continue;

                DetailInfo = pgs.TroopsHere[0];
                if (input.RightMouseClick && pgs.TroopsHere[0].GetOwner() == EmpireManager.Player)
                {
                    GameAudio.PlaySfxAsync("sd_troop_takeoff");
                    Ship.CreateTroopShipAtPoint(P.Owner.data.DefaultTroopShip, P.Owner, P.Center, pgs.TroopsHere[0]);
                    P.TroopsHere.Remove(pgs.TroopsHere[0]);
                    pgs.TroopsHere[0].SetPlanet(null);
                    pgs.TroopsHere.Clear();
                    ClickedTroop = true;
                    DetailInfo = null;
                }

                return true;
            }

            if (!ClickedTroop)
            {
                foreach (PlanetGridSquare pgs in P.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        DetailInfo = pgs;
                        var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                            pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                        if (pgs.building != null && bRect.HitTest(input.CursorPosition) && Input.RightMouseClick)
                        {
                            if (pgs.building.Scrappable)
                            {
                                ToScrap = pgs.building;
                                string message = string.Concat("Do you wish to scrap ",
                                    Localizer.Token(pgs.building.NameTranslationIndex),
                                    "? Half of the building's construction cost will be recovered to your storage.");
                                var messageBox = new MessageBoxScreen(Empire.Universe, message);
                                messageBox.Accepted += ScrapAccepted;
                                ScreenManager.AddScreen(messageBox);
                            }

                            ClickedTroop = true;
                            return true;
                        }
                    }

                    if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(input.CursorPosition))
                        continue;

                    DetailInfo = pgs.TroopsHere;
                }
            }

            return false;
        }

        bool HandleCycleColoniesLeftRight(InputState input)
        {
            if      (RightColony.Rect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(Localizer.Token(GameText.ViewNextColony));
            else if (LeftColony.Rect.HitTest(input.CursorPosition))  ToolTip.CreateTooltip(Localizer.Token(GameText.ViewPreviousColony));

            bool canView = (Empire.Universe.Debug || P.Owner == EmpireManager.Player);
            if (!canView)
                return false;
           
            int change = 0;
            if (input.Right || RightColony.HandleInput(input) && input.LeftMouseClick)
                change = +1;
            else if (input.Left || LeftColony.HandleInput(input) && input.LeftMouseClick)
                change = -1;

            if (change != 0)
            {
                var planets = P.Owner.GetPlanets();
                int newIndex = planets.IndexOf(P) + change;
                if (newIndex >= planets.Count) newIndex = 0;
                else if (newIndex < 0)         newIndex = planets.Count-1;

                Planet nextOrPrevPlanet = planets[newIndex];
                if (nextOrPrevPlanet != P)
                {
                    Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, nextOrPrevPlanet, eui);
                }
                return true; // planet changed, ColonyScreen will be replaced
            }
            return false;
        }

        void HandlePlanetNameChangeTextBox(InputState input)
        {
            if (!EditNameButton.HitTest(input.CursorPosition))
            {
                EditHoverState = 0;
            }
            else
            {
                EditHoverState = 1;
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
                    foreach (SolarSystem.Ring ring in P.ParentSystem.RingList)
                    {
                        if (ring.planet == P)
                        {
                            PlanetName.Text = string.Concat(P.ParentSystem.Name, " ",
                                RomanNumerals.ToRoman(ringnum));
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
        }

        void HandleExportImportButtons(InputState input)
        {
            if (!GlobalStats.HardcoreRuleset)
            {
                if (foodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    foodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    P.FS = (Planet.GoodState) ((int) P.FS + (int) Planet.GoodState.IMPORT);
                    if (P.FS > Planet.GoodState.EXPORT)
                        P.FS = Planet.GoodState.STORE;
                }

                if (prodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    prodDropDown.Toggle();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    P.PS = (Planet.GoodState) ((int) P.PS + (int) Planet.GoodState.IMPORT);
                    if (P.PS > Planet.GoodState.EXPORT)
                        P.PS = Planet.GoodState.STORE;
                }
            }
            else
            {
                foreach (ThreeStateButton b in ResourceButtons)
                    b.HandleInput(input, ScreenManager);
            }
        }

        void HandleBuildListClicks(InputState input)
        {
            foreach (ScrollList.Entry e in buildSL.VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader header)
                {
                    if (header.HandleInput(input, e))
                        break;
                }
                else if (e.CheckHover(input))
                {
                    Selector = e.CreateSelector();

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
                                var qi = new QueueItem(P);
                                if (e.TryGet(out Ship ship))
                                {
                                    qi.isShip = true;
                                    qi.sData = ship.shipData;
                                    qi.Cost = ship.GetCost(P.Owner);
                                    qi.productionTowards = 0f;
                                    P.ConstructionQueue.Add(qi);
                                    Reset = true;
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.TryGet(out Troop troop))
                                {
                                    qi.isTroop = true;
                                    qi.troopType = troop.Name;
                                    qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                                    qi.productionTowards = 0f;
                                    P.ConstructionQueue.Add(qi);
                                    Reset = true;
                                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                                }
                                else if (e.TryGet(out Building building))
                                {
                                    P.AddBuildingToCQ(building, true);
                                    Reset = true;
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
                        var qi = new QueueItem(P);
                        if (e.item is Building building)
                        {
                            P.AddBuildingToCQ(building, true);
                        }
                        else if (e.item is Ship ship)
                        {
                            qi.isShip = true;
                            qi.sData = ship.shipData;
                            qi.Cost = ship.GetCost(P.Owner);
                            qi.productionTowards = 0f;
                            P.ConstructionQueue.Add(qi);
                        }
                        else if (e.item is Troop troop)
                        {
                            qi.isTroop = true;
                            qi.troopType = troop.Name;
                            qi.Cost = ResourceManager.GetTroopCost(troop.Name);
                            qi.productionTowards = 0f;
                            P.ConstructionQueue.Add(qi);
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
        }

        void HandleDragBuildingOntoTile(InputState input)
        {
            if (!(ActiveBuildingEntry?.item is Building building))
                return;

            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.ClickRect.HitTest(MousePos) || !input.LeftMouseReleased)
                    continue;

                if (pgs.Habitable && pgs.building == null && pgs.QItem == null && !building.IsBiospheres)
                {
                    AddBuildingToConstructionQueue(building, pgs, playerAdded: true);
                    ActiveBuildingEntry = null;
                    break;
                }

                if (pgs.Habitable || pgs.Biosphere || pgs.QItem != null || !building.CanBuildAnywhere)
                {
                    GameAudio.PlaySfxAsync("UI_Misc20");
                    ActiveBuildingEntry = null;
                    break;
                }

                AddBuildingToConstructionQueue(building, pgs, playerAdded: true);
                ActiveBuildingEntry = null;
                break;
            }

            if (ActiveBuildingEntry != null)
            {
                foreach (QueueItem qi in P.ConstructionQueue)
                {
                    if (qi.isBuilding && qi.Building.Name == building.Name && building.Unique)
                    {
                        ActiveBuildingEntry = null;
                        break;
                    }
                }
            }

            if (input.RightMouseClick)
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

        private void OnSendTroopsClicked(UIButton b)
        {
            Array<Ship> troopShips;
            using (eui.empire.GetShips().AcquireReadLock())
                troopShips = new Array<Ship>(eui.empire.GetShips()
                    .Where(troop => troop.TroopList.Count > 0
                                    && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                                    && troop.fleet == null && !troop.InCombat)
                    .OrderBy(distance => Vector2.Distance(distance.Center, P.Center)));

            Array<Planet> planetTroops = new Array<Planet>(eui.empire.GetPlanets()
                .Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, P.Center))
                .Where(Name => Name.Name != P.Name));

            if (troopShips.Count > 0)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                troopShips.First().AI.OrderRebase(P, true);
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
                        troop.AI.OrderRebase(P, true);
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
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != EmpireManager.Player)
                    continue;

                play = true;
                Ship.CreateTroopShipAtPoint(P.Owner.data.DefaultTroopShip, P.Owner, P.Center, pgs.TroopsHere[0]);
                P.TroopsHere.Remove(pgs.TroopsHere[0]);
                pgs.TroopsHere[0].SetPlanet(null);
                pgs.TroopsHere.Clear();
                ClickedTroop = true;
                DetailInfo = null;
            }

            if (play)
            {
                GameAudio.PlaySfxAsync("sd_troop_takeoff");
            }
        }

        void AddBuildingToConstructionQueue(Building building, PlanetGridSquare where, bool playerAdded = true)
        {
            var qi = new QueueItem(P)
            {
                isBuilding = true,
                Building = building,
                IsPlayerAdded = playerAdded,
                Cost = building.ActualCost,
                productionTowards = 0f,
                pgs = @where
            };
            where.QItem = qi;
            P.ConstructionQueue.Add(qi);
            Reset = true;
        }

        private void HandleConstructionQueueInput(InputState input)
        {
            int i = QSL.FirstVisibleIndex;
            foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(Input.CursorPosition))
                {
                    Selector = e.CreateSelector();
                }

                if (e.WasUpHovered(input))
                {
                    ToolTip.CreateTooltip(63);
                    if (!input.IsCtrlKeyDown || input.LeftMouseDown || input.LeftMouseReleased)
                    {
                        if (input.LeftMouseClick && i > 0)
                        {
                            QueueItem item = P.ConstructionQueue[i - 1];
                            P.ConstructionQueue[i - 1] = P.ConstructionQueue[i];
                            P.ConstructionQueue[i] = item;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else if (i > 0)
                    {
                        QueueItem item = P.ConstructionQueue[i];
                        P.ConstructionQueue.Remove(item);
                        P.ConstructionQueue.Insert(0, item);
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
                            QueueItem item = P.ConstructionQueue[i + 1];
                            P.ConstructionQueue[i + 1] = P.ConstructionQueue[i];
                            P.ConstructionQueue[i] = item;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                    else if (i + 1 < QSL.NumExpandedEntries)
                    {
                        QueueItem item = P.ConstructionQueue[i];
                        P.ConstructionQueue.Remove(item);
                        P.ConstructionQueue.Insert(0, item);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        break;
                    }
                }

                if (e.WasApplyHovered(input) && !P.RecentCombat && P.CrippledTurns <= 0)
                {
                    if (!input.IsCtrlKeyDown || input.LeftMouseDown || input.LeftMouseReleased) // @todo WTF??
                    {
                        if (input.LeftMouseClick)
                        {
                            GameAudio.PlaySfxAsync(P.ApplyStoredProduction(i) ? "sd_ui_accept_alt3" : "UI_Misc20");
                        }
                    }
                    else if (P.ProdHere == 0f)
                    {
                        GameAudio.PlaySfxAsync("UI_Misc20");
                    }
                    else
                    {
                        P.ApplyAllStoredProduction(i);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                }

                if (e.WasCancelHovered(input) && input.LeftMouseClick)
                {
                    var item = e.Get<QueueItem>();
                    P.ProdHere += item.productionTowards;

                    if (item.pgs != null)
                    {
                        item.pgs.QItem = null;
                    }

                    if (item.Goal != null)
                    {
                        if (item.Goal is BuildConstructionShip)
                        {
                            P.Owner.GetEmpireAI().Goals.Remove(item.Goal);
                        }

                        if (item.Goal.GetFleet() != null)
                            P.Owner.GetEmpireAI().Goals.Remove(item.Goal);
                    }

                    P.ConstructionQueue.Remove(item);
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
                ++i;
            }

            QSL.HandleInput(input, P);
        }

        public void ResetLists()
        {
            Reset = true;
        }

        private void ScrapAccepted(object sender, EventArgs e)
        {
            ToScrap?.ScrapBuilding(P);
            Update(0f);
        }

        public override void Update(float elapsedTime)
        {
            P.UpdateIncomes(false);
            if (!P.CanBuildInfantry)
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(GameText.Troops))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(GameText.Buildings));
                    if (P.HasSpacePort)
                    {
                        build.AddTab(Localizer.Token(GameText.Ships));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(GameText.Troops))
                        continue;
                    add = false;
                    foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
                        if (entry.TryGet(out Troop troop))
                            troop.Update(elapsedTime);
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(GameText.Buildings));
                    if (P.HasSpacePort)
                    {
                        build.AddTab(Localizer.Token(GameText.Ships));
                    }
                    build.AddTab(Localizer.Token(GameText.Troops));
                }
            }
            if (!P.HasSpacePort)
            {
                bool remove = false;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(GameText.Ships))
                    {
                        continue;
                    }
                    remove = true;
                }
                if (remove)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(GameText.Buildings));
                    if (P.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(GameText.Troops));
                    }
                }
            }
            else
            {
                bool add = true;
                foreach (Submenu.Tab tab in build.Tabs)
                {
                    if (tab.Title != Localizer.Token(GameText.Ships))
                    {
                        continue;
                    }
                    add = false;
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(GameText.Buildings));
                    build.AddTab(Localizer.Token(GameText.Ships));
                    if (P.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(GameText.Troops));
                    }
                }
            }
        }

        void HandleSliders(InputState input)
        {
            Sliders.HandleInput(input);
            P.UpdateIncomes(loadUniverse:false);
        }

        void CreateSliders(Rectangle laborPanel)
        {
            int sliderW = ((int)(laborPanel.Width * 0.6)).RoundUpToMultipleOf(10);
            int sliderX = laborPanel.X + 60;
            int sliderY = laborPanel.Y + 25;
            int slidersAreaH = laborPanel.Height - 25;
            int spacingY = (int)(0.25 * slidersAreaH);
            Sliders = new ColonySliderGroup(this, laborPanel);
            Sliders.Create(sliderX, sliderY, sliderW, spacingY);
            Sliders.SetPlanet(P);
        }
            
        void DrawSliders(SpriteBatch batch)
        {
            Sliders.Draw(batch);
        }
    }
}

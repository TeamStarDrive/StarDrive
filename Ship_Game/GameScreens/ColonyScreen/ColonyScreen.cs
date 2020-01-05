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
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class ColonyScreen : PlanetScreen, IListScreen
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
        private UICheckBox GovOrbitals;
        private UICheckBox GovMilitia;
        private UICheckBox DontScrapBuildings;
        private UITextEntry PlanetName = new UITextEntry();
        private Rectangle PlanetIcon;
        private EmpireUIOverlay eui;
        private ToggleButton LeftColony;
        private ToggleButton RightColony;
        private UIButton LaunchAllTroops;
        private UIButton LaunchSingleTroop;
        private UIButton BuildPlatform;
        private UIButton BuildStation;
        private UIButton BuildShipyard;
        private UIButton CallTroops;  //fbedard
        private DropOptions<int> GovernorDropdown;
        public CloseButton close;
        private Array<ThreeStateButton> ResourceButtons = new Array<ThreeStateButton>();
        private Rectangle GridPos;
        private Submenu subColonyGrid;
        private ScrollList buildSL;
        private ScrollList CQueue;
        private DropDownMenu foodDropDown;
        private DropDownMenu prodDropDown;
        private ProgressBar FoodStorage;
        private ProgressBar ProdStorage;
        private Rectangle FoodStorageIcon;
        private Rectangle ProfStorageIcon;
        private float ButtonUpdateTimer;   // updates buttons once per second
        private string PlatformsStats = "Platforms:";
        private string StationsStats  = "Stations:";
        private string ShipyardsStats = "Shipyards:";

        ColonySliderGroup Sliders;

        private object DetailInfo;
        private Building ToScrap;
        private ScrollList.Entry ActiveBuildingEntry;

        public bool ClickedTroop;
        bool Reset;
        int EditHoverState;

        private Selector Selector;
        private Rectangle EditNameButton;
        private static bool Popup;  //fbedard
        private readonly SpriteFont Font8 = Fonts.Arial8Bold;
        private readonly SpriteFont Font12 = Fonts.Arial12Bold;
        private readonly SpriteFont Font20 = Fonts.Arial20Bold;
        private readonly Empire Player     = EmpireManager.Player;

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
            close = new CloseButton(theMenu3.X + theMenu3.Width - 52, theMenu3.Y + 22);
            var theMenu4 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            PlanetInfo = new Submenu(theMenu4);
            PlanetInfo.AddTab(Localizer.Token(326));
            var theMenu5 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pDescription = new Submenu(theMenu5);

            var laborPanel = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + 20, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pLabor = new Submenu(laborPanel);
            pLabor.AddTab(Localizer.Token(327));

            CreateSliders(laborPanel);

            var theMenu7 = new Rectangle(theMenu2.X + 20, theMenu2.Y + 20 + theMenu4.Height + theMenu5.Height + laborPanel.Height + 40, (int)(0.400000005960464 * theMenu2.Width), (int)(0.25 * (theMenu2.Height - 80)));
            pStorage = new Submenu(theMenu7);
            pStorage.AddTab(Localizer.Token(328));

            FoodStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.330000013113022 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
            FoodStorage.Max = p.Storage.Max;
            FoodStorage.Progress = p.FoodHere;
            FoodStorage.color = "green";
            foodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
            foodDropDown.AddOption(Localizer.Token(329));
            foodDropDown.AddOption(Localizer.Token(330));
            foodDropDown.AddOption(Localizer.Token(331));
            foodDropDown.ActiveIndex = (int)p.FS;
            var iconStorageFood = ResourceManager.Texture("NewUI/icon_storage_food");
            FoodStorageIcon = new Rectangle(theMenu7.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageFood.Width, iconStorageFood.Height);
            ProdStorage = new ProgressBar(new Rectangle(theMenu7.X + 100, theMenu7.Y + 25 + (int)(0.660000026226044 * (theMenu7.Height - 25)), (int)(0.400000005960464 * theMenu7.Width), 18));
            ProdStorage.Max = p.Storage.Max;
            ProdStorage.Progress = p.ProdHere;
            var iconStorageProd = ResourceManager.Texture("NewUI/icon_storage_production");
            ProfStorageIcon = new Rectangle(theMenu7.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - iconStorageFood.Height / 2, iconStorageProd.Width, iconStorageFood.Height);
            prodDropDown = new DropDownMenu(new Rectangle(theMenu7.X + 100 + (int)(0.400000005960464 * theMenu7.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, (int)(0.200000002980232 * theMenu7.Width), 18));
            prodDropDown.AddOption(Localizer.Token(329));
            prodDropDown.AddOption(Localizer.Token(330));
            prodDropDown.AddOption(Localizer.Token(331));
            prodDropDown.ActiveIndex = (int)p.PS;

            var theMenu8 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu4.Y, theMenu2.Width - 60 - theMenu4.Width, (int)(theMenu2.Height * 0.5));
            subColonyGrid = new Submenu(theMenu8);
            subColonyGrid.AddTab(Localizer.Token(332));
            var theMenu9 = new Rectangle(theMenu2.X + 20 + theMenu4.Width + 20, theMenu8.Y + theMenu8.Height + 20, theMenu2.Width - 60 - theMenu4.Width, theMenu2.Height - 20 - theMenu8.Height - 40);
            pFacilities = new Submenu(theMenu9);
            pFacilities.AddTab(Localizer.Token(333));

            ButtonUpdateTimer = 1;
            LaunchAllTroops   = Button(theMenu8.X + theMenu8.Width - 175, theMenu8.Y - 5, "Launch All Troops", OnLaunchTroopsClicked);
            LaunchSingleTroop = Button(theMenu8.X + theMenu8.Width - LaunchAllTroops.Rect.Width - 185,
                                       theMenu8.Y - 5, "Launch Single Troop", OnLaunchSingleTroopClicked);

            CallTroops        = Button(theMenu8.X + theMenu8.Width - LaunchSingleTroop.Rect.Width - 365,
                                       theMenu8.Y - 5, "Call Troops", OnSendTroopsClicked);

            LaunchAllTroops.Tooltip   = Localizer.Token(1952);
            LaunchSingleTroop.Tooltip = Localizer.Token(1950);
            CallTroops.Tooltip        = Localizer.Token(1949);

            BuildShipyard = Button(theMenu9.X + theMenu9.Width - 175, theMenu9.Y - 5, "Build Shipyard", OnBuildShipyardClick);
            BuildStation  = Button(theMenu9.X + theMenu9.Width - LaunchAllTroops.Rect.Width - 185,
                                   theMenu9.Y - 5, "Build Station", OnBuildStationClick);

            BuildPlatform = Button(theMenu9.X + theMenu9.Width - LaunchSingleTroop.Rect.Width - 365,
                                   theMenu9.Y - 5, "Build Platform", OnBuildPlatformClick);

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

            //new ScrollList(pFacilities, 40);
            var theMenu10 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20, theMenu3.Width - 40, (int)(0.5 * (theMenu3.Height - 60)));
            build = new Submenu(theMenu10);
            build.AddTab(Localizer.Token(334));
            buildSL = new ScrollList(build);
            PlayerDesignsToggle = new ToggleButton(
                new Vector2(build.Right - 270, build.Y),
                ToggleButtonStyle.Grid, "SelectionBox/icon_grid");

            PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
            if (p.HasSpacePort)
                build.AddTab(Localizer.Token(335));
            if (p.AllowInfantry)
                build.AddTab(Localizer.Token(336));
            var theMenu11 = new Rectangle(theMenu3.X + 20, theMenu3.Y + 20 + 20 + theMenu10.Height, theMenu3.Width - 40, theMenu3.Height - 40 - theMenu10.Height - 20 - 3);
            queue = new Submenu(theMenu11);
            queue.AddTab(Localizer.Token(337));

            CQueue = new ScrollList(queue, ListOptions.Draggable);

            PlanetIcon = new Rectangle(theMenu4.X + theMenu4.Width - 148, theMenu4.Y + (theMenu4.Height - 25) / 2 - 64 + 25, 128, 128);
            GridPos = new Rectangle(subColonyGrid.Rect.X + 10, subColonyGrid.Rect.Y + 30, subColonyGrid.Rect.Width - 20, subColonyGrid.Rect.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
                planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.x * width, GridPos.Y + planetGridSquare.y * height, width, height);
            PlanetName.Text = p.Name;
            PlanetName.MaxCharacters = 12;
            if (p.Owner != null)
            {
                DetailInfo = p.Description;
                var rectangle4 = new Rectangle(pDescription.Rect.X + 10, pDescription.Rect.Y + 30, 124, 148);
                var rectangle5 = new Rectangle(rectangle4.X + rectangle4.Width + 20, rectangle4.Y + rectangle4.Height - 15, (int)Fonts.Pirulen16.MeasureString(Localizer.Token(370)).X, Fonts.Pirulen16.LineSpacing);
                GovernorDropdown = new DropOptions<int>(new Rectangle(rectangle5.X + 30, rectangle5.Y + 30, 100, 18));
                GovernorDropdown.AddOption("--", 1);
                GovernorDropdown.AddOption(Localizer.Token(4064), 0); // Core
                GovernorDropdown.AddOption(Localizer.Token(4065), 2); // Industrial
                GovernorDropdown.AddOption(Localizer.Token(4066), 4); // Agricultural
                GovernorDropdown.AddOption(Localizer.Token(4067), 3); // Research
                GovernorDropdown.AddOption(Localizer.Token(4068), 5); // Military
                GovernorDropdown.AddOption(Localizer.Token(5087), 6); // Trade Hub
                GovernorDropdown.ActiveIndex = GetIndex(p);

                P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                GovOrbitals  = new UICheckBox(rectangle4.X - 3, rectangle5.Y + Font12.LineSpacing + 5,
                    () => p.GovOrbitals, Fonts.Arial12Bold, Localizer.Token(1960), 1961);

                GovMilitia   = new UICheckBox(rectangle4.X - 3, rectangle5.Y + (Font12.LineSpacing + 5) * 2,
                    () => p.GovMilitia, Fonts.Arial12Bold, Localizer.Token(1956), 1957);

                DontScrapBuildings = new UICheckBox(rectangle4.X + 240, rectangle5.Y + (Font12.LineSpacing + 5),
                    () => p.DontScrapBuildings, Fonts.Arial12Bold, Localizer.Token(1941), 1942);
            }
            else
            {
                Empire.Universe.LookingAtPlanet = false;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (P.Owner == null)
                return;
            P.UpdateIncomes(false);
            LeftMenu.Draw(batch);
            RightMenu.Draw(batch);
            TitleBar.Draw(batch);
            LeftColony.Draw(ScreenManager);
            RightColony.Draw(ScreenManager);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(369), TitlePos, new Color(255, 239, 208));
            FoodStorage.Max = P.Storage.Max;
            FoodStorage.Progress = P.FoodHere;
            ProdStorage.Max = P.Storage.Max;
            ProdStorage.Progress = P.ProdHere;
            PlanetInfo.Draw(batch);
            pDescription.Draw(batch);
            pLabor.Draw(batch);
            pStorage.Draw(batch);
            subColonyGrid.Draw(batch);
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

                if (pgs.Biosphere && P.Owner != null)
                {
                    batch.FillRectangle(pgs.ClickRect, P.Owner.EmpireColor.Alpha(0.3f));
                }
                DrawPGSIcons(pgs);
            }
            foreach (PlanetGridSquare planetGridSquare in P.TilesList)
            {
                if (planetGridSquare.Highlighted)
                    batch.DrawRectangle(planetGridSquare.ClickRect, Color.White, 2f);
            }

            pFacilities.Draw(batch);
            DrawDetailInfo(new Vector2(pFacilities.Rect.X + 15, pFacilities.Rect.Y + 35));
            build.Draw(batch);
            queue.Draw(batch);

            if (build.Tabs[0].Selected)
            {
                DrawBuildingsWeCanBuild(batch);
            }
            else if (build.Tabs[1].Selected)
            {
                if (P.HasSpacePort)
                    DrawBuildableShipsList(batch);
                else if (P.AllowInfantry)
                    DrawBuildTroopsList(batch);
            }
            else if (P.AllowInfantry && build.Tabs[2].Selected)
            {
                DrawBuildTroopsList(batch);
            }

            DrawConstructionQueue(batch);

            buildSL.Draw(batch);
            Selector?.Draw(batch);

            DrawSliders(batch);

            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);
            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            var vector2_2 = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
            P.Name = PlanetName.Text;
            PlanetName.Draw(batch, Font20, vector2_2, new Color(255, 239, 208));
            EditNameButton = new Rectangle((int)(vector2_2.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(vector2_2.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (EditHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), EditNameButton, Color.White);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 768)
                vector2_2.Y += Font20.LineSpacing * 2;
            else
                vector2_2.Y += Font20.LineSpacing;
            batch.DrawString(Font12, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, P.CategoryName, position3, new Color(255, 239, 208));
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            var color = new Color(255, 239, 208);
            batch.DrawString(Font12, P.PopulationStringForPlayer, position3, color);
            var rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(385) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            string fertility;
            if (P.FertilityFor(Player).AlmostEqual(P.MaxFertilityFor(Player)))
            {
                fertility = P.FertilityFor(Player).String(2);
                batch.DrawString(Font12, fertility, position3, color);
            }
            else
            {
                Color fertColor = P.FertilityFor(Player) < P.MaxFertilityFor(Player) ? Color.LightGreen : Color.Pink;
                fertility = $"{P.FertilityFor(Player).String(2)} / {P.MaxFertilityFor(Player).String(2)}";
                batch.DrawString(Font12, fertility, position3, fertColor);
            }
            float fertEnvMultiplier = EmpireManager.Player.RacialEnvModifer(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                Vector2 fertMultiplier = new Vector2(position3.X + Font12.MeasureString($"{fertility} ").X, position3.Y);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }
            if (P.TerraformPoints > 0)
            {
                Color terraformColor = P.Owner?.EmpireColor ?? Color.White;
                string terraformText = Localizer.Token(683); // Terraform Planet is the default text
                if (P.TilesToTerraform)
                    terraformText  = Localizer.Token(1972);
                else if (P.BioSpheresToTerraform 
                         && P.Category == P.Owner?.data.PreferredEnv 
                         && P.MaxFertilityFor(Player).AlmostEqual(P.TerraformMaxFertilityTarget))
                { 
                    terraformText = Localizer.Token(1919);
                }

                Vector2 terraformPos = new Vector2(vector2_2.X + num5 * 3.9f, vector2_2.Y + (Font12.LineSpacing + 2) * 5);
                batch.DrawString(Font12, $"{terraformText} - {(P.TerraformPoints * 100).String(0)}%", terraformPos, terraformColor);
            }

            if (P.NumIncomingFreighters > 0 && P.Owner?.isPlayer == true)
            {
                Vector2 incomingTitle = new Vector2(vector2_2.X + + 200, vector2_2.Y - (Font12.LineSpacing + 2) * 3);
                Vector2 incomingData =  new Vector2(vector2_2.X + 200 + num5, vector2_2.Y - (Font12.LineSpacing + 2) * 3);
                int lineDown = Font12.LineSpacing + 2;
                batch.DrawString(Font12, "Incoming Freighters:", incomingTitle, Color.White);
                incomingTitle.Y += lineDown;
                incomingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(161)}:", incomingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.IncomingFoodFreighters}", incomingData, Color.White);
                incomingTitle.Y += lineDown;
                incomingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(162)}:", incomingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.IncomingProdFreighters}", incomingData, Color.White);
                incomingTitle.Y += lineDown;
                incomingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(1962)}:", incomingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.IncomingColonistsFreighters}", incomingData, Color.White);
            }

            if (P.NumOutgoingFreighters > 0 && P.Owner?.isPlayer == true)
            {
                Vector2 outgoingTitle = new Vector2(vector2_2.X + +200, vector2_2.Y + (Font12.LineSpacing + 2) * 2);
                Vector2 outgoingData  = new Vector2(vector2_2.X + 200 + num5, vector2_2.Y + (Font12.LineSpacing + 2) * 2);
                int lineDown = Font12.LineSpacing + 2;
                batch.DrawString(Font12, "Outgoing Freighters:", outgoingTitle, Color.White);
                outgoingTitle.Y += lineDown;
                outgoingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(161)}:", outgoingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.OutgoingFoodFreighters}", outgoingData, Color.White);
                outgoingTitle.Y += lineDown;
                outgoingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(162)}:", outgoingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.OutgoingProdFreighters}", outgoingData, Color.White);
                outgoingTitle.Y += lineDown;
                outgoingData.Y  += lineDown;
                batch.DrawString(Font12, $"{Localizer.Token(1962)}:", outgoingTitle, Color.Gray);
                batch.DrawString(Font12, $"{P.OutGoingColonistsFreighters}", outgoingData, Color.White);
            }

            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(386) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            batch.DrawString(Font12, P.MineralRichness.String(), position3, new Color(255, 239, 208));
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(387) + ":").X, Font12.LineSpacing);


            // The Doctor: For planet income breakdown

            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

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

            var portrait = new Rectangle(pDescription.Rect.X + 10, pDescription.Rect.Y + 30, 124, 148);
            while (portrait.Bottom > pDescription.Rect.Bottom)
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
                                            pDescription.Rect.Right - portrait.Right - 20,
                                            pDescription.Rect.Height - 60);

            var descCursor = new Vector2(description.X, description.Y);
            batch.DrawString(Font12, P.WorldType, descCursor, Color.White);
            descCursor.Y += Font12.LineSpacing + 5;

            GovernorDropdown.Pos = descCursor;
            GovernorDropdown.Reset();
            descCursor.Y += GovernorDropdown.Height + 5;

            string colonyTypeInfo = Font12.ParseText(P.ColonyTypeInfoText, description.Width);
            batch.DrawString(Font12, colonyTypeInfo, descCursor, Color.White);
            GovernorDropdown.Draw(batch); // draw dropdown on top of other text
            if (P.Owner.isPlayer && GovernorDropdown.ActiveIndex != 0)
            {
                // only for Governor colonies
                GovOrbitals.Draw(batch); 
                GovMilitia.Draw(batch);
                if (P.colonyType != Planet.ColonyType.TradeHub) // not for trade hubs, which do not build structures anyway
                    DontScrapBuildings.Draw(batch); 
            }

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

            DrawOrbitalStats(batch);

            base.Draw(batch);

            if (ScreenManager.NumScreens == 2)
                Popup = true;

            close.Draw(batch);
            DrawActiveBuildingEntry(batch); // draw dragged item as topmost

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(74);
        }

        void DrawOrbitalStats(SpriteBatch batch)
        {
            if (P.Owner != EmpireManager.Player)
                return;

            if (P.colonyType == Planet.ColonyType.Colony || P.colonyType != Planet.ColonyType.Colony && !P.GovOrbitals)
            {
                // Show build buttons
                BuildPlatform.Visible = P.Owner.CanBuildPlatforms && P.HasSpacePort;
                BuildStation.Visible  = P.Owner.CanBuildStations && P.HasSpacePort;
                BuildShipyard.Visible = P.Owner.CanBuildShipyards && P.HasSpacePort;
            }
            else if (P.GovOrbitals)
            {
                BuildPlatform.Visible = false;
                BuildStation.Visible  = false;
                BuildShipyard.Visible = false;

                // Draw Governor current / wanted orbitals
                Vector2 platformsStatVec = new Vector2(BuildPlatform.X + 30, BuildPlatform.Y + 5);
                Vector2 stationsStatVec  = new Vector2(BuildStation.X + 30, BuildStation.Y + 5);
                Vector2 shipyardsStatVec = new Vector2(BuildShipyard.X + 30, BuildShipyard.Y + 5);
                if (P.Owner.CanBuildPlatforms)
                    batch.DrawString(Font12, PlatformsStats, platformsStatVec, Color.White);

                if (P.Owner.CanBuildStations)
                    batch.DrawString(Font12, StationsStats, stationsStatVec, Color.White);

                if (P.Owner.CanBuildShipyards)
                    batch.DrawString(Font12, ShipyardsStats, shipyardsStatVec, Color.White);
            }

        }

        Color TextColor { get; } = new Color(255, 239, 208);

        void DrawText(ref Vector2 cursor, string text)
        {
            DrawText(ref cursor, text, Color.White);
        }

        void DrawText(ref Vector2 cursor, int tokenId, Color color)
        {
            DrawText(ref cursor, Localizer.Token(tokenId), color);
        }

        void DrawText(ref Vector2 cursor, string text, Color color)
        {
            ScreenManager.SpriteBatch.DrawString(Font12, text, cursor, color);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawTitledLine(ref Vector2 cursor, int titleId, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Font12, Localizer.Token(titleId) +": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Font12, text, textCursor, TextColor);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        string MultiLineFormat(string text)
        {
            return Font12.ParseText(text, pFacilities.Rect.Width - 40);
        }

        string MultiLineFormat(int token)
        {
            return MultiLineFormat(Localizer.Token(token));
        }

        void DrawMultiLine(ref Vector2 cursor, string text, Color color)
        {
            string multiline = MultiLineFormat(text);
            ScreenManager.SpriteBatch.DrawString(Font12, multiline, cursor, color);
            cursor.Y += (Font12.MeasureString(multiline).Y + Font12.LineSpacing);
        }

        void DrawCommoditiesArea(Vector2 bCursor)
        {
            ScreenManager.SpriteBatch.DrawString(Font12, MultiLineFormat(4097), bCursor, TextColor);
        }

        void DrawDetailInfo(Vector2 bCursor)
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
                    DrawTitledLine(ref bCursor, 338, t.TargetType.ToString());
                    DrawTitledLine(ref bCursor, 339, strength);
                    DrawTitledLine(ref bCursor, 2218, t.ActualHardAttack.ToString());
                    DrawTitledLine(ref bCursor, 2219, t.ActualSoftAttack.ToString());
                    DrawTitledLine(ref bCursor, 6008, t.BoardingStrength.ToString());
                    DrawTitledLine(ref bCursor, 6023, t.Level.ToString());
                    DrawTitledLine(ref bCursor, 1966, t.ActualRange.ToString());
                    break;

                case string _:
                    DrawMultiLine(ref bCursor, P.Description);
                    string desc = "";
                    if (P.IsCybernetic)  desc = Localizer.Token(2028);
                    else switch (P.FS)
                    {
                        case Planet.GoodState.EXPORT: desc = Localizer.Token(2025); break;
                        case Planet.GoodState.IMPORT: desc = Localizer.Token(2026); break;
                        case Planet.GoodState.STORE:  desc = Localizer.Token(2027); break;
                    }

                    DrawMultiLine(ref bCursor, desc);
                    desc = "";
                    if (P.colonyType == Planet.ColonyType.Colony)
                    {
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(345); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(346); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(347); break;
                        }
                    }
                    else
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(1953); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(1954); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(1955); break;
                        }
                    DrawMultiLine(ref bCursor, desc);
                    if (P.IsStarving)
                        DrawMultiLine(ref bCursor, Localizer.Token(344), Color.LightPink);
                    DrawPlanetStat(ref bCursor, spriteBatch);
                    break;

                case PlanetGridSquare pgs:
                    switch (pgs.building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            spriteBatch.DrawString(Font20, Localizer.Token(348), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                        case null when pgs.Habitable:
                            spriteBatch.DrawString(Font20, Localizer.Token(350), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            return;
                    }

                    if (!pgs.Habitable && pgs.building == null)
                    {
                        if (P.IsBarrenType)
                        {
                            spriteBatch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            spriteBatch.DrawString(Font12, MultiLineFormat(352), bCursor, color);
                            return;
                        }
                        spriteBatch.DrawString(Font20, Localizer.Token(351), bCursor, color);
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
                    spriteBatch.DrawString(Font20, Localizer.Token(selectedBuilding.NameTranslationIndex), bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionIndex);
                    spriteBatch.DrawString(Font12, selectionText, bCursor, color);
                    bCursor.Y += Font12.MeasureString(selectionText).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, spriteBatch, selectedBuilding);
                    break;
            }
        }

        void DrawPlanetStat(ref Vector2 cursor, SpriteBatch batch)
        {
            DrawBuildingInfo(ref cursor, batch, P.PopPerTileFor(Player) / 1000, "UI/icon_pop_22", "Colonists per Habitable Tile (Billions)");
            DrawBuildingInfo(ref cursor, batch, P.BasePopPerTile / 1000, "UI/icon_pop_22", "Colonists per Biosphere (Billions)");
            if (P.NonCybernetic)
            {
                DrawBuildingInfo(ref cursor, batch, P.Food.NetYieldPerColonist - P.FoodConsumptionPerColonist
                                , "NewUI/icon_food", "Net food per colonist allocated to Food Production");
                DrawBuildingInfo(ref cursor, batch, P.Food.NetFlatBonus, "NewUI/icon_food", "Net flat food generated per turn");
            }

            DrawBuildingInfo(ref cursor, batch, P.Prod.NetYieldPerColonist - P.ProdConsumptionPerColonist, "NewUI/icon_production", "Net production per colonist allocated to Industry");
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetFlatBonus, "NewUI/icon_production", "Net flat production generated per turn");
            DrawBuildingInfo(ref cursor, batch, P.Res.NetYieldPerColonist, "NewUI/icon_science", "Net research per colonist allocated to Science");
            DrawBuildingInfo(ref cursor, batch, P.Res.NetFlatBonus, "NewUI/icon_science", "Net flat research generated per turn");
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Building b)
        {
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatFoodAmount, "NewUI/icon_food", Localizer.Token(354));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFoodPerColonist, "NewUI/icon_food", Localizer.Token(2042));
            DrawBuildingInfo(ref bCursor, batch, b.SensorRange, "NewUI/icon_sensors", Localizer.Token(6000), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.ProjectorRange, "NewUI/icon_projection", Localizer.Token(6001), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatProductionAmount, "NewUI/icon_production", Localizer.Token(355));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerColonist, "NewUI/icon_production", Localizer.Token(356));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatPopulation / 1000, "NewUI/icon_population", Localizer.Token(2043));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatResearchAmount, "NewUI/icon_science", Localizer.Token(357));
            DrawBuildingInfo(ref bCursor, batch, b.PlusResearchPerColonist, "NewUI/icon_science", Localizer.Token(358));
            DrawBuildingInfo(ref bCursor, batch, b.PlusTaxPercentage * 100, "NewUI/icon_money", Localizer.Token(359), percent: true);
            DrawBuildingInfo(ref bCursor, batch, b.MaxFertilityOnBuildFor(Player, P.Category), "NewUI/icon_food", Localizer.Token(360));
            DrawBuildingInfo(ref bCursor, batch, b.PlanetaryShieldStrengthAdded, "NewUI/icon_planetshield", Localizer.Token(361));
            DrawBuildingInfo(ref bCursor, batch, b.CreditsPerColonist, "NewUI/icon_money", Localizer.Token(362));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerRichness, "NewUI/icon_production", Localizer.Token(363));
            DrawBuildingInfo(ref bCursor, batch, b.ShipRepair * 10, "NewUI/icon_queue_rushconstruction", Localizer.Token(6137));
            DrawBuildingInfo(ref bCursor, batch, b.CombatStrength, "Ground_UI/Ground_Attack", Localizer.Token(364));
            float maintenance = -(b.Maintenance + b.Maintenance * P.Owner.data.Traits.MaintMod);
            DrawBuildingInfo(ref bCursor, batch, maintenance, "NewUI/icon_money", Localizer.Token(365));
            if (b.TheWeapon != null)
            {
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.BaseRange, "UI/icon_offense", "Range", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.DamageAmount, "UI/icon_offense", "Damage", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.EMPDamage, "UI/icon_offense", "EMP Damage", signs: false);
                DrawBuildingInfo(ref bCursor, batch, b.TheWeapon.NetFireDelay, "UI/icon_offense", "Fire Delay", signs: false);
            }

            if (b.DefenseShipsCapacity > 0)
                DrawBuildingInfo(ref bCursor, batch, b.DefenseShipsCapacity, "UI/icon_hangar", b.DefenseShipsRole + " Defense Ships", signs: false);
            if (b.PlusTerraformPoints > 0)
            {
                string terraformStats = MultiLineFormat(TerraformPotential(out Color terraformColor));
                bCursor.Y += Font12.LineSpacing * 2;
                batch.DrawString(Font12, terraformStats, bCursor, terraformColor);
                bCursor.Y += Font12.LineSpacing;
            }
        }

        public float PositiveTerraformTargetFertility()
        {
            var buildingList = P.BuildingList.Filter(b => b.MaxFertilityOnBuild > 0);
            float positiveFertilityOnBuild = buildingList.Sum(b => b.MaxFertilityOnBuild);

            return 1 + positiveFertilityOnBuild / Player.RacialEnvModifer(Player.data.PreferredEnv);
        }

        string TerraformPotential(out Color color)
        {
            color                       = Color.LightGreen;
            float targetFertility       = PositiveTerraformTargetFertility();
            int numUninhabitableTiles   = P.TilesList.Count(t => !t.Habitable);
            int numBiospheres           = P.TilesList.Count(t => t.Biosphere);
            int numNegativeEnvBuildings = P.BuildingList.Count(b => b.MaxFertilityOnBuild < 0);
            float minEstimatedMaxPop    = P.TileArea * P.BasePopPerTile * Player.RacialEnvModifer(Player.data.PreferredEnv) 
                                          + P.BuildingList.Sum(b => b.MaxPopIncrease);

            string text = "Terraformer Process Stages: ";
            string initialText = text;

            if (numNegativeEnvBuildings > 0) // not full potential due to bad env buildings
                text += $"Scrap {numNegativeEnvBuildings} environment degrading buildings. ";

            if (numUninhabitableTiles > 0)
                text += $"Make {numUninhabitableTiles} tiles habitable. ";

            if (P.Category != Player.data.PreferredEnv)
                text += $"Terraform the planet to {Player.data.PreferredEnv.ToString()}. ";

            if (numBiospheres > 0)
                text += $"Remove {numBiospheres} Biospheres. ";

            if (minEstimatedMaxPop > P.MaxPopulationFor(Player))
                text += $"Increase Population to a minimum of {(minEstimatedMaxPop / 1000).String(2)} Billion colonists. ";

            if (targetFertility.Greater(P.MaxFertilityFor(Player))) // better new fertility max
                text += $"Fertility will be changed to {targetFertility}.";

            if (text == initialText)
            {
                color = Color.Yellow;
                text = "Terraformers will have no effect on this planet.";
            }

            return text;
        }

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, string texture, 
                              string toolTip, bool percent = false, bool signs = true)
        {
            DrawBuildingInfo(ref cursor, batch, value, ResourceManager.Texture(texture), toolTip, percent, signs);
        }

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, SubTexture texture, 
                              string toolTip, bool percent = false, bool signs = true)
        {
            if (value.AlmostEqual(0))
                return;

            var fIcon   = new Rectangle((int)cursor.X, (int)cursor.Y, texture.Width, texture.Height);
            var tCursor = new Vector2(cursor.X + fIcon.Width + 5f, cursor.Y + 3f);
            string plusOrMinus = "";
            Color color = Color.White;
            if (signs)
            {
                plusOrMinus = value < 0 ? "-" : "+";
                color = value < 0 ? Color.Pink : Color.LightGreen;
            }
            batch.Draw(texture, fIcon, Color.White);
            string suffix = percent ? "% " : " ";
            string text = string.Concat(plusOrMinus, Math.Abs(value).String(2), suffix, toolTip);
            batch.DrawString(Font12, text, tCursor, color);
            cursor.Y += Font12.LineSpacing + 10;
        }

        void DrawTroopLevel(Troop troop, Rectangle rect)
        {
            SpriteFont font = Font12;
            var levelRect   = new Rectangle(rect.X + 30, rect.Y + 22, font.LineSpacing, font.LineSpacing + 5);
            var pos         = new Vector2((rect.X + 15 + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                                         (1 + rect.Y + 5 + rect.Height / 2 - font.LineSpacing / 2));

            ScreenManager.SpriteBatch.FillRectangle(levelRect, new Color(0, 0, 0, 200));
            ScreenManager.SpriteBatch.DrawRectangle(levelRect, troop.Loyalty.EmpireColor);
            ScreenManager.SpriteBatch.DrawString(font, troop.Level.ToString(), pos, Color.Gold);
        }

        void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                Troop troop        = pgs.SingleTroop;
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

        void HandleDetailInfo(InputState input)
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
            pFacilities.HandleInputNoReset(input);
            GovOrbitals.HandleInput(input);
            GovMilitia.HandleInput(input);
            DontScrapBuildings.HandleInput(input);

            if (HandleCycleColoniesLeftRight(input))
                return true;

            P.UpdateIncomes(false);
            HandleDetailInfo(input);
            buildSL.HandleInput(input);
            build.HandleInput(input);

            // We are monitoring AI Colonies
            if (P.Owner != EmpireManager.Player && !Log.HasDebugger)
            {
                // Input not captured, let Universe Screen manager what happens
                return false;
            }

            HandlePlanetNameChangeTextBox(input);

            GovernorDropdown.HandleInput(input);
            P.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;

            HandleSliders(input);

            if (P.HasSpacePort && build.Tabs.Count > 1 && build.Tabs[1].Selected)
            {
                if (PlayerDesignsToggle.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2225));
                }
                if (PlayerDesignsToggle.HandleInput(input) && !input.LeftMouseReleased)
                {
                    GameAudio.AcceptClick();
                    GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
                    PlayerDesignsToggle.Active = GlobalStats.ShowAllDesigns;
                    ResetLists();
                }
            }

            Selector = null;
            if (HandleTroopSelect(input))
                return true;

            HandleExportImportButtons(input);
            HandleConstructionQueueInput(input);
            if (HandleDragBuildingOntoTile(input))
            {
                ActiveBuildingEntry = null; // building was placed or discarded
                return true;
            }

            if (HandleBuildListClicks(input))
                return true;

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
                    pgs.Highlighted = false;
                }
                else
                {
                    if (!pgs.Highlighted)
                    {
                        GameAudio.ButtonMouseOver();
                    }

                    pgs.Highlighted = true;
                }

                if (pgs.TroopsHere.Count <= 0 || !pgs.TroopClickRect.HitTest(MousePos))
                    continue;

                DetailInfo = pgs.SingleTroop;
                if (input.RightMouseClick && pgs.SingleTroop.Loyalty == EmpireManager.Player)
                {
                    GameAudio.TroopTakeOff();
                    pgs.SingleTroop.Launch(pgs);
                    ClickedTroop = true;
                    DetailInfo   = null;
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
                                ScreenManager.AddScreenDeferred(messageBox);
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
            if      (RightColony.Rect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(Localizer.Token(2279));
            else if (LeftColony.Rect.HitTest(input.CursorPosition))  ToolTip.CreateTooltip(Localizer.Token(2280));

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
            if (foodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                foodDropDown.Toggle();
                GameAudio.AcceptClick();
                P.FS = (Planet.GoodState) ((int) P.FS + (int) Planet.GoodState.IMPORT);
                if (P.FS > Planet.GoodState.EXPORT)
                    P.FS = Planet.GoodState.STORE;
            }

            if (prodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                prodDropDown.Toggle();
                GameAudio.AcceptClick();
                P.PS = (Planet.GoodState) ((int) P.PS + (int) Planet.GoodState.IMPORT);
                if (P.PS > Planet.GoodState.EXPORT)
                    P.PS = Planet.GoodState.STORE;
            }
        }

        void OnBuildPlatformClick(UIButton b)
        {
            if (BuildOrbital(P.Owner.BestPlatformWeCanBuild))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        void OnBuildStationClick(UIButton b)
        {
            if (BuildOrbital(P.Owner.BestStationWeCanBuild))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        void OnBuildShipyardClick(UIButton b)
        {
            string shipyardName = ResourceManager.ShipsDict[P.Owner.data.DefaultShipyard].Name;
            Ship shipyard = ResourceManager.GetShipTemplate(shipyardName);
            if (BuildOrbital(shipyard))
                GameAudio.AffirmativeClick();
            else
                GameAudio.NegativeClick();
        }

        bool BuildOrbital(Ship orbital)
        {
            if (orbital == null || P.IsOutOfOrbitalsLimit(orbital))
                return false;

            P.AddOrbital(orbital);
            return true;
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
                if (!pgs.TroopsAreOnTile || pgs.SingleTroop.Loyalty != EmpireManager.Player || !pgs.SingleTroop.CanMove)
                    continue;

                play = true;
                pgs.SingleTroop.Launch(pgs);
                ClickedTroop = true;
                DetailInfo = null;
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
            if (P.TroopsHere.Count == 0)
                GameAudio.NegativeClick();
            else
            {
                Troop troop = P.TroopsHere.Filter(t => t.Loyalty == EmpireManager.Player).RandItem();
                troop.Launch();
                GameAudio.TroopTakeOff();
                UpdateButtons();
            }
        }

        public void ResetLists()
        {
            Reset = true;
        }

        void ScrapAccepted()
        {
            if (ToScrap != null)
                P.ScrapBuilding(ToScrap);

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
                    if (P.HasSpacePort)
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
                    foreach (ScrollList.Entry entry in buildSL.VisibleEntries)
                        if (entry.TryGet(out Troop troop))
                            troop.Update(elapsedTime);
                }
                if (add)
                {
                    build.Tabs.Clear();
                    build.AddTab(Localizer.Token(334));
                    if (P.HasSpacePort)
                    {
                        build.AddTab(Localizer.Token(335));
                    }
                    build.AddTab(Localizer.Token(336));
                }
            }
            if (!P.HasSpacePort)
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
                    if (P.AllowInfantry)
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
                    if (P.AllowInfantry)
                    {
                        build.AddTab(Localizer.Token(336));
                    }
                }
            }

            UpdateButtonTimer(elapsedTime);
        }

        void HandleSliders(InputState input)
        {
            Sliders.HandleInput(input);
            P.UpdateIncomes(false);
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

        void UpdateButtonTimer(float elapsedTime)
        {
            ButtonUpdateTimer -= elapsedTime;
            if (ButtonUpdateTimer.Greater(0))
                return;

            ButtonUpdateTimer = 1;
            UpdateButtons();
            UpdateGovOrbitalStats();
        }

        void UpdateGovOrbitalStats()
        {
            if (P.Owner != Empire.Universe.player || !P.GovOrbitals || P.colonyType == Planet.ColonyType.Colony)
                return;

            Planet.WantedOrbitals wantedOrbitals = P.GovernorWantedOrbitals();
            PlatformsStats = $"Platforms: {P.NumPlatforms}/{wantedOrbitals.Platforms}";
            StationsStats  = $"Stations: {P.NumStations}/{wantedOrbitals.Stations}";
            ShipyardsStats = $"Shipyards: {P.NumShipyards}/{wantedOrbitals.Shipyards}";
        }

        void UpdateButtons()
        {
            // fbedard: Display button
            if (P.Owner == Empire.Universe.player)
            {
                int troopsLanding = P.Owner.GetShips()
                    .Filter(s => s.TroopList.Count > 0 && s.AI.State != AIState.Resupply && s.AI.State != AIState.Orbit)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == P));

                if (troopsLanding > 0)
                {
                    CallTroops.Text = $"Incoming Troops: {troopsLanding}";
                    CallTroops.Style = ButtonStyle.Military;
                }
                else
                {
                    CallTroops.Text = "Call Troops";
                    CallTroops.Style = ButtonStyle.Default;
                }

                UpdateButtonText(LaunchAllTroops, P.TroopsHere.Count(t => t.CanMove), "Launch All Troops");
                UpdateButtonText(BuildPlatform, P.NumPlatforms, "Build Platform");
                UpdateButtonText(BuildStation, P.NumStations, "Build Station");
                UpdateButtonText(BuildShipyard, P.NumShipyards, "Build Shipyard");
            }

            CallTroops.Visible        = P.Owner == Empire.Universe.player;
            int numTroopsCanLaunch    = P.TroopsHere.Count(t => t.Loyalty == EmpireManager.Player && t.CanMove);
            LaunchSingleTroop.Visible = CallTroops.Visible && numTroopsCanLaunch > 0;
            LaunchAllTroops.Visible   = CallTroops.Visible && numTroopsCanLaunch > 1;
        }

        void UpdateButtonText(UIButton button, int value, string defaultText)
        {
            button.Text = value > 0 ? $"{defaultText} ({value})" : defaultText;
        }
    }
}
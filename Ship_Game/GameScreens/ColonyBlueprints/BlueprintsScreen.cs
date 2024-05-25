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
using System.Drawing;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Font = Ship_Game.Graphics.Font;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class BlueprintsScreen : GameScreen
    {
        readonly string BuildingsTabText = Localizer.Token(GameText.Buildings); // BUILDINGS

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
        readonly Rectangle GridPos;
        readonly Submenu SubColonyGrid;
        readonly Rectangle PlanetShieldIconRect;
        readonly ProgressBar PlanetShieldBar;
        readonly UILabel FilterBuildableItemsLabel;

        readonly ScrollList<BlueprintsBuildableListItem> BuildableList;

        object DetailInfo;
        Building ToScrap;
        PlanetGridSquare BioToScrap;

        Rectangle EditNameButton;
        readonly Font Font8 = Fonts.Arial8Bold;
        readonly Font Font12 = Fonts.Arial12Bold;
        readonly Font Font14 = Fonts.Arial14Bold;
        readonly Font Font20 = Fonts.Arial20Bold;
        readonly Font TextFont;
        public readonly Empire Player;

        public BlueprintsScreen(UniverseScreen parent, EmpireUIOverlay empUI, Empire player) : base(parent, toPause: parent)
        {
            Player = player;
            Eui = empUI;
            TextFont = LowRes ? Font8 : Font12;

            var titleBar = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleBar);
            TitlePos = new Vector2(titleBar.X + titleBar.Width / 2 - Fonts.Laserian14.MeasureString("Colony Blueprints").X / 2f, titleBar.Y + titleBar.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            LeftMenu = new Menu1(2, titleBar.Y + titleBar.Height + 5, titleBar.Width, ScreenHeight - (titleBar.Y + titleBar.Height) - 7);
            RightMenu = new Menu1(titleBar.Right + 10, titleBar.Y, ScreenWidth / 3 - 15, ScreenHeight - titleBar.Y - 2);
            Add(new CloseButton(RightMenu.Right - 52, RightMenu.Y + 22));

            RectF planetInfoR = new(LeftMenu.X + 20, LeftMenu.Y + 20,
                                    (int)(0.4f * LeftMenu.Width),
                                    (int)(0.23f * (LeftMenu.Height - 80)));
            PlanetInfo = new(planetInfoR, GameText.PlanetInfo);
            Submenu pDescription = new(LeftMenu.X + 20, LeftMenu.Y + 40 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            RectF subColonyR = new(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y,
                                   LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);
            SubColonyGrid = new(subColonyR, GameText.Colony);

            RectF pFacilitiesR = new(LeftMenu.X + 20 + PlanetInfo.Width + 20,
                                     SubColonyGrid.Bottom + 20,
                                     LeftMenu.Width - 60 - PlanetInfo.Width,
                                     LeftMenu.Height - 20 - SubColonyGrid.Height - 40);

            Array<LocalizedText> pFacTabs = new()
            {
                GameText.Statistics2,
                GameText.Description,
                GameText.Trade2,
            };


            RectF buildableR = new(RightMenu.X + 20, RightMenu.Y + 40,
                                   RightMenu.Width - 40, 0.5f * (RightMenu.Height - 40));
            BuildableList = base.Add(new ScrollList<BlueprintsBuildableListItem>(buildableR, 40));
            BuildableList.EnableItemHighlight = true;
            //BuildableList.OnDoubleClick = OnBuildableItemDoubleClicked;
            //BuildableList.OnHovered = OnBuildableHoverChange;
            //BuildableList.EnableDragOutEvents = true;

            //if (p.OwnerIsPlayer || p.Universe.Debug)
            //    BuildableList.OnDragOut = OnBuildableListDrag;

            int iconSize = LowRes ? 80 : 128;
            int iconOffsetX = LowRes ? 100 : 148;
            int iconOffsetY = LowRes ? 0 : 25;

            PlanetIcon = new Rectangle((int)PlanetInfo.Right - iconOffsetX,
                (int)PlanetInfo.Y + ((int)PlanetInfo.Height - iconOffsetY) / 2 - iconSize / 2 + (LowRes ? 0 : 25), iconSize, iconSize);

            Rectangle planetShieldBarRect = new Rectangle(PlanetIcon.X, PlanetInfo.Rect.Y + 4, PlanetIcon.Width, 20);
            PlanetShieldBar = new ProgressBar(planetShieldBarRect)
            {
                color = "blue"
            };

            PlanetShieldIconRect = new Rectangle(planetShieldBarRect.X - 30, planetShieldBarRect.Y - 2, 20, 20);

            GridPos = new Rectangle(SubColonyGrid.Rect.X + 10, SubColonyGrid.Rect.Y + 30, SubColonyGrid.Rect.Width - 20, SubColonyGrid.Rect.Height - 35);
            int width = GridPos.Width / 7;
            int height = GridPos.Height / 5;
            //foreach (PlanetGridSquare planetGridSquare in p.TilesList)
            //    planetGridSquare.ClickRect = new Rectangle(GridPos.X + planetGridSquare.X * width, GridPos.Y + planetGridSquare.Y * height, width, height);

            //PlanetName = Add(new UITextEntry(p.Name));
            //PlanetName.Color = Colors.Cream;
            //PlanetName.MaxCharacters = 20;

            /*
            if (p.Owner != null)
            {
                GovernorDetails = Add(new GovernorDetailsComponent(this, p, pDescription.RectF, governorTabSelected));
            }
            else
            {
                p.Universe.Screen.LookingAtPlanet = false;
            }*/

            //P.RefreshBuildingsWeCanBuildHere();
            // Vector2 detailsVector = new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35);
            RefreshBuildableList();
        }

        void AddLabel(ref UILabel uiLabel, Vector2 pos, LocalizedText text, Font font, Color color)
        {
            if (uiLabel == null)
                uiLabel = Add(new UILabel(pos, text, font, color));

            uiLabel.Visible = false;
        }

        void RefreshBuildableList()
        {
            BuildableList.Reset();
            foreach (Building b in Player.GetUnlockedBuildings())
            {
                var item = new BlueprintsBuildableListItem(this, b);
                BuildableList.AddItem(item);
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.ColonyOverview), TitlePos, Colors.Cream);
            LeftMenu.Draw(batch, elapsed);
            RightMenu.Draw(batch, elapsed);
            batch.DrawRectangle(BuildableList.ItemsHousing, Color.Red); // items housing border
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override void Update(float elapsedTime)
        {
            base.Update(elapsedTime);
        }
    }
}

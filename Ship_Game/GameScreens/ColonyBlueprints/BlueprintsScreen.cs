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
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class BlueprintsScreen : GameScreen
    {
        readonly Array<BlueprintsTile> TilesList = new(35);
        readonly string BuildingsTabText = Localizer.Token(GameText.Buildings); // BUILDINGS

        readonly Menu1 LeftMenu;
        readonly Menu1 RightMenu;
        readonly Submenu PlanetInfo;
        readonly Submenu PlanStats;
        readonly UITextEntry PlanetName;
        public EmpireUIOverlay Eui;
        readonly Submenu SubPlanArea;
        bool PlanAreaHovered;
        readonly Rectangle PlanetShieldIconRect;
        readonly ProgressBar PlanetShieldBar;
        readonly UILabel FilterBuildableItemsLabel;
        readonly Planet planetTemplate;

        readonly ScrollList<BlueprintsBuildableListItem> BuildableList;

        Building HoveredBuilding;
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
            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            base.Add(new Menu2(titleRect));
            Vector2 titlePos = new(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.ColonyBlueprintsTitle)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            base.Add(new UILabel(titlePos, GameText.ColonyBlueprintsTitle, Fonts.Laserian14));
            LeftMenu = base.Add(new Menu1(2, titleRect.Y + titleRect.Height + 5, titleRect.Width, ScreenHeight - (titleRect.Y + titleRect.Height) - 7));
            RightMenu = base.Add(new Menu1(titleRect.Right + 10, titleRect.Y, ScreenWidth / 3 - 15, ScreenHeight - titleRect.Y - 2));
            Add(new CloseButton(RightMenu.Right - 52, RightMenu.Y + 22));

            RectF planetInfoR = new(LeftMenu.X + 20, LeftMenu.Y + 20,
                                    (int)(0.4f * LeftMenu.Width),
                                    (int)(0.23f * (LeftMenu.Height - 80)));
            PlanetInfo = new(planetInfoR, GameText.PlanetInfo);
            Submenu pDescription = new(LeftMenu.X + 20, LeftMenu.Y + 40 + PlanetInfo.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));

            RectF planAreaR = new(LeftMenu.X + 20 + PlanetInfo.Width + 20, PlanetInfo.Y,
                                   LeftMenu.Width - 60 - PlanetInfo.Width, LeftMenu.Height * 0.5f);

            SubPlanArea = base.Add(new Submenu(planAreaR, GameText.Colony));




            RectF planetStatsRect = new(LeftMenu.X + 20 + PlanetInfo.Width + 20,
                                        SubPlanArea.Bottom + 20,
                                        LeftMenu.Width - 60 - PlanetInfo.Width,
                                        LeftMenu.Height - 20 - SubPlanArea.Height - 40);

            base.Add(new Submenu(planetStatsRect, GameText.Statistics2));



            RectF buildableMenuR = new(RightMenu.X + 20, RightMenu.Y + 20,
                                   RightMenu.Width - 40, 0.5f * (RightMenu.Height - 40));

            RectF buildableR = new(buildableMenuR.X, buildableMenuR.Y+20, buildableMenuR.W, buildableMenuR.H -20);
            BuildableList = base.Add(new ScrollList<BlueprintsBuildableListItem>(buildableR, 40));
            BuildableList.EnableItemHighlight = true;
            BuildableList.OnDoubleClick = OnBuildableItemDoubleClicked;
            BuildableList.EnableDragOutEvents = true;
            BuildableList.OnDragOut = OnBuildableListDrag;

            int iconSize = LowRes ? 80 : 128;
            int iconOffsetX = LowRes ? 100 : 148;
            int iconOffsetY = LowRes ? 0 : 25;

            /*Rectangle planetShieldBarRect = new Rectangle(PlanetIcon.X, PlanetInfo.Rect.Y + 4, PlanetIcon.Width, 20);
            PlanetShieldBar = new ProgressBar(planetShieldBarRect)
            {
                color = "blue"
            };

            PlanetShieldIconRect = new Rectangle(planetShieldBarRect.X - 30, planetShieldBarRect.Y - 2, 20, 20); */
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

            Rectangle gridPos = new Rectangle(SubPlanArea.Rect.X + 10, SubPlanArea.Rect.Y + 30, 
                                              SubPlanArea.Rect.Width - 20, SubPlanArea.Rect.Height - 35);
            CreateBlueprintsTiles(gridPos);
            RefreshBuildableList();
        }

        void CreateBlueprintsTiles(Rectangle gridPos)
        {
            int width = gridPos.Width / SolarSystemBody.TileMaxX;
            int height = gridPos.Height / SolarSystemBody.TileMaxY;

            for (int y = 0; y < SolarSystemBody.TileMaxY; y++)
                for (int x = 0; x < SolarSystemBody.TileMaxX; x++)
                {
                    UIPanel panel = base.Add(new UIPanel(new Rectangle(gridPos.X + x * width, gridPos.Y + y * height, width, height), Color.White));
                    panel.Tooltip = GameText.RightClickToRemove;
                    TilesList.Add(new BlueprintsTile(panel));
                }
        }

        void OnBuildableItemDoubleClicked(BlueprintsBuildableListItem item)
        {
            BlueprintsTile tile = TilesList.Find(t => t.IsFree);
            if (tile != null)
            {
                tile.AddBuilding(item.Building);
                RefreshBuildableList();
            }
            else
            {
                GameAudio.NegativeClick();
            }

        }

        void RefreshBuildableList()
        {
            BuildableList.Reset();
            AddOutpost();
            foreach (Building b in Player.GetUnlockedBuildings())
            {
                if (b.IsSuitableForBlueprints && !TilesList.Any(t => t.BuildingNameExists(b.Name)))
                {
                    var item = new BlueprintsBuildableListItem(this, b);
                    BuildableList.AddItem(item);
                }
            }
        }

        void AddOutpost()
        {
            Building outpost = ResourceManager.GetBuildingTemplate(Building.OutpostId);
            if (outpost != null)
            {
                TilesList[0].AddBuilding(outpost);
                TilesList[0].Panel.Tooltip = null;
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
            if (PlanAreaHovered)
            {
                Rectangle areaHoverRect = new((int)SubPlanArea.X+2, (int)SubPlanArea.Y+2, (int)SubPlanArea.Width-4, (int)SubPlanArea.Height-4);
                batch.DrawRectangle(areaHoverRect, Color.White, 2f);
            }

            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override void Update(float elapsedTime)
        {
            base.Update(elapsedTime);
        }

        public override bool HandleInput(InputState input)
        {
            HoveredBuilding = GetHoveredBuildingFromBuildableList(input);
            foreach (BlueprintsTile tile in TilesList)
            {
                if (tile.HasBuilding && tile.Panel.HitTest(input.CursorPosition))
                {
                    HoveredBuilding = tile.Building;
                    tile.Panel.Color = tile.Building.IsCapitalOrOutpost ? Color.Red : Color.Orange;
                    if (Input.RightMouseClick)
                    {
                        if (!tile.Building.IsCapitalOrOutpost)
                        {
                            tile.RemoveBuilding();
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
                    tile.Panel.Color = Color.White;
                }
            }

            return base.HandleInput(input);
        }

        void OnBuildableListDrag(BlueprintsBuildableListItem item, DragEvent evt, bool outside)
        {
            bool inPlanArea = SubPlanArea.HitTest(Input.CursorPosition);
            if (inPlanArea)
            {
                PlanAreaHovered = inPlanArea;
            }

            if (evt != DragEvent.End)
                return;

            if (outside && item != null) // TODO: somehow `item` can be null, not sure how it happens
            {
                if (inPlanArea)
                    OnBuildableItemDoubleClicked(item);
                else
                    GameAudio.NegativeClick();

                return;
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

        public override void ExitScreen()
        {
            TilesList.Clear();
            base.ExitScreen();
        }
    }
}

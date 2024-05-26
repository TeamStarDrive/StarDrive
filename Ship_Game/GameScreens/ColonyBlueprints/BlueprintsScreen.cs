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
using Ship_Game.Ships;
using Ship_Game.Audio;
using static System.Net.Mime.MediaTypeNames;
using Font = Ship_Game.Graphics.Font;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Ship_Game
{
    public partial class BlueprintsScreen : GameScreen
    {
        readonly Array<BlueprintsTile> TilesList = new(35);

        readonly Menu1 LeftMenu;
        readonly Menu1 RightMenu;
        readonly Submenu SubBlueprintsOptions;
        readonly Submenu PlanStats;
        readonly Submenu SubExperimentalParameters;
        readonly UILabel BlueprintsName;
        readonly UICheckBox ExclusiveCheckbox;
        public bool Exclusive;
        readonly Submenu SubPlanArea;
        public bool PlanAreaHovered { get; private set; }
        readonly Rectangle PlanetShieldIconRect;
        readonly ProgressBar PlanetShieldBar;
        readonly UILabel FilterBuildableItemsLabel;
        readonly FloatSlider InitPopulationSlider;
        float InitPopulationBillion = 5;

        readonly ScrollList<BlueprintsBuildableListItem> BuildableList;
        readonly DropOptions<Planet.ColonyType> SwitchColonyType;
        readonly DropOptions<string> LinkBlueprints;

        Building HoveredBuilding;
        readonly Font Font8 = Fonts.Arial8Bold;
        readonly Font Font12 = Fonts.Arial12Bold;
        readonly Font Font14 = Fonts.Arial14Bold;
        readonly Font Font20 = Fonts.Arial20Bold;
        readonly Font TextFont;
        public readonly Empire Player;

        public BlueprintsScreen(UniverseScreen parent, Empire player) : base(parent, toPause: parent)
        {
            Player = player;
            TextFont = LowRes ? Font8 : Font12;
            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            base.Add(new Menu2(titleRect));
            Vector2 titlePos = new(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.ColonyBlueprintsTitle)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
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
                                   RightMenu.Width - 40, 0.5f * (RightMenu.Height - 40));
            base.Add(new Submenu(buildableMenuR, GameText.Buildings));

            RectF experimentalR = new(LeftMenu.X + 20, LeftMenu.Y + 40 + SubBlueprintsOptions.Height, 0.4f * LeftMenu.Width, 0.25f * (LeftMenu.Height - 80));
            base.Add(new Submenu(experimentalR, GameText.Buildings));


            float blueprintsOptionsX = SubBlueprintsOptions.X + 10;
            BlueprintsName = base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 45), 
                "New Bluebrints", Font14, Color.Gold));
            ExclusiveCheckbox = base.Add(new UICheckBox(blueprintsOptionsX, SubBlueprintsOptions.Y + 65 + Font14.LineSpacing,
                () => Exclusive, TextFont, GameText.ExclusiveBlueprints, GameText.ExclusiveBlueprintsTip));
            ExclusiveCheckbox.TextColor = Color.Wheat;

            base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 75 + Font14.LineSpacing * 3),
                "Link Blueprints to:", TextFont, Color.Wheat, GameText.ExclusiveBlueprintsTip));
            LinkBlueprints = base.Add(Add(new DropOptions<string>(blueprintsOptionsX + 150, SubBlueprintsOptions.Y + 75 + Font14.LineSpacing * 3, 200, 18)));
            LinkBlueprints.AddOption(option: "--", "");

            base.Add(new UILabel(new Vector2(blueprintsOptionsX, SubBlueprintsOptions.Y + 70 + Font14.LineSpacing * 2), 
                "Switch Governor to:", TextFont, Color.Wheat, GameText.ExclusiveBlueprintsTip));
            SwitchColonyType = base.Add(Add(new DropOptions<Planet.ColonyType>(blueprintsOptionsX+150, SubBlueprintsOptions.Y + 70 + Font14.LineSpacing*2, 100, 18)));
            SwitchColonyType.AddOption(option: "--", Planet.ColonyType.Colony);
            SwitchColonyType.AddOption(option: GameText.Core, Planet.ColonyType.Core);
            SwitchColonyType.AddOption(option: GameText.Industrial, Planet.ColonyType.Industrial);
            SwitchColonyType.AddOption(option: GameText.Agricultural, Planet.ColonyType.Agricultural);
            SwitchColonyType.AddOption(option: GameText.Research, Planet.ColonyType.Research);
            SwitchColonyType.AddOption(option: GameText.Military, Planet.ColonyType.Military);
            SwitchColonyType.ActiveValue = Planet.ColonyType.Colony;

            RectF initPopR = new(blueprintsOptionsX, experimentalR.Y + 20, 270, 50);
            InitPopulationSlider = SliderDecimal1(initPopR, GameText.Population, 0, 20, InitPopulationBillion);
            InitPopulationSlider.OnChange = (s) => InitPopulationBillion = (s.AbsoluteValue).RoundToFractionOf10();


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
            base.Draw(batch, elapsed);
            DrawHoveredBuildListBuildingInfo(batch);
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
            ColonyScreen.DrawBuildingStaticInfo(ref bCursor, batch, TextFont, Player, fertility: 1, richness: 1, Player.data.Traits.PreferredEnv, HoveredBuilding);
            ColonyScreen.DrawBuildingInfo(ref bCursor, batch, TextFont, HoveredBuilding.ActualShipRepairBlueprints(3), "NewUI/icon_queue_rushconstruction", GameText.ShipRepair);

            //DrawBuildingInfo(ref bCursor, batch, font, b.ActualShipRepair(P), "NewUI/icon_queue_rushconstruction", GameText.ShipRepair);
            //if (selectedBuilding.IsWeapon)
            //    selectedBuilding.CalcMilitaryStrength(P); // So the building will have TheWeapon for stats

            ColonyScreen.DrawBuildingWeaponStats(ref bCursor, batch, TextFont, HoveredBuilding, planetLevel: 3);
        }

        public override void Update(float elapsedTime)
        {
            base.Update(elapsedTime);
        }

        public override bool HandleInput(InputState input)
        {
            PlanAreaHovered = BuildableList.IsDragging && SubPlanArea.HitTest(Input.CursorPosition);
            HoveredBuilding = GetHoveredBuildingFromBuildableList(input);
            foreach (BlueprintsTile tile in TilesList)
            {
                if (tile.HasBuilding && tile.Panel.HitTest(input.CursorPosition))
                {
                    HoveredBuilding = BuildableList.IsDragging ? null : tile.Building;
                    if (HoveredBuilding != null)
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
            if (evt != DragEvent.End)
                return;

            if (outside && item != null) // TODO: somehow `item` can be null, not sure how it happens
            {
                if (PlanAreaHovered)
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

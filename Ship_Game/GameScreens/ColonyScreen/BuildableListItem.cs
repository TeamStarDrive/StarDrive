using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class BuildableListItem : ScrollListItem<BuildableListItem>
    {
        public readonly ColonyScreen Screen;
        public Building Building;
        public Ship Ship;
        public Troop Troop;
        string BuildingDescr;
        readonly SubTexture ProdIcon = ResourceManager.Texture("NewUI/icon_production");
        readonly SpriteFont Font8 = Fonts.Arial8Bold;
        readonly SpriteFont Font12 = Fonts.Arial12Bold;

        public BuildableListItem(ColonyScreen screen, string headerText) : base(headerText)
        {
            Screen = screen;
        }
        public BuildableListItem(ColonyScreen screen, Building b) : this(screen, false, false)
        {
            Building = b;
        }
        public BuildableListItem(ColonyScreen screen, Ship s) : this(screen, true, true)
        {
            Ship = s;
        }
        public BuildableListItem(ColonyScreen screen, Troop t) : this(screen, true, false)
        {
            Troop = t;
        }

        BuildableListItem(ColonyScreen screen, bool plus, bool edit)
        {
            Screen = screen;
            if (plus) AddPlus(new Vector2(-50, 0), /*Add to Q:*/51, OnPlusClicked);
            if (edit) AddEdit(new Vector2(-20, 0), /*Edit Ship:*/52, OnEditClicked);
        }

        void OnPlusClicked()
        {
            int repeat = 1;
            if (Screen.Input.IsShiftKeyDown)     repeat = 5;
            else if (Screen.Input.IsCtrlKeyDown) repeat = 10;
            BuildIt(repeat);
        }

        void OnEditClicked()
        {
            if (Ship != null)
            {
                var sdScreen = new ShipDesignScreen(Empire.Universe, Screen.eui);
                Screen.ScreenManager.AddScreen(sdScreen);
                sdScreen.ChangeHull(Ship.shipData);
            }
        }

        public void BuildIt(int numItemsToBuild = 1)
        {
            if (Building != null) Screen.Build(Building);
            else if (Ship != null) Screen.Build(Ship, numItemsToBuild);
            else if (Troop != null) Screen.Build(Troop, numItemsToBuild);
        }

        public override bool HandleInput(InputState input)
        {
            bool captured = base.HandleInput(input);
            if (Hovered)
            {
                if (Screen.ActiveBuildingEntry == null && Building != null && input.LeftMouseHeld(0.1f))
                    Screen.ActiveBuildingEntry = this;

                Screen.ShowSelectedShipOverlay(Pos, Ship);
            }
            return captured;
        }

        public override void Update(float deltaTime)
        {
            Troop?.Update(deltaTime);
            base.Update(deltaTime);
        }

        // Give a custom height for this scroll list item
        public override int ItemHeight => 40;

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
            if   (Building != null)  DrawBuilding(batch, Building);
            else if (Troop != null)  DrawTroop(batch, Troop);
            else if (Ship != null)   DrawShip(batch, Ship);
        }

        void DrawProductionInfo(SpriteBatch batch, float maintenance, float cost)
        {
            float upkeepY = CenterY - Font12.LineSpacing/2f - 4;
            if (maintenance >= 0f)
                batch.DrawString(Font12, maintenance.String(2)+" BC/Y", Right-184, upkeepY, Color.Salmon); // Maintenance
            batch.Draw(ProdIcon, Right - 120, CenterY - ProdIcon.CenterY - 4); // Production Icon
            batch.DrawString(Font12, cost.String(), Right-92, upkeepY); // Build Cost
        }

        void DrawBuilding(SpriteBatch batch, Building b)
        {
            Planet p = Screen.P;
            bool unprofitable = !p.WeCanAffordThis(b, p.colonyType) && b.Maintenance > 0f;
            Color buildColor  = Hovered ? Color.White  : unprofitable ? new Color(255,200,200) : Color.White;
            Color profitColor = Hovered ? Color.Orange : unprofitable ? Color.Chocolate : Color.Green;

            if (BuildingDescr == null)
            {
                string text = BuildingShortDescription(b) + (unprofitable ? " (unprofitable)" : "");
                BuildingDescr = Font8.ParseText(text, 280f);
            }

            batch.Draw(b.IconTex, new Vector2(X+4, Y+4), new Vector2(32), buildColor); // Icon
            batch.DrawString(Font12, Localizer.Token(b.NameTranslationIndex), X+44, Y+4, buildColor); // Title
            batch.DrawString(Font8, BuildingDescr, X+46, Y+20, profitColor); // Description
            DrawProductionInfo(batch, GetMaintenance(b), b.ActualCost);
        }

        void DrawTroop(SpriteBatch batch, Troop troop)
        {
            troop.Draw(batch, new Vector2(X+4, Y+4), new Vector2(32)); // Icon
            batch.DrawString(Font12, troop.DisplayNameEmpire(Screen.P.Owner), X+44, Y+4); // Title
            batch.DrawString(Font8, troop.Class, X+46, Y+20, Color.Orange); // Description
            DrawProductionInfo(batch, -1, troop.ActualCost);
        }

        void DrawShip(SpriteBatch batch, Ship ship)
        {
            // Everything from Left --> to --> Right 
            batch.Draw(ship.BaseHull.Icon, new Vector2(X, Y), new Vector2(48)); // Icon
            batch.DrawString(Font12, GetShipName(ship), X+60, Y+4, Hovered ? Color.Green : Color.White); // Title
            batch.DrawLine(Font8, X+60, Y+20, 
                (ship.BaseHull.Name+": ", Color.DarkGray),
                ($"Base Strength: {ship.BaseStrength.String(0)}", Color.Orange)); // Description
            DrawProductionInfo(batch, GetShipUpkeep(ship), GetShipCost(ship));
        }

        static string GetShipName(Ship ship)
        {
            return ship.IsPlatformOrStation ? ship.Name + " " + Localizer.Token(2041)
                                            : ship.Name;
        }

        float GetShipUpkeep(Ship ship)
        {
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                return ship.GetMaintCostRealism(Screen.P.Owner);
            return ship.GetMaintCost(Screen.P.Owner);
        }

        int GetShipCost(Ship ship)
        {
            return (int)(ship.GetCost(Screen.P.Owner) * Screen.P.ShipBuildingModifier);
        }

        float GetMaintenance(Building b) => b.Maintenance + b.Maintenance * Screen.P.Owner.data.Traits.MaintMod;

        string BuildingShortDescription(Building b)
        {
            string description = Localizer.Token(b.ShortDescriptionIndex);

            Planet p = Screen.P;
            if (b.MaxFertilityOnBuild.NotZero())
            {
                string fertilityChange = $"{b.MaxFertilityOnBuild * Screen.Player.RacialEnvModifer(p.Category)}";
                if (b.MaxFertilityOnBuild.Greater(0))
                    fertilityChange = $"+{fertilityChange}";
                description = $"{fertilityChange} {description}";
            }

            if (b.IsBiospheres)
                description = $"{(p.BasePopPerTile/1000).String(2) } {description}";
            
            return description;
        }
    }
}

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
        readonly SubTexture CostIcon = ResourceManager.Texture("UI/icon_money_22");
        readonly SpriteFont Font8 = Fonts.Arial8Bold;
        readonly SpriteFont Font12 = Fonts.Arial12Bold;
        readonly bool LowRes;

        public BuildableListItem(ColonyScreen screen, string headerText) : base(headerText)
        {
            Screen = screen;
            LowRes = screen.LowRes;
        }
        public BuildableListItem(ColonyScreen screen, Building b) : this(screen, false, false)
        {
            Building = b;
        }
        public BuildableListItem(ColonyScreen screen, Ship s, bool edit = true) : this(screen, true, edit)
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
            LowRes = screen.LowRes;
            if (plus) AddPlus(new Vector2(LowRes ? -36 : -50, 0), /*Add to Q: */51, OnPlusClicked);
            if (edit) AddEdit(new Vector2(LowRes ? -14 : -20, 0), /*Edit Ship:*/52, OnEditClicked);
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
            // Note: for ships, the queue item code knows how to handle production cost modifiers
            if (Building != null)   Screen.Build(Building);
            else if (Ship != null)  Screen.Build(Ship, Ship.GetCost(Screen.P.Owner), numItemsToBuild);
            else if (Troop != null) Screen.Build(Troop, numItemsToBuild);
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            Troop?.Update(fixedDeltaTime);
            base.Update(fixedDeltaTime);
        }

        // Give a custom height for this scroll list item
        public override int ItemHeight => LowRes ? 32 : 42;

        float IconSize  => LowRes ? 36 : 48;
        float ProdWidth => LowRes ? 90 : 120;
        float TextWidth => Width - IconSize - ProdWidth;
        float TextX     => X + IconSize;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if   (Building != null)  DrawBuilding(batch, Building);
            else if (Troop != null)  DrawTroop(batch, Troop);
            else if (Ship != null)   DrawShip(batch, Ship);
        }

        void DrawProductionInfo(SpriteBatch batch, float maintenance, float prod, int cost = 0)
        {
            SpriteFont font = LowRes ? Fonts.Arial10 : Font12;
            float x = Right - ProdWidth;
            float y = Y+4;
            var iconSize = new Vector2(font.LineSpacing+2);
            batch.Draw(ProdIcon, new Vector2(x, y), iconSize); // Production Icon
            batch.DrawString(font, prod.String(), x + iconSize.X + 2, y); // Build Production Cost

            string maintString = maintenance.String(2) + " BC/Y";
            float maintX       = x + iconSize.X + 50;

            if (cost > 0)
            {
                batch.Draw(CostIcon, new Vector2(x, y + iconSize.Y + 5), iconSize); // Credits Icon
                batch.DrawString(font, cost.String(), x + iconSize.X + 2, y + iconSize.Y + 5); // Build Credit Cost
            }

            if (maintenance > 0f)
                batch.DrawString(Font8, maintString, maintX, y + iconSize.Y + 6, Color.DarkRed); // Maintenance
        }

        void DrawBuilding(SpriteBatch batch, Building b)
        {
            Planet p = Screen.P;
            bool unprofitable = !p.WeCanAffordThis(b, p.colonyType) && b.Maintenance > 0f;
            Color buildColor  = Hovered ? Color.White  : unprofitable ? new Color(255,200,200) : Color.White;
            Color profitColor = Hovered ? Color.Orange : unprofitable ? Color.Chocolate : Color.Green;

            if (BuildingDescr == null)
                BuildingDescr = Font8.ParseText(BuildingShortDescription(b), TextWidth);

            batch.Draw(b.IconTex, new Vector2(X, Y-2), new Vector2(IconSize), buildColor); // Icon
            batch.DrawString(Font12, Localizer.Token(b.NameTranslationIndex), TextX+2, Y+2, buildColor); // Title
            batch.DrawString(Font8, BuildingDescr, TextX+4, Y+16, profitColor); // Description
            int creditCost = b.IsMilitary ? GetCreditCharge((int)b.ActualCost) : 0;
            DrawProductionInfo(batch, GetMaintenance(b), b.ActualCost, creditCost);
        }

        void DrawTroop(SpriteBatch batch, Troop troop)
        {
            troop.Draw(batch, new Vector2(X, Y-2), new Vector2(IconSize)); // Icon
            batch.DrawString(Font12, troop.DisplayNameEmpire(Screen.P.Owner), TextX+2, Y+2); // Title
            batch.DrawString(Font8, troop.Class, TextX+4, Y+16, Color.Orange); // Description
            DrawProductionInfo(batch, -1, troop.ActualCost);
        }

        void DrawShip(SpriteBatch batch, Ship ship)
        {
            // Everything from Left --> to --> Right 
            batch.Draw(ship.BaseHull.Icon, new Vector2(X, Y-2), new Vector2(IconSize)); // Icon
            batch.DrawString(Font12, GetShipName(ship), TextX+2, Y+2, Hovered ? Color.Green : Color.White); // Title
            batch.DrawLine(Font8, TextX+4, Y+16, 
                (ship.BaseHull.Name, Color.DarkGray), ($" strength:{ship.BaseStrength.String(0)}", Color.Orange)); // Description

            int shipProdCost = GetShipProdCost(ship);
            int shipCost     = GetCreditCharge(shipProdCost);
            DrawProductionInfo(batch, GetShipUpkeep(ship), shipProdCost, shipCost);
        }

        static string GetShipName(Ship ship)
        {
            return ship.IsPlatformOrStation ? ship.Name + " " + Localizer.Token(2041)
                                            : ship.Name;
        }

        float GetShipUpkeep(Ship ship)
        {
            return ship.GetMaintCost(Screen.P.Owner);
        }

        int GetShipProdCost(Ship ship)
        {
            return (int)(ship.GetCost(Screen.P.Owner) * Screen.P.ShipBuildingModifier);
        }

        int GetCreditCharge(float cost)
        {
            return Screen.P.Owner.EstimateCreditCost(cost);
        }

        float GetMaintenance(Building b) => b.ActualMaintenance(Screen.P);

        string BuildingShortDescription(Building b)
        {
            string description = Localizer.Token(b.ShortDescriptionIndex);

            Planet p = Screen.P;
            if (b.MaxFertilityOnBuild.NotZero())
            {
                string fertilityChange = $"{b.MaxFertilityOnBuild * Screen.Player.PlayerEnvModifier(p.Category)}";
                if (b.MaxFertilityOnBuild.Greater(0))
                    fertilityChange = $"+{fertilityChange}";
                description = $"{fertilityChange} {description}";
            }

            if (b.IsBiospheres)
                description = $"{(p.PopPerBiosphere(EmpireManager.Player)/1000).String(2) } {description}";
            
            return description;
        }
    }
}

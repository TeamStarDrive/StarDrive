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
        SubTexture ProdIcon = ResourceManager.Texture("NewUI/icon_production");

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

        void DrawBuilding(SpriteBatch batch, Building b)
        {
            SubTexture icon = ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48");
            SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

            Planet p = Screen.P;
            SpriteFont Font8 = Fonts.Arial8Bold;
            SpriteFont Font12 = Fonts.Arial12Bold;

            bool unprofitable = !p.WeCanAffordThis(b, p.colonyType) && b.Maintenance > 0f;
            Color buildColor = unprofitable ? new Color(255,200,200) : Color.White;
            if (Hovered) buildColor = Color.White; // hover color
            string descr = BuildingShortDescription(b) + (unprofitable ? " (unprofitable)" : "");
            descr = Font8.ParseText(descr, 280f);

            var position = new Vector2(X + 60f, Y);

            batch.Draw(icon, new Rectangle((int)X + 12, (int)Y + 4, 32, 32), buildColor);
            batch.DrawString(Font12, Localizer.Token(b.NameTranslationIndex), position, buildColor);
            position.Y += Font12.LineSpacing;

            if (!Hovered)
            {
                batch.DrawString(Font8, descr, position, unprofitable ? Color.Chocolate : Color.Green);
                position.X = (Right - 100);
                var r = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5,
                    iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, r, Color.White);

                position = new Vector2((r.X - 60), (1 + r.Y + r.Height / 2 - Font12.LineSpacing / 2));
                string maintenance = b.Maintenance.ToString("F2");
                batch.DrawString(Font8, maintenance+" BC/Y", position, Color.Salmon);

                position = new Vector2((r.X + 26), (r.Y + r.Height / 2 - Font12.LineSpacing / 2));
                batch.DrawString(Font12, b.ActualCost.String(), position, Color.White);
            }
            else
            {
                batch.DrawString(Font8, descr, position, Color.Orange);
                position.X = (Right - 100);
                var r = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5,
                    iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, r, Color.White);

                position = new Vector2((r.X - 60), (1 + r.Y + r.Height / 2 - Font12.LineSpacing / 2));
                float actualMaint = b.Maintenance + b.Maintenance * p.Owner.data.Traits.MaintMod;
                string maintenance = actualMaint.ToString("F2");
                batch.DrawString(Font8, maintenance+" BC/Y", position, Color.Salmon);

                position = new Vector2((r.X + 26), (r.Y + r.Height / 2 - Font12.LineSpacing / 2));
                batch.DrawString(Font12, b.ActualCost.String(), position, Color.White);
            }
        }

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

        void DrawTroop(SpriteBatch batch, Troop troop)
        {
            Planet p = Screen.P;
            SpriteFont Font8 = Fonts.Arial8Bold;
            SpriteFont Font12 = Fonts.Arial12Bold;

            SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");
            var tl = new Vector2(List.X + 20, Y);

            if (!Hovered)
            {
                troop.Draw(batch, new Rectangle((int) tl.X, (int) tl.Y, 29, 30));
                var position = new Vector2(tl.X + 40f, tl.Y + 3f);
                batch.DrawString(Font12, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                position.Y += Font12.LineSpacing;
                batch.DrawString(Fonts.Arial8Bold, troop.Class, position, Color.Orange);

                position.X = Right - 100;
                var dest2 = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5, iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, dest2, Color.White);
                position = new Vector2(dest2.X + 26, dest2.Y + dest2.Height / 2 - Font12.LineSpacing / 2);
                batch.DrawString(Font12, ((int)troop.ActualCost).ToString(), position, Color.White);
            }
            else
            {
                troop.Draw(batch, new Rectangle((int) tl.X, (int) tl.Y, 29, 30));
                Vector2 position = new Vector2(tl.X + 40f, tl.Y + 3f);
                batch.DrawString(Font12, troop.DisplayNameEmpire(p.Owner), position, Color.White);
                position.Y += Font12.LineSpacing;
                batch.DrawString(Font8, troop.Class, position, Color.Orange);
                position.X = Right - 100;
                Rectangle destinationRectangle2 = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5,
                    iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, destinationRectangle2, Color.White);
                position = new Vector2(destinationRectangle2.X + 26,
                    destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                    Font12.LineSpacing / 2);
                batch.DrawString(Font12, ((int) troop.ActualCost).ToString(), position, Color.White);
            }
        }

        void DrawShip(SpriteBatch batch, Ship ship)
        {
            SpriteFont Font12 = Fonts.Arial12Bold;

            // Everything from Left --> to --> Right 
            batch.Draw(ship.BaseHull.Icon, new Vector2(X+4, Y+4), new Vector2(32));
            batch.DrawString(Font12, GetShipName(ship), X+44, Y+4, Hovered ? Color.Green : Color.White);
            batch.DrawLine(Fonts.Arial8Bold, X+46, Y+20, 
                (ship.BaseHull.Name+": ", Color.DarkGray),
                ($"Base Strength: {ship.BaseStrength.String(0)}", Color.Orange));

            float upkeepY = CenterY - Font12.LineSpacing/2f - 4;
            batch.DrawString(Font12, GetShipUpkeep(ship).String(2)+" BC/Y", Right-184, upkeepY, Color.Salmon);
            batch.DrawString(Font12, GetShipCost(ship).ToString(), Right-92, upkeepY);
            batch.Draw(ProdIcon, Right - 120, CenterY - ProdIcon.CenterY - 4);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class BuildListItem : ScrollList<BuildListItem>.Entry
    {
        public readonly ColonyScreen Screen;

        public Building Building;
        public Ship Ship;
        public Troop Troop;

        public BuildListItem(ColonyScreen screen, string headerText) : base(headerText)
        {
            Screen = screen;
        }
        public BuildListItem(ColonyScreen screen) : this(screen, false, false)
        {
        }
        public BuildListItem(ColonyScreen screen, bool plusAndEdit) : this(screen, plusAndEdit, plusAndEdit)
        {
            // 50: Rush Production
            // 53: CancelProduction
        }
        public BuildListItem(ColonyScreen screen, bool plus, bool edit)
        {
            Screen = screen;
            if (plus) AddPlus(new Vector2(-60, 0), /*Add to Q:*/51, OnPlusClicked);
            if (edit) AddEdit(new Vector2(-30, 0), /*Edit Ship:*/52, OnEditClicked);
        }

        void OnPlusClicked()
        {
            int repeat = 1;
            if (Screen.Input.IsShiftKeyDown)     repeat = 5;
            else if (Screen.Input.IsCtrlKeyDown) repeat = 10;

            if (Building != null) Screen.Build(Building);
            else if (Ship != null) Screen.Build(Ship, repeat);
            else if (Troop != null) Screen.Build(Troop, repeat);
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

        public override bool HandleInput(InputState input)
        {
            bool captured = base.HandleInput(input);
            if (Hovered)
            {
                if (Screen.ActiveBuildingEntry == null && Building != null && input.LeftMouseHeld(0.1f))
                    Screen.ActiveBuildingEntry = this;
            }
            return captured;
        }

        public override void Update(float deltaTime)
        {
            Troop?.Update(deltaTime);
        }

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

            var position = new Vector2(List.X + 60f, Y - 4f);

            batch.Draw(icon, new Rectangle((int)X, (int)Y, 29, 30), buildColor);
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
            Planet p = Screen.P;
            SpriteFont Font8 = Fonts.Arial8Bold;
            SpriteFont Font12 = Fonts.Arial12Bold;
            var topLeft =  new Vector2(List.X + 20, Y);

            if (!Hovered)
            {
                batch.Draw(ship.BaseHull.Icon, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30), Color.White);
                var position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                batch.DrawString(Font12,
                    (ship.IsPlatformOrStation ? ship.Name + " " + Localizer.Token(2041) : ship.Name), position, Color.White);
                position.Y += Font12.LineSpacing;

                var role = ship.BaseHull.Name;
                batch.DrawString(Font8, role + ": ", position, Color.DarkGray);
                position.X = position.X + Font8.MeasureString(role).X + 8;
                batch.DrawString(Font8,
                    $"Base Strength: {ship.BaseStrength.String(0)}", position, Color.Orange);


                //Forgive my hacks this code of nightmare must GO!
                position.X = (Right - 120);
                var iconProd = ResourceManager.Texture("NewUI/icon_production");
                var destinationRectangle2 = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5,
                    iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, destinationRectangle2, Color.White);

                // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                position = new Vector2((destinationRectangle2.X - 60),
                    (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
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

                batch.DrawString(Font8, upkeep+" BC/Y", position, Color.Salmon);

                // ~~~

                position = new Vector2(destinationRectangle2.X + 26, destinationRectangle2.Y + destinationRectangle2.Height / 2 -
                    Font12.LineSpacing / 2);
                batch.DrawString(Font12, ((int) (ship.GetCost(p.Owner) * p.ShipBuildingModifier)).ToString(), position, Color.White);
            }
            else
            {
                batch.Draw(ship.BaseHull.Icon, new Rectangle((int) topLeft.X, (int) topLeft.Y, 29, 30), Color.White);
                Vector2 position = new Vector2(topLeft.X + 40f, topLeft.Y + 3f);
                batch.DrawString(Font12,
                    ship.IsPlatformOrStation
                        ? ship.Name + " " + Localizer.Token(2041)
                        : ship.Name, position, Color.Green);
                position.Y += Font12.LineSpacing;
                //var role = Localizer.GetRole(ship.shipData.HullRole, EmpireManager.Player);
                var role = ship.BaseHull.Name;
                batch.DrawString(Font8, role + ": ", position, Color.DarkGray);
                position.X = position.X + Font8.MeasureString(role).X + 8;
                batch.DrawString(Font8,
                    $"Base Strength: {ship.BaseStrength.String(0)}", position, Color.Orange);

                position.X = (Right - 120);
                SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");
                var destinationRectangle2 = new Rectangle((int) position.X, (int)CenterY - iconProd.Height / 2 - 5,
                    iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, destinationRectangle2, Color.White);

                // The Doctor - adds new UI information in the build menus for the per tick upkeep of ship

                position = new Vector2((destinationRectangle2.X - 60),
                    (1 + destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
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

                batch.DrawString(Font8, upkeep+" BC/Y", position, Color.Salmon);

                // ~~~

                position = new Vector2((destinationRectangle2.X + 26),
                    (destinationRectangle2.Y + destinationRectangle2.Height / 2 - Font12.LineSpacing / 2));
                batch.DrawString(Font12, ((int) (ship.GetCost(p.Owner) * p.ShipBuildingModifier)).ToString(), position, Color.White);


                //DrawPlusEdit(batch);

                Screen.DrawSelectedShipInfo((int)position.X, (int)CenterY, ship, batch);
            }
        }
    }
}

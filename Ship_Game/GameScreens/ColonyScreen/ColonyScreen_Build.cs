using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        Ship SelectedShip;
        bool ResetBuildableList;

        readonly string BuildingsTabText = Localizer.Token(334); // BUILDINGS
        readonly string ShipsTabText = Localizer.Token(335); // SHIPS
        readonly string TroopsTabText = Localizer.Token(336); // TROOPS

        void OnBuildableTabChanged(int tabIndex)
        {
            PlayerDesignsToggle.Visible = BuildableTabs.IsSelected(ShipsTabText);
            ResetBuildableList = true;
        }

        void OnPlayerDesignsToggleClicked(ToggleButton button)
        {
            GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
            PlayerDesignsToggle.Enabled = GlobalStats.ShowAllDesigns;
            ResetBuildableList = true;
        }

        void ResetBuildableTabs()
        {
            int selected = BuildableTabs.SelectedIndex;

            BuildableTabs.Clear();
            BuildableTabs.AddTab(BuildingsTabText);
            if (P.HasSpacePort)     BuildableTabs.AddTab(ShipsTabText);
            if (P.CanBuildInfantry) BuildableTabs.AddTab(TroopsTabText);

            BuildableTabs.SelectedIndex = selected;
        }

        void UpdateBuildAndConstructLists(float elapsedTime)
        {
            if (P.HasSpacePort     && !BuildableTabs.ContainsTab(ShipsTabText) ||
                P.CanBuildInfantry && !BuildableTabs.ContainsTab(TroopsTabText))
            {
                ResetBuildableTabs();
            }

            if (BuildableTabs.IsSelected(BuildingsTabText))
            {
                ResetBuildableList |= BuildableList.NumEntries != P.GetBuildingsCanBuild().Count;
                if (ResetBuildableList)
                {
                    Building[] buildings = P.GetBuildingsCanBuild().Sorted(b => b.Name);
                    BuildableList.SetItems(buildings.Select(b => new BuildableListItem(this, b)));
                }
            }
            else if (BuildableTabs.IsSelected(ShipsTabText))
            {
                // NOTE: Ships list is hierarchical, so checking if buildable ships list
                //       changed is also more complicated
                TryPopulateBuildableShips();
            }
            else if (BuildableTabs.IsSelected(TroopsTabText))
            {
                string[] troopTypes = P.Owner.GetTroopsWeCanBuild();
                ResetBuildableList |= BuildableList.NumEntries != troopTypes.Length;
                if (ResetBuildableList)
                {
                    Troop[] troopTemplates = troopTypes.Select(ResourceManager.GetTroopTemplate);
                    BuildableList.SetItems(troopTemplates.Select(t => new BuildableListItem(this, t)));
                }
            }

            if (!ConstructionQueue.AllEntries.EqualElements(P.ConstructionQueue))
            {
                ConstructionQueue.SetItems(P.ConstructionQueue);
            }

            ResetBuildableList = false;
        }

        class ShipCategory
        {
            public string Name;
            public readonly Array<Ship> Ships = new Array<Ship>();
            public int Size;
            public override string ToString() => $"Category {Name} Size={Size} Count={Ships.Count}";
        }

        ShipCategory[] BuildableShipHierarchy = Empty<ShipCategory>.Array;

        bool BuildableShipsChanged(ShipCategory[] newHierarchy)
        {
            return !BuildableShipHierarchy.EqualElements(newHierarchy, (catA, catB) =>
            {
                return catA.Name == catB.Name
                    && catA.Ships.EqualElements(catB.Ships, (shipA, shipB) => shipA.Name == shipB.Name);
            });
        }

        void TryPopulateBuildableShips()
        {
            Ship[] buildableShips = P.Owner.ShipsWeCanBuild
                .Select(shipName => ResourceManager.GetShipTemplate(shipName))
                .Where(ship => ship.IsBuildableByPlayer).ToArray();

            var categoryMap = new Map<string, ShipCategory>();

            foreach (Ship ship in buildableShips)
            {
                string name = Localizer.GetRole(ship.DesignRole, P.Owner);
                if (!categoryMap.TryGetValue(name, out ShipCategory c))
                {
                    c = new ShipCategory{ Name = name, Size = ship.SurfaceArea };
                    categoryMap.Add(name, c);
                }
                c.Ships.Add(ship);
            }

            // first sort the categories by name:
            ShipCategory[] categories = categoryMap.Values.ToArray().Sorted(c => c.Name);
            foreach (ShipCategory category in categories)
            {
                category.Ships.Sort((a, b) => // rank better ships as first:
                {
                    float diff = b.BaseStrength - a.BaseStrength;
                    if (diff.NotEqual(0)) return (int)diff;
                    return string.CompareOrdinal(b.Name, a.Name);
                });
            }

            if (!BuildableShipsChanged(categories))
                return;
            
            BuildableShipHierarchy = categories;
            BuildableList.Reset();

            // and then sort each ship category individually by Strength
            foreach (ShipCategory category in categories)
            {
                // and add to Build list
                BuildableListItem catHeader = BuildableList.AddItem(new BuildableListItem(this, category.Name));
                foreach (Ship ship in category.Ships)
                    catHeader.AddSubItem(new BuildableListItem(this, ship));
            }
        }

        public void DrawSelectedShipInfo(int x, int y, Ship ship, SpriteBatch batch)
        {
            if (ship != SelectedShip) // no need to do these calcs all the time for the same ship
            {
                // TODO: USE NEW FAST POWER RECALC FROM SHIP DESIGN SCREEN
                ship.RecalculatePower();
                ship.ShipStatusChange();
                SelectedShip = ship;
            }

            var shipBackground  = new Rectangle(x - 840, y - 120, 360, 240);
            var shipOverlay     = new Rectangle(x - 700, y - 100, 200, 200);
            Vector2 cursor      = new Vector2(x - 815, y - 119);
            float mass          = ship.Mass * EmpireManager.Player.data.MassModifier;
            float subLightSpeed = ship.Thrust / mass; 
            float warpSpeed     = ship.WarpThrust / mass * EmpireManager.Player.data.FTLModifier;
            float turnRate      = ship.TurnThrust.ToDegrees() / mass / 700;
            batch.Draw(ResourceManager.Texture("NewUI/colonyShipBuildBG"), shipBackground, Color.White);
            ship.RenderOverlay(batch, shipOverlay, true, moduleHealthColor: false);
            DrawShipValueLine(ship.Name, "", ref cursor, batch, Font12, Color.White);
            DrawShipValueLine(ship.shipData.ShipCategory + ", " + ship.shipData.CombatState, "", ref cursor, batch, Font8, Color.Gray);
            WriteLine(ref cursor, Font8);
            DrawShipValueLine("Weapons:", ship.Weapons.Count, ref cursor, batch, Font8, Color.LightBlue);
            DrawShipValueLine("Max W.Range:", ship.WeaponsMaxRange, ref cursor, batch, Font8, Color.LightBlue);
            DrawShipValueLine("Avr W.Range:", ship.WeaponsAvgRange, ref cursor, batch, Font8, Color.LightBlue);
            DrawShipValueLine("Warp:", warpSpeed, ref cursor, batch, Font8, Color.LightGreen);
            DrawShipValueLine("Speed:", subLightSpeed, ref cursor, batch, Font8, Color.LightGreen);
            DrawShipValueLine("Turn Rate:", turnRate, ref cursor, batch, Font8, Color.LightGreen);
            DrawShipValueLine("Repair:", ship.RepairRate, ref cursor, batch, Font8, Color.Goldenrod);
            DrawShipValueLine("Shields:", ship.shield_max, ref cursor, batch, Font8, Color.Goldenrod);
            DrawShipValueLine("EMP Def:", ship.EmpTolerance, ref cursor, batch, Font8, Color.Goldenrod);
            DrawShipValueLine("Hangars:", ship.Carrier.AllFighterHangars.Length, ref cursor, batch, Font8, Color.IndianRed);
            DrawShipValueLine("Troop Bays:", ship.Carrier.AllTroopBays.Length, ref cursor, batch, Font8, Color.IndianRed);
            DrawShipValueLine("Troops:", ship.TroopCapacity, ref cursor, batch, Font8, Color.IndianRed);
            DrawShipValueLine("Bomb Bays:", ship.BombBays.Count, ref cursor, batch, Font8, Color.IndianRed);
            DrawShipValueLine("Cargo Space:", ship.CargoSpaceMax, ref cursor, batch, Font8, Color.Khaki);
        }

        void DrawShipValueLine(string description, string data, ref Vector2 cursor, SpriteBatch batch, SpriteFont font, Color color)
        {
            WriteLine(ref cursor, font);
            Vector2 ident = new Vector2(cursor.X + 80, cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data, ident, color);
        }

        void DrawShipValueLine(string description, float data, ref Vector2 cursor, SpriteBatch batch, SpriteFont font, Color color)
        {
            if (data.LessOrEqual(0))
                return;

            WriteLine(ref cursor, font);
            Vector2 ident = new Vector2(cursor.X + 80, cursor.Y);
            batch.DrawString(font, description, cursor, color);
            batch.DrawString(font, data.GetNumberString(), ident, color);
        }

        void WriteLine(ref Vector2 cursor, SpriteFont font)
        {
            cursor.Y += font.LineSpacing + 2;
        }

        public bool Build(Building b, PlanetGridSquare where = null)
        {
            if (P.Construction.AddBuilding(b, where, true))
            {
                GameAudio.AcceptClick();
                return true;
            }
            GameAudio.NegativeClick();
            return false;
        }

        public void Build(Ship ship, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                if (P.IsOutOfOrbitalsLimit(ship))
                {
                    GameAudio.NegativeClick();
                    return;
                }

                if (ship.IsPlatformOrStation || ship.shipData.IsShipyard)
                {
                    P.AddOrbital(ship);
                }
                else
                {
                    P.ConstructionQueue.Add(new QueueItem(P)
                    {
                        isShip = true,
                        isOrbital = ship.IsPlatformOrStation,
                        sData = ship.shipData,
                        Cost = ship.GetCost(P.Owner),
                        ProductionSpent = 0f
                    });
                }
            }
            GameAudio.AcceptClick();
        }

        public void Build(Troop troop, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                P.ConstructionQueue.Add(new QueueItem(P)
                {
                    isTroop         = true,
                    TroopType       = troop.Name,
                    Cost            = troop.ActualCost,
                    ProductionSpent = 0f
                });
            }
            GameAudio.AcceptClick();
        }

        void DrawActiveBuildingEntry(SpriteBatch batch)
        {
            if (ActiveBuildingEntry == null) return; // nothing to draw

            var b = ActiveBuildingEntry.Building;
            var icon = ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48");
            var rect = new Rectangle(Input.MouseX, Input.MouseY, icon.Width, icon.Height);

            bool canBuild = P.FindTileUnderMouse(Input.CursorPosition)?.CanBuildHere(b) == true;
            batch.Draw(icon, rect, canBuild ? Color.White : Color.OrangeRed);
        }

        bool HandleDragBuildingOntoTile(InputState input)
        {
            if (ActiveBuildingEntry == null) return false;

            Building b = ActiveBuildingEntry.Building;
            if (input.LeftMouseReleased)
            {
                PlanetGridSquare tile = P.FindTileUnderMouse(input.CursorPosition);
                if (tile == null || !Build(b, tile))
                    GameAudio.NegativeClick();
                return true;
            }
            
            if (input.RightMouseClick || input.LeftMouseClick)
                return true;

            return ActiveBuildingEntry != null && b.Unique && P.BuildingBuiltOrQueued(b);
        }
    }
}

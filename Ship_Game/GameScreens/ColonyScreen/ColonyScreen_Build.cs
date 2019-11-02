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
            PlayerDesignsToggle.Pressed = GlobalStats.ShowAllDesigns;
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

            if (ResetBuildableList || BuildableShipsChanged(categories))
            {
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
        }

        void OnBuildableItemDoubleClicked(BuildableListItem item)
        {
            item.BuildIt(1);
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

        bool BuildOrbital(Ship orbital)
        {
            if (orbital == null || P.IsOutOfOrbitalsLimit(orbital))
                return false;

            P.AddOrbital(orbital);
            return true;
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

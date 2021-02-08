using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        bool ResetBuildableList;
        string FilterItemsText;

        readonly string BuildingsTabText = Localizer.Token(334); // BUILDINGS
        readonly string ShipsTabText = Localizer.Token(335); // SHIPS
        readonly string TroopsTabText = Localizer.Token(336); // TROOPS

        void OnBuildableTabChanged(int tabIndex)
        {
            PlayerDesignsToggle.Visible    = BuildableTabs.IsSelected(ShipsTabText);
            BuildableList.EnableDragOutEvents = BuildableTabs.IsSelected(BuildingsTabText);
            ResetBuildableList = true;
        }

        void OnPlayerDesignsToggleClicked(ToggleButton button)
        {
            GlobalStats.ShowAllDesigns = !GlobalStats.ShowAllDesigns;
            PlayerDesignsToggle.IsToggled = GlobalStats.ShowAllDesigns;
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
                var buildingsCanBuild = P.GetBuildingsCanBuild();
                if (FilterBuildableItems.Text.NotEmpty())
                    buildingsCanBuild= buildingsCanBuild.Filter(b => b.Name.ToLower().Contains(FilterBuildableItems.Text.ToLower()));

                ResetBuildableList |= BuildableList.NumEntries != buildingsCanBuild.Count;

                if (ResetBuildableList || FilterItemsText != FilterBuildableItems.Text) 
                {
                    FilterItemsText = FilterBuildableItems.Text;
                    Building[] buildings = P.GetBuildingsCanBuild().Sorted(b => b.Name);
                    if (FilterBuildableItems.Text.NotEmpty())
                        buildings = buildings.Filter(b => b.Name.ToLower().Contains(FilterBuildableItems.Text.ToLower()));

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

            if (!ConstructionQueue.IsDragging)
            {
                if (!ConstructionQueue.AllEntries.Select(item => item.Item).EqualElements(P.ConstructionQueue))
                {
                    var newItems = P.ConstructionQueue.Select(qi => new ConstructionQueueScrollListItem(qi, LowRes));
                    ConstructionQueue.SetItems(newItems);
                }
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

            if (FilterBuildableItems.Text.NotEmpty())
            {
                if (ResetBuildableList || FilterItemsText != FilterBuildableItems.Text)
                {
                    FilterItemsText = FilterBuildableItems.Text;
                    var shipList    = buildableShips.Filter(s => s.Name.ToLower().Contains(FilterBuildableItems.Text.ToLower()));
                    shipList        = shipList.SortedDescending(s => s.BaseStrength);
                    BuildableList.SetItems(shipList.Select(s => new BuildableListItem(this, s)));
                    return;
                }
            }

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

            if (FilterItemsText.NotEmpty() && FilterBuildableItems.Text.IsEmpty())
            {
                FilterItemsText     = "";
                 ResetBuildableList = true; // filter is empty so we need a refresh
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
                        catHeader.AddSubItem(new BuildableListItem(this, ship, !ship.shipData.IsShipyard));
                }
            }
        }

        void OnBuildableItemDoubleClicked(BuildableListItem item)
        {
            item.BuildIt(1);
        }

        void OnBuildableHoverChange(BuildableListItem item)
        {
            ShipInfoOverlay.ShowToLeftOf(new Vector2(BuildableList.X, item?.Y ?? 0f), item?.Ship);
        }

        void OnBuildableListDrag(BuildableListItem item, DragEvent evt, bool outside)
        {
            if (evt != DragEvent.End)
                return;

            if (outside)
            {
                Building b = item.Building;
                if (b != null)
                {
                    PlanetGridSquare tile = P.FindTileUnderMouse(Input.CursorPosition);
                    if (tile != null && Build(b, tile))
                        return;
                }
            }

            GameAudio.NegativeClick();
        }

        void OnConstructionItemReorder(ConstructionQueueScrollListItem item, int oldIndex, int newIndex)
        {
            P.Construction.Reorder(oldIndex, newIndex);
        }

        public bool Build(Building b, PlanetGridSquare where = null)
        {
            if (P.Construction.Enqueue(b, where, true))
            {
                GameAudio.AcceptClick();
                ClearItemsFilter();
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
                    P.Construction.Enqueue(ship);
                }
            }

            GameAudio.AcceptClick();
        }

        public void Build(Troop troop, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                P.Construction.Enqueue(troop);
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

        void ClearItemsFilter()
        {
            if (FilterItemsText.IsEmpty())
                return;

            FilterItemsText    = "";
            ResetBuildableList = true;
            FilterBuildableItems.ClearTextInput();
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

        void OnClearFilterClick(UIButton b)
        {
            ClearItemsFilter();
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
    }
}

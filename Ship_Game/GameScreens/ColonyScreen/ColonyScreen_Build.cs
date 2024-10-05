using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        bool ResetBuildableList;
        string FilterItemsText;

        readonly string BuildingsTabText = Localizer.Token(GameText.Buildings); // BUILDINGS
        readonly string ShipsTabText = Localizer.Token(GameText.Ships); // SHIPS
        readonly string TroopsTabText = Localizer.Token(GameText.Troops); // TROOPS

        void OnBuildableTabChanged(int tabIndex)
        {
            PlayerDesignsToggle.Visible    = BuildableTabs.IsSelected(ShipsTabText);
            BuildableList.EnableDragOutEvents = BuildableTabs.IsSelected(BuildingsTabText);
            ResetBuildableList = true;
        }

        void OnPlayerDesignsToggleClicked(ToggleButton button)
        {
            Universe.P.ShowAllDesigns = !Universe.P.ShowAllDesigns;
            PlayerDesignsToggle.IsToggled = !Universe.P.ShowAllDesigns;
            ResetBuildableList = true;
        }

        void ResetBuildableTabs()
        {
            int selected = BuildableTabs.SelectedIndex;

            BuildableTabs.ClearTabs();
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
                ResetBuildableList |= BuildableList.NumEntries != buildingsCanBuild.Count;

                string filter = FilterBuildableItems.Text.ToLower();
                if (ResetBuildableList || FilterItemsText != filter) 
                {
                    FilterItemsText = filter;
                    Building[] buildings = P.GetBuildingsCanBuild().Sorted(b => b.Name);
                    if (filter.NotEmpty())
                        buildings = buildings.Filter(b => b.Name.ToLower().Contains(filter));

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
            public readonly Array<IShipDesign> Ships = new();
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
            IShipDesign[] buildableShips = Empty<IShipDesign>.Array;

            // enable all ships in the sandbox
            if (Universe.Debug && Universe.Screen is DeveloperUniverse)
            {
                buildableShips = ResourceManager.Ships.Designs.ToArr();
            }
            else if (P.Owner != null)
            {
                buildableShips = P.Owner.ShipsWeCanBuild
                    .Filter(ship => (ship.IsBuildableByPlayer(Universe.Player) && Universe.Screen.Player.WeCanBuildThis(ship) || Universe.Debug)
                                    && !ship.IsResearchStation
                                    && !ship.IsMiningStation
                                    && !ship.IsConstructor
                                    && !ship.IsSubspaceProjector
                                    && !ship.IsDysonSwarmController);
            }

            string filter = FilterBuildableItems.Text.ToLower();
            if (filter.IsEmpty() && FilterItemsText.NotEmpty())
            {
                FilterItemsText = "";
                ResetBuildableList = true; // filter is empty so revert back to Ship Categories
            }

            if (filter.NotEmpty() && (ResetBuildableList || FilterItemsText != filter))
            {
                FilterItemsText = filter;
                var shipList = buildableShips.Filter(s => s.Name.ToLower().Contains(filter));
                shipList = shipList.SortedDescending(s => s.BaseStrength);
                BuildableList.SetItems(shipList.Select(s => new BuildableListItem(this, s)));
                return;
            }

            var categoryMap = new Map<string, ShipCategory>();

            foreach (IShipDesign ship in buildableShips)
            {
                string name = Localizer.GetRole(ship.Role, P.Owner);
                if (!categoryMap.TryGetValue(name, out ShipCategory c))
                {
                    c = new(){ Name = name, Size = ship.SurfaceArea };
                    categoryMap.Add(name, c);
                }
                c.Ships.Add(ship);
            }

            // first sort the categories by name:
            ShipCategory[] categories = categoryMap.Values.Sorted(c => c.Name);
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
                    BuildableListItem catHeader = BuildableList.AddItem(new(this, category.Name));
                    foreach (IShipDesign ship in category.Ships)
                        catHeader.AddSubItem(new BuildableListItem(this, ship, !ship.IsShipyard));
                }
            }
        }

        void OnBuildableItemDoubleClicked(BuildableListItem item)
        {
            if (P.Owner != Player && !P.Universe.Debug)
                return;

            item.BuildIt(1);
        }

        void OnBuildableHoverChange(BuildableListItem item)
        {
            if (item == null) // lost hover
            {
                ShipInfoOverlay.Hide();
            }
            else
            {
                ShipInfoOverlay.ShowToLeftOf(new Vector2(BuildableList.X, item.Y), item.Ship);
            }
        }

        void OnBuildableListDrag(BuildableListItem item, DragEvent evt, bool outside)
        {
            if (evt != DragEvent.End)
                return;

            if (outside && item != null) // TODO: somehow `item` can be null, not sure how it happens
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

        void OnConstructionItemReorder(ConstructionQueueScrollListItem item, int relativeChange)
        {
            P.Construction.Reorder(item.Item, relativeChange);
        }

        void OnConstructionItemHovered(ConstructionQueueScrollListItem item)
        {
            if (item == null) // lost hover
            {
                ShipInfoOverlay.Hide();
            }
            else if (item.Item.isShip)
            {
                ShipInfoOverlay.ShowToLeftOf(item.Pos, item.Item.ShipData);
            }
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

        public void Build(IShipDesign ship, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                if (P.IsOutOfOrbitalsLimit(ship))
                {
                    GameAudio.NegativeClick();
                    return;
                }

                if (ship.IsPlatformOrStation || ship.IsShipyard)
                {
                    P.AddOrbital(ship);
                }
                else
                {
                    P.Construction.Enqueue(ship, ship.IsFreighter ? QueueItemType.Freighter : QueueItemType.CombatShip);
                }
            }

            GameAudio.AcceptClick();
        }

        public void Build(Troop troop, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                P.Construction.Enqueue(troop, QueueItemType.Troop);
            }

            GameAudio.AcceptClick();
        }

        void ClearItemsFilter()
        {
            if (FilterItemsText.IsEmpty())
                return;

            FilterItemsText    = "";
            ResetBuildableList = true;
            FilterBuildableItems.Clear();
        }

        void OnClearFilterClick(UIButton b)
        {
            ClearItemsFilter();
        }
    }
}

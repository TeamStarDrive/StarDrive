using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.FleetDesign;
using Ship_Game.Ships;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game;

// Galaxy-only navigation. Station categories are exclusive so an asset is never listed twice.
public sealed partial class EmpireAssetsPanel : UIElementContainer
{
    internal enum AssetKind { All, Fleets, Planets, Research, Starbases, Mining }
    internal enum AssetSort { Name, Food, Production, Research }
    static readonly string[] SortNames = { "Name", "Food", "Production", "Research" };
    static readonly string[] SortIcons = { null, "NewUI/icon_food", "NewUI/icon_production", "NewUI/icon_science" };
    static readonly string[] CategoryNames = { "All assets", "Fleets", "Planets", "Research stations", "Starbases / shipyards", "Mining stations" };
    static readonly string[] CategoryIcons =
    {
        "NewUI/icon_tiles", "FleetIcons/1", "NewUI/icon_planet_terran_01_mid",
        "NewUI/icon_science", "TacticalIcons/symbol_station", "NewUI/icon_production"
    };
    readonly UniverseScreen Screen;
    readonly FleetButtonsList FleetShortcuts;
    readonly UITextEntry Search;
    readonly List<Row> Rows = new();
    readonly List<Row> Assets = new();
    readonly bool[] SectionCollapsed = new bool[6];
    readonly int[] Counts = new int[6];
    bool Collapsed, ReverseSort;
    bool AutomationSelected;
    AssetKind Filter;
    AssetSort SortBy;
    int Offset;
    float RefreshTimer;
    Vector2 Cursor;

    sealed class Row
    {
        public AssetKind Kind;
        public Fleet Fleet;
        public Planet Planet;
        public Ship Station;
        public bool IsHeader;
        public string Name;
        public QueueItem[] Queue = Array.Empty<QueueItem>();
        public Ship[] Members = Array.Empty<Ship>();
        public readonly List<(TacticalIcon Icon, int Count)> Composition = new();
        public float Strength;
        public float SortValue;
        public int Height => IsHeader ? 23 : Planet != null ? 57 : Fleet != null ? 43 : 37;
    }

    public EmpireAssetsPanel(RectF rect, UniverseScreen screen,
                             Action<FleetButton> onClick, Action<FleetButton> onHotKey,
                             Func<FleetButton, bool> isSelected) : base(rect)
    {
        Screen = screen;
        screen.aw.AttachSidebar(this);
        FleetShortcuts = new FleetButtonsList(rect, screen, screen, onClick, onHotKey, isSelected);
        Search = Add(new UITextEntry(ContentX + 12, rect.Y + 34, ContentWidth - 24, 22, Fonts.Arial12, "")
        {
            Color = Color.Wheat, MaxCharacters = 60,
            OnTextChanged = _ => { Offset = 0; Rebuild(); }
        });
        screen.OnExit += Search.StopInput;
        Rebuild();
    }

    bool ShouldHide => Screen.LookingAtPlanet || Screen.DefiningAO || Screen.DefiningTradeRoutes;
    const int RailWidth = 48;
    float ContentX => X + RailWidth;
    float ContentWidth => Width - RailWidth;
    RectF HeaderRect => new(ContentX, Y, ContentWidth, 28);
    // Keep content on-screen while the decorative left rail extends beyond the viewport.
    RectF FrameRect => Collapsed ? new(X - 28, Y - 50, 100, Height + 74)
                                 : new(X - 28, Y - 50, Width + 50, Height + 74);
    // Persistent action order: Empire Assets, AI Automation, then future actions.
    RectF RailActionRect(int index) => new(X + 3, Y + index * 42, 32, 32);
    RectF ListRect => new(ContentX + 7, Y + 91, ContentWidth - 14, Math.Max(57, Height - 119));
    RectF TabRect(int index) => new(ContentX + 7 + index * (ContentWidth - 14) / 6, Y + 61, (ContentWidth - 14) / 6 - 3, 26);
    RectF FooterRect => new(ContentX + 8, Y + Height - 24, ContentWidth - 16, 20);
    RectF SortRect(int index) => new(FooterRect.X + 34 + index * 31, FooterRect.Y, 28, 20);
    RectF SortDirectionRect => new(FooterRect.Right - 70, FooterRect.Y, 70, 20);
    static RectF CenterRect(RectF row) => new(row.Right - 22, row.Y + 3, 20, 19);
    static RectF RushRect(RectF row) => new(row.Right - 24, row.Y + 35, 21, 19);
    static RectF QueueRect(RectF row) => new(row.X + 39, row.Y + 37, row.W - 67, 17);

    // Called for live owned ships only. Specialist stations take precedence over generic orbitals.
    internal static AssetKind ClassifyStation(Ship ship)
    {
        if (!ship.Active || ship.IsSubspaceProjector) return AssetKind.All;
        if (ship.IsResearchStation) return AssetKind.Research;
        if (ship.IsMiningStation) return AssetKind.Mining;
        if (ship.IsPlatformOrStation || ship.IsShipyard) return AssetKind.Starbases;
        return AssetKind.All;
    }

    void Rebuild()
    {
        Assets.Clear();
        Array.Clear(Counts);
        for (int key = Empire.FirstFleetKey; key <= Empire.LastFleetKey; ++key)
        {
            Fleet fleet = Screen.Player.GetFleetOrNull(key);
            if (fleet == null || fleet.CountShips == 0) continue;
            var row = new Row { Kind = AssetKind.Fleets, Fleet = fleet, Name = fleet.Name,
                                Members = fleet.Ships.ToArray(), Strength = fleet.GetStrength() };
            // Cache composition outside Draw; large fleets use the same tactical icon grouping
            // as the old fleet buttons, including secondary role icons.
            foreach (Ship member in row.Members)
            {
                TacticalIcon icon = member.TacticalIcon();
                int index = row.Composition.FindIndex(c => c.Icon.Equals(icon));
                if (index < 0) row.Composition.Add((icon, 1));
                else row.Composition[index] = (icon, row.Composition[index].Count + 1);
            }
            Assets.Add(row);
        }
        foreach (Planet planet in Screen.Player.GetPlanets())
            Assets.Add(new Row { Kind = AssetKind.Planets, Planet = planet, Name = planet.Name,
                                 Queue = planet.ConstructionQueueSnapshot, SortValue = SortBy switch
                                 {
                                     AssetSort.Food => planet.Food.NetIncome,
                                     AssetSort.Production => planet.Prod.NetIncome,
                                     AssetSort.Research => planet.Res.NetIncome,
                                     _ => 0
                                 } });
        foreach (Ship ship in Screen.Player.OwnedShips)
        {
            if (ship.Loyalty != Screen.Player) continue;
            AssetKind kind = ClassifyStation(ship);
            if (kind != AssetKind.All)
                Assets.Add(new Row { Kind = kind, Station = ship, Name = ship.ShipName, Strength = ship.GetStrength() });
        }
        foreach (Row row in Assets) ++Counts[(int)row.Kind];
        Counts[0] = Assets.Count;
        int direction = ReverseSort ? -1 : 1;
        Assets.Sort((a, b) =>
        {
            int category = a.Kind.CompareTo(b.Kind);
            if (category != 0) return category;
            if (a.Planet != null && SortBy != AssetSort.Name)
            {
                int value = direction * a.SortValue.CompareTo(b.SortValue);
                if (value != 0) return value;
                // Equal income retains a stable alphabetical order, even for descending sorts.
                int name = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
                return name != 0 ? name : a.Planet.Id.CompareTo(b.Planet.Id);
            }
            return (SortBy == AssetSort.Name ? direction : 1) * StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
        });
        Rows.Clear();
        string query = Search.Text.Trim();
        bool Matches(string value) => value?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        for (int category = 1; category < CategoryNames.Length; ++category)
        {
            var kind = (AssetKind)category;
            if (Filter != AssetKind.All && Filter != kind) continue;
            // Empty specialist sections add clutter in young empires.
            if (Counts[category] == 0 && Filter == AssetKind.All && category > 2) continue;
            Rows.Add(new Row { Kind = kind, IsHeader = true, Name = CategoryNames[category] });
            if (SectionCollapsed[category]) continue;
            foreach (Row row in Assets)
            {
                if (row.Kind != kind) continue;
                string location = row.Planet?.System.Name ?? row.Station?.System?.Name;
                if (Matches(row.Name) || Matches(location)) Rows.Add(row);
            }
        }
        Offset = Math.Clamp(Offset, 0, MaxOffset);
    }

    int MaxOffset
    {
        get
        {
            float height = 0;
            int start = Rows.Count;
            while (start > 0 && height + Rows[start - 1].Height <= ListRect.H)
                height += Rows[--start].Height;
            return Math.Max(0, Math.Min(start, Rows.Count - 1));
        }
    }

    IEnumerable<(Row Row, RectF Rect)> VisibleRows()
    {
        float y = ListRect.Y;
        for (int i = Offset; i < Rows.Count; ++i)
        {
            Row row = Rows[i];
            if (y + row.Height > ListRect.Bottom) yield break;
            yield return (row, new RectF(ListRect.X, y, ListRect.W - 7, row.Height - 2));
            y += row.Height;
        }
    }

    public override void PerformLayout()
    {
        base.PerformLayout();
        Search.RectF = new RectF(ContentX + 12, Y + 34, ContentWidth - 24, 22);
        Search.PerformLayout();
        Screen.aw.CloseSidebarChoices();
    }

    public override void Update(float fixedDeltaTime)
    {
        if (ShouldHide)
        {
            if (Search.HandlingInput) Search.StopInput();
            Screen.aw.CloseSidebarChoices();
            return;
        }
        Search.Visible = !Collapsed && !AutomationSelected;
        RefreshTimer -= fixedDeltaTime;
        if (RefreshTimer <= 0)
        {
            RefreshTimer = 0.5f;
            Rebuild();
        }
        base.Update(fixedDeltaTime);
    }

    public override bool HandleInput(InputState input)
    {
        if (ShouldHide || Screen.pieMenu.Visible) return false;
        Cursor = input.CursorPosition;
        // Navigation stays available while search owns keyboard focus.
        if (HandleNavigation(input)) return true;
        if (!Collapsed && !AutomationSelected)
        {
            bool searchCaptured = Search.HandleInput(input);
            if (Search.HandlingInput || searchCaptured) return true;
        }
        if (input.AutomationWindow && !Screen.Debug)
        {
            ToggleAutomation();
            return true;
        }
        if (HandleNavigationShortcuts(input)) return true;
        if (FleetShortcuts.HandleFleetHotkeys(input)) return true;
        if (!Collapsed && AutomationSelected && Screen.aw.HandleSidebarPopup(input, AutomationRect)) return true;
        if (!FrameRect.HitTest(Cursor)) return false;
        if (input.LeftMouseClick && !Collapsed && HeaderRect.HitTest(Cursor))
        {
            CollapseSidebar();
            return true;
        }
        if (Collapsed) return true;
        if (AutomationSelected)
        {
            Screen.aw.HandleSidebarInput(input, AutomationRect);
            return true;
        }
        if (input.LeftMouseClick)
        {
            for (int i = 0; i < CategoryNames.Length; ++i)
                if (TabRect(i).HitTest(Cursor))
                {
                    Filter = (AssetKind)i;
                    Offset = 0;
                    Rebuild();
                    return true;
                }
            for (int i = 0; i < SortNames.Length; ++i)
                if (SortRect(i).HitTest(Cursor))
                {
                    SetSort((AssetSort)i);
                    return true;
                }
            if (SortDirectionRect.HitTest(Cursor))
            {
                ReverseSort = !ReverseSort;
                Offset = 0;
                Rebuild();
                return true;
            }
        }
        if (ListRect.HitTest(Cursor))
        {
            if (input.ScrollIn) Offset = Math.Max(0, Offset - 1);
            if (input.ScrollOut) Offset = Math.Min(MaxOffset, Offset + 1);
            if (input.LeftMouseClick)
                foreach (var (row, rect) in VisibleRows())
                    if (rect.HitTest(Cursor)) { Activate(row, rect, input); break; }
        }
        return true; // No pointer click or wheel event may pass through to the map.
    }

    void ToggleAssets()
    {
        Collapsed = !AutomationSelected && !Collapsed;
        AutomationSelected = false;
        Screen.aw.SetSidebarOpen(false);
        Search.Visible = !Collapsed;
        Search.StopInput();
    }

    RectF AutomationRect => new(ContentX + 7, Y + 32, ContentWidth - 14, Height - 36);

    internal void ToggleAutomation()
    {
        Collapsed = AutomationSelected && !Collapsed;
        AutomationSelected = true;
        Search.Visible = false;
        Search.StopInput();
        Screen.aw.SetSidebarOpen(!Collapsed);
    }

    void CollapseSidebar()
    {
        Collapsed = true;
        Search.Visible = false;
        Search.StopInput();
        Screen.aw.SetSidebarOpen(false);
    }

    void Activate(Row row, RectF rect, InputState input)
    {
        if (row.IsHeader)
        {
            SectionCollapsed[(int)row.Kind] = !SectionCollapsed[(int)row.Kind];
            Rebuild();
            return;
        }
        bool center = CenterRect(rect).HitTest(Cursor);
        if (row.Fleet != null)
        {
            if (row.Fleet.CountShips == 0) return;
            Screen.SetSelectedFleet(row.Fleet);
            if (center || input.LeftMouseDoubleClick) Screen.SnapViewFleet(row.Fleet);
        }
        else if (row.Planet != null && row.Planet.Owner == Screen.Player)
        {
            if (RushRect(rect).HitTest(Cursor))
            {
                QueueItem item = row.Queue.Length > 0 ? row.Queue[0] : null;
                bool all = input.IsCtrlKeyDown, continuous = input.IsShiftKeyDown;
                Screen.RunOnSimThread(() =>
                {
                    if (RushItem(row.Planet, Screen.Player, item, all, continuous)) GameAudio.AcceptClick();
                    else GameAudio.NegativeClick();
                });
                return;
            }
            Screen.SetSelectedPlanet(row.Planet);
            if (input.LeftMouseDoubleClick) Screen.SnapViewColony(row.Planet, combatView: false);
            else if (center)
                Screen.SnapViewSystem(row.Planet.System, row.Planet, UniverseScreen.UnivScreenState.SystemView);
        }
        else if (row.Station != null && row.Station.Active && row.Station.Loyalty == Screen.Player)
        {
            Screen.SetSelectedShip(row.Station);
            if (center || input.LeftMouseDoubleClick) Screen.SnapViewShip(row.Station);
        }
    }

    // Must run on the simulation thread. Target the displayed item by identity, never blindly
    // rush index zero: the queue may finish, reorder, or change ownership between draw and click.
    internal static bool RushItem(Planet planet, Empire player, QueueItem item, bool all, bool continuous)
    {
        if (planet.Owner != player || item == null || item.IsComplete || item.IsCancelled) return false;
        int index = -1;
        for (int i = 0; i < planet.ConstructionQueue.Count; ++i)
            if (ReferenceEquals(planet.ConstructionQueue[i], item)) { index = i; break; }
        if (index < 0) return false;
        if (continuous)
        {
            item.Rush = !item.Rush;
            return true;
        }
        float amount = Math.Min(all ? planet.ProdHere : 10f, Math.Min(item.ProductionNeeded, planet.ProdHere));
        return planet.Construction.RushProduction(index, amount, rushButton: true);
    }

    internal int AssetCount(AssetKind kind) => Counts[(int)kind];

    internal void SetSort(AssetSort sort)
    {
        ReverseSort = SortBy == sort ? !ReverseSort : sort != AssetSort.Name;
        SortBy = sort;
        Offset = 0;
        Rebuild();
    }

    internal Planet[] ListedPlanets => Rows.FindAll(r => r.Planet != null).ConvertAll(r => r.Planet).ToArray();
}

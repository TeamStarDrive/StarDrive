using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug.Page;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDGraphics.Input;
using SDUtils;
using Ship_Game.Universe;
using System.Linq;

namespace Ship_Game.Debug;

public sealed partial class DebugInfoScreen : GameScreen
{
    public bool IsOpen = true;
    public readonly UniverseScreen Screen;
    public readonly UniverseState Universe;
    public readonly RectF Win = new(30, 100, 1200, 700);

    public DebugModes Mode => Screen.DebugMode;
    readonly DebugModes[] Modes;
    DebugPage Page;

    public readonly Submenu ModesTab;
    public readonly ShipDebugInfoPanel ShipInfoPanel;
    public readonly ShipModuleInfoPanel ModuleInfoPanel;

    public DebugInfoScreen(UniverseScreen screen) : base(screen, toPause: null)
    {
        Screen = screen;
        Universe = screen.UState;

        Modes = typeof(DebugModes).GetEnumValues().Cast<DebugModes>()
                .ToArr().Filter(m => m != DebugModes.Disabled);

        var modesRect = new RectF(10, 70, ScreenWidth - 20, ScreenHeight - 80);
        var modeStrings = Modes.Select(m => (LocalizedText)m.ToString());
        ModesTab = Add(new Submenu(modesRect, modeStrings));
        ModesTab.OnTabChange = OnModesTabChange;
        ModesTab.SelectedIndex = (int)Mode;
            
        ShipInfoPanel = ModesTab.Add(new ShipDebugInfoPanel(this, new(10,135), new(350, 500)));
        ModuleInfoPanel = ModesTab.Add(new ShipModuleInfoPanel(Screen, new(-300, 200), new(300, 300))
        {
            ParentAlign = Align.TopRight
        });
    }

    void OnModesTabChange(int modeIndex)
    {
        ResearchText.Clear();
        Universe.SetDebugMode((DebugModes)modeIndex);
    }

    public override bool HandleInput(InputState input)
    {
        if (input.KeyPressed(Keys.Left) || input.KeyPressed(Keys.Right))
        {
            int modeIndex = (int)Mode + (input.KeyPressed(Keys.Left) ? -1 : +1);
            if (modeIndex < 0) modeIndex = Modes.Length - 1;
            if (modeIndex >= Modes.Length) modeIndex = 0;
            OnModesTabChange(modeIndex);
            return true;
        }

        if (ModuleInfoPanel.TrySelectModule(input, out ShipModule module))
        {
            ModuleInfoPanel.ShowModule(module);
        }

        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        // if the debug window hits a cyclic crash it can be turned off in game.
        // I don't see a point in crashing the game because of a debug window error.
        try
        {
            if (Page != null && Page.Mode != Mode) // destroy page if it's no longer needed
            {
                Page.RemoveFromParent();
                Page = null;
            }

            if (Page == null) // create page if needed
            {
                Page = CreatePage(Mode);
                if (Page != null)
                    Add(Page);
            }

            base.Update(fixedDeltaTime);
        }
        catch (Exception e)
        {
            Universe.SetDebugMode(false);
            Screen.ToggleDebugWindow();
            Log.Error(e, "DebugWindowCrashed");
        }
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        try
        {
            DrawDebugPrimitives(elapsed.RealTime.Seconds);
            VisualizeShipGoals();

            ShipInfoPanel.Hidden = (Mode is DebugModes.Particles 
                or DebugModes.StoryAndEvents) || !ShipInfoPanel.HasSelectedSomething();

            ModesTab.Hidden = Screen.workersPanel?.Visible == true;

            base.Draw(batch, elapsed);
        }
        catch { }
    }

    DebugPage CreatePage(DebugModes mode)
    {
        return mode switch
        {
            DebugModes.Normal => null, // nothing in normal
            DebugModes.Empire => new EmpireInfoDebug(this),
            DebugModes.Targeting => new TargetingDebug(this),
            DebugModes.PathFinder => new PathFinderDebug(this),
            DebugModes.Tech => new TechDebug(this),
            DebugModes.Input => new InputDebug(this),
            DebugModes.Pirates => new PiratesDebug(this),
            DebugModes.Remnants => new RemnantsDebug(this),
            DebugModes.Agents => new AgentsDebug(this),
            DebugModes.Relationship => new RelationshipDebug(this),
            DebugModes.FleetMulti => new FleetMultipliersDebug(this),
            DebugModes.Trade => new TradeDebug(this),
            DebugModes.Planets => new PlanetDebug(this),
            DebugModes.Influence => new InfluenceDebug(this),
            DebugModes.Solar => new SolarDebug(this),
            DebugModes.War => new WarDebug(this),
            DebugModes.SpatialManager => new SpatialDebug(this),
            DebugModes.StoryAndEvents => new StoryAndEventsDebug(this),
            DebugModes.Particles => new ParticleDebug(this),
            DebugModes.ThreatMatrix => new ThreatMatrixDebug(this),
            DebugModes.DefenseCo => new DefenseCoordinatorDebug(this),
            DebugModes.Tasks => new TasksDebug(this),
            DebugModes.Perf => new PerfDebug(this),
            DebugModes.SpaceRoads=> new SpaceRoadsDebug(this),
            DebugModes.Goals => new EmpireGoalsDebug(this),
            DebugModes.MiningOps => new MiningOpsDebug(this),
            _ => null
        };
    }

    public void DefenseCoLogsNull(bool found, Ship ship, SolarSystem systemToDefend)
    {
        if (Mode != DebugModes.DefenseCo)
            return;
        if (!found && ship.Active)
        {
            Log.Info(ConsoleColor.Yellow, systemToDefend == null
                ? "SystemCommander: Remove : SystemToDefend Was Null"
                : "SystemCommander: Remove : Ship Not Found in Any");
        }
    }

    public void DefenseCoLogsMultipleSystems(Ship ship)
    {
        if (Mode != DebugModes.DefenseCo) return;
        Log.Info(color: ConsoleColor.Yellow, text: $"SystemCommander: Remove : Ship Was in Multiple SystemCommanders: {ship}");
    }

    public void DefenseCoLogsNotInPool()
    {
        if (Mode != DebugModes.DefenseCo) return;
        Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : Not in DefensePool");
    }
    public void DefenseCoLogsSystemNull()
    {
        if (Mode != DebugModes.DefenseCo) return;
        Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : SystemToDefend Was Null");
    }
        
    public bool DebugLogText(string text, DebugModes mode)
    {
        if (IsOpen && Mode == mode && GlobalStats.VerboseLogging)
        {
            Log.Info(text);
            return true;
        }
        return false;
    }

    readonly Dictionary<string, Array<string>> ResearchText = new();

    public void ResearchLog(string text, Empire empire)
    {
        if (!DebugLogText(text, DebugModes.Tech))
            return;
        if (GetResearchLog(empire, out Array<string> empireTechs))
        {
            empireTechs.Add(text);
        }
        else
        {
            ResearchText.Add(empire.Name, new(){ text });
        }
    }

    public void ClearResearchLog(Empire empire)
    {
        if (GetResearchLog(empire, out Array<string> empireTechs))
            empireTechs.Clear();
    }

    public bool GetResearchLog(Empire e, out Array<string> empireTechs)
    {
        return ResearchText.TryGetValue(e.Name, out empireTechs);
    }

}
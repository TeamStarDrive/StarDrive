using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;

namespace Ship_Game;

public sealed partial class EmpireAssetsPanel
{
    internal enum SidebarAction
    {
        Assets, Automation, Colonies, Research, Economy, Empire, Diplomacy, Fleets,
        Espionage, Shipyard, Ships, Blueprints, Construction, Menu, Help
    }
    static readonly string[] NavigationIcons =
    {
        "assets", "automation", "colonies", "research", "economy", "empire", "diplomacy", "fleets",
        "espionage", "shipyard", "ships", "blueprints", "construction", "menu", "help"
    };
    static readonly string[] NavigationTips =
    {
        "Empire Assets", "AI Automation (H)", "Colonization planner / planet reconnaissance (L)",
        "Research (R)", "Economic overview (T)", "Empire and colony management (U)", "Diplomacy (I)",
        "Fleet manager (J)", "Espionage (E)", "Ship designer (Y)", "Ship roster (K)",
        "Colony blueprints (F)", "Deep-space construction (B)", "Main menu (O)", "Help / Codex (F1)"
    };
    int NavigationOffset;
    int NavigationCapacity => Math.Max(1, (int)((Height - 120) / 42));
    int NavigationMaxOffset => Math.Max(0, NavigationIcons.Length - 2 - NavigationCapacity);
    RectF NavigationScrollRect => new(X, Y + 84, RailWidth, Math.Max(1, Height - 84));
    RectF NavigationUpRect => new(X + 3, Y + 80, 32, 12);
    RectF NavigationDownRect => new(X + 3, Y + Height - 20, 32, 14);

    internal IEnumerable<(SidebarAction Action, RectF Rect)> NavigationButtons()
    {
        yield return (SidebarAction.Assets, RailActionRect(0));
        yield return (SidebarAction.Automation, RailActionRect(1));
        NavigationOffset = Math.Clamp(NavigationOffset, 0, NavigationMaxOffset);
        int end = Math.Min(NavigationIcons.Length, 2 + NavigationOffset + NavigationCapacity);
        for (int i = 2 + NavigationOffset; i < end; ++i)
            yield return ((SidebarAction)i, new RectF(X + 3, Y + 96 + (i - 2 - NavigationOffset) * 42, 32, 32));
    }

    bool HandleNavigation(InputState input)
    {
        if (NavigationScrollRect.HitTest(Cursor))
        {
            if (input.ScrollIn || input.ScrollOut)
            {
                NavigationOffset = Math.Clamp(NavigationOffset + (input.ScrollOut ? 1 : -1), 0, NavigationMaxOffset);
                return true;
            }
        }
        if (NavigationMaxOffset > 0 && input.LeftMouseClick)
        {
            if (NavigationUpRect.HitTest(Cursor))
            {
                NavigationOffset = Math.Max(0, NavigationOffset - 1);
                return true;
            }
            if (NavigationDownRect.HitTest(Cursor))
            {
                NavigationOffset = Math.Min(NavigationMaxOffset, NavigationOffset + 1);
                return true;
            }
        }
        if (!input.LeftMouseClick) return false;
        foreach (var (action, rect) in NavigationButtons())
            if (rect.HitTest(Cursor))
            {
                Search.StopInput();
                Screen.aw.CloseSidebarChoices();
                ActivateNavigation(action);
                return true;
            }
        return false;
    }

    bool HandleNavigationShortcuts(InputState input)
    {
        if (GlobalStats.TakingInput) return false;
        if (Screen.EmpireUI?.HandleInput(input) == true) return true;
        SidebarAction? action = input.PlanetListScreen ? SidebarAction.Colonies
            : input.BlueprintsSceen ? SidebarAction.Blueprints
            : input.DeepSpaceBuildWindow ? SidebarAction.Construction
            : input.ShipListScreen ? SidebarAction.Ships
            : input.FleetDesignScreen ? SidebarAction.Fleets : null;
        if (!action.HasValue) return false;
        ActivateNavigation(action.Value);
        return true;
    }

    void ActivateNavigation(SidebarAction action)
    {
        switch (action)
        {
            case SidebarAction.Assets: ToggleAssets(); return;
            case SidebarAction.Automation: ToggleAutomation(); return;
            case SidebarAction.Colonies:
                Screen.ScreenManager.AddScreen(new PlanetListScreen(Screen, Screen.EmpireUI)); break;
            case SidebarAction.Blueprints:
                Screen.ScreenManager.AddScreen(new BlueprintsScreen(Screen, Screen.Player)); break;
            case SidebarAction.Construction:
                Screen.InputOpenDeepSpaceBuildWindow(); return;
            default:
                string target = action switch
                {
                    SidebarAction.Economy => "Budget", SidebarAction.Ships => "ShipList",
                    SidebarAction.Menu => "Main Menu", SidebarAction.Help => "?", _ => action.ToString()
                };
                Screen.EmpireUI.OpenNavigation(target);
                return;
        }
        GameAudio.AcceptClick();
    }
}

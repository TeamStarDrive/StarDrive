using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.AI;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game;

public sealed partial class EmpireUIOverlay
{
    internal const int DashboardHeight = 36;
    int DashboardMusicWidth => Universe.ScreenWidth >= 1440 ? 190 : 84;
    internal enum DashboardItem { Money, Food, Production, Science, Research, Population, Freight, Empire, Fleets, Resources, Alerts }
    static readonly float[] DashboardWeights = { 1.25f, 1.1f, 1.1f, .8f, 1.5f, 1.05f, .65f, .55f, .65f, 1f, .4f };
    static readonly float[] DashboardSpeeds = { .5f, 1f, 2f, 4f };
    static readonly Color DashboardGold = new(205, 184, 135);
    static readonly Color DashboardCyan = new(79, 200, 215);
    readonly string[] DashboardValues = new string[11];
    readonly string[] DashboardTips = new string[11];
    readonly float[] DashboardNet = new float[3];
    internal string DashboardValue(DashboardItem item) => DashboardValues[(int)item];
    internal string DashboardTip(DashboardItem item) => DashboardTips[(int)item];
    readonly string[] DashboardIcons = {
        "NewUI/SidebarIcons/economy", "NewUI/icon_food", "NewUI/icon_production",
        "NewUI/icon_science", "NewUI/SidebarIcons/research", "NewUI/icon_population",
        "NewUI/SidebarIcons/ships", "NewUI/SidebarIcons/colonies", "NewUI/SidebarIcons/fleets",
        "NewUI/SidebarIcons/assets", "NewUI/SidebarIcons/help"
    };
    Vector2 DashboardCursor = new(-1, -1);
    int DashboardRefreshTick;
    bool DashboardReady;
    EmpireExoticBonuses[] DashboardResources = System.Array.Empty<EmpireExoticBonuses>();

    internal RectF DashboardItemRect(DashboardItem item)
    {
        float total = 0, before = 0;
        for (int i = 0; i < DashboardWeights.Length; ++i)
        {
            total += DashboardWeights[i];
            if (i < (int)item) before += DashboardWeights[i];
        }
        // Cap the entire resource strip so wide displays don't stretch each item.
        float available = Math.Max(1, Math.Min(906, Universe.ScreenWidth - 252 - DashboardMusicWidth));
        return new RectF(before / total * available, 0, DashboardWeights[(int)item] / total * available, DashboardHeight);
    }

    // 0 toggles pause; 1..4 select a simulation speed without changing pause state.
    internal RectF DashboardSpeedRect(int index)
        => new(Universe.ScreenWidth - 174 + index * 34, 6, 31, 24);

    internal RectF DashboardMusicRect(int index)
        => new(Universe.ScreenWidth - 252 + index * 26, 6, 24, 24);

    static string DashNumber(double value) => Math.Abs(value) >= 1000000 ? $"{value / 1000000:0.#}M"
        : Math.Abs(value) >= 1000 ? $"{value / 1000:0.#}K" : $"{value:0.#}";
    static string DashSigned(float value) => (value >= 0 ? "+" : "") + DashNumber(value);

    internal void RefreshDashboard()
    {
        float food = 0, prod = 0, storage = 0, foodGross = 0, foodNet = 0, prodGross = 0, prodNet = 0;
        float pop = 0, popMax = 0, growth = 0, strength = 0, cargo = 0, capacity = 0;
        int colonies = 0, starving = 0, freighters = 0, trading = 0, ships = 0;
        var systems = new HashSet<SolarSystem>();
        foreach (Planet p in Player.GetPlanets())
        {
            ++colonies;
            systems.Add(p.System);
            food += p.FoodHere; prod += p.ProdHere; storage += p.Storage.Max;
            foodGross += p.Food.GrossIncome; foodNet += p.Food.NetIncome;
            prodGross += p.Prod.GrossIncome; prodNet += p.Prod.NetIncome;
            pop += p.PopulationBillion; popMax += p.MaxPopulationBillion;
            growth += p.LastPopulationGrowthBillion;
            if (p.IsStarving) ++starving;
        }
        foreach (var ship in Player.OwnedShips)
        {
            if (!ship.Active) continue;
            ++ships;
            strength += ship.GetStrength();
            if (!ship.IsFreighter) continue;
            ++freighters;
            if (ship.AI.State == AIState.SystemTrader) ++trading;
            cargo += ship.CargoSpaceUsed;
            capacity += ship.CargoSpaceMax;
        }
        float income = Player.EstimateNetIncomeAtTaxRate(Player.data.TaxRate);
        float science = Player.Research.NetResearch;
        DashboardNet[0] = income; DashboardNet[1] = foodNet; DashboardNet[2] = prodNet;
        Set(DashboardItem.Money, DashNumber(Player.Money), $"TREASURY\nCredits: {Player.Money:0.#}\nEstimated net income: {DashSigned(income)} / turn\nTax rate: {Player.data.TaxRate:P0}\nClick for economic overview.");
        Set(DashboardItem.Food, DashNumber(food), $"FOOD\nEmpire reserves: {food:0.#} / {storage:0.#}\nGeneration: {foodGross:0.#} / turn\nNet after consumption: {DashSigned(foodNet)} / turn\nStarving colonies: {starving}\nReserves are held locally at each colony, not in a shared pool.\nClick for colony management.");
        Set(DashboardItem.Production, DashNumber(prod), $"PRODUCTION\nEmpire reserves: {prod:0.#} / {storage:0.#}\nGross generation: {prodGross:0.#} / turn\nNet output: {DashSigned(prodNet)} / turn\nNet output is before construction spending. Reserves are held locally.\nClick for colony management.");
        Set(DashboardItem.Science, DashNumber(science), $"SCIENCE\nNet research: {science:0.#} / turn\nResearch stations: {Player.Research.ResearchStationResearchPerturn:0.#} / turn\nDisruption multiplier: {Player.Research.DisruptionMultiplier:0.##}x\nClick for research.");
        string research = "Choose research";
        string researchTip = "RESEARCH\nNo active research. Click to select a technology.";
        if (Player.Research.HasTopic)
        {
            var tech = Player.Research.Current;
            string turns = science > 0 ? $"{Math.Ceiling(Math.Max(0, tech.TechCost - tech.Progress) / science):0}t" : "stalled";
            research = $"{Player.Research.TopicLocText.Text} / {turns}";
            researchTip = $"RESEARCH\n{Player.Research.TopicLocText.Text}\nProgress: {tech.Progress:0.#} / {tech.TechCost:0.#} ({tech.PercentResearched:P0})\nEstimated remaining: {turns}\nClick for research.";
        }
        Set(DashboardItem.Research, research, researchTip);
        Set(DashboardItem.Population, $"{pop:0.#}B", $"POPULATION\nTotal: {pop:0.###} billion\nCapacity: {popMax:0.###} billion\nLast natural growth: {growth:+0.000;-0.000;0} billion / turn\nGrowth excludes migration and conquest; available after a colony turn.\nClick for colony management.");
        Set(DashboardItem.Freight, freighters == 0 ? "--" : $"{100f * trading / freighters:0}%", $"FREIGHT\nAssigned to trade: {trading} / {freighters}\nIdle / other orders: {freighters - trading}\nCargo aboard: {cargo:0.#} / {capacity:0.#}\nUnder construction: {Player.FreightersBeingBuilt}\nUtilization measures trade assignments, not cargo fullness.\nClick for freight utilization and construction.");
        Set(DashboardItem.Empire, colonies.ToString(), $"EMPIRE\nColonies: {colonies}\nColonized systems: {systems.Count}\nClick for empire management.");
        Set(DashboardItem.Fleets, ships.ToString(), $"SHIPS & FLEETS\nActive owned ships and stations: {ships}\nCombined strength: {strength:0.#}\nClick for fleet management.");
        var resources = new List<EmpireExoticBonuses>();
        var resourceTip = new StringBuilder("UNIQUE RESOURCES\n");
        foreach (var bonus in Player.GetExoticBonuses().Values)
        {
            resources.Add(bonus);
            resourceTip.AppendLine($"{Localizer.Token(bonus.Good.RefinedNameIndex)}: +{bonus.DynamicBonusString} {bonus.Good.ExoticBonusType}");
            resourceTip.AppendLine($"  Storage {bonus.CurrentStorage:0.#} / {Player.MaxExoticStorage:0.#}; refining {bonus.TotalRefinedPerTurn:0.#}, consumption {bonus.Consumption:0.#} / turn");
        }
        DashboardResources = resources.ToArray();
        resourceTip.Append("Click for resource bonuses and mining operations.");
        Set(DashboardItem.Resources, resources.Count == 0 ? "--" : "", resourceTip.ToString());
        bool idleResearch = Player.Research.NoTopic && !Player.Research.NoResearchLeft;
        Set(DashboardItem.Alerts, (starving + (idleResearch ? 1 : 0)).ToString(), $"EMPIRE ALERTS\nStarving colonies: {starving}\n{(idleResearch ? "No active research." : "Research requires no attention.")}\nClick for {(idleResearch ? "research" : "colony management")}.");
        BuildDashboardCards(food, prod, storage, foodGross, foodNet, prodGross, prodNet,
            pop, popMax, growth, freighters, trading, cargo, capacity);
        DashboardRefreshTick = Environment.TickCount;
        DashboardReady = true;
    }

    void Set(DashboardItem item, string value, string tip)
    {
        DashboardValues[(int)item] = value;
        DashboardTips[(int)item] = tip;
    }

    void DrawDashboard(SpriteBatch batch)
    {
        if (!DashboardReady || unchecked((uint)(Environment.TickCount - DashboardRefreshTick)) >= 1000)
            RefreshDashboard();
        batch.FillRectangle(new RectF(0, 0, Universe.ScreenWidth, DashboardHeight), new Color(5, 14, 20, 245).Premultiplied());
        batch.FillRectangle(new RectF(0, 0, Universe.ScreenWidth, 2), new Color(55, 65, 65));
        batch.FillRectangle(new RectF(0, DashboardHeight - 1, Universe.ScreenWidth, 1), DashboardGold * .6f);
        for (int i = 0; i < DashboardValues.Length; ++i)
        {
            var item = (DashboardItem)i;
            RectF rect = DashboardItemRect(item);
            bool hover = rect.HitTest(DashboardCursor);
            if (hover) batch.FillRectangle(rect, new Color(30, 45, 47));
            batch.FillRectangle(new RectF(rect.Right - 1, 7, 1, 22), new Color(62, 67, 62));
            if (item == DashboardItem.Alerts)
            {
                DrawDashboardText(batch, "! " + DashboardValues[i], rect, DashboardValues[i] == "0" ? Color.Gray : Color.Orange);
            }
            else if (item == DashboardItem.Resources && DashboardResources.Length > 0)
            {
                float size = Math.Min(20, (rect.W - 8) / DashboardResources.Length);
                for (int n = 0; n < DashboardResources.Length; ++n)
                {
                    var bonus = DashboardResources[n];
                    batch.Draw(ResourceManager.Texture("Goods/" + bonus.Good.UID),
                        new RectF(rect.X + 4 + n * size, (DashboardHeight - size) / 2, size, size),
                        bonus.DynamicBonus > 0 ? Color.White : Color.Gray);
                }
            }
            else
            {
                float iconSize = rect.W < 75 ? 14 : 20;
                batch.Draw(ResourceManager.Texture(DashboardIcons[i]), new RectF(rect.X + 5, (DashboardHeight - iconSize) / 2, iconSize, iconSize), Color.White);
                RectF textRect = new(rect.X + iconSize + 9, 0, rect.W - iconSize - 13, DashboardHeight);
                if (item == DashboardItem.Research) { textRect.Y = 1; textRect.H = 21; }
                if (i < 3 && Fonts.Arial11Bold.TextWidth(DashboardValues[i] + "  " + DashSigned(DashboardNet[i])) <= textRect.W)
                {
                    float deltaWidth = Fonts.Arial11Bold.TextWidth(DashSigned(DashboardNet[i])) + 6;
                    DrawDashboardText(batch, DashSigned(DashboardNet[i]), new RectF(textRect.Right - deltaWidth, 0, deltaWidth, DashboardHeight),
                        DashboardNet[i] < 0 ? Color.Salmon : Color.LightGreen);
                    textRect.W -= deltaWidth;
                }
                DrawDashboardText(batch, DashboardValues[i], textRect,
                    item == DashboardItem.Science ? DashboardCyan : DashboardGold);
            }
            if (item == DashboardItem.Research)
                DrawDashboardMeter(batch, new RectF(rect.X + 29, 25, rect.W - 37, 6),
                    Player.Research.HasTopic ? Player.Research.Current.PercentResearched : 0, 1, DashboardCyan);
        }
        string date = Universe.StarDateString;
        bool customSpeed = System.Array.IndexOf(DashboardSpeeds, Universe.UState.GameSpeed) < 0;
        DrawDashboardText(batch, date, new RectF(Universe.ScreenWidth - 249 - DashboardMusicWidth, 0, 70, DashboardHeight), DashboardGold);
        DrawDashboardMusic(batch);
        for (int i = 0; i < 5; ++i)
        {
            RectF rect = DashboardSpeedRect(i);
            bool selected = i == 0 ? Universe.UState.Paused : (i == 4 && customSpeed) || Math.Abs(Universe.UState.GameSpeed - DashboardSpeeds[i - 1]) < .001f;
            batch.FillRectangle(rect, selected ? new Color(20, 61, 68) : new Color(10, 20, 25));
            batch.DrawRectangle(rect, selected ? DashboardCyan : new Color(87, 82, 63));
            DrawDashboardText(batch, i == 0 ? (Universe.UState.Paused ? ">" : "II") : $"{(i == 4 && customSpeed ? Universe.UState.GameSpeed : DashboardSpeeds[i - 1]):0.##}x", rect, selected ? DashboardCyan : DashboardGold);
            if (rect.HitTest(DashboardCursor)) ToolTip.CreateTooltip(i == 0 ? "Pause / resume (Space)" : $"Set speed to {DashboardSpeeds[i - 1]:0.#}x; keeps the current pause state.\nCurrent speed: {Universe.UState.GameSpeed:0.###}x");
        }
        if (new RectF(Universe.ScreenWidth - 252 - DashboardMusicWidth, 0, 75, DashboardHeight).HitTest(DashboardCursor))
            ToolTip.CreateTooltip($"Stardate {Universe.StarDateString}\nCurrent speed: {Universe.UState.GameSpeed:0.###}x\n{(Universe.UState.Paused ? "Paused" : "Running")}");
    }

    void DrawDashboardMusic(SpriteBatch batch)
    {
        var manager = Universe.ScreenManager;
        RectF music = new(Universe.ScreenWidth - 174 - DashboardMusicWidth, 0, DashboardMusicWidth, DashboardHeight);
        if (DashboardMusicWidth > 84)
            DrawDashboardText(batch, manager.AmbientTrackTitle, new RectF(music.X + 4, 0, music.W - 86, DashboardHeight), DashboardGold);
        string[] labels = { "|<", manager.AmbientMusicPaused ? ">" : "II", ">|" };
        for (int i = 0; i < 3; ++i)
        {
            RectF rect = DashboardMusicRect(i);
            bool hover = rect.HitTest(DashboardCursor);
            batch.FillRectangle(rect, hover ? new Color(25, 47, 51) : new Color(10, 20, 25));
            batch.DrawRectangle(rect, new Color(65, 76, 76));
            DrawDashboardText(batch, labels[i], rect, DashboardCyan);
        }
        if (music.HitTest(DashboardCursor))
            ToolTip.CreateTooltip($"MUSIC - {manager.AmbientTrackTitle}\nPrevious track / pause or resume / next track\n{(Ship_Game.Audio.GameAudio.IsMusicDisabled ? "Music muted or audio unavailable" : manager.AmbientMusicPaused ? "Paused" : "Playing")}");
    }

    static void DrawDashboardText(SpriteBatch batch, string text, RectF rect, Color color)
    {
        var font = Fonts.Arial11Bold;
        // Truncate labels instead of scaling the whole HUD into unreadable text.
        if (font.TextWidth(text) > rect.W)
        {
            while (text.Length > 0 && font.TextWidth(text + "...") > rect.W) text = text.Substring(0, text.Length - 1);
            text += "...";
        }
        batch.DrawString(font, text, new Vector2(rect.X + Math.Max(0, (rect.W - font.TextWidth(text)) / 2), rect.Y + (rect.H - font.LineSpacing) / 2), color);
    }

    public bool HandleDashboardInput(InputState input)
    {
        DashboardCursor = input.CursorPosition;
        UpdateDashboardHover();
        if (DashboardCardVisible && DashboardCardRect().HitTest(DashboardCursor))
            return !input.WasAnyKeyPressed;
        if (!new RectF(0, 0, Universe.ScreenWidth, DashboardHeight).HitTest(DashboardCursor)) return false;
        // Keyboard shortcuts still work while the cursor rests on the bar.
        if (!input.InGameSelect) return !input.WasAnyKeyPressed;
        for (int i = 0; i < 3; ++i)
        {
            if (!DashboardMusicRect(i).HitTest(DashboardCursor)) continue;
            if (i == 1) Universe.ScreenManager.ToggleAmbientMusic();
            else Universe.ScreenManager.SkipAmbientMusic(i == 0 ? -1 : 1);
            return true;
        }
        for (int i = 0; i < 5; ++i)
        {
            if (!DashboardSpeedRect(i).HitTest(DashboardCursor)) continue;
            if (i == 0) Universe.UState.Paused = !Universe.UState.Paused;
            else Universe.UState.GameSpeed = DashboardSpeeds[i - 1];
            return true;
        }
        for (int i = 0; i < DashboardValues.Length; ++i)
        {
            if (!DashboardItemRect((DashboardItem)i).HitTest(DashboardCursor)) continue;
            switch ((DashboardItem)i)
            {
                case DashboardItem.Money: OpenNavigation("Budget"); break;
                case DashboardItem.Science:
                case DashboardItem.Research: OpenNavigation("Research"); break;
                case DashboardItem.Freight: Universe.FreighterUtilizationWindow?.ToggleVisibility(); break;
                case DashboardItem.Resources: Universe.ExoticBonusesWindow?.ToggleVisibility(); break;
                case DashboardItem.Fleets: OpenNavigation("Fleets"); break;
                case DashboardItem.Alerts: OpenNavigation(Player.Research.NoTopic && !Player.Research.NoResearchLeft ? "Research" : "Empire"); break;
                default: OpenNavigation("Empire"); break;
            }
            return true;
        }
        return true;
    }
}

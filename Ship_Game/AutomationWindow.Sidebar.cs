using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game;

public sealed partial class AutomationWindow
{
    EmpireAssetsPanel Sidebar;
    readonly List<AutomationRow> SidebarRows = new();
    readonly bool[] ClosedGroups = { false, false, true };
    readonly PanelSkin[] Plates = new PanelSkin[4];
    int ScrollRow, ChoiceOffset;
    AutomationRow Choosing;
    readonly List<string> Choices = new();
    RectF ChoiceAnchor;
    bool HadResearch, HadMining;
    static readonly Color Gold = new(222, 187, 113);
    static readonly Color Dim = new(147, 159, 163);

    sealed class AutomationRow
    {
        public string Name;
        public LocalizedText Tooltip;
        public Ref<bool> Setting;
        public Ref<bool> AutomaticDesign;
        public Func<DropOptions<int>> Designs;
        public Action<string> SelectDesign;
        public Func<bool> Available;
        public int Group, Plate;
        public bool Header => Setting == null;
        public bool Enabled => Available == null || Available();
        public int Height => Header ? 27 : Designs != null ? 56 : 30;
    }

    internal void AttachSidebar(EmpireAssetsPanel sidebar) => Sidebar = sidebar;

    internal void SetSidebarOpen(bool open)
    {
        CloseSidebarChoices();
        if (open && !IsOpen)
        {
            LoadContent(); // Retains the existing design filtering and saved selections.
            BuildSidebarRows();
        }
        IsOpen = open;
        GameAudio.AcceptClick();
    }

    internal void CloseSidebarChoices()
    {
        Choosing = null;
        Choices.Clear();
        ChoiceOffset = 0;
    }

    void BuildSidebarRows()
    {
        SidebarRows.Clear();
        void Header(string name, int group) => SidebarRows.Add(new AutomationRow { Name = name, Group = group });
        AutomationRow Toggle(string name, Expression<Func<bool>> binding, LocalizedText tooltip, int group = 1)
        {
            var row = new AutomationRow { Name = name, Setting = new Ref<bool>(binding), Tooltip = tooltip,
                Group = group, Plate = 3 };
            SidebarRows.Add(row);
            return row;
        }
        void Ship(string name, Expression<Func<bool>> binding, LocalizedText tooltip,
                  Func<DropOptions<int>> designs, Action<string> select,
                  Expression<Func<bool>> automatic = null, Func<bool> available = null, int plate = 1)
        {
            AutomationRow row = Toggle(name, binding, tooltip, 0);
            row.Designs = designs;
            row.SelectDesign = select;
            row.AutomaticDesign = automatic == null ? null : new Ref<bool>(automatic);
            row.Available = available;
            row.Plate = plate;
        }

        Header("SHIPS & STATIONS", 0);
        Ship("Exploration", () => Screen.Player.AutoExplore, GameText.YourEmpireWillAutomaticallyManage,
            () => ScoutDropDown, name => Screen.Player.data.CurrentAutoScout = name);
        Ship("Colonization", () => Screen.Player.AutoColonize, GameText.YourEmpireWillAutomaticallyCreate,
            () => ColonyShipDropDown, name => Screen.Player.data.CurrentAutoColony = name,
            () => Screen.Player.AutoPickBestColonizer);
        Ship("Trade", () => Screen.Player.AutoFreighters, GameText.YourEmpireWillAutomaticallyManage2,
            () => FreighterDropDown, name => Screen.Player.data.CurrentAutoFreighter = name,
            () => Screen.Player.AutoPickBestFreighter);
        Ship("Projectors", () => Screen.Player.AutoBuildSpaceRoads, GameText.YourEmpireWillAutomaticallyCreate2,
            () => ConstructorDropDown, name => Screen.Player.data.CurrentConstructor = name,
            () => Screen.Player.AutoPickConstructors);
        if (ResearchStationsEnabled)
            Ship("Research stations", () => Screen.Player.AutoBuildResearchStations, GameText.AutoBuildResearchStationTip,
                () => ResearchStationDropDown, name => Screen.Player.data.CurrentResearchStation = name,
                () => Screen.Player.AutoPickBestResearchStation, () => Screen.Player.CanBuildResearchStations, 2);
        if (MiningOpsEnabled)
            Ship("Mining stations", () => Screen.Player.AutoBuildMiningStations, GameText.AutoBuildMiningStationTip,
                () => MiningStationDropDown, name => Screen.Player.data.CurrentMiningStation = name,
                () => Screen.Player.AutoPickBestMiningStation, () => Screen.Player.CanBuildMiningStations, 2);

        Header("EMPIRE MANAGEMENT", 1);
        Toggle("Auto research", () => Screen.Player.AutoResearch, GameText.YourEmpireWillAutomaticallySelect);
        Toggle("Auto taxes", () => Screen.Player.AutoTaxes, GameText.YourEmpireWillAutomaticallyManage3);
        Toggle("Auto terraformers", () => Screen.Player.AutoBuildTerraformers, GameText.AutoBuildTerraformersTip);
        Toggle("Rush construction", () => RushConstruction, GameText.RushAllConstructionTip);
        Toggle("Inter-empire trade", () => UState.P.AllowPlayerInterTrade, GameText.AllowPlayerInterTradeTip);
        Toggle("Prioritize projectors", () => UState.P.PrioitizeProjectors, GameText.PrioritizeProjectorTip);

        Header("ALERTS & POLICIES", 2);
        // Positive labels invert the saved suppression flags so checked always means enabled.
        AddAlert("Building alerts", () => UState.P.SuppressOnBuildNotifications, "Show notifications when buildings finish construction.");
        AddAlert("Inhibition warnings", () => UState.P.DisableInhibitionWarning, "Show inhibition warning notifications.");
        AddAlert("Volcano warnings", () => UState.P.DisableVolcanoWarning, "Show volcano activation and deactivation notifications.");
        AddAlert("Crash-site warnings", () => UState.P.DisableCrashSiteWarning, "Show crash-site warning notifications.");
        Toggle("Starvation warnings", () => UState.P.EnableStarvationWarning, GameText.EnableStarvationWarningTip, 2);
        HadResearch = Screen.Player.CanBuildResearchStations;
        HadMining = Screen.Player.CanBuildMiningStations;

        void AddAlert(string name, Expression<Func<bool>> suppressed, LocalizedText tooltip)
        {
            var saved = new Ref<bool>(suppressed);
            var row = Toggle(name, suppressed, tooltip, 2);
            row.Setting = new Ref<bool>(() => !saved.Value, value => saved.Value = !value);
        }
    }

    List<AutomationRow> DisplayRows(RectF body)
    {
        if (HadResearch != Screen.Player.CanBuildResearchStations || HadMining != Screen.Player.CanBuildMiningStations)
        {
            HadResearch = Screen.Player.CanBuildResearchStations;
            HadMining = Screen.Player.CanBuildMiningStations;
            UpdateDropDowns();
            CloseSidebarChoices();
        }
        var rows = SidebarRows.FindAll(r => r.Header || !ClosedGroups[r.Group]);
        ScrollRow = Math.Clamp(ScrollRow, 0, LastScrollRow(rows, body));
        return rows;
    }

    static int LastScrollRow(List<AutomationRow> rows, RectF body)
    {
        float used = 0;
        int first = rows.Count;
        while (first > 0 && used + rows[first - 1].Height <= body.H)
            used += rows[--first].Height;
        return Math.Max(0, Math.Min(first, rows.Count - 1));
    }

    IEnumerable<(AutomationRow Row, RectF Rect)> VisibleAutomationRows(List<AutomationRow> rows, RectF body)
    {
        float y = body.Y;
        for (int i = ScrollRow; i < rows.Count; ++i)
        {
            AutomationRow row = rows[i];
            if (y + row.Height > body.Bottom) yield break;
            yield return (row, new RectF(body.X, y, body.W - 7, row.Height - 3));
            y += row.Height;
        }
    }

    static RectF DesignRect(RectF row) => new(row.X + 25, row.Y + 28, row.W - 33, 20);

    internal void HandleSidebarInput(InputState input, RectF body)
    {
        if (!IsOpen || !body.HitTest(input.CursorPosition)) return;
        List<AutomationRow> rows = DisplayRows(body);
        if (input.ScrollIn) ScrollRow = Math.Max(0, ScrollRow - 1);
        if (input.ScrollOut) ScrollRow = Math.Min(LastScrollRow(rows, body), ScrollRow + 1);
        if (!input.LeftMouseClick) return;
        foreach (var (row, rect) in VisibleAutomationRows(rows, body))
        {
            if (!rect.HitTest(input.CursorPosition)) continue;
            if (row.Header)
            {
                ClosedGroups[row.Group] = !ClosedGroups[row.Group];
                ScrollRow = Math.Min(ScrollRow, LastScrollRow(DisplayRows(body), body));
            }
            else if (row.Enabled)
            {
                if (row.Designs != null && DesignRect(rect).HitTest(input.CursorPosition))
                {
                    if (row.Setting.Value) OpenChoices(row, DesignRect(rect));
                }
                else row.Setting.Value = !row.Setting.Value;
            }
            GameAudio.AcceptClick();
            return;
        }
    }

    void OpenChoices(AutomationRow row, RectF anchor)
    {
        CloseSidebarChoices();
        // Re-check available designs every time, including designs unlocked while open.
        UpdateDropDowns();
        if (row.AutomaticDesign != null) Choices.Add("Automatic");
        DropOptions<int> drop = row.Designs();
        var entries = new DropOptions<int>.Entry[drop.Count];
        drop.CopyTo(entries);
        foreach (var entry in entries) Choices.Add(entry.Name.Text);
        if (Choices.Count == 0) return;
        Choosing = row;
        ChoiceAnchor = anchor;
    }

    int ChoicePageSize(RectF body) => Math.Max(1, Math.Min(8, (int)(body.H / 23)));

    RectF ChoiceRect(RectF body)
    {
        float height = Math.Min(ChoicePageSize(body), Choices.Count) * 23;
        return new RectF(body.X, Math.Clamp(ChoiceAnchor.Bottom, body.Y, Math.Max(body.Y, body.Bottom - height)),
            body.W - 7, height);
    }

    internal bool HandleSidebarPopup(InputState input, RectF body)
    {
        if (Choosing == null) return false;
        if (!Choosing.Enabled || !Choosing.Setting.Value)
        {
            CloseSidebarChoices();
            return false;
        }
        if (input.Escaped || input.RightMouseClick)
        {
            CloseSidebarChoices();
            return true;
        }
        RectF popup = ChoiceRect(body);
        bool over = popup.HitTest(input.CursorPosition);
        if (input.ScrollIn || input.ScrollOut)
        {
            if (over)
                ChoiceOffset = Math.Clamp(ChoiceOffset + (input.ScrollOut ? 1 : -1), 0,
                    Math.Max(0, Choices.Count - ChoicePageSize(body)));
            return true;
        }
        if (input.LeftMouseClick)
        {
            if (over)
            {
                int index = ChoiceOffset + (int)((input.CursorPosition.Y - popup.Y) / 23);
                if (index < Choices.Count)
                {
                    if (Choosing.AutomaticDesign != null) Choosing.AutomaticDesign.Value = index == 0;
                    if (Choosing.AutomaticDesign == null || index != 0)
                    {
                        string name = Choices[index];
                        Choosing.Designs().SetActiveEntry(name);
                        Choosing.SelectDesign(name);
                    }
                    GameAudio.AcceptClick();
                }
            }
            CloseSidebarChoices();
            return true; // Dismissal cannot toggle a row or select a galaxy object underneath.
        }
        return over;
    }

    void Plate(SpriteBatch batch, int index, RectF rect, Color tint)
    {
        if (Plates[index] == null)
        {
            string[] names = { "automation_header_v2", "automation_ship_v2", "automation_station_v2", "automation_empire_v2" };
            // Only the header and ship originals have exterior canvas padding.
            float top = index == 0 ? 0.28f : index == 1 ? 0.195f : 0;
            float bottom = index == 0 ? 0.71f : index == 1 ? 0.805f : 1;
            Plates[index] = new PanelSkin(ResourceManager.Texture("NewUI/" + names[index]), top, bottom);
        }
        Plates[index].Draw(batch, rect, tint);
    }

    // Fixed-size corners/edges surround a quiet surface. The decorative blueprint is
    // fitted uniformly on the right, so neither a short header nor a taller design row
    // squashes its artwork. New backgrounds can be added as independent PNG files.
    sealed class PanelSkin
    {
        readonly SubTexture[] Pieces = new SubTexture[9];
        readonly SubTexture Surface, Motif;

        public PanelSkin(SubTexture original, float top, float bottom)
        {
            int y = (int)(original.Height * top);
            int height = (int)(original.Height * bottom) - y;
            var source = new SubTexture(original.Name, original.X, original.Y + y, original.Width, height,
                original.Texture, original.TexturePath);
            int border = Math.Max(1, (int)(Math.Min(source.Width, source.Height) * 0.08f));
            int[] xs = { 0, border, source.Width - border, source.Width };
            int[] ys = { 0, border, source.Height - border, source.Height };
            SubTexture Part(int x, int py, int w, int h) => new(source.Name, source.X + x, source.Y + py,
                w, h, source.Texture, source.TexturePath);
            for (int row = 0; row < 3; ++row)
                for (int col = 0; col < 3; ++col)
                    Pieces[row * 3 + col] = Part(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]);
            Surface = Part(border * 2, border * 2, source.Width / 4, source.Height - border * 4);
            Motif = Part(source.Width / 2, border, source.Width / 2 - border, source.Height - border * 2);
        }

        public void Draw(SpriteBatch batch, RectF rect, Color tint)
        {
            const float border = 3;
            var inside = new RectF(rect.X + border, rect.Y + border, rect.W - border * 2, rect.H - border * 2);
            batch.Draw(Surface, inside, tint);
            float scale = Math.Min(inside.H / Motif.Height, inside.W * 0.55f / Motif.Width);
            float w = Motif.Width * scale, h = Motif.Height * scale;
            batch.Draw(Motif, new RectF(inside.Right - w, inside.Y + (inside.H - h) / 2, w, h), tint);
            float[] xs = { rect.X, inside.X, inside.Right, rect.Right };
            float[] ys = { rect.Y, inside.Y, inside.Bottom, rect.Bottom };
            for (int row = 0; row < 3; ++row)
                for (int col = 0; col < 3; ++col)
                    if (row != 1 || col != 1)
                        batch.Draw(Pieces[row * 3 + col], new RectF(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]), tint);
        }
    }

    static string FitLabel(string text, float width)
    {
        text ??= "";
        if (Fonts.Arial10.TextWidth(text) <= width) return text;
        while (text.Length > 0 && Fonts.Arial10.TextWidth(text + "...") > width)
            text = text.Substring(0, text.Length - 1);
        return text + "...";
    }

    static void Label(SpriteBatch batch, string text, float x, float y, Color color)
        => batch.DrawString(Fonts.Arial10, text, new Vector2(x, y), color);

    internal void DrawSidebarHeader(SpriteBatch batch, RectF header)
    {
        Plate(batch, 3, header, Color.White);
        batch.DrawString(Fonts.Arial12Bold, "AI AUTOMATION", new Vector2(header.X + 7, header.Y + 7), Gold);
        Label(batch, "<", header.Right - 18, header.Y + 7, Gold);
    }

    internal void DrawSidebar(SpriteBatch batch, RectF body, Vector2 cursor)
    {
        List<AutomationRow> rows = DisplayRows(body);
        foreach (var (row, rect) in VisibleAutomationRows(rows, body))
        {
            Plate(batch, row.Header ? 0 : row.Plate, rect, row.Enabled ? Color.White : new Color(150, 150, 150));
            if (row.Header)
            {
                Label(batch, ClosedGroups[row.Group] ? ">" : "v", rect.X + 7, rect.Y + 6, Gold);
                Label(batch, row.Name, rect.X + 23, rect.Y + 6, Gold);
                continue;
            }
            bool enabled = row.Enabled;
            bool on = row.Setting.Value;
            var check = new RectF(rect.X + 7, rect.Y + 7, 13, 13);
            batch.FillRectangle(check, new Color(6, 13, 18));
            batch.DrawRectangle(check, on && enabled ? Gold : Dim);
            if (on)
                batch.Draw(ResourceManager.Texture("NewUI/Checkmark10x"), check, enabled ? Gold : Dim);
            // Shade just the label area to protect readability over blueprint details.
            batch.FillRectangle(new RectF(rect.X + 24, rect.Y + 4, rect.W - 30, 20), new Color(0, 5, 10, 125).Premultiplied());
            float stateWidth = row.Designs != null ? 33 : 0;
            Label(batch, FitLabel(row.Name, rect.W - 32 - stateWidth), rect.X + 26, rect.Y + 7, enabled ? Color.Wheat : Dim);
            if (row.Designs != null)
            {
                Label(batch, on ? "ON" : "OFF", rect.Right - 28, rect.Y + 7, enabled && on ? Gold : Dim);
                RectF design = DesignRect(rect);
                batch.FillRectangle(design, new Color(4, 10, 16, 235).Premultiplied());
                batch.DrawRectangle(design, enabled && on ? new Color(117, 140, 145) : new Color(56, 66, 70));
                DropOptions<int> drop = row.Designs();
                string selected = !enabled ? "Technology required" : row.AutomaticDesign?.Value == true ? "Automatic"
                    : drop.Count > 0 ? drop.ActiveName : "No designs available";
                Label(batch, FitLabel(selected, design.W - 22), design.X + 5, design.Y + 3, enabled && on ? Color.Wheat : Dim);
                Label(batch, "v", design.Right - 13, design.Y + 3, enabled && on ? Gold : Dim);
                if (Choosing == null && design.HitTest(cursor))
                    ToolTip.CreateTooltip(!enabled ? "Unlock the required station technology first."
                        : !on ? "Enable this automation task to select its design."
                        : selected + (row.AutomaticDesign != null ? "\nChoose Automatic or a specific ship design." : "\nChoose a scout design."));
                else if (Choosing == null && rect.HitTest(cursor)) ToolTip.CreateTooltip(row.Tooltip);
            }
            else if (Choosing == null && rect.HitTest(cursor)) ToolTip.CreateTooltip(row.Tooltip);
        }
        int last = LastScrollRow(rows, body);
        if (last > 0)
        {
            var track = new RectF(body.Right - 4, body.Y, 3, body.H);
            batch.FillRectangle(track, new Color(31, 49, 55));
            float thumb = Math.Max(20, body.H * (rows.Count - last) / rows.Count);
            batch.FillRectangle(new RectF(track.X, track.Y + (track.H - thumb) * ScrollRow / last, 3, thumb), Gold);
            if (track.HitTest(cursor)) ToolTip.CreateTooltip("Scroll to see more automation settings.");
        }
        if (Choosing != null) DrawChoices(batch, body, cursor);
    }

    void DrawChoices(SpriteBatch batch, RectF body, Vector2 cursor)
    {
        RectF popup = ChoiceRect(body);
        batch.FillRectangle(popup, new Color(7, 17, 24));
        batch.DrawRectangle(popup, Gold);
        int count = Math.Min(ChoicePageSize(body), Choices.Count - ChoiceOffset);
        for (int i = 0; i < count; ++i)
        {
            var rect = new RectF(popup.X + 1, popup.Y + i * 23 + 1, popup.W - 2, 21);
            bool hover = rect.HitTest(cursor);
            if (hover) batch.FillRectangle(rect, new Color(59, 53, 34));
            Label(batch, FitLabel(Choices[ChoiceOffset + i], rect.W - 14), rect.X + 5, rect.Y + 4, hover ? Color.White : Color.Wheat);
            if (hover) ToolTip.CreateTooltip(Choices[ChoiceOffset + i]);
        }
        if (Choices.Count > count)
        {
            float thumb = popup.H * count / Choices.Count;
            batch.FillRectangle(new RectF(popup.Right - 3, popup.Y + (popup.H - thumb) * ChoiceOffset / (Choices.Count - count), 2, thumb), Gold);
        }
    }
}

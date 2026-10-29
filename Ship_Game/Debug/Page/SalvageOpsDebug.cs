using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page;

public class SalvageOpsDebug : DebugPage
{
    // the columns from the table's left edge
    const float ShipX = 130;
    const float OwnerX = 330;
    const float ProductionX = 530;
    const float TechsX = 650;
    const float TechsWidth = 400;
    const float SalvagerX = 1070;
    const float ProgressX = 1270;
    const float TableWidth = 1470;
    const int RowsPerScroll = 3;

    int FirstRow;
    RectF TableRect;
    bool Overflows;
    // a derelict's techs never change, so their lines are made once
    readonly Map<int, string[]> TechLinesById = new();

    public SalvageOpsDebug(DebugInfoScreen parent) : base(parent, DebugModes.SalvageOps)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        if (!Universe.P.SalvageOps)
        {
            Text.SetCursor(Parent.Win.X + 100, Parent.Win.Y + 100, Color.White);
            Text.String("Salvage Ops disabled. No data available");
            return;
        }

        Derelict[] rows = Universe.Derelicts.Current.Filter(d => d.CanBeSalvaged);
        Array.Sort(rows, BySystemAndName);

        float left = Parent.Win.X + 10;
        float lineHeight = Text.Font.LineSpacing;
        float bottom = Parent.ScreenHeight - 20 - lineHeight; // a line left for the scroll note
        Text.SetCursor(left, Parent.Win.Y + 50, Color.White);
        Text.String($"Salvageable: {rows.Length}/{Derelicts.SalvageableCap(Universe)}   Drifting: {Universe.Derelicts.NumDrifting}/{Derelicts.DriftingCap(Universe)}"
                    + $"   Derelict chance: {Universe.Derelicts.Chance(Universe).String(1)}%");
        Text.NewLine();
        DrawHeaders();

        TableRect = new(left, Text.Cursor.Y, TableWidth, bottom - Text.Cursor.Y);
        FirstRow = Math.Min(FirstRow, LastScreenStart(rows, TableRect.H, lineHeight));
        int end = FirstRow;
        for (; end < rows.Length; ++end)
        {
            string[] techs = TechLines(rows[end]);
            if (Text.Cursor.Y + techs.Length * lineHeight > bottom)
                break;
            DrawRow(rows[end], techs);
        }
        Overflows = FirstRow > 0 || end < rows.Length;
        if (Overflows)
            Text.String(Color.Gray, $"Derelicts {FirstRow + 1}-{end} of {rows.Length}, the mouse wheel over the table scrolls");

        base.Draw(batch, elapsed);
    }

    // the first row of the last screen of rows, as far as the table scrolls down
    int LastScreenStart(Derelict[] rows, float room, float lineHeight)
    {
        float height = 0f;
        for (int i = rows.Length - 1; i >= 0; --i)
        {
            height += TechLines(rows[i]).Length * lineHeight;
            if (height > room)
                return Math.Min(i + 1, rows.Length - 1);
        }
        return 0;
    }

    static string ShipNameOf(Derelict d) => d.ShipName.NotEmpty() ? d.ShipName : d.Design.Name;

    static int BySystemAndName(Derelict a, Derelict b)
    {
        int bySystem = string.CompareOrdinal(a.System?.Name, b.System?.Name);
        if (bySystem != 0)
            return bySystem;
        int byName = string.CompareOrdinal(ShipNameOf(a), ShipNameOf(b));
        return byName != 0 ? byName : a.Id.CompareTo(b.Id);
    }

    void DrawHeaders()
    {
        Text.Color = Color.Wheat;
        Text.String(0, "System", false);
        Text.String(ShipX, "Ship", false);
        Text.String(OwnerX, "Previous owner", false);
        Text.String(ProductionX, "Production", false);
        Text.String(TechsX, "Techs", false);
        Text.String(SalvagerX, "Salvager", false);
        Text.String(ProgressX, "Progress");
        Text.Color = Color.White;
    }

    void DrawRow(Derelict d, string[] techs)
    {
        Text.String(0, d.System?.Name ?? "", false);
        Text.String(ShipX, ShipNameOf(d), false);
        Text.String(OwnerX, d.Owner?.Name ?? "", false);
        Text.String(ProductionX, $"{d.ProductionLeft.String(1)}/{d.Production.String(1)}", false);
        if (Salvager(d, out SalvageDerelict.Stage stage, out SalvageDerelict order) is { } salvager)
        {
            Text.Color = salvager.EmpireColor;
            Text.String(SalvagerX, salvager.Name, false);
            Text.Color = Color.White;
            Text.String(ProgressX, Progress(salvager, d, stage, order), false);
        }
        for (int i = 0; i < techs.Length; ++i)
            Text.String(TechsX, techs[i]);
    }

    // the empire holding it, or before any claim the first with an order on it, with how far that empire's work has come
    Empire Salvager(Derelict d, out SalvageDerelict.Stage stage, out SalvageDerelict order)
    {
        if (d.Claimant is { } claimant)
        {
            stage = SalvageDerelict.StageOf(claimant, d, out _, out order);
            return claimant;
        }
        var empires = Universe.Empires;
        for (int i = 0; i < empires.Count; ++i)
        {
            stage = SalvageDerelict.StageOf(empires[i], d, out Empire claimedNow, out order);
            if (claimedNow != null && claimedNow != empires[i])
            {
                stage = SalvageDerelict.StageOf(claimedNow, d, out _, out order); // claimed since the first read: its holder's own work
                return claimedNow;
            }
            if (stage != SalvageDerelict.Stage.None)
                return empires[i];
        }
        stage = SalvageDerelict.Stage.None;
        order = null;
        return null;
    }

    static string Progress(Empire salvager, Derelict d, SalvageDerelict.Stage stage, SalvageDerelict order) => stage switch
    {
        SalvageDerelict.Stage.WaitingForShip   => "waiting for a salvage ship",
        SalvageDerelict.Stage.ShipOrdered      => "salvage ship ordered",
        SalvageDerelict.Stage.ShipOnItsWay     => "salvage ship on its way",
        SalvageDerelict.Stage.PlatformOrdered  => "platform ordered",
        SalvageDerelict.Stage.BuildingPlatform => "building platform",
        SalvageDerelict.Stage.BreakingIn       => $"breaking in, {order?.BreakInTurnsLeft} turns left",
        SalvageDerelict.Stage.Recovering       => $"recovering {Percent(DerelictRecovery.Progress(salvager, d, order?.RecoveryWork ?? 0f))}",
        _                                      => $"salvaging {Percent(d.Production > 0f ? 1f - d.ProductionLeft / d.Production : 0f)}",
    };

    static string Percent(float share) => $"{(share * 100f).String(1)}%";

    // its techs' names, broken into lines that fit the column
    string[] TechLines(Derelict d)
    {
        if (TechLinesById.TryGetValue(d.Id, out string[] made))
            return made;
        if (d.Techs.Length == 0)
            return TechLinesById[d.Id] = new[] { "none" };

        var lines = new Array<string>();
        string line = "";
        for (int i = 0; i < d.Techs.Length; ++i)
        {
            string name = ResourceManager.TryGetTech(d.Techs[i], out Technology tech) ? tech.Name.Text : d.Techs[i];
            string longer = line.Length == 0 ? name : $"{line}, {name}";
            if (line.Length > 0 && Text.Font.MeasureString(longer).X > TechsWidth)
            {
                lines.Add(line + ",");
                line = name;
            }
            else
            {
                line = longer;
            }
        }
        lines.Add(line);
        return TechLinesById[d.Id] = lines.ToArray();
    }

    public override bool HandleInput(InputState input)
    {
        if (Visible && Overflows && (input.ScrollIn || input.ScrollOut) && TableRect.HitTest(input.CursorPosition))
        {
            FirstRow = Math.Max(0, FirstRow + (input.ScrollOut ? RowsPerScroll : -RowsPerScroll));
            return true;
        }
        return base.HandleInput(input);
    }
}

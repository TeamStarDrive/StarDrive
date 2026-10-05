using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.AI;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game;

public sealed partial class EmpireUIOverlay
{
    internal sealed class DashboardRow
    {
        public string Label, Value, Icon, Detail;
        public Color Color = DashboardGold;
        public float Amount, Maximum;
        public bool Meter;
        public int Height => Detail != null ? 55 : Meter ? 43 : 25;
    }

    readonly List<DashboardRow>[] DashboardCards = new List<DashboardRow>[11];
    int DashboardHover = -1, DashboardHoverTick;
    bool DashboardCardVisible => DashboardHover >= 0 && DashboardReady
        && unchecked((uint)(Environment.TickCount - DashboardHoverTick)) >= 250;

    internal IReadOnlyList<DashboardRow> DashboardRows(DashboardItem item) => DashboardCards[(int)item];

    void UpdateDashboardHover()
    {
        if (DashboardCardVisible && DashboardCardRect().HitTest(DashboardCursor)) return;
        int hovered = -1;
        for (int i = 0; i < DashboardCards.Length; ++i)
            if (DashboardItemRect((DashboardItem)i).HitTest(DashboardCursor)) { hovered = i; break; }
        if (hovered == DashboardHover) return;
        DashboardHover = hovered;
        DashboardHoverTick = Environment.TickCount;
    }

    RectF DashboardCardRect()
    {
        float height = 62;
        if (DashboardHover >= 0 && DashboardCards[DashboardHover] != null)
            foreach (var row in DashboardCards[DashboardHover]) height += row.Height;
        float width = DashboardHover == (int)DashboardItem.Resources ? 450 : 390;
        width = Math.Min(width, Universe.ScreenWidth - 16);
        float x = DashboardItemRect((DashboardItem)Math.Max(0, DashboardHover)).X;
        return new RectF(Math.Max(8, Math.Min(x, Universe.ScreenWidth - width - 8)), DashboardHeight, width, height);
    }

    internal static string DashboardBonusEffect(ExoticBonusType type) => type switch
    {
        ExoticBonusType.Credits => "Credit income",
        ExoticBonusType.ShieldRecharge => "Shield recharge",
        ExoticBonusType.DamageReduction => "Damage reduction",
        ExoticBonusType.Production => "Production output",
        ExoticBonusType.RepairRate => "Ship repair rate",
        ExoticBonusType.WarpSpeed => "Warp speed",
        _ => "Empire bonus"
    };

    void BuildDashboardCards(float food, float prod, float storage, float foodGross, float foodNet,
        float prodGross, float prodNet, float pop, float popMax, float growth,
        int freighters, int trading, float cargo, float capacity)
    {
        // Simple entries retain their existing concise statistics; richer cards below
        // supply structured values and charts from the same simulation sources.
        for (int i = 0; i < DashboardCards.Length; ++i)
        {
            DashboardCards[i] = new List<DashboardRow>();
            string[] lines = DashboardTips[i].Split('\n');
            for (int n = 1; n < lines.Length; ++n)
            {
                string line = lines[n];
                if (line.StartsWith("Click") || line.StartsWith("Growth excludes") || line.StartsWith("Reserves are")
                    || line.StartsWith("Net output is") || line.StartsWith("Utilization measures")) continue;
                int colon = line.IndexOf(':');
                DashboardCards[i].Add(new DashboardRow { Label = colon < 0 ? line : line.Substring(0, colon),
                    Value = colon < 0 ? "" : line.Substring(colon + 1).Trim() });
            }
        }

        List<DashboardRow> rows = null;
        void Start(DashboardItem item) { rows = DashboardCards[(int)item]; rows.Clear(); }
        void Row(string label, string value, Color? color = null)
            => rows.Add(new DashboardRow { Label = label, Value = value, Color = color ?? DashboardGold });
        void Meter(string label, float value, float max, Color color, string display = null)
            => rows.Add(new DashboardRow { Label = label, Value = display ?? $"{value:0.#} / {max:0.#}",
                Amount = value, Maximum = max, Meter = true, Color = color });

        Start(DashboardItem.Money);
        float spending = Player.AllSpending + Player.MoneySpendOnProductionNow;
        float moneyScale = Math.Max(Player.GrossIncome, spending);
        Row("Treasury", $"{Player.Money:0.#} credits");
        Meter("Income / turn", Player.GrossIncome, moneyScale, Color.LightGreen, DashSigned(Player.GrossIncome));
        Meter("Expenses / turn", spending, moneyScale, Color.Salmon, "-" + DashNumber(spending));
        Row("Balance / turn", DashSigned(DashboardNet[0]), DashboardNet[0] < 0 ? Color.Salmon : Color.LightGreen);
        Row("Planetary taxes", DashNumber(Player.GrossPlanetIncome));
        Row("Trade & other income", DashNumber(Player.GrossIncome - Player.GrossPlanetIncome));
        Row("Ships & stations", "-" + DashNumber(Player.TotalShipMaintenance));
        Row("Buildings", "-" + DashNumber(Player.TotalBuildingMaintenance));
        Row("Troops", "-" + DashNumber(Player.TroopCostOnPlanets));
        Row("Production spending", "-" + DashNumber(Player.MoneySpendOnProductionThisTurn + Player.MoneySpendOnProductionNow));
        Row("Espionage", "-" + DashNumber(Player.EspionageCostLastTurn));

        Start(DashboardItem.Food);
        Meter("Colony reserves", food, storage, Color.LightGreen);
        float consumption = Math.Max(0, foodGross - foodNet);
        Meter("Generation / turn", foodGross, Math.Max(foodGross, consumption), Color.LightGreen, DashSigned(foodGross));
        Meter("Consumption / turn", consumption, Math.Max(foodGross, consumption), Color.Salmon, "-" + DashNumber(consumption));
        Row("Balance / turn", DashSigned(foodNet), foodNet < 0 ? Color.Salmon : Color.LightGreen);

        Start(DashboardItem.Production);
        Meter("Colony reserves", prod, storage, DashboardGold);
        Row("Gross output / turn", DashNumber(prodGross));
        Row("Available output / turn", DashSigned(prodNet), prodNet < 0 ? Color.Salmon : Color.LightGreen);

        Start(DashboardItem.Population);
        Meter("Population / capacity", pop, popMax, DashboardCyan, $"{pop:0.##}B / {popMax:0.##}B");
        Row("Natural growth / turn", $"{growth:+0.000;-0.000;0}B", growth < 0 ? Color.Salmon : Color.LightGreen);

        Start(DashboardItem.Research);
        if (Player.Research.HasTopic)
        {
            var tech = Player.Research.Current;
            Row(Player.Research.TopicLocText.Text, "");
            Meter("Research progress", tech.Progress, tech.TechCost, DashboardCyan, $"{tech.PercentResearched:P0}   |   {tech.Progress:0.#} / {tech.TechCost:0.#}");
            Row("Science / turn", DashNumber(Player.Research.NetResearch), DashboardCyan);
            Row("Time remaining", Player.Research.NetResearch > 0
                ? $"{Math.Ceiling(Math.Max(0, tech.TechCost - tech.Progress) / Player.Research.NetResearch):0} turns" : "Stalled", DashboardCyan);
        }
        else Row("Select a technology", "No active research");

        Start(DashboardItem.Freight);
        Meter("Freighters trading", trading, freighters, DashboardCyan, $"{trading} / {freighters}   |   {(freighters == 0 ? 0 : 100f * trading / freighters):0}%");
        Meter("Cargo aboard", cargo, capacity, DashboardGold);
        Row("Idle / other orders", (freighters - trading).ToString());
        Row("Under construction", Player.FreightersBeingBuilt.ToString());
        float[] assigned = new float[3];
        int[] counts = new int[3], imports = new int[3], exports = new int[3];
        Goods[] goods = { Goods.Food, Goods.Production, Goods.Colonists };
        foreach (var ship in Player.OwnedShips)
        {
            if (!ship.Active || !ship.IsFreighter || ship.AI.State != AIState.SystemTrader) continue;
            for (int i = 0; i < goods.Length; ++i)
                if (ship.AI.HasTradeGoal(goods[i])) { assigned[i] += ship.CargoSpaceMax; ++counts[i]; }
        }
        foreach (var p in Player.GetPlanets())
        {
            if (p.FoodImportSlots > 0) ++imports[0]; if (p.FoodExportSlots > 0) ++exports[0];
            if (p.ProdImportSlots > 0) ++imports[1]; if (p.ProdExportSlots > 0) ++exports[1];
            if (p.ColonistsImportSlots > 0) ++imports[2]; if (p.ColonistsExportSlots > 0) ++exports[2];
        }
        float assignedTotal = assigned[0] + assigned[1] + assigned[2];
        for (int i = Player.NonCybernetic ? 0 : 1; i < goods.Length; ++i)
            rows.Add(new DashboardRow { Label = goods[i].ToString(), Value = $"{counts[i]} freighters",
                Detail = $"Capacity {assigned[i]:0.#}   |   {imports[i]} importing / {exports[i]} exporting colonies",
                Icon = i == 0 ? "NewUI/icon_food" : i == 1 ? "NewUI/icon_production" : "NewUI/icon_population",
                Meter = true, Amount = assigned[i], Maximum = assignedTotal,
                Color = i == 0 ? Color.LightGreen : i == 1 ? DashboardGold : DashboardCyan });

        Start(DashboardItem.Resources);
        Row("EMPIRE EFFECTS", "Applied bonuses", DashboardCyan);
        foreach (var bonus in DashboardResources)
        {
            ExoticBonusType type = bonus.Good.ExoticBonusType;
            float multiplier = type == ExoticBonusType.RepairRate || type == ExoticBonusType.ShieldRecharge
                ? Player.GetDynamicExoticBonusMuliplier(type) : Player.GetStaticExoticBonusMuliplier(type);
            float effect = (multiplier - 1) * 100;
            Row(DashboardBonusEffect(type), $"+{effect:0.#}%", effect > 0 ? DashboardCyan : Color.Gray);
        }
        Row("RESOURCE SUPPLY", "Per turn", DashboardGold);
        foreach (var bonus in DashboardResources)
            rows.Add(new DashboardRow { Label = Localizer.Token(bonus.Good.RefinedNameIndex),
                Value = $"Reserve {bonus.CurrentStorage:0.#} / {Player.MaxExoticStorage:0.#}",
                Detail = $"Refining {bonus.TotalRefinedPerTurn:0.#} / turn   |   Consumption {bonus.Consumption:0.#} / turn",
                Icon = "Goods/" + bonus.Good.UID, Color = bonus.CurrentStorage > 0 ? DashboardCyan : Color.Gray,
                Meter = true, Amount = bonus.CurrentStorage, Maximum = Player.MaxExoticStorage });
        if (DashboardResources.Length == 0) Row("No unique resources", "");
    }

    internal static float DashboardMeterFraction(float value, float max)
        => max <= 0 || float.IsNaN(value) || float.IsNaN(max) ? 0 : Math.Max(0, Math.Min(1, value / max));

    static void DrawDashboardMeter(SpriteBatch batch, RectF rect, float value, float max, Color color)
    {
        batch.FillRectangle(rect, new Color(3, 10, 15));
        batch.DrawRectangle(rect, new Color(47, 66, 70));
        float width = Math.Max(0, rect.W - 4) * DashboardMeterFraction(value, max);
        if (width <= 0) return;
        batch.FillRectangle(new RectF(rect.X + 2, rect.Y + 2, width, Math.Max(1, rect.H - 4)), color * .8f);
        batch.FillRectangle(new RectF(rect.X + 2, rect.Y + 2, width, 1), color);
    }

    public void DrawDashboardPopover(SpriteBatch batch)
    {
        if (!Universe.IsActive || !DashboardCardVisible) return;
        DrawDashboardCard(batch, (DashboardItem)DashboardHover, DashboardCardRect());
    }

    internal void DrawDashboardCard(SpriteBatch batch, DashboardItem item, RectF rect)
    {
        if (!DashboardReady) RefreshDashboard();
        batch.FillRectangle(new RectF(rect.X + 4, rect.Y + 4, rect.W, rect.H), new Color(0, 0, 0, 120).Premultiplied());
        batch.FillRectangle(rect, new Color(5, 15, 22));
        batch.DrawRectangle(rect, DashboardGold * .65f);
        batch.FillRectangle(new RectF(rect.X + 1, rect.Y + 1, rect.W - 2, 32), new Color(18, 34, 40));
        batch.Draw(ResourceManager.Texture(DashboardIcons[(int)item]), new RectF(rect.X + 12, rect.Y + 8, 18, 18), Color.White);
        string title = item == DashboardItem.Resources ? "UNIQUE RESOURCE BONUSES" : item == DashboardItem.Money ? "EMPIRE ECONOMY" : item.ToString().ToUpperInvariant();
        batch.DrawString(Fonts.Arial12Bold, title, new Vector2(rect.X + 40, rect.Y + 9), DashboardGold);
        float y = rect.Y + 42;
        foreach (var row in DashboardCards[(int)item])
        {
            float x = rect.X + 14;
            if (row.Icon != null)
            {
                batch.Draw(ResourceManager.Texture(row.Icon), new RectF(x, y, 20, 20), Color.White);
                x += 28;
            }
            float valueWidth = Fonts.Arial11Bold.TextWidth(row.Value);
            string label = row.Label;
            float available = rect.Right - 14 - valueWidth - 12 - x;
            while (label.Length > 0 && Fonts.Arial11Bold.TextWidth(label) > available)
                label = label.Substring(0, label.Length - 1);
            batch.DrawString(Fonts.Arial11Bold, label, new Vector2(x, y), DashboardGold);
            batch.DrawString(Fonts.Arial11Bold, row.Value, new Vector2(rect.Right - 14 - valueWidth, y), row.Color);
            if (row.Detail != null)
                batch.DrawString(Fonts.Arial11Bold, row.Detail, new Vector2(x, y + 19), new Color(153, 171, 178));
            if (row.Meter)
                DrawDashboardMeter(batch, new RectF(x, y + (row.Detail != null ? 38 : 22), rect.Right - 14 - x, 7), row.Amount, row.Maximum, row.Color);
            y += row.Height;
        }
    }
}

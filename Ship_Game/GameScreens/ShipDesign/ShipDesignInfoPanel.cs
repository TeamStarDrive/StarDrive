using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens.ShipDesign
{
    using GT = GameText;

    /// <summary>
    /// Displays STATS information of the currently active design
    /// </summary>
    public class ShipDesignInfoPanel : UIElementContainer
    {
        readonly ShipDesignScreen Screen;
        DesignShip S;
        ShipDesignStats Ds;

        UIList StatsList;
        float ItemHeight = 14;
        float ValueWidth = 80;
        float TitleWidth;
        Graphics.Font StatsFont = Fonts.Arial12Bold;

        Array<(UIElementV2, Func<bool>)> DynamicVisibility = new Array<(UIElementV2, Func<bool>)>();

        public ShipDesignInfoPanel(ShipDesignScreen screen, in Rectangle rect) : base(rect)
        {
            Screen = screen;
        }

        public void SetActiveDesign(DesignShip ship)
        {
            S = ship;
            Ds = ship.DesignStats;
            CreateElements();
        }

        void CreateElements()
        {
            Elements.Clear();
            DynamicVisibility.Clear();

            StatsList = Add(new UIList(Pos, Size));
            StatsList.SetRelPos(0, 0);
            TitleWidth = Width - ValueWidth;

            Color ok = Color.LightGreen;
            Color power = Color.LightSkyBlue;

            Val(() => S.GetCost(), GT.ProductionCost, GT.TT_ProductionCost, Tint.GoodBad);
            Val(() => S.GetMaintCost(), GT.UpkeepCost, GT.TT_UpkeepCost, Tint.GoodBad);
            Val(() => S.SurfaceArea, GT.TotalModuleSlots, GT.TT_TotalModuleSlots, Tint.GoodBad);
            Val(() => S.Mass, GT.Mass, GT.TT_Mass, Tint.GoodBad);
            Line();

            Val(() => Ds.PowerCapacity, GT.PowerCapacity, GT.TT_PowerCapacity, Tint.No, power, 
                                        col: Col(Tint.CompareValue, () => Ds.PowerConsumed));
            Val(() => Ds.PowerRecharge, GT.PowerRecharge, GT.TT_PowerRecharge, Tint.GoodBad, power);
            Val(() => Ds.DrawAtWarp, GT.RechargeAtWarp, GT.TT_RechargeAtWarp, Tint.GoodBad, power, vis: Ds.IsWarpCapable);

            Val(() => -Ds.PowerConsumed, GT.ExcessWpnPwrDrain, GT.TT_ExcessWpnPwrDrain, titleColor:power, vis: Ds.HasEnergyWepsPositive);
            Val(() => Ds.EnergyDuration, GT.WpnFirePowerTime, GT.TT_WpnFirePowerTime, Tint.BadLowerThan2, power, vis: Ds.HasEnergyWepsPositive);
            Val("INF", GT.WpnFirePowerTime, GT.TT_WpnFirePowerTime, Tint.No, power, ok, vis: Ds.HasEnergyWepsNegative);

            Val(() => -Ds.PowerConsumedWithBeams, GT.BurstWpnPwrDrain, GT.TT_BurstWpnPwerDrain, Tint.No, power, vis: Ds.HasBeams);
            Val(() => Ds.BurstEnergyDuration, GT.BurstWpnPwrTime, GT.TT_BurstWpnPwrTime, Tint.Bad, power, vis: Ds.HasBeamDurationNegative);
            Val("INF", GT.BurstWpnPwrTime, GT.TT_BurstWpnPwrTime, Tint.No, power, ok, vis: Ds.HasBeamDurationPositive);
            Line();
        }

        UI.UIKeyValueLabel Val(Func<float> dynamicValue, LocalizedText title, LocalizedText tooltip, 
                                 Tint tint = Tint.No, Color? titleColor = null,  Color? valueColor = null,
                                 Func<float, Color> col = null, Func<bool> vis = null,
                                 LocalizedText? valueText = null)
        {
            var lbl = new UI.UIKeyValueLabel(title, valueText ?? "11.11k", titleColor, valueColor)
            {
                Separator = ":     ",
                Width = Width,
                Split = TitleWidth,
                DynamicValue = dynamicValue,
                Tooltip = tooltip,
                Color = col ?? (tint != Tint.No ? Col(tint) : null),
                Height = ItemHeight,
            };

            lbl.Key.TextAlign = TextAlign.Right;
            lbl.Key.Width = TitleWidth;
            lbl.Key.Font = lbl.Value.Font = StatsFont;
            if (vis != null)
                DynamicVisibility.Add((lbl, vis));
            //lbl.DebugDraw = true;
            StatsList.Add(lbl);
            return lbl;
        }

        UI.UIKeyValueLabel Val(LocalizedText valueText, LocalizedText title, LocalizedText tooltip, 
                               Tint tint = Tint.No, Color? titleColor = null,  Color? valueColor = null,
                               Func<float, Color> col = null, Func<bool> vis = null)
        {
            return Val(null, title, tooltip, tint, titleColor, valueColor, col, vis, valueText);
        }

        void Line()
        {
            StatsList.Add(new UI.UISpacer(Width, ItemHeight));
        }

        enum Tint
        {
            No,
            Bad,
            GoodBad,
            BadLowerThan2,
            BadPLessThan1,
            CompareValue
        }

        Func<float, Color> Col(Tint tint, Func<float> compareValue = null)
        {
            return (float value) =>
            {
                switch (tint)
                {
                    case Tint.GoodBad:       return value > 0f ? Color.LightGreen : Color.LightPink;
                    case Tint.Bad:           return Color.LightPink;
                    case Tint.BadLowerThan2: return value > 2f ? Color.LightGreen : Color.LightPink;
                    case Tint.BadPLessThan1: return value > 1f ? Color.LightGreen : Color.LightPink;
                    case Tint.CompareValue:  return compareValue() < value ? Color.LightGreen : Color.LightPink;
                    case Tint.No:
                    default: return Color.White;
                }
            };
        }

        public override void Update(float fixedDeltaTime)
        {
            // Toggle which items are visible
            foreach ((UIElementV2 item, Func<bool> visibility) in DynamicVisibility)
            {
                bool visible = visibility();
                if (item.Visible != visible)
                {
                    item.Visible = visible;
                    StatsList.RequiresLayout = true;
                }
            }
            
            base.Update(fixedDeltaTime);
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    using GT = GameText;

    /// <summary>
    /// Displays STATS information of the currently active design
    /// </summary>
    public class ShipDesignInfoPanel : UIElementContainer
    {
        Ship S;
        ShipDesignStats Ds;
        bool UpdateDesignStats = false;

        UIList StatsList;
        float ItemHeight = 11;
        float ValueWidth = 80;
        float TitleWidth;
        Graphics.Font StatsFont = Fonts.Arial11Bold;

        Array<(UIElementV2, Func<bool>)> DynamicVisibility = new Array<(UIElementV2, Func<bool>)>();

        public ShipDesignInfoPanel(in Rectangle rect) : base(rect)
        {
        }

        public void SetActiveDesign(Ship ship, ShipDesignStats ds = null)
        {
            S = ship;
            Ds = ds ?? new ShipDesignStats(ship);
            UpdateDesignStats = ds == null;

            CreateElements();
        }

        void CreateElements()
        {
            Elements.Clear();
            DynamicVisibility.Clear();

            StatsList = Add(new UIList(Pos, Size));
            StatsList.Padding = Vector2.Zero;
            StatsList.SetRelPos(0, 0);
            TitleWidth = Width - ValueWidth;

            Color good = Color.LightGreen;
            Color energy = Color.LightSkyBlue;
            Color protect = Color.Goldenrod;
            Color engines = Color.DarkSeaGreen;
            Color ordnance = Color.IndianRed;

            Val(() => S.GetCost(), GT.ProductionCost, GT.TT_ProductionCost, Tint.Pos);
            Val(() => S.GetMaintCost(), GT.UpkeepCost, GT.TT_UpkeepCost, Tint.Pos);
            Val(() => S.SurfaceArea, GT.TotalModuleSlots, GT.TT_TotalModuleSlots, Tint.Pos);
            Val(() => S.Mass, GT.Mass, GT.TT_Mass, Tint.Pos);
            Line();

            Val(() => Ds.PowerCapacity, GT.PowerCapacity, GT.TT_PowerCapacity, Tint.No, energy, col: ColGreater(() => Ds.PowerConsumed));
            Val(() => Ds.PowerRecharge, GT.PowerRecharge, GT.TT_PowerRecharge, Tint.Pos, energy);
            Val(() => Ds.ChargeAtWarp, GT.RechargeAtWarp, GT.TT_RechargeAtWarp, Tint.Pos, energy, vis: Ds.IsWarpCapable);

            Val(() => -Ds.PowerConsumed, GT.ExcessWpnPwrDrain, GT.TT_ExcessWpnPwrDrain, Tint.No, energy, vis: Ds.HasEnergyWepsPositive);
            Val(() => Ds.EnergyDuration, GT.WpnFirePowerTime, GT.TT_WpnFirePowerTime, Tint.Two, energy, vis: Ds.HasEnergyWepsPositive);
            Val("INF", GT.WpnFirePowerTime, GT.TT_WpnFirePowerTime, Tint.No, energy, good, vis: Ds.HasEnergyWepsNegative);

            Val(() => -Ds.PowerConsumedWithBeams, GT.BurstWpnPwrDrain, GT.TT_BurstWpnPwerDrain, Tint.No, energy, vis: Ds.HasBeams);
            Val(() => Ds.BurstEnergyDuration, GT.BurstWpnPwrTime, GT.TT_BurstWpnPwrTime, Tint.Bad, energy, vis: Ds.HasBeamDurationNegative);
            Val("INF", GT.BurstWpnPwrTime, GT.TT_BurstWpnPwrTime, Tint.No, energy, good, vis: Ds.HasBeamDurationPositive);
            
            Val(() => Ds.WarpTime, GT.FtlTime, GT.TT_FtlTime, Tint.Pos, energy, vis: Ds.HasFiniteWarp);
            Val("INF", GT.FtlTime, GT.TT_FtlTime, Tint.No, energy, good, vis: Ds.HasInfiniteWarp);
            Line();

            Val(() => S.Health, GT.TotalHitpoints, GT.TT_HitPoints, Tint.Pos, protect);
            ValNZ(() => S.RepairRate, GT.RepairRate, GT.TT_RepairRate, Tint.Pos, protect);

            Val(() => S.shield_max, GT.ShieldPower, GT.TT_ShieldPower, Tint.Pos, protect, vis: Ds.HasRegularShields);
            Val(() => S.shield_max, GT.ShieldPower, GT.TT_ShieldPower, Tint.Pos, Color.Gold, vis: Ds.HasAmplifiedMains);
            ValNZ(() => (int)S.Stats.ShieldAmplifyPerShield, GT.ShieldAmplify, GT.TT_ShieldAmplify, Tint.Pos, protect);
            ValNZ(() => S.BonusEMP_Protection, GT.EmpProtection, GT.TT_EmpProtection, Tint.Pos, protect);
            ValNZ(() => S.ECMValue, GT.Ecm3, GT.TT_Ecm3, Tint.Pos, protect);
            Line();

            Val(() => S.MaxFTLSpeed, GT.FtlSpeed, GT.TT_FtlSpeed, Tint.No, engines, vis: Ds.IsWarpCapable, col: ColGreater(20_000));
            Val(() => S.MaxSTLSpeed, GT.SublightSpeed, GT.TT_SublightSpeed, Tint.No, engines, col: ColGreater(50));
            Val(() => S.RotationRadiansPerSecond.ToDegrees(), GT.TurnRate, GT.TT_TurnRate, Tint.No, engines, col: ColGreater(15));
            Line();

            ValNZ(() => S.OrdAddedPerSecond, GT.OrdnanceCreated, GT.TT_OrdnanceCreated, Tint.No, ordnance);
            Val(() => S.OrdinanceMax, GT.OrdnanceCapacity, GT.TT_OrdnanceCap, Tint.No, ordnance, vis: Ds.HasOrdnance);
            Val(() => Ds.AmmoTime, GT.AmmoTime, GT.TT_AmmoTime, Tint.No, ordnance, vis: Ds.HasOrdFinite, col: ColGreater(30));
            Val("INF", GT.AmmoTime, GT.TT_AmmoTime, Tint.No, ordnance, good, vis: Ds.HasOrdInfinite);
            ValNZ(() => S.TroopCapacity, GT.TroopCapacity, GT.TT_TroopCapacity, Tint.No, ordnance);
            Line();

            ValNZ(() => S.CargoSpaceMax, GT.CargoSpace, GT.TT_CargoSpace);
            ValNZ(() => S.TargetingAccuracy, GT.FireControl, GT.TT_FireControl);
            ValNZ(() => S.TrackingPower, GT.FcsPower, GT.TT_FcsPower);
            ValNZ(() => S.SensorRange, GT.SensorRange3, GT.TT_SensorRange3);

            ValNZ(() => Ds.Strength, GT.ShipOffense, GT.TT_ShipOffense);
            ValNZ(() => Ds.RelativeStrength, GT.RelativeStrength, GT.TT_RelativeStrength);
        }

        UI.UIKeyValueLabel Val(Func<float> dynamicValue, LocalizedText title, LocalizedText tooltip, 
                                 Tint tint = Tint.No, Color? titleColor = null,  Color? valueColor = null,
                                 Func<float, Color> col = null, Func<bool> vis = null, LocalizedText? valueText = null)
        {
            var lbl = new UI.UIKeyValueLabel(title, valueText ?? "11.11k", titleColor, valueColor)
            {
                Separator = ":     ",
                Width = Width,
                Split = TitleWidth,
                DynamicValue = dynamicValue,
                Tooltip = tooltip,
                Color = col ?? (tint != Tint.No ? Tinted(tint) : null),
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

        // Displays the dynamicValue if it's Greater than 0
        UI.UIKeyValueLabel ValNZ(Func<float> dynamicValue, LocalizedText title, LocalizedText tooltip, 
                                      Tint tint = Tint.No, Color? titleColor = null,  Color? valueColor = null,
                                      Func<float, Color> col = null, LocalizedText? valueText = null)
        {
            Func<bool> vis = () => dynamicValue() > 0;
            return Val(dynamicValue, title, tooltip, tint, titleColor, valueColor, col, vis, valueText);
        }

        void Line()
        {
            StatsList.Add(new UI.UISpacer(Width, ItemHeight - 3));
        }

        enum Tint
        {
            No, // no tint
            Bad, // this value is bad
            Pos, // value must be positive
            One, // must be greater than 1
            Two, // must be greater than 2
        }

        Func<float, Color> Tinted(Tint tint)
        {
            return (v) =>
            {
                switch (tint)
                {
                    default: case Tint.No:   return Color.White;
                    case Tint.Bad: return Color.LightPink;
                    case Tint.Pos: return v > 0f ? Color.LightGreen : Color.LightPink;
                    case Tint.One: return v > 1f ? Color.LightGreen : Color.LightPink;
                    case Tint.Two: return v > 2f ? Color.LightGreen : Color.LightPink;
                }
            };
        }

        // value must be greater than compareValue()
        Func<float, Color> ColGreater(Func<float> compareValue)
        {
            return (v) => v > compareValue() ? Color.LightGreen : Color.LightPink;
        }

        Func<float, Color> ColGreater(float compareValue)
        {
            return (v) => v > compareValue ? Color.LightGreen : Color.LightPink;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (UpdateDesignStats)
                Ds.Update();

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

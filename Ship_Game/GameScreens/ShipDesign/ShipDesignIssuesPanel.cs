﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.ShipDesign
{
    /// <summary>
    /// Panel which contains the Ship Completion %, Issues and Information
    /// </summary>
    public class ShipDesignIssuesPanel : UIElementContainer
    {
        readonly ShipDesignScreen Screen;
        DesignShip S;
        ShipDesignIssues Issues;
        
        UI.UIKeyValueLabel DesignCompletion;
        UIButton BtnDesignIssues;
        UIButton BtnInformation;

        public ShipDesignIssuesPanel(ShipDesignScreen screen, in Rectangle rect) : base(rect)
        {
            Screen = screen;

            DesignCompletion = Add(new UI.UIKeyValueLabel(GameText.NoEmptySlots, "100%", valueColor:Color.LightGreen));
            DesignCompletion.Tooltip = GameText.InOrderToCompleteYour;

            BtnDesignIssues = AddTextButton("Design Issues");
            BtnInformation = AddTextButton("Information");
            BtnInformation.Visible = BtnDesignIssues.Visible = false;

            BtnDesignIssues.Tooltip = GameText.StatesAnyDesignIssuesThe;
            BtnDesignIssues.OnClick = b => AddDesignIssuesScreen();
            BtnInformation.OnClick = b => AddDesignIssuesScreen();
            //DebugDraw = true;

            DesignCompletion.SetLocalPos(20, 0);
            BtnDesignIssues.SetLocalPos(16, 18);
            BtnInformation.SetLocalPos(16, 18);
        }

        public int CompletionPercent
        {
            get
            {
                if (S == null)
                    return 0;
                int slots = S.Modules.Sum(m => m.Area);
                return (int)((slots == S.SurfaceArea ? 1f : slots / (float)S.SurfaceArea) * 100);
            }
        }
        
        public void SetActiveDesign(DesignShip ship)
        {
            S = ship;
            if (ship == null)
                return;
            Issues = new ShipDesignIssues(ship.ShipData);
        }

        UIButton AddTextButton(string text)
        {
            string firstLetter = text[0].ToString().ToUpper();
            string remaining = text.Remove(0, 1);

            var btn = Add(new UIButton(ButtonStyle.Text, LocalizedText.None));
            btn.Pos = new Vector2(Rect.X, Rect.Y);
            btn.RichText.AddText(firstLetter, Fonts.Pirulen20);
            btn.RichText.AddText(remaining, Fonts.Pirulen16);
            btn.DefaultTextColor = btn.PressTextColor = Color.Green;
            btn.HoverTextColor = Color.White;
            btn.TextShadows = true;
            return btn;
        }

        void AddDesignIssuesScreen()
        {
            var issues = new ShipDesignIssuesScreen(Screen, Issues.CurrentDesignIssues);
            Screen.ScreenManager.AddScreen(issues);
        }

        public override void Update(float fixedDeltaTime)
        {
            BtnInformation.Visible = BtnDesignIssues.Visible = false;

            if (S == null)
                return;

            Issues.Reset();
            int percent = CompletionPercent;
            DesignCompletion.ValueText = percent + "%";
            DesignCompletion.ValueColor = GetCompletionColor(percent);

            if (percent >= 75)
            {
                var ds = S.DesignStats;
                Issues.CheckIssueNoCommand(ds.NumCmdModules);
                Issues.CheckIssueBackupCommand(ds.NumCmdModules, S.SurfaceArea);
                Issues.CheckIssueStationaryHoldPositionHangars(S.Carrier.AllFighterHangars.Length, S.ShipData.DefaultCombatState);
                Issues.CheckIssueUnpoweredModules(ds.UnpoweredModules);
                Issues.CheckIssueOrdnance(ds.AvgOrdnanceUsed, S.OrdAddedPerSecond, ds.AmmoTime);
                Issues.CheckIssuePowerRecharge(ds.HasEnergyWeapons, ds.PowerRecharge, S.PowerStoreMax, ds.PowerConsumed, out bool hasRechargeIssue);
                Issues.CheckPowerRequiredToFireOnce(S, out bool hasFirePowerIssues);
                Issues.CheckPowerCapacityWithSalvo(S, ds.PowerRecharge, hasFirePowerIssues, out bool hasSalvoFirePowerIssues);
                Issues.CheckIssueOrdnanceBurst(ds.BurstOrdnance, S.OrdinanceMax);
                Issues.CheckIssueLowWarpTime(ds.ChargeAtWarp, ds.WarpTime, S.MaxFTLSpeed);
                Issues.CheckIssueNoWarp(S.MaxSTLSpeed, S.MaxFTLSpeed);
                Issues.CheckIssueSlowWarp(S.MaxFTLSpeed);
                Issues.CheckIssueNoSpeed(S.MaxSTLSpeed);
                Issues.CheckTargetExclusions(ds.NumWeaponSlots > 0, ds.CanTargetFighters, ds.CanTargetCorvettes, ds.CanTargetCapitals);
                Issues.CheckTruePD(S.SurfaceArea, ds.PointDefenseValue);
                Issues.CheckWeaponPowerTime(ds.HasEnergyWeapons, ds.PowerConsumed > 0, ds.EnergyDuration);
                Issues.CheckCombatEfficiency(ds.PowerConsumed, ds.EnergyDuration, ds.PowerRecharge, ds.NumWeapons, ds.NumOrdWeapons);
                Issues.CheckBurstPowerTime(ds.BeamPeakPowerNeeded > 0, ds.BurstEnergyDuration);
                Issues.CheckOrdnanceVsEnergyWeapons(ds.NumWeapons, ds.NumOrdWeapons, ds.AvgOrdnanceUsed, S.OrdAddedPerSecond);
                Issues.CheckTroopsVsBays(S.TroopCapacity, ds.NumTroopBays);
                Issues.CheckTroops(S.TroopCapacity, S.SurfaceArea);
                Issues.CheckAccuracy(ds.WeaponAccuracies);
                Issues.CheckTargets(ds.PoweredWeapons, S.TrackingPower);
                Issues.CheckSecondaryCarrier(ds.TotalHangarArea > 0, Screen.Role, (int)S.WeaponsMaxRange);
                Issues.CheckDedicatedCarrier(ds.TotalHangarArea > 0, Screen.Role, (int)S.WeaponsMaxRange, S.SensorRange,
                    S.ShipData.DefaultCombatState == CombatState.ShortRange || S.ShipData.DefaultCombatState == CombatState.AttackRuns);

                if (!hasFirePowerIssues && !hasSalvoFirePowerIssues && !hasRechargeIssue)
                    Issues.CheckExcessPowerCells(ds.BeamPeakPowerNeeded > 0, ds.BurstEnergyDuration, ds.PowerConsumed, ds.HasPowerCells,
                        ds.PowerRecharge, S.PowerStoreMax, S.Weapons.Any(w => w.PowerRequiredToFire > 0));
            }
            
            switch (Issues.CurrentWarningLevel)
            {
                case WarningLevel.None: break;
                case WarningLevel.Informative: BtnInformation.Visible = true; break;
                default: BtnDesignIssues.Visible = true; break;
            }

            BtnDesignIssues.DefaultTextColor = BtnDesignIssues.PressTextColor = Issues.CurrentWarningColor;
            BtnInformation.DefaultTextColor = BtnInformation.PressTextColor = Issues.CurrentWarningColor;

            base.Update(fixedDeltaTime);
        }

        static Color GetCompletionColor(int percent)
        {
            Color color;
            if    (percent == 100) color = Color.LightGreen;
            else if (percent < 33) color = Color.Red;
            else if (percent < 66) color = Color.Orange;
            else                   color = Color.Yellow;
            return color;
        }
    }
}

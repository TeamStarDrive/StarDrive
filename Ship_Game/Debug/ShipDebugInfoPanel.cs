using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using Ship_Game.UI;

namespace Ship_Game.Debug;

public class ShipDebugInfoPanel : Submenu
{
    new readonly DebugInfoScreen Parent;
    UniverseScreen Screen => Parent.Screen;
    readonly TextDrawerComponent Text;

    public ShipDebugInfoPanel(DebugInfoScreen parent, LocalPos pos, Vector2 size)
        : base(pos, size)
    {
        Parent = parent;
        Text = new(parent);
        Color = new Color(0, 0, 0, 50);
    }

    public bool HasSelectedSomething()
    {
        return Screen.SelectedFleet is { }
            || Screen.CurrentGroup is { }
            || Screen.SelectedShip is { }
            || Screen.SelectedShipList.NotEmpty;
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        base.Draw(batch, elapsed);

        Text.SetCursor(Pos.X + 10, Pos.Y + 30, Color.White);

        if (Screen.SelectedFleet is { } fleet)
        {
            Text.String($"Fleet: {fleet.Name}  IsCoreFleet:{fleet.IsCoreFleet}");
            Text.String($"Ships:{fleet.Ships.Count} STR:{fleet.GetStrength()} Vmax:{fleet.SpeedLimit}");
            Text.String($"Distance: {fleet.AveragePosition().Distance(fleet.FinalPosition)}");
            Text.String($"FormationMove:{fleet.InFormationMove}  ReadyForWarp:{fleet.ReadyForWarp}");

            if (fleet.FleetTask != null)
            {
                Text.String(fleet.FleetTask.Type.ToString());
                if (fleet.FleetTask.TargetPlanet != null)
                    Text.String(fleet.FleetTask.TargetPlanet.Name);
                Text.String($"Step: {fleet.TaskStep}");
            }

            if (fleet.Ships.NotEmpty)
            {
                Text.String("");
                Text.String("-- First Ship AIState:");
                DrawShipOrderQueueInfo(fleet.Ships.First);
                DrawWayPointsInfo(fleet.Ships.First);
            }
        }
        // only show CurrentGroup if we selected more than one ship
        else if (Screen.CurrentGroup is { } sg && Screen.SelectedShipList.Count > 1)
        {
            Text.String($"ShipGroup ({sg.CountShips})  x {(int)sg.FinalPosition.X} y {(int)sg.FinalPosition.Y}");
            Text.String("");
            Text.String("-- First Ship AIState:");
            DrawShipOrderQueueInfo(Screen.SelectedShipList.First);
        }
        else if (Screen.SelectedShip is { } s)
        {
            Text.String($"Ship {s.ShipName}  x {s.Position.X:0} y {s.Position.Y:0}");
            Text.String($"ID: {s.Id}");
            Text.String($"VEL: {s.Velocity.Length():0}  "
                       +$"LIMIT: {s.SpeedLimit:0}  "
                       +$"Vmax: {s.VelocityMax:0}  ");

            Text.String($"FTLMax: {s.MaxFTLSpeed:0}  "
                       +$"{s.WarpState}  {s.ThrustThisFrame}  {s.DebugThrustStatus}");

            Text.String($"E:{s.ShipEngines.EngineStatus} {s.ShipEngines.ReadyForWarp} FLEET:{s.ShipEngines.ReadyForFormationWarp}");

            DrawShipOrderQueueInfo(s);
            DrawWayPointsInfo(s);

            Text.String($"On Defense: {s.Loyalty.AI.DefensiveCoordinator.Contains(s)}");
            if (s.Fleet != null)
            {
                Text.String($"Fleet: {s.Fleet.Name}  {(int)s.Fleet.FinalPosition.X}x{(int)s.Fleet.FinalPosition.Y}  Vmax:{s.Fleet.SpeedLimit}");
            }

            Text.String(s.Pool != null ? "In Force Pool" : "NOT In Force Pool");

            if (s.AI.State == AIState.SystemDefender)
            {
                SolarSystem systemToDefend = s.AI.SystemToDefend;
                Text.String($"Defending {systemToDefend?.Name ?? "Awaiting Order"}");
            }

            Text.String(s.System == null ? "Deep Space" : $"System: {s.System.Name}");
            var influence = s.GetProjectorInfluenceEmpires().Select(e => e.Name);
            Text.String("Influence: " + (s.IsInFriendlyProjectorRange ? "Friendly"
                                      :  s.IsInHostileProjectorRange  ? "Hostile" : "Neutral")
                                      + " | " + string.Join(",", influence));

            string gravityWell = s.Universe.GravityWells ? s.System?.IdentifyGravityWell(s)?.Name : "disabled";
            Text.String($"GravityWell: {gravityWell}   Inhibited:{s.IsInhibitedByUnfriendlyGravityWell}");

            var combatColor = s.InCombat ? Color.Green : Color.LightPink;
            var inCombat = s.InCombat ? s.AI.BadGuysNear ? "InCombat" : "ERROR" : "NotInCombat";
            Text.String(combatColor, $"{inCombat} PriTarget:{s.AI.HasPriorityTarget} PriOrder:{s.AI.HasPriorityOrder}");
            if (s.AI.IgnoreCombat)
                Text.String(Color.Pink, "Ignoring Combat!");
            if (s.IsFreighter)
            {
                Text.String($"Trade Timer:{s.TradeTimer}");
                ShipAI.ShipGoal g = s.AI.OrderQueue.PeekLast;
                if (g?.Trade != null && g.Trade.BlockadeTimer < 120)
                    Text.String($"Blockade Timer:{g.Trade.BlockadeTimer}");
            }

            if (s.AI.Target is { } shipTarget)
            {
                Text.SetCursor(Pos.X + 200, Pos.Y + 620f, Color.White);
                Text.String("Target: "+ shipTarget.Name);
                Text.String(shipTarget.Active ? "Active" : "Error - Active");
            }

            float currentStr = s.GetStrength(), baseStr = s.BaseStrength;
            Text.String($"Strength: {currentStr.String(0)} / {baseStr.String(0)}  ({(currentStr/baseStr).PercentString()})");
            Text.String($"HP: {s.Health.String(0)} / {s.HealthMax.String(0)}  ({s.HealthPercent.PercentString()})");
            Text.String($"Mass: {s.Mass.String(0)}");
            Text.String($"EMP Damage: {s.EMPDamage} / {s.EmpTolerance} :Recovery: {s.EmpRecovery}");
            Text.String($"IntSlots: {s.ActiveInternalModuleSlots}/{s.NumInternalSlots}  ({s.InternalSlotsHealthPercent.PercentString()})");
            Text.String($"DPS: {s.TotalDps}");
            Text.String($"Sensor: {s.AI.GetSensorRadius().GetNumberString()} "+
                        $"BadGuys:{s.AI.BadGuysNear}[{s.AI.PotentialTargets.Length}] CanRepair:{s.CanRepair}");
            Text.SetCursor(Pos.X + 250, 600f, Color.White);
            foreach (SystemCommander sc in s.Loyalty.AI.DefensiveCoordinator.DefenseDict.Values)
            {
                if (sc.ContainsShip(s))
                    Text.String(sc.System.Name);
            }
        }
        else if (Screen.SelectedShipList.NotEmpty)
        {
            IReadOnlyList<Ship> ships = Screen.SelectedShipList;
            Text.String($"SelectedShips: {ships.Count} ");
            Text.String($"Total Str: {ships.Sum(ss => ss.BaseStrength).String(1)} ");
        }
    }

    void DrawWayPointsInfo(Ship ship)
    {
        if (ship.AI.HasWayPoints)
        {
            WayPoint[] wayPoints = ship.AI.CopyWayPoints();
            Text.String($"WayPoints ({wayPoints.Length}):");
            for (int i = 0; i < wayPoints.Length; ++i)
                Text.String($"  {i+1}:  {wayPoints[i].Position}");
        }
    }

    void DrawShipOrderQueueInfo(Ship ship)
    {
        if (ship.AI.OrderQueue.NotEmpty)
        {
            ShipAI.ShipGoal[] goals = ship.AI.OrderQueue.ToArray();
            Vector2 pos = ship.AI.GoalTarget;
            Text.String($"AI: {ship.AI.State} CS:{ship.AI.CombatState} TgtDst: {pos.Distance(ship.Position).GetNumberString()}");
            Text.String($"OrderQueue ({goals.Length}):");
            for (int i = 0; i < goals.Length; ++i)
            {
                ShipAI.ShipGoal g = goals[i];
                Text.String($"  {i+1}:  {g.Plan}  {g.MoveOrder}");
            }
        }
        else
        {
            Text.String($"AIState: {ship.AI.State}  CombatState: {ship.AI.CombatState}");
            Text.String("OrderQueue is EMPTY");
        }
    }
}

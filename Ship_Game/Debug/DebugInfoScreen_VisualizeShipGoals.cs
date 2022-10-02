using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;

namespace Ship_Game.Debug;

public sealed partial class DebugInfoScreen
{
    void VisualizeShipGoals()
    {
        if (Screen.SelectedFleet is { } f)
        {
            DrawArrowImm(f.FinalPosition, f.FinalPosition + f.FinalDirection * 200f, Color.OrangeRed);
            DrawCircleImm(f.FinalPosition, f.GetRelativeSize().Length(), Color.Red);
            DrawCircleImm(f.FinalPosition, ShipEngines.AtFinalFleetPos, Color.MediumVioletRed);

            foreach (Ship ship in f.Ships)
                VisualizeShipGoal(ship, false);

            if (f.FleetTask == null)
            {
                DrawCircleImm(f.AveragePosition(), 30, Color.Magenta);
                DrawCircleImm(f.AveragePosition(), 60, Color.DarkMagenta);
            }
        }
        // only show CurrentGroup if we selected more than one ship
        else if (Screen.CurrentGroup is { } g && Screen.SelectedShipList.Count > 1)
        {
            DrawArrowImm(g.FinalPosition, g.FinalPosition + g.FinalDirection * 200f, Color.OrangeRed);
            foreach (Ship ship in g.Ships)
                VisualizeShipGoal(ship, false);
        }
        else if (Screen.SelectedShip is { } s)
        {
            VisualizeShipGoal(s);
            if (Universe.IsSystemViewOrCloser && Mode == DebugModes.Normal && !Screen.ShowShipNames)
                DrawWeaponArcs(s);
            DrawSensorInfo(s);
        }
        else if (Screen.SelectedShipList.NotEmpty)
        {
            foreach (Ship ship in Screen.SelectedShipList)
                VisualizeShipGoal(ship, false);
        }
    }

    void DrawSensorInfo(Ship ship)
    {
        foreach (Projectile p in ship.AI.TrackProjectiles)
        {
            float r = Math.Max(p.Radius, 32f);
            DrawCircleImm(p.Position, r, Color.Yellow);
        }
        foreach (Ship s in ship.AI.FriendliesNearby)
        {
            DrawCircleImm(s.Position, s.Radius, Color.Green);
        }
        foreach (Ship s in ship.AI.PotentialTargets)
        {
            DrawCircleImm(s.Position, s.Radius, Color.Red);
        }
    }

    void DrawWeaponArcs(Ship ship)
    {
        foreach (Weapon w in ship.Weapons)
        {
            ShipModule m = w.Module;
            float facing = ship.Rotation + m.TurretAngleRads;
            float range = w.GetActualRange(ship.Loyalty);

            Vector2 moduleCenter = m.Position + m.WorldSize*0.5f;
            ShipDesignScreen.DrawWeaponArcs(ScreenManager.SpriteBatch, Screen, w, m, moduleCenter, 
                                            range * 0.25f, ship.Rotation, m.TurretAngle);

            DrawCircleImm(w.Origin, m.Radius/(float)Math.Sqrt(2), Color.Crimson);

            Ship targetShip = ship.AI.Target;
            GameObject target = targetShip;
            if (w.FireTarget is ShipModule sm)
            {
                targetShip = sm.GetParent();
                target = sm;
            }

            if (targetShip != null)
            {
                bool inRange = ship.CheckRangeToTarget(w, target);
                float bigArc = m.FieldOfFire*1.2f;
                bool inBigArc = RadMath.IsTargetInsideArc(m.Position, target.Position,
                                                ship.Rotation + m.TurretAngleRads, bigArc);
                if (inRange && inBigArc) // show arc lines if we are close to arc edges
                {
                    bool inArc = ship.IsInsideFiringArc(w, target.Position);

                    Color inArcColor = inArc ? Color.LawnGreen : Color.Orange;
                    DrawLineImm(m.Position, target.Position, inArcColor, 3f);

                    DrawLineImm(m.Position, m.Position + facing.RadiansToDirection() * range, Color.Crimson);
                    Vector2 left  = (facing - m.FieldOfFire * 0.5f).RadiansToDirection();
                    Vector2 right = (facing + m.FieldOfFire * 0.5f).RadiansToDirection();
                    DrawLineImm(m.Position, m.Position + left * range, Color.Crimson);
                    DrawLineImm(m.Position, m.Position + right * range, Color.Crimson);

                    string text = $"Target: {targetShip.Name}\nInArc: {inArc}";
                    DrawShadowStringProjected(m.Position, 0f, 1f, inArcColor, text);
                }
            }
        }
    }

    void VisualizeShipGoal(Ship ship, bool detailed = true)
    {
        if (ship == null)
            return;

        if (ship.AI.OrderQueue.NotEmpty)
        {
            ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekFirst;
            Vector2 pos = ship.AI.GoalTarget;

            DrawLineImm(ship.Position, pos, Color.YellowGreen);
            //if (detailed) DrawCircleImm(pos, 1000f, Color.Yellow);
            //DrawCircleImm(pos, 75f, Color.Maroon);

            Vector2 thrustTgt = ship.AI.ThrustTarget;
            if (detailed && thrustTgt.NotZero())
            {
                DrawLineImm(pos, thrustTgt, Color.Orange);
                DrawLineImm(ship.Position, thrustTgt, Color.Orange);
                DrawCircleImm(thrustTgt, 40f, Color.MediumVioletRed, 2f);
            }

            // goal direction arrow
            DrawArrowImm(pos, pos + goal.Direction * 50f, Color.Wheat);

            // velocity arrow
            if (detailed)
                DrawArrowImm(ship.Position, ship.Position+ship.Velocity, Color.OrangeRed);

            // ship direction arrow
            DrawArrowImm(ship.Position, ship.Position+ship.Direction*200f, Color.GhostWhite);
        }
        if (ship.AI.HasWayPoints)
        {
            WayPoint[] wayPoints = ship.AI.CopyWayPoints();
            if (wayPoints.Length > 0)
            {
                DrawLineImm(ship.Position, wayPoints[0].Position, Color.ForestGreen);
                for (int i = 1; i < wayPoints.Length; ++i) // draw WayPoints chain
                    DrawLineImm(wayPoints[i-1].Position, wayPoints[i].Position, Color.ForestGreen);
            }
        }
        if (ship.Fleet != null)
        {
            Vector2 formationPos = ship.Fleet.GetFormationPos(ship);
            Vector2 finalPos = ship.Fleet.GetFinalPos(ship);
            Color color = Color.Magenta.Alpha(0.5f);
            DrawCircleImm(finalPos, ship.Radius*0.5f, Color.Blue.Alpha(0.5f), 0.8f);
            DrawCircleImm(formationPos, ship.Radius-10, color, 0.8f);
            DrawLineImm(ship.Position, formationPos, color, 0.8f);
        }
    }
}
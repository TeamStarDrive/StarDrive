using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Debug.Page;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.Sandbox;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;

namespace Ship_Game.Debug
{
    public enum DebugModes
    {
        Normal,
        Empire,
        Targeting,
        PathFinder,
        DefenseCo,
        Trade,
        Planets,
        AO,
        ThreatMatrix,
        SpatialManager,
        Input,
        Tech,
        Solar, // Sun timers, black hole data, pulsar radiation radius...
        War,
        Pirates,
        Remnants,
        Agents,
        Relationship,
        FleetMulti,
        StoryAndEvents,
        Tasks,
        Particles,
        Last // dummy value
    }


    public sealed partial class DebugInfoScreen : GameScreen
    {
        public bool IsOpen = true;
        public readonly UniverseScreen Screen;
        public readonly UniverseState Universe;
        public readonly Rectangle Win = new(30, 100, 1200, 700);

        public DebugModes Mode => Screen.DebugMode;
        readonly Array<DebugPrimitive> Primitives = new();
        DebugPage Page;

        public DebugInfoScreen(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            Universe = screen.UState;
        }

        readonly Dictionary<string, Array<string>> ResearchText = new();

        public void ResearchLog(string text, Empire empire)
        {
            if (!DebugLogText(text, DebugModes.Tech))
                return;
            if (GetResearchLog(empire, out Array<string> empireTechs))
            {
                empireTechs.Add(text);
            }
            else
            {
                ResearchText.Add(empire.Name, new(){ text });
            }
        }

        public void ClearResearchLog(Empire empire)
        {
            if (GetResearchLog(empire, out Array<string> empireTechs))
                empireTechs.Clear();
        }

        public bool GetResearchLog(Empire e, out Array<string> empireTechs)
        {
            return ResearchText.TryGetValue(e.Name, out empireTechs);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.Left) || input.KeyPressed(Keys.Right))
            {
                ResearchText.Clear();

                DebugModes mode = Mode;
                mode += input.KeyPressed(Keys.Left) ? -1 : +1;
                Screen.UState.SetDebugMode(Mode switch
                {
                    >= DebugModes.Last => DebugModes.Normal,
                    < DebugModes.Normal => DebugModes.Last - 1,
                    _ => mode
                });
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            if (Page != null && Page.Mode != Mode) // destroy page if it's no longer needed
            {
                Page.RemoveFromParent();
                Page = null;
            }

            if (Page == null) // create page if needed
            {
                Page = CreatePage(Mode);
                if (Page != null)
                    Add(Page);
            }

            UpdateDebugShips();
            base.Update(fixedDeltaTime);
        }

        DebugPage CreatePage(DebugModes mode)
        {
            return mode switch
            {
                DebugModes.Targeting => new TargetingDebug(this),
                DebugModes.Tech => new TechDebug(this),
                DebugModes.Input => new InputDebug(this),
                DebugModes.Empire => new EmpireInfoDebug(this),
                DebugModes.Pirates => new PiratesDebug(this),
                DebugModes.Remnants => new RemnantsDebug(this),
                DebugModes.Agents => new AgentsDebug(this),
                DebugModes.PathFinder => new PathFinderDebug(this),
                DebugModes.Relationship => new RelationshipDebug(this),
                DebugModes.FleetMulti => new FleetMultipliersDebug(this),
                DebugModes.Trade => new TradeDebug(this),
                DebugModes.Planets => new PlanetDebug(this),
                DebugModes.Solar => new SolarDebug(this),
                DebugModes.War => new WarDebug(this),
                DebugModes.AO => new AODebug(this),
                DebugModes.SpatialManager => new SpatialDebug(this),
                DebugModes.StoryAndEvents => new StoryAndEventsDebug(this),
                DebugModes.Particles => new ParticleDebug(this),
                DebugModes.ThreatMatrix => new ThreatMatrixDebug(this),
                DebugModes.DefenseCo => new DefenseCoordinatorDebug(this),
                DebugModes.Tasks => new TasksDebug(this),
                _ => null
            };
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            try
            {
                TextFont = Fonts.Arial20Bold;
                SetTextCursor(50f, 50f, Color.Red);

                DrawString(Color.Yellow, Mode.ToString());

                TextFont = Fonts.Arial12Bold;
                TextCursor.Y -= (float)(Fonts.Arial20Bold.LineSpacing + 2) * 4;
                TextCursor.X += Fonts.Arial20Bold.TextWidth("XXXXXXXXXXXXXXXXXXXX");

                DrawDebugPrimitives(elapsed.RealTime.Seconds);

                base.Draw(batch, elapsed);

                ShipInfo();
            }
            catch { }
        }
        
        void UpdateDebugShips()
        {
            //if (DebugPlatformSpeed == null) // platform is only enabled in sandbox universe
            //    return;
            //float platformSpeed = DebugPlatformSpeed.AbsoluteValue;
            //float speedLimiter = SpeedLimitSlider.RelativeValue;

            //if (Screen.SelectedShip != null)
            //{
            //    Ship ship = Screen.SelectedShip;
            //    ship.SetSpeedLimit(speedLimiter * ship.VelocityMaximum);
            //}

            //foreach (PredictionDebugPlatform platform in GetPredictionDebugPlatforms())
            //{
            //    platform.CanFire = CanDebugPlatformFire;
            //    if (platformSpeed.NotZero())
            //    {
            //        platform.Velocity.X = platformSpeed;
            //    }
            //}
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
        
        void DrawSensorInfo(Ship ship)
        {
            foreach (Projectile p in ship.AI.TrackProjectiles)
            {
                float r = Math.Max(p.Radius, 32f);
                DrawCircleImm(p.Position, r, Color.Yellow, 1f);
            }
            foreach (Ship s in ship.AI.FriendliesNearby)
            {
                DrawCircleImm(s.Position, s.Radius, Color.Green, 1f);
            }
            foreach (Ship s in ship.AI.PotentialTargets)
            {
                DrawCircleImm(s.Position, s.Radius, Color.Red, 1f);
            }
        }

        void ShipInfo()
        {
            float y = (ScreenHeight - 700f).Clamped(100, 450);
            SetTextCursor(Win.X + 10, y, Color.White);

            // never show ship info in particle debug
            if (Mode == DebugModes.Particles)
                return;

            if (Screen.SelectedFleet != null)
            {
                Fleet fleet = Screen.SelectedFleet;
                DrawArrowImm(fleet.FinalPosition, fleet.FinalPosition+fleet.FinalDirection*200f, Color.OrangeRed);
                DrawCircleImm(fleet.FinalPosition, fleet.GetRelativeSize().Length(), Color.Red);
                DrawCircleImm(fleet.FinalPosition, ShipEngines.AtFinalFleetPos, Color.MediumVioletRed);

                foreach (Ship ship in fleet.Ships)
                    VisualizeShipGoal(ship, false);

                DrawString($"Fleet: {fleet.Name}  IsCoreFleet:{fleet.IsCoreFleet}");
                DrawString($"Ships:{fleet.Ships.Count} STR:{fleet.GetStrength()} Vmax:{fleet.SpeedLimit}");
                DrawString($"Distance: {fleet.AveragePosition().Distance(fleet.FinalPosition)}");
                DrawString($"FormationMove:{fleet.InFormationMove}  ReadyForWarp:{fleet.ReadyForWarp}");

                if (fleet.FleetTask != null)
                {
                    DrawString(fleet.FleetTask.Type.ToString());
                    if (fleet.FleetTask.TargetPlanet != null)
                        DrawString(fleet.FleetTask.TargetPlanet.Name);
                    DrawString($"Step: {fleet.TaskStep}");
                }
                else
                {
                    DrawCircleImm(fleet.AveragePosition(), 30, Color.Magenta);
                    DrawCircleImm(fleet.AveragePosition(), 60, Color.DarkMagenta);
                }

                if (fleet.Ships.NotEmpty)
                {
                    DrawString("");
                    DrawString("-- First Ship AIState:");
                    DrawShipOrderQueueInfo(fleet.Ships.First);
                    DrawWayPointsInfo(fleet.Ships.First);
                }
            }
            // only show CurrentGroup if we selected more than one ship
            else if (Screen.CurrentGroup != null && Screen.SelectedShipList.Count > 1)
            {
                ShipGroup group = Screen.CurrentGroup;
                DrawArrowImm(group.FinalPosition, group.FinalPosition+group.FinalDirection*200f, Color.OrangeRed);
                foreach (Ship ship in group.Ships)
                    VisualizeShipGoal(ship, false);

                DrawString($"ShipGroup ({group.CountShips})  x {(int)group.FinalPosition.X} y {(int)group.FinalPosition.Y}");
                DrawString("");
                DrawString("-- First Ship AIState:");
                DrawShipOrderQueueInfo(Screen.SelectedShipList.First);
            }
            else if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;

                DrawString($"Ship {ship.ShipName}  x {ship.Position.X:0} y {ship.Position.Y:0}");
                DrawString($"VEL: {ship.Velocity.Length():0}  "
                          +$"LIMIT: {ship.SpeedLimit:0}  "
                          +$"Vmax: {ship.VelocityMax:0}  "
                          +$"FTLMax: {ship.MaxFTLSpeed:0}  "
                          +$"{ship.WarpState}  {ship.ThrustThisFrame}  {ship.DebugThrustStatus}");

                DrawString($"ENG:{ship.ShipEngines.EngineStatus} FTL:{ship.ShipEngines.ReadyForWarp} FLEET:{ship.ShipEngines.ReadyForFormationWarp}");

                VisualizeShipOrderQueue(ship);
                if (Screen.UState.IsSystemViewOrCloser && Mode == DebugModes.Normal && !Screen.ShowShipNames)
                    DrawWeaponArcs(ship);
                DrawSensorInfo(ship);

                DrawString($"On Defense: {ship.Loyalty.AI.DefensiveCoordinator.Contains(ship)}");
                if (ship.Fleet != null)
                {
                    DrawString($"Fleet: {ship.Fleet.Name}  {(int)ship.Fleet.FinalPosition.X}x{(int)ship.Fleet.FinalPosition.Y}  Vmax:{ship.Fleet.SpeedLimit}");
                }

                DrawString(ship.Pool != null ? "In Force Pool" : "NOT In Force Pool");

                if (ship.AI.State == AIState.SystemDefender)
                {
                    SolarSystem systemToDefend = ship.AI.SystemToDefend;
                    DrawString($"Defending {systemToDefend?.Name ?? "Awaiting Order"}");
                }

                DrawString(ship.System == null ? "Deep Space" : $"System: {ship.System.Name}");
                var influence = ship.GetProjectorInfluenceEmpires().Select(e => e.Name);
                DrawString("Influence: " + (ship.IsInFriendlyProjectorRange ? "Friendly"
                                         :  ship.IsInHostileProjectorRange  ? "Hostile" : "Neutral")
                                         + " | " + string.Join(",", influence));

                string gravityWell = ship.Universe.GravityWells ? ship?.System?.IdentifyGravityWell(ship)?.Name : "disabled";
                DrawString($"GravityWell: {gravityWell}   Inhibited:{ship.IsInhibitedByUnfriendlyGravityWell}");

                var combatColor = ship.InCombat ? Color.Green : Color.LightPink;
                var inCombat = ship.InCombat ? ship.AI.BadGuysNear ? "InCombat" : "ERROR" : "NotInCombat";
                DrawString(combatColor, $"{inCombat} PriTarget:{ship.AI.HasPriorityTarget} PriOrder:{ship.AI.HasPriorityOrder}");
                if (ship.AI.IgnoreCombat)
                    DrawString(Color.Pink, "Ignoring Combat!");
                if (ship.IsFreighter)
                {
                    DrawString($"Trade Timer:{ship.TradeTimer}");
                    ShipAI.ShipGoal g = ship.AI.OrderQueue.PeekLast;
                    if (g?.Trade != null && g.Trade.BlockadeTimer < 120)
                        DrawString($"Blockade Timer:{g.Trade.BlockadeTimer}");
                }

                if (ship.AI.Target is Ship shipTarget)
                {
                    SetTextCursor(Win.X + 200, 620f, Color.White);
                    DrawString("Target: "+ shipTarget.Name);
                    DrawString(shipTarget.Active ? "Active" : "Error - Active");
                }
                float currentStr = ship.GetStrength(), baseStr = ship.BaseStrength;
                DrawString($"Strength: {currentStr.String(0)} / {baseStr.String(0)}  ({(currentStr/baseStr).PercentString()})");
                DrawString($"HP: {ship.Health.String(0)} / {ship.HealthMax.String(0)}  ({ship.HealthPercent.PercentString()})");
                DrawString($"Mass: {ship.Mass.String(0)}");
                DrawString($"EMP Damage: {ship.EMPDamage} / {ship.EmpTolerance} :Recovery: {ship.EmpRecovery}");
                DrawString($"IntSlots: {ship.ActiveInternalModuleSlots}/{ship.NumInternalSlots}  ({ship.InternalSlotsHealthPercent.PercentString()})");
                DrawString($"DPS: {ship.TotalDps}");
                SetTextCursor(Win.X + 250, 600f, Color.White);
                foreach (KeyValuePair<SolarSystem, SystemCommander> entry in ship.Loyalty.AI.DefensiveCoordinator.DefenseDict)
                    foreach (var defender in entry.Value.OurShips) {
                        if (defender.Key == ship.Id)
                            DrawString(entry.Value.System.Name);
                    }
            }
            else if (Screen.SelectedShipList.NotEmpty)
            {
                IReadOnlyList<Ship> ships = Screen.SelectedShipList;
                foreach (Ship ship in ships)
                    VisualizeShipGoal(ship, false);

                DrawString($"SelectedShips: {ships.Count} ");
                DrawString($"Total Str: {ships.Sum(s => s.BaseStrength).String(1)} ");
            }
            VisualizePredictionDebugger();
        }

        IEnumerable<PredictionDebugPlatform> GetPredictionDebugPlatforms()
        {
            IReadOnlyList<Ship> ships = Screen.UState.Ships;
            for (int i = 0; i < ships.Count; ++i)
                if (ships[i] is PredictionDebugPlatform platform)
                    yield return platform;
        }

        void VisualizePredictionDebugger()
        {
            foreach (PredictionDebugPlatform platform in GetPredictionDebugPlatforms())
            {
                //DrawString($"Platform Accuracy: {(int)(platform.AccuracyPercent*100)}%");
                foreach (PredictedLine line in platform.Predictions)
                {
                    DrawLineImm(line.Start, line.End, Color.YellowGreen);
                    //DrawCircleImm(line.End, 75f, Color.Red);
                }
            }
        }

        void VisualizeShipGoal(Ship ship, bool detailed = true)
        {
            if (ship?.AI.OrderQueue.NotEmpty == true)
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
            if (ship?.AI.HasWayPoints == true)
            {
                WayPoint[] wayPoints = ship.AI.CopyWayPoints();
                if (wayPoints.Length > 0)
                {
                    DrawLineImm(ship.Position, wayPoints[0].Position, Color.ForestGreen);
                    for (int i = 1; i < wayPoints.Length; ++i) // draw WayPoints chain
                        DrawLineImm(wayPoints[i-1].Position, wayPoints[i].Position, Color.ForestGreen);
                }
            }
            if (ship?.Fleet != null)
            {
                Vector2 formationPos = ship.Fleet.GetFormationPos(ship);
                Vector2 finalPos = ship.Fleet.GetFinalPos(ship);
                Color color = Color.Magenta.Alpha(0.5f);
                DrawCircleImm(finalPos, ship.Radius*0.5f, Color.Blue.Alpha(0.5f), 0.8f);
                DrawCircleImm(formationPos, ship.Radius-10, color, 0.8f);
                DrawLineImm(ship.Position, formationPos, color, 0.8f);
            }
        }

        void VisualizeShipOrderQueue(Ship ship)
        {
            VisualizeShipGoal(ship);
            DrawShipOrderQueueInfo(ship);
            DrawWayPointsInfo(ship);
        }

        void DrawShipOrderQueueInfo(Ship ship)
        {
            if (ship.AI.OrderQueue.NotEmpty)
            {
                ShipAI.ShipGoal[] goals = ship.AI.OrderQueue.ToArray();
                Vector2 pos = ship.AI.GoalTarget;
                DrawString($"AIState: {ship.AI.State}  CombatState: {ship.AI.CombatState}  FromTarget: {pos.Distance(ship.Position).String(0)}");
                DrawString($"OrderQueue ({goals.Length}):");
                for (int i = 0; i < goals.Length; ++i)
                {
                    ShipAI.ShipGoal g = goals[i];
                    DrawString($"  {i+1}:  {g.Plan}  {g.MoveOrder}");
                }
            }
            else
            {
                DrawString($"AIState: {ship.AI.State}  CombatState: {ship.AI.CombatState}");
                DrawString("OrderQueue is EMPTY");
            }
        }

        void DrawWayPointsInfo(Ship ship)
        {
            if (ship.AI.HasWayPoints)
            {
                WayPoint[] wayPoints = ship.AI.CopyWayPoints();
                DrawString($"WayPoints ({wayPoints.Length}):");
                for (int i = 0; i < wayPoints.Length; ++i)
                    DrawString($"  {i+1}:  {wayPoints[i].Position}");
            }
        }

        public void DefenseCoLogsNull(bool found, Ship ship, SolarSystem systemToDefend)
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            if (!found && ship.Active)
            {
                Log.Info(ConsoleColor.Yellow, systemToDefend == null
                                    ? "SystemCommander: Remove : SystemToDefend Was Null"
                                    : "SystemCommander: Remove : Ship Not Found in Any");
            }
        }

        public void DefenseCoLogsMultipleSystems(Ship ship)
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: $"SystemCommander: Remove : Ship Was in Multiple SystemCommanders: {ship}");
        }
        public void DefenseCoLogsNotInSystem()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Not in SystemCommander");
        }

        public void DefenseCoLogsNotInPool()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : Not in DefensePool");
        }
        public void DefenseCoLogsSystemNull()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : SystemToDefend Was Null");
        }
    }
}
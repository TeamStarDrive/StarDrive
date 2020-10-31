using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug.Page;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.Sandbox;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Ships.AI;
using static Ship_Game.AI.ShipAI;
using Ship_Game.Fleets;

namespace Ship_Game.Debug
{
    public enum DebugModes
    {
        Normal,
        Targeting,
        PathFinder,
        DefenseCo,
        Trade,
        Planets,
        AO,
        ThreatMatrix,
        SpatialManager,
        input,
        Tech,
        Solar, // Sun timers, black hole data, pulsar radiation radius...
        RelationsWar,
        Pirates,
        Remnants,
        Last // dummy value
    }


    public sealed partial class DebugInfoScreen : GameScreen
    {
        public bool IsOpen = true;
        readonly UniverseScreen Screen;
        Rectangle Win = new Rectangle(30, 100, 1200, 700);
        public static int ShipsDied;
        public static int ProjDied;
        public static int ProjCreated;
        public static int ModulesCreated;
        public static int ModulesDied;
        public static string CanceledMTaskName;
        public static int CanceledMtasksCount;
        public static string CanceledMTask1Name;
        public static string CanceledMTask2Name;
        public static string CanceledMTask3Name;
        public static string CanceledMTask4Name;
        public static int CanceledMtask1Count;
        public static int CanceledMtask2Count;
        public static int CanceledMtask3Count;
        public static int CanceledMtask4Count;
        int ShipsNotInForcePool;
        int ShipsInDefForcePool;
        int ShipsInAoPool;

        public static DebugModes Mode { get; private set; }
        readonly Array<DebugPrimitive> Primitives = new Array<DebugPrimitive>();
        DebugPage Page;
        readonly FloatSlider SpeedLimitSlider;
        readonly FloatSlider DebugPlatformSpeed;
        bool CanDebugPlatformFire;

        public DebugInfoScreen(UniverseScreen screen) : base(screen, pause:false)
        {
            Screen = screen;
            if (screen is DeveloperSandbox.DeveloperUniverse)
            {
                SpeedLimitSlider = Slider(RelativeToAbsolute(-200f, 400f), 200, 40, "Debug SpeedLimit", 0f, 1f, 1f);
                DebugPlatformSpeed = Slider(RelativeToAbsolute(-200f, 440f), 200, 40, "Platform Speed", -500f, 500f, 0f);
                Checkbox(RelativeToAbsolute(-200f, 480f), () => CanDebugPlatformFire, "Start Firing", 0);
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (empire == Empire.Universe.player || empire.isFaction)
                    continue;

                bool flag = false;
                foreach (Ship ship in empire.GetShips())
                {
                    if (ship?.Active != true) continue;
                    if (ship.DesignRole < ShipData.RoleName.troopShip) continue;
                    if (empire.Pool.ForcePoolContains(ship)) continue;

                    foreach (AO ao in empire.GetEmpireAI().AreasOfOperations)
                    {
                        if (ao.OffensiveForcePoolContains(ship) || ao.WaitingShipsContains(ship) || ao.GetCoreFleet() == ship.fleet)
                        {
                            ShipsInAoPool++;
                            flag = true;
                        }
                    }

                    if (flag)
                        continue;

                    if (empire.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship) )
                    {
                        ++ShipsInDefForcePool;
                        continue;
                    }

                    ++ShipsNotInForcePool;
                }
            }
        }

        readonly Dictionary<string, Array<string>> ResearchText = new Dictionary<string, Array<string>>();

        public void ResearchLog(string text, Empire empire)
        {
            if (!DebugLogText(text, DebugModes.Tech))
                return;
            if (ResearchText.TryGetValue(empire.Name, out Array<string> empireTechs))
            {
                empireTechs.Add(text);
            }
            else
            {
                ResearchText.Add(empire.Name, new Array<string> {text});
            }
        }

        public void ClearResearchLog(Empire empire)
        {
            if (ResearchText.TryGetValue(empire.Name, out Array<string> empireTechs))
                empireTechs.Clear();
        }

        public override void PerformLayout()
        {
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.Left) || input.KeyPressed(Keys.Right))
            {
                ResearchText.Clear();
                HideAllDebugGameInfo();
                Mode += input.KeyPressed(Keys.Left) ? -1 : +1;
                if      (Mode >= DebugModes.Last)  Mode = DebugModes.Normal;
                else if (Mode < DebugModes.Normal) Mode = DebugModes.Last - 1;
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
                switch (Mode)
                {
                    case DebugModes.PathFinder: Page = Add(new PathFinderDebug(Screen,  this)); break;
                    case DebugModes.Trade:   Page = Add(new TradeDebug(Screen, this)); break;
                    case DebugModes.Planets: Page = Add(new PlanetDebug(Screen,this)); break;
                    case DebugModes.Solar:   Page = Add(new SolarDebug(Screen, this)); break;
                    case DebugModes.RelationsWar: Page = Add(new DebugWar(Screen, this)); break;
                }
            }

            UpdateDebugShips();
            base.Update(fixedDeltaTime);
        }

        void UpdateDebugShips()
        {
            if (DebugPlatformSpeed == null) // platform is only enabled in sandbox universe
                return;
            float platformSpeed = DebugPlatformSpeed.AbsoluteValue;
            float speedLimiter = SpeedLimitSlider.RelativeValue;

            if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;
                ship.SetSpeedLimit(speedLimiter * ship.VelocityMaximum);
            }

            foreach (PredictionDebugPlatform platform in GetPredictionDebugPlatforms())
            {
                platform.CanFire = CanDebugPlatformFire;
                if (platformSpeed.NotZero())
                {
                    platform.Velocity.X = platformSpeed;
                }
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Page?.Draw(batch, elapsed);

            try
            {
                TextFont = Fonts.Arial20Bold;
                SetTextCursor(50f, 50f, Color.Red);

                DrawString(Color.Yellow, Mode.ToString());
                //DrawString("Ships Died:   " + ShipsDied);
                //DrawString("Proj Died:    " + ProjDied);
                //DrawString("Proj Created: " + ProjCreated);
                //DrawString("Mods Created: " + ModulesCreated);
                //DrawString("Mods Died:    " + ModulesDied);

                TextCursor.Y -= (float)(Fonts.Arial20Bold.LineSpacing + 2) * 4;
                TextCursor.X += Fonts.Arial20Bold.TextWidth("XXXXXXXXXXXXXXXXXXXX");
                //DrawString("LastMTaskCanceled: " + CanceledMTaskName);

                //DrawString(CanceledMTask1Name + ": " + CanceledMtask1Count);
                //DrawString(CanceledMTask2Name + ": " + CanceledMtask2Count);
                //DrawString(CanceledMTask3Name + ": " + CanceledMtask3Count);
                //DrawString(CanceledMTask4Name + ": " + CanceledMtask4Count);

                //DrawString($"Ships not in Any Pool: {ShipsNotInForcePool} In Defenspool: {ShipsInDefForcePool} InAoPools: {ShipsInAoPool} ");
                DrawDebugPrimitives(elapsed.RealTime.Seconds);
                TextFont = Fonts.Arial12Bold;
                switch (Mode)
                {
                    case DebugModes.Normal:       EmpireInfo();       break;
                    case DebugModes.DefenseCo:    DefcoInfo();        break;
                    case DebugModes.ThreatMatrix: ThreatMatrixInfo(); break;
                    case DebugModes.Targeting:    Targeting();        break;
                    case DebugModes.input:        InputDebug();       break;
                    case DebugModes.Tech:         Tech();             break;
                    case DebugModes.Pirates:      Pirates();          break;
                    case DebugModes.Remnants:     RemnantInfo();      break;
                }
                base.Draw(batch, elapsed);
                ShipInfo();
            }
            catch { }
        }



        void Tech()
        {
            TextCursor.Y -= (float)(Fonts.Arial20Bold.LineSpacing + 2) * 4;
            int column = 0;
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e.isFaction || e.data.Defeated)
                    continue;

                SetTextCursor(Win.X + 10 + 255 * column, Win.Y + 10, e.EmpireColor);
                DrawString(e.data.Traits.Name);

                if (e.data.DiplomaticPersonality != null)
                {
                    DrawString(e.data.DiplomaticPersonality.Name);
                    DrawString(e.data.EconomicPersonality.Name);
                }

                DrawString($"corvettes: {e.canBuildCorvettes}");
                DrawString($"frigates: {e.canBuildFrigates}");
                DrawString($"cruisers: {e.canBuildCruisers}");
                DrawString($"bombers: {e.canBuildBombers}");
                DrawString($"carriers: {e.canBuildCarriers}");

                if (e.Research.HasTopic)
                {
                    DrawString($"Research: {e.Research.Current.Progress:0}/{e.Research.Current.TechCost:0} ({e.Research.NetResearch.String()} / {e.Research.MaxResearchPotential.String()})");
                    DrawString("   --" + e.Research.Topic);
                    Ship bestShip = e.GetEmpireAI().TechChooser.LineFocus.BestCombatShip;
                    if (bestShip != null)
                    {
                        var neededTechs = bestShip.shipData.TechsNeeded.Except(e.ShipTechs);
                        float techCost = 0;
                        foreach(var tech in neededTechs)
                            techCost += e.TechCost(tech);

                        DrawString($"Ship : {bestShip.Name}");
                        DrawString($"Hull : {bestShip.BaseHull.Role}");
                        DrawString($"Role : {bestShip.DesignRole}");
                        DrawString($"Str : {(int)bestShip.BaseStrength} - Tech : {techCost}");
                    }
                }
                DrawString("");
                if (ResearchText.TryGetValue(e.Name, out var empireLog))
                    for (int x = 0; x < empireLog.Count - 1; x++)
                    {
                        var text = empireLog[x];
                        DrawString(text ?? "Error");
                    }
                ++column;
            }
        }

        void Targeting()
        {
            Array<Ship> masterShipList = Screen.GetMasterShipList();
            for (int i = 0; i < masterShipList.Count; ++i)
            {
                Ship ship = masterShipList[i];
                if (ship == null || !ship.InFrustum || ship.AI.Target == null)
                    continue;

                foreach (Weapon weapon in ship.Weapons)
                {
                    var module = weapon.FireTarget as ShipModule;
                    if (module == null || module.GetParent() != ship.AI.Target || weapon.Tag_Beam || weapon.Tag_Guided)
                        continue;

                    Screen.DrawCircleProjected(module.Center, 8f, 6, Color.MediumVioletRed);
                    if (weapon.DebugLastImpactPredict.NotZero())
                    {
                        Vector2 impactNoError = weapon.ProjectedImpactPointNoError(module);
                        Screen.DrawLineProjected(weapon.Origin, weapon.DebugLastImpactPredict, Color.Yellow);

                        Screen.DrawCircleProjected(impactNoError, 22f, 10, Color.BlueViolet, 2f);
                        Screen.DrawStringProjected(impactNoError, 28f, Color.BlueViolet, "pip");
                        Screen.DrawLineProjected(impactNoError, weapon.DebugLastImpactPredict, Color.DarkKhaki, 2f);
                    }

                    // TODO: re-implement this
                    //Projectile projectile = ship.CopyProjectiles.FirstOrDefault(p => p.Weapon == weapon);
                    //if (projectile != null)
                    //{
                    //    Screen.DrawLineProjected(projectile.Center, projectile.Center + projectile.Velocity, Color.Red);
                    //}
                    break;
                }
            }
        }

        void DrawWeaponArcs(Ship ship)
        {
            foreach (Weapon w in ship.Weapons)
            {
                ShipModule m = w.Module;
                float facing = ship.Rotation + m.FacingRadians;
                float size = w.GetActualRange();

                Screen.ProjectToScreenCoords(m.Center, size, 
                                      out Vector2 posOnScreen, out float sizeOnScreen);
                ShipDesignScreen.DrawWeaponArcs(ScreenManager.SpriteBatch,
                                      ship.Rotation, w, m, posOnScreen, sizeOnScreen*0.25f, ship.TrackingPower);

                DrawCircleImm(w.Origin, m.Radius/(float)Math.Sqrt(2), Color.Crimson);

                Ship targetShip = ship.AI.Target;
                GameplayObject target = targetShip;
                if (w.FireTarget is ShipModule sm)
                {
                    targetShip = sm.GetParent();
                    target = sm;
                }

                if (targetShip != null)
                {
                    bool inRange = ship.CheckRangeToTarget(w, target);
                    float bigArc = m.FieldOfFire*1.2f;
                    bool inBigArc = RadMath.IsTargetInsideArc(m.Center, target.Center,
                                                    ship.Rotation + m.FacingRadians, bigArc);
                    if (inRange && inBigArc) // show arc lines if we are close to arc edges
                    {
                        bool inArc = ship.IsInsideFiringArc(w, target.Center);

                        Color inArcColor = inArc ? Color.LawnGreen : Color.Orange;
                        DrawLineImm(m.Center, target.Center, inArcColor, 3f);

                        DrawLineImm(m.Center, m.Center + facing.RadiansToDirection() * size, Color.Crimson);
                        Vector2 left  = (facing - m.FieldOfFire * 0.5f).RadiansToDirection();
                        Vector2 right = (facing + m.FieldOfFire * 0.5f).RadiansToDirection();
                        DrawLineImm(m.Center, m.Center + left * size, Color.Crimson);
                        DrawLineImm(m.Center, m.Center + right * size, Color.Crimson);

                        string text = $"Target: {targetShip.Name}\nInArc: {inArc}";
                        DrawShadowStringProjected(m.Center, 0f, 1f, inArcColor, text);
                    }
                }
            }
        }

        void ShipInfo()
        {
            float y = (ScreenHeight - 700f).Clamped(100, 450);
            SetTextCursor(Win.X + 10, y, Color.White);

            if (Screen.SelectedFleet != null)
            {
                Fleet fleet = Screen.SelectedFleet;
                DrawArrowImm(fleet.FinalPosition, fleet.FinalPosition+fleet.FinalDirection*200f, Color.OrangeRed);
                foreach (Ship ship in fleet.Ships)
                    VisualizeShipGoal(ship, false);

                if (fleet.FleetTask != null)
                {
                    DrawString(fleet.FleetTask.type.ToString());

                    if (fleet.FleetTask.TargetPlanet != null)
                        DrawString(fleet.FleetTask.TargetPlanet.Name);

                    DrawString("Step: "+fleet.TaskStep);
                    DrawString("Fleet Speed: " + fleet.SpeedLimit);
                    DrawString("Ready For Warp: " + fleet.ReadyForWarp);
                    DrawString("In Formation Warp: " + fleet.InFormationWarp);
                    DrawString("Ships: " + fleet.Ships.Count);
                    DrawString("Strength: " + fleet.GetStrength());
                }
                else
                {
                    // @todo DrawLines similar to UniverseScreen.DrawLines. This code should be refactored
                    DrawString("Core fleet :" + fleet.IsCoreFleet);
                    DrawString(fleet.Name);
                    DrawString("Ships: " + fleet.Ships.Count);
                    DrawString("Strength: " + fleet.GetStrength());
                    DrawString("FleetSpeed: " + fleet.SpeedLimit);
                    DrawString("Distance: " + fleet.FinalPosition.Distance(fleet.AveragePosition()));

                    DrawString("Ready For Warp: " + fleet.ReadyForWarp);
                    DrawString("In Formation Warp: " + fleet.InFormationWarp);
                    DrawCircleImm(fleet.AveragePosition(), 30, Color.Magenta);
                    DrawCircleImm(fleet.AveragePosition(), 60, Color.DarkMagenta);
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

                if (group.HasFleetGoal)
                {
                    DrawLineImm(group.FinalPosition, group.NextGoalMovePosition, Color.YellowGreen);
                }
            }
            else if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;

                DrawString($"Ship {ship.ShipName}  x {(int)ship.Center.X} y {(int)ship.Center.Y}");
                DrawString($"VEL: {(int)ship.Velocity.Length()}  "
                          +$"LIMIT: {(int)ship.SpeedLimit}  {ship.WarpState}"
                          +$"  {ship.ThrustThisFrame}  {ship.DebugThrustStatus}");
                VisualizeShipOrderQueue(ship);
                DrawWeaponArcs(ship);

                DrawString($"On Defense: {ship.loyalty.GetEmpireAI().DefensiveCoordinator.Contains(ship)}");
                if (ship.fleet != null)
                {
                    DrawString($"Fleet {ship.fleet.Name}  {(int)ship.fleet.FinalPosition.X}x{(int)ship.fleet.FinalPosition.Y}");
                    DrawString($"Fleet speed: {ship.fleet.SpeedLimit}");
                }

                DrawString(ship.loyalty.GetEmpireAI().AreasOfOperations.Any(ao=> ao.OffensiveForcePoolContains(ship)) ? "In Force Pool" : "NOT In Force Pool");

                if (ship.AI.State == AIState.SystemDefender)
                {
                    SolarSystem systemToDefend = ship.AI.SystemToDefend;
                    DrawString($"Defending {systemToDefend?.Name ?? "Awaiting Order"}");
                }

                DrawString(ship.System == null ? "Deep Space" : $"{ship.System.Name} system");
                string[] influence = ship.GetProjectorInfluenceEmpires().Select(e=>e.Name).ToArray();
                DrawString("Influences: " + string.Join(",", influence));
                DrawString("InfluenceType: " + (ship.IsInFriendlyProjectorRange ? "Friendly"
                                             :  ship.IsInHostileProjectorRange  ? "Hostile" : "Neutral"));

                string gravityWell = Empire.Universe.GravityWells ? ship?.System?.IdentifyGravityWell(ship)?.Name : "disabled";
                DrawString($"GravityWell: {gravityWell}   Inhibited:{ship.IsInhibitedByUnfriendlyGravityWell}");

                DrawString(ship.InCombat ? Color.Green : Color.LightPink,
                           ship.InCombat ? ship.AI.BadGuysNear ? "InCombat" : "ERROR" : "Not in Combat");
                DrawString(ship.AI.HasPriorityTarget ? "Priority Target" : "No Priority Target");
                DrawString(ship.AI.HasPriorityOrder ? "Priority Order" : "No Priority Order");
                if (ship.IsFreighter)
                {
                    DrawString($"Trade Timer:{ship.TradeTimer}");
                    ShipGoal g = ship.AI.OrderQueue.PeekLast;
                    if (g?.Trade != null && g.Trade.BlockadeTimer < 120)
                        DrawString($"Blockade Timer:{g.Trade.BlockadeTimer}");
                }

                if (ship.AI.Target is Ship shipTarget)
                {
                    SetTextCursor(Win.X + 200, 620f, Color.White);
                    DrawString("Target: "+ shipTarget.Name);
                    DrawString(shipTarget.Active ? "Active" : "Error - Active");
                }
                DrawString($"Strength: {ship.BaseStrength}");
                DrawString($"VelocityMax: {ship.VelocityMaximum}  FTLMax: {ship.MaxFTLSpeed}");
                DrawString($"HP: {ship.Health} / {ship.HealthMax}");
                DrawString("Ship Mass: " + ship.Mass);
                DrawString("EMP Damage: " + ship.EMPDamage + " / " + ship.EmpTolerance + " :Recovery: " + ship.EmpRecovery);
                DrawString("ActiveIntSlots: " + ship.ActiveInternalSlotCount + " / " + ship.InternalSlotCount + " (" + Math.Round((decimal)ship.ActiveInternalSlotCount / ship.InternalSlotCount * 100,1) + "%)");
                SetTextCursor(Win.X + 250, 600f, Color.White);
                foreach (KeyValuePair<SolarSystem, SystemCommander> entry in ship.loyalty.GetEmpireAI().DefensiveCoordinator.DefenseDict)
                    foreach (var defender in entry.Value.OurShips) {
                        if (defender.Key == ship.guid)
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
            Array<Ship> ships = Screen.GetMasterShipList();
            for (int i = 0; i < ships.Count; ++i)
                if (ships[i] is PredictionDebugPlatform platform)
                    yield return platform;
        }

        void VisualizePredictionDebugger()
        {
            foreach (PredictionDebugPlatform platform in GetPredictionDebugPlatforms())
            {
                DrawString($"Platform Accuracy: {(int)(platform.AccuracyPercent*100)}%");
                foreach (PredictedLine line in platform.Predictions)
                {
                    DrawLineImm(line.Start, line.End, Color.YellowGreen);
                    //DrawCircleImm(line.End, 75f, Color.Red);
                }
            }
        }

        void VisualizeShipGoal(Ship ship, bool detailed = true)
        {
            if (ship.AI.OrderQueue.NotEmpty)
            {
                ShipGoal goal = ship.AI.OrderQueue.PeekFirst;
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
                for (int i = 1; i < wayPoints.Length; ++i) // draw WayPoints chain
                    DrawLineImm(wayPoints[i-1].Position, wayPoints[i].Position, Color.ForestGreen);
            }
            if (ship.fleet != null)
            {
                Vector2 formationPos = ship.fleet.GetFormationPos(ship);
                Color color = Color.Magenta.Alpha(0.5f);
                DrawCircleImm(formationPos, ship.Radius-10, color, 0.8f);
                DrawLineImm(ship.Center, formationPos, color, 0.8f);
            }
        }

        void VisualizeShipOrderQueue(Ship ship)
        {
            VisualizeShipGoal(ship);

            if (ship.AI.OrderQueue.NotEmpty)
            {
                ShipGoal[] goals = ship.AI.OrderQueue.ToArray();
                Vector2 pos = goals[0].TargetPlanet?.Center ?? ship.AI.Target?.Center ?? goals[0].MovePosition;
                DrawString($"Ship distance from goal: {pos.Distance(ship.Position)}");
                DrawString($"AI State: {ship.AI.State}");
                DrawString($"Combat State: {ship.AI.CombatState}");
                DrawString($"OrderQueue ({goals.Length}):");
                for (int i = 0; i < goals.Length; ++i)
                    DrawString($"  {i}: {goals[i].Plan}");
            }
            else
            {
                DrawString($"AI State: {ship.AI.State}");
                DrawString($"Combat State: {ship.AI.CombatState}");
                DrawString("OrderQueue is EMPTY");
            }
            if (ship.AI.HasWayPoints)
            {
                WayPoint[] wayPoints = ship.AI.CopyWayPoints();
                DrawString($"WayPoints ({wayPoints.Length}):");
                for (int i = 0; i < wayPoints.Length; ++i)
                    DrawString($"  {i}:  {wayPoints[i].Position}");
            }
        }

        void Pirates()
        {
            int column = 0;
            foreach (Empire e in EmpireManager.PirateFactions)
            {
                if (e.data.Defeated)
                    continue;

                var goals = e.Pirates.Goals;
                SetTextCursor(Win.X + 10 + 255 * column, Win.Y + 95, e.EmpireColor);
                DrawString("------------------------");
                DrawString(e.Name);
                DrawString("------------------------");
                DrawString($"Level: {e.Pirates.Level}");
                DrawString($"Pirate Bases Goals: {goals.Count(g => g.type == GoalType.PirateBase)}");
                DrawString($"Pirate Payments Goals: {goals.Count(g => g.type == GoalType.PirateDirectorPayment)}");
                DrawString($"Spawned Ships: {e.Pirates.SpawnedShips.Count}");
                NewLine();
                DrawString($"Raid Management Goals ({goals.Count(g => g.type == GoalType.PirateDirectorRaid)})");
                DrawString("---------------------------------------------");
                foreach (Goal g in goals)
                {
                    if (g.type == GoalType.PirateDirectorRaid)
                    {
                        Empire target = g.TargetEmpire;
                        string targetName = target.Name;
                        int threatLevel = e.Pirates.ThreatLevelFor(g.TargetEmpire);
                        DrawString(target.EmpireColor, $"Raid Director For: {targetName}, Threat Level: {threatLevel}");
                    }
                }

                NewLine();
                DrawString($"Ongoing Raids ({goals.Count(g => g.IsRaid)}/{e.Pirates.Level})");
                DrawString("---------------------------------------------");
                foreach (Goal g in goals)
                {
                    if (g.IsRaid)
                    {
                        Empire target = g.TargetEmpire;
                        string targetName = target.Name;
                        Ship targetShip = g.TargetShip;
                        string shipName = targetShip?.Name ?? "None";
                        DrawString(target.EmpireColor, $"{g.type} vs. {targetName}, Target Ship: {shipName} in {targetShip?.SystemName ?? "None"}");
                    }
                }

                NewLine();

                DrawString($"Base Defense Goals ({goals.Count(g => g.type == GoalType.PirateDefendBase)})");
                DrawString("---------------------------------------------");
                foreach (Goal g in goals)
                {
                    if (g.type == GoalType.PirateDefendBase)
                    {
                        Ship targetShip = g.TargetShip;
                        string shipName = targetShip?.Name ?? "None";
                        DrawString($"Defending {shipName} in {targetShip?.SystemName ?? "None"}");
                    }
                }

                NewLine(1);

                DrawString($"Fighter Designs We Can Launch ({e.Pirates.ShipsWeCanBuild.Count})");
                DrawString("---------------------------------------------");
                foreach (string shipName in e.Pirates.ShipsWeCanBuild)
                    DrawString(shipName);

                NewLine();

                DrawString($"Ship Designs We Can Spawn ({e.Pirates.ShipsWeCanSpawn.Count})");
                DrawString("---------------------------------------------");
                foreach (string shipName in e.Pirates.ShipsWeCanSpawn)
                    DrawString(shipName);

                column += 3;
            }
        }

        void RemnantInfo()
        {
            Empire e = EmpireManager.Remnants;
            SetTextCursor(Win.X + 10 + 255, Win.Y + 150, e.EmpireColor);
            DrawString($"Remnant Story: {e.Remnants.Story}");
            DrawString(!e.Remnants.Activated
                ? $"Trigger Progress: {e.Remnants.StoryTriggerKillsXp}/{e.Remnants.ActivationXpNeeded.String()}"
                : $"Level Up Stardate: {e.Remnants.NextLevelUpDate}");

            DrawString(!e.Remnants.Hibernating
                ? $"Next Hibernation in: {e.Remnants.NextLevelUpDate - e.Remnants.NeededHibernationTurns / 10f}"
                : $"Hibernating for: {e.Remnants.HibernationTurns} turns");

            string activatedString = e.Remnants.Activated ? "Yes" : "No";
            activatedString = e.data.Defeated ? "Defeated" : activatedString;
            DrawString($"Activated: {activatedString}");
            DrawString($"Level: {e.Remnants.Level}");



            DrawString($"Resources: {e.Remnants.Production.String()}");
            NewLine();
            DrawString("Empires Score and Strength:");
            for (int i = 0; i < EmpireManager.MajorEmpires.Length; i++)
            {
                Empire empire = EmpireManager.MajorEmpires[i];
                DrawString(empire.EmpireColor, $"{empire.data.Name} - Score: {empire.TotalScore}, Strength: {empire.CurrentMilitaryStrength}");
            }

            var empiresList = GlobalStats.RestrictAIPlayerInteraction ? EmpireManager.NonPlayerEmpires
                                                                      : EmpireManager.MajorEmpires;
            NewLine();
            float averageScore = (float)empiresList.Average(empire => empire.TotalScore);
            float averageStr = empiresList.Average(empire => empire.CurrentMilitaryStrength);
            DrawString($"AI Empire Average Score:     {averageScore.String(0)}");
            DrawString($"AI Empire Average Strength: {averageStr.String(0)}");

            NewLine();
            Empire bestScore = empiresList.FindMax(empire => empire.TotalScore);
            Empire bestStr = empiresList.FindMax(empire => empire.CurrentMilitaryStrength);

            float diffFromAverageScore = bestScore.TotalScore / averageScore.LowerBound(1) * 100;
            float diffFromAverageStr = bestStr.CurrentMilitaryStrength / averageStr.LowerBound(1) * 100;

            DrawString(bestScore.EmpireColor, $"Highest Score Empire: {bestScore.data.Name} ({(diffFromAverageScore - 100).String(1)}% above average)");
            DrawString(bestStr.EmpireColor, $"Highest Str Empire:     {bestStr.data.Name} ({(diffFromAverageStr - 100).String(1)}% above average)");

            NewLine();
            DrawString("Goals:");
            foreach (Goal goal in e.GetEmpireAI().Goals)
            {
                if (goal.type != GoalType.RemnantBalancersEngage)
                {
                    DrawString($"{goal.type}");
                }
                else
                {
                    Color color = goal.ColonizationTarget?.Owner?.EmpireColor ?? e.EmpireColor;
                    DrawString(color, $"{goal.type}, Target Planet: {goal.ColonizationTarget?.Name}, Bombers Wanted: {goal.ShipLevel}");
                }
            }

            NewLine();
            DrawString("Fleets:");
            foreach (Fleet fleet in e.GetFleetsDict().Values)
            {
                if (fleet.FleetTask == null)
                    continue;

                Color color = fleet.FleetTask.TargetPlanet.Owner?.EmpireColor ?? e.EmpireColor;
                DrawString(color,$"Target Planet: {fleet.FleetTask.TargetPlanet.Name}, Ships: {fleet.Ships.Count}" +
                                  $", str: {fleet.GetStrength().String()}, Task Step: {fleet.TaskStep}");
            }
        }

        void EmpireInfo()
        {
            int column = 0;
            foreach (Empire e in EmpireManager.MajorEmpires)
            {
                if (e.data.Defeated)
                    continue;

                SetTextCursor(Win.X + 10 + 255 * column, Win.Y + 95, e.EmpireColor);
                DrawString(e.data.Traits.Name);

                if (e.data.DiplomaticPersonality != null)
                {
                    DrawString(e.data.DiplomaticPersonality.Name);
                    DrawString(e.data.EconomicPersonality.Name);
                }
                DrawString($"Money: {e.Money.String()} A:({e.GetActualNetLastTurn().String()}) T:({e.GrossIncome.String()})");
                DrawString($"Treasury Goal: {e.GetEmpireAI().TreasuryGoal().String()}");
                float taxRate = e.data.TaxRate * 100f;
                DrawString("Tax Rate:     "+taxRate.ToString("#.0")+"%");
                DrawString($"Ship Maint:  ({(int)e.GetEmpireAI().BuildCapacity}) InUse:{(int)e.TotalShipMaintenance} - Scrap:{(int)e.TotalMaintenanceInScrap}");
                DrawString($"Ship War Maint:  War:{(int)e.TotalWarShipMaintenance} - Orb:{(int)e.TotalOrbitalMaintenance} - Trp:{(int)e.TotalTroopShipMaintenance}");
                DrawString($"Ship Civ Maint:  Civ:{(int)e.TotalCivShipMaintenance} - Sup:{(int)e.TotalEmpireSupportMaintenance}");

                Array<Ship> ships = e.GetShips();
                DrawString($"Ship Count:  ({ships.Count}) " +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.platform || warship?.DesignRole == ShipData.RoleName.station)}" +
                           $" {ships.Count(warship => warship?.DesignRole ==  ShipData.RoleName.fighter || warship?.DesignRole == ShipData.RoleName.corvette)}" +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.frigate)}" +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.cruiser )}" +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.capital)}" +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.carrier)}" +
                           $" {ships.Count(warship => warship?.DesignRole == ShipData.RoleName.bomber)}"
                           );
                DrawString($"Build Maint:   ({(int)e.data.ColonyBudget}) {(int)e.TotalBuildingMaintenance}");
                DrawString($"Spy Count:     ({(int)e.data.SpyBudget}) {e.data.AgentList.Count}");
                DrawString("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
                DrawString("Planet Count:  "+e.GetPlanets().Count);
                if (e.Research.HasTopic)
                {
                    DrawString($"Research: {e.Research.Current.Progress:0}/{e.Research.Current.TechCost:0}({e.Research.NetResearch.String()})");
                    DrawString("   --" + e.Research.Topic);
                }
                else
                {
                    NewLine(2);
                }

                NewLine(3);
                DrawString("Total Pop: "+ e.GetTotalPop(out float maxPop).String(1) 
                                        + "/" + maxPop.String(1) 
                                        + "/" + e.GetTotalPopPotential().String(1));

                DrawString("Gross Food: "+ e.GetGrossFoodPerTurn().String());
                DrawString("Military Str: "+ (int)e.CurrentMilitaryStrength);
                DrawString($"Fleets: Str: {(int)e.Pool.InitialStrength} Avail: {e.Pool.InitialReadyFleets}");
                for (int x = 0; x < e.GetEmpireAI().Goals.Count; x++)
                {
                    Goal g = e.GetEmpireAI().Goals[x];
                    if (!(g is MarkForColonization))
                        continue;

                    NewLine();
                    string held = g.Held ? "(Held" : "";
                    DrawString($"{held}{g.UID} {g.ColonizationTarget.Name}" +
                               $" (x{e.GetTargetsStrMultiplier(g.ColonizationTarget.guid).String(1)})");

                    DrawString(15f, $"Step: {g.StepName}");
                    if (g.FinishedShip != null && g.FinishedShip.Active)
                        DrawString(15f, "Has ship");
                }

                MilitaryTask[] tasks = e.GetEmpireAI().GetTasks().ToArray();
                for (int j = 0; j < tasks.Length; j++)
                {
                    MilitaryTask task = tasks[j];
                    string sysName = "Deep Space";
                    for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
                    {
                        SolarSystem sys = UniverseScreen.SolarSystemList[i];
                        if (task.AO.InRadius(sys.Position, sys.Radius))
                            sysName = sys.Name;
                    }

                    NewLine();
                    var planet =task.TargetPlanet?.Name ?? "";
                    DrawString($"FleetTask: {task.type} {sysName} {planet}");
                    DrawString(15f, $"Step:  {task.Step} - Priority:{task.Priority}");
                    float ourStrength = task.Fleet?.GetStrength() ?? task.MinimumTaskForceStrength;
                    string strMultiplier = task.TargetPlanet != null 
                        ? $" (x{e.GetTargetsStrMultiplier(task.TargetPlanet.guid).String(1)})" 
                        : "";
                    
                    if (task.type == MilitaryTask.TaskType.AssaultPirateBase && task.TargetShip != null)
                        strMultiplier = $" (x{e.GetTargetsStrMultiplier(task.TargetShip.guid).String(1)})";

                    DrawString(15f, $"Strength: Them: {(int)task.EnemyStrength} Us: {(int)ourStrength} {strMultiplier}");
                    if (task.WhichFleet != -1)
                    {
                        DrawString(15f, "Fleet: " + task.Fleet.Name);
                        DrawString(15f, $" Ships: {task.Fleet.Ships.Count} CanWin: {task.Fleet.CanTakeThisFight(task.EnemyStrength)}");
                    }
                }

                NewLine();
                foreach ((Empire them, Relationship rel) in e.AllRelations)
                {
                    TextColor = them.EmpireColor;
                    if (rel.Treaty_NAPact)
                        DrawString(15f, "NA Pact with "+ them.data.Traits.Plural);

                    if (rel.Treaty_Trade)
                        DrawString(15f, "Trade Pact with "+ them.data.Traits.Plural);

                    if (rel.Treaty_OpenBorders)
                        DrawString(15f, "Open Borders with "+ them.data.Traits.Plural);

                    if (rel.AtWar)
                        DrawString(15f, $"War with {them.data.Traits.Plural} ({rel.ActiveWar?.WarType})");
                }
                ++column;
                if (Screen.SelectedSystem != null)
                {
                    SetTextCursor(Win.X + 10, 600f, Color.White);
                    foreach (Ship ship in Screen.SelectedSystem.ShipList)
                    {
                        DrawString(ship.Active ? ship.Name : ship.Name + " (inactive)");
                    }

                    SetTextCursor(Win.X + 300, 600f, Color.White);
                }
            }
        }

        void DefcoInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                DefensiveCoordinator defco = e.GetEmpireAI().DefensiveCoordinator;
                foreach (var kv in defco.DefenseDict)
                {
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.RankImportance * 100, e.EmpireColor, 6);
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.IdealShipStrength * 10, e.EmpireColor, 3);
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.TroopsWanted * 100, e.EmpireColor, 4);
                }
                foreach(Ship ship in defco.DefensiveForcePool)
                    Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 6);

                foreach(AO ao in e.GetEmpireAI().AreasOfOperations)
                    Screen.DrawCircleProjectedZ(ao.Center, ao.Radius, e.EmpireColor, 16);
            }
        }

        void ThreatMatrixInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                var pins = e.GetEmpireAI().ThreatMatrix.GetPins();
                for (int i = 0; i < pins.Length; i++)
                {
                    ThreatMatrix.Pin pin = pins[i];
                    if (pin?.Ship == null || pin.Position == Vector2.Zero)
                        continue;
                    float increaser = (int) Empire.Universe.viewState / 100f;
                    Screen.DrawCircleProjected(pin.Position,
                        increaser + pin.Ship.Radius, 6, e.EmpireColor);

                    if (!pin.InBorders) continue;
                    Screen.DrawCircleProjected(pin.Position,
                        increaser + pin.Ship.Radius, 3, e.EmpireColor);
                }
            }
        }

        void SpatialManagement()
        {
            SetTextCursor(50f, 150f, Color.White);
            SpatialManager manager = UniverseScreen.Spatial;
            DrawString($"Spatial.Type: {manager.Name}");
            DrawString($"Spatial.Collisions: {manager.Collisions}");
            DrawString($"Spatial.ActiveObjects: {manager.Count}");
            manager.DebugVisualize(Screen);
        }

        void InputDebug()
        {
            DrawString($"Mouse Moved {Screen.Input.MouseMoved}");

            DrawString($"RightHold Held  {Screen.Input.RightHold.IsHolding}");
            DrawString($"RightHold Time  {Screen.Input.RightHold.Time}");
            DrawString($"RightHold Start {Screen.Input.RightHold.StartPos}");
            DrawString($"RightHold End   {Screen.Input.RightHold.EndPos}");

            DrawString($"LeftHold Held   {Screen.Input.LeftHold.IsHolding}");
            DrawString($"LeftHold Time   {Screen.Input.LeftHold.Time}");
            DrawString($"LeftHold Start  {Screen.Input.LeftHold.StartPos}");
            DrawString($"LeftHold End    {Screen.Input.LeftHold.EndPos}");
        }



        public static void DefenseCoLogsNull(bool found, Ship ship, SolarSystem systemToDefend)
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

        public static void DefenseCoLogsMultipleSystems(Ship ship)
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: $"SystemCommander: Remove : Ship Was in Multiple SystemCommanders: {ship}");
        }
        public static void DefenseCoLogsNotInSystem()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Not in SystemCommander");
        }

        public static void DefenseCoLogsNotInPool()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : Not in DefensePool");
        }
        public static void DefenseCoLogsSystemNull()
        {
            if (Mode != DebugModes.DefenseCo) return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : SystemToDefend Was Null");
        }
    }
}
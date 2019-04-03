using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Debug.Page;
using Ship_Game.GameScreens.Sandbox;
using static Ship_Game.AI.ShipAI;

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
        Last // dummy value
    }


    public sealed partial class DebugInfoScreen : GameScreen
    {
        public bool IsOpen = true;
        readonly UniverseScreen Screen;
        Rectangle Win = new Rectangle(30, 200, 1200, 700);
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
                    if (ship.DesignRole < ShipData.RoleName.troopShip) continue;
                    if (empire.GetForcePool().Contains(ship)) continue;

                    foreach (AO ao in empire.GetEmpireAI().AreasOfOperations)
                    {
                        if (ao.GetOffensiveForcePool().Contains(ship) || ao.GetWaitingShips().Contains(ship) || ao.GetCoreFleet() == ship.fleet)
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
                if      (Mode > DebugModes.Last)   Mode = DebugModes.Normal;
                else if (Mode < DebugModes.Normal) Mode = DebugModes.Last - 1;
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(float deltaTime)
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
                }
            }

            UpdateDebugShips(deltaTime);
            base.Update(deltaTime);
        }

        void UpdateDebugShips(float deltaTime)
        {
            if (DebugPlatformSpeed == null) // platform is only enabled in sandbox universe
                return;
            float platformSpeed = DebugPlatformSpeed.AbsoluteValue;
            float speedLimiter = SpeedLimitSlider.RelativeValue;

            if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;
                ship.Speed = speedLimiter * ship.velocityMaximum;
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
        
        public void Draw(GameTime gameTime)
        {
            Page?.Draw(Screen.ScreenManager.SpriteBatch);
            
            try
            {
                TextFont = Fonts.Arial20Bold;
                SetTextCursor(50f, 50f, Color.Red);

                DrawString(Color.Yellow, Mode.ToString());
                DrawString("Ships Died:   " + ShipsDied);
                DrawString("Proj Died:    " + ProjDied);
                DrawString("Proj Created: " + ProjCreated);
                DrawString("Mods Created: " + ModulesCreated);
                DrawString("Mods Died:    " + ModulesDied);

                TextCursor.Y -= (float)(Fonts.Arial20Bold.LineSpacing + 2) * 4;
                TextCursor.X += Fonts.Arial20Bold.TextWidth("XXXXXXXXXXXXXXXXXXXX");
                DrawString("LastMTaskCanceled: " + CanceledMTaskName);

                DrawString(CanceledMTask1Name + ": " + CanceledMtask1Count);
                DrawString(CanceledMTask2Name + ": " + CanceledMtask2Count);
                DrawString(CanceledMTask3Name + ": " + CanceledMtask3Count);
                DrawString(CanceledMTask4Name + ": " + CanceledMtask4Count);

                DrawString($"Ships not in Any Pool: {ShipsNotInForcePool} In Defenspool: {ShipsInDefForcePool} InAoPools: {ShipsInAoPool} ");
                DrawDebugPrimitives((float)gameTime.ElapsedGameTime.TotalSeconds);
                TextFont = Fonts.Arial12Bold;
                switch (Mode)
                {
                    case DebugModes.Normal        : EmpireInfo(); break;
                    case DebugModes.DefenseCo     : DefcoInfo(); break;
                    case DebugModes.ThreatMatrix  : ThreatMatrixInfo(); break;
                    case DebugModes.Targeting     : Targeting(); break;
                    case DebugModes.SpatialManager: SpatialManagement(); break;
                    case DebugModes.input         : InputDebug(); break;
                    case DebugModes.Tech          : Tech(); break;
                }
                base.Draw(ScreenManager.SpriteBatch);
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

                if (!string.IsNullOrEmpty(e.ResearchTopic))
                {
                    DrawString($"Research: {e.CurrentResearch.Progress:0}/{e.CurrentResearch.TechCost:0} ({e.GetProjectedResearchNextTurn().String()} / {e.MaxResearchPotential.String()})");
                    DrawString("   --" + e.ResearchTopic);
                    Ship bestShip = e.GetEmpireAI().TechChooser.LineFocus.BestCombatShip;
                    if (bestShip != null)
                    {
                        DrawString($"Ship : {bestShip.Name}");
                        DrawString($"Hull : {bestShip.BaseHull.Role}");                        
                        DrawString($"Role : {bestShip.DesignRole}");
                        DrawString($"Str : {(int)bestShip.BaseStrength} - Tech : {bestShip.shipData.TechScore}");
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
            for (int i = 0; i < Screen.MasterShipList.Count; ++i)
            {
                Ship ship = Screen.MasterShipList[i];
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
                        weapon.ProjectedImpactPointNoError(module, out Vector2 impactNoError);
                        Screen.DrawLineProjected(weapon.Center, weapon.DebugLastImpactPredict, Color.Yellow);

                        Screen.DrawCircleProjected(impactNoError, 22f, 10, Color.BlueViolet, 2f);
                        Screen.DrawStringProjected(impactNoError, 28f, Color.BlueViolet, "pip");
                        Screen.DrawLineProjected(impactNoError, weapon.DebugLastImpactPredict, Color.DarkKhaki, 2f);
                    }

                    Projectile projectile = ship.CopyProjectiles().FirstOrDefault(p => p.Weapon == weapon);
                    if (projectile != null)
                    {
                        Screen.DrawLineProjected(projectile.Center, projectile.Center + projectile.Velocity, Color.Red);
                    }
                    break;
                }
            }
        }

        void ShipInfo()
        {
            SetTextCursor(Win.X + 10, 500f, Color.White);

            if (Screen.SelectedFleet != null)
            {
                Fleet fleet = Screen.SelectedFleet;
                DrawArrowImm(fleet.Position, fleet.Position+fleet.Direction*200f, Color.OrangeRed);
                foreach (Ship ship in fleet.Ships)
                    VisualizeShipGoal(ship, false);

                if (fleet.FleetTask != null)
                {
                    DrawString(fleet.FleetTask.type.ToString());

                    if (fleet.FleetTask.TargetPlanet != null)
                        DrawString(fleet.FleetTask.TargetPlanet.Name);

                    DrawString("Step: "+fleet.TaskStep);
                }
                else
                {
                    // @todo DrawLines similar to UniverseScreen.DrawLines. This code should be refactored
                    DrawString("Core fleet :" + fleet.IsCoreFleet);
                    DrawString(fleet.Name);
                    DrawString("Ships: " + fleet.Ships.Count);
                    DrawString("Strength: " + fleet.GetStrength());

                    string shipAI = fleet.Ships?.FirstOrDefault()?.AI.State.ToString() ?? "";
                    DrawString("Ship State: " + shipAI);
                }
            }
            else if (Screen.CurrentGroup != null)
            {
                ShipGroup group = Screen.CurrentGroup;
                DrawArrowImm(group.Position, group.Position+group.Direction*200f, Color.OrangeRed);
                foreach (Ship ship in group.Ships)
                    VisualizeShipGoal(ship, false);

                DrawString($"ShipGroup ({group.CountShips})  x {(int)group.Position.X} y {(int)group.Position.Y}");

                if (group.GoalMovePosition.NotZero())
                {
                    DrawLineImm(group.Position, group.GoalMovePosition, Color.YellowGreen);
                }
            }
            else if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;

                DrawString($"Ship {Screen.SelectedShip.ShipName}  x {(int)ship.Center.X} y {(int)ship.Center.Y}");
                DrawString($"Ship velocity: {ship.Velocity.Length()}");
                VisualizeShipOrderQueue(ship);

                DrawString($"On Defense: {ship.DoingSystemDefense}");
                if (ship.fleet != null)
                {
                    DrawString($"Fleet {ship.fleet.Name}  {(int)ship.fleet.Position.X}x{(int)ship.fleet.Position.Y}");
                    DrawString($"Fleet speed: {ship.fleet.Speed}");
                }

                DrawString(!Screen.SelectedShip.loyalty.GetForcePool().Contains(Screen.SelectedShip)
                           ? "NOT In Force Pool" : "In Force Pool");

                if (Screen.SelectedShip.AI.State == AIState.SystemDefender)
                {
                    SolarSystem systemToDefend = Screen.SelectedShip.AI.SystemToDefend;
                    DrawString($"Defending {systemToDefend?.Name ?? "Awaiting Order"}");
                }

                DrawString(ship.System == null ? "Deep Space" : $"{ship.System.Name} system");

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
                    SetTextCursor(Win.X + 150, 600f, Color.White);
                    DrawString("Target: "+ shipTarget.Name);
                    DrawString(shipTarget.Active ? "Active" : "Error - Active");
                }
                DrawString($"Strength: {ship.BaseStrength}");
                DrawString($"Max Velocity {ship.velocityMaximum}");
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

                DrawString($"SelectedShips ({ships.Count}) ");
            }
            VisualizePredictionDebugger();
        }

        IEnumerable<PredictionDebugPlatform> GetPredictionDebugPlatforms()
        {
            for (int i = 0; i < Screen.MasterShipList.Count; ++i)
                if (Screen.MasterShipList[i] is PredictionDebugPlatform platform)
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
                Vector2[] wayPoints = ship.AI.CopyWayPoints();
                for (int i = 1; i < wayPoints.Length; ++i) // draw waypoints chain
                    DrawLineImm(wayPoints[i-1], wayPoints[i], Color.ForestGreen);
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
                Vector2[] wayPoints = ship.AI.CopyWayPoints();
                DrawString($"WayPoints ({wayPoints.Length}):");
                for (int i = 0; i < wayPoints.Length; ++i)
                    DrawString($"  {i}:  {wayPoints[i]}");
            }
        }

        void EmpireInfo()
        {
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
                DrawString($"Money: {e.Money.String()} A:({e.GetActualNetLastTurn().String()}) T:({e.GrossIncome.String()})");
                float taxRate = e.data.TaxRate * 100f;
                DrawString("Tax Rate:      "+taxRate.ToString("#.0")+"%");
                DrawString("Ship Maint:    "+e.TotalShipMaintenance);

                Array<Ship> ships = e.GetShips();
                DrawString($"Ship Count:    {ships.Count}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.platform || warship?.DesignRole == ShipData.RoleName.station)}" +
                           $" :{ships.Count(warship=> warship?.DesignRole ==  ShipData.RoleName.fighter || warship?.DesignRole == ShipData.RoleName.corvette)}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.frigate)}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.cruiser )}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.capital)}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.carrier)}" +
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.bomber)}"
                           );
                DrawString("Build Maint:   "+e.TotalBuildingMaintenance);
                DrawString("Spy Count:     "+e.data.AgentList.Count);
                DrawString("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
                DrawString("Planet Count:  "+e.GetPlanets().Count);
                if (!string.IsNullOrEmpty(e.ResearchTopic))
                {
                    DrawString($"Research: {e.CurrentResearch.Progress:0}/{e.CurrentResearch.TechCost:0}({e.GetProjectedResearchNextTurn().String()})");
                    DrawString("   --"+e.ResearchTopic);
                }

                NewLine(3);
                DrawString("Total Pop: "+ e.GetTotalPop().String());
                DrawString("Gross Food: "+ e.GetGrossFoodPerTurn().String());
                DrawString("Military Str: "+ e.MilitaryScore);
                for (int x = 0; x < e.GetEmpireAI().Goals.Count; x++)
                {
                    Goal g = e.GetEmpireAI().Goals[x];
                    if (!(g is MarkForColonization))
                        continue;

                    NewLine();
                    string held = g.Held ? "(Held" : "";
                    DrawString($"{held}{g.UID} {g.ColonizationTarget.Name}");
                    DrawString(15f, $"Step: {g.StepName}");
                    if (g.FinishedShip != null && g.FinishedShip.Active)
                        DrawString(15f, "Has ship");
                }

                for (int j = 0; j < e.GetEmpireAI().TaskList.Count; j++)
                {
                    MilitaryTask task = e.GetEmpireAI().TaskList[j];
                    if (task == null)
                        continue;
                    string sysName = "Deep Space";
                    for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
                    {
                        SolarSystem sys = UniverseScreen.SolarSystemList[i];
                        if (task.AO.InRadius(sys.Position, sys.Radius))
                            sysName = sys.Name;
                    }
                    NewLine();
                    DrawString($"FleetTask: {task.type} ({sysName})");
                    DrawString(15f, "Step: " + task.Step);
                    DrawString(15f, "Str Needed: " + task.MinimumTaskForceStrength);
                    DrawString(15f, "Which Fleet: " + task.WhichFleet);
                }

                NewLine();
                foreach (KeyValuePair<Empire, Relationship> relationship in e.AllRelations)
                {
                    TextColor = relationship.Key.EmpireColor;
                    if (relationship.Value.Treaty_NAPact)
                        DrawString(15f, "NA Pact with "+ relationship.Key.data.Traits.Plural);

                    if (relationship.Value.Treaty_Trade)
                        DrawString(15f, "Trade Pact with "+ relationship.Key.data.Traits.Plural);

                    if (relationship.Value.Treaty_OpenBorders)
                        DrawString(15f, "Open Borders with "+ relationship.Key.data.Traits.Plural);

                    if (relationship.Value.AtWar)
                        DrawString(15f, $"War with {relationship.Key.data.Traits.Plural} ({relationship.Value.ActiveWar.WarType})");
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
                if (e.isPlayer || e.isFaction)
                    continue;
              
                foreach (ThreatMatrix.Pin pin in e.GetEmpireAI().ThreatMatrix.Pins.Values.ToArray())
                {
                    if (pin.Position == Vector2.Zero|| pin.Ship == null) continue;
                    Screen.DrawCircleProjected(pin.Position, 50f + pin.Ship.Radius, 6, e.EmpireColor);

                    if (!pin.InBorders) continue;
                    Screen.DrawCircleProjected(pin.Position, 50f + pin.Ship.Radius, 3, e.EmpireColor);
                }

            }
        }

        void SpatialManagement()
        {
            UniverseScreen.SpaceManager.DebugVisualize(Screen);
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
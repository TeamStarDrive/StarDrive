using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI.Tasks;
using static Ship_Game.AI.ShipAI;

namespace Ship_Game.Debug
{
    public enum DebugModes
    {
        Normal,
        Targeting,
        Pathing,
        DefenseCo,
        Trade,
        AO,
        ThreatMatrix,
        SpatialManager,
        Last, // dummy value
    }

    public sealed class DebugInfoScreen 
    {
        public bool IsOpen;
        private readonly ScreenManager ScreenManager;
        private readonly UniverseScreen Screen;
        private Rectangle Win;
        public static int ShipsDied;
        public static int ProjDied;
        public static int ProjCreated;
        public static int ModulesCreated;
        public static int ModulesDied;
        public static string CanceledMTaskName;
        public static int CanceledMtasksCount;
        public static int CanceledExplorationT;
        public static int Canceledclear;
        public static int CanceledCohesive;
        public static string OtherTask;
        public static string CanceledMTask1Name;
        public static string CanceledMTask2Name;
        public static string CanceledMTask3Name;
        public static string CanceledMTask4Name;
        public static int CanceledMtask1Count;
        public static int CanceledMtask2Count;
        public static int CanceledMtask3Count;
        public static int CanceledMtask4Count;
        private int Shipsnotinforcepool;
        private int ShipsinDefforcepool;
        public Ship ItemToBuild;
        private string Fmt = "0.#";
        public static sbyte Loadmodels = 0;
        private static DebugModes Mode;

        public DebugInfoScreen(ScreenManager screenManager, UniverseScreen screen)
        {
            this.IsOpen = true;
            this.Screen = screen;
            this.ScreenManager = screenManager;
            Win = new Rectangle(30, 200, 1200, 700);

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (empire == Empire.Universe.player || empire.isFaction || empire.MinorRace)
                    continue;
                bool flag = false;
                foreach (Ship ship in empire.GetShips())
                {
                    if (!empire.GetForcePool().Contains(ship))
                    {
                        foreach (AO ao in empire.GetGSAI().AreasOfOperations)
                            if (ao.GetOffensiveForcePool().Contains(ship) && ship?.shipData.Role != ShipData.RoleName.troop && ship?.BaseStrength > 0)
                                flag = true;
                        if (flag)
                            continue;

                        if (empire.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship) )
                            ++ShipsinDefforcepool;
                        else if (ship != null && (!ship.loyalty.GetForcePool().Contains(ship)))
                            ++Shipsnotinforcepool;
                    }
                }
            }
        }


        private Vector2 TextCursor  = Vector2.Zero;
        private Color   TextColor   = Color.White;
        private SpriteFont TextFont = Fonts.Arial12Bold;

        private void SetTextCursor(float x, float y, Color color)
        {
            TextCursor = new Vector2(x, y);
            TextColor  = color;
        }
        private void DrawString(string text)
        {
            ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, TextColor);
            NewLine(text.Count(c => c == '\n') + 1);
        }
        private void DrawString(float offsetX, string text)
        {
            Vector2 pos = TextCursor;
            pos.X += offsetX;
            ScreenManager.SpriteBatch.DrawString(TextFont, text, pos, TextColor);
            NewLine(text.Count(c => c == '\n') + 1);
        }
        private void DrawString(Color color, string text)
        {
            ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, color);
            NewLine(text.Count(c => c == '\n') + 1);
        }
        private void NewLine(int lines = 1) => TextCursor.Y += (TextFont == Fonts.Arial12Bold ? TextFont.LineSpacing : TextFont.LineSpacing+2) * lines;

        public void Draw(GameTime gameTime)
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
            TextCursor.X += Fonts.Arial20Bold.MeasureString("XXXXXXXXXXXXXXXXXXXX").X;
            DrawString("LastMTaskCanceled: "+ CanceledMTaskName);

            DrawString(CanceledMTask1Name + ": " + CanceledMtask1Count);
            DrawString(CanceledMTask2Name + ": " + CanceledMtask2Count);
            DrawString(CanceledMTask3Name + ": " + CanceledMtask3Count);
            DrawString(CanceledMTask4Name + ": " + CanceledMtask4Count);

            DrawString("Ships not in Any Pool: "+Shipsnotinforcepool+" In Defenspool: "+ShipsinDefforcepool);

            TextFont = Fonts.Arial12Bold;
            switch (Mode)
            {
                case DebugModes.Normal:       EmpireInfo();       break;
                case DebugModes.DefenseCo:    DefcoInfo();        break;
                case DebugModes.ThreatMatrix: ThreatMatrixInfo(); break;
                case DebugModes.Pathing:      PathingInfo();      break;
                case DebugModes.Trade:        TradeInfo();        break;
                case DebugModes.Targeting:    Targeting();        break;
                case DebugModes.SpatialManager: SpatialManagement(); break;
            }
            ShipInfo();
        }
        private void Targeting()
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

                    Vector2 impactNew = weapon.FindProjectedImpactPoint(ship.AI.Target);
                    Vector2 impactOld = weapon.Center.FindProjectedImpactPointOld(ship.Velocity,
                        weapon.ProjectileSpeed, module.Center, ship.AI.Target.Velocity);

                    Screen.DrawCircleProjected(module.Center, 8f, 6, Color.Pink);
    
                    Screen.DrawLineProjected(weapon.Center, impactNew, Color.Yellow);
                    Screen.DrawLineProjected(weapon.Center, impactOld, Color.LightYellow);

                    Projectile projectile = ship.Projectiles.FirstOrDefault(p => p.Weapon == weapon);
                    if (projectile != null)
                    {
                        Screen.DrawLineProjected(projectile.Center, projectile.Center + projectile.Velocity, Color.Red);
                    }
                    break;
                }
            }
        }
        private void ShipInfo()
        {
            if (Screen.SelectedFleet != null)
            {
                SetTextCursor(Win.X + 10, 600f, Color.White);
                if (Screen.SelectedFleet.FleetTask != null)
                {
                    DrawString(Screen.SelectedFleet.FleetTask.type.ToString());

                    if (Screen.SelectedFleet.FleetTask.GetTargetPlanet() != null)
                        DrawString(Screen.SelectedFleet.FleetTask.GetTargetPlanet().Name);

                    DrawString("Step: "+Screen.SelectedFleet.TaskStep);
                }
                else
                {
                    // @todo DrawLines similar to UniverseScreen.DrawLines. This code should be refactored
                    DrawString("core fleet :" + Screen.SelectedFleet.IsCoreFleet);
                    DrawString(Screen.SelectedFleet.Name);
                    DrawString("Ships: " + Screen.SelectedFleet.Ships.Count);
                    DrawString("Strength: " + Screen.SelectedFleet.GetStrength());

                    string shipAI = Screen.SelectedFleet.Ships?.FirstOrDefault()?.AI.State.ToString() ?? "";
                    DrawString("Ship State: " + shipAI);
                }
            }
            if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;
                SetTextCursor(Win.X + 10, 600f, Color.White);

                DrawString(Screen.SelectedShip.Name);
                DrawString(ship.Center.ToString());
                DrawString("On Defense: "+ship.DoingSystemDefense);
                if (ship.fleet != null)
                {
                    DrawString(ship.fleet.Name);
                    DrawString("Fleet pos: "+ship.fleet.Position);
                    DrawString("Fleet speed: "+ship.fleet.Speed);
                }
                DrawString("Ship speed: "+ship.Velocity.Length());
                DrawString(!Screen.SelectedShip.loyalty.GetForcePool().Contains(Screen.SelectedShip)
                    ? "NOT In Force Pool"
                    : "In Force Pool");
                if (Screen.SelectedShip.AI.State == AIState.SystemDefender)
                {
                    SolarSystem systemToDefend = Screen.SelectedShip.AI.SystemToDefend;
                    if (systemToDefend != null)
                        DrawString("Defending "+systemToDefend.Name);
                    else
                        DrawString("Defending Awaiting Order");
                }
                if (ship.System == null)
                {
                    DrawString("Deep Space");
                }
                else
                {
                    DrawString(ship.System.Name + " system");
                }
                DrawString(ship.InCombat ? Color.Green : Color.LightPink,
                    ship.InCombat ? ship.AI.BadGuysNear ? "InCombat" : "ERROR" : "Not in Combat");                
                DrawString(ship.AI.HasPriorityTarget ? "Priority Target" : "No Priority Target");
                DrawString(ship.AI.HasPriorityOrder ? "Priority Order" : "No Priority Order");
                DrawString("AI State: "+ship.AI.State);


                if (ship.AI.OrderQueue.IsEmpty)
                {
                    DrawString("Nothing in the Order queue");
                }
                else
                {
                    foreach (ShipGoal order in ship.AI.OrderQueue)
                    {
                        DrawString("Executing Order: "+order.Plan);
                    }
                }
                if (ship.AI.Target is Ship shipTarget)
                {
                    SetTextCursor(Win.X + 150, 600f, Color.White);
                    DrawString("Target: "+ shipTarget.Name);
                    DrawString(shipTarget.Active ? "Active" : "Error - Active");
                }
                DrawString("Strength: " + ship.BaseStrength);
                DrawString("HP: " + ship.Health + " / " + ship.HealthMax);

                SetTextCursor(Win.X + 250, 600f, Color.White);
                foreach (KeyValuePair<SolarSystem, SystemCommander> entry in ship.loyalty.GetGSAI().DefensiveCoordinator.DefenseDict)
                    foreach (var defender in entry.Value.ShipsDict) {
                        if (defender.Key == ship.guid)
                            DrawString(entry.Value.System.Name);
                    }
            }
        }
        private void EmpireInfo()
        {
            int column = 0;
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e.isFaction || e.MinorRace)
                    continue;

                SetTextCursor(Win.X + 10 + 255 * column, Win.Y + 10, e.EmpireColor);
                DrawString(e.data.Traits.Name);

                if (e.data.DiplomaticPersonality != null)
                {
                    DrawString(e.data.DiplomaticPersonality.Name);
                    DrawString(e.data.EconomicPersonality.Name);
                }
                DrawString($"Money: {e.Money.ToString(Fmt)} ({e.GetActualNetLastTurn()})");
                float taxRate = e.data.TaxRate * 100f;
                DrawString("Tax Rate:      "+taxRate.ToString("#.0")+"%");
                DrawString("Ship Maint:    "+e.GetTotalShipMaintenance());
                DrawString("Ship Count:    "+e.GetShips().Count);
                DrawString("Build Maint:   "+e.GetTotalBuildingMaintenance());
                DrawString("Spy Count:     "+e.data.AgentList.Count);
                DrawString("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
                DrawString("Planet Count:  "+e.GetPlanets().Count);
                if (!string.IsNullOrEmpty(e.ResearchTopic))
                {
                    float gamePaceStatic = UniverseScreen.GamePaceStatic * ResourceManager.TechTree[e.ResearchTopic].Cost;
                    DrawString($"Research: {e.GetTDict()[e.ResearchTopic].Progress:0}/{gamePaceStatic:0}({e.GetProjectedResearchNextTurn().ToString(Fmt)})");
                    DrawString("   --"+e.ResearchTopic);
                }

                NewLine(3);
                DrawString("Total Pop: "+ e.GetTotalPop().ToString(Fmt));
                DrawString("Gross Food: "+ e.GetGrossFoodPerTurn().ToString(Fmt));
                DrawString("Military Str: "+ e.MilitaryScore);
                for (int x = 0; x < e.GetGSAI().Goals.Count; x++)
                {
                    Goal g = e.GetGSAI().Goals[x];
                    if (g.GoalName != "MarkForColonization")
                        continue;

                    NewLine();
                    string held = g.Held ? "(Held" : "";
                    DrawString($"{held}{g.GoalName} {g.GetMarkedPlanet().Name}");
                    DrawString(15f, "Step: " + g.Step);
                    if (g.GetColonyShip() != null && g.GetColonyShip().Active)
                        DrawString(15f, "Has ship");
                }

                for (int j = 0; j < e.GetGSAI().TaskList.Count; j++)
                {
                    MilitaryTask task = e.GetGSAI().TaskList[j];
                    if (task == null)
                        continue;
                    string sysName = "Deep Space";
                    for (int i = 0; i < UniverseScreen.SolarSystemList.Count; i++)
                    {
                        SolarSystem sys = UniverseScreen.SolarSystemList[i];
                        if (task.AO.InRadius(sys.Position, 100000f))
                            sysName = sys.Name;
                    }
                    NewLine();
                    DrawString($"FleetTask: {task.type} ({sysName})");
                    DrawString(15f, "Step: " + task.Step);
                    DrawString(15f, "Str Needed: " + task.MinimumTaskForceStrength);
                    DrawString(15f, "Which Fleet: " + task.WhichFleet);
                }

                NewLine();
                foreach (var relationship in e.AllRelations)
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
        private void DefcoInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                DefensiveCoordinator defco = e.GetGSAI().DefensiveCoordinator;
                foreach (var kv in defco.DefenseDict)
                {                    
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.RankImportance * 100, e.EmpireColor, 6);
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.IdealShipStrength * 10, e.EmpireColor, 3);
                    Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.TroopsWanted * 100, e.EmpireColor, 4);
                }
                foreach(Ship ship in defco.DefensiveForcePool)                                                        
                    Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 6);
                
                foreach(AO ao in e.GetGSAI().AreasOfOperations)                
                    Screen.DrawCircleProjectedZ(ao.Center, ao.Radius, e.EmpireColor, 16);
                

            }
        }
        private void ThreatMatrixInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e.isPlayer || e.isFaction)
                    continue;
                foreach (ThreatMatrix.Pin pin in e.GetGSAI().ThreatMatrix.Pins.Values)
                {
                    if (pin.Position != Vector2.Zero)
                    {
                        Screen.DrawCircleProjectedZ(pin.Position, 50f, e.EmpireColor, 6);

                        if (pin.InBorders)
                        {
                            Screen.DrawCircleProjectedZ(pin.Position, 50f, e.EmpireColor, 3);                            
                        }
                    }
                }

            }
        }
        private void PathingInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
                for (int x = 0; x < e.grid.GetLength(0); x++)
                    for (int y = 0; y < e.grid.GetLength(1); y++)
                    {
                        if (e.grid[x, y] != 1)
                            continue;
                        var translated = new Vector2((x - e.granularity) * Screen.reducer, (y - e.granularity) * Screen.reducer);                        
                        Screen.DrawCircleProjectedZ(translated, Screen.reducer * .001f , e.EmpireColor, 4);
                    }
        }
        private void TradeInfo()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                foreach (Planet planet in e.GetPlanets())
                {                    
                    Screen.DrawCircleProjectedZ(planet.Center, planet.ExportFSWeight * 1000, e.EmpireColor, 6);
                    Screen.DrawCircleProjectedZ(planet.Center, planet.ExportPSWeight * 10, e.EmpireColor, 3);                    
                }

                foreach (Ship ship in e.GetShips())
                {
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;
                    
                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.FoodOrProd == "Food" ? Color.GreenYellow : Color.SteelBlue, 6);
                            break;
                        case Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.FoodOrProd == "Food" ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case Plan.PickupPassengers:
                        case Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }
                }   

            }
        }

        private void SpatialManagement()
        {
            UniverseScreen.SpaceManager.DebugVisualize(Screen);
            //foreach (SolarSystem system in Screen.SolarSystemDict.Values)
            //{
            //    system.spatialManager.DebugVisualize(Screen);
            //}
        }

        public bool HandleInput(InputState input)
        {
            if      (input.WasKeyPressed(Keys.Left))  --Mode;
            else if (input.WasKeyPressed(Keys.Right)) ++Mode;
            if      (Mode > DebugModes.Last)   Mode = DebugModes.Normal;
            else if (Mode < DebugModes.Normal) Mode = DebugModes.Last - 1;
            return false;
        }

        public static void DefenseCoLogsNull(bool found, Ship ship, SolarSystem systoDefend)
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            if (!found && ship.Active)
            {
                Log.Info(color: ConsoleColor.Yellow,
                    text:
                    systoDefend == null
                        ? "SystemCommander: Remove : SystemToDefend Was Null"
                        : "SystemCommander: Remove : Ship Not Found in Any");
            }
        }
        public static void DefenseCoLogsMultipleSystems()
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Ship Was in Multiple SystemCommanders");
        }
        public static void DefenseCoLogsNotInSystem()
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Not in SystemCommander");
        }

        public static void DefenseCoLogsNotInPool()
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : Not in DefensePool");
        }
        public static void DefenseCoLogsSystemNull()
        {
            if (Mode != DebugModes.DefenseCo)
                return;
            Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : SystemToDefend Was Null");
        }
    }
}
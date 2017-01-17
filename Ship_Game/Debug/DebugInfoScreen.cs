using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game;
using System.IO;
using Microsoft.Xna.Framework.Input;
using static Ship_Game.DrawRoutines;

namespace Ship_Game.Debug
{
    public enum DebugModes
    {
        Normal,
        DefenseCo,
        Pathing,
        ThreatMatrix
    }

    public sealed class DebugInfoScreen 
    {
        public bool IsOpen;
        private readonly ScreenManager ScreenManager;
        private readonly UniverseScreen Screen;
        private Rectangle Win;
        private Array<Checkbox> Checkboxes = new Array<Checkbox>();
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
        private int ShipsnotinDefforcepool;
        public Ship ItemToBuild;
        private string Fmt = "0.#";
        public static sbyte Loadmodels = 0;
        private static DebugModes Mode;
        static DebugInfoScreen() { }

        public DebugInfoScreen(ScreenManager screenManager, UniverseScreen screen)
        {
            this.Screen = screen;
            this.ScreenManager = screenManager;
            Win = new Rectangle(30, 200, 1200, 700);
            try
            {
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (empire == Empire.Universe.player || empire.isFaction || empire.MinorRace)
                        continue;
                    bool flag = false;
                    foreach (Ship ship in empire.GetShips())
                        if (!empire.GetForcePool().Contains(ship))
                        {
                            foreach (AO ao in empire.GetGSAI().AreasOfOperations)
                                if (ao.GetOffensiveForcePool().Contains(ship))
                                    if (ship?.shipData.Role != ShipData.RoleName.troop && ship?.BaseStrength > 0)

                                        flag = true;

                            if (flag) continue;
                            if (!empire.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                            {
                                if (ship.shipData.Role != ShipData.RoleName.troop && ship.BaseStrength > 0)
                                    ++ShipsnotinDefforcepool;
                            }
                            else
                            {
                                if (ship != null
                                    && (ship.shipData.Role != ShipData.RoleName.troop && ship.BaseStrength > 0))
                                    ++Shipsnotinforcepool;
                            }
                        }
                }
            }
            catch { }
        }


        public void Draw(GameTime gameTime)
        {
            Vector2 halloweenCursor = new Vector2(50f, 50f);
            HalloweenCursor(ref halloweenCursor, Mode.ToString(), Color.Yellow);
            HalloweenCursor(ref halloweenCursor, string.Concat("Ships Died: ", ShipsDied),Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat("Proj Died: ", ProjDied), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat("Proj Created: ", ProjCreated), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat("Mods Created: ", ModulesCreated), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat("Mods Died: ", ModulesDied), Color.Red);
    
            halloweenCursor.Y = halloweenCursor.Y - (float) (Fonts.Arial20Bold.LineSpacing + 2) * 4;
            halloweenCursor.X = halloweenCursor.X + Fonts.Arial20Bold.MeasureString("XXXXXXXXXXXXXXXXXXXX").X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                string.Concat("LastMTaskCanceled: ", CanceledMTaskName), halloweenCursor, Color.Red);
            halloweenCursor.Y = halloweenCursor.Y + (Fonts.Arial20Bold.LineSpacing + 2);

            HalloweenCursor(ref halloweenCursor, string.Concat(CanceledMTask1Name, ": ", CanceledMtask1Count), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat(CanceledMTask2Name, ": ", CanceledMtask2Count), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat(CanceledMTask3Name, ": ", CanceledMtask2Count), Color.Red);
            HalloweenCursor(ref halloweenCursor, string.Concat(CanceledMTask4Name, ": ", CanceledMtask2Count), Color.Red);
            

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                string.Concat("Ships not in forcepool: ", Shipsnotinforcepool, " Not in Defenspool: ",
                    ShipsnotinDefforcepool), halloweenCursor, Color.Red);


            Vector2 cursor;

            int column = 0;
            
            switch (Mode)
            {
                case DebugModes.Normal:
                    {

                        foreach (Empire e in EmpireManager.Empires)
                        {
                            if (e.isFaction || e.MinorRace)
                                continue;
                            cursor = new Vector2(Win.X + 10 + 225 * column, Win.Y + 10);
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.Traits.Name, cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            if (e.data.DiplomaticPersonality != null)
                            {
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.DiplomaticPersonality.Name,
                                    cursor, e.EmpireColor);
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.EconomicPersonality.Name,
                                    cursor, e.EmpireColor);
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            }
                            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                            SpriteFont arial12Bold = Fonts.Arial12Bold;
                            var str = new object[]
                                {"Money: ", e.Money.ToString(Fmt), " (", e.GetActualNetLastTurn(), ")"};
                            spriteBatch.DrawString(arial12Bold, string.Concat(str), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            float taxRate = e.data.TaxRate * 100f;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Tax Rate: ", taxRate.ToString("#.0"), "%"), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Ship Maint: ", e.GetTotalShipMaintenance()), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Ship Count: ", e.GetShips().Count), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Build Maint: ", e.GetTotalBuildingMaintenance()), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Spy Count: ", e.data.AgentList.Count()), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Spy Defenders: ",
                                    e.data.AgentList.Where(defenders => defenders.Mission == AgentMission.Defending).Count()),
                                cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Planet Count: ", e.GetPlanets().Count()), cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            if (!string.IsNullOrEmpty(e.ResearchTopic))
                            {
                                SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                                SpriteFont spriteFont = Fonts.Arial12Bold;
                                var strArrays = new[]
                                {
                        "Research: ", e.GetTDict()[e.ResearchTopic].Progress.ToString("0"), "/", null, null, null, null
                    };
                                float gamePaceStatic = UniverseScreen.GamePaceStatic *
                                                       ResourceManager.TechTree[e.ResearchTopic].Cost;
                                strArrays[3] = gamePaceStatic.ToString("0");
                                strArrays[4] = "(";
                                float projectedResearchNextTurn = e.GetProjectedResearchNextTurn();
                                strArrays[5] = projectedResearchNextTurn.ToString(Fmt);
                                strArrays[6] = ")";
                                spriteBatch1.DrawString(spriteFont, string.Concat(strArrays), cursor, e.EmpireColor);
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                    string.Concat("   --", e.ResearchTopic), cursor, e.EmpireColor);
                            }
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
                            SpriteFont arial12Bold1 = Fonts.Arial12Bold;
                            float totalPop = e.GetTotalPop();
                            spriteBatch2.DrawString(arial12Bold1, string.Concat("Total Pop: ", totalPop.ToString(Fmt)), cursor,
                                e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            SpriteBatch spriteBatch3 = ScreenManager.SpriteBatch;
                            SpriteFont spriteFont1 = Fonts.Arial12Bold;
                            float grossFoodPerTurn = e.GetGrossFoodPerTurn();
                            spriteBatch3.DrawString(spriteFont1, string.Concat("Gross Food: ", grossFoodPerTurn.ToString(Fmt)),
                                cursor, e.EmpireColor);
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                string.Concat("Military Str: ", e.MilitaryScore), cursor, e.EmpireColor);
                            foreach (Goal g in e.GetGSAI().Goals)
                            {
                                if (g.GoalName != "MarkForColonization")
                                    continue;
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                g.Held
                                    ? string.Concat("(Held)", g.GoalName, " ", g.GetMarkedPlanet().Name)
                                    : string.Concat(g.GoalName, " ", g.GetMarkedPlanet().Name), cursor, e.EmpireColor);
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Step: ", g.Step),
                                    cursor + new Vector2(15f, 0f), e.EmpireColor);
                                if (g.GetColonyShip() == null || !g.GetColonyShip().Active)
                                    continue;
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Has ship",
                                    cursor + new Vector2(15f, 0f), e.EmpireColor);
                            }

                            {
                                e.GetGSAI()
                                    .TaskList.ForEach(task =>
                                    {
                                        string sysName = "Deep Space";
                                        foreach (SolarSystem sys in UniverseScreen.SolarSystemList)
                                        {
                                            if (Vector2.Distance(task.AO, sys.Position) >= 100000f)
                                                continue;
                                            sysName = sys.Name;
                                        }
                                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                        SpriteBatch spriteBatch4 = ScreenManager.SpriteBatch;
                                        SpriteFont arial12Bold2 = Fonts.Arial12Bold;
                                        var str1 = new[] { "Task: ", task.type.ToString(), " (", sysName, ")" };
                                        spriteBatch4.DrawString(arial12Bold2, string.Concat(str1), cursor, e.EmpireColor);
                                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                            string.Concat("Step: ", task.Step),
                                            cursor + new Vector2(15f, 0f), e.EmpireColor);
                                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                            string.Concat("Str Needed: ", task.MinimumTaskForceStrength),
                                            cursor + new Vector2(15f, 0f),
                                            e.EmpireColor);
                                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                            string.Concat("Which Fleet: ", task.WhichFleet), cursor + new Vector2(15f, 0f),
                                            e.EmpireColor);
                                    }, false, false, false);
                            }
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            foreach (var relationship in e.AllRelations)
                            {
                                if (relationship.Value.Treaty_NAPact)
                                {
                                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                        string.Concat("NA Pact with ", relationship.Key.data.Traits.Plural),
                                        cursor + new Vector2(15f, 0f), relationship.Key.EmpireColor);
                                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                }
                                if (relationship.Value.Treaty_Trade)
                                {
                                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                        string.Concat("Trade Pact with ", relationship.Key.data.Traits.Plural),
                                        cursor + new Vector2(15f, 0f), relationship.Key.EmpireColor);
                                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                }
                                if (relationship.Value.Treaty_OpenBorders)
                                {
                                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                                        string.Concat("Open Borders with ", relationship.Key.data.Traits.Plural),
                                        cursor + new Vector2(15f, 0f), relationship.Key.EmpireColor);
                                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                                }
                                if (!relationship.Value.AtWar)
                                    continue;
                                SpriteBatch spriteBatch5 = ScreenManager.SpriteBatch;
                                SpriteFont spriteFont2 = Fonts.Arial12Bold;
                                var plural = new object[]
                                {
                        "War with ", relationship.Key.data.Traits.Plural, " (", relationship.Value.ActiveWar.WarType,
                        ")"
                                };
                                spriteBatch5.DrawString(spriteFont2, string.Concat(plural), cursor + new Vector2(15f, 0f),
                                    relationship.Key.EmpireColor);
                                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            }
                            column++;
                        }
                        break;
                    }
                case DebugModes.DefenseCo:
                {
                        foreach (Empire e in EmpireManager.Empires)
                        {
                            if (e.isPlayer || e.isFaction)
                                continue;
                            foreach (var kv in e.GetGSAI().DefensiveCoordinator.DefenseDict)
                            {


                                Circle circle = DrawSelectionCircles(kv.Value.System.Position, kv.Value.RankImportance *20000);
                                Primitives2D.DrawCircle(ScreenManager.SpriteBatch, circle.Center, circle.Radius, 6, e.EmpireColor);
                                //if (pin.InBorders)
                                //{
                                //    circle = DrawSelectionCircles(pin.Position, 50f);
                                //    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 3, e.EmpireColor);
                                //}

                            }

                        }
                    }
                    break;
                case DebugModes.ThreatMatrix:
                {
                        foreach (Empire e in EmpireManager.Empires)
                        {
                            if (e.isPlayer || e.isFaction)
                                continue;
                            foreach (ThreatMatrix.Pin pin in e.GetGSAI().ThreatMatrix.Pins.Values)
                            {
                                if (pin.Position != Vector2.Zero) // && pin.InBorders)
                                {
                                    Circle circle = DrawSelectionCircles(pin.Position, 50f);
                                    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 6, e.EmpireColor);
                                    if (pin.InBorders)
                                    {
                                        circle = DrawSelectionCircles(pin.Position, 50f);
                                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 3, e.EmpireColor);
                                    }
                                }
                            }

                        }
                        break;
                    }
                case DebugModes.Pathing:
                {
                        foreach (Empire e in EmpireManager.Empires)
                            for (int x = 0; x < e.grid.GetLength(0); x++)
                                for (int y = 0; y < e.grid.GetLength(1); y++)
                                {
                                    if (e.grid[x, y] != 1)
                                        continue;
                                    Vector2 translated = new Vector2((x - e.granularity) * Screen.reducer, (y - e.granularity) * Screen.reducer);
                                    Circle circle = DrawSelectionCircles(translated, Screen.reducer * .5f);
                                    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 4, e.EmpireColor);
                                }
                        break;
                }

                default:
                    break;
            }
            if (Screen.SelectedSystem != null)
            {
                cursor = new Vector2(Win.X + 10, 600f);
                foreach (Ship ship in Screen.SelectedSystem.ShipList)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        ship.Active ? ship.Name : string.Concat(ship.Name, " (inactive)"), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                }
                cursor = new Vector2(Win.X + 300, 600f);
                foreach (GameplayObject go in Screen.SelectedSystem.spatialManager.CollidableObjects)
                {
                    if (!(go is Ship))
                        continue;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat((go as Ship).Name, " "),
                        cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                }
            }
            if (Screen.SelectedFleet != null)
            {
                cursor = new Vector2(Win.X + 10, 600f);
                if (Screen.SelectedFleet.Task != null)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        Screen.SelectedFleet.Task.type.ToString(), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    if (Screen.SelectedFleet.Task.GetTargetPlanet() != null)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            Screen.SelectedFleet.Task.GetTargetPlanet().Name, cursor, Color.White);
                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    }
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        string.Concat("Step: ", Screen.SelectedFleet.TaskStep.ToString()), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                }
                else
                {
                    // @todo DrawLines similar to UniverseScreen.DrawLines. This code should be refactored
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        "core fleet :" + Screen.SelectedFleet.IsCoreFleet, cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Screen.SelectedFleet.Name, cursor,
                        Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        "Ships: " + Screen.SelectedFleet.Ships.Count, cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        "Strength: " + Screen.SelectedFleet.GetStrength(), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;

                    string shipAI = Screen.SelectedFleet.Ships.FirstOrDefault()?.GetAI().State.ToString() ?? "";
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Ship State: " + shipAI, cursor,
                        Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                }
            }
            if (Screen.SelectedShip != null)
            {
                Ship ship = Screen.SelectedShip;
                cursor = new Vector2(Win.X + 10, 600f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Screen.SelectedShip.Name, cursor,
                    Color.White);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Center.ToString(), cursor,
                    Color.White);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    string.Concat("On Defense: ", ship.DoingSystemDefense.ToString()), cursor, Color.White);
                if (ship.fleet != null)
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.fleet.Name, cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        string.Concat("Fleet pos: ", ship.fleet.Position.ToString()), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        string.Concat("Fleet speed: ", ship.fleet.speed), cursor, Color.White);
                }
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    string.Concat("Ship speed: ", ship.Velocity.Length()), cursor, Color.White);
                if (!Screen.SelectedShip.loyalty.GetForcePool().Contains(Screen.SelectedShip))
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "NOT In Force Pool", cursor,
                        Color.White);
                }
                else
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "In Force Pool", cursor, Color.White);
                }
                if (Screen.SelectedShip.GetAI().State == AIState.SystemDefender)
                {
                    SolarSystem systemToDefend = Screen.SelectedShip.GetAI().SystemToDefend;
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    if (systemToDefend != null)

                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            string.Concat("Defending ", systemToDefend.Name), cursor, Color.White);
                    else
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            string.Concat("Defending ", "Awaiting Order"), cursor, Color.White);
                }
                if (ship.System == null)
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Deep Space", cursor, Color.White);
                    lock (GlobalStats.DeepSpaceLock)
                    {
                        if (!UniverseScreen.DeepSpaceManager.CollidableObjects.Contains(ship))
                        {
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "ERROR-DS CO", cursor,
                                Color.LightPink);
                        }
                        else
                        {
                            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Manager OK", cursor,
                                Color.White);
                        }
                    }
                }
                else
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        string.Concat(ship.System.Name, " system"), cursor, Color.White);
                    if (!ship.System.spatialManager.CollidableObjects.Contains(ship))
                    {
                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "ERROR -SM CO", cursor,
                            Color.LightPink);
                    }
                    else
                    {
                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Manager OK", cursor, Color.White);
                    }
                }
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    ship.InCombat ? "InCombat" : "Not in Combat", cursor,
                    ship.InCombat ? Color.Green : Color.LightPink);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    ship.GetAI().hasPriorityTarget ? "Priority Target" : "No Priority Target", cursor, Color.White);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    ship.GetAI().HasPriorityOrder ? "Priority Order" : "No Priority Order", cursor, Color.White);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    string.Concat("AI State: ", ship.GetAI().State.ToString()), cursor, Color.White);
                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;

                if (ship.GetAI().OrderQueue.IsEmpty)
                {
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Nothing in the Order queue", cursor,
                        Color.White);
                }
                else
                {
                    foreach (ArtificialIntelligence.ShipGoal order in ship.GetAI().OrderQueue)
                    {
                        cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            string.Concat("Executing Order: ", order.Plan), cursor, Color.White);
                    }
                }
                if (ship.GetAI().Target != null)
                {
                    cursor = new Vector2(Win.X + 150, 600f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        string.Concat("Target: ", (ship.GetAI().Target as Ship).Name), cursor, Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        (ship.GetAI().Target as Ship).Active ? "Active" : "Error - Active", cursor, Color.White);
                }

                cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    "Strength: " + ship.BaseStrength, cursor, Color.White);
                cursor.Y =
                    cursor.Y + Fonts.Arial12Bold.LineSpacing; //Added by Gretman so I can test the health bug
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                    "HP: " + ship.Health + " / " + ship.HealthMax, cursor, Color.White);
                cursor = new Vector2(Win.X + 250, 600f);
                foreach (var entry in ship.loyalty.GetGSAI()
                    .DefensiveCoordinator.DefenseDict)
                foreach (var defender in entry.Value.ShipsDict)
                {
                    if (defender.Key != ship.guid)
                        continue;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, entry.Value.System.Name, cursor,
                        Color.White);
                    cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
                }
            }
        }

        private void HalloweenCursor( ref Vector2 halloweenCursor, string data,  Color color)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
               data , halloweenCursor, color);
            halloweenCursor.Y = halloweenCursor.Y + (Fonts.Arial20Bold.LineSpacing + 2);
            
        }

        public  bool  HandleInput(InputState input)
        {
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Left) && input.LastKeyboardState.IsKeyUp(Keys.Left))
                Mode--;
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.Right) && input.LastKeyboardState.IsKeyUp(Keys.Right))
                Mode++;
            if (Mode > DebugModes.ThreatMatrix || Mode < DebugModes.Normal)
                Mode = DebugModes.Normal;
            return false;
        }
    }
}
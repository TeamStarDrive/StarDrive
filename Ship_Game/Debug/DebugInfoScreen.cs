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
        Planets,
        AO,
        ThreatMatrix,
        SpatialManager,
        input,
        Tech,
        Last // dummy value
    }
    public struct DebugTextBlock
    {
        public Array<string> Lines;
        public string Header;
        public float HeaderSize;
        public Color HeaderColor;
        public string Footer;
        public float FooterSize;
        public Color FooterColor;
        public Array<Color> LineColor;

        public void AddRange(Array<string> lines)
        {            
            foreach (string line in lines)            
                AddLine(line);                            
        }

        public void AddRange(Array<string> lines, Color color)
        {
            foreach (string line in lines)
            {
                AddLine(line);
                LineColor.Add(color);
            }
        }

        public Array<string> GetFormattedLines()
        {
            var text = new Array<string>();
            if (Header.NotEmpty()) text.Add(Header);
            text.AddRange(Lines);
            if (Footer.NotEmpty()) text.Add(Footer);
            return text;
        }
        public void AddLine(string text) => AddLine(text, GetLastColor());        
        public void AddLine(string text, Color color)
        {
            Lines = Lines ?? new Array<string>();
            LineColor = LineColor ?? new Array<Color>();
            Lines.Add(text);
            LineColor.Add(color);
        }
        private Color GetLastColor()
        {
            if (LineColor?.IsEmpty ?? true) return Color.White;
            return LineColor.Last;
        }

    }

    
    

    public class PlanetData : DebugPage
    {
        private UniverseScreen Screen;
        private Rectangle DrawArea;
        public PlanetData(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Planets)
        {
            Screen = screen;
            DrawArea = parent.Rect;
        }

        public override void Update(float deltaTime, DebugModes mode)
        {
            Planet planet = Screen.SelectedPlanet;

            Array<DebugTextBlock> text;
            if (planet == null)
            {
                text = new Array<DebugTextBlock>();
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (empire.isFaction)
                        continue;
                    DebugTextBlock block = empire.DebugEmpirePlanetInfo();
                    block.Header = empire.Name;
                    block.HeaderColor = empire.EmpireColor;

                    text.Add(block);

                }
                for (int i = 0; i < text.Count; i++)
                {
                    ShowDebugGameInfo(i, text[i], Rect.X + 10 + 300 * i, Rect.Y + 250);
                }
                return;
            }
            HideAllDebugText();
            
            text = planet.DebugPlanetInfo();
            if (text == null)
                return;
            if (text?.IsEmpty == true) return;
            for (int i = 0; i < text.Count; i++)
            {
                DebugTextBlock lines = text[i];
                ShowDebugGameInfo(i, lines, Rect.X + 10 + 300 * i, Rect.Y + 250);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
            {
                base.Draw(spriteBatch);
                return;
            }
            Planet planet = Screen.SelectedPlanet;
            int totalFreighters = 0;
            foreach (Empire e in EmpireManager.Empires)
            {
                foreach (Ship ship in e.GetShips())
                {
                    if (ship?.Active != true) continue;
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;

                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 6);
                            if (planet == ship.AI.end) totalFreighters++;

                            break;
                        case Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case Plan.PickupPassengers:
                        case Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }
                }

            }
            base.Draw(spriteBatch);
        }
    }

    public sealed class DebugInfoScreen : GameScreen
    {
        public bool IsOpen;        
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
        private int ShipsInAOPool;
        private int WarShips;
        private int Freighters;
        private int UtilityShip;
        public Ship ItemToBuild;
        public static sbyte Loadmodels = 0;
        public static DebugModes Mode { get; private set; }
        private readonly Array<DebugPrimitive> Primitives = new Array<DebugPrimitive>();
        private Dictionary<string, Array<string>> ResearchText = new Dictionary<string, Array<string>>();        
        private DebugPage Page;

        public DebugInfoScreen(ScreenManager screenManager, UniverseScreen screen) : base(screen)
        {
            IsOpen = true;
            Screen = screen;
            ScreenManager = screenManager;
            Win = new Rectangle(30, 200, 1200, 700);            
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
                        if (ao.GetOffensiveForcePool().Contains(ship) || ao.GetWaitingShips().Contains(ship) || ao.GetCoreFleet() == ship.fleet)
                        {
                            ShipsInAOPool++;
                            flag = true;
                        }
                    if (flag)
                        continue;

                    if (empire.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship) )
                    {
                        ++ShipsinDefforcepool;
                        continue;
                    }

                   
                    ++Shipsnotinforcepool;

                }
            }
        }
   
        private Array<UILabel> DebugText;
        private void HideAllDebugGameInfo()
        {
            if (DebugText == null) return;
            for (int i = 0; i < DebugText.Count; i++)
            {
                var column = DebugText[i];
                column.Hide();
            }
        }
        private void HideDebugGameInfo(int column)
        {
            DebugText?[column].Hide();
        }

        private void ShowDebugGameInfo(int column, Array<string> lines, float x, float y)
        {
            if (DebugText == null)            
                DebugText = new Array<UILabel>();                
            
            if (DebugText.Count <= column)            
                DebugText.Add(Label(x, y, ""));
            

            DebugText[column].Show();
            DebugText[column].MultilineText = lines;
        
        }

        public bool DebugLogText(string text, DebugModes mode)
        {
            if (!IsOpen || mode != DebugModes.Last &&  Mode != mode || !GlobalStats.VerboseLogging) return false;
            Log.Info(text);
            return true;
        }

        public static void LogSelected(object selected, string text, DebugModes mode = DebugModes.Last)
        {     
            if (Empire.Universe.SelectedShip        != selected
                && Empire.Universe.SelectedPlanet    != selected
                && Empire.Universe.SelectedFleet    != selected
                && Empire.Universe.SelectedItem     != selected
                && Empire.Universe.SelectedSystem   != selected
                && Empire.Universe.SelectedShipList != selected
                )
                return;
            Empire.Universe?.DebugWin?.DebugLogText(text, mode);
        }

        public void ClearResearchLog(Empire empire)
        {
            if (!ResearchText.TryGetValue(empire.Name, out Array<string> empireTechs))
                return;
            
            empireTechs.Clear();
            
        }
        public void ResearchLog(string text, Empire empire)
        {
            if (!DebugLogText(text, DebugModes.Tech)) return;
            if (!ResearchText.TryGetValue(empire.Name, out Array<string> empireTechs))
            {
                var techs = new Array<string>();
                techs.Add(text);
                ResearchText.Add(empire.Name, techs);
            }
            else
            {
                empireTechs.Add(text);
            }
        }

        public void DebugWarningText(string text, DebugModes mode)
        {
            if (!IsOpen || Mode != mode || !GlobalStats.VerboseLogging) return;
            Log.Warning(text);
        }

        private Vector2 TextCursor  = Vector2.Zero;
        private Color   TextColor   = Color.White;
        private SpriteFont TextFont = Fonts.Arial12Bold;

        public override void PerformLayout()
        {
        }

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
        private void NewLine(int lines = 1)
            => TextCursor.Y += (TextFont == Fonts.Arial12Bold ? TextFont.LineSpacing : TextFont.LineSpacing+2) * lines;

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
                TextCursor.X += Fonts.Arial20Bold.MeasureString("XXXXXXXXXXXXXXXXXXXX").X;
                DrawString("LastMTaskCanceled: " + CanceledMTaskName);

                DrawString(CanceledMTask1Name + ": " + CanceledMtask1Count);
                DrawString(CanceledMTask2Name + ": " + CanceledMtask2Count);
                DrawString(CanceledMTask3Name + ": " + CanceledMtask3Count);
                DrawString(CanceledMTask4Name + ": " + CanceledMtask4Count);

                DrawString($"Ships not in Any Pool: {Shipsnotinforcepool} In Defenspool: {ShipsinDefforcepool} InAoPools: {ShipsInAOPool} ");
                DrawDebugPrimitives((float)gameTime.ElapsedGameTime.TotalSeconds);
                TextFont = Fonts.Arial12Bold;
                switch (Mode)
                {
                    case DebugModes.Normal        : EmpireInfo(); break;
                    case DebugModes.DefenseCo     : DefcoInfo(); break;
                    case DebugModes.ThreatMatrix  : ThreatMatrixInfo(); break;
                    //case DebugModes.Pathing       : PathingInfo(); break;
                    //case DebugModes.Trade         : TradeInfo(); break;
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

        public bool ValidatePage()
        {
            if (Page?.DebugMode == Mode)
            {
                Page.Show();
                return true;
            }
            switch (Mode)
            {
                case DebugModes.Normal:
                    break;
                case DebugModes.Targeting:
                    break;
                case DebugModes.Pathing:
                    Page = new PathDebug(Screen, this);
                    return true;
                case DebugModes.DefenseCo:
                    break;
                case DebugModes.Trade:
                    Page = new TradeDebug(Screen, this);
                    return true;
                case DebugModes.AO:
                    break;
                case DebugModes.ThreatMatrix:
                    break;
                case DebugModes.SpatialManager:
                    break;
                case DebugModes.input:
                    break;
                case DebugModes.Tech:
                    break;
                case DebugModes.Last:
                    break;
                case DebugModes.Planets:
                    Page = new PlanetData(Screen, this);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return false;
        }

        public override void Update(float deltaTime)
        {
            if (ValidatePage())
                Page?.Update(deltaTime, Mode);
            else Page?.Hide();
            
            base.Update(deltaTime);
        }
        private void Tech()
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
                    var techEntry = e.GetTechEntry(e.ResearchTopic);
                    float gamePaceStatic = techEntry.TechCost;
                    DrawString($"Research: {techEntry.Progress:0}/{gamePaceStatic:0} ({e.GetProjectedResearchNextTurn().String()} / {e.MaxResearchPotential.String()})");
                    DrawString("   --" + e.ResearchTopic);
                    Ship bestShip = e.GetEmpireAI().GetBestCombatShip;
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

                    Screen.DrawCircleProjected(module.Center, 8f, 6, Color.Pink);

                    if (weapon.ProjectedImpactPoint(ship.AI.Target, out Vector2 impactNew))
                        Screen.DrawLineProjected(weapon.Center, impactNew, Color.Yellow);


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

                    if (Screen.SelectedFleet.FleetTask.TargetPlanet != null)
                        DrawString(Screen.SelectedFleet.FleetTask.TargetPlanet.Name);

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
                DrawString("Combat State: " + ship.AI.CombatState);

                if (ship.AI.State == AIState.SystemTrader)
                    DrawString($"Trading Prod:{ship.TradingProd} food:{ship.TradingFood} Goods:{ship.AI.FoodOrProd}");
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
                DrawString("Max Velocity " + ship.velocityMaximum);
                DrawString("HP: " + ship.Health + " / " + ship.HealthMax);
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
        }

        private void EmpireInfo()
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
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.cruiser || warship?.DesignRole == ShipData.RoleName.frigate)}" +                           
                           $" :{ships.Count(warship => warship?.DesignRole == ShipData.RoleName.capital)}" +
                           $" :{ships.Count(warship => warship?.DesignRole >= ShipData.RoleName.bomber && warship?.DesignRole <= ShipData.RoleName.carrier)}"
                           );
                DrawString("Build Maint:   "+e.TotalBuildingMaintenance);
                DrawString("Spy Count:     "+e.data.AgentList.Count);
                DrawString("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
                DrawString("Planet Count:  "+e.GetPlanets().Count);
                if (!string.IsNullOrEmpty(e.ResearchTopic))
                {
                    float cost = ResourceManager.TechTree[e.ResearchTopic].ActualCost;
                    DrawString($"Research: {e.GetTDict()[e.ResearchTopic].Progress:0}/{cost:0}({e.GetProjectedResearchNextTurn().String()})");
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

        private void DefcoInfo()
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

        private void ThreatMatrixInfo()
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



        private void TradeInfo()
        {
            Planet planet = Screen.SelectedPlanet;
            foreach (Empire e in EmpireManager.Empires)
            {
                foreach (Ship ship in e.GetShips())
                {
                    if (ship?.Active != true) continue;
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;
                    
                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 6);
                            break;
                        case Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case Plan.PickupPassengers:
                        case Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }
                }   

            }
            if (planet?.Owner == null)
            {
                HideAllDebugGameInfo();
                return;
            }

            Array<DebugTextBlock> text = planet.TradeAI.DebugText();
            for (int i = 0; i < text.Count; i++)
            {
                ShowDebugGameInfo(i, text[i].Lines, Win.X + 10 + 400 * i, Win.Y + 20);  
            }
        }

        private void SpatialManagement()
        {
            UniverseScreen.SpaceManager.DebugVisualize(Screen);
        }

        private void InputDebug()
        {
            DrawString($"RightMouseHeld {Screen.Input.RightMouseHeld()}");

            DrawString($"Mouse Moved {Screen.Input.MouseMoved}");
            DrawString($"RightMouseWasHeld {Screen.Input.RightMouseWasHeld}");

            DrawString($"RightMouseTimer {Screen.Input.ReadRightMouseDownTime}");
            DrawString($"RightMouseHoldStartLocalation {Screen.Input.StartRighthold}");
            
        }

        public override bool HandleInput(InputState input)
        {
            Page?.HandleInput(input);
            if (!input.WasKeyPressed(Keys.Left) && !input.WasKeyPressed(Keys.Right))
                return false;
            ResearchText.Clear();
            HideAllDebugGameInfo();
            if (input.WasKeyPressed(Keys.Left)) --Mode;
            else                                ++Mode;

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

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, float lifeTime)
        {
            if (mode != Mode) return;
            lock (Primitives) Primitives.Add(new DebugCircle(worldPos, radius, Color.Yellow, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color, float lifeTime)
        {
            if (mode != Mode) return;
            lock (Primitives) Primitives.Add(new DebugCircle(worldPos, radius, color, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color)
        {
            if (mode != Mode) return;
            lock (Primitives) Primitives.Add(new DebugCircle(worldPos, radius, color, 0f));
        }

        public bool IgnoreThisShip(Ship ship)
        {
            return ship != null && Screen.SelectedShip != null && Screen.SelectedShip != ship;
        }

        public void DrawLine(DebugModes mode, Vector2 startInWorld, Vector2 endInWorld, 
                                              float width, Color color, float lifeTime)
        {
            if (mode != Mode) return;
            lock (Primitives) Primitives.Add(new DebugLine(startInWorld, endInWorld, width, color, lifeTime));
        }

        private void DrawDebugPrimitives(float gameDeltaTime)
        {
            lock (Primitives)
            for (int i = Primitives.Count-1; i >= 0; --i)
            {
                DebugPrimitive primitive = Primitives[i];
                primitive.Draw(Screen);
                if (!Screen.Paused && primitive.Update(gameDeltaTime))
                {
                    Primitives.RemoveAtSwapLast(i);
                }
            }
        }


        public void DrawGameObject(DebugModes mode, GameplayObject obj)
        {
            if (mode != Mode)
                return;

            if (obj.IsInFrustum)
            {
                lock (Primitives)
                    Primitives.Add(new DebugGameObject(obj, Color.Red, 0f/*transient*/));
            }
        }
    }
}
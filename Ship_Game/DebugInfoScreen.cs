using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
	public class DebugInfoScreen
	{
		public bool isOpen;

		private Ship_Game.ScreenManager ScreenManager;

		private UniverseScreen screen;

		private Rectangle win;

		private List<Checkbox> Checkboxes = new List<Checkbox>();

		public static int ShipsDied;

		public static int ProjDied;

		public static int ProjCreated;

		public static int ModulesCreated;

		public static int ModulesDied;
        public static string canceledMTaskName;
        public static int canceledMtasksCount;
        public static int canceledExplorationT;
        public static int canceledclear;
        public static int canceledCohesive;
        public static string OtherTask;

        public static string canceledMTask1Name;
        public static string canceledMTask2Name;
        public static string canceledMTask3Name;
        public static string canceledMTask4Name;
        public static int canceledMtask1Count;
        public static int canceledMtask2Count;
        public static int canceledMtask3Count;
        public static int canceledMtask4Count;

        private int shipsnotinforcepool;
        private int shipsnotinDefforcepool;

		public Ship itemToBuild;

		private string fmt = "0.#";

		static DebugInfoScreen()
		{
		}

		public DebugInfoScreen(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			this.win = new Rectangle(30, 200, 1200, 700);
            
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                if (empire == Empire.universeScreen.player || empire.isFaction)
                    continue;
                bool flag=false;
                foreach (Ship ship in empire.GetShips())
                {
                    if (!empire.GetForcePool().Contains(ship))
                    {

                        foreach (AO ao in empire.GetGSAI().AreasOfOperations)
                        {
                            if (ao.GetOffensiveForcePool().Contains(ship))
                                if (ship.Role != "troop" && ship.BaseStrength>0)

                                    flag = true;
                        }

                        if (!flag)

                            if (!empire.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                            {
                                if (ship.Role != "troop" && ship.BaseStrength > 0)
                                    ++this.shipsnotinDefforcepool;
                            }
                            else
                            {
                                if (ship.Role != "troop" && ship.BaseStrength > 0)
                                ++this.shipsnotinforcepool;
                            }

                    }


                }
            }
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public void Draw(GameTime gameTime)
		{
			Vector2 HalloweenCursor = new Vector2(50f, 50f);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Ships Died: ", DebugInfoScreen.ShipsDied), HalloweenCursor, Color.Red);
			HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Proj Died: ", DebugInfoScreen.ProjDied), HalloweenCursor, Color.Red);
			HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Proj Created: ", DebugInfoScreen.ProjCreated), HalloweenCursor, Color.Red);
			HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Mods Created: ", DebugInfoScreen.ModulesCreated), HalloweenCursor, Color.Red);
			HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Mods Died: ", DebugInfoScreen.ModulesDied), HalloweenCursor, Color.Red);
            HalloweenCursor.Y = HalloweenCursor.Y - (float)(Fonts.Arial20Bold.LineSpacing + 2)*4;
            HalloweenCursor.X = HalloweenCursor.X + (Fonts.Arial20Bold.MeasureString("XXXXXXXXXXXXXXXXXXXX").X);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("LastMTaskCanceled: ", DebugInfoScreen.canceledMTaskName), HalloweenCursor, Color.Red);
            HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
            
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(DebugInfoScreen.canceledMTask1Name,": ", DebugInfoScreen.canceledMtask1Count), HalloweenCursor, Color.Red);
            HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(DebugInfoScreen.canceledMTask2Name, ": ", DebugInfoScreen.canceledMtask2Count), HalloweenCursor, Color.Red);
            HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(DebugInfoScreen.canceledMTask3Name, ": ", DebugInfoScreen.canceledMtask3Count), HalloweenCursor, Color.Red);
            HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(DebugInfoScreen.canceledMTask4Name, ": ", DebugInfoScreen.canceledMtask4Count), HalloweenCursor, Color.Red);

            HalloweenCursor.Y = HalloweenCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);

            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Ships not in forcepool: ", this.shipsnotinforcepool," Not in Defenspool: ",this.shipsnotinDefforcepool), HalloweenCursor, Color.Red);
    
            
            Vector2 Cursor = new Vector2((float)(this.win.X + 10), (float)(this.win.Y + 10));

			int column = 0;
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.win, Color.Black);
			foreach (Empire e in EmpireManager.EmpireList)
			{
				if (e.isFaction)
				{
					continue;
				}
				Cursor = new Vector2((float)(this.win.X + 10 + 225 * column), (float)(this.win.Y + 10));
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.Traits.Name, Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				if (e.data.DiplomaticPersonality != null)
				{
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.DiplomaticPersonality.Name, Cursor, e.EmpireColor);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.data.EconomicPersonality.Name, Cursor, e.EmpireColor);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
				SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
				SpriteFont arial12Bold = Fonts.Arial12Bold;
				object[] str = new object[] { "Money: ", e.Money.ToString(this.fmt), " (", e.GetActualNetLastTurn(), ")" };
				spriteBatch.DrawString(arial12Bold, string.Concat(str), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				float taxRate = e.data.TaxRate * 100f;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Tax Rate: ", taxRate.ToString("#.0"), "%"), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Ship Maint: ", e.GetTotalShipMaintenance()), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Ship Count: ", e.GetShips().Count), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Build Maint: ", e.GetTotalBuildingMaintenance()), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Spy Count: ", e.data.AgentList.Count()), Cursor, e.EmpireColor);
                Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Spy Defenders: ", e.data.AgentList.Where(defenders => defenders.Mission == AgentMission.Defending).Count()), Cursor, e.EmpireColor);
                Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Planet Count: ", e.GetPlanets().Count()), Cursor, e.EmpireColor);
                Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				if (e.ResearchTopic != "")
				{
					SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
					SpriteFont spriteFont = Fonts.Arial12Bold;
					string[] strArrays = new string[] { "Research: ", e.GetTDict()[e.ResearchTopic].Progress.ToString("0"), "/", null, null, null, null };
					float gamePaceStatic = UniverseScreen.GamePaceStatic * ResourceManager.TechTree[e.ResearchTopic].Cost;
					strArrays[3] = gamePaceStatic.ToString("0");
					strArrays[4] = "(";
					float projectedResearchNextTurn = e.GetProjectedResearchNextTurn();
					strArrays[5] = projectedResearchNextTurn.ToString(this.fmt);
					strArrays[6] = ")";
					spriteBatch1.DrawString(spriteFont, string.Concat(strArrays), Cursor, e.EmpireColor);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("   --", e.ResearchTopic), Cursor, e.EmpireColor);
				}
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				SpriteBatch spriteBatch2 = this.ScreenManager.SpriteBatch;
				SpriteFont arial12Bold1 = Fonts.Arial12Bold;
				float totalPop = e.GetTotalPop();
				spriteBatch2.DrawString(arial12Bold1, string.Concat("Total Pop: ", totalPop.ToString(this.fmt)), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				SpriteBatch spriteBatch3 = this.ScreenManager.SpriteBatch;
				SpriteFont spriteFont1 = Fonts.Arial12Bold;
				float grossFoodPerTurn = e.GetGrossFoodPerTurn();
				spriteBatch3.DrawString(spriteFont1, string.Concat("Gross Food: ", grossFoodPerTurn.ToString(this.fmt)), Cursor, e.EmpireColor);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Military Str: ", e.MilitaryScore), Cursor, e.EmpireColor);
				foreach (Goal g in e.GetGSAI().Goals)
				{
					if (g.GoalName != "MarkForColonization")
					{
						continue;
					}
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (g.Held ? string.Concat("(Held)", g.GoalName, " ", g.GetMarkedPlanet().Name) : string.Concat(g.GoalName, " ", g.GetMarkedPlanet().Name)), Cursor, e.EmpireColor);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Step: ", g.Step), Cursor + new Vector2(15f, 0f), e.EmpireColor);
					if (g.GetColonyShip() == null || !g.GetColonyShip().Active)
					{
						continue;
					}
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Has ship", Cursor + new Vector2(15f, 0f), e.EmpireColor);
				}
				lock (GlobalStats.TaskLocker)
				{
					foreach (MilitaryTask task in e.GetGSAI().TaskList)
					{
						string sysName = "Deep Space";
						foreach (SolarSystem sys in UniverseScreen.SolarSystemList)
						{
							if (Vector2.Distance(task.AO, sys.Position) >= 100000f)
							{
								continue;
							}
							sysName = sys.Name;
						}
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						SpriteBatch spriteBatch4 = this.ScreenManager.SpriteBatch;
						SpriteFont arial12Bold2 = Fonts.Arial12Bold;
						string[] str1 = new string[] { "Task: ", task.type.ToString(), " (", sysName, ")" };
						spriteBatch4.DrawString(arial12Bold2, string.Concat(str1), Cursor, e.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Step: ", task.Step), Cursor + new Vector2(15f, 0f), e.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Str Needed: ", task.MinimumTaskForceStrength), Cursor + new Vector2(15f, 0f), e.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Which Fleet: ", task.WhichFleet), Cursor + new Vector2(15f, 0f), e.EmpireColor);
					}
				}
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in e.GetRelations())
				{
					if (Relationship.Value.Treaty_NAPact)
					{
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("NA Pact with ", Relationship.Key.data.Traits.Plural), Cursor + new Vector2(15f, 0f), Relationship.Key.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (Relationship.Value.Treaty_Trade)
					{
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Trade Pact with ", Relationship.Key.data.Traits.Plural), Cursor + new Vector2(15f, 0f), Relationship.Key.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (Relationship.Value.Treaty_OpenBorders)
					{
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Open Borders with ", Relationship.Key.data.Traits.Plural), Cursor + new Vector2(15f, 0f), Relationship.Key.EmpireColor);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (!Relationship.Value.AtWar)
					{
						continue;
					}
					SpriteBatch spriteBatch5 = this.ScreenManager.SpriteBatch;
					SpriteFont spriteFont2 = Fonts.Arial12Bold;
					object[] plural = new object[] { "War with ", Relationship.Key.data.Traits.Plural, " (", Relationship.Value.ActiveWar.WarType, ")" };
					spriteBatch5.DrawString(spriteFont2, string.Concat(plural), Cursor + new Vector2(15f, 0f), Relationship.Key.EmpireColor);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
				column++;
			}
			if (this.screen.SelectedSystem != null)
			{
				Cursor = new Vector2((float)(this.win.X + 10), 600f);
				foreach (Ship ship in this.screen.SelectedSystem.ShipList)
				{
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (ship.Active ? ship.Name : string.Concat(ship.Name, " (inactive)")), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
				Cursor = new Vector2((float)(this.win.X + 300), 600f);
				foreach (GameplayObject go in this.screen.SelectedSystem.spatialManager.CollidableObjects)
				{
					if (!(go is Ship))
					{
						continue;
					}
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat((go as Ship).Name, " "), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
			}
			if (this.screen.SelectedFleet != null)
			{
				Cursor = new Vector2((float)(this.win.X + 10), 600f);
				if (this.screen.SelectedFleet.Task != null)
				{
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.screen.SelectedFleet.Task.type.ToString(), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (this.screen.SelectedFleet.Task.GetTargetPlanet() != null)
					{
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.screen.SelectedFleet.Task.GetTargetPlanet().Name, Cursor, Color.White);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Step: ", this.screen.SelectedFleet.TaskStep.ToString()), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
			}
			if (this.screen.SelectedShip != null)
			{
				Ship ship = this.screen.SelectedShip;
				Cursor = new Vector2((float)(this.win.X + 10), 600f);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.screen.SelectedShip.Name, Cursor, Color.White);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Center.ToString(), Cursor, Color.White);
				if (ship.fleet != null)
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.fleet.Name, Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Fleet pos: ", ship.fleet.Position.ToString()), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Fleet speed: ", ship.fleet.speed), Cursor, Color.White);
				}
				if (!this.screen.SelectedShip.loyalty.GetForcePool().Contains(this.screen.SelectedShip))
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "NOT In Force Pool", Cursor, Color.White);
				}
				else
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "In Force Pool", Cursor, Color.White);
				}
				if (this.screen.SelectedShip.GetAI().State == AIState.SystemDefender)
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Defending ", this.screen.SelectedShip.GetAI().SystemToDefend.Name), Cursor, Color.White);
				}
				if (ship.GetSystem() == null)
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Deep Space", Cursor, Color.White);
					lock (GlobalStats.DeepSpaceLock)
					{
						if (!UniverseScreen.DeepSpaceManager.CollidableObjects.Contains(ship))
						{
							Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "ERROR", Cursor, Color.LightPink);
						}
						else
						{
							Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Manager OK", Cursor, Color.White);
						}
					}
				}
				else
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(ship.GetSystem().Name, " system"), Cursor, Color.White);
					if (!ship.GetSystem().spatialManager.CollidableObjects.Contains(ship))
					{
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "ERROR", Cursor, Color.LightPink);
					}
					else
					{
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Manager OK", Cursor, Color.White);
					}
				}
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (ship.InCombat ? "InCombat" : "Not in Combat"), Cursor, (ship.InCombat ? Color.Green : Color.LightPink));
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (ship.GetAI().hasPriorityTarget ? "Priority Target" : "No Priority Target"), Cursor, Color.White);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (ship.GetAI().HasPriorityOrder ? "Priority Order" : "No Priority Order"), Cursor, Color.White);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("AI State: ", ship.GetAI().State.ToString()), Cursor, Color.White);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				if (ship.GetAI().OrderQueue.Count <= 0)
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Nothing in the Order queue", Cursor, Color.White);
				}
				else
				{
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Executing Order: ", ship.GetAI().OrderQueue.First.Value.Plan), Cursor, Color.White);
				}
				if (ship.GetAI().Target != null)
				{
					Cursor = new Vector2((float)(this.win.X + 150), 600f);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Target: ", (ship.GetAI().Target as Ship).Name), Cursor, Color.White);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((ship.GetAI().Target as Ship).Active ? "Active" : "Error"), Cursor, Color.White);
				}
				Cursor = new Vector2((float)(this.win.X + 250), 600f);
				foreach (KeyValuePair<SolarSystem, SystemCommander> entry in ship.loyalty.GetGSAI().DefensiveCoordinator.DefenseDict)
				{
					foreach (KeyValuePair<Guid, Ship> defender in entry.Value.ShipsDict)
					{
						if (defender.Key != ship.guid)
						{
							continue;
						}
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, entry.Value.system.Name, Cursor, Color.White);
						Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
				}
			}
		}

		~DebugInfoScreen()
		{
			this.Dispose(false);
		}

		public bool HandleInput(InputState input)
		{
			return false;
		}
	}
}
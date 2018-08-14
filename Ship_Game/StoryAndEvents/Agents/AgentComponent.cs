using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class AgentComponent
	{
		public Agent SelectedAgent;

		public Rectangle ComponentRect;

		public Rectangle SubRect;

		public Rectangle OpsSubRect;

		public ScrollList AgentSL;

		public ScrollList OpsSL;

		private ScreenManager ScreenManager;

		public DanButton RecruitButton;

        public EspionageScreen Escreen;

		private MissionEntry Training;

		private MissionEntry Infiltrate;

		private MissionEntry Assassinate;

		private MissionEntry Sabotage;

		private MissionEntry StealTech;

		private MissionEntry StealShip;

		private MissionEntry InciteRebellion;

		private Selector selector;
        private int spyLimitCount;
        private bool AutoTrain;
        private UICheckBox CBAutoRepeat;
        private bool SpyMute;
        private UICheckBox cbSpyMute;
        private int empirePlanetSpys;


        public AgentComponent(Rectangle r, EspionageScreen Escreen)
        {
            SpyMute = EmpireManager.Player.data.SpyMute;
            AutoTrain = EmpireManager.Player.data.SpyMissionRepeat;
            this.Escreen = Escreen;
            ComponentRect = r;
            ScreenManager = Empire.Universe.ScreenManager;
            SubRect = new Rectangle(ComponentRect.X, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            OpsSubRect = new Rectangle(Escreen.OperationsRect.X + 20, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            Submenu sub = new Submenu(ComponentRect);
            AgentSL = new ScrollList(sub, 40);
            foreach (Agent agent in EmpireManager.Player.data.AgentList)
            {
                AgentSL.AddItem(agent);
            }
            Rectangle c = ComponentRect;
            c.X = OpsSubRect.X;
            Submenu opssub = new Submenu(c);
            OpsSL = new ScrollList(opssub, 30);
            Training = new MissionEntry(AgentMission.Training, this);
            Infiltrate = new MissionEntry(AgentMission.Infiltrate, this);
            Assassinate = new MissionEntry(AgentMission.Assassinate, this);
            Sabotage = new MissionEntry(AgentMission.Sabotage, this);
            StealTech = new MissionEntry(AgentMission.StealTech, this);
            StealShip = new MissionEntry(AgentMission.Robbery, this);
            InciteRebellion = new MissionEntry(AgentMission.InciteRebellion, this);
            OpsSL.AddItem(Training);
            OpsSL.AddItem(Infiltrate);
            OpsSL.AddItem(Assassinate);
            OpsSL.AddItem(Sabotage);
            OpsSL.AddItem(StealTech);
            OpsSL.AddItem(StealShip);
            OpsSL.AddItem(InciteRebellion);
            RecruitButton = new DanButton(new Vector2((float)(ComponentRect.X), (float)(ComponentRect.Y + ComponentRect.Height + 5f)), Localizer.Token(2179))
            {
                Toggled = true
            };
        }

        //added by gremlin deveksmod spy draw
        public void Draw(SpriteBatch batch)
        {
            batch.FillRectangle(SubRect, Color.Black);
            AgentSL.Draw(batch);
            RecruitButton.Draw(ScreenManager);
            Rectangle moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            batch.Draw(ResourceManager.Texture("NewUI/icon_money"), moneyRect, Color.White);

            Vector2 costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            batch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            // @todo Why are we creating new checkboxes every frame??
            CBAutoRepeat = new UICheckBox(null, OpsSubRect.X - 10, moneyRect.Y - 30, () => AutoTrain, Fonts.Arial12, "Repeat Missions", 0);
            cbSpyMute    = new UICheckBox(null, OpsSubRect.X - 10, moneyRect.Y - 15, () => SpyMute,   Fonts.Arial12, "Mute Spies", 0);

            EmpireManager.Player.data.SpyMute = SpyMute;
            EmpireManager.Player.data.SpyMissionRepeat = AutoTrain;

            CBAutoRepeat.Draw(batch);
            cbSpyMute.Draw(batch);

            Rectangle spyLimit = new Rectangle(moneyRect.X + 65, moneyRect.Y, 21, 20);
            batch.Draw(ResourceManager.Texture("NewUI/icon_lock"), spyLimit, Color.White);
            Vector2 spyLimitPos = new Vector2((spyLimit.X + 25), (spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));
            //empirePlanetSpys = EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            //if (EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            empirePlanetSpys = EmpireManager.Player.GetPlanets().Count() / 3 + 3;
            spyLimitCount = (empirePlanetSpys - EmpireManager.Player.data.AgentList.Count);
            if (empirePlanetSpys < 0) empirePlanetSpys = 0;
            batch.DrawString(Fonts.Arial12, string.Concat("For Hire : ", spyLimitCount.ToString(), " / ", empirePlanetSpys.ToString()), spyLimitPos, Color.White);

            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
            {
                var agent = e.Get<Agent>();
                var r = new Rectangle(e.X, e.Y, 25, 26);
                batch.Draw(ResourceManager.Texture("UI/icon_spy"), r, Color.White);
                var namecursor = new Vector2(r.X + 30, r.Y);

                batch.DrawString(Fonts.Arial12Bold, agent.Name, namecursor, Color.White);
                namecursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                string missionstring = Localizer.Token(agent.MissionNameIndex);
                batch.DrawString(Fonts.Arial12, missionstring, namecursor, Color.Gray);
                for (int j = 0; j < agent.Level; j++)
                {
                    var levelRect = new Rectangle(e.Right - 18 - 12 * j, e.Y, 12, 11);
                    batch.Draw(ResourceManager.Texture("UI/icon_star"), levelRect, Color.White);
                }
                if (agent.Mission != AgentMission.Defending)
                {
                    if (!string.IsNullOrEmpty(agent.TargetEmpire) && agent.Mission != AgentMission.Training && agent.Mission != AgentMission.Undercover)
                    {
                        Vector2 targetCursor = namecursor;
                        targetCursor.X += 75f;
                        missionstring = Localizer.Token(2199) + ": " + EmpireManager.GetEmpireByName(agent.TargetEmpire).data.Traits.Plural;
                        batch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
                    }
                    else if (agent.TargetGUID != Guid.Empty && agent.Mission == AgentMission.Undercover)
                    {
                        Vector2 targetCursor = namecursor;
                        targetCursor.X += 75f;
                        missionstring = Localizer.Token(2199) + ": " + Empire.Universe.PlanetsDict[agent.TargetGUID].Name;
                        batch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
                    }
                    if (agent.Mission != AgentMission.Undercover)
                    {
                        Vector2 turnsCursor = namecursor;
                        turnsCursor.X += 193f;
                        missionstring = Localizer.Token(2200) + ": " + agent.TurnsRemaining;
                        batch.DrawString(Fonts.Arial12, missionstring, turnsCursor, Color.Gray);
                    }
                }
            }
            selector?.Draw(batch);

            if (SelectedAgent != null)
            {
                batch.FillRectangle(OpsSubRect, Color.Black);
                OpsSL.Draw(batch);
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                {
                    e.Get<MissionEntry>().Draw(batch, e.Rect);
                }
            }
        }

		public static string GetName(string[] tokens)
		{
			var firstNames = new Array<string>();
			var lastNames = new Array<string>();
            foreach (string t in tokens)
			{
				if (t.Split(' ').Length != 1)
				{
					lastNames.Add(t);
				}
				else
				{
					firstNames.Add(t);
					lastNames.Add(t);
				}
			}

            string first = RandomMath.RandItem(firstNames);
            string last  = RandomMath.RandItem(lastNames);
            return $"{first} {last}";
		}


	    string[] LoadNames()
	    {
	        string playerNames = $"Content/NameGenerators/spynames_{EmpireManager.Player.data.Traits.ShipType}.txt";
	        string names = File.Exists(playerNames)
	            ? File.ReadAllText(playerNames)
	            : File.ReadAllText("Content/NameGenerators/spynames_Humans.txt");
	        return names.Split(',');
	    }

        //added by gremlin deveksmod Spy Handleinput
        public void HandleInput(InputState input)
        {
            AgentSL.HandleInput(input);
            if (SelectedAgent != null)
            {
                OpsSL.HandleInput(input);
            }
            if (RecruitButton.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2180));
            }
            if (RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) || spyLimitCount <= 0)//EmpireManager.Player.data.AgentList.Count >= EmpireManager.Player.GetPlanets().Count)
                {
                    GameAudio.PlaySfxAsync("UI_Misc20");
                }
                else
                {
                    EmpireManager.Player.Money -= ResourceManager.AgentMissionData.AgentCost;


                    var a = new Agent
                    {
                        Name = GetName(LoadNames()),
                        Age = RandomMath.RandomBetween(20, 30)
                    };
                    //Added new agent information
                    int randomPlanetIndex = RandomMath.InRange(EmpireManager.Player.GetPlanets().Count);
                    a.HomePlanet = EmpireManager.Player.GetPlanets()[randomPlanetIndex].Name;
                    EmpireManager.Player.data.AgentList.Add(a);
                    AgentSL.AddItem(a);
                    a.AssignMission(AgentMission.Training, EmpireManager.Player, "");
                }
            }
            selector = null;
            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
            {
                if (!e.CheckHover(input))
                    continue;
                selector = e.CreateSelector();
                if (input.InGameSelect)
                {
                    SelectedAgent = e.Get<Agent>();
                    foreach (MissionEntry mission in OpsSL.AllItems<MissionEntry>())
                        mission.Initialize();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
            }
            if (SelectedAgent != null)
            {
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                {
                    var mission = e.Get<MissionEntry>();
                    mission.HandleInput(input);
                    if (e.CheckHover(input))
                    {
                        ToolTip.CreateTooltip(!mission.DoMission.Rect.HitTest(input.CursorPosition)
                            ? Localizer.Token(mission.DescriptionIndex)
                            : Localizer.Token(2198));
                    }
                }
            }
            if (CBAutoRepeat != null && cbSpyMute != null)
            {
                CBAutoRepeat.HandleInput(input);
                cbSpyMute.HandleInput(input);
            }
        }

		public void Reinitialize()
		{
		    if (SelectedAgent == null)
		        return;
		    foreach (MissionEntry mission in OpsSL.AllItems<MissionEntry>())
		        mission.Initialize();
		}
	}
}
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class AgentComponent : IDisposable
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
            this.SpyMute = EmpireManager.Player.data.SpyMute;
            this.AutoTrain = EmpireManager.Player.data.SpyMissionRepeat;
            this.Escreen = Escreen;
            this.ComponentRect = r;
            this.ScreenManager = Empire.Universe.ScreenManager;
            this.SubRect = new Rectangle(this.ComponentRect.X, this.ComponentRect.Y + 25, this.ComponentRect.Width, this.ComponentRect.Height - 25);
            this.OpsSubRect = new Rectangle(Escreen.OperationsRect.X + 20, this.ComponentRect.Y + 25, this.ComponentRect.Width, this.ComponentRect.Height - 25);
            Submenu sub = new Submenu(this.ComponentRect);
            this.AgentSL = new ScrollList(sub, 40);
            foreach (Agent agent in EmpireManager.Player.data.AgentList)
            {
                this.AgentSL.AddItem(agent);
            }
            Rectangle c = this.ComponentRect;
            c.X = this.OpsSubRect.X;
            Submenu opssub = new Submenu(c);
            this.OpsSL = new ScrollList(opssub, 30);
            this.Training = new MissionEntry(AgentMission.Training, this);
            this.Infiltrate = new MissionEntry(AgentMission.Infiltrate, this);
            this.Assassinate = new MissionEntry(AgentMission.Assassinate, this);
            this.Sabotage = new MissionEntry(AgentMission.Sabotage, this);
            this.StealTech = new MissionEntry(AgentMission.StealTech, this);
            this.StealShip = new MissionEntry(AgentMission.Robbery, this);
            this.InciteRebellion = new MissionEntry(AgentMission.InciteRebellion, this);
            this.OpsSL.AddItem(this.Training);
            this.OpsSL.AddItem(this.Infiltrate);
            this.OpsSL.AddItem(this.Assassinate);
            this.OpsSL.AddItem(this.Sabotage);
            this.OpsSL.AddItem(this.StealTech);
            this.OpsSL.AddItem(this.StealShip);
            this.OpsSL.AddItem(this.InciteRebellion);
            this.RecruitButton = new DanButton(new Vector2((float)(this.ComponentRect.X), (float)(this.ComponentRect.Y + this.ComponentRect.Height + 5f)), Localizer.Token(2179))
            {
                Toggled = true
            };
        }

        //added by gremlin deveksmod spy draw
        public void Draw()
        {
            ScreenManager.SpriteBatch.FillRectangle(SubRect, Color.Black);
            AgentSL.Draw(ScreenManager.SpriteBatch);
            RecruitButton.Draw(ScreenManager);
            Rectangle moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], moneyRect, Color.White);

            Vector2 costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            // @todo Why are we creating new checkboxes every frame??
            CBAutoRepeat = new UICheckBox(null, OpsSubRect.X - 10, moneyRect.Y - 30, () => AutoTrain, Fonts.Arial12, "Repeat Missions", 0);
            cbSpyMute    = new UICheckBox(null, OpsSubRect.X - 10, moneyRect.Y - 15, () => SpyMute,   Fonts.Arial12, "Mute Spies", 0);

            EmpireManager.Player.data.SpyMute = SpyMute;
            EmpireManager.Player.data.SpyMissionRepeat = AutoTrain;

            CBAutoRepeat.Draw(ScreenManager.SpriteBatch);
            cbSpyMute.Draw(ScreenManager.SpriteBatch);

            Rectangle spyLimit = new Rectangle((int)moneyRect.X + 65, (int)moneyRect.Y, 21, 20);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_lock"], spyLimit, Color.White);
            Vector2 spyLimitPos = new Vector2((float)(spyLimit.X + 25), (float)(spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));
            //empirePlanetSpys = EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            //if (EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            empirePlanetSpys = EmpireManager.Player.GetPlanets().Count() / 3 + 3;
            spyLimitCount = (empirePlanetSpys - EmpireManager.Player.data.AgentList.Count);
            if (empirePlanetSpys < 0) empirePlanetSpys = 0;
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("For Hire : ", spyLimitCount.ToString(), " / ", empirePlanetSpys.ToString()), spyLimitPos, Color.White);

            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
            {
                var agent = e.item as Agent;
                var r = new Rectangle(e.clickRect.X, e.clickRect.Y, 25, 26);
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy"], r, Color.White);
                var namecursor = new Vector2(r.X + 30, r.Y);

                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, agent.Name, namecursor, Color.White);
                namecursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                string missionstring = Localizer.Token(agent.MissionNameIndex);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, namecursor, Color.Gray);
                for (int j = 0; j < agent.Level; j++)
                {
                    var levelRect = new Rectangle(e.clickRect.X + e.clickRect.Width - 18 - 12 * j, e.clickRect.Y, 12, 11);
                    ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/icon_star"], levelRect, Color.White);
                }
                if (agent.Mission != AgentMission.Defending)
                {
                    if (!string.IsNullOrEmpty(agent.TargetEmpire) && agent.Mission != AgentMission.Training && agent.Mission != AgentMission.Undercover)
                    {
                        Vector2 targetCursor = namecursor;
                        targetCursor.X += 75f;
                        missionstring = Localizer.Token(2199) + ": " + EmpireManager.GetEmpireByName(agent.TargetEmpire).data.Traits.Plural;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
                    }
                    else if (agent.TargetGUID != Guid.Empty && agent.Mission == AgentMission.Undercover)
                    {
                        Vector2 targetCursor = namecursor;
                        targetCursor.X += 75f;
                        missionstring = Localizer.Token(2199) + ": " + Empire.Universe.PlanetsDict[agent.TargetGUID].Name;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
                    }
                    if (agent.Mission != AgentMission.Undercover)
                    {
                        Vector2 turnsCursor = namecursor;
                        turnsCursor.X += 193f;
                        missionstring = Localizer.Token(2200) + ": " + agent.TurnsRemaining;
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, turnsCursor, Color.Gray);
                    }
                }
            }
            selector?.Draw(ScreenManager.SpriteBatch);

            if (SelectedAgent != null)
            {
                ScreenManager.SpriteBatch.FillRectangle(OpsSubRect, Color.Black);
                OpsSL.Draw(ScreenManager.SpriteBatch);
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                {
                    ((MissionEntry)e.item).Draw(ScreenManager, e.clickRect);
                }
            }
        }

		public static string GetName(string[] Tokens)
		{
			string ret = "";
			Array<string> PotentialFirst = new Array<string>();
			Array<string> PotentialSecond = new Array<string>();
			string[] tokens = Tokens;
			for (int i = 0; i < (int)tokens.Length; i++)
			{
				string t = tokens[i];
				char[] chrArray = new char[] { ' ' };
				if ((int)t.Split(chrArray).Length != 1)
				{
					PotentialSecond.Add(t);
				}
				else
				{
					PotentialFirst.Add(t);
					PotentialSecond.Add(t);
				}
			}
			ret = string.Concat(ret, PotentialFirst[RandomMath.InRange(PotentialFirst.Count)], " ");
			ret = string.Concat(ret, PotentialSecond[RandomMath.InRange(PotentialSecond.Count)]);
			return ret;
		}

		public void HandleInputorig(InputState input)
		{
			string Names;
			this.AgentSL.HandleInput(input);
			if (this.SelectedAgent != null)
			{
				this.OpsSL.HandleInput(input);
			}
			if (this.RecruitButton.r.HitTest(input.CursorPosition))
			{
				ToolTip.CreateTooltip(Localizer.Token(2180));
			}
			if (this.RecruitButton.HandleInput(input))
			{
				if (EmpireManager.Player.Money < 250f)
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
				else
				{
					Empire empireByName = EmpireManager.Player;
					empireByName.Money = empireByName.Money - 250f;
					Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", EmpireManager.Player.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", EmpireManager.Player.data.Traits.ShipType, ".txt")));
					string[] Tokens = Names.Split(new char[] { ',' });
					Agent a = new Agent()
					{
						Name = AgentComponent.GetName(Tokens)
					};
					EmpireManager.Player.data.AgentList.Add(a);
					this.AgentSL.AddItem(a);
				}
			}
			selector = null;
            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
			{
			    if (!e.clickRect.HitTest(input.CursorPosition))
			        continue;
			    
			    selector = new Selector(e.clickRect);
			    if (input.InGameSelect)
			    {
			        SelectedAgent = e.item as Agent;
			        foreach (MissionEntry mission in OpsSL.AllItems<MissionEntry>())
			            mission.Initialize();
			        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
			    }
			}
			if (SelectedAgent != null)
			{
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
				{
                    var mission = (MissionEntry)e.item;
				    mission.HandleInput(input);
					if (e.clickRect.HitTest(input.CursorPosition))
					{
					    ToolTip.CreateTooltip(!mission.DoMission.Rect.HitTest(input.CursorPosition)
					        ? Localizer.Token(mission.DescriptionIndex)
					        : Localizer.Token(2198));
					}
				}
			}
		}
        //added by gremlin deveksmod Spy Handleinput
        public void HandleInput(InputState input)
        {
            string Names;
            this.AgentSL.HandleInput(input);
            if (this.SelectedAgent != null)
            {
                this.OpsSL.HandleInput(input);
            }
            if (this.RecruitButton.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2180));
            }
            if (this.RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) || spyLimitCount <= 0)//EmpireManager.Player.data.AgentList.Count >= EmpireManager.Player.GetPlanets().Count)
                {
                    GameAudio.PlaySfxAsync("UI_Misc20");
                }
                else
                {
                    Empire empireByName = EmpireManager.Player;
                    empireByName.Money -= ResourceManager.AgentMissionData.AgentCost;
                    Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", EmpireManager.Player.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", EmpireManager.Player.data.Traits.ShipType, ".txt")));
                    string[] Tokens = Names.Split(new char[] { ',' });
                    Agent a = new Agent();
                    a.Name = AgentComponent.GetName(Tokens);
                    //Added new agent information
                    a.Age = RandomMath.RandomBetween(20, 30);
                    int RandomPlanetIndex = RandomMath.InRange(EmpireManager.Player.GetPlanets().Count);
                    a.HomePlanet = EmpireManager.Player.GetPlanets()[RandomPlanetIndex].Name;
                    EmpireManager.Player.data.AgentList.Add(a);
                    this.AgentSL.AddItem(a);
                    a.AssignMission(AgentMission.Training, empireByName, "");

                }
            }
            selector = null;
            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
            {
                if (!e.clickRect.HitTest(input.CursorPosition))
                    continue;
                selector = new Selector(e.clickRect);
                if (input.InGameSelect)
                {
                    SelectedAgent = e.item as Agent;
                    foreach (MissionEntry mission in OpsSL.AllItems<MissionEntry>())
                        mission.Initialize();
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
            }
            if (SelectedAgent != null)
            {
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                {
                    var mission = (MissionEntry)e.item;
                    mission.HandleInput(input);
                    if (e.clickRect.HitTest(input.CursorPosition))
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

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~AgentComponent() { Destroy();  }

        private void Destroy()
        {
            AgentSL?.Dispose(ref AgentSL);
            OpsSL?.Dispose(ref OpsSL);
        }
	}
}
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class AgentComponent : UIElementContainer
	{
		public Agent SelectedAgent;

		public Rectangle ComponentRect;

		public Rectangle SubRect;

		public Rectangle OpsSubRect;

		public ScrollList AgentSL;

		public ScrollList OpsSL;

		private ScreenManager ScreenManager;

		public DanButton RecruitButton;

        public EspionageScreen EspionageScreen;

		private MissionEntry Training;

		private MissionEntry Infiltrate;

		private MissionEntry Assassinate;

		private MissionEntry Sabotage;

		private MissionEntry StealTech;

		private MissionEntry StealShip;

		private MissionEntry InciteRebellion;

		private Selector selector;
        private int spyLimitCount;
        private int empirePlanetSpys;

        public AgentComponent(EspionageScreen espionageScreen, Rectangle r, Rectangle operationsRect) : base(espionageScreen, r)
        {
            EspionageScreen = espionageScreen;

            ComponentRect = r;
            ScreenManager = Empire.Universe.ScreenManager;
            SubRect = new Rectangle(ComponentRect.X, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            OpsSubRect = new Rectangle(operationsRect.X + 20, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);

            AgentSL = new ScrollList(new Submenu(ComponentRect), 40);
            AgentSL.SetItems(EmpireManager.Player.data.AgentList);

            Rectangle c = ComponentRect;
            c.X = OpsSubRect.X;
            OpsSL = new ScrollList(new Submenu(c), 30);
            Training        = new MissionEntry(AgentMission.Training, this);
            Infiltrate      = new MissionEntry(AgentMission.Infiltrate, this);
            Assassinate     = new MissionEntry(AgentMission.Assassinate, this);
            Sabotage        = new MissionEntry(AgentMission.Sabotage, this);
            StealTech       = new MissionEntry(AgentMission.StealTech, this);
            StealShip       = new MissionEntry(AgentMission.Robbery, this);
            InciteRebellion = new MissionEntry(AgentMission.InciteRebellion, this);
            OpsSL.AddItem(Training);
            OpsSL.AddItem(Infiltrate);
            OpsSL.AddItem(Assassinate);
            OpsSL.AddItem(Sabotage);
            OpsSL.AddItem(StealTech);
            OpsSL.AddItem(StealShip);
            OpsSL.AddItem(InciteRebellion);
            RecruitButton = new DanButton(new Vector2(ComponentRect.X, ComponentRect.Y + ComponentRect.Height + 5f), Localizer.Token(2179))
            {
                Toggled = true
            };
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y,      () => EmpireManager.Player.data.SpyMissionRepeat, "Repeat Missions", 0);
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y + 15, () => EmpireManager.Player.data.SpyMute,          "Mute Spies",      0);
        }

        public override void Draw(SpriteBatch batch)
        {
            SubTexture money    = ResourceManager.Texture("NewUI/icon_money");
            SubTexture iconLock = ResourceManager.Texture("NewUI/icon_lock");

            batch.FillRectangle(SubRect, Color.Black);
            AgentSL.Draw(batch);
            RecruitButton.Draw(ScreenManager);
            var moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            batch.Draw(money, moneyRect, Color.White);

            var costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            batch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            base.Draw(batch);

            var spyLimit = new Rectangle(moneyRect.X + 65, moneyRect.Y, 21, 20);
            batch.Draw(iconLock, spyLimit, Color.White);
            var spyLimitPos = new Vector2((spyLimit.X + 25), (spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));

            empirePlanetSpys = EmpireManager.Player.NumPlanets / 3 + 3;
            spyLimitCount = (empirePlanetSpys - EmpireManager.Player.data.AgentList.Count);
            if (empirePlanetSpys < 0) empirePlanetSpys = 0;
            batch.DrawString(Fonts.Arial12, $"For Hire : {spyLimitCount} / {empirePlanetSpys}", spyLimitPos, Color.White);

            foreach (ScrollList.Entry e in AgentSL.VisibleEntries)
            {
                var agent = e.Get<Agent>();
                DrawAgent(batch, e, agent);
            }

            selector?.Draw(batch);

            if (SelectedAgent != null)
            {
                batch.FillRectangle(OpsSubRect, Color.Black);
                OpsSL.Draw(batch);
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                    e.Get<MissionEntry>().Draw(batch, e.Rect);
            }
        }

	    private static void DrawAgent(SpriteBatch batch, ScrollList.Entry e, Agent agent)
	    {
            SubTexture spy  = ResourceManager.Texture("UI/icon_spy");
            SubTexture star = ResourceManager.Texture("UI/icon_star");

            var r = new Rectangle(e.X, e.Y, 25, 26);
	        batch.Draw(spy, r, Color.White);
	        var namecursor = new Vector2(r.X + 30, r.Y);

	        batch.DrawString(Fonts.Arial12Bold, agent.Name, namecursor, Color.White);
	        namecursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
	        batch.DrawString(Fonts.Arial12, Localizer.Token(agent.MissionNameIndex), namecursor, Color.Gray);

	        for (int j = 0; j < agent.Level; j++)
	        {
	            var levelRect = new Rectangle(e.Right - 18 - 12 * j, e.Y, 12, 11);
	            batch.Draw(star, levelRect, Color.White);
	        }

	        if (agent.Mission != AgentMission.Defending)
	        {
	            if (!string.IsNullOrEmpty(agent.TargetEmpire) && agent.Mission != AgentMission.Training &&
	                agent.Mission != AgentMission.Undercover)
	            {
	                Vector2 targetCursor = namecursor;
	                targetCursor.X += 75f;
	                string mission = Localizer.Token(2199) + ": " +
	                                 EmpireManager.GetEmpireByName(agent.TargetEmpire).data.Traits.Plural;
	                batch.DrawString(Fonts.Arial12, mission, targetCursor, Color.Gray);
	            }
	            else if (agent.TargetGUID != Guid.Empty && agent.Mission == AgentMission.Undercover)
	            {
	                Vector2 targetCursor = namecursor;
	                targetCursor.X += 75f;
	                string mission = Localizer.Token(2199) + ": " + Empire.Universe.PlanetsDict[agent.TargetGUID].Name;
	                batch.DrawString(Fonts.Arial12, mission, targetCursor, Color.Gray);
	            }

	            if (agent.Mission != AgentMission.Undercover)
	            {
	                Vector2 turnsCursor = namecursor;
	                turnsCursor.X += 193f;
	                string mission = Localizer.Token(2200) + ": " + agent.TurnsRemaining;
	                batch.DrawString(Fonts.Arial12, mission, turnsCursor, Color.Gray);
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
        public override bool HandleInput(InputState input)
        {
            if (AgentSL.HandleInput(input))
                return true;

            if (SelectedAgent != null)
            {
                if (OpsSL.HandleInput(input))
                    return true;
            }

            if (RecruitButton.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2180));
            }

            if (RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) || spyLimitCount <= 0)//EmpireManager.Player.data.AgentList.Count >= EmpireManager.Player.GetPlanets().Count)
                {
                    GameAudio.NegativeClick();
                }
                else
                {
                    EmpireManager.Player.AddMoney(-ResourceManager.AgentMissionData.AgentCost);
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
                        mission.UpdateMissionAvailability();
                    GameAudio.AcceptClick();
                }
            }

            if (SelectedAgent != null)
            {
                foreach (ScrollList.Entry e in OpsSL.VisibleEntries)
                {
                    var mission = e.Get<MissionEntry>();
                    if (mission.HandleInput(input, e.CheckHover(input)))
                        return true;
                }
            }

            return base.HandleInput(input);
        }

		public void Reinitialize()
		{
		    if (SelectedAgent == null)
		        return;
		    foreach (MissionEntry mission in OpsSL.AllItems<MissionEntry>())
		        mission.UpdateMissionAvailability();
		}
	}
}
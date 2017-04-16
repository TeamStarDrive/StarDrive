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
        private Checkbox CBAutoRepeat;
        private bool SpyMute;
        private Checkbox cbSpyMute;
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
            Submenu sub = new Submenu(this.ScreenManager, this.ComponentRect);
            this.AgentSL = new ScrollList(sub, 40);
            foreach (Agent agent in EmpireManager.Player.data.AgentList)
            {
                this.AgentSL.AddItem(agent);
            }
            Rectangle c = this.ComponentRect;
            c.X = this.OpsSubRect.X;
            Submenu opssub = new Submenu(this.ScreenManager, c);
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

		public void Draworig()
		{
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.SubRect, Color.Black);
			this.AgentSL.Draw(this.ScreenManager.SpriteBatch);
			this.RecruitButton.Draw(this.ScreenManager);
			Rectangle MoneyRect = new Rectangle(this.RecruitButton.r.X + 200, this.RecruitButton.r.Y + this.RecruitButton.r.Height / 2 - 10, 21, 20);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], MoneyRect, Color.White);
			Vector2 costPos = new Vector2((float)(MoneyRect.X + 25), (float)(MoneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2));
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "250", costPos, Color.White);
			for (int i = this.AgentSL.indexAtTop; i < this.AgentSL.Entries.Count && i < this.AgentSL.indexAtTop + this.AgentSL.entriesToDisplay; i++)
			{
				try
				{
					ScrollList.Entry e = this.AgentSL.Entries[i];
					Agent agent = e.item as Agent;
					Rectangle r = new Rectangle(e.clickRect.X, e.clickRect.Y, 25, 26);
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy"], r, Color.White);
					Vector2 namecursor = new Vector2((float)(r.X + 30), (float)r.Y);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, agent.Name, namecursor, Color.White);
					namecursor.Y = namecursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
					string missionstring = Localizer.Token(agent.MissionNameIndex);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, namecursor, Color.Gray);
					for (int j = 0; j < agent.Level; j++)
					{
						Rectangle levelRect = new Rectangle(e.clickRect.X + e.clickRect.Width - 18 - 12 * j, e.clickRect.Y, 12, 11);
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_star"], levelRect, Color.White);
					}
					if ((e.item as Agent).Mission != AgentMission.Defending)
					{
						if (!string.IsNullOrEmpty((e.item as Agent).TargetEmpire) && (e.item as Agent).Mission != AgentMission.Training && (e.item as Agent).Mission != AgentMission.Undercover)
						{
							Vector2 targetCursor = namecursor;
							targetCursor.X = targetCursor.X + 75f;
							missionstring = string.Concat(Localizer.Token(2199), ": ", EmpireManager.GetEmpireByName((e.item as Agent).TargetEmpire).data.Traits.Plural);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
						}
						else if ((e.item as Agent).TargetGUID != Guid.Empty && (e.item as Agent).Mission == AgentMission.Undercover)
						{
							Vector2 targetCursor = namecursor;
							targetCursor.X = targetCursor.X + 75f;
							missionstring = string.Concat(Localizer.Token(2199), ": ", Empire.Universe.PlanetsDict[(e.item as Agent).TargetGUID].Name);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, targetCursor, Color.Gray);
						}
						if ((e.item as Agent).Mission != AgentMission.Undercover)
						{
							Vector2 turnsCursor = namecursor;
							turnsCursor.X = turnsCursor.X + 193f;
							missionstring = string.Concat(Localizer.Token(2200), ": ", (e.item as Agent).TurnsRemaining);
							this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, turnsCursor, Color.Gray);
						}
					}
				}
				catch
				{
				}
			}
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			if (this.SelectedAgent != null)
			{
				Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.OpsSubRect, Color.Black);
				this.OpsSL.Draw(this.ScreenManager.SpriteBatch);
				for (int i = this.OpsSL.indexAtTop; i < this.OpsSL.Entries.Count && i < this.OpsSL.indexAtTop + this.OpsSL.entriesToDisplay; i++)
				{
					try
					{
						ScrollList.Entry e = this.OpsSL.Entries[i];
						(e.item as MissionEntry).Draw(this.ScreenManager, e.clickRect);
					}
					catch
					{
					}
				}
			}
		}
        //added by gremlin deveksmod spy draw
        public void Draw()
        {
            Primitives2D.FillRectangle(ScreenManager.SpriteBatch, SubRect, Color.Black);
            AgentSL.Draw(ScreenManager.SpriteBatch);
            RecruitButton.Draw(ScreenManager);
            Rectangle moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], moneyRect, Color.White);

            Vector2 costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            // @todo Why are we creating new checkboxes every frame??
            CBAutoRepeat = new Checkbox(OpsSubRect.X - 10, moneyRect.Y - 30, () => AutoTrain, Fonts.Arial12, "Repeat Missions", 0);
            cbSpyMute    = new Checkbox(OpsSubRect.X - 10, moneyRect.Y - 15, () => SpyMute,   Fonts.Arial12, "Mute Spies", 0);

            EmpireManager.Player.data.SpyMute = SpyMute;
            EmpireManager.Player.data.SpyMissionRepeat = AutoTrain;

            CBAutoRepeat.Draw(ScreenManager);
            cbSpyMute.Draw(ScreenManager);

            Rectangle spyLimit = new Rectangle((int)moneyRect.X + 65, (int)moneyRect.Y, 21, 20);
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_lock"], spyLimit, Color.White);
            Vector2 spyLimitPos = new Vector2((float)(spyLimit.X + 25), (float)(spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));
            //empirePlanetSpys = EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            //if (EmpireManager.Player.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            empirePlanetSpys = EmpireManager.Player.GetPlanets().Count() / 3 + 3;
            spyLimitCount = (empirePlanetSpys - EmpireManager.Player.data.AgentList.Count);
            if (empirePlanetSpys < 0) empirePlanetSpys = 0;
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("For Hire : ", spyLimitCount.ToString(), " / ", empirePlanetSpys.ToString()), spyLimitPos, Color.White);

            for (int i = AgentSL.indexAtTop; i < AgentSL.Entries.Count && i < AgentSL.indexAtTop + AgentSL.entriesToDisplay; i++)
            {
                try
                {
                    ScrollList.Entry e = AgentSL.Entries[i];
                    Agent agent = e.item as Agent;
                    Rectangle r = new Rectangle(e.clickRect.X, e.clickRect.Y, 25, 26);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy"], r, Color.White);
                    Vector2 namecursor = new Vector2((float)(r.X + 30), (float)r.Y);
                    //Ref<bool> acomRef = new Ref<bool>(() => GlobalStats.PlanetaryGravityWells, (bool x) => GlobalStats.PlanetaryGravityWells = x);

                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, agent.Name, namecursor, Color.White);
                    namecursor.Y = namecursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    string missionstring = Localizer.Token(agent.MissionNameIndex);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, missionstring, namecursor, Color.Gray);
                    for (int j = 0; j < agent.Level; j++)
                    {
                        Rectangle levelRect = new Rectangle(e.clickRect.X + e.clickRect.Width - 18 - 12 * j, e.clickRect.Y, 12, 11);
                        this.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/icon_star"], levelRect, Color.White);
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
                catch (Exception ex)
                {
                    Log.Error(ex, "AgentComponent Draw crashed at index {0} out of {1}", i, AgentSL.Entries.Count);
                }
            }
            selector?.Draw();

            if (SelectedAgent != null)
            {
                Primitives2D.FillRectangle(ScreenManager.SpriteBatch, OpsSubRect, Color.Black);
                OpsSL.Draw(ScreenManager.SpriteBatch);
                for (int i = OpsSL.indexAtTop; i < OpsSL.Entries.Count && i < OpsSL.indexAtTop + OpsSL.entriesToDisplay; i++)
                {
                    try
                    {
                        ScrollList.Entry e = OpsSL.Entries[i];
                        (e.item as MissionEntry).Draw(ScreenManager, e.clickRect);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "MissionEntry Draw crashed");
                    }
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
			if (HelperFunctions.CheckIntersection(this.RecruitButton.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(Localizer.Token(2180), this.ScreenManager);
			}
			if (this.RecruitButton.HandleInput(input))
			{
				if (EmpireManager.Player.Money < 250f)
				{
					GameAudio.PlaySfx("UI_Misc20");
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
			this.selector = null;
			for (int i = this.AgentSL.indexAtTop; i < this.AgentSL.Entries.Count && i < this.AgentSL.indexAtTop + this.AgentSL.entriesToDisplay; i++)
			{
				try
				{
					ScrollList.Entry e = this.AgentSL.Entries[i];
					if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
					{
						this.selector = new Selector(this.ScreenManager, e.clickRect);
						if (input.InGameSelect)
						{
							this.SelectedAgent = e.item as Agent;
							foreach (ScrollList.Entry entry in this.OpsSL.Entries)
							{
								(entry.item as MissionEntry).Initialize();
							}
							GameAudio.PlaySfx("sd_ui_accept_alt3");
						}
					}
				}
				catch
				{
				}
			}
			if (this.SelectedAgent != null)
			{
				for (int i = this.OpsSL.indexAtTop; i < this.OpsSL.Entries.Count && i < this.OpsSL.indexAtTop + this.OpsSL.entriesToDisplay; i++)
				{
					try
					{
						ScrollList.Entry e = this.OpsSL.Entries[i];
						(e.item as MissionEntry).HandleInput(input);
						if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
						{
							if (!HelperFunctions.CheckIntersection((e.item as MissionEntry).DoMission.Rect, input.CursorPosition))
							{
								ToolTip.CreateTooltip(Localizer.Token((e.item as MissionEntry).DescriptionIndex), this.ScreenManager);
							}
							else
							{
								ToolTip.CreateTooltip(Localizer.Token(2198), Empire.Universe.ScreenManager);
							}
						}
					}
					catch
					{
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
            if (HelperFunctions.CheckIntersection(this.RecruitButton.r, input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2180), this.ScreenManager);
            }
            if (this.RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) || spyLimitCount <= 0)//EmpireManager.Player.data.AgentList.Count >= EmpireManager.Player.GetPlanets().Count)
                {
                    GameAudio.PlaySfx("UI_Misc20");
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
            this.selector = null;
            for (int i = this.AgentSL.indexAtTop; i < this.AgentSL.Entries.Count && i < this.AgentSL.indexAtTop + this.AgentSL.entriesToDisplay; i++)
            {
                try
                {
                    ScrollList.Entry e = this.AgentSL.Entries[i];
                    if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
                    {
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            this.SelectedAgent = e.item as Agent;
                            foreach (ScrollList.Entry entry in this.OpsSL.Entries)
                            {
                                (entry.item as MissionEntry).Initialize();
                            }
                            GameAudio.PlaySfx("sd_ui_accept_alt3");
                        }
                    }
                }
                catch
                {
                }
            }
            if (this.SelectedAgent != null)
            {
                for (int i = this.OpsSL.indexAtTop; i < this.OpsSL.Entries.Count && i < this.OpsSL.indexAtTop + this.OpsSL.entriesToDisplay; i++)
                {
                    try
                    {
                        ScrollList.Entry e = this.OpsSL.Entries[i];
                        (e.item as MissionEntry).HandleInput(input);
                        if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
                        {
                            if (!HelperFunctions.CheckIntersection((e.item as MissionEntry).DoMission.Rect, input.CursorPosition))
                            {
                                ToolTip.CreateTooltip(Localizer.Token((e.item as MissionEntry).DescriptionIndex), this.ScreenManager);
                            }
                            else
                            {
                                ToolTip.CreateTooltip(Localizer.Token(2198), Empire.Universe.ScreenManager);
                            }
                        }
                    }
                    catch
                    {
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
			if (this.SelectedAgent != null)
			{
				for (int i = this.AgentSL.indexAtTop; i < this.AgentSL.Entries.Count && i < this.AgentSL.indexAtTop + this.AgentSL.entriesToDisplay; i++)
				{
					ScrollList.Entry item = this.AgentSL.Entries[i];
					foreach (ScrollList.Entry entry in this.OpsSL.Entries)
					{
						(entry.item as MissionEntry).Initialize();
					}
				}
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AgentComponent() { Dispose(false);  }

        private void Dispose(bool disposing)
        {
            AgentSL?.Dispose(ref AgentSL);
            OpsSL?.Dispose(ref OpsSL);
        }
	}
}
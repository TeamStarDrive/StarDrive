using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class MissionEntry
	{
		public bool Available;

		public UIButton DoMission;

		private AgentComponent Component;

		public AgentMission TheMission;

		public int cost;

		public int turns;

		public int NameIndex;

		public int DescriptionIndex;

		public MissionEntry(AgentMission am, AgentComponent parent)
		{
			this.Component = parent;
			this.TheMission = am;
            DoMission = new UIButton(null, ButtonStyle.Low80, 0f, 0f, "Go");
            DoMission.OnClick += DoMission_OnClick;
		}

        private void DoMission_OnClick(UIButton button)
        {
            Component.SelectedAgent.AssignMission(TheMission, 
                EmpireManager.Player, Component.Escreen.SelectedEmpire.data.Traits.Name);

        }

        public void Draw(SpriteBatch batch, Rectangle clickRect)
		{
            var cursor = new Vector2(clickRect.X, (clickRect.Y + clickRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
		    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(this.NameIndex), cursor, (this.Available ? Color.White : Color.Gray));
			cursor.X += 120f;
			HelperFunctions.ClampVectorToInt(ref cursor);
		    batch.DrawString(Fonts.Arial12Bold, string.Concat(this.turns, " turns"), cursor, (this.Available ? Color.White : Color.Gray));
			cursor.X += 70f;
			var smallmoney = new Rectangle((int)cursor.X, (int)cursor.Y - 3, 21, 20);
		    batch.Draw(ResourceManager.Texture("NewUI/icon_money"), smallmoney, Color.White);
			cursor.X += 25f;
			HelperFunctions.ClampVectorToInt(ref cursor);
		    batch.DrawString(Fonts.Arial12Bold, cost.ToString(), cursor, (Available ? Color.White : Color.Gray));
			if (Available)
			{
                Texture2D tex = ResourceManager.Texture("EmpireTopBar/empiretopbar_low_btn_80px");
                DoMission.Rect = new Rectangle(smallmoney.X + 50, (int)cursor.Y - 1, tex.Width, tex.Height);
				DoMission.Draw(batch);
			}
		    batch.DrawLine(new Vector2(clickRect.X, (clickRect.Y + clickRect.Height)), new Vector2((clickRect.X + clickRect.Width), (clickRect.Y + clickRect.Height)), Color.OrangeRed);
		}

        //added by gremlin MissionHandleInput
        public void HandleInput(InputState input)
        {
            DoMission.HandleInput(input);
        }

        //added by gremlin deveks missionInit
        public void Initialize()
        {
            this.Available = false;
            switch (this.TheMission)
            {
                case AgentMission.Training:
                    {
                        if (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover)
				        {
					        this.Available = true;
				        }
                        this.turns = ResourceManager.AgentMissionData.TrainingTurns;
                        this.cost = ResourceManager.AgentMissionData.TrainingCost;
                        this.NameIndex = 2196;
                        this.DescriptionIndex = 2197;
                        break;
                    }
                case AgentMission.Infiltrate:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.InfiltrateTurns;
                        this.cost = ResourceManager.AgentMissionData.InfiltrateCost;
                        this.NameIndex = 2188;
                        this.DescriptionIndex = 2189;
                        break;
                    }
                case AgentMission.Assassinate:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.AssassinateTurns;
                        this.cost = ResourceManager.AgentMissionData.AssassinateCost;
                        this.NameIndex = 2184;
                        this.DescriptionIndex = 2185;
                        break;
                    }
                case AgentMission.Sabotage:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.SabotageTurns;
                        this.cost = ResourceManager.AgentMissionData.SabotageCost;
                        this.NameIndex = 2190;
                        this.DescriptionIndex = 2191;
                        break;
                    }
                case AgentMission.StealTech:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.StealTechTurns;
                        this.cost = ResourceManager.AgentMissionData.StealTechCost;
                        this.NameIndex = 2194;
                        this.DescriptionIndex = 2195;
                        break;
                    }
                case AgentMission.Robbery:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.RobberyTurns;
                        this.cost = ResourceManager.AgentMissionData.RobberyCost;
                        this.NameIndex = 2192;
                        this.DescriptionIndex = 2193;
                        break;
                    }
                case AgentMission.InciteRebellion:
                    {
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.Player && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            this.Available = true;
                        }
                        this.turns = ResourceManager.AgentMissionData.RebellionTurns;
                        this.cost = ResourceManager.AgentMissionData.RebellionCost;
                        this.NameIndex = 2186;
                        this.DescriptionIndex = 2187;
                        break;
                    }
            }
            if (EmpireManager.Player.Money < (float)this.cost || this.Component.Escreen.SelectedEmpire.data.Defeated || this.Component.SelectedAgent.Mission == AgentMission.Recovering)
            {
                this.Available = false;
            }
        }
	}
}
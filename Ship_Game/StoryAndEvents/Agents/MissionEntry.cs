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
			Component = parent;
			TheMission = am;
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
		    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(NameIndex), cursor, (Available ? Color.White : Color.Gray));
			cursor.X += 120f;
			HelperFunctions.ClampVectorToInt(ref cursor);
		    batch.DrawString(Fonts.Arial12Bold, string.Concat(turns, " turns"), cursor, (Available ? Color.White : Color.Gray));
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
            Available = false;
            switch (TheMission)
            {
                case AgentMission.Training:
                    {
                        if (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover)
				        {
					        Available = true;
				        }
                        turns = ResourceManager.AgentMissionData.TrainingTurns;
                        cost = ResourceManager.AgentMissionData.TrainingCost;
                        NameIndex = 2196;
                        DescriptionIndex = 2197;
                        break;
                    }
                case AgentMission.Infiltrate:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.InfiltrateTurns;
                        cost = ResourceManager.AgentMissionData.InfiltrateCost;
                        NameIndex = 2188;
                        DescriptionIndex = 2189;
                        break;
                    }
                case AgentMission.Assassinate:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.AssassinateTurns;
                        cost = ResourceManager.AgentMissionData.AssassinateCost;
                        NameIndex = 2184;
                        DescriptionIndex = 2185;
                        break;
                    }
                case AgentMission.Sabotage:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.SabotageTurns;
                        cost = ResourceManager.AgentMissionData.SabotageCost;
                        NameIndex = 2190;
                        DescriptionIndex = 2191;
                        break;
                    }
                case AgentMission.StealTech:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.StealTechTurns;
                        cost = ResourceManager.AgentMissionData.StealTechCost;
                        NameIndex = 2194;
                        DescriptionIndex = 2195;
                        break;
                    }
                case AgentMission.Robbery:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.RobberyTurns;
                        cost = ResourceManager.AgentMissionData.RobberyCost;
                        NameIndex = 2192;
                        DescriptionIndex = 2193;
                        break;
                    }
                case AgentMission.InciteRebellion:
                    {
                        if (Component.Escreen.SelectedEmpire != EmpireManager.Player && (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover))
                        {
                            Available = true;
                        }
                        turns = ResourceManager.AgentMissionData.RebellionTurns;
                        cost = ResourceManager.AgentMissionData.RebellionCost;
                        NameIndex = 2186;
                        DescriptionIndex = 2187;
                        break;
                    }
            }
            if (EmpireManager.Player.Money < (float)cost || Component.Escreen.SelectedEmpire.data.Defeated || Component.SelectedAgent.Mission == AgentMission.Recovering)
            {
                Available = false;
            }
        }
	}
}
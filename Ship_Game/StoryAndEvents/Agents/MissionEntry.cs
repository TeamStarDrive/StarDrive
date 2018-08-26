using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class MissionEntry : UIElementContainer
	{
		private bool MissionAvailable;
	    private readonly UIButton DoMission;
		private readonly AgentComponent Component;
	    private readonly AgentMission TheMission;
		private int cost;
	    private int turns;
	    private int NameIndex;
	    private int DescriptionIndex;

		public MissionEntry(AgentMission am, AgentComponent parent) : base(null, Vector2.Zero)
		{
			Component = parent;
			TheMission = am;
            DoMission = ButtonLow(0f, 0f, "Go", DoMission_OnClick);
		}

        private void DoMission_OnClick(UIButton button)
        {
            Component.SelectedAgent.AssignMission(TheMission, 
                EmpireManager.Player, Component.EspionageScreen.SelectedEmpire.data.Traits.Name);
            UpdateMissionAvailability();
        }

        public void Draw(SpriteBatch batch, Rectangle clickRect)
		{
            var cursor = new Vector2(clickRect.X, clickRect.Center.Y - Fonts.Arial12Bold.LineSpacing / 2);

            void DrawString(string text)
            {
                batch.DrawString(Fonts.Arial12Bold, text, cursor, MissionAvailable ? Color.White : Color.Gray);
            }

		    DrawString(Localizer.Token(NameIndex));
			cursor.X += 120f;

		    DrawString(turns + " turns");
			cursor.X += 70f;

			var smallmoney = new Rectangle((int)cursor.X, (int)cursor.Y - 3, 21, 20);
		    batch.Draw(ResourceManager.Texture("NewUI/icon_money"), smallmoney, Color.White);
			cursor.X += 25f;
		    
		    batch.DrawString(Fonts.Arial12Bold, cost.ToString(), cursor, (MissionAvailable ? Color.White : Color.Gray));


		    DoMission.X = smallmoney.X + 50;
		    DoMission.Y = (int)cursor.Y - 1;
            DoMission.Visible = MissionAvailable;

		    base.Draw(batch);

            batch.DrawLine(new Vector2(clickRect.X,     clickRect.Bottom),
		                   new Vector2(clickRect.Right, clickRect.Bottom), Color.OrangeRed);
		}

        public bool HandleInput(InputState input, bool entryIsHovered)
        {
            if (entryIsHovered)
            {
                ToolTip.CreateTooltip(!DoMission.Rect.HitTest(input.CursorPosition)
                    ? Localizer.Token(DescriptionIndex)
                    : Localizer.Token(2198));
            }
            return base.HandleInput(input);
        }

        private bool IsRivalEmpire => Component.EspionageScreen.SelectedEmpire != EmpireManager.Player;

        private bool SelectedAgentAvailable => Component.SelectedAgent.Mission == AgentMission.Defending
                                            || Component.SelectedAgent.Mission == AgentMission.Undercover;

        //added by gremlin deveks missionInit
        public void UpdateMissionAvailability()
        {
            MissionAvailable = IsRivalEmpire && SelectedAgentAvailable;
            switch (TheMission)
            {
                case AgentMission.Training:
                    MissionAvailable = SelectedAgentAvailable;
                    turns = ResourceManager.AgentMissionData.TrainingTurns;
                    cost  = ResourceManager.AgentMissionData.TrainingCost;
                    NameIndex        = 2196;
                    DescriptionIndex = 2197;
                    break;
                case AgentMission.Infiltrate:
                    turns = ResourceManager.AgentMissionData.InfiltrateTurns;
                    cost  = ResourceManager.AgentMissionData.InfiltrateCost;
                    NameIndex        = 2188;
                    DescriptionIndex = 2189;
                    break;
                case AgentMission.Assassinate:
                    turns = ResourceManager.AgentMissionData.AssassinateTurns;
                    cost  = ResourceManager.AgentMissionData.AssassinateCost;
                    NameIndex        = 2184;
                    DescriptionIndex = 2185;
                    break;
                case AgentMission.Sabotage:
                    turns = ResourceManager.AgentMissionData.SabotageTurns;
                    cost  = ResourceManager.AgentMissionData.SabotageCost;
                    NameIndex        = 2190;
                    DescriptionIndex = 2191;
                    break;
                case AgentMission.StealTech:
                    turns = ResourceManager.AgentMissionData.StealTechTurns;
                    cost  = ResourceManager.AgentMissionData.StealTechCost;
                    NameIndex        = 2194;
                    DescriptionIndex = 2195;
                    break;
                case AgentMission.Robbery:
                    turns = ResourceManager.AgentMissionData.RobberyTurns;
                    cost  = ResourceManager.AgentMissionData.RobberyCost;
                    NameIndex        = 2192;
                    DescriptionIndex = 2193;
                    break;
                case AgentMission.InciteRebellion:
                    turns = ResourceManager.AgentMissionData.RebellionTurns;
                    cost  = ResourceManager.AgentMissionData.RebellionCost;
                    NameIndex        = 2186;
                    DescriptionIndex = 2187;
                    break;
            }

            if (EmpireManager.Player.Money < cost || 
                Component.EspionageScreen.SelectedEmpire.data.Defeated || 
                Component.SelectedAgent.Mission == AgentMission.Recovering)
            {
                MissionAvailable = false;
            }
        }
	}
}
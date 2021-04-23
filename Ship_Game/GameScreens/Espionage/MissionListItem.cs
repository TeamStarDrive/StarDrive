using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms.VisualStyles;

namespace Ship_Game.GameScreens.Espionage
{
    public sealed class MissionListItem : ScrollListItem<MissionListItem>
    {
        bool MissionAvailable;
        readonly UIButton DoMission;
        readonly AgentComponent Component;
        readonly AgentMission TheMission;
        int cost;
        int turns;
        GameText NameIndex;
        GameText DescriptionIndex;

        public MissionListItem(AgentMission am, AgentComponent parent)
        {
            Component = parent;
            TheMission = am;
            DoMission = ButtonLow(0f, 0f, "Go", DoMission_OnClick);
            UpdateMissionAvailability();
        }

        void DoMission_OnClick(UIButton button)
        {
            if (Component.SelectedAgent.Mission == AgentMission.Defending || Component.SelectedAgent.Mission == AgentMission.Undercover)
            {
                Component.SelectedAgent.AssignMission(TheMission,
                    EmpireManager.Player, Component.EspionageScreen.SelectedEmpire.data.Traits.Name);
            }
            UpdateMissionAvailability();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            var cursor = new Vector2(X, CenterY - Fonts.Arial12Bold.LineSpacing / 2);

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

            base.Draw(batch, elapsed);

            batch.DrawLine(BotLeft, BotRight, Color.OrangeRed);
        }

        public override bool HandleInput(InputState input)
        {
            bool captured = base.HandleInput(input);
            if (Hovered)
            {
                ToolTip.CreateTooltip(!DoMission.Rect.HitTest(input.CursorPosition)
                    ? Localizer.Token(DescriptionIndex)
                    : Localizer.Token(GameText.ExecuteThisMissionForThe));
            }
            return captured;
        }

        bool IsRivalEmpire => Component.EspionageScreen.SelectedEmpire != EmpireManager.Player;

        bool SelectedAgentAvailable => Component.SelectedAgent?.Mission == AgentMission.Defending
                                            || Component.SelectedAgent?.Mission == AgentMission.Undercover;

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
                    NameIndex        = GameText.AgentTraining;
                    DescriptionIndex = GameText.SendThisAgentOnA7;
                    break;
                case AgentMission.Infiltrate:
                    turns = ResourceManager.AgentMissionData.InfiltrateTurns;
                    cost  = ResourceManager.AgentMissionData.InfiltrateCost;
                    NameIndex        = GameText.Infiltrate;
                    DescriptionIndex = GameText.SendThisAgentOnA3;
                    break;
                case AgentMission.Assassinate:
                    turns = ResourceManager.AgentMissionData.AssassinateTurns;
                    cost  = ResourceManager.AgentMissionData.AssassinateCost;
                    NameIndex        = GameText.Assassinate;
                    DescriptionIndex = GameText.SendThisAgentOnA;
                    break;
                case AgentMission.Sabotage:
                    turns = ResourceManager.AgentMissionData.SabotageTurns;
                    cost  = ResourceManager.AgentMissionData.SabotageCost;
                    NameIndex        = GameText.Sabotage;
                    DescriptionIndex = GameText.SendThisAgentOnA4;
                    break;
                case AgentMission.StealTech:
                    turns = ResourceManager.AgentMissionData.StealTechTurns;
                    cost  = ResourceManager.AgentMissionData.StealTechCost;
                    NameIndex        = GameText.StealTech;
                    DescriptionIndex = GameText.SendThisAgentOnA6;
                    break;
                case AgentMission.Robbery:
                    turns = ResourceManager.AgentMissionData.RobberyTurns;
                    cost  = ResourceManager.AgentMissionData.RobberyCost;
                    NameIndex        = GameText.Robbery;
                    DescriptionIndex = GameText.SendThisAgentOnA5;
                    break;
                case AgentMission.InciteRebellion:
                    turns = ResourceManager.AgentMissionData.RebellionTurns;
                    cost  = ResourceManager.AgentMissionData.RebellionCost;
                    NameIndex        = GameText.InciteRebels;
                    DescriptionIndex = GameText.SendThisAgentOnA2;
                    break;
            }

            if (EmpireManager.Player.Money < cost 
                || Component.EspionageScreen.SelectedEmpire.data.Defeated 
                || Component.SelectedAgent?.Mission != AgentMission.Defending && Component.SelectedAgent?.Mission != AgentMission.Undercover
                || TheMission == AgentMission.Training && Component.SelectedAgent?.Level >= 3)
            {
                MissionAvailable = false;
            }
        }
    }
}

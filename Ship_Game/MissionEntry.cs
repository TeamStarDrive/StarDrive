using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class MissionEntry
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
			this.DoMission = new UIButton()
			{
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px_pressed"],
				Text = "Go",
				Launches = "New Campaign"
			};
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Rectangle clickRect)
		{
			Vector2 Cursor = new Vector2((float)clickRect.X, (float)(clickRect.Y + clickRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(this.NameIndex), Cursor, (this.Available ? Color.White : Color.Gray));
			Cursor.X = Cursor.X + 120f;
			HelperFunctions.ClampVectorToInt(ref Cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.turns, " turns"), Cursor, (this.Available ? Color.White : Color.Gray));
			Cursor.X = Cursor.X + 70f;
			Rectangle smallmoney = new Rectangle((int)Cursor.X, (int)Cursor.Y - 3, 21, 20);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], smallmoney, Color.White);
			Cursor.X = (float)(smallmoney.X + 25);
			HelperFunctions.ClampVectorToInt(ref Cursor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.cost.ToString(), Cursor, (this.Available ? Color.White : Color.Gray));
			if (this.Available)
			{
				this.DoMission.Rect = new Rectangle(smallmoney.X + 50, (int)Cursor.Y - 1, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_80px"].Height);
				this.DoMission.DrawLowRes(ScreenManager.SpriteBatch);
			}
			Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2((float)clickRect.X, (float)(clickRect.Y + clickRect.Height)), new Vector2((float)(clickRect.X + clickRect.Width), (float)(clickRect.Y + clickRect.Height)), Color.OrangeRed);
		}

		public void HandleInputORIG(InputState input)
		{
			if (this.Available)
			{
				if (!HelperFunctions.CheckIntersection(this.DoMission.Rect, input.CursorPosition))
				{
					this.DoMission.State = UIButton.PressState.Normal;
				}
				else
				{
					if (this.DoMission.State != UIButton.PressState.Hover && this.DoMission.State != UIButton.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					this.DoMission.State = UIButton.PressState.Hover;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
					{
						this.DoMission.State = UIButton.PressState.Pressed;
					}
					if (input.InGameSelect)
					{
                        this.Component.SelectedAgent.AssignMission(this.TheMission, EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty), this.Component.Escreen.SelectedEmpire.data.Traits.Name);
						return;
					}
				}
			}
		}
        //added by gremlin MissionHandleInput
        public void HandleInput(InputState input)
        {
            //if (this.Available)
            {
                if (!HelperFunctions.CheckIntersection(this.DoMission.Rect, input.CursorPosition))
                {
                    this.DoMission.State = UIButton.PressState.Normal;

                }
                else
                {
                    if (this.DoMission.State != UIButton.PressState.Hover && this.DoMission.State != UIButton.PressState.Pressed)
                    {
                        AudioManager.PlayCue("mouse_over4");
                    }
                    this.DoMission.State = UIButton.PressState.Hover;
                    if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    {
                        this.DoMission.State = UIButton.PressState.Pressed;
                    }
                    if (input.InGameSelect)
                    {
                        this.Component.SelectedAgent.AssignMission(this.TheMission, EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty), this.Component.Escreen.SelectedEmpire.data.Traits.Name);
                        return;
                    }
                }
            }
        }
		public void InitializeORIG()
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
					this.turns = 25;
					this.cost = 50;
					this.NameIndex = 2196;
					this.DescriptionIndex = 2197;
					break;
				}
				case AgentMission.Infiltrate:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 30;
					this.cost = 75;
					this.NameIndex = 2188;
					this.DescriptionIndex = 2189;
					break;
				}
				case AgentMission.Assassinate:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 50;
					this.cost = 75;
					this.NameIndex = 2184;
					this.DescriptionIndex = 2185;
					break;
				}
				case AgentMission.Sabotage:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 30;
					this.cost = 75;
					this.NameIndex = 2190;
					this.DescriptionIndex = 2191;
					break;
				}
				case AgentMission.StealTech:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 50;
					this.cost = 250;
					this.NameIndex = 2194;
					this.DescriptionIndex = 2195;
					break;
				}
				case AgentMission.Robbery:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 30;
					this.cost = 50;
					this.NameIndex = 2192;
					this.DescriptionIndex = 2193;
					break;
				}
				case AgentMission.InciteRebellion:
				{
                    if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
					{
						this.Available = true;
					}
					this.turns = 100;
					this.cost = 250;
					this.NameIndex = 2186;
					this.DescriptionIndex = 2187;
					break;
				}
			}
			if (EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).Money < (float)this.cost)
			{
				this.Available = false;
			}
            if (this.Component.Escreen.SelectedEmpire.data.Defeated)
			{
				this.Available = false;
			}
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
                        if (this.Component.Escreen.SelectedEmpire != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && (this.Component.SelectedAgent.Mission == AgentMission.Defending || this.Component.SelectedAgent.Mission == AgentMission.Undercover))
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
            if (EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).Money < (float)this.cost || this.Component.Escreen.SelectedEmpire.data.Defeated || this.Component.SelectedAgent.Mission == AgentMission.Recovering)
            {
                this.Available = false;
            }
        }
	}
}
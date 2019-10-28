using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class EspionageScreen : GameScreen
    {
        public Empire SelectedEmpire;
        AgentComponent Agents;
        static readonly Color PanelBackground = new Color(23, 20, 14);

        public EspionageScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;

            //DebugDraw = true;
        }

        public override void LoadContent()
        {
            var titleRect = new Rectangle(ScreenWidth / 2 - 200, 44, 400, 80);
            Add(new Menu2(this, titleRect));

            if (ScreenHeight > 766)
            {
                Add(new Menu2(this, titleRect));

                // "Espionage"
                string espionage = Localizer.Token(6089);
                var titlePos = new Vector2(titleRect.Center.X - Fonts.Laserian14.MeasureString(espionage).X / 2f, 
                                           titleRect.Center.Y - Fonts.Laserian14.LineSpacing / 2);
                Label(titlePos, espionage, Fonts.Laserian14, new Color(255, 239, 208));
            }


            var ourRect = new Rectangle(ScreenWidth / 2 - 640, (ScreenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1280, 660);
            Add(new Menu2(this, ourRect));

            CloseButton(ourRect.Right - 40, ourRect.Y + 20);

            var agentsRect     = new Rectangle(ourRect.X + 60,         ourRect.Y + 250, 368, 376);
            var dossierRect    = new Rectangle(agentsRect.Right + 30,  agentsRect.Y,    368, 376);
            var operationsRect = new Rectangle(dossierRect.Right + 30, agentsRect.Y,    368, 376);

            Add(new EmpiresPanel(this, ourRect, operationsRect));
            Add(new AgentsPanel(this, agentsRect));
            Add(new DossierPanel(this, dossierRect));
            Add(new OperationsPanel(this, operationsRect));

            var agentComponentRect = new Rectangle(agentsRect.X + 20, agentsRect.Y + 35, agentsRect.Width - 40, agentsRect.Height - 95);
            Agents = Add(new AgentComponent(this, agentComponentRect, operationsRect));

            GameAudio.MuteRacialMusic();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();

            base.Draw(batch);

            batch.End();
        }

        static float GetEspionageDefense(Empire e)
        {
            float espionageDefense = 0f;
            foreach (Agent agent in e.data.AgentList)
            {
                if (agent.Mission == AgentMission.Defending)
                    espionageDefense += agent.Level * ResourceManager.AgentMissionData.DefenceLevelBonus;
            }
            espionageDefense /= e.NumPlanets / 3 + 1;
            espionageDefense += e.data.SpyModifier + e.data.DefensiveSpyBonus;
            return espionageDefense;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.RightMouseClick ||
                (input.KeyPressed(Keys.E) && !GlobalStats.TakingInput))
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////


        class EmpiresPanel : UIElementContainer
        {
            readonly EspionageScreen Screen;
            //readonly ScrollList<OperationsListItem> OperationsSL;

            //class OperationsListItem : ScrollList<OperationsListItem>.Entry
            //{
            //    public Operation Operation;
            //}

            public EmpiresPanel(EspionageScreen screen, Rectangle rect, Rectangle operationsRect) : base(screen, rect)
            {
                Screen = screen;

                //var opsRect = new Rectangle(operationsRect.X + 20, operationsRect.Y + 20, 
                //                            operationsRect.Width - 40, operationsRect.Height - 45);
                //OperationsSL = new ScrollList<OperationsListItem>(new Submenu(opsRect), Fonts.Arial12Bold.LineSpacing + 5);

                var empires = new Array<Empire>();
                foreach (Empire e in EmpireManager.Empires)
                    if (!e.isFaction) empires.Add(e);

                float x = Screen.ScreenWidth / 2f - (148f * empires.Count) / 2f;
                Pos = new Vector2(x, rect.Y + 10);

                UIList list = AddList(new Vector2(Pos.X + 10, rect.Y + 40));
                list.Padding = new Vector2(10f, 10f);
                list.LayoutStyle = ListLayoutStyle.Resize;
                list.Direction = new Vector2(1f, 0f);

                foreach (Empire e in empires)
                    list.Add(new EmpireButton(screen, e, new Rectangle(0, 0, 134, 148), OnEmpireSelected));

                Size = new Vector2(list.Width, 188);
                Screen.SelectedEmpire = EmpireManager.Player;
            }

            void OnEmpireSelected(EmpireButton button)
            {
                if (EmpireManager.Player == button.Empire || EmpireManager.Player.GetRelations(button.Empire).Known)
                {
                    Screen.SelectedEmpire = button.Empire;
                    //if (EmpireManager.Player == button.Empire)
                    //{
                    //    foreach (OperationsListItem item in OperationsSL.AllEntries)
                    //        item.Operation.Selected = false;
                    //}
                    Screen.Agents.Reinitialize();
                }
            }
        }

        class EmpireButton : UIElementV2
        {
            public readonly Empire Empire;
            readonly EspionageScreen Screen;
            readonly Action<EmpireButton> OnClick;

            public EmpireButton(EspionageScreen screen, Empire e, Rectangle rect, Action<EmpireButton> onClick) : base(null, rect)
            {
                Empire = e;
                Screen = screen;
                OnClick = onClick;
            }

            public override bool HandleInput(InputState input)
            {
                if (input.InGameSelect && Rect.HitTest(input.CursorPosition))
                {
                    GameAudio.EchoAffirmative();
                    OnClick(this);
                    return true;
                }
                return false;
            }

            public override void Draw(SpriteBatch batch)
            {
                // red background:
                if (EmpireManager.Player != Empire && EmpireManager.Player.GetRelations(Empire).AtWar && !Empire.data.Defeated)
                {
                    batch.FillRectangle(Rect.Bevel(2), Color.Red);
                }

                void DrawRacePortrait()
                {
                    batch.Draw(ResourceManager.Texture("Portraits/" + Empire.data.PortraitName), Rect, Color.White);
                    batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), Rect, Color.White);

                    Vector2 size = Fonts.Arial12Bold.MeasureString(Empire.data.Traits.Name);
                    var nameCursor = new Vector2(Rect.X + 62 - size.X / 2f, Rect.Y + 148 + 8);
                    batch.DrawString(Fonts.Arial12Bold, Empire.data.Traits.Name, nameCursor, Color.White);
                }

                if (Empire.data.Defeated)
                {
                    DrawRacePortrait();

                    if (Empire.data.AbsorbedBy == null)
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/x_red"), Rect, Color.White);
                    }
                    else
                    {
                        var r = new Rectangle(Rect.X, Rect.Y, 124, 124);
                        var e = EmpireManager.GetEmpireByName(Empire.data.AbsorbedBy);
                        batch.Draw(ResourceManager.Flag(e.data.Traits.FlagIndex), r, e.EmpireColor);
                    }
                }
                else if (EmpireManager.Player == Empire || EmpireManager.Player.GetRelations(Empire).Known)
                {
                    DrawRacePortrait();

                    SubTexture shield = ResourceManager.Texture("UI/icon_shield");

                    // Added by McShooterz: Display Spy Defense value
                    var defenseIcon = new Rectangle(Rect.Center.X - shield.Width, Rect.Y + Fonts.Arial12.LineSpacing + 164, shield.Width, shield.Height);
                    batch.Draw(shield, defenseIcon, Color.White);

                    float espionageDefense = GetEspionageDefense(Empire);
                    var defPos = new Vector2(defenseIcon.Right + 2, defenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, espionageDefense.String(1), defPos, Color.White);

                    if (defenseIcon.HitTest(Screen.Input.CursorPosition))
                        ToolTip.CreateTooltip(Localizer.Token(7031));
                }
                else if (EmpireManager.Player != Empire)
                {
                    batch.Draw(ResourceManager.Texture("Portraits/unknown"), Rect, Color.White);
                }

                if (Empire == Screen.SelectedEmpire)
                    batch.DrawRectangle(Rect, Color.Orange);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////

        class AgentsPanel : UIPanel
        {
            public AgentsPanel(EspionageScreen screen, Rectangle rect) : base(screen, rect, PanelBackground)
            {
                Label(rect.X + 20, rect.Y + 10, 6090, Fonts.Arial20Bold);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        class DossierPanel : UIPanel
        {
            readonly EspionageScreen Screen;
            public DossierPanel(EspionageScreen screen, Rectangle rect) : base(screen, rect, PanelBackground)
            {
                Screen = screen;
                Label(rect.X + 20, rect.Y + 10, 6092, Fonts.Arial20Bold);
            }
            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);

                Agent agent = Screen.Agents.SelectedAgent;
                if (agent == null)
                    return;

                var cursor = new Vector2(X + 20, Y + 10);

                void DrawText(int token, string text, Color color)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(token) + text, cursor, color);
                    cursor.Y += (Fonts.Arial12Bold.LineSpacing + 4);
                }

                void DrawValue(int token, short value)
                {
                    DrawText(token, value.ToString(), value > 0 ? Color.White : Color.LightGray);
                }

                // @todo Why is this here?
                if (agent.HomePlanet.IsEmpty())
                    agent.HomePlanet = EmpireManager.Player.data.Traits.HomeworldName;

                cursor.Y += 24;
                DrawText(6108, agent.Name, Color.Orange);
                cursor.Y += 4;
                DrawText(6109, agent.HomePlanet, Color.LightGray);
                DrawText(6110, agent.Age.String(0), Color.LightGray);
                DrawText(6111, agent.ServiceYears.String(1) + Localizer.Token(6119), Color.LightGray);
                cursor.Y += 16;
                DrawValue(6112, agent.Training);
                DrawValue(6113, agent.Assassinations);
                DrawValue(6114, agent.Infiltrations);
                DrawValue(6115, agent.Sabotages);
                DrawValue(6116, agent.TechStolen);
                DrawValue(6117, agent.Robberies);
                DrawValue(6118, agent.Rebellions);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        class OperationsPanel : UIPanel
        {
            readonly EspionageScreen Screen;
            readonly UILabel AgentName;
            readonly UILabel AgentLevel;
            public OperationsPanel(EspionageScreen screen, Rectangle rect) : base(screen, rect, PanelBackground)
            {
                Screen = screen;
                AgentName  = Label(rect.X + 20, rect.Y + 10, "", Fonts.Arial20Bold);
                AgentLevel = Label(AgentName.X, AgentName.Y + Fonts.Arial20Bold.LineSpacing + 2, "", Fonts.Arial12Bold);
                AgentName.DropShadow = true;
                AgentLevel.Color = Color.Gray;
            }
            public override void Draw(SpriteBatch batch)
            {
                Agent agent = Screen.Agents.SelectedAgent;
                AgentName.Visible = agent != null;
                AgentLevel.Visible = agent != null;
                if (agent != null)
                {
                    AgentName.Text  = agent.Name;
                    AgentLevel.Text = $"Level {agent.Level} Agent";
                }
                base.Draw(batch);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class EspionageScreen : GameScreen
    {
        public Empire SelectedEmpire;
        private AgentComponent Agents;
        private static readonly Color PanelBackground = new Color(23, 20, 14);

        public EspionageScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);

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
            Agents = new AgentComponent(this, agentComponentRect, operationsRect);

            GameAudio.MuteRacialMusic();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();

            base.Draw(batch);

            if (IsActive)
                ToolTip.Draw(batch); // draw current tooltip

            batch.End();
        }

        private static float GetEspionageDefense(Empire e)
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
                (input.WasKeyPressed(Keys.E) && !GlobalStats.TakingInput))
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////


        private class EmpiresPanel : UIElementV2
        {
            private readonly EspionageScreen Screen;
            private readonly Array<RaceEntry> Races = new Array<RaceEntry>();
            private readonly ScrollList OperationsSL;

            public EmpiresPanel(EspionageScreen screen, Rectangle rect, Rectangle operationsRect) : base(screen, rect)
            {
                Screen = screen;

                var opsRect = new Rectangle(operationsRect.X + 20, operationsRect.Y + 20, operationsRect.Width - 40, operationsRect.Height - 45);
                OperationsSL = new ScrollList(new Submenu(opsRect), Fonts.Arial12Bold.LineSpacing + 5);

                foreach (Empire e in EmpireManager.Empires)
                {
                    if (e != EmpireManager.Player)
                    {
                        if (e.isFaction)
                            continue;
                    }
                    else
                    {
                        Screen.SelectedEmpire = e;
                    }
                    Races.Add(new RaceEntry { e = e });
                }

                var cursor = new Vector2(Screen.ScreenWidth / 2f - 148f * Races.Count / 2, rect.Y + 10);
                base.Pos = cursor;
                base.Size = new Vector2(Races.Count * 148, 188);
                int j = 0;
                foreach (RaceEntry re in Races)
                {
                    re.container = new Rectangle((int)cursor.X + 10 + j * 148, rect.Y + 40, 124, 148);
                    ++j;
                }
            }

            public override void Draw(SpriteBatch batch)
            {
                foreach (RaceEntry race in Races)
                    if (!race.e.isFaction)
                        DrawRaceEntry(batch, race);
            }

            public override bool HandleInput(InputState input)
            {
                if (input.InGameSelect && HandleSelectEmpire(input))
                {
                    Screen.Agents.Reinitialize();
                    return true;
                }
                return false;
            }

            private bool HandleSelectEmpire(InputState input)
            {
                foreach (RaceEntry race in Races)
                {
                    if (EmpireManager.Player == race.e || !EmpireManager.Player.GetRelations(race.e).Known)
                    {
                        if (EmpireManager.Player == race.e && race.container.HitTest(input))
                        {
                            Screen.SelectedEmpire = race.e;
                            GameAudio.PlaySfxAsync("echo_affirm");
                            foreach (ScrollList.Entry f in OperationsSL.VisibleEntries)
                                f.Get<Operation>().Selected = false;
                            return true;
                        }
                    }
                    else
                    {
                        if (race.container.HitTest(input))
                        {
                            Screen.SelectedEmpire = race.e;
                            return true;
                        }
                    }
                }
                return false;
            }

            private void DrawRaceEntry(SpriteBatch batch, RaceEntry race)
            {
                // red background:
                if (EmpireManager.Player != race.e && EmpireManager.Player.GetRelations(race.e).AtWar && !race.e.data.Defeated)
                {
                    var war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4,
                        race.container.Height + 4);
                    batch.FillRectangle(war, Color.Red);
                }

                void DrawRacePortrait()
                {
                    var nameCursor =
                        new Vector2(race.container.X + 62 - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f,
                            race.container.Y + 148 + 8);
                    batch.Draw(ResourceManager.Texture("Portraits/" + race.e.data.PortraitName), race.container, Color.White);
                    batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    batch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, nameCursor, Color.White);
                }

                if (race.e.data.Defeated)
                {
                    DrawRacePortrait();

                    if (race.e.data.AbsorbedBy == null)
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/x_red"), race.container, Color.White);
                    }
                    else
                    {
                        var r = new Rectangle(race.container.X, race.container.Y, 124, 124);
                        KeyValuePair<string, Texture2D> item =
                            ResourceManager.FlagTextures[
                                EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).data.Traits.FlagIndex];
                        batch.Draw(item.Value, r, EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).EmpireColor);
                    }
                }
                else if (EmpireManager.Player == race.e || EmpireManager.Player.GetRelations(race.e).Known)
                {
                    DrawRacePortrait();

                    // Added by McShooterz: Display Spy Defense value
                    var defenseIcon = new Rectangle(race.container.X + 62, race.container.Y + (Fonts.Arial12.LineSpacing) + 164,
                        ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                    batch.Draw(ResourceManager.Texture("UI/icon_shield"), defenseIcon, Color.White);

                    float espionageDefense = GetEspionageDefense(race.e);
                    var defPos = new Vector2(defenseIcon.X + defenseIcon.Width + 2,
                        defenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, espionageDefense.String(1), defPos, Color.White);

                    if (defenseIcon.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                        ToolTip.CreateTooltip(Localizer.Token(7031));
                }
                else if (EmpireManager.Player != race.e)
                {
                    batch.Draw(ResourceManager.Texture("Portraits/unknown"), race.container, Color.White);
                }

                if (race.e == Screen.SelectedEmpire)
                    batch.DrawRectangle(race.container, Color.Orange);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private class AgentsPanel : UIPanel
        {
            private readonly EspionageScreen Screen;
            public AgentsPanel(EspionageScreen screen, Rectangle rect) : base(screen, rect, PanelBackground)
            {
                Screen = screen;
                Label(rect.X + 20, rect.Y + 10, 6090, Fonts.Arial20Bold);
            }
            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private class DossierPanel : UIPanel
        {
            private readonly EspionageScreen Screen;
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

        private class OperationsPanel : UIPanel
        {
            private readonly EspionageScreen Screen;
            private readonly UILabel AgentName;
            private readonly UILabel AgentLevel;
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class EspionageScreen : GameScreen
    {
        private UniverseScreen screen;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 DMenu;

        public bool LowRes;

        public Rectangle SelectedInfoRect;

        public Rectangle IntelligenceRect;

        public Rectangle OperationsRect;

        public Empire SelectedEmpire;

        private Array<RaceEntry> Races = new Array<RaceEntry>();

        private GenericSlider SpyBudgetSlider;

        private GenericSlider CounterSpySlider;

        private Rectangle OpSLRect;

        private ScrollList OperationsSL;

        private DanButton ExecuteOperation;

        private AgentComponent AgentComponent;

        private CloseButton close;

        private float TransitionElapsedTime;

        public EspionageScreen(UniverseScreen screen) : base(screen)
        {
            this.screen = screen;
            IsPopup = true;
        
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 766)
            {
                TitleBar.Draw();
                ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(6089), TitlePos, new Color(255, 239, 208));
            }
            DMenu.Draw();
            Color color = new Color(118, 102, 67, 50);
            foreach (RaceEntry race in Races)
            {
                if (race.e.isFaction)
                {
                    continue;
                }
                Vector2 NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
                if (race.e.data.Defeated)
                {
                    if (race.e.data.AbsorbedBy == null)
                    {
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/x_red"), race.container, Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                        Rectangle r = new Rectangle(race.container.X, race.container.Y, 124, 124);
                        KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).data.Traits.FlagIndex];
                        batch.Draw(item.Value, r, EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).EmpireColor);
                    }
                }
                else if (EmpireManager.Player != race.e && EmpireManager.Player.GetRelations(race.e).Known)
                {
                    if (EmpireManager.Player.GetRelations(race.e).AtWar && !race.e.data.Defeated)
                    {
                        Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
                        ScreenManager.SpriteBatch.FillRectangle(war, Color.Red);
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                    //Added by McShooterz: Display Spy Defense value
                    Rectangle EspionageDefenseIcon = new Rectangle(race.container.X + 62, race.container.Y + (Fonts.Arial12.LineSpacing) + 164, ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), EspionageDefenseIcon, Color.White);
                    Vector2 defPos = new Vector2((float)(EspionageDefenseIcon.X + EspionageDefenseIcon.Width + 2), (float)(EspionageDefenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                    float EspionageDefense = 0f;
                    foreach(Agent agent in race.e.data.AgentList)
                    {
                        if(agent.Mission == AgentMission.Defending)
                            EspionageDefense += agent.Level * ResourceManager.AgentMissionData.DefenceLevelBonus;
                    }
                    EspionageDefense /= race.e.GetPlanets().Count / 3 + 1;
                    EspionageDefense += race.e.data.SpyModifier + race.e.data.DefensiveSpyBonus;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, EspionageDefense.ToString("0."), defPos, Color.White);
                    if (EspionageDefenseIcon.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                    {
                        ToolTip.CreateTooltip(Localizer.Token(7031));
                    }
                }
                else if (EmpireManager.Player != race.e)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/unknown"), race.container, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                    //Added by McShooterz: Display Spy Defense value
                    Rectangle EspionageDefenseIcon = new Rectangle(race.container.X + 62, race.container.Y + (Fonts.Arial12.LineSpacing) + 164, ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), EspionageDefenseIcon, Color.White);
                    Vector2 defPos = new Vector2((float)(EspionageDefenseIcon.X + EspionageDefenseIcon.Width + 2), (float)(EspionageDefenseIcon.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                    float EspionageDefense = 0f;
                    foreach (Agent agent in race.e.data.AgentList)
                    {
                        if (agent.Mission == AgentMission.Defending)
                            EspionageDefense += agent.Level * ResourceManager.AgentMissionData.DefenceLevelBonus;
                    }
                    EspionageDefense /= race.e.GetPlanets().Count / 3 + 1;
                    EspionageDefense += race.e.data.SpyModifier + race.e.data.DefensiveSpyBonus;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, EspionageDefense.ToString("0."), defPos, Color.White);
                    if (EspionageDefenseIcon.HitTest(new Vector2(Mouse.GetState().X,Mouse.GetState().Y)))
                    {
                        ToolTip.CreateTooltip(Localizer.Token(7031));
                    }
                }
                if (race.e != SelectedEmpire)
                {
                    continue;
                }
                ScreenManager.SpriteBatch.DrawRectangle(race.container, Color.Orange);
            }
            ScreenManager.SpriteBatch.FillRectangle(SelectedInfoRect, new Color(23, 20, 14));
            ScreenManager.SpriteBatch.FillRectangle(IntelligenceRect, new Color(23, 20, 14));
            ScreenManager.SpriteBatch.FillRectangle(OperationsRect, new Color(23, 20, 14));
            Vector2 TextCursor = new Vector2((float)(SelectedInfoRect.X + 20), (float)(SelectedInfoRect.Y + 10));
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 4);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(6090), TextCursor, Fonts.Arial20Bold);
            TextCursor.X = IntelligenceRect.X + 20;
            TextCursor.Y = TextCursor.Y - (float)(Fonts.Arial20Bold.LineSpacing + 4);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(6092), TextCursor, Fonts.Arial20Bold);
            //Agent Dossier information
            if (AgentComponent.SelectedAgent != null)
            {
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 25);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(6108), AgentComponent.SelectedAgent.Name), TextCursor, Color.Orange);
                //if (this.AgentComponent.SelectedAgent.HomePlanet == "" || this.AgentComponent.SelectedAgent.HomePlanet == null) 
                if (string.IsNullOrEmpty(AgentComponent.SelectedAgent.HomePlanet))
                    AgentComponent.SelectedAgent.HomePlanet = EmpireManager.Player.data.Traits.HomeworldName; 
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 6);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6109), AgentComponent.SelectedAgent.HomePlanet), TextCursor, Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6110), AgentComponent.SelectedAgent.Age.ToString("0.0"), Localizer.Token(6119)), TextCursor, Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6111), AgentComponent.SelectedAgent.ServiceYears.ToString("0.0"), Localizer.Token(6119)), TextCursor, Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 20);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6112), AgentComponent.SelectedAgent.Training), TextCursor, AgentComponent.SelectedAgent.Training > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6113), AgentComponent.SelectedAgent.Assassinations), TextCursor, AgentComponent.SelectedAgent.Assassinations > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6114), AgentComponent.SelectedAgent.Infiltrations), TextCursor, AgentComponent.SelectedAgent.Infiltrations > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6115), AgentComponent.SelectedAgent.Sabotages), TextCursor, AgentComponent.SelectedAgent.Sabotages > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6116), AgentComponent.SelectedAgent.TechStolen), TextCursor, AgentComponent.SelectedAgent.TechStolen > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6117), AgentComponent.SelectedAgent.Robberies), TextCursor, AgentComponent.SelectedAgent.Robberies > 0 ? Color.White : Color.LightGray);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 3);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6118), AgentComponent.SelectedAgent.Rebellions), TextCursor, AgentComponent.SelectedAgent.Rebellions > 0 ? Color.White : Color.LightGray);
            }
            //End of dossier
            AgentComponent.Draw(ScreenManager.SpriteBatch);
            if (AgentComponent.SelectedAgent != null)
            {
                TextCursor = new Vector2((float)(OperationsRect.X + 20), (float)(OperationsRect.Y + 10));
                HelperFunctions.DrawDropShadowText(ScreenManager, AgentComponent.SelectedAgent.Name, TextCursor, Fonts.Arial20Bold);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Level ", AgentComponent.SelectedAgent.Level.ToString(), " Agent"), TextCursor, Color.Gray);
            }
            close.Draw(batch);
            if (IsActive)
            {
                ToolTip.Draw(ScreenManager.SpriteBatch);
            }
            ScreenManager.SpriteBatch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }
            if (input.KeysCurr.IsKeyDown(Keys.E) && !input.KeysPrev.IsKeyDown(Keys.E) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            AgentComponent.HandleInput(input);
            //this.showExecuteButton = false;
            bool GotRace = false;
            foreach (RaceEntry race in Races)
            {
                if (EmpireManager.Player == race.e || !EmpireManager.Player.GetRelations(race.e).Known)
                {
                    if (EmpireManager.Player != race.e || !HelperFunctions.ClickedRect(race.container, input))
                    {
                        continue;
                    }
                    SelectedEmpire = race.e;
                    GotRace = true;
                    GameAudio.PlaySfxAsync("echo_affirm");
                    foreach (ScrollList.Entry f in OperationsSL.VisibleEntries)
                    {
                        ((Operation)f.item).Selected = false;
                    }
                }
                else
                {
                    if (!HelperFunctions.ClickedRect(race.container, input))
                    {
                        continue;
                    }
                    SelectedEmpire = race.e;
                    GotRace = true;
                }
            }
            
            if (!AgentComponent.ComponentRect.HitTest(input.CursorPosition))
            {
                if (input.InGameSelect && !GotRace)
                {
                    AgentComponent.SelectedAgent = null;
                }
                else if (input.InGameSelect && GotRace)
                {
                    AgentComponent.Reinitialize();
                }
            }
            if (input.Escaped || input.MouseCurr.RightButton == ButtonState.Pressed)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            float screenWidth = (float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            float screenHeight = (float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            Rectangle titleRect = new Rectangle((int)screenWidth / 2 - 200, 44, 400, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(6089)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            Rectangle leftRect = new Rectangle((int)screenWidth / 2 - 640, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1280, 660);
            DMenu = new Menu2(leftRect);
            close = new CloseButton(this, new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
            SelectedInfoRect = new Rectangle(leftRect.X + 60, leftRect.Y + 250, 368, 376);
            IntelligenceRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            OperationsRect = new Rectangle(IntelligenceRect.X + IntelligenceRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            OpSLRect = new Rectangle(OperationsRect.X + 20, OperationsRect.Y + 20, OperationsRect.Width - 40, OperationsRect.Height - 45);
            Submenu OpSub = new Submenu(OpSLRect);
            OperationsSL = new ScrollList(OpSub, Fonts.Arial12Bold.LineSpacing + 5);
            Vector2 ExecutePos = new Vector2((float)(OperationsRect.X + OperationsRect.Width / 2 - 91), (float)(OperationsRect.Y + OperationsRect.Height - 60));
            ExecuteOperation = new DanButton(ExecutePos, "Execute Op")
            {
                Toggled = true
            };
            Rectangle ComponentRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 35, SelectedInfoRect.Width - 40, SelectedInfoRect.Height - 95);
            AgentComponent = new AgentComponent(ComponentRect, this);
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e != EmpireManager.Player)
                {
                    if (e.isFaction)
                        continue;
                }
                else
                {
                    SelectedEmpire = e;
                }
                Races.Add(new RaceEntry { e = e });

            }
            Vector2 Cursor = new Vector2(screenWidth / 2f - (float)(148 * Races.Count / 2), (float)(leftRect.Y + 10));
            int j = 0;
            foreach (RaceEntry re in Races)
            {
                re.container = new Rectangle((int)Cursor.X + 10 + j * 148, leftRect.Y + 40, 124, 148);
                j++;
            }
            Rectangle rectangle = new Rectangle();
            SpyBudgetSlider = new GenericSlider(rectangle, Localizer.Token(1637), 0f, 100f)
            {
                amount = 0f
            };
            Rectangle rectangle1 = new Rectangle();
            CounterSpySlider = new GenericSlider(rectangle1, Localizer.Token(1637), 0f, 100f)
            {
                amount = 0f
            };
            GameAudio.MuteRacialMusic();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TransitionElapsedTime += elapsedTime;
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

    }
}

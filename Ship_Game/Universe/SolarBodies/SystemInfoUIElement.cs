using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;

namespace Ship_Game
{
    public sealed class SystemInfoUIElement : UIElement
    {
        public static SpriteFont SysFont;

        public static SpriteFont DataFont;

        private Rectangle SliderRect;

        private Rectangle clickRect;

        private UniverseScreen screen;

        private Rectangle LeftRect;

        private Rectangle RightRect;

        //private Rectangle PlanetIconRect;

        //private Rectangle flagRect;

        //private string PlanetTypeRichness;

        private Vector2 PlanetTypeCursor;

        public SolarSystem s;

        private Selector sel;

        private float ClickTimer;

        private float TimerDelay = 0.25f;

        public float SelectionTimer;

        private bool Hovering;

        private float HoverTimer;

        private Array<ClickMe> ClickList = new Array<ClickMe>();

        new private Color tColor = new Color(255, 239, 208);

        public SystemInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            sel = new Selector(r, Color.Black);
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
            clickRect = new Rectangle(ElementRect.X + ElementRect.Width - 16, ElementRect.Y + ElementRect.Height / 2 - 11, 11, 22);
            LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawInPosition(gameTime);
        }

        public void DrawInPosition(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            SystemInfoUIElement clickTimer = this;
            clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
            SystemInfoUIElement selectionTimer = this;
            selectionTimer.SelectionTimer = selectionTimer.SelectionTimer + elapsedTime;
            Vector3 pScreenSpace = screen.Viewport.Project(new Vector3(s.Position, 0f), screen.projection, screen.view, Matrix.Identity);
            Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
            Vector2 radialPos = new Vector2(s.Position.X + 4500f, s.Position.Y);
            Vector3 insetRadialPos = screen.Viewport.Project(new Vector3(radialPos, 0f), screen.projection, screen.view, Matrix.Identity);
            Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
            float pRadius = Vector2.Distance(insetRadialSS, pPos);
            if (pRadius < 5f)
            {
                pRadius = 5f;
            }
            Rectangle rectangle = new Rectangle((int)pPos.X - (int)pRadius, (int)pPos.Y - (int)pRadius, (int)pRadius * 2, (int)pRadius * 2);
            ScreenManager.SpriteBatch.BracketRectangle(pPos, pRadius, Color.White);
            float count = 0.4f / s.PlanetList.Count;
            if (SelectionTimer > 0.4f)
            {
                SelectionTimer = 0.4f;
            }
            Hovering = false;
            ClickList.Clear();
            float TransitionPosition = 1f - SelectionTimer / 0.4f;
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);
            if (s.IsExploredBy(EmpireManager.Player))
            {
                for (int i = 0; i < s.PlanetList.Count; i++)
                {
                    Vector2 planetPos = pPos.PointFromAngle(s.PlanetList[i].OrbitalAngle, 40 + 40 * i);
                    planetPos = planetPos - ((Vector2.Normalize(planetPos - pPos) * (40 + 40 * i)) * transitionOffset);
                    DrawCircle(pPos, pPos.Distance(planetPos), (s.PlanetList[i].Owner == null ? new Color(50, 50, 50, 90) : new Color(s.PlanetList[i].Owner.EmpireColor, 100)), 2f);
                }
                for (int i = 0; i < s.PlanetList.Count; i++)
                {
                    Planet planet = s.PlanetList[i];
                    Vector2 planetPos = pPos.PointFromAngle(s.PlanetList[i].OrbitalAngle, 40 + 40 * i);
                    planetPos = planetPos - ((Vector2.Normalize(planetPos - pPos) * (40 + 40 * i)) * transitionOffset);
                    float fIconScale = 1.0f + ((float)(Math.Log(s.PlanetList[i].Scale)));
                    Rectangle PlanetRect = new Rectangle((int)planetPos.X - (int)(16 * fIconScale / 2), (int)planetPos.Y - (int)(16 * fIconScale / 2), (int)(16 * fIconScale), (int)(16 * fIconScale));
                    if (PlanetRect.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                    {
                        Hovering = true;
                        int widthplus = (int)(4f * (HoverTimer / 0.2f));
                        PlanetRect = new Rectangle((int)planetPos.X - ((int)(16 * fIconScale / 2) + widthplus), (int)planetPos.Y - ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus));
                        ClickMe cm = new ClickMe
                        {
                            p = s.PlanetList[i],
                            r = PlanetRect
                        };
                        ClickList.Add(cm);
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Planets/", s.PlanetList[i].PlanetType)), PlanetRect, Color.White);
            
                    if (screen.SelectedPlanet == s.PlanetList[i])
                    {
                        ScreenManager.SpriteBatch.BracketRectangle(PlanetRect, (s.PlanetList[i].Owner != null ? s.PlanetList[i].Owner.EmpireColor : Color.Gray), 3);
                    }
                    Planet p = s.PlanetList[i];
                    PlanetTypeCursor = new Vector2(PlanetRect.X + PlanetRect.Width / 2 - SysFont.MeasureString(p.Name).X / 2f, PlanetRect.Y + PlanetRect.Height + 4);
                    HelperFunctions.ClampVectorToInt(ref PlanetTypeCursor);
                    bool hasAnamoly = false;
                    bool hasCommodities = false;
                    bool hastroops =false;
                    //bool hasEnemyTroop = false;          //Not referenced in code, removing to save memory
                    int playerTroops = 0;
                    int sideSpacing = 0;
                    if (p.IsExploredBy(EmpireManager.Player))
                    {
                        int j = 0;
                        #region replaced
                        //while (j < this.s.PlanetList[i].BuildingList.Count)
                        //{
                        //    if (this.s.PlanetList[i].BuildingList[j].EventTriggerUID == "")
                        //    {
                        //        j++;
                        //    }
                        //    else
                        //    {
                        //        hasAnamoly = true;
                        //        break;
                        //    }
                        //} 
                        #endregion

                        while (j < s.PlanetList[i].BuildingList.Count)
                        {
                            
                            Building building = s.PlanetList[i].BuildingList[j];
                            
                            if (!string.IsNullOrEmpty(building.EventTriggerUID))
                            {
                                hasAnamoly = true;
                            }
                            if (building.IsCommodity)
                            {
                                hasCommodities = true;
                            }
                            if (hasCommodities && hasAnamoly)
                                break;
                            j++;


                        }
                        j = 0;
                        if (planet.Owner != null && planet.Owner.isPlayer)
                        while (j < s.PlanetList[i].TroopsHere.Count)
                        {
                            if (!s.PlanetList[i].TroopsHere[j].GetOwner().isPlayer)
                            {
                                //hasEnemyTroop = true;

                            }
                            else
                            {
                                hastroops = true;
                                playerTroops++;
                            }
                            j++;
                        } 
                        if (hasAnamoly)
                        {
                            sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
                            float f = (float)Math.Sin(totalGameTime.TotalSeconds);
                            f = Math.Abs(f) * 255f;
                            Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_anomaly_small"), flashRect, flashColor);
                            if (flashRect.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                            {
                                ToolTip.CreateTooltip(121);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (hasCommodities)
                        {
                            
                                sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
                            float f = (float)Math.Sin(totalGameTime.TotalSeconds);
                            f = Math.Abs(f) * 255f;
                            Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/marketIcon"), flashRect, flashColor);
                            if (flashRect.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                            {
                                ToolTip.CreateTooltip(121);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (hastroops)
                        {

                            sideSpacing += 4;
                            TimeSpan totalGameTime = gameTime.TotalGameTime;
                            float f = (float)Math.Sin(totalGameTime.TotalSeconds);
                            f = Math.Abs(f) * 255f;
                            Color flashColor = new Color(255, 255, 255, (byte)f);
                            Rectangle flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), flashRect, flashColor);
                            if (flashRect.HitTest(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)))
                            {
                                ToolTip.CreateTooltip(121);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (p.Owner == null)
                        {
                            HelperFunctions.DrawDropShadowText1(ScreenManager, p.Name, PlanetTypeCursor, SysFont, (p.Habitable ? tColor : Color.LightPink));
                        }
                        else
                        {
                            HelperFunctions.DrawDropShadowText1(ScreenManager, p.Name, PlanetTypeCursor, SysFont, (p.Habitable ? p.Owner.EmpireColor : Color.LightPink));
                        }
                        if (p.Habitable)
                        {
                            int Spacing = DataFont.LineSpacing;
                            PlanetTypeCursor.Y += (Spacing + 4);
                            string popString = p.PopulationString;
                            PlanetTypeCursor.X = PlanetRect.X + PlanetRect.Width / 2 - DataFont.MeasureString(popString).X / 2f;
                            ScreenManager.SpriteBatch.DrawString(DataFont, popString, PlanetTypeCursor, tColor);
                            Rectangle flagRect = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 10, PlanetRect.Y - 20, 20, 20);
                            if (p.Owner != null)
                            {
                                ScreenManager screenManager = ScreenManager;
                                KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[p.Owner.data.Traits.FlagIndex];
                                HelperFunctions.DrawDropShadowImage(screenManager, flagRect, item.Value, p.Owner.EmpireColor);
                            }
                            Rectangle fIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)PlanetTypeCursor.Y + Spacing, 10, 10);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                            Rectangle pIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(PlanetTypeCursor.Y + 2 * Spacing), 10, 10);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);
                            Rectangle rIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(PlanetTypeCursor.Y + 3 * Spacing), 10, 10);
                            Rectangle tIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(PlanetTypeCursor.Y + 4 * Spacing), 10, 10);
                            if (p.Owner != null && p.Owner == EmpireManager.Player)
                            {
                                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIcon, Color.White);

                                    
                                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), tIcon, Color.White);
                                

                            }
                            Vector2 ft = new Vector2(fIcon.X + 12, fIcon.Y);
                            Vector2 pt = new Vector2(pIcon.X + 12, pIcon.Y);
                            HelperFunctions.ClampVectorToInt(ref ft);
                            HelperFunctions.ClampVectorToInt(ref pt);
                            if (p.Owner == null || p.Owner != EmpireManager.Player)
                            {
                                ScreenManager.SpriteBatch.DrawString(DataFont, p.Fertility.String(), ft, tColor);
                                ScreenManager.SpriteBatch.DrawString(DataFont, p.MineralRichness.String(), pt, tColor);
                            }
                            else
                            {
                                p.UpdateIncomes(false);
                                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                                SpriteFont dataFont = DataFont;
                                float netFoodPerTurn = p.GetNetFoodPerTurn();
                                spriteBatch.DrawString(dataFont, netFoodPerTurn.String(), ft, tColor);
                                SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                                SpriteFont spriteFont = DataFont;
                                float netProductionPerTurn = p.GetNetProductionPerTurn();
                                spriteBatch1.DrawString(spriteFont, netProductionPerTurn.String(), pt, tColor);
                                Vector2 rt = new Vector2(rIcon.X + 12, rIcon.Y);
                                HelperFunctions.ClampVectorToInt(ref rt);
                                ScreenManager.SpriteBatch.DrawString(DataFont, p.NetResearchPerTurn.String(), rt, tColor);
                                Vector2 tt = new Vector2(rIcon.X + 12, tIcon.Y);
                                HelperFunctions.ClampVectorToInt(ref tt);
                                ScreenManager.SpriteBatch.DrawString(DataFont, playerTroops.ToString(), tt, tColor);
                            }
                        }
                        float x = Mouse.GetState().X;
                        MouseState state = Mouse.GetState();
                        Vector2 MousePos = new Vector2(x, state.Y);
                        foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
                        {
                            if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
                            {
                                continue;
                            }
                            Rectangle Flag = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 6, PlanetRect.Y - 17, 13, 17);
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/flagicon"), Flag, EmpireManager.Player.EmpireColor);
                            if (!Flag.HitTest(MousePos))
                            {
                                continue;
                            }
                            ToolTip.CreateTooltip(26);
                        }
                    }
                }
            }
            if (!Hovering)
            {
                HoverTimer = 0f;
            }
            else
            {
                SystemInfoUIElement hoverTimer = this;
                hoverTimer.HoverTimer = hoverTimer.HoverTimer + elapsedTime;
                if (HoverTimer > 0.1f)
                {
                    HoverTimer = 0.1f;
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (s == null)
                return false;
            foreach (ClickMe clickMe in ClickList)
            {
                if (clickMe.r.HitTest(input.CursorPosition) && input.InGameSelect)
                {
                    if (ClickTimer < (double)TimerDelay)
                    {
                        screen.SelectedPlanet = clickMe.p;
                        screen.pInfoUI.SetPlanet(clickMe.p);
                        screen.ViewPlanet(null);
                        return true;
                    }

                    GameAudio.PlaySfxAsync("mouse_over4");
                    screen.SelectedPlanet = clickMe.p;
                    screen.pInfoUI.SetPlanet(clickMe.p);
                    ClickTimer = 0.0f;
                    return true;
                }
            }
            return ElementRect.HitTest(input.CursorPosition);
        }

        public void SetSystem(SolarSystem s)
        {
            if (this.s != s)
            {
                SelectionTimer = 0f;
            }
            this.s = s;
        }

        public struct ClickMe
        {
            public Rectangle r;

            public Planet p;
        }
    }
}
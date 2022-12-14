using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    /// <summary>
    /// In GalaxyView, draws an overlay of orbits and planets on top of selected solar system
    /// </summary>
    public sealed class SolarsystemOverlay : UIElement
    {
        public static Graphics.Font SysFont => Fonts.Arial12Bold;
        public static Graphics.Font DataFont => Fonts.Arial10;
        public float SelectionTimer;

        UniverseScreen Universe;
        SolarSystem Sys;
        float ClickTimer;
        float TimerDelay = 0.25f;
        bool Hovering;
        float HoverTimer;
        new Color tColor = Colors.Cream;
        Array<ClickMe> ClickList = new Array<ClickMe>();

        public SolarsystemOverlay(Rectangle r, ScreenManager sm, UniverseScreen universe)
        {
            Universe = universe;
            ScreenManager = sm;
            ElementRect = r;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Sys == null)
                return;

            Empire player = Universe.Player;

            ClickTimer += elapsed.RealTime.Seconds;
            SelectionTimer += elapsed.RealTime.Seconds;

            Vector2d pPos = Universe.ProjectToScreenPosition(Sys.Position.ToVec3());
            var radialPos = new Vector2(Sys.Position.X + 4500f, Sys.Position.Y);
            Vector2d insetRadialSS = Universe.ProjectToScreenPosition(radialPos.ToVec3());
            double pRadius = insetRadialSS.Distance(pPos).LowerBound(5);

            batch.BracketRectangle(pPos, pRadius, Color.White);

            if (SelectionTimer > 0.4f)
                SelectionTimer = 0.4f;

            Hovering = false;
            ClickList.Clear();

            float transitionPos = 1f - (SelectionTimer / 0.4f);
            float transitionOffset = transitionPos*transitionPos;

            if (Sys.IsExploredBy(player))
            {
                for (int i = 0; i < Sys.PlanetList.Count; i++)
                {
                    Planet p = Sys.PlanetList[i];
                    Vector2d planetPos = pPos.PointFromAngle(p.OrbitalAngle, 40 + 40 * i);
                    planetPos -= ((planetPos - pPos).Normalized() * (40 + 40 * i) * transitionOffset);

                    Color color = p.Owner == null ? new Color(50, 50, 50, 90) : new Color(p.Owner.EmpireColor, 100);
                    ScreenManager.SpriteBatch.DrawCircle(pPos, pPos.Distance(planetPos), color, 2f);
                }

                for (int i = 0; i < Sys.PlanetList.Count; i++)
                {
                    Planet p = Sys.PlanetList[i];
                    Vector2d planetPos = pPos.PointFromAngle(p.OrbitalAngle, 40 + 40 * i);
                    planetPos -= ((planetPos - pPos).Normalized() * (40 + 40 * i) * transitionOffset);

                    float fIconScale = 1.0f + ((float)Math.Log(p.Scale));
                    Rectangle PlanetRect = new Rectangle((int)planetPos.X - (int)(16 * fIconScale / 2), (int)planetPos.Y - (int)(16 * fIconScale / 2), (int)(16 * fIconScale), (int)(16 * fIconScale));
                    if (PlanetRect.HitTest(Universe.Input.CursorPosition))
                    {
                        Hovering = true;
                        int widthplus = (int)(4f * (HoverTimer / 0.2f));
                        PlanetRect = new Rectangle((int)planetPos.X - ((int)(16 * fIconScale / 2) + widthplus), (int)planetPos.Y - ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus));
                        ClickList.Add(new ClickMe { p = p, r = PlanetRect });
                    }
                    batch.Draw(p.PlanetTexture, PlanetRect, Color.White);
            
                    if (Universe.SelectedPlanet == p)
                    {
                        batch.BracketRectangle(PlanetRect, p.Owner?.EmpireColor ?? Color.Gray, 3);
                    }

                    var planetTypeCursor = new Vector2(PlanetRect.X + PlanetRect.Width / 2 - SysFont.MeasureString(p.Name).X / 2f, PlanetRect.Y + PlanetRect.Height + 4);
                    planetTypeCursor = planetTypeCursor.Rounded();
                    bool hasAnamoly = false;
                    bool hasCommodities = false;
                    bool hastroops =false;
                    //bool hasEnemyTroop = false;          //Not referenced in code, removing to save memory
                    int playerTroops = 0;
                    int sideSpacing = 0;

                    if (p.IsExploredBy(player))
                    {
                        hasAnamoly = p.HasAnomaly;
                        hasCommodities = p.HasCommodities;

                        if (p.OwnerIsPlayer)
                        {
                            playerTroops += p.Troops.NumTroopsHere(p.Owner);
                            hastroops |= playerTroops > 0;
                        }

                        if (hasAnamoly)
                        {
                            sideSpacing += 4;
                            var flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            batch.Draw(ResourceManager.Texture("UI/icon_anomaly_small"), flashRect, Universe.CurrentFlashColor);
                            if (flashRect.HitTest(Universe.Input.CursorPosition))
                            {
                                ToolTip.CreateTooltip(GameText.IndicatesThatAnAnomalyWas);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (hasCommodities)
                        {
                            sideSpacing += 4;
                            var flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            batch.Draw(ResourceManager.Texture("UI/marketIcon"), flashRect, Universe.CurrentFlashColor);
                            if (flashRect.HitTest(Universe.Input.CursorPosition))
                            {
                                ToolTip.CreateTooltip(GameText.IndicatesThatAnAnomalyWas);
                            }
                            sideSpacing += flashRect.Width;
                        }
                        if (hastroops)
                        {
                            sideSpacing += 4;
                            var flashRect = new Rectangle(PlanetRect.X + PlanetRect.Width + sideSpacing, PlanetRect.Y + PlanetRect.Height / 2 - 7, 14, 14);
                            batch.Draw(ResourceManager.Texture("UI/icon_troop"), flashRect, Universe.CurrentFlashColor);
                            if (flashRect.HitTest(Universe.Input.CursorPosition))
                            {
                                ToolTip.CreateTooltip(GameText.IndicatesThatAnAnomalyWas);
                            }
                            sideSpacing += flashRect.Width;
                        }

                        if (p.Owner == null)
                        {
                            batch.DrawDropShadowText1(p.Name, planetTypeCursor, SysFont, p.Habitable ? tColor : Color.LightPink);
                        }
                        else
                        {
                            batch.DrawDropShadowText1(p.Name, planetTypeCursor, SysFont, (p.Habitable ? p.Owner.EmpireColor : Color.LightPink));
                        }

                        if (p.Habitable)
                        {
                            int Spacing = DataFont.LineSpacing;
                            planetTypeCursor.Y += (Spacing + 4);
                            p.UpdateMaxPopulation();
                            string popString = p.PopulationStringForPlayer;
                            planetTypeCursor.X = PlanetRect.X + PlanetRect.Width / 2 - DataFont.MeasureString(popString).X / 2f;
                            batch.DrawString(DataFont, popString, planetTypeCursor, tColor);
                            Rectangle flagRect = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 10, PlanetRect.Y - 20, 20, 20);
                            if (p.Owner != null)
                            {
                                batch.DrawDropShadowImage(flagRect, ResourceManager.Flag(p.Owner), p.Owner.EmpireColor);
                            }
                            Rectangle fIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)planetTypeCursor.Y + Spacing, 10, 10);
                            Rectangle pIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 2 * Spacing), 10, 10);
                            var rIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 3 * Spacing), 10, 10);
                            var tIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 4 * Spacing), 10, 10);
                            batch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);

                            if (p.Owner != null && p.Owner == player)
                            {
                                batch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIcon, Color.White);
                                batch.Draw(ResourceManager.Texture("UI/icon_troop"), tIcon, Color.White);
                            }

                            if (p.Owner == null || p.Owner != player)
                            {
                                batch.DrawString(DataFont, p.FertilityFor(player).String(), new Vector2(fIcon.X + 12, fIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.MineralRichness.String(), new Vector2(pIcon.X + 12, pIcon.Y).Rounded(), tColor);
                            }
                            else
                            {
                                p.UpdateIncomes();
                                batch.DrawString(DataFont, p.Food.NetIncome.String(), new Vector2(fIcon.X + 12, fIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.Prod.NetIncome.String(), new Vector2(pIcon.X + 12, pIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.Res.NetIncome.String(), new Vector2(rIcon.X + 12, rIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, playerTroops.ToString(), new Vector2(rIcon.X + 12, tIcon.Y).Rounded(), tColor);
                            }
                        }

                        foreach (Goal g in player.AI.Goals)
                        {
                            if (g.IsColonizationGoal(p))
                            {
                                var flag = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 6, PlanetRect.Y - 17, 13, 17);
                                batch.Draw(ResourceManager.Texture("UI/flagicon"), flag, player.EmpireColor);
                                if (flag.HitTest(Universe.Input.CursorPosition))
                                    ToolTip.CreateTooltip(GameText.IndicatesThatYourEmpireHas);
                            }
                        }
                    }
                }
            }

            if (Hovering)
            {
                HoverTimer += elapsed.RealTime.Seconds;
                if (HoverTimer > 0.1f)
                    HoverTimer = 0.1f;
            }
            else
            {
                HoverTimer = 0f;
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (Sys == null)
                return false;
            foreach (ClickMe clickMe in ClickList)
            {
                if (clickMe.r.HitTest(input.CursorPosition) && input.InGameSelect)
                {
                    if (ClickTimer < (double)TimerDelay)
                    {
                        Universe.SelectedPlanet = clickMe.p;
                        Universe.pInfoUI.SetPlanet(clickMe.p);
                        Universe.SnapViewColony(clickMe.p.Owner != Universe.Player);
                        return true;
                    }

                    GameAudio.MouseOver();
                    Universe.SelectedPlanet = clickMe.p;
                    Universe.pInfoUI.SetPlanet(clickMe.p);
                    ClickTimer = 0.0f;
                    return true;
                }
            }
            return ElementRect.HitTest(input.CursorPosition);
        }

        public void SetSystem(SolarSystem s)
        {
            if (Sys != s)
                SelectionTimer = 0f;
            Sys = s;
        }

        public struct ClickMe
        {
            public Rectangle r;
            public Planet p;
        }
    }
}
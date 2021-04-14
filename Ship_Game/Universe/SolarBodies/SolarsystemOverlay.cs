using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;

namespace Ship_Game
{
    /// <summary>
    /// In GalaxyView, draws an overlay of orbits and planets on top of selected solar system
    /// </summary>
    public sealed class SolarsystemOverlay : UIElement
    {
        public static Graphics.Font SysFont;
        public static Graphics.Font DataFont;
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

            ClickTimer += elapsed.RealTime.Seconds;
            SelectionTimer += elapsed.RealTime.Seconds;

            Vector2 pPos = Universe.ProjectTo2D(Sys.Position.ToVec3());
            Vector2 radialPos = new Vector2(Sys.Position.X + 4500f, Sys.Position.Y);
            Vector2 insetRadialSS = Universe.ProjectTo2D(radialPos.ToVec3());
            float pRadius = insetRadialSS.Distance(pPos).LowerBound(5f);

            batch.BracketRectangle(pPos, pRadius, Color.White);

            if (SelectionTimer > 0.4f)
                SelectionTimer = 0.4f;

            Hovering = false;
            ClickList.Clear();

            float transitionPos = 1f - (SelectionTimer / 0.4f);
            float transitionOffset = transitionPos*transitionPos;

            if (Sys.IsExploredBy(EmpireManager.Player))
            {
                for (int i = 0; i < Sys.PlanetList.Count; i++)
                {
                    Vector2 planetPos = pPos.PointFromAngle(Sys.PlanetList[i].OrbitalAngle, 40 + 40 * i);
                    planetPos -= ((planetPos - pPos).Normalized() * (40 + 40 * i) * transitionOffset);
                    DrawCircle(pPos, pPos.Distance(planetPos), (Sys.PlanetList[i].Owner == null ? new Color(50, 50, 50, 90) : new Color(Sys.PlanetList[i].Owner.EmpireColor, 100)), 2f);
                }

                for (int i = 0; i < Sys.PlanetList.Count; i++)
                {
                    Planet planet = Sys.PlanetList[i];
                    Vector2 planetPos = pPos.PointFromAngle(Sys.PlanetList[i].OrbitalAngle, 40 + 40 * i);
                    planetPos -= ((planetPos - pPos).Normalized() * (40 + 40 * i) * transitionOffset);

                    float fIconScale = 1.0f + ((float)Math.Log(Sys.PlanetList[i].Scale));
                    Rectangle PlanetRect = new Rectangle((int)planetPos.X - (int)(16 * fIconScale / 2), (int)planetPos.Y - (int)(16 * fIconScale / 2), (int)(16 * fIconScale), (int)(16 * fIconScale));
                    if (PlanetRect.HitTest(Universe.Input.CursorPosition))
                    {
                        Hovering = true;
                        int widthplus = (int)(4f * (HoverTimer / 0.2f));
                        PlanetRect = new Rectangle((int)planetPos.X - ((int)(16 * fIconScale / 2) + widthplus), (int)planetPos.Y - ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus), 2 * ((int)(16 * fIconScale / 2) + widthplus));
                        ClickMe cm = new ClickMe
                        {
                            p = Sys.PlanetList[i],
                            r = PlanetRect
                        };
                        ClickList.Add(cm);
                    }
                    batch.Draw(Sys.PlanetList[i].PlanetTexture, PlanetRect, Color.White);
            
                    if (Universe.SelectedPlanet == Sys.PlanetList[i])
                    {
                        batch.BracketRectangle(PlanetRect, (Sys.PlanetList[i].Owner != null ? Sys.PlanetList[i].Owner.EmpireColor : Color.Gray), 3);
                    }

                    Planet p = Sys.PlanetList[i];
                    var planetTypeCursor = new Vector2(PlanetRect.X + PlanetRect.Width / 2 - SysFont.MeasureString(p.Name).X / 2f, PlanetRect.Y + PlanetRect.Height + 4);
                    planetTypeCursor = planetTypeCursor.Rounded();
                    bool hasAnamoly = false;
                    bool hasCommodities = false;
                    bool hastroops =false;
                    //bool hasEnemyTroop = false;          //Not referenced in code, removing to save memory
                    int playerTroops = 0;
                    int sideSpacing = 0;

                    if (p.IsExploredBy(EmpireManager.Player))
                    {
                        int j = 0;

                        while (j < Sys.PlanetList[i].BuildingList.Count)
                        {
                            Building building = Sys.PlanetList[i].BuildingList[j];
                            
                            if (building.EventHere)
                            {
                                hasAnamoly = true;
                            }
                            if (building.IsCommodity || building.IsVolcano || building.IsCrater)
                            {
                                hasCommodities = true;
                            }
                            if (hasCommodities && hasAnamoly)
                                break;
                            j++;
                        }

                        j = 0;
                        if (planet.Owner != null && planet.Owner.isPlayer)
                        while (j < Sys.PlanetList[i].TroopsHere.Count)
                        {
                            if (!Sys.PlanetList[i].TroopsHere[j].Loyalty.isPlayer)
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
                                HelperFunctions.DrawDropShadowImage(batch, flagRect, ResourceManager.Flag(p.Owner), p.Owner.EmpireColor);
                            }
                            Rectangle fIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)planetTypeCursor.Y + Spacing, 10, 10);
                            Rectangle pIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 2 * Spacing), 10, 10);
                            var rIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 3 * Spacing), 10, 10);
                            var tIcon = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 15, (int)(planetTypeCursor.Y + 4 * Spacing), 10, 10);
                            batch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
                            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);

                            if (p.Owner != null && p.Owner == EmpireManager.Player)
                            {
                                batch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIcon, Color.White);
                                batch.Draw(ResourceManager.Texture("UI/icon_troop"), tIcon, Color.White);
                            }

                            if (p.Owner == null || p.Owner != EmpireManager.Player)
                            {
                                batch.DrawString(DataFont, p.FertilityFor(EmpireManager.Player).String(), new Vector2(fIcon.X + 12, fIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.MineralRichness.String(), new Vector2(pIcon.X + 12, pIcon.Y).Rounded(), tColor);
                            }
                            else
                            {
                                p.UpdateIncomes(false);
                                batch.DrawString(DataFont, p.Food.NetIncome.String(), new Vector2(fIcon.X + 12, fIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.Prod.NetIncome.String(), new Vector2(pIcon.X + 12, pIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, p.Res.NetIncome.String(), new Vector2(rIcon.X + 12, rIcon.Y).Rounded(), tColor);
                                batch.DrawString(DataFont, playerTroops.ToString(), new Vector2(rIcon.X + 12, tIcon.Y).Rounded(), tColor);
                            }
                        }

                        foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
                        {
                            if (g.ColonizationTarget == null || g.ColonizationTarget != p)
                                continue;

                            var flag = new Rectangle(PlanetRect.X + PlanetRect.Width / 2 - 6, PlanetRect.Y - 17, 13, 17);
                            batch.Draw(ResourceManager.Texture("UI/flagicon"), flag, EmpireManager.Player.EmpireColor);
                            if (flag.HitTest(Universe.Input.CursorPosition))
                                ToolTip.CreateTooltip(GameText.IndicatesThatYourEmpireHas);
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
                        Universe.SnapViewColony(clickMe.p.Owner != EmpireManager.Player);
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
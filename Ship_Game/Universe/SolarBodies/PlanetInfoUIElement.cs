using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class PlanetInfoUIElement : UIElement
    {
        private Rectangle SliderRect;

        private Rectangle clickRect;

        private UniverseScreen screen;

        private Rectangle LeftRect;

        private Rectangle RightRect;

        private Rectangle PlanetIconRect;

        private Rectangle flagRect;

        private Rectangle moneyRect;
        private Rectangle SendTroops;

        private Rectangle popRect;

        private string PlanetTypeRichness;
        private Vector2 PlanetTypeCursor;

        public Planet p;
        private Selector sel;
        private SkinnableButton Inspect;
        private SkinnableButton Invade;

        ColonySliderGroup Sliders;
        private Rectangle Housing;

        private Rectangle DefenseRect;
        private Rectangle InjuryRect;
        private Rectangle OffenseRect;
        private Rectangle ShieldRect;
        private Rectangle DefenseShipsRect;
        private Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        private Rectangle Mark;

        public PlanetInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            sel = new Selector(r, Color.Black);
            Housing = r;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            clickRect = new Rectangle(ElementRect.Right - 16, ElementRect.CenterY() - 11, 11, 22);
            LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            PlanetIconRect = new Rectangle(LeftRect.X + 75, Housing.Y + 120, 80, 80);
            Inspect = new SkinnableButton(new Rectangle(PlanetIconRect.CenterX() - 16, PlanetIconRect.Y, 32, 32), "UI/viewPlanetIcon")
            {
                HoverColor = tColor,
                IsToggle = false
            };
            Invade = new SkinnableButton(new Rectangle(PlanetIconRect.X + PlanetIconRect.Width / 2 - 16, PlanetIconRect.Y + 48, 32, 32), "UI/ColonizeIcon")
            {
                HoverColor = tColor,
                IsToggle = false
            };

            SliderRect = new Rectangle(r.Right - 100, r.Bottom - 40, 500, 40);
            Sliders = new ColonySliderGroup(null, SliderRect);
            Sliders.Create(RightRect.X, Housing.Y + 120, 145, 40);

            flagRect         = new Rectangle(r.X + r.Width - 60, Housing.Y + 63, 26, 26);
            DefenseRect      = new Rectangle(LeftRect.X + 13, Housing.Y + 114, 22, 22);
            OffenseRect      = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 22, 22, 22);
            InjuryRect       = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 44, 22, 22);
            ShieldRect       = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 66, 22, 22);
            DefenseShipsRect = new Rectangle(LeftRect.X + 13, Housing.Y + 114 + 88, 22, 22);
        }

        public override void Draw(GameTime gameTime)
        {
            if (p == null)
                return;

            SpriteBatch batch = ScreenManager.SpriteBatch;

            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ToolTipItems.Clear();
            var def = new TippedItem
            {
                r = DefenseRect,
                TIP_ID = 31
            };
            ToolTipItems.Add(def);
            var injury = new TippedItem
            {
                r = InjuryRect,
                TIP_ID = 249
            };
            ToolTipItems.Add(injury);
            var offense = new TippedItem
            {
                r = OffenseRect,
                TIP_ID = 250
            };
            ToolTipItems.Add(offense);
            var defenseShips = new TippedItem
            {
                r = DefenseShipsRect,
                TIP_ID = 251
            };
            ToolTipItems.Add(defenseShips);
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            var MousePos = new Vector2(x, state.Y);
            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            var NamePos = new Vector2(Housing.X + 41, Housing.Y + 65);
            if (p.Owner == null || !p.IsExploredBy(EmpireManager.Player))
            {
                if (DrawUnexploredUninhabited(NamePos, MousePos)) return;
                return;
            }
            batch.DrawString(Fonts.Arial20Bold, p.Name, NamePos, tColor);
            batch.Draw(ResourceManager.Flag(p.Owner), flagRect, p.Owner.EmpireColor);
            var cursor = new Vector2(sel.Rect.X + sel.Rect.Width - 65, NamePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);
            
            string pop = p.PopulationString;
            cursor.X -= (Fonts.Arial12Bold.MeasureString(pop).X + 5f);
            batch.DrawString(Fonts.Arial12Bold, pop, cursor, tColor);

            popRect = new Rectangle((int)cursor.X - 23, (int)cursor.Y - 3, 22, 22);
            batch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

            moneyRect = new Rectangle(popRect.X - 70, popRect.Y, 22, 22);
            var moneyCursor = new Vector2((float)moneyRect.X + 24, cursor.Y);

            if (p.Owner == EmpireManager.Player)
            {
                string sNetIncome = p.Money.NetRevenue.String(2);
                batch.DrawString(Fonts.Arial12Bold, sNetIncome, moneyCursor, p.Money.NetRevenue > 0.0 ? Color.LightGreen : Color.Salmon);
                batch.Draw(ResourceManager.Texture("UI/icon_money_22"), moneyRect, Color.White);
            }

            PlanetTypeRichness = p.LocalizedRichness;
            PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f, PlanetIconRect.Y + PlanetIconRect.Height + 5);
            batch.Draw(p.PlanetTexture, PlanetIconRect, Color.White);
            batch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            p.UpdateIncomes(false);

            DrawPlanetStats(DefenseRect, ((float)p.TotalDefensiveStrength).String(1), "UI/icon_shield", Color.White, Color.White);

            // Added by Fat Bastard - display total injury level inflicted automatically to invading troops
            if (p.TotalInvadeInjure > 0)
                DrawPlanetStats(InjuryRect, ((float)p.TotalInvadeInjure).String(1), "UI/icon_injury", Color.White, Color.White);

            // Added by Fat Bastard - display total space offense of the planet
            if (p.TotalSpaceOffense > 0)
            {
                string offenseNumberString = ((float) Math.Round(p.TotalSpaceOffense,0)).GetNumberString();
                DrawPlanetStats(OffenseRect, offenseNumberString, "UI/icon_offense", Color.White, Color.White);
            }

            if (p.ShieldStrengthMax > 0f)
                DrawPlanetStats(ShieldRect, p.ShieldStrengthCurrent.String(0), "NewUI/icon_planetshield", Color.White, Color.Green);

            // Added by Fat Bastard - display total defense ships stationed on this planet
            int maxDefenseShips = p.MaxDefenseShips;
            if (maxDefenseShips > 0 )
            {
                int currentDefenseShips = p.CurrentDefenseShips;
                if (currentDefenseShips == maxDefenseShips)
                    DrawPlanetStats(DefenseShipsRect, currentDefenseShips.ToString(), "UI/icon_hangar", Color.White, Color.White);
                else
                    DrawPlanetStats(DefenseShipsRect, currentDefenseShips + "/" + maxDefenseShips , "UI/icon_hangar", Color.Yellow, Color.White);
            }
            Inspect.Draw(ScreenManager);
            Invade.Draw(ScreenManager);
        }

        bool DrawUnexploredUninhabited(Vector2 namePos, Vector2 mousePos)
        {
            if (!p.IsExploredBy(EmpireManager.Player))
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                    Localizer.Token(1429) + p.LocalizedCategory, namePos, tColor);
                var textCursor = new Vector2(sel.Rect.X + sel.Rect.Width - 65,
                    namePos.Y + Fonts.Arial20Bold.LineSpacing / 2f - Fonts.Arial12Bold.LineSpacing / 2f + 2f);
                string pop = p.PopulationString;
                textCursor.X = textCursor.X - (Fonts.Arial12Bold.MeasureString(pop).X + 5f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, pop, textCursor, tColor);

                popRect = new Rectangle((int) textCursor.X - 23, (int) textCursor.Y - 3, 22, 22);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

                string text = Localizer.Token(1430);
                var cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, tColor);
                return true;
            }

            if (!p.Habitable)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, p.Name, namePos, tColor);
                string text = Localizer.Token(1427);
                Vector2 Cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, Cursor, tColor);
                return true;
            }

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, p.Name, namePos, tColor);
            Vector2 TextCursor = new Vector2(sel.Rect.X + sel.Rect.Width - 65,
                namePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);

            string pop2 = p.PopulationString;
            TextCursor.X -= (Fonts.Arial12Bold.MeasureString(pop2).X + 5f);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, pop2, TextCursor, tColor);

            popRect = new Rectangle((int) TextCursor.X - 23, (int) TextCursor.Y - 3, 22, 22);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_pop_22"), popRect, Color.White);

            PlanetTypeRichness = p.LocalizedRichness;
            PlanetTypeCursor =
                new Vector2(
                    PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f,
                    PlanetIconRect.Y + PlanetIconRect.Height + 5);
            ScreenManager.SpriteBatch.Draw(p.PlanetTexture, PlanetIconRect,
                Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            Rectangle fIcon = new Rectangle(240,
                Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_food").Height,
                ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
            TippedItem ti = new TippedItem
            {
                r = fIcon,
                TIP_ID = 20
            };
            ToolTipItems.Add(ti);
            Vector2 tcurs = new Vector2(fIcon.X + 25, Housing.Y + 205);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.Fertility.String(), tcurs, tColor);
            Rectangle pIcon = new Rectangle(300,
                Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_production").Height,
                ResourceManager.Texture("NewUI/icon_production").Width,
                ResourceManager.Texture("NewUI/icon_production").Height);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);
            ti = new TippedItem
            {
                r = pIcon,
                TIP_ID = 21
            };
            ToolTipItems.Add(ti);
            tcurs = new Vector2(325f, Housing.Y + 205);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), tcurs, tColor);
            Mark = new Rectangle(RightRect.X - 10, Housing.Y + 150, 182, 25);
            Vector2 Text = new Vector2(RightRect.X + 25, Mark.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/dan_button_blue"), Mark, Color.White);
            if (GlobalStats.IsGermanOrPolish)
            {
                Text.X = Text.X - 9f;
            }

            bool marked = false;
            foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget == null || g.ColonizationTarget != p)
                {
                    continue;
                }

                marked = true;
            }

            if (marked)
            {
                if (!Mark.HitTest(mousePos))
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text,
                        new Color(88, 108, 146));
                }
                else
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text,
                        new Color(174, 202, 255));
                }

                ti = new TippedItem
                {
                    r = Mark,
                    TIP_ID = 25
                };
                ToolTipItems.Add(ti);
            }
            else
            {
                if (!Mark.HitTest(mousePos))
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text,
                        new Color(88, 108, 146));
                }
                else
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text,
                        new Color(174, 202, 255));
                }

                ti = new TippedItem
                {
                    r = Mark,
                    TIP_ID = 24
                };
                ToolTipItems.Add(ti);
            }

            //Ship troopShip
            ti = new TippedItem
            {
                r = pIcon,
                TIP_ID = 21
            };
            ToolTipItems.Add(ti);

            SendTroops = new Rectangle(Mark.X, Mark.Y - Mark.Height - 5, 182, 25);
            Text = new Vector2(SendTroops.X + 25, SendTroops.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/dan_button_blue"), SendTroops, Color.White);
            int troops = screen.player
                .GetShips()
                .Where(troop => troop.TroopList.Count > 0)
                .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet == p));
            if (!SendTroops.HitTest(mousePos))
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text,
                    new Color(88, 108, 146)); // Localizer.Token(1425)
            else
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text,
                    new Color(174, 202, 255)); // Localizer.Token(1425)

            Inspect.Draw(ScreenManager);
            Invade.Draw(ScreenManager);
            return false;
        }

        private void DrawPlanetStats(Rectangle rect, string data, string texturePath, Color color, Color texcolor)
        {
            SpriteFont font = Fonts.Arial12Bold;
            Vector2 pos     = new Vector2((rect.X + rect.Width + 2), (rect.Y + 11 - font.LineSpacing / 2));
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(texturePath), rect, texcolor);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data, pos, color);
        }

        public override bool HandleInput(InputState input)
        {
            if (p == null)
            {
                return false;
            }
            if (ShieldRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2240));
            }
            foreach (TippedItem ti in ToolTipItems)
            {
                if (!ti.r.HitTest(input.CursorPosition))
                {
                    continue;
                }
                ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (Mark.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                bool marked = false;
                Goal markedGoal = null;
                foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
                {
                    if (g.ColonizationTarget == null || g.ColonizationTarget != p)
                    {
                        continue;
                    }
                    marked = true;
                    markedGoal = g;
                }
                if (marked)
                {
                    GameAudio.EchoAffirmative();
                    if (markedGoal.FinishedShip != null)
                    {
                        markedGoal.FinishedShip.AI.OrderQueue.Clear();
                        markedGoal.FinishedShip.AI.State = AIState.AwaitingOrders;
                    }
                    EmpireManager.Player.GetEmpireAI().Goals.QueuePendingRemoval(markedGoal);
                    EmpireManager.Player.GetEmpireAI().Goals.ApplyPendingRemovals();
                }
                else
                {
                    GameAudio.EchoAffirmative();
                    EmpireManager.Player.GetEmpireAI().Goals.Add(new MarkForColonization(p, EmpireManager.Player));
                }
            }
            if (SendTroops.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                var troopShips = new Array<Ship>(screen.player.GetShips()
                     .Where(troop => troop.TroopList.Count > 0
                         && (troop.AI.State == AIState.AwaitingOrders || troop.AI.State == AIState.Orbit)
                         && troop.fleet == null && !troop.InCombat).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                var planetTroops = new Array<Planet>(screen.player.GetPlanets().Where(troops => troops.TroopsHere.Count > 1).OrderBy(distance => Vector2.Distance(distance.Center, p.Center)));
                if (troopShips.Count > 0)
                {
                    GameAudio.EchoAffirmative();
                    troopShips.First().AI.OrderAssaultPlanet(p);
                }
                else if (planetTroops.Count > 0)
                {
                    {
                        Ship troop = planetTroops.First().TroopsHere.First().Launch();
                        if (troop != null)
                        {
                            GameAudio.EchoAffirmative();                              
                            troop.AI.OrderAssaultPlanet(p);
                        }
                    }
                }
                else
                {
                    GameAudio.BlipClick();
                }
            }

            if (Inspect.Hover)
            {
                if (p.Owner == null || p.Owner != EmpireManager.Player)
                {
                    ToolTip.CreateTooltip(61);
                }
                else
                {
                    ToolTip.CreateTooltip(76);
                }
            }
            if (Invade.Hover)
            {
                ToolTip.CreateTooltip(62);
            }
            if (p.Habitable)
            {
                if (Inspect.HandleInput(input))
                {
                    screen.ViewPlanet();
                }
                if (Invade.HandleInput(input))
                {
                    screen.OpenCombatMenu();
                }
            }
            if (!ElementRect.HitTest(input.CursorPosition))
            {
                return false;
            }
            if (p.Owner != null && p.Owner == EmpireManager.Player)
            {
                p.UpdateIncomes(false);
                Sliders.HandleInput(input);
            }
            return true;
        }

        public void SetPlanet(Planet p)
        {
            this.p = p;
            Sliders.SetPlanet(p);
        }

        private struct TippedItem
        {
            public Rectangle r;
            public int TIP_ID;
        }
    }
}
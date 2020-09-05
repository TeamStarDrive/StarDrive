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
        Planet P;
        readonly UniverseScreen Screen;
        Rectangle MoneyRect;
        Rectangle SendTroops;
        Rectangle PopRect;
        string PlanetTypeRichness;
        Vector2 PlanetTypeCursor;
        readonly Selector Sel;
        readonly SkinnableButton Inspect;
        readonly SkinnableButton Invade;
        readonly Rectangle Housing;
        readonly Rectangle DefenseRect;
        readonly Rectangle InjuryRect;
        readonly Rectangle OffenseRect;
        readonly Rectangle ShieldRect;
        readonly Rectangle DefenseShipsRect;
        readonly Rectangle RightRect;
        readonly Rectangle PlanetIconRect;
        readonly Rectangle FlagRect;

        readonly Rectangle TilesRect;
        readonly Rectangle PopPerTileRect;
        readonly Rectangle BiospheredPopRect;
        readonly Rectangle TerraformedPopRect;
        readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        Rectangle Mark;
        AssignLaborComponent AssignLabor;

        readonly SpriteFont Font8  = Fonts.Arial8Bold;
        readonly SpriteFont Font12 = Fonts.Arial12Bold;

        public PlanetInfoUIElement(in Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            this.Screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            Sel = new Selector(r, Color.Black);
            Housing = r;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            var leftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
            RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
            PlanetIconRect = new Rectangle(leftRect.X + 75, Housing.Y + 120, 80, 80);
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

            FlagRect         = new Rectangle(r.X + r.Width - 60, Housing.Y + 63, 26, 26);
            DefenseRect      = new Rectangle(leftRect.X + 13, Housing.Y + 114, 22, 22);
            OffenseRect      = new Rectangle(leftRect.X + 13, Housing.Y + 114 + 22, 22, 22);
            InjuryRect       = new Rectangle(leftRect.X + 13, Housing.Y + 114 + 44, 22, 22);
            ShieldRect       = new Rectangle(leftRect.X + 13, Housing.Y + 114 + 66, 22, 22);
            DefenseShipsRect = new Rectangle(leftRect.X + 13, Housing.Y + 114 + 88, 22, 22);

            // Use the same positions for unexplored planet data
            TilesRect          = DefenseRect;
            PopPerTileRect     = OffenseRect;
            BiospheredPopRect  = InjuryRect;
            TerraformedPopRect = ShieldRect;
        }

        public override void Update(UpdateTimes elapsed)
        {
            AssignLabor?.Update(elapsed.RealTime.Seconds);
            base.Update(elapsed);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (P == null)
                return;

            MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            ToolTipItems.Clear();
            var population = new TippedItem
            {
                r = PopRect,
                TIP_ID = 254
            };
            ToolTipItems.Add(population);

            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            var MousePos = new Vector2(x, state.Y);
            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            var NamePos = new Vector2(Housing.X + 41, Housing.Y + 65);
            P.UpdateMaxPopulation();
            if (P.Owner == null || !P.IsExploredBy(EmpireManager.Player))
            {
                DrawUnexploredUninhabited(NamePos, MousePos);
                return;
            }

            AddExploredTips();
            batch.DrawString(Fonts.Arial20Bold, P.Name, NamePos, tColor);
            batch.Draw(ResourceManager.Flag(P.Owner), FlagRect, P.Owner.EmpireColor);
            var cursor = new Vector2(Sel.Rect.X + Sel.Rect.Width - 65, NamePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);

            string pop = P.PopulationStringForPlayer;
            cursor.X -= (Fonts.Arial12Bold.MeasureString(pop).X + 5f);
            batch.DrawString(Fonts.Arial12Bold, pop, cursor, tColor);

            PopRect = new Rectangle((int)cursor.X - 23, (int)cursor.Y - 3, 22, 22);
            batch.Draw(ResourceManager.Texture("UI/icon_pop_22"), PopRect, Color.White);

            MoneyRect = new Rectangle(PopRect.X - 70, PopRect.Y, 22, 22);
            var moneyCursor = new Vector2((float)MoneyRect.X + 24, cursor.Y);

            if (P.Owner == EmpireManager.Player)
            {
                string sNetIncome = P.Money.NetRevenue.String(2);
                batch.DrawString(Fonts.Arial12Bold, sNetIncome, moneyCursor, P.Money.NetRevenue > 0.0 ? Color.LightGreen : Color.Salmon);
                batch.Draw(ResourceManager.Texture("UI/icon_money_22"), MoneyRect, Color.White);
            }

            PlanetTypeRichness = P.LocalizedRichness;
            PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f, PlanetIconRect.Y + PlanetIconRect.Height + 5);
            batch.Draw(P.PlanetTexture, PlanetIconRect, Color.White);
            batch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            P.UpdateIncomes(false);

            DrawPlanetStats(DefenseRect, ((float)P.TotalDefensiveStrength).String(1), "UI/icon_shield", Color.White, Color.White);

            // Added by Fat Bastard - display total injury level inflicted automatically to invading troops
            if (P.TotalInvadeInjure > 0)
                DrawPlanetStats(InjuryRect, ((float)P.TotalInvadeInjure).String(1), "UI/icon_injury", Color.White, Color.White);

            // Added by Fat Bastard - display total space offense of the planet
            if (P.TotalGeodeticOffense > 0)
            {
                string offenseNumberString = ((float) Math.Round(P.TotalGeodeticOffense, 0)).GetNumberString();
                DrawPlanetStats(OffenseRect, offenseNumberString, "UI/icon_offense", Color.White, Color.White);
            }

            if (P.ShieldStrengthMax > 0f)
                DrawPlanetStats(ShieldRect, P.ShieldStrengthCurrent.String(0), "NewUI/icon_planetshield", Color.White, Color.Green);

            // Added by Fat Bastard - display total defense ships stationed on this planet
            int maxDefenseShips = P.MaxDefenseShips;
            if (maxDefenseShips > 0 )
            {
                int currentDefenseShips = P.CurrentDefenseShips;
                if (currentDefenseShips == maxDefenseShips)
                    DrawPlanetStats(DefenseShipsRect, currentDefenseShips.ToString(), "UI/icon_hangar", Color.White, Color.White);
                else
                    DrawPlanetStats(DefenseShipsRect, currentDefenseShips + "/" + maxDefenseShips , "UI/icon_hangar", Color.Yellow, Color.White);
            }
            Inspect.Draw(batch);
            Invade.Draw(batch);

            AssignLabor?.Draw(batch, elapsed);
        }

        bool DrawUnexploredUninhabited(Vector2 namePos, Vector2 mousePos)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            if (!P.IsExploredBy(EmpireManager.Player))
            {
                batch.DrawString(Fonts.Arial20Bold,
                    Localizer.Token(1429) + P.LocalizedCategory, namePos, tColor);
                var textCursor = new Vector2(Sel.Rect.X + Sel.Rect.Width - 65,
                    namePos.Y + Fonts.Arial20Bold.LineSpacing / 2f - Fonts.Arial12Bold.LineSpacing / 2f + 2f);

                string text = Localizer.Token(1430);
                var cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                batch.DrawString(Fonts.Arial12Bold, text, cursor, tColor);
                return true;
            }

            if (!P.Habitable)
            {
                batch.DrawString(Fonts.Arial20Bold, P.Name, namePos, tColor);
                string text = Localizer.Token(1427);
                Vector2 Cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                batch.DrawString(Fonts.Arial12Bold, text, Cursor, tColor);
                return true;
            }

            batch.DrawString(Fonts.Arial20Bold, P.Name, namePos, tColor);
            Vector2 TextCursor = new Vector2(Sel.Rect.X + Sel.Rect.Width - 65,
                namePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);

            string pop2 = P.PopulationStringForPlayer;
            TextCursor.X -= (Fonts.Arial12Bold.MeasureString(pop2).X + 5f);
            batch.DrawString(Fonts.Arial12Bold, pop2, TextCursor, tColor);

            PopRect = new Rectangle((int) TextCursor.X - 23, (int) TextCursor.Y - 3, 22, 22);
            batch.Draw(ResourceManager.Texture("UI/icon_pop_22"), PopRect, Color.White);

            PlanetTypeRichness = P.LocalizedRichness;
            PlanetTypeCursor =
                new Vector2(
                    PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f,
                    PlanetIconRect.Y + PlanetIconRect.Height + 5);
            batch.Draw(P.PlanetTexture, PlanetIconRect,
                Color.White);
            batch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            Rectangle fIcon = new Rectangle(200,
                Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_food").Height,
                ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            batch.Draw(ResourceManager.Texture("NewUI/icon_food"), fIcon, Color.White);
            TippedItem ti = new TippedItem
            {
                r = fIcon,
                TIP_ID = 20
            };
            ToolTipItems.Add(ti);
            Vector2 tcurs = new Vector2(fIcon.X + 25, Housing.Y + 205);
            float fertility = P.FertilityFor(EmpireManager.Player);
            batch.DrawString(Fonts.Arial12Bold, fertility.String(2), tcurs, tColor);
            
            float fertEnvMultiplier = EmpireManager.Player.RacialEnvModifer(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                var fertMultiplier = new Vector2(tcurs.X + Font12.MeasureString($"{fertility.String(2)} ").X, tcurs.Y+2);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }


            Rectangle pIcon = new Rectangle(300,
                Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - ResourceManager.Texture("NewUI/icon_production").Height,
                ResourceManager.Texture("NewUI/icon_production").Width,
                ResourceManager.Texture("NewUI/icon_production").Height);
            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), pIcon, Color.White);
            ti = new TippedItem
            {
                r = pIcon,
                TIP_ID = 21
            };
            ToolTipItems.Add(ti);

            AddUnExploredTips();
            tcurs = new Vector2(325f, Housing.Y + 205);
            batch.DrawString(Fonts.Arial12Bold, P.MineralRichness.String(), tcurs, tColor);

            int numHabitableTile    = P.TotalHabitableTiles;
            int numUnhabitableTiles = P.TileArea - numHabitableTile;
            float popPerTile        = P.BasePopPerTile * fertEnvMultiplier;
            float biospheredPop     = P.MaxPopulationBillionFor(EmpireManager.Player) + P.PopPerBiosphere * numUnhabitableTiles / 1000;

            DrawPlanetStats(TilesRect, $"{numHabitableTile}", "NewUI/icon_tiles", Color.White, Color.White);
            DrawPlanetStats(PopPerTileRect, $"{popPerTile.String(0)}m", "NewUI/icon_poppertile", Color.White, Color.White);
            DrawPlanetStats(BiospheredPopRect, biospheredPop.String(1), "NewUI/icon_biospheres", Color.White, Color.White);

            if (EmpireManager.Player.IsBuildingUnlocked(Building.TerraformerId))
            {
                float terraformedPop = P.PotentialMaxPopBillionsFor(EmpireManager.Player);
                DrawPlanetStats(TerraformedPopRect, terraformedPop.String(1),
                    "NewUI/icon_terraformer", Color.White, Color.White);
            }
            Mark = new Rectangle(RightRect.X - 10, Housing.Y + 150, 182, 25);
            Vector2 Text = new Vector2(RightRect.X + 25, Mark.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2);
            batch.Draw(ResourceManager.Texture("UI/dan_button_blue"), Mark, Color.White);
            if (GlobalStats.IsGermanOrPolish)
            {
                Text.X = Text.X - 9f;
            }

            bool marked = false;
            foreach (Goal g in EmpireManager.Player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget == null || g.ColonizationTarget != P)
                {
                    continue;
                }

                marked = true;
            }

            if (marked)
            {
                if (!Mark.HitTest(mousePos))
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text,
                        new Color(88, 108, 146));
                }
                else
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(1426), Text,
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
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text,
                        new Color(88, 108, 146));
                }
                else
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(1425), Text,
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
            batch.Draw(ResourceManager.Texture("UI/dan_button_blue"), SendTroops, Color.White);
            int troops = Screen.player
                .GetShips()
                .Where(troop => troop.HasOurTroops)
                .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet == P));
            if (!SendTroops.HitTest(mousePos))
                batch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text,
                    new Color(88, 108, 146)); // Localizer.Token(1425)
            else
                batch.DrawString(Fonts.Arial12Bold, String.Concat("Invading : ", troops), Text,
                    new Color(174, 202, 255)); // Localizer.Token(1425)

            Inspect.Draw(batch);
            Invade.Draw(batch);
            return false;
        }

        void AddExploredTips()
        {
            TippedItem ti = new TippedItem
            {
                r = DefenseRect,
                TIP_ID = 31
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = InjuryRect,
                TIP_ID = 249
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = OffenseRect,
                TIP_ID = 250
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = ShieldRect,
                TIP_ID = 264
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = DefenseShipsRect,
                TIP_ID = 251
            };
            ToolTipItems.Add(ti);
        }

        void AddUnExploredTips()
        {
            TippedItem ti = new TippedItem
            {
                r = TilesRect,
                TIP_ID = 1904
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = PopPerTileRect,
                TIP_ID = 1905
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = BiospheredPopRect,
                TIP_ID = 1906
            };
            ToolTipItems.Add(ti);
            ti = new TippedItem
            {
                r = TerraformedPopRect,
                TIP_ID = 1907
            };
            ToolTipItems.Add(ti);
        }

        void DrawPlanetStats(Rectangle rect, string data, string texturePath, Color color, Color texcolor)
        {
            SpriteFont font = Fonts.Arial12Bold;
            Vector2 pos     = new Vector2((rect.X + rect.Width + 2), (rect.Y + 11 - font.LineSpacing / 2));
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(texturePath), rect, texcolor);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, data, pos, color);
        }

        public override bool HandleInput(InputState input)
        {
            if (P == null)
            {
                return false;
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
                    if (g.ColonizationTarget == null || g.ColonizationTarget != P)
                    {
                        continue;
                    }
                    marked = true;
                    markedGoal = g;
                }
                if (marked)
                {
                    GameAudio.EchoAffirmative();
                    markedGoal.FinishedShip?.AI.ClearOrders();
                    EmpireManager.Player.GetEmpireAI().Goals.QueuePendingRemoval(markedGoal);
                    EmpireManager.Player.GetEmpireAI().Goals.ApplyPendingRemovals();
                }
                else
                {
                    GameAudio.EchoAffirmative();
                    EmpireManager.Player.GetEmpireAI().Goals.Add(new MarkForColonization(P, EmpireManager.Player));
                }
            }
            if (SendTroops.HitTest(input.CursorPosition) && input.InGameSelect)
            {

                if (EmpireManager.Player.GetTroopShipForRebase(out Ship troopShip, P))
                {
                    GameAudio.EchoAffirmative();
                    troopShip.AI.OrderLandAllTroops(P);
                }
                else
                    GameAudio.BlipClick();
            }

            if (Inspect.Hover)
            {
                if (P.Owner == null || P.Owner != EmpireManager.Player)
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
            if (P.Habitable)
            {
                if (Inspect.HandleInput(input))
                {
                    Screen.SnapViewColony(combatView: false);
                }
                if (Invade.HandleInput(input))
                {
                    Screen.SnapViewColony(combatView: true);
                }
            }
            if (!ElementRect.HitTest(input.CursorPosition))
            {
                return false;
            }

            if (AssignLabor != null && AssignLabor.HandleInput(input))
                return true;

            return true;
        }

        public void SetPlanet(Planet p)
        {
            if (P != p)
            {
                P = p;
                if (p != null && P.Owner == EmpireManager.Player)
                {
                    int x = PlanetIconRect.Right + 20;
                    var sliderRect = new RectF(x, PlanetIconRect.Y-40,
                                               ElementRect.Right-x-20, PlanetIconRect.Height+50);
                    AssignLabor = new AssignLaborComponent(p, sliderRect, useTitleFrame: false);
                }
                else
                {
                    AssignLabor = null;
                }
            };
        }

        struct TippedItem
        {
            public Rectangle r;
            public int TIP_ID;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class PlanetInfoUIElement : UIElement
    {
        Planet P;
        readonly UniverseScreen Screen;
        Empire Player => Screen.Player;
        readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        Rectangle MoneyRect;
        readonly Rectangle SendTroops;
        readonly Rectangle MarkedRect;
        readonly Rectangle CancelInvasionRect;
        readonly Rectangle ExoticRect;
        readonly Rectangle ExoticResourceIconRect;
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
        AssignLaborComponent AssignLabor;

        readonly Graphics.Font Font8  = Fonts.Arial8Bold;
        readonly Graphics.Font Font12 = Fonts.Arial12Bold;
        readonly Color ButtonTextColor   = new Color(174, 202, 255);
        readonly Color ButtonHoverColor  = new Color(88, 108, 146);
                                                                                  
        public PlanetInfoUIElement(in Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Screen = screen;
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

            SendTroops = new Rectangle(RightRect.X - 17, Housing.Y + 130, 182, 25);
            MarkedRect = new Rectangle(RightRect.X - 17, Housing.Y + 160, 182, 25);
            CancelInvasionRect = MarkedRect; // Replaces the colonization rect when invading
            ExoticRect = new Rectangle(leftRect.X + 15, Housing.Y + 140, 182, 25);
            ExoticResourceIconRect = new Rectangle(leftRect.X + 15, Housing.Y + 170, 20, 20);
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

            if (Screen.Debug)
                DrawDebugPlanetBudget();

            0f.SmoothStep(1f, TransitionPosition);
            ToolTipItems.Clear();
            ToolTipItems.Add(new TippedItem(PopRect, GameText.PopulationInBillionsVsMax));

            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            var namePos = new Vector2(Housing.X + 15, Housing.Y + 65);

            Graphics.Font font = Fonts.Arial8Bold;
            if (P.Name.Length < 12)      { font = Fonts.Arial20Bold; namePos.X += 15; }
            else if (P.Name.Length < 13) { font = Fonts.Arial12Bold; namePos.X += 10; }
            else if (P.Name.Length < 17) { font = Fonts.Arial10;     namePos.X += 5; }
           
            P.UpdateMaxPopulation();
            if (P.Owner == null || !P.IsExploredBy(Player))
            {
                DrawUnexploredUninhabited(namePos, Screen.Input.CursorPosition);
                return;
            }

            AddExploredTips();
            //Empire ownerforPlanetOrMining = 
            batch.DrawString(font, P.Name, namePos, P.Owner?.EmpireColor ?? tColor);
            batch.Draw(ResourceManager.Flag(P.Owner), FlagRect, P.Owner.EmpireColor);
            var cursor = new Vector2(Sel.Rect.X + Sel.Rect.Width - 65, namePos.Y + Fonts.Arial20Bold.LineSpacing / 2 - Fonts.Arial12Bold.LineSpacing / 2 + 2f);

            string pop = P.PopulationStringForPlayer;
            cursor.X -= (Fonts.Arial12Bold.MeasureString(pop).X + 5f);
            batch.DrawString(Fonts.Arial12Bold, pop, cursor, tColor);

            PopRect = new Rectangle((int)cursor.X - 23, (int)cursor.Y - 3, 22, 22);
            batch.Draw(ResourceManager.Texture("UI/icon_pop_22"), PopRect, Color.White);

            MoneyRect = new Rectangle(PopRect.X - 60, PopRect.Y, 22, 22);
            var moneyCursor = new Vector2((float)MoneyRect.X + 24, cursor.Y);

            if (P.Owner == Player)
            {
                string sNetIncome = P.Money.NetRevenue.String(2);
                batch.DrawString(Fonts.Arial12Bold, sNetIncome, moneyCursor, P.Money.NetRevenue > 0.0 ? Color.LightGreen : Color.Salmon);
                batch.Draw(ResourceManager.Texture("UI/icon_money_22"), MoneyRect, Color.White);
            }

            PlanetTypeRichness = P.LocalizedRichness;
            PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f, PlanetIconRect.Y + PlanetIconRect.Height + 5);
            batch.Draw(P.PlanetTexture, PlanetIconRect, Color.White);
            batch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            P.UpdateIncomes();

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

            DrawColonyType(batch);
            DrawFertProdStats(batch);
            DrawColonization(batch, Screen.Input.CursorPosition);
            DrawSendTroops(batch, Screen.Input.CursorPosition);
            Inspect.Draw(batch);
            Invade.Draw(batch);

            AssignLabor?.Draw(batch, elapsed);
        }

        void DrawDebugPlanetBudget()
        {
            if (P.Owner != null)
            {
                var budget = P.Owner.AI.PlanetBudgets?.Filter(b => b.P == P) ?? Array.Empty<PlanetBudget>();
                if (budget.Length == 1)
                    budget[0].DrawBudgetInfo(Screen);
            }
        }

        bool DrawUnexploredUninhabited(Vector2 namePos, Vector2 mousePos)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            if (!P.IsExploredBy(Player))
            {
                batch.DrawString(Fonts.Arial20Bold,
                    Localizer.Token(GameText.Unexplored) + P.LocalizedCategory, namePos, tColor);

                string text = Localizer.Token(GameText.SendAShipToThis);
                var cursor = new Vector2(Housing.X + 20, Housing.Y + 115);
                batch.DrawString(Fonts.Arial12Bold, text, cursor, tColor);
                return true;
            }

            if (!P.Habitable)
            {
                batch.DrawString(Fonts.Arial20Bold, P.Name, namePos, tColor);
                string text = Localizer.Token(GameText.ThisPlanetIsNotHabitable);
                Vector2 cursor = new Vector2(Housing.X + 20, Housing.Y + 110);
                batch.DrawString(Fonts.Arial12Bold, text, cursor, tColor);
                if (P.IsResearchable)
                    DrawResearchStation(batch, mousePos);
                else if (P.IsMineable)
                    DrawMiningOps(namePos, batch, mousePos);

                return true;
            }

            batch.DrawString(Fonts.Arial20Bold, P.Name, namePos, tColor);
            Vector2 textCursor = new Vector2(Sel.Rect.X + Sel.Rect.Width - 65,
                namePos.Y + Fonts.Arial20Bold.LineSpacing / 2f - Fonts.Arial12Bold.LineSpacing / 2f + 2f);

            string pop2 = P.PopulationStringForPlayer;
            textCursor.X -= (Fonts.Arial12Bold.MeasureString(pop2).X + 5f);
            batch.DrawString(Fonts.Arial12Bold, pop2, textCursor, tColor);

            PopRect = new Rectangle((int)textCursor.X - 23, (int)textCursor.Y - 3, 22, 22);
            batch.Draw(ResourceManager.Texture("UI/icon_pop_22"), PopRect, Color.White);

            PlanetTypeRichness = P.LocalizedRichness;
            PlanetTypeCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Arial12Bold.MeasureString(PlanetTypeRichness).X / 2f,
                                           PlanetIconRect.Y + PlanetIconRect.Height + 5);

            batch.Draw(P.PlanetTexture, PlanetIconRect,
                Color.White);
            batch.DrawString(Fonts.Arial12Bold, PlanetTypeRichness, PlanetTypeCursor, tColor);
            DrawFertProdStats(batch);
            AddUnExploredTips();

            float fertEnvMultiplier = Player.PlayerEnvModifier(P.Category);
            int numHabitableTile    = P.TotalHabitableTiles;
            float popPerTile        = P.BasePopPerTile * fertEnvMultiplier;
            float biospherePop      = P.PotentialMaxPopBillionsFor(Player, true);

            DrawPlanetStats(TilesRect, $"{numHabitableTile}", "NewUI/icon_tiles", Color.White, Color.White);
            DrawPlanetStats(PopPerTileRect, $"{popPerTile.String(0)}m", "NewUI/icon_poppertile", Color.White, Color.White);
            DrawPlanetStats(BiospheredPopRect, biospherePop.String(2), "NewUI/icon_biospheres", Color.White, Color.White);

            float terraformedPop = P.PotentialMaxPopBillionsWithTerraformFor(Player);
            DrawPlanetStats(TerraformedPopRect, terraformedPop.String(1),
                "NewUI/icon_terraformer", Color.White, Color.White);

            DrawColonization(batch, mousePos);
            DrawSendTroops(batch, mousePos);
            Inspect.Draw(batch);
            Invade.Draw(batch);
            return false;
        }

        void DrawSendTroops(SpriteBatch batch, Vector2 mousePos)
        {
            if (P.Owner == Player || P.Owner != null && !Player.IsAtWarWith(P.Owner))
                return; // Cannot send troops to this planet or different UI for player owner.

            Vector2 textPos        = new Vector2(SendTroops.X + 25, SendTroops.Y + 12 - Font12.LineSpacing / 2 - 2);
            int incomingTroops     = IncomingTroops;
            Color buttonBaseColor  = ButtonTextColor;
            Color buttonHoverColor = ButtonHoverColor;
            string texName         = "UI/dan_button_blue";
            string text = "Invade";
            if (P.Owner != null)
            {
                if (incomingTroops > 0)
                {
                    text             = $"Invading: {incomingTroops}";
                    buttonBaseColor  = Color.Red;
                    texName          = "UI/dan_button_red";
                    buttonHoverColor = Color.White;
                    DrawCancelInvasion(batch, mousePos);
                }
            }
            else
                text = incomingTroops > 0 ? $"Enroute: {incomingTroops}" : "Send Troops";

            batch.Draw(ResourceManager.Texture(texName), SendTroops, Color.White);
            batch.DrawString(Font12, text, textPos, SendTroops.HitTest(mousePos) ? buttonBaseColor
                                                                                 : buttonHoverColor);
        }

        void DrawCancelInvasion(SpriteBatch batch, Vector2 mousePos)
        {
            Vector2 textPos = new Vector2(RightRect.X - 12, CancelInvasionRect.Y + 12 - Font12.LineSpacing / 2 - 2);
            batch.Draw(ResourceManager.Texture("UI/dan_button_blue"), CancelInvasionRect, Color.White);
            batch.DrawString(Font12, "Cancel Invasion", textPos, CancelInvasionRect.HitTest(mousePos) ? ButtonTextColor
                                                                                                      : ButtonHoverColor);
        }

        void DrawColonyType(SpriteBatch batch)
        {
            Vector2 textPos = new Vector2(RightRect.X -15, RightRect.Y + 65);
            batch.DrawString(Fonts.Arial10, P.WorldType, textPos, tColor);
        }

        int IncomingTroops
        {
            get
            {
                // todo: double loop sum. 
                var ships = Screen.Player.OwnedShips;
                return ships
                    .Where(s => s != null && s.HasOurTroops &&
                                s.AI.OrderQueue.Any(g => g.Plan == ShipAI.Plan.LandTroop && g.TargetPlanet == P))
                    .Sum(s => s.TroopCount);
            }
        }

        void DrawColonization(SpriteBatch batch, Vector2 mousePos)
        {
            if (P.Owner != null)
                return;

            Vector2 textPos = new Vector2(RightRect.X + 18, MarkedRect.Y + 12 - Font12.LineSpacing / 2 - 2);
            batch.Draw(ResourceManager.Texture("UI/dan_button_blue"), MarkedRect, Color.White);

            LocalizedText tip = GameText.MarkThisPlanetForColonization;
            LocalizedText tipText = GameText.Colonize;
            if (Player.AI.HasGoal(g => g.IsColonizationGoal(P)))
            {
                tip = GameText.CancelTheColonizationMissionThat;
                tipText = GameText.CancelColonize;
            }

            ToolTipItems.Add(new TippedItem(MarkedRect, tip));
            batch.DrawString(Font12, tipText, textPos, MarkedRect.HitTest(mousePos) ? ButtonTextColor 
                                                                                    : ButtonHoverColor);
        }

        void DrawResearchStation(SpriteBatch batch, Vector2 mousePos)
        {
            if (P.IsResearchStationDeployedBy(Player))
                return;

            Vector2 textPos = new Vector2(ExoticRect.X + 13, ExoticRect.Y + 13 - Font12.LineSpacing / 2 - 2);
            batch.Draw(ResourceManager.Texture(Player.CanBuildResearchStations ? "NewUI/dan_button_blue_clear" 
                : "NewUI/dan_button_disabled"), ExoticRect, Color.White);

            LocalizedText tip = Player.CanBuildResearchStations ? GameText.DeployResearchStationTip : GameText.CannotBuildResearchStationTip;
            LocalizedText tipText = GameText.DeployResearchStation;
            if (Player.AI.HasGoal(g => g.IsResearchStationGoal(P)))
            {
                tip = GameText.CancelDeployResearchStationTip;
                tipText = GameText.CancelDeployResearchStation;
            }

            ToolTipItems.Add(new TippedItem(ExoticRect, tip));
            batch.DrawString(Font12, tipText, textPos, Player.CanBuildResearchStations ? ExoticRect.HitTest(mousePos) ? ButtonTextColor : ButtonHoverColor
                                                                                       : Color.Gray);
        }

        void DrawMiningOps(Vector2 namePos, SpriteBatch batch, Vector2 mousePos)
        {
            if (P.Mining.Owner != null)
            {
                batch.DrawString(Fonts.Arial20Bold, P.Name, namePos, P.Mining.Owner.EmpireColor);
                batch.Draw(ResourceManager.Flag(P.Mining.Owner), FlagRect, P.Mining.Owner.EmpireColor);
            }

            batch.Draw(P.Mining.ExoticResourceIcon, ExoticResourceIconRect);
            Vector2 resourceStatPos = new Vector2(ExoticResourceIconRect.X + 23, ExoticResourceIconRect.Y+2);
            Vector2 resourceStatDeployed = new Vector2(ExoticResourceIconRect.X + 23, ExoticResourceIconRect.Y + 19);
            Vector2 resourceStatInProgress = new Vector2(ExoticResourceIconRect.X + 23, ExoticResourceIconRect.Y + 34);
            string stats = $"{P.Mining.TranslatedResourceName.Text}: Richness " +
                $"{P.Mining.Richness}, Refine Ratio: {(P.Mining.RefiningRatio * Player.data.RefiningRatioMultiplier).UpperBound(1)}";
            batch.DrawString(Font12, stats, resourceStatPos, Color.White);

            int numDeployed = P.OrbitalStations.Filter(s => s.IsMiningStation && s.Loyalty == Player).Length;
            int numInProgress = Player.AI.CountGoals(g => g.IsMiningOpsGoal(P) && g.TargetShip == null);
            string statsDeployed = $"{numDeployed}/{Mineable.MaximumMiningStations} Deployed    ";
            batch.DrawString(Font12, statsDeployed, resourceStatDeployed, numDeployed > 0 ? Color.Green : Color.Gray);
            if (numInProgress > 0)
            {
                string statsInProgress = $"{numInProgress} In Progress";
                batch.DrawString(Font12, statsInProgress, resourceStatInProgress, Color.Gold);
            }
            ToolTipItems.Add(new TippedItem(ExoticResourceIconRect, $"{P.Mining.ResourceDescription.Text}\n{new LocalizedText(GameText.MineableRichnessTip).Text}"));
            if (P.Mining.Owner != null && P.Mining.Owner != Player)
                return;

            Vector2 textPos = new Vector2(ExoticRect.X + 13, ExoticRect.Y + 13 - Font12.LineSpacing / 2 - 2);
            batch.Draw(ResourceManager.Texture(Player.CanBuildMiningStations && P.Mining.CanAddMiningStationFor(Player) 
                ? "NewUI/dan_button_clear"
                : "NewUI/dan_button_disabled"), ExoticRect, Color.White);

            LocalizedText tip = Player.CanBuildMiningStations ? GameText.DeployMiningStationTip : GameText.CannotBuildMiningStationTip;
            LocalizedText tipText = P.Mining.Owner != null && P.Mining.Owner != Player ? GameText.CannotDeployMiningStationNotOwnerTip : GameText.DeployMiningStation;


            ToolTipItems.Add(new TippedItem(ExoticRect, tip));
            batch.DrawString(Font12, tipText, textPos, Player.CanBuildMiningStations ? ExoticRect.HitTest(mousePos) ? Color.Gold : Color.LightYellow
                                                                                     : Color.Gray);
        }

        void DrawFertProdStats(SpriteBatch batch)
        {
            var foodTex = ResourceManager.Texture("NewUI/icon_food");
            var fIcon = new Rectangle(200,Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - foodTex.Height, foodTex.Width, foodTex.Height);
            batch.Draw(foodTex, fIcon, Color.White);
            ToolTipItems.Add(new TippedItem(fIcon, GameText.IndicatesHowMuchFoodThis));

            var tcurs = new Vector2(fIcon.X + 25, Housing.Y + 205);
            float fertility   = P.FertilityFor(Player);
            float maxFert     = P.MaxFertilityFor(Player);
            string fertString = fertility.AlmostEqual(maxFert) ? fertility.String(2) : $"{fertility.String(2)}/{maxFert.String(2)}";
            batch.DrawString(Fonts.Arial12Bold, fertString, tcurs, tColor);

            float fertEnvMultiplier = Player.PlayerEnvModifier(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                var fertMultiplier = new Vector2(tcurs.X + Font12.MeasureString(fertString).X + 3, tcurs.Y + 2);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }

            var prodTex = ResourceManager.Texture("NewUI/icon_production");
            var pIcon = new Rectangle(325, Housing.Y + 210 + Fonts.Arial12Bold.LineSpacing - prodTex.Height, prodTex.Width, prodTex.Height);
            batch.Draw(prodTex, pIcon, Color.White);
            ToolTipItems.Add(new TippedItem(pIcon, GameText.APlanetsMineralRichnessDirectly));

            tcurs = new Vector2(350f, Housing.Y + 205);
            batch.DrawString(Fonts.Arial12Bold, P.MineralRichness.String(), tcurs, tColor);
        }

        void AddExploredTips()
        {
            ToolTipItems.Add(new TippedItem(DefenseRect, GameText.IndicatesThisColonysTotalStrength));
            ToolTipItems.Add(new TippedItem(InjuryRect, GameText.EveryTroopInvadingThisPlanet));
            ToolTipItems.Add(new TippedItem(OffenseRect, GameText.ThePlanetsSpaceOffenseVs));
            ToolTipItems.Add(new TippedItem(ShieldRect, GameText.IndicatesTheCurrentStrengthOf));
            ToolTipItems.Add(new TippedItem(DefenseShipsRect, GameText.IndicatesTheTileRangeThis));
        }

        void AddUnExploredTips()
        {
            ToolTipItems.Add(new TippedItem(TilesRect, GameText.ThisIndicatesHowManyTiles));
            ToolTipItems.Add(new TippedItem(PopPerTileRect, GameText.ThisIndicatesHowMuchPopulation));
            ToolTipItems.Add(new TippedItem(BiospheredPopRect, GameText.ThisIndicatesWhatWouldThe));
            ToolTipItems.Add(new TippedItem(TerraformedPopRect, GameText.ThisIndicatesWhatWouldThe2));
        }

        void DrawPlanetStats(Rectangle rect, string data, string texturePath, Color color, Color texColor)
        {
            Graphics.Font font = Fonts.Arial12Bold;
            Vector2 pos     = new Vector2((rect.X + rect.Width + 2), (rect.Y + 11 - font.LineSpacing / 2));
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(texturePath), rect, texColor);
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
                if (ti.Rect.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(ti.Tooltip);
            }
            if (P.Owner == null && MarkedRect.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (Player.AI.HasGoal(g => g.IsColonizationGoal(P)))
                {
                    Player.AI.CancelColonization(P);
                    GameAudio.EchoAffirmative();
                }
                else
                {
                    GameAudio.EchoAffirmative();
                    Player.AI.AddGoalAndEvaluate(new MarkForColonization(P, Player, isManual:true));
                }
            }
            if (SendTroops.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (Player.GetTroopShipForRebase(out Ship troopShip, P.Position, P.Name))
                {
                    GameAudio.EchoAffirmative();
                    troopShip.AI.OrderLandAllTroops(P, clearOrders:true);
                    if (Player.Universe.Paused) 
                        Player.Universe.Objects.UpdateLists();
                }
                else
                    GameAudio.BlipClick();
            }
            if (P.IsResearchable && ExoticRect.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if      (Player.AI.HasGoal(g => g.IsResearchStationGoal(P))) Player.AI.CancelResearchStation(P);
                else if (Player.CanBuildResearchStations)                    Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, P));
                else                                                         GameAudio.NegativeClick();

                GameAudio.EchoAffirmative();
            }
            else if (P.IsMineable && ExoticRect.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (P.Mining.CanAddMiningStationFor(Player)) Player.AI.AddGoalAndEvaluate(new MiningOps(Player, P));
                else GameAudio.NegativeClick();
            }

            if (P.Owner != null 
                && !P.IsResearchable
                && !P.IsMineable
                && P.Owner != Player 
                && CancelInvasionRect.HitTest(input.CursorPosition) 
                && input.InGameSelect)
            {
                var shipList = Player.OwnedShips;
                foreach (Ship ship in shipList)
                {
                    if (ship.AI.State == AIState.AssaultPlanet && ship.AI.OrderQueue.Any(g => g.TargetPlanet == P))
                    {
                        if (ship.DesignRole == RoleName.troopShip)
                            ship.AI.OrderOrbitNearest(true);
                        else
                            ship.AI.OrderRebaseToNearest();
                    }
                }
            }

            if (Inspect.Hover)
            {
                if (P.Owner == null || P.Owner != Player)
                {
                    ToolTip.CreateTooltip(GameText.ViewPlanetDetails);
                }
                else
                {
                    ToolTip.CreateTooltip(GameText.OpensColonyOverviewScreen);
                }
            }
            if (Invade.Hover)
            {
                ToolTip.CreateTooltip(GameText.OpenTheGroundAssaultView);
            }
            if (P.Habitable || P.Universe.Debug)
            {
                if (Inspect.HandleInput(input))
                {
                    Screen.SnapViewColony(P, combatView: false);
                }
                if (Invade.HandleInput(input))
                {
                    Screen.SnapViewColony(P, combatView: true);
                }
            }
            if (!ElementRect.HitTest(input.CursorPosition))
                return false;

            if (AssignLabor != null && AssignLabor.HandleInput(input))
                return true;

            return true;
        }

        public void SetPlanet(Planet p)
        {
            if (P != p)
            {
                P = p;
                if (p != null && P.Owner == Player)
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
    }
}

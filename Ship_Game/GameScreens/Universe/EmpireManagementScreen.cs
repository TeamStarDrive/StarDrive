using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class EmpireManagementScreen : GameScreen
    {
        public readonly UniverseScreen Universe;
        EmpireUIOverlay eui;
        private readonly ScrollList<ColoniesListItem> ColoniesList;
        private readonly GovernorDetailsComponent GovernorDetails;
        private readonly RectF ERect;

        private readonly SortButton SbPop;
        private readonly SortButton SbFood;
        private readonly SortButton SbProd;
        private readonly SortButton SbRes;
        private readonly SortButton SbMoney;

        readonly UILabel AvailableTroops;
        readonly UILabel TroopConsumption;
        private RectF GovernorRect;

        private readonly Color Cream           = Colors.Cream;
        private readonly Color White           = Color.White;
        private readonly Graphics.Font NormalFont = Fonts.Arial20Bold;

        public Planet SelectedPlanet { get; private set; }
        
        public EmpireManagementScreen(UniverseScreen parent, EmpireUIOverlay empUI)
            : base(parent, toPause: parent)
        {
            Universe = parent;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            eui = empUI;

            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            Add(new Menu2(titleRect));
            Add(new UILabel(titleRect, GameText.EmpireManagement, Fonts.Laserian14, Cream)
                { TextAlign = TextAlign.Center });

            var mainBkg = new Rectangle(2, titleRect.Bottom + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);
            Add(new Menu2(mainBkg));
            Add(new CloseButton(mainBkg.Right - 40, mainBkg.Y + 20));

            ERect = new(mainBkg.X + 20, titleRect.Bottom + 30, ScreenWidth - 40, (0.7f * mainBkg.Height).RoundUpTo(40));
            RectF colonies = new(ERect.X, ERect.Y + 15, ERect.W, ERect.H - 15);
            ColoniesList = Add(new ScrollList<ColoniesListItem>(colonies, 80));
            ColoniesList.OnClick       = OnColonyListItemClicked;
            ColoniesList.OnDoubleClick = OnColonyListItemDoubleClicked;
            ColoniesList.EnableItemHighlight = true;

            SbPop   = new SortButton(eui.Player.data.ESSort, "pop");
            SbFood  = new SortButton(eui.Player.data.ESSort, "food");
            SbProd  = new SortButton(eui.Player.data.ESSort, "prod");
            SbRes   = new SortButton(eui.Player.data.ESSort, "res");
            SbMoney = new SortButton(eui.Player.data.ESSort, "money");

            var planets = Universe.Player.GetPlanets();
            int sidePanelWidths = (int)(ScreenWidth * 0.3f);
            GovernorRect = new RectF(ColoniesList.Right - sidePanelWidths - 23, ColoniesList.Bottom - 5, sidePanelWidths, ScreenHeight - ColoniesList.Bottom - 22);
            GovernorDetails = Add(new GovernorDetailsComponent(this, planets[0], GovernorRect));
            ResetColoniesList(planets);
            int totalTroops = Universe.Player.TotalTroops();
            string troopText = $"Total Troops: {totalTroops}";
            Vector2 troopPos = new(titleRect.X + titleRect.Width + 17, titleRect.Y + 35);
            AvailableTroops = Add(new UILabel(troopPos, troopText, LowRes ? Fonts.Arial12Bold : Fonts.Arial20Bold, Color.White));
            if (totalTroops > 0)
            {
                string consumption = $"Consuming {(totalTroops * Troop.Consumption * (1 + Universe.Player.data.Traits.ConsumptionModifier)).String(1)} " +
                                     $"{Localizer.Token(Universe.Player.IsCybernetic ? GameText.Production : GameText.Food)}";

                Vector2 consumptionPos = new(troopPos.X, troopPos.Y + 25);
                TroopConsumption = Add(new UILabel(consumptionPos, consumption, LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold,
                    Universe.Player.IsCybernetic ? Color.SandyBrown : Color.Green));
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();

            base.Draw(batch, elapsed);
            
            var PlanetInfoRect = new Rectangle((int)ERect.X + 22, (int)(ERect.Y + ERect.H), (int)(ScreenWidth * 0.3f), (int)(ScreenHeight - ERect.Y - ERect.H - 22));
            int iconSize = PlanetInfoRect.X + PlanetInfoRect.Height - (int)((PlanetInfoRect.X + PlanetInfoRect.Height) * 0.4f);
            var PlanetIconRect = new Rectangle(PlanetInfoRect.X + 10, PlanetInfoRect.Y + PlanetInfoRect.Height / 2 - iconSize / 2, iconSize, iconSize);
            var nameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Pirulen16.MeasureString(SelectedPlanet.Name).X / 2f, PlanetInfoRect.Y + 15);
            batch.Draw(SelectedPlanet.PlanetTexture, PlanetIconRect, White);
            batch.DrawString(Fonts.Pirulen16, SelectedPlanet.Name, nameCursor, White);
            
            var PNameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width + 5, nameCursor.Y + 20f);
            var InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Class)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.CategoryName, InfoCursor, Cream);
            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Population)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.PopulationStringForPlayer, InfoCursor, Cream);
            var hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Population)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(Input.CursorPosition))
                ToolTip.CreateTooltip(GameText.AColonysPopulationIsA);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Fertility)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.FertilityFor(Universe.Player).String(), InfoCursor, Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Fertility)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
                ToolTip.CreateTooltip(GameText.IndicatesHowMuchFoodThis);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Richness)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.MineralRichness.String(), InfoCursor, Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Richness)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(GameText.APlanetsMineralRichnessDirectly);
            }

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2) * 2;

            string text = Fonts.Arial12Bold.ParseText(SelectedPlanet.Description, PlanetInfoRect.Width - PlanetIconRect.Width + 15);
            if (Fonts.Arial12Bold.MeasureString(text).Y + PNameCursor.Y <= ScreenHeight - 20)
            {
                batch.DrawString(Fonts.Arial12Bold, text, PNameCursor, White);
            }
            else
            {
                batch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(SelectedPlanet.Description, PlanetInfoRect.Width - PlanetIconRect.Width + 15), PNameCursor, Color.White);
            }

            ColoniesListItem e1 = ColoniesList.ItemAtTop;
            var MapRect = new Rectangle(PlanetInfoRect.Right - 20, PlanetInfoRect.Y - 3, e1.QueueRect.X - PlanetInfoRect.Right, PlanetInfoRect.Height);
            int desiredWidth = 700;
            int desiredHeight = 500;
            var buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight);
            while (!MapRect.Contains(buildingsRect))
            {
                desiredWidth -= 7;
                desiredHeight -= 5;
                buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight);
            }
            buildingsRect = new Rectangle(MapRect.CenterX() - desiredWidth/2, MapRect.Y, desiredWidth, desiredHeight);
            MapRect.X = buildingsRect.X;
            MapRect.Width = buildingsRect.Width;
            int xSize = buildingsRect.Width / 7;
            int ySize = buildingsRect.Height / 5;

            batch.Draw(ResourceManager.Texture("PlanetTiles/" + SelectedPlanet.PlanetTileId), buildingsRect, White);
            batch.DrawRectangle(MapRect, new Color(118, 102, 67, 255));

            foreach (PlanetGridSquare tile in SelectedPlanet.TilesList)
            {
                var rect = new Rectangle(buildingsRect.X + tile.X * xSize, buildingsRect.Y + tile.Y * ySize, xSize, ySize);

                if (!tile.Habitable)
                {
                    batch.FillRectangle(rect, new Color(0, 0, 0, 200));
                }
                batch.DrawRectangle(rect, new Color(211, 211, 211, 100), 0.5f);

                if (tile.Building != null)
                {
                    Color c = tile.QItem != null ? White : new Color(White, 128);
                    batch.Draw(tile.Building.IconTex, rect.CenterF - new Vector2(18), new Vector2(36), c);
                }

                DrawTileIcons(tile, rect);
            }

            // draw some border around the governor component
            /*
            var GovernorRect = new Rectangle(MapRect.Right, MapRect.Y, e1.Rect.Right - MapRect.Right, MapRect.Height);
            batch.DrawRectangle(GovernorRect, new Color(118, 102, 67, 255));*/

            if (ColoniesList.NumEntries > 0)
            {
                ColoniesListItem entry = ColoniesList.ItemAtTop;
                var textCursor         = new Vector2(entry.SysNameRect.X + 30, ERect.Y);
                SubTexture iconPop     = ResourceManager.Texture("UI/icon_pop");
                SubTexture iconFood    = ResourceManager.Texture("NewUI/icon_food");
                SubTexture iconProd    = ResourceManager.Texture("NewUI/icon_production");
                SubTexture iconRes     = ResourceManager.Texture("NewUI/icon_science");
                SubTexture iconMoney   = ResourceManager.Texture("NewUI/icon_money");

                batch.DrawString(NormalFont, Localizer.Token(GameText.System), textCursor, Cream);
                textCursor = new Vector2(entry.PlanetNameRect.X + 30, ERect.Y);
                batch.DrawString(NormalFont, Localizer.Token(GameText.Planet), textCursor, Cream);
                SbPop.rect   = DrawStatTexture(entry.PopRect.X, (int)textCursor.Y, iconPop);
                SbFood.rect  = DrawStatTexture(entry.FoodRect.X, (int)textCursor.Y, iconFood);
                SbProd.rect  = DrawStatTexture(entry.ProdRect.X, (int)textCursor.Y, iconProd);
                SbRes.rect   = DrawStatTexture(entry.ResRect.X, (int)textCursor.Y, iconRes);
                SbMoney.rect = DrawStatTexture(entry.MoneyRect.X, (int)textCursor.Y, iconMoney);
                batch.Draw(iconPop, SbPop.rect, White);
                batch.Draw(iconFood, SbFood.rect, White);
                batch.Draw(iconProd, SbProd.rect, White);
                batch.Draw(iconRes, SbRes.rect, White);
                batch.Draw(iconMoney, SbMoney.rect, White);
                textCursor = new Vector2(entry.SliderRect.X + 30, ERect.Y);
                batch.DrawString(NormalFont, Localizer.Token(GameText.Labor), textCursor, Cream);
                textCursor = new Vector2(entry.StorageRect.X + 30, ERect.Y);
                batch.DrawString(NormalFont, Localizer.Token(GameText.Storage2), textCursor, Cream);
                textCursor = new Vector2(entry.QueueRect.X + 30, ERect.Y);
                batch.DrawString(NormalFont, Localizer.Token(GameText.Construction2), textCursor, Cream);
            }

            var lineColor = new Color(118, 102, 67, 255);
            float columnTop = ERect.Y + 35;
            float columnBot = PlanetInfoRect.Y - 20;

            var topLeftSL = new Vector2(e1.PlanetNameRect.X, columnTop);
            var botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.PopRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.FoodRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.ProdRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.ResRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.MoneyRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.SliderRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.StorageRect.X + 5, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.QueueRect.X, columnTop);
            botSL     = new Vector2(topLeftSL.X, columnBot);
            batch.DrawLine(topLeftSL, botSL, lineColor);

            batch.DrawRectangle(ColoniesList.ItemsHousing, lineColor); // items housing border

            var pos = new Vector2(ScreenWidth - Fonts.Pirulen16.TextWidth("Paused") - 13f, 44f);
            batch.DrawString(Fonts.Pirulen16, "Paused", pos, White);
            batch.SafeEnd();
        }

        Rectangle DrawStatTexture(int x, int y, SubTexture icon)
        {
            return new Rectangle(x + 15 - icon.Width / 2, y, icon.Width, icon.Height);
        }

        void DrawTileIcons(PlanetGridSquare pgs, Rectangle rect)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(rect.X, rect.Y, 10, 10);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, White);
                ScreenManager.SpriteBatch.FillRectangle(rect, Universe.Player.EmpireColor.Alpha(0.4f));
            }

            if (Universe.Player.IsBuildingUnlocked(Building.TerraformerId) && (pgs.CanTerraform || pgs.BioCanTerraform))
            {
                var terraform = new Rectangle(rect.X + rect.Width - 10, rect.Y, 10, 10);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraform, Color.White);
            }
        }

        void OnColonyListItemClicked(ColoniesListItem item)
        {
            SelectedPlanet = item.P;
            GovernorDetails.SetPlanetDetails(SelectedPlanet, GovernorRect, (int)GovernorDetails?.CurrentTabIndex);
            GovernorDetails.PerformLayout();
        }

        void OnColonyListItemDoubleClicked(ColoniesListItem item)
        {
            Universe.SelectedPlanet = item.P;
            Universe.SnapViewColony(combatView: false);
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.U) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            HandleSortButton(input, SbPop, GameText.IndicatesThisColonysCurrentPopulation, p => p.PopulationBillion);
            HandleSortButton(input, SbFood, GameText.TheNetAmountOfFood, p => p.Food.NetIncome);
            HandleSortButton(input, SbProd, GameText.TheNetAmountOfProduction, p => p.Prod.NetIncome);
            HandleSortButton(input, SbRes, GameText.TheNetAmountOfResearch, p => p.Res.NetIncome);
            HandleSortButton(input, SbMoney, GameText.TheNetIncomeOfThis, p => p.Money.NetRevenue);

            return base.HandleInput(input);
        }

        void HandleSortButton(InputState input, SortButton button, LocalizedText tooltip, Func<Planet, float> selector)
        {
            if (button.rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(tooltip);
            }
            if (button.HandleInput(input))
            {
                var planets = Universe.Player.GetPlanets();
                button.Ascending = !button.Ascending;
                ResetColoniesList(button.Ascending
                    ? planets.OrderBy(selector)
                    : planets.OrderByDescending(selector));
            }
        }

        void ResetColoniesList(IEnumerable<Planet> sortedList)
        {
            ColoniesList.Reset();
            foreach (Planet p in sortedList)
            {
                ColoniesList.AddItem(new ColoniesListItem(this, p));
            }

            SelectedPlanet = ColoniesList.AllEntries[0].P;
            GovernorDetails.SetPlanetDetails(SelectedPlanet, GovernorRect, (int)GovernorDetails?.CurrentTabIndex);
            GovernorDetails.PerformLayout();
        }
    }
}

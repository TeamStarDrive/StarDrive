using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class EmpireManagementScreen : GameScreen
    {
        EmpireUIOverlay eui;
        private readonly ScrollList2<ColoniesListItem> ColoniesList;
        private readonly GovernorDetailsComponent GovernorDetails;
        private readonly Rectangle eRect;

        private readonly SortButton SbPop;
        private readonly SortButton SbFood;
        private readonly SortButton SbProd;
        private readonly SortButton SbRes;
        private readonly SortButton SbMoney;

        private readonly Color Cream           = Colors.Cream;
        private readonly Color White           = Color.White;
        private readonly SpriteFont NormalFont = Fonts.Arial20Bold;

        public Planet SelectedPlanet { get; private set; }

        public EmpireManagementScreen(GameScreen parent, EmpireUIOverlay empUI) : base(parent)
        {
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            eui = empUI;

            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            Add(new Menu2(titleRect));
            Add(new UILabel(titleRect, GameText.EmpireManagement, Fonts.Laserian14, Cream)
                { Align = TextAlign.Center });

            var mainBkg = new Rectangle(2, titleRect.Bottom + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);
            Add(new Menu2(mainBkg));
            Add(new CloseButton(mainBkg.Right - 40, mainBkg.Y + 20));

            ColoniesList = Add(new ScrollList2<ColoniesListItem>(mainBkg.X + 20, titleRect.Bottom + 30,
                                                                ScreenWidth - 40, (0.7f * mainBkg.Height).RoundUpTo(40), 80));
            ColoniesList.OnClick       = OnColonyListItemClicked;
            ColoniesList.OnDoubleClick = OnColonyListItemDoubleClicked;
            ColoniesList.EnableItemHighlight = true;
            eRect = ColoniesList.Rect;

            SbPop   = new SortButton(eui.empire.data.ESSort, "pop");
            SbFood  = new SortButton(eui.empire.data.ESSort, "food");
            SbProd  = new SortButton(eui.empire.data.ESSort, "prod");
            SbRes   = new SortButton(eui.empire.data.ESSort, "res");
            SbMoney = new SortButton(eui.empire.data.ESSort, "money");

            var planets = EmpireManager.Player.GetPlanets();
            int sidePanelWidths = (int)(ScreenWidth * 0.3f);
            var governorRect = new RectF(ColoniesList.Right - sidePanelWidths - 20, ColoniesList.Bottom, sidePanelWidths, ScreenHeight - ColoniesList.Bottom - 22);
            GovernorDetails = Add(new GovernorDetailsComponent(this, planets[0], governorRect, governorVideo: false));
            ResetColoniesList(planets);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();

            base.Draw(batch, elapsed);
            
            var PlanetInfoRect = new Rectangle(eRect.X + 22, eRect.Y + eRect.Height, (int)(ScreenWidth * 0.3f), ScreenHeight - eRect.Y - eRect.Height - 22);
            int iconSize = PlanetInfoRect.X + PlanetInfoRect.Height - (int)((PlanetInfoRect.X + PlanetInfoRect.Height) * 0.4f);
            var PlanetIconRect = new Rectangle(PlanetInfoRect.X + 10, PlanetInfoRect.Y + PlanetInfoRect.Height / 2 - iconSize / 2, iconSize, iconSize);
            var nameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Pirulen16.MeasureString(SelectedPlanet.Name).X / 2f, PlanetInfoRect.Y + 15);
            batch.Draw(SelectedPlanet.PlanetTexture, PlanetIconRect, White);
            batch.DrawString(Fonts.Pirulen16, SelectedPlanet.Name, nameCursor, White);
            
            var PNameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width + 5, nameCursor.Y + 20f);
            var InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(384)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.CategoryName, InfoCursor, Cream);
            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(385)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.PopulationStringForPlayer, InfoCursor, Cream);
            var hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(Input.CursorPosition))
                ToolTip.CreateTooltip(75);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.FertilityFor(EmpireManager.Player).String(), InfoCursor, Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
                ToolTip.CreateTooltip(20);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.MineralRichness.String(), InfoCursor, Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(21);
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
            var MapRect = new Rectangle(PlanetInfoRect.Right, PlanetInfoRect.Y, e1.QueueRect.X - PlanetInfoRect.Right, PlanetInfoRect.Height);
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
                var rect = new Rectangle(buildingsRect.X + tile.x * xSize, buildingsRect.Y + tile.y * ySize, xSize, ySize);

                if (!tile.Habitable)
                {
                    batch.FillRectangle(rect, new Color(0, 0, 0, 200));
                }
                batch.DrawRectangle(rect, new Color(211, 211, 211, 100), 0.5f);

                if (tile.building != null)
                {
                    Color c = tile.QItem != null ? White : new Color(White, 128);
                    batch.Draw(tile.building.IconTex, rect.Center() - new Vector2(18), new Vector2(36), c);
                }

                DrawTileIcons(tile, rect);
            }

            // draw some border around the governor component
            var GovernorRect = new Rectangle(MapRect.Right, MapRect.Y, e1.Rect.Right - MapRect.Right, MapRect.Height);
            batch.DrawRectangle(GovernorRect, new Color(118, 102, 67, 255));

            if (ColoniesList.NumEntries > 0)
            {
                ColoniesListItem entry = ColoniesList.ItemAtTop;
                var textCursor         = new Vector2(entry.SysNameRect.X + 30, eRect.Y);
                SubTexture iconPop     = ResourceManager.Texture("UI/icon_pop");
                SubTexture iconFood    = ResourceManager.Texture("NewUI/icon_food");
                SubTexture iconProd    = ResourceManager.Texture("NewUI/icon_production");
                SubTexture iconRes     = ResourceManager.Texture("NewUI/icon_science");
                SubTexture iconMoney   = ResourceManager.Texture("NewUI/icon_money");

                batch.DrawString(NormalFont, Localizer.Token(192), textCursor, Cream);
                textCursor = new Vector2(entry.PlanetNameRect.X + 30, eRect.Y);
                batch.DrawString(NormalFont, Localizer.Token(389), textCursor, Cream);
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
                textCursor = new Vector2(entry.SliderRect.X + 30, eRect.Y);
                batch.DrawString(NormalFont, Localizer.Token(390), textCursor, Cream);
                textCursor = new Vector2(entry.StorageRect.X + 30, eRect.Y);
                batch.DrawString(NormalFont, Localizer.Token(391), textCursor, Cream);
                textCursor = new Vector2(entry.QueueRect.X + 30, eRect.Y);
                batch.DrawString(NormalFont, Localizer.Token(392), textCursor, Cream);
            }

            var lineColor = new Color(118, 102, 67, 255);
            int columnTop = eRect.Y + 35;
            int columnBot = PlanetInfoRect.Y - 20;

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
            batch.End();
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
                ScreenManager.SpriteBatch.FillRectangle(rect, EmpireManager.Player.EmpireColor.Alpha(0.4f));
            }

            if (EmpireManager.Player.IsBuildingUnlocked(Building.TerraformerId) && (pgs.CanTerraform || pgs.BioCanTerraform))
            {
                var terraform = new Rectangle(rect.X + rect.Width - 10, rect.Y, 10, 10);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraform, Color.White);
            }
        }

        void OnColonyListItemClicked(ColoniesListItem item)
        {
            SelectedPlanet = item.p;
            GovernorDetails.SetPlanetDetails(SelectedPlanet);
            GovernorDetails.PerformLayout();
        }

        void OnColonyListItemDoubleClicked(ColoniesListItem item)
        {
            Empire.Universe.SelectedPlanet = item.p;
            Empire.Universe.SnapViewColony(combatView: false);
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

            HandleSortButton(input, SbPop, 2278, p => p.PopulationBillion);
            HandleSortButton(input, SbFood, 2174, p => p.Food.NetIncome);
            HandleSortButton(input, SbProd, 2175, p => p.Prod.NetIncome);
            HandleSortButton(input, SbRes, 2176, p => p.Res.NetIncome);
            HandleSortButton(input, SbMoney, 2177, p => p.Money.NetRevenue);

            return base.HandleInput(input);
        }

        void HandleSortButton(InputState input, SortButton button, int tooltip, Func<Planet, float> selector)
        {
            if (button.rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(tooltip));
            }
            if (button.HandleInput(input))
            {
                var planets = EmpireManager.Player.GetPlanets();
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

            SelectedPlanet = ColoniesList.AllEntries[0].p;
            GovernorDetails.SetPlanetDetails(SelectedPlanet);
            GovernorDetails.PerformLayout();
        }
    }
}
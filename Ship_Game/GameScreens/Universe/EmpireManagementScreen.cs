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

        Menu2 TitleBar;
        Vector2 TitlePos;
        Menu2 EMenu;

        ScrollList<ColoniesListItem> ColoniesList;
        GovernorDetailsComponent GovernorDetails;
        Rectangle leftRect;
        Rectangle eRect;

        SortButton pop;
        SortButton food;
        SortButton prod;
        SortButton res;
        SortButton money;

        public Planet SelectedPlanet { get; private set; }


        public EmpireManagementScreen(GameScreen parent, EmpireUIOverlay empUI) : base(parent)
        {
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            eui = empUI;

            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.CenterX() - Fonts.Laserian14.MeasureString(Localizer.Token(383)).X / 2f,
                                   titleRect.CenterY() - Fonts.Laserian14.LineSpacing / 2);
            leftRect = new Rectangle(2, titleRect.Bottom + 5, ScreenWidth - 10, ScreenHeight - titleRect.Bottom - 7);

            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            EMenu = new Menu2(leftRect);
            eRect = new Rectangle(2, titleRect.Bottom + 25, ScreenWidth - 40, (int)(0.66f * (ScreenHeight - titleRect.Bottom - 7)));
            eRect.Height = eRect.Height.RoundDownToMultipleOf(80);

            ColoniesList = Add(new ScrollList<ColoniesListItem>(eRect, 80));
            ColoniesList.OnClick = OnColonyListItemClicked;
            ColoniesList.OnDoubleClick = OnColonyListItemDoubleClicked;

            pop   = new SortButton(eui.empire.data.ESSort, "pop");
            food  = new SortButton(eui.empire.data.ESSort, "food");
            prod  = new SortButton(eui.empire.data.ESSort, "prod");
            res   = new SortButton(eui.empire.data.ESSort, "res");
            money = new SortButton(eui.empire.data.ESSort, "money");

            var planets = EmpireManager.Player.GetPlanets();
            int sidePanelWidths = (int)(ScreenWidth * 0.3f);
            var governorRect = new Rectangle(eRect.Right - sidePanelWidths - 20, eRect.Bottom, sidePanelWidths, ScreenHeight - eRect.Bottom - 22);
            GovernorDetails = Add(new GovernorDetailsComponent(this, planets[0], governorRect, governorVideo: false));
            ResetColoniesList(planets);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw(batch);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(383), TitlePos, Colors.Cream);
            EMenu.Draw(batch);

            base.Draw(batch);
            
            var PlanetInfoRect = new Rectangle(eRect.X + 22, eRect.Y + eRect.Height, (int)(ScreenWidth * 0.3f), ScreenHeight - eRect.Y - eRect.Height - 22);
            int iconSize = PlanetInfoRect.X + PlanetInfoRect.Height - (int)((PlanetInfoRect.X + PlanetInfoRect.Height) * 0.4f);
            var PlanetIconRect = new Rectangle(PlanetInfoRect.X + 10, PlanetInfoRect.Y + PlanetInfoRect.Height / 2 - iconSize / 2, iconSize, iconSize);
            var nameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width / 2 - Fonts.Pirulen16.MeasureString(SelectedPlanet.Name).X / 2f, PlanetInfoRect.Y + 15);
            batch.Draw(SelectedPlanet.PlanetTexture, PlanetIconRect, Color.White);
            batch.DrawString(Fonts.Pirulen16, SelectedPlanet.Name, nameCursor, Color.White);
            
            var PNameCursor = new Vector2(PlanetIconRect.X + PlanetIconRect.Width + 5, nameCursor.Y + 20f);
            var InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(384)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.CategoryName, InfoCursor, Colors.Cream);
            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(385)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.PopulationStringForPlayer, InfoCursor, Colors.Cream);
            var hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(Input.CursorPosition))
                ToolTip.CreateTooltip(75);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.FertilityFor(EmpireManager.Player).String(), InfoCursor, Colors.Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
                ToolTip.CreateTooltip(20);

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387)+":", PNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, SelectedPlanet.MineralRichness.String(), InfoCursor, Colors.Cream);
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387)+":").X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(21);
            }

            PNameCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2) * 2;

            string text = Fonts.Arial12Bold.ParseText(SelectedPlanet.Description, PlanetInfoRect.Width - PlanetIconRect.Width + 15);
            if (Fonts.Arial12Bold.MeasureString(text).Y + PNameCursor.Y <= ScreenHeight - 20)
            {
                batch.DrawString(Fonts.Arial12Bold, text, PNameCursor, Color.White);
            }
            else
            {
                batch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(SelectedPlanet.Description, PlanetInfoRect.Width - PlanetIconRect.Width + 15), PNameCursor, Color.White);
            }

            ColoniesListItem e1 = ColoniesList.FirstItem;
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
            int xsize = buildingsRect.Width / 7;
            int ysize = buildingsRect.Height / 5;

            var pgs = new PlanetGridSquare();
            foreach (PlanetGridSquare realPgs in SelectedPlanet.TilesList)
            {
                pgs.Biosphere  = realPgs.Biosphere;
                pgs.building   = realPgs.building ?? realPgs.QItem?.Building;
                pgs.ClickRect  = new Rectangle(buildingsRect.X + realPgs.x * xsize, buildingsRect.Y + realPgs.y * ysize, xsize, ysize);
                pgs.Habitable  = realPgs.Habitable;
                pgs.TroopsHere = realPgs.TroopsHere;

                if (!pgs.Habitable)
                {
                    batch.FillRectangle(pgs.ClickRect, new Color(0, 0, 0, 200));
                }
                batch.DrawRectangle(pgs.ClickRect, new Color(211, 211, 211, 70), 2f);

                if (pgs.building != null)
                {
                    Color c = pgs.QItem != null ? Color.White : new Color(Color.White, 128);
                    batch.Draw(pgs.building.IconTex, pgs.ClickRect.Center() - new Vector2(24), new Vector2(48), c);
                }

                DrawPGSIcons(pgs);
            }

            batch.Draw(ResourceManager.Texture("PlanetTiles/"+SelectedPlanet.PlanetTileId), buildingsRect, Color.White);
            batch.DrawRectangle(MapRect, new Color(118, 102, 67, 255));

            // draw some border around the governor component
            var GovernorRect = new Rectangle(MapRect.Right, MapRect.Y, e1.Rect.Right - MapRect.Right, MapRect.Height);
            batch.DrawRectangle(GovernorRect, new Color(118, 102, 67, 255));

            if (ColoniesList.NumEntries > 0)
            {
                ColoniesListItem entry = ColoniesList.FirstItem;
                Vector2 TextCursor = new Vector2(entry.SysNameRect.X + 30, eRect.Y - Fonts.Arial20Bold.LineSpacing + 33);
                batch.DrawString(Fonts.Arial20Bold, Localizer.Token(192), TextCursor, Colors.Cream);
                TextCursor = new Vector2(entry.PlanetNameRect.X + 30, eRect.Y - Fonts.Arial20Bold.LineSpacing + 33);
                batch.DrawString(Fonts.Arial20Bold, Localizer.Token(389), TextCursor, Colors.Cream);
                pop.rect = new Rectangle(entry.PopRect.X + 15 - ResourceManager.Texture("NewUI/icon_food").Width / 2, (int)TextCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                batch.Draw(ResourceManager.Texture("UI/icon_pop"), pop.rect, Color.White);
                food.rect = new Rectangle(entry.FoodRect.X + 15 - ResourceManager.Texture("NewUI/icon_food").Width / 2, (int)TextCursor.Y, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                batch.Draw(ResourceManager.Texture("NewUI/icon_food"), food.rect, Color.White);
                prod.rect = new Rectangle(entry.ProdRect.X + 15 - ResourceManager.Texture("NewUI/icon_production").Width / 2, (int)TextCursor.Y, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                batch.Draw(ResourceManager.Texture("NewUI/icon_production"), prod.rect, Color.White);
                res.rect = new Rectangle(entry.ResRect.X + 15 - ResourceManager.Texture("NewUI/icon_science").Width / 2, (int)TextCursor.Y, ResourceManager.Texture("NewUI/icon_science").Width, ResourceManager.Texture("NewUI/icon_science").Height);
                batch.Draw(ResourceManager.Texture("NewUI/icon_science"), res.rect, Color.White);
                money.rect = new Rectangle(entry.MoneyRect.X + 15 - ResourceManager.Texture("NewUI/icon_money").Width / 2, (int)TextCursor.Y, ResourceManager.Texture("NewUI/icon_money").Width, ResourceManager.Texture("NewUI/icon_money").Height);
                batch.Draw(ResourceManager.Texture("NewUI/icon_money"), money.rect, Color.White);
                TextCursor = new Vector2(entry.SliderRect.X + 30, eRect.Y - Fonts.Arial20Bold.LineSpacing + 33);
                batch.DrawString(Fonts.Arial20Bold, Localizer.Token(390), TextCursor, Colors.Cream);
                TextCursor = new Vector2(entry.StorageRect.X + 30, eRect.Y - Fonts.Arial20Bold.LineSpacing + 33);
                batch.DrawString(Fonts.Arial20Bold, Localizer.Token(391), TextCursor, Colors.Cream);
                TextCursor = new Vector2(entry.QueueRect.X + 30, eRect.Y - Fonts.Arial20Bold.LineSpacing + 33);
                batch.DrawString(Fonts.Arial20Bold, Localizer.Token(392), TextCursor, Colors.Cream);
            }

            var lineColor = new Color(118, 102, 67, 255);
            var topLeftSL = new Vector2(e1.SysNameRect.X, eRect.Y + 35);
            var botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.PlanetNameRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.PopRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.FoodRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.ProdRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.ResRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.MoneyRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2(e1.SliderRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.StorageRect.X + 5, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2(e1.QueueRect.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, lineColor);

            topLeftSL = new Vector2(e1.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, Color.Red);

            topLeftSL = new Vector2(e1.Right, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, PlanetInfoRect.Y);
            batch.DrawLine(topLeftSL, botSL, Color.Magenta);

            var leftBot = new Vector2(e1.X, PlanetInfoRect.Y);
            batch.DrawLine(leftBot, botSL, Color.Blue);
            leftBot = new Vector2(e1.X, eRect.Y + 35);
            botSL = new Vector2(topLeftSL.X, eRect.Y + 35);
            batch.DrawLine(leftBot, botSL, Color.Red);

            var pos = new Vector2(ScreenWidth - Fonts.Pirulen16.TextWidth("Paused") - 13f, 44f);
            batch.DrawString(Fonts.Pirulen16, "Paused", pos, Color.White);
            batch.End();
        }

        void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 36, pgs.ClickRect.Y, 35, 35);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Troops/"+pgs.SingleTroop.TexturePath), pgs.TroopClickRect, Color.White);
            }
            float numFood = 0f;
            float numProd = 0f;
            float numRes = 0f;
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood += pgs.building.PlusFoodPerColonist * SelectedPlanet.PopulationBillion * SelectedPlanet.Food.Percent;
                    numFood += pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd += pgs.building.PlusFlatProductionAmount;
                    numProd += pgs.building.PlusProdPerColonist * SelectedPlanet.PopulationBillion * SelectedPlanet.Prod.Percent;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes += pgs.building.PlusResearchPerColonist * SelectedPlanet.PopulationBillion * SelectedPlanet.Res.Percent;
                    numRes += pgs.building.PlusFlatResearchAmount;
                }
            }
            float total = numFood + numProd + numRes;
            float totalSpace = pgs.ClickRect.Width - 30;
            float spacing = totalSpace / total;
            Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.Texture("NewUI/icon_food").Height, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            for (int i = 0; (float)i < numFood; i++)
            {
                if (numFood - i <= 0f || numFood - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numFood - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numProd; i++)
            {
                if (numProd - i <= 0f || numProd - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numProd - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numRes; i++)
            {
                if (numRes - i <= 0f || numRes - i >= 1f)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rect, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, numRes - i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
        }

        
        void OnColonyListItemClicked(ColoniesListItem item)
        {
            SelectedPlanet = item.p;
            GovernorDetails.SetPlanetDetails(SelectedPlanet);
        }

        void OnColonyListItemDoubleClicked(ColoniesListItem item)
        {
            Empire.Universe.SelectedPlanet = item.p;
            Empire.Universe.ViewPlanet();
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
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }

            if (base.HandleInput(input))
                return true;

            HandleSortButton(input, pop, 2278, p => p.Population);
            HandleSortButton(input, food, 139, p => p.Food.NetIncome);
            HandleSortButton(input, prod, 140, p => p.Prod.NetIncome);
            HandleSortButton(input, res, 141, p => p.Res.NetIncome);
            HandleSortButton(input, res, 142, p => p.Money.NetRevenue);
            return false;
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
                ColoniesList.AddItem(new ColoniesListItem(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 80, this));
            }

            SelectedPlanet = ColoniesList.AllEntries[0].p;
            GovernorDetails.SetPlanetDetails(SelectedPlanet);
        }
    }
}
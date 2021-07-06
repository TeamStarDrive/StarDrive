using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        int IncomingFreighters;
        int IncomingFoodFreighters;
        int IncomingProdFreighters;
        int IncomingColoFreighters;
        int OutgoingFreighters;
        int OutgoingFoodFreighters;
        int OutgoingProdFreighters;
        int OutgoingColoFreighters;
        int IncomingFood;
        int IncomingProd;
        float IncomingPop;
        int UpdateTimer;
        bool Blockade;
        bool BioSpheresResearched;
        float TroopConsumption;
        bool Terraformable; 
        int NumTerraformersHere;
        int NumMaxTerraformers;
        bool NeedLevel1Terraform;
        bool NeedLevel2Terraform;
        bool NeedLevel3Terraform;
        int NumVolcanoes;
        int NumTerraformableTiles;
        int TerraformLevel;
        float MinEstimatedMaxPop;
        float TerraMaxPopBillion; // After terraforming
        float TerraTargetFertility; // After terraforming

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, string texture,
            string toolTip, bool percent = false, bool signs = true, int digits = 2)
        {
            DrawBuildingInfo(ref cursor, batch, value, ResourceManager.Texture(texture), toolTip, digits, percent, signs);
        }

        void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, float value, SubTexture texture,
            string toolTip, int digits, bool percent = false, bool signs = true)
        {
            if (value.AlmostEqual(0))
                return;

            var fIcon = new Rectangle((int) cursor.X, (int) cursor.Y, texture.Width, texture.Height);
            var tCursor = new Vector2(cursor.X + fIcon.Width + 5f, cursor.Y + 3f);
            string plusOrMinus = "";
            Color color = Color.White;
            if (signs)
            {
                plusOrMinus = value < 0 ? "-" : "+";
                color = value < 0 ? Color.Red : Color.Green;
            }

            batch.Draw(texture, fIcon, Color.White);
            string suffix = percent ? "% " : " ";
            string text = string.Concat(plusOrMinus, Math.Abs(value).String(digits), suffix, toolTip);
            batch.DrawString(TextFont, text, tCursor, color);
            cursor.Y += TextFont.LineSpacing + 10;
        }

        void DrawTroopLevel(Troop troop)
        {
            Graphics.Font font = Font12;
            Rectangle rect = troop.ClickRect;
            var levelRect = new Rectangle(rect.X + 30, rect.Y + 22, font.LineSpacing, font.LineSpacing + 5);
            var pos = new Vector2((rect.X + 15 + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                (1 + rect.Y + 5 + rect.Height / 2 - font.LineSpacing / 2));

            ScreenManager.SpriteBatch.FillRectangle(levelRect, new Color(0, 0, 0, 200));
            ScreenManager.SpriteBatch.DrawRectangle(levelRect, troop.Loyalty.EmpireColor);
            ScreenManager.SpriteBatch.DrawString(font, troop.Level.ToString(), pos, Color.Gold);
        }

        void DrawTileIcons(SpriteBatch batch, PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                var biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                batch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, Color.White);
            }

            if (BioSpheresResearched && (pgs.CanTerraform || pgs.BioCanTerraform))
            {
                var terraform = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 20, pgs.ClickRect.Y, 20, 20);
                batch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraform, Color.White);
                if (pgs.BioCanTerraform)
                {
                    var terraformHarder = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 30, pgs.ClickRect.Y, 20, 20);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraformHarder, Color.White);
                }
            }

            if (pgs.TroopsAreOnTile)
            {
                using (pgs.TroopsHere.AcquireReadLock())
                {
                    for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                    {
                        Troop troop = pgs.TroopsHere[i];
                        troop.SetColonyScreenRect(pgs);
                        troop.DrawIcon(batch, troop.ClickRect);
                        if (troop.Level > 0)
                            DrawTroopLevel(troop);
                    }
                }
            }

            float numFood = 0f;
            float numProd = 0f;
            float numRes = 0f;
            if (pgs.Building != null)
            {
                if (pgs.Building.PlusFlatFoodAmount > 0f || pgs.Building.PlusFoodPerColonist > 0f)
                {
                    numFood += pgs.Building.PlusFoodPerColonist * P.PopulationBillion * P.Food.Percent;
                    numFood += pgs.Building.PlusFlatFoodAmount;
                }

                if (pgs.Building.PlusFlatProductionAmount > 0f || pgs.Building.PlusProdPerColonist > 0f)
                {
                    numProd += pgs.Building.PlusFlatProductionAmount;
                    numProd += pgs.Building.PlusProdPerColonist * P.PopulationBillion * P.Prod.Percent;
                }

                if (pgs.Building.PlusProdPerRichness > 0f)
                {
                    numProd += pgs.Building.PlusProdPerRichness * P.MineralRichness;
                }

                if (pgs.Building.PlusResearchPerColonist > 0f || pgs.Building.PlusFlatResearchAmount > 0f)
                {
                    numRes += pgs.Building.PlusResearchPerColonist * P.PopulationBillion * P.Res.Percent;
                    numRes += pgs.Building.PlusFlatResearchAmount;
                }
            }

            float total = numFood + numProd + numRes;
            float totalSpace = pgs.ClickRect.Width - 30;
            float spacing = totalSpace / total;
            SubTexture foodIcon = ResourceManager.Texture("NewUI/icon_food");
            SubTexture prodIcon = ResourceManager.Texture("NewUI/icon_production");
            SubTexture scienceIcon = ResourceManager.Texture("NewUI/icon_science");

            var rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - foodIcon.Height,
                foodIcon.Width, foodIcon.Height);

            void DrawIcons(SubTexture icon, float amount)
            {
                for (int i = 0; i < amount; i++)
                {
                    float percent = (amount - i);
                    if (percent <= 0f || percent >= 1f)
                        batch.Draw(icon, rect, Color.White);
                    else
                        batch.Draw(icon, new Vector2(rect.X, rect.Y), Color.White, 0f, Vector2.Zero, percent,
                            SpriteEffects.None, 1f);
                    rect.X += (int) spacing;
                }
            }

            DrawIcons(foodIcon, numFood);
            DrawIcons(prodIcon, numProd);
            DrawIcons(scienceIcon, numRes);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (P.Owner == null || !Visible)
                return;

            P.UpdateIncomes(false);
            LeftMenu.Draw(batch, elapsed);
            RightMenu.Draw(batch, elapsed);
            TitleBar.Draw(batch, elapsed);
            LeftColony.Draw(batch, elapsed);
            RightColony.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.ColonyOverview), TitlePos, Colors.Cream);

            PlanetInfo.Draw(batch, elapsed);
            PStorage.Draw(batch, elapsed);
            SubColonyGrid.Draw(batch, elapsed);

            DrawPlanetSurfaceGrid(batch);
            PFacilities.Draw(batch, elapsed);
            DrawDetailInfo(batch, new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35));
            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);

            float num5 = 80f;
            var cursor = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
            PlanetName.SetPos(cursor);
            PlanetName.Draw(batch, elapsed);

            EditNameButton = new Rectangle((int)(cursor.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(cursor.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            batch.Draw(PlanetName.HandlingInput
                       ? ResourceManager.Texture("NewUI/icon_build_edit_hover2")
                       : ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);

            if (ScreenHeight > 768)
                cursor.Y += Font20.LineSpacing * 2;
            else
                cursor.Y += Font20.LineSpacing;
            batch.DrawString(TextFont, Localizer.Token(GameText.Class) + ":", cursor, Color.Orange);
            Vector2 position3 = new Vector2(cursor.X + num5, cursor.Y);
            batch.DrawString(TextFont, P.CategoryName, position3, Colors.Cream);
            cursor.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(cursor.X + num5, cursor.Y);
            batch.DrawString(TextFont, Localizer.Token(GameText.Population) + ":", cursor, Color.Orange);
            var color = Colors.Cream;
            batch.DrawString(TextFont, P.PopulationStringForPlayer, position3, color);
            var rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString(Localizer.Token(GameText.Population) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(GameText.AColonysPopulationIsA);
            cursor.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(cursor.X + num5, cursor.Y);
            batch.DrawString(TextFont, Localizer.Token(GameText.Fertility) + ":", cursor, Color.Orange);
            string fertility;
            if (P.FertilityFor(Player).AlmostEqual(P.MaxFertilityFor(Player))
                || P.FertilityFor(Player).AlmostZero() && P.MaxFertilityFor(Player).LessOrEqual(0))
            {
                fertility = P.FertilityFor(Player).String(2);
                batch.DrawString(TextFont, fertility, position3, color);
            }
            else
            {
                Color fertColor = P.FertilityFor(Player) < P.MaxFertilityFor(Player) ? Color.LightGreen : Color.Pink;
                fertility = $"{P.FertilityFor(Player).String(2)} / {P.MaxFertilityFor(Player).LowerBound(0).String(2)}";
                batch.DrawString(TextFont, fertility, position3, fertColor);
            }
            float fertEnvMultiplier = EmpireManager.Player.PlayerEnvModifier(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                var fertMultiplier = new Vector2(position3.X + TextFont.MeasureString($"{fertility} ").X, position3.Y+2);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }

            UpdateData();
            rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString(Localizer.Token(GameText.Fertility) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesHowMuchFoodThis);

            cursor.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(cursor.X + num5, cursor.Y);
            batch.DrawString(TextFont, Localizer.Token(GameText.Richness) + ":", cursor, Color.Orange);
            batch.DrawString(TextFont, P.MineralRichness.String(), position3, Colors.Cream);
            rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString(Localizer.Token(GameText.Richness) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(GameText.APlanetsMineralRichnessDirectly);

            cursor.Y += TextFont.LineSpacing * 2 + 4;
            if (P.TerraformPoints > 0)
            {
                Color terraformColor = P.Owner?.EmpireColor ?? Color.White;
                string terraformText = Localizer.Token(GameText.Terraforming); // Terraforming in Progress
                batch.DrawString(TextFont, terraformText, cursor, ApplyCurrentAlphaToColor(terraformColor));
            }

            DrawFoodAndStorage(batch);
            DrawShields(batch);
            BlockadeLabel.Visible   = Blockade;
            BlockadeLabel.Color     = ApplyCurrentAlphaToColor(Color.Red);
            StarvationLabel.Visible = P.IsStarving;
            StarvationLabel.Color   = ApplyCurrentAlphaToColor(Color.Red);

            base.Draw(batch, elapsed);
        }

        string IncomingPopString => IncomingPop.LessOrEqual(1) ? $"{(IncomingPop * 1000).String(2)}m" : $"{IncomingPop.String()}b";

        void DrawShields(SpriteBatch batch)
        {
            if (P.ShieldStrengthMax <= 0)
                return;

            PlanetShieldBar.Max      = P.ShieldStrengthMax;
            PlanetShieldBar.Progress = P.ShieldStrengthCurrent;
            PlanetShieldBar.Draw(batch);
            batch.Draw(ResourceManager.Texture("NewUI/icon_planetshield"), PlanetShieldIconRect, Color.LightSkyBlue);
            if (P.ShieldStrengthCurrent > 0)
                batch.Draw(P.PlanetTexture, PlanetIcon, ApplyCurrentAlphaToColor(Color.LightSkyBlue));
        }

        void DrawFoodAndStorage(SpriteBatch batch)
        {
            FoodStorage.Max = P.Storage.Max;
            ProdStorage.Max = P.Storage.Max;
            FoodStorage.Progress = P.FoodHere.RoundUpTo(1);
            ProdStorage.Progress = P.ProdHere.RoundUpTo(1);
            if (P.FS == Planet.GoodState.STORE) FoodDropDown.ActiveIndex = 0;
            else if (P.FS == Planet.GoodState.IMPORT) FoodDropDown.ActiveIndex = 1;
            else if (P.FS == Planet.GoodState.EXPORT) FoodDropDown.ActiveIndex = 2;
            if (P.NonCybernetic)
            {
                FoodStorage.Draw(batch);
                FoodDropDown.Draw(batch);
            }
            else
            {
                FoodStorage.DrawGrayed(batch);
                FoodDropDown.DrawGrayed(batch);
            }

            ProdStorage.Draw(batch);
            if (P.PS == Planet.GoodState.STORE) ProdDropDown.ActiveIndex = 0;
            else if (P.PS == Planet.GoodState.IMPORT) ProdDropDown.ActiveIndex = 1;
            else if (P.PS == Planet.GoodState.EXPORT) ProdDropDown.ActiveIndex = 2;
            ProdDropDown.Draw(batch);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), FoodStorageIcon, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), ProfStorageIcon, Color.White);

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesTheAmountOfFood);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesTheAmountOfProduction);
        }

        void DrawPlanetSurfaceGrid(SpriteBatch batch)
        {
            var planetGridRect = new Rectangle(GridPos.X, GridPos.Y + 1, GridPos.Width - 4, GridPos.Height - 3);
            batch.Draw(ResourceManager.Texture("PlanetTiles/" + P.PlanetTileId), planetGridRect, Color.White);

            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.Habitable)
                    batch.FillRectangle(pgs.ClickRect, new Color(0, 0, 0, 200));

                batch.DrawRectangle(pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
                if (pgs.Building != null)
                {
                    var buildingIcon = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                        pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.Building.Icon + "_64x64"),
                        buildingIcon, pgs.Building.IsPlayerAdded ? Color.WhiteSmoke : Color.White);
                }
                else if (pgs.QItem != null)
                {
                    Rectangle destinationRectangle2 = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                        pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.QItem.Building.Icon + "_64x64"),
                        destinationRectangle2, new Color(255, 255, 255, 128));
                }

                if (pgs.Biosphere && P.Owner != null)
                {
                    batch.FillRectangle(pgs.ClickRect, P.Owner.EmpireColor.Alpha(0.4f));
                }

                DrawTileIcons(batch, pgs);
            }

            foreach (PlanetGridSquare planetGridSquare in P.TilesList)
            {
                if (planetGridSquare.Highlighted)
                    batch.DrawRectangle(planetGridSquare.ClickRect, Color.White, 2f);
            }
        }

        Color TextColor { get; } = Colors.Cream;

        void DrawTitledLine(ref Vector2 cursor, GameText title, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Font12, Localizer.Token(title)+": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Font12, text, textCursor, TextColor);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        string MultiLineFormat(LocalizedText text)
        {
            return TextFont.ParseText(text.Text, PFacilities.Rect.Width - 40);
        }

        void DrawMultiLine(ref Vector2 cursor, LocalizedText text, Color color)
        {
            string multiline = MultiLineFormat(text);

            ScreenManager.SpriteBatch.DrawString(TextFont, multiline, cursor, color);
            cursor.Y += (TextFont.MeasureString(multiline).Y + TextFont.LineSpacing);
        }

        void DrawDetailInfo(SpriteBatch batch, Vector2 bCursor)
        {
            if (IsStatTabSelected)
            {
                DrawMoney(ref bCursor, batch);
                DrawPlanetStat(ref bCursor, batch);
                //DrawCommoditiesArea(bCursor);
                return;
            }

            if (IsTradeTabSelected)
            {
                IncomingFoodBar.Draw(batch);
                IncomingProdBar.Draw(batch);
                IncomingColoBar.Draw(batch);
                OutgoingFoodBar.Draw(batch);
                OutgoingProdBar.Draw(batch);
                OutgoingColoBar.Draw(batch);
                return;
            }

            if (IsTerraformTabSelected)
            {
                if (NeedLevel1Terraform && TerraformLevel >= 1) VolcanoTerraformBar.Draw(batch);
                if (NeedLevel2Terraform && TerraformLevel >= 2) TileTerraformBar.Draw(batch);
                if (NeedLevel3Terraform && TerraformLevel >= 3) PlanetTerraformBar.Draw(batch);
                return;
            }

            Color color = Color.Wheat;
            switch (DetailInfo)
            {
                case Troop t:
                    batch.DrawString(Font20, t.DisplayNameEmpire(P.Owner), bCursor, TextColor);
                    bCursor.Y += Font20.LineSpacing + 2;
                    string strength = t.Strength < t.ActualStrengthMax ? t.Strength + "/" + t.ActualStrengthMax
                        : t.ActualStrengthMax.String(1);

                    DrawMultiLine(ref bCursor, t.Description);
                    DrawTitledLine(ref bCursor, GameText.TroopClass, t.TargetType.ToString());
                    DrawTitledLine(ref bCursor, GameText.Strength, strength);
                    DrawTitledLine(ref bCursor, GameText.HardAttack, t.ActualHardAttack.ToString());
                    DrawTitledLine(ref bCursor, GameText.SoftAttack, t.ActualSoftAttack.ToString());
                    DrawTitledLine(ref bCursor, GameText.Boarding, t.BoardingStrength.ToString());
                    DrawTitledLine(ref bCursor, GameText.Level, t.Level.ToString());
                    DrawTitledLine(ref bCursor, GameText.Range2, t.ActualRange.ToString());
                    break;

                case string _:
                    DrawMultiLine(ref bCursor, P.Description);
                    string desc = "";
                    if (P.IsCybernetic)  desc = Localizer.Token(GameText.TheOccupantsOfThisPlanet);
                    else switch (P.FS)
                    {
                        case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.ThisColonyIsSetTo); break;
                        case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.ThisColonyIsSetTo2); break;
                        case Planet.GoodState.STORE:  desc = Localizer.Token(GameText.ThisPlanetIsNeitherImporting); break;
                    }

                    DrawMultiLine(ref bCursor, desc);
                    desc = "";
                    if (P.colonyType == Planet.ColonyType.Colony)
                    {
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.ThisPlanetIsManuallyExporting); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.ThisPlanetIsManuallyImporting); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(GameText.ThisPlanetIsManuallyStoring); break;
                        }
                    }
                    else
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.TheGovernorIsExportingProduction); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.TheGovernorIsImportingProduction); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(GameText.TheGovernorIsStoringProduction); break;
                        }
                    DrawMultiLine(ref bCursor, desc);
                    if (P.IsStarving)
                        DrawMultiLine(ref bCursor, Localizer.Token(GameText.ThisPlanetsPopulationIsShrinking), Color.LightPink);

                    break;
                case PlanetGridSquare pgs:
                    switch (pgs.Building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            batch.DrawString(Font20, Localizer.Token(GameText.HabitableBiosphere), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(GameText.DragAStructureFromThe), bCursor, color);
                            DrawTilePopInfo(ref bCursor, batch, pgs);
                            return;
                        case null when pgs.Habitable:
                            batch.DrawString(Font20, Localizer.Token(GameText.HabitableLand), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(GameText.DragAStructureFromThe), bCursor, color);
                            DrawTilePopInfo(ref bCursor, batch, pgs);
                            return;
                    }

                    if (!pgs.Habitable && !pgs.BuildingOnTile)
                    {
                        if (P.IsBarrenType)
                        {
                            batch.DrawString(Font20, Localizer.Token(GameText.UninhabitableLand), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(GameText.ThisLandIsNotHabitable), bCursor, color);
                        }
                        else
                        {
                            batch.DrawString(Font20, Localizer.Token(GameText.UninhabitableLand), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(GameText.ThisLandIsNotHabitable), bCursor, color);
                        }

                        DrawTilePopInfo(ref bCursor, batch, pgs);
                    }

                    if (pgs.Building == null)
                        return;

                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    batch.DrawString(Font20, pgs.Building.TranslatedName, bCursor, color);
                    bCursor.Y   += Font20.LineSpacing + 5;
                    string buildingDescription  = MultiLineFormat(pgs.Building.DescriptionText);
                    batch.DrawString(TextFont, buildingDescription, bCursor, color);
                    bCursor.Y   += TextFont.MeasureString(buildingDescription).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, batch, pgs.Building, pgs);
                    DrawTilePopInfo(ref bCursor, batch, pgs, 2);
                    if (!pgs.Building.Scrappable)
                        return;

                    bCursor.Y += TextFont.LineSpacing * 2;
                    batch.DrawString(TextFont, "You may scrap this building by right clicking it", bCursor, Color.White);
                    break;

                case Building selectedBuilding:
                    batch.DrawString(Font20, selectedBuilding.TranslatedName, bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionText);
                    batch.DrawString(TextFont, selectionText, bCursor, color);
                    bCursor.Y += TextFont.MeasureString(selectionText).Y + Font20.LineSpacing;
                    if (selectedBuilding.isWeapon)
                        selectedBuilding.CalcMilitaryStrength(); // So the building will have TheWeapon for stats

                    DrawSelectedBuildingInfo(ref bCursor, batch, selectedBuilding);
                    break;
            }
        }

        void DrawMoney(ref Vector2 cursor, SpriteBatch batch)
        {
            string gIncome = Localizer.Token(GameText.GrossIncome);
            string gUpkeep = Localizer.Token(GameText.Expenditure2);
            string nIncome = Localizer.Token(GameText.NetIncome);
            string nLosses = Localizer.Token(GameText.NetLosses);

            float grossIncome = P.Money.GrossRevenue;
            float grossUpkeep = P.Money.Maintenance + P.SpaceDefMaintenance;
            float netIncome   = P.Money.NetRevenue;

            Graphics.Font font = LowRes ? Font8 : Font14;

            batch.DrawString(font, $"{gIncome}: ", cursor, Color.LightGray);
            batch.DrawString(font, $"{grossIncome.String(2)} BC/Y", new Vector2(cursor.X + 150, cursor.Y), Color.LightGreen);
            cursor.Y += font.LineSpacing +  1;

            batch.DrawString(font, $"{gUpkeep}: ", cursor, Color.LightGray);
            batch.DrawString(font, $"{grossUpkeep.String(2)} BC/Y", new Vector2(cursor.X + 150, cursor.Y), Color.Pink);
            cursor.Y += font.LineSpacing + 1;

            batch.DrawString(font, $"{(netIncome > 0 ? nIncome : nLosses)}: ", cursor, Color.LightGray);
            batch.DrawString(font, $"{netIncome.String(2)} BC/Y", new Vector2(cursor.X + 150, cursor.Y), netIncome > 0.0 ? Color.Green : Color.Red);
            cursor.Y += font.LineSpacing*2 + 1;
        }

        void DrawTilePopInfo(ref Vector2 cursor, SpriteBatch batch, PlanetGridSquare tile, int spacing = 5)
        {
            float popPerTile = P.BasePopPerTile * Player.PlayerEnvModifier(P.Category);
            float popBonus   = tile.Building?.MaxPopIncrease ?? 0;
            cursor.Y += Font20.LineSpacing * spacing;
            if (tile.LavaHere)
            {
                batch.DrawString(TextFont, $"{MultiLineFormat(GameText.NoOneCanLiveOn)}", cursor, Color.Orange);
                return;
            }

            if (tile.VolcanoHere && !tile.Habitable)
            {
                batch.DrawString(TextFont, $"{MultiLineFormat(GameText.TheVolcanoHerePreventsBuilding)}", cursor, Color.Orange);
                return;
            }

            if (tile.Habitable && tile.Biosphere)
            {
                batch.DrawString(TextFont, $"{(P.PopPerBiosphere(Player) + popBonus).String(1)}" +
                                         $" {MultiLineFormat(GameText.MillionColonistsCanLiveUnder)}", cursor, Player.EmpireColor);

                cursor.Y += Font20.LineSpacing;
                if (tile.BioCanTerraform)
                {
                    batch.DrawString(TextFont, MultiLineFormat("This tile can be terraformed " +
                            "as part of terraforming operations. The process will take more time due to " +
                            "Biosphere Terraforming complexity."), cursor, Player.EmpireColor);

                    cursor.Y += Font20.LineSpacing * 2;
                }
            }
            else if (tile.Habitable)
            {
                batch.DrawString(TextFont, $"{(popPerTile + popBonus).String(1)} {MultiLineFormat(GameText.MillionColonistsCanLiveOn)}", cursor, Color.LightGreen);
            }
            else
            {
                string bioText = Localizer.Token(GameText.MillionColonistsCouldBeLiving);
                if (BioSpheresResearched && tile.CanTerraform)
                {
                    batch.DrawString(TextFont, "This tile can be terraformed as part of terraforming operations.", cursor, Player.EmpireColor);
                    cursor.Y += Font20.LineSpacing;
                    bioText += " However, building Biospheres here will complicate future terraforming efforts on the tile.";
                }

                batch.DrawString(TextFont, $"{(P.PopPerBiosphere(Player) + popBonus).String(1)} {MultiLineFormat(bioText)}", cursor, Color.Gold);
            }
        }

        void DrawPlanetStat(ref Vector2 cursor, SpriteBatch batch)
        {
            DrawBuildingInfo(ref cursor, batch, P.PopPerTileFor(Player) / 1000, "UI/icon_pop_22", Localizer.Token(GameText.ColonistsPerHabitableTileBillions));
            DrawBuildingInfo(ref cursor, batch, P.PopPerBiosphere(Player) / 1000, "UI/icon_pop_22", Localizer.Token(GameText.ColonistsPerBiosphereBillions));
            DrawBuildingInfo(ref cursor, batch, P.Food.NetYieldPerColonist - P.FoodConsumptionPerColonist, "NewUI/icon_food", Localizer.Token(GameText.NetFoodPerColonistAllocated), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Food.NetFlatBonus, "NewUI/icon_food", Localizer.Token(GameText.NetFlatFoodGeneratedPer), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetYieldPerColonist - P.ProdConsumptionPerColonist, "NewUI/icon_production", Localizer.Token(GameText.NetProductionPerColonistAllocated), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetFlatBonus, "NewUI/icon_production", Localizer.Token(GameText.NetFlatProductionGeneratedPer), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetYieldPerColonist, "NewUI/icon_science", Localizer.Token(GameText.NetResearchPerColonistAllocated), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetFlatBonus, "NewUI/icon_science", Localizer.Token(GameText.NetFlatResearchGeneratedPer), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.CurrentProductionToQueue, "NewUI/icon_queue_rushconstruction",
                $"{Localizer.Token(GameText.MaximumProductionToQueuePer)} ({P.InfraStructure} taken from Storage)", digits: 1);

            DrawBuildingInfo(ref cursor, batch, -P.Money.TroopMaint, "UI/icon_troop_shipUI", Localizer.Token(GameText.CreditsPerTurnForTroop), digits: 2);
            DrawBuildingInfo(ref cursor, batch, -TroopConsumption, "UI/icon_troop_shipUI", GetTroopsConsumptionText(), digits: 2);
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Building b, PlanetGridSquare tile = null)
        {
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatFoodAmount, "NewUI/icon_food", Localizer.Token(GameText.FoodPerTurn));
            DrawBuildingInfo(ref bCursor, batch, b.FoodCache, "NewUI/icon_food", Localizer.Token(GameText.FoodRemainingHereThisBuilding), signs: false, digits: 0);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFoodPerColonist, "NewUI/icon_food", Localizer.Token(GameText.FoodPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, batch, b.SensorRange, "NewUI/icon_sensors", Localizer.Token(GameText.SensorRange), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.ProjectorRange, "NewUI/icon_projection", Localizer.Token(GameText.SubspaceProjectionArea), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatProductionAmount, "NewUI/icon_production", Localizer.Token(GameText.ProductionPerTurn));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerColonist, "NewUI/icon_production", Localizer.Token(GameText.ProductionPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, batch, b.ProdCache, "NewUI/icon_production", Localizer.Token(GameText.ProductionRemainingHereThisBuilding), signs: false, digits: 0);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatPopulation / 1000, "NewUI/icon_population", Localizer.Token(GameText.ColonistsPerTurn));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatResearchAmount, "NewUI/icon_science", Localizer.Token(GameText.ResearchPerTurn));
            DrawBuildingInfo(ref bCursor, batch, b.PlusResearchPerColonist, "NewUI/icon_science", Localizer.Token(GameText.ResearchPerTurnPerAssigned));
            DrawBuildingInfo(ref bCursor, batch, b.PlusTaxPercentage * 100, "NewUI/icon_money", Localizer.Token(GameText.IncreaseToTaxIncomes), percent: true);
            DrawBuildingInfo(ref bCursor, batch, b.MaxFertilityOnBuildFor(Player, P.Category), "NewUI/icon_food", Localizer.Token(GameText.MaxFertilityChangeOnBuild));
            DrawBuildingInfo(ref bCursor, batch, b.PlanetaryShieldStrengthAdded, "NewUI/icon_planetshield", Localizer.Token(GameText.PlanetaryShieldStrengthAdded));
            DrawBuildingInfo(ref bCursor, batch, b.CreditsPerColonist, "NewUI/icon_money", Localizer.Token(GameText.CreditsAddedPerColonist));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerRichness, "NewUI/icon_production", Localizer.Token(GameText.ProductionPerRichness));
            DrawBuildingInfo(ref bCursor, batch, b.ShipRepair * 10, "NewUI/icon_queue_rushconstruction", Localizer.Token(GameText.ShipRepair));
            DrawBuildingInfo(ref bCursor, batch, b.Infrastructure, "NewUI/icon_queue_rushconstruction", Localizer.Token(GameText.ProductionInfrastructure));
            DrawBuildingInfo(ref bCursor, batch, b.CombatStrength, "Ground_UI/Ground_Attack", Localizer.Token(GameText.CombatStrength));
            DrawBuildingInfo(ref bCursor, batch, b.DefenseShipsCapacity, "UI/icon_hangar", b.DefenseShipsRole + " Defense Ships", signs: false);

            float maintenance = -b.ActualMaintenance(P);
            DrawBuildingInfo(ref bCursor, batch, maintenance, "NewUI/icon_money", Localizer.Token(GameText.CreditsPerTurnInMaintenance));

            DrawBuildingWeaponStats(ref bCursor, batch, b);
            DrawFertilityOnBuildWarning(ref bCursor, batch, b);

            if (tile?.VolcanoHere == true)
                DrawVolcanoChance(ref bCursor, batch, tile.Volcano.ActivationChanceText(out Color color), color);
        }

        string GetTroopsConsumptionText()
        {
            string text = P.Owner.IsCybernetic
                ? Localizer.Token(GameText.ProductionConsumptionPerTurnFor) // Prod consumption for cybernetic troops
                : Localizer.Token(GameText.FoodConsumptionPerTurnFor); // Food consumption for cybernetic troops

            if (P.AnyOfOurTroops(P.Owner) && P.Owner.TroopInSpaceFoodNeeds.LessOrEqual(0))
                text = $"{text} {Localizer.Token(GameText.OnSurface)}"; // On surface only
            else if (!P.AnyOfOurTroops(P.Owner) && P.Owner.TroopInSpaceFoodNeeds.Greater(0))
                text = $"{text} {Localizer.Token(GameText.InSpace)}"; // In space only
            else
                text = $"{text} {Localizer.Token(GameText.OnSurfaceAndInSpace)}"; // On surface and In space

            return text;
        }

        void DrawVolcanoChance(ref Vector2 cursor, SpriteBatch batch, string text, Color color)
        {
            batch.DrawString(TextFont, text, cursor, color);
            cursor.Y += TextFont.LineSpacing;
        }

        void DrawFertilityOnBuildWarning(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.MaxFertilityOnBuild.LessOrEqual(0))
                return;

            if (P.MaxFertility + b.MaxFertilityOnBuildFor(Player, P.Category) < 0)
            {
                string warning = MultiLineFormat($"{Localizer.Token(GameText.NegativeEnvWarning)} {P.MaxFertility}).");

                cursor.Y += TextFont.LineSpacing;
                batch.DrawString(TextFont, warning, cursor, Color.Red);
                cursor.Y += TextFont.LineSpacing * 4;
            }
        }

        void DrawBuildingWeaponStats(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.TheWeapon == null)
                return;

            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.BaseRange, "UI/icon_offense", "Range", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.DamageAmount, "UI/icon_offense", "Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.EMPDamage, "UI/icon_offense", "EMP Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.ActualFireDelay(P), "UI/icon_offense", "Fire Delay", signs: false);
        }

        string GetTargetFertilityText(out Color color)
        {
            color = Color.Yellow;
            if (TerraformLevel < 3)
                return "";

            if (TerraTargetFertility.Less(1))
            {
                if (TerraTargetFertility <= 0)
                    color = Color.Red;

                return $" {TerraTargetFertility.String(2)} {Localizer.Token(GameText.TerraformNegativeEnv)}";
            }

            if (TerraTargetFertility.Greater(P.MaxFertilityFor(Player))) // Better new fertility max
            {
                color = Color.Green;
                return $" {TerraTargetFertility.String(2)}";
            }

            return "";
        }

        void UpdateData() // This will update statistics in an interval to reduce threading issues
        {
            if (UpdateTimer <= 0)
            {
                IncomingFreighters     = P.NumIncomingFreighters;
                IncomingFoodFreighters = P.IncomingFoodFreighters;
                IncomingProdFreighters = P.IncomingProdFreighters;
                IncomingColoFreighters = P.IncomingColonistsFreighters;
                OutgoingFreighters     = P.NumOutgoingFreighters;
                OutgoingFoodFreighters = P.OutgoingFoodFreighters;
                OutgoingProdFreighters = P.OutgoingProdFreighters;
                OutgoingColoFreighters = P.OutGoingColonistsFreighters;
                BioSpheresResearched   = Player.IsBuildingUnlocked(Building.BiospheresId);
                IncomingFood           = TotalIncomingCargo(Goods.Food).RoundUpTo(1);
                IncomingProd           = TotalIncomingCargo(Goods.Production).RoundUpTo(1);
                IncomingPop            = (TotalIncomingCargo(Goods.Colonists) / 1000).RoundToFractionOf100();
                Blockade               = P.Quarantine || P.SpaceCombatNearPlanet;
                TroopConsumption       = P.TotalTroopConsumption;
                Terraformable          = P.Terraformable;
                NumTerraformersHere    = P.TerraformersHere;
                NumMaxTerraformers     = P.TerraformerLimit;
                NeedLevel1Terraform    = P.HasVolcanoesToTerraform;
                NeedLevel2Terraform    = P.HasTilesToTerraform;
                NeedLevel3Terraform    = P.Category != P.Owner.data.PreferredEnv || P.BaseMaxFertility.Less(P.TerraformedMaxFertility);
                NumVolcanoes           = P.TilesList.Filter(t => t.VolcanoHere).Length;
                NumTerraformableTiles  = P.TilesList.Filter(t => t.CanTerraform).Length;
                TerraformLevel         = P.Owner.data.Traits.TerraformingLevel;
                TerraTargetFertility   = TerraformTargetFertility();
                MinEstimatedMaxPop     = P.PotentialMaxPopBillionsFor(P.Owner);
                TerraMaxPopBillion     = P.PotentialMaxPopBillionsFor(P.Owner, true);
                UpdateTimer            = 150;
            }
            else
            {
                UpdateTimer -= 1;
            }
        }

        float TotalIncomingCargo(Goods goods)
        {
            var freighterList = P.IncomingFreighters.Filter(s => s.AI.HasTradeGoal(goods));
            return freighterList.Sum(s => s.GetCargo(goods));
        }
    }
}

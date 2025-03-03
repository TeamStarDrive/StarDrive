using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Graphics;

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
        int NumTerrain; // Terrain to terraform
        int NumTerraformableTiles;
        int TerraformLevel;
        bool DysonSwarmTabAllowed;
        float MinEstimatedMaxPop;
        float TerraMaxPopBillion; // After terraforming
        float TerraTargetFertility; // After terraforming

        public static void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, Font font, float value, string texture,
            LocalizedText summary, bool percent = false, bool signs = true, int digits = 2)
        {
            DrawBuildingInfo(ref cursor, batch, font, value, ResourceManager.Texture(texture), summary.Text, digits, percent, signs);
        }

        static void DrawBuildingInfo(ref Vector2 cursor, SpriteBatch batch, Font font, float value, SubTexture texture,
            LocalizedText summary, int digits, bool percent = false, bool signs = true)
        {
            if (value.AlmostEqual(0))
                return;

            var fIcon = new Rectangle((int) cursor.X, (int) cursor.Y, 18, 18);
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
            string text = string.Concat(plusOrMinus, Math.Abs(value).String(digits), suffix, summary.Text);
            batch.DrawString(font, text, tCursor, color);
            cursor.Y += font.LineSpacing + 5;
        }

        void DrawTroopLevel(Troop troop)
        {
            Font font = Font12;
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
                bool hoveringOverBio = biosphere.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive;
                batch.Draw(ResourceManager.Texture("Buildings/icon_biosphere_48x48"), biosphere, hoveringOverBio ? Color.Red : Color.White);
                if (hoveringOverBio) 
                    ToolTip.CreateTooltip(GameText.BioshperesAreBuiltHere);
            }

            if (pgs.CanTerraform || pgs.BioCanTerraform)
            {
                var terraform = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 20, pgs.ClickRect.Y, 20, 20);
                var terraformHarder = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 30, pgs.ClickRect.Y, 20, 20);
                bool hoveringOverTerra = terraform.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive;
                batch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraform, hoveringOverTerra ? Color.Orange : Color.White);
                if (pgs.BioCanTerraform)
                {
                    bool hoveringOverTerraHard = terraformHarder.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive || hoveringOverTerra;
                    batch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraformHarder, hoveringOverTerraHard ? Color.Red : Color.White);
                    if (hoveringOverTerraHard)
                        ToolTip.CreateTooltip(GameText.ThisTileCanBeTerraformedHarder);
                }
                else if (hoveringOverTerra)
                {
                    ToolTip.CreateTooltip(GameText.ThisTileCanBeTerraformed);
                }
            }

            if (pgs.TroopsAreOnTile)
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

            float numFood = 0f;
            float numProd = 0f;
            float numRes = 0f;
            if (pgs.Building != null)
            {
                if (pgs.Building.PlusFlatFoodAmount > 0f || pgs.Building.PlusFoodPerColonist > 0f)
                {
                    numFood += pgs.Building.PlusFoodPerColonist * P.PopulationBillion * P.Food.Percent * P.Fertility;
                    numFood += pgs.Building.PlusFlatFoodAmount;
                }

                if (pgs.Building.PlusFlatProductionAmount > 0f || pgs.Building.PlusProdPerColonist > 0f)
                {
                    numProd += pgs.Building.PlusFlatProductionAmount;
                    numProd += pgs.Building.PlusProdPerColonist * P.PopulationBillion * P.Prod.Percent * P.MineralRichness;
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

            P.UpdateIncomes();
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
            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);

            DrawDetailInfo(batch, new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35));

            float num5 = 100;
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
            if (rect.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
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
            float fertEnvMultiplier = Player.PlayerEnvModifier(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                var fertMultiplier = new Vector2(position3.X + TextFont.MeasureString($"{fertility} ").X, position3.Y+2);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }

            UpdatePlanetDataForDrawing();
            rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString(Localizer.Token(GameText.Fertility) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesHowMuchFoodThis);

            cursor.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(cursor.X + num5, cursor.Y);
            batch.DrawString(TextFont, Localizer.Token(GameText.Richness) + ":", cursor, Color.Orange);
            batch.DrawString(TextFont, P.MineralRichness.String(), position3, Colors.Cream);
            rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString(Localizer.Token(GameText.Richness) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
                ToolTip.CreateTooltip(GameText.APlanetsMineralRichnessDirectly);

            cursor.Y += TextFont.LineSpacing + 2;
            if (OutgoingColoFreighters > 0 || IncomingColoFreighters > 0 || P.ColonistsImportSlots > 0)
            {
                position3 = new Vector2(cursor.X + num5, cursor.Y);
                batch.DrawString(TextFont, P.ColonistsImportSlots > 0 ?"Incoming Pop: " : "Outgoing Pop: ", cursor, P.Owner.EmpireColor);
                DrawColoSlots(batch, position3);
                rect = new Rectangle((int)cursor.X, (int)cursor.Y, (int)TextFont.MeasureString("Incoming Pop: ").X, TextFont.LineSpacing);
                if (rect.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
                    ToolTip.CreateTooltip(GameText.IncomingOutGoingTip);
            }

            cursor.Y += TextFont.LineSpacing + 2;
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
                DrawFoodSlots(batch);
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
            DrawProdSlots(batch);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), FoodStorageIcon, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), ProfStorageIcon, Color.White);

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesTheAmountOfFood);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && P.Universe.Screen.IsActive)
                ToolTip.CreateTooltip(GameText.IndicatesTheAmountOfProduction);
        }

        void DrawFoodSlots(SpriteBatch batch)
        {
            if (P.FS == Planet.GoodState.STORE)
                return;

            Vector2 textPos = new(FoodStorage.pBar.X+2, FoodStorage.pBar.Y + 20);
            LocalizedText text = new(GameText.IncomingFreighters);
            int enroute =  IncomingFoodFreighters;
            int maxSlots = P.FoodImportSlots;
            string amount = IncomingFood > 0 ? $"({IncomingFood.String()})" : "";

            if (P.FS == Planet.GoodState.EXPORT)
            {
                text = GameText.OutgoingFreighters;
                enroute = OutgoingFoodFreighters;
                maxSlots = P.FoodExportSlots;
            }

            DrawTradeSlots(batch, textPos, text, Color.LightGreen, enroute, maxSlots, amount);
        }

        void DrawProdSlots(SpriteBatch batch)
        {
            if (P.PS == Planet.GoodState.STORE)
                return;

            Vector2 textPos = new(ProdStorage.pBar.X+2, ProdStorage.pBar.Y + 20);
            LocalizedText text = new(GameText.IncomingFreighters);
            int enroute = IncomingProdFreighters;
            int maxSlots = P.ProdImportSlots;
            string amount = IncomingProd > 0 ? $"({IncomingProd.String()})" : "";

            if (P.PS == Planet.GoodState.EXPORT)
            {
                text = GameText.OutgoingFreighters;
                enroute = OutgoingProdFreighters;
                maxSlots = P.ProdExportSlots;
                amount = "";
            }

            DrawTradeSlots(batch, textPos, text, Color.SandyBrown, enroute, maxSlots, amount);
        }

        void DrawColoSlots(SpriteBatch batch, Vector2 textPos)
        {
            int enroute = IncomingColoFreighters;
            int maxSlots = P.ColonistsImportSlots;
            string amount = IncomingPop > 0 ? $"({IncomingPopString})" : "";
            if (P.ColonistsExportSlots > 0)
            {
                enroute = OutgoingColoFreighters;
                maxSlots = P.ColonistsExportSlots;
                amount = "";
            }

            DrawTradeSlots(batch, textPos, "", Colors.Cream, enroute, maxSlots, amount, punctuation: false);
        }


        void DrawTradeSlots(SpriteBatch batch, Vector2 pos, LocalizedText goodsType,
            Color color, int enroute, int openSlots, string amount, bool punctuation = true)
        {
            if (openSlots == 0)
                color = Color.Gray;

            if (enroute == 0)
                color = new Color(color, 128);

            string puncText = punctuation ? ": " : "";
            batch.DrawString(TextFont, $"{goodsType.Text}{puncText}{enroute}/{openSlots} {amount}", pos, color);
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
            if (IsDysonSwarmTabSelected)
            {
                DysonSwarmControllerProgress.Draw(batch);
                DysonSwarmProgress.Draw(batch);
                DysonSwarmProductionBoost.Draw(batch);
                return;
            }

            if (IsStatTabSelected)
            {
                DrawMoney(ref bCursor, batch);
                DrawPlanetStat(ref bCursor, batch, TextFont);
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
                if (NeedLevel1Terraform && TerraformLevel >= 1) TerrainTerraformBar.Draw(batch);
                if (NeedLevel2Terraform && TerraformLevel >= 2) TileTerraformBar.Draw(batch);
                if (NeedLevel3Terraform && TerraformLevel >= 3) PlanetTerraformBar.Draw(batch);
                return;
            }

            switch (DetailInfo)
            {
                case Building buildableBuilding: // BuildList building
                    DrawHoveredBuildListBuildingInfo(batch, bCursor, buildableBuilding);
                    break;
                case Troop buildableTroop: // BuildList troop
                    DrawHoveredBuildListTroopInfo(batch, bCursor, buildableTroop);
                    break;
                // for null or string case, we always draw entire colony descr
                case null: case string _:
                    DrawColonyDescription(bCursor);
                    break;
                case PlanetGridSquare pgs: // hovering over a PlanetGridSquare
                    DrawHoveredPGSInfo(batch, bCursor, pgs);
                    break;
            }
        }

        // TODO: extracted method, needs refactor/clean
        void DrawHoveredPGSInfo(SpriteBatch batch, Vector2 bCursor, PlanetGridSquare pgs)
        {
            Color color = Color.Wheat;

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
            bCursor.Y += Font20.LineSpacing + 5;
            string buildingDescription = MultiLineFormat(pgs.Building.DescriptionText);
            batch.DrawString(TextFont, buildingDescription, bCursor, color);
            bCursor.Y += TextFont.MeasureString(buildingDescription).Y + Font20.LineSpacing;
            DrawSelectedBuildingInfo(ref bCursor, batch, TextFont, P.Owner, P.Fertility, P.MineralRichness, P.Category, P.Level, pgs.Building, pgs);
            DrawTilePopInfo(ref bCursor, batch, pgs, 2);
            if (!pgs.Building.Scrappable)
                return;

            bCursor.Y += TextFont.LineSpacing * 2;
            batch.DrawString(TextFont, "You may scrap this building by right clicking it", bCursor, Color.White);
        }

        // TODO: extracted method, needs refactor/clean
        void DrawHoveredBuildListBuildingInfo(SpriteBatch batch, Vector2 bCursor, Building selectedBuilding)
        {
            Color color = Color.Wheat;

            batch.DrawString(Font20, selectedBuilding.TranslatedName, bCursor, color);
            bCursor.Y += Font20.LineSpacing + 5;
            string selectionText = MultiLineFormat(selectedBuilding.DescriptionText);
            batch.DrawString(TextFont, selectionText, bCursor, color);
            bCursor.Y += TextFont.MeasureString(selectionText).Y + Font20.LineSpacing;
            if (selectedBuilding.IsWeapon)
                selectedBuilding.CalcMilitaryStrength(P); // So the building will have TheWeapon for stats

            DrawSelectedBuildingInfo(ref bCursor, batch, TextFont, P.Owner, P.Fertility, P.MineralRichness, P.Category, P.Level, selectedBuilding);
        }

        // TODO: extracted method, needs refactor/clean
        void DrawHoveredBuildListTroopInfo(SpriteBatch batch, Vector2 bCursor, Troop t)
        {
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
        }

        // TODO: extracted method, needs refactor/clean
        void DrawColonyDescription(Vector2 bCursor)
        {
            DrawMultiLine(ref bCursor, P.Description);
            string desc = "";
            if (P.IsCybernetic) desc = Localizer.Token(GameText.TheOccupantsOfThisPlanet);
            else switch (P.FS)
                {
                    case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.ThisColonyIsSetTo); break;
                    case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.ThisColonyIsSetTo2); break;
                    case Planet.GoodState.STORE: desc = Localizer.Token(GameText.ThisPlanetIsNeitherImporting); break;
                }

            DrawMultiLine(ref bCursor, desc);
            desc = "";
            if (P.CType == Planet.ColonyType.Colony)
            {
                switch (P.PS)
                {
                    case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.ThisPlanetIsManuallyExporting); break;
                    case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.ThisPlanetIsManuallyImporting); break;
                    case Planet.GoodState.STORE: desc = Localizer.Token(GameText.ThisPlanetIsManuallyStoring); break;
                }
            }
            else
                switch (P.PS)
                {
                    case Planet.GoodState.EXPORT: desc = Localizer.Token(GameText.TheGovernorIsExportingProduction); break;
                    case Planet.GoodState.IMPORT: desc = Localizer.Token(GameText.TheGovernorIsImportingProduction); break;
                    case Planet.GoodState.STORE: desc = Localizer.Token(GameText.TheGovernorIsStoringProduction); break;
                }
            DrawMultiLine(ref bCursor, desc);
            if (P.IsStarving)
                DrawMultiLine(ref bCursor, Localizer.Token(GameText.ThisPlanetsPopulationIsShrinking), Color.LightPink);
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

            Font font = LowRes ? Font8 : Font14;

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
                    batch.DrawString(TextFont, MultiLineFormat(GameText.ThisTileCanBeTerraformed), cursor, Player.EmpireColor);
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

        void DrawPlanetStat(ref Vector2 cursor, SpriteBatch batch, Font font)
        {
            DrawBuildingInfo(ref cursor, batch, font, P.PopPerTileFor(Player) / 1000, "UI/icon_pop_22", GameText.ColonistsPerHabitableTileBillions);
            DrawBuildingInfo(ref cursor, batch, font, P.PopPerBiosphere(Player) / 1000, "UI/icon_pop_22", GameText.ColonistsPerBiosphereBillions);
            DrawBuildingInfo(ref cursor, batch, font, P.Food.NetYieldPerColonist - P.FoodConsumptionPerColonist, "NewUI/icon_food", Localizer.Token(GameText.NetFoodPerColonistAllocated), digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.Food.NetFlatBonus, "NewUI/icon_food", GameText.NetFlatFoodGeneratedPer, digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.Prod.NetYieldPerColonist - P.ProdConsumptionPerColonist, "NewUI/icon_production", GameText.NetProductionPerColonistAllocated, digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.Prod.NetFlatBonus, "NewUI/icon_production", GameText.NetFlatProductionGeneratedPer, digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.Res.NetYieldPerColonist, "NewUI/icon_science", GameText.NetResearchPerColonistAllocated, digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.Res.NetFlatBonus, "NewUI/icon_science", GameText.NetFlatResearchGeneratedPer, digits: 1);
            DrawBuildingInfo(ref cursor, batch, font, P.CurrentProductionToQueue, "NewUI/icon_queue_rushconstruction",
                $"{new LocalizedText(GameText.MaximumProductionToQueuePer).Text} ({P.InfraStructure} taken from Storage)", digits: 1);

            string combat = P.SpaceCombatNearPlanet ? " (reduced due to space combat)" : "";
            DrawBuildingInfo(ref cursor, batch, font, P.GeodeticManager.GetPlanetRepairRatePerSecond(), "NewUI/icon_queue_rushconstruction",
                $"{new LocalizedText(GameText.ShipRepair).Text} Per Second{combat}", digits: 1);

            DrawBuildingInfo(ref cursor, batch, font, -P.Money.TroopMaint, "UI/icon_troop_shipUI", Localizer.Token(GameText.CreditsPerTurnForTroop), digits: 2);
            DrawBuildingInfo(ref cursor, batch, font, -TroopConsumption, "UI/icon_troop_shipUI", GetTroopsConsumptionText(), digits: 2);
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Font font, Empire owner,
            float fertility, float richness, PlanetCategory category, int planetLevel, Building b, PlanetGridSquare tile = null)
        {
            DrawBuildingStaticInfo(ref bCursor, batch, font, owner, fertility, richness, category, b);
            DrawBuildingInfo(ref bCursor, batch, font, b.ActualShipRepair(P), "NewUI/icon_queue_rushconstruction", GameText.ShipRepair);
            DrawBuildingWeaponStats(ref bCursor, batch, font, b, planetLevel);

            DrawFertilityOnBuildWarning(ref bCursor, batch, b);

            if (tile?.VolcanoHere == true)
                DrawVolcanoChance(ref bCursor, batch, tile.Volcano.ActivationChanceText(out Color color), color);
        }

        public static void DrawBuildingStaticInfo(ref Vector2 bCursor, SpriteBatch batch, Font font, Empire owner,
            float fertility, float richness, PlanetCategory category, Building b)
        {
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusFlatFoodAmount, "NewUI/icon_food", GameText.FoodPerTurn);
            DrawBuildingInfo(ref bCursor, batch, font, b.FoodCache, "NewUI/icon_food", GameText.FoodRemainingHereThisBuilding, signs: false, digits: 0);
            DrawBuildingInfo(ref bCursor, batch, font, ColonyResource.FoodYieldFormula(fertility, b.PlusFoodPerColonist - 1), "NewUI/icon_food", GameText.FoodPerTurnPerAssigned);
            DrawBuildingInfo(ref bCursor, batch, font, b.SensorRange, "NewUI/icon_sensors", GameText.SensorRange, signs: false);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusFlatProductionAmount, "NewUI/icon_production", GameText.ProductionPerTurn);
            DrawBuildingInfo(ref bCursor, batch, font, ColonyResource.ProdYieldFormula(richness, b.PlusProdPerColonist - 1, owner), "NewUI/icon_production", GameText.ProductionPerTurnPerAssigned);
            DrawBuildingInfo(ref bCursor, batch, font, b.ProdCache, "NewUI/icon_production", GameText.ProductionRemainingHereThisBuilding, signs: false, digits: 0);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusFlatPopulation / 1000, "NewUI/icon_population", GameText.ColonistsPerTurn, digits: 3);
            DrawBuildingInfo(ref bCursor, batch, font, b.MaxPopIncrease / 1000, "NewUI/icon_population", GameText.PopMax, digits: 2);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusFlatResearchAmount, "NewUI/icon_science", GameText.ResearchPerTurn);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusResearchPerColonist, "NewUI/icon_science", GameText.ResearchPerTurnPerAssigned);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusTaxPercentage * 100, "NewUI/icon_money", GameText.IncreaseToTaxIncomes, percent: true);
            DrawBuildingInfo(ref bCursor, batch, font, b.MaxFertilityOnBuildFor(owner, category), "NewUI/icon_food", GameText.MaxFertilityChangeOnBuild);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlanetaryShieldStrengthAdded, "NewUI/icon_planetshield", GameText.PlanetaryShieldStrengthAdded);
            DrawBuildingInfo(ref bCursor, batch, font, b.CreditsPerColonist, "NewUI/icon_money", GameText.CreditsAddedPerColonist);
            DrawBuildingInfo(ref bCursor, batch, font, b.Income, "NewUI/icon_money", GameText.FlatIncomePerTurn);
            DrawBuildingInfo(ref bCursor, batch, font, b.PlusProdPerRichness, "NewUI/icon_production", GameText.ProductionPerRichness);
            DrawBuildingInfo(ref bCursor, batch, font, b.Infrastructure, "NewUI/icon_queue_rushconstruction", GameText.ProductionInfrastructure);
            DrawBuildingInfo(ref bCursor, batch, font, b.StorageAdded, "NewUI/icon_storage_production", GameText.Storage);
            DrawBuildingInfo(ref bCursor, batch, font, b.CombatStrength, "Ground_UI/Ground_Attack", GameText.CombatStrength);
            DrawBuildingInfo(ref bCursor, batch, font, b.Defense, "UI/icon_shield", GameText.Defense);
            DrawBuildingInfo(ref bCursor, batch, font, b.DefenseShipsCapacity, "UI/icon_hangar", b.DefenseShipsRole + " Defense Ships", signs: false);
            float maintenance = -b.ActualMaintenance(owner);
            DrawBuildingInfo(ref bCursor, batch, font, maintenance, "NewUI/icon_money",
                Localizer.Token(maintenance > 0 ? GameText.CreditsPerTurn : GameText.CreditsPerTurnInMaintenance));
        }

            string GetTroopsConsumptionText()
        {
            string text = P.IsCybernetic
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

        public static void DrawBuildingWeaponStats(ref Vector2 cursor, SpriteBatch batch, Font font, Building b, int planetLevel)
        {
            if (b.TheWeapon == null)
                return;

            DrawBuildingInfo(ref cursor, batch, font,b.TheWeapon.BaseRange, "UI/icon_offense", "Range", signs: false);
            DrawBuildingInfo(ref cursor, batch, font, b.TheWeapon.DamageAmount, "UI/icon_offense", "Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, font, b.TheWeapon.EMPDamage, "UI/icon_offense", "EMP Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, font, b.ActualFireDelay(planetLevel), "UI/icon_offense", "Fire Delay", signs: false);
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

        // Fat_Bastard: This will update statistics in an interval to reduce threading issues
        void UpdatePlanetDataForDrawing()
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
                IncomingFood           = P.IncomingFood.RoundUpTo(1);
                IncomingProd           = P.IncomingProd.RoundUpTo(1);
                IncomingPop            = (P.IncomingPop / 1000f).RoundToFractionOf100();
                Blockade               = P.Quarantine || P.SpaceCombatNearPlanet;
                TroopConsumption       = P.TotalTroopConsumption;
                Terraformable          = P.Terraformable;
                NumTerraformersHere    = P.TerraformersHere;
                NumMaxTerraformers     = P.TerraformerLimit;
                NeedLevel1Terraform    = P.HasTerrainToTerraform;
                NeedLevel2Terraform    = P.HasTilesToTerraform;
                NeedLevel3Terraform    = P.Category != P.Owner.data.PreferredEnvPlanet || P.BaseMaxFertility.Less(P.TerraformedMaxFertility);
                NumTerrain             = P.CountBuildings(b => b.CanBeTerraformed);
                NumTerraformableTiles  = P.TilesList.Count(t => t.CanTerraform);
                TerraformLevel         = P.ContainsEventTerraformers ? 3 : P.Owner.data.Traits.TerraformingLevel;
                TerraTargetFertility   = TerraformTargetFertility();
                MinEstimatedMaxPop     = P.PotentialMaxPopBillionsFor(P.Owner);
                TerraMaxPopBillion     = P.PotentialMaxPopBillionsFor(P.Owner, true);
                DysonSwarmTabAllowed   = P.Owner.CanBuildDysonSwarmIn(P.System);

                if (TerraformLevel > 0 && !PFacilities.Tabs.Any(t => t.Title == Localizer.Token(GameText.BB_Tech_Terraforming_Name))
                    || DysonSwarmTabAllowed && !PFacilities.Tabs.Any(t => t.Title == Localizer.Token(GameText.DysonSwarm)))
                {
                    PopulatePfacilitieTabs();
                }

                UpdateTimer = 150;
            }
            else
            {
                UpdateTimer -= 1;
            }
        }
    }
}

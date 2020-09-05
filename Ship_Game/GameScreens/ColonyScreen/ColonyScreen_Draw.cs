using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

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
        bool TerraformResearched;

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
                color = value < 0 ? Color.Pink : Color.LightGreen;
            }

            batch.Draw(texture, fIcon, Color.White);
            string suffix = percent ? "% " : " ";
            string text = string.Concat(plusOrMinus, Math.Abs(value).String(digits), suffix, toolTip);
            batch.DrawString(Font12, text, tCursor, color);
            cursor.Y += Font12.LineSpacing + 10;
        }

        void DrawTroopLevel(Troop troop)
        {
            SpriteFont font = Font12;
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

            if (TerraformResearched && (pgs.CanTerraform || pgs.BioCanTerraform))
            {
                var terraform = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 20, pgs.ClickRect.Y, 20, 20);
                batch.Draw(ResourceManager.Texture("Buildings/icon_terraformer_48x48"), terraform, Color.White);
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
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood += pgs.building.PlusFoodPerColonist * P.PopulationBillion * P.Food.Percent;
                    numFood += pgs.building.PlusFlatFoodAmount;
                }

                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd += pgs.building.PlusFlatProductionAmount;
                    numProd += pgs.building.PlusProdPerColonist * P.PopulationBillion * P.Prod.Percent;
                }

                if (pgs.building.PlusProdPerRichness > 0f)
                {
                    numProd += pgs.building.PlusProdPerRichness * P.MineralRichness;
                }

                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes += pgs.building.PlusResearchPerColonist * P.PopulationBillion * P.Res.Percent;
                    numRes += pgs.building.PlusFlatResearchAmount;
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

        public override void Draw(SpriteBatch batch)
        {
            if (P.Owner == null || !Visible)
                return;

            P.UpdateIncomes(false);
            LeftMenu.Draw(batch);
            RightMenu.Draw(batch);
            TitleBar.Draw(batch);
            LeftColony.Draw(batch);
            RightColony.Draw(batch);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(369), TitlePos, Colors.Cream);

            PlanetInfo.Draw(batch);
            pDescription.Draw(batch);
            pStorage.Draw(batch);
            subColonyGrid.Draw(batch);

            DrawPlanetSurfaceGrid(batch);
            pFacilities.Draw(batch);
            DrawDetailInfo(batch, new Vector2(pFacilities.Rect.X + 15, pFacilities.Rect.Y + 35));
            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);

            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            var vector2_2 = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
            P.Name = PlanetName.Text;
            PlanetName.Draw(batch, Font20, vector2_2, Colors.Cream);
            EditNameButton = new Rectangle((int)(vector2_2.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(vector2_2.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (EditHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), EditNameButton, Color.White);

            if (ScreenHeight > 768)
                vector2_2.Y += Font20.LineSpacing * 2;
            else
                vector2_2.Y += Font20.LineSpacing;
            batch.DrawString(Font12, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, P.CategoryName, position3, Colors.Cream);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            var color = Colors.Cream;
            batch.DrawString(Font12, P.PopulationStringForPlayer, position3, color);
            var rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(385) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            string fertility;
            if (P.FertilityFor(Player).AlmostEqual(P.MaxFertilityFor(Player)))
            {
                fertility = P.FertilityFor(Player).String(2);
                batch.DrawString(Font12, fertility, position3, color);
            }
            else
            {
                Color fertColor = P.FertilityFor(Player) < P.MaxFertilityFor(Player) ? Color.LightGreen : Color.Pink;
                fertility = $"{P.FertilityFor(Player).String(2)} / {P.MaxFertilityFor(Player).LowerBound(0).String(2)}";
                batch.DrawString(Font12, fertility, position3, fertColor);
            }
            float fertEnvMultiplier = EmpireManager.Player.RacialEnvModifer(P.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                var fertMultiplier = new Vector2(position3.X + Font12.MeasureString($"{fertility} ").X, position3.Y+2);
                batch.DrawString(Font8, $"(x {fertEnvMultiplier.String(2)})", fertMultiplier, fertEnvColor);
            }
            if (P.TerraformPoints > 0)
            {
                Color terraformColor = P.Owner?.EmpireColor ?? Color.White;
                string terraformText = Localizer.Token(683); // Terraform Planet is the default text
                if (P.TilesToTerraform)
                {
                    terraformText  = Localizer.Token(1972);
                }
                else if (P.BioSpheresToTerraform
                      && P.Category == P.Owner?.data.PreferredEnv 
                      && P.BaseMaxFertility.GreaterOrEqual(P.TerraformedMaxFertility))
                {
                    terraformText = Localizer.Token(1919);
                }

                var terraformPos = new Vector2(vector2_2.X + num5 * 3.9f, vector2_2.Y + (Font12.LineSpacing + 2) * 5);
                batch.DrawString(Font12, $"{terraformText} - {(P.TerraformPoints * 100).String(0)}%", terraformPos, terraformColor);
            }

            UpdateData();
            if (IncomingFreighters > 0 && (P.Owner?.isPlayer == true || Empire.Universe.Debug))
            {
                Vector2 incomingTitle = new Vector2(vector2_2.X + + 200, vector2_2.Y - (Font12.LineSpacing + 2) * 3);
                Vector2 incomingData =  new Vector2(vector2_2.X + 200 + num5, vector2_2.Y - (Font12.LineSpacing + 2) * 3);
                batch.DrawString(Font12, "Incoming Freighters:", incomingTitle, Color.White);

                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingFoodFreighters,
                    IncomingFood.String(), GameText.Food);
                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingProdFreighters,
                    IncomingProd.String(), GameText.Production);
                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingColoFreighters,
                    IncomingPop.String(2), GameText.Colonists);

            }

            if (OutgoingFreighters > 0 && (P.Owner?.isPlayer == true || Empire.Universe.Debug))
            {
                Vector2 outgoingTitle = new Vector2(vector2_2.X + +200, vector2_2.Y + (Font12.LineSpacing + 2) * 2);
                Vector2 outgoingData  = new Vector2(vector2_2.X + 200 + num5, vector2_2.Y + (Font12.LineSpacing + 2) * 2);
                batch.DrawString(Font12, "Outgoing Freighters:", outgoingTitle, Color.White);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingFoodFreighters, GameText.Food);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingProdFreighters, GameText.Production);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingProdFreighters, GameText.Colonists);
            }

            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(386) + ":").X, Font12.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += Font12.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(Font12, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            batch.DrawString(Font12, P.MineralRichness.String(), position3, Colors.Cream);
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)Font12.MeasureString(Localizer.Token(387) + ":").X, Font12.LineSpacing);

            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

            float grossIncome = P.Money.GrossRevenue;
            float grossUpkeep = P.Money.Maintenance;
            float netIncome   = P.Money.NetRevenue;

            Vector2 positionGIncome = vector2_2;
            positionGIncome.X = vector2_2.X + 1;
            positionGIncome.Y = vector2_2.Y + 28;
            Vector2 positionGrossIncome = position3;
            positionGrossIncome.Y = position3.Y + 28;
            positionGrossIncome.X = position3.X + 1;

            batch.DrawString(Fonts.Arial10, gIncome + ":", positionGIncome, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossIncome.String(2) + " BC/Y", positionGrossIncome, Color.LightGray);

            Vector2 positionGUpkeep = positionGIncome;
            positionGUpkeep.Y = positionGIncome.Y + (Fonts.Arial12.LineSpacing);
            Vector2 positionGrossUpkeep = positionGrossIncome;
            positionGrossUpkeep.Y += (Fonts.Arial12.LineSpacing);

            batch.DrawString(Fonts.Arial10, gUpkeep + ":", positionGUpkeep, Color.LightGray);
            batch.DrawString(Fonts.Arial10, grossUpkeep.String(2) + " BC/Y", positionGrossUpkeep, Color.LightGray);

            Vector2 positionNIncome = positionGUpkeep;
            positionNIncome.X = positionGUpkeep.X - 1;
            positionNIncome.Y = positionGUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);
            Vector2 positionNetIncome = positionGrossUpkeep;
            positionNetIncome.X = positionGrossUpkeep.X - 1;
            positionNetIncome.Y = positionGrossUpkeep.Y + (Fonts.Arial12.LineSpacing + 2);

            batch.DrawString(Fonts.Arial12, (netIncome > 0.0 ? nIncome : nLosses) + ":", positionNIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);
            batch.DrawString(Font12, netIncome.String(2) + " BC/Y", positionNetIncome, netIncome > 0.0 ? Color.LightGreen : Color.Salmon);

            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);

            DrawFoodAndStorage(batch);
            DrawOrbitalStats(batch);

            base.Draw(batch);
        }

        void DrawIncomingFreighters(SpriteBatch batch, ref Vector2 incomingTitle, ref Vector2 incomingData, 
            int numFreighters, string incomingCargo, GameText text)
        {
            if (numFreighters == 0)
                return;

            int lineDown      = Font12.LineSpacing + 2;
            string freighters = $"{numFreighters} ";
            incomingTitle.Y  += lineDown;
            incomingData.Y   += lineDown;

            batch.DrawString(Font12, $"{new LocalizedText(text).Text}:", incomingTitle, Color.Gray);
            batch.DrawString(Font12, freighters, incomingData, Color.LightGreen);
            if (incomingCargo == "0" || incomingCargo == "0.00")
                return;

            Vector2 numCargo = new Vector2(incomingData.X + Font12.MeasureString(freighters).X, incomingData.Y + 1);
            batch.DrawString(Font8, $"({incomingCargo})", numCargo, Color.DarkKhaki);
        }

        void DrawOutgoingFreighters(SpriteBatch batch, ref Vector2 outgoingTitle, ref Vector2 outgoingData, 
            int numFreighters, GameText text)
        {
            if (numFreighters == 0)
                return;

            int lineDown     = Font12.LineSpacing + 2;
            outgoingTitle.Y += lineDown;
            outgoingData.Y  += lineDown;
            batch.DrawString(Font12, $"{new LocalizedText(text).Text}:", outgoingTitle, Color.Gray);
            batch.DrawString(Font12, numFreighters.String(), outgoingData, Color.Gold);
        }

        void DrawFoodAndStorage(SpriteBatch batch)
        {
            FoodStorage.Max = P.Storage.Max;
            ProdStorage.Max = P.Storage.Max;
            FoodStorage.Progress = P.FoodHere.RoundUpTo(1);
            ProdStorage.Progress = P.ProdHere.RoundUpTo(1);
            if (P.FS == Planet.GoodState.STORE) foodDropDown.ActiveIndex = 0;
            else if (P.FS == Planet.GoodState.IMPORT) foodDropDown.ActiveIndex = 1;
            else if (P.FS == Planet.GoodState.EXPORT) foodDropDown.ActiveIndex = 2;
            if (P.NonCybernetic)
            {
                FoodStorage.Draw(batch);
                foodDropDown.Draw(batch);
            }
            else
            {
                FoodStorage.DrawGrayed(batch);
                foodDropDown.DrawGrayed(batch);
            }

            ProdStorage.Draw(batch);
            if (P.PS == Planet.GoodState.STORE) prodDropDown.ActiveIndex = 0;
            else if (P.PS == Planet.GoodState.IMPORT) prodDropDown.ActiveIndex = 1;
            else if (P.PS == Planet.GoodState.EXPORT) prodDropDown.ActiveIndex = 2;
            prodDropDown.Draw(batch);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_food"), FoodStorageIcon, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/icon_storage_production"), ProfStorageIcon, Color.White);

            if (FoodStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(73);
            if (ProfStorageIcon.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(74);
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
                if (pgs.building != null)
                {
                    var buildingIcon = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                        pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Buildings/icon_" + pgs.building.Icon + "_64x64"),
                        buildingIcon, pgs.building.IsPlayerAdded ? Color.WhiteSmoke : Color.White);
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

        void DrawOrbitalStats(SpriteBatch batch)
        {
            if (P.Owner != EmpireManager.Player)
                return;

            if (P.colonyType == Planet.ColonyType.Colony || P.colonyType != Planet.ColonyType.Colony && !P.GovOrbitals)
            {
                // Show build buttons
                BuildPlatform.Visible = P.Owner.CanBuildPlatforms && P.HasSpacePort;
                BuildStation.Visible  = P.Owner.CanBuildStations && P.HasSpacePort;
                BuildShipyard.Visible = P.Owner.CanBuildShipyards && P.HasSpacePort;
            }
            else if (P.GovOrbitals)
            {
                BuildPlatform.Visible = false;
                BuildStation.Visible  = false;
                BuildShipyard.Visible = false;

                // Draw Governor current / wanted orbitals
                Vector2 platformsStatVec = new Vector2(BuildPlatform.X + 30, BuildPlatform.Y + 5);
                Vector2 stationsStatVec  = new Vector2(BuildStation.X + 30, BuildStation.Y + 5);
                Vector2 shipyardsStatVec = new Vector2(BuildShipyard.X + 30, BuildShipyard.Y + 5);
                if (P.Owner.CanBuildPlatforms)
                    batch.DrawString(Font12, PlatformsStats, platformsStatVec, Color.White);

                if (P.Owner.CanBuildStations)
                    batch.DrawString(Font12, StationsStats, stationsStatVec, Color.White);

                if (P.Owner.CanBuildShipyards)
                    batch.DrawString(Font12, ShipyardsStats, shipyardsStatVec, Color.White);
            }

        }

        Color TextColor { get; } = Colors.Cream;

        void DrawTitledLine(ref Vector2 cursor, int titleId, string text)
        {
            Vector2 textCursor = cursor;
            textCursor.X += 100f;

            ScreenManager.SpriteBatch.DrawString(Font12, Localizer.Token(titleId) +": ", cursor, TextColor);
            ScreenManager.SpriteBatch.DrawString(Font12, text, textCursor, TextColor);
            cursor.Y += Font12.LineSpacing;
        }

        void DrawMultiLine(ref Vector2 cursor, string text)
        {
            DrawMultiLine(ref cursor, text, TextColor);
        }

        string MultiLineFormat(LocalizedText text)
        {
            return Font12.ParseText(text.Text, pFacilities.Rect.Width - 40);
        }

        void DrawMultiLine(ref Vector2 cursor, LocalizedText text, Color color)
        {
            string multiline = MultiLineFormat(text);
            ScreenManager.SpriteBatch.DrawString(Font12, multiline, cursor, color);
            cursor.Y += (Font12.MeasureString(multiline).Y + Font12.LineSpacing);
        }

        void DrawCommoditiesArea(Vector2 bCursor)
        {
            ScreenManager.SpriteBatch.DrawString(Font12, MultiLineFormat(4097), bCursor, TextColor);
        }

        void DrawDetailInfo(SpriteBatch batch, Vector2 bCursor)
        {
            if (pFacilities.NumTabs > 1 && pFacilities.SelectedIndex == 1)
            {
                DrawCommoditiesArea(bCursor);
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
                    DrawTitledLine(ref bCursor, 338, t.TargetType.ToString());
                    DrawTitledLine(ref bCursor, 339, strength);
                    DrawTitledLine(ref bCursor, 2218, t.ActualHardAttack.ToString());
                    DrawTitledLine(ref bCursor, 2219, t.ActualSoftAttack.ToString());
                    DrawTitledLine(ref bCursor, 6008, t.BoardingStrength.ToString());
                    DrawTitledLine(ref bCursor, 6023, t.Level.ToString());
                    DrawTitledLine(ref bCursor, 1966, t.ActualRange.ToString());
                    break;

                case string _:
                    DrawMultiLine(ref bCursor, P.Description);
                    string desc = "";
                    if (P.IsCybernetic)  desc = Localizer.Token(2028);
                    else switch (P.FS)
                    {
                        case Planet.GoodState.EXPORT: desc = Localizer.Token(2025); break;
                        case Planet.GoodState.IMPORT: desc = Localizer.Token(2026); break;
                        case Planet.GoodState.STORE:  desc = Localizer.Token(2027); break;
                    }

                    DrawMultiLine(ref bCursor, desc);
                    desc = "";
                    if (P.colonyType == Planet.ColonyType.Colony)
                    {
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(345); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(346); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(347); break;
                        }
                    }
                    else
                        switch (P.PS)
                        {
                            case Planet.GoodState.EXPORT: desc = Localizer.Token(1953); break;
                            case Planet.GoodState.IMPORT: desc = Localizer.Token(1954); break;
                            case Planet.GoodState.STORE:  desc = Localizer.Token(1955); break;
                        }
                    DrawMultiLine(ref bCursor, desc);
                    if (P.IsStarving)
                        DrawMultiLine(ref bCursor, Localizer.Token(344), Color.LightPink);
                    DrawPlanetStat(ref bCursor, batch);
                    break;

                case PlanetGridSquare pgs:
                    float popPerTile = P.BasePopPerTile * Player.RacialEnvModifer(P.Category);
                    switch (pgs.building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            batch.DrawString(Font20, Localizer.Token(348), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            bCursor.Y += Font20.LineSpacing * 5;
                            if (TerraformResearched && pgs.BioCanTerraform)
                            {
                                batch.DrawString(Font12, "This tile can be terraformed as part of terraforming operations.", bCursor, Player.EmpireColor);
                                bCursor.Y += Font20.LineSpacing;
                            }

                            batch.DrawString(Font12, $"{P.PopPerBiosphere.String(0)} {MultiLineFormat(1897)}", bCursor, Player.EmpireColor);
                            return;
                        case null when pgs.Habitable:
                            batch.DrawString(Font20, Localizer.Token(350), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(Font12, MultiLineFormat(349), bCursor, color);
                            bCursor.Y += Font20.LineSpacing * 5;
                            batch.DrawString(Font12, $"{popPerTile.String(0)} {MultiLineFormat(1898)}", bCursor, Color.LightGreen);
                            return;
                    }

                    if (!pgs.Habitable && pgs.building == null)
                    {
                        if (P.IsBarrenType)
                        {
                            batch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(Font12, MultiLineFormat(352), bCursor, color);
                        }
                        else
                        {
                            batch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(Font12, MultiLineFormat(353), bCursor, color);
                        }

                        bCursor.Y += Font20.LineSpacing * 5;
                        if (TerraformResearched && pgs.CanTerraform)
                        {
                            batch.DrawString(Font12, "This tile can be terraformed as part of terraforming operations.", bCursor, Player.EmpireColor);
                            bCursor.Y += Font20.LineSpacing;
                        }

                        batch.DrawString(Font12, $"{P.PopPerBiosphere.String(0)} {MultiLineFormat(1896)}", bCursor, Color.Gold);
                        return;
                    }

                    if (pgs.building == null)
                        return;

                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    batch.DrawString(Font20, Localizer.Token(pgs.building.NameTranslationIndex), bCursor, color);
                    bCursor.Y   += Font20.LineSpacing + 5;
                    string buildingDescription  = MultiLineFormat(pgs.building.DescriptionIndex);
                    batch.DrawString(Font12, buildingDescription, bCursor, color);
                    bCursor.Y   += Font12.MeasureString(buildingDescription).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, batch, pgs.building);
                    if (!pgs.building.Scrappable)
                        return;

                    bCursor.Y += (Font12.LineSpacing * 3);
                    batch.DrawString(Font12, "You may scrap this building by right clicking it", bCursor, Color.White);
                    break;

                case Building selectedBuilding:
                    batch.DrawString(Font20, Localizer.Token(selectedBuilding.NameTranslationIndex), bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionIndex);
                    batch.DrawString(Font12, selectionText, bCursor, color);
                    bCursor.Y += Font12.MeasureString(selectionText).Y + Font20.LineSpacing;
                    if (selectedBuilding.isWeapon)
                        selectedBuilding.CalcMilitaryStrength(); // So the building will have TheWeapon for stats

                    DrawSelectedBuildingInfo(ref bCursor, batch, selectedBuilding);
                    break;
            }
        }

        void DrawPlanetStat(ref Vector2 cursor, SpriteBatch batch)
        {
            DrawBuildingInfo(ref cursor, batch, P.PopPerTileFor(Player) / 1000, "UI/icon_pop_22", Localizer.Token(1874));
            DrawBuildingInfo(ref cursor, batch, P.PopPerBiosphere / 1000, "UI/icon_pop_22", Localizer.Token(1875));
            DrawBuildingInfo(ref cursor, batch, P.Food.NetYieldPerColonist - P.FoodConsumptionPerColonist, "NewUI/icon_food", Localizer.Token(1876), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Food.NetFlatBonus, "NewUI/icon_food", Localizer.Token(1877), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetYieldPerColonist - P.ProdConsumptionPerColonist, "NewUI/icon_production", Localizer.Token(1878), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetFlatBonus, "NewUI/icon_production", Localizer.Token(1879), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetYieldPerColonist, "NewUI/icon_science", Localizer.Token(1880), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetFlatBonus, "NewUI/icon_science", Localizer.Token(1881), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.MaxProductionToQueue, "NewUI/icon_queue_rushconstruction", Localizer.Token(1873), digits: 1);
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Building b)
        {
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatFoodAmount, "NewUI/icon_food", Localizer.Token(354));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFoodPerColonist, "NewUI/icon_food", Localizer.Token(2042));
            DrawBuildingInfo(ref bCursor, batch, b.SensorRange, "NewUI/icon_sensors", Localizer.Token(6000), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.ProjectorRange, "NewUI/icon_projection", Localizer.Token(6001), signs: false);
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatProductionAmount, "NewUI/icon_production", Localizer.Token(355));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerColonist, "NewUI/icon_production", Localizer.Token(356));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatPopulation / 1000, "NewUI/icon_population", Localizer.Token(2043));
            DrawBuildingInfo(ref bCursor, batch, b.PlusFlatResearchAmount, "NewUI/icon_science", Localizer.Token(357));
            DrawBuildingInfo(ref bCursor, batch, b.PlusResearchPerColonist, "NewUI/icon_science", Localizer.Token(358));
            DrawBuildingInfo(ref bCursor, batch, b.PlusTaxPercentage * 100, "NewUI/icon_money", Localizer.Token(359), percent: true);
            DrawBuildingInfo(ref bCursor, batch, b.MaxFertilityOnBuildFor(Player, P.Category), "NewUI/icon_food", Localizer.Token(360));
            DrawBuildingInfo(ref bCursor, batch, b.PlanetaryShieldStrengthAdded, "NewUI/icon_planetshield", Localizer.Token(361));
            DrawBuildingInfo(ref bCursor, batch, b.CreditsPerColonist, "NewUI/icon_money", Localizer.Token(362));
            DrawBuildingInfo(ref bCursor, batch, b.PlusProdPerRichness, "NewUI/icon_production", Localizer.Token(363));
            DrawBuildingInfo(ref bCursor, batch, b.ShipRepair * 10, "NewUI/icon_queue_rushconstruction", Localizer.Token(6137));
            DrawBuildingInfo(ref bCursor, batch, b.Infrastructure, "NewUI/icon_queue_rushconstruction", Localizer.Token(1872));
            DrawBuildingInfo(ref bCursor, batch, b.CombatStrength, "Ground_UI/Ground_Attack", Localizer.Token(364));
            DrawBuildingInfo(ref bCursor, batch, b.DefenseShipsCapacity, "UI/icon_hangar", b.DefenseShipsRole + " Defense Ships", signs: false);

            float maintenance = -b.ActualMaintenance(P);
            DrawBuildingInfo(ref bCursor, batch, maintenance, "NewUI/icon_money", Localizer.Token(365));

            DrawBuildingWeaponStats(ref bCursor, batch, b);
            DrawTerraformerStats(ref bCursor, batch, b);
            DrawFertilityOnBuildWarning(ref bCursor, batch, b);
        }

        void DrawFertilityOnBuildWarning(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.MaxFertilityOnBuild.LessOrEqual(0))
                return;

            if (P.MaxFertility + b.MaxFertilityOnBuildFor(Player, P.Category) < 0)
            {
                string warning = MultiLineFormat("WARNING - This building won't raise Max Fertility " +
                                                  "above 0 due to currently present negative environment " +
                                                  $"buildings on this planet (effective Max Fertility is {P.MaxFertility}).");

                cursor.Y += Font12.LineSpacing;
                batch.DrawString(Font12, warning, cursor, Color.Red);
                cursor.Y += Font12.LineSpacing * 4;
            }
        }

        void DrawTerraformerStats(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.PlusTerraformPoints.LessOrEqual(0))
                return;

            string terraformStats = TerraformPotential(out Color terraformColor);
            cursor.Y += Font12.LineSpacing;
            batch.DrawString(Font12, terraformStats, cursor, terraformColor);
            cursor.Y += Font12.LineSpacing * 4;
        }

        void DrawBuildingWeaponStats(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.TheWeapon == null)
                return;

            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.BaseRange, "UI/icon_offense", "Range", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.DamageAmount, "UI/icon_offense", "Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.EMPDamage, "UI/icon_offense", "EMP Damage", signs: false);
            DrawBuildingInfo(ref cursor, batch, b.TheWeapon.NetFireDelay, "UI/icon_offense", "Fire Delay", signs: false);
        }

        void UpdateData() // This will update freighters in an interval to reduce threading issues
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
                TerraformResearched    = Player.IsBuildingUnlocked(Building.TerraformerId);
                IncomingFood           = TotalIncomingCargo(Goods.Food).RoundUpTo(1);
                IncomingProd           = TotalIncomingCargo(Goods.Production).RoundUpTo(1);
                IncomingPop            = (TotalIncomingCargo(Goods.Colonists) / 1000).RoundToFractionOf100();
                UpdateTimer            = 300;
            }
            else if (!Empire.Universe.Paused)
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
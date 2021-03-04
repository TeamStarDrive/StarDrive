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
        bool Blockade;
        bool BioSpheresResearched;

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
            batch.DrawString(Fonts.Laserian14, Localizer.Token(369), TitlePos, Colors.Cream);

            PlanetInfo.Draw(batch, elapsed);
            PDescription.Draw(batch, elapsed);
            PStorage.Draw(batch, elapsed);
            SubColonyGrid.Draw(batch, elapsed);

            DrawPlanetSurfaceGrid(batch);
            PFacilities.Draw(batch, elapsed);
            DrawDetailInfo(batch, new Vector2(PFacilities.Rect.X + 15, PFacilities.Rect.Y + 35));
            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);

            float num5 = 80f;
            if (GlobalStats.IsGermanOrPolish)
                num5 += 20f;
            var vector2_2 = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
            P.Name = PlanetName.Text;
            PlanetName.Draw(batch, elapsed, Font20, vector2_2, Colors.Cream);
            EditNameButton = new Rectangle((int)(vector2_2.X + (double)Font20.MeasureString(P.Name).X + 12.0), (int)(vector2_2.Y + (double)(Font20.LineSpacing / 2) - ResourceManager.Texture("NewUI/icon_build_edit").Height / 2) - 2, ResourceManager.Texture("NewUI/icon_build_edit").Width, ResourceManager.Texture("NewUI/icon_build_edit").Height);
            if (EditHoverState == 0 && !PlanetName.HandlingInput)
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit"), EditNameButton, Color.White);
            else
                batch.Draw(ResourceManager.Texture("NewUI/icon_build_edit_hover2"), EditNameButton, Color.White);

            if (ScreenHeight > 768)
                vector2_2.Y += Font20.LineSpacing * 2;
            else
                vector2_2.Y += Font20.LineSpacing;
            batch.DrawString(TextFont, Localizer.Token(384) + ":", vector2_2, Color.Orange);
            Vector2 position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(TextFont, P.CategoryName, position3, Colors.Cream);
            vector2_2.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(TextFont, Localizer.Token(385) + ":", vector2_2, Color.Orange);
            var color = Colors.Cream;
            batch.DrawString(TextFont, P.PopulationStringForPlayer, position3, color);
            var rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)TextFont.MeasureString(Localizer.Token(385) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(75);
            vector2_2.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(TextFont, Localizer.Token(386) + ":", vector2_2, Color.Orange);
            string fertility;
            if (P.FertilityFor(Player).AlmostEqual(P.MaxFertilityFor(Player)))
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
            if (P.TerraformPoints > 0)
            {
                Color terraformColor = P.Owner?.EmpireColor ?? Color.White;
                string terraformText = Localizer.Token(683); // Terraform Planet is the default text
                if (P.HasTilesToTerraform)
                {
                    terraformText  = Localizer.Token(1972);
                }
                else if (P.BioSpheresToTerraform
                      && P.Category == P.Owner?.data.PreferredEnv 
                      && P.BaseMaxFertility.GreaterOrEqual(P.TerraformedMaxFertility))
                {
                    terraformText = Localizer.Token(1919);
                }

                int terraformOffSetX = LowRes ? 30 : 20;
                var terraformPos = new Vector2(PlanetIcon.X - terraformOffSetX, PlanetIcon.Y + PlanetIcon.Height + TextFont.LineSpacing-5);
                batch.DrawString(TextFont, $"{terraformText} - {(P.TerraformPoints * 100).String(0)}%", terraformPos, terraformColor);
            }

            UpdateData();
            if (IncomingFreighters > 0 && (P.Owner?.isPlayer == true || Empire.Universe.Debug))
            {
                Vector2 incomingTitle = new Vector2(vector2_2.X + + 200, vector2_2.Y - (TextFont.LineSpacing + 2) * 3);
                Vector2 incomingData =  new Vector2(vector2_2.X + 200 + num5, vector2_2.Y - (TextFont.LineSpacing + 2) * 3);
                batch.DrawString(TextFont, "Incoming Freighters:", incomingTitle, Color.White);

                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingFoodFreighters,
                    IncomingFood.String(), GameText.Food);
                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingProdFreighters,
                    IncomingProd.String(), GameText.Production);

                string popString = IncomingPop.LessOrEqual(1) ? $"{(IncomingPop * 1000).String(2)}m" : $"{IncomingPop.String()}b";
                DrawIncomingFreighters(batch, ref incomingTitle, ref incomingData, IncomingColoFreighters,
                    popString, GameText.Colonists);

            }

            if (OutgoingFreighters > 0 && (P.Owner?.isPlayer == true || Empire.Universe.Debug))
            {
                Vector2 outgoingTitle = new Vector2(vector2_2.X + +200, vector2_2.Y + (TextFont.LineSpacing + 2) * 2);
                Vector2 outgoingData  = new Vector2(vector2_2.X + 200 + num5, vector2_2.Y + (TextFont.LineSpacing + 2) * 2);
                batch.DrawString(TextFont, "Outgoing Freighters:", outgoingTitle, Color.White);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingFoodFreighters, GameText.Food);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingProdFreighters, GameText.Production);
                DrawOutgoingFreighters(batch, ref outgoingTitle, ref outgoingData, OutgoingColoFreighters, GameText.Colonists);
            }

            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)TextFont.MeasureString(Localizer.Token(386) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(20);
            vector2_2.Y += TextFont.LineSpacing + 2;
            position3 = new Vector2(vector2_2.X + num5, vector2_2.Y);
            batch.DrawString(TextFont, Localizer.Token(387) + ":", vector2_2, Color.Orange);
            batch.DrawString(TextFont, P.MineralRichness.String(), position3, Colors.Cream);
            rect = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)TextFont.MeasureString(Localizer.Token(387) + ":").X, TextFont.LineSpacing);
            if (rect.HitTest(Input.CursorPosition) && Empire.Universe.IsActive)
                ToolTip.CreateTooltip(21);

            DrawFoodAndStorage(batch);
            BlockadeLabel.Visible = Blockade;
            BlockadeLabel.Color   = ApplyCurrentAlphaToColor(Color.Red);

            base.Draw(batch, elapsed);
        }

        void DrawIncomingFreighters(SpriteBatch batch, ref Vector2 incomingTitle, ref Vector2 incomingData, 
            int numFreighters, string incomingCargo, GameText text)
        {
            if (numFreighters == 0)
                return;

            int lineDown      = TextFont.LineSpacing + 2;
            string freighters = $"{numFreighters} ";
            incomingTitle.Y  += lineDown;
            incomingData.Y   += lineDown;

            batch.DrawString(TextFont, $"{new LocalizedText(text).Text}:", incomingTitle, Color.Gray);
            batch.DrawString(TextFont, freighters, incomingData, Color.LightGreen);
            if (incomingCargo == "0" || incomingCargo == "0m")
                return;

            Vector2 numCargo = new Vector2(incomingData.X + TextFont.MeasureString(freighters).X, incomingData.Y + 1);
            batch.DrawString(Font8, $"({incomingCargo})", numCargo, Color.DarkKhaki);
        }

        void DrawOutgoingFreighters(SpriteBatch batch, ref Vector2 outgoingTitle, ref Vector2 outgoingData, 
            int numFreighters, GameText text)
        {
            if (numFreighters == 0)
                return;

            int lineDown     = TextFont.LineSpacing + 2;
            outgoingTitle.Y += lineDown;
            outgoingData.Y  += lineDown;
            batch.DrawString(TextFont, $"{new LocalizedText(text).Text}:", outgoingTitle, Color.Gray);
            batch.DrawString(TextFont, numFreighters.String(), outgoingData, Color.Gold);
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
            return TextFont.ParseText(text.Text, PFacilities.Rect.Width - 40);
        }

        void DrawMultiLine(ref Vector2 cursor, LocalizedText text, Color color)
        {
            string multiline = MultiLineFormat(text);

            ScreenManager.SpriteBatch.DrawString(TextFont, multiline, cursor, color);
            cursor.Y += (TextFont.MeasureString(multiline).Y + TextFont.LineSpacing);
        }

        void DrawCommoditiesArea(Vector2 bCursor)
        {
            ScreenManager.SpriteBatch.DrawString(TextFont, MultiLineFormat(4097), bCursor, TextColor);
        }

        void DrawDetailInfo(SpriteBatch batch, Vector2 bCursor)
        {
            if (PFacilities.SelectedIndex == 0)
            {
                DrawMoney(ref bCursor, batch);
                DrawPlanetStat(ref bCursor, batch);
                //DrawCommoditiesArea(bCursor);
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

                    break;
                case PlanetGridSquare pgs:
                    switch (pgs.Building)
                    {
                        case null when pgs.Habitable && pgs.Biosphere:
                            batch.DrawString(Font20, Localizer.Token(348), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(349), bCursor, color);
                            DrawTilePopInfo(ref bCursor, batch, pgs);
                            return;
                        case null when pgs.Habitable:
                            batch.DrawString(Font20, Localizer.Token(350), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(349), bCursor, color);
                            DrawTilePopInfo(ref bCursor, batch, pgs);
                            return;
                    }

                    if (!pgs.Habitable)
                    {
                        if (P.IsBarrenType)
                        {
                            batch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(352), bCursor, color);
                        }
                        else
                        {
                            batch.DrawString(Font20, Localizer.Token(351), bCursor, color);
                            bCursor.Y += Font20.LineSpacing + 5;
                            batch.DrawString(TextFont, MultiLineFormat(353), bCursor, color);
                        }

                        DrawTilePopInfo(ref bCursor, batch, pgs);
                        return;
                    }

                    if (pgs.Building == null)
                        return;

                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    batch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), bRect, Color.White);
                    batch.DrawString(Font20, Localizer.Token(pgs.Building.NameTranslationIndex), bCursor, color);
                    bCursor.Y   += Font20.LineSpacing + 5;
                    string buildingDescription  = MultiLineFormat(pgs.Building.DescriptionIndex);
                    batch.DrawString(TextFont, buildingDescription, bCursor, color);
                    bCursor.Y   += TextFont.MeasureString(buildingDescription).Y + Font20.LineSpacing;
                    DrawSelectedBuildingInfo(ref bCursor, batch, pgs.Building);
                    DrawTilePopInfo(ref bCursor, batch, pgs, 2);
                    if (!pgs.Building.Scrappable)
                        return;

                    bCursor.Y += TextFont.LineSpacing * 2;
                    batch.DrawString(TextFont, "You may scrap this building by right clicking it", bCursor, Color.White);
                    break;

                case Building selectedBuilding:
                    batch.DrawString(Font20, Localizer.Token(selectedBuilding.NameTranslationIndex), bCursor, color);
                    bCursor.Y += Font20.LineSpacing + 5;
                    string selectionText = MultiLineFormat(selectedBuilding.DescriptionIndex);
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
            string gIncome = Localizer.Token(6125);
            string gUpkeep = Localizer.Token(6126);
            string nIncome = Localizer.Token(6127);
            string nLosses = Localizer.Token(6129);

            float grossIncome = P.Money.GrossRevenue;
            float grossUpkeep = P.Money.Maintenance + P.SpaceDefMaintenance;
            float netIncome   = P.Money.NetRevenue;

            SpriteFont font = LowRes ? Font8 : Font14;

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
            if (tile.Habitable && tile.Biosphere)
            {
                batch.DrawString(TextFont, $"{(P.PopPerBiosphere(Player) + popBonus).String(1)}" +
                                         $" {MultiLineFormat(1897)}", cursor, Player.EmpireColor);

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
                batch.DrawString(TextFont, $"{(popPerTile + popBonus).String(1)} {MultiLineFormat(1898)}", cursor, Color.LightGreen);
            }
            else
            {
                string bioText = new LocalizedText(GameText.MillionColonistsCouldBeLiving).Text;
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
            DrawBuildingInfo(ref cursor, batch, P.PopPerTileFor(Player) / 1000, "UI/icon_pop_22", Localizer.Token(1874));
            DrawBuildingInfo(ref cursor, batch, P.PopPerBiosphere(Player) / 1000, "UI/icon_pop_22", Localizer.Token(1875));
            DrawBuildingInfo(ref cursor, batch, P.Food.NetYieldPerColonist - P.FoodConsumptionPerColonist, "NewUI/icon_food", Localizer.Token(1876), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Food.NetFlatBonus, "NewUI/icon_food", Localizer.Token(1877), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetYieldPerColonist - P.ProdConsumptionPerColonist, "NewUI/icon_production", Localizer.Token(1878), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Prod.NetFlatBonus, "NewUI/icon_production", Localizer.Token(1879), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetYieldPerColonist, "NewUI/icon_science", Localizer.Token(1880), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.Res.NetFlatBonus, "NewUI/icon_science", Localizer.Token(1881), digits: 1);
            DrawBuildingInfo(ref cursor, batch, P.CurrentProductionToQueue, "NewUI/icon_queue_rushconstruction",
                $"{Localizer.Token(1873)} ({P.InfraStructure} taken from Storage)", digits: 1);
        }

        void DrawSelectedBuildingInfo(ref Vector2 bCursor, SpriteBatch batch, Building b, PlanetGridSquare tile = null)
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

            if (tile?.VolcanoHere == true)
                DrawVolcanoChance(ref bCursor, batch, tile.Volcano.ActivationChanceText());
        }

        void DrawVolcanoChance(ref Vector2 cursor, SpriteBatch batch, string text)
        {
            batch.DrawString(TextFont, text, cursor, Color.Red);
            cursor.Y += TextFont.LineSpacing;
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

                cursor.Y += TextFont.LineSpacing;
                batch.DrawString(TextFont, warning, cursor, Color.Red);
                cursor.Y += TextFont.LineSpacing * 4;
            }
        }

        void DrawTerraformerStats(ref Vector2 cursor, SpriteBatch batch, Building b)
        {
            if (b.PlusTerraformPoints.LessOrEqual(0))
                return;

            string terraformStats = TerraformPotential(out Color terraformColor);
            cursor.Y += TextFont.LineSpacing;
            batch.DrawString(TextFont, terraformStats, cursor, terraformColor);
            cursor.Y += TextFont.LineSpacing * 4;
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
                BioSpheresResearched   = Player.IsBuildingUnlocked(Building.BiospheresId);
                IncomingFood           = TotalIncomingCargo(Goods.Food).RoundUpTo(1);
                IncomingProd           = TotalIncomingCargo(Goods.Production).RoundUpTo(1);
                IncomingPop            = (TotalIncomingCargo(Goods.Colonists) / 1000).RoundToFractionOf100();
                Blockade               = P.Quarantine || P.SpaceCombatNearPlanet;
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
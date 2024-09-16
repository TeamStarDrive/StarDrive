using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        // this is called every UI frame, ~60fps
        public override void Update(float elapsedTime)
        {
            if (P.Owner == null)
            {
                if (!IsExiting)
                    ExitScreen();
                return;
            }

            P.UpdateIncomes();
            UpdateBuildAndConstructLists(elapsedTime);
            UpdateTradeTab();
            UpdateTerraformTab();
            UpdateDysonSwarmTab();
            base.Update(elapsedTime);
        }

        void UpdateTradeTab()
        {
            TradeTitle.Visible           =
            ManualImportTitle.Visible    =
            ManualExportTitle.Visible    =
            IncomingTradeTitle.Visible   =
            OutgoingTradeTitle.Visible   =
            IncomingColoPanel.Visible    =
            IncomingProdPanel.Visible    = 
            IncomingFoodPanel.Visible    = 
            OutgoingColoPanel.Visible    =
            OutgoingProdPanel.Visible    =
            OutgoingFoodPanel.Visible    = IsTradeTabSelected;

            ImportFoodSlotSlider.Visible =
            ImportProdSlotSlider.Visible =
            ImportColoSlotSlider.Visible =
            ExportFoodSlotSlider.Visible =
            ExportProdSlotSlider.Visible =
            ExportColoSlotSlider.Visible = IsTradeTabSelected && P.Owner == Player;

            IncomingFoodAmount.Visible   = IsTradeTabSelected && IncomingFood > 0;
            IncomingProdAmount.Visible   = IsTradeTabSelected && IncomingProd > 0;
            IncomingColoAmount.Visible   = IsTradeTabSelected && IncomingPop > 0;

            if (!IsTradeTabSelected)
                return;

            IncomingFoodBar.Max      = P.FoodImportSlots;
            IncomingFoodBar.Progress = IncomingFoodFreighters;
            IncomingProdBar.Max      = P.ProdImportSlots;
            IncomingProdBar.Progress = IncomingProdFreighters;
            IncomingColoBar.Max      = P.ColonistsImportSlots;
            IncomingColoBar.Progress = IncomingColoFreighters;
            OutgoingFoodBar.Max      = P.FoodExportSlots;
            OutgoingFoodBar.Progress = OutgoingFoodFreighters;
            OutgoingProdBar.Max      = P.ProdExportSlots;
            OutgoingProdBar.Progress = OutgoingProdFreighters;
            OutgoingColoBar.Max      = P.ColonistsExportSlots;
            OutgoingColoBar.Progress = OutgoingColoFreighters;

            IncomingFoodAmount.Text = $"({IncomingFood.String()})";
            IncomingProdAmount.Text = $"({IncomingProd.String()})";
            IncomingColoAmount.Text = $"({IncomingPopString})";

            P.ManualFoodImportSlots = (int)Math.Round(ImportFoodSlotSlider.AbsoluteValue);
            P.ManualProdImportSlots = (int)Math.Round(ImportProdSlotSlider.AbsoluteValue);
            P.ManualColoImportSlots = (int)Math.Round(ImportColoSlotSlider.AbsoluteValue);
            P.ManualFoodExportSlots = (int)Math.Round(ExportFoodSlotSlider.AbsoluteValue);
            P.ManualProdExportSlots = (int)Math.Round(ExportProdSlotSlider.AbsoluteValue);
            P.ManualColoExportSlots = (int)Math.Round(ExportColoSlotSlider.AbsoluteValue);

            IncomingTradeTitle.Color = GetIncomingTradeTitleColor();
            OutgoingTradeTitle.Color = GetOutgoingTradeTitleColor();
            ManualImportTitle.Color  = GetManualImportSlotsOverrideColor();
            ManualExportTitle.Color  = GetManualExportSlotsOverrideColor();
        }

        void UpdateDysonSwarmTab()
        {
            if (!IsDysonSwarmTabSelected)
            {
                if (DysonSwarmTypeTitle?.Visible == true)
                    HideDysonSwarmUI();

                return;
            }
            DysonSwarmTypeTitle.Visible       =
            DysonSwarmControllerPanel.Visible =
            DysonSwarmProdBoost.Visible       =
            DysonSwarmPanel.Visible           = true;
            DysonSwarmStartButton.Visible = !P.System.HasDysonSwarm && P.OwnerIsPlayer;
            DysonSwarmKillButton.Visible = P.System.HasDysonSwarm && P.OwnerIsPlayer;
            DysonSwarmStatus.Visible = P.System.HasDysonSwarm;
            if (P.System.HasDysonSwarm)
            {
                DysonSwarmControllerProgress.Progress = P.System.DysonSwarm.ControllerCompletion * 100;
                DysonSwarmProgress.Progress = P.System.DysonSwarm.NumSwarmSats;
                DysonSwarmProductionBoost.Progress = P.System.DysonSwarm.ProductionBoost;
                if (P.System.DysonSwarm.IsCompleted)
                {
                    DysonSwarmStatus.Text = GameText.DysonSwarmDeploymentCompleted;
                    DysonSwarmStatus.Color = Color.Green;
                }
                else
                {
                    DysonSwarmStatus.Text =  GameText.TerraformersInProgress;
                    DysonSwarmStatus.Color = ApplyCurrentAlphaToColor(Color.Yellow);
                }
                
            }
        }

        void HideDysonSwarmUI()
        {
            DysonSwarmTypeTitle.Visible =
            DysonSwarmControllerPanel.Visible =
            DysonSwarmProdBoost.Visible =
            DysonSwarmPanel.Visible = 
            DysonSwarmStartButton.Visible =
            DysonSwarmStatus.Visible = 
            DysonSwarmKillButton.Visible = false;
        }

        void UpdateTerraformTab()
        {
            if (TerraformLevel < 1)
                return;

            TerrainTerraformTitle.Visible =
            TerraformStatusTitle.Visible  =
            TerraformStatus.Visible       =
            TerraformTitle.Visible        = IsTerraformTabSelected;

            TerraformersHereTitle.Visible =
            TerraformersHere.Visible      = IsTerraformTabSelected && Terraformable;

            TileTerraformTitle.Visible   = IsTerraformTabSelected && TerraformLevel >= 2;
            PlanetTerraformTitle.Visible = IsTerraformTabSelected && TerraformLevel >= 3;
            VolcanoTerraformDone.Visible = TerrainTerraformTitle.Visible && !NeedLevel1Terraform;
            TileTerraformDone.Visible    = TileTerraformTitle.Visible && !NeedLevel2Terraform;
            PlanetTerraformDone.Visible  = PlanetTerraformTitle.Visible && !NeedLevel3Terraform;

            TerraformTitle.Text   = $"{Localizer.Token(GameText.TerraformingOperationsLevel)} {TerraformLevel}";
            TerraformersHere.Text = P.ContainsEventTerraformers && P.Owner.data.Traits.TerraformingLevel < 1 
                ? Localizer.Token(GameText.TerraformersUnknownOrigin) 
                : $"{NumTerraformersHere}/{NumMaxTerraformers}";

            if (P.TerraformingHere)
            {
                TerraformStatus.Text  = GameText.TerraformersInProgress;
                TerraformStatus.Color = ApplyCurrentAlphaToColor(Color.Yellow);
            }
            else
            {
                TerraformStatus.Text  = Terraformable ? GameText.TerraformersNotStarted : GameText.TerraformersDone;
                TerraformStatus.Color = Terraformable ? Color.Orange : Color.Green;
            }

            TerrainTerraformTitle.Text = NumTerrain > 0 ? $"{Localizer.Token(GameText.TerraformersTerrain2)}{NumTerrain}):" 
                                                        : Localizer.Token(GameText.TerraformersTerrain);

            TileTerraformTitle.Text    = NumTerraformableTiles > 0 ? $"{Localizer.Token(GameText.TerraformersTiles2)}{NumTerraformableTiles}):" : Localizer.Token(GameText.TerraformersTiles);

            TerrainTerraformBar.Progress = NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;
            TileTerraformBar.Progress    = NeedLevel2Terraform && !NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;
            PlanetTerraformBar.Progress  = NeedLevel3Terraform && !NeedLevel2Terraform & !NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;

            TargetFertilityTitle.Visible =
            TargetFertility.Visible      = IsTerraformTabSelected  && NeedLevel3Terraform && TerraformLevel >= 3;
            TargetFertility.Text         = GetTargetFertilityText(out Color color);
            TargetFertility.Color        = color;

            EstimatedMaxPop.Text         = $"{MinEstimatedMaxPop.String(1)}";
            EstimatedMaxPopTitle.Visible =
            EstimatedMaxPop.Visible      = IsTerraformTabSelected 
                                            && NeedLevel3Terraform 
                                            && TerraformLevel >= 3 && TerraMaxPopBillion.Less(MinEstimatedMaxPop);
        }

        bool IsDysonSwarmTabSelected => PFacilities.IsTabSelected(Localizer.Token(GameText.DysonSwarm));
        bool IsTerraformTabSelected  => PFacilities.IsTabSelected(Localizer.Token(GameText.BB_Tech_Terraforming_Name));
        bool IsTradeTabSelected      => PFacilities.SelectedIndex == 2;
        bool IsStatTabSelected       => PFacilities.SelectedIndex == 0;

        Color GetManualImportSlotsOverrideColor()
        {
            if (P.ManualFoodImportSlots > 0
                || P.ManualProdImportSlots > 0
                || P.ManualColoImportSlots > 0)
            {
                return P.Owner.EmpireColor;
            }

            return Color.Gray;
        }

        Color GetManualExportSlotsOverrideColor()
        {
            if (P.ManualFoodExportSlots > 0
                || P.ManualProdExportSlots > 0
                || P.ManualColoExportSlots > 0)
            {
                return P.Owner.EmpireColor;
            }

            return Color.Gray;
        }

        Color GetIncomingTradeTitleColor()
        {
            if (IncomingFoodFreighters > 0
                || IncomingProdFreighters > 0
                || IncomingColoFreighters > 0)
            {
                return P.Owner.EmpireColor;
            }

            return Color.Gray;
        }

        Color GetOutgoingTradeTitleColor()
        {
            if (OutgoingFoodFreighters > 0
                || OutgoingProdFreighters > 0
                || OutgoingColoFreighters > 0)
            {
                return P.Owner.EmpireColor;
            }

            return Color.Gray;
        }
    }


}

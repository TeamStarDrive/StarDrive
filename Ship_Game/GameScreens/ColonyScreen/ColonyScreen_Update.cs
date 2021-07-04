using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        public override void Update(float elapsedTime)
        {
            P.UpdateIncomes(false);
            UpdateBuildAndConstructLists(elapsedTime);
            UpdateTradeTab();
            UpdateTerraformTab();
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

            P.SetManualFoodImportSlots((int)ImportFoodSlotSlider.AbsoluteValue);
            P.SetManualProdImportSlots((int)ImportProdSlotSlider.AbsoluteValue);
            P.SetManualColoImportSlots((int)ImportColoSlotSlider.AbsoluteValue);
            P.SetManualFoodExportSlots((int)ExportFoodSlotSlider.AbsoluteValue);
            P.SetManualProdExportSlots((int)ExportProdSlotSlider.AbsoluteValue);
            P.SetManualColoExportSlots((int)ExportColoSlotSlider.AbsoluteValue);

            IncomingTradeTitle.Color = GetIncomingTradeTitleColor();
            OutgoingTradeTitle.Color = GetOutgoingTradeTitleColor();
            ManualImportTitle.Color  = GetManualImportSlotsOverrideColor();
            ManualExportTitle.Color  = GetManualExportSlotsOverrideColor();
        }

        void UpdateTerraformTab()
        {
            if (TerraformLevel < 1)
                return;

            VolcanoTerraformTitle.Visible =
            TerraformStatusTitle.Visible  =
            TerraformStatus.Visible       =
            TerraformTitle.Visible        = IsTerraformTabSelected;

            TerraformersHereTitle.Visible =
            TerraformersHere.Visible      = IsTerraformTabSelected && Terraformable;

            TileTerraformTitle.Visible   = IsTerraformTabSelected && TerraformLevel >= 2;
            PlanetTerraformTitle.Visible = IsTerraformTabSelected && TerraformLevel >= 3;
            VolcanoTerraformDone.Visible = VolcanoTerraformTitle.Visible && !NeedLevel1Terraform;
            TileTerraformDone.Visible    = TileTerraformTitle.Visible && !NeedLevel2Terraform;
            PlanetTerraformDone.Visible  = PlanetTerraformTitle.Visible && !NeedLevel3Terraform;

            TerraformTitle.Text   = $"{Localizer.Token(GameText.TerraformingOperationsLevel)} {TerraformLevel}";
            TerraformersHere.Text = $"{NumTerraformersHere}/{NumMaxTerraformers}";

            if (P.TerraformingHere)
            {
                TerraformStatus.Text  = Localizer.Token(GameText.TerraformersInProgress);
                TerraformStatus.Color = ApplyCurrentAlphaToColor(Color.Yellow);
            }
            else
            {
                TerraformStatus.Text  = Terraformable ? Localizer.Token(GameText.TerraformersNotStarted) : Localizer.Token(GameText.TerraformersDone);
                TerraformStatus.Color = Terraformable ? Color.Orange : Color.Green;
            }

            VolcanoTerraformTitle.Text = NumVolcanoes > 0 ?  $"{Localizer.Token(GameText.TerraformersVolcanoes2)}{NumVolcanoes}):" : Localizer.Token(GameText.TerraformersVolcanoes);
            TileTerraformTitle.Text    = NumTerraformableTiles > 0 ? $"{Localizer.Token(GameText.TerraformersTiles2)}{NumTerraformableTiles}):" : Localizer.Token(GameText.TerraformersTiles);

            VolcanoTerraformBar.Progress = NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;
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

        bool IsTerraformTabSelected => PFacilities.SelectedIndex == 3;
        bool IsTradeTabSelected     => PFacilities.SelectedIndex == 2;
        bool IsStatTabSelected      => PFacilities.SelectedIndex == 0;

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

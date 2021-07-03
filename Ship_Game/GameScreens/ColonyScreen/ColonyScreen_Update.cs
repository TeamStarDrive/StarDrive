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
            UpdateTerraformTab();
            base.Update(elapsedTime);
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

            TerraformersHere.Text = $"{NumTerraformersHere}/{NumMaxTerraformers}";

            if (P.TerraformingHere)
            {
                TerraformStatus.Text  = Localizer.Token(4324);
                TerraformStatus.Color = ApplyCurrentAlphaToColor(Color.Yellow);
            }
            else
            {
                TerraformStatus.Text  = Terraformable ? Localizer.Token(4323) : Localizer.Token(4317);
                TerraformStatus.Color = Terraformable ? Color.Orange : Color.Green;
            }

            VolcanoTerraformTitle.Text = NumVolcanoes > 0 ?  $"{Localizer.Token(4327)}{NumVolcanoes}):" : Localizer.Token(4325);
            TileTerraformTitle.Text    = NumTerraformableTiles > 0 ? $"{Localizer.Token(4328)}{ NumTerraformableTiles}):" : Localizer.Token(4326);

            VolcanoTerraformBar.Progress = NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;
            TileTerraformBar.Progress    = NeedLevel2Terraform && !NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;
            PlanetTerraformBar.Progress  = NeedLevel3Terraform && !NeedLevel2Terraform & !NeedLevel1Terraform ? P.TerraformPoints * 100 : 0;

            TargetFertilityTitle.Visible =
            TargetFertility.Visible      = IsTerraformTabSelected  && NeedLevel3Terraform && TerraformLevel >= 3;
            TargetFertility.Text         = GetTargetFertilityText(out Color color);
            TargetFertility.Color        = color;

            EstimatedMaxPop.Text         = $"{MinEstimatedMaxPop.String(2)}";
            EstimatedMaxPopTitle.Visible =
            EstimatedMaxPop.Visible      = IsTerraformTabSelected 
                                            && NeedLevel3Terraform 
                                            && TerraformLevel >= 3 && TerraMaxPopBillion.Less(MinEstimatedMaxPop);
        }

        bool IsTerraformTabSelected => PFacilities.SelectedIndex == 2;
        bool IsStatTabSelected      => PFacilities.SelectedIndex == 0;
    }
}
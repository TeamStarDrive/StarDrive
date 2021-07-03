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
            int terraLevel = P.Owner.data.Traits.TerraformingLevel;
            if (terraLevel < 1)
                return;

            VolcanoTerraformTitle.Visible =
            TerraformStatusTitle.Visible  =
            TerraformStatus.Visible       =
            TerraformTitle.Visible        = IsTerraformTabSelected;

            TerraformersHereTitle.Visible =
            TerraformersHere.Visible      = IsTerraformTabSelected && Terraformable;

            TileTerraformTitle.Visible   = IsTerraformTabSelected && terraLevel >= 2;
            PlanetTerraformTitle.Visible = IsTerraformTabSelected && terraLevel >= 3;
            VolcanoTerraform.Visible     = VolcanoTerraformTitle.Visible && !NeedLevel1Terraform;
            TileTerraform.Visible        = TileTerraformTitle.Visible && !NeedLevel2Terraform;
            PlanetTerraform.Visible      = PlanetTerraformTitle.Visible && !NeedLevel3Terraform;

            TerraformersHere.Text = $"{NumTerraformersHere}/{NumMaxTerraformers}";

            if (P.TerraformingHere)
            {
                TerraformStatus.Text  = "In Progress";
                TerraformStatus.Color = ApplyCurrentAlphaToColor(Color.Yellow);
            }
            else
            {
                TerraformStatus.Text  = Terraformable ? "Not Started" : "Done";
                TerraformStatus.Color = Terraformable ? Color.Orange : Color.Green;
            }



        }

        bool IsTerraformTabSelected => PFacilities.SelectedIndex == 2;
        bool IsStatTabSelected      => PFacilities.SelectedIndex == 0;
    }
}
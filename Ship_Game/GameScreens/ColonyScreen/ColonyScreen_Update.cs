using System.Linq;
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
            if (P.Owner.data.Traits.TerraformingLevel < 1)
                return;

            TerraformTitle.Visible = PFacilities.SelectedIndex == 2;
        }
    }
}
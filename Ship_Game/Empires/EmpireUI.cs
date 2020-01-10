namespace Ship_Game.Empires
{
    public class EmpireUI
    {
        readonly Empire Empire;
        public EmpireUI(Empire empire) => Empire = empire;

        public UILabel UILabelTechLevelInfo()
        {
            string techLevelString = "Empire ShipTech Level";
            string techCountString = "Total";
            string labelText       = $"{techLevelString} : {Empire.GetEmpireTechLevel()}";
            labelText             += $" - {techCountString}: {Empire.ShipTechs.Count}";
            var techUILabel        = new UILabel(labelText);
            
            return techUILabel;
        }
    }
}
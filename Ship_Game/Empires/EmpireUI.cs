namespace Ship_Game.Empires
{
    public class EmpireUI
    {
        readonly Empire Empire;
        public EmpireUI(Empire empire) => Empire = empire;

        public UILabel UILabelTechLevelInfo()
        {
            var techLevelString = new LocalizedText("Empire ShipTech Level", LocalizationMethod.RawText).Text;
            var techCountString = new LocalizedText("Total", LocalizationMethod.RawText).Text;
            var labelText = $"{techLevelString} : {Empire.GetEmpireTechLevel()}";
            labelText += $" - {techCountString}: {Empire.ShipTechs.Count}";
            var techUILabel = new UILabel(labelText);
            return techUILabel;
        }
    }
}
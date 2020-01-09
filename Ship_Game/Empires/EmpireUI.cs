namespace Ship_Game.Empires
{
    public class EmpireUI
    {
        readonly Empire Empire;
        public EmpireUI(Empire empire) => Empire = empire;

        static string LocalizeRawText(string text) => new LocalizedText(text, LocalizationMethod.RawText).Text;

        public UILabel UILabelTechLevelInfo()
        {
            var techLevelString = LocalizeRawText("Empire ShipTech Level");
            var techCountString = LocalizeRawText("Total");
            var labelText       = $"{techLevelString} : {Empire.GetEmpireTechLevel()}";
            labelText          += $" - {techCountString}: {Empire.ShipTechs.Count}";
            var techUILabel     = new UILabel(labelText);
            return techUILabel;
        }
    }
}
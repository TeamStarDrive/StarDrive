using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsPlantMole : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 20; // need to get 20 and above in a roll of d100)
        const float BaseRelationDamage = 10;
        const int BaseRampUpTurns = 20;

        public InfiltrationOpsPlantMole(Empire owner, Empire them, int levelCost, byte level) :
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationOpsType.PlantMole, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them);
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? SuccessTargetNumber / 2 : SuccessTargetNumber, Level);
            Espionage espionage = Owner.GetEspionage(Them);
            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                case InfiltrationOpsResult.GreatSuccess:
                case InfiltrationOpsResult.Success:
                    aftermath.GoodResult = true;
                    var mole = Mole.PlantMole(Owner, Them, out Planet planet);
                    aftermath.Planet = planet; // Planet will not be nul if mole is not null
                    aftermath.CustomMessage = mole != null ? $"{Localizer.Token(GameText.NewSuccessfullyInfiltratedAColony)} {planet.Name}." 
                                                           : $"{Localizer.Token(GameText.NoColonyForInfiltration)} {Them.data.Traits.Name}";

                    if (result is InfiltrationOpsResult.Phenomenal or InfiltrationOpsResult.GreatSuccess)
                    {
                        SetProgress(Cost * (result is InfiltrationOpsResult.Phenomenal ? 0.5f : 0.25f));
                        if (mole != null)
                            aftermath.CustomMessage = $"{aftermath.CustomMessage}\n{Localizer.Token(GameText.WeMadeInfiltrationProgress)}";
                    }

                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.NewWasUnableToInfiltrate;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.NewWasUnableToInfiltrateMiserable;
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.NewWasUnableToInfiltrateDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasFoiled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.NewWasUnableToInfiltrateWipedOut;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.NewWipedOutNetworkInfiltration)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.Level}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            aftermath.SendNotifications(Owner.Universe);
        }
    }
}

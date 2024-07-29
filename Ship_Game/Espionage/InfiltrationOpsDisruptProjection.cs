using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsDisruptProjection : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        public const float PercentOfLevelCost = 0.5f;
        const int SuccessTargetNumber = 45; // need to get 45 and above in a roll of d100)
        const float BaseRelationDamage = 20;
        public const int BaseRampUpTurns = 45;

        [StarDataConstructor]
        public InfiltrationOpsDisruptProjection() { }

        public InfiltrationOpsDisruptProjection(Empire owner, Empire them, int levelCost) :
            base((int)(levelCost * PercentOfLevelCost), InfiltrationOpsType.SlowResearch, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            var result = RollMissionResult(Owner, Them, SuccessTargetNumber);
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them, result);
            Espionage espionage = Owner.GetEspionage(Them);
            aftermath.MessageUseTheirName = true;
            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    espionage.SetDisruptProjectionChance(100);
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    espionage.SetDisruptProjectionChance(75);
                    break;
                case InfiltrationOpsResult.Success:
                    espionage.SetDisruptProjectionChance(50);
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.InfiltrationDisruptProjectionFail;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.InfiltrationDisruptProjectionMiserableFail;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptProjectionMiserableFailVictim)}";
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.InfiltrationDisruptProjectionCriticalFail;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptProjectionCriticalFailVictim)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.InfiltrationDisruptProjectionDisaster;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptProjectionDisasterVictim)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.EffectiveLevel}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (aftermath.GoodResult)
            {
                aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationDisruptSuccessVictim);
                aftermath.Message = GameText.InfiltrationDisruptProjectionSuccess;
            }

            aftermath.SendNotifications(Owner.Universe);
        }
    }
}

using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Data.Serialization;
using System.Linq;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsCounterEspionage : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 10;
        const int BaseRampUpTurns = 20;

        public InfiltrationOpsCounterEspionage(Empire owner, Empire them, int levelCost, byte level) :
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
            Espionage theirEspionage = Them.GetEspionage(Owner);
            var potentialMoles = Them.data.MoleList.Filter(m => !m.Sticky && Owner.GetPlanets().Any(p => p.Id == m.PlanetId));

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    if (theirEspionage.Level > 0)
                        theirEspionage.WipeoutInfiltration();
                    else if (potentialMoles.Length > 0)
                        Them.RemoveMole(Them.Random.Item(potentialMoles));
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    if (theirEspionage.Level > 0)
                        theirEspionage.ReduceInfiltrationLevel();
                    else if (potentialMoles.Length > 0)
                        Them.RemoveMole(Them.Random.Item(potentialMoles));
                    break;
                case InfiltrationOpsResult.Success:
                    if (potentialMoles.Length > 0)
                        Them.RemoveMole(Them.Random.Item(potentialMoles));
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = Localizer.Token(GameText.CounterEspioangeOpsFailed);
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseMiserable;
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasFoiled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.FailedToInciteUpriseWipedOut;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.NewWipedOutNetworkUprise)}\n" +
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

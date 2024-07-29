using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsUprise : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        public const float PercentOfLevelCost = 0.5f;
        const int SuccessTargetNumber  = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 10;
        public const int BaseRampUpTurns = 40;

        [StarDataConstructor]
        public InfiltrationOpsUprise() { }

        public InfiltrationOpsUprise(Empire owner, Empire them, int levelCost) :
            base((int)(levelCost * PercentOfLevelCost), InfiltrationOpsType.Uprise, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? SuccessTargetNumber / 2 : SuccessTargetNumber);
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them, result);
            Espionage espionage = Owner.GetEspionage(Them);
            var potentials      = Them.GetPlanets().Sorted(p => p.PopulationBillion).TakeItems(5);
            Planet targetPlanet = Them.Random.Item(potentials);
            bool addRebellion   = false;
            int numRebels       = 3;

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    numRebels += 4;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    numRebels += 2;
                    break;
                case InfiltrationOpsResult.Success:
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.NewFailedToInciteUprise;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseMiserable;
                    aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationUpriseMiserableFailVictim);
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationUpriseCriticalFailVictim)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.FailedToInciteUpriseWipedOut;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.NewWipedOutNetworkUprise)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.EffectiveLevel}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (addRebellion)
            {
                aftermath.MessageToVictim = $"{Localizer.Token(GameText.IncitedUpriseOn)} {targetPlanet.Name}";
                aftermath.CustomMessage = $"{Localizer.Token(GameText.WeIncitedUprise)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                Them.AddRebellion(targetPlanet, numRebels);
            }
            aftermath.SendNotifications(Owner.Universe);
        }
    }
}
